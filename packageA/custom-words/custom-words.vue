<template>
	<view :class="['container', 'theme-' + themeMode]">
		<!-- 页面标题 -->
		<view class="header">
			<!-- <text class="title">自定义搜索词管理</text> -->
			<text class="subtitle">为干员添加个人常用的别名和搜索词</text>
		</view>

		<!-- 搜索框 -->
		<view class="search-section">
			<view class="search-input-container">
				<image class="search-icon" src="/static/search-icon.png"></image>
				<input 
					class="search-input" 
					v-model="searchText" 
					placeholder="搜索干员名称"
					@input="onSearchInput"
				/>
				<text class="clear-search" @click="clearSearch" v-if="searchText">×</text>
			</view>
		</view>

		<!-- 统计信息 -->
		<view class="stats-section">
			<text class="stats-text">共 {{ filteredCharacters.length }} 个干员</text>
			<text class="stats-text">已为 {{ customWordsCount }} 个干员添加自定义搜索词</text>
		</view>

		<!-- 干员列表 -->
		<view class="character-list">
			<view 
				class="character-item" 
				v-for="(character, index) in paginatedCharacters" 
				:key="character.name"
				:class="{ expanded: expandedIndex === index }"
			>
				<view class="character-header" @click="toggleExpand(index)">
					<view class="character-info">
						<text class="character-name">{{ character.name }}</text>
						<text class="character-english" v-if="character.englishname">{{ character.englishname }}</text>
					</view>
					<view class="character-actions">
						<text class="custom-count" v-if="getCustomWordCount(character.name) > 0">
							{{ getCustomWordCount(character.name) }}个自定义
						</text>
						<text class="expand-icon">{{ expandedIndex === index ? '−' : '+' }}</text>
					</view>
				</view>

				<view class="character-details" v-if="expandedIndex === index">
					<!-- 固定外号 -->
					<view class="word-section">
						<text class="section-title">固定外号</text>
						<view class="word-list">
							<view 
								class="word-tag fixed" 
								v-for="word in character.searchword" 
								:key="word"
							>
								<text class="word-text">{{ word }}</text>
							</view>
							<text class="no-words" v-if="!character.searchword || character.searchword.length === 0">
								暂无固定外号
							</text>
						</view>
					</view>

					<!-- 自定义外号 -->
					<view class="word-section">
						<view class="section-header">
							<text class="section-title">自定义搜索词</text>
							<text class="section-desc">最多添加5个自定义搜索词</text>
						</view>
						<view class="word-list">
							<view 
								class="word-tag custom" 
								v-for="word in getCustomWords(character.name)" 
								:key="word"
							>
								<text class="word-text">{{ word }}</text>
								<text class="delete-icon" @click="removeCustomWord(character.name, word)">×</text>
							</view>
							<text class="no-words" v-if="getCustomWordCount(character.name) === 0">
								暂无自定义搜索词
							</text>
						</view>

						<!-- 添加自定义搜索词 -->
						<view class="add-word-section" v-if="getCustomWordCount(character.name) < 5">
							<input 
								class="add-word-input" 
								v-model="newWords[character.name]" 
								:placeholder="`为${character.name}添加搜索词`"
								maxlength="10"
								@confirm="addCustomWord(character.name)"
							/>
							<button 
								class="add-word-btn" 
								@click="addCustomWord(character.name)"
								:disabled="!newWords[character.name] || newWords[character.name].trim() === ''"
							>
								添加
							</button>
						</view>
						<text class="max-tip" v-else>已达到最多5个自定义搜索词</text>
					</view>
				</view>
			</view>
		</view>

		<!-- 分页控件 -->
		<view class="pagination" v-if="totalPages > 1">
		<button
			class="page-btn" 
			:disabled="currentPage <= 1"
			@click="goToFirstPage"
		>
			首页
		</button>
			<button 
				class="page-btn" 
				:disabled="currentPage === 1" 
				@click="prevPage"
			>
				上一页
			</button>
			<text class="page-info">第 {{ currentPage }} 页 / 共 {{ totalPages }} 页</text>
			<button 
				class="page-btn" 
				:disabled="currentPage === totalPages" 
				@click="nextPage"
			>
				下一页
			</button>
			<button
				class="page-btn" 
				:disabled="currentPage >= totalPages"
				@click="goToLastPage"
			>
				尾页
			</button>
		</view>

		<!-- 空状态 -->
		<view class="empty-state" v-if="filteredCharacters.length === 0">
			<image class="empty-icon" src="/static/empty-icon.png"></image>
			<text class="empty-text">未找到相关干员</text>
			<text class="empty-tip" v-if="searchText">尝试调整搜索词</text>
		</view>

		<!-- 操作提示 -->
		<view class="tip-section">
			<text class="tip-text">• 固定外号来自问卷征集，无法修改或删除</text>
			<text class="tip-text">• 每个干员最多可添加5个自定义搜索词</text>
			<text class="tip-text">• 自定义搜索词将在"外号搜索"启用时生效</text>
		</view>
	</view>
</template>

<script>
	export default {
		data() {
			return {
				allCharacters: [], // 所有干员
				filteredCharacters: [], // 过滤后的干员
				customWords: [], // 自定义搜索词
				searchText: '', // 搜索文本
				currentPage: 1, // 当前页码
				pageSize: 10, // 每页数量
				expandedIndex: -1, // 当前展开的干员索引
				newWords: {}, // 新增搜索词输入框内容
				// 主题模式
				themeMode: 'simple'
			}
		},
		computed: {
			// 总页数
			totalPages() {
				return Math.ceil(this.filteredCharacters.length / this.pageSize);
			},
			// 当前页的干员
			paginatedCharacters() {
				const start = (this.currentPage - 1) * this.pageSize;
				const end = start + this.pageSize;
				return this.filteredCharacters.slice(start, end);
			},
			// 自定义搜索词数量
			customWordsCount() {
				return this.customWords.length;
			}
		},
		onLoad() {
			this.loadThemeSetting();
			this.loadData();

			// 设置分享配置
			    wx.showShareMenu({
			      withShareTicket: true,
			      menus: ['shareAppMessage', 'shareTimeline']
			    });
			  },
			  
			  onShareAppMessage() {
			    return {
			      title: '方舟通行证谷子查询工具-自定义搜索词', 
			      path: '/packageA/custom-words/custom-words', // 分享路径
			      imageUrl: '' // 如果留空，会自动使用小程序截图
					}
				},
				
				onShareTimeline() {
				  return {
				    title: '方舟通行证谷子查询工具-关于我们', 
				    imageUrl: '' // 如果留空，会自动使用小程序截图
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

			// 加载数据
			loadData() {
				try {
					// 加载搜索词数据
					const searchWords = uni.getStorageSync('searchWords');
					if (searchWords && Array.isArray(searchWords)) {
						this.allCharacters = searchWords;
						this.filteredCharacters = [...searchWords];
					}

					// 加载自定义搜索词
					const customWords = uni.getStorageSync('customSearchWords');
					if (customWords && Array.isArray(customWords)) {
						this.customWords = customWords;
					} else {
						this.customWords = [];
						uni.setStorageSync('customSearchWords', []);
					}

					console.log(`加载数据: ${this.allCharacters.length} 个干员, ${this.customWords.length} 个自定义词`);
				} catch (e) {
					console.error('加载数据失败:', e);
					uni.showToast({
						title: '加载数据失败',
						icon: 'error'
					});
				}
			},

			// 搜索输入处理
			onSearchInput() {
				this.currentPage = 1;
				this.expandedIndex = -1;
				
				if (!this.searchText.trim()) {
					this.filteredCharacters = [...this.allCharacters];
					return;
				}

				const searchTerm = this.searchText.trim().toLowerCase();
				this.filteredCharacters = this.allCharacters.filter(character => {
					return character.name.toLowerCase().includes(searchTerm) ||
						   (character.englishname && character.englishname.toLowerCase().includes(searchTerm)) ||
						   this.checkCustomWordsMatch(character.name, searchTerm);
				});
			},

			// 检查自定义搜索词匹配
			checkCustomWordsMatch(characterName, searchTerm) {
				const customWord = this.customWords.find(item => item.characterName === characterName);
				if (customWord && customWord.words) {
					return customWord.words.some(word => word.toLowerCase().includes(searchTerm));
				}
				return false;
			},

			// 清空搜索
			clearSearch() {
				this.searchText = '';
				this.filteredCharacters = [...this.allCharacters];
				this.currentPage = 1;
			},

			// 切换展开状态
			toggleExpand(index) {
				if (this.expandedIndex === index) {
					this.expandedIndex = -1;
				} else {
					this.expandedIndex = index;
				}
			},

			// 获取干员的自定义搜索词
			getCustomWords(characterName) {
				const customWord = this.customWords.find(item => item.characterName === characterName);
				return customWord ? customWord.words : [];
			},

			// 获取干员的自定义搜索词数量
			getCustomWordCount(characterName) {
				return this.getCustomWords(characterName).length;
			},

			// 添加自定义搜索词
			addCustomWord(characterName) {
				const word = this.newWords[characterName] ? this.newWords[characterName].trim() : '';
				
				if (!word) {
					uni.showToast({
						title: '请输入搜索词',
						icon: 'none'
					});
					return;
				}

				if (word.length > 10) {
					uni.showToast({
						title: '搜索词不能超过10个字符',
						icon: 'none'
					});
					return;
				}

				// 检查是否已存在（包括固定外号）
				const character = this.allCharacters.find(c => c.name === characterName);
				const existingWords = [
					...(character.searchword || []),
					...this.getCustomWords(characterName)
				];

				if (existingWords.some(existing => existing.toLowerCase() === word.toLowerCase())) {
					uni.showToast({
						title: '该搜索词已存在',
						icon: 'none'
					});
					return;
				}

				// 添加自定义搜索词
				let customWord = this.customWords.find(item => item.characterName === characterName);
				
				if (customWord) {
					// 更新现有条目
					if (customWord.words.length >= 5) {
						uni.showToast({
							title: '最多添加5个自定义搜索词',
							icon: 'none'
						});
						return;
					}
					customWord.words.push(word);
				} else {
					// 创建新条目
					customWord = {
						characterName: characterName,
						words: [word]
					};
					this.customWords.push(customWord);
				}

				// 保存到本地存储
				uni.setStorageSync('customSearchWords', this.customWords);

				// 清空输入框
				this.$set(this.newWords, characterName, '');

				uni.showToast({
					title: '添加成功',
					icon: 'success'
				});

				console.log(`为干员 ${characterName} 添加搜索词: ${word}`);
			},

			// 删除自定义搜索词
			removeCustomWord(characterName, word) {
				uni.showModal({
					title: '确认删除',
					content: `确定要删除"${word}"吗？`,
					confirmText: '删除',
					confirmColor: '#FA5151',
					success: (res) => {
						if (res.confirm) {
							const customWord = this.customWords.find(item => item.characterName === characterName);
							if (customWord) {
								// 删除搜索词
								const index = customWord.words.indexOf(word);
								if (index > -1) {
									customWord.words.splice(index, 1);
								}

								// 如果该干员没有自定义搜索词了，删除整个条目
								if (customWord.words.length === 0) {
									const wordIndex = this.customWords.indexOf(customWord);
									if (wordIndex > -1) {
										this.customWords.splice(wordIndex, 1);
									}
								}

								// 保存到本地存储
								uni.setStorageSync('customSearchWords', this.customWords);

								uni.showToast({
									title: '删除成功',
									icon: 'success'
								});

								console.log(`删除干员 ${characterName} 的搜索词: ${word}`);
							}
						}
					}
				});
			},

			// 上一页
			prevPage() {
				if (this.currentPage > 1) {
					this.currentPage--;
					this.expandedIndex = -1;
				}
			},

			// 下一页
			nextPage() {
				if (this.currentPage < this.totalPages) {
					this.currentPage++;
					this.expandedIndex = -1;
				}
			},
			// 首页
			goToFirstPage() {
				this.currentPage = 1;
			},
			
			// 尾页
			goToLastPage() {
				this.currentPage = this.totalPages;
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

	.header {
		text-align: center;
		margin-bottom: 30rpx;
	}

	.title {
		font-size: 36rpx;
		font-weight: bold;
		color: #333;
		display: block;
		margin-bottom: 10rpx;
	}

	.subtitle {
		font-size: 26rpx;
		color: #666;
	}

	/* 搜索区域 */
	.search-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 20rpx;
		margin-bottom: 20rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.search-input-container {
		display: flex;
		align-items: center;
		background-color: #f8f8f8;
		border-radius: 50rpx;
		padding: 20rpx 30rpx;
		position: relative;
	}

	.search-icon {
		width: 32rpx;
		height: 32rpx;
		margin-right: 20rpx;
	}

	.search-input {
		flex: 1;
		font-size: 28rpx;
	}

	.clear-search {
		font-size: 36rpx;
		color: #999;
		padding: 10rpx;
	}

	/* 统计信息 */
	.stats-section {
		display: flex;
		justify-content: space-between;
		margin-bottom: 20rpx;
		padding: 0 10rpx;
	}

	.stats-text {
		font-size: 24rpx;
		color: #666;
	}

	/* 干员列表 */
	.character-list {
		margin-bottom: 30rpx;
	}

	.character-item {
		background-color: #fff;
		border-radius: 16rpx;
		margin-bottom: 20rpx;
		overflow: hidden;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
		transition: all 0.3s ease;
	}

	.character-item.expanded {
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.1);
	}

	.character-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 25rpx 30rpx;
		border-bottom: 1rpx solid transparent;
	}

	.character-item.expanded .character-header {
		border-bottom-color: #f0f0f0;
	}

	.character-info {
		flex: 1;
	}

	.character-name {
		font-size: 30rpx;
		color: #333;
		font-weight: 500;
		display: block;
		margin-bottom: 5rpx;
	}

	.character-english {
		font-size: 24rpx;
		color: #666;
	}

	.character-actions {
		display: flex;
		align-items: center;
		gap: 15rpx;
	}

	.custom-count {
		font-size: 22rpx;
		color: #409EFF;
		background-color: #e8f4ff;
		padding: 6rpx 12rpx;
		border-radius: 15rpx;
	}

	.expand-icon {
		font-size: 28rpx;
		color: #999;
		font-weight: bold;
		width: 40rpx;
		height: 40rpx;
		text-align: center;
		line-height: 40rpx;
	}

	/* 干员详情 */
	.character-details {
		padding: 0 30rpx 25rpx;
	}

	.word-section {
		margin-top: 25rpx;
	}

	.section-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 15rpx;
	}

	.section-title {
		font-size: 26rpx;
		color: #333;
		font-weight: 500;
	}

	.section-desc {
		font-size: 22rpx;
		color: #999;
	}

	.word-list {
		display: flex;
		flex-wrap: wrap;
		gap: 15rpx;
		margin-bottom: 20rpx;
	}

	.word-tag {
		display: flex;
		align-items: center;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
		font-size: 24rpx;
	}

	.word-tag.fixed {
		background-color: #f0f0f0;
		color: #666;
	}

	.word-tag.custom {
		background-color: #e8f4ff;
		color: #409EFF;
	}

	.word-text {
		margin-right: 10rpx;
	}

	.delete-icon {
		font-size: 28rpx;
		font-weight: bold;
		color: #F56C6C;
		padding: 5rpx;
	}

	.no-words {
		font-size: 24rpx;
		color: #999;
		font-style: italic;
	}

	/* 添加搜索词区域 */
	.add-word-section {
		display: flex;
		align-items: center;
		gap: 15rpx;
		margin-top: 15rpx;
	}

	.add-word-input {
		flex: 1;
		background-color: #f8f8f8;
		border-radius: 8rpx;
		padding: 20rpx;
		font-size: 26rpx;
	}

	.add-word-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 8rpx;
		font-size: 26rpx;
		padding: 20rpx 30rpx;
	}

	.add-word-btn:disabled {
		background-color: #c0c4cc;
		color: #fff;
	}

	.max-tip {
		font-size: 24rpx;
		color: #E6A23C;
		margin-top: 10rpx;
		display: block;
	}

	/* 分页控件 */
	.pagination {
		display: flex;
		align-items: center;
		justify-content: space-between;
		background-color: #fff;
		border-radius: 16rpx;
		padding: 20rpx 20rpx;
		margin-bottom: 30rpx;
		box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
	}

	.page-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 8rpx;
		font-size: 26rpx;
		padding: 15rpx 25rpx;
	}

	.page-btn:disabled {
		background-color: #c0c4cc;
		color: #fff;
	}

	.page-info {
		font-size: 26rpx;
		color: #666;
	}

	/* 空状态 */
	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 100rpx 0;
		background-color: #fff;
		border-radius: 16rpx;
		margin-bottom: 30rpx;
	}

	.empty-icon {
		width: 120rpx;
		height: 120rpx;
		margin-bottom: 30rpx;
		opacity: 0.5;
	}

	.empty-text {
		font-size: 28rpx;
		color: #999;
		margin-bottom: 15rpx;
	}

	.empty-tip {
		font-size: 24rpx;
		color: #ccc;
	}

	/* 提示区域 */
	.tip-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 25rpx 30rpx;
	}

	.tip-text {
		font-size: 24rpx;
		color: #666;
		display: block;
		margin-bottom: 10rpx;
		line-height: 1.5;
	}

	.tip-text:last-child {
		margin-bottom: 0;
	}

	/* ========== 科技风格 ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
	}
	.container.theme-ark .header,
	.container.theme-ark .search-section,
	.container.theme-ark .stats-section,
	.container.theme-ark .character-list,
	.container.theme-ark .character-card,
	.container.theme-ark .empty-state,
	.container.theme-ark .tip-section {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border: 1rpx solid #2A2A4A;
	}
	.container.theme-ark .subtitle { color: #e0e0e0; }
	.container.theme-ark .search-input-container { background-color: #16162A; border-color: #2A2A4A; }
	.container.theme-ark .search-input { color: #e0e0e0; }
	.container.theme-ark .search-icon { filter: brightness(0) invert(1); opacity: 0.7; }
	.container.theme-ark .clear-search { color: #888; }
	.container.theme-ark .stats-text { color: #FF6B35; }
	.container.theme-ark .character-name { color: #e0e0e0; }
	.container.theme-ark .words-tags { color: #e0e0e0; }
	.container.theme-ark .fixed-tag { background: rgba(255,107,53,0.15); color: #FF6B35; }
	.container.theme-ark .word-tag { background: rgba(255,107,53,0.1); color: #FF6B35; border-color: rgba(255,107,53,0.3); }
	.container.theme-ark .empty-text { color: #888; }
	.container.theme-ark .tip-text { color: #888; }
	.container.theme-ark .pagination { background: rgba(22,22,42,0.8); }
	.container.theme-ark .page-btn { background: #16162A; color: #e0e0e0; border-color: #2A2A4A; }
	.container.theme-ark .page-btn:active { background: #FF6B35; color: #fff; }
	.container.theme-ark .add-word-input { background-color: #16162A; color: #e0e0e0; border-color: #2A2A4A; }
	.container.theme-ark .add-word-section { background: rgba(22,22,42,0.5); border-color: #2A2A4A; }

	/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .header,
	.container.theme-jieyuan .search-section,
	.container.theme-jieyuan .stats-section,
	.container.theme-jieyuan .character-list,
	.container.theme-jieyuan .character-card,
	.container.theme-jieyuan .empty-state,
	.container.theme-jieyuan .tip-section {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .subtitle { color: #333; }
	.container.theme-jieyuan .search-input-container { background: rgba(0,0,0,0.05); border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .search-input { color: #333; }
	.container.theme-jieyuan .stats-text { color: #399383; }
	.container.theme-jieyuan .character-name { color: #333; }
	.container.theme-jieyuan .words-tags { color: #333; }
	.container.theme-jieyuan .fixed-tag { background: rgba(57,147,131,0.15); color: #399383; }
	.container.theme-jieyuan .word-tag { background: rgba(226,88,132,0.1); color: #e25884; border-color: rgba(226,88,132,0.2); }
	.container.theme-jieyuan .empty-text { color: #888; }
	.container.theme-jieyuan .tip-text { color: #888; }
	.container.theme-jieyuan .pagination { background: rgba(255, 255, 255, 0.9); }
	.container.theme-jieyuan .page-btn { background: rgba(0,0,0,0.05); color: #333; border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .page-btn:active { background: linear-gradient(90deg, #e25884, #399383); color: #fff; }
</style>
