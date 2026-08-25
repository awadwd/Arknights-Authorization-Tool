/**
 * 错误日志工具
 * 统一记录运行时报错，最多保留 10 条
 * 使用：catch (e) { this.logError(e, '上下文描述'); }
 * 注意：仅记录 error 级别异常，不记录 warn/debug
 */
const MAX_LOG_COUNT = 10;
const STORAGE_KEY = 'errorLog';

/**
 * 记录一条错误日志
 * @param {Error|string|any} error 错误对象或消息
 * @param {string} context 上下文描述（哪个方法/页面/操作）
 * @param {string} source 来源页面或模块名（可选，默认从调用栈推断）
 */
function logError(error, context, source) {
	try {
		// 读取现有日志
		const existing = uni.getStorageSync(STORAGE_KEY) || [];

		// 提取错误信息
		let name = '';
		let message = '';
		let stack = '';
		if (error instanceof Error) {
			name = error.name || 'Error';
			message = error.message || String(error);
			stack = error.stack || '';
		} else if (typeof error === 'string') {
			name = 'Error';
			message = error;
		} else {
			try {
				message = JSON.stringify(error);
			} catch (e) {
				message = String(error);
			}
			name = (error && error.name) || 'Error';
		}

		// 推断来源（从 stack 取第一行文件路径）
		if (!source && stack) {
			const m = stack.match(/at\s+.*?\s+\((.*?):\d+:\d+\)/);
			if (m) source = m[1];
		}
		if (!source) source = 'unknown';

		// 截断 stack 太长
		if (stack && stack.length > 1000) {
			stack = stack.substring(0, 1000) + '...';
		}

		const entry = {
			time: formatTime(new Date()),
			timestamp: Date.now(),
			context: context || '',
			source: source || '',
			name: name,
			message: message,
			stack: stack
		};

		// 头部插入
		existing.unshift(entry);
		// 截断到最多 10 条
		const trimmed = existing.slice(0, MAX_LOG_COUNT);

		uni.setStorageSync(STORAGE_KEY, trimmed);

		// 同时输出到控制台
		console.error('[errorLog]', context || '未指定上下文', '|', name + ':', message);

		return entry;
	} catch (e) {
		// 静默失败，避免日志记录本身报错
		console.error('[errorLog] 记录失败:', e);
		return null;
	}
}

/**
 * 获取所有错误日志
 */
function getErrorLogs() {
	try {
		return uni.getStorageSync(STORAGE_KEY) || [];
	} catch (e) {
		return [];
	}
}

/**
 * 清空错误日志
 */
function clearErrorLogs() {
	try {
		uni.removeStorageSync(STORAGE_KEY);
		return true;
	} catch (e) {
		return false;
	}
}

/**
 * 删除单条日志（按时间戳）
 */
function removeErrorLog(timestamp) {
	try {
		const existing = uni.getStorageSync(STORAGE_KEY) || [];
		const filtered = existing.filter(item => item.timestamp !== timestamp);
		uni.setStorageSync(STORAGE_KEY, filtered);
		return true;
	} catch (e) {
		return false;
	}
}

function pad2(n) {
	return n < 10 ? '0' + n : '' + n;
}

function formatTime(date) {
	const y = date.getFullYear();
	const m = pad2(date.getMonth() + 1);
	const d = pad2(date.getDate());
	const hh = pad2(date.getHours());
	const mm = pad2(date.getMinutes());
	const ss = pad2(date.getSeconds());
	return y + '-' + m + '-' + d + ' ' + hh + ':' + mm + ':' + ss;
}

export default {
	logError,
	getErrorLogs,
	clearErrorLogs,
	removeErrorLog,
	MAX_LOG_COUNT
};
