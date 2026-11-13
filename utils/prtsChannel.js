// PRTS帮查 · 双链路调度
// A 链路 = 腾讯元器 openapi（原有查询逻辑，Bearer AppKey）
// B 链路 = 腾讯云智能体开发平台（ADP）Claw 模式，AppKey 直连
//
// 模式由用户在页面上显式选择（普通模式 / Claw 模式），不做随机分流。
//   普通模式 = 腾讯元器：快、精度低、无多轮上下文
//   Claw 模式 = 腾讯云 ADP：准、较慢、按会话保持上下文
//
// 接口依据（2026-10-27 实测）：
//   POST https://wss.lke.cloud.tencent.com/adp/v2/chat
//   header: Accept: text/event-stream
//   body:   { RequestId, ConversationId, AppKey, VisitorId, Contents, Stream: "disable" }
//   Stream=disable 时服务端仍回 SSE 帧，但正文一次性给出（text.replace / message.done），
//   所以解析端只需要完整的 SSE 文本，不依赖各端 chunk 回调。
//   ConversationId 允许客户端自造（32 位 hex），无需 SecretId / SecretKey 签名。
//   注意：Claw 模式应用不吃 /v1/qbot/chat/sse（会回 460030「该应用类型不支持当前请求」）。
//
// ★ 微信小程序为什么必须走 enableChunked（2026-10-27 修）：
//   该接口回 Content-Type: text/event-stream + Transfer-Encoding: chunked。
//   小程序 wx.request 默认要等响应体结束才回调，遇到 SSE 长流会一直挂到 timeout，
//   报 "request:fail timeout"（App/H5 端正常，只有小程序端复现）。
//   正解 = wx.request 开 enableChunked，用 onChunkReceived 增量收、自己判断终止帧，
//   收到 data: [DONE] 立刻 abort，不等连接关闭。

export const CHANNEL_YUANQI = "yuanqi";
export const CHANNEL_CLAW = "claw";

// ---------------- 模式元信息（文案单一来源） ----------------
// 页面模板、提示条、切换弹窗都从这里取，别再往页面里散写「普通模式 / 精度更高」这类字面量。
export const CHANNEL_META = {
	[CHANNEL_YUANQI]: {
		key: CHANNEL_YUANQI,
		name: "普通模式",
		badge: "普通",
		speed: "快",
		hint: "响应更快，但精度较低",
		notice: "普通模式：响应更快，但精度较低、不支持多轮上下文，结果仅供参考哟~"
	},
	[CHANNEL_CLAW]: {
		key: CHANNEL_CLAW,
		name: "Claw 模式",
		badge: "Claw",
		speed: "准",
		hint: "精度更高，但响应较慢",
		notice: "Claw 模式：腾讯云 ADP 提供，盒号更准、支持多轮追问，但响应较慢。"
	}
};

// 传入任何非法值都退回普通模式，页面不用再自己兜底
export function channelMeta(c) {
	return CHANNEL_META[c] || CHANNEL_META[CHANNEL_YUANQI];
}

// 腾讯云站对话端点（独立站为 https://adp.tencent.com/adp/v2/chat，本项目用腾讯云站）
export const CLAW_CHAT_ENDPOINT = "https://wss.lke.cloud.tencent.com/adp/v2/chat";
// ADP 应用 AppKey（Claw 模式应用，控制台「调用信息」里生成）
export const CLAW_APP_KEY =
	"MmwufMcFLOAfbeOEXfLovtNoclgmxSMxaYwosDTCkLwYcwrqgVjLzSFztKtTZaZZiMDtSbFQDKdmXCogThbVLCMWycxWuLkXRUftezkPVOYZKzgYkxlQUMLLIThqqXen";

// Claw 单次请求超时（毫秒）。小程序端给得短一些：超时就提示换模式，别让用户干等
const CLAW_TIMEOUT_MP = 45000;
const CLAW_TIMEOUT_APP = 60000;

function hex32() {
	let s = "";
	const dict = "0123456789abcdef";
	for (let i = 0; i < 32; i++) s += dict.charAt(Math.floor(Math.random() * 16));
	return s;
}

// Claw 会话 ID：客户端自造，32 位 hex。同一会话固定复用以保留多轮上下文
export function genConversationId() {
	return hex32();
}

// 从 message 的 Contents 里拼出正文
function textsOf(message) {
	const list = message && message.Contents;
	if (!list || !list.length) return "";
	let s = "";
	for (let i = 0; i < list.length; i++) {
		const c = list[i];
		if (c && c.Type === "text" && c.Text) s += c.Text;
	}
	return s;
}

// 解析 SSE 文本。返回 { content, error, conversationId }
// 事件顺序：request_ack → response.created → message.added(tool_call) → … →
//           message.added(reply) → text.replace / text.delta → message.done → response.completed → done
export function parseClawReply(raw) {
	const out = { content: "", error: "", conversationId: "" };
	if (!raw) return out;

	if (typeof raw !== "string") {
		// 个别平台会把 SSE 直接当 JSON 解出来，兜底从 Messages 里取
		const msgs = (raw && raw.Response && raw.Response.Messages) || [];
		for (let i = 0; i < msgs.length; i++) {
			if (msgs[i] && msgs[i].Type === "reply") out.content = textsOf(msgs[i]) || out.content;
		}
		if (raw && raw.Type === "error") {
			out.error = (raw.Error && raw.Error.Message) || "未知错误";
		}
		return out;
	}

	const lines = raw.split(/\r?\n/);
	const replyIds = {};
	const toolIds = {};
	let delta = "";
	let replaced = "";
	let looseReplace = "";
	let doneText = "";
	let completedText = "";

	for (let i = 0; i < lines.length; i++) {
		const line = lines[i].trim();
		if (line.indexOf("data:") !== 0) continue;
		const payload = line.slice(5).trim();
		if (!payload || payload === "[DONE]") continue;

		let evt;
		try {
			evt = JSON.parse(payload);
		} catch (e) {
			continue;
		}
		if (!evt || !evt.Type) continue;

		if (evt.Type === "error") {
			out.error = (evt.Error && evt.Error.Message) || "ADP 返回错误";
			continue;
		}

		if (evt.Type === "message.added" && evt.Message) {
			if (evt.Message.Type === "reply") replyIds[evt.MessageId] = 1;
			else if (evt.Message.Type === "tool_call") toolIds[evt.MessageId] = 1;
			continue;
		}

		if (evt.Type === "text.delta") {
			// 只收 reply 消息的增量；tool_call 的 text.delta 是工具入参，不能当答案
			if (evt.Text && replyIds[evt.MessageId]) delta += evt.Text;
			continue;
		}

		if (evt.Type === "text.replace") {
			if (typeof evt.Text !== "string") continue;
			if (replyIds[evt.MessageId]) replaced = evt.Text;
			else if (!toolIds[evt.MessageId]) looseReplace = evt.Text;
			continue;
		}

		if (evt.Type === "message.done" && evt.Message && evt.Message.Type === "reply") {
			doneText = textsOf(evt.Message) || doneText;
			continue;
		}

		if (evt.Type === "response.completed" && evt.Response) {
			out.conversationId = evt.Response.ConversationId || out.conversationId;
			const msgs = evt.Response.Messages || [];
			for (let k = 0; k < msgs.length; k++) {
				if (msgs[k] && msgs[k].Type === "reply") {
					completedText = textsOf(msgs[k]) || completedText;
				}
			}
		}
	}

	// done 最权威（最终态），其次 replace，再退到 response.completed 里的整段与 delta 拼接
	out.content = doneText || replaced || completedText || looseReplace || delta;
	return out;
}

// 统一构造请求体
function buildClawBody(message, cid, visitorId) {
	return {
		RequestId: hex32(),
		ConversationId: cid,
		AppKey: CLAW_APP_KEY,
		VisitorId: visitorId || "prts_visitor",
		Contents: [{ Type: "text", Text: message }],
		Incremental: false,
		EnableMultiIntent: true,
		Stream: "disable"
	};
}

// 带 code 的错误，页面据此给不同文案
function clawError(message, code) {
	const e = new Error(message);
	e.code = code || "claw";
	return e;
}

// 把原始 SSE 文本解析成最终结果
function settleFromRaw(raw, cid, resolve, reject) {
	if (!raw) {
		reject(clawError("ADP 没有返回内容", "empty"));
		return;
	}
	const parsed = parseClawReply(raw);
	if (parsed.error) {
		reject(clawError("ADP：" + parsed.error, "server"));
		return;
	}
	if (!parsed.content) {
		reject(clawError("ADP 没有返回有效内容", "empty"));
		return;
	}
	resolve({
		content: parsed.content,
		channel: CHANNEL_CLAW,
		conversationId: parsed.conversationId || cid
	});
}

// ---------------- 小程序专用：SSE 分块接收 ----------------
// #ifdef MP-WEIXIN
// UTF-8 解码。返回 { text, rest }，rest 是块边界上被切断、还没凑齐的字节
function utf8Decode(bytes) {
	let out = "";
	let i = 0;
	const n = bytes.length;
	while (i < n) {
		const b = bytes[i];
		if (b < 0x80) {
			out += String.fromCharCode(b);
			i += 1;
		} else if (b >= 0xc2 && b <= 0xdf) {
			if (i + 1 >= n) break;
			out += String.fromCharCode(((b & 0x1f) << 6) | (bytes[i + 1] & 0x3f));
			i += 2;
		} else if (b >= 0xe0 && b <= 0xef) {
			if (i + 2 >= n) break;
			out += String.fromCharCode(((b & 0x0f) << 12) | ((bytes[i + 1] & 0x3f) << 6) | (bytes[i + 2] & 0x3f));
			i += 3;
		} else if (b >= 0xf0 && b <= 0xf4) {
			if (i + 3 >= n) break;
			const cp =
				((b & 0x07) << 18) |
				((bytes[i + 1] & 0x3f) << 12) |
				((bytes[i + 2] & 0x3f) << 6) |
				(bytes[i + 3] & 0x3f);
			const c = cp - 0x10000;
			out += String.fromCharCode(0xd800 + (c >> 10), 0xdc00 + (c & 0x3ff));
			i += 4;
		} else {
			// 非法起始字节，跳过
			i += 1;
		}
	}
	return { text: out, rest: bytes.slice(i) };
}

// 拼接上一块残留 + 新块，再解码
function decodeChunk(carry, data) {
	if (typeof data === "string") return { text: data, rest: new Uint8Array(0) };
	let inc = null;
	if (data instanceof ArrayBuffer) inc = new Uint8Array(data);
	else if (data && typeof data.byteLength === "number") inc = new Uint8Array(data);
	if (!inc) return { text: "", rest: carry };
	const merged = new Uint8Array(carry.length + inc.length);
	merged.set(carry, 0);
	merged.set(inc, carry.length);
	return utf8Decode(merged);
}

// 终止帧判定：event: done（紧跟 [DONE]）或 response.completed
function isStreamFinished(raw) {
	if (!raw) return false;
	return (
		raw.indexOf("event: done") >= 0 ||
		raw.indexOf("data: [DONE]") >= 0 ||
		raw.indexOf('"Type":"response.completed"') >= 0 ||
		raw.indexOf('"Type": "response.completed"') >= 0
	);
}

// 把已收到的 SSE 文本归约成用户可理解的阶段（不暴露工具名/参数/TraceId）
//   thinking   已发出，模型在想
//   querying   模型调了工具，正在查数据
//   composing  工具已返回，正在组织答案
//   answering  正文开始出现
function createStageScanner() {
	const toolIds = {};
	let stage = "thinking";
	return {
		stage: function () {
			return stage;
		},
		// 只喂以 \n 收尾的完整片段，跨块的半行留到下一次
		feed: function (seg) {
			const lines = seg.split("\n");
			for (let i = 0; i < lines.length; i++) {
				const t = lines[i].trim();
				if (t.indexOf("data:") !== 0) continue;
				const p = t.slice(5).trim();
				if (!p || p === "[DONE]") continue;
				let e;
				try {
					e = JSON.parse(p);
				} catch (err) {
					continue;
				}
				if (!e || !e.Type) continue;
				const mt = (e.Message && e.Message.Type) || "";
				if (e.Type === "message.added" && mt === "tool_call" && e.MessageId) {
					toolIds[e.MessageId] = 1;
					stage = "querying";
				} else if (e.Type === "message.done" && mt === "tool_call") {
					stage = "composing";
				} else if (e.Type === "message.done" && mt === "reply") {
					stage = "answering";
				} else if (e.Type === "text.replace" || e.Type === "text.delta") {
					// 工具入参的增量不是答案，不能算作答
					if (!toolIds[e.MessageId]) stage = "answering";
				}
			}
			return stage;
		}
	};
}

// 小程序端失败归类
function classifyMPFail(err) {
	const m = (err && (err.errMsg || err.message)) || "未知错误";
	if (m.indexOf("abort") >= 0) return { message: "请求已取消", code: "abort" };
	if (m.indexOf("timeout") >= 0) {
		return {
			message: "Claw 响应超时（已等待 " + Math.round(CLAW_TIMEOUT_MP / 1000) + " 秒）",
			code: "timeout"
		};
	}
	if (m.indexOf("domain") >= 0 || m.indexOf("合法域名") >= 0) {
		return { message: "小程序未配置合法域名 wss.lke.cloud.tencent.com", code: "domain" };
	}
	if (m.indexOf("-118") >= 0) {
		return { message: "连接被中断（网络或证书校验失败）", code: "net" };
	}
	return { message: "网络请求失败：" + m, code: "net" };
}

function askClawMP(message, cid, visitorId, onStage) {
	return new Promise((resolve, reject) => {
		let raw = "";
		let carry = new Uint8Array(0);
		let settled = false;
		let task = null;
		let lastStage = "";
		let scanPos = 0;
		const scanner = createStageScanner();

		function emitStage(stage) {
			if (!onStage || !stage || stage === lastStage) return;
			lastStage = stage;
			try {
				onStage(stage);
			} catch (e) {}
		}

		function finish() {
			if (settled) return;
			settled = true;
			// 补扫最后一段没以 \n 收尾的内容
			if (raw.length > scanPos) {
				scanner.feed(raw.slice(scanPos));
				scanPos = raw.length;
				emitStage(scanner.stage());
			}
			// 已经拿到终止帧，主动断开，避免 keep-alive 挂到超时
			try {
				if (task && typeof task.abort === "function") task.abort();
			} catch (e) {}
			settleFromRaw(raw, cid, resolve, reject);
		}

		function giveUp(err) {
			if (settled) return;
			settled = true;
			const c = classifyMPFail(err);
			reject(clawError(c.message, c.code));
		}

		emitStage("thinking");

		task = wx.request({
			url: CLAW_CHAT_ENDPOINT,
			method: "POST",
			timeout: CLAW_TIMEOUT_MP,
			enableChunked: true,
			responseType: "text",
			header: {
				"Content-Type": "application/json",
				Accept: "text/event-stream"
			},
			data: buildClawBody(message, cid, visitorId),
			success(res) {
				if (settled) return;
				if (res && typeof res.data === "string" && res.data.length > raw.length) {
					raw = res.data;
				}
				if (!raw) {
					settled = true;
					reject(clawError("ADP 请求失败：HTTP " + (res && res.statusCode), "http"));
					return;
				}
				finish();
			},
			fail(err) {
				giveUp(err);
			}
		});

		// 基础库 2.20.1+ 才有；没有就退回 success 一次性收
		if (task && typeof task.onChunkReceived === "function") {
			task.onChunkReceived(function (res) {
				if (settled) return;
				const decoded = decodeChunk(carry, (res || {}).data);
				carry = decoded.rest;
				raw += decoded.text;
				const cut = raw.lastIndexOf("\n");
				if (cut > scanPos) {
					scanner.feed(raw.slice(scanPos, cut));
					scanPos = cut + 1;
					emitStage(scanner.stage());
				}
				if (isStreamFinished(raw)) finish();
			});
		}
	});
}
// #endif

// ---------------- App / H5：uni.request 一次收全 ----------------
// #ifndef MP-WEIXIN
function askClawRequest(message, cid, visitorId) {
	return new Promise((resolve, reject) => {
		uni.request({
			url: CLAW_CHAT_ENDPOINT,
			method: "POST",
			timeout: CLAW_TIMEOUT_APP,
			// dataType / responseType 都显式声明为文本，避免被当成 JSON 预解析
			dataType: "text",
			responseType: "text",
			header: {
				"Content-Type": "application/json",
				Accept: "text/event-stream"
			},
			data: buildClawBody(message, cid, visitorId),
			success: (res) => {
				const parsed = parseClawReply(res && res.data);
				if (parsed.error) {
					reject(clawError("ADP：" + parsed.error, "server"));
					return;
				}
				if (res && res.statusCode !== 200 && !parsed.content) {
					reject(clawError("ADP 请求失败：" + res.statusCode, "http"));
					return;
				}
				if (!parsed.content) {
					reject(clawError("ADP 没有返回有效内容", "empty"));
					return;
				}
				resolve({
					content: parsed.content,
					channel: CHANNEL_CLAW,
					conversationId: parsed.conversationId || cid
				});
			},
			fail: (err) => {
				const m = (err && err.errMsg) || "未知错误";
				const code = m.indexOf("timeout") >= 0 ? "timeout" : "net";
				reject(clawError("ADP 网络请求失败：" + m, code));
			}
		});
	});
}
// #endif

// 调用 ADP Claw 对话接口
// message: 用户提问
// conversationId: 会话 ID（同一会话传同一个）
// visitorId: 稳定用户标识
export function askClaw(message, conversationId, visitorId, onStage) {
	const cid = conversationId || genConversationId();
	// #ifdef MP-WEIXIN
	return askClawMP(message, cid, visitorId, onStage);
	// #endif
	// #ifndef MP-WEIXIN
	return askClawRequest(message, cid, visitorId);
	// #endif
}
// ---------------- 普通模式：腾讯元器 openapi ----------------
// 原先这段网络代码写在页面 methods 里，导致两条链路的返回结构和错误语义不一致：
// Claw 回 { content, channel }，元器回 { content, thinking }，页面拿 result.channel 永远是空，
// 所以普通模式的回答从来不显示链路标签。这里统一成 { content, channel } + 带 code 的错误。
export const YUANQI_APPKEY = "j2igdTjEliGF84QH75irkWLc51mpmP26";
export const YUANQI_ASSISTANT_ID = "2011824824720938752";
export const YUANQI_BASE_URL = "https://yuanqi.tencent.com";

function yuanqiError(message, code) {
	const err = new Error(message);
	err.code = code;
	return err;
}

// 把元器返回体归一成纯文本。纯函数、不碰 uni，方便单独测。
export function parseYuanqiReply(data) {
	if (!data || !data.choices || !data.choices.length) {
		return { error: "PRTS 返回格式异常", code: "server" };
	}
	const choice = data.choices[0] || {};
	const message = choice.message || {};
	let content = "";
	const list = message.content;
	if (Array.isArray(list)) {
		for (let i = 0; i < list.length; i++) {
			const item = list[i];
			if (item && item.type === "text" && item.text) content += item.text;
		}
	} else if (typeof list === "string") {
		content = list;
	}
	if (!content) {
		const detail = (data.error && data.error.message) || "PRTS 没有返回有效内容";
		return { error: detail, code: "empty" };
	}
	let thinking = "";
	if (Array.isArray(message.steps)) {
		for (let i = 0; i < message.steps.length; i++) {
			const step = message.steps[i];
			if (step && step.content) thinking += step.content + "\n";
		}
	}
	return { content: content, thinking: thinking };
}

function classifyYuanqiFail(errMsg) {
	const m = errMsg || "";
	if (/timeout/i.test(m)) return yuanqiError("普通模式响应超时", "timeout");
	if (/domain|合法域名/i.test(m)) return yuanqiError("普通模式域名未在小程序后台配置", "domain");
	if (/abort/i.test(m)) return yuanqiError("请求已取消", "abort");
	return yuanqiError("网络请求失败：" + (m || "未知错误"), "net");
}

// opts: { appkey, assistantId, baseUrl, userId }
export function askYuanqi(message, opts) {
	const o = opts || {};
	const appkey = o.appkey || YUANQI_APPKEY;
	const assistantId = o.assistantId || YUANQI_ASSISTANT_ID;
	const baseUrl = o.baseUrl || YUANQI_BASE_URL;
	return new Promise(function (resolve, reject) {
		uni.request({
			url: baseUrl + "/openapi/v1/agent/chat/completions",
			method: "POST",
			header: {
				"Content-Type": "application/json",
				"Authorization": "Bearer " + appkey
			},
			data: {
				assistant_id: assistantId,
				user_id: o.userId || "",
				stream: false,
				messages: [
					{ role: "user", content: [{ type: "text", text: message }] }
				]
			},
			success: function (res) {
				const status = res.statusCode;
				if (status === 401 || status === 403) {
					reject(yuanqiError("元器 API 认证失败（" + status + "）", "auth"));
					return;
				}
				if (status === 404) {
					reject(yuanqiError("智能体不存在（404）", "notfound"));
					return;
				}
				if (status === 429) {
					reject(yuanqiError("请求过于频繁（429）", "rate"));
					return;
				}
				if (status !== 200) {
					reject(yuanqiError("元器请求失败：" + status, "http"));
					return;
				}
				const parsed = parseYuanqiReply(res.data);
				if (parsed.error) {
					reject(yuanqiError(parsed.error, parsed.code));
					return;
				}
				resolve({
					content: parsed.content,
					thinking: parsed.thinking,
					channel: CHANNEL_YUANQI
				});
			},
			fail: function (err) {
				reject(classifyYuanqiFail((err && err.errMsg) || ""));
			}
		});
	});
}
