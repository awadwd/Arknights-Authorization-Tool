/**
 * haptic.js —— 全站统一的触感出口
 * ----------------------------------------------------------------------------
 * 为什么要有这一层：
 *   项目里原来散落 24 处 `uni.vibrateShort()`（全部无参数）。它在 App 端 iOS 的
 *   底层是 AudioServicesPlaySystemSound(kSystemSoundID_Vibrate) —— **就是系统通知
 *   震动本身**，所以「点按钮」和「来通知」手感完全一致，用户容易误会收到了通知。
 *
 * 各平台策略：
 *   · App（iOS / Android）→ 原生触感引擎，走 UTS 插件 uni_modules/ak-haptic
 *       iOS     : UISelectionFeedbackGenerator / UIImpactFeedbackGenerator（Taptic Engine）
 *       Android : VibrationEffect.createPredefined(EFFECT_TICK / EFFECT_CLICK)
 *   · 微信小程序 → uni.vibrateShort({ type })（微信会映射到 iOS 的 Taptic 引擎，效果不错）
 *   · H5 / 其它  → 静默忽略（浏览器无可靠触感能力）
 *
 * 用法：
 *   import { haptic } from '@/utils/haptic.js'
 *   haptic('light')    // 点击 / 选中 / 切换（默认）
 *   haptic('medium')   // 长按 / 落位 / 重要确认
 *   haptic('heavy')    // 删除 / 危险操作
 *
 * ⚠️ App 端依赖 UTS 插件，改动插件内的 .uts 文件后**必须重打自定义基座**。
 *    若插件不可用（未重打基座 / 编译失败），本文件会自动兜底到 uni.vibrateShort，
 *    不会抛错、不会影响业务。
 */

// #ifdef APP-PLUS
import {
	impact as akImpact,
	prepare as akPrepare,
	isSupported as akSupported
} from '@/uni_modules/ak-haptic'
// #endif

const VALID = ['light', 'medium', 'heavy']

/** 归一化强度，非法值一律按 light 处理 */
function normalize(style) {
	return VALID.indexOf(style) >= 0 ? style : 'light'
}

/**
 * 触发一次触感反馈（永不抛错）
 * @param {'light'|'medium'|'heavy'} style 触感强度，默认 light
 */
export function haptic(style) {
	const s = normalize(style)

	// #ifdef APP-PLUS
	try {
		if (akSupported && akSupported()) {
			akImpact(s)
			return
		}
	} catch (e) {
		// 插件异常 → 继续往下走通用兜底，保证功能不中断
	}
	// #endif

	try {
		// #ifdef MP-WEIXIN
		uni.vibrateShort({ type: s, fail: () => {} })
		return
		// #endif
		// #ifndef MP-WEIXIN
		uni.vibrateShort({ fail: () => {} })
		// #endif
	} catch (e) {
		// 忽略：H5 等平台无触感能力
	}
}

/**
 * 预热原生触感引擎，降低「首次触发」的延迟。
 * iOS 建议在即将高频触发的场景前调用（进入编辑态、打开长列表、拉起弹窗等）。
 * 在页面 onLoad / onShow 里调一次即可，开销极小、失败无副作用。
 */
export function hapticPrepare() {
	// #ifdef APP-PLUS
	try {
		if (akPrepare && akSupported && akSupported()) {
			akPrepare()
		}
	} catch (e) {
		// 忽略
	}
	// #endif
}

/** 语义化快捷方法（可选，读起来更清楚） */
export function hapticLight() {
	haptic('light')
}
export function hapticMedium() {
	haptic('medium')
}
export function hapticHeavy() {
	haptic('heavy')
}

export default haptic
