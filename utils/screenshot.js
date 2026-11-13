/**
 * screenshot.js —— 视图截图（不使用 canvas）
 * ----------------------------------------------------------------------------
 * 为什么要有这一层：
 *   分享图不再用 canvas 手绘，而是「把页面上已经渲染好的卡片截下来」。
 *   各平台能力不同，这里统一成一个出口：
 *
 *   · App（Android / iOS）
 *       plus.webview.currentWebview().draw(bitmap) —— HTML5+ **内置**能力，
 *       不需要 uni-plugin-screenshot 那类原生插件，也不用重打自定义基座。
 *       截的是 Webview 当前可视区域，所以分享页要做成整屏卡片（截图即成品）。
 *
 *   · 微信小程序
 *       微信没有开放「截取某个 view」的 API（Skyline 的 takeSnapshot 不通用），
 *       这里明确返回 UNSUPPORTED，由页面降级成「小程序卡片 / 复制文案」。
 *
 *   · H5 / 浏览器
 *       浏览器同样没有整节点截图的标准 API，返回 UNSUPPORTED。
 *
 * 用法：
 *   import { captureView, canCaptureView } from '@/utils/screenshot.js'
 *   if (canCaptureView()) {
 *     const path = await captureView({ name: 'daily_share' })
 *   }
 */

/**
 * 当前平台是否支持视图截图
 * @returns {boolean}
 */
export function canCaptureView() {
	// #ifdef APP-PLUS
	return typeof plus !== 'undefined' && !!(plus.nativeObj && plus.nativeObj.Bitmap);
	// #endif
	// #ifndef APP-PLUS
	return false;
	// #endif
}

/**
 * 截取当前 Webview 可视区域并保存为临时图片
 * @param {Object} [options]
 * @param {String} [options.name]  文件名前缀，默认 share_<时间戳>
 * @param {Boolean} [options.keep] 是否保留磁盘文件（默认保留，方便后续保存相册/分享）
 * @returns {Promise<String>} 本地图片路径（App 端形如 _doc/share_xxx.png）
 */
export function captureView(options) {
	const opt = options || {};
	return new Promise((resolve, reject) => {
		// #ifdef APP-PLUS
		try {
			if (typeof plus === 'undefined' || !plus.nativeObj || !plus.nativeObj.Bitmap) {
				reject({ code: 'UNSUPPORTED', message: '当前环境不支持视图截图' });
				return;
			}
			const prefix = opt.name || ('share_' + Date.now());
			const name = prefix + '_' + Date.now();
			const bitmap = new plus.nativeObj.Bitmap(name);
			const webview = plus.webview.currentWebview();

			const cleanup = function () {
				try { bitmap.clear(); } catch (e) {}
			};

			// ★ Android 上 webview.draw() 取不到 GPU 合成层，会截出一张全黑的空图
			//   （2026-10-27 真机反馈：提示保存成功，相册里全是黑的）。
			//   截图前把当前 Webview 临时切成软件图层（LAYER_TYPE_SOFTWARE = 1），
			//   截完立刻切回硬件层（LAYER_TYPE_HARDWARE = 2）。
			//   拿不到原生对象 / 调用失败就按原样截，不影响 iOS 和其它平台。
			let nativeWv = null;
			let forcedSoftware = false;
			try {
				if (plus.os.name === 'Android' && plus.android && plus.android.currentWebview) {
					nativeWv = plus.android.currentWebview();
					if (nativeWv && nativeWv.setLayerType) {
						nativeWv.setLayerType(1, null);
						nativeWv.invalidate();
						forcedSoftware = true;
					}
				}
			} catch (e) {
				nativeWv = null;
			}
			const restoreLayer = function () {
				if (!forcedSoftware || !nativeWv) return;
				forcedSoftware = false;
				try { nativeWv.setLayerType(2, null); } catch (e) {}
			};

			const doDraw = function () {
			webview.draw(bitmap, function () {
				const target = '_doc/' + name + '.png';
				bitmap.save(target, { overwrite: true, format: 'png' }, function (res) {
					cleanup();
					// ★ 必须转成平台绝对路径再交出去。
					//   saveImageToPhotosAlbum / shareWithSystem 不认 "_doc/xxx.png"
					//   这种 plus 相对路径 —— 它不会报错，而是存进相册一张全黑的空图
					//   （2026-10-27 真机反馈：提示保存成功，相册里全是黑的）。
					resolve(toLocalUrl((res && res.target) || target));
				}, function (err) {
					cleanup();
					restoreLayer();
					reject(err || { code: 'SAVE_FAILED', message: '图片保存失败' });
				});
			}, function (err) {
				cleanup();
				restoreLayer();
				reject(err || { code: 'DRAW_FAILED', message: '截图失败' });
			});
			};

			// 切了软件层要等它重绘一帧再截，否则截到的还是旧的空内容
			if (forcedSoftware) setTimeout(doDraw, 120);
			else doDraw();
		} catch (e) {
			reject(e);
		}
		// #endif
		// #ifndef APP-PLUS
		reject({ code: 'UNSUPPORTED', message: '当前平台不支持视图截图' });
		// #endif
	});
}

/**
 * 把本地路径转成可直接给 Image / shareWithSystem 使用的绝对路径
 * @param {String} path
 * @returns {String}
 */
export function toLocalUrl(path) {
	if (!path) return '';
	// #ifdef APP-PLUS
	try {
		if (typeof plus !== 'undefined' && plus.io && plus.io.convertLocalFileSystemURL) {
			return plus.io.convertLocalFileSystemURL(path);
		}
	} catch (e) {}
	// #endif
	return path;
}

/**
 * 微信小程序：把分享数据写进 storage，跳到 Skyline 截图页
 * 该页在 pages.json 里单独开启 renderer: skyline，用 <snapshot> + takeSnapshot 出图；
 * 其它页面仍是 WebView（混合渲染），不影响现有界面。
 * @param {Object} payload 见 packageA/share_shot/share_shot.vue 的 payload 结构
 * @returns {Boolean} 是否已跳转（非小程序恒为 false）
 */
export function openMpShareShot(payload) {
	// #ifdef MP-WEIXIN
	try {
		uni.setStorageSync('share_shot_payload', payload || {});
	} catch (e) {}
	uni.navigateTo({
		url: '/packageA/share_shot/share_shot',
		fail: () => {
			try { uni.showToast({ title: '打开分享页失败', icon: 'none' }); } catch (e2) {}
		}
	});
	return true;
	// #endif
	// #ifndef MP-WEIXIN
	return false;
	// #endif
}

export default { canCaptureView, captureView, toLocalUrl, openMpShareShot };

