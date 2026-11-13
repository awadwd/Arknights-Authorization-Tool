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

			<!-- 宽屏：分页控件并进筛选卡，省掉一整张卡的高度 -->
			<view class="pagination-section pagination-inline" v-if="isWide && !toolbarCollapsed">
				<view class="pagination-info">
					<text class="page-text">第 {{ currentPage }} 页 / 共 {{ totalPages }} 页</text>
					<text class="data-count">共 {{ filteredData.length }} 盒 (筛选后)</text>
					<text v-if="hasAdvancedFilter" class="advanced-filter-tip">高级筛选已应用</text>
				</view>
				<view class="pagination-controls">
					<view class="page-btn-row">
						<button class="page-btn" :disabled="currentPage <= 1" @click="goToFirstPage">首页</button>
						<button class="page-btn" :disabled="currentPage <= 1" @click="prevPage">上一页</button>
						<button class="page-btn" :disabled="currentPage >= totalPages" @click="nextPage">下一页</button>
						<button class="page-btn" :disabled="currentPage >= totalPages" @click="goToLastPage">尾页</button>
					</view>
					<view class="jump-group">
						<input class="jump-input" type="number" v-model="jumpPageInput" placeholder="页码" />
						<button class="jump-btn" @click="handleJumpPage">跳转</button>
					</view>
				</view>
			</view>
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
		
		<!-- 分页控制（窄屏：独立一张卡；宽屏并进筛选卡，见下方 inline 版本） -->
		<view class="pagination-section" v-if="!isWide || toolbarCollapsed">
			<view class="pagination-info">
				<text class="page-text">第 {{ currentPage }} 页 / 共 {{ totalPages }} 页</text>
				<text class="data-count">共 {{ filteredData.length }} 盒 (筛选后)</text>
				<!-- 显示当前高级筛选条件（可选） -->
				<text v-if="hasAdvancedFilter" class="advanced-filter-tip">高级筛选已应用</text>
			</view>

			<view class="pagination-controls">
				<view class="page-btn-row">
					<button class="page-btn" :disabled="currentPage <= 1" @click="goToFirstPage">首页</button>
					<button class="page-btn" :disabled="currentPage <= 1" @click="prevPage">上一页</button>
					<button class="page-btn" :disabled="currentPage >= totalPages" @click="nextPage">下一页</button>
					<button class="page-btn" :disabled="currentPage >= totalPages" @click="goToLastPage">尾页</button>
				</view>
				<!-- 跳转至指定页 -->
				<view class="jump-group">
					<input class="jump-input" type="number" v-model="jumpPageInput" placeholder="页码" />
					<button class="jump-btn" @click="handleJumpPage">跳转</button>
				</view>
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
				class="box-item box-card" 
				v-for="(box, index) in currentPageData" 
				:key="index"
			>
				<!-- 卡片头部：盒号标题 -->
				<view class="box-card-header">
					<text class="box-card-title">盒号 {{ box.Box_id }}</text>
					<text class="box-card-type" :class="getBoxTypeClass(box.Box_type)">
						{{ getBoxTypeText(box.Box_type) }}
					</text>
				</view>
				
				<!-- 官方图片 -->
				<view class="box-image-section" v-if="box.Box_ImageUrl">
					<view 
						class="box-image-wrapper"
						:class="{ 'expanded': expandedBoxes[box.Box_id] }"
						@click="previewBoxImage(box.Box_ImageUrl)"
					>
						<image 
							class="box-official-image" 
							:src="imgSrc(box.Box_ImageUrl)" 
							mode="widthFix"
							@load="onBoxImageLoad(box.Box_id, $event)"
						></image>
						<view class="image-preview-hint">
							<text class="preview-hint-text">🔍 点击查看大图</text>
						</view>
					</view>
					<!-- 展开/收起按钮 -->
					<view 
						class="image-expand-btn"
						v-if="expandedBoxes[box.Box_id] !== undefined"
						@click.stop="toggleImageExpand(box.Box_id)"
					>
						<text class="expand-icon">{{ expandedBoxes[box.Box_id] ? '▲' : '▼' }}</text>
						<text class="expand-text">{{ expandedBoxes[box.Box_id] ? '收起' : '展开' }}</text>
					</view>
				</view>
				
				<!-- 基本信息网格 -->
				<view class="box-info-grid">
					<view class="info-item">
						<text class="info-label">首发日期</text>
						<text class="info-value">{{ formatDate(box.release_date) }}</text>
					</view>
					<view class="info-item">
						<text class="info-label">价格</text>
						<text class="info-value">{{ box.retail_price || '未知' }}</text>
					</view>
					<view class="info-item">
						<text class="info-label">类型</text>
						<text class="info-value">{{ box.type === true || box.type === 'true' ? '盲抽' : '单领' }}</text>
					</view>
					<view class="info-item">
						<text class="info-label">复刻时间</text>
						<text class="info-value" v-if="!getReplicateRecords(box).length">暂无记录</text>
						<view class="replicate-records" v-else>
							<text class="replicate-line" v-for="(rec, rIdx) in getReplicateRecords(box)" :key="rIdx">{{ rec }}</text>
						</view>
					</view>
				</view>
				
				<!-- 干员网格（紧凑尺寸） -->
				<view class="characters-grid-compact">
					<view 
						class="character-item-compact" 
						v-for="(character, charIndex) in getBoxCharacters(box)" 
						:key="charIndex"
						@click="toggleFavoriteCharacterDirectly(character.name)"
					>
						<view class="character-avatar-wrap">
							<image 
								class="character-avatar-compact" 
								:src="imgSrc(pAvatar(character))" 
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
						<view class="character-info-compact">
							<text class="character-name-compact">{{ character.name }}</text>
							<text v-if="character.nolyELITE1" class="elite-tag-compact">仅精一</text>
						<!-- 市价信息（仅开启时显示，灰色紧凑） -->
						<view class="character-market-price" v-if="showMarketPrice && character.market_price">
							<text class="market-price-tag-c">精一:<text class="mp-value-c">{{ character.market_price.ELITE1 || 0 }}</text><text v-if="character.market_price.ELITE2 && !character.nolyELITE1"> | 精二:<text class="mp-value-c">{{ character.market_price.ELITE2 }}</text></text>
							</text>
						</view>
					</view>
				</view>
			</view>
			<button
			    class="action-btn feedback-btn"
			    @click="goToBoxFeedback(box.Box_id)"
			>
			    反馈纠错
			</button>
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
										:src="imgSrc(pAvatar(character))" 
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
			<view class="modal-content" id="afm-card" @tap.stop>
				<view class="modal-header" id="afm-header">
					<text class="modal-title">高级筛选</text>
					<button class="close-btn" @click="closeAdvancedFilter">×</button>
				</view>
				<scroll-view class="modal-body" id="afm-body" scroll-y :style="filterBodyHeight ? { height: filterBodyHeight + 'px' } : ''">
					<view class="filter-scroll-inner">
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
					<!-- 干员属性筛选（PRTS 维度，默认折叠） -->
					<view class="filter-group">
						<view class="facet-toggle" @click="toggleFacetPanel">
							<text class="filter-group-title">干员属性</text>
							<view class="facet-toggle-right">
								<text class="facet-count" v-if="facetCount()">{{ facetCount() }} 项</text>
								<text class="facet-arrow" :class="{ open: facetExpanded }">›</text>
							</view>
						</view>
						<view class="facet-panel" v-if="facetExpanded">
							<view class="facet-summary">
								<text class="facet-summary-text">{{ facetSummaryText() }}</text>
								<text class="facet-summary-clear" v-if="facetCount()" @click.stop="clearAllFacets">清空</text>
							</view>
							<view class="facet-group" v-for="g in facetGroups" :key="g.key">
								<view class="facet-group-head">
									<text class="facet-group-title">{{ g.label }}</text>
									<view class="facet-ops">
										<text class="facet-op" @click.stop="selectAllFacet(g.key)">全选</text>
										<text class="facet-op" v-if="charFacets[g.key].length" @click.stop="clearFacet(g.key)">清除</text>
									</view>
								</view>
								<view class="facet-chips">
									<text
										class="facet-chip"
										v-for="opt in facetOptions(g.key)"
										:key="g.key + opt"
										:class="{ active: charFacets[g.key].indexOf(opt) >= 0 }"
										@click.stop="toggleFacet(g.key, opt)"
									>{{ facetOptionLabel(g.key, opt) }}</text>
								</view>
							</view>
							<text class="facet-tip">不同分组同时满足，同组多选为任一满足；盒内只要有一位干员满足即可保留。</text>
						</view>
					</view>
					</view>
				</scroll-view>
				<view class="modal-footer" id="afm-footer">
					<text class="footer-facet-hint" v-if="facetCount()">{{ facetSummaryText() }}</text>
					<button class="reset-btn" @click="resetAdvancedFilter">重置</button>
					<button class="cancel-btn" @click="closeAdvancedFilter">取消</button>
					<button class="confirm-btn" @click="applyAdvancedFilter">确定</button>
				</view>
			</view>
		</view>
	</view>
	</view>
</template>

<script>
	import errorLog from "@/utils/errorLog.js";
import { markDailyTask } from "@/utils/dailyTask.js";
import * as imageCache from "@/utils/imageCache.js";
	import { fetchSelfHosted } from "@/utils/knowCloudProxy.js";
import * as boxDataSync from "@/utils/boxDataSync.js";
	import avatarMixin from "@/utils/avatarMixin.js";
import { mergeGuessData, pruneStoredGuessData } from "@/utils/guessData.js";
	import { boxCtx, emptyFacets, FACET_GROUPS, optionsOf, optionLabel, matchCharFacets, hasFacets } from "@/utils/avatarPref.js";
	// 开源版本：知晓云配置已禁用，使用 GitHub 直链
	const GITHUB_DATA_SOURCES = {
		boxIdUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/Box_Id.json'
	};
	
	// 知晓云配置 - 开源版本clientId为空，商业版请配置知晓云
	const KNOW_CLOUD_CONFIG = {
		clientId: '21680db9b1362913357c',
		baseUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main',
		tableNames: {
			Version: ''          // 开源版本使用 GitHub 直链
		}
	}
	
	export default {

		mixins: [avatarMixin],

		data() {
			return {
				// 主题模式
				themeMode: 'simple',
				toolbarCollapsed: false,
				// 宽屏（桌面壳 Mac / Windows）：分页控件并进筛选卡，少占一张卡的高度
				isWide: false,
				characterData: [],
				filteredData: [],
				currentPage: 1,
				pageSize: 5,
        jumpPageInput: '',
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
				charFacets: emptyFacets(),
				facetExpanded: false,
				filterBodyHeight: 0,          // scroll-view 实测高度（0 = 自适应）
				facetGroups: FACET_GROUPS,
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
				expandedBoxes: {},
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
				       this.filterReplicate !== '' || this.filterType !== '' || hasFacets(this.charFacets);
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
		onLoad(options) {
		// 每日任务打点
		markDailyTask('browse_list');
		// 宽屏（桌面壳 Mac / Windows）一页放 10 个；手机/小程序维持 5 个
		// 同时决定分页控件是否并进筛选卡（窄屏放不下，仍旧单独一张卡）
		this.applyWideFlag();
		// #ifdef H5
		this._bindWideResize();
		// #endif
			// 恢复分享链接参数
			if (typeof options !== "undefined" && options) {
				if (options.filterType !== undefined && options.filterType !== '') this.filterType = decodeURIComponent(options.filterType);
				if (options.filterIndex !== undefined) this.filterIndex = parseInt(options.filterIndex);
				if (options.sortIndex !== undefined) this.sortIndex = parseInt(options.sortIndex);
				if (options.sortOrder) this.sortOrder = options.sortOrder;
				if (options.page !== undefined) this.currentPage = parseInt(options.page);
			}
			// 加载主题设置
			this.loadThemeSetting();

			// 动态设置导航栏标题
			uni.setNavigationBarTitle({
				title: '明日方舟通行证列表工具'
			});

			console.log('盒号列表页面加载，开始初始化数据...');
			this.loadLocalData().then(() => {
				this.loadFavorites();
        this.applyFilterAndSort(true);
        // 恢复分享时的页码（在筛选排序完成后）
        if (typeof options !== "undefined" && options && options.page !== undefined) {
          const savedPage = parseInt(options.page);
          if (savedPage >= 1 && savedPage <= this.totalPages) {
            this.currentPage = savedPage;
            console.log("[DEBUG] Restored page from share:", savedPage);
          }
        }
			});
		},
		onShow() {
			this.refreshAvatarPref();
			// 页面显示时重新加载收藏数据
			this.loadFavorites();
			this.loadMarketPriceSetting(); // 确保每次显示都重新加载市价设置
		},
		onShareAppMessage() {
			return {
				title: '方舟通行证谷子查询工具-通行证盒号列表' + (this.filterType === 'true' ? ' [盲抽]' : this.filterType === 'false' ? ' [单领/赠品]' : '') + ' p' + this.currentPage,
				path: '/pages/list/list?filterType=' + encodeURIComponent(this.filterType) + '&filterIndex=' + this.filterIndex + '&sortIndex=' + this.sortIndex + '&sortOrder=' + this.sortOrder + '&page=' + this.currentPage,
				imageUrl: ''
			}
		},
		onShareTimeline() {
			return {
				title: '方舟通行证谷子查询工具-通行证盒号列表' + (this.filterType === 'true' ? ' [盲抽]' : this.filterType === 'false' ? ' [单领/赠品]' : '') + ' p' + this.currentPage,
				imageUrl: ''
			}
		},
		onUnload() {
			// #ifdef H5
			this._unbindWideResize();
			// #endif
		},
		methods: {
			// 宽屏判定：≥720px 视为桌面宽屏（与每页 10 盒的阈值保持一致）
			applyWideFlag() {
				let w = 0;
				try {
					w = uni.getSystemInfoSync().windowWidth || 0;
				} catch (e) {}
				const next = w >= 720;
				if (next === this.isWide) return;
				this.isWide = next;
				this.pageSize = next ? 10 : 5;
				// 每页条数变了要重算分页；数据还没加载时不用管，onLoad 后续会自己算
				if (this.characterData && this.characterData.length) {
					this.applyFilterAndSort(true);
				}
			},
			// #ifdef H5
			_bindWideResize() {
				if (typeof window === 'undefined' || this._wideResizeBound) return;
				this._wideResizeBound = true;
				this._onWideResize = () => this.applyWideFlag();
				window.addEventListener('resize', this._onWideResize);
			},
			_unbindWideResize() {
				if (typeof window === 'undefined' || !this._wideResizeBound) return;
				this._wideResizeBound = false;
				try {
					window.removeEventListener('resize', this._onWideResize);
				} catch (e) {}
			},
			// #endif
			// 图片地址：优先返回本地持久缓存路径，未命中则返回远程地址并后台缓存
			// 盒图与干员头像基本不变，缓存后不再重复走网络
			imgSrc(url) {
				try {
					return imageCache.resolve(url);
				} catch (e) {
					return url || "";
				}
			},
			logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},

			// 切换官方大图展开/收起
			toggleImageExpand(boxId) {
				this.$set(this.expandedBoxes, boxId, !this.expandedBoxes[boxId]);
			},

			// 大图加载回调
			onBoxImageLoad(boxId, e) {
				this.$set(this.expandedBoxes, boxId, false);
			},

			// 获取复刻记录数组
			getReplicateRecords(box) {
				const list = [];
				const base = box.replicate_date;
				if (base) list.push(base + " 第一次复刻");
				for (let i = 1; i <= 5; i++) {
					const v = box["replicate_date_0" + i];
					if (v) list.push(v + " " + ["", "第一次", "第二次", "第三次", "第四次", "第五次"][i] + "复刻");
				}
				return list;
			},

			// 数字转中文序数
			toChineseOrdinal(n) {
			        const arr = ["", "一", "二", "三", "四", "五"];
			        return arr[n] || String(n);
			    },
			    formatDate(dateStr) {
			        if (!dateStr) return '—';
			        return dateStr;
			    },
			    previewBoxImage(imageUrl) {
			        uni.previewImage({ urls: [imageUrl], current: imageUrl });
			    },
			    goToBoxFeedback(boxId) {
			    	uni.navigateTo({
			    		url: `/pages/box_feedback_new/box_feedback_new?boxId=${encodeURIComponent(boxId)}`
			    	});
			    },
			    toggleToolbar() {
			        this.toolbarCollapsed = !this.toolbarCollapsed;
			    },
			// 加载主题设置
				loadThemeSetting() {
					try {
						const themeMode = uni.getStorageSync('themeMode');
						if (themeMode && (themeMode === 'simple' || themeMode === 'ak' || themeMode === 'ark' || themeMode === 'jieyuan' || themeMode === 'liquid' || themeMode === 'liquidDark')) {
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

			// 知晓云 API 请求封装 - 使用ClientID认证
			async knowCloudRequest(tableName, options = {}, retryCount = 0) {
				// 优先走自建 API，失败则回落知晓云
				try {
				  const sh = await fetchSelfHosted(tableName, options);
				  if (sh) return sh;
				} catch (e) {
				  // 忽略，继续走知晓云
				}
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
					this.logError(error);
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
				let res;
				const sh_Version = await fetchSelfHosted("Version", { data: { limit: 1 } });
				if (sh_Version) {
				  res = { statusCode: 200, data: sh_Version };
				} else {
				res = await uni.request({
					url: 'https://21680db9b1362913357c.myminapp.com/hserve/v2.2/table/Version/record/?limit=1',
					method: 'GET',
					header: {
						'X-Hydrogen-Client-ID': '21680db9b1362913357c',
						'Content-Type': 'application/json'
					},
					timeout: 10000
				});
				}
				if (res.statusCode === 200 && res.data && res.data.objects && res.data.objects.length > 0) {
					const versionData = res.data.objects[0];
					return {
						url: versionData.url || '',
						version: versionData.version || versionData.Version || '',
						cloudUpdateTime: versionData.updated_at || versionData.created_at || ''
					};
				}
				throw new Error('未找到版本数据');
			} catch (error) {
				this.logError(error);
				console.error('从知晓云获取URL失败:', error);
				return {
					url: 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json',
					version: 'v1.9.2.0',
					cloudUpdateTime: ''
				};
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
			          hasMarketPrice: hasMarketPrice,
			          _avatarCtx: boxCtx(box)
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
					this.logError(error);
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
					this.logError(error);
					console.error('字符串存储失败:', error);
					this.dataStatusMessage = '数据存储失败';
				}
			},
			
			// 新增：跳转到盒详情页面
			goToBoxInfo(boxId) {
				uni.navigateTo({
					url: `/pages/box_info/box_info?boxId=${encodeURIComponent(boxId)}`
				});
			},
			
			// 格式化云端时间
			formatCloudTime(isoTime) {
				if (!isoTime) return '';
				try {
					const date = new Date(isoTime);
					return `${date.getFullYear()}-${(date.getMonth()+1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')} ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
				} catch (e) {
					this.logError(e);
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
					// 正式数据上线后清掉重复的预测盒号
					pruneStoredGuessData();
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
						allData = mergeGuessData(allData, guessData).data;
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
					this.logError(e);
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
					this.logError(error);
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

// 加载数据 - 多源降级策略
// 优先级：知晓云 → GitCode Raw → jsDelivr CDN → GitHub Raw → 固定死链接
			async loadData() {
				// 1. 获取版本信息（以知晓云为主）
				if (!this.latestVersion) {
					try {
						const versionInfo = await this.getDataUrlFromMinapp();
						this.dataUrl = versionInfo.url;
						this.latestVersion = versionInfo.version;
						this.cloudUpdateTime = versionInfo.cloudUpdateTime || this.cloudUpdateTime;
					} catch (error) {
						this.logError(error);
						console.error('获取数据URL失败:', error);
						this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
						this.latestVersion = this.latestVersion || '未知版本';
					}
				}
				
				if (!this.dataUrl) {
					this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
				}
				
				// 2. 构建多源列表（本地/自定义数据激活时返回空列表，禁止远程下载）
				const sourceList = this.buildDataSourceList(this.dataUrl);
				if (!sourceList.length) {
					this.dataStatusMessage = '当前使用本地/自定义数据，已跳过远程下载';
					return;
				}
				// 增量更新快路径：先探服务端版本 —— 没变直接用本地缓存，变了只拉差异记录
				// 任何一步不满足（无缓存 / 接口不可用 / 拉取失败）都会返回 null，自然落回下面的完整包下载
				try {
					const _inc = await boxDataSync.ensure();
					if (_inc && _inc.data && _inc.data.length) {
						console.log('[增量更新] ' + _inc.source + '，' + _inc.data.length + ' 个盒号');
						this.dataStatusMessage = '数据已是最新';
						await this.handleListDownloadSuccess(_inc.data, _inc.source);
						return;
					}
				} catch (_incErr) {
					this.logError(_incErr);
					console.warn('[增量更新] 降级为完整包下载:', _incErr);
				}
				this.dataStatusMessage = '正在下载干员数据...';
				
				// 3. 依次尝试各数据源
				for (let i = 0; i < sourceList.length; i++) {
					const source = sourceList[i];
					console.log('[加载数据] 尝试源 ' + (i + 1) + '/' + sourceList.length + ': ' + source.name);
					try {
						const data = await this.tryDownloadFromSource(source.url);
						if (data) {
							await this.handleListDownloadSuccess(data, source.name);
							return;
						}
					} catch (err) {
						this.logError(err);
						console.warn('[加载数据] 源 ' + source.name + ' 失败:', err.message || err);
					}
				}
				
				// 4. 所有源都失败
				this.dataStatusMessage = '下载失败，所有数据源均不可用';
			},
			
			// 构建数据源列表
			buildDataSourceList(primaryUrl) {
				const sources = [];
				// 本地/自定义数据激活时，禁止从远程获取 box_id.json
				try {
					const ds = uni.getStorageSync('dataSource');
					if (ds === 'local' || ds === 'custom') return sources;
				} catch (e) { this.logError(e); }
				const githubRepo = 'awadwd/ArknightsAuthorization_Series-mirror';
				const boxFile = 'Box_Id.json';
				
				// #ifdef H5
				if (primaryUrl.includes('raw.gitcode.com')) {
					sources.push({ name: 'GitCode(H5代理)', url: primaryUrl.replace('https://raw.gitcode.com', '/gitcode') });
				} else {
					sources.push({ name: '知晓云', url: primaryUrl });
				}
				// #endif
				
				// #ifndef H5
				if (primaryUrl.includes('gitcode.com')) {
					sources.push({ name: 'GitCode', url: primaryUrl });
				}
				sources.push({ name: 'jsDelivr(CDN)', url: 'https://cdn.jsdelivr.net/gh/' + githubRepo + '@main/' + boxFile });
				sources.push({ name: 'GitHub Raw', url: 'https://raw.githubusercontent.com/' + githubRepo + '/main/' + boxFile });
				sources.push({ name: 'GitCode(固定)', url: 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/blobs/305677c26b71ba53f916c48112b2b17afb23d32e/Box_Id.json' });
				// #endif
				
				if (primaryUrl && !primaryUrl.includes('gitcode.com')) {
					sources.push({ name: 'GitCode(备用)', url: 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json' });
				}
				

				
				// 读取用户在设置页保存的数据源顺序，按用户偏好重排备选源
				try {
					const order = uni.getStorageSync('dataSourceOrder');
					if (Array.isArray(order) && order.length) {
						const orderMap = { 'knowCloud':0, 'domestic':1, 'github':2, 'local':3, 'custom':4 };
						const orderIdx = v => (orderMap[v] !== undefined ? orderMap[v] : 99);
						const bucket = { knowCloud:[], domestic:[], github:[], local:[], custom:[] };
						sources.forEach(s => {
							let k = 'github';
							if (s.name === '知晓云') k = 'knowCloud';
							else if (s.name.startsWith('GitCode')) k = 'domestic';
							else if (s.name === 'jsDelivr(CDN)' || s.name === 'GitHub Raw') k = 'github';
							else if (s.name.includes('GitHub')) k = 'github';
							(bucket[k] = bucket[k] || []).push(s);
						});
						const sortedKeys = order.slice().sort((a,b) => orderIdx(a) - orderIdx(b));
						const reordered = [];
						sortedKeys.forEach(k => { (bucket[k]||[]).forEach(s => reordered.push(s)); });
						sources.splice(0, sources.length, ...reordered);
					}
				} catch (e) {
					this.logError(e);
					
				}
				
				return sources;
			},
			
			// 尝试从单个源下载
			tryDownloadFromSource(url) {
				return new Promise((resolve, reject) => {
					uni.request({
						url: url,
						method: 'GET',
						timeout: 15000,
						success: (res) => {
							if (res.statusCode === 200) {
								let data = res.data;
								if (typeof data === 'string') {
									try { data = JSON.parse(data); } catch (e) {
										this.logError(e);
										reject(new Error('JSON解析失败')); return; 
									}
								}
								if (Array.isArray(data)) { resolve(data); } else { reject(new Error('数据格式不正确')); }
							} else if (res.statusCode === 429) {
								reject(new Error('429 Rate Limited'));
							} else {
								reject(new Error('HTTP ' + res.statusCode));
							}
						},
						fail: (err) => reject(err)
					});
				});
			},
			
			// 处理下载成功
			async handleListDownloadSuccess(rawData, sourceName) {
				let data = rawData;
					boxDataSync.adopt(data, this.latestVersion);
				
				// 验证数据完整性
				this.validateDataStructure(data);
				
			// 存储成功使用的源信息
			this.currentVersion = this.latestVersion || this.currentVersion || '未知版本';
			console.log('[加载数据] 成功从 ' + sourceName + ' 获取数据');
				
			// 使用新的存储方式
				this.saveDataWithValidation(data);
				
				uni.showToast({ title: '更新成功 [' + sourceName + ']', icon: 'success', duration: 2000 });
			},
			
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
					this.logError(error);
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
					this.logError(error);
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
							hasMarketPrice = !!(market_price && (market_price.ELITE1 || market_price.ELITE2));
						}
						
						if (characterName && characterName.trim()) {
							console.log(`[getBox] ${characterName}: market_price=`, market_price, 'hasMP:', hasMarketPrice);
							characters.push({
								name: characterName,
								avatar: imageUrl || this.defaultAvatar,
								nolyELITE1: nolyELITE1,
								hotcharacter: hotcharacter,
								market_price: market_price,
								hasMarketPrice: hasMarketPrice,
								_avatarCtx: boxCtx(box)
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
			applyFilterAndSort(preservePage = false) {
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
				
				// 干员属性筛选：盒内至少有一位干员满足全部所选属性
				if (hasFacets(this.charFacets)) {
					filtered = filtered.filter(box => {
						for (let i = 1; i <= 10; i++) {
							const c = box['character' + i];
							const nm = typeof c === 'string' ? c : (c && c.name);
							if (nm && matchCharFacets(this.charFacets, nm)) return true;
						}
						return false;
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
				if (!preservePage) this.currentPage = 1;
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
					this.logError(error);
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
					this.logError(error);
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
					this.logError(error);
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
        handleJumpPage() {
            const page = parseInt(this.jumpPageInput);
            if (!page || page < 1) {
                uni.showToast({ title: '请输入有效页码', icon: 'none' });
                return;
            }
            const maxPage = this.totalPages;
            if (page > maxPage) {
                uni.showToast({ title: '超过最大页数(' + maxPage + ')', icon: 'none' });
                return;
            }
            this.currentPage = page;
            this.jumpPageInput = '';
        },

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
				this.facetExpanded = false;
				this.filterBodyHeight = 0;
				this.showAdvancedFilter = true;
				this.$nextTick(() => setTimeout(() => this.layoutFilterBody(), 60));
			},

			// 展开/收起干员属性后重新量一次高度
			toggleFacetPanel() {
				this.facetExpanded = !this.facetExpanded;
				this.filterBodyHeight = 0;
				this.$nextTick(() => setTimeout(() => this.layoutFilterBody(), 60));
			},

			/**
			 * scroll-view 在 App 端必须有确定高度才滚得动。
			 * 先让它按内容撑开，量出真实内容高度；超过卡片可用高度时再锁成固定像素高。
			 * 内容没超时保持自适应，避免弹窗永远占满 80vh。
			 */
			layoutFilterBody() {
				// 直接量卡片自身高度（已被 max-height 夹住），不用 vh 换算，避免与 webview 视口对不上
				const q = uni.createSelectorQuery();
				q.select('#afm-card').boundingClientRect();
				q.select('#afm-header').boundingClientRect();
				q.select('#afm-footer').boundingClientRect();
				q.select('#afm-body').boundingClientRect();
				q.exec((res) => {
					if (!res || !res[0] || !res[1] || !res[2] || !res[3]) return;
					const card = res[0], head = res[1], foot = res[2], body = res[3];
					// 扣掉头尾和 2px 余量，剩下才是滚动区可用高度
					// （padding 已挪到内层 .filter-scroll-inner，这里不用再扣）
					const avail = card.height - head.height - foot.height - 2;
					if (avail < 160) return;
					if (body.height > avail + 1) {
						this.filterBodyHeight = Math.floor(avail);
					}
				});
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
				this.charFacets = emptyFacets();
			},
			
			// 应用高级筛选
			applyAdvancedFilter() {
				this.applyFilterAndSort();
				this.closeAdvancedFilter();
			},


			// ===== 干员属性筛选（默认折叠） =====
			facetOptions(key) {
				return optionsOf(key, this.charFacets.prof);
			},

			facetOptionLabel(key, value) {
				return optionLabel(key, value);
			},

			toggleFacet(key, value) {
				const arr = this.charFacets[key];
				const i = arr.indexOf(value);
				if (i >= 0) arr.splice(i, 1);
				else arr.push(value);
				if (key === 'prof') {
					const valid = this.facetOptions('sub');
					this.charFacets.sub = this.charFacets.sub.filter((x) => valid.indexOf(x) >= 0);
				}
				// 即时生效，面板里的「命中 N 个盒号」会同步变化
				this.applyFilterAndSort();
			},

			clearFacet(key) {
				this.charFacets[key] = [];
				this.applyFilterAndSort();
			},

			// 整组全选（再点一次不会取消，用「清除」）
			selectAllFacet(key) {
				this.charFacets[key] = this.facetOptions(key).slice();
				if (key === 'prof') {
					const valid = this.facetOptions('sub');
					this.charFacets.sub = this.charFacets.sub.filter((x) => valid.indexOf(x) >= 0);
				}
				this.applyFilterAndSort();
			},

			// 清空全部干员属性条件
			clearAllFacets() {
				this.charFacets = emptyFacets();
				this.applyFilterAndSort();
			},

			// 只按干员属性统计（不受其它筛选项影响，方便判断单项条件是否生效）
			facetHitCount() {
				if (!hasFacets(this.charFacets)) return this.characterData.length;
				let n = 0;
				this.characterData.forEach((box) => {
					for (let i = 1; i <= 10; i++) {
						const c = box['character' + i];
						const nm = typeof c === 'string' ? c : (c && c.name);
						if (nm && matchCharFacets(this.charFacets, nm)) { n++; return; }
					}
				});
				return n;
			},

			facetSummaryText() {
				const n = this.facetCount();
				if (!n) return '未选条件 · 共 ' + this.characterData.length + ' 个盒号';
				return '已选 ' + n + ' 项 · 命中 ' + this.facetHitCount() + ' 个盒号';
			},

			facetCount() {
				let n = 0;
				this.facetGroups.forEach((g) => { n += (this.charFacets[g.key] || []).length; });
				return n;
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
		flex-direction: column;
		gap: 16rpx;
		background-color: #fff;
		border-radius: 16rpx;
		padding: 20rpx 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}

	.pagination-info {
		display: flex;
		flex-direction: row;
		flex-wrap: wrap;
		align-items: baseline;
		gap: 8rpx 20rpx;
	}

	.page-text {
		font-size: 28rpx;
		color: #333;
		font-weight: bold;
	}

	.data-count {
		font-size: 24rpx;
		color: #999;
	}

	.advanced-filter-tip {
		font-size: 22rpx;
		color: #E6A23C;
	}

	.pagination-controls {
		display: flex;
		flex-direction: column;
		align-items: stretch;
		gap: 12rpx;
		width: 100%;
	}

	/* 四个翻页按钮等宽排一行：窄屏也不会再挤成大小不一、上下错位 */
	.page-btn-row {
		display: flex;
		flex-direction: row;
		gap: 10rpx;
		width: 100%;
	}

	.page-btn {
		flex: 1 1 0;
		min-width: 0;
		white-space: nowrap;
		text-align: center;
		background-color: #409EFF;
		color: #fff;
		border-radius: 8rpx;
		font-size: 24rpx;
		line-height: 1.6;
		padding: 8rpx 0;
		margin: 0;
	}

	.page-btn:disabled {
		background-color: #c0c4cc;
		color: #fff;
		opacity: 0.6;
	}

	/* 页码 + 跳转横排：之前竖着叠成两行，把整行高度撑歪 */
	.jump-group {
		display: flex;
		flex-direction: row;
		align-items: center;
		justify-content: flex-end;
		gap: 10rpx;
	}

	.jump-input {
		border: 1px solid #dcdfe6;
		border-radius: 6rpx;
		padding: 4rpx 10rpx;
		font-size: 22rpx;
		width: 110rpx;
		text-align: center;
		background-color: #fff;
		flex: 0 0 auto;
	}

	.jump-btn {
		background-color: #67C23A;
		color: #fff;
		border-radius: 6rpx;
		font-size: 22rpx;
		line-height: 1.6;
		padding: 6rpx 20rpx;
		margin: 0;
		/* 宽屏下这一行比较挤，不加 nowrap 会把「跳转」拆成两行 */
		white-space: nowrap;
		flex: 0 0 auto;
	}

	/* ── 宽屏：分页并进筛选卡，自身不再画卡片底与阴影 ───────────────── */
	.pagination-inline {
		grid-column: 1 / -1;
		flex-direction: row;
		align-items: center;
		justify-content: space-between;
		gap: 20rpx;
		background: transparent;
		box-shadow: none;
		border-radius: 0;
		padding: 22rpx 0 0;
		margin: 22rpx 0 0;
		border-top: 1rpx solid #eee;
	}

	.pagination-inline .pagination-controls {
		flex-direction: row;
		align-items: center;
		width: auto;
		gap: 16rpx;
	}

	.pagination-inline .page-btn-row {
		width: auto;
		gap: 8rpx;
	}

	.pagination-inline .page-btn {
		flex: 0 0 auto;
		padding: 8rpx 18rpx;
	}

	.pagination-inline .jump-group {
		justify-content: flex-start;
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
	}/* ========== 界园风格 ========== */
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
	/* ========== 卡片大图与基本信息 ========== */
	.box-image-wrapper {
		position: relative;
		margin: 16rpx 20rpx 8rpx;
		background: #f5f5f5;
		border-radius: 12rpx;
		overflow: hidden;
		max-height: 240rpx;
		transition: max-height 0.3s ease;
	}
	.box-image-wrapper.expanded {
		max-height: 1200rpx;
	}
	.box-image {
		width: 100%;
		display: block;
	}
	.image-toggle-hint {
		position: absolute;
		bottom: 8rpx;
		right: 16rpx;
		background: rgba(0,0,0,0.5);
		padding: 4rpx 16rpx;
		border-radius: 20rpx;
		font-size: 22rpx;
	}
	.toggle-text { color: #fff; }

	.box-info-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 12rpx 24rpx;
		margin: 12rpx 20rpx 20rpx;
		padding: 16rpx;
		background: #fafafa;
		border-radius: 12rpx;
	}
	.info-item {
		display: flex;
		flex-direction: column;
		gap: 4rpx;
	}
	.info-replicate { grid-column: 1 / span 2; }
	.info-label { font-size: 22rpx; color: #999; }
	.info-value { font-size: 26rpx; color: #333; }
	.info-replicate-list { display: flex; flex-direction: column; }
	.replicate-line { font-size: 24rpx; color: #666; }

/* ========== Search.vue 风格卡片样式 ========== */
.box-card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.box-card-title {
	font-size: 36rpx;
	font-weight: bold;
	color: #409EFF;
}

.box-card-type {
	font-size: 22rpx;
	padding: 6rpx 16rpx;
	border-radius: 20rpx;
	color: #fff;
	font-weight: 500;
}

.box-image-section {
	margin: 0 -30rpx 20rpx;
	background: linear-gradient(135deg, #f5f7fa 0%, #e4e7ed 100%);
	position: relative;
}

.box-image-wrapper {
	max-height: 300rpx;
	overflow: hidden;
	transition: max-height 0.3s ease;
}

.box-image-wrapper.expanded {
	max-height: 1200rpx;
}

.box-official-image {
	width: 100%;
	display: block;
}

.image-preview-hint {
	position: absolute;
	bottom: 60rpx;
	left: 50%;
	transform: translateX(-50%);
	background: rgba(0, 0, 0, 0.5);
	padding: 8rpx 20rpx;
	border-radius: 20rpx;
	opacity: 0;
	transition: opacity 0.3s;
}

.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.image-expand-btn {
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 16rpx 0;
	background: linear-gradient(to bottom, rgba(255,255,255,0) 0%, rgba(255,255,255,0.9) 30%, #fff 100%);
	position: absolute;
	bottom: 0;
	left: 0;
	right: 0;
	cursor: pointer;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 24rpx;
	color: #409EFF;
}

.box-info-grid {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: 20rpx;
	margin-bottom: 25rpx;
	padding: 20rpx;
	background-color: #f8fafc;
	border-radius: 12rpx;
}

.info-item {
	display: flex;
	flex-direction: column;
	gap: 8rpx;
}

.info-label {
	font-size: 24rpx;
	color: #999;
}

.info-value {
	font-size: 28rpx;
	color: #333;
	font-weight: 500;
}

.replicate-section {
	grid-column: 1 / span 2;
	margin-top: 8rpx;
}

.replicate-list {
	display: flex;
	flex-direction: column;
	margin-top: 6rpx;
}

.replicate-line {
	font-size: 24rpx;
	color: #67C23A;
	line-height: 1.6;
}

.replicate-line-empty {
	font-size: 24rpx;
	color: #999;
	margin-top: 6rpx;
}

.characters-grid-compact {
	display: grid;
	grid-template-columns: repeat(2, 1fr);
	gap: 16rpx;
	margin-bottom: 20rpx;
}

.character-item-compact {
	display: flex;
	align-items: center;
	background-color: #f8fafc;
	border-radius: 12rpx;
	padding: 12rpx;
	transition: all 0.2s;
}

.character-avatar-wrap {
	position: relative;
	margin-right: 12rpx;
	flex-shrink: 0;
}

.character-avatar-compact {
	width: 60rpx;
	height: 60rpx;
	border-radius: 8rpx;
	border: 1rpx solid #e0e0e0;
}

.character-info-compact {
	flex: 1;
	min-width: 0;
}

.character-name-compact {
	font-size: 26rpx;
	color: #333;
	font-weight: 600;
	display: block;
	margin-bottom: 4rpx;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.elite-tag-compact {
	font-size: 20rpx;
	color: #E6A23C;
	background: rgba(230, 162, 60, 0.1);
	padding: 2rpx 8rpx;
	border-radius: 4rpx;
}
.character-market-price {
  margin-top: 4rpx;
  display: block;
}

.market-price-tag-c {
  font-size: 20rpx;
  color: #999;
  line-height: 1.4;
}

.mp-value-c {
  color: #666;
  font-weight: 500;
}

.box-card-actions {
	display: flex;
	gap: 20rpx;
	padding-top: 20rpx;
	border-top: 1rpx solid #f0f0f0;
}

.action-btn {
	flex: 1;
	font-size: 26rpx;
	padding: 16rpx 0;
	border-radius: 10rpx;
	text-align: center;
	font-weight: 500;
	border: none;
	background: transparent;
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

.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}


	/* ===== 干员属性筛选（折叠面板） ===== */
	.facet-toggle {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 20rpx 24rpx;
		border-radius: 12rpx;
		background: #f7f8fa;
	}

	.facet-toggle-right {
		display: flex;
		align-items: center;
	}

	.facet-count {
		margin-right: 10rpx;
		padding: 2rpx 12rpx;
		border-radius: 999rpx;
		font-size: 20rpx;
		color: #007aff;
		background: rgba(0, 122, 255, 0.1);
	}

	.facet-arrow {
		font-size: 32rpx;
		color: #bbb;
		transition: transform 0.2s;
	}

	.facet-arrow.open {
		transform: rotate(90deg);
	}

	.facet-panel {
		margin-top: 16rpx;
		padding: 0 4rpx;
	}

	.facet-group {
		padding: 12rpx 0;
		border-bottom: 1rpx dashed #eee;
	}

	.facet-group:last-child {
		border-bottom: none;
	}

	.facet-group-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 10rpx;
	}

	.facet-group-title {
		font-size: 24rpx;
		font-weight: 600;
		color: #444;
	}

	.facet-ops {
		display: flex;
		align-items: center;
	}

	.facet-op {
		margin-left: 24rpx;
		padding: 2rpx 0;
		font-size: 22rpx;
		color: #999;
	}

	.facet-summary {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin: 0 0 16rpx;
		padding: 14rpx 18rpx;
		border-radius: 10rpx;
		background: rgba(0, 122, 255, 0.06);
	}

	.facet-summary-text {
		font-size: 22rpx;
		color: #007aff;
	}

	.facet-summary-clear {
		margin-left: 16rpx;
		font-size: 22rpx;
		color: #007aff;
		text-decoration: underline;
	}

	.facet-chips {
		display: flex;
		flex-wrap: wrap;
	}

	.facet-chip {
		margin: 0 12rpx 12rpx 0;
		padding: 10rpx 20rpx;
		border-radius: 8rpx;
		font-size: 23rpx;
		line-height: 1.3;
		color: #555;
		background: #f2f3f5;
		border: 1rpx solid transparent;
	}

	.facet-chip.active {
		color: #007aff;
		background: rgba(0, 122, 255, 0.08);
		border-color: #007aff;
	}

	.facet-result-row {
		padding: 4rpx 0 14rpx;
}
	.facet-result {
		font-size: 22rpx;
		color: #007aff;
}
	.facet-tip {
		display: block;
		padding: 12rpx 0 4rpx;
		font-size: 21rpx;
		line-height: 1.6;
		color: #999;
	}

	/* 高级筛选弹窗：关闭按钮固定贴右 */
	.advanced-filter-modal .close-btn {
		margin-left: auto !important;
		margin-right: 0 !important;
		flex-shrink: 0;
	}

	/* ===== 高级筛选弹窗：滚动修复 =====
	 * 问题：scroll-view 在 App 端没有确定高度就滚不动 —— 内容一多会直接顶开卡片，
	 * 底部被 overflow:hidden 裁掉，手指也滑不动。
	 * 处理：默认按内容撑开（flex:0 0 auto）先量出真实高度，超过卡片能给的余量时，
	 * 由 layoutFilterBody() 写入固定像素高（行内 style），scroll-view 内部才滚得动。
	 */
	.advanced-filter-modal .modal-header,
	.advanced-filter-modal .modal-footer {
		flex-shrink: 0;
	}

	.advanced-filter-modal .modal-body {
		/* 高度由 layoutFilterBody() 实测后写入行内样式，
		   这里只保证在未写入前按内容撑开（不要用 flex:1，否则量不到真实内容高度） */
		flex: 0 0 auto;
		min-height: 0;
		/* 行内 height 是含内边距的总高，必须 border-box */
		box-sizing: border-box;
		/* 内边距交给内层，滚动区高度计算才干净 */
		padding: 0 !important;
	}

	.advanced-filter-modal .filter-scroll-inner {
		padding: 30rpx 30rpx 48rpx;
	}

	/* 底部按钮区永远露出，不参与压缩 */
	.advanced-filter-modal .modal-footer {
		flex-shrink: 0;
	}

	.advanced-filter-modal .modal-footer {
		flex-wrap: wrap;
	}

	.footer-facet-hint {
		flex: 0 0 100%;
		font-size: 22rpx;
		color: #007aff;
		padding-bottom: 4rpx;
	}

	/* ── Windows 桌面宽屏：筛选/排序/高级筛选 排成一行 ───────────────── */
	@media (min-width: 720px) {
		html.is-win-shell .control-section {
			display: grid;
			grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
			column-gap: 24rpx;
			row-gap: 0;
			align-items: end;
		}

		html.is-win-shell .section-title {
			margin-bottom: 10rpx;
		}

		html.is-win-shell .advanced-filter-btn {
			width: auto;
			justify-self: end;
			padding: 15rpx 44rpx;
		}
	}


	/* 复刻时间：多条记录必须竖排（text 默认 inline，会连成一行） */
	.replicate-records {
		display: flex;
		flex-direction: column;
		gap: 2rpx;
	}

	/* ── 宽屏：两列「卡片」，一屏 10 盒也不至于散成一片 ───────────────── */
	@media (min-width: 720px) {
		html.is-win-shell .boxes-list {
			display: grid;
			grid-template-columns: repeat(2, minmax(0, 1fr));
			gap: 22rpx;
			align-items: start;
			padding: 4rpx 2rpx 8rpx;
		}

		html.is-win-shell .box-item {
			min-width: 0;
			padding: 20rpx 22rpx 18rpx;
			background: #ffffff;
			border: 1rpx solid #e9edf3;
			border-radius: 16rpx;
			border-bottom: 1rpx solid #e9edf3;
			box-shadow: 0 2rpx 10rpx rgba(28, 34, 43, 0.04);
		}

		/* 基础样式里有一条右列左分隔线，卡片化之后不再需要 */
		html.is-win-shell .box-item:nth-child(2n) {
			border-left: 1rpx solid #e9edf3;
		}

		/* 卡片下面是画布，不要再垫一层白底 */
		html.is-win-shell .boxes-list {
			background-color: transparent;
		}

		/* uni 的 <image mode="widthFix"> 会用 background-size:100% 100% 把长图压扁，
		   这里强制等比裁切，只取中间一段做头图 */
		html.is-win-shell .box-image-wrapper :deep(div) {
			background-size: cover !important;
			background-position: center !important;
		}

		html.is-win-shell .box-card-header {
			margin-bottom: 12rpx;
			padding-bottom: 10rpx;
		}

		html.is-win-shell .box-card-title {
			font-size: 32rpx;
		}

		/* 图片：压成「头图」高度，点开才放大，避免一屏只剩一张图 */
		html.is-win-shell .box-image-section {
			margin: 0 0 12rpx;
			border-radius: 12rpx;
			overflow: hidden;
		}

		html.is-win-shell .box-image-wrapper {
			margin: 0;
			max-height: 180rpx;
			border-radius: 12rpx;
		}

		html.is-win-shell .box-image-wrapper.expanded {
			max-height: 520rpx;
		}

		html.is-win-shell .box-official-image {
			max-height: 180rpx;
			object-fit: cover;
		}

		html.is-win-shell .box-image-wrapper.expanded .box-official-image {
			max-height: 520rpx;
			object-fit: contain;
		}

		html.is-win-shell .box-info-grid {
			margin: 0 0 14rpx;
			padding: 14rpx 16rpx;
			gap: 10rpx 24rpx;
		}

		/* 窄了一半的卡片里，2 列头像格浪费横向空间 → 3 列更紧凑 */
		html.is-win-shell .characters-grid-compact {
			grid-template-columns: repeat(3, minmax(0, 1fr));
			gap: 10rpx;
			margin-bottom: 12rpx;
		}

		html.is-win-shell .character-item-compact {
			padding: 10rpx;
		}

		/* 反馈是次要操作，宽屏下不要拉成一根绿色大横条 */
		html.is-win-shell .feedback-btn {
			display: block;
			width: 150rpx;
			margin: 4rpx 0 0 auto;
			padding: 8rpx 0;
			font-size: 22rpx;
			line-height: 1.6;
			border-radius: 8rpx;
		}

		/* ── 深色主题：列表页原本只有浅色一套色，这里补上深色底 ─────────── */
		html.is-win-shell .theme-ark .control-section,
		html.is-win-shell .theme-liquidDark .control-section,
		html.is-win-shell .theme-ark .pagination-section,
		html.is-win-shell .theme-liquidDark .pagination-section,
		html.is-win-shell .theme-ark .market-price-status-section,
		html.is-win-shell .theme-liquidDark .market-price-status-section,
		html.is-win-shell .theme-ark .empty-state,
		html.is-win-shell .theme-liquidDark .empty-state,
		html.is-win-shell .theme-ark .box-item,
		html.is-win-shell .theme-liquidDark .box-item {
			background: #1c2026;
			border-color: rgba(255, 255, 255, 0.08);
		}

		html.is-win-shell .theme-ark .box-info-grid,
		html.is-win-shell .theme-liquidDark .box-info-grid,
		html.is-win-shell .theme-ark .character-item-compact,
		html.is-win-shell .theme-liquidDark .character-item-compact,
		html.is-win-shell .theme-ark .jump-input,
		html.is-win-shell .theme-liquidDark .jump-input {
			background: #242931;
		}

		html.is-win-shell .theme-ark .box-card-header,
		html.is-win-shell .theme-liquidDark .box-card-header {
			border-bottom-color: rgba(255, 255, 255, 0.08);
		}

		html.is-win-shell .theme-ark .box-card-title,
		html.is-win-shell .theme-liquidDark .box-card-title {
			color: #7fb6f5;
		}

		html.is-win-shell .theme-ark .info-value,
		html.is-win-shell .theme-liquidDark .info-value,
		html.is-win-shell .theme-ark .character-name-compact,
		html.is-win-shell .theme-liquidDark .character-name-compact {
			color: #e6e8eb;
		}

		/* 图片底色 / 展开渐变 / 收藏角标在深色下都要压暗，否则会发白 */
		html.is-win-shell .theme-ark .box-image-section,
		html.is-win-shell .theme-liquidDark .box-image-section {
			background: #242931;
		}

		html.is-win-shell .theme-ark .image-expand-btn,
		html.is-win-shell .theme-liquidDark .image-expand-btn {
			background: linear-gradient(to bottom,
				rgba(28, 32, 38, 0) 0%,
				rgba(28, 32, 38, 0.9) 30%,
				#1c2026 100%);
		}

		html.is-win-shell .theme-ark .favorite-indicator,
		html.is-win-shell .theme-liquidDark .favorite-indicator {
			background-color: #2b313a;
			border-color: #1c2026;
			color: #c7cdd5;
		}

		/* 图片底色 / 展开渐变 / 收藏角标在深色下都要压暗，否则会发白 */
		html.is-win-shell .theme-ark .box-image-section,
		html.is-win-shell .theme-liquidDark .box-image-section {
			background: #242931;
		}

		html.is-win-shell .theme-ark .image-expand-btn,
		html.is-win-shell .theme-liquidDark .image-expand-btn {
			background: linear-gradient(to bottom,
				rgba(28, 32, 38, 0) 0%,
				rgba(28, 32, 38, 0.9) 30%,
				#1c2026 100%);
		}

		html.is-win-shell .theme-ark .favorite-indicator,
		html.is-win-shell .theme-liquidDark .favorite-indicator {
			background-color: #2b313a;
			border-color: #1c2026;
			color: #c7cdd5;
		}

		html.is-win-shell .theme-ark .info-label,
		html.is-win-shell .theme-liquidDark .info-label,
		html.is-win-shell .theme-liquidDark .section-title,
		html.is-win-shell .theme-ark .section-title,
		html.is-win-shell .theme-ark .picker-text,
		html.is-win-shell .theme-liquidDark .picker-text {
			color: #9aa0a6;
		}
	}
</style>
