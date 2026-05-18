<template>
	<view :class="['container', 'theme-' + themeMode]">
		<!-- 筛选和排序控制区域 -->
		<view class="toolbar-toggle" @click="toggleToolbar">
			<text>{{ toolbarCollapsed ? '▼ 展开' : '▲ 收起' }}</text>
		</view>
		
		<view class="control-section"
				v-show="!toolbarCollapsed">
			<!-- 盒类型筛选 -->
			<view class="filter-section">
				<text class="section-title">盒类型筛选：</text>
				<view class="filter-controls">
					<picker 
						class="filter-picker" 
						@change="onFilterChange" 
						:value="filterIndex" 
						:range="filterOptions"
					>
						<view class="picker-text">{{ filterOptions[filterIndex] }}</view>
					</picker>
				</view>
			</view>
			
			<!-- 排序选项 -->
			<view class="sort-section">
				<text class="section-title">排序方式：</text>
				<view class="sort-controls">
					<picker 
						class="sort-picker" 
						@change="onSortChange" 
						:value="sortIndex" 
						:range="sortOptions"
					>
						<view class="picker-text">{{ sortOptions[sortIndex] }}</view>
					</picker>
					<button 
						class="sort-order-btn" 
						@click="toggleSortOrder"
					>
						{{ sortOrder === 'asc' ? '升序 ↑' : '降序 ↓' }}
					</button>
				</view>
			</view>
			
			<!-- 新增：高级筛选按钮 -->
			<button class="advanced-filter-btn" @click="openAdvancedFilter">高级筛选</button>
		</view>
		
		<!-- 市价状态提示 -->
		<view class="market-price-status-section" v-if="hasMarketPriceData">
			<text class="mp-close-btn" @click="dismissMarketPriceTip">✕</text>
			<text class="mp-status-text" :class="showMarketPrice ? 'mp-enabled' : 'mp-disabled'">
				{{ showMarketPrice ? '✓ 正在显示市价信息' : '○ 市价信息已隐藏' }}
			</text>
			<text class="mp-tip" v-if="!showMarketPrice">（前往设置页面开启）</text>
			<text class="mp-settings-btn" v-if="!showMarketPrice" @click="goToSettings">去设置</text>
		</view>
		
		<!-- 分页控制 -->
		<view class="pagination-section">
			<view class="pagination-info">
				<text class="page-text">第 {{ currentPage }} 页 / 共 {{ totalPages }} 页</text>
				<text class="data-count">共 {{ filteredData.length }} 盒 (筛选后)</text>
				<!-- 显示当前高级筛选条件（可选） -->
				<text v-if="hasAdvancedFilter" class="advanced-filter-tip">高级筛选已应用</text>
			</view>
			
			<view class="pagination-controls">
				<button 
					class="page-btn" 
					:disabled="currentPage <= 1"
					@click="goToFirstPage"
				>
					首页
				</button>
				<button 
					class="page-btn" 
					:disabled="currentPage <= 1"
					@click="prevPage"
				>
					上一页
				</button>
				<button 
					class="page-btn" 
					:disabled="currentPage >= totalPages"
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
		</view>
		
		<!-- 数据状态提示 -->
		<view class="data-status" v-if="dataStatusMessage">
			<text>{{ dataStatusMessage }}</text>
			<button 
				v-if="dataStatusMessage && (dataStatusMessage.includes('失败') || dataStatusMessage.includes('错误'))" 
				class="update-btn small" 
				@click="loadData"
			>
				重试加载
			</button>
		</view>
		
		<!-- 市价提示信息（只在开启时显示） -->
		<view class="market-price-hint" v-if="showMarketPrice && hasMarketPriceData">
		  <text class="hint-text">💡 市价信息来源于千岛APP，数据仅供参考，请以实际为准</text>
		</view>
		
		<!-- 盒号与干员列表 -->
		<view class="boxes-list" v-if="currentPageData.length > 0">
			<view 
				class="box-item" 
				v-for="(box, index) in currentPageData" 
				:key="index"
			>
				<view class="box-header">
					<view class="box-header-left">
						<text class="box-id">盒号 {{ box.Box_id }}</text>
						<text class="box-type" :class="getBoxTypeClass(box.Box_type)">{{ getBoxTypeText(box.Box_type) }}</text>
					</view>
					<view class="box-header-right">
						<!-- 收藏盒内干员按钮 -->
						<button 
							class="favorite-characters-btn" 
							@click="openBoxCharactersModal(box.Box_id, getBoxCharacters(box))"
						>
							收藏
						</button>
						<!-- 查看详情按钮 -->
						<button 
							class="view-info-btn" 
							@click="goToBoxInfo(box.Box_id)"
						>
							本盒信息
						</button>
					</view>
				</view>
				
				<view class="characters-grid">
					<view 
						class="character-card" 
						v-for="(character, charIndex) in getBoxCharacters(box)" 
						:key="charIndex"
						@click="toggleFavoriteCharacterDirectly(character.name)"
					>
						<view class="character-avatar-container">
							<image 
								class="character-avatar" 
								:src="character.avatar" 
								mode="aspectFit" 
								@error="onAvatarError(box.Box_id, charIndex)"
								@load="onAvatarLoad(box.Box_id, charIndex)"
							></image>
							<!-- 热门干员图标 -->
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
								<!-- 仅精一标签 -->
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
		</view>
		
		<!-- 空状态 -->
		<view class="empty-state" v-if="filteredData.length === 0 && !dataStatusMessage.includes('正在')">
			<image class="empty-icon" src="/static/empty-icon.png"></image>
			<text class="empty-text">暂无数据</text>
			<button class="update-btn" @click="loadData">加载数据</button>
		</view>
		
		<!-- 页脚信息 -->
		<view class="footer">
			<text class="update-info" v-if="localUpdateTime">方舟通行证谷子查询工具</text>
			<text class="update-info" v-if="cloudUpdateTime">本工具为玩家自制，与官方无关</text>
			<text class="version-info" v-if="currentVersion">数据版本: {{ currentVersion }}</text>
		</view>
		
		<!-- 盒内干员收藏模态框 -->
		<view class="image-modal" v-if="showBoxCharactersModal" @tap="hideBoxCharactersModal">
			<view class="image-modal-content" @tap.stop>
				<view class="image-modal-header">
					<text class="image-modal-title">收藏盒号 {{ currentBoxId }} 内的干员</text>
					<button class="close-btn" @click="hideBoxCharactersModal">×</button>
				</view>
				<scroll-view class="image-container" scroll-y>
					<view class="box-characters-list">
						<view 
							class="box-character-item" 
							v-for="(character, index) in modalBoxCharacters" 
							:key="index"
						>
							<view class="box-character-info">
								<view class="character-avatar-container">
									<image 
										class="character-avatar" 
										:src="character.avatar" 
										mode="aspectFit"
										@error="onModalAvatarError(index)"
									></image>
									<image 
										v-if="character.hotcharacter" 
										class="hot-character-icon" 
										src="/static/hot.png" 
										mode="aspectFit"
									></image>
								</view>
								<view class="character-details">
									<view class="character-name-line">
										<text class="character-name">{{ character.name }}</text>
										<text v-if="character.nolyELITE1" class="elite-tag">仅精一</text>
									</view>
									<!-- 模态框中的市场价显示 - 同样受showMarketPrice控制 -->
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
									<!-- 模态框中的市价关闭提示 -->
									<view class="price-disabled-hint" v-else-if="character.hasMarketPrice && !showMarketPrice">
										<text class="hint-text-small">市价信息已隐藏</text>
									</view>
								</view>
							</view>
							<button 
								class="favorite-btn" 
								:class="isFavoriteCharacter(character.name) ? 'favorited' : ''"
								@click="toggleFavoriteCharacter(character.name)"
							>
								{{ isFavoriteCharacter(character.name) ? '已收藏' : '收藏' }}
							</button>
						</view>
					</view>
				</scroll-view>
				<view class="image-modal-footer">
					<view class="modal-footer-actions">
						<button class="select-all-btn" @click.stop="selectAllCharacters">
							{{ isAllSelected ? '取消全选' : '全选' }}
						</button>
						<button 
							class="favorite-box-btn" 
							:class="isFavorite(currentBoxId) ? 'favorited' : ''"
							@click.stop="toggleFavorite(currentBoxId)"
						>
							{{ isFavorite(currentBoxId) ? '已收藏本盒' : '收藏本盒' }}
						</button>
					</view>
					<text class="image-tip">点击干员右侧的收藏按钮进行收藏</text>
				</view>
			</view>
		</view>
		
		<!-- ============= 新增：高级筛选模态框 ============= -->
		<view class="advanced-filter-modal" v-if="showAdvancedFilter" @tap="closeAdvancedFilter">
			<view class="modal-content" @tap.stop>
				<view class="modal-header">
					<text class="modal-title">高级筛选</text>
					<button class="close-btn" @click="closeAdvancedFilter">×</button>
				</view>
				<scroll-view class="modal-body" scroll-y>
					<!-- 发布时间筛选 -->
					<view class="filter-group">
						<text class="filter-group-title">发布时间</text>
						<view class="date-pickers">
							<picker class="date-picker" :range="yearOptions" @change="onYearChange" :value="filterYearIndex">
								<view class="picker-text">{{ filterYearText }}</view>
							</picker>
							<picker class="date-picker" :range="monthOptions" @change="onMonthChange" :value="filterMonthIndex">
								<view class="picker-text">{{ filterMonthText }}</view>
							</picker>
							<picker class="date-picker" :range="dayOptions" @change="onDayChange" :value="filterDayIndex">
								<view class="picker-text">{{ filterDayText }}</view>
							</picker>
						</view>
					</view>
					
					<!-- 复刻状态筛选 -->
					<view class="filter-group">
						<text class="filter-group-title">复刻状态</text>
						<view class="radio-group">
							<label class="radio-label" @click="filterReplicate = ''">
								<text class="radio" :class="{ active: filterReplicate === '' }">●</text>
								<text>全部</text>
							</label>
							<label class="radio-label" @click="filterReplicate = 'true'">
								<text class="radio" :class="{ active: filterReplicate === 'true' }">●</text>
								<text>有复刻</text>
							</label>
							<label class="radio-label" @click="filterReplicate = 'false'">
								<text class="radio" :class="{ active: filterReplicate === 'false' }">●</text>
								<text>无复刻</text>
							</label>
						</view>
					</view>
					
					<!-- 盲抽/单领筛选 -->
					<view class="filter-group">
						<text class="filter-group-title">盲抽/单领</text>
						<view class="radio-group">
							<label class="radio-label" @click="filterType = ''">
								<text class="radio" :class="{ active: filterType === '' }">●</text>
								<text>全部</text>
							</label>
							<label class="radio-label" @click="filterType = 'true'">
								<text class="radio" :class="{ active: filterType === 'true' }">●</text>
								<text>盲抽</text>
							</label>
							<label class="radio-label" @click="filterType = 'false'">
								<text class="radio" :class="{ active: filterType === 'false' }">●</text>
								<text>单领/赠品</text>
							</label>
						</view>
					</view>
				</scroll-view>
				<view class="modal-footer">
					<button class="reset-btn" @click="resetAdvancedFilter">重置</button>
					<button class="cancel-btn" @click="closeAdvancedFilter">取消</button>
					<button class="confirm-btn" @click="applyAdvancedFilter">确定</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	// 开源版本：知晓云配置已禁用，使用 GitHub 直链
	const GITHUB_DATA_SOURCES = {
		boxIdUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/Box_Id.json'
	};
	
	// 知晓云配置 - 开源版本clientId为空，商业版请配置知晓云
	const KNOW_CLOUD_CONFIG = {
		clientId: '',
		baseUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main',
		tableNames: {
			Version: ''          // 开源版本使用 GitHub 直链
		}
	}
	
	export default {
		data() {
			return {
				// 主题模式
				themeMode: 'simple',
				toolbarCollapsed: false,
				characterData: [],
				filteredData: [],
				currentPage: 1,
				pageSize: 5,
				dataStatusMessage: '',
				cloudUpdateTime: '',
				localUpdateTime: '',
				currentVersion: '',
				dataUrl: '',
				defaultAvatar: '/static/default-avatar.png',
				avatarCache: {},
				filterIndex: 0,
				filterOptions: ['全部盒类型', '常规款', '白名单凭证', '特别通行认证', '联动款', '音律联觉通行证'],
				filterMap: {
					0: 'all',
					1: 'normal',
					2: 'whitelist',
					3: 'special',
					4: 'cooperation',
					5: 'ambience'
				},
				sortIndex: 0,
				sortOrder: 'asc',
				sortOptions: ['按盒号排序', '按干员数量排序', '按发布时间排序'], // 新增按发布时间排序
				
				// 收藏数据
				favoriteBoxIds: [],
				favoriteCharacterNames: [],
				
				// 盒内干员收藏模态框数据
				showBoxCharactersModal: false,
				currentBoxId: '',
				modalBoxCharacters: [],
				
				// 知晓云重试计数
				retryCount: 0,
				latestVersion: '', // 存储最新的云端版本
				
				// 市价展示功能
				showMarketPrice: false,
				hasMarketPriceData: false,
				
				// ============= 新增：高级筛选相关数据 =============
				showAdvancedFilter: false,           // 是否显示高级筛选模态框
				// 发布时间筛选值
				filterYear: '',                       // 选中的年份，如'2024'
				filterMonth: '',                       // 选中的月份，如'05'
				filterDay: '',                         // 选中的日期，如'01'
				filterYearIndex: 0,                     // 年份选择器索引
				filterMonthIndex: 0,                     // 月份选择器索引
				filterDayIndex: 0,                       // 日期选择器索引
				yearOptions: ['全部年份'],                // 年份选项
				monthOptions: ['全部月份'],               // 月份选项
				dayOptions: ['全部日期'],                  // 日期选项
				// 复刻状态筛选
				filterReplicate: '',                     // ''全部，'true'有复刻，'false'无复刻
				// 盲抽/单领筛选
				filterType: '',                           // ''全部，'true'盲抽，'false'单领/赠品
			}
		},
		computed: {
			totalPages() {
				return Math.ceil(this.filteredData.length / this.pageSize);
			},
			currentPageData() {
				const startIndex = (this.currentPage - 1) * this.pageSize;
				const endIndex = startIndex + this.pageSize;
				return this.filteredData.slice(startIndex, endIndex);
			},
			currentFilter() {
				return this.filterMap[this.filterIndex] || 'all';
			},
			// 全选状态计算
			isAllSelected() {
				if (this.modalBoxCharacters.length === 0) return false;
				return this.modalBoxCharacters.every(character => 
					this.isFavoriteCharacter(character.name)
				);
			},
			// 是否有高级筛选条件
			hasAdvancedFilter() {
				return this.filterYear !== '' || this.filterMonth !== '' || this.filterDay !== '' ||
				       this.filterReplicate !== '' || this.filterType !== '';
			},
			// 年份选择器显示文本
			filterYearText() {
				return this.filterYear || '全部年份';
			},
			filterMonthText() {
				return this.filterMonth ? this.filterMonth + '月' : '全部月份';
			},
			filterDayText() {
				return this.filterDay ? this.filterDay + '日' : '全部日期';
			}
		},
		watch: {
			filterIndex() {
				this.applyFilterAndSort();
			},
			sortIndex() {
				this.applyFilterAndSort();
			},
			sortOrder() {
				this.applyFilterAndSort();
			},
			// 监听数据变化更新年份选项
			characterData: {
				handler() {
					this.updateDateOptions();
				},
				deep: true
			}
		},
		onLoad() {
			
			// 加载主题设置
			this.loadThemeSetting();

			// 动态设置导航栏标题
			uni.setNavigationBarTitle({
				title: '明日方舟通行证列表工具'
			});

			console.log('盒号列表页面加载，开始初始化数据...');
			this.loadLocalData();
			this.loadFavorites();
		},
		onShow() {
			// 页面显示时重新加载收藏数据
			  this.loadFavorites();
			  this.loadMarketPriceSetting(); // 确保每次显示都重新加载市价设置
		},
		onShareAppMessage() {
			return {
				title: '方舟通行证谷子查询工具-通行证盒号列表',
				path: '/pages/list/list',
				imageUrl: ''
			}
		},
		onShareTimeline() {
			return {
				title: '方舟通行证谷子查询工具-通行证盒号列表',
				imageUrl: ''
			}
		},
		methods: {
			toggleToolbar() {
				this.toolbarCollapsed = !this.toolbarCollapsed;
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
						console.error('加载主题设置失败:', e);
						this.themeMode = 'simple';
					}
				},

			// 知晓云 API 请求封装 - 使用ClientID认证
			async knowCloudRequest(tableName, options = {}, retryCount = 0) {
				// 防止无限重试
				if (retryCount > 3) {
					console.error('知晓云请求重试次数过多');
					throw new Error('知晓云请求重试次数过多');
				}
				
				try {
					const tableNameKey = KNOW_CLOUD_CONFIG.tableNames[tableName];
					if (!tableNameKey) {
						throw new Error(`未找到表 ${tableName} 的配置`);
					}
					
					// 构建基础URL
					let url = `${KNOW_CLOUD_CONFIG.baseUrl}/table/${tableNameKey}/record/`;
					
					// 处理查询参数 - 兼容微信小程序环境
					if (options.data) {
						const params = [];
						Object.keys(options.data).forEach(key => {
							params.push(`${key}=${encodeURIComponent(options.data[key])}`);
						});
						if (params.length > 0) {
							url += `?${params.join('&')}`;
						}
					}
					
					// 构建请求参数
					const requestOptions = {
						url: url,
						method: options.method || 'GET',
						header: {
							'X-Hydrogen-Client-ID': KNOW_CLOUD_CONFIG.clientId,
							'Content-Type': 'application/json'
						},
						timeout: 15000,
						...options // 保留其他选项
					};
					
					// 移除重复的header
					if (options.header) {
						Object.assign(requestOptions.header, options.header);
					}
					
					console.log('发送知晓云请求:', requestOptions.url);
					
					const res = await uni.request(requestOptions);
					
					console.log('知晓云响应状态:', res.statusCode);
					
					if (res.statusCode === 200) {
						return res.data;
					} else {
						console.error('知晓云请求错误:', res.statusCode, res.data);
						throw new Error(`请求失败: ${res.statusCode}`);
					}
				} catch (error) {
					console.error(`知晓云请求失败 (${tableName}):`, error);
					// 如果还有重试次数，则重试
					if (retryCount < 3) {
						console.log(`第${retryCount + 1}次重试...`);
						return await this.knowCloudRequest(tableName, options, retryCount + 1);
					}
					throw error;
				}
			},
			
			// 从知晓云获取数据URL和版本信息
			async getDataUrlFromMinapp() {
				try {
					const res = await this.knowCloudRequest('Version', {
						data: {
							limit: 1,
							offset: 0
						}
					});
					
					console.log('知晓云版本信息响应:', res);
					
					if (res && res.objects && res.objects.length > 0) {
						const versionData = res.objects[0];
						console.log('版本数据:', versionData);
						return {
							url: versionData.url || '',
							version: versionData.version || versionData.Version || '',
							cloudUpdateTime: versionData.updated_at || versionData.created_at || ''
						};
					} else {
						console.error('知晓云返回数据格式不正确:', res);
						throw new Error('未找到版本数据或数据格式不正确');
					}
				} catch (error) {
					console.error('从知晓云获取URL失败:', error);
					throw error;
				}
			},
			
			// ============= 新增：市价相关方法 =============
						
			// 修改loadMarketPriceSetting方法，确保正确加载：
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
			      this.showMarketPrice = showMarketPrice === 'true';
			    }
			    
			    console.log('加载后的市价设置:', this.showMarketPrice);
			    
			    // 检查是否有市价数据
			    this.checkMarketPriceData();
			  } catch (e) {
			    console.error('加载市价设置失败:', e);
			    this.showMarketPrice = false; // 默认关闭
			  }
			},
			
			// 修改checkMarketPriceData方法：
			checkMarketPriceData() {
			  // 如果数据为空，直接返回false
			  if (!this.characterData || this.characterData.length === 0) {
			    this.hasMarketPriceData = false;
			    console.log('暂无数据，无法检查市价数据');
			    return;
			  }
			  
			  // 检查数据中是否有市价字段
			  let foundMarketPrice = false;
			  console.log('开始检查市价数据，数据长度:', this.characterData.length);
			  
			  // 检查前5个盒号
			  for (let i = 0; i < Math.min(this.characterData.length, 5); i++) {
			    const box = this.characterData[i];
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
			
			// 修改getBoxCharacters方法，确保正确标记hasMarketPrice：
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
			      } else if (box[charKey] && box[charKey].name) {
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
			
			// 移除List页面中的市价开关切换方法，因为不允许在List页面开启
			// 可以添加一个跳转到设置页面的方法：
			goToSettings() {
			  uni.navigateTo({
			    url: '/packageA/Setting/Setting'
			  });
			},
			dismissMarketPriceTip() {
			  this.hasMarketPriceData = false;
			},
			
			// 保存数据后检查市价数据
			saveDataWithValidation(data) {
				try {
					// 方法1：直接存储原始数据
					console.log('尝试方法1：直接存储原始数据');
					uni.setStorageSync('arknightsData', data);
					
					// 立即验证存储的数据
					const savedData = uni.getStorageSync('arknightsData');
					console.log('存储后验证 - 数据类型:', typeof savedData);
					console.log('存储后验证 - 第一个元素:', savedData[0]);
					console.log('存储后验证 - Box_info 类型:', typeof savedData[0]?.Box_info);
					console.log('存储后验证 - Box_info 值:', savedData[0]?.Box_info);
					
					// 如果存储失败，尝试方法2：字符串存储
					if (!savedData || !Array.isArray(savedData) || typeof savedData[0]?.Box_info === 'string') {
						console.log('方法1失败，尝试方法2：字符串存储');
						this.saveDataAsString(data);
						return;
					}
					
					// 存储成功，更新界面
					this.characterData = savedData;
					this.applyFilterAndSort();
					this.updateLocalInfo();
					this.dataStatusMessage = `数据加载成功 (${savedData.length} 个盒号)`;
					
					// 检查是否有市价数据
					this.checkMarketPriceData();
					
				} catch (error) {
					console.error('数据存储失败:', error);
					this.dataStatusMessage = '数据存储失败';
				}
			},
			
			// 字符串方式存储数据
			saveDataAsString(data) {
				try {
					console.log('使用字符串方式存储数据');
					const dataString = JSON.stringify(data);
					uni.setStorageSync('arknightsDataString', dataString);
					
					// 验证字符串存储
					const savedString = uni.getStorageSync('arknightsDataString');
					if (savedString) {
						const parsedData = JSON.parse(savedString);
						console.log('字符串存储验证 - Box_info 类型:', typeof parsedData[0]?.Box_info);
						console.log('字符串存储验证 - Box_info 值:', parsedData[0]?.Box_info);
						
						this.characterData = parsedData;
						this.applyFilterAndSort();
						this.updateLocalInfo();
						this.dataStatusMessage = `数据加载成功 (${parsedData.length} 个盒号)`;
						
						// 检查是否有市价数据
						this.checkMarketPriceData();
					}
				} catch (error) {
					console.error('字符串存储失败:', error);
					this.dataStatusMessage = '数据存储失败';
				}
			},
			
			// 新增：跳转到盒详情页面
			goToBoxInfo(boxId) {
				uni.navigateTo({
					url: `/pages/box_info/box_info?boxId=${boxId}`
				});
			},
			
			// 格式化云端时间
			formatCloudTime(isoTime) {
				if (!isoTime) return '';
				try {
					const date = new Date(isoTime);
					return `${date.getFullYear()}-${(date.getMonth()+1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')} ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
				} catch (e) {
					console.error('格式化云端时间失败:', e);
					return isoTime;
				}
			},
			
			// 加载本地数据
			async loadLocalData() {
				try {
					// 检查多个数据存储位置
					const data = uni.getStorageSync('arknightsData');
					const githubData = uni.getStorageSync('githubCharacters');
					const currentData = uni.getStorageSync('currentData');
					const localUpdateTime = uni.getStorageSync('localUpdateTime');
					const cloudUpdateTime = uni.getStorageSync('cloudUpdateTime');
					const version = uni.getStorageSync('dataVersion');
					const currentVersion = uni.getStorageSync('currentDataVersion');
					const url = uni.getStorageSync('dataUrl');
					const enableGuessData = uni.getStorageSync('enableGuessData');
					const guessData = uni.getStorageSync('guessData');
					
					let allData = [];
					let usedSource = '';
					
					// 优先级：arknightsData > currentData > githubCharacters
					if (data && Array.isArray(data) && data.length > 0) {
						allData = [...data];
						usedSource = 'arknightsData';
					} else if (currentData && Array.isArray(currentData) && currentData.length > 0) {
						allData = [...currentData];
						usedSource = 'currentData';
					} else if (githubData && Array.isArray(githubData) && githubData.length > 0) {
						allData = [...githubData];
						usedSource = 'githubCharacters';
					}
					
					if (enableGuessData === 'true' && guessData && Array.isArray(guessData) && guessData.length > 0) {
						allData = [...allData, ...guessData];
					}
					
					if (allData.length > 0) {
						this.characterData = allData;
						this.applyFilterAndSort();
						this.localUpdateTime = localUpdateTime || '';
						this.cloudUpdateTime = cloudUpdateTime || '';
						// 优先使用 currentDataVersion
						this.currentVersion = currentVersion || version || '';
						this.dataUrl = url || '';
						this.dataStatusMessage = `已加载本地数据 (${allData.length} 个)${usedSource ? ' [' + usedSource + ']' : ''}`;
						
						if (!this.currentVersion) {
							this.checkForUpdates();
						} else {
							setTimeout(() => {
								this.checkForUpdates();
							}, 1000);
						}
					} else {
						this.dataStatusMessage = '正在获取数据信息...';
						this.checkForUpdates();
					}
				} catch (e) {
					console.error('加载本地数据失败:', e);
					this.dataStatusMessage = '加载本地数据失败，正在尝试获取最新数据...';
					this.checkForUpdates();
				}
			},
			
			// 检查更新
			async checkForUpdates() {
				try {
					this.dataStatusMessage = '正在检查更新...';
					
					const versionInfo = await this.getDataUrlFromMinapp();
					console.log('获取到的版本信息:', versionInfo);
					
					this.dataUrl = versionInfo.url;
					this.cloudUpdateTime = versionInfo.cloudUpdateTime;
					this.latestVersion = versionInfo.version; // 存储最新版本

					uni.setStorageSync('cloudUpdateTime', this.cloudUpdateTime);

					const localVersion = uni.getStorageSync('dataVersion');
					console.log('本地版本:', localVersion, '云端版本:', this.latestVersion);
					
					// 只有当本地版本不存在或与云端版本不同时才下载
					if (!localVersion || localVersion !== this.latestVersion) {
						this.dataStatusMessage = `当前有新版本 (${this.latestVersion}),建议尽快更新`;
					} else {
						this.currentVersion = localVersion;
						this.dataStatusMessage = `数据已是最新版本 (${this.currentVersion})`;
						// 如果本地没有数据，即使版本相同也下载
						if (this.characterData.length === 0) {
							this.loadData();
						}
					}
				} catch (error) {
					console.error('检查更新失败:', error);
					this.dataStatusMessage = '检查更新失败，使用备用URL';
					
					// 使用备用URL
					this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
					
					// 如果没有版本信息，设置为未知
					if (!this.currentVersion) {
						this.currentVersion = '未知版本';
					}
					
					// 如果本地没有数据，下载数据
					if (this.characterData.length === 0) {
						this.loadData();
					}
				}
			},
			
			// 加载数据
			async loadData() {
				// 确保有最新的版本信息
				if (!this.latestVersion) {
					try {
						const versionInfo = await this.getDataUrlFromMinapp();
						this.dataUrl = versionInfo.url;
						this.latestVersion = versionInfo.version;
						this.cloudUpdateTime = versionInfo.cloudUpdateTime || this.cloudUpdateTime;
					} catch (error) {
						console.error('获取数据URL失败:', error);
						// 如果获取失败，使用备用URL
						this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
						this.latestVersion = this.latestVersion || '未知版本';
					}
				}
				
				// 如果没有设置dataUrl，使用备用URL
				if (!this.dataUrl) {
					this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
				}
				
				this.dataStatusMessage = '正在下载干员数据...';
				
				uni.request({
					url: this.dataUrl,
					method: 'GET',
					timeout: 15000,
					success: (res) => {
						if (res.statusCode === 200) {
							let data = res.data;
							
							if (typeof data === 'string') {
								try {
									data = JSON.parse(data);
								} catch (e) {
									console.error('JSON解析失败:', e);
									this.dataStatusMessage = '数据格式错误';
									return;
								}
							}
							
							if (Array.isArray(data)) {
								console.log('原始数据类型:', typeof data);
								console.log('原始数据第一个元素:', data[0]);
								console.log('Box_info 原始类型:', typeof data[0]?.Box_info);
								console.log('Box_info 原始值:', data[0]?.Box_info);
								
								// 验证数据完整性
								this.validateDataStructure(data);
								
								// 使用新的存储方式
								this.saveDataWithValidation(data);
								
							} else {
								this.dataStatusMessage = '数据格式不正确';
							}
						} else {
							this.dataStatusMessage = `服务器错误: ${res.statusCode}`;
						}
					},
					fail: (err) => {
						console.error('下载数据失败:', err);
						this.dataStatusMessage = '下载失败，请检查网络连接';
					}
				});
			},
			
			// 验证数据结构
			validateDataStructure(data) {
				if (!Array.isArray(data)) {
					console.error('数据不是数组');
					return;
				}
				
				const sampleBox = data[0];
				if (!sampleBox) {
					console.error('数据为空');
					return;
				}
				
				console.log('=== 数据结构验证 ===');
				console.log('Box_id:', sampleBox.Box_id);
				console.log('Box_info 类型:', typeof sampleBox.Box_info);
				console.log('Box_info 值:', sampleBox.Box_info);
				console.log('Box_ImageUrl:', sampleBox.Box_ImageUrl);
				console.log('character1 类型:', typeof sampleBox.character1);
				console.log('character1 值:', sampleBox.character1);
				console.log('=== 验证结束 ===');
			},
			
			// 带验证的数据保存
			saveDataWithValidation(data) {
				try {
					// 方法1：直接存储原始数据
					console.log('尝试方法1：直接存储原始数据');
					uni.setStorageSync('arknightsData', data);
					
					// 立即验证存储的数据
					const savedData = uni.getStorageSync('arknightsData');
					console.log('存储后验证 - 数据类型:', typeof savedData);
					console.log('存储后验证 - 第一个元素:', savedData[0]);
					console.log('存储后验证 - Box_info 类型:', typeof savedData[0]?.Box_info);
					console.log('存储后验证 - Box_info 值:', savedData[0]?.Box_info);
					
					// 如果存储失败，尝试方法2：字符串存储
					if (!savedData || !Array.isArray(savedData) || typeof savedData[0]?.Box_info === 'string') {
						console.log('方法1失败，尝试方法2：字符串存储');
						this.saveDataAsString(data);
						return;
					}
					
					// 存储成功，更新界面
					this.characterData = savedData;
					this.applyFilterAndSort();
					this.updateLocalInfo();
					this.dataStatusMessage = `数据加载成功 (${savedData.length} 个盒号)`;
					
				} catch (error) {
					console.error('数据存储失败:', error);
					this.dataStatusMessage = '数据存储失败';
				}
			},
			
			// 字符串方式存储数据
			saveDataAsString(data) {
				try {
					console.log('使用字符串方式存储数据');
					const dataString = JSON.stringify(data);
					uni.setStorageSync('arknightsDataString', dataString);
					
					// 验证字符串存储
					const savedString = uni.getStorageSync('arknightsDataString');
					if (savedString) {
						const parsedData = JSON.parse(savedString);
						console.log('字符串存储验证 - Box_info 类型:', typeof parsedData[0]?.Box_info);
						console.log('字符串存储验证 - Box_info 值:', parsedData[0]?.Box_info);
						
						this.characterData = parsedData;
						this.applyFilterAndSort();
						this.updateLocalInfo();
						this.dataStatusMessage = `数据加载成功 (${parsedData.length} 个盒号)`;
					}
				} catch (error) {
					console.error('字符串存储失败:', error);
					this.dataStatusMessage = '数据存储失败';
				}
			},
			
			// 更新本地信息
			updateLocalInfo() {
				const now = new Date();
				this.localUpdateTime = `${now.getFullYear()}-${(now.getMonth()+1).toString().padStart(2, '0')}-${now.getDate().toString().padStart(2, '0')} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
				
				// 下载成功后，更新当前版本为最新版本
				this.currentVersion = this.latestVersion || this.currentVersion || '未知版本';
				
				uni.setStorageSync('localUpdateTime', this.localUpdateTime);
				uni.setStorageSync('cloudUpdateTime', this.cloudUpdateTime);
				uni.setStorageSync('dataVersion', this.currentVersion);
				uni.setStorageSync('dataUrl', this.dataUrl);
			},
		
			// 获取盒子的所有干员
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
						
						if (typeof box[charKey] === 'string') {
							characterName = box[charKey];
							imageUrl = '';
							nolyELITE1 = false;
							hotcharacter = false;
							market_price = null;
						} else if (box[charKey].name) {
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
								market_price: market_price
							});
						}
					}
				}
				return characters;
			},
			
			// 头像加载成功处理
			onAvatarLoad(boxId, charIndex) {
				console.log(`头像加载成功: 盒${boxId}干员${charIndex}`);
				const cacheKey = `${boxId}_${charIndex}`;
				this.$set(this.avatarCache, cacheKey, true);
			},
			
			// 头像加载失败处理
			onAvatarError(boxId, charIndex) {
				console.error(`头像加载失败: 盒${boxId}干员${charIndex}`);
				const cacheKey = `${boxId}_${charIndex}`;
				this.$set(this.avatarCache, cacheKey, false);
			},
			
			// 模态框头像加载失败处理
			onModalAvatarError(index) {
				this.$set(this.modalBoxCharacters[index], 'avatar', this.defaultAvatar);
			},
			
			// 筛选选项变化
			onFilterChange(e) {
				this.filterIndex = parseInt(e.detail.value);
				this.currentPage = 1;
			},
			
			// 应用筛选和排序
			applyFilterAndSort() {
				let filtered = [...this.characterData];
				
				// 盒类型筛选
				if (this.currentFilter !== 'all') {
					filtered = filtered.filter(box => {
						const boxType = box.Box_type || 'normal';
						
						if (this.currentFilter === 'normal') {
							return !boxType || boxType === 'normal';
						} else {
							return boxType === this.currentFilter;
						}
					});
				}
				
				// ============= 新增：高级筛选 =============
				// 发布时间筛选
				if (this.filterYear) {
					filtered = filtered.filter(box => {
						const releaseDate = box.release_date || '';
						return releaseDate.startsWith(this.filterYear);
					});
				}
				if (this.filterMonth) {
					filtered = filtered.filter(box => {
						const releaseDate = box.release_date || '';
						// 月份格式为 "/MM/"
						const monthPattern = '/' + this.filterMonth + '/';
						return releaseDate.includes(monthPattern);
					});
				}
				if (this.filterDay) {
					filtered = filtered.filter(box => {
						const releaseDate = box.release_date || '';
						// 日期格式为 "/DD"
						const dayPattern = '/' + this.filterDay;
						return releaseDate.endsWith(dayPattern);
					});
				}
				
				// 复刻状态筛选
				if (this.filterReplicate !== '') {
					const target = this.filterReplicate === 'true'; // 转为布尔值
					filtered = filtered.filter(box => {
						// 注意：box.replicate 可能是布尔值或字符串 'true'/'false'
						let replicateValue = box.replicate;
						if (typeof replicateValue === 'string') {
							replicateValue = replicateValue === 'true';
						}
						return replicateValue === target;
					});
				}
				
				// 盲抽/单领筛选
				if (this.filterType !== '') {
					const target = this.filterType === 'true'; // 转为布尔值
					filtered = filtered.filter(box => {
						let typeValue = box.type;
						if (typeof typeValue === 'string') {
							typeValue = typeValue === 'true';
						}
						return typeValue === target;
					});
				}
				
				// 排序
				if (this.sortIndex === 0) {
					// 按盒号排序（原有逻辑）
					filtered.sort((a, b) => {
						const aId = a.Box_id || '';
						const bId = b.Box_id || '';
						const aType = a.Box_type || 'normal';
						const bType = b.Box_type || 'normal';
						
						return this.sortOrder === 'asc' ? 
							this.compareBoxes(aType, aId, bType, bId) : 
							this.compareBoxes(bType, bId, aType, aId);
					});
				} else if (this.sortIndex === 1) {
					// 按干员数量排序
					filtered.sort((a, b) => {
						const aCount = this.getCharacterCount(a);
						const bCount = this.getCharacterCount(b);
						return this.sortOrder === 'asc' ? aCount - bCount : bCount - aCount;
					});
				} else if (this.sortIndex === 2) {
					// 按发布时间排序
					filtered.sort((a, b) => {
						const aDate = a.release_date || '';
						const bDate = b.release_date || '';
						if (aDate === bDate) return 0;
						if (!aDate) return 1;  // 无发布日期的放最后
						if (!bDate) return -1;
						return this.sortOrder === 'asc' ? 
							aDate.localeCompare(bDate) : 
							bDate.localeCompare(aDate);
					});
				}
				
				this.filteredData = filtered;
				// 应用筛选后重置页码
				this.currentPage = 1;
			},
			
			// 盒比较算法
			compareBoxes(typeA, idA, typeB, idB) {
				const priorityA = this.getBoxTypePriority(typeA);
				const priorityB = this.getBoxTypePriority(typeB);
				
				if (priorityA !== priorityB) {
					return priorityA - priorityB;
				}
				
				if (typeA === 'normal') {
					return this.compareNormalBoxIds(idA, idB);
				} else {
					return idA.localeCompare(idB, 'zh-CN');
				}
			},
			
			// 比较常规款盒号
			compareNormalBoxIds(idA, idB) {
				const numA = this.extractNumberFromId(idA);
				const numB = this.extractNumberFromId(idB);
				return numA - numB;
			},
			
			// 从盒号中提取数字
			extractNumberFromId(boxId) {
				const match = boxId.match(/(\d+(?:\.\d+)?)/);
				if (match) {
					return parseFloat(match[1]);
				}
				return 0;
			},
			
			// 计算盒子的干员数量
			getCharacterCount(box) {
				let count = 0;
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
							count++;
						}
					}
				}
				return count;
			},
			
			// 获取盒类型优先级
			getBoxTypePriority(boxType) {
				const priorityMap = {
					'normal': 1,
					'whitelist': 2,
					'special': 3,
					'cooperation': 4,
					'ambience': 5
				};
				return priorityMap[boxType] || 999;
			},
			
			// 排序选项变化
			onSortChange(e) {
				this.sortIndex = parseInt(e.detail.value);
			},
			
			// 切换排序方向
			toggleSortOrder() {
				this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
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
			
			// ============= 收藏相关方法 =============
			
			// 加载收藏数据
			loadFavorites() {
				try {
					this.favoriteBoxIds = uni.getStorageSync('favoriteBoxIds') || [];
					this.favoriteCharacterNames = uni.getStorageSync('favoriteCharacterNames') || [];
					console.log('已收藏的盒号:', this.favoriteBoxIds);
					console.log('已收藏的干员:', this.favoriteCharacterNames);
				} catch (error) {
					console.error('加载收藏数据失败:', error);
					this.favoriteBoxIds = [];
					this.favoriteCharacterNames = [];
				}
			},
			
			// 切换盒号收藏状态
			toggleFavorite(boxId) {
				try {
					const index = this.favoriteBoxIds.indexOf(boxId);
					if (index > -1) {
						// 已收藏，取消收藏
						this.favoriteBoxIds.splice(index, 1);
						uni.showToast({
							title: '已取消收藏',
							icon: 'success'
						});
					} else {
						// 未收藏，添加收藏
						this.favoriteBoxIds.push(boxId);
						uni.showToast({
							title: '收藏成功',
							icon: 'success'
						});
					}
					// 保存到本地存储
					uni.setStorageSync('favoriteBoxIds', this.favoriteBoxIds);
					// 强制更新视图
					this.$forceUpdate();
				} catch (error) {
					console.error('操作收藏失败:', error);
					uni.showToast({
						title: '操作失败',
						icon: 'none'
					});
				}
			},
			
			// 检查是否已收藏盒号
			isFavorite(boxId) {
				return this.favoriteBoxIds.includes(boxId);
			},
			
			// 切换干员收藏状态
			toggleFavoriteCharacter(characterName) {
				try {
					const index = this.favoriteCharacterNames.indexOf(characterName);
					if (index > -1) {
						this.favoriteCharacterNames.splice(index, 1);
						uni.showToast({
							title: '已取消收藏',
							icon: 'success'
						});
					} else {
						this.favoriteCharacterNames.push(characterName);
						uni.showToast({
							title: '收藏成功',
							icon: 'success'
						});
					}
					uni.setStorageSync('favoriteCharacterNames', this.favoriteCharacterNames);
					this.$forceUpdate();
				} catch (error) {
					uni.showToast({
						title: '操作失败',
						icon: 'none'
					});
				}
			},
			
			// 直接点击干员卡片收藏
			toggleFavoriteCharacterDirectly(characterName) {
				this.toggleFavoriteCharacter(characterName);
			},
			
			// 检查是否已收藏干员
			isFavoriteCharacter(characterName) {
				return this.favoriteCharacterNames.includes(characterName);
			},
			
			// 全选/取消全选盒内干员
			selectAllCharacters() {
				if (this.isAllSelected) {
					// 取消全选
					this.modalBoxCharacters.forEach(character => {
						const index = this.favoriteCharacterNames.indexOf(character.name);
						if (index > -1) {
							this.favoriteCharacterNames.splice(index, 1);
						}
					});
					uni.showToast({
						title: '已取消全选',
						icon: 'success'
					});
				} else {
					// 全选
					this.modalBoxCharacters.forEach(character => {
						if (!this.favoriteCharacterNames.includes(character.name)) {
							this.favoriteCharacterNames.push(character.name);
						}
					});
					uni.showToast({
						title: '已全选',
						icon: 'success'
					});
				}
				uni.setStorageSync('favoriteCharacterNames', this.favoriteCharacterNames);
				this.$forceUpdate();
			},
			
			// 显示盒内干员模态框
			openBoxCharactersModal(boxId, characters) {
				this.showBoxCharactersModal = true;
				this.currentBoxId = boxId;
				this.modalBoxCharacters = characters;
			},
			
			// 隐藏盒内干员模态框
			hideBoxCharactersModal() {
				this.showBoxCharactersModal = false;
				this.currentBoxId = '';
				this.modalBoxCharacters = [];
			},
			
			// 上一页
			prevPage() {
				if (this.currentPage > 1) {
					this.currentPage--;
				}
			},
			
			// 下一页
			nextPage() {
				if (this.currentPage < this.totalPages) {
					this.currentPage++;
				}
			},
			
			// 首页
			goToFirstPage() {
				this.currentPage = 1;
			},
			
			// 尾页
			goToLastPage() {
				this.currentPage = this.totalPages;
			},
			
			// ============= 新增：高级筛选相关方法 =============
			
			// 打开高级筛选模态框
			openAdvancedFilter() {
				this.showAdvancedFilter = true;
				// 备份当前筛选值，以便取消时恢复（可选）
				// 这里简单处理，直接显示当前值
			},
			
			// 关闭高级筛选模态框
			closeAdvancedFilter() {
				this.showAdvancedFilter = false;
			},
			
			// 重置高级筛选条件
			resetAdvancedFilter() {
				this.filterYear = '';
				this.filterMonth = '';
				this.filterDay = '';
				this.filterYearIndex = 0;
				this.filterMonthIndex = 0;
				this.filterDayIndex = 0;
				this.filterReplicate = '';
				this.filterType = '';
			},
			
			// 应用高级筛选
			applyAdvancedFilter() {
				this.applyFilterAndSort();
				this.closeAdvancedFilter();
			},
			
			// 更新日期选项（从数据中提取年份）
			updateDateOptions() {
				// 年份选项
				const years = new Set();
				this.characterData.forEach(box => {
					if (box.release_date && box.release_date.length >= 4) {
						const year = box.release_date.substring(0, 4);
						if (year.match(/^\d{4}$/)) {
							years.add(year);
						}
					}
				});
				const sortedYears = Array.from(years).sort();
				this.yearOptions = ['全部年份', ...sortedYears];
				
				// 月份选项固定1-12
				const months = [];
				for (let i = 1; i <= 12; i++) {
					months.push(i.toString().padStart(2, '0'));
				}
				this.monthOptions = ['全部月份', ...months.map(m => m + '月')];
				
				// 日期选项固定1-31
				const days = [];
				for (let i = 1; i <= 31; i++) {
					days.push(i.toString().padStart(2, '0'));
				}
				this.dayOptions = ['全部日期', ...days.map(d => d + '日')];
			},
			
			// 年份选择器变化
			onYearChange(e) {
				this.filterYearIndex = parseInt(e.detail.value);
				if (this.filterYearIndex === 0) {
					this.filterYear = '';
				} else {
					this.filterYear = this.yearOptions[this.filterYearIndex];
				}
			},
			
			// 月份选择器变化
			onMonthChange(e) {
				this.filterMonthIndex = parseInt(e.detail.value);
				if (this.filterMonthIndex === 0) {
					this.filterMonth = '';
				} else {
					// 月份选项显示为 "05月"，提取前两位数字
					const monthText = this.monthOptions[this.filterMonthIndex];
					this.filterMonth = monthText.substring(0, 2);
				}
			},
			
			// 日期选择器变化
			onDayChange(e) {
				this.filterDayIndex = parseInt(e.detail.value);
				if (this.filterDayIndex === 0) {
					this.filterDay = '';
				} else {
					const dayText = this.dayOptions[this.filterDayIndex];
					this.filterDay = dayText.substring(0, 2);
				}
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
	
	/* 控制区域样式 */
	.control-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
		position: relative;
	}
	
	.control-section.collapsed {
		padding: 0;
		background: transparent;
	}
	
	.toolbar-toggle {
		position: absolute;
		top: -40rpx;
		right: 20rpx;
		font-size: 28rpx;
		color: #666;
		z-index: 10;
	}
	
	.toolbar-toggle-icon {
		font-size: 24rpx;
		color: #888;
	}
	
	.control-section.collapsed .toolbar-toggle-icon {
		color: #aaa;
	}
	
	.filter-section, .sort-section {
		flex: 1;
		min-width: 250rpx;
	}
	
	.section-title {
		font-size: 28rpx;
		color: #333;
		font-weight: bold;
		display: block;
		margin-bottom: 15rpx;
	}
	
	.filter-controls, .sort-controls {
		display: flex;
		align-items: center;
		gap: 20rpx;
	}
	
	.filter-picker, .sort-picker {
		background-color: #f8f8f8;
		border-radius: 10rpx;
		padding: 15rpx 20rpx;
		flex: 1;
	}
	
	.picker-text {
		font-size: 26rpx;
		color: #333;
	}
	
	.sort-order-btn {
		background-color: #f0f0f0;
		color: #666;
		font-size: 24rpx;
		border-radius: 10rpx;
		padding: 15rpx 20rpx;
		white-space: nowrap;
	}
	
	/* 新增：高级筛选按钮 */
	.advanced-filter-btn {
		background-color: #409EFF;
		color: #fff;
		font-size: 26rpx;
		border-radius: 10rpx;
		padding: 15rpx 30rpx;
		white-space: nowrap;
	}
	
	.pagination-section {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background-color: #fff;
		border-radius: 16rpx;
		padding: 20rpx 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	.pagination-info {
		display: flex;
		flex-direction: column;
	}
	
	.page-text {
		font-size: 28rpx;
		color: #333;
		font-weight: bold;
	}
	
	.data-count {
		font-size: 24rpx;
		color: #999;
		margin-top: 5rpx;
	}
	
	.advanced-filter-tip {
		font-size: 22rpx;
		color: #E6A23C;
		margin-top: 5rpx;
	}
	
	.pagination-controls {
		display: flex;
	}
	
	.page-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 10rpx;
		font-size: 24rpx;
		padding: 12rpx 16rpx;
		margin-left: 10rpx;
	}
	
	.page-btn:disabled {
		background-color: #c0c4cc;
		color: #fff;
		opacity: 0.6;
	}
	
	.data-status {
		background-color: #e8f4ff;
		border-radius: 16rpx;
		padding: 20rpx;
		margin-bottom: 30rpx;
		display: flex;
		justify-content: space-between;
		align-items: center;
		font-size: 24rpx;
		color: #409EFF;
	}
	
	.boxes-list {
		background-color: #fff;
		border-radius: 16rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		overflow: hidden;
		margin-bottom: 30rpx;
	}
	
	.box-item {
		border-bottom: 1rpx solid #f0f0f0;
		padding: 30rpx;
	}
	
	.box-item:last-child {
		border-bottom: none;
	}
	
	.box-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 20rpx;
		padding-bottom: 15rpx;
		border-bottom: 1rpx solid #f5f5f5;
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
		font-size: 32rpx;
		font-weight: bold;
		color: #409EFF;
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
	
	.view-info-btn {
		background-color: #67C23A;
		color: #fff;
		font-size: 24rpx;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
		white-space: nowrap;
	}
	
	.favorite-characters-btn {
		background-color: #409EFF;
		color: #fff;
		font-size: 24rpx;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
		white-space: nowrap;
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
	
	.hot-character-icon {
		position: absolute;
		top: -8rpx;
		right: -8rpx;
		width: 30rpx;
		height: 30rpx;
		z-index: 2;
	}
	
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
		font-size: 32rpx;
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
	
	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 100rpx 0;
		background-color: #fff;
		border-radius: 16rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
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
		margin-bottom: 30rpx;
	}
	
	.update-btn {
		background-color: #67C23A;
		color: #fff;
		border-radius: 50rpx;
		font-size: 26rpx;
		padding: 20rpx 40rpx;
	}
	
	.update-btn.small {
		padding: 10rpx 20rpx;
		font-size: 24rpx;
	}
	
	.footer {
		text-align: center;
		padding-top: 30rpx;
		border-top: 1rpx solid #e0e0e0;
	}
	
	.update-info {
		font-size: 24rpx;
		color: #999;
		display: block;
		margin-bottom: 5rpx;
	}
	
	.version-info {
		font-size: 24rpx;
		color: #999;
		display: block;
	}
	
	/* 盒内干员模态框样式 */
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
	
	.image-container {
		flex: 1;
		padding: 30rpx;
		max-height: 70vh;
	}
	
	.box-characters-list {
		display: flex;
		flex-direction: column;
		gap: 15rpx;
	}
	
	.box-character-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background-color: #f8fafc;
		border-radius: 12rpx;
		padding: 20rpx;
		border: 1rpx solid #e0e0e0;
	}
	
	.box-character-info {
		display: flex;
		align-items: center;
		flex: 1;
	}
	
	.character-details {
		flex: 1;
		margin-left: 15rpx;
	}
	
	.image-modal-footer {
		padding: 20rpx 30rpx;
		border-top: 1rpx solid #f0f0f0;
		text-align: center;
		display: flex;
		flex-direction: column;
		gap: 15rpx;
	}
	
	.modal-footer-actions {
		display: flex;
		gap: 15rpx;
	}
	
	.select-all-btn {
		flex: 1;
		background-color: #409EFF;
		color: #fff;
		font-size: 26rpx;
		padding: 15rpx 30rpx;
		border-radius: 8rpx;
	}
	
	.favorite-box-btn {
		flex: 1;
		background-color: #f0f0f0;
		color: #666;
		font-size: 26rpx;
		padding: 15rpx 30rpx;
		border-radius: 8rpx;
		transition: all 0.3s;
	}
	
	.favorite-box-btn.favorited {
		background-color: #E6A23C;
		color: #fff;
	}
	
	.image-tip {
		font-size: 24rpx;
		color: #999;
	}
	
	/* ============= 新增：高级筛选模态框样式 ============= */
	.advanced-filter-modal {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: rgba(0, 0, 0, 0.6);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 10000;
		padding: 40rpx;
	}
	
	.modal-content {
		background-color: #fff;
		border-radius: 16rpx;
		width: 100%;
		max-width: 650rpx;
		max-height: 80vh;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}
	
	.modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 30rpx;
		border-bottom: 1rpx solid #f0f0f0;
	}
	
	.modal-title {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
	}
	
	.modal-body {
		flex: 1;
		padding: 30rpx;
	}
	
	.filter-group {
		margin-bottom: 40rpx;
	}
	
	.filter-group-title {
		font-size: 28rpx;
		font-weight: bold;
		color: #409EFF;
		display: block;
		margin-bottom: 20rpx;
	}
	
	.date-pickers {
		display: flex;
		gap: 20rpx;
	}
	
	.date-picker {
		flex: 1;
		background-color: #f8f8f8;
		border-radius: 10rpx;
		padding: 20rpx;
		text-align: center;
	}
	
	.radio-group {
		display: flex;
		flex-direction: column;
		gap: 20rpx;
	}
	
	.radio-label {
		display: flex;
		align-items: center;
		gap: 15rpx;
		font-size: 28rpx;
		color: #333;
	}
	
	.radio {
		width: 36rpx;
		height: 36rpx;
		border-radius: 50%;
		border: 2rpx solid #ccc;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-size: 28rpx;
		color: transparent;
		transition: all 0.2s;
	}
	
	.radio.active {
		border-color: #409EFF;
		color: #409EFF;
	}
	
	.modal-footer {
		display: flex;
		padding: 20rpx 30rpx;
		border-top: 1rpx solid #f0f0f0;
		gap: 20rpx;
	}
	
	.reset-btn {
		flex: 1;
		background-color: #f0f0f0;
		color: #666;
		font-size: 28rpx;
		padding: 20rpx;
		border-radius: 8rpx;
	}
	
	.cancel-btn {
		flex: 1;
		background-color: #f0f0f0;
		color: #666;
		font-size: 28rpx;
		padding: 20rpx;
		border-radius: 8rpx;
	}
	
	.confirm-btn {
		flex: 1;
		background-color: #409EFF;
		color: #fff;
		font-size: 28rpx;
		padding: 20rpx;
		border-radius: 8rpx;
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
	
	@media (max-width: 750rpx) {
		.control-section {
			flex-direction: column;
			align-items: stretch;
		}
		
		.advanced-filter-btn {
			width: 100%;
		}
		
		.box-header {
			flex-direction: column;
			align-items: flex-start;
		}
		
		.box-header-left {
			width: 100%;
			margin-bottom: 10rpx;
			justify-content: space-between;
		}
		
		.box-header-right {
			flex-direction: column;
			width: 100%;
			gap: 8rpx;
		}
		
		.favorite-characters-btn, .view-info-btn {
			width: 100%;
			font-size: 22rpx;
			padding: 10rpx;
		}
		
		.box-character-item {
			flex-direction: column;
			align-items: flex-start;
		}
		
		.box-character-info {
			width: 100%;
			margin-bottom: 15rpx;
		}
		
		.box-character-item .favorite-btn {
			width: 100%;
		}
		
		.modal-footer-actions {
			flex-direction: column;
			width: 100%;
		}
		
		.select-all-btn, .favorite-box-btn {
			width: 100%;
			font-size: 24rpx;
		}
		
		.date-pickers {
			flex-direction: column;
		}
	}

	/* ========== 科技风格 ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
	}
	.container.theme-ark .control-section,
	.container.theme-ark .boxes-list,
	.container.theme-ark .data-status,
	.container.theme-ark .advanced-filter-modal .modal-content,
	.container.theme-ark .character-card {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border: 1rpx solid #2A2A4A;
	}
	.container.theme-ark .section-title,
	.container.theme-ark .picker-text,
	.container.theme-ark .box-number,
	.container.theme-ark .character-name { color: #e0e0e0; }
	.container.theme-ark .filter-picker { background-color: #16162A; border-color: #2A2A4A; }
	.container.theme-ark .sort-btn { background: #16162A; color: #e0e0e0; border-color: #2A2A4A; }
	.container.theme-ark .sort-btn.active { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border-color: transparent; }
	.container.theme-ark .box-card { background: rgba(22,22,42,0.6); border-color: #2A2A4A; }
	.container.theme-ark .box-card:active { border-color: #FF6B35; }
	.container.theme-ark .market-price-hint { background: rgba(255,107,53,0.08); }
	.container.theme-ark .hint-text { color: #FF6B35; }
	.container.theme-ark .pagination { background: rgba(22,22,42,0.8); }
	.container.theme-ark .page-btn { background: #16162A; color: #e0e0e0; border-color: #2A2A4A; }
	.container.theme-ark .page-btn:active { background: #FF6B35; color: #fff; }
	.container.theme-ark .favorite-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .favorite-btn.favorited { background: #555; color: #FF6B35; }
	.container.theme-ark .close-btn { background: rgba(255,107,53,0.1); color: #FF6B35; }
	.container.theme-ark .modal-title { color: #FF6B35; }
	.container.theme-ark .advanced-filter-section { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border-color: #2A2A4A; }
	.container.theme-ark .advanced-filter-title { color: #FF6B35; }
	.container.theme-ark .advanced-filter-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .section-subtitle { color: #888; }
	.container.theme-ark .search-icon { filter: brightness(0) invert(1); opacity: 0.7; }
	.container.theme-ark .advanced-filter-header { border-bottom-color: #2A2A4A; }
	.container.theme-ark .character-tags { color: #888; }
	.container.theme-ark .empty-state { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border-color: #2A2A4A; }
	.container.theme-ark .empty-icon { filter: brightness(0) invert(1); opacity: 0.4; }
	.container.theme-ark .empty-text { color: #888; }

	/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .control-section,
	.container.theme-jieyuan .boxes-list,
	.container.theme-jieyuan .data-status,
	.container.theme-jieyuan .advanced-filter-modal .modal-content {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .section-title,
	.container.theme-jieyuan .picker-text,
	.container.theme-jieyuan .box-number,
	.container.theme-jieyuan .character-name { color: #333; }
	.container.theme-jieyuan .filter-picker { background: rgba(0,0,0,0.05); border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .sort-btn { background: rgba(0,0,0,0.05); color: #333; border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .sort-btn.active { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border-color: transparent; }
	.container.theme-jieyuan .box-card { background: rgba(0,0,0,0.03); border-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .box-card:active { border-color: #e25884; }
	.container.theme-jieyuan .market-price-hint { background: rgba(226, 88, 132, 0.08); }
	.container.theme-jieyuan .hint-text { color: #e25884; }
	.container.theme-jieyuan .pagination { background: rgba(255, 255, 255, 0.9); }
	.container.theme-jieyuan .page-btn { background: rgba(0,0,0,0.05); color: #333; border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .page-btn:active { background: linear-gradient(90deg, #e25884, #399383); color: #fff; }
	.container.theme-jieyuan .favorite-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .favorite-btn.favorited { background: rgba(226, 88, 132, 0.15); color: #e25884; }
	.container.theme-jieyuan .close-btn { background: rgba(226, 88, 132, 0.1); color: #e25884; }
	.container.theme-jieyuan .modal-title { color: #399383; }
	.container.theme-jieyuan .advanced-filter-section { background: rgba(255, 255, 255, 0.95); border-color: rgba(226, 88, 132, 0.15); }
	.container.theme-jieyuan .advanced-filter-title { background: linear-gradient(90deg, #e25884, #399383); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
</style>
