// utils/mpUpdate.js
//
// 微信小程序版本更新管理（wx.getUpdateManager 封装）
// ---------------------------------------------------------------------------
// 只在微信小程序端生效，其它平台所有导出函数都是空操作，页面无需再判断平台。
//
// 微信的机制：
//   1. 冷启动时微信会自动向后台检查一次新版本，结果通过 onCheckForUpdate 回调；
//   2. 有新版本时会在后台静默下载，下载完成触发 onUpdateReady，此时调用
//      applyUpdate() 才会重启并切到新版本；
//   3. 没有「主动触发检查」的官方 API，手动检测只能读取本次启动的检查结果，
//      因此超时（默认 2.5s）视为「已是最新版本」。
// ---------------------------------------------------------------------------

/** @type {any} */
let manager = null;
let bound = false;
/** @type {Function[]} */
const readyCbs = [];
/** @type {Function|null} */
let manualResolve = null;
/** @type {any} */
let manualTimer = null;
let readyState = false;
/** 首页可能已经弹过一次「发现新版本」，避免设置页再弹一遍 */
let promptConsumed = false;

function getManager() {
	// #ifdef MP-WEIXIN
	if (manager) return manager;
	try {
		if (typeof wx !== "undefined" && wx && typeof wx.getUpdateManager === "function") {
			manager = wx.getUpdateManager();
		}
	} catch (e) {
		manager = null;
	}
	return manager;
	// #endif
	// #ifndef MP-WEIXIN
	return null;
	// #endif
}

function settleManual(value) {
	if (manualTimer) {
		clearTimeout(manualTimer);
		manualTimer = null;
	}
	if (manualResolve) {
		const done = manualResolve;
		manualResolve = null;
		try {
			done(value);
		} catch (e) {
			/* 调用方异常不影响其它逻辑 */
		}
	}
}

function bind() {
	const m = getManager();
	if (!m || bound) return m;
	bound = true;

	m.onCheckForUpdate((res) => {
		const hasUpdate = !!(res && res.hasUpdate);
		// 手动检测会挂一个 resolve，这里把结果回给它
		if (!readyState) settleManual(hasUpdate);
	});

	m.onUpdateReady(() => {
		readyState = true;
		settleManual(true);
		readyCbs.forEach((cb) => {
			try {
				cb();
			} catch (e) {
				/* 单个回调失败不影响其它 */
			}
		});
	});

	m.onUpdateFailed(() => {
		settleManual(null);
	});

	return m;
}

/**
 * 注册「新版本已下载完成」回调，可重复注册。
 * 小程序以外平台直接忽略。
 */
export function onUpdateReady(cb) {
	if (typeof cb === "function") readyCbs.push(cb);
	bind();
}

/**
 * 手动检查更新。
 * @param {number} timeout 超时毫秒数，超时按「已是最新」处理
 * @returns {Promise<boolean|null>} true=有新版本 / false=已是最新 / null=检查失败
 */
export function checkUpdate(timeout) {
	const m = bind();
	if (!m) return Promise.resolve(null);
	if (readyState) return Promise.resolve(true);

	return new Promise((resolve) => {
		// 连续点击时，把上一次的等待先按「已是最新」结束，避免悬挂
		settleManual(false);
		manualResolve = resolve;
		manualTimer = setTimeout(() => {
			manualTimer = null;
			settleManual(false);
		}, timeout || 2500);
	});
}

/** 重启并应用已下载的新版本 */
export function applyUpdate() {
	const m = getManager();
	if (!m || typeof m.applyUpdate !== "function") return;
	try {
		m.applyUpdate();
	} catch (e) {
		/* 忽略：部分基础库不支持 */
	}
}

/**
 * 只弹一次「发现新版本」。
 * 首页自动监听和设置页手动检测共用，返回 false 表示已经提示过了。
 */
export function consumePrompt() {
	if (promptConsumed) return false;
	promptConsumed = true;
	return true;
}
