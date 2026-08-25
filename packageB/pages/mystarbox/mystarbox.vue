<template>
	<view :class="['container', 'theme-' + themeMode]">
		<!-- 市价状态提示区域 -->
		<view class="market-price-status-section" v-if="hasMarketPriceData">
			<text class="mp-close-btn" @click="dismissMarketPriceTip">✕</text>
			<text class="mp-status-text" :class="showMarketPrice ? 'mp-enabled' : 'mp-disabled'">
				{{ showMarketPrice ? '✓ 正在显示市价信息' : '○ 市价信息已隐藏' }}
			</text>
			<text class="mp-tip" v-if="!showMarketPrice">（前往设置页面开启）</text>
			<text class="mp-settings-btn" v-if="!showMarketPrice" @click="goToSettings">去设置</text>
		</view>
		
		<!-- 市价提示信息（只在开启时显示） -->
		<view class="market-price-hint" v-if="showMarketPrice && hasMarketPriceData">
		  <text class="hint-text">💡 市价信息来源于千岛APP，数据仅供参考，请以实际为准</text>
		</view>

		<!-- 分类选项 -->
		<view class="category-options">
			<button 
				class="category-btn" 
				:class="{ active: activeCategory === 'box' }"
				@click="switchCategory('box')"
			>
				盒号收藏
			</button>
			<button 
				class="category-btn" 
				:class="{ active: activeCategory === 'character' }"
				@click="switchCategory('character')"
			>
				干员收藏
			</button>
		</view>

		<!-- 收藏统计 -->
		<view class="stats-section" v-if="(activeCategory === 'box' && favoriteBoxes.length > 0) || (activeCategory === 'character' && favoriteCharacters.length > 0)">
			<text class="stats-text">
				{{ activeCategory === 'box' ? `已收藏 ${favoriteBoxes.length} 个盒号` : `已收藏 ${favoriteCharacters.length} 个干员` }}
			</text>
			<button 
				class="clear-all-btn" 
				@click="showClearConfirm"
				v-if="(activeCategory === 'box' && favoriteBoxes.length > 0) || (activeCategory === 'character' && favoriteCharacters.length > 0)"
			>
				清空当前收藏
			</button>
		</view>

		<!-- 空状态 -->
		<view class="empty-state" v-if="activeCategory === 'box' && favoriteBoxes.length === 0">
			<image class="empty-icon" src="/static/empty-icon.png"></image>
			<text class="empty-text">暂无盒号收藏</text>
			<text class="empty-tip">在搜索或列表页面可以收藏盒号</text>
			<view class="empty-actions">
				<button class="empty-btn" @click="navigateTo('/pages/Search/Search')">去搜索页面</button>
				<button class="empty-btn" @click="navigateTo('/pages/list/list')">去列表页面</button>
			</view>
		</view>

		<view class="empty-state" v-if="activeCategory === 'character' && favoriteCharacters.length === 0">
			<image class="empty-icon" src="/static/empty-icon.png"></image>
			<text class="empty-text">暂无干员收藏</text>
			<text class="empty-tip">在搜索结果中可以收藏干员</text>
			<button class="empty-btn" @click="navigateTo('/pages/Search/Search')">去搜索干员</button>
		</view>

		<!-- 收藏的盒号列表 -->
		<view class="favorites-section" v-if="activeCategory === 'box' && favoriteBoxes.length > 0">
			<scroll-view class="favorites-list" scroll-y>
				<view 
					class="favorite-item" 
					v-for="(box, index) in favoriteBoxes" 
					:key="box.Box_id"
				>
					<view class="box-header">
						<view class="box-header-left">
							<text class="box-id">盒号 {{ box.Box_id }}</text>
							<text class="box-type" :class="getBoxTypeClass(box.Box_type)">
								{{ getBoxTypeText(box.Box_type) }}
							</text>
						</view>
						<view class="box-header-right">
							<button 
								class="favorite-btn" 
								:class="isFavoriteBox(box.Box_id) ? 'favorited' : ''"
								@click="toggleFavoriteBox(box.Box_id)"
							>
								{{ isFavoriteBox(box.Box_id) ? '已收藏' : '收藏' }}
							</button>
							<button 
								class="view-info-btn" 
								@click="goToBoxInfo(box.Box_id)"
							>
								查看详情
							</button>
						</view>
					</view>
					
					<!-- 干员网格 -->
					<view class="characters-grid">
						<view 
							class="character-card" 
							v-for="(character, charIndex) in getBoxCharacters(box)" 
							:key="charIndex"
							@click="handleCharacterClick(character.name)"
						>
							<view class="character-avatar-container">
								<image 
									class="character-avatar" 
									:src="character.avatar" 
									mode="aspectFit"
									@error="onAvatarError(box.Box_id, charIndex)"
								></image>
								<image 
									v-if="character.hotcharacter" 
									class="hot-character-icon" 
									src="/static/hot.png" 
									mode="aspectFit"
								></image>
								<!-- 收藏状态指示器 -->
								<view 
									class="favorite-indicator" 
									:class="isFavoriteCharacter(character.name) ? 'favorited' : ''"
								>
									{{ isFavoriteCharacter(character.name) ? '★' : '☆' }}
								</view>
							</view>
							<view class="character-info">
								<view class="character-name-line">
									<text class="character-name">{{ character.name }}</text>
									<text v-if="character.nolyELITE1" class="elite-tag">仅精一</text>
								</view>
								<!-- 市场价显示小标签 - 根据showMarketPrice决定是否显示 -->
								<view class="market-price-tags" v-if="showMarketPrice && character.market_price">
									<text 
										class="price-tag elite1-tag" 
										v-if="character.market_price.ELITE1"
									>
										精一 {{ character.market_price.ELITE1 }}元
									</text>
									<text 
										class="price-tag elite2-tag" 
										v-if="character.market_price.ELITE2 && !character.nolyELITE1"
									>
										精二 {{ character.market_price.ELITE2 }}元
									</text>
								</view>
								<!-- 市价关闭时的提示 -->
								<view class="price-disabled-hint" v-else-if="character.hasMarketPrice && !showMarketPrice">
									<text class="hint-text-small">市价信息已隐藏</text>
								</view>
							</view>
						</view>
					</view>
				</view>
			</scroll-view>
		</view>

		<!-- 收藏的干员列表 -->
		<view class="favorites-section" v-if="activeCategory === 'character' && favoriteCharacters.length > 0">
			<scroll-view class="favorites-list" scroll-y>
				<view 
					class="character-favorite-item" 
					v-for="(character, index) in favoriteCharacters" 
					:key="character.name + index"
				>
					<view class="character-result-header">
						<view class="character-with-avatar">
							<view class="character-avatar-container">
								<image 
									class="character-avatar" 
									:src="character.avatar" 
									mode="aspectFit"
									@error="onCharacterAvatarError(index)"
								></image>
								<image 
									v-if="character.hotcharacter" 
									class="hot-character-icon" 
									src="/static/hot.png" 
									mode="aspectFit"
								></image>
							</view>
							<view class="character-info">
								<view class="character-name-line">
									<text class="label">干员：</text>
									<text class="value">{{ character.name }}</text>
									<text v-if="character.nolyELITE1" class="elite-tag">仅精一</text>
								</view>
								<text class="label">所在盒号：</text>
								<text class="value">{{ character.boxIds.join(', ') }}</text>
							</view>
						</view>
						<view class="character-favorite-actions">
							<button 
								class="favorite-btn" 
								:class="isFavoriteCharacter(character.name) ? 'favorited' : ''"
								@click="toggleFavoriteCharacter(character.name)"
							>
								{{ isFavoriteCharacter(character.name) ? '已收藏' : '收藏' }}
							</button>
							<button 
								class="view-info-btn" 
								v-if="character.boxIds.length === 1"
								@click="goToBoxInfo(character.boxIds[0])"
							>
								查看盒数据
							</button>
							<button 
								class="view-info-btn" 
								v-if="character.boxIds.length > 1"
								@click="handleViewBoxData(character.name, character.boxIds)"
							>
								选择盒号
							</button>
						</view>
					</view>
					
					<!-- 市场价信息 -->
					<view class="market-price-section" v-if="character.market_price">
						<text class="market-price-title">市场价：</text>
						<view class="market-price-tags" v-if="showMarketPrice">
							<text 
								class="price-tag elite1-tag" 
								v-if="character.market_price.ELITE1"
							>
								精一 {{ character.market_price.ELITE1 }}元
							</text>
							<text 
								class="price-tag elite2-tag" 
								v-if="character.market_price.ELITE2 && !character.nolyELITE1"
							>
								精二 {{ character.market_price.ELITE2 }}元
							</text>
						</view>
						<!-- 市价关闭时的提示 -->
						<view class="price-disabled-hint" v-else>
							<text class="hint-text-small">市价信息已隐藏</text>
						</view>
					</view>
				</view>
			</scroll-view>
		</view>

		<!-- 盒号选择模态框 -->
		<view class="image-modal" v-if="showBoxSelectionModal" @tap="hideBoxSelectionModal">
			<view class="image-modal-content" @tap.stop>
				<view class="image-modal-header">
					<text class="image-modal-title">{{ modalTitle }}</text>
					<button class="close-btn" @click="hideBoxSelectionModal">×</button>
				</view>
				<scroll-view class="image-container" scroll-y>
					<view class="box-selection-list">
						<view 
							class="box-selection-item" 
							v-for="box in modalBoxList" 
							:key="box.boxId"
							@click="selectBox(box.boxId)"
						>
							<view class="box-selection-header">
								<view class="box-selection-left">
									<text class="box-selection-id">盒号 {{ box.boxId }}</text>
									<text class="box-selection-type" :class="getBoxTypeClass(box.boxType)">{{ getBoxTypeText(box.boxType) }}</text>
								</view>
								<button 
									class="favorite-btn-small" 
									:class="isFavoriteBox(box.boxId) ? 'favorited' : ''"
									@click.stop="toggleFavoriteBox(box.boxId)"
								>
									{{ isFavoriteBox(box.boxId) ? '★' : '☆' }}
								</button>
							</view>
							<view class="box-selection-characters">
								<text class="box-selection-character" v-for="char in box.characters" :key="char.name">
									{{ char.name }}
								</text>
							</view>
						</view>
					</view>
				</scroll-view>
				<view class="image-modal-footer">
					<text class="image-tip">请选择要查看的盒号</text>
				</view>
			</view>
		</view>

		<!-- 返回按钮 -->
		<view class="footer-actions">
			<button class="back-btn" @click="goBack">返回上一页</button>
		</view>
	</view>
</template>

<script>
	import errorLog from "@/utils/errorLog.js";
	export default {
		data() {
			return {
				activeCategory: 'box', // 'box' 或 'character'
				favoriteBoxes: [],
				favoriteCharacters: [],
				allBoxes: [],

				// 模态框相关
				showBoxSelectionModal: false,
				modalTitle: '',
				modalBoxList: [],

				// 默认头像
				defaultAvatar: '/static/default-avatar.png',

				// ============= 新增：市价展示功能 =============
				showMarketPrice: false, // 默认关闭市价展示
				hasMarketPriceData: false, // 是否有市价数据

				// 主题模式
				themeMode: 'simple'
			}
		},
		onLoad() {
			// 设置分享配置
			wx.showShareMenu({
				withShareTicket: true,
				menus: ['shareAppMessage', 'shareTimeline']
			});

			console.log('收藏页面加载');
			this.loadThemeSetting();
			this.loadFavorites();
		},
		onShow() {
			this.loadFavorites();
			this.loadMarketPriceSetting(); // 确保每次显示都重新加载市价设置
		},
		onShareAppMessage() {
			return {
				title: '方舟通行证谷子查询工具-我的收藏',
				path: '/packageB/pages/mystarbox/mystarbox',
				imageUrl: ''
			}
		},
		
		onShareTimeline() {
			return {
				title: '方舟通行证谷子查询工具-我的收藏',
				imageUrl: ''
			}
		},
		methods: {
			logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
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
						this.logError(e);
						console.error('加载主题设置失败:', e);
						this.themeMode = 'simple';
					}
				},

			// ============= 新增：市价相关方法 =============

			// 加载市价设置
			loadMarketPriceSetting() {
				try {
					// 首先尝试从本地存储读取设置
					const showMarketPrice = uni.getStorageSync('showMarketPrice');
					console.log('从本地存储读取市价设置:', showMarketPrice);
					
					// 如果没有设置过，默认false（关闭）
					if (showMarketPrice === undefined || showMarketPrice === null) {
						this.showMarketPrice = false;
						uni.setStorageSync('showMarketPrice', 'false'); // 确保有一个默认值
					} else {
						this.showMarketPrice = showMarketPrice === true || showMarketPrice === 'true';
					}
					
					console.log('加载后的市价设置:', this.showMarketPrice);
					
					// 检查是否有市价数据
					this.checkMarketPriceData();
				} catch (e) {
					this.logError(e);
					console.error('加载市价设置失败:', e);
					this.showMarketPrice = false; // 默认关闭
				}
			},
			
			// 检查是否有市价数据
			checkMarketPriceData() {
				// 如果数据为空，直接返回false
				if (!this.allBoxes || this.allBoxes.length === 0) {
					this.hasMarketPriceData = false;
					console.log('暂无数据，无法检查市价数据');
					return;
				}
				
				// 检查数据中是否有市价字段
				let foundMarketPrice = false;
				console.log('开始检查市价数据，数据长度:', this.allBoxes.length);
				
				// 检查前5个盒号
				for (let i = 0; i < Math.min(this.allBoxes.length, 5); i++) {
					const box = this.allBoxes[i];
					console.log(`检查第${i+1}个盒号:`, box.Box_id);
					
					// 直接检查干员字段
					for (let j = 1; j <= 10; j++) {
						const charKey = `character${j}`;
						if (box[charKey] && typeof box[charKey] === 'object') {
							const character = box[charKey];
							console.log(`  ${character.name}: market_price =`, character.market_price);
							
							if (character.market_price && 
								(character.market_price.ELITE1 || character.market_price.ELITE2)) {
								foundMarketPrice = true;
								console.log('发现市价数据:', character.market_price);
								break;
							}
						}
					}
					
					if (foundMarketPrice) break;
				}
				
				this.hasMarketPriceData = foundMarketPrice;
				console.log('检查市价数据结果:', this.hasMarketPriceData ? '有市价数据' : '无市价数据');
			},
			
			// 跳转到设置页面
			goToSettings() {
				uni.navigateTo({
					url: '/packageA/Setting/Setting'
				});
			},
			dismissMarketPriceTip() {
				this.hasMarketPriceData = false;
			},
			
			// 获取盒子的所有干员（修改后版本，包含hasMarketPrice标记）
			getBoxCharacters(box) {
				const characters = [];
				for (let i = 1; i <= 10; i++) {
					const charKey = `character${i}`;
					if (box[charKey]) {
						let characterName = '';
						let imageUrl = '';
						let nolyELITE1 = false;
						let hotcharacter = false;
						let market_price = null;
						let hasMarketPrice = false;
						
						if (typeof box[charKey] === 'string') {
							characterName = box[charKey];
							imageUrl = '';
							nolyELITE1 = false;
							hotcharacter = false;
							market_price = null;
							hasMarketPrice = false;
						} else if (box[charKey].name) {
							characterName = box[charKey].name;
							imageUrl = box[charKey].imageUrl || '';
							nolyELITE1 = box[charKey].nolyELITE1 === true;
							hotcharacter = box[charKey].hotcharacter === true;
							market_price = box[charKey].market_price || null;
							// 检查是否有市价数据
							hasMarketPrice = market_price && 
								(market_price.ELITE1 || market_price.ELITE2);
						}
						
						if (characterName && characterName.trim()) {
							characters.push({
								name: characterName,
								avatar: imageUrl || this.defaultAvatar,
								nolyELITE1: nolyELITE1,
								hotcharacter: hotcharacter,
								market_price: market_price,
								hasMarketPrice: hasMarketPrice
							});
						}
					}
				}
				return characters;
			},
			
			// 切换分类
			switchCategory(category) {
				this.activeCategory = category;
			},
			
			// 加载收藏数据
			loadFavorites() {
				try {
					// 获取所有盒号数据
					const allData = uni.getStorageSync('arknightsData') || [];
					const guessData = uni.getStorageSync('guessData') || [];
					const enableGuessData = uni.getStorageSync('enableGuessData') === 'true';
					
					this.allBoxes = [...allData];
					if (enableGuessData) {
						this.allBoxes = [...this.allBoxes, ...guessData];
					}

					// 获取收藏的盒号ID列表
					const favoriteBoxIds = uni.getStorageSync('favoriteBoxIds') || [];
					
					// 根据收藏ID过滤出收藏的盒号
					this.favoriteBoxes = this.allBoxes.filter(box => 
						favoriteBoxIds.includes(box.Box_id)
					);
					
					// 获取收藏的干员名列表
					const favoriteCharacterNames = uni.getStorageSync('favoriteCharacterNames') || [];
					
					// 构建收藏的干员数据（修改后版本，包含hasMarketPrice标记）
					this.favoriteCharacters = this.buildFavoriteCharacters(favoriteCharacterNames);
					
					console.log(`收藏统计: ${this.favoriteBoxes.length}个盒号, ${this.favoriteCharacters.length}个干员`);
					
					// 检查是否有市价数据
					this.checkMarketPriceData();
					
				} catch (error) {
					this.logError(error);
					console.error('加载收藏数据失败:', error);
					uni.showToast({
						title: '加载收藏失败',
						icon: 'none'
					});
				}
			},
			
			// 构建收藏的干员数据（修改后版本，包含hasMarketPrice标记）
			buildFavoriteCharacters(characterNames) {
				const characterMap = {};
				
				// 遍历所有盒号，找到收藏的干员
				this.allBoxes.forEach(box => {
					// 遍历盒中的干员
					for (let i = 1; i <= 10; i++) {
						const charKey = `character${i}`;
						if (box[charKey]) {
							let characterName = '';
							let imageUrl = '';
							let nolyELITE1 = false;
							let hotcharacter = false;
							let market_price = null;
							let hasMarketPrice = false;
							
							if (typeof box[charKey] === 'string') {
								characterName = box[charKey];
							} else if (box[charKey].name) {
								characterName = box[charKey].name;
								imageUrl = box[charKey].imageUrl || '';
								nolyELITE1 = box[charKey].nolyELITE1 === true;
								hotcharacter = box[charKey].hotcharacter === true;
								market_price = box[charKey].market_price || null;
								// 检查是否有市价数据
								hasMarketPrice = market_price && 
									(market_price.ELITE1 || market_price.ELITE2);
							}
							
							// 如果是收藏的干员
							if (characterName && characterNames.includes(characterName)) {
								if (!characterMap[characterName]) {
									characterMap[characterName] = {
										name: characterName,
										avatar: imageUrl || this.defaultAvatar,
										boxIds: [],
										nolyELITE1: nolyELITE1,
										hotcharacter: hotcharacter,
										market_price: market_price,
										hasMarketPrice: hasMarketPrice
									};
								}
								
								// 添加盒号
								if (!characterMap[characterName].boxIds.includes(box.Box_id)) {
									characterMap[characterName].boxIds.push(box.Box_id);
								}
								
								// 更新头像（如果有更好的头像）
								if (characterMap[characterName].avatar === this.defaultAvatar && imageUrl) {
									characterMap[characterName].avatar = imageUrl;
								}
								
								// 更新标记
								if (nolyELITE1) characterMap[characterName].nolyELITE1 = true;
								if (hotcharacter) characterMap[characterName].hotcharacter = true;
								if (market_price) characterMap[characterName].market_price = market_price;
								if (hasMarketPrice) characterMap[characterName].hasMarketPrice = true;
							}
						}
					}
				});
				
				// 转换为数组并排序
				return Object.values(characterMap).sort((a, b) => a.name.localeCompare(b.name, 'zh-CN'));
			},
			
			// 头像加载失败处理
			onAvatarError(boxId, charIndex) {
				console.log(`头像加载失败: 盒${boxId}干员${charIndex}`);
			},
			
			onCharacterAvatarError(index) {
				if (this.favoriteCharacters[index]) {
					this.$set(this.favoriteCharacters[index], 'avatar', this.defaultAvatar);
				}
			},
			
			// 获取盒子类型文本
			getBoxTypeText(boxType) {
				const typeMap = {
					'whitelist': '白名单凭证',
					'special': '特别通行认证',
					'cooperation': '联动款',
					'ambience': '音律联觉通行证',
					'normal': '常规款'
				};
				return typeMap[boxType] || '常规款';
			},
			
			// 获取盒子类型样式类
			getBoxTypeClass(boxType) {
				const classMap = {
					'whitelist': 'type-whitelist',
					'special': 'type-special',
					'cooperation': 'type-cooperation',
					'ambience': 'type-ambience',
					'normal': 'type-normal'
				};
				return classMap[boxType] || 'type-normal';
			},
			
			// 跳转到盒详情页面
			goToBoxInfo(boxId) {
				uni.navigateTo({
					url: `/pages/box_info/box_info?boxId=${boxId}`
				});
			},
			
			// 处理干员点击
			handleCharacterClick(characterName) {
				// 可以跳转到搜索页面搜索该干员
				uni.navigateTo({
					url: `/pages/search/search?searchText=${encodeURIComponent(characterName)}&searchType=character`
				});
			},
			
			// 盒号收藏相关方法
			isFavoriteBox(boxId) {
				const favoriteBoxIds = uni.getStorageSync('favoriteBoxIds') || [];
				return favoriteBoxIds.includes(boxId);
			},
			
			toggleFavoriteBox(boxId) {
				try {
					const favoriteBoxIds = uni.getStorageSync('favoriteBoxIds') || [];
					const index = favoriteBoxIds.indexOf(boxId);
					
					if (index > -1) {
						// 取消收藏
						favoriteBoxIds.splice(index, 1);
						uni.showToast({
							title: '已取消收藏',
							icon: 'success'
						});
					} else {
						// 添加收藏
						favoriteBoxIds.push(boxId);
						uni.showToast({
							title: '收藏成功',
							icon: 'success'
						});
					}
					
					uni.setStorageSync('favoriteBoxIds', favoriteBoxIds);
					
					// 重新加载收藏数据
					this.loadFavorites();
					
				} catch (error) {
					this.logError(error);
					console.error('操作失败:', error);
					uni.showToast({
						title: '操作失败',
						icon: 'none'
					});
				}
			},
			
			// 干员收藏相关方法
			isFavoriteCharacter(characterName) {
				const favoriteCharacterNames = uni.getStorageSync('favoriteCharacterNames') || [];
				return favoriteCharacterNames.includes(characterName);
			},
			
			toggleFavoriteCharacter(characterName) {
				try {
					const favoriteCharacterNames = uni.getStorageSync('favoriteCharacterNames') || [];
					const index = favoriteCharacterNames.indexOf(characterName);
					
					if (index > -1) {
						// 取消收藏
						favoriteCharacterNames.splice(index, 1);
						uni.showToast({
							title: '已取消收藏',
							icon: 'success'
						});
					} else {
						// 添加收藏
						favoriteCharacterNames.push(characterName);
						uni.showToast({
							title: '收藏成功',
							icon: 'success'
						});
					}
					
					uni.setStorageSync('favoriteCharacterNames', favoriteCharacterNames);
					
					// 重新加载收藏数据
					this.loadFavorites();
					
				} catch (error) {
					this.logError(error);
					console.error('操作失败:', error);
					uni.showToast({
						title: '操作失败',
						icon: 'none'
					});
				}
			},
			
			// 模态框相关方法
			handleViewBoxData(characterName, boxIds) {
				if (boxIds.length === 1) {
					this.goToBoxInfo(boxIds[0]);
					return;
				}
				this.showBoxSelectionModal = true;
				this.modalTitle = `选择 ${characterName} 所在的盒号`;
				this.modalBoxList = this.getBoxDetails(boxIds);
			},
			
			getBoxDetails(boxIds) {
				return boxIds.map(boxId => {
					const box = this.allBoxes.find(item => item.Box_id === boxId);
					if (box) {
						const characters = [];
						for (let i = 1; i <= 10; i++) {
							const charKey = `character${i}`;
							if (box[charKey]) {
								let characterName = '';
								if (typeof box[charKey] === 'string') {
									characterName = box[charKey];
								} else if (box[charKey].name) {
									characterName = box[charKey].name;
								}
								if (characterName && characterName.trim()) {
									characters.push({
										name: characterName
									});
								}
							}
						}
						return {
							boxId: boxId,
							boxType: box.Box_type || 'normal',
							characters: characters
						};
					}
					return {
						boxId: boxId,
						boxType: 'normal',
						characters: []
					};
				});
			},
			
			selectBox(boxId) {
				this.hideBoxSelectionModal();
				this.goToBoxInfo(boxId);
			},
			
			hideBoxSelectionModal() {
				this.showBoxSelectionModal = false;
				this.modalTitle = '';
				this.modalBoxList = [];
			},
			
			// 导航方法
			navigateTo(url) {
				uni.navigateTo({
					url: url
				});
			},
			
			// 显示清空收藏确认
			showClearConfirm() {
				const content = this.activeCategory === 'box' 
					? '确定要清空所有收藏的盒号吗？此操作不可撤销。'
					: '确定要清空所有收藏的干员吗？此操作不可撤销。';
				
				uni.showModal({
					title: '确认清空收藏',
					content: content,
					confirmColor: '#FA5151',
					success: (res) => {
						if (res.confirm) {
							this.clearAllFavorites();
						}
					}
				});
			},
			
			// 清空所有收藏
			clearAllFavorites() {
				try {
					if (this.activeCategory === 'box') {
						uni.setStorageSync('favoriteBoxIds', []);
						this.favoriteBoxes = [];
					} else {
						uni.setStorageSync('favoriteCharacterNames', []);
						this.favoriteCharacters = [];
					}
					
					uni.showToast({
						title: '已清空收藏',
						icon: 'success'
					});
				} catch (error) {
					this.logError(error);
					console.error('清空收藏失败:', error);
					uni.showToast({
						title: '清空收藏失败',
						icon: 'none'
					});
				}
			},
			
			// 返回上一页
			goBack() {
				uni.navigateBack();
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

	/* 市价状态提示区域样式 */
	.market-price-status-section {
		display: flex;
		align-items: center;
		background-color: #fff;
		border-radius: 16rpx;
		padding: 16rpx 24rpx;
		margin-bottom: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		gap: 12rpx;
	}

	.mp-close-btn {
		font-size: 28rpx;
		color: #C0C4CC;
		flex-shrink: 0;
		padding: 0 8rpx;
	}

	.mp-status-text {
		font-size: 26rpx;
		font-weight: 500;
		flex-shrink: 0;
	}

	.mp-status-text.mp-enabled {
		color: #67C23A;
	}

	.mp-status-text.mp-disabled {
		color: #E6A23C;
	}

	.mp-tip {
		font-size: 24rpx;
		color: #999;
	}

	.mp-settings-btn {
		font-size: 24rpx;
		color: #409EFF;
		flex-shrink: 0;
		margin-left: auto;
	}
	
	/* 市价提示信息样式 */
	.market-price-hint {
	  background-color: #e8f4ff;
	  border-radius: 12rpx;
	  padding: 15rpx 20rpx;
	  margin-bottom: 20rpx;
	  display: flex;
	  align-items: center;
	  gap: 10rpx;
	}
	
	.hint-text {
	  font-size: 24rpx;
	  color: #409EFF;
	  line-height: 1.4;
	}

	/* 分类选项 */
	.category-options {
		display: flex;
		justify-content: center;
		margin-bottom: 30rpx;
		gap: 20rpx;
	}

	.category-btn {
		flex: 1;
		background-color: #f0f0f0;
		color: #666;
		font-size: 28rpx;
		border-radius: 50rpx;
		padding: 20rpx 0;
		font-weight: 500;
		transition: all 0.3s ease;
	}

	.category-btn.active {
		background-color: #409EFF;
		color: #fff;
	}

	.category-btn:active {
		transform: scale(0.98);
	}

	.stats-section {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background-color: #fff;
		border-radius: 16rpx;
		padding: 20rpx 30rpx;
		margin-bottom: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
	}

	.stats-text {
		font-size: 28rpx;
		color: #333;
		font-weight: bold;
	}

	.clear-all-btn {
		background-color: #FA5151;
		color: #fff;
		font-size: 24rpx;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
	}

	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 100rpx 0;
		background-color: #fff;
		border-radius: 16rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}

	.empty-icon {
		width: 120rpx;
		height: 120rpx;
		margin-bottom: 30rpx;
		opacity: 0.5;
	}

	.empty-text {
		font-size: 32rpx;
		color: #999;
		margin-bottom: 15rpx;
	}

	.empty-tip {
		font-size: 26rpx;
		color: #ccc;
		margin-bottom: 40rpx;
		text-align: center;
	}

	.empty-actions {
		display: flex;
		gap: 20rpx;
	}

	.empty-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		font-size: 26rpx;
		padding: 15rpx 30rpx;
	}

	.favorites-section {
		background-color: #fff;
		border-radius: 16rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		overflow: hidden;
		margin-bottom: 30rpx;
		max-height: 70vh;
	}

	.favorites-list {
		max-height: 70vh;
		padding: 20rpx;
	}

	.favorite-item {
		border-bottom: 1rpx solid #f0f0f0;
		padding: 20rpx 0;
	}

	.favorite-item:last-child {
		border-bottom: none;
	}

	/* 盒号头部 */
	.box-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 15rpx;
		flex-wrap: wrap;
		gap: 15rpx;
	}

	.box-header-left {
		display: flex;
		align-items: center;
		gap: 15rpx;
		flex: 1;
	}

	.box-id {
		font-size: 28rpx;
		color: #666;
		font-weight: bold;
	}

	.box-type {
		font-size: 22rpx;
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

	.box-header-right {
		display: flex;
		align-items: center;
		gap: 10rpx;
	}

	.favorite-btn {
		background-color: #f0f0f0;
		color: #666;
		font-size: 24rpx;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
		transition: all 0.3s;
		white-space: nowrap;
	}

	.favorite-btn.favorited {
		background-color: #E6A23C;
		color: #fff;
	}

	.view-info-btn {
		background-color: #409EFF;
		color: #fff;
		font-size: 24rpx;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
		white-space: nowrap;
	}

	/* 干员网格 */
	.characters-grid {
		display: grid;
		grid-template-columns: repeat(2, 1fr);
		gap: 20rpx;
	}

	.character-card {
		display: flex;
		align-items: center;
		background-color: #f8fafc;
		border-radius: 12rpx;
		padding: 20rpx;
		transition: all 0.3s;
		position: relative;
		cursor: pointer;
	}

	.character-card:active {
		background-color: #e8f4ff;
		transform: scale(0.98);
	}

	/* 干员头像容器，用于放置hot图标和收藏指示器 */
	.character-avatar-container {
		position: relative;
		margin-right: 20rpx;
		flex-shrink: 0;
	}

	.character-avatar {
		width: 80rpx;
		height: 80rpx;
		border-radius: 8rpx;
		border: 1rpx solid #e0e0e0;
	}

	/* 热门干员图标样式 */
	.hot-character-icon {
		position: absolute;
		top: -8rpx;
		right: -8rpx;
		width: 30rpx;
		height: 30rpx;
		z-index: 2;
	}

	/* 收藏指示器 */
	.favorite-indicator {
		position: absolute;
		bottom: -5rpx;
		right: -5rpx;
		width: 28rpx;
		height: 28rpx;
		background-color: #f0f0f0;
		color: #666;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 18rpx;
		border: 2rpx solid #fff;
		z-index: 3;
		transition: all 0.3s;
	}
	
	.favorite-indicator.favorited {
		background-color: #E6A23C;
		color: #fff;
	}

	.character-info {
		flex: 1;
	}

	.character-name-line {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		margin-bottom: 8rpx;
	}

	.character-name {
		font-size: 28rpx;
		color: #333;
		margin-right: 10rpx;
		font-weight: bold;
	}

	.elite-tag {
		background-color: #FF6B6B;
		color: #fff;
		font-size: 20rpx;
		padding: 4rpx 8rpx;
		border-radius: 4rpx;
		font-weight: bold;
	}

	.market-price-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 8rpx;
	}

	.price-tag {
		font-size: 20rpx;
		padding: 4rpx 8rpx;
		border-radius: 4rpx;
		color: #fff;
		font-weight: bold;
	}

	.elite1-tag {
		background-color: #E6A23C;
	}

	.elite2-tag {
		background-color: #409EFF;
	}
	
	/* 市价关闭时的提示 */
	.price-disabled-hint {
		margin-top: 5rpx;
	}
	
	.hint-text-small {
		font-size: 22rpx;
		color: #999;
		font-style: italic;
	}

	/* 干员收藏项 */
	.character-favorite-item {
		border-bottom: 1rpx solid #f0f0f0;
		padding: 20rpx 0;
	}

	.character-favorite-item:last-child {
		border-bottom: none;
	}

	.character-result-header {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		gap: 20rpx;
		margin-bottom: 15rpx;
	}

	.character-with-avatar {
		display: flex;
		align-items: flex-start;
		flex: 1;
	}

	.label {
		font-size: 28rpx;
		color: #666;
		margin-right: 10rpx;
	}

	.value {
		font-size: 28rpx;
		color: #409EFF;
		font-weight: bold;
		margin-right: 20rpx;
	}

	.character-favorite-actions {
		display: flex;
		flex-direction: column;
		gap: 10rpx;
		align-items: flex-end;
	}

	.market-price-section {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 10rpx;
		padding: 15rpx;
		background-color: #f8fafc;
		border-radius: 8rpx;
	}

	.market-price-title {
		font-size: 26rpx;
		color: #333;
		font-weight: bold;
	}

	/* 模态框样式 */
	.image-modal {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: rgba(0, 0, 0, 0.8);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 9999;
		padding: 40rpx;
	}

	.image-modal-content {
		background-color: #fff;
		border-radius: 16rpx;
		width: 100%;
		max-width: 700rpx;
		max-height: 90vh;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}

	.image-modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 30rpx;
		border-bottom: 1rpx solid #f0f0f0;
	}

	.image-modal-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #333;
		display: block;
		flex: 1;
	}

	.close-btn {
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

	.close-btn:active {
		background-color: #e0e0e0;
	}

	.image-container {
		flex: 1;
		padding: 30rpx;
		max-height: 70vh;
	}

	.box-selection-list {
		display: flex;
		flex-direction: column;
		gap: 20rpx;
	}

	.box-selection-item {
		background-color: #f8fafc;
		border-radius: 12rpx;
		padding: 20rpx;
		border: 1rpx solid #e0e0e0;
	}

	.box-selection-item:active {
		background-color: #e8f4ff;
	}

	.box-selection-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 10rpx;
	}

	.box-selection-left {
		display: flex;
		align-items: center;
		gap: 15rpx;
		flex: 1;
	}

	.box-selection-id {
		font-size: 28rpx;
		font-weight: bold;
		color: #409EFF;
	}

	.box-selection-type {
		font-size: 22rpx;
		padding: 6rpx 12rpx;
		border-radius: 6rpx;
		color: #fff;
	}

	.favorite-btn-small {
		background-color: #f0f0f0;
		color: #666;
		font-size: 24rpx;
		width: 60rpx;
		height: 60rpx;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		transition: all 0.3s;
	}

	.favorite-btn-small.favorited {
		background-color: #E6A23C;
		color: #fff;
	}

	.box-selection-characters {
		display: flex;
		flex-wrap: wrap;
		gap: 10rpx;
	}

	.box-selection-character {
		background-color: #e8f4ff;
		color: #409EFF;
		padding: 6rpx 12rpx;
		border-radius: 6rpx;
		font-size: 22rpx;
	}

	.image-modal-footer {
		padding: 20rpx 30rpx;
		border-top: 1rpx solid #f0f0f0;
		text-align: center;
	}

	.image-tip {
		font-size: 24rpx;
		color: #999;
	}

	/* 返回按钮 */
	.footer-actions {
		text-align: center;
	}

	.back-btn {
		background-color: #909399;
		color: #fff;
		border-radius: 50rpx;
		font-size: 28rpx;
		padding: 20rpx 60rpx;
	}

	/* 响应式调整 */
	@media (min-width: 750rpx) {
		.characters-grid {
			grid-template-columns: repeat(3, 1fr);
		}
	}

	@media (min-width: 1000rpx) {
		.characters-grid {
			grid-template-columns: repeat(5, 1fr);
		}
	}

	/* 响应式调整 - 小屏幕 */
	@media (max-width: 750rpx) {
		.container {
			padding: 20rpx;
		}

		.category-options {
			flex-direction: column;
			gap: 15rpx;
		}

		.box-header {
			flex-direction: column;
			align-items: flex-start;
		}

		.box-header-left {
			width: 100%;
			margin-bottom: 10rpx;
		}

		.box-header-right {
			flex-direction: row;
			width: 100%;
			justify-content: flex-end;
		}

		.character-result-header {
			flex-direction: column;
		}

		.character-with-avatar {
			width: 100%;
			margin-bottom: 15rpx;
		}

		.character-favorite-actions {
			flex-direction: row;
			width: 100%;
			justify-content: flex-end;
		}

		.favorite-btn, .view-info-btn {
			font-size: 22rpx;
			padding: 10rpx 15rpx;
		}

		.characters-grid {
			grid-template-columns: repeat(2, 1fr);
		}

		.image-modal {
			padding: 20rpx;
		}

		.image-modal-content {
			max-width: 100%;
		}
	}

	/* ========== 科技风格 ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
	}
	.container.theme-ark .market-price-status-section,
	.container.theme-ark .favorite-section,
	.container.theme-ark .category-tabs,
	.container.theme-ark .tab-item.active,
	.container.theme-ark .empty-state,
	.container.theme-ark .market-price-hint {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border: 1rpx solid #2A2A4A;
	}
	.container.theme-ark .status-text { color: #FF6B35; }
	.container.theme-ark .tab-item { color: #888; }
	.container.theme-ark .box-card,
	.container.theme-ark .character-card { background: rgba(22,22,42,0.6); border-color: #2A2A4A; }
	.container.theme-ark .box-name,
	.container.theme-ark .character-name { color: #e0e0e0; }
	.container.theme-ark .character-meta { color: #888; }
	.container.theme-ark .box-id { color: #FF6B35; }
	.container.theme-ark .favorite-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .favorite-btn.favorited { background: rgba(255,107,53,0.15); color: #FF6B35; }
	.container.theme-ark .hint-text { color: #FF6B35; }
	.container.theme-ark .modal-content { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border: 1rpx solid #2A2A4A; }
	.container.theme-ark .modal-title { color: #FF6B35; }
	.container.theme-ark .close-btn { background: rgba(255,107,53,0.1); color: #FF6B35; }
	.container.theme-ark .view-info-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .character-avatar { border-color: #2A2A4A; }
	.container.theme-ark .mp-settings-btn { color: #FF6B35; }
	.container.theme-ark .box-list-item { border-bottom-color: #2A2A4A; }
	.container.theme-ark .box-list-title { color: #FF6B35; }
	.container.theme-ark .empty-icon { filter: brightness(0) invert(1); opacity: 0.4; }

	/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .market-price-status-section,
	.container.theme-jieyuan .favorite-section,
	.container.theme-jieyuan .category-tabs,
	.container.theme-jieyuan .tab-item.active,
	.container.theme-jieyuan .empty-state,
	.container.theme-jieyuan .market-price-hint {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .mp-status-text { color: #399383; }
	.container.theme-jieyuan .mp-settings-btn { color: #e25884; }
	.container.theme-jieyuan .tab-item { color: #888; }
	.container.theme-jieyuan .tab-item.active { color: #399383; }
	.container.theme-jieyuan .box-card,
	.container.theme-jieyuan .character-card { background: rgba(0,0,0,0.03); border-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .box-name,
	.container.theme-jieyuan .character-name { color: #333; }
	.container.theme-jieyuan .character-meta { color: #888; }
	.container.theme-jieyuan .box-id { color: #399383; }
	.container.theme-jieyuan .favorite-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .favorite-btn.favorited { background: rgba(226, 88, 132, 0.15); color: #e25884; }
	.container.theme-jieyuan .hint-text { color: #e25884; }
	.container.theme-jieyuan .modal-content { background: rgba(255, 255, 255, 0.95); border: 1rpx solid rgba(226, 88, 132, 0.15); }
	.container.theme-jieyuan .modal-title { color: #399383; }
	.container.theme-jieyuan .close-btn { background: rgba(226, 88, 132, 0.1); color: #e25884; }
</style>
