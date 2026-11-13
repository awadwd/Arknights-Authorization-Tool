// utils/firstRunData.js
//
// 首次启动 / 本地无数据时的冷启动加载。
// 只走自建增量 API（pages.dev，已确认带 Access-Control-Allow-Origin: *），
// 不重复实现移动端那套「多源完整包回退」链路：
//   1. 本地已有数据 → 直接用
//   2. manifest 拿全部盒号 id 与版本
//   3. records 分批取完整记录（单批 200）
//
// 注意：写入 currentData 的是「上游原始记录」，与移动端首页 adopt 的口径一致。

import { SELF_HOSTED_BASE } from './boxDataSync.js';

const API = SELF_HOSTED_BASE + '/api/box-data';
const BATCH = 200;

function getJson(url, timeout) {
	return new Promise((resolve) => {
		uni.request({
			url,
			method: 'GET',
			timeout: timeout || 12000,
			dataType: 'text',
			success: (res) => {
				if (res.statusCode !== 200) {
					resolve(null);
					return;
				}
				let body = res.data;
				if (typeof body === 'string') {
					try {
						body = JSON.parse(body);
					} catch (e) {
						resolve(null);
						return;
					}
				}
				resolve(body && typeof body === 'object' ? body : null);
			},
			fail: () => resolve(null),
		});
	});
}

/** 本地已有的盒号数据（兼容移动端两个历史键名） */
export function localBoxes() {
	try {
		const current = uni.getStorageSync('currentData');
		if (Array.isArray(current) && current.length) return current;

		const legacy = uni.getStorageSync('arknightsData');
		if (Array.isArray(legacy) && legacy.length) return legacy;
	} catch (e) {
		// ignore
	}
	return null;
}

/** 一个盒号里有多少位干员。上游字段是 character1..characterN。 */
export function boxCharacterCount(box) {
	if (!box) return 0;

	if (Array.isArray(box.Characters)) return box.Characters.length;
	if (Array.isArray(box.characters)) return box.characters.length;

	let count = 0;
	for (let i = 1; i <= 24; i++) {
		if (box['character' + i]) count++;
	}
	return count;
}

/**
 * 取盒号数据。有本地就返回本地，没有就联网拉一次并落盘。
 * @returns {Promise<{data: Array, source: 'cache'|'network', version: string} | null>}
 */
export async function loadBoxData(opts) {
	opts = opts || {};

	if (!opts.force) {
		const cached = localBoxes();
		if (cached) return { data: cached, source: 'cache', version: '' };
	}

	const manifest = await getJson(API + '/manifest');
	if (!manifest || !Array.isArray(manifest.items) || manifest.items.length === 0) return null;

	const ids = manifest.items.map((item) => item[0]).filter(Boolean);
	const records = {};

	for (let i = 0; i < ids.length; i += BATCH) {
		const chunk = ids.slice(i, i + BATCH);
		const url = API + '/records?ids=' + chunk.map(encodeURIComponent).join(',');
		const payload = await getJson(url, 20000);
		if (!payload || !payload.records) return null;
		Object.assign(records, payload.records);
	}

	const data = Object.values(records);
	if (data.length === 0) return null;

	try {
		uni.setStorageSync('currentData', data);
		uni.setStorageSync('arknightsData', data);
		uni.setStorageSync('currentDataVersion', manifest.version || '');
	} catch (e) {
		// 存不下也不影响本次展示
	}

	return { data, source: 'network', version: manifest.version || '' };
}
