// PRTS帮查 · 会话本地存储
//
// 负责三件事：读写、配额裁剪、写入失败降级。
//
// ★ 为什么需要裁剪（这不是过度设计）：
//   页面每次查询结束都会把整个会话列表 uni.setStorageSync 写回一遍，而消息只增不减。
//   小程序单个 key 上限 1MB、整站上限 10MB，长期使用后写入会抛错，
//   表现是「聊着聊着历史记录没了 / 保存失败」，从错误信息根本看不出是容量问题。
//   这里按会话数、消息总数、单条正文长度、闲置天数四个维度兜住。
//
// ★ 为什么 save 不改对象引用：
//   调用方（页面）持有正在流式填充的消息对象，如果 save 返回裁剪后的深拷贝并让页面替换，
//   页面上正在写的 aiMsg 就会指向一个已脱离列表的对象，后续赋值全部丢失。
//   所以这里只负责「写进去」，不回落任何新对象给调用方。

const STORAGE_KEY = "prtsSessions";

// 正常配额
const LIMITS = {
	maxSessions: 30,
	maxMessages: 300,
	maxContent: 8000,
	maxIdleDays: 90
};

// 写入失败后的收紧配额（用户当时还在聊，优先保住最近的）
const LIMITS_TIGHT = {
	maxSessions: 15,
	maxMessages: 120,
	maxContent: 4000,
	maxIdleDays: 30
};

const DAY_MS = 24 * 60 * 60 * 1000;

function sanitizeMessage(raw) {
	if (!raw || typeof raw !== "object" || !raw.id) return null;
	let content = typeof raw.content === "string" ? raw.content : "";
	if (content.length > LIMITS.maxContent) {
		content = content.slice(0, LIMITS.maxContent) + "…";
	}
	return {
		id: raw.id,
		role: raw.role === "user" ? "user" : "assistant",
		content: content,
		channel: typeof raw.channel === "string" ? raw.channel : "",
		time: typeof raw.time === "number" ? raw.time : 0
	};
}

function sanitizeSession(raw) {
	if (!raw || typeof raw !== "object" || !raw.id) return null;
	const messages = Array.isArray(raw.messages) ? raw.messages : [];
	const cleaned = [];
	for (let i = 0; i < messages.length; i++) {
		const m = sanitizeMessage(messages[i]);
		if (m) cleaned.push(m);
	}
	return {
		id: raw.id,
		title: typeof raw.title === "string" ? raw.title : "",
		updatedAt: typeof raw.updatedAt === "number" ? raw.updatedAt : 0,
		clawConversationId: raw.clawConversationId || "",
		messages: cleaned
	};
}

function sortByRecent(list) {
	return list.slice().sort(function (a, b) {
		return (b.updatedAt || 0) - (a.updatedAt || 0);
	});
}

// 纯函数：按配额裁剪，返回新数组。导出是为了单测，页面不要直接用。
export function trimSessions(input, limits) {
	const lim = limits || LIMITS;
	const now = Date.now();
	const list = Array.isArray(input) ? input : [];
	const alive = [];
	for (let i = 0; i < list.length; i++) {
		const s = sanitizeSession(list[i]);
		if (!s) continue;
		// 没有 updatedAt 的老数据当作刚更新，别因为字段缺失就把用户记录删了
		if (s.updatedAt && now - s.updatedAt > lim.maxIdleDays * DAY_MS) continue;
		alive.push(s);
	}
	const sorted = sortByRecent(alive);
	const kept = [];
	let msgBudget = lim.maxMessages;
	for (let i = 0; i < sorted.length; i++) {
		if (kept.length >= lim.maxSessions) break;
		if (msgBudget <= 0) break;
		const s = sorted[i];
		const msgs = s.messages.length > msgBudget ? s.messages.slice(-msgBudget) : s.messages;
		msgBudget -= msgs.length;
		// 文案截断在 sanitizeMessage 已做，这里只控制条数
		kept.push({
			id: s.id,
			title: s.title,
			updatedAt: s.updatedAt,
			clawConversationId: s.clawConversationId,
			messages: msgs
		});
	}
	return kept;
}

// 读取并做一次清洗 + 裁剪。任何异常都退回空列表，不让页面启动失败。
export function loadSessions() {
	let raw = null;
	try {
		raw = uni.getStorageSync(STORAGE_KEY);
	} catch (e) {
		return [];
	}
	if (!raw || !Array.isArray(raw)) return [];
	return trimSessions(raw, LIMITS);
}

// 写入。返回 true 表示确实写进去了。
// 第一次按正常配额，失败则收紧配额重试一次，仍失败才放弃（交给页面提示用户）。
export function saveSessions(list) {
	const attempts = [LIMITS, LIMITS_TIGHT];
	for (let i = 0; i < attempts.length; i++) {
		const trimmed = trimSessions(list, attempts[i]);
		try {
			uni.setStorageSync(STORAGE_KEY, trimmed);
			return true;
		} catch (e) {
			// 容量不足或序列化失败，继续收紧
		}
	}
	return false;
}

export function clearSessions() {
	try {
		uni.removeStorageSync(STORAGE_KEY);
		return true;
	} catch (e) {
		return false;
	}
}

export { LIMITS, LIMITS_TIGHT, STORAGE_KEY };
