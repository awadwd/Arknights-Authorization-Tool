/**
 * 云端同步层
 *
 * 解决两个问题：
 *  1. 次数原先只存 uni storage，用户清缓存就能把「游戏 3 次 / 宜挂 2 次」刷满。
 *     现在次数以服务端为真源，本地只做显示缓存，清掉缓存下次进入会重新拉回真实用量。
 *  2. 个性化设置可以在云端留一份存档，换设备凭存档码取回。
 *
 * 身份：设备指纹。由平台/机型/屏幕等稳定字段哈希成 32 位 hex，不采集、不上传原始信息；
 *     清缓存后重新算出来的值不变，所以「清缓存」不能变成重置次数的手段。
 *
 * 离线策略：所有写操作失败都不阻断本地玩法（该玩还能玩），但会进一个待补交队列，
 *         下次联网时按顺序补交，服务端用 rid 做幂等，不会重复计数。
 */

const API_BASE = 'https://arknightsauthorizationseriesdata-editor.pages.dev';
const DK_KEY = 'cloudDeviceKey';
const PENDING_KEY = 'cloudQuotaPending';
const STATE_KEY = 'cloudQuotaState';

/** 本地缓存的最近一次云端状态，用于首屏同步渲染（真正的值以服务端为准） */
export function getCachedState() {
	try {
		const v = uni.getStorageSync(STATE_KEY);
		if (v && typeof v === 'object') return v;
		if (typeof v === 'string' && v) return JSON.parse(v);
	} catch (e) { /* 缓存坏了按无缓存处理 */ }
	return null;
}

function cacheState(state) {
	try {
		uni.setStorageSync(STATE_KEY, state);
	} catch (e) { /* 写不进去不影响主流程 */ }
}

/** 今天的日期键 YYYY-MM-DD（用设备本地时区，和用户认知一致） */
export function todayStr() {
	const d = new Date();
	const p = (n) => (n < 10 ? '0' + n : '' + n);
	return d.getFullYear() + '-' + p(d.getMonth() + 1) + '-' + p(d.getDate());
}

/** FNV-1a 32 位，加不同种子跑四轮拼成 32 位 hex */
function hash32(str, seed) {
	let h = seed >>> 0;
	for (let i = 0; i < str.length; i++) {
		h ^= str.charCodeAt(i);
		h = Math.imul(h, 16777619);
	}
	return (h >>> 0).toString(16).padStart(8, '0');
}

function hash128(str) {
	return hash32(str, 2166136261) + hash32(str, 0x9e3779b9) +
		hash32(str, 0x85ebca6b) + hash32(str, 0xc2b2ae35);
}

/** 采集稳定字段拼指纹。刻意避开会随版本变化的东西（如完整 UA） */
function fingerprintSource() {
	const parts = [];
	try {
		const si = uni.getSystemInfoSync() || {};
		parts.push(
			si.platform || '',
			si.system || '',
			si.model || '',
			si.brand || '',
			si.screenWidth || '',
			si.screenHeight || '',
			si.pixelRatio || '',
			si.deviceId || '',
			si.deviceBrand || ''
		);
	} catch (e) { /* 取不到就只用后面的字段 */ }
	// #ifdef H5
	try {
		// 只取硬件相关与平台，不取完整 UA（浏览器升级会变，反而导致身份漂移）
		parts.push(navigator.platform || '', String(navigator.hardwareConcurrency || ''));
		parts.push(String(screen.width || '') + 'x' + String(screen.height || ''));
	} catch (e) { /* 忽略 */ }
	// #endif
	return parts.join('|');
}

/**
 * 设备标识：优先用本地已存的（浏览器升级也不会导致次数"重置"），
 * 本地没有时按指纹算一个并持久化。
 */
export function deviceKey() {
	let saved = '';
	try {
		saved = uni.getStorageSync(DK_KEY) || '';
	} catch (e) { /* 忽略 */ }
	if (/^[a-f0-9]{32}$/.test(saved)) return saved;
	const key = hash128(fingerprintSource() + '|akpassport-v1');
	try {
		uni.setStorageSync(DK_KEY, key);
	} catch (e) { /* 写不进去就每次现算，指纹稳定所以值不变 */ }
	return key;
}

function request(path, options) {
	return new Promise((resolve) => {
		let done = false;
		const finish = (v) => { if (!done) { done = true; resolve(v); } };
		const timer = setTimeout(() => finish(null), (options && options.timeout) || 6000);
		try {
			uni.request({
				url: API_BASE + path,
				method: (options && options.method) || 'GET',
				data: options && options.data,
				header: { 'Content-Type': 'application/json' },
				timeout: (options && options.timeout) || 6000,
				success: (res) => {
					clearTimeout(timer);
					if (res && res.statusCode >= 200 && res.statusCode < 300 && res.data) {
						finish(res.data);
					} else {
						finish(null);
					}
				},
				fail: () => { clearTimeout(timer); finish(null); }
			});
		} catch (e) {
			clearTimeout(timer);
			finish(null);
		}
	});
}

/** 拉取当日云端次数；失败返回 null（调用方沿用本地值） */
export async function fetchQuota(day) {
	const d = day || todayStr();
	const data = await request('/api/quota?dk=' + deviceKey() + '&day=' + d);
	if (!data || data.ok !== true) return null;
	cacheState(data);
	return data;
}

/**
 * 上报一次操作
 * @param {'sync'|'consume'|'unlimited'|'grant'|'tasks'} op
 * @param {'game'|'daily'} kind
 * @param {object} extra 额外字段（tasks 数等）
 */
export async function postQuota(op, kind, extra) {
	const body = Object.assign({
		dk: deviceKey(),
		day: todayStr(),
		op: op,
		kind: kind || 'game'
	}, extra || {});
	const data = await request('/api/quota', { method: 'POST', data: body });
	if (!data || data.ok !== true) {
		// 写失败就排队，等下次联网补交
		queuePending(body);
		return null;
	}
	cacheState(data);
	return data;
}

function readPending() {
	try {
		const v = uni.getStorageSync(PENDING_KEY);
		if (Array.isArray(v)) return v;
		if (typeof v === 'string' && v) return JSON.parse(v) || [];
	} catch (e) { /* 忽略 */ }
	return [];
}

function queuePending(body) {
	// 「sync」不需要补交；消费类最多留 20 条，避免长期离线堆积
	if (body.op === 'sync') return;
	const list = readPending();
	list.push(body);
	while (list.length > 20) list.shift();
	try {
		uni.setStorageSync(PENDING_KEY, list);
	} catch (e) { /* 忽略 */ }
}

/** 把离线期间的待补交操作按顺序补上去 */
export async function flushPending() {
	const list = readPending();
	if (!list.length) return 0;
	let sent = 0;
	const remain = [];
	for (let i = 0; i < list.length; i++) {
		const body = list[i];
		// 只补交当天的，跨天的旧账没意义
		if (body.day !== todayStr()) continue;
		const data = await request('/api/quota', { method: 'POST', data: body });
		if (data && data.ok === true) sent++;
		else remain.push(body);
	}
	try {
		uni.setStorageSync(PENDING_KEY, remain);
	} catch (e) { /* 忽略 */ }
	return sent;
}

/** 生成一次性请求 id，供服务端去重 */
export function makeRid() {
	return Date.now().toString(36) + Math.random().toString(36).slice(2, 8);
}

/** 是否已开通云端（服务端可用） */
export async function isCloudReady() {
	const data = await request('/api/quota?dk=' + deviceKey() + '&day=' + todayStr(), { timeout: 4000 });
	return !!(data && data.ok === true);
}

/* ---------------------------------------------------------------- 云存档 */

/**
 * 上传设置存档
 * @param {string} token 压缩后的设置口令（profileShare.encodePrefs 产物）
 * @param {{ code?: string, pin?: string, stat?: object }} opts
 */
export async function archiveSave(token, opts) {
	const o = opts || {};
	return request('/api/archive', {
		method: 'POST',
		data: { p: token, code: o.code || '', pin: o.pin || '', stat: o.stat || null },
		timeout: 10000
	});
}

/** 读取存档 */
export async function archiveLoad(code, pin) {
	const q = '/api/archive?code=' + encodeURIComponent(code || '') +
		(pin ? '&pin=' + encodeURIComponent(pin) : '');
	return request(q, { timeout: 10000 });
}

/** 删除存档 */
export async function archiveDelete(code, pin) {
	const q = '/api/archive?code=' + encodeURIComponent(code || '') +
		(pin ? '&pin=' + encodeURIComponent(pin) : '');
	return request(q, { method: 'DELETE', timeout: 10000 });
}

export default {
	deviceKey,
	todayStr,
	fetchQuota,
	postQuota,
	flushPending,
	makeRid,
	getCachedState,
	isCloudReady,
	archiveSave,
	archiveLoad,
	archiveDelete
};
