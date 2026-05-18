<template>
	<view :class="['container', 'theme-' + themeMode]">
		<!-- 头部盒号信息 -->
		<view class="header">
			<text class="box-title">通行认证 {{ boxId }}</text>
		</view>

		<!-- 资料信息区域 -->
		<view class="info-section">
			<!-- 修改标题行，使用flex布局 -->
			<view class="section-title-container">
				<text class="section-title">资料</text>
				<text 
					class="feedback-link" 
					@click="gotoFeedback"
				>资料勘记有误，点我去反馈哟&gt;</text>
			</view>
			
			<view class="info-item">
				<text class="info-label">发售日期</text>
				<text class="info-value">{{ boxData.release_date || '暂无记录' }}</text>
			</view>
			
			<view class="info-item">
				<text class="info-label">尺寸</text>
				<text class="info-value">{{ boxData.size || '暂无记录' }}</text>
			</view>
			
			<view class="info-item">
				<text class="info-label">材质</text>
				<text class="info-value">{{ boxData.material || '暂无记录' }}</text>
			</view>
			
			<view class="info-item">
				<text class="info-label">零售价格</text>
				<text class="info-value">{{ boxData.retail_price || '暂无记录' }}</text>
			</view>
			
			<view class="info-item">
				<text class="info-label">类型</text>
				<text class="info-value">{{ getTypeText(boxData.type) }}</text>
			</view>
			
			<view class="info-item">
				<text class="info-label">是否有复刻</text>
				<text class="info-value">{{ getReplicateText(boxData.replicate) }}</text>
				<text v-if="boxData.replicate === 'true' || boxData.replicate === true" class="info-value">
					(复刻时间: {{ boxData.replicate_date || '暂无数据' }})
				</text>
			</view>
		</view>

		<!-- 官方大图区域 -->
		<view class="image-section" v-if="boxData.Box_ImageUrl">
			<view class="section-title">官方大图</view>
			<view class="image-container">
				<view 
					class="image-wrapper"
					@tap="previewImage"
				>
					<image 
						class="box-image" 
						:src="boxData.Box_ImageUrl" 
						mode="widthFix"
						@load="onImageLoad"
						@error="onImageError"
						:style="{ minHeight: imageLoading ? '200px' : 'auto' }"
						show-menu-by-longpress
					></image>
				</view>
				<view class="image-tip" v-if="!imageLoading">
					<text>点击图片预览大图</text>
				</view>
				<view class="image-loading" v-if="imageLoading">图片加载中...</view>
			</view>
		</view>

		<!-- 加载状态 -->
		<view class="loading-state" v-if="loading">
			<text class="loading-text">正在加载数据...</text>
		</view>

		<!-- 空状态 -->
		<view class="empty-state" v-if="!loading && (!boxData || Object.keys(boxData).length === 0)">
			<image class="empty-icon" src="/static/empty-icon.png"></image>
			<text class="empty-text">暂无该盒号信息</text>
			<button class="back-btn" @click="goBack">返回列表</button>
			<button class="refresh-btn" @click="forceRefresh">重新加载数据</button>
		</view>
	</view>
</template>

<script>
	export default {
		data() {
			return {
				// 主题模式
				themeMode: 'simple',
				boxId: '',
				boxData: {},
				imageLoading: false,
				loading: false,
				// 下拉刷新相关
				isRefreshing: false
			}
		},
		onLoad(options) {
			// 加载主题设置
			this.loadThemeSetting();

			console.log('box_info页面加载，参数:', options);
			// 兼容 calendar.vue 传的 box_id 参数名
			this.boxId = options.boxId || options.box_id || '';
			this.loadBoxData();
			// 设置分享配置
			wx.showShareMenu({
				withShareTicket: true,
				menus: ['shareAppMessage', 'shareTimeline']
			});
			
			uni.setNavigationBarTitle({
				title: `明日方舟通行认证 ${this.boxId} 盒号信息`,
			});
		},
		
		// 下拉刷新生命周期
		onPullDownRefresh() {
			console.log('触发下拉刷新');
			this.handleRefresh();
		},
		  
		onShareAppMessage() {
			return {
				title: `明日方舟通行认证 ${this.boxId} 盒号信息`,
				path: `/pages/box_info/box_info?boxId=${this.boxId}`,
				imageUrl: ''
			}
		},
		
		onShareTimeline() {
			return {
				title: `明日方舟通行认证 ${this.boxId} 盒号信息`,
				imageUrl: ''
			}
		},
		
		methods: {
			// 加载主题设置
				loadThemeSetting() {
					try {
						const themeMode = uni.getStorageSync('themeMode');
						if (themeMode && (themeMode === 'simple' || themeMode === 'ark' || themeMode === 'jieyuan')) {
							this.themeMode = themeMode;
						} else {
							this.themeMode = 'simple';
						}
						// 4月1日愚人节彩蛋 - 仅在当日首次触发
						const now = new Date();
						const month = now.getMonth() + 1;
						const day = now.getDate();
						const year = now.getFullYear();
						
						if (month === 4 && day === 1) {
							// 检查是否已在今天触发过
							const aprilFoolsTriggered = uni.getStorageSync('aprilFoolsTriggered');
							const triggeredDate = aprilFoolsTriggered ? aprilFoolsTriggered.date : null;
							const today = `${year}-04-01`;
							
							// 如果今天还没触发过，则触发
							if (triggeredDate !== today) {
								this.themeMode = 'jieyuan';
								uni.showToast({
									title:'博士，愚人节快乐~',
									icon:'error'
								});
								// 记录今天已触发
								uni.setStorageSync('aprilFoolsTriggered', { date: today, triggered: true });
							}
						}
						console.log('加载主题设置:', this.themeMode);
					} catch (e) {
						console.error('加载主题设置失败:', e);
						this.themeMode = 'simple';
					}
				},

			// 跳转到反馈问卷
			gotoFeedback() {
				console.log('跳转到反馈问卷');
				try {
					// 微信小程序跳转方法
					if (wx && wx.openEmbeddedMiniProgram) {
						wx.openEmbeddedMiniProgram({
							appId: 'wxebadf544ddae62cb',
							path: 'pages/webview/index?sid=25733968&hash=15bc&navigateBackMiniProgram=true',
							success: (res) => {
								console.log('跳转问卷成功:', res);
							},
							fail: (err) => {
								console.error('跳转问卷失败:', err);
								// 备用方案：显示提示
								uni.showToast({
									title: '跳转失败，请稍后重试',
									icon: 'none'
								});
							}
						});
					} else {
						// 如果不是微信环境，显示提示
						uni.showToast({
							title: '请在微信小程序中打开此功能',
							icon: 'none'
						});
					}
				} catch (error) {
					console.error('跳转问卷异常:', error);
					uni.showToast({
						title: '跳转失败',
						icon: 'none'
					});
				}
			},
			
			// 图片预览 - 使用微信小程序的previewImage
			previewImage() {
				if (!this.boxData.Box_ImageUrl) {
					return;
				}
				
				console.log('预览图片:', this.boxData.Box_ImageUrl);
				
				// 使用微信小程序的图片预览API
				wx.previewImage({
					urls: [this.boxData.Box_ImageUrl],
					current: this.boxData.Box_ImageUrl,
					success: () => {
						console.log('图片预览成功');
					},
					fail: (err) => {
						console.error('图片预览失败:', err);
						uni.showToast({
							title: '图片预览失败',
							icon: 'none'
						});
					}
				});
			},
			
			// 下拉刷新处理
			handleRefresh() {
				if (this.isRefreshing) {
					return;
				}
				
				this.isRefreshing = true;
				console.log('开始下拉刷新数据');
				
				// 显示刷新状态
				uni.showLoading({
					title: '刷新中...'
				});
				
				// 重新加载数据
				this.forceRefresh().finally(() => {
					// 隐藏刷新状态
					uni.hideLoading();
					uni.stopPullDownRefresh();
					this.isRefreshing = false;
					
					// 显示刷新成功提示
					uni.showToast({
						title: '刷新成功',
						icon: 'success',
						duration: 1500
					});
				});
			},
			
			// 加载盒子数据
			loadBoxData() {
				if (!this.boxId) {
					uni.showToast({
						title: '盒号参数错误',
						icon: 'none'
					});
					return;
				}

				this.loading = true;
				
				try {
					// 从本地缓存加载数据
					const localData = this.loadLocalData();
					
					if (localData && localData.length > 0) {
						console.log('从本地缓存加载数据');
						this.findBoxInData(localData);
					} else {
						console.log('本地无缓存数据，从网络加载');
						this.loadDataFromNetwork();
					}
				} catch (error) {
					console.error('加载数据失败:', error);
					uni.showToast({
						title: '数据加载失败',
						icon: 'none'
					});
				} finally {
					this.loading = false;
				}
			},
			
			// 加载本地数据
			loadLocalData() {
				try {
					const data = uni.getStorageSync('arknightsData');
					const enableGuessData = uni.getStorageSync('enableGuessData');
					const guessData = uni.getStorageSync('guessData');
					
					let allData = [];
					
					if (data && Array.isArray(data) && data.length > 0) {
						allData = [...data];
						console.log('本地数据加载成功:', allData.length, '个盒号');
					}
					
					// 如果启用了预测数据，合并预测数据
					if (enableGuessData === 'true' && guessData && Array.isArray(guessData) && guessData.length > 0) {
						allData = [...allData, ...guessData];
						console.log('合并预测数据:', guessData.length, '个盒号');
					}
					
					return allData;
				} catch (e) {
					console.error('加载本地数据失败:', e);
					return [];
				}
			},
			
			// 在数据中查找盒子
			findBoxInData(data) {
				console.log('在数据中查找盒号:', this.boxId);
				
				const box = data.find(item => {
					return String(item.Box_id) === String(this.boxId);
				});
				
				console.log('找到的盒子数据:', box);
				console.log('type 字段类型:', typeof box?.type, '值:', box?.type);
				console.log('replicate 字段类型:', typeof box?.replicate, '值:', box?.replicate);
				
				if (box) {
					this.boxData = box;
					console.log('盒子信息数据:', {
						release_date: box.release_date,
						size: box.size,
						material: box.material,
						retail_price: box.retail_price,
						type: box.type,
						replicate: box.replicate
					});
				} else {
					console.log('未找到盒号:', this.boxId);
					uni.showToast({
						title: '未找到该盒号信息',
						icon: 'none'
					});
				}
			},
			
			// 从网络加载数据
			loadDataFromNetwork() {
				return new Promise((resolve, reject) => {
					// 获取数据URL
					const dataUrl = uni.getStorageSync('dataUrl') || 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
					
					console.log('从网络加载数据，URL:', dataUrl);
					
					uni.request({
						url: dataUrl,
						method: 'GET',
						timeout: 15000,
						success: (res) => {
							if (res.statusCode === 200) {
								let data = res.data;
								
								// 处理字符串数据
								if (typeof data === 'string') {
									try {
										data = JSON.parse(data);
									} catch (e) {
										console.error('JSON解析失败:', e);
										uni.showToast({
											title: '数据格式错误',
											icon: 'none'
										});
										reject(e);
										return;
									}
								}
								
								if (Array.isArray(data)) {
									console.log('网络数据加载成功，总数:', data.length);
									this.findBoxInData(data);
									resolve(data);
								} else {
									uni.showToast({
										title: '数据格式不正确',
										icon: 'none'
									});
									reject(new Error('数据格式不正确'));
								}
							} else {
								uni.showToast({
									title: `服务器错误: ${res.statusCode}`,
									icon: 'none'
								});
								reject(new Error(`服务器错误: ${res.statusCode}`));
							}
						},
						fail: (err) => {
							console.error('网络请求失败:', err);
							uni.showToast({
								title: '网络请求失败',
								icon: 'none'
							});
							reject(err);
						}
					});
				});
			},

			// 获取类型文本 - 修复字符串 "true"/"false" 问题
			getTypeText(type) {
				console.log('getTypeText 输入:', type, '类型:', typeof type);
				
				// 处理字符串 "true" 和 "false"
				if (type === true || type === "true") {
					return '盲抽';
				} else if (type === false || type === "false") {
					return '单领/赠品';
				} else {
					return '暂无记录';
				}
			},

			// 获取复刻文本 - 修复字符串 "true"/"false" 问题
			getReplicateText(replicate) {
				console.log('getReplicateText 输入:', replicate, '类型:', typeof replicate);
				
				// 处理字符串 "true" 和 "false"
				if (replicate === true || replicate === "true") {
					return '是';
				} else if (replicate === false || replicate === "false") {
					return '否';
				} else {
					return '暂无记录';
				}
			},

			// 图片加载成功
			onImageLoad() {
				console.log('官方大图加载成功');
				this.imageLoading = false;
			},

			// 图片加载失败
			onImageError() {
				console.error('官方大图加载失败');
				this.imageLoading = false;
				uni.showToast({
					title: '图片加载失败',
					icon: 'none'
				});
			},

			// 返回列表
			goBack() {
				uni.navigateBack();
			},
			
			// 强制刷新数据
			forceRefresh() {
				this.loading = true;
				return this.loadDataFromNetwork().finally(() => {
					this.loading = false;
				});
			}
		}
	}
</script>

<style scoped>
	.container {
		padding: 20rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}

	.header {
		text-align: center;
		margin-bottom: 20rpx;
		padding: 20rpx 0;
		background-color: #fff;
		border-radius: 12rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.box-title {
		font-size: 36rpx;
		font-weight: bold;
		color: #333;
	}

	/* 资料区域标题容器 */
	.section-title-container {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 20rpx;
		flex-wrap: wrap;
	}

	.section-title {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
		display: inline-block;
	}

	/* 反馈链接样式 */
	.feedback-link {
		font-size: 24rpx;
		color: #409EFF;
		text-decoration: underline;
		padding: 8rpx 12rpx;
		border-radius: 6rpx;
		transition: all 0.2s ease;
		text-align: right;
		flex-shrink: 0;
		margin-left: 20rpx;
	}

	/* 点击效果 */
	.feedback-link:active {
		background-color: rgba(64, 158, 255, 0.1);
		color: #337ecc;
		transform: scale(0.98);
	}

	.info-section {
		background-color: #fff;
		border-radius: 12rpx;
		padding: 25rpx 20rpx;
		margin-bottom: 20rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.info-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 15rpx 0;
		border-bottom: 1rpx solid #f0f0f0;
	}

	.info-item:last-child {
		border-bottom: none;
	}

	.info-label {
		font-size: 26rpx;
		color: #666;
		flex: 1;
	}

	.info-value {
		font-size: 26rpx;
		color: #333;
		font-weight: 500;
		flex: 2;
		text-align: right;
	}

	.image-section {
		background-color: #fff;
		border-radius: 12rpx;
		padding: 25rpx 20rpx;
		margin-bottom: 20rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.image-container {
		text-align: center;
	}

	.image-wrapper {
		display: inline-block;
		width: 100%;
	}

	.box-image {
		width: 100%;
		border-radius: 8rpx;
		background-color: #f8f8f8;
	}

	.image-tip {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8rpx;
		margin-top: 15rpx;
	}

	.image-tip text {
		font-size: 22rpx;
		color: #999;
	}

	.image-loading {
		font-size: 26rpx;
		color: #999;
		text-align: center;
		padding: 30rpx 0;
	}

	.loading-state {
		display: flex;
		justify-content: center;
		align-items: center;
		padding: 60rpx 0;
		background-color: #fff;
		border-radius: 12rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.loading-text {
		font-size: 28rpx;
		color: #666;
	}

	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 80rpx 0;
		background-color: #fff;
		border-radius: 12rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.empty-icon {
		width: 100rpx;
		height: 100rpx;
		margin-bottom: 20rpx;
		opacity: 0.5;
	}

	.empty-text {
		font-size: 26rpx;
		color: #999;
		margin-bottom: 20rpx;
	}

	.back-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 40rpx;
		font-size: 24rpx;
		padding: 15rpx 30rpx;
		margin-bottom: 15rpx;
	}

	.refresh-btn {
		background-color: #67C23A;
		color: #fff;
		border-radius: 40rpx;
		font-size: 24rpx;
		padding: 15rpx 30rpx;
	}

	/* 响应式调整 */
	@media (max-width: 750rpx) {
		.section-title-container {
			flex-direction: column;
			align-items: flex-start;
		}
		
		.feedback-link {
			align-self: flex-end;
			margin-top: 10rpx;
			margin-left: 0;
		}
	}

	@media (min-width: 1000rpx) {
		.container {
			max-width: 1200rpx;
			margin: 0 auto;
		}
	}

	/* ========== 科技风格 ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
	}
	.container.theme-ark .box-detail-card,
	.container.theme-ark .market-price-section,
	.container.theme-ark .history-section {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border: 1rpx solid #2A2A4A;
	}
	.container.theme-ark .box-id,
	.container.theme-ark .character-name,
	.container.theme-ark .box-info-text,
	.container.theme-ark .section-title { color: #e0e0e0; }
	.container.theme-ark .character-tag { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; }
	.container.theme-ark .box-meta { color: #888; }
	.container.theme-ark .action-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .back-btn { background: rgba(255,107,53,0.1); color: #FF6B35; }
	.container.theme-ark .share-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .box-info-label { color: #888; }
	.container.theme-ark .favorite-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .favorite-btn.favorited { background: #555; color: #FF6B35; }
	.container.theme-ark .market-price-tag { background: rgba(255,107,53,0.1); color: #FF6B35; border-color: rgba(255,107,53,0.3); }
	.container.theme-ark .market-price-value { color: #e0e0e0; }
	.container.theme-ark .history-item { border-bottom-color: #2A2A4A; }
	.container.theme-ark .history-text { color: #888; }
	.container.theme-ark .history-time { color: #888; }
	.container.theme-ark .empty-state { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border-color: #2A2A4A; }
	.container.theme-ark .empty-icon { filter: brightness(0) invert(1); opacity: 0.4; }
	.container.theme-ark .empty-text { color: #888; }

	/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .box-detail-card,
	.container.theme-jieyuan .market-price-section,
	.container.theme-jieyuan .history-section {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .box-id,
	.container.theme-jieyuan .character-name,
	.container.theme-jieyuan .box-info-text,
	.container.theme-jieyuan .section-title { color: #333; }
	.container.theme-jieyuan .character-tag { background: linear-gradient(90deg, #e25884, #399383); color: #fff; }
	.container.theme-jieyuan .box-meta { color: #888; }
	.container.theme-jieyuan .action-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .back-btn { background: rgba(226, 88, 132, 0.1); color: #e25884; }
	.container.theme-jieyuan .share-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .box-info-label { color: #888; }
	.container.theme-jieyuan .favorite-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .favorite-btn.favorited { background: rgba(226, 88, 132, 0.15); color: #e25884; }
	.container.theme-jieyuan .market-price-tag { background: rgba(226, 88, 132, 0.1); color: #e25884; border-color: rgba(226, 88, 132, 0.3); }
	.container.theme-jieyuan .market-price-value { color: #333; }
	.container.theme-jieyuan .history-item { border-bottom-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .history-text { color: #666; }
	.container.theme-jieyuan .history-time { color: #888; }
	.container.theme-jieyuan .empty-state { background: rgba(255, 255, 255, 0.9); border-color: rgba(226, 88, 132, 0.15); }
	.container.theme-jieyuan .empty-icon { opacity: 0.5; }
	.container.theme-jieyuan .empty-text { color: #888; }
</style>
