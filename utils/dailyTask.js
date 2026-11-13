/**
 * 今日宜挂 · 每日任务与重测次数管理
 *
 * 设计要点：
 *  1. 每日任务按「日期」做种子随机抽取，同一天多次进入结果一致，跨天自动重抽；
 *  2. 任务与全站真实功能一一对应（切主题 / 查干员 / 查盒号 / 看日历 …），
 *     完成判定由各页面调用 markDailyTask() 打点写入，本模块只负责读写；
 *  3. 奖励是「推导」出来的而不是「发放」的 —— 奖励次数 = 已完成任务数（全部完成再 +1），
 *     因此重复进入页面、异常中断都不会重复发放或漏发；
 *  4. 看完激励视频广告 → 当日无限次重测（unlimited）。
 *
 * 存储键：
 *  dailyTaskFlags  { date, done: { taskId: true } }   ← 各业务页面写入
 *  dailyTaskState  { date, taskIds: [], unlimited, adCount }  ← 本页写入
 *
 * 次数以服务端为真源（见 utils/cloudSync.js）：
 * 本地只记得「今天已用几次」用于显示，实际用量由服务端按设备指纹计数，
 * 清缓存后进入页面会重新拉回，不能靠清缓存把次数刷回来。
 */

import { postQuota, fetchQuota, makeRid, todayStr } from './cloudSync.js';

const FLAG_KEY = 'dailyTaskFlags';
const STATE_KEY = 'dailyTaskState';

/** 基础次数：1 次正式 + 1 次额外 */
export const BASE_DRAWS = 2;

/** 任务池：与全站功能一一对应 */
export const TASK_POOL = [
	{
		id: 'theme_try',
		title: '换个主题',
		desc: '在设置里切换一次界面主题（不妨试试工业深色）',
		path: '/packageA/Setting/Setting?tab=theme'
	},
	{
		id: 'search_operator',
		title: '查个干员',
		desc: '去搜索页查一位干员，看看 TA 出过哪些通行证',
		path: '/pages/Search/Search'
	},
	{
		id: 'query_box',
		title: '查个盒号',
		desc: '在搜索页切到「盒号」模式，查任意一个盒号',
		path: '/pages/Search/Search'
	},
	{
		id: 'browse_list',
		title: '翻翻盒号表',
		desc: '打开通行证盒号列表浏览一眼',
		path: '/pages/list/list'
	},
	{
		id: 'view_calendar',
		title: '看看上线日历',
		desc: '打开日历，看通行证的上线与复刻时间',
		path: '/packageA/calendar/calendar'
	},
	{
		id: 'view_about',
		title: '认识一下',
		desc: '打开「关于我们」，看看这个工具是怎么来的',
		path: '/packageA/aboutus/aboutus'
	},
	{
		id: 'scan_try',
		title: '试一次扫码',
		desc: '用扫码识别任意一个通行证包装条码',
		path: '/pages/scan_barcode/scan_barcode'
	},
	{
		id: 'gacha_try',
		title: '抽一次卡',
		desc: '打开模拟抽卡页面，试试今天的手气',
		path: '/packageB/pages/gacha_simulated/gacha_simulated'
	},
	{
		id: 'star_try',
		title: '点亮展柜',
		desc: '在通行证展柜里点亮任意一张',
		path: '/pages/card_share/card_share'
	},
	{
		id: 'feedback_try',
		title: '补一条数据',
		desc: '打开数据不全盒号列表，点一个盒号提交反馈',
		path: '/pages/incomplete_list/incomplete_list'
	}
];

const TASK_MAP = {};
TASK_POOL.forEach(t => { TASK_MAP[t.id] = t; });

/** 今天的日期键，例如 2026-11-01（补零，和云端一致） */
export function getTodayKey() {
	return todayStr();
}

/** FNV-1a 字符串哈希，把日期转成种子 */
function hashStr(str) {
	let h = 2166136261;
	for (let i = 0; i < str.length; i++) {
		h ^= str.charCodeAt(i);
		h = Math.imul(h, 16777619);
	}
	return h >>> 0;
}

/** mulberry32：同种子同序列，保证「同一天任务固定」 */
function seededRandom(seed) {
	let s = seed >>> 0;
	return function () {
		s = (s + 0x6D2B79F5) >>> 0;
		let t = Math.imul(s ^ (s >>> 15), 1 | s);
		t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
		return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
	};
}

/** 按日期稳定抽取 1~3 个任务 id */
function pickTaskIds(dateKey) {
	const rnd = seededRandom(hashStr(dateKey));
	const pool = TASK_POOL.map(t => t.id);
	// Fisher-Yates 洗牌
	for (let i = pool.length - 1; i > 0; i--) {
		const j = Math.floor(rnd() * (i + 1));
		const tmp = pool[i];
		pool[i] = pool[j];
		pool[j] = tmp;
	}
	const count = 1 + Math.floor(rnd() * 3); // 1 ~ 3 个
	return pool.slice(0, Math.max(1, Math.min(3, count))).sort();
}

function safeGet(key, fallback) {
	try {
		const v = uni.getStorageSync(key);
		return (v === '' || v === null || v === undefined) ? fallback : v;
	} catch (e) {
		return fallback;
	}
}

function safeSet(key, value) {
	try {
		uni.setStorageSync(key, value);
		return true;
	} catch (e) {
		console.error('[dailyTask] storage 写入失败', e);
		return false;
	}
}

/**
 * 读取当日任务状态；跨天或首次进入时自动重抽任务并清空完成打点
 */
export function getDailyState() {
	const today = getTodayKey();
	let state = safeGet(STATE_KEY, null);
	if (!state || typeof state !== 'object' || state.date !== today || !Array.isArray(state.taskIds) || !state.taskIds.length) {
		state = {
			date: today,
			taskIds: pickTaskIds(today),
			unlimited: false,
			adCount: 0
		};
		safeSet(STATE_KEY, state);
		// 跨天：清掉昨天的完成打点
		const flags = safeGet(FLAG_KEY, null);
		if (!flags || flags.date !== today) {
			safeSet(FLAG_KEY, { date: today, done: {} });
		}
	}
	return state;
}

/** 局部更新当日状态 */
export function saveDailyState(patch) {
	const state = getDailyState();
	const next = Object.assign({}, state, patch || {}, { date: getTodayKey() });
	safeSet(STATE_KEY, next);
	return next;
}

/** 当日各任务的完成情况 { taskId: true } */
export function getDoneFlags() {
	const today = getTodayKey();
	const flags = safeGet(FLAG_KEY, null);
	if (!flags || typeof flags !== 'object' || flags.date !== today) return {};
	return flags.done && typeof flags.done === 'object' ? flags.done : {};
}

/**
 * 业务页面埋点：完成某个任务时调用
 * @param {string} taskId 任务 id（见 TASK_POOL）
 */
export function markDailyTask(taskId) {
	if (!taskId || !TASK_MAP[taskId]) return false;
	const today = getTodayKey();
	let flags = safeGet(FLAG_KEY, null);
	if (!flags || typeof flags !== 'object' || flags.date !== today || !flags.done || typeof flags.done !== 'object') {
		flags = { date: today, done: {} };
	}
	if (flags.done[taskId]) return false;
	flags.done[taskId] = true;
	safeSet(FLAG_KEY, flags);
	// 任务完成数决定能多抽几次，交给服务端记一份，换设备/清缓存不会丢
	reportTaskCount();
	return true;
}

/** 把「今日已完成任务数」上报到服务端（失败自动进补交队列） */
export function reportTaskCount() {
	const done = Object.keys(getDoneFlags()).length;
	postQuota('tasks', 'daily', { tasks: done });
	return done;
}

/**
 * 从云端同步今日宜挂的用量（页面 onShow 调用）。
 * @returns {Promise<{used:number, unlimited:boolean, tasks:number}|null>} 服务端不可用时返回 null
 */
export async function syncDailyQuota() {
	const remote = await fetchQuota(todayStr());
	if (!remote || remote.ok !== true || !remote.daily) return null;
	const d = remote.daily;
	return {
		used: Math.max(0, Number(d.used) || 0),
		unlimited: d.unlimited === true,
		tasks: Math.max(0, Number(d.tasks) || 0)
	};
}

/** 用掉一次重测机会 → 记到服务端 */
export function reportDailyConsume() {
	postQuota('consume', 'daily', { rid: makeRid() });
}

/**
 * 汇总当日进度：任务清单 + 已完成数 + 奖励次数 + 可用次数
 * @param {number} usedDraws 今日已用次数（来自 dailyRecState.usedToday）
 */
export function getDailyProgress(usedDraws) {
	const state = getDailyState();
	const done = getDoneFlags();
	const used = Math.max(0, Number(usedDraws) || 0);

	const tasks = state.taskIds.map(id => {
		const def = TASK_MAP[id] || { id: id, title: id, desc: '', path: '' };
		return Object.assign({}, def, { done: !!done[id] });
	});
	const total = tasks.length;
	const doneCount = tasks.filter(t => t.done).length;
	const allDone = total > 0 && doneCount === total;

	// 奖励推导：每完成 1 个 +1 次；全部完成再 +1 次
	const rewardDraws = doneCount + (allDone ? 1 : 0);
	const availableDraws = BASE_DRAWS + rewardDraws;
	const unlimited = !!state.unlimited;
	const remaining = unlimited ? Infinity : Math.max(0, availableDraws - used);

	return {
		date: state.date,
		tasks: tasks,
		total: total,
		doneCount: doneCount,
		allDone: allDone,
		rewardDraws: rewardDraws,
		availableDraws: availableDraws,
		usedDraws: used,
		unlimited: unlimited,
		adCount: state.adCount || 0,
		remaining: remaining
	};
}

/** 广告看完 → 解锁当日无限次 */
export function unlockUnlimited() {
	const next = saveDailyState({ unlimited: true, adCount: (getDailyState().adCount || 0) + 1 });
	postQuota('unlimited', 'daily');
	return next;
}

export default {
	BASE_DRAWS,
	TASK_POOL,
	getTodayKey,
	getDailyState,
	saveDailyState,
	getDoneFlags,
	markDailyTask,
	reportTaskCount,
	syncDailyQuota,
	reportDailyConsume,
	getDailyProgress,
	unlockUnlimited
};
