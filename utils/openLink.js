/**
 * openLink.js —— 全站统一的「链接打开 / 复制」出口
 * ----------------------------------------------------------------------------
 * 为什么要有这一层：
 *   项目里原来散落着 9 处「复制链接到剪贴板 + 提示去浏览器打开」的实现。
 *   在小程序里这是**必须**的（平台不允许直接跳外部链接），
 *   但同一个 App 里这么做就很别扭 —— 用户点的是「打开链接」，
 *   拿到的却只有一段剪贴板文本，还得自己退出 App、切浏览器、粘贴。
 *
 * 各平台策略：
 *   · App（Android / iOS）→ plus.runtime.openURL()，直接唤起**系统默认浏览器**
 *   · H5                  → window.open() 新标签页
 *   · 小程序 / 其他端      → 复制到剪贴板 + 提示（保留原有能力）
 *
 * 用法：
 *   import { openLink } from '@/utils/openLink.js'
 *   openLink('https://example.com')
 *   openLink(url, { copyTip: '链接已复制，请到浏览器打开' })
 *
 * ⚠️ 条件编译说明：本文件只用 APP-PLUS / H5 两个宏，
 *    小程序端的「复制」走**代码末尾的默认分支**，而不是 `#ifdef MP-*`。
 *    原因：平台宏一旦不被识别（如 uni-app 里的 APP-ANDROID），`#ifdef` 内的代码
 *    会被**静默丢弃**，功能直接失效且编译不报错。默认分支没有这个风险。
 */

/** 复制到剪贴板（内部使用，永不抛错） */
function copyToClipboard(link, tip) {
	try {
		uni.setClipboardData({
			data: link,
			success: () => {
				if (tip) {
					uni.showToast({ title: tip, icon: 'none', duration: 2500 })
				}
			},
			fail: () => {
				uni.showToast({ title: '复制失败，请手动复制', icon: 'none' })
			}
		})
	} catch (e) {
		console.error('[openLink] 复制到剪贴板失败:', e)
	}
}

/**
 * 打开（或复制）一个链接，按平台自动选择最合适的方式。
 * @param {string} url 目标链接
 * @param {object} [options]
 * @param {string} [options.copyTip]  复制成功后的提示文案（小程序端 / 兜底）
 * @param {string} [options.failTip]  App / H5 打开失败并回落复制时的提示文案
 * @param {string} [options.emptyTip] 链接为空时的提示文案
 * @returns {boolean} 是否已受理（true = 已打开或已复制）
 */
export function openLink(url, options = {}) {
	const link = String(url == null ? '' : url).trim()
	if (!link) {
		uni.showToast({ title: options.emptyTip || '暂无链接', icon: 'none' })
		return false
	}

	const copyTip = options.copyTip || '链接已复制，请前往浏览器打开'
	const failTip = options.failTip || '打开失败，链接已复制，请前往浏览器打开'

	// ---------- App（Android / iOS）：直接唤起系统默认浏览器 ----------
	// #ifdef APP-PLUS
	try {
		// 第二个参数是失败回调：设备上没有能处理该链接的应用（无浏览器 / 非法 scheme）时触发
		plus.runtime.openURL(link, () => {
			copyToClipboard(link, failTip)
		})
		return true
	} catch (e) {
		console.error('[openLink] App 唤起浏览器失败，回落复制:', e)
		copyToClipboard(link, failTip)
		return false
	}
	// #endif

	// ---------- H5：新标签页 ----------
	// #ifdef H5
	try {
		window.open(link, '_blank')
		return true
	} catch (e) {
		console.error('[openLink] H5 打开失败，回落复制:', e)
		copyToClipboard(link, failTip)
		return false
	}
	// #endif

	// ---------- 小程序 / 其他平台：保留复制能力 ----------
	copyToClipboard(link, copyTip)
	return true
}

/**
 * 当前平台下「处理链接」这个动作该怎么称呼，用于弹窗按钮 / 说明文案。
 * @returns {'打开链接'|'复制链接'}
 */
export function linkActionText() {
	let text = '复制链接'
	// #ifdef APP-PLUS
	text = '打开链接'
	// #endif
	// #ifdef H5
	text = '打开链接'
	// #endif
	return text
}

/**
 * 复制一段文本到剪贴板（非链接场景，如导出设置）。
 * 与链接复制共用同一套容错逻辑，避免各页面散写 uni.setClipboardData。
 * @param {string} text
 * @param {string} [tip] 成功提示
 * @returns {boolean} 是否已发起复制
 */
export function copyText(text, tip) {
	const value = String(text == null ? '' : text)
	if (!value) {
		uni.showToast({ title: '没有可复制的内容', icon: 'none' })
		return false
	}
	copyToClipboard(value, tip || '已复制到剪贴板')
	return true
}

export default openLink
