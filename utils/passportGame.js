/**
 * utils/passportGame.js
 * 通行证游乐场 —— 数据索引 / 出题引擎 / 每日次数
 * ---------------------------------------------------------------------------
 * 设计要点：
 *  1. 所有题目都从「本工具收录的真实盒号数据」现场生成，没有写死的题库。
 *     数据换了（新增盒号、新增复刻），题目跟着变。
 *  2. 盒号 id 不一定是数字（有「嘉年华2022」「ManiFesto:」这种），
 *     一律按字符串处理，不做数值比较。
 *  3. 每日 3 次基础机会，分享 / 首胜各 +1，上限 5；看激励视频解锁当日不限次。
 *     跨天自动重置（按本地日期键判断，不依赖网络时间）。
 *  4. 次数以服务端为真源（见 utils/cloudSync.js）：本地只是显示缓存，
 *     清掉缓存下次进入会把真实用量拉回来，不能靠清缓存刷次数。
 */

import { fetchQuota, postQuota, makeRid, todayStr } from './cloudSync.js';

const STATE_KEY = 'passportGameState';
const BEST_KEY = 'passportGameBest';

/** 每日基础次数 */
export const BASE_PLAYS = 3;
/** 每日上限（多玩不宜） */
export const MAX_PLAYS = 5;

/**
 * 错题反馈问卷（腾讯问卷）。
 * 由「腾讯问卷」连接器生成后把链接填在这里；留空时反馈入口会退化为
 * 「复制题目内容」，用户可自行粘贴到问卷。
 */
export const SURVEY_URL = 'https://wj.qq.com/s2/28463298/h4yf/';

/* =========================================================================
 * 基础工具
 * ========================================================================= */

function rand(n) {
	return Math.floor(Math.random() * n);
}

function pick(arr) {
	return arr && arr.length ? arr[rand(arr.length)] : null;
}

function shuffle(arr) {
	const a = (arr || []).slice();
	for (let i = a.length - 1; i > 0; i--) {
		const j = rand(i + 1);
		const t = a[i];
		a[i] = a[j];
		a[j] = t;
	}
	return a;
}

function sample(arr, n) {
	return shuffle(arr).slice(0, n);
}

function uniqueBy(arr, keyFn) {
	const seen = Object.create(null);
	const out = [];
	(arr || []).forEach((item) => {
		const k = keyFn(item);
		if (seen[k]) return;
		seen[k] = 1;
		out.push(item);
	});
	return out;
}

/* =========================================================================
 * 数据索引
 * ========================================================================= */

/**
 * 把原始盒号数组整理成出题用的索引
 * @param {Array} boxes arknightsData（上游原始记录）
 */
export function buildIndex(boxes) {
	const list = [];
	const byId = Object.create(null);
	const charBoxes = Object.create(null);

	(boxes || []).forEach((raw) => {
		if (!raw || typeof raw !== 'object') return;
		const rawId = raw.Box_id !== undefined ? raw.Box_id : raw.box_id !== undefined ? raw.box_id : raw.id;
		const id = rawId === undefined || rawId === null ? '' : String(rawId).trim();
		if (!id || byId[id]) return;

		const chars = [];
		const seen = Object.create(null);
		for (let i = 1; i <= 24; i++) {
			const c = raw['character' + i];
			if (!c) continue;
			const name = (typeof c === 'string' ? c : (c && c.name) || '').trim();
			if (!name || seen[name]) continue;
			seen[name] = 1;
			chars.push({
				name: name,
				avatar: (c && typeof c === 'object' && c.imageUrl) || ''
			});
		}

		const releaseRaw = String(raw.release_date || '').trim();
		const m = /^(\d{4})[/\-.](\d{1,2})[/\-.](\d{1,2})/.exec(releaseRaw);
		const releaseTs = m ? Date.UTC(Number(m[1]), Number(m[2]) - 1, Number(m[3])) : 0;

		const reprintDates = [];
		if (raw.replicate_date) reprintDates.push(String(raw.replicate_date));
		for (let i = 1; i <= 5; i++) {
			const v = raw['replicate_date_0' + i];
			if (v) reprintDates.push(String(v));
		}
		const reprint = String(raw.replicate || '').toLowerCase() === 'true' || reprintDates.length > 0;

		const box = {
			id: id,
			chars: chars,
			names: chars.map((c) => c.name),
			nameSet: chars.reduce((acc, c) => {
				acc[c.name] = 1;
				return acc;
			}, Object.create(null)),
			releaseRaw: releaseRaw,
			releaseTs: releaseTs,
			reprint: reprint,
			reprintDate: reprintDates[0] || '',
			reprintCount: reprintDates.length
		};

		list.push(box);
		byId[id] = box;
		box.names.forEach((n) => {
			if (!charBoxes[n]) charBoxes[n] = [];
			charBoxes[n].push(id);
		});
	});

	const charNames = Object.keys(charBoxes);
	const reprintBoxes = list.filter((b) => b.reprint);
	const plainBoxes = list.filter((b) => !b.reprint);
	const datedBoxes = list.filter((b) => b.releaseTs > 0);
	const multiCharBoxes = list.filter((b) => b.chars.length >= 4);

	return {
		boxes: list,
		byId: byId,
		charBoxes: charBoxes,
		charNames: charNames,
		reprintBoxes: reprintBoxes,
		plainBoxes: plainBoxes,
		datedBoxes: datedBoxes,
		multiCharBoxes: multiCharBoxes,
		boxCount: list.length,
		charCount: charNames.length,
		reprintCount: reprintBoxes.length
	};
}

/** 索引是否可用（数据不全时不进入游戏） */
export function indexReady(idx) {
	return !!(idx && idx.boxCount >= 8 && idx.charNames.length >= 30);
}

/* =========================================================================
 * 出题：通行证竞猜
 * ========================================================================= */

/** 选项统一用「文字 + 副标题」描述，方便展示盒号/日期等补充信息 */
function q(type, tag, prompt, options, answerIndex, explain) {
	return {
		type: type,
		tag: tag,
		prompt: prompt,
		options: options,
		answer: answerIndex,
		explain: explain
	};
}

/** 把正确项和干扰项打乱，返回 { options, answer } */
function buildOptions(correct, wrong, subOf) {
	const all = shuffle([correct].concat(wrong));
	const options = all.map((v) => ({ text: v, sub: subOf ? subOf(v) : '' }));
	let answer = 0;
	for (let i = 0; i < all.length; i++) {
		if (all[i] === correct) {
			answer = i;
			break;
		}
	}
	return { options: options, answer: answer };
}

/** 盒号 X 里有谁 */
function genInBox(idx) {
	const box = pick(idx.multiCharBoxes);
	if (!box) return null;
	const pool = idx.charNames.filter((n) => !box.nameSet[n]);
	if (pool.length < 3) return null;
	const correct = pick(box.names);
	const wrong = sample(pool, 3);
	const o = buildOptions(correct, wrong);
	return q('inBox', '盒内成员', '盒号「' + box.id + '」中收录了下面哪位干员？', o.options, o.answer,
		'盒号「' + box.id + '」共 ' + box.chars.length + ' 位干员：' + box.names.join('、'));
}

/** 盒号 X 里没有谁 */
function genNotInBox(idx) {
	const box = pick(idx.multiCharBoxes);
	if (!box) return null;
	const pool = idx.charNames.filter((n) => !box.nameSet[n]);
	if (pool.length < 1) return null;
	const correct = pick(pool);
	const wrong = sample(box.names, 3);
	if (wrong.length < 3) return null;
	const o = buildOptions(correct, wrong);
	return q('notInBox', '排除法', '下面哪位干员【不在】盒号「' + box.id + '」中？', o.options, o.answer,
		'盒号「' + box.id + '」收录的是：' + box.names.join('、'));
}

/** 哪个盒号复刻过 */
function genReprinted(idx) {
	if (!idx.reprintBoxes.length || idx.plainBoxes.length < 3) return null;
	const correct = pick(idx.reprintBoxes);
	const wrong = sample(idx.plainBoxes, 3);
	const o = buildOptions(correct.id, wrong.map((b) => b.id), (id) => {
		const b = idx.byId[id];
		return b && b.releaseRaw ? '上线 ' + b.releaseRaw : '';
	});
	return q('reprinted', '复刻', '以下哪个盒号【复刻过】？', o.options, o.answer,
		'盒号「' + correct.id + '」复刻时间：' + (correct.reprintDate || '已复刻'));
}

/** 哪个盒号没复刻过 */
function genPlain(idx) {
	if (!idx.reprintBoxes.length || idx.plainBoxes.length < 1) return null;
	const correct = pick(idx.plainBoxes);
	const wrong = sample(idx.reprintBoxes, 3);
	if (wrong.length < 3) return null;
	const o = buildOptions(correct.id, wrong.map((b) => b.id), (id) => {
		const b = idx.byId[id];
		return b && b.releaseRaw ? '上线 ' + b.releaseRaw : '';
	});
	return q('plain', '复刻', '以下哪个盒号【至今没有复刻过】？', o.options, o.answer,
		'盒号「' + correct.id + '」目前只有首版，没有复刻记录');
}

/** 干员在哪个盒号里 */
function genCharBox(idx) {
	const candidates = idx.charNames.filter((n) => {
		const list = idx.charBoxes[n] || [];
		return list.length >= 1 && idx.boxCount - list.length >= 3;
	});
	if (!candidates.length) return null;
	const name = pick(candidates);
	const mine = idx.charBoxes[name];
	const correctId = pick(mine);
	const others = idx.boxes.filter((b) => mine.indexOf(b.id) < 0).map((b) => b.id);
	if (others.length < 3) return null;
	const o = buildOptions(correctId, sample(others, 3), (id) => {
		const b = idx.byId[id];
		return b && b.releaseRaw ? '上线 ' + b.releaseRaw : '';
	});
	const extra = mine.length > 1 ? '（共出现于 ' + mine.length + ' 个盒号）' : '';
	return q('charBox', '干员溯源', '干员「' + name + '」出现在以下哪个盒号中？', o.options, o.answer,
		'「' + name + '」所在的盒号：' + mine.join('、') + extra);
}

/** 盒里几个人 */
function genCount(idx) {
	const boxes = idx.boxes.filter((b) => b.chars.length >= 4 && b.chars.length <= 9);
	if (boxes.length < 1) return null;
	const box = pick(boxes);
	const n = box.chars.length;
	const nums = uniqueBy([n - 1, n + 1, n - 2, n + 2, n + 3].filter((v) => v >= 2 && v <= 12), (v) => v);
	const wrong = sample(nums, 3);
	if (wrong.length < 3) return null;
	const o = buildOptions(n, wrong, (v) => '位干员');
	return q('count', '数一数', '盒号「' + box.id + '」里一共有几位干员？', o.options, o.answer,
		'盒号「' + box.id + '」收录 ' + n + ' 位：' + box.names.join('、'));
}

/** 最早 / 最晚上线 */
function genDate(idx) {
	const dated = idx.datedBoxes;
	if (dated.length < 4) return null;
	const four = sample(dated, 4);
	const tsSet = uniqueBy(four.map((b) => b.releaseTs), (v) => v);
	if (tsSet.length < 4) return null;
	const earliest = Math.random() < 0.5;
	let target = four[0];
	four.forEach((b) => {
		if (earliest ? b.releaseTs < target.releaseTs : b.releaseTs > target.releaseTs) target = b;
	});
	const o = buildOptions(target.id, four.filter((b) => b.id !== target.id).map((b) => b.id),
		(id) => {
			const b = idx.byId[id];
			return b ? b.releaseRaw : '';
		});
	const detail = four.slice().sort((a, b) => a.releaseTs - b.releaseTs).map((b) => b.id + '（' + b.releaseRaw + '）').join('\n');
	return q('date', '时间线', earliest ? '以下哪个盒号上线时间最早？' : '以下哪个盒号上线时间最晚？',
		o.options, o.answer, '按上线时间排序：\n' + detail);
}

/** 谁和 TA 同盒 */
function genSameBox(idx) {
	const boxes = idx.multiCharBoxes.filter((b) => b.chars.length >= 4);
	if (!boxes.length) return null;
	const candidates = boxes.filter((b) => b.names.some((n) => (idx.charBoxes[n] || []).length === 1));
	if (!candidates.length) return null;
	const box = pick(candidates);
	// 锚点必须是「只出现在这一个盒号里」的干员：跨盒干员会让题目出现多个可能答案，
	// 例如「老鲤」同时收录在 23.0 与「琅环玄机」里，拿它当锚点会让人怀疑答案错了。
	const anchors = box.names.filter((n) => (idx.charBoxes[n] || []).length === 1);
	const anchor = pick(anchors);
	const correct = pick(box.names.filter((n) => n !== anchor));
	if (!correct) return null;
	const blocked = Object.create(null);
	(idx.charBoxes[anchor] || []).forEach((bid) => {
		const b = idx.byId[bid];
		if (b) b.names.forEach((n) => (blocked[n] = 1));
	});
	const pool = idx.charNames.filter((n) => !blocked[n] && n !== anchor);
	if (pool.length < 3) return null;
	const o = buildOptions(correct, sample(pool, 3));
	return q('sameBox', '同盒搭档', '哪位干员和「' + anchor + '」在同一个盒号里出现过？', o.options, o.answer,
		'「' + anchor + '」与「' + correct + '」都在盒号「' + box.id + '」中\n该盒完整名单：' + box.names.join('、'));
}

const GENERATORS = [
	{ type: 'inBox', fn: genInBox },
	{ type: 'notInBox', fn: genNotInBox },
	{ type: 'reprinted', fn: genReprinted },
	{ type: 'plain', fn: genPlain },
	{ type: 'charBox', fn: genCharBox },
	{ type: 'count', fn: genCount },
	{ type: 'date', fn: genDate },
	{ type: 'sameBox', fn: genSameBox }
];

/**
 * 生成一轮竞猜（默认 5 题，题型尽量不重复）
 * @returns {Array<{type,tag,prompt,options,answer,explain}>}
 */
export function makeQuizRound(idx, count) {
	const total = count || 5;
	if (!indexReady(idx)) return [];
	const order = shuffle(GENERATORS);
	const out = [];
	const usedType = Object.create(null);
	const usedPrompt = Object.create(null);

	order.forEach((g) => {
		if (out.length >= total || usedType[g.type]) return;
		for (let attempt = 0; attempt < 4; attempt++) {
			const item = g.fn(idx);
			if (item && !usedPrompt[item.prompt]) {
				usedType[g.type] = 1;
				usedPrompt[item.prompt] = 1;
				out.push(item);
				break;
			}
		}
	});

	// 题型不够时允许复用题型（换数据），保证题目数量
	let guard = 0;
	while (out.length < total && guard < 40) {
		guard++;
		const item = pick(GENERATORS).fn(idx);
		if (item && !usedPrompt[item.prompt]) {
			usedPrompt[item.prompt] = 1;
			out.push(item);
		}
	}
	return out;
}

/* =========================================================================
 * 出题：盒号速配（限时判断「在 / 不在」）
 * ========================================================================= */

/**
 * @returns {{name:string, boxId:string, answer:boolean, explain:string}|null}
 */
export function makeDuelQuestion(idx) {
	if (!indexReady(idx) || !idx.multiCharBoxes.length) return null;
	const box = pick(idx.multiCharBoxes);
	const pool = idx.charNames.filter((n) => !box.nameSet[n]);
	const inIt = Math.random() < 0.5;
	if (!inIt && !pool.length) return null;
	const name = inIt ? pick(box.names) : pick(pool);
	return {
		name: name,
		boxId: box.id,
		answer: inIt,
		explain: '盒号「' + box.id + '」：' + box.names.join('、')
	};
}

/* =========================================================================
 * 出题：通行证接龙（找与当前盒有共同干员的盒）
 * ========================================================================= */

/**
 * 给当前盒挑 4 个候选盒，其中恰好 1 个与它共享干员
 * @returns {{currentId:string, options:Array<{id:string}>, answerId:string, sharedChar:string, currentNames:string[]}|null}
 */
export function makeChainRound(idx, currentId) {
	if (!indexReady(idx)) return null;
	const cur = idx.byId[currentId];
	if (!cur) return null;

	const linked = [];
	idx.boxes.forEach((b) => {
		if (b.id === cur.id) return;
		for (let i = 0; i < cur.names.length; i++) {
			if (b.nameSet[cur.names[i]]) {
				linked.push({ id: b.id, shared: cur.names[i] });
				return;
			}
		}
	});
	if (!linked.length) return null;

	const correct = pick(linked);
	const lonely = idx.boxes.filter((b) => {
		if (b.id === cur.id) return false;
		for (let i = 0; i < cur.names.length; i++) {
			if (b.nameSet[cur.names[i]]) return false;
		}
		return true;
	});
	if (lonely.length < 3) return null;
	const decoys = sample(lonely, 3).map((b) => ({ id: b.id, names: b.names }));

	return {
		currentId: cur.id,
		currentNames: cur.names,
		options: shuffle([{ id: correct.id, names: (idx.byId[correct.id] || {}).names || [] }].concat(decoys)),
		answerId: correct.id,
		sharedChar: correct.shared
	};
}

/** 挑一个「接得下去」的起始盒 */
export function pickChainStart(idx) {
	if (!indexReady(idx)) return '';
	const pool = idx.boxes.filter((b) => {
		if (!b.names.length) return false;
		for (let i = 0; i < idx.boxes.length; i++) {
			const other = idx.boxes[i];
			if (other.id === b.id) continue;
			for (let k = 0; k < b.names.length; k++) {
				if (other.nameSet[b.names[k]]) return true;
			}
		}
		return false;
	});
	return pool.length ? pick(pool).id : (idx.boxes[0] ? idx.boxes[0].id : '');
}

/** 接龙里「另一个盒含哪几位共同干员」，用于答对后的说明 */
export function sharedCharsOf(idx, aId, bId) {
	const a = idx.byId[aId];
	const b = idx.byId[bId];
	if (!a || !b) return [];
	return a.names.filter((n) => b.nameSet[n]);
}

/* =========================================================================
 * 每日次数
 * ========================================================================= */

export function todayKey() {
	const d = new Date();
	return d.getFullYear() + '-' + (d.getMonth() + 1) + '-' + d.getDate();
}

function readJson(key, fallback) {
	try {
		const v = uni.getStorageSync(key);
		if (v && typeof v === 'object') return v;
		if (typeof v === 'string' && v) return JSON.parse(v);
	} catch (e) {
		// 存储损坏时按默认值走，不让游戏崩在读写上
	}
	return fallback;
}

function writeJson(key, value) {
	try {
		uni.setStorageSync(key, value);
		return true;
	} catch (e) {
		console.error('[passportGame] 写入存储失败', e);
		return false;
	}
}

/** 读取当日状态，跨天自动重置 */
export function getState() {
	const today = todayKey();
	const s = readJson(STATE_KEY, null);
	if (!s || s.date !== today) {
		const fresh = { date: today, used: 0, share: 0, win: 0, unlimited: false };
		writeJson(STATE_KEY, fresh);
		return fresh;
	}
	return {
		date: today,
		used: Math.max(0, Number(s.used) || 0),
		share: s.share ? 1 : 0,
		win: s.win ? 1 : 0,
		unlimited: !!s.unlimited
	};
}

function saveState(patch) {
	const next = Object.assign({}, getState(), patch || {}, { date: todayKey() });
	writeJson(STATE_KEY, next);
	return next;
}

/** 当日额度概览 */
export function getQuota() {
	const s = getState();
	const bonus = s.share + s.win;
	const total = Math.min(MAX_PLAYS, BASE_PLAYS + bonus);
	const remaining = s.unlimited ? Infinity : Math.max(0, total - s.used);
	return {
		date: s.date,
		used: s.used,
		total: total,
		remaining: remaining,
		unlimited: s.unlimited,
		bonus: bonus,
		shareUsed: !!s.share,
		winUsed: !!s.win
	};
}

/** 开始一局：扣一次机会。返回是否允许开始 */
export function beginPlay() {
	const q0 = getQuota();
	if (!q0.unlimited && q0.remaining <= 0) return false;
	saveState({ used: getState().used + 1 });
	// 本地先扣（界面立刻响应），服务端再记一笔；失败会进补交队列
	postQuota('consume', 'game', { rid: makeRid() }).then((remote) => {
		if (remote) mergeRemote(remote);
	});
	return true;
}

/**
 * 把云端状态合并回本地：取两者中「更严格」的那个。
 * 用 max 而不是覆盖，是为了避免本地刚扣的次数被一个稍早的响应抹掉。
 */
function mergeRemote(remote) {
	if (!remote || remote.ok !== true || !remote.game) return;
	const local = getState();
	const serverUsed = Math.max(0, Number(remote.game.used) || 0);
	const serverBonus = Math.max(0, Number(remote.game.bonus) || 0);
	const patch = {
		used: Math.max(local.used, serverUsed),
		unlimited: local.unlimited || remote.game.unlimited === true
	};
	// 云端只有 bonus 总数，本地是 share / win 两个位；缺多少补多少，先补分享再补首胜
	let missing = serverBonus - (local.share + local.win);
	if (missing > 0 && !local.share) { patch.share = 1; missing--; }
	if (missing > 0 && !local.win) { patch.win = 1; }
	if (patch.used !== local.used || patch.unlimited !== local.unlimited ||
		patch.share !== undefined || patch.win !== undefined) {
		saveState(patch);
	}
}

/**
 * 从云端同步当日次数（页面 onLoad / onShow 调用）。
 * 离线或服务不可用时保持本地值，不影响游玩。
 * @returns {Promise<object>} 同步后的额度概览
 */
export async function syncQuota() {
	const remote = await fetchQuota(todayStr());
	if (remote && remote.ok === true) mergeRemote(remote);
	return getQuota();
}

/**
 * 领取奖励次数
 * @param {'share'|'win'} kind 分享战绩 / 当日首胜
 * @returns {boolean} 是否真的发到了（今天已领过或已达上限则 false）
 */
export function grantBonus(kind) {
	const s = getState();
	if (kind === 'share' && s.share) return false;
	if (kind === 'win' && s.win) return false;
	if (BASE_PLAYS + s.share + s.win >= MAX_PLAYS) return false;
	const patch = {};
	patch[kind] = 1;
	saveState(patch);
	postQuota('grant', 'game', { rid: makeRid() }).then((remote) => {
		if (remote) mergeRemote(remote);
	});
	return true;
}

/** 看完激励视频：当日不限次 */
export function unlockUnlimited() {
	saveState({ unlimited: true });
	postQuota('unlimited', 'game', { rid: makeRid() });
	return getQuota();
}

/** 历史最好成绩（跨天保留） */
export function getBest() {
	const b = readJson(BEST_KEY, null);
	return {
		duel: Math.max(0, Number(b && b.duel) || 0),
		chain: Math.max(0, Number(b && b.chain) || 0),
		quiz: Math.max(0, Number(b && b.quiz) || 0)
	};
}

/**
 * 记录最好成绩
 * @param {'duel'|'chain'|'quiz'} kind
 * @param {number} value
 * @returns {boolean} 是否破纪录
 */
export function saveBest(kind, value) {
	const best = getBest();
	if (value <= best[kind]) return false;
	const next = Object.assign({}, best);
	next[kind] = value;
	writeJson(BEST_KEY, next);
	return true;
}

export default {
	buildIndex,
	indexReady,
	makeQuizRound,
	makeDuelQuestion,
	makeChainRound,
	pickChainStart,
	sharedCharsOf,
	getQuota,
	syncQuota,
	beginPlay,
	grantBonus,
	unlockUnlimited,
	getBest,
	saveBest,
	BASE_PLAYS,
	MAX_PLAYS,
	SURVEY_URL
};
