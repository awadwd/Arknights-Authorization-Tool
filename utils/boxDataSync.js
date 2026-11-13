// utils/boxDataSync.js
//
// 盒号数据「增量更新」客户端引擎
// ---------------------------------------------------------------------------
// 与服务端 Cloudflare Functions 配合（自建 API）：
//   GET /api/box-data/manifest?light=1   → 只回版本号（约 200 字节），用于「有没有更新」
//   GET /api/box-data/manifest           → 回每个盒号的 8 位指纹（93 盒约 3KB）
//   GET /api/box-data/records?ids=a,b    → 只回变化盒号的完整记录（单次上限 500）
//
// 相比每次整包重下 Box_Id.json（约 158KB 且上游未压缩），
// 常态下「没有更新」时只花约 200 字节；「个别盒号更新」时只拉那几条。
//
// 设计原则：
//   1. 只做「增量」，不做「全量」。拿不到增量条件时一律返回 null，
//      由调用方回退到原有的多源完整包下载链，绝不影响既有逻辑。
//   2. 任一步失败即整体放弃（不产出半套数据），避免本地数据被写坏。
//   3. 缓存必须存「上游原始记录」——因为服务端指纹是按原始字段算的；
//      页面里清洗过的 arknightsData（数字被转成字符串）算出来必然对不上。
//      因此全量下载成功后需要调用 adopt(rawData, version) 回填原始数据。
// ---------------------------------------------------------------------------

export const SELF_HOSTED_BASE = "https://arknightsauthorizationseriesdata-editor.pages.dev";

const MANIFEST_URL = SELF_HOSTED_BASE + "/api/box-data/manifest";
const RECORDS_URL = SELF_HOSTED_BASE + "/api/box-data/records";

const CACHE_KEY = "boxSyncCacheV1"; // { version, savedAt, boxes: [原始记录] }
const CFG_KEY = "boxSyncEnabled";   // 允许用户/调试关闭（默认开）
const BATCH = 200;                  // 单次请求盒号数（服务端上限 500，留余量）
const RECENT_MS = 6 * 60 * 60 * 1000; // 6 小时内不重复探版本（同一会话内已有内存短路）

// ---------------------------------------------------------------------------
// 指纹：必须与服务端 functions/api/_lib/boxData.js 完全一致，否则差异计算失效
// ---------------------------------------------------------------------------

export function hash(str) {
	let h = 0x811c9dc5;
	for (let i = 0; i < str.length; i++) {
		h ^= str.charCodeAt(i);
		h = (h + ((h << 1) + (h << 4) + (h << 7) + (h << 8) + (h << 24))) >>> 0;
	}
	return ("00000000" + h.toString(16)).slice(-8);
}

export function fingerprint(box) {
	const skip = { updatedAt: 1, updateTime: 1, lastModified: 1 };
	const keys = Object.keys(box).filter((k) => !skip[k]).sort();
	const parts = [];
	for (const k of keys) {
		const v = box[k];
		if (v === null || v === undefined) {
			parts.push(k + ":");
		} else if (typeof v === "object") {
			parts.push(k + ":" + JSON.stringify(v));
		} else {
			parts.push(k + ":" + v);
		}
	}
	return parts.join("|");
}

function fpOf(box) {
	try {
		return hash(fingerprint(box));
	} catch (e) {
		return "";
	}
}

// ---------------------------------------------------------------------------
// 基础设施
// ---------------------------------------------------------------------------

function log(...a) {
	try {
		console.log("[boxDataSync]", ...a);
	} catch (e) {}
}

/** 本地/自定义数据源激活时，绝不允许联网 */
export function isEnabled() {
	try {
		if (uni.getStorageSync(CFG_KEY) === false) return false;
		const ds = uni.getStorageSync("dataSource");
		if (ds === "local" || ds === "custom") return false;
	} catch (e) {}
	return true;
}

export function setEnabled(v) {
	try {
		uni.setStorageSync(CFG_KEY, !!v);
	} catch (e) {}
}

/** 统一的请求：强制 text 再自行解析（真机 uni.request 会自动把 JSON 转成对象，统一处理更稳） */
function requestJson(url, timeout) {
	return new Promise((resolve) => {
		uni.request({
			url: url,
			method: "GET",
			timeout: timeout || 8000,
			dataType: "text",
			success: (res) => {
				if (res.statusCode !== 200) {
					resolve(null);
					return;
				}
				let d = res.data;
				if (typeof d === "string") {
					try {
						d = JSON.parse(d);
					} catch (e) {
						resolve(null);
						return;
					}
				}
				resolve(d && typeof d === "object" ? d : null);
			},
			fail: () => resolve(null)
		});
	});
}

function readCache() {
	try {
		const c = uni.getStorageSync(CACHE_KEY);
		if (c && typeof c === "object" && Array.isArray(c.boxes) && c.boxes.length) {
			return c;
		}
	} catch (e) {}
	return null;
}

function writeCache(c) {
	try {
		uni.setStorageSync(CACHE_KEY, c);
	} catch (e) {
		log("缓存写入失败（可能超出存储配额）", e);
	}
}

// ---------------------------------------------------------------------------
// 对外 API
// ---------------------------------------------------------------------------

/** 同步读取本地缓存的原始盒号数组；没有则返回 null */
export function peek() {
	const c = readCache();
	return c ? c.boxes : null;
}

export function getVersion() {
	const c = readCache();
	return c ? c.version || "" : "";
}

export function stats() {
	const c = readCache();
	return {
		enabled: isEnabled(),
		hasCache: !!c,
		version: c ? c.version : "",
		count: c ? (c.boxes || []).length : 0,
		savedAt: c ? c.savedAt : 0
	};
}

export function clear() {
	try {
		uni.removeStorageSync(CACHE_KEY);
		log("缓存已清空");
	} catch (e) {}
}

/**
 * 全量下载成功后回填「原始数据」，让下次启动能走增量。
 * 必须在页面清洗数据之前、用上游原始 rawData 调用。
 * @param {Array} rawBoxes 上游原始记录数组
 * @param {string} version 版本号（可空，空则下次会重新对齐）
 */
export function adopt(rawBoxes, version) {
	if (!Array.isArray(rawBoxes) || !rawBoxes.length) return false;
	try {
		writeCache({
			version: version || "",
			savedAt: Date.now(),
			boxes: rawBoxes
		});
		log("已回填原始数据，盒号数 =", rawBoxes.length, "版本 =", version || "(空)");
		return true;
	} catch (e) {
		return false;
	}
}

let _inflight = null;
let _memVersion = "";   // 会话内短路：同一次运行里版本没变就不再探版本
let _memAt = 0;

/**
 * 走增量路径取数据。
 * @returns {Promise<null | {data:Array, version:string, source:string, changed:number, fromCache:boolean}>}
 *          返回 null 表示「未能走增量」，调用方应回退到原有完整包下载。
 */
export function ensure(opts) {
	opts = opts || {};
	if (_inflight) return _inflight;
	_inflight = _run(opts).then(
		(r) => {
			_inflight = null;
			return r;
		},
		(e) => {
			_inflight = null;
			log("增量异常，降级完整包", e);
			return null;
		}
	);
	return _inflight;
}

async function _run(opts) {
	if (!isEnabled()) return null;

	const cache = readCache();
	// 没有任何本地原始数据 → 无从比对差异，交给完整包
	if (!cache) return null;

	// 会话内短路（同一版本不重复探）
	const now = Date.now();
	if (!opts.force && _memVersion && _memVersion === cache.version && now - _memAt < RECENT_MS) {
		return {
			data: cache.boxes,
			version: cache.version,
			source: "增量·本次已校验",
			changed: 0,
			fromCache: true
		};
	}

	// 1) 版本探针（最省的一步）
	const light = await requestJson(MANIFEST_URL + "?light=1", 6000);
	if (!light || !light.ok || !light.version) {
		log("版本探针不可用，降级完整包");
		return null;
	}

	if (cache.version && cache.version === light.version) {
		cache.savedAt = Date.now();
		writeCache(cache);
		_memVersion = light.version;
		_memAt = now;
		log("版本未变，命中本地缓存", light.version);
		return {
			data: cache.boxes,
			version: light.version,
			source: "增量·无变化",
			changed: 0,
			fromCache: true
		};
	}

	// 2) 指纹清单
	const man = await requestJson(MANIFEST_URL, 8000);
	if (!man || !man.ok || !Array.isArray(man.items)) {
		log("指纹清单不可用，降级完整包");
		return null;
	}

	// 3) 与本地指纹比对
	const localFp = {};
	for (const b of cache.boxes) {
		if (!b || typeof b !== "object") continue;
		localFp[String(b.Box_id || "")] = fpOf(b);
	}
	const serverIds = [];
	const changedIds = [];
	for (const it of man.items) {
		if (!it || it.length < 2) continue;
		const id = String(it[0]);
		serverIds.push(id);
		if (localFp[id] !== it[1]) changedIds.push(id);
	}
	const removedIds = Object.keys(localFp).filter((id) => id && serverIds.indexOf(id) < 0);

	if (!changedIds.length && !removedIds.length) {
		cache.version = man.version;
		cache.savedAt = Date.now();
		writeCache(cache);
		_memVersion = man.version;
		_memAt = now;
		log("指纹全一致，无变化", man.version);
		return {
			data: cache.boxes,
			version: man.version,
			source: "增量·无变化",
			changed: 0,
			fromCache: true
		};
	}

	// 4) 分身拉变化记录（任一批失败 → 整体放弃，避免半套数据）
	const map = {};
	for (const b of cache.boxes) {
		if (!b || typeof b !== "object") continue;
		map[String(b.Box_id || "")] = b;
	}

	let fetched = 0;
	for (let i = 0; i < changedIds.length; i += BATCH) {
		const batch = changedIds.slice(i, i + BATCH);
		const url = RECORDS_URL + "?ids=" + encodeURIComponent(batch.join(","));
		const rec = await requestJson(url, 12000);
		if (!rec || !rec.ok || !rec.records || typeof rec.records !== "object") {
			log("差异记录拉取失败，降级完整包", batch.length);
			return null;
		}
		for (const k in rec.records) {
			if (Object.prototype.hasOwnProperty.call(rec.records, k)) {
				map[k] = rec.records[k];
				fetched++;
			}
		}
	}

	for (const id of removedIds) delete map[id];

	// 5) 按服务端清单顺序重排，保证顺序稳定可复现
	const boxes = [];
	for (const id of serverIds) {
		if (map[id]) boxes.push(map[id]);
	}

	if (!boxes.length) {
		log("合并结果为空，降级完整包");
		return null;
	}

	writeCache({ version: man.version, savedAt: Date.now(), boxes: boxes });
	_memVersion = man.version;
	_memAt = now;
	log("增量完成：更新", changedIds.length, "删除", removedIds.length, "结果", boxes.length);

	return {
		data: boxes,
		version: man.version,
		source: "增量(" + changedIds.length + "改" + removedIds.length + "删)",
		changed: changedIds.length + removedIds.length,
		fromCache: false
	};
}

export default {
	ensure,
	adopt,
	peek,
	getVersion,
	stats,
	clear,
	isEnabled,
	setEnabled,
	hash,
	fingerprint
};
