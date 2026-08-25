<template>

			<view :class="['container', 'theme-' + themeMode]">
		<!-- 搜索区域 -->
		<view class="search-section" :class="{ 'collapsed': toolbarCollapsed }">
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
				
				
				<view class="scan-btn-icon" @click="startScan">
				<image class="scan-btn-icon-img" src="/static/scan-barcode.png" mode="aspectFit"></image>
				</view>
				<button class="advanced-filter-btn" @click="openAdvancedFilter">筛选</button>
				
			</view>
			
			<!-- 搜索建议下拉列表 -->
			<view class="suggestions-dropdown" v-if="showSearchSuggestions && searchSuggestions.length > 0">
				<scroll-view class="suggestions-list" scroll-y>
					<view 
						class="suggestion-item" 
						v-for="(suggestion, index) in displayedSuggestions" 
						:key="index"
						@click="selectSuggestion(suggestion)"
					>
						<view class="suggestion-content">
							<text class="suggestion-name">{{ suggestion.name }}</text>
							<view class="suggestion-tags">
								<text v-if="searchType === 'character' && suggestion.englishname" class="suggestion-tag english">英文名</text>
								<text v-if="searchType === 'character' && suggestion.japanesename" class="suggestion-tag japanese">日文名</text>
								<text v-if="searchType === 'character' && suggestion.searchword && suggestion.searchword.length > 0" class="suggestion-tag nickname">外号</text>
								<text v-if="searchType === 'box'" class="suggestion-tag" :class="getBoxTypeClass(suggestion.boxType)">{{ getBoxTypeText(suggestion.boxType) }}</text>
							</view>
						</view>
						<text class="suggestion-type">{{ getSuggestionTypeText(suggestion.type) }}</text>
					</view>
					
					<view 
						class="load-more" 
						v-if="hasMoreSuggestions"
						@click="loadMoreSuggestions"
					>
						<text class="load-more-text">加载更多...</text>
					</view>
				</scroll-view>
			</view>
			
			<!-- 点击空白区域收起的遮罩层 -->
			<view 
				class="suggestions-mask" 
				v-if="showSearchSuggestions" 
				@click="hideSuggestions"
			></view>
			
			<view class="search-options" v-if="!showSearchSuggestions">
				<button 
					class="option-btn" 
					:class="{ active: searchType === 'character' }"
					@click="setSearchType('character')"
				>
					按干员名查询盒号
				</button>
				<button 
					class="option-btn" 
					:class="{ active: searchType === 'box' }"
					@click="setSearchType('box')"
				>
					按盒号查询干员
				</button>
			</view>
			
			<!-- 搜索提示 -->
			<view class="search-tips" v-if="hasEnabledSearchModes && searchType === 'character' && !showSearchSuggestions">
				<text class="search-tip-text">已启用: 
					<text v-if="enableEnglishSearch" class="search-tag">英文搜索</text>
					<text v-if="enableJapaneseSearch" class="search-tag">日文搜索</text>
					<text v-if="enableNicknameSearch" class="search-tag">外号搜索</text>
				</text>
			</view>
			
			
			<button class="search-btn" @click="handleSearch" v-if="!showSearchSuggestions">查询</button>
		</view>
		
		<!-- 结果展示区域 -->
		<view class="result-section" v-if="searchResults.length > 0">
			<view class="result-header">
				<text class="result-title">查询结果</text>
				<text class="result-count">共找到 {{ searchResults.length }} 个结果</text>
				<!-- 新增：显示高级筛选已应用提示 -->
				<text v-if="hasAdvancedFilter" class="advanced-filter-tip">高级筛选已应用</text>
			</view>
			
			<scroll-view class="result-list" scroll-y @scroll="onResultScroll">
				<view 
					class="result-item box-card" 
					v-for="(result, index) in searchResults" 
					:key="index"
				>
					<!-- 按干员名查询盒号的结果展示 - 卡片样式 -->
					<view v-if="searchType === 'character'">
						<!-- 角色信息头部 -->
						<view class="character-card-header">
							<view class="character-avatar-container">
								<image 
									class="character-avatar-large" 
									:src="result.avatar" 
									mode="aspectFit" 
									@error="onAvatarError(index)"
								></image>
								<image 
									v-if="result.hotcharacter" 
									class="hot-character-icon" 
									src="/static/hot.png" 
									mode="aspectFit"
								></image>
							</view>
							<view class="character-info-main">
								<text class="character-name-main">{{ result.name || result.characterName }}</text>
								<text v-if="result.nolyELITE1" class="elite-tag">仅精一</text>
								<text class="match-type" v-if="result.matchType && result.matchType !== 'chinese'">
									通过{{ getMatchTypeText(result.matchType) }}匹配
								</text>
							</view>
						</view>
						
						<!-- 所在盒号标签列表 -->
						<view class="box-tags-section">
							<text class="section-label">出现在以下盒号中（共 {{ result.boxIds.length }} 盒）：</text>
							<view class="box-tags-list">
								<view 
									class="box-tag"
									v-for="(boxId, boxIndex) in result.boxIds" 
									:key="boxIndex"
									@click="showBoxDetail(boxId)"
								>
									<text class="box-tag-text">{{ boxId }}</text>
									<text class="box-tag-arrow">▶</text>
								</view>
							</view>
						</view>
						
						<!-- 底部操作按钮 -->
						<view class="character-card-actions">
							<button 
								class="action-btn favorite-btn"
								:class="isFavoriteCharacter(result.characterName) ? 'favorited' : ''"
								@click="toggleFavoriteCharacter(result.characterName)"
							>
								{{ isFavoriteCharacter(result.characterName) ? '已收藏' : '收藏' }}
							</button>
						</view>
					</view>
					
					<!-- 按盒号查询干员的结果展示 - 卡片样式 -->
					<view v-if="searchType === 'box'">
						<!-- 卡片头部：盒号标题 -->
						<view class="box-card-header">
							<text class="box-card-title">{{ result.boxId }}</text>
							<text class="box-card-type" :class="getBoxTypeClass(result.boxType)">
								{{ getBoxTypeText(result.boxType) }}
							</text>
						</view>
						
						<!-- 官方图片 -->
						<view class="box-image-section" v-if="result.Box_ImageUrl">
							<view 
								class="box-image-wrapper"
								:class="{ 'expanded': result.isImageExpanded }"
								@click="previewBoxImage(result.Box_ImageUrl)"
							>
								<image 
									class="box-official-image" 
									:src="result.Box_ImageUrl" 
									mode="widthFix"
									@error="onBoxImageError(result.boxId)"
									@load="onBoxImageLoad(result, $event)"
								></image>
								<view class="image-preview-hint">
									<text class="preview-hint-text">🔍 点击查看大图</text>
								</view>
							</view>
							<!-- 展开/收起按钮 -->
							<view 
								class="image-expand-btn"
								v-if="result.showExpandBtn"
								@click.stop="toggleImageExpand(result)"
							>
								<text class="expand-icon">{{ result.isImageExpanded ? '▲' : '▼' }}</text>
								<text class="expand-text">{{ result.isImageExpanded ? '收起' : '展开' }}</text>
							</view>
						</view>
						
						<!-- 基本信息网格 -->
						<view class="box-info-grid">
							<view class="info-item">
								<text class="info-label">首发日期</text>
								<text class="info-value">{{ formatDate(result.release_date) }}</text>
							</view>
							<view class="info-item">
								<text class="info-label">价格</text>
								<text class="info-value">{{ result.retail_price || '未知' }}</text>
							</view>
							<view class="info-item">
								<text class="info-label">类型</text>
								<text class="info-value">{{ result.type === true || result.type === 'true' ? '盲抽' : '单领' }}</text>
							</view>
							<view class="info-item replicate-section">
								<text class="info-label">复刻记录</text>
								<view class="replicate-list" v-if="getReplicateRecords(result).length">
									<text class="replicate-line" v-for="(rec, rIdx) in getReplicateRecords(result)" :key="rIdx">{{ rec }}</text>
								</view>
								<text class="replicate-line-empty" v-else>暂无记录</text>
							</view>
						</view>
						
						<!-- 干员网格（更小尺寸） -->
						<view class="characters-grid-compact">
							<view 
								class="character-item-compact" 
								v-for="(character, charIndex) in result.characters" 
								:key="charIndex"
								@click="onCharacterInBoxClick(character.name, result.boxId)"
							>
								<view class="character-avatar-wrap">
									<image 
										class="character-avatar-compact" 
										:src="character.avatar" 
										mode="aspectFit"
										@error="onAvatarListError(index, charIndex)"
									></image>
									<image 
										v-if="character.hotcharacter" 
										class="hot-character-icon" 
										src="/static/hot.png" 
										mode="aspectFit"
									></image>
									<!-- 收藏状态指示器 -->
									<view 
										class="favorite-indicator" @click.stop="toggleFavoriteCharacterDirectly(character.name)" 
										:class="isFavoriteCharacter(character.name) ? 'favorited' : ''"
									>
										{{ isFavoriteCharacter(character.name) ? '★' : '☆' }}
									</view>
								</view>
								<view class="character-info-compact">
									<text class="character-name-compact">{{ character.name }}</text>
									<text v-if="character.nolyELITE1" class="elite-tag-compact">仅精一</text>
								</view>
							</view>
						</view>
						
						<!-- 底部按钮 -->
						<view class="box-card-actions">
							<button 
								class="action-btn favorite-btn"
								@click="openBoxCharactersModal(result.boxId, result.characters)"
							>
								收藏
							</button>
							<button 
								class="action-btn feedback-btn"
								@click="goToBoxFeedback(result.boxId)"
							>
								反馈纠错
							</button>
						</view>
					</view>
				</view>
			</scroll-view>
		</view>
		
		<!-- 空状态 -->
		<view class="empty-state" v-if="hasSearched && searchResults.length === 0">
			<image class="empty-icon" src="/static/empty-icon.png"></image>
			<text class="empty-text">未找到相关结果</text>
			<text class="empty-tip" v-if="hasEnabledSearchModes">尝试调整搜索词或检查设置中的搜索选项</text>
		</view>
		
		<!-- 数据状态显示 -->
		<view class="data-status" v-if="dataStatusMessage">
			<view class="data-status-main">
				<text class="status-text">{{ dataStatusMessage }}</text>
				<view class="data-status-actions">
					<button v-if="dataStatusMessage && (dataStatusMessage.includes('失败') || dataStatusMessage.includes('错误'))" 
							class="update-btn small" @click="downloadData">重试下载</button>
				</view>
			</view>
			
			<view class="data-details" v-if="localUpdateTime || cloudUpdateTime || currentVersion">
				<text class="data-detail-item" v-if="localUpdateTime">本地上次更新: {{ localUpdateTime }}</text>
				<text class="data-detail-item" v-if="cloudUpdateTime">云端最新更新: {{ formatCloudTime(cloudUpdateTime) }}</text>
				<text class="data-detail-item" v-if="characterData.length > 0">当前数据: {{ characterData.length }} 个盒号</text>
				<text class="data-detail-item" v-if="currentVersion">数据版本: {{ currentVersion }}</text>
			</view>
		</view>
		
		<!-- 更新数据按钮 -->
		<view class="update-section">
			<button class="update-btn" @click="downloadData">更新干员数据</button>
		</view>
		
		<!-- ============= 新增：高级筛选模态框 ============= -->
		<view class="advanced-filter-modal" v-if="showAdvancedFilter" @tap="closeAdvancedFilter">
			<view class="modal-content" @tap.stop>
				<view class="modal-header">
					<text class="modal-title">高级筛选</text>
					<button class="close-btn" @click="closeAdvancedFilter">×</button>
				</view>
				<scroll-view class="modal-body" scroll-y>
					<!-- 盒类型筛选下拉框 -->
					<view class="filter-group">
						<text class="filter-group-title">单盒类型</text>
						<view class="date-pickers">
					<picker 
						class="filter-picker" 
						@change="onFilterChange" 
						:value="filterIndex" 
						:range="filterOptions"
					>
						<view class="picker-text">{{ filterOptions[filterIndex] }}</view>
						<!-- 新增：高级筛选按钮 -->
						
						
					</picker>
					</view>
					</view>
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
									:class="isFavorite(box.boxId) ? 'favorited' : ''"
									@click.stop="toggleFavorite(box.boxId)"
								>
									{{ isFavorite(box.boxId) ? '★' : '☆' }}
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
									<view class="market-price-tags" v-if="character.market_price">
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
		
		<!-- 盒号详情弹窗（用于干员查询结果点击盒号标签） -->
		<view class="box-detail-modal" v-if="showBoxDetailModal" @click="closeBoxDetail">
			<view class="box-detail-content" @click.stop>
				<view class="box-detail-header">
					<view class="box-detail-header-left">
						<text class="box-detail-title">盒号 {{ selectedBox?.boxId }}</text>
						<text class="box-detail-type-tag" :class="'tag-' + getBoxTypeClass(selectedBox.boxType)">{{ getBoxTypeText(selectedBox.boxType) }}</text>
					</view>
					<button class="close-btn" @click="closeBoxDetail">×</button>
				</view>
				<scroll-view class="box-detail-body" scroll-y v-if="selectedBox">
					<!-- 官方图片 -->
					<view class="box-image-section" v-if="selectedBox.Box_ImageUrl">
						<view 
							class="box-image-wrapper"
							:class="{ 'expanded': selectedBox.isImageExpanded }"
							@click="previewBoxImage(selectedBox.Box_ImageUrl)"
						>
							<image 
								class="box-official-image" 
								:src="selectedBox.Box_ImageUrl" 
								mode="widthFix"
							></image>
							<view class="image-preview-hint">
								<text class="preview-hint-text">🔍 点击查看大图</text>
							</view>
						</view>
						<view 
							class="image-expand-btn"
							v-if="selectedBox.showExpandBtn"
							@click.stop="toggleImageExpand(selectedBox)"
						>
							<text class="expand-icon">{{ selectedBox.isImageExpanded ? '▲' : '▼' }}</text>
							<text class="expand-text">{{ selectedBox.isImageExpanded ? '收起' : '展开' }}</text>
						</view>
					</view>
					
					<!-- 基本信息 -->
					<view class="box-info-grid">
						<view class="info-item">
							<text class="info-label">首发日期</text>
							<text class="info-value">{{ formatDate(selectedBox.release_date) }}</text>
						</view>
						<view class="info-item">
							<text class="info-label">价格</text>
							<text class="info-value">{{ selectedBox.retail_price || '未知' }}</text>
						</view>
						<view class="info-item">
							<text class="info-label">类型</text>
							<text class="info-value">{{ selectedBox.type === true || selectedBox.type === 'true' ? '盲抽' : '单领' }}</text>
						</view>
						<view class="info-item replicate-section">
							<text class="info-label">复刻记录</text>
							<view class="replicate-list" v-if="getReplicateRecords(selectedBox).length">
								<text class="replicate-line" v-for="(rec, rIdx) in getReplicateRecords(selectedBox)" :key="rIdx">{{ rec }}</text>
							</view>
							<text class="replicate-line-empty" v-else>暂无记录</text>
						</view>
					</view>
					
					<!-- 干员列表 -->
					<view class="characters-section">
						<!-- <text class="section-title">干员列表（{{ selectedBox.characters?.length || 0 }} 位）</text> -->
						<view class="characters-grid-compact">
							<view 
								class="character-item-compact" 
								v-for="(character, charIndex) in selectedBox.characters" 
								:key="charIndex"
								@click="onCharacterInBoxClick(character.name, selectedBox.boxId)"
							>
								<view class="character-avatar-wrap">
									<image 
										class="character-avatar-compact" 
										:src="character.avatar" 
										mode="aspectFit"
									></image>
									<image 
										v-if="character.hotcharacter" 
										class="hot-character-icon" 
										src="/static/hot.png" 
										mode="aspectFit"
									></image>
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
								</view>
							</view>
						</view>
					</view>
				</scroll-view>
				<view class="box-detail-footer">
					<button 
						class="action-btn favorite-btn"
						@click="openBoxCharactersModal(selectedBox.boxId, selectedBox.characters)"
					>
						收藏
					</button>
					<button 
						class="action-btn feedback-btn"
						@click="goToBoxFeedback(selectedBox.boxId)"
					>
						反馈纠错
					</button>
				</view>
			</view>
		</view>
	</view>

		<!-- 干员所在其他盒号弹窗 -->
		<view class="modal-mask" v-if="showCharBoxesModal" @click="closeCharBoxesModal">
			<view class="char-boxes-modal" @click.stop>
				<view class="char-boxes-header">
					<text class="char-boxes-title">「{{ charBoxesName }}」还在这些盒号</text>
					<text class="modal-close" @click="closeCharBoxesModal">✕</text>
				</view>
				<scroll-view class="char-boxes-body" scroll-y>
					<view v-if="charBoxesList.length === 0" class="char-boxes-empty">
						<text>未在其他盒号中出现</text>
					</view>
					<view
						v-for="(bid, idx) in charBoxesList"
						:key="idx"
						class="char-box-chip"
						@click="openOtherBox(bid)"
					>
						<text class="char-box-chip-text">{{ bid }}</text>
						<text class="char-box-chip-arrow">▶</text>
					</view>
				</scroll-view>
			</view>
		</view>

</template>

<script>
	import errorLog from "@/utils/errorLog.js";
	// 开源版本：知晓云配置已禁用，使用 GitHub 直链
	const GITHUB_DATA_SOURCES = {
		searchWordUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/searchWord.json'
	};
	
	// 知晓云配置 - 开源版本clientId为空，商业版请配置知晓云
	const KNOW_CLOUD_CONFIG = {
		clientId: 'YOUR_KNOW_CLOUD_CLIENT_ID',
		baseUrl: 'https://YOUR_KNOW_CLOUD_CLIENT_ID.myminapp.com/hserve/v2.2',
		tableNames: {
			choearth_notice: 'choearth_notice',
			more_notice: 'more_notice',
			Version: 'Version',
			SearchWord_Version: 'SearchWord_Version'
		}
	}

	export default {
		data() {
			return {
				// 主题模式
				themeMode: 'simple',
				toolbarCollapsed: false,
				lastScrollTop: 0,
				hasEnoughResults: false,
				searchText: '',
				searchType: 'character',

				// 隐私协议（官方API）
				showPrivacyModal: false,
				privacyContractName: '《隐私保护指引》',
				pendingScanAction: false,
				searchResults: [],
				hasSearched: false,
				characterData: [],
				dataStatusMessage: '',
				cloudUpdateTime: '',
				localUpdateTime: '',
				currentVersion: '',
				dataUrl: '',
				defaultAvatar: '/static/default-avatar.png',
				shouldAutoSearch: false,
				
				// 筛选相关数据
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
				
				// 搜索设置
				enableEnglishSearch: false,
				enableJapaneseSearch: false,
				enableNicknameSearch: false,
				searchWords: [],
				customSearchWords: [],
				
				// 内置外号映射表（示例，真实数据从gitcode获取)
				builtinNicknames: {
					'阿米娅': ['兔兔', '阿米驴', 'amiya'],
					'温蒂': ['weedy', '推推'],
					'维什戴尔': ['ew', 'EW'],
					'逻各斯': ['李狗剩', 'Logos'],
					'星熊': ['鬼姐'],
					'塑心': ['阿尔图罗'],
					'凯尔希': ['老猞猁', '牢猫'],
				},
				
				// 盒号选择模态框相关数据
				showBoxSelectionModal: false,
				modalTitle: '',
				modalBoxList: [],
				
				// 盒内干员收藏模态框
				showBoxCharactersModal: false,
				currentBoxId: '',
				modalBoxCharacters: [],
				
				// 盒号详情弹窗（用于干员查询结果）
				showBoxDetailModal: false,
				showCharBoxesModal: false,
				charBoxesList: [],
				charBoxesName: '',
				barcodeMap: {},
				selectedBox: null,
				
				// 搜索建议相关数据
				showSearchSuggestions: false,
				searchSuggestions: [],
				suggestionsPage: 1,
				suggestionsPageSize: 10,
				suggestionLoadTimer: null,
				blurTimer: null,
				
				// 收藏数据
				favoriteBoxIds: [],
				favoriteCharacterNames: [],
				
				// 知晓云重试计数
				retryCount: 0,
				latestVersion: '',
				
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
			searchPlaceholder() {
				return this.searchType === 'character' ? 
					'请输入干员名称，如：阿米娅' : 
					'请输入盒号，如：1 或 1.0';
			},
			currentFilter() {
				return this.filterMap[this.filterIndex] || 'all';
			},
			hasEnabledSearchModes() {
				return this.enableEnglishSearch || this.enableJapaneseSearch || this.enableNicknameSearch;
			},
			displayedSuggestions() {
				const startIndex = 0;
				const endIndex = this.suggestionsPage * this.suggestionsPageSize;
				return this.searchSuggestions.slice(startIndex, endIndex);
			},
			hasMoreSuggestions() {
				return this.displayedSuggestions.length < this.searchSuggestions.length;
			},
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
		onLoad(options) {
			
			// 加载主题设置
			this.loadThemeSetting();

		    uni.setNavigationBarTitle({
		        title: '明日方舟通行证查询工具'
		    });

		    console.log('页面加载，开始初始化数据...');
		    
		    // func_search 跳转：按类型直接选择搜索方式
		    if (options.type) {
		        this.searchType = options.type; // 'character' 或 'box'
		    }
		    
		    if (options.searchText && options.searchType) {
		        this.searchText = decodeURIComponent(options.searchText);
		        this.searchType = options.searchType;
		        this.shouldAutoSearch = true;
		    }
		    
		    // 日历页跳转：自动填入盒号并触发搜索
		    if (options.fromCalendar === 'true') {
		        const calBoxId = uni.getStorageSync('calendarSearchBoxId');
		        if (calBoxId) {
		            this.searchType = 'box';
		            this.searchText = String(calBoxId);
		            this.shouldAutoSearch = true;
		            uni.removeStorageSync('calendarSearchBoxId');
		        }
		    }
		    
		    // 扫码页跳转：自动填入盒号并搜索
		    const scannedBoxId = uni.getStorageSync('scannedBoxId');
		    if (scannedBoxId) {
		        this.searchType = 'box';
		        this.searchText = String(scannedBoxId);
		        this.shouldAutoSearch = true;
		        uni.removeStorageSync('scannedBoxId');
		    }

		    // 扫码页跳转：自动填入干员名并搜索
		    const charName = uni.getStorageSync('searchCharacterName');
		    if (charName) {
		        this.searchType = 'character';
		        this.searchText = charName;
		        this.shouldAutoSearch = true;
		        uni.removeStorageSync('searchCharacterName');
		    }
		    
		    this.loadLocalData();
		    this.loadSearchSettings();
		    this.loadFavorites();
		    
		    // 先尝试从本地加载搜索词数据
		    this.loadSearchWordsFromLocal();
		    
		    // 如果本地没有数据或数据无效，尝试从云端获取
		    if (this.enableEnglishSearch || this.enableNicknameSearch) {
		        if (this.searchWords.length === 0) {
		            console.log('本地搜索词数据为空，开始从云端获取');
		            this.fetchSearchWordsFromCloud();
		        } else {
		            console.log('使用本地搜索词数据，数量:', this.searchWords.length);
		        }
		    }
		    
		    uni.showShareMenu({
		        withShareTicket: true,
		        menus: ['shareAppMessage', 'shareTimeline']
		    });
		},
		onShow() {
		    // 每次页面显示时重新加载搜索设置和收藏数据
		    this.loadSearchSettings();
		    this.loadFavorites();
		    
		    // 强制重新加载搜索词数据
		    this.loadSearchWordsFromLocal();
		    
		    console.log('Search页面 onShow - 重新加载搜索设置');
		    
		    // 检查是否需要重新获取搜索词数据
		    if (this.enableEnglishSearch || this.enableNicknameSearch) {
		        const lastFetchTime = uni.getStorageSync('lastSearchWordsFetch');
		        const now = Date.now();
		        // 如果超过30分钟没有更新，重新获取
		        if (!lastFetchTime || (now - lastFetchTime) > 1800000) {
		            this.fetchSearchWordsFromCloud();
		        } else {
		            console.log('搜索词数据在有效期内，使用本地缓存');
		        }
		    }
		},
		onShareAppMessage() {
			let title = '方舟通行证谷子查询工具-搜索工具';
			let path = '/pages/Search/Search';
			
			if (this.searchText && this.hasSearched) {
				if (this.searchType === 'character') {
					title = `明日方舟通行证 - ${this.searchText} 所在盒搜索结果`;
					path = `/pages/Search/Search?searchText=${encodeURIComponent(this.searchText)}&searchType=character`;
				} else {
					title = `明日方舟通行证 - 盒号 ${this.searchText} 干员搜索结果`;
					path = `/pages/Search/Search?searchText=${encodeURIComponent(this.searchText)}&searchType=box`;
				}
				
				if (this.searchResults.length > 0) {
					title += ` (${this.searchResults.length}个结果)`;
				}
			}
			
			return {
				title: title,
				path: path,
				imageUrl: ''
			}
		},
		onShareTimeline() {
			return {
				title: '方舟通行证谷子查询工具-搜索工具',
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
			onResultScroll(e) {
				const scrollTop = e.detail.scrollTop;
				const delta = scrollTop - this.lastScrollTop;
				const hasEnough = this.searchResults.length > 3;
				
				if (hasEnough) {
					if (delta > 10 && !this.toolbarCollapsed) {
						// 往下滑 → 收起工具栏
						this.toolbarCollapsed = true;
					} else if (delta < -10 && this.toolbarCollapsed) {
						// 往上滑 → 展开工具栏
						this.toolbarCollapsed = false;
					}
				}
				
				this.lastScrollTop = scrollTop;
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

			// 知晓云 API 请求封装
			async knowCloudRequest(tableName, options = {}, retryCount = 0) {
				if (retryCount > 3) {
					console.error('知晓云请求重试次数过多');
					throw new Error('知晓云请求重试次数过多');
				}
				
				try {
					const tableNameKey = KNOW_CLOUD_CONFIG.tableNames[tableName];
					if (!tableNameKey) {
						throw new Error(`未找到表 ${tableName} 的配置`);
					}
					
					let url = `${KNOW_CLOUD_CONFIG.baseUrl}/table/${tableNameKey}/record/`;
					
					if (options.data) {
						const params = [];
						Object.keys(options.data).forEach(key => {
							params.push(`${key}=${encodeURIComponent(options.data[key])}`);
						});
						if (params.length > 0) {
							url += `?${params.join('&')}`;
						}
					}
					
					const requestOptions = {
						url: url,
						method: options.method || 'GET',
						header: {
							'X-Hydrogen-Client-ID': KNOW_CLOUD_CONFIG.clientId,
							'Content-Type': 'application/json'
						},
						timeout: 15000,
						...options
					};
					
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
					if (retryCount < 3) {
						console.log(`第${retryCount + 1}次重试...`);
						return await this.knowCloudRequest(tableName, options, retryCount + 1);
					}
					throw error;
				}
			},
			
			// 从知晓云获取搜索词数据 - 修复版
			async fetchSearchWordsFromCloud() {
				try {
					console.log('开始从知晓云获取搜索词数据...');
					
					const res = await this.knowCloudRequest('SearchWord_Version', {
						data: {
							limit: 1000,
							offset: 0
						}
					});
					
					console.log('知晓云搜索词数据响应:', res);
					
					if (res && res.objects && res.objects.length > 0) {
						const firstObj = res.objects[0];
						
						// 检查是否是版本记录（有url字段），需要从url下载实际数据
						if (firstObj && firstObj.url) {
							console.log('检测到版本记录，开始下载搜索词JSON:', firstObj.url);
							
							const jsonRes = await new Promise((resolve, reject) => {
								uni.request({
									url: firstObj.url,
									success: (res2) => {
										if (res2.statusCode === 200 && res2.data) {
											resolve(res2.data);
										} else {
											reject(new Error('下载失败: ' + res2.statusCode));
										}
									},
									fail: reject
								});
							});
							
							// 处理下载的JSON数据
							const searchWordsData = this.processSearchWordData(jsonRes);
							this.saveSearchWordsData(searchWordsData);
							return;
						}
						
						// 直接是搜索词数据
						const searchWordsData = this.processSearchWordData(res.objects);
						this.saveSearchWordsData(searchWordsData);
					}
				} catch (error) {
					this.logError(error);
					console.error('从知晓云获取搜索词失败:', error);
					this.loadSearchWordsFromLocal();
				}
			},
			
			// 处理搜索词数据（提取公共逻辑）
			processSearchWordData(data) {
				const searchWordsData = [];
				const items = Array.isArray(data) ? data : (data.objects || []);
				
				items.forEach(item => {
					// 检查是否是 {character1: {name, englishname, serachword}} 格式
					let charData = item;
					const keys = Object.keys(item);
					if (keys.length === 1 && typeof item[keys[0]] === 'object') {
						// 是嵌套格式，取内层对象
						charData = item[keys[0]];
					}
					
					// 跳过无效数据
					if (!charData.name || charData.name.trim() === '') {
						return;
					}
					
					// 处理searchword字段（注意：原始数据中是 serachword 拼写错误）
					let searchword = [];
					const searchWordSource = charData.searchword || charData.serachword || charData.SearchWord || charData.searchWord;
					if (searchWordSource) {
						if (Array.isArray(searchWordSource)) {
							searchword = searchWordSource;
						} else if (typeof searchWordSource === 'string') {
							try {
								const parsed = JSON.parse(searchWordSource);
								if (Array.isArray(parsed)) {
									searchword = parsed;
								} else {
									searchword = searchWordSource.split(/[,，、]/).map(w => w.trim()).filter(w => w);
								}
							} catch (e) {
								this.logError(e);
								searchword = searchWordSource.split(/[,，、]/).map(w => w.trim()).filter(w => w);
							}
						}
					}
					
					// 处理其他字段
					const searchWordItem = {
						name: charData.name.trim(),
						englishname: (charData.englishname || charData.EnglishName || '').trim(),
						japanesename: (charData.japanesename || charData.JapaneseName || '').trim(),
						searchword: searchword
					};
					
					// 只添加有效的英文名和日文名
					if (searchWordItem.englishname === '') {
						delete searchWordItem.englishname;
					}
					if (searchWordItem.japanesename === '') {
						delete searchWordItem.japanesename;
					}
					
					// 如果searchword为空数组，删除该字段
					if (searchWordItem.searchword.length === 0) {
						delete searchWordItem.searchword;
					}
					
					searchWordsData.push(searchWordItem);
				});
				
				return searchWordsData;
			},
			
			// 保存搜索词数据到本地
			saveSearchWordsData(searchWordsData) {
				console.log('处理后的搜索词数据:', searchWordsData.length, '条');
				
				// 保存到本地存储
				uni.setStorageSync('searchWords', searchWordsData);
				uni.setStorageSync('lastSearchWordsFetch', Date.now());
				
				// 更新当前页面的搜索词
				this.searchWords = searchWordsData;
				console.log('搜索词数据获取成功，数量:', searchWordsData.length);
				
				// 显示成功提示
				setTimeout(() => {
					uni.showToast({
						title: '搜索词数据已更新',
						icon: 'success',
						duration: 2000
					});
				}, 500);
			},
			
			/// 从本地存储加载搜索词数据 - 增强版本
loadSearchWordsFromLocal() {
    try {
        console.log('开始从本地加载搜索词数据...');
        
        const localSearchWords = uni.getStorageSync('searchWords');
        console.log('从本地存储读取的searchWords:', localSearchWords);
        
        if (localSearchWords && Array.isArray(localSearchWords) && localSearchWords.length > 0) {
            this.searchWords = localSearchWords;
            console.log('从本地加载搜索词数据成功，数量:', localSearchWords.length);
            
            // 验证数据格式
            const validWords = this.validateSearchWords(localSearchWords);
            console.log('有效搜索词数据数量:', validWords);
            
            // 如果数据无效，尝试从云端重新获取
            if (validWords === 0) {
                console.warn('本地搜索词数据格式无效，尝试重新获取');
                if (this.enableEnglishSearch || this.enableNicknameSearch) {
                    this.fetchSearchWordsFromCloud();
                }
            }
        } else {
            console.warn('本地无搜索词数据或数据格式不正确');
            this.searchWords = [];
            
            // 如果启用了搜索功能但本地没有数据，自动获取
            if (this.enableEnglishSearch || this.enableNicknameSearch) {
                console.log('启用了搜索功能但无本地数据，开始从云端获取');
                this.fetchSearchWordsFromCloud();
            }
        }
    } catch (e) {
    	this.logError(e);
        console.error('从本地加载搜索词数据失败:', e);
        this.searchWords = [];
    }
},

// 验证搜索词数据格式
validateSearchWords(searchWords) {
    if (!Array.isArray(searchWords)) return 0;
    
    let validCount = 0;
    searchWords.forEach(item => {
        // 检查是否包含必需字段
        if (item && item.name && typeof item.name === 'string' && item.name.trim() !== '') {
            validCount++;
        }
    });
    
    return validCount;
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
					this.logError(error);
					console.error('从知晓云获取URL失败:', error);
					throw error;
				}
			},
			
			// 搜索相关方法
			onSearchFocus() {
				this.showSearchSuggestions = true;
				this.loadSearchSuggestions();
				
				if (this.blurTimer) {
					clearTimeout(this.blurTimer);
					this.blurTimer = null;
				}
			},
			
			onSearchBlur() {
				// 不再自动隐藏
			},
			
			onSearchInput() {
				if (this.suggestionLoadTimer) {
					clearTimeout(this.suggestionLoadTimer);
				}
				this.suggestionLoadTimer = setTimeout(() => {
					this.loadSearchSuggestions();
				}, 300);
			},
			
			hideSuggestions() {
				this.showSearchSuggestions = false;
				
				if (this.blurTimer) {
					clearTimeout(this.blurTimer);
					this.blurTimer = null;
				}
			},
			
			loadSearchSuggestions() {
				if (!this.showSearchSuggestions) return;
				this.suggestionsPage = 1;
				if (this.searchType === 'character') {
					this.loadCharacterSuggestions();
				} else {
					this.loadBoxSuggestions();
				}
			},
			
			loadCharacterSuggestions() {
				const allSuggestions = [];
				
				// 从搜索词数据中添加
				if (this.searchWords && this.searchWords.length > 0) {
					this.searchWords.forEach(item => {
						if (item.name) {
							const suggestion = {
								name: item.name,
								type: 'searchWords'
							};
							
							if (item.englishname) {
								suggestion.englishname = item.englishname;
							}
							if (item.japanesename) {
								suggestion.japanesename = item.japanesename;
							}
							if (item.searchword && Array.isArray(item.searchword) && item.searchword.length > 0) {
								suggestion.searchword = item.searchword;
							}
							
							allSuggestions.push(suggestion);
						}
					});
				}
				
				// 从自定义搜索词中添加
				if (this.customSearchWords && this.customSearchWords.length > 0) {
					this.customSearchWords.forEach(item => {
						if (item.characterName) {
							allSuggestions.push({
								name: item.characterName,
								words: item.words || [],
								type: 'customSearchWords'
							});
						}
					});
				}
				
				// 从内置外号中添加
				Object.keys(this.builtinNicknames).forEach(characterName => {
					allSuggestions.push({
						name: characterName,
						nicknames: this.builtinNicknames[characterName],
						type: 'builtinNicknames'
					});
				});
				
				// 从干员数据中添加
				if (this.characterData && this.characterData.length > 0) {
					const characterNames = new Set();
					this.characterData.forEach(box => {
						for (let i = 1; i <= 10; i++) {
							const charKey = `character${i}`;
							if (box[charKey]) {
								let characterName = '';
								if (typeof box[charKey] === 'string') {
									characterName = box[charKey];
								} else if (box[charKey].name) {
									characterName = box[charKey].name;
								}
								if (characterName && characterName.trim() && !characterNames.has(characterName)) {
									characterNames.add(characterName);
									const exists = allSuggestions.some(suggestion => suggestion.name === characterName);
									if (!exists) {
										allSuggestions.push({
											name: characterName,
											type: 'characterData'
										});
									}
								}
							}
						}
					});
				}
				
				// 根据搜索文本过滤
				let filteredSuggestions = allSuggestions;
				if (this.searchText.trim()) {
					const searchTerm = this.searchText.trim().toLowerCase();
					filteredSuggestions = allSuggestions.filter(suggestion => {
						if (suggestion.name.toLowerCase().includes(searchTerm)) return true;
						if (suggestion.englishname && suggestion.englishname.toLowerCase().includes(searchTerm)) return true;
						if (suggestion.japanesename && suggestion.japanesename.toLowerCase().includes(searchTerm)) return true;
						if (suggestion.searchword && Array.isArray(suggestion.searchword)) {
							for (const nickname of suggestion.searchword) {
								if (nickname.toLowerCase().includes(searchTerm)) return true;
							}
						}
						if (suggestion.words && Array.isArray(suggestion.words)) {
							for (const word of suggestion.words) {
								if (word.toLowerCase().includes(searchTerm)) return true;
							}
						}
						if (suggestion.nicknames && Array.isArray(suggestion.nicknames)) {
							for (const nickname of suggestion.nicknames) {
								if (nickname.toLowerCase().includes(searchTerm)) return true;
							}
						}
						return false;
					});
				}
				
				// 去重并排序
				const uniqueSuggestions = [];
				const seenNames = new Set();
				filteredSuggestions.forEach(suggestion => {
					if (!seenNames.has(suggestion.name)) {
						seenNames.add(suggestion.name);
						uniqueSuggestions.push(suggestion);
					}
				});
				uniqueSuggestions.sort((a, b) => a.name.localeCompare(b.name, 'zh-CN'));
				this.searchSuggestions = uniqueSuggestions;
			},
			
			loadBoxSuggestions() {
				const allBoxSuggestions = [];
				if (this.characterData && this.characterData.length > 0) {
					const seenBoxIds = new Set();
					this.characterData.forEach(box => {
						const boxId = box.Box_id;
						const boxType = box.Box_type || 'normal';
						if (boxId && !seenBoxIds.has(boxId)) {
							seenBoxIds.add(boxId);
							allBoxSuggestions.push({
								name: boxId,
								boxType: boxType,
								type: 'boxData'
							});
						}
					});
				}
				let filteredSuggestions = allBoxSuggestions;
				if (this.searchText.trim()) {
					const searchTerm = this.searchText.trim().toLowerCase();
					filteredSuggestions = allBoxSuggestions.filter(suggestion => {
						if (suggestion.name.toLowerCase().includes(searchTerm)) return true;
						const boxTypeText = this.getBoxTypeText(suggestion.boxType).toLowerCase();
						if (boxTypeText.includes(searchTerm)) return true;
						return false;
					});
				}
				filteredSuggestions.sort((a, b) => {
					const aNum = a.name.match(/\d+(\.\d+)?/);
					const bNum = b.name.match(/\d+(\.\d+)?/);
					if (aNum && bNum) {
						return parseFloat(aNum[0]) - parseFloat(bNum[0]);
					} else if (aNum) {
						return -1;
					} else if (bNum) {
						return 1;
					} else {
						return a.name.localeCompare(b.name, 'zh-CN');
					}
				});
				this.searchSuggestions = filteredSuggestions;
			},
			
			selectSuggestion(suggestion) {
				this.searchText = suggestion.name;
				this.showSearchSuggestions = false;
				setTimeout(() => {
					this.handleSearch();
				}, 100);
			},
			
			loadMoreSuggestions() {
				this.suggestionsPage += 1;
			},
			
			getSuggestionTypeText(type) {
				const typeMap = {
					'searchWords': '搜索词',
					'customSearchWords': '自定义',
					'builtinNicknames': '内置外号',
					'characterData': '干员数据',
					'boxData': '盒号数据'
				};
				return typeMap[type] || '未知';
			},
			
			setSearchType(type) {
				this.searchType = type;
				this.searchResults = [];
				this.hasSearched = false;
				this.searchText = '';
				this.showSearchSuggestions = false;
				this.filterIndex = 0;
			},
			
			onFilterChange(e) {
				this.filterIndex = parseInt(e.detail.value);
				// 筛选变化后，如果已搜索过，重新搜索
				if (this.hasSearched) {
					this.handleSearch();
				}
			},
			
			handleSearch() {
				if (!this.searchText.trim()) {
					uni.showToast({
						title: '请输入搜索内容',
						icon: 'none'
					});
					return;
				}
				if (this.characterData.length === 0) {
					uni.showToast({
						title: '数据未加载，请先更新数据',
						icon: 'none'
					});
					return;
				}
				this.searchResults = [];
				this.hasSearched = true;
				this.showSearchSuggestions = false;
				if (this.searchType === 'character') {
					this.searchByCharacter();
				} else {
					this.searchByBox();
				}
			},
			
			searchByCharacter() {
				const searchTerm = this.searchText.trim().toLowerCase();
				const characterMap = {};
				
				this.characterData.forEach(box => {
					// 应用盒类型筛选
					if (this.currentFilter !== 'all') {
						const boxType = box.Box_type || 'normal';
						if (this.currentFilter === 'normal') {
							if (boxType && boxType !== 'normal') return;
						} else {
							if (boxType !== this.currentFilter) return;
						}
					}
					
					// ============= 新增：高级筛选 =============
					// 发布时间筛选
					if (!this.passDateFilter(box)) return;
					
					// 复刻状态筛选
					if (!this.passReplicateFilter(box)) return;
					
					// 盲抽/单领筛选
					if (!this.passTypeFilter(box)) return;
					
					// 遍历盒中的干员
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
							} else if (box[charKey].name) {
								characterName = box[charKey].name;
								imageUrl = box[charKey].imageUrl || '';
								nolyELITE1 = box[charKey].nolyELITE1 === true;
								hotcharacter = box[charKey].hotcharacter === true;
								market_price = box[charKey].market_price || null;
							}
							
							if (characterName) {
								const matchResult = this.checkCharacterMatch(characterName, searchTerm);
								if (matchResult.isMatch) {
									if (!characterMap[characterName]) {
										characterMap[characterName] = {
											characterName: characterName,
											avatar: imageUrl || this.defaultAvatar,
											boxIds: [],
											matchType: matchResult.matchType,
											nolyELITE1: nolyELITE1,
											hotcharacter: hotcharacter,
											market_price: market_price
										};
									}
									if (!characterMap[characterName].boxIds.includes(box.Box_id)) {
										characterMap[characterName].boxIds.push(box.Box_id);
									}
									if (characterMap[characterName].avatar === this.defaultAvatar && imageUrl) {
										characterMap[characterName].avatar = imageUrl;
									}
									if (matchResult.matchType && (!characterMap[characterName].matchType || 
										characterMap[characterName].matchType === 'chinese')) {
										characterMap[characterName].matchType = matchResult.matchType;
									}
									if (nolyELITE1) characterMap[characterName].nolyELITE1 = true;
									if (hotcharacter) characterMap[characterName].hotcharacter = true;
									if (market_price) characterMap[characterName].market_price = market_price;
								}
							}
						}
					}
				});
				
				this.searchResults = Object.values(characterMap);
			},
			
			searchByBox() {
				const searchTerm = this.searchText.trim();
				const results = [];
				const cleanSearchTerm = searchTerm.replace(/\s+/g, '');
				
				this.characterData.forEach(box => {
					const boxId = String(box.Box_id || '');
					const boxType = box.Box_type || 'normal';
					
					// 应用盒类型筛选
					if (this.currentFilter !== 'all') {
						if (this.currentFilter === 'normal') {
							if (boxType && boxType !== 'normal') return;
						} else {
							if (boxType !== this.currentFilter) return;
						}
					}
					
					// ============= 新增：高级筛选 =============
					if (!this.passDateFilter(box)) return;
					if (!this.passReplicateFilter(box)) return;
					if (!this.passTypeFilter(box)) return;
					
					let isMatch = false;
					
					// 精确匹配
					if (boxId === searchTerm || boxId === cleanSearchTerm) {
						isMatch = true;
					} 
					// 常规盒号匹配
					else if (boxType === 'normal' || !boxType) {
						const boxNumberMatch = boxId.match(/^(\d+(?:\.\d+)?)$/);
						if (boxNumberMatch) {
							const boxNumber = boxNumberMatch[1];
							const searchPatterns = [
								boxNumber,
								boxNumber.replace('.0', ''),
								boxNumber.split('.')[0]
							];
							if (searchPatterns.includes(cleanSearchTerm)) {
								isMatch = true;
							}
						}
					} 
					// 特殊盒号匹配
					else {
						const specialBoxMatch = boxId.match(/^(.+?)(\d+(?:\.\d+)?)$/);
						if (specialBoxMatch) {
							const prefix = specialBoxMatch[1];
							const numberPart = specialBoxMatch[2];
							const matchPatterns = [
								prefix + numberPart,
								prefix + numberPart.replace('.0', ''),
								prefix + numberPart.split('.')[0]
							];
							if (matchPatterns.some(pattern => pattern === cleanSearchTerm)) {
								isMatch = true;
							}
							if (/^\d+(\.\d+)?$/.test(cleanSearchTerm)) {
								const searchNumber = cleanSearchTerm;
								const numberPatterns = [
									numberPart,
									numberPart.replace('.0', ''),
									numberPart.split('.')[0]
								];
								if (numberPatterns.includes(searchNumber) && this.currentFilter !== 'all') {
									isMatch = true;
								}
							}
						}
					}
					
					if (isMatch) {
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
						results.push({
							boxId: boxId,
							boxType: boxType,
							Box_ImageUrl: box.Box_ImageUrl || '',
							release_date: box.release_date || '',
							retail_price: box.retail_price || '',
							type: box.type || false,
							replicate: box.replicate || false,
							replicate_date: box.replicate_date || '',
						replicate_date_01: box.replicate_date_01 || '',
						replicate_date_02: box.replicate_date_02 || '',
						replicate_date_03: box.replicate_date_03 || '',
						replicate_date_04: box.replicate_date_04 || '',
						replicate_date_05: box.replicate_date_05 || '',
							characters: characters,
							isImageExpanded: false,
							showExpandBtn: false
						});
					}
				});
				this.searchResults = results;
			},
			
			// ============= 新增：高级筛选辅助方法 =============
			// 日期筛选
			passDateFilter(box) {
				const releaseDate = box.release_date || '';
				if (this.filterYear && !releaseDate.startsWith(this.filterYear)) return false;
				if (this.filterMonth) {
					const monthPattern = '/' + this.filterMonth + '/';
					if (!releaseDate.includes(monthPattern)) return false;
				}
				if (this.filterDay) {
					const dayPattern = '/' + this.filterDay;
					if (!releaseDate.endsWith(dayPattern)) return false;
				}
				return true;
			},
			
			// 复刻状态筛选
			passReplicateFilter(box) {
				if (this.filterReplicate === '') return true;
				let replicateValue = box.replicate;
				if (typeof replicateValue === 'string') {
					replicateValue = replicateValue === 'true';
				}
				return replicateValue === (this.filterReplicate === 'true');
			},
			
			// 盲抽/单领筛选
			passTypeFilter(box) {
				if (this.filterType === '') return true;
				let typeValue = box.type;
				if (typeof typeValue === 'string') {
					typeValue = typeValue === 'true';
				}
				return typeValue === (this.filterType === 'true');
			},
			
			// 检查字符匹配
			checkCharacterMatch(characterName, searchTerm) {
			    // 中文名匹配（基础匹配）
			    if (characterName.toLowerCase().includes(searchTerm)) {
			        return { isMatch: true, matchType: 'chinese' };
			    }
			    
			    // 如果启用了任何搜索功能，检查搜索词数据
			    if (this.enableEnglishSearch || this.enableJapaneseSearch || this.enableNicknameSearch) {
			        // 查找搜索词数据 - 使用更灵活的匹配
			        const searchWordData = this.searchWords.find(item => {
			            if (!item || !item.name) return false;
			            return item.name === characterName || 
			                   (item.englishname && item.englishname.toLowerCase() === characterName.toLowerCase()) ||
			                   (item.japanesename && item.japanesename === characterName);
			        });
			        
			        if (searchWordData) {
			            // 英文名匹配
			            if (this.enableEnglishSearch && searchWordData.englishname) {
			                const englishName = searchWordData.englishname.toLowerCase();
			                if (englishName.includes(searchTerm)) {
			                    return { isMatch: true, matchType: 'english' };
			                }
			            }
			            
			            // 日文名匹配
			            if (this.enableJapaneseSearch && searchWordData.japanesename) {
			                const japaneseName = searchWordData.japanesename.toLowerCase();
			                if (japaneseName.includes(searchTerm)) {
			                    return { isMatch: true, matchType: 'japanese' };
			                }
			            }
			            
			            // 外号匹配
			            if (this.enableNicknameSearch && searchWordData.searchword) {
			                let searchWords = searchWordData.searchword;
			                if (!Array.isArray(searchWords) && typeof searchWords === 'string') {
			                    // 如果是字符串，尝试分割
			                    searchWords = searchWords.split(/[,，、]/).map(w => w.trim());
			                }
			                
			                if (Array.isArray(searchWords)) {
			                    for (const nickname of searchWords) {
			                        if (nickname && nickname.toLowerCase().includes(searchTerm)) {
			                            return { isMatch: true, matchType: 'nickname' };
			                        }
			                    }
			                }
			            }
			        }
			        
			        // 内置外号匹配
			        if (this.enableNicknameSearch && this.builtinNicknames[characterName]) {
			            for (const nickname of this.builtinNicknames[characterName]) {
			                if (nickname.toLowerCase().includes(searchTerm)) {
			                    return { isMatch: true, matchType: 'nickname' };
			                }
			            }
			        }
			        
			        // 自定义搜索词匹配
			        if (this.enableNicknameSearch) {
			            const customWord = this.customSearchWords.find(item => 
			                item.characterName === characterName
			            );
			            if (customWord && customWord.words && Array.isArray(customWord.words)) {
			                for (const word of customWord.words) {
			                    if (word.toLowerCase().includes(searchTerm)) {
			                        return { isMatch: true, matchType: 'custom' };
			                    }
			                }
			            }
			        }
			    }
			    
			    return { isMatch: false };
			},
			
			getMatchTypeText(matchType) {
				const typeMap = {
					'chinese': '中文名',
					'english': '英文名',
					'japanese': '日文名',
					'nickname': '外号',
					'custom': '自定义搜索词'
				};
				return typeMap[matchType] || '名称';
			},
			
			// 格式化云端时间
			formatCloudTime(time) {
			    if (!time) return '';
			    
			    try {
			        let date;
			        
			        // 判断是否为时间戳（数字或数字字符串，且长度在10-13位之间）
			        if ((typeof time === 'number' || /^\d+$/.test(time)) && 
			            (time.toString().length === 10 || time.toString().length === 13)) {
			            
			            // 如果是10位时间戳（秒），转换为13位（毫秒）
			            const timestamp = parseInt(time);
			            date = timestamp.toString().length === 10 ? 
			                new Date(timestamp * 1000) : 
			                new Date(timestamp);
			        } else {
			            // 尝试作为ISO字符串解析
			            date = new Date(time);
			        }
			        
			        // 检查日期是否有效
			        if (isNaN(date.getTime())) {
			            console.warn('无效的时间格式:', time);
			            return '未知时间';
			        }
			        
			        return `${date.getFullYear()}-${(date.getMonth()+1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')} ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
			    } catch (e) {
			    	this.logError(e);
			        console.error('格式化云端时间失败:', e, '原始时间:', time);
			        return typeof time === 'string' ? time : '时间格式错误';
			    }
			},
			
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
			
			onAvatarError(index) {
				this.$set(this.searchResults[index], 'avatar', this.defaultAvatar);
			},
			
			onAvatarListError(boxIndex, charIndex) {
				this.$set(this.searchResults[boxIndex].characters[charIndex], 'avatar', this.defaultAvatar);
			},
			
			onModalAvatarError(index) {
				this.$set(this.modalBoxCharacters[index], 'avatar', this.defaultAvatar);
			},
			
			// 卡片相关方法
		formatDate(dateStr) {
			if (!dateStr) return '未知';
			if (/^\d{8}$/.test(dateStr)) {
				const year = dateStr.substring(0, 4);
				const month = dateStr.substring(4, 6);
				const day = dateStr.substring(6, 8);
				return `${year}年${month}月${day}日`;
			}
			if (/^\d{4}-\d{2}-\d{2}$/.test(dateStr)) {
				const parts = dateStr.split('-');
				return `${parts[0]}年${parts[1]}月${parts[2]}日`;
			}
			return dateStr;
		},
		
		previewBoxImage(imageUrl) {
			if (!imageUrl) return;
			uni.previewImage({
				urls: [imageUrl],
				current: imageUrl
			});
		},
		
		onBoxImageLoad(result, event) {
			const { height } = event.detail;
			if (height > 150) {
				this.$set(result, 'showExpandBtn', true);
				this.$set(result, 'isImageExpanded', false);
			}
		},
		
		toggleImageExpand(result) {
			this.$set(result, 'isImageExpanded', !result.isImageExpanded);
		},
		
		onBoxImageError(boxId) {
			console.error(`盒号 ${boxId} 图片加载失败`);
		},
		
		// 扫码处理
		startScan() {
			// #ifdef MP-WEIXIN
			wx.requirePrivacyAuthorize({
				success: () => { this.doStartScan(); },
				fail: () => { this.onPrivacyCancel(); }
			});
			// #endif
			// #ifndef MP-WEIXIN
			this.doStartScan();
			// #endif
		},

		// 实际扫码
		doStartScan() {
			uni.scanCode({
				scanType: ['barCode'],
				success: (res) => {
					if (res && res.result) this.handleScannedBarcode(res.result);
				},
				fail: (err) => {
					if (err && err.errMsg && err.errMsg.indexOf('cancel') === -1) {
						uni.showToast({ title: '扫描失败', icon: 'none' });
					}
				}
			});
		},

		// 同意隐私协议（官方按钮回调）
		onPrivacyConfirm() {
			this.showPrivacyModal = false;
			if (this.pendingScanAction) {
				this.pendingScanAction = false;
				this.doStartScan();
			}
		},

		onPrivacyCancel() {
			this.showPrivacyModal = false;
			this.pendingScanAction = false;
			uni.showToast({ title: '需要同意隐私协议才能使用扫描功能', icon: 'none' });
		},

		// 构建 条码->盒号 映射（来自 characterData 的 barcode/barcodes 字段）
		buildBarcodeMap() {
			const map = {};
			const data = this.characterData || [];
			data.forEach(box => {
				const bid = String(box.Box_id);
				if (box.barcode) map[String(box.barcode)] = bid;
				if (Array.isArray(box.barcodes)) {
					box.barcodes.forEach(b => { map[String(b)] = bid; });
				}
			});
			this.barcodeMap = map;
			return map;
		},

		// 处理扫描到的条码
		handleScannedBarcode(barcode) {
			if (!barcode) return;
			const map = this.buildBarcodeMap();
			const boxId = map[String(barcode)];
			if (boxId) {
				this.searchType = 'box';
				this.searchText = boxId;
				this.handleSearch();
				uni.showToast({ title: '已查询盒号 ' + boxId, icon: 'none' });
			} else {
				this.showQuestionnaire(barcode);
			}
		},

		// 点击盒内干员 -> 展示该干员出现在的其他盒号
		onCharacterInBoxClick(charName, boxId) {
			if (!charName) return;
			const target = String(charName).trim().toLowerCase();
			const list = [];
			(this.characterData || []).forEach(box => {
				if (String(box.Box_id) === String(boxId)) return;
				for (let i = 1; i <= 10; i++) {
					const c = box['character' + i];
					let nm = '';
					if (typeof c === 'string') nm = c;
					else if (c && c.name) nm = c.name;
					if (nm && nm.trim().toLowerCase() === target) { list.push(String(box.Box_id)); break; }
				}
			});
			this.charBoxesName = charName;
			this.charBoxesList = list;
			this.showCharBoxesModal = true;
		},

		openOtherBox(boxId) {
			this.showCharBoxesModal = false;
			this.showBoxDetail(boxId);
		},

		closeCharBoxesModal() {
			this.showCharBoxesModal = false;
		},

		showQuestionnaire(barcode) {
			const url = 'https://wj.qq.com/s2/27523743/l0g2/';
			uni.showModal({
				title: '条码未收录',
				content: '条码 ' + barcode + ' 暂未录入数据库。\n欢迎填写问卷补充：\n' + url,
				confirmText: '复制链接',
				cancelText: '关闭',
				success: (res) => {
					if (res.confirm) {
						uni.setClipboardData({ data: url, success: () => { uni.showToast({ title: '链接已复制', icon: 'none' }); } });
					}
				}
			});
		},

		goToBoxFeedback(boxId) {
			uni.navigateTo({
				url: `/pages/box_feedback_new/box_feedback_new?boxId=${boxId}`
			});
		},
		
		showBoxDetail(boxId) {
			const boxData = this.characterData.find(box => box.Box_id === boxId);
			if (boxData) {
				const result = {
					boxId: boxData.Box_id,
					boxType: boxData.Box_type,
					Box_ImageUrl: boxData.Box_ImageUrl,
					release_date: boxData.release_date,
					retail_price: boxData.retail_price,
					type: boxData.type,
					replicate: boxData.replicate,
					replicate_date: boxData.replicate_date,
					replicate_date_01: boxData.replicate_date_01 || '',
					replicate_date_02: boxData.replicate_date_02 || '',
					replicate_date_03: boxData.replicate_date_03 || '',
					replicate_date_04: boxData.replicate_date_04 || '',
					replicate_date_05: boxData.replicate_date_05 || '',
					characters: this.getBoxCharacters(boxData),
					isImageExpanded: false,
					showExpandBtn: false
				};
				this.selectedBox = result;
				this.showBoxDetailModal = true;
			} else {
				uni.showToast({
					title: '未找到盒号数据',
					icon: 'none'
				});
			}
		},
		
		closeBoxDetail() {
			this.showBoxDetailModal = false;
			this.selectedBox = null;
		},
		
		// 获取复刻记录列表
		getReplicateRecords(box) {
			if (!box) return [];
			const records = [];
			if (box.replicate_date) {
				records.push(box.replicate_date + ' 第一次复刻');
			}
			const extras = ['replicate_date_01', 'replicate_date_02', 'replicate_date_03', 'replicate_date_04', 'replicate_date_05'];
			for (let i = 0; i < extras.length; i++) {
				const key = extras[i];
				if (box[key]) {
					records.push(box[key] + ' 第' + this.toChineseOrdinal(i + 2) + '次复刻');
				}
			}
			return records;
		},

		// 数字转中文序数
		toChineseOrdinal(n) {
			const map = ['零', '一', '二', '三', '四', '五', '六', '七', '八', '九', '十'];
			return map[n] || String(n);
		},

		getBoxCharacters(box) {
			const characters = [];
			for (let i = 1; i <= 10; i++) {
				const charKey = `character${i}`;
				if (box[charKey]) {
					let characterName = '';
					let avatar = '';
					let nolyELITE1 = false;
					let hotcharacter = false;
					
					if (typeof box[charKey] === 'string') {
						characterName = box[charKey];
						avatar = '';
					} else if (typeof box[charKey] === 'object') {
						characterName = box[charKey].name || '';
						avatar = box[charKey].imageUrl || box[charKey].avatar || '';
						nolyELITE1 = box[charKey].nolyELITE1 || false;
						hotcharacter = box[charKey].hotcharacter || false;
					}
					
					if (characterName && characterName.trim()) {
						characters.push({
							name: characterName.trim(),
							avatar: avatar,
							nolyELITE1: nolyELITE1,
							hotcharacter: hotcharacter
						});
					}
				}
			}
			return characters;
		},
		
		// 收藏相关方法
			loadFavorites() {
				try {
					this.favoriteBoxIds = uni.getStorageSync('favoriteBoxIds') || [];
					this.favoriteCharacterNames = uni.getStorageSync('favoriteCharacterNames') || [];
				} catch (error) {
					this.logError(error);
					this.favoriteBoxIds = [];
					this.favoriteCharacterNames = [];
				}
			},
			
			toggleFavorite(boxId) {
				try {
					const index = this.favoriteBoxIds.indexOf(boxId);
					if (index > -1) {
						this.favoriteBoxIds.splice(index, 1);
						uni.showToast({
							title: '已取消收藏',
							icon: 'success'
						});
					} else {
						this.favoriteBoxIds.push(boxId);
						uni.showToast({
							title: '收藏成功',
							icon: 'success'
						});
					}
					uni.setStorageSync('favoriteBoxIds', this.favoriteBoxIds);
					this.$forceUpdate();
				} catch (error) {
					this.logError(error);
					uni.showToast({
						title: '操作失败',
						icon: 'none'
					});
				}
			},
			
			isFavorite(boxId) {
				return this.favoriteBoxIds.includes(boxId);
			},
			
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
			
			toggleFavoriteCharacterDirectly(characterName) {
				this.toggleFavoriteCharacter(characterName);
			},
			
			isFavoriteCharacter(characterName) {
				return this.favoriteCharacterNames.includes(characterName);
			},
			
			selectAllCharacters() {
				if (this.isAllSelected) {
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
			
			// 模态框相关方法
			goToBoxInfo(boxId) {
				uni.navigateTo({
					url: `/pages/box_info/box_info?boxId=${boxId}`
				});
			},
			
			handleViewBoxData(characterName, boxIds) {
				if (boxIds.length === 1) {
					this.goToBoxInfo(boxIds[0]);
					return;
				}
				this.showBoxSelectionModal = true;
				this.modalTitle = `选择 ${characterName} 所在的盒号`;
				this.modalBoxList = this.getBoxDetails(boxIds);
			},
			
			openBoxCharactersModal(boxId, characters) {
				this.showBoxCharactersModal = true;
				this.currentBoxId = boxId;
				this.modalBoxCharacters = characters;
			},
			
			hideBoxCharactersModal() {
				this.showBoxCharactersModal = false;
				this.currentBoxId = '';
				this.modalBoxCharacters = [];
			},
			
			getBoxDetails(boxIds) {
				return boxIds.map(boxId => {
					const box = this.characterData.find(item => item.Box_id === boxId);
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
			
			// 数据加载方法
			loadSearchSettings() {
				try {
					const enableEnglishSearch = uni.getStorageSync('enableEnglishSearch');
					const enableJapaneseSearch = uni.getStorageSync('enableJapaneseSearch');
					const enableNicknameSearch = uni.getStorageSync('enableNicknameSearch');
					const customSearchWords = uni.getStorageSync('customSearchWords');
					
					// 确保这些值是布尔值
					this.enableEnglishSearch = enableEnglishSearch === 'true' || enableEnglishSearch === true;
					this.enableJapaneseSearch = enableJapaneseSearch === 'true' || enableJapaneseSearch === true;
					this.enableNicknameSearch = enableNicknameSearch === 'true' || enableNicknameSearch === true;
					
					this.customSearchWords = customSearchWords || [];
					
					console.log('加载搜索设置:', {
						english: this.enableEnglishSearch,
						japanese: this.enableJapaneseSearch,
						nickname: this.enableNicknameSearch,
						customWordsCount: this.customSearchWords.length
					});
					
				} catch (e) {
					this.logError(e);
					console.error('加载搜索设置失败:', e);
					// 设置默认值
					this.enableEnglishSearch = false;
					this.enableJapaneseSearch = false;
					this.enableNicknameSearch = false;
					this.customSearchWords = [];
				}
			},
			
			async loadLocalData() {
				try {
					const data = uni.getStorageSync('arknightsData');
					const localUpdateTime = uni.getStorageSync('localUpdateTime');
					const cloudUpdateTime = uni.getStorageSync('cloudUpdateTime');
					const version = uni.getStorageSync('dataVersion');
					const url = uni.getStorageSync('dataUrl');
					const enableGuessData = uni.getStorageSync('enableGuessData');
					const guessData = uni.getStorageSync('guessData');
					
					let allData = [];
					if (data && Array.isArray(data) && data.length > 0) {
						allData = [...data];
						this.dataStatusMessage = `已加载本地数据 (${data.length} 个盒号)`;
					}
					
					if (enableGuessData === 'true' && guessData && Array.isArray(guessData) && guessData.length > 0) {
						allData = [...allData, ...guessData];
						this.dataStatusMessage += ` + 预测数据 (${guessData.length} 个盒号)`;
					}
					
					if (allData.length > 0) {
						this.characterData = allData;
						this.localUpdateTime = localUpdateTime || '';
						this.cloudUpdateTime = cloudUpdateTime || '';
						this.currentVersion = version || '';
						this.dataUrl = url || '';
						
						// 更新日期选项
						this.updateDateOptions();
						
						if (this.shouldAutoSearch && this.characterData.length > 0) {
							setTimeout(() => {
								this.handleSearch();
								this.shouldAutoSearch = false;
							}, 500);
						}
						
						if (!this.currentVersion) {
							this.checkForUpdates();
						} else {
							setTimeout(() => {
								this.checkForUpdates();
							}, 1000);
						}
					} else {
						this.dataStatusMessage = '正在获取数据信息...';
						setTimeout(() => {
							this.checkForUpdates();
						}, 2000);
					}
				} catch (e) {
					this.logError(e);
					console.error('加载本地数据失败:', e);
					this.dataStatusMessage = '加载本地数据失败，正在尝试获取最新数据...';
					setTimeout(() => {
						this.checkForUpdates();
					}, 2000);
				}
			},
			
			async checkForUpdates() {
				try {
					this.dataStatusMessage = '正在检查更新...';
					
					const versionInfo = await this.getDataUrlFromMinapp();
					console.log('获取到的版本信息:', versionInfo);
					
					this.dataUrl = versionInfo.url;
					this.cloudUpdateTime = versionInfo.cloudUpdateTime;
					this.latestVersion = versionInfo.version;

					uni.setStorageSync('cloudUpdateTime', this.cloudUpdateTime);

					const localVersion = uni.getStorageSync('dataVersion');
					console.log('本地版本:', localVersion, '云端版本:', this.latestVersion);
					
					if (!localVersion || localVersion !== this.latestVersion) {
						this.dataStatusMessage = `发现新版本数据 (${this.latestVersion})，请点击按钮手动更新,更新会消耗一定流量，建议WI-FI下更新`;
					} else {
						this.currentVersion = localVersion;
						this.dataStatusMessage = `数据已是最新版本 (${this.currentVersion})`;
						if (this.characterData.length === 0) {
							this.downloadData();
						}
					}
				} catch (error) {
					this.logError(error);
					console.error('检查更新失败:', error);
					this.dataStatusMessage = '检查更新失败，使用备用URL';
					
					this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
					
					if (!this.currentVersion) {
						this.currentVersion = '未知版本';
					}
					
					if (this.characterData.length === 0) {
						this.downloadData();
					}
				}
			},
			
			// 下载数据 - 多源降级策略
			// 优先级：知晓云 → GitCode Raw → jsDelivr CDN → GitHub Raw → 固定死链接
			async downloadData() {
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

				// 2. 构建多源列表
				const sourceList = this.buildDataSourceList(this.dataUrl);
				this.dataStatusMessage = '正在下载干员数据...';

				// 3. 依次尝试各数据源
				for (let i = 0; i < sourceList.length; i++) {
					const source = sourceList[i];
					console.log(`[下载数据] 尝试源 ${i + 1}/${sourceList.length}: ${source.name}`);
					try {
						const data = await this.tryDownloadFromSource(source.url);
						if (data) {
							await this.handleSearchDownloadSuccess(data, source.name);
							return;
						}
					} catch (err) {
						this.logError(err);
						console.warn(`[下载数据] 源 ${source.name} 失败:`, err.message || err);
					}
				}

				// 4. 所有源都失败
				this.dataStatusMessage = '下载失败，所有数据源均不可用';
				if (this.characterData.length === 0) {
					setTimeout(() => {
						uni.showModal({
							title: '网络错误',
							content: '无法下载干员数据，请检查网络连接后重试',
							showCancel: false
						});
					}, 500);
				}
			},

			
			// 构建数据源列表
			buildDataSourceList(primaryUrl) {
				const sources = [];
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
								reject(new Error(HTTP ));
							}
						},
						fail: (err) => reject(err)
					});
				});
			},
			
			// 处理下载成功
			async handleSearchDownloadSuccess(rawData, sourceName) {
				const cleanedData = rawData.map(box => {
					const cleanedBox = {};
					for (const key in box) {
						if (key === 'Box_id') { cleanedBox[key] = String(box[key] || ''); }
						else if (key.startsWith('character')) { cleanedBox[key] = box[key]; }
						else { cleanedBox[key] = String(box[key] || ''); }
					}
					return cleanedBox;
				});
				
				this.characterData = cleanedData;
				this.currentVersion = this.latestVersion || this.currentVersion || '未知版本';
				this.updateDateOptions();
				
				uni.setStorageSync('arknightsData', cleanedData);
				const now = new Date();
					this.localUpdateTime = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`;
				uni.setStorageSync('localUpdateTime', this.localUpdateTime);
				uni.setStorageSync('cloudUpdateTime', this.cloudUpdateTime);
				uni.setStorageSync('dataVersion', this.currentVersion);
				uni.setStorageSync('dataUrl', this.dataUrl);
				
					this.dataStatusMessage = `数据更新成功  (${cleanedData.length} 个盒号)`;
					uni.showToast({ title: `更新成功 (${sourceName || '数据'})`, icon: 'success', duration: 2000 });
				
				if (this.shouldAutoSearch && this.characterData.length > 0) {
						setTimeout(() => { this.handleSearch(); this.shouldAutoSearch = false; }, 500);
				}
			},
			// ============= 新增：高级筛选相关方法 =============
			
			// 打开高级筛选模态框
			openAdvancedFilter() {
				this.showAdvancedFilter = true;
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
				this.closeAdvancedFilter();
				// 如果已搜索过，重新搜索以应用新筛选
				if (this.hasSearched) {
					this.handleSearch();
				}
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
	
	.search-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
		position: relative;
		overflow: hidden;
		transition: all 0.3s ease;
	}
	
	.search-section.collapsed {
		padding: 0;
		margin-bottom: 0;
		height: 0;
	}
	
	.search-row {
		display: flex;
		align-items: center;
		margin-bottom: 30rpx;
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
	}
	
	.filter-picker {
		background-color: #f8f8f8;
		border-radius: 50rpx;
		padding: 20rpx 30rpx;
		min-width: 200rpx;
	}
	
	.picker-text {
		font-size: 28rpx;
		color: #333;
		text-align: center;
	}
	
	/* 新增：高级筛选按钮样式 */
	.advanced-filter-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		padding: 20rpx 30rpx;
		font-size: 26rpx;
		white-space: nowrap;
	}
	
	.suggestions-dropdown {
		position: relative;
		background-color: #fff;
		border-radius: 0 0 16rpx 16rpx;
		box-shadow: 0 8rpx 30rpx rgba(0, 0, 0, 0.15);
		z-index: 1000;
		overflow: hidden;
		border: 1rpx solid #e0e0e0;
		border-top: none;
		max-height: 400rpx;
		margin-top: -1rpx;
	}
	
	.suggestions-list {
		max-height: 400rpx;
	}
	
	.suggestions-mask {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: transparent;
		z-index: 999;
	}
	
	.suggestion-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 24rpx 30rpx;
		border-bottom: 1rpx solid #f0f0f0;
		transition: background-color 0.2s ease;
	}
	
	.suggestion-item:active {
		background-color: #f5f5f5;
	}
	
	.suggestion-content {
		display: flex;
		align-items: center;
		flex: 1;
	}
	
	.suggestion-name {
		font-size: 28rpx;
		color: #333;
		margin-right: 20rpx;
		font-weight: 500;
	}
	
	.suggestion-tags {
		display: flex;
		gap: 10rpx;
	}
	
	.suggestion-tag {
		font-size: 22rpx;
		padding: 6rpx 12rpx;
		border-radius: 6rpx;
		color: #fff;
		font-weight: 500;
	}
	
	.suggestion-tag.english {
		background-color: #409EFF;
	}
	
	.suggestion-tag.japanese {
		background-color: #FF6B6B;
	}
	
	.suggestion-tag.nickname {
		background-color: #67C23A;
	}
	
	.suggestion-tag.type-normal {
		background-color: #67C23A;
	}
	
	.suggestion-tag.type-whitelist {
		background-color: #E6A23C;
	}
	
	.suggestion-tag.type-special {
		background-color: #F56C6C;
	}
	
	.suggestion-tag.type-cooperation {
		background-color: #909399;
	}
	
	.suggestion-tag.type-ambience {
		background-color: #409EFF;
	}
	
	.suggestion-type {
		font-size: 24rpx;
		color: #999;
	}
	
	.load-more {
		display: flex;
		justify-content: center;
		align-items: center;
		padding: 24rpx 30rpx;
		background-color: #f8f8f8;
	}
	
	.load-more:active {
		background-color: #f0f0f0;
	}
	
	.load-more-text {
		font-size: 26rpx;
		color: #409EFF;
		font-weight: 500;
	}
	
	.search-options {
		display: flex;
		justify-content: space-between;
		margin-bottom: 30rpx;
		gap: 15rpx;
	}
	
	.option-btn {
		flex: 1;
		background-color: #f0f0f0;
		color: #666;
		font-size: 26rpx;
		border-radius: 10rpx;
		padding: 20rpx 0;
		font-weight: 500;
		transition: all 0.3s ease;
	}
	
	.option-btn.active {
		background-color: #409EFF;
		color: #fff;
	}
	
	.option-btn:active {
		transform: scale(0.98);
	}
	
	.search-tips {
		margin-bottom: 25rpx;
		padding: 15rpx 0;
	}
	
	.search-tip-text {
		font-size: 26rpx;
		color: #666;
	}
	
	.search-tag {
		display: inline-block;
		background-color: #e8f4ff;
		color: #409EFF;
		padding: 6rpx 12rpx;
		border-radius: 6rpx;
		margin: 0 8rpx;
		font-size: 24rpx;
		font-weight: 500;
	}
	
	.search-btn {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		font-size: 30rpx;
		padding: 24rpx 0;
		font-weight: 500;
		transition: all 0.3s ease;
	}
	
	.search-btn:active {
		background-color: #337ecc;
		transform: scale(0.98);
	}
	
	/* 数据状态区域 - 整合所有信息 */
	.data-status {
		background-color: #e8f4ff;
		border-radius: 16rpx;
		padding: 25rpx;
		margin-bottom: 30rpx;
		font-size: 24rpx;
		color: #409EFF;
	}
	
	.data-status-main {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 15rpx;
	}
	
	.status-text {
		font-size: 26rpx;
		font-weight: bold;
	}
	
	.data-status-actions {
		display: flex;
		align-items: center;
		gap: 15rpx;
	}
	
	.update-btn.small {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		font-size: 22rpx;
		padding: 8rpx 20rpx;
		line-height: 1.4;
	}
	
	.data-details {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
		border-top: 1rpx solid rgba(64, 158, 255, 0.2);
		padding-top: 15rpx;
	}
	
	.data-detail-item {
		font-size: 22rpx;
		color: #666;
	}
	
	.update-section {
		text-align: center;
		margin-top: 40rpx;
	}
	
	.update-btn {
		background-color: #67C23A;
		color: #fff;
		border-radius: 50rpx;
		font-size: 26rpx;
		padding: 20rpx 40rpx;
		margin-bottom: 20rpx;
	}
	
	.result-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	.result-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 20rpx;
		padding-bottom: 20rpx;
		border-bottom: 1rpx solid #f0f0f0;
	}
	
	.result-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #333;
	}
	
	.result-count {
		font-size: 24rpx;
		color: #999;
	}
	
	.advanced-filter-tip {
		font-size: 22rpx;
		color: #E6A23C;
		margin-left: 20rpx;
	}
	
	.result-list {
		/* max-height: 600rpx; 允许结果完全展示 */
	}
	
	.result-item {
		padding: 20rpx 0;
		border-bottom: 1rpx solid #f5f5f5;
	}
	
	.result-item:last-child {
		border-bottom: none;
	}
	
	/* 干员查询结果头部布局 */
	.character-result-header {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		gap: 20rpx;
	}
	
	.character-with-avatar {
		display: flex;
		align-items: flex-start;
		flex: 1;
	}
	
	.character-actions {
		display: flex;
		flex-direction: column;
		gap: 10rpx;
		align-items: flex-end;
	}
	
	.character-avatar-container {
		position: relative;
		margin-right: 20rpx;
		flex-shrink: 0;
	}
	
	.character-avatar {
		width: 80rpx;
		height: 80rpx;
		border-radius: 10rpx;
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
		margin-bottom: 5rpx;
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
	
	.elite-tag {
		background-color: #FF6B6B;
		color: #fff;
		font-size: 20rpx;
		padding: 4rpx 8rpx;
		border-radius: 4rpx;
		font-weight: bold;
		margin-left: 10rpx;
	}
	
	.match-type {
		font-size: 22rpx;
		color: #999;
		margin-left: 10rpx;
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
		background-color: #67C23A;
		color: #fff;
		font-size: 24rpx;
		padding: 12rpx 20rpx;
		border-radius: 8rpx;
		white-space: nowrap;
		flex-shrink: 0;
		height: fit-content;
	}
	
	.view-info-btn:active {
		background-color: #5daf34;
	}
	
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
	
	/* 干员网格布局 */
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
		cursor: pointer;
	}
	
	.character-card:active {
		background-color: #e8f4ff;
		transform: scale(0.98);
	}
	
	.character-name {
		font-size: 32rpx;
		color: #333;
		margin-right: 10rpx;
		font-weight: bold;
	}
	
	/* 市场价标签样式 */
	.market-price-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 8rpx;
		margin-top: 8rpx;
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
	
	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 100rpx 0;
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
		text-align: center;
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
	
	/* 原各种模态框样式保持不变 */
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
	
	/* 移动端响应式调整 */
	@media (max-width: 750rpx) {
		.container {
			padding: 25rpx;
		}
		
		.search-section {
			padding: 25rpx;
			margin-bottom: 25rpx;
		}
		
		.search-row {
			margin-bottom: 25rpx;
			gap: 15rpx;
			flex-wrap: wrap;
		}
		
		.search-input-container {
			padding: 18rpx 25rpx;
			flex: 1 1 calc(100% - 120rpx);
		}
		
		.filter-picker {
			padding: 18rpx 25rpx;
			min-width: 180rpx;
		}
		
		.advanced-filter-btn {
			padding: 18rpx 25rpx;
			font-size: 24rpx;
		}
		
		.suggestion-item {
			padding: 20rpx 25rpx;
		}
		
		.image-modal {
			padding: 20rpx;
		}
		
		.image-modal-content {
			max-width: 100%;
		}
		
		.character-result-header {
			flex-direction: column;
			align-items: flex-start;
		}
		
		.character-with-avatar {
			width: 100%;
			margin-bottom: 15rpx;
		}
		
		.character-actions {
			flex-direction: row;
			width: 100%;
			justify-content: flex-end;
		}
		
		.view-info-btn {
			align-self: flex-end;
			width: 100%;
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
			flex-direction: column;
			width: 100%;
			gap: 8rpx;
		}
		
		.favorite-btn, .view-info-btn {
			width: 100%;
			font-size: 22rpx;
			padding: 10rpx;
		}
		
		.box-selection-header {
			flex-direction: column;
			align-items: flex-start;
		}
		
		.box-selection-left {
			width: 100%;
			margin-bottom: 10rpx;
		}
		
		.favorite-btn-small {
			align-self: flex-end;
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
	.container.theme-ark .container { background: transparent; }
	.container.theme-ark .search-section,
	.container.theme-ark .result-card,
	.container.theme-ark .box-selection-modal,
	.container.theme-ark .modal-content,
	.container.theme-ark .advanced-filter-modal .modal-content {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border: 1rpx solid #2A2A4A;
	}
	.container.theme-ark .search-input-container { background-color: #16162A; border-color: #2A2A4A; }
	.container.theme-ark .search-input-container.focus { border-color: #FF6B35; }
	.container.theme-ark .search-input { color: #e0e0e0; }
	.container.theme-ark .search-icon { filter: brightness(0) invert(1); opacity: 0.7; }
	.container.theme-ark .filter-picker,
	.container.theme-ark .picker-text { background-color: #16162A; color: #e0e0e0; }
	.container.theme-ark .advanced-filter-btn,
	.container.theme-ark .search-btn,
	.container.theme-ark .update-btn,
	.container.theme-ark .load-more-text,
	.container.theme-ark .select-all-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .suggestion-item { border-bottom-color: #2A2A4A; }
	.container.theme-ark .suggestion-name { color: #e0e0e0; }
	.container.theme-ark .suggestion-type { color: #888; }
	.container.theme-ark .box-id { color: #FF6B35; }
	.container.theme-ark .character-tag { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; }
	.container.theme-ark .favorite-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .favorite-btn.favorited { background: #555; color: #FF6B35; }
	.container.theme-ark .view-info-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .data-status { background: rgba(255,107,53,0.1); border-left-color: #FF6B35; color: #FF6B35; }
	.container.theme-ark .pagination { background: rgba(22,22,42,0.8); }
	.container.theme-ark .page-btn { background: #16162A; color: #e0e0e0; border-color: #2A2A4A; }
	.container.theme-ark .page-btn:active { background: #FF6B35; color: #fff; }
	.container.theme-ark .close-btn { background: rgba(255,107,53,0.1); color: #FF6B35; }
	.container.theme-ark .close-btn:active { background: #FF6B35; color: #fff; }
	.container.theme-ark .modal-header { border-bottom-color: #2A2A4A; }
	.container.theme-ark .modal-title { color: #FF6B35; }
	.container.theme-ark .box-selection-header { border-bottom-color: #2A2A4A; }
	.container.theme-ark .box-selection-title { color: #FF6B35; }
	.container.theme-ark .box-card { background: rgba(22,22,42,0.6); border-color: #2A2A4A; }
	.container.theme-ark .box-card:active { border-color: #FF6B35; }
	.container.theme-ark .box-name { color: #e0e0e0; }
	.container.theme-ark .box-date { color: #888; }
	.container.theme-ark .market-price-hint { background: rgba(255,107,53,0.08); border-color: rgba(255,107,53,0.2); }
	.container.theme-ark .hint-text { color: #FF6B35; }
	.container.theme-ark .advanced-filter-section { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border-color: #2A2A4A; }
	.container.theme-ark .advanced-filter-title { color: #FF6B35; }
	.container.theme-ark .section-title { color: #e0e0e0; }
	.container.theme-ark .date-pickers { background-color: #16162A; }
	.container.theme-ark .section-subtitle { color: #888; }
	.container.theme-ark .character-result-card { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border: 1rpx solid #2A2A4A; }
	.container.theme-ark .character-name { color: #e0e0e0; }
	.container.theme-ark .box-characters-list { background: transparent; }
	.container.theme-ark .box-character-item { background: rgba(22,22,42,0.4); border-color: #2A2A4A; }
	.container.theme-ark .character-details { color: #e0e0e0; }
	.container.theme-ark .go-to-settings-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .modal-body { color: #e0e0e0; }
	.container.theme-ark .favorite-box-btn { background: rgba(255,107,53,0.1); color: #FF6B35; border-color: rgba(255,107,53,0.3); }
	.container.theme-ark .favorite-box-btn.favorited { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; }
	.container.theme-ark .filter-options { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border-color: #2A2A4A; }
	.container.theme-ark .option-btn { background: #16162A; color: #e0e0e0; border-color: #2A2A4A; }
	.container.theme-ark .option-btn.active { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border-color: transparent; }
	.container.theme-ark .option-btn:active { opacity: 0.8; }
	.container.theme-ark .search-type-tabs { background: transparent; }
	.container.theme-ark .tab-item { color: #888; }
	.container.theme-ark .tab-item.active { color: #FF6B35; border-bottom-color: #FF6B35; }
	.container.theme-ark .clear-search { color: #888; }

	/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .search-section,
	.container.theme-jieyuan .result-card,
	.container.theme-jieyuan .box-selection-modal,
	.container.theme-jieyuan .modal-content,
	.container.theme-jieyuan .advanced-filter-modal .modal-content {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .search-input-container {
		background: rgba(0,0,0,0.05);
		border-color: rgba(226, 88, 132, 0.2);
	}
	.container.theme-jieyuan .search-input-container.focus {
		border-color: #e25884;
		box-shadow: 0 0 10rpx rgba(226, 88, 132, 0.2);
	}
	.container.theme-jieyuan .search-input { color: #333; }
	.container.theme-jieyuan .filter-picker { background: rgba(0,0,0,0.05); }
	.container.theme-jieyuan .picker-text { color: #333; }
	.container.theme-jieyuan .advanced-filter-btn,
	.container.theme-jieyuan .search-btn,
	.container.theme-jieyuan .update-btn,
	.container.theme-jieyuan .load-more-text,
	.container.theme-jieyuan .select-all-btn {
		background: linear-gradient(90deg, #e25884, #399383);
		color: #fff;
		border: none;
	}
	.container.theme-jieyuan .suggestion-item { border-bottom-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .suggestion-name { color: #333; }
	.container.theme-jieyuan .suggestion-type { color: #888; }
	.container.theme-jieyuan .box-id { color: #399383; }
	.container.theme-jieyuan .character-tag { background: linear-gradient(90deg, #e25884, #399383); color: #fff; }
	.container.theme-jieyuan .favorite-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
	.container.theme-jieyuan .favorite-btn.favorited { background: rgba(226, 88, 132, 0.15); color: #e25884; }
	.container.theme-jieyuan .data-status { background: rgba(255, 255, 255, 0.9); border-left-color: #399383; color: #399383; }
	.container.theme-jieyuan .status-text { color: #333; }
	.container.theme-jieyuan .pagination { background: rgba(255, 255, 255, 0.9); }
	.container.theme-jieyuan .page-btn { background: rgba(0,0,0,0.05); color: #333; border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .page-btn:active { background: linear-gradient(90deg, #e25884, #399383); color: #fff; }
	.container.theme-jieyuan .close-btn { background: rgba(226, 88, 132, 0.1); color: #e25884; }
	.container.theme-jieyuan .close-btn:active { background: #e25884; color: #fff; }
	.container.theme-jieyuan .modal-header { border-bottom-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .modal-title { color: #e25884; }
	.container.theme-jieyuan .box-selection-header { border-bottom-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .box-selection-title { color: #399383; }
	.container.theme-jieyuan .box-card { background: rgba(0,0,0,0.03); border-color: rgba(226, 88, 132, 0.1); }
	.container.theme-jieyuan .box-card:active { border-color: #e25884; }
	.container.theme-jieyuan .box-name { color: #333; }
	.container.theme-jieyuan .box-date { color: #888; }
	.container.theme-jieyuan .market-price-hint { background: rgba(226, 88, 132, 0.08); border-color: rgba(226, 88, 132, 0.2); }
	.container.theme-jieyuan .hint-text { color: #e25884; }
	.container.theme-jieyuan .advanced-filter-section { background: rgba(255, 255, 255, 0.95); border-color: rgba(226, 88, 132, 0.15); }
	.container.theme-jieyuan .advanced-filter-title { background: linear-gradient(90deg, #e25884, #399383); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
	.container.theme-jieyuan .section-title { color: #333; }
	.container.theme-jieyuan .section-subtitle { color: #888; }

.toolbar-toggle {
	text-align: right;
	padding: 6rpx 20rpx;
	font-size: 24rpx;
	color: #888;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 卡片样式（同步 list.vue）========== */

/* 结果项卡片 */
.result-item.box-card {
	background-color: #fff;
	border-radius: 20rpx;
	padding: 30rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.08);
	overflow: hidden;
	margin-bottom: 20rpx;
}

/* 卡片头部 */
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

/* 官方图片区域 */
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
	max-height: none;
}

.box-official-image {
	width: 100%;
	display: block;
}

/* 图片预览提示 */
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

.box-image-wrapper:active .image-preview-hint,
.box-image-wrapper:hover .image-preview-hint {
	opacity: 1;
}

.preview-hint-text {
	font-size: 22rpx;
	color: #fff;
}

/* 展开/收起按钮 */
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

.box-image-wrapper.expanded + .image-expand-btn {
	position: relative;
	background: #f8fafc;
	border-top: 1rpx solid #e0e0e0;
}

.expand-icon {
	font-size: 24rpx;
	color: #409EFF;
	margin-right: 8rpx;
}

.expand-text {
	font-size: 26rpx;
	color: #409EFF;
	font-weight: 500;
}

/* 基本信息网格 */
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

/* 复刻信息 */
.replicate-section {
	flex-direction: column;
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

/* 紧凑型干员网格 */
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

.character-item-compact:active {
	background-color: #e8f4ff;
	transform: scale(0.98);
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

/* 收藏指示器 */
.favorite-indicator {
	position: absolute;
	bottom: -4rpx;
	right: -4rpx;
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
}

.favorite-indicator.favorited {
	background-color: #E6A23C;
	color: #fff;
}

/* 操作按钮 */
.box-card-actions,
.character-card-actions {
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

.action-btn.favorite-btn {
	background-color: #f0f0f0;
	color: #666;
}

.action-btn.favorite-btn.favorited {
	background-color: #E6A23C;
	color: #fff;
}

.action-btn.feedback-btn {
	background-color: #67C23A;
	color: #fff;
}

/* 干员查询结果卡片 */
.character-card-header {
	display: flex;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}

.character-avatar-large {
	width: 100rpx;
	height: 100rpx;
	border-radius: 12rpx;
	border: 2rpx solid #e0e0e0;
	margin-right: 20rpx;
}

.character-info-main {
	flex: 1;
}

.character-name-main {
	font-size: 36rpx;
	font-weight: bold;
	color: #333;
	display: block;
	margin-bottom: 8rpx;
}

/* 盒号标签列表 */
.box-tags-section {
	margin-bottom: 20rpx;
}

.section-label {
	font-size: 26rpx;
	color: #666;
	margin-bottom: 15rpx;
	display: block;
}

.box-tags-list {
	display: flex;
	flex-wrap: wrap;
	gap: 12rpx;
}

.box-tag {
	display: flex;
	align-items: center;
	background-color: #e8f4ff;
	padding: 12rpx 20rpx;
	border-radius: 8rpx;
	transition: all 0.2s;
}

.box-tag:active {
	background-color: #409EFF;
}

.box-tag-text {
	font-size: 28rpx;
	color: #409EFF;
	font-weight: 500;
}

.box-tag:active .box-tag-text {
	color: #fff;
}

.box-tag-arrow {
	font-size: 20rpx;
	color: #409EFF;
	margin-left: 8rpx;
}

.box-tag:active .box-tag-arrow {
	color: #fff;
}

/* 主题适配 - 简单主题 */
.container.theme-simple .result-item.box-card {
	background-color: #fff;
	border: 1rpx solid #e0e0e0;
	box-shadow: none;
}

/* 主题适配 - Ark主题 */
.container.theme-ark .result-item.box-card {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-ark .box-card-title {
	color: #409EFF;
}

/* 主题适配 - 结缘主题 */
.container.theme-jieyuan .result-item.box-card {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

.container.theme-jieyuan .box-card-title {
	color: #e25884;
}

.container.theme-jieyuan .box-tag {
	background-color: rgba(226, 88, 132, 0.1);
}

.container.theme-jieyuan .box-tag-text {
	color: #e25884;
}

.container.theme-jieyuan .box-tag:active {
	background-color: #e25884;
}

.container.theme-jieyuan .box-tag:active .box-tag-text,
.container.theme-jieyuan .box-tag:active .box-tag-arrow {
	color: #fff;
}

/* ========== 盒号详情弹窗（同步 list.vue 卡片样式）========== */
.box-detail-modal {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	background: rgba(0, 0, 0, 0.6);
	z-index: 1000;
	display: flex;
	align-items: center;
	justify-content: center;
}

.box-detail-content {
	background-color: #fff;
	border-radius: 20rpx;
	width: 90%;
	max-width: 650rpx;
	max-height: 85vh;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}

.box-detail-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 20rpx;
	padding-bottom: 15rpx;
	border-bottom: 2rpx solid #f0f0f0;
}
.box-detail-header-left {
	display: flex;
	align-items: center;
	gap: 12rpx;
	flex: 1;
	min-width: 0;
}
.box-detail-title {
	font-size: 36rpx;
	font-weight: bold;
	color: #409EFF;
}
.box-detail-type-tag {
	font-size: 22rpx;
	padding: 4rpx 16rpx;
	border-radius: 20rpx;
	background-color: #409EFF;
	color: #fff;
	flex-shrink: 0;
}
.box-detail-type-tag.tag-whitelist {
	background-color: #67c23a;
}
.box-detail-type-tag.tag-special {
	background-color: #e6a23c;
}
.box-detail-type-tag.tag-ambience {
	background-color: #909399;
}

/* .box-detail-title unified above */

.box-detail-body {
	flex: 1;
	padding: 30rpx;
	max-height: 60vh;
}

.box-detail-footer {
	display: flex;
	gap: 20rpx;
	padding: 20rpx 30rpx;
	border-top: 1rpx solid #f0f0f0;
}

/* 暗色主题适配 */
.container.theme-ark .box-detail-content {
	background: linear-gradient(135deg, rgba(255,255,255,0.95) 0%, rgba(240,248,255,0.95) 100%);
	border: 1rpx solid rgba(64, 158, 255, 0.2);
}

.container.theme-jieyuan .box-detail-content {
	background: #fff;
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}

		/* 搜索栏扫码按钮 */
		.scan-btn-icon {
			width: 64rpx;
			height: 64rpx;
			margin-left: 16rpx;
			display: flex;
			align-items: center;
			justify-content: center;
			flex-shrink: 0;
		}
		.scan-btn-icon-img {
			width: 48rpx;
			height: 48rpx;
		}

		/* 干员跨盒查询弹窗 */
		.char-boxes-modal {
			background-color: #ffffff;
			border-radius: 24rpx;
			width: 80%;
			max-width: 600rpx;
			max-height: 70vh;
			display: flex;
			flex-direction: column;
			overflow: hidden;
		}
		.char-boxes-header {
			display: flex;
			justify-content: space-between;
			align-items: center;
			padding: 30rpx;
			border-bottom: 1rpx solid #f0f0f0;
		}
		.char-boxes-title {
			font-size: 30rpx;
			font-weight: bold;
			color: #333;
		}
		.char-boxes-body {
			padding: 20rpx 30rpx;
		}
		.char-boxes-empty {
			text-align: center;
			color: #999;
			font-size: 26rpx;
			padding: 40rpx 0;
		}
		.char-box-chip {
			display: flex;
			justify-content: space-between;
			align-items: center;
			padding: 26rpx 24rpx;
			margin-bottom: 16rpx;
			background-color: #f5f6f8;
			border-radius: 12rpx;
		}
		.char-box-chip:active {
			background-color: #e8f4ff;
		}
		.char-box-chip-text {
			font-size: 30rpx;
			color: #409EFF;
			font-weight: bold;
		}
		.char-box-chip-arrow {
			font-size: 24rpx;
			color: #999;
		}


		/* 通用弹窗遮罩（跨盒查询弹窗复用） */
		.modal-mask {
			position: fixed;
			top: 0;
			left: 0;
			right: 0;
			bottom: 0;
			background-color: rgba(0, 0, 0, 0.5);
			display: flex;
			align-items: center;
			justify-content: center;
			z-index: 999;
		}
		.modal-close {
			font-size: 36rpx;
			color: #999;
			padding: 0 10rpx;
		}


	/* 隐私协议弹窗 */
</style>
