/**
 * 小程序 wx API 兜底（仅非微信小程序端加载，见 main.js 条件编译）。
 *
 * 背景：uni-app 在 H5 / App 里只给 window 挂了一个空的 wx 对象，
 * 但不少页面在 onLoad 第一句就写：
 *     wx.showShareMenu({ withShareTicket: true, menus: [...] })
 * 在微信里没问题，在 H5 里直接抛 TypeError，onLoad 后半段（主题初始化、
 * 列表加载、排序恢复……）全部被截断。
 *
 * 现象就是「设置里切了深色，这一页还是浅色」，因为 loadThemeSetting() 压根没跑到。
 *
 * 这里只补「调用了也无害」的分享类 API；文件系统、支付等真实能力不做假实现，
 * 免得把真正的错误藏起来。
 */

const SHARE_APIS = ['showShareMenu', 'hideShareMenu'];

export function installWxShim() {
	if (typeof wx === 'undefined' || wx === null) return;

	for (let i = 0; i < SHARE_APIS.length; i++) {
		const key = SHARE_APIS[i];
		if (typeof wx[key] !== 'function') {
			wx[key] = function () {};
		}
	}
}
