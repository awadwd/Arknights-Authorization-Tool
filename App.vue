<script>
	import errorLog from "@/utils/errorLog.js";
	import boxWidget from "@/utils/boxWidget.js";

	// #ifdef APP-PLUS
	import { autoCheckOnLaunch } from "@/utils/appUpdate.js";
	import { fire as fireNotify } from "@/utils/notify.js";
	// #endif
	export default {
		logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
		onLaunch: function() {
			console.log('App Launch');
			// 分栏 / 宽屏布局是 Windows 壳专属：只有壳内才给 <html> 挂开关，
			// 手机 App / 小程序 / 普通浏览器永远拿不到这个 class。
			// #ifdef H5
			try {
				if (typeof document !== 'undefined' && typeof window !== 'undefined'
					&& window.chrome && window.chrome.webview) {
					document.documentElement.classList.add('is-win-shell');
				}
			} catch (e) {}
			// #endif
			// 全局错误监听 - 捕获异步未捕获错误
			try {
				uni.onError(function(err) {
					errorLog.logError(err, 'App.onError');
				});
			} catch (e) { console.error('[App] uni.onError init failed:', e); }
			try {
				uni.onUnhandledRejection(function(res) {
					const err = (res && res.reason) || new Error('UnhandledRejection');
					errorLog.logError(err, 'App.onUnhandledRejection');
				});
			} catch (e) { console.error('[App] uni.onUnhandledRejection init failed:', e); }
			// 拦截 console.error 也写入日志（仅在调试用）
			// 注意: 不会拦截 console.warn / console.log / debug

			// ===== 桌面小组件：scheme 唤起处理 =====
			// #ifdef APP-PLUS
			try {
				const consumeWidgetArgs = function() {
					try {
						const args = plus.runtime.arguments;
						if (args) boxWidget.handleSchemeArgs(args);
					} catch (e) {}
				};
				// 冷启动：等 plus 环境与首屏就绪后再读参数
				setTimeout(consumeWidgetArgs, 600);
				// 热启动：App 已在后台时点小组件，系统会走 onNewIntent
				plus.globalEvent.addEventListener('newintent', consumeWidgetArgs);
			} catch (e) {
				console.error('[App] 小组件 scheme 监听注册失败:', e);
			}
			// #endif
			// #ifdef APP-PLUS
			// 冷启动静默检查更新：24 小时最多一次，只提示不自动下载
			setTimeout(() => {
				try {
					autoCheckOnLaunch({
						onUpdate: (r) => {
							// 先落一条系统通知：用户可能正把 App 挂在后台，
							// 只弹 modal 的话他会完全错过这次更新。
							try {
								fireNotify('app', '发现新版本 v' + r.latest, '当前 v' + r.current + '，可去设置页更新');
							} catch (e) {}
							uni.showModal({
								title: '发现新版本 v' + r.latest,
								content: '当前版本 v' + r.current + '，可前往设置页下载安装。',
								confirmText: '去更新',
								cancelText: '稍后',
								success: (res) => {
									if (res.confirm) {
										uni.navigateTo({ url: '/packageA/Setting/Setting?checkUpdate=1' });
									}
								}
							});
						}
					});
				} catch (e) {}
			}, 4000);
			// #endif
		},
		onShow: function() {
			console.log('App Show')
			// 把最新的盒号数据同步到桌面小组件。
			// syncNow 内部有 3 分钟节流 + 内容签名去重，重复调用不会产生额外开销。
			boxWidget.syncNow(false);
			// 小组件里抽的卡攒在原生日志里，回到前台先合并进页面的历史 / 盈亏。
			// 空日志会在内部直接返回，开销可忽略。
			boxWidget.drainGachaJournal();
		},
		onHide: function() {
			console.log('App Hide')
		}
	}
</script>

<style>
	/* ==========================================================================
	   全局主题层（工业浅色 theme-ak / 工业深色 theme-ark）
	   设计令牌、全站通用表面规则与首页专属规则统一收敛在 common/ak-theme.css
	   ========================================================================== */
	@import './common/ak-theme.css';
	@import './common/liquid-theme.css';
	/* picker 浮层必须盖在所有自定义弹层之上：
	   uni 内置 .uni-picker-container 是 z-index:999，而本项目的模态框普遍用 10000，
	   弹层里再点 picker（如「高级筛选」里的年份/月份/日期）会被遮罩挡住，表现为点了没反应。 */
	.uni-picker-container {
		z-index: 10001 !important;
	}


	/* ========== 全局主题样式 ========== */

	/* ========== 简约风格（默认） ========== */
	.container {
		padding: 0 20rpx 30rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}

	/* ========== 科技风格（明日方舟主题） ========== */
	/* 迁移说明：原「渐变画布 + 橙色网格暗纹 + 发光卡片」的深色规则已移除，
	   改由 common/ak-theme.css 的 theme-ark 工业深色规则统一接管 */

	/* ========== 界园风格（粉绿渐变风格） ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
		position: relative;
	}

	/* 界园风格暗纹背景 */
	.container.theme-jieyuan::before {
		content: '';
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-image:
			radial-gradient(circle at 20% 30%, rgba(226, 88, 132, 0.05) 0%, transparent 50%),
			radial-gradient(circle at 80% 70%, rgba(57, 147, 131, 0.05) 0%, transparent 50%);
		pointer-events: none;
		z-index: 0;
	}

	/* ========== 通用卡片样式 ========== */

	/* 简约风格 */
	.card {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}

	/* ========== 通用标题样式 ========== */

	.title {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
	}

	/* ========== 通用文字样式 ========== */

	.text-primary {
		color: #333;
	}

	.text-secondary {
		color: #666;
	}

	.text-tertiary {
		color: #999;
	}

	/* ========== 通用按钮样式 ========== */

	.btn-primary {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		padding: 20rpx 40rpx;
	}

	.btn-secondary {
		background-color: #f0f0f0;
		color: #666;
		border-radius: 50rpx;
		padding: 20rpx 40rpx;
	}

	/* ========== 输入框样式 ========== */

	.input {
		background-color: #f5f5f5;
		border-radius: 50rpx;
		padding: 20rpx 30rpx;
		color: #333;
	}

	/* ========== 列表项样式 ========== */

	.list-item {
		background-color: #fff;
		border-bottom: 1rpx solid #f0f0f0;
		padding: 25rpx 30rpx;
	}

	/* ========== 标签样式 ========== */

	.tag {
		background-color: #e8f4ff;
		color: #409EFF;
		border-radius: 20rpx;
		padding: 4rpx 12rpx;
		font-size: 22rpx;
	}

	/* ========== 状态栏样式 ========== */

	.status-bar {
		background-color: #e8f4ff;
		border-radius: 16rpx;
		padding: 25rpx;
		color: #409EFF;
	}

	/* 每个页面公共css */

	/* #ifdef H5 */
	/* 桌面端（WinUI 壳）：滚动条收细，别像网页一样占掉 15px */
	::-webkit-scrollbar { width: 6px; height: 6px; }
	::-webkit-scrollbar-thumb { background: rgba(0, 0, 0, 0.18); border-radius: 3px; }
	::-webkit-scrollbar-thumb:hover { background: rgba(0, 0, 0, 0.32); }
	::-webkit-scrollbar-track { background: transparent; }
	/* 桌面端：导航交给左侧栏，顶部返回箭头留着只会挤占标题 */
	@media (min-width: 540px) {
		html.is-win-shell .uni-page-head-hd { visibility: hidden !important; width: 0 !important; }
		html.is-win-shell .uni-page-head-bd { padding-left: 4px !important; }
		html.is-win-shell .uni-page-head { border-bottom: 1px solid rgba(0, 0, 0, 0.06); box-shadow: none !important; }
	}
	/* #endif */

	/* --------------------------------------------------------------------------
	   长列表 scroll-view（H5）
	   uni-h5 的 scroll-view 内部是 <uni-scroll-view><div .uni-scroll-view>×2>…
	   两层 div 都是自动高度、按内容撑开。放在「底部抽屉 + flex:1」里时外层高度
	   被内容顶大，列表直接溢出屏幕而且滚不动（盒号 99 盒就是这种情况）。
	   给带 .sheet-grow 的 scroll-view 建一层绝对定位的撑满链，内部才能在固定
	   高度里滚动。这些 DOM 属于 uni 组件，拿不到页面 scopeId，所以只能写全局。
	   -------------------------------------------------------------------------- */
	.sheet-grow { position: relative; overflow: hidden; }
	.sheet-grow > .uni-scroll-view,
	.sheet-grow > .uni-scroll-view > .uni-scroll-view {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		height: auto !important;
	}
</style>
