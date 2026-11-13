/**
 * 宽屏两栏布局判断
 *
 * ★ 分栏是 Windows 壳专属能力（用户约定）：
 *   只有跑在 Windows 壳（WebView2）里、且窗口足够宽时才左右分栏；
 *   手机 App / 小程序 / 普通浏览器 一律走「切换 + 单栏」。
 *
 * 为什么不用「宽度」一刀切：
 *   手机横屏时 uni-app 的 windowWidth 会变成较长的那条边（模拟器/大屏机常常
 *   ≥ 960 逻辑 px），按宽度判断会把手机误判成桌面，出现不该有的分栏。
 *   所以先用 inShell() 做平台闸门，宽度只作为壳内的第二道条件。
 *
 * 壳内为什么是 720：
 *   Windows 壳（WebView2）由 DesktopPatches 把根字号钉死 18px，rpx 不随窗口放大，
 *   两栏各 300px 左右就能正常排版 → 720 足够。
 *
 * 用法：
 *   import wideLayout from '@/utils/wideLayout.js';
 *   export default { mixins: [wideLayout], ... }
 *   模板里用 `wideLayout` 布尔值控制切换控件与两栏渲染。
 */
const SHELL_BREAKPOINT = 720;

function inShell() {
	try {
		// #ifdef H5
		if (typeof window !== 'undefined' && window.chrome && window.chrome.webview) return true;
		// #endif
	} catch (e) {}
	return false;
}

function breakpoint() {
	return SHELL_BREAKPOINT;
}

function readWidth() {
	try {
		// #ifdef H5
		if (typeof window !== 'undefined' && window.innerWidth) return window.innerWidth;
		// #endif
		return uni.getSystemInfoSync().windowWidth || 0;
	} catch (e) {
		return 0;
	}
}

/** 唯一判定位：非 Windows 壳恒 false */
function computeWide() {
	if (!inShell()) return false;
	return readWidth() >= breakpoint();
}

export { inShell as isShell };

export default {
	data() {
		return {
			wideLayout: computeWide()
		};
	},
	created() {
		this.wideLayout = computeWide();
		// #ifdef H5
		// 只有壳内才需要跟着窗口宽度变化；其他平台恒为单栏，不必挂监听
		if (inShell() && typeof window !== 'undefined' && window.matchMedia) {
			this._wideMql = window.matchMedia('(min-width: ' + breakpoint() + 'px)');
			this._wideOnChange = (e) => {
				this.wideLayout = !!e.matches;
			};
			if (this._wideMql.addEventListener) {
				this._wideMql.addEventListener('change', this._wideOnChange);
			} else if (this._wideMql.addListener) {
				this._wideMql.addListener(this._wideOnChange);
			}
		}
		// #endif
	},
	beforeUnmount() {
		// #ifdef H5
		if (this._wideMql && this._wideOnChange) {
			if (this._wideMql.removeEventListener) {
				this._wideMql.removeEventListener('change', this._wideOnChange);
			} else if (this._wideMql.removeListener) {
				this._wideMql.removeListener(this._wideOnChange);
			}
		}
		// #endif
		this._wideMql = null;
		this._wideOnChange = null;
	},
	methods: {
		syncWideLayout() {
			this.wideLayout = computeWide();
		}
	}
};
