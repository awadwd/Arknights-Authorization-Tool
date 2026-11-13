// utils/guessData.js
//
// 正式盒号数据 与 预测盒号数据 的合并 / 去重
// ---------------------------------------------------------------------------
// 背景：
//   预测盒号是从预测数据源下载、存在本地 guessData 里的临时数据。某个盒号在正式
//   box_id.json 上线后，本地 guessData 里仍会留着同一条旧预测记录。各页面原来都是
//   直接 [...正式数据, ...预测数据]，同号盒出现两次：列表里重复一个盒子，展柜里
//   同一盒的角色被算两遍（看起来"多出角色"）。
//
// 规则：
//   同一个 Box_id 只保留正式版；预测数据里重复的整条丢弃。正式数据内部若本身有
//   重复也一并收敛（保留第一条），避免上游数据异常导致同样的现象。
// ---------------------------------------------------------------------------

/** 取出盒号，兼容 Box_id / box_id / id 三种写法 */
export function boxIdOf(box) {
	if (!box || typeof box !== "object") return "";
	const raw = box.Box_id !== undefined ? box.Box_id : box.box_id !== undefined ? box.box_id : box.id;
	if (raw === undefined || raw === null) return "";
	return String(raw).trim();
}

/** 记录数上限，防止异常数据无限膨胀 */
const MAX_BOXES = 5000;

/**
 * 合并正式数据与预测数据，正式数据优先。
 * @param {Array} official 正式盒号数据
 * @param {Array} guess 预测盒号数据
 * @returns {{data: Array, added: number, dropped: number}}
 *          data=合并结果，added=实际新增的预测盒号数，dropped=被正式数据覆盖而丢弃的数量
 */
export function mergeGuessData(official, guess) {
	const base = Array.isArray(official) ? official.slice(0, MAX_BOXES) : [];
	const pending = Array.isArray(guess) ? guess : [];
	const result = { data: base, added: 0, dropped: 0 };

	const seen = Object.create(null);
	const take = (box) => {
		const id = boxIdOf(box);
		if (!id) {
			// 没有盒号的数据无法判重，按各页面原有行为保留
			return true;
		}
		if (seen[id]) return false;
		seen[id] = true;
		return true;
	};

	// 正式数据内部先去重
	const deduped = [];
	base.forEach((box) => {
		if (take(box)) deduped.push(box);
	});

	const extra = [];
	pending.forEach((box) => {
		const id = boxIdOf(box);
		if (id && seen[id]) {
			result.dropped++;
			return;
		}
		if (id) seen[id] = true;
		extra.push(box);
	});

	result.data = deduped.concat(extra);
	result.added = extra.length;
	return result;
}

/** 读取当前正式数据（三个存储位置优先级与各页面保持一致） */
function readOfficial() {
	try {
		const sources = ["arknightsData", "currentData", "githubCharacters"];
		for (let i = 0; i < sources.length; i++) {
			const v = uni.getStorageSync(sources[i]);
			if (Array.isArray(v) && v.length > 0) return v;
		}
	} catch (e) {
		/* 读取失败按没有正式数据处理 */
	}
	return [];
}

/**
 * 清理本地预测数据中「已被正式版覆盖」的盒号。
 * 正式数据更新后调用一次，guessData 不会越积越多，设置页的计数也会跟着变准。
 * @returns {number} 被清理掉的条数
 */
export function pruneStoredGuessData() {
	try {
		const guess = uni.getStorageSync("guessData");
		if (!Array.isArray(guess) || guess.length === 0) return 0;

		const official = readOfficial();
		if (!official.length) return 0;

		const ids = Object.create(null);
		official.forEach((box) => {
			const id = boxIdOf(box);
			if (id) ids[id] = true;
		});
		if (!Object.keys(ids).length) return 0;

		const kept = guess.filter((box) => {
			const id = boxIdOf(box);
			return !(id && ids[id]);
		});
		const removed = guess.length - kept.length;
		if (removed > 0) uni.setStorageSync("guessData", kept);
		return removed;
	} catch (e) {
		return 0;
	}
}

export default {
	boxIdOf,
	mergeGuessData,
	pruneStoredGuessData
};
