<template>
	<view :class="['container', 'theme-' + themeMode]">

		<!-- 页面说明 -->
		<view class="page-intro">
			<text class="intro-text">以下是与通行证相关的实用工具和资源</text>
		</view>

		<!-- 友情链接列表 -->
		<view class="links-list">
			<view 
				class="link-card" 
				v-for="(item, index) in friendLinks" 
				:key="index"
				@click="handleLinkClick(item)"
			>
				<view class="link-icon-wrap">
					<image class="link-icon" :src="item.icon" mode="aspectFit"></image>
				</view>
				<view class="link-content">
					<view class="link-header">
						<text class="link-title">{{ item.title }}</text>
						<text class="link-type-tag" :class="'type-' + item.type">{{ item.type === 0 ? '网址' : '小程序' }}</text>
					</view>
					<text class="link-desc">{{ item.content }}</text>
					<view class="link-notice" v-if="item.notice">
						<text class="notice-icon">⚠</text>
						<text class="notice-text">{{ item.notice }}</text>
					</view>
				</view>
				<view class="link-arrow">
					<text class="arrow-icon">›</text>
				</view>
			</view>
		</view>

		<!-- 空状态 -->
		<view class="empty-state" v-if="friendLinks.length === 0">
			<text class="empty-text">暂无友情链接</text>
			<text class="empty-hint">请联系作者添加</text>
		</view>

		<!-- 免责声明 -->
		<view class="disclaimer">
			<view class="disclaimer-line">个人玩家自制，用于聚合明日方舟同人类工具，仅供爱好者交流使用；以上网页或小程序内的内容与本小程序无关</view>
			<view class="disclaimer-line">部分资源来源于网络，如有侵权请联系 2726269007@qq.com 删除</view>
		</view>
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
// ============================================
// 友情链接数据配置区 - 在此添加/修改链接
// ============================================
const FRIEND_LINKS_DATA = [
	// 示例数据格式：
	// {
	// 	icon: '/static/link-icon.png',      // 图标路径
	// 	title: '工具名称',                   // 标题
	// 	content: '这是一个实用的工具介绍',    // 介绍内容
	// 	link: 'https://example.com',         // 链接地址（网址或小程序路径）
	// 	type: 0,                             // 0=网址链接, 1=小程序
	// 	notice: '使用时请注意xxx'             // 注意事项（可选）
	// },
	
	// TODO: 在此添加实际的友情链接数据
	{
		icon: 'https://i0.hdslb.com/bfs/archive/ccb4747e9e090b4ae82ebb620ee9ef425a3aee04.jpg', title: '赛博《抽通行证》',content: '来自Bilibili的up主@Turkana，十分真实的抽通行证模拟器（指和现实一样的非 bushi）',link:'domfin.space/game/txz',type:0,notice:'目前可能会 存在访问人数过多导致无法链接的问题'
	},
	{
		icon: '/static/link-tool.png', title: '明日方舟通行证汇总文档',content: '来自Bilibili的up主@燎冬谣汇总的通行证系列文档',link:'https://www.bilibili.com/opus/758829522099372082',type:0,notice:'可能更新稍慢哦~'
	},
	{
		icon: '/static/default-avatar.png', title: 'PRTS Wiki',content: '玩家共同构筑的明日方舟中文Wiki',link:'https://prts.wiki/w/%E9%A6%96%E9%A1%B5',type:0,notice:''
	},
	{
		icon: 'https://gw.alicdn.com/2f/ae/TB1FiEQf8v0gK0jSZKbwu2K2FXa.png', title: '明日方舟官方旗舰店',content: '购买通行证的官方渠道',link:'https://mingrifangzhou.tmall.com/',type:0,notice:''
	},
	{
		icon: 'https://tvax4.sinaimg.cn/crop.0.0.600.600.180/0071VPLMly8icon0qkud5j30go0gowfn.jpg', title: '明日方舟朝陇山官方账号',content: '通行证上新官方消息渠道',link:'https://weibo.com/u/6441489862',type:0,notice:''
	},
	{
		icon: '/static/github.png', title: '本项目Github数据仓库',content: '通行证数据仓库',link:'https://github.com/awadwd/ArknightsAuthorization_Series-mirror',type:0,notice:'推荐有开发能力的博士们使用~'
	},
];
// ============================================

export default {
	data() {
		return {
			themeMode: 'simple',
			friendLinks: FRIEND_LINKS_DATA
		};
	},

	onLoad() {
		this.loadThemeSetting();
		uni.setNavigationBarTitle({ title: '蟑螂の通行证友情小工具' });
	},

	methods: {
		logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
		goBack() {
			uni.navigateBack();
		},

		loadThemeSetting() {
			try {
				const themeMode = uni.getStorageSync('themeMode');
				if (themeMode && (themeMode === 'simple' || themeMode === 'ark' || themeMode === 'jieyuan')) {
					this.themeMode = themeMode;
				}
			} catch (e) {
				this.logError(e);
				this.themeMode = 'simple';
			}
		},

		// 处理链接点击
		handleLinkClick(item) {
			if (item.type === 0) {
				// 网址链接：复制到剪贴板
				this.copyLink(item);
			} else {
				// 小程序：尝试打开
				this.openMiniProgram(item);
			}
		},

		// 复制链接到剪贴板
		copyLink(item) {
			uni.setClipboardData({
				data: item.link,
				success: () => {
					uni.showToast({
						title: '链接已复制，请到浏览器打开',
						icon: 'none',
						duration: 2500
					});
				},
				fail: () => {
					uni.showToast({ title: '复制失败', icon: 'none' });
				}
			});
		},

		// 打开小程序
		openMiniProgram(item) {
			// #ifdef MP-WEIXIN
			// 尝试通过链接解析小程序参数
			const appId = this.extractAppId(item.link);
			const path = this.extractPath(item.link);
			
			if (appId) {
				uni.navigateToMiniProgram({
					appId: appId,
					path: path,
					success: () => {
						console.log('打开小程序成功');
					},
					fail: (err) => {
						console.error('打开小程序失败:', err);
						// 尝试显示小程序码
						this.showMiniProgramCode(item);
					}
				});
			} else {
				// 无法解析appId，显示小程序码
				this.showMiniProgramCode(item);
			}
			// #endif
			
			// #ifndef MP-WEIXIN
			// 非微信环境，复制名称推荐手动搜索
			this.copyNameForSearch(item);
			// #endif
		},

		// 从链接中提取小程序appId
		extractAppId(link) {
			// 支持格式：wx1234567890abcdef 或包含appId参数的URL
			if (!link) return null;
			
			// 直接是appId格式
			if (/^wx[a-f0-9]{16}$/i.test(link)) {
				return link;
			}
			
			// 从URL参数中提取
			const match = link.match(/[?&]appId=([^&]+)/i);
			if (match) return match[1];
			
			return null;
		},

		// 从链接中提取小程序路径
		extractPath(link) {
			if (!link) return '';
			const match = link.match(/[?&]path=([^&]+)/i);
			return match ? decodeURIComponent(match[1]) : '';
		},

		// 显示小程序码（预览图片）
		showMiniProgramCode(item) {
			// 如果有小程序码图片，显示预览
			if (item.qrCode) {
				uni.previewImage({
					urls: [item.qrCode],
					current: item.qrCode
				});
			} else {
				// 没有小程序码，复制名称推荐手动搜索
				this.copyNameForSearch(item);
			}
		},

		// 复制名称推荐手动搜索
		copyNameForSearch(item) {
			uni.setClipboardData({
				data: item.title,
				success: () => {
					uni.showModal({
						title: '提示',
						content: `「${item.title}」名称已复制，请前往微信搜索该小程序`,
						showCancel: false,
						confirmText: '知道了'
					});
				},
				fail: () => {
					uni.showToast({ 
						title: '请手动搜索：' + item.title, 
						icon: 'none',
						duration: 3000
					});
				}
			});
		}
	}
};
</script>

<style>
.container {
	min-height: 100vh;
	background-color: #f5f7fa;
	padding-bottom: 40rpx;
}

/* 导航栏 */
.nav-bar {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	height: 88rpx;
	background-color: #fff;
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding-top: env(safe-area-inset-top);
	z-index: 100;
	box-shadow: 0 2rpx 10rpx rgba(0,0,0,0.05);
}

.nav-back {
	width: 80rpx;
	height: 88rpx;
	display: flex;
	align-items: center;
	justify-content: center;
}

.back-icon {
	font-size: 48rpx;
	color: #333;
	line-height: 1;
}

.nav-title {
	font-size: 32rpx;
	color: #333;
	font-weight: bold;
}

.nav-placeholder {
	width: 80rpx;
}

/* 页面说明 */
.page-intro {

	padding: 0 30rpx;
	margin-bottom: 20rpx;
}

.intro-text {
	font-size: 26rpx;
	color: #666;
	line-height: 1.6;
}

/* 链接列表 */
.links-list {
	padding: 0 30rpx;
}

.link-card {
	background-color: #fff;
	border-radius: 16rpx;
	padding: 24rpx;
	margin-bottom: 20rpx;
	display: flex;
	align-items: flex-start;
	box-shadow: 0 4rpx 20rpx rgba(0,0,0,0.05);
}

.link-icon-wrap {
	width: 80rpx;
	height: 80rpx;
	border-radius: 16rpx;
	background-color: #f0f4f8;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	margin-right: 20rpx;
}

.link-icon {
	width: 60rpx;
	height: 60rpx;
}

.link-content {
	flex: 1;
	min-width: 0;
}

.link-header {
	display: flex;
	align-items: center;
	margin-bottom: 10rpx;
}

.link-title {
	font-size: 30rpx;
	color: #333;
	font-weight: bold;
	margin-right: 12rpx;
}

.link-type-tag {
	font-size: 20rpx;
	padding: 4rpx 12rpx;
	border-radius: 8rpx;
}

.link-type-tag.type-0 {
	background-color: #e6f7ff;
	color: #1890ff;
}

.link-type-tag.type-1 {
	background-color: #f6ffed;
	color: #52c41a;
}

.link-desc {
	font-size: 26rpx;
	color: #666;
	line-height: 1.5;
	display: block;
}

.link-notice {
	display: flex;
	align-items: flex-start;
	margin-top: 12rpx;
	padding: 12rpx 16rpx;
	background-color: #fff7e6;
	border-radius: 8rpx;
}

.notice-icon {
	font-size: 24rpx;
	color: #fa8c16;
	margin-right: 8rpx;
	flex-shrink: 0;
}

.notice-text {
	font-size: 22rpx;
	color: #fa8c16;
	line-height: 1.4;
	flex: 1;
}

.link-arrow {
	margin-left: 16rpx;
	display: flex;
	align-items: center;
}

.arrow-icon {
	font-size: 36rpx;
	color: #ccc;
}

/* 空状态 */
.empty-state {
	margin-top: 200rpx;
	display: flex;
	flex-direction: column;
	align-items: center;
}

.empty-text {
	font-size: 32rpx;
	color: #999;
	margin-bottom: 16rpx;
}

.empty-hint {
	font-size: 26rpx;
	color: #bbb;
}

/* 主题适配 - 方舟 */
.container.theme-ark {
	background-color: #1a1a2e;
}

.container.theme-ark .nav-bar {
	background-color: #16213e;
}

.container.theme-ark .nav-title,
.container.theme-ark .back-icon {
	color: #fff;
}

.container.theme-ark .intro-text {
	color: #a0a0a0;
}

.container.theme-ark .link-card {
	background-color: #16213e;
	box-shadow: 0 4rpx 20rpx rgba(0,0,0,0.2);
}

.container.theme-ark .link-icon-wrap {
	background-color: #1a1a2e;
}

.container.theme-ark .link-title {
	color: #fff;
}

.container.theme-ark .link-desc {
	color: #a0a0a0;
}

.container.theme-ark .arrow-icon {
	color: #666;
}

.container.theme-ark .empty-text,
.container.theme-ark .empty-hint {
	color: #666;
}
	/* 免责声明 */
	.disclaimer {
		margin: 40rpx 30rpx 60rpx;
		padding: 24rpx 20rpx;
		background-color: #f5f5f5;
		border-radius: 12rpx;
		text-align: center;
	}

	.disclaimer-line {
		font-size: 22rpx;
		color: #999;
		line-height: 1.8;
	}

	.container.theme-ark .disclaimer {
		background-color: #1a1a2e;
	}

	.container.theme-ark .disclaimer-line {
		color: #666;
	}


</style>
