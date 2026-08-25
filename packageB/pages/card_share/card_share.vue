<template>
	<view class="container">
		<!-- 模式选择和功能区域 -->
		<view class="mode-section">
			<view class="mode-tabs">
				<view 
					class="mode-tab" 
					:class="{ active: currentMode === 'show' }"
					@click="changeMode('show')"
				>
					<text class="mode-icon">🌟</text>
					<text class="mode-text">炫耀模式</text>
				</view>
				<view 
					class="mode-tab" 
					:class="{ active: currentMode === 'trade' }"
					@click="changeMode('trade')"
				>
					<text class="mode-icon">💼</text>
					<text class="mode-text">出物模式</text>
				</view>
			</view>
			
			<!-- 功能按钮 -->
			<view class="action-buttons">
				<button 
					class="action-btn select-all-btn"
					@click="toggleSelectAll"
				>
					{{ isAllSelected ? '取消全选' : '全选点亮' }}
				</button>
				<button 
					class="action-btn share-btn"
					@click="openSharePreview"
					:disabled="isGeneratingShareImage"
				>
					{{ isGeneratingShareImage ? '生成中...' : '生成分享图' }}
				</button>
				<button 
					class="action-btn want-btn"
					@click="toggleWantMode"
					:class="{ active: wantMode }"
				>
					{{ wantMode ? '取消想要' : '标记想要的' }}
				</button>
			</view>
		</view>
		
		<!-- 筛选和统计 -->
		<view class="filter-stats-section">
			<picker 
				class="box-filter-picker" 
				@change="onBoxFilterChange" 
				:value="boxFilterIndex" 
				:range="boxFilterOptions"
			>
				<view class="picker-text">{{ boxFilterOptions[boxFilterIndex] }}</view>
			</picker>
			
			<view class="stats-info">
				<text class="stat-item">点亮: {{ ownedCount }}/{{ totalCharacters }}</text>
				<text class="stat-item">想要: {{ wantedCount }}</text>
				<text class="stat-item">出物: {{ tradeCount }}</text>
			</view>
		</view>
		
		<!-- 精英化级别选择 -->
		<view class="elite-level-selector">
			<view 
				class="elite-option" 
				:class="{ active: eliteLevel === 'all' }"
				@click="setEliteLevel('all')"
			>
				<text>全部</text>
			</view>
			<view 
				class="elite-option" 
				:class="{ active: eliteLevel === 'elite1' }"
				@click="setEliteLevel('elite1')"
			>
				<text class="elite-tag elite1-tag">仅精一</text>
			</view>
			<view 
				class="elite-option" 
				:class="{ active: eliteLevel === 'elite2' }"
				@click="setEliteLevel('elite2')"
			>
				<text class="elite-tag elite2-tag">精二</text>
			</view>
		</view>
		
		<!-- 角色盒子列表 -->
		<scroll-view 
			class="boxes-scroll-view" 
			scroll-y 
			@scrolltolower="loadMore"
			:scroll-top="scrollTop"
		>
			<view 
				class="box-section" 
				v-for="(box, index) in displayBoxes" 
				:key="box?.Box_id || index"
			>
				<view class="box-header">
					<text class="box-title">盒号 {{ box?.Box_id || '未知' }}</text>
					<text class="box-type-tag" :class="getBoxTypeClass(box?.Box_type)">
						{{ getBoxTypeText(box?.Box_type) }}
					</text>
					<button 
						class="select-box-btn"
						@click="toggleSelectBox(box?.Box_id)"
					>
						{{ isBoxAllSelected(box) ? '取消全选' : '全选点亮' }}
					</button>
				</view>
				
				<view class="characters-grid">
					<view 
						class="character-item" 
						v-for="(character, charIndex) in getFilteredCharacters(box)" 
						:key="charIndex"
						@click="toggleCharacterOwnership(character, box?.Box_id)"
						@longpress="toggleCharacterWant(character?.name)"
					>
						<view class="character-avatar-wrapper">
							<image 
								class="character-avatar" 
								:class="{ 
									'owned': isCharacterOwned(character?.name),
									'grayed': !isCharacterOwned(character?.name),
									'wanted': isCharacterWanted(character?.name),
									'for-trade': isCharacterForTrade(character?.name)
								}"
								:src="character?.avatar || defaultAvatar" 
								mode="aspectFit"
								@error="onAvatarError(character)"
							></image>
							
							<view class="status-indicators">
								<view 
									class="owned-indicator elite1-indicator" 
									v-if="isCharacterOwnedAtLevel(character?.name, 'elite1')"
								>
									Ⅰ
								</view>
								<view 
									class="owned-indicator elite2-indicator" 
									v-if="isCharacterOwnedAtLevel(character?.name, 'elite2') && !character?.nolyELITE1"
								>
									Ⅱ
								</view>
								
								<view 
									class="want-indicator" 
									v-if="isCharacterWanted(character?.name)"
								>
									❤️
								</view>
								
								<view 
									class="trade-indicator" 
									v-if="isCharacterForTrade(character?.name)"
								>
									💼
								</view>
								
								<view 
									class="hot-indicator" 
									v-if="character?.hotcharacter"
								>
									🔥
								</view>
							</view>
							
							<view class="price-tags" v-if="character?.market_price && currentMode === 'show'">
								<text 
									class="price-tag elite1-tag" 
									v-if="character.market_price.ELITE1"
								>
									{{ character.market_price.ELITE1 }}
								</text>
								<text 
									class="price-tag elite2-tag" 
									v-if="character.market_price.ELITE2 && !character.nolyELITE1"
								>
									{{ character.market_price.ELITE2 }}
								</text>
							</view>
							
							<view class="trade-price-inputs" v-if="currentMode === 'trade' && isCharacterOwned(character?.name)">
								<view class="price-input-row">
									<text v-if="!character?.nolyELITE1" class="price-label">精一:</text>
									<input 
										v-if="!character?.nolyELITE1"
										class="price-input" 
										type="number" 
										:placeholder="isCharacterOwnedAtLevel(character?.name, 'elite1') ? '出价' : '-'"
										:disabled="!isCharacterOwnedAtLevel(character?.name, 'elite1')"
										:value="getTradePrice(character?.name, 'elite1')"
										@input="(e) => setTradePrice(character?.name, 'elite1', e.detail.value)"
									/>
									<text v-if="!character?.nolyELITE1" class="price-unit">元</text>
								</view>
								<view class="price-input-row">
									<text v-if="!character?.nolyELITE1" class="price-label">精二:</text>
									<input 
										v-if="!character?.nolyELITE1"
										class="price-input" 
										type="number" 
										:placeholder="isCharacterOwnedAtLevel(character?.name, 'elite2') ? '出价' : '-'"
										:disabled="!isCharacterOwnedAtLevel(character?.name, 'elite2')"
										:value="getTradePrice(character?.name, 'elite2')"
										@input="(e) => setTradePrice(character?.name, 'elite2', e.detail.value)"
									/>
									<text v-if="!character?.nolyELITE1" class="price-unit">元</text>
									<text v-if="character?.nolyELITE1" class="only-elite1-tip">仅精一</text>
								</view>
							</view>
						</view>
						
						<text class="character-name">{{ character?.name || '未知角色' }}</text>
					</view>
				</view>
			</view>
			
			<view class="load-more" v-if="hasMore && filteredBoxes.length > 0">
				<text>加载更多...</text>
			</view>
			
			<view class="empty-state" v-if="displayBoxes.length === 0">
				<image class="empty-icon" src="/static/empty-icon.png"></image>
				<text class="empty-text">暂无数据或没有符合条件的角色</text>
				<button class="sync-btn" @click="syncWithFavorites">
					✨ 从收藏同步
				</button>
			</view>
		</scroll-view>
		
		<!-- 分享预览模态框 -->
		<view class="share-preview-modal" v-if="showSharePreviewModal" @tap="closeSharePreviewModal">
			<view class="share-preview-content" @tap.stop>
				<view class="share-preview-header">
					<text class="share-preview-title">分享图预览</text>
					<button class="share-close-btn" @click="closeSharePreviewModal">×</button>
				</view>
				
				<view class="share-preview-body">
					<!-- 分享内容预览 -->
					<view class="share-content-preview" id="shareContent">
						<view class="share-header">
							<text class="share-title">
								{{ currentMode === 'show' ? '🌟 我的明日方舟收藏 🌟' : '💼 出物清单 💼' }}
							</text>
							<text class="share-subtitle">
								{{ currentMode === 'show' ? '炫耀模式 - 展示我的所有收藏' : '出物模式 - 这些角色可以交易' }}
							</text>
						</view>
						
						<view class="share-stats">
							<view class="stat-row">
								<text class="stat-label">已点亮角色:</text>
								<text class="stat-value">{{ ownedCount }}个</text>
							</view>
							
							<view class="stat-row" v-if="currentMode === 'trade' && tradeCount > 0">
								<text class="stat-label">出物角色:</text>
								<text class="stat-value">{{ tradeCount }}个</text>
							</view>
							
							<view class="stat-row" v-if="wantedCount > 0">
								<text class="stat-label">想要的角色:</text>
								<text class="stat-value wanted-stat">{{ wantedCount }}个</text>
							</view>
							
							<view class="stat-row">
								<text class="stat-label">生成时间:</text>
								<text class="stat-value">{{ shareGeneratedTime }}</text>
							</view>
						</view>
						
						<view class="share-divider"></view>
						
						<view class="share-characters-section">
							<text class="section-title">角色列表</text>
							
							<view 
								class="share-box-section" 
								v-for="(box, index) in getShareBoxes()" 
								:key="box?.Box_id || index"
								v-if="box && getShareCharacters(box).length > 0"
							>
								<view class="share-box-header">
									<text class="share-box-title">盒号 {{ box?.Box_id || '未知' }}</text>
									<text class="share-box-type" :class="getBoxTypeClass(box?.Box_type)">
										{{ getBoxTypeText(box?.Box_type) }}
									</text>
								</view>
								
								<view class="share-characters-grid">
									<view 
										class="share-character-item" 
										v-for="(character, charIndex) in getShareCharacters(box)" 
										:key="charIndex"
									>
										<view class="share-character-content">
											<text class="share-character-name">{{ character?.name || '未知角色' }}</text>
											
											<view class="share-level-tags">
												<text 
													class="level-tag elite1-tag" 
													v-if="isCharacterOwnedAtLevel(character?.name, 'elite1')"
												>
													精一
												</text>
												<text 
													class="level-tag elite2-tag" 
													v-if="isCharacterOwnedAtLevel(character?.name, 'elite2') && !character?.nolyELITE1"
												>
													精二
												</text>
											</view>
											
											<view class="share-status-tags">
												<text 
													class="status-tag wanted-tag" 
													v-if="isCharacterWanted(character?.name)"
												>
													❤️ 想要
												</text>
												
												<view 
													class="price-info" 
													v-if="currentMode === 'trade' && isCharacterForTrade(character?.name)"
												>
													<text 
														class="price-tag" 
														v-if="getTradePrice(character?.name, 'elite1')"
													>
														精一: {{ getTradePrice(character?.name, 'elite1') }}元
													</text>
													<text 
														class="price-tag" 
														v-if="getTradePrice(character?.name, 'elite2') && !character?.nolyELITE1"
													>
														精二: {{ getTradePrice(character?.name, 'elite2') }}元
													</text>
												</view>
												
												<view 
													class="price-info" 
													v-if="currentMode === 'show' && character?.market_price"
												>
													<text 
														class="price-tag" 
														v-if="character.market_price.ELITE1"
													>
														市价: {{ character.market_price.ELITE1 }}元
													</text>
													<text 
														class="price-tag" 
														v-if="character.market_price.ELITE2 && !character?.nolyELITE1"
													>
														市价: {{ character.market_price.ELITE2 }}元
													</text>
												</view>
											</view>
										</view>
									</view>
								</view>
							</view>
							
							<view class="no-characters" v-if="getTotalShareCharacters() === 0">
								<text class="no-characters-text">
									{{ currentMode === 'show' ? '暂无点亮角色' : '暂无出物角色' }}
								</text>
							</view>
						</view>
						
						<view class="share-watermark" v-if="watermarkText">
							<text>{{ watermarkText }}</text>
						</view>
						
						<view class="share-footer">
							<text class="footer-text">方舟通行证谷子查询工具</text>
							<text class="footer-text">长按保存图片，分享给好友</text>
						</view>
					</view>
				</view>
				
				<!-- 操作按钮 -->
				<view class="share-preview-footer">
					<view class="share-options">
						<view class="option-item">
							<text class="option-label">显示价格:</text>
							<switch 
								:checked="shareShowPrices" 
								@change="shareShowPrices = $event.detail.value"
								color="#409EFF"
							/>
						</view>
						<view class="option-item" v-if="wantedCount > 0">
							<text class="option-label">显示想要:</text>
							<switch 
								:checked="shareShowWanted" 
								@change="shareShowWanted = $event.detail.value"
								color="#FF6B6B"
							/>
						</view>
					</view>
					
					<button 
						class="preview-action-btn capture-btn" 
						@click="generateShareImage"
						:disabled="isGeneratingShareImage"
					>
						{{ isGeneratingShareImage ? '生成中...' : '🖼️ 生成分享图' }}
					</button>
					
					<view class="action-buttons-row" v-if="shareImagePath">
						<image 
							:src="shareImagePath" 
							class="captured-image-preview"
							mode="widthFix"
							@click="previewShareImage"
						></image>
						<view class="share-buttons">
							<button class="preview-action-btn save-btn" @click="saveShareImage">
								💾 保存图片
							</button>
						</view>
					</view>
				</view>
			</view>
		</view>
		
		<!-- 隐藏的Canvas，用于生成图片 -->
		<canvas 
			canvas-id="shareCanvas" 
			style="position: absolute; left: -9999px; top: -9999px; width: 750rpx; height: 1334rpx;"
			id="shareCanvas"
		></canvas>
		
		<!-- 底部操作栏 -->
		<view class="bottom-action-bar">
			<view class="quick-actions">
				<button class="quick-btn" @click="selectAllElite1">
					<text>全选精一</text>
				</button>
				<button class="quick-btn" @click="selectAllElite2">
					<text>全选精二</text>
				</button>
				<button class="quick-btn" @click="clearAll">
					<text>清空所有</text>
				</button>
			</view>
			
			<view class="summary-info">
				<text class="summary-text">已点亮: {{ ownedCount }} | 想要: {{ wantedCount }}</text>
			</view>
		</view>
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
export default {
	data() {
		return {
			// 基础数据
			characterData: [],
			filteredBoxes: [],
			displayBoxes: [], // 当前显示的盒子（分页）
			
			// 用户数据 - 支持同时拥有精一和精二
			ownedCharacters: {}, // {角色名: {elite1: true/false, elite2: true/false, boxId: 'xxx'}}
			wantedCharacters: [], // 想要的角色
			tradeCharacters: {}, // 出物的角色 {角色名: {elite1: {price: 0, note: ''}, elite2: {price: 0, note: ''}}}
			
			// 界面状态
			currentMode: 'show', // 'show'或'trade'
			wantMode: false,
			eliteLevel: 'all', // 'all', 'elite1', 'elite2'
			boxFilterIndex: 0,
			boxFilterOptions: ['全部盒子', '常规款', '白名单', '特别通行', '联动款', '音律联觉'],
			boxFilterMap: {
				0: 'all',
				1: 'normal',
				2: 'whitelist',
				3: 'special',
				4: 'cooperation',
				5: 'ambience'
			},
			
			// 分页
			pageSize: 3, // 每次加载的盒子数
			currentPage: 1,
			hasMore: true,
			scrollTop: 0,
			
			// 分享预览相关
			showSharePreviewModal: false,
			isGeneratingShareImage: false,
			shareImagePath: '',
			shareShowPrices: true,
			shareShowWanted: true,
			watermarkText: '明日方舟通行证收集',
			shareGeneratedTime: '',
			
			// 默认头像
			defaultAvatar: '/static/default-avatar.png',
			
			// 统计数据
			ownedCount: 0,
			wantedCount: 0,
			tradeCount: 0,
			totalCharacters: 0
		}
	},
	computed: {
		// 当前筛选的盒子类型
		currentBoxFilter() {
			return this.boxFilterMap[this.boxFilterIndex] || 'all';
		},
		
		// 是否全部选中（任意级别）
		isAllSelected() {
			const allCharacters = this.getAllCharactersList();
			return allCharacters.every(char => char && this.isCharacterOwned(char.name));
		}
	},
	onLoad() {
		
		uni.setNavigationBarTitle({
			title: '我的通行证分享'
		});
		
		// 设置分享配置
			    wx.showShareMenu({
			      withShareTicket: true,
			      menus: ['shareAppMessage', 'shareTimeline']
			    });
			  },
			  
			  onShareAppMessage() {
			    return {
			      title: '方舟通行证谷子查询工具-通行证分享', // 您可以修改这个标题
			      path: '/packageB/pages/card_share/card_share', // 分享路径
			      imageUrl: '' // 如果留空，会自动使用小程序截图
					}
				},
				
				onShareTimeline() {
				  return {
				    title: '方舟通行证谷子查询工具-通行证分享', // 您可以修改这个标题
				    imageUrl: '' // 如果留空，会自动使用小程序截图
				  }
		
		console.log('我的收藏页面加载');
		this.loadData();
		this.loadUserData();
	},
	onShow() {
		// 页面显示时重新加载用户数据
		this.loadUserData();
	},
	methods: {
			logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
			// 加载角色数据 - 添加调试信息
			async loadData() {
				try {
					console.log('开始加载角色数据...');
					
					// 从本地存储加载数据
					const data = uni.getStorageSync('arknightsData');
					const guessData = uni.getStorageSync('guessData');
					const enableGuessData = uni.getStorageSync('enableGuessData');
					
					console.log('主数据:', data?.length || 0, '条');
					console.log('猜测数据:', guessData?.length || 0, '条');
					console.log('启用猜测数据:', enableGuessData);
					
					let allData = [];
					
					if (data && Array.isArray(data) && data.length > 0) {
						allData = [...data];
					}
					
					if (enableGuessData === 'true' && guessData && Array.isArray(guessData) && guessData.length > 0) {
						allData = [...allData, ...guessData];
					}
					
					console.log('总数据:', allData.length, '条');
					
					if (allData.length > 0) {
						// 过滤掉无效数据
						this.characterData = allData.filter(item => item && typeof item === 'object');
						console.log('有效数据:', this.characterData.length, '条');
						
						this.applyFilter();
						this.calculateStatistics();
					} else {
						console.warn('没有数据');
						uni.showToast({
							title: '请先加载数据',
							icon: 'none'
						});
					}
				} catch (e) {
					this.logError(e);
					console.error('加载数据失败:', e);
					uni.showToast({
						title: '加载数据失败',
						icon: 'none'
					});
				}
			},
			
			// 加载用户数据 - 添加调试信息
			loadUserData() {
				try {
					console.log('开始加载用户数据...');
					
					// 加载拥有的角色
					const owned = uni.getStorageSync('ownedCharacters') || {};
					this.ownedCharacters = owned;
					console.log('拥有的角色:', Object.keys(owned).length, '个');
					
					// 加载想要的角色
					const wanted = uni.getStorageSync('wantedCharacters') || [];
					this.wantedCharacters = wanted;
					console.log('想要的角色:', wanted.length, '个');
					
					// 加载出物的角色
					const trade = uni.getStorageSync('tradeCharacters') || {};
					this.tradeCharacters = trade;
					console.log('出物的角色:', Object.keys(trade).length, '个');
					
					console.log('用户数据加载完成');
					this.calculateStatistics();
				} catch (e) {
					this.logError(e);
					console.error('加载用户数据失败:', e);
				}
			},
		
		// 保存用户数据
		saveUserData() {
			try {
				uni.setStorageSync('ownedCharacters', this.ownedCharacters);
				uni.setStorageSync('wantedCharacters', this.wantedCharacters);
				uni.setStorageSync('tradeCharacters', this.tradeCharacters);
				console.log('用户数据保存成功');
			} catch (e) {
				this.logError(e);
				console.error('保存用户数据失败:', e);
			}
		},
		
		// 从收藏同步
		syncWithFavorites() {
			try {
				const favoriteCharacterNames = uni.getStorageSync('favoriteCharacterNames') || [];
				
				// 将收藏的角色标记为拥有（默认精一）
				favoriteCharacterNames.forEach(characterName => {
					if (!this.ownedCharacters[characterName]) {
						this.ownedCharacters = {
							...this.ownedCharacters,
							[characterName]: {
								elite1: true,
								elite2: false,
								boxId: this.findCharacterBox(characterName)
							}
						};
					} else {
						// 如果已存在，确保精一被选中
						this.ownedCharacters = {
							...this.ownedCharacters,
							[characterName]: {
								...this.ownedCharacters[characterName],
								elite1: true
							}
						};
					}
				});
				
				this.saveUserData();
				this.calculateStatistics();
				uni.showToast({
					title: '已从收藏同步',
					icon: 'success'
				});
			} catch (e) {
				this.logError(e);
				console.error('同步失败:', e);
				uni.showToast({
					title: '同步失败',
					icon: 'none'
				});
			}
		},
		
		// 查找角色所在的盒子
		findCharacterBox(characterName) {
			for (const box of this.characterData) {
				if (!box) continue;
				const characters = this.getBoxCharacters(box);
				for (const char of characters) {
					if (char && char.name === characterName) {
						return box.Box_id || '';
					}
				}
			}
			return '';
		},
		
		// 获取盒子的所有角色
		getBoxCharacters(box) {
			if (!box || typeof box !== 'object') {
				console.warn('getBoxCharacters: box is undefined or not an object', box);
				return [];
			}
			
			const characters = [];
			for (let i = 1; i <= 10; i++) {
				const charKey = `character${i}`;
				if (box[charKey]) {
					let characterName = '';
					let imageUrl = '';
					let nolyELITE1 = false;
					let hotcharacter = false;
					let market_price = null;
					
					if (typeof box[charKey] === 'string') {
						characterName = box[charKey];
					} else if (box[charKey] && box[charKey].name) {
						characterName = box[charKey].name;
						imageUrl = box[charKey].imageUrl || '';
						nolyELITE1 = box[charKey].nolyELITE1 === true;
						hotcharacter = box[charKey].hotcharacter === true;
						market_price = box[charKey].market_price || null;
					}
					
					if (characterName && characterName.trim()) {
						characters.push({
							name: characterName,
							avatar: imageUrl || this.defaultAvatar,
							nolyELITE1: nolyELITE1,
							hotcharacter: hotcharacter,
							market_price: market_price,
							boxId: box.Box_id || ''
						});
					}
				}
			}
			return characters;
		},
		
		// 获取过滤后的角色（根据精英化级别）
		getFilteredCharacters(box) {
			if (!box) {
				console.warn('getFilteredCharacters: box is undefined');
				return [];
			}
			
			const characters = this.getBoxCharacters(box);
			
			if (this.eliteLevel === 'all') {
				return characters;
			}
			
			return characters.filter(char => {
				if (!char) return false;
				if (this.eliteLevel === 'elite1') {
					return char.nolyELITE1 || this.isCharacterOwnedAtLevel(char.name, 'elite1');
				} else if (this.eliteLevel === 'elite2') {
					return !char.nolyELITE1 && this.isCharacterOwnedAtLevel(char.name, 'elite2');
				}
				return true;
			});
		},
		
		// 获取所有角色列表
		getAllCharactersList() {
			let allCharacters = [];
			for (const box of this.characterData) {
				if (!box) continue;
				const characters = this.getBoxCharacters(box);
				allCharacters = allCharacters.concat(characters);
			}
			return allCharacters;
		},
		
		// 应用筛选
		applyFilter() {
			let filtered = [...this.characterData].filter(item => item && typeof item === 'object');
			
			// 按盒子类型筛选
			if (this.currentBoxFilter !== 'all') {
				filtered = filtered.filter(box => {
					const boxType = box.Box_type || 'normal';
					if (this.currentBoxFilter === 'normal') {
						return !boxType || boxType === 'normal';
					} else {
						return boxType === this.currentBoxFilter;
					}
				});
			}
			
			this.filteredBoxes = filtered;
			this.currentPage = 1;
			this.loadDisplayBoxes();
			this.calculateStatistics();
		},
		
		// 加载显示盒子（分页）
		loadDisplayBoxes() {
			const startIndex = 0;
			const endIndex = this.currentPage * this.pageSize;
			this.displayBoxes = this.filteredBoxes.slice(startIndex, endIndex);
			this.hasMore = endIndex < this.filteredBoxes.length;
		},
		
		// 加载更多
		loadMore() {
			if (!this.hasMore || this.filteredBoxes.length === 0) return;
			
			this.currentPage++;
			const startIndex = 0;
			const endIndex = this.currentPage * this.pageSize;
			this.displayBoxes = this.filteredBoxes.slice(startIndex, endIndex);
			this.hasMore = endIndex < this.filteredBoxes.length;
		},
		
		// 盒子筛选变化
		onBoxFilterChange(e) {
			this.boxFilterIndex = parseInt(e.detail.value) || 0;
			this.applyFilter();
		},
		
		// 切换模式
		changeMode(mode) {
			this.currentMode = mode;
			if (mode === 'trade') {
				uni.showToast({
					title: '出物模式：点击角色标记为可出物，长按标记为想要的',
					icon: 'none',
					duration: 3000
				});
			}
		},
		
		// 切换想要模式
		toggleWantMode() {
			this.wantMode = !this.wantMode;
			if (this.wantMode) {
				uni.showToast({
					title: '长按角色标记为想要的',
					icon: 'none',
					duration: 2000
				});
			}
		},
		
		// 设置精英化级别筛选
		setEliteLevel(level) {
			this.eliteLevel = level;
		},
		
		// 角色头像错误处理
		onAvatarError(character) {
			if (character) {
				character.avatar = this.defaultAvatar;
			}
		},
		
		// 检查角色是否拥有（任意级别）
		isCharacterOwned(characterName) {
			if (!characterName) return false;
			const charData = this.ownedCharacters[characterName];
			return charData && (charData.elite1 || charData.elite2);
		},
		
		// 检查角色是否在特定级别拥有
		isCharacterOwnedAtLevel(characterName, level) {
			if (!characterName) return false;
			const charData = this.ownedCharacters[characterName];
			if (!charData) return false;
			return level === 'elite1' ? charData.elite1 : charData.elite2;
		},
		
		// 检查角色是否标记为想要
		isCharacterWanted(characterName) {
			if (!characterName) return false;
			return this.wantedCharacters.includes(characterName);
		},
		
		// 检查角色是否标记为出物（任意级别）
		isCharacterForTrade(characterName) {
			if (!characterName) return false;
			const tradeData = this.tradeCharacters[characterName];
			return tradeData && (tradeData.elite1 || tradeData.elite2);
		},
		
		// 获取出物价格
		getTradePrice(characterName, level) {
			if (!characterName) return '';
			const tradeData = this.tradeCharacters[characterName];
			if (!tradeData || !tradeData[level]) return '';
			return tradeData[level].price || '';
		},
		
		// 设置出物价格
		setTradePrice(characterName, level, price) {
			if (!characterName) return;
			
			// 确保拥有该级别的角色才能设置价格
			if (!this.isCharacterOwnedAtLevel(characterName, level)) {
				uni.showToast({
					title: `请先点亮角色的${level === 'elite1' ? '精一' : '精二'}`,
					icon: 'none'
				});
				return;
			}
			
			const priceNum = parseFloat(price);
			if (price && isNaN(priceNum)) {
				uni.showToast({
					title: '请输入有效的价格',
					icon: 'none'
				});
				return;
			}
			
			this.tradeCharacters = {
				...this.tradeCharacters,
				[characterName]: {
					...(this.tradeCharacters[characterName] || {}),
					[level]: {
						price: priceNum || 0,
						note: ''
					}
				}
			};
			
			// 如果价格为0或空，则清除该级别的出物标记
			if (!priceNum) {
				const updatedTrade = { ...this.tradeCharacters };
				if (updatedTrade[characterName]) {
					delete updatedTrade[characterName][level];
					// 如果两个级别都没有价格，则删除整个角色
					if (!updatedTrade[characterName].elite1 && !updatedTrade[characterName].elite2) {
						delete updatedTrade[characterName];
					}
				}
				this.tradeCharacters = updatedTrade;
			}
			
			this.saveUserData();
			this.calculateStatistics();
		},
		
		// 切换角色拥有状态
		toggleCharacterOwnership(character, boxId) {
			if (!character) return;
			
			const characterName = character.name;
			
			if (this.wantMode) {
				// 想要模式下直接标记为想要
				this.toggleCharacterWant(characterName);
				return;
			}
			
			if (this.currentMode === 'trade') {
				// 出物模式下，如果已拥有，则弹出价格输入
				if (this.isCharacterOwned(characterName)) {
					// 已拥有，显示价格输入框
					// 价格输入框已经在模板中显示
					return;
				} else {
					// 未拥有，先点亮再出物
					this.toggleCharacterOwnershipSimple(characterName, boxId);
				}
				return;
			}
			
			// 炫耀模式：切换拥有状态
			this.toggleCharacterOwnershipSimple(characterName, boxId);
		},
		
		// 简单的拥有状态切换
		toggleCharacterOwnershipSimple(characterName, boxId) {
			if (!characterName) return;
			
			const currentData = this.ownedCharacters[characterName];
			
			if (currentData) {
				// 已拥有，根据角色类型切换状态
				const character = this.findCharacterByName(characterName);
				
				if (character && character.nolyELITE1) {
					// 仅精一的角色，点击取消拥有
					const newOwned = { ...this.ownedCharacters };
					delete newOwned[characterName];
					this.ownedCharacters = newOwned;
				} else {
					// 可以精二的角色，循环切换：
					// 1. 只有精一 -> 只有精二
					// 2. 只有精二 -> 都没有
					// 3. 都没有 -> 只有精一
					if (currentData.elite1 && !currentData.elite2) {
						// 只有精一 -> 只有精二
						this.ownedCharacters = {
							...this.ownedCharacters,
							[characterName]: {
								elite1: false,
								elite2: true,
								boxId: boxId || currentData.boxId
							}
						};
					} else if (!currentData.elite1 && currentData.elite2) {
						// 只有精二 -> 都没有
						const newOwned = { ...this.ownedCharacters };
						delete newOwned[characterName];
						this.ownedCharacters = newOwned;
					} else {
						// 都有或都没有 -> 只有精一
						this.ownedCharacters = {
							...this.ownedCharacters,
							[characterName]: {
								elite1: true,
								elite2: false,
								boxId: boxId || currentData.boxId
							}
						};
					}
				}
			} else {
				// 未拥有，设置为拥有精一
				this.ownedCharacters = {
					...this.ownedCharacters,
					[characterName]: {
						elite1: true,
						elite2: false,
						boxId: boxId || ''
					}
				};
			}
			
			this.saveUserData();
			this.calculateStatistics();
		},
		
		// 根据名称查找角色
		findCharacterByName(characterName) {
			if (!characterName) return null;
			
			for (const box of this.characterData) {
				if (!box) continue;
				const characters = this.getBoxCharacters(box);
				for (const char of characters) {
					if (char && char.name === characterName) {
						return char;
					}
				}
			}
			return null;
		},
		
		// 切换角色想要状态
		toggleCharacterWant(characterName) {
			if (!characterName) return;
			
			const index = this.wantedCharacters.indexOf(characterName);
			if (index > -1) {
				this.wantedCharacters.splice(index, 1);
			} else {
				this.wantedCharacters.push(characterName);
			}
			
			this.saveUserData();
			this.calculateStatistics();
		},
		
		// 切换全选
		toggleSelectAll() {
			const allCharacters = this.getAllCharactersList();
			
			if (this.isAllSelected) {
				// 取消全选 - 清空所有拥有状态
				this.ownedCharacters = {};
				// 同时清空出物状态
				this.tradeCharacters = {};
			} else {
				// 全选
				const newOwned = {};
				allCharacters.forEach(char => {
					if (!char) return;
					newOwned[char.name] = {
						elite1: true,
						elite2: !char.nolyELITE1, // 如果可以精二，则同时选中精二
						boxId: char.boxId
					};
				});
				this.ownedCharacters = newOwned;
			}
			
			this.saveUserData();
			this.calculateStatistics();
		},
		
		// 检查盒子是否全部选中
		isBoxAllSelected(box) {
			if (!box) return false;
			const characters = this.getBoxCharacters(box);
			return characters.every(char => char && this.isCharacterOwned(char.name));
		},
		
		// 切换盒子全选
		toggleSelectBox(boxId) {
			if (!boxId) return;
			
			const box = this.characterData.find(b => b && b.Box_id === boxId);
			if (!box) return;
			
			const characters = this.getBoxCharacters(box);
			const isAllSelected = this.isBoxAllSelected(box);
			
			if (isAllSelected) {
				// 取消全选这个盒子
				const newOwned = { ...this.ownedCharacters };
				characters.forEach(char => {
					if (char && char.name && newOwned[char.name]) {
						delete newOwned[char.name];
					}
				});
				this.ownedCharacters = newOwned;
			} else {
				// 全选这个盒子
				const newOwned = { ...this.ownedCharacters };
				characters.forEach(char => {
					if (char && char.name) {
						newOwned[char.name] = {
							elite1: true,
							elite2: !char.nolyELITE1, // 如果可以精二，则同时选中精二
							boxId: boxId
						};
					}
				});
				this.ownedCharacters = newOwned;
			}
			
			this.saveUserData();
			this.calculateStatistics();
		},
		
		// 全选精一
		selectAllElite1() {
			const allCharacters = this.getAllCharactersList();
			const newOwned = { ...this.ownedCharacters };
			
			allCharacters.forEach(char => {
				if (!char) return;
				if (!newOwned[char.name]) {
					newOwned[char.name] = {
						elite1: true,
						elite2: false,
						boxId: char.boxId
					};
				} else {
					newOwned[char.name].elite1 = true;
				}
			});
			
			this.ownedCharacters = newOwned;
			this.saveUserData();
			this.calculateStatistics();
			uni.showToast({
				title: '已全选精一',
				icon: 'success'
			});
		},
		
		// 全选精二
		selectAllElite2() {
			const allCharacters = this.getAllCharactersList();
			const newOwned = { ...this.ownedCharacters };
			
			allCharacters.forEach(char => {
				if (!char) return;
				if (!char.nolyELITE1) { // 可以精二的角色
					if (!newOwned[char.name]) {
						newOwned[char.name] = {
							elite1: false,
							elite2: true,
							boxId: char.boxId
						};
					} else {
						newOwned[char.name].elite2 = true;
					}
				}
			});
			
			this.ownedCharacters = newOwned;
			this.saveUserData();
			this.calculateStatistics();
			uni.showToast({
				title: '已全选可精二角色',
				icon: 'success'
			});
		},
		
		// 清空所有
		clearAll() {
			uni.showModal({
				title: '确认清空',
				content: '确定要清空所有点亮状态吗？',
				success: (res) => {
					if (res.confirm) {
						this.ownedCharacters = {};
						this.wantedCharacters = [];
						this.tradeCharacters = {};
						this.saveUserData();
						this.calculateStatistics();
						uni.showToast({
							title: '已清空',
							icon: 'success'
						});
					}
				}
			});
		},
		
		// 计算统计信息
		calculateStatistics() {
			// 计算拥有的角色数（去重，一个角色无论有几个级别都算一个）
			this.ownedCount = Object.keys(this.ownedCharacters).length;
			
			// 计算想要的角色数
			this.wantedCount = this.wantedCharacters.length;
			
			// 计算出物的角色数（去重）
			this.tradeCount = Object.keys(this.tradeCharacters).length;
			
			// 计算总角色数（去重）
			const allCharacters = new Set();
			for (const box of this.characterData) {
				if (!box) continue;
				const characters = this.getBoxCharacters(box);
				characters.forEach(char => {
					if (char && char.name) {
						allCharacters.add(char.name);
					}
				});
			}
			this.totalCharacters = allCharacters.size;
		},
		
		// 获取盒子类型文本
		getBoxTypeText(boxType) {
			if (!boxType) return '常规款';
			const typeMap = {
				'whitelist': '白名单',
				'special': '特别通行',
				'cooperation': '联动款',
				'ambience': '音律联觉',
				'normal': '常规款'
			};
			return typeMap[boxType] || '常规款';
		},
		
		// 获取盒子类型样式类
		getBoxTypeClass(boxType) {
			if (!boxType) return 'type-normal';
			const classMap = {
				'whitelist': 'type-whitelist',
				'special': 'type-special',
				'cooperation': 'type-cooperation',
				'ambience': 'type-ambience',
				'normal': 'type-normal'
			};
			return classMap[boxType] || 'type-normal';
		},
		
		// ============= 分享功能 =============
		
		// 打开分享预览
		openSharePreview() {
			// 更新生成时间
			const now = new Date();
			this.shareGeneratedTime = `${now.getFullYear()}-${(now.getMonth()+1).toString().padStart(2, '0')}-${now.getDate().toString().padStart(2, '0')} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
			
			// 清空之前的截图
			this.shareImagePath = '';
			
			// 打开模态框
			this.showSharePreviewModal = true;
		},
		
		// 关闭分享预览
		closeSharePreviewModal() {
			this.showSharePreviewModal = false;
			this.shareImagePath = '';
		},
		
		// 获取用于分享的盒子列表
		getShareBoxes() {
			// 获取所有盒子，过滤掉无效数据
			return this.characterData.filter(box => box && typeof box === 'object');
		},
		
		// 获取盒子的分享角色
		getShareCharacters(box) {
			if (!box) {
				console.warn('getShareCharacters: box is undefined');
				return [];
			}
			
			const characters = this.getBoxCharacters(box);
			
			// 根据当前模式筛选角色
			return characters.filter(char => {
				if (!char || !char.name) return false;
				if (this.currentMode === 'show') {
					// 炫耀模式：显示所有已拥有的角色
					return this.isCharacterOwned(char.name);
				} else {
					// 出物模式：显示所有出物的角色
					return this.isCharacterForTrade(char.name);
				}
			});
		},
		
		// 获取分享角色总数
		getTotalShareCharacters() {
			let total = 0;
			for (const box of this.getShareBoxes()) {
				total += this.getShareCharacters(box).length;
			}
			return total;
		},
		
// 生成分享图片（使用Canvas手动绘制）- 修改为长截图形式
async generateShareImage() {
    this.isGeneratingShareImage = true;
    
    try {
        // 获取要分享的角色数据
        const shareData = this.prepareShareData();
        
        // 计算需要的Canvas高度
        const characters = shareData.characters;
        const charactersPerRow = 4;
        const rows = Math.ceil(characters.length / charactersPerRow);
        
        // 计算总高度
        const padding = 15; // 左右边距（375的一半）
        const titleHeight = 80; // 标题和统计信息区域高度
        const characterRowHeight = 90; // 每行角色高度（180的一半）
        const footerHeight = 80; // 底部水印和图片高度
        
        const totalHeight = titleHeight + (rows * characterRowHeight) + footerHeight;
        
        // 创建Canvas上下文
        const ctx = uni.createCanvasContext('shareCanvas', this);
        
        // Canvas尺寸 - 使用375宽度
        const canvasWidth = 375;
        const canvasHeight = Math.max(totalHeight, 1000); // 确保最小高度
        
        // 1. 绘制白色背景
        ctx.setFillStyle('#ffffff');
        ctx.fillRect(0, 0, canvasWidth, canvasHeight);
        
        // 2. 绘制标题
        ctx.setFontSize(18);
        ctx.setFillStyle('#333333');
        ctx.setTextAlign('center');
        const title = this.currentMode === 'show' ? '🌟 我的明日方舟收藏 🌟' : '💼 出物清单 💼';
        ctx.fillText(title, canvasWidth / 2, 40);
        
        // 3. 绘制副标题
        ctx.setFontSize(12);
        ctx.setFillStyle('#666666');
        const subtitle = this.currentMode === 'show' ? '炫耀模式 - 展示我的所有收藏' : '出物模式 - 这些角色可以交易';
        ctx.fillText(subtitle, canvasWidth / 2, 60);
        
        // 4. 绘制分割线
        ctx.setStrokeStyle('#e0e0e0');
        ctx.setLineWidth(1);
        ctx.moveTo(padding, 75);
        ctx.lineTo(canvasWidth - padding, 75);
        ctx.stroke();
        
        // 5. 绘制统计信息
        ctx.setFontSize(12);
        ctx.setTextAlign('left');
        let yPos = 95;
        
        // 已点亮
        ctx.setFillStyle('#666666');
        ctx.fillText('已点亮角色:', padding, yPos);
        ctx.setFillStyle('#333333');
        ctx.setTextAlign('right');
        ctx.fillText(`${this.ownedCount}个`, canvasWidth - padding, yPos);
        
        ctx.setTextAlign('left');
        yPos += 20;
        
        // 出物角色（如果是出物模式）
        if (this.currentMode === 'trade' && this.tradeCount > 0) {
            ctx.setFillStyle('#666666');
            ctx.fillText('出物角色:', padding, yPos);
            ctx.setFillStyle('#333333');
            ctx.setTextAlign('right');
            ctx.fillText(`${this.tradeCount}个`, canvasWidth - padding, yPos);
            
            ctx.setTextAlign('left');
            yPos += 20;
        }
        
        // 想要的角色
        if (this.shareShowWanted && this.wantedCount > 0) {
            ctx.setFillStyle('#666666');
            ctx.fillText('想要的角色:', padding, yPos);
            ctx.setFillStyle('#FF6B6B');
            ctx.setTextAlign('right');
            ctx.fillText(`${this.wantedCount}个`, canvasWidth - padding, yPos);
            
            ctx.setTextAlign('left');
            yPos += 20;
        }
        
        // 生成时间
        ctx.setFillStyle('#666666');
        ctx.fillText('生成时间:', padding, yPos);
        ctx.setFillStyle('#333333');
        ctx.setTextAlign('right');
        ctx.fillText(this.shareGeneratedTime, canvasWidth - padding, yPos);
        
        yPos += 30;
        
        // 6. 绘制角色列表标题
        ctx.setTextAlign('left');
        ctx.setFontSize(15);
        ctx.setFillStyle('#333333');
        ctx.fillText('角色列表', padding, yPos);
        
        yPos += 25;
        
        // 7. 绘制角色列表 - 一行四个，长截图形式
        const characterItemWidth = (canvasWidth - padding * 2) / charactersPerRow;
        const avatarSize = 40; // 头像大小（80的一半）
        const avatarMargin = 8; // 头像与文字间距（15的一半）
        
        let startY = yPos;
        let currentY = startY;
        
        if (characters && characters.length > 0) {
            for (let i = 0; i < characters.length; i++) {
                const char = characters[i];
                const rowIndex = Math.floor(i / charactersPerRow);
                const colIndex = i % charactersPerRow;
                
                const itemX = padding + colIndex * characterItemWidth;
                const itemY = startY + rowIndex * characterRowHeight;
                
                // 更新当前Y位置
                currentY = itemY;
                
                // 绘制角色头像背景（圆形）
                const avatarX = itemX + characterItemWidth / 2;
                const avatarY = itemY + avatarMargin + avatarSize / 2;
                
                // 绘制头像圆形背景
                ctx.setFillStyle('#f8fafc');
                ctx.beginPath();
                ctx.arc(avatarX, avatarY, avatarSize / 2, 0, Math.PI * 2);
                ctx.fill();
                
                // 绘制角色名称
                ctx.setFontSize(12);
                ctx.setFillStyle('#333333');
                ctx.setTextAlign('center');
                
                // 处理角色名称过长问题
                let displayName = char.name;
                if (displayName.length > 4) {
                    displayName = displayName.substring(0, 4);
                }
                
                const nameY = itemY + avatarSize + avatarMargin * 2 + 12;
                ctx.fillText(displayName, avatarX, nameY);
                
                // 绘制精英化级别标签
                let tagsY = nameY + 10;
                let tagX = avatarX - 20;
                
                if (char.elite1) {
                    ctx.setFillStyle('#E6A23C');
                    ctx.fillRect(tagX, tagsY, 22, 12);
                    ctx.setFontSize(9);
                    ctx.setFillStyle('#ffffff');
                    ctx.setTextAlign('center');
                    ctx.fillText('精一', tagX + 11, tagsY + 9);
                    tagX += 25;
                }
                
                if (char.elite2 && !char.nolyELITE1) {
                    ctx.setFillStyle('#409EFF');
                    ctx.fillRect(tagX, tagsY, 22, 12);
                    ctx.setFontSize(9);
                    ctx.setFillStyle('#ffffff');
                    ctx.setTextAlign('center');
                    ctx.fillText('精二', tagX + 11, tagsY + 9);
                }
                
                // 绘制价格信息
                if (this.shareShowPrices) {
                    let priceY = itemY + characterRowHeight - 8;
                    
                    if (this.currentMode === 'trade' && char.tradePrices) {
                        ctx.setFontSize(10);
                        ctx.setFillStyle('#E6A23C');
                        ctx.setTextAlign('center');
                        
                        if (char.tradePrices.elite1) {
                            ctx.fillText(`${char.tradePrices.elite1}元`, avatarX, priceY);
                        } else if (char.tradePrices.elite2 && !char.nolyELITE1) {
                            ctx.fillText(`${char.tradePrices.elite2}元`, avatarX, priceY);
                        }
                    } else if (this.currentMode === 'show' && char.marketPrices) {
                        ctx.setFontSize(10);
                        ctx.setFillStyle('#E6A23C');
                        ctx.setTextAlign('center');
                        
                        if (char.marketPrices.elite1) {
                            ctx.fillText(`${char.marketPrices.elite1}元`, avatarX, priceY);
                        } else if (char.marketPrices.elite2 && !char.nolyELITE1) {
                            ctx.fillText(`${char.marketPrices.elite2}元`, avatarX, priceY);
                        }
                    }
                }
                
                // 绘制想要标记
                if (this.shareShowWanted && char.wanted) {
                    ctx.setFontSize(14);
                    ctx.setFillStyle('#FF6B6B');
                    ctx.setTextAlign('center');
                    ctx.fillText('❤️', avatarX + 15, itemY + avatarSize - 8);
                }
            }
            
            // 更新Y位置到角色列表底部
            currentY += characterRowHeight;
        } else {
            // 没有角色的情况
            ctx.setFontSize(13);
            ctx.setFillStyle('#999999');
            ctx.setTextAlign('center');
            ctx.fillText(
                this.currentMode === 'show' ? '暂无点亮角色' : '暂无出物角色',
                canvasWidth / 2,
                startY + 30
            );
            currentY = startY + 60;
        }
        
        currentY += 20;
        
        // 8. 绘制底部水印
        ctx.setFontSize(10);
        ctx.setFillStyle('#999999');
        ctx.setTextAlign('center');
        ctx.fillText(this.watermarkText, canvasWidth / 2, currentY);
        
        currentY += 20;
        
        // 9. 绘制Searchus.png图片
        try {
            // 获取图片信息
            const imageInfo = await new Promise((resolve, reject) => {
                uni.getImageInfo({
                    src: '/static/Searchus.png',
                    success: resolve,
                    fail: reject
                });
            });
            
            // 计算图片尺寸，保持宽高比
            const imgWidth = 90; // 图片宽度（180的一半）
            const imgHeight = (imageInfo.height * imgWidth) / imageInfo.width;
            
            const imgX = (canvasWidth - imgWidth) / 2;
            const imgY = currentY;
            
            // 绘制图片
            ctx.drawImage('/static/Searchus.png', imgX, imgY, imgWidth, imgHeight);
            
            // 更新Y位置
            currentY += imgHeight + 10;
            
        } catch (imgError) {
        	this.logError(imgError);
            console.error('加载Searchus.png失败:', imgError);
            // 如果图片加载失败，绘制文字代替
            ctx.setFontSize(11);
            ctx.setFillStyle('#999999');
            ctx.setTextAlign('center');
            ctx.fillText('微信搜索"方舟通行证谷子查询工具"获取更多~', canvasWidth / 2, currentY);
            currentY += 15;
        }
        
        // 10. 绘制底部版权信息
        ctx.setFontSize(10);
        ctx.setFillStyle('#999999');
        ctx.fillText('长按保存图片，分享给好友', canvasWidth / 2, currentY);
        
        // 等待Canvas绘制完成
        await new Promise((resolve, reject) => {
            ctx.draw(true, () => {
                setTimeout(() => {
                    resolve();
                }, 500);
            });
        });
        
        // 11. 生成图片
        await new Promise((resolve, reject) => {
            uni.canvasToTempFilePath({
                canvasId: 'shareCanvas',
                fileType: 'png',
                quality: 1,
                success: (res) => {
                    console.log('Canvas生成图片成功:', res.tempFilePath);
                    this.shareImagePath = res.tempFilePath;
                    resolve();
                },
                fail: (err) => {
                    console.error('Canvas生成图片失败:', err);
                    reject(err);
                }
            }, this);
        });
        
        uni.showToast({
            title: '分享图生成成功',
            icon: 'success'
        });
        
    } catch (error) {
    	this.logError(error);
        console.error('生成分享图失败:', error);
        uni.showToast({
            title: '生成分享图失败，请重试',
            icon: 'none'
        });
    } finally {
        this.isGeneratingShareImage = false;
    }
},
		
		// 准备分享数据
		prepareShareData() {
			const characters = [];
			
			for (const box of this.getShareBoxes()) {
				const boxChars = this.getShareCharacters(box);
				for (const char of boxChars) {
					if (!char || !char.name) continue;
					
					const characterData = {
						name: char.name,
						elite1: this.isCharacterOwnedAtLevel(char.name, 'elite1'),
						elite2: this.isCharacterOwnedAtLevel(char.name, 'elite2'),
						wanted: this.isCharacterWanted(char.name),
						nolyELITE1: char.nolyELITE1
					};
					
					if (this.currentMode === 'trade' && this.isCharacterForTrade(char.name)) {
						characterData.tradePrices = {
							elite1: this.getTradePrice(char.name, 'elite1'),
							elite2: this.getTradePrice(char.name, 'elite2')
						};
					} else if (this.currentMode === 'show' && char.market_price) {
						characterData.marketPrices = {
							elite1: char.market_price.ELITE1,
							elite2: char.market_price.ELITE2
						};
					}
					
					characters.push(characterData);
				}
			}
			
			return {
				characters: characters,
				ownedCount: this.ownedCount,
				tradeCount: this.tradeCount,
				wantedCount: this.wantedCount
			};
		},
		
		// 预览分享图片（双击/点击预览）
		previewShareImage() {
			if (!this.shareImagePath) {
				uni.showToast({
					title: '请先生成分享图',
					icon: 'none'
				});
				return;
			}
			
			// 使用uni.previewImage预览图片
			uni.previewImage({
				urls: [this.shareImagePath],
				current: 0,
				success: () => {
					console.log('预览图片成功');
				},
				fail: (err) => {
					console.error('预览图片失败:', err);
					uni.showToast({
						title: '预览图片失败',
						icon: 'none'
					});
				}
			});
		},
		
		// 保存分享图片
		saveShareImage() {
			if (!this.shareImagePath) {
				uni.showToast({
					title: '请先生成分享图',
					icon: 'none'
				});
				return;
			}
			
			uni.saveImageToPhotosAlbum({
				filePath: this.shareImagePath,
				success: () => {
					uni.showToast({
						title: '保存成功',
						icon: 'success'
					});
				},
				fail: (err) => {
					console.error('保存失败:', err);
					if (err.errMsg && err.errMsg.includes('auth')) {
						uni.showModal({
							title: '需要授权',
							content: '需要相册权限才能保存图片',
							success: (res) => {
								if (res.confirm) {
									uni.openSetting();
								}
							}
						});
					} else {
						uni.showToast({
							title: '保存失败，请检查权限',
							icon: 'none'
						});
					}
				}
			});
		},
		
		// 分割文本为多行（处理长标题）
		splitTextToLines(text, maxChars, maxWidth) {
			if (!text || text.length <= maxChars) {
				return [text];
			}
			
			const lines = [];
			let currentLine = '';
			
			for (let i = 0; i < text.length; i++) {
				currentLine += text[i];
				
				// 如果达到最大字符数或遇到换行符
				if (currentLine.length >= maxChars || text[i] === '\n') {
					lines.push(currentLine.trim());
					currentLine = '';
				}
			}
			
			if (currentLine.length > 0) {
				lines.push(currentLine.trim());
			}
			
			return lines;
		}
	}
}
</script>

<style scoped>
.container {
	padding: 20rpx;
	background-color: #f5f5f5;
	min-height: 100vh;
	padding-bottom: 120rpx;
}

/* 模式选择区域 */
.mode-section {
	background-color: #fff;
	border-radius: 16rpx;
	padding: 20rpx;
	margin-bottom: 20rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}

.mode-tabs {
	display: flex;
	justify-content: space-around;
	margin-bottom: 20rpx;
}

.mode-tab {
	flex: 1;
	text-align: center;
	padding: 20rpx;
	border-radius: 12rpx;
	background-color: #f8f8f8;
	margin: 0 10rpx;
	transition: all 0.3s;
}

.mode-tab.active {
	background-color: #409EFF;
	color: #fff;
	transform: translateY(-2rpx);
	box-shadow: 0 4rpx 12rpx rgba(64, 158, 255, 0.3);
}

.mode-icon {
	display: block;
	font-size: 40rpx;
	margin-bottom: 10rpx;
}

.mode-text {
	font-size: 24rpx;
	font-weight: bold;
}

.action-buttons {
	display: flex;
	gap: 15rpx;
}

.action-btn {
	flex: 1;
	padding: 20rpx;
	border-radius: 10rpx;
	font-size: 24rpx;
	white-space: nowrap;
}

.select-all-btn {
	background-color: #67C23A;
	color: #fff;
}

.share-btn {
	background-color: #409EFF;
	color: #fff;
}

.want-btn {
	background-color: #f0f0f0;
	color: #666;
}

.want-btn.active {
	background-color: #FF6B6B;
	color: #fff;
}

/* 筛选和统计区域 */
.filter-stats-section {
	display: flex;
	justify-content: space-between;
	align-items: center;
	background-color: #fff;
	border-radius: 16rpx;
	padding: 20rpx;
	margin-bottom: 20rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}

.box-filter-picker {
	background-color: #f8f8f8;
	border-radius: 10rpx;
	padding: 15rpx 20rpx;
	flex: 1;
	margin-right: 20rpx;
}

.picker-text {
	font-size: 26rpx;
	color: #333;
}

.stats-info {
	display: flex;
	flex-direction: column;
	align-items: flex-end;
}

.stat-item {
	font-size: 22rpx;
	color: #666;
	margin-bottom: 5rpx;
}

/* 精英化级别选择 */
.elite-level-selector {
	display: flex;
	background-color: #fff;
	border-radius: 16rpx;
	padding: 15rpx;
	margin-bottom: 20rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}

.elite-option {
	flex: 1;
	text-align: center;
	padding: 15rpx;
	margin: 0 5rpx;
	border-radius: 10rpx;
	background-color: #f8f8f8;
	color: #666;
	transition: all 0.3s;
}

.elite-option.active {
	background-color: #409EFF;
	color: #fff;
}

.elite-tag {
	font-size: 22rpx;
	padding: 4rpx 8rpx;
	border-radius: 4rpx;
	font-weight: bold;
}

.elite1-tag {
	background-color: #E6A23C;
	color: #fff;
}

.elite2-tag {
	background-color: #409EFF;
	color: #fff;
}

/* 滚动区域 */
.boxes-scroll-view {
	height: calc(100vh - 400rpx);
}

/* 盒子部分 */
.box-section {
	background-color: #fff;
	border-radius: 16rpx;
	padding: 20rpx;
	margin-bottom: 20rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}

.box-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 1rpx solid #f0f0f0;
}

.box-title {
	font-size: 28rpx;
	font-weight: bold;
	color: #333;
}

.box-type-tag {
	font-size: 20rpx;
	padding: 6rpx 12rpx;
	border-radius: 6rpx;
	color: #fff;
}

.type-normal {
	background-color: #67C23A;
}

.type-whitelist {
	background-color: #E6A23C;
}

.type-special {
	background-color: #F56C6C;
}

.type-cooperation {
	background-color: #909399;
}

.type-ambience {
	background-color: #409EFF;
}

.select-box-btn {
	background-color: #f0f0f0;
	color: #666;
	font-size: 22rpx;
	padding: 10rpx 15rpx;
	border-radius: 8rpx;
}

/* 角色网格 */
.characters-grid {
	display: grid;
	grid-template-columns: repeat(4, 1fr);
	gap: 15rpx;
}

.character-item {
	display: flex;
	flex-direction: column;
	align-items: center;
}

.character-avatar-wrapper {
	position: relative;
	width: 120rpx;
	height: 160rpx;
	margin-bottom: 10rpx;
}

.character-avatar {
	width: 100%;
	height: 120rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	transition: all 0.3s;
}

.character-avatar.owned {
	border-color: #67C23A;
	box-shadow: 0 4rpx 12rpx rgba(103, 194, 58, 0.3);
}

.character-avatar.grayed {
	filter: grayscale(100%);
	opacity: 0.6;
}

.character-avatar.wanted {
	border-color: #FF6B6B;
}

.character-avatar.for-trade {
	border-color: #E6A23C;
}

.status-indicators {
	position: absolute;
	top: -10rpx;
	right: -10rpx;
	display: flex;
	flex-direction: column;
	gap: 5rpx;
}

.owned-indicator,
.want-indicator,
.trade-indicator,
.hot-indicator {
	width: 30rpx;
	height: 30rpx;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16rpx;
	border: 2rpx solid #fff;
	color: #fff;
	font-weight: bold;
}

.elite1-indicator {
	background-color: #E6A23C;
}

.elite2-indicator {
	background-color: #409EFF;
}

.want-indicator {
	background-color: #FF6B6B;
	font-size: 18rpx;
}

.trade-indicator {
	background-color: #E6A23C;
	font-size: 18rpx;
}

.hot-indicator {
	background-color: #F56C6C;
	font-size: 18rpx;
}

.price-tags {
	position: absolute;
	bottom: 40rpx;
	left: 0;
	right: 0;
	display: flex;
	justify-content: center;
	gap: 5rpx;
}

.price-tag {
	font-size: 18rpx;
	padding: 2rpx 6rpx;
	border-radius: 4rpx;
	color: #fff;
	font-weight: bold;
	background-color: rgba(0, 0, 0, 0.7);
}

/* 出物模式价格输入 */
.trade-price-inputs {
	position: absolute;
	bottom: 0;
	left: 0;
	right: 0;
	background-color: rgba(255, 255, 255, 0.9);
	padding: 5rpx;
	border-radius: 0 0 12rpx 12rpx;
}

.price-input-row {
	display: flex;
	align-items: center;
	margin-bottom: 3rpx;
}

.price-label {
	font-size: 16rpx;
	color: #666;
	width: 40rpx;
}

.price-input {
	flex: 1;
	height: 30rpx;
	font-size: 16rpx;
	text-align: center;
	background-color: #f8f8f8;
	border-radius: 4rpx;
	padding: 0 5rpx;
}

.price-unit {
	font-size: 16rpx;
	color: #666;
	width: 20rpx;
}

.only-elite1-tip {
	font-size: 16rpx;
	color: #999;
	text-align: center;
	width: 100%;
}

.character-name {
	font-size: 22rpx;
	color: #333;
	text-align: center;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	width: 120rpx;
	margin-top: 5rpx;
}

/* 加载更多 */
.load-more {
	text-align: center;
	padding: 30rpx;
	color: #999;
	font-size: 24rpx;
}

/* 空状态 */
.empty-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 100rpx 0;
}

.empty-icon {
	width: 150rpx;
	height: 150rpx;
	margin-bottom: 30rpx;
	opacity: 0.5;
}

.empty-text {
	font-size: 28rpx;
	color: #999;
	margin-bottom: 30rpx;
}

.sync-btn {
	background-color: #409EFF;
	color: #fff;
	border-radius: 50rpx;
	font-size: 26rpx;
	padding: 20rpx 40rpx;
}

/* 分享预览模态框 */
.share-preview-modal {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background-color: rgba(0, 0, 0, 0.8);
	display: flex;
	align-items: center;
	justify-content: center;
	z-index: 10000;
	padding: 40rpx;
}

.share-preview-content {
	background-color: #fff;
	border-radius: 16rpx;
	width: 100%;
	max-width: 800rpx;
	max-height: 90vh;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

.share-preview-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 30rpx;
	border-bottom: 1rpx solid #f0f0f0;
}

.share-preview-title {
	font-size: 30rpx;
	font-weight: bold;
	color: #333;
}

.share-close-btn {
	width: 60rpx;
	height: 60rpx;
	border-radius: 50%;
	background-color: #f0f0f0;
	color: #666;
	font-size: 36rpx;
	display: flex;
	align-items: center;
	justify-content: center;
}

.share-preview-body {
	flex: 1;
	padding: 20rpx;
	overflow-y: auto;
}

/* 分享内容预览样式 */
.share-content-preview {
	background-color: #ffffff;
	border: 1rpx solid #e0e0e0;
	border-radius: 12rpx;
	padding: 30rpx;
}

.share-header {
	text-align: center;
	margin-bottom: 30rpx;
}

.share-title {
	font-size: 32rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 10rpx;
}

.share-subtitle {
	font-size: 24rpx;
	color: #666;
}

.share-stats {
	background-color: #f8fafc;
	border-radius: 10rpx;
	padding: 20rpx;
	margin-bottom: 30rpx;
}

.stat-row {
	display: flex;
	justifyContent: space-between;
	margin-bottom: 10rpx;
}

.stat-row:last-child {
	margin-bottom: 0;
}

.stat-label {
	font-size: 24rpx;
	color: #666;
}

.stat-value {
	font-size: 24rpx;
	color: #333;
	font-weight: bold;
}

.wanted-stat {
	color: #FF6B6B;
}

.share-divider {
	height: 1rpx;
	background-color: #e0e0e0;
	margin: 30rpx 0;
}

.share-characters-section {
	margin-bottom: 30rpx;
}

.section-title {
	font-size: 28rpx;
	font-weight: bold;
	color: #333;
	margin-bottom: 20rpx;
	display: block;
}

.share-box-section {
	margin-bottom: 25rpx;
}

.share-box-header {
	display: flex;
	alignItems: center;
	margin-bottom: 15rpx;
}

.share-box-title {
	font-size: 26rpx;
	font-weight: bold;
	color: #409EFF;
	margin-right: 15rpx;
}

.share-box-type {
	font-size: 20rpx;
	padding: 4rpx 10rpx;
	border-radius: 6rpx;
	color: #fff;
	background-color: #67C23A;
}

.share-characters-grid {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: 15rpx;
}

.share-character-item {
	background-color: #f8fafc;
	border-radius: 10rpx;
	padding: 15rpx;
	border: 1rpx solid #e0e0e0;
}

.share-character-content {
	display: flex;
	flex-direction: column;
}

.share-character-name {
	font-size: 26rpx;
	font-weight: bold;
	color: #333;
	margin-bottom: 8rpx;
}

.share-level-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 8rpx;
	margin-bottom: 8rpx;
}

.level-tag {
	font-size: 20rpx;
	padding: 4rpx 8rpx;
	border-radius: 4rpx;
	color: #fff;
	font-weight: bold;
}

.share-status-tags {
	display: flex;
	flex-direction: column;
	gap: 5rpx;
}

.status-tag {
	font-size: 20rpx;
	padding: 4rpx 8rpx;
	border-radius: 4rpx;
	background-color: #f0f0f0;
	color: #666;
}

.wanted-tag {
	background-color: #FF6B6B;
	color: #fff;
}

.price-info {
	display: flex;
	flex-direction: column;
	gap: 5rpx;
}

.price-tag {
	font-size: 20rpx;
	color: #E6A23C;
	font-weight: bold;
}

.no-characters {
	text-align: center;
	padding: 50rpx 0;
}

.no-characters-text {
	font-size: 24rpx;
	color: #999;
}

.share-watermark {
	text-align: center;
	padding: 20rpx 0;
	border-top: 1rpx dashed #e0e0e0;
	margin-top: 30rpx;
}

.share-watermark text {
	font-size: 20rpx;
	color: #999;
}

.share-footer {
	text-align: center;
	padding-top: 20rpx;
	border-top: 1rpx solid #e0e0e0;
}

.footer-text {
	font-size: 18rpx;
	color: #999;
	display: block;
	margin-bottom: 5rpx;
}

/* 分享预览底部操作按钮 */
.share-preview-footer {
	padding: 20rpx;
	border-top: 1rpx solid #f0f0f0;
}

.share-options {
	display: flex;
	justify-content: space-around;
	margin-bottom: 20rpx;
}

.option-item {
	display: flex;
	align-items: center;
}

.option-label {
	font-size: 24rpx;
	color: #666;
	margin-right: 10rpx;
}

.preview-action-btn {
	padding: 20rpx;
	border-radius: 10rpx;
	font-size: 24rpx;
	margin-bottom: 15rpx;
	width: 100%;
}

.capture-btn {
	background-color: #409EFF;
	color: #fff;
}

.capture-btn:disabled {
	opacity: 0.5;
}

.action-buttons-row {
	display: flex;
	flex-direction: column;
	gap: 15rpx;
}

.captured-image-preview {
	width: 100%;
	border-radius: 8rpx;
	margin-bottom: 15rpx;
	max-height: 300rpx;
	cursor: pointer;
}

.share-buttons {
	display: flex;
	gap: 15rpx;
}

.save-btn {
	background-color: #67C23A;
	color: #fff;
	flex: 1;
}

.share-btn {
	background-color: #E6A23C;
	color: #fff;
	flex: 1;
}

/* 底部操作栏 */
.bottom-action-bar {
	position: fixed;
	bottom: 0;
	left: 0;
	right: 0;
	background-color: #fff;
	padding: 20rpx;
	border-top: 1rpx solid #e0e0e0;
	box-shadow: 0 -4rpx 20rpx rgba(0, 0, 0, 0.1);
	display: flex;
	flex-direction: column;
	gap: 15rpx;
}

.quick-actions {
	display: flex;
	gap: 15rpx;
}

.quick-btn {
	flex: 1;
	background-color: #f8f8f8;
	color: #666;
	padding: 15rpx;
	border-radius: 10rpx;
	font-size: 22rpx;
}

.summary-info {
	text-align: center;
}

.summary-text {
	font-size: 24rpx;
	color: #333;
	font-weight: bold;
}

/* 响应式调整 */
@media (min-width: 750rpx) {
	.characters-grid {
		grid-template-columns: repeat(6, 1fr);
	}
	
	.character-avatar-wrapper {
		width: 100rpx;
		height: 140rpx;
	}
	
	.character-name {
		width: 100rpx;
		font-size: 20rpx;
	}
	
	.share-characters-grid {
		grid-template-columns: repeat(3, 1fr);
	}
}

@media (max-width: 500rpx) {
	.characters-grid {
		grid-template-columns: repeat(3, 1fr);
	}
	
	.action-buttons {
		flex-direction: column;
	}
	
	.action-buttons-row {
		flex-direction: column;
	}
	
	.character-avatar-wrapper {
		height: 150rpx;
	}
}
</style>