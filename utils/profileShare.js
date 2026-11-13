/**
 * 个性化设置「分享 / 跨设备恢复」
 * ==================================================================
 * 两条通道，都不增加服务器成本：
 *
 *   A. 设置口令（无服务端）
 *      把个性化设置压成一段可粘贴的口令 `AKP1…`，丢进聊天窗口、备忘录、
 *      二维码都行。对方在「设置 → 个性化分享」里粘贴即可还原。
 *      纯本地能力，任何端都可用，断网也能用。
 *
 *   B. 六位短码（复用现有 Cloudflare Pages + KV）
 *      POST /api/profile 把口令存进 KV，换一个 6 位短码（如 `K7M2QX`）。
 *      换设备时输短码即可取回；短码 30 天自动过期，可随时用删除码撤销。
 *      KV 走的是编辑器项目已有的 AUTH_STORE 绑定，不新增任何付费资源。
 *
 * 编码：JSON → 短键压缩 → LZ77 → base64url。
 * 一份典型设置（主题 + 布局 + 工具排序 + 头像规则 + 收藏）压完约 120–500 字符。
 */

/* =========================================================================
 * 参与分享的字段（顺序即编码下标，改动会导致旧口令失效，只能追加）
 * ========================================================================= */

const onOff = (v) => (v === true || v === 'true' ? '开' : '关');

export const PREF_FIELDS = [
	{
		key: 'themeMode',
		group: '外观',
		label: '界面主题',
		fmt: (v) => (v === 'ark' ? '科技风格' : '简约风格')
	},
	{
		key: 'homeLayout',
		group: '外观',
		label: '首页布局',
		fmt: (v) => ({ grid: '宫格', list: '列表', minimal: '极简' }[v] || String(v || '宫格'))
	},
	{
		key: 'toolsConfig',
		group: '外观',
		label: '工具箱排序',
		fmt: (v) => {
			const list = Array.isArray(v) ? v : [];
			const hidden = list.filter((t) => t && t.visible === false).length;
			return list.length + ' 个工具' + (hidden ? '（隐藏 ' + hidden + ' 个）' : '');
		}
	},
	{
		key: 'avatarPrefSettings',
		group: '外观',
		label: '头像优先规则',
		fmt: (v) => {
			if (!v || typeof v !== 'object') return '默认';
			const rules = Array.isArray(v.rules) ? v.rules.length : 0;
			const white = Array.isArray(v.white) ? v.white.length : 0;
			const black = Array.isArray(v.black) ? v.black.length : 0;
			const def = Number(v.def) === 2 ? '精二' : '精一';
			const bits = ['默认' + def];
			if (rules) bits.push('规则 ' + rules + ' 条');
			if (white) bits.push('强制精二 ' + white + ' 人');
			if (black) bits.push('强制精一 ' + black + ' 人');
			return bits.join(' · ');
		}
	},
	{ key: 'showMarketPrice', group: '显示', label: '显示市场价', fmt: onOff },
	{ key: 'enableEnglishSearch', group: '搜索', label: '英文搜索', fmt: onOff },
	{ key: 'enableJapaneseSearch', group: '搜索', label: '日文搜索', fmt: onOff },
	{ key: 'enableNicknameSearch', group: '搜索', label: '昵称搜索', fmt: onOff },
	{ key: 'enableGuessData', group: '搜索', label: '猜你想要', fmt: onOff },
	{
		key: 'favoriteBoxIds',
		group: '内容',
		label: '收藏盒号',
		fmt: (v) => (Array.isArray(v) && v.length ? v.length + ' 个' : '无')
	},
	{
		key: 'favoriteCharacterNames',
		group: '内容',
		label: '收藏干员',
		fmt: (v) => (Array.isArray(v) && v.length ? v.length + ' 位' : '无')
	},
	{
		key: 'cardShareTopCollapsed',
		group: '内容',
		label: '展柜顶部收起',
		fmt: onOff
	},
	{ key: 'autoUpdateSetting', group: '更新', label: '自动更新数据', fmt: onOff },
	{ key: 'updateNotifyEnabled', group: '更新', label: '版本更新提醒', fmt: onOff }
];

export const PREF_KEYS = PREF_FIELDS.map((f) => f.key);

/** 云端短码接口（与盒号增量接口同一个 Cloudflare Pages 项目） */
export const PROFILE_API = 'https://arknightauthorizationseriesdata-editor.pages.dev/api/profile';

const TOKEN_HEAD = 'AKP1';

/* =========================================================================
 * UTF-8 / base64url（小程序与 App 端没有 TextEncoder / btoa）
 * ========================================================================= */

function utf8Encode(str) {
	const out = [];
	for (let i = 0; i < str.length; i++) {
		let c = str.charCodeAt(i);
		if (c >= 0xd800 && c <= 0xdbff && i + 1 < str.length) {
			const c2 = str.charCodeAt(i + 1);
			if (c2 >= 0xdc00 && c2 <= 0xdfff) {
				c = ((c - 0xd800) << 10) + (c2 - 0xdc00) + 0x10000;
				i++;
			}
		}
		if (c < 0x80) out.push(c);
		else if (c < 0x800) out.push(0xc0 | (c >> 6), 0x80 | (c & 63));
		else if (c < 0x10000) out.push(0xe0 | (c >> 12), 0x80 | ((c >> 6) & 63), 0x80 | (c & 63));
		else out.push(0xf0 | (c >> 18), 0x80 | ((c >> 12) & 63), 0x80 | ((c >> 6) & 63), 0x80 | (c & 63));
	}
	return out;
}

function utf8Decode(bytes) {
	let out = '';
	let i = 0;
	while (i < bytes.length) {
		const b = bytes[i++];
		if (b < 0x80) out += String.fromCharCode(b);
		else if (b >= 0xc0 && b < 0xe0) out += String.fromCharCode(((b & 31) << 6) | (bytes[i++] & 63));
		else if (b >= 0xe0 && b < 0xf0) {
			out += String.fromCharCode(((b & 15) << 12) | ((bytes[i++] & 63) << 6) | (bytes[i++] & 63));
		} else {
			let cp = ((b & 7) << 18) | ((bytes[i++] & 63) << 12) | ((bytes[i++] & 63) << 6) | (bytes[i++] & 63);
			cp -= 0x10000;
			out += String.fromCharCode(0xd800 + (cp >> 10), 0xdc00 + (cp & 1023));
		}
	}
	return out;
}

const B64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_';
const B64_INDEX = (() => {
	const map = Object.create(null);
	for (let i = 0; i < B64.length; i++) map[B64[i]] = i;
	return map;
})();

function toBase64Url(bytes) {
	let out = '';
	let i = 0;
	for (; i + 2 < bytes.length; i += 3) {
		const n = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2];
		out += B64[(n >> 18) & 63] + B64[(n >> 12) & 63] + B64[(n >> 6) & 63] + B64[n & 63];
	}
	const rest = bytes.length - i;
	if (rest === 1) {
		const n = bytes[i] << 16;
		out += B64[(n >> 18) & 63] + B64[(n >> 12) & 63];
	} else if (rest === 2) {
		const n = (bytes[i] << 16) | (bytes[i + 1] << 8);
		out += B64[(n >> 18) & 63] + B64[(n >> 12) & 63] + B64[(n >> 6) & 63];
	}
	return out;
}

function fromBase64Url(str) {
	const clean = String(str || '').replace(/[^A-Za-z0-9\-_]/g, '');
	const out = [];
	let buffer = 0;
	let bits = 0;
	for (let i = 0; i < clean.length; i++) {
		const v = B64_INDEX[clean[i]];
		if (v === undefined) continue;
		buffer = (buffer << 6) | v;
		bits += 6;
		if (bits >= 8) {
			bits -= 8;
			out.push((buffer >> bits) & 0xff);
		}
	}
	return out;
}

/* =========================================================================
 * LZ77（够用就好的轻量实现：3..130 字节匹配，最大回溯 64KB）
 * ========================================================================= */

const MIN_MATCH = 3;
const MAX_MATCH = 130;
const MAX_OFFSET = 65535;

function lzCompress(bytes) {
	const out = [];
	const head = Object.create(null); // 3 字节 key → 最近出现位置
	let literalStart = 0;
	let i = 0;

	const flushLiteral = (end) => {
		let p = literalStart;
		while (p < end) {
			const run = Math.min(128, end - p);
			out.push(run - 1);
			for (let k = 0; k < run; k++) out.push(bytes[p + k]);
			p += run;
		}
	};

	while (i + MIN_MATCH <= bytes.length) {
		const key = (bytes[i] << 16) | (bytes[i + 1] << 8) | bytes[i + 2];
		const prev = head[key];
		head[key] = i;
		let matched = 0;
		if (prev !== undefined && i - prev <= MAX_OFFSET) {
			const limit = Math.min(MAX_MATCH, bytes.length - i);
			while (matched < limit && bytes[prev + matched] === bytes[i + matched]) matched++;
		}
		if (matched < MIN_MATCH) {
			i++;
			continue;
		}
		flushLiteral(i);
		const offset = i - prev;
		out.push(0x80 | (matched - MIN_MATCH));
		out.push((offset >> 8) & 0xff);
		out.push(offset & 0xff);
		for (let k = 1; k < matched; k++) {
			const p = i + k;
			if (p + MIN_MATCH <= bytes.length) {
				const k2 = (bytes[p] << 16) | (bytes[p + 1] << 8) | bytes[p + 2];
				head[k2] = p;
			}
		}
		i += matched;
		literalStart = i;
	}
	flushLiteral(bytes.length);
	return out;
}

function lzDecompress(bytes) {
	const out = [];
	let i = 0;
	while (i < bytes.length) {
		const ctrl = bytes[i++];
		if (ctrl & 0x80) {
			const len = (ctrl & 0x7f) + MIN_MATCH;
			const offset = (bytes[i++] << 8) | bytes[i++];
			if (!offset) throw new Error('bad token');
			const start = out.length - offset;
			if (start < 0) throw new Error('bad offset');
			for (let k = 0; k < len; k++) out.push(out[start + k]);
		} else {
			const run = ctrl + 1;
			for (let k = 0; k < run; k++) out.push(bytes[i++]);
		}
	}
	return out;
}

/* =========================================================================
 * 口令编解码
 * ========================================================================= */

/** 把设置对象编成口令；数据太短时直接走原文，避免压缩反而变大 */
export function encodePrefs(data) {
	const packed = [];
	PREF_FIELDS.forEach((f, idx) => {
		const v = data ? data[f.key] : undefined;
		if (v === undefined || v === null || v === '') return;
		packed.push([idx, v]);
	});
	const json = JSON.stringify(packed);
	const raw = utf8Encode(json);
	const zipped = lzCompress(raw);
	if (zipped.length + 1 < raw.length) {
		return TOKEN_HEAD + 'z' + toBase64Url(zipped);
	}
	return TOKEN_HEAD + 'r' + toBase64Url(raw);
}

/** 解码口令，失败返回 null */
export function decodePrefs(token) {
	const text = String(token || '').trim();
	if (!text) return null;
	const body = text.replace(/\s+/g, '');
	let payload;
	if (body.indexOf(TOKEN_HEAD + 'z') === 0) {
		try {
			payload = lzDecompress(fromBase64Url(body.slice(TOKEN_HEAD.length + 1)));
		} catch (e) {
			return null;
		}
	} else if (body.indexOf(TOKEN_HEAD + 'r') === 0) {
		payload = fromBase64Url(body.slice(TOKEN_HEAD.length + 1));
	} else {
		return null;
	}
	let packed;
	try {
		packed = JSON.parse(utf8Decode(payload));
	} catch (e) {
		return null;
	}
	if (!Array.isArray(packed)) return null;
	const data = {};
	packed.forEach((pair) => {
		if (!Array.isArray(pair) || pair.length !== 2) return;
		const field = PREF_FIELDS[Number(pair[0])];
		if (field) data[field.key] = pair[1];
	});
	if (!Object.keys(data).length) return null;
	return data;
}

/* =========================================================================
 * 本地读写
 * ========================================================================= */

function readKey(key) {
	try {
		const v = uni.getStorageSync(key);
		return v === '' || v === undefined ? undefined : v;
	} catch (e) {
		return undefined;
	}
}

/** 采集当前设备上的个性化设置 */
export function collectPrefs() {
	const data = {};
	PREF_FIELDS.forEach((f) => {
		const v = readKey(f.key);
		if (v !== undefined && v !== null) data[f.key] = v;
	});
	return data;
}

/**
 * 应用一份设置
 * @returns {{ ok:boolean, n:number, keys:string[] }}
 */
export function applyPrefs(data) {
	const keys = [];
	if (!data || typeof data !== 'object') return { ok: false, n: 0, keys };
	PREF_FIELDS.forEach((f) => {
		if (data[f.key] === undefined) return;
		try {
			uni.setStorageSync(f.key, data[f.key]);
			keys.push(f.key);
		} catch (e) { /* 单项失败不影响其它 */ }
	});
	return { ok: keys.length > 0, n: keys.length, keys };
}

/** 生成预览条目：[{group,label,currentText,incomingText,same}] */
export function diffPrefs(incoming, current) {
	const now = current || collectPrefs();
	const list = [];
	PREF_FIELDS.forEach((f) => {
		if (!incoming || incoming[f.key] === undefined) return;
		const before = f.fmt(now[f.key]);
		const after = f.fmt(incoming[f.key]);
		list.push({ group: f.group, label: f.label, currentText: before, incomingText: after, same: before === after });
	});
	return list;
}

/* =========================================================================
 * 分享链接（仅 H5 有网页地址，App / 小程序降级为纯口令）
 * ========================================================================= */

const PAGE_PATH = '/packageA/profile_share/profile_share';

export function buildShareLink(token) {
	// #ifdef H5
	try {
		const loc = window.location;
		const base = loc.origin + loc.pathname.replace(/index\.html$/, '');
		return base + '#' + PAGE_PATH + '?p=' + token;
	} catch (e) { /* 落回口令 */ }
	// #endif
	return '';
}

/* =========================================================================
 * 短码（Cloudflare Pages + KV）
 * ========================================================================= */

function request(options) {
	return new Promise((resolve) => {
		uni.request({
			timeout: 12000,
			header: { 'content-type': 'application/json' },
			...options,
			success: (res) => resolve({ ok: true, status: res.statusCode, data: res.data }),
			fail: (err) => resolve({ ok: false, error: err })
		});
	});
}

/** 上传口令，换一个短码 */
export async function uploadProfile(token) {
	const res = await request({
		url: PROFILE_API,
		method: 'POST',
		data: { p: token }
	});
	if (!res.ok) return { ok: false, msg: '网络不通，先复制口令用' };
	const body = res.data || {};
	if (body.ok && body.code) return { ok: true, code: body.code, del: body.del, days: body.days };
	return { ok: false, msg: body.error || '短码服务暂不可用' };
}

/** 用短码换回口令 */
export async function fetchProfile(code) {
	const res = await request({
		url: PROFILE_API + '?code=' + encodeURIComponent(String(code || '').trim().toUpperCase()),
		method: 'GET'
	});
	if (!res.ok) return { ok: false, msg: '网络不通' };
	const body = res.data || {};
	if (body.ok && body.p) return { ok: true, p: body.p, at: body.at };
	if (body.error === 'expired' || body.error === 'not_found') return { ok: false, msg: '短码不存在或已过期' };
	return { ok: false, msg: body.error || '短码读取失败' };
}

/** 撤销短码 */
export async function deleteProfile(code, del) {
	const res = await request({
		url: PROFILE_API + '?code=' + encodeURIComponent(String(code || '').trim().toUpperCase()) + '&del=' + encodeURIComponent(String(del || '').trim().toUpperCase()),
		method: 'DELETE'
	});
	if (!res.ok) return { ok: false, msg: '网络不通' };
	const body = res.data || {};
	return body.ok ? { ok: true } : { ok: false, msg: body.error || '撤销失败' };
}

/** 短码形状校验（6 位，去掉了 0/O/1/I 这类易混字符） */
export function isShortCode(text) {
	return /^[2-9A-HJ-NP-Z]{6}$/.test(String(text || '').trim().toUpperCase());
}

/** 从一段文字里抠出 6 位短码（用户可能把带说明的整段说明一起粘进来） */
export function extractShortCode(input) {
	const text = String(input || '').trim().toUpperCase();
	if (isShortCode(text)) return text;
	const m = /(^|[^2-9A-HJ-NP-Z])([2-9A-HJ-NP-Z]{6})(?![2-9A-HJ-NP-Z])/.exec(text);
	return m ? m[2] : '';
}

/** 粘贴进来的内容 → 口令（支持短码、口令、带参数的分享链接） */
export function extractToken(input) {
	const text = String(input || '').trim();
	if (!text) return '';
	// 剪贴板里可能是一整段带说明的文字，只取 AKP1 开头、到第一个非法字符为止
	const hit = new RegExp(TOKEN_HEAD + '[A-Za-z0-9\\-_]*').exec(text);
	if (hit) return hit[0];
	const byLink = /[?&]p=([A-Za-z0-9\-_]+)/.exec(text);
	if (byLink) return byLink[1];
	return '';
}

export default {
	PREF_FIELDS,
	PREF_KEYS,
	PROFILE_API,
	encodePrefs,
	decodePrefs,
	collectPrefs,
	applyPrefs,
	diffPrefs,
	buildShareLink,
	uploadProfile,
	fetchProfile,
	deleteProfile,
	isShortCode,
	extractToken,
	extractShortCode
};
