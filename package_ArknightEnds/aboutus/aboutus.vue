<template>
	<view class="container">
		<!-- 顶部Logo和名称 -->
		<view class="header-section">
			<view class="logo-container">
				<image class="app-logo" src="/static/app-logo.png" mode="aspectFit"></image>
			</view>
			<text class="app-name">方舟终末白名单凭证小助手</text>
			<image class="qq-logo" src="/static/QQ.png"></image><text class="app-version">反馈群：128568825</text>
			<p></p>
			<image class="qq-logo" src="/static/QQ.png"></image><text class="app-version">通行证交流群：1045868302</text>
			<p></p>
			<text class="app-version-link" @click="gotoNotice('697a057b1f9139d2e7b9b6a7')">免责声明</text>
			<text class="app-version-link" @click="gotoNotice('697a059bf07d9206ee9a9550')">长期公告</text>
			<text class="app-version-link" @click="gotoNotice('697a04d052987431f1b5d5a7')">隐私政策</text>
		</view>

		<!-- 开发者信息 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">开发者</text>
			</view>
			<view class="section-content">
				<view class="grid-container">
					<view 
						class="contributor-item" 
						v-for="(dev, index) in visibleDevelopers" 
						:key="index"
					>
						<view class="avatar-container">
							<image class="contributor-avatar" :src="dev.avatar" mode="aspectFit"></image>
						</view>
						<text class="contributor-name">{{ dev.name }}</text>
						<text class="contributor-role">{{ dev.role }}</text>
					</view>
				</view>
				<view class="expand-section" v-if="developers.length > 4">
					<text class="expand-text" @click="toggleExpand('developers')">
						{{ expandedSections.developers ? '收起' : `展开全部 ${developers.length} 位开发者` }}
					</text>
					<text class="expand-icon">{{ expandedSections.developers ? '−' : '+' }}</text>
				</view>
			</view>
		</view>

		<!-- 玩家贡献 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">玩家贡献</text>
			</view>
			<view class="section-content">
				<view class="grid-container">
					<view 
						class="contributor-item" 
						v-for="(contributor, index) in visibleContributors" 
						:key="index"
					>
						<view class="avatar-container">
							<image class="contributor-avatar" :src="contributor.avatar" mode="aspectFit"></image>
						</view>
						<text class="contributor-name">{{ contributor.name }}</text>
						<text class="contributor-role">{{ contributor.contribution }}</text>
					</view>
					
					<view class="contributor-item"  @click="onCarouselItemClick()">
						<view class="avatar-container">
							<image class="contributor-avatar" src="/static/add.png" mode="aspectFit"></image>
						</view>
						<text class="contributor-name">加入贡献</text>
						<text class="contributor-role">加入玩家贡献</text>
					</view>
				</view>
				<view class="expand-section" v-if="contributors.length > 4">
					<text class="expand-text" @click="toggleExpand('contributors')">
						{{ expandedSections.contributors ? '收起' : `展开全部 ${contributors.length} 位贡献者` }}
					</text>
					<text class="expand-icon">{{ expandedSections.contributors ? '−' : '+' }}</text>
				</view>
			</view>
		</view>

		<!-- 服务支持 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">服务支持</text>
			</view>
			<view class="section-content">
				<view class="support-grid">
					<view class="support-item" @click="gotofeedback()">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/questionnaire-tool.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">问题反馈</text>
						<text class="support-desc">遇到问题或有好建议？欢迎反馈</text>
					</view>
					<view class="support-item" @click="gotoquestionnaire()">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/notice-icon.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">广告反馈</text>
						<text class="support-desc">遇到广告问题？点击给我们反馈</text>
					</view>
					<view class="support-item" @click="gotoCustomSerachWordquestionnaire()">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/questionnaire-tool.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">外号搜索词征集</text>
						<text class="support-desc">参与征集干员搜索外号词问卷</text>
					</view>
					<!-- <view class="support-item">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/support-icon.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">技术支持</text>
						<text class="support-desc">提供稳定的服务支持</text>
					</view> -->
				</view>
			</view>
		</view>

		<!-- 特别鸣谢 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">特别鸣谢</text>
			</view>
			<view class="section-content">
				<view class="grid-container">
					<view 
						class="contributor-item" 
						v-for="(thank, index) in visibleThanks" 
						:key="index"
					>
						<view class="avatar-container">
							<image class="contributor-avatar" :src="thank.avatar" mode="aspectFit"></image>
						</view>
						<text class="contributor-name">{{ thank.name }}</text>
						<text class="contributor-role">{{ thank.role }}</text>
					</view>
				</view>
				<view class="expand-section" v-if="thanks.length > 4">
					<text class="expand-text" @click="toggleExpand('thanks')">
						{{ expandedSections.thanks ? '收起' : `展开全部 ${thanks.length} 位鸣谢` }}
					</text>
					<text class="expand-icon">{{ expandedSections.thanks ? '−' : '+' }}</text>
				</view>
			</view>
		</view>

		<!-- 版权信息 -->
		<view class="footer-section">
			<text class="copyright-text">方舟终末白名单凭证小助手</text>
			<text class="copyright-text">本工具为玩家自制，与官方无关</text>
		</view>
	</view>
</template>

<script>
	export default {
		data() {
			return {
				expandedSections: {
					developers: false,
					contributors: false,
					thanks: false
				},
				// 开发者数据
				developers: [
					{ avatar: 'https://cdn-img.gitcode.com/ce/dc/3c5021e04dcecfe62128e57eed8c46953ee2ec07db99d6f18d3836bebd602703.png?time=1762782076962', name: '黄金洲', role: '项目开发' },
					{ avatar: 'https://cdn-img.gitcode.com/ea/fd/e88a2fd71f3d8ae99826067868c104188f434e56ec299a166f09214b04a72c7e.png?time=1762782475487', name: '小丛子', role: '多语言功能开发' },
				],
				// 玩家贡献者数据
				contributors: [
					{ avatar: '/static/default-avatar.png', name: '虚位以待', contribution: '虚位以待' },
				],
				// 特别鸣谢数据
				thanks: [
					{ avatar: '/static/default-avatar.png', name: 'PRTS Wiki', role: '提供部分干员头像/英文名' },
					{ avatar: '/static/default-avatar.png', name: '明日方舟官方', role: '数据支持' },
					{ avatar: 'https://bkimg.cdn.bcebos.com/pic/b64543a98226cffc1e170998dd565d90f603738dbcd8?x-bce-process=image', name: '森空岛', role: '提供部分非可操控干员头像' },
					{ avatar: 'https://p.qlogo.cn/gh/1045868302/1045868302/40', name: '长沙罗德岛秘密交易中心', role: '赞助/研发'}
				]
			}
		},
		computed: {
			// 可见的开发者（根据展开状态）
			visibleDevelopers() {
				return this.expandedSections.developers ? this.developers : this.developers.slice(0, 4);
			},
			// 可见的贡献者（根据展开状态）
			visibleContributors() {
				return this.expandedSections.contributors ? this.contributors : this.contributors.slice(0, 4);
			},
			// 可见的鸣谢（根据展开状态）
			visibleThanks() {
				return this.expandedSections.thanks ? this.thanks : this.thanks.slice(0, 4);
			}
		},
		onLoad() {
			this.getAppVersion();
			// 设置分享配置
			    wx.showShareMenu({
			      withShareTicket: true,
			      menus: ['shareAppMessage', 'shareTimeline']
			    });
			  },
			  
			  onShareAppMessage() {
			    return {
			      title: '方舟通行证谷子查询工具-关于我们', // 您可以修改这个标题
			      path: '/packageA/aboutus/aboutus', // 分享路径
			      imageUrl: '' // 如果留空，会自动使用小程序截图
			    }
			  },
			  
			  onShareTimeline() {
			    return {
			      title: '方舟通行证谷子查询工具-关于我们', // 您可以修改这个标题
			      imageUrl: '' // 如果留空，会自动使用小程序截图
			    }
			  },
		
		methods: {
			gotofeedback() {
				wx.openEmbeddedMiniProgram({
				appId: 'wxebadf544ddae62cb',
				path: 'pages/webview/index?sid=24775309&hash=7022&navigateBackMiniProgram=true',});
			},
			
			// 跳转到公告页面（新增方法）
			gotoNotice(id) {
				if (id) {
					uni.navigateTo({
						url: `/packageA/more-notice/more-notice?id=${id}&isLeftPanelCollapsed=true`
					});
				} else {
					uni.navigateTo({
						url: '/packageA/more-notice/more-notice'
					});
				}
			},
			
			// 获取应用版本
			getAppVersion() {
				this.appVersion = '1.4.5.3';
			},
			
			// 切换展开状态
			toggleExpand(section) {
				this.expandedSections[section] = !this.expandedSections[section];
			},
			
			
			onCarouselItemClick() {
			  // 统一复制固定链接并提示用户
			  const fixedLink = "https://afdian.com/a/AuthorizationSeries";
			  
			  // 复制链接到剪贴板
			  uni.setClipboardData({
			    data: fixedLink,
			    success: () => {
			      // 显示提示信息
			      uni.showToast({
			        title: '链接复制成功',
			        icon: 'success'
			      });
			    },
			    fail: (err) => {
			      console.error('复制链接失败:', err);
			      uni.showToast({
			        title: '复制失败，请手动复制链接',
			        icon: 'none'
			      });
			    }
			  });
			},
			
			//跳转至微信小程序问卷页面
			gotoquestionnaire() {
				wx.openEmbeddedMiniProgram({
				appId: 'wxebadf544ddae62cb',
				path: 'pages/webview/index?sid=25128247&hash=3578&navigateBackMiniProgram=true',});
			},
			
			gotoCustomSerachWordquestionnaire(){
				wx.openEmbeddedMiniProgram({
					appId: 'wxebadf544ddae62cb',
					path: 'pages/webview/index?sid=24662080&hash=738a&navigateBackMiniProgram=true',
				});
			}
		}
	}
</script>

<style scoped>
	.container {
		padding: 30rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}

	/* 顶部区域 */
	.header-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 40rpx 30rpx;
		text-align: center;
		margin-bottom: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
	}

	.logo-container {
		width: 120rpx;
		height: 120rpx;
		border-radius: 24rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		margin: 0 auto 20rpx;
	}

	.app-logo {
		width: 60rpx;
		height: 60rpx;
	}
	
	.qq-logo {
		width: 30rpx;
		height: 30rpx;
	}

	.app-name {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
		display: block;
		margin-bottom: 10rpx;
	}

	.app-version {
		font-size: 26rpx;
		color: #999;
	}
	
	.app-version-link {
		font-size: 26rpx;
		color: #409EFF;
		margin: 0 10rpx;
		text-decoration: underline;
	}
	
	.app-version-link:active {
		opacity: 0.7;
	}

	/* 分区样式 */
	.section {
		background-color: #fff;
		border-radius: 16rpx;
		margin-bottom: 30rpx;
		overflow: hidden;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
	}

	.section-header {
		padding: 25rpx 30rpx;
		border-bottom: 1rpx solid #f0f0f0;
		background-color: #fafafa;
	}

	.section-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #333;
	}

	.section-content {
		padding: 30rpx;
	}

	/* 网格布局 */
	.grid-container {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 30rpx 20rpx;
		margin-bottom: 20rpx;
	}

	/* 贡献者项目 */
	.contributor-item {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
	}

	.avatar-container {
		width: 100rpx;
		height: 100rpx;
		border-radius: 50%;
		background-color: #f0f0f0;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-bottom: 15rpx;
		overflow: hidden;
	}

	.contributor-avatar {
		width: 60rpx;
		height: 60rpx;
	}

	.contributor-name {
		font-size: 24rpx;
		color: #333;
		font-weight: 500;
		margin-bottom: 5rpx;
		line-height: 1.2;
	}

	.contributor-role {
		font-size: 20rpx;
		color: #666;
		line-height: 1.2;
	}

	/* 展开/收起区域 */
	.expand-section {
		display: flex;
		align-items: center;
		justify-content: center;
		padding-top: 20rpx;
		border-top: 1rpx solid #f5f5f5;
		color: #409EFF;
		font-size: 26rpx;
	}

	.expand-text {
		margin-right: 10rpx;
	}

	.expand-icon {
		font-size: 24rpx;
		font-weight: bold;
	}

	/* 服务支持网格 */
	.support-grid {
		display: grid;
		grid-template-columns: repeat(2, 1fr);
		gap: 30rpx;
	}

	.support-item {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		padding: 25rpx 20rpx;
		background-color: #f8f9fa;
		border-radius: 12rpx;
	}

	.support-icon-container {
		width: 80rpx;
		height: 80rpx;
		border-radius: 16rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-bottom: 15rpx;
	}

	.support-icon {
		width: 40rpx;
		height: 40rpx;
	}

	.support-name {
		font-size: 26rpx;
		color: #333;
		font-weight: 500;
		margin-bottom: 8rpx;
	}

	.support-desc {
		font-size: 22rpx;
		color: #666;
		line-height: 1.3;
	}

	/* 底部版权 */
	.footer-section {
		text-align: center;
		padding: 30rpx 0;
	}

	.copyright-text {
		font-size: 24rpx;
		color: #999;
		display: block;
		margin-bottom: 10rpx;
	}

	.copyright-text:last-child {
		margin-bottom: 0;
	}

	/* 响应式调整 */
	@media (max-width: 750rpx) {
		.grid-container {
			grid-template-columns: repeat(2, 1fr);
			gap: 40rpx 30rpx;
		}
		
		.support-grid {
			grid-template-columns: 1fr;
		}
	}
</style>