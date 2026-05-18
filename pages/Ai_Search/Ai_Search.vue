<template>
	<view :class="['container', 'theme-' + themeMode]">
		<view class="header">
			<!-- <text class="title">模拟抽卡</text> -->
			<uni-notice-bar scrollable single showIcon text="注意:当前AI模型正在测试当中,故搜索精度很低,且存在大部分角色无法搜出的情况,请您慎重使用!!!"></uni-notice-bar>
		</view>
		 <!-- 搜索区域 -->
		<view class="search-section">
			<view class="search-row">
				<view class="search-input-container" :class="{ 'focus': showSearchSuggestions }">
					<image class="search-icon" src="/static/search-icon.png"></image>
					<input 
						class="search-input" 
						v-model="searchText" 
						:placeholder="searchPlaceholder"
						@confirm="handleSearch"
						@focus="onSearchFocus"
						@blur="onSearchBlur"
						@input="onSearchInput"
					/>
				</view>
				<button class="search-btn" @click="handleSearch" :disabled="isLoading">
					{{ isLoading ? '查询中...' : '查询' }}
				</button>
			</view>
		</view>
		
		<!-- AI回答区域 -->
		<view class="ai-response-section" v-if="aiResponse || isLoading">
			<view class="response-header">
				<image class="ai-icon" src="/static/ai-icon.png"></image>
				<text class="response-title">AI助手回答</text>
				<view class="response-actions">
					<button class="copy-btn" @click="copyResponse" v-if="aiResponse">
						<image class="copy-icon" src="/static/copy-icon.png"></image>
						复制
					</button>
					<button class="clear-btn" @click="clearResponse" v-if="aiResponse || isLoading">
						<image class="clear-icon" src="/static/clear-icon.png"></image>
						清除
					</button>
				</view>
			</view>
			
			<view class="response-content">
				<!-- 加载动画 -->
				<view class="loading-container" v-if="isLoading">
					<view class="loading-dots">
						<view class="dot"></view>
						<view class="dot"></view>
						<view class="dot"></view>
					</view>
					<text class="loading-text">AI正在思考中，请稍候...</text>
				</view>
				
				<!-- AI回答内容 -->
				<view class="ai-content" v-if="aiResponse">
					<text class="ai-text">{{ aiResponse }}</text>
				</view>
			</view>
			
			<!-- 思考链（如果支持） -->
			<view class="thinking-section" v-if="showThinking && aiThinking">
				<text class="thinking-title">AI思考过程：</text>
				<text class="thinking-content">{{ aiThinking }}</text>
			</view>
			
			<!-- 回答信息 -->
			<view class="response-info" v-if="responseInfo">
				<text class="info-text">回答时间：{{ responseTime }}</text>
				<text class="info-text">AI模型：{{ aiModel }}</text>
			</view>
		</view>
		
		<!-- 历史记录 -->
		<view class="history-section" v-if="conversationHistory.length > 0 && !isLoading">
			<view class="history-header">
				<text class="history-title">历史对话</text>
				<button class="clear-history-btn" @click="clearHistory">清除历史</button>
			</view>
			
			<view class="history-list">
				<view 
					class="history-item" 
					v-for="(item, index) in conversationHistory" 
					:key="index"
					@click="loadHistory(item)"
				>
					<text class="history-question">{{ item.question }}</text>
					<text class="history-time">{{ formatTime(item.time) }}</text>
				</view>
			</view>
		</view>
		
		<!-- 使用说明 -->
		<view class="instructions-section" v-if="conversationHistory.length === 0 && !aiResponse">
			<view class="instruction-card">
				<image class="instruction-icon" src="/static/bulb-icon.png"></image>
				<view class="instruction-content">
					<text class="instruction-title">AI助手使用说明</text>
					<view class="instruction-list">
						<text class="instruction-item">• 可以查询干员所在的盒号</text>
						<text class="instruction-item">• 可以查询盒内包含的干员</text>
						<text class="instruction-item">• 支持自然语言提问</text>
					</view>
				</view>
			</view>
			
			<view class="example-section">
				<text class="example-title">提问示例：</text>
				<view class="example-list">
					<view 
						class="example-item" 
						v-for="(example, index) in examples" 
						:key="index"
						@click="useExample(example)"
					>
						<text class="example-text">{{ example }}</text>
						<image class="example-arrow" src="/static/right.png"></image>
					</view>
				</view>
			</view>
		
		<!-- 错误提示 -->
		<view class="error-section" v-if="errorMessage">
			<view class="error-content">
				<image class="error-icon" src="/static/error-icon.png"></image>
				<text class="error-text">{{ errorMessage }}</text>
			</view>
			<button class="retry-btn" @click="retrySearch">重试</button>
		</view>
		
		<!-- 底部信息 -->
		 <view class="footer-section">
			<text class="footer-text">本服务由腾讯元器提供</text>
			<text class="footer-text">内容仅供参考，请仔细甄别</text>
			<text class="footer-text">数据更新日期：{{ dataUpdateDate }}</text>
		</view>
	</view>
	</view>
</template>

<script>
	export default {
		data() {
			return {
				searchText: '',
				searchPlaceholder: '例如：艾雅法拉在哪个盒？或 盒1.0有哪些干员？',
				showSearchSuggestions: false,
				// AI相关
				aiResponse: '',
				aiThinking: '',
				isLoading: false,
				responseTime: '',
				aiModel: '云开发AI助手',
				showThinking: false,
				responseInfo: false,
				// 错误处理
				errorMessage: '',
				// 历史记录
				conversationHistory: [],
				// 使用示例
				examples: [
					'干员艾雅法拉在哪个盒？',
					'通行证1.0有哪些干员？',
					'凯尔希的盒号是多少？',
					'一共有多少个盒？'
				],
				// 数据更新
				dataUpdateDate: '2024-01-15',
				// 云开发环境
				cloudInitialized: false,
				// 主题模式
				themeMode: 'simple'
			}
		},

		onLoad() {
			console.log('AI搜索页面加载');
			this.loadThemeSetting();
			this.initCloud();
			this.loadHistoryFromStorage();
			this.loadDataUpdateDate();
		},
		
		onShow() {
			// 页面显示时重新检查云开发状态
			if (!this.cloudInitialized) {
				this.initCloud();
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

			// 初始化云开发
			async initCloud() {
				if (this.cloudInitialized) return;
				
				try {
					if (!wx.cloud) {
						console.error('请使用 2.2.3 或以上的基础库以使用云能力');
						this.errorMessage = '请更新微信版本以使用AI功能';
						return;
					}
					
					// 初始化云开发
					await wx.cloud.init({
						env: "arknights-series-8fzgq8l473ec963",
						traceUser: true
					});
					
					this.cloudInitialized = true;
					console.log('云开发初始化成功');
				} catch (error) {
					console.error('云开发初始化失败:', error);
					this.errorMessage = 'AI服务初始化失败，请稍后重试';
				}
			},
			
			// 从缓存加载历史记录
			loadHistoryFromStorage() {
				try {
					const history = uni.getStorageSync('aiConversationHistory');
					if (history && Array.isArray(history)) {
						this.conversationHistory = history.slice(0, 10); // 最多保留10条
					}
				} catch (e) {
					console.error('加载历史记录失败:', e);
				}
			},
			
			// 保存历史记录
			saveHistoryToStorage(question, answer) {
				try {
					const historyItem = {
						question: question,
						answer: answer,
						time: new Date().getTime()
					};
					
					// 添加到历史记录开头
					this.conversationHistory.unshift(historyItem);
					
					// 最多保留10条
					if (this.conversationHistory.length > 10) {
						this.conversationHistory = this.conversationHistory.slice(0, 10);
					}
					
					// 保存到本地存储
					uni.setStorageSync('aiConversationHistory', this.conversationHistory);
				} catch (e) {
					console.error('保存历史记录失败:', e);
				}
			},
			
			// 加载数据更新日期
			loadDataUpdateDate() {
				try {
					const updateTime = uni.getStorageSync('localUpdateTime');
					if (updateTime) {
						// 提取日期部分
						const dateMatch = updateTime.match(/\d{4}-\d{2}-\d{2}/);
						if (dateMatch) {
							this.dataUpdateDate = dateMatch[0];
						}
					}
				} catch (e) {
					console.error('加载数据更新日期失败:', e);
				}
			},
			
			// 搜索框相关方法
			onSearchFocus() {
				this.showSearchSuggestions = true;
			},
			
			onSearchBlur() {
				setTimeout(() => {
					this.showSearchSuggestions = false;
				}, 200);
			},
			
			onSearchInput() {
				// 可以在这里添加搜索建议逻辑
			},
			
			// 处理搜索
			async handleSearch() {
				if (!this.searchText.trim()) {
					uni.showToast({
						title: '请输入问题',
						icon: 'none'
					});
					return;
				}
				
				if (!this.cloudInitialized) {
					await this.initCloud();
					if (!this.cloudInitialized) {
						uni.showToast({
							title: 'AI服务未就绪',
							icon: 'none'
						});
						return;
					}
				}
				
				// 清空之前的响应和错误
				this.clearResponse();
				this.errorMessage = '';
				
				// 开始加载
				this.isLoading = true;
				this.responseTime = '';
				this.responseInfo = false;
				
				try {
					const question = this.searchText.trim();
					console.log('开始发送问题:', question);
					
					// 记录开始时间
					const startTime = Date.now();
					
					// 调用AI接口
					const result = await this.sendMessageToAI(question);
					
					// 计算响应时间
					const endTime = Date.now();
					const duration = (endTime - startTime) / 1000;
					this.responseTime = `${duration.toFixed(1)}秒`;
					
					// 保存历史记录
					this.saveHistoryToStorage(question, result.content || result);
					
					// 显示响应信息
					this.responseInfo = true;
					
					// 显示成功提示
					uni.showToast({
						title: 'AI已回答',
						icon: 'success',
						duration: 1500
					});
					
				} catch (error) {
					console.error('AI查询失败:', error);
					this.errorMessage = this.getErrorMessage(error);
					
					uni.showToast({
						title: '查询失败',
						icon: 'none'
					});
				} finally {
					this.isLoading = false;
				}
			},
			
			// 发送消息到AI
			async sendMessageToAI(message) {
				try {
					// 调用微信云开发的AI接口
					const res = await wx.cloud.extend.AI.bot.sendMessage({
						data: {
							botId: 'ibot-arknights-ks57pq',
							msg: message
						}
					});
					
					let fullResponse = '';
					let thinkingContent = '';
					
					// 处理流式响应
					for await (let event of res.eventStream) {
						// 收到结束信号，终止循环
						if (event.data === '[DONE]') {
							break;
						}
						
						try {
							const data = JSON.parse(event.data);
							
							// 获取思维链内容（如果支持）
							const think = data.reasoning_content;
							if (think) {
								thinkingContent += think;
								this.showThinking = true;
								this.aiThinking = thinkingContent;
							}
							
							// 获取输出正文
							const content = data.content;
							if (content) {
								fullResponse += content;
								// 实时更新显示
								this.aiResponse = fullResponse;
							}
						} catch (parseError) {
							console.warn('解析AI响应时出错:', parseError);
						}
					}
					
					// 如果没有使用流式API，尝试其他方式
					if (!fullResponse) {
						// 尝试使用textStream
						for await (let text of res.textStream) {
							fullResponse += text;
							this.aiResponse = fullResponse;
						}
					}
					
					// 如果还是没有内容，检查是否有直接响应
					if (!fullResponse && res.result) {
						fullResponse = res.result.content || JSON.stringify(res.result, null, 2);
						this.aiResponse = fullResponse;
					}
					
					if (!fullResponse) {
						throw new Error('AI没有返回有效内容');
					}
					
					return {
						content: fullResponse,
						thinking: thinkingContent
					};
					
				} catch (error) {
					console.error('调用AI接口失败:', error);
					throw error;
				}
			},
			
			// 获取错误信息
			getErrorMessage(error) {
				if (error.errMsg) {
					if (error.errMsg.includes('cloud function not found')) {
						return 'AI服务暂时不可用，请稍后重试';
					} else if (error.errMsg.includes('network timeout')) {
						return '网络超时，请检查网络连接';
					} else if (error.errMsg.includes('not auth')) {
						return '未授权访问AI服务';
					}
					return error.errMsg;
				}
				return error.message || '未知错误';
			},
			
			// 使用示例问题
			useExample(example) {
				this.searchText = example;
				this.handleSearch();
			},
			
			// 加载历史记录
			loadHistory(item) {
				this.searchText = item.question;
				this.aiResponse = item.answer;
				this.responseInfo = true;
				this.responseTime = '历史记录';
			},
			
			// 清除历史记录
			clearHistory() {
				uni.showModal({
					title: '确认清除',
					content: '确定要清除所有历史对话吗？',
					success: (res) => {
						if (res.confirm) {
							this.conversationHistory = [];
							uni.removeStorageSync('aiConversationHistory');
							uni.showToast({
								title: '历史记录已清除',
								icon: 'success'
							});
						}
					}
				});
			},
			
			// 清除AI响应
			clearResponse() {
				this.aiResponse = '';
				this.aiThinking = '';
				this.showThinking = false;
				this.responseInfo = false;
				this.isLoading = false;
				this.errorMessage = '';
			},
			
			// 复制响应内容
			copyResponse() {
				if (!this.aiResponse) return;
				
				uni.setClipboardData({
					data: this.aiResponse,
					success: () => {
						uni.showToast({
							title: '已复制到剪贴板',
							icon: 'success'
						});
					},
					fail: (err) => {
						console.error('复制失败:', err);
						uni.showToast({
							title: '复制失败',
							icon: 'none'
						});
					}
				});
			},
			
			// 重试搜索
			retrySearch() {
				this.errorMessage = '';
				this.handleSearch();
			},
			
			// 格式化时间
			formatTime(timestamp) {
				const date = new Date(timestamp);
				const now = new Date();
				const diff = now - date;
				
				// 如果是一天内，显示相对时间
				if (diff < 24 * 60 * 60 * 1000) {
					if (diff < 60 * 1000) {
						return '刚刚';
					} else if (diff < 60 * 60 * 1000) {
						const minutes = Math.floor(diff / (60 * 1000));
						return `${minutes}分钟前`;
					} else {
						const hours = Math.floor(diff / (60 * 60 * 1000));
						return `${hours}小时前`;
					}
				}
				
				// 否则显示日期
				const month = date.getMonth() + 1;
				const day = date.getDate();
				const hour = date.getHours().toString().padStart(2, '0');
				const minute = date.getMinutes().toString().padStart(2, '0');
				
				return `${month}/${day} ${hour}:${minute}`;
			}
		}
	}
</script>

<style>
	.container {
		padding: 30rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}
	
	/* 搜索区域 */
	.search-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
		position: relative;
	}
	
	.search-row {
		display: flex;
		align-items: center;
		gap: 20rpx;
	}
	
	.search-input-container {
		display: flex;
		align-items: center;
		background-color: #f8f8f8;
		border-radius: 50rpx;
		padding: 20rpx 30rpx;
		flex: 1;
		transition: all 0.3s ease;
		border: 2rpx solid transparent;
	}
	
	.search-input-container.focus {
		border-color: #409EFF;
		background-color: #fff;
		box-shadow: 0 0 10rpx rgba(64, 158, 255, 0.2);
	}
	
	.search-icon {
		width: 36rpx;
		height: 36rpx;
		margin-right: 20rpx;
	}
	
	.search-input {
		flex: 1;
		font-size: 28rpx;
		color: #333;
		background: transparent;
	}
	
	.search-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		font-size: 28rpx;
		padding: 20rpx 40rpx;
		border: none;
		min-width: 120rpx;
		transition: all 0.3s ease;
	}
	
	.search-btn:disabled {
		background-color: #ccc;
		opacity: 0.7;
	}
	
	.search-btn:active {
		transform: scale(0.95);
	}
	
	/* AI回答区域 */
	.ai-response-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	.response-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 20rpx;
		padding-bottom: 20rpx;
		border-bottom: 1rpx solid #f0f0f0;
	}
	
	.ai-icon {
		width: 36rpx;
		height: 36rpx;
		margin-right: 10rpx;
	}
	
	.response-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #409EFF;
		flex: 1;
	}
	
	.response-actions {
		display: flex;
		gap: 15rpx;
	}
	
	.copy-btn, .clear-btn {
		font-size: 24rpx;
		padding: 8rpx 16rpx;
		border-radius: 6rpx;
		border: none;
		display: flex;
		align-items: center;
	}
	
	.copy-btn {
		background-color: #e8f4ff;
		color: #409EFF;
	}
	
	.clear-btn {
		background-color: #ffe6e6;
		color: #ff6b6b;
	}
	
	.copy-icon, .clear-icon {
		width: 20rpx;
		height: 20rpx;
		margin-right: 6rpx;
	}
	
	.response-content {
		min-height: 100rpx;
	}
	
	/* 加载动画 */
	.loading-container {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 40rpx 0;
	}
	
	.loading-dots {
		display: flex;
		gap: 10rpx;
		margin-bottom: 20rpx;
	}
	
	.dot {
		width: 20rpx;
		height: 20rpx;
		background-color: #409EFF;
		border-radius: 50%;
		animation: dot-pulse 1.5s infinite ease-in-out;
	}
	
	.dot:nth-child(2) {
		animation-delay: 0.2s;
	}
	
	.dot:nth-child(3) {
		animation-delay: 0.4s;
	}
	
	@keyframes dot-pulse {
		0%, 60%, 100% {
			transform: translateY(0);
			opacity: 0.6;
		}
		30% {
			transform: translateY(-20rpx);
			opacity: 1;
		}
	}
	
	.loading-text {
		font-size: 26rpx;
		color: #666;
	}
	
	/* AI回答内容 */
	.ai-content {
		background-color: #f8f9fa;
		border-radius: 12rpx;
		padding: 25rpx;
		margin-bottom: 20rpx;
	}
	
	.ai-text {
		font-size: 28rpx;
		color: #333;
		line-height: 1.6;
		word-break: break-word;
	}
	
	/* 思考链 */
	.thinking-section {
		background-color: #f0f7ff;
		border-radius: 12rpx;
		padding: 20rpx;
		margin-top: 20rpx;
		border-left: 4rpx solid #409EFF;
	}
	
	.thinking-title {
		font-size: 26rpx;
		font-weight: bold;
		color: #409EFF;
		display: block;
		margin-bottom: 10rpx;
	}
	
	.thinking-content {
		font-size: 24rpx;
		color: #666;
		line-height: 1.5;
		font-style: italic;
	}
	
	/* 回答信息 */
	.response-info {
		display: flex;
		justify-content: space-between;
		margin-top: 20rpx;
		padding-top: 15rpx;
		border-top: 1rpx solid #f0f0f0;
	}
	
	.info-text {
		font-size: 22rpx;
		color: #999;
	}
	
	/* 历史记录 */
	.history-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	.history-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 20rpx;
	}
	
	.history-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #333;
	}
	
	.clear-history-btn {
		font-size: 24rpx;
		color: #999;
		padding: 8rpx 16rpx;
		border: 1rpx solid #ddd;
		border-radius: 6rpx;
		background: transparent;
	}
	
	.history-list {
		display: flex;
		flex-direction: column;
		gap: 15rpx;
	}
	
	.history-item {
		padding: 20rpx;
		background-color: #f8f8f8;
		border-radius: 12rpx;
		display: flex;
		justify-content: space-between;
		align-items: center;
		transition: background-color 0.3s;
	}
	
	.history-item:active {
		background-color: #e8f4ff;
	}
	
	.history-question {
		font-size: 26rpx;
		color: #333;
		flex: 1;
		margin-right: 15rpx;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	
	.history-time {
		font-size: 22rpx;
		color: #999;
		flex-shrink: 0;
	}
	
	/* 使用说明 */
	.instructions-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	.instruction-card {
		display: flex;
		background-color: #f0f7ff;
		border-radius: 12rpx;
		padding: 25rpx;
		margin-bottom: 30rpx;
	}
	
	.instruction-icon {
		width: 40rpx;
		height: 40rpx;
		margin-right: 20rpx;
		flex-shrink: 0;
	}
	
	.instruction-content {
		flex: 1;
	}
	
	.instruction-title {
		font-size: 28rpx;
		font-weight: bold;
		color: #409EFF;
		display: block;
		margin-bottom: 15rpx;
	}
	
	.instruction-list {
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	
	.instruction-item {
		font-size: 24rpx;
		color: #666;
		line-height: 1.4;
	}
	
	/* 示例部分 */
	.example-section {
		margin-top: 30rpx;
	}
	
	.example-title {
		font-size: 28rpx;
		font-weight: bold;
		color: #333;
		display: block;
		margin-bottom: 20rpx;
	}
	
	.example-list {
		display: flex;
		flex-direction: column;
		gap: 15rpx;
	}
	
	.example-item {
		padding: 20rpx;
		background-color: #f8f8f8;
		border-radius: 12rpx;
		display: flex;
		justify-content: space-between;
		align-items: center;
		transition: all 0.3s;
	}
	
	.example-item:active {
		background-color: #e8f4ff;
		transform: translateX(10rpx);
	}
	
	.example-text {
		font-size: 26rpx;
		color: #333;
		flex: 1;
	}
	
	.example-arrow {
		width: 24rpx;
		height: 24rpx;
	}
	
	/* 错误提示 */
	.error-section {
		background-color: #ffe6e6;
		border-radius: 16rpx;
		padding: 25rpx;
		margin-bottom: 30rpx;
	}
	
	.error-content {
		display: flex;
		align-items: center;
		margin-bottom: 20rpx;
	}
	
	.error-icon {
		width: 32rpx;
		height: 32rpx;
		margin-right: 15rpx;
	}
	
	.error-text {
		font-size: 26rpx;
		color: #ff6b6b;
		flex: 1;
	}
	
	.retry-btn {
		background-color: #ff6b6b;
		color: #fff;
		border-radius: 8rpx;
		font-size: 26rpx;
		padding: 15rpx;
		border: none;
		width: 100%;
	}
	
	/* 底部信息 */
	.footer-section {
		text-align: center;
		padding: 30rpx 0;
	}
	
	.footer-text {
		font-size: 24rpx;
		color: #999;
		display: block;
		margin-bottom: 10rpx;
	}
	
	.footer-text:last-child {
		margin-bottom: 0;
	}

	/* ========== 科技风格 ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
	}
	.container.theme-ark .search-section,
	.container.theme-ark .result-section,
	.container.theme-ark .history-section,
	.container.theme-ark .instruction-card {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border: 1rpx solid #2A2A4A;
	}
	.container.theme-ark .search-input-container { background-color: #16162A; border-color: #2A2A4A; }
	.container.theme-ark .search-input-container.focus { border-color: #FF6B35; }
	.container.theme-ark .search-input { color: #e0e0e0; }
	.container.theme-ark .search-icon { filter: brightness(0) invert(1); opacity: 0.7; }
	.container.theme-ark .ai-response { color: #e0e0e0; }
	.container.theme-ark .response-info { color: #888; }
	.container.theme-ark .send-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .history-item { border-bottom-color: #2A2A4A; }
	.container.theme-ark .history-question { color: #e0e0e0; }
	.container.theme-ark .history-time { color: #888; }
	.container.theme-ark .example-tag { background: rgba(255,107,53,0.15); color: #FF6B35; border-color: rgba(255,107,53,0.3); }
	.container.theme-ark .example-item { color: #FF6B35; }
	.container.theme-ark .thinking-text { color: #888; }
	.container.theme-ark .examples-grid { background: transparent; }
	.container.theme-ark .example-text { color: #888; }
	.container.theme-ark .data-update-info { color: #888; }
	.container.theme-ark .error-message { color: #FF6B35; }
	.container.theme-ark .header-text { color: #e0e0e0; }
	.container.theme-ark .footer-text { color: #888; }

	/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .search-section,
	.container.theme-jieyuan .result-section,
	.container.theme-jieyuan .history-section,
	.container.theme-jieyuan .instruction-card {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .search-input-container { background: rgba(0,0,0,0.05); border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .search-input-container.focus { border-color: #e25884; box-shadow: 0 0 10rpx rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .search-input { color: #333; }
	.container.theme-jieyuan .ai-response { color: #333; }
	.container.theme-jieyuan .response-info { color: #888; }
	.container.theme-jieyuan .send-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .history-item { border-bottom-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .history-question { color: #333; }
	.container.theme-jieyuan .history-time { color: #888; }
	.container.theme-jieyuan .example-tag { background: rgba(226, 88, 132, 0.1); color: #e25884; border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .example-item { color: #399383; }
	.container.theme-jieyuan .thinking-text { color: #888; }
</style>