<script>
	import errorLog from "@/utils/errorLog.js";
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
		},
		onShow: function() {
			console.log('App Show')
		},
		onHide: function() {
			console.log('App Hide')
		}
	}
</script>

<style>
	/* ========== 全局主题样式 ========== */

	/* ========== 简约风格（默认） ========== */
	.container {
		padding: 0 20rpx 30rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}

	/* ========== 科技风格（明日方舟主题） ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 50%, #0D0D15 100%);
		position: relative;
	}

	/* 科技感网格背景 */
	.container.theme-ark::before {
		content: '';
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-image:
			linear-gradient(rgba(255, 107, 53, 0.03) 1px, transparent 1px),
			linear-gradient(90deg, rgba(255, 107, 53, 0.03) 1px, transparent 1px);
		background-size: 50rpx 50rpx;
		pointer-events: none;
		z-index: 0;
	}

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

	/* 科技风格 */
	.container.theme-ark .card {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		border: 1rpx solid #2A2A4A;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.3);
	}

	/* ========== 通用标题样式 ========== */

	.title {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
	}

	.container.theme-ark .title {
		color: #FF6B35;
		text-shadow: 0 0 10rpx rgba(255, 107, 53, 0.3);
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

	.container.theme-ark .text-primary {
		color: #E8E8F0;
	}

	.container.theme-ark .text-secondary {
		color: #8A8AA0;
	}

	.container.theme-ark .text-tertiary {
		color: #6A6A80;
	}

	/* ========== 通用按钮样式 ========== */

	.btn-primary {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		padding: 20rpx 40rpx;
	}

	.container.theme-ark .btn-primary {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		border-radius: 4rpx;
		border: 1rpx solid #FF6B35;
		box-shadow: 0 0 20rpx rgba(255, 107, 53, 0.3);
	}

	.btn-secondary {
		background-color: #f0f0f0;
		color: #666;
		border-radius: 50rpx;
		padding: 20rpx 40rpx;
	}

	.container.theme-ark .btn-secondary {
		background: linear-gradient(135deg, #2A2A4A 0%, #1A1A2E 100%);
		color: #E8E8F0;
		border-radius: 4rpx;
		border: 1rpx solid #2A2A4A;
	}

	/* ========== 输入框样式 ========== */

	.input {
		background-color: #f5f5f5;
		border-radius: 50rpx;
		padding: 20rpx 30rpx;
		color: #333;
	}

	.container.theme-ark .input {
		background: linear-gradient(135deg, #1E1E3A 0%, #2A2A4A 100%);
		border-radius: 4rpx;
		border: 1rpx solid #2A2A4A;
		color: #E8E8F0;
	}

	/* ========== 列表项样式 ========== */

	.list-item {
		background-color: #fff;
		border-bottom: 1rpx solid #f0f0f0;
		padding: 25rpx 30rpx;
	}

	.container.theme-ark .list-item {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-bottom: 1rpx solid #2A2A4A;
	}

	/* ========== 标签样式 ========== */

	.tag {
		background-color: #e8f4ff;
		color: #409EFF;
		border-radius: 20rpx;
		padding: 4rpx 12rpx;
		font-size: 22rpx;
	}

	.container.theme-ark .tag {
		background: rgba(255, 107, 53, 0.1);
		color: #FF6B35;
		border-radius: 4rpx;
		border: 1rpx solid rgba(255, 107, 53, 0.3);
	}

	/* ========== 状态栏样式 ========== */

	.status-bar {
		background-color: #e8f4ff;
		border-radius: 16rpx;
		padding: 25rpx;
		color: #409EFF;
	}

	.container.theme-ark .status-bar {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		border: 1rpx solid #2A2A4A;
		color: #00D4AA;
	}

	/* ========== 图片适配深色主题 ========== */

	/* 在深色主题下，给图片添加亮度调整 */
	.container.theme-ark .icon-img {
		filter: brightness(1.2);
	}

	/* 对于黑色图标，在深色主题下需要反色或增加亮度 */
	.container.theme-ark .icon-dark {
		filter: brightness(2) invert(1);
	}

	/* 每个页面公共css */
</style>
