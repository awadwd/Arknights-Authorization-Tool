	<template>

				<view class="container" :class="'theme-' + themeMode">
				<!-- 左侧抽屉：顶部按钮收进来，点左侧把手滑出 -->
				<view class="left-drawer-handle" @click="toggleLeftDrawer">
					<text class="left-drawer-handle-icon">☰</text>
				</view>
				<view class="left-drawer-mask" v-if="leftDrawerOpen" @click="closeLeftDrawer"></view>
				<view class="left-drawer" :class="{ open: leftDrawerOpen, 'has-top-fns': topRegionInDrawer }">
					<view class="left-drawer-head">
						<text class="left-drawer-title">快捷功能</text>
						<text class="left-drawer-close" @click="closeLeftDrawer">✕</text>
					</view>
					<scroll-view class="left-drawer-scroll" scroll-y>
						<!-- 顶部功能区（抽屉模式） -->
						<view class="drawer-block" v-if="topRegionInDrawer">
							<text class="drawer-block-title">顶部功能区</text>
							<view class="drawer-mode-tabs">
								<view class="drawer-mode-tab" :class="{ active: currentMode === 'show' }" @click="changeMode('show')"><text class="mode-icon">🏆</text><text class="mode-text">点亮模式</text></view>
								<view class="drawer-mode-tab" :class="{ active: currentMode === 'trade' }" @click="changeMode('trade')"><text class="mode-icon">📋</text><text class="mode-text">出物模式</text></view>
							</view>
							<view class="search-input-wrapper drawer-search"><input class="search-input" v-model="searchKeyword" placeholder="搜索角色名称..." @input="handleSearch" /><view class="search-clear" v-if="searchKeyword" @click="clearSearch">×</view></view>
							<view class="search-results drawer-search-results" v-if="showSearchResults && searchResults.length > 0">
								<view class="search-result-item" v-for="(result, index) in searchResults" :key="index" @click="onSearchResultClick(result)"><view class="result-info"><text class="result-character-name">{{ result.character && result.character.name }}</text><text class="result-box-id">盒号 {{ result.boxId }}</text></view><view class="result-actions"><button class="result-btn" :class="isCharacterOwned(result.character && result.character.name) ? 'owned' : 'lightup'" @click.stop="onSearchResultToggle(result)">{{ isCharacterOwned(result.character && result.character.name) ? '已点亮' : '点亮' }}</button></view></view>
							</view>
							<view class="no-search-results" v-if="showSearchResults && searchResults.length === 0 && searchKeyword"><text>没有找到匹配的角色</text></view>
							<view class="drawer-ach-row">
								<view class="achievement-btn drawer-ach-btn" @click="viewAchievements"><text class="trophy-icon">🏆</text><text class="achievement-text">成就榜</text><text class="achievement-count">{{ unlockedAchievements.length }}/{{ achievements.length }}</text><view class="badge" v-if="unreadAchievement > 0"><text class="badge-num">{{ unreadAchievement }}</text></view></view>
								<text class="drawer-ach-tip">单击点亮精一 · 双击点亮精二</text>
							</view>
							<view class="drawer-filters">
								<button class="filter-btn" v-for="(option, index) in boxFilterOptions" :key="index" :class="{ active: boxFilterIndex === index }" @click="setBoxFilter(index)">{{ option }}</button>
							</view>
							<view class="drawer-stats">
								<text class="drawer-stat">点亮 {{ ownedCount }}/{{ totalCharacters }}</text>
								<text class="drawer-stat">想要 {{ wantedCount }}</text>
								<text class="drawer-stat">出物 {{ tradeCount }}</text>
							</view>
							<view class="drawer-elite">
								<view class="elite-option" :class="{ active: eliteLevel === 'all' }" @click="setEliteLevel('all')"><text class="elite-tag all-tag">全部</text></view>
								<view class="elite-option" :class="{ active: eliteLevel === 'elite1' }" @click="setEliteLevel('elite1')"><text class="elite-tag elite1-tag">仅精一</text></view>
								<view class="elite-option" :class="{ active: eliteLevel === 'elite2' }" @click="setEliteLevel('elite2')"><text class="elite-tag elite2-tag">可精二</text></view>
							</view>
						</view>

						<!-- 快捷功能 -->
						<view class="drawer-block">
							<text class="drawer-block-title">快捷功能</text>
							<view class="left-drawer-item" @click="drawerAction('overview')">
								<text class="left-drawer-item-icon">🖼</text>
								<text class="left-drawer-item-text">{{ isGeneratingShareImage ? '生成中..' : '查看概览' }}</text>
							</view>
							<view class="left-drawer-item" @click="openSharePoster">
								<text class="left-drawer-item-icon">📤</text>
								<text class="left-drawer-item-text">分享展柜</text>
							</view>
							<view class="left-drawer-item" @click="drawerAction('want')">
								<text class="left-drawer-item-icon">💜</text>
								<text class="left-drawer-item-text">{{ wantMode ? '取消想要' : '标记想要' }}</text>
							</view>
							<view class="left-drawer-item" @click="drawerAction('scan')">
								<text class="left-drawer-item-icon">📷</text>
								<text class="left-drawer-item-text">扫码查盒</text>
							</view>
						</view>

						<!-- 顶部功能区位置 -->
						<view class="drawer-block">
							<text class="drawer-block-title">顶部功能区位置</text>
							<view class="drawer-seg">
								<view class="drawer-seg-item" :class="{ active: !topRegionInDrawer }" @click="setTopRegionInDrawer(false)">留在页面</view>
								<view class="drawer-seg-item" :class="{ active: topRegionInDrawer }" @click="setTopRegionInDrawer(true)">收进抽屉</view>
							</view>
							<text class="left-drawer-tip">收进抽屉后，页面顶部只保留一条入口；模式切换、搜索、盒筛选与精一/精二筛选都在本抽屉内操作。</text>
						</view>
					</scroll-view>
				</view>


				<!-- 顶部功能区（不随列表滚动）：可整体向上收起，也可整体收进左侧抽屉 -->
				<view class="top-region" :class="{ 'is-folded': topCollapsed, 'is-in-drawer': topRegionInDrawer }">
					<!-- 抽屉模式：页面只留一条入口 -->
					<view class="top-region-entry" v-if="topRegionInDrawer">
						<view class="tre-main" @click="openLeftDrawer">
							<text class="tre-icon">☰</text>
							<text class="tre-text">顶部功能区已收进左侧抽屉</text>
							<text class="tre-meta">点亮 {{ ownedCount }}/{{ totalCharacters }}</text>
						</view>
						<text class="tre-action" @click.stop="setTopRegionInDrawer(false)">收回</text>
					</view>
					<block v-else>
						<view class="top-region-body" v-if="!topCollapsed">
						<view class="mode-section">
							<view class="mode-tabs">

								<view class="mode-tab" :class="{ active: currentMode === 'show' }" @click="changeMode('show')"><text class="mode-icon">🏆</text><text class="mode-text">点亮模式</text></view>

								<view class="mode-tab" :class="{ active: currentMode === 'trade' }" @click="changeMode('trade')"><text class="mode-icon">📋</text><text class="mode-text">出物模式</text></view>

							</view>

					
						</view>

						<view class="search-input-wrapper"><input class="search-input" v-model="searchKeyword" placeholder="搜索角色名称..." @input="handleSearch" /><view class="search-clear" v-if="searchKeyword" @click="clearSearch">×</view></view>

						<view class="search-results" v-if="showSearchResults && searchResults.length > 0">

							<view class="search-result-item" v-for="(result, index) in searchResults" :key="index" @click="onSearchResultClick(result)"><view class="result-info"><text class="result-character-name">{{ result.character && result.character.name }}</text><text class="result-box-id">盒号 {{ result.boxId }}</text></view><view class="result-actions"><button class="result-btn" :class="isCharacterOwned(result.character && result.character.name) ? 'owned' : 'lightup'" @click.stop="onSearchResultToggle(result)">{{ isCharacterOwned(result.character && result.character.name) ? '已点亮' : '点亮' }}</button></view></view>

						</view>

						<view class="no-search-results" v-if="showSearchResults && searchResults.length === 0 && searchKeyword"><text>没有找到匹配的角色</text></view>

						 <view class="operation-bar">

						    <!-- 左侧成就榜按钮 -->

						    <view class="achievement-btn" @click="viewAchievements">

						      <text class="trophy-icon">🏆</text>

						      <text class="achievement-text">成就榜</text>

						      <text class="achievement-count">{{ unlockedAchievements.length }}/{{ achievements.length }}</text>

						      <view class="badge" v-if="unreadAchievement > 0">

						        <text class="badge-num">{{ unreadAchievement }}</text>

						      </view>

						    </view>

						    <!-- 右侧操作提示 -->

						    <view class="tip-area">

						      <text class="tip-text">单击 → 点亮精一</text>

						      <text class="tip-text">双击 → 点亮精二</text>

						    </view>

						  </view>

						<view class="achievement-popup" v-if="showAchievementPopup && currentAchievement"><view class="achievement-popup-content" @click.stop><view class="achievement-popup-icon-text"><text class="achievement-popup-icon">{{ currentAchievement.icon }}</text><text class="achievement-popup-name">{{ currentAchievement.name }}</text></view><text class="achievement-popup-desc">{{ currentAchievement.desc }}</text><button class="achievement-popup-close" @click="showAchievementPopup = false">知道了</button></view></view>

						<view class="achievement-list-mask" v-if="showAchievementList" @click="showAchievementList = false"><view class="achievement-list-panel" @click.stop><view class="achievement-list-header"><text class="achievement-list-title">🏆 成就榜</text><text class="achievement-list-progress">{{ unlockedAchievements.length }}/{{ achievements.length }}</text><button class="achievement-list-close" @click="showAchievementList = false">×</button></view><scroll-view class="achievement-scroll" scroll-y><view class="achievement-item" v-for="item in achievements" :key="item.id" :class="{ unlocked: unlockedAchievements.includes(item.id) }" @click="showAchievementDetail(item)"><text class="achievement-icon">{{ item.icon }}</text><view class="achievement-info"><text class="achievement-name">{{ unlockedAchievements.includes(item.id) ? item.name : '???' }}</text><text class="achievement-desc">{{ item.desc }}</text></view><text class="achievement-status">{{ unlockedAchievements.includes(item.id) ? '已解锁' : '未解锁' }}</text></view></scroll-view><text class="achievement-progress-text">已解锁 {{ unlockedAchievements.length }}/{{ achievements.length }}</text></view></view>

						<view class="filter-stats-section"><view class="box-filter-buttons"><button class="filter-btn" v-for="(option, index) in boxFilterOptions" :key="index" :class="{ active: boxFilterIndex === index }" @click="setBoxFilter(index)">{{ option }}</button></view><view class="stats-info"><text class="stat-item">点亮: {{ ownedCount }}/{{ totalCharacters }}</text><text class="stat-item">想要: {{ wantedCount }}</text><text class="stat-item">出物: {{ tradeCount }}</text></view></view>

						<view class="elite-level-selector"><view class="elite-option" :class="{ active: eliteLevel === 'all' }" @click="setEliteLevel('all')"><text class="elite-tag all-tag">全部</text></view><view class="elite-option" :class="{ active: eliteLevel === 'elite1' }" @click="setEliteLevel('elite1')"><text class="elite-tag elite1-tag">仅精一</text></view><view class="elite-option" :class="{ active: eliteLevel === 'elite2' }" @click="setEliteLevel('elite2')"><text class="elite-tag elite2-tag">可精二</text></view></view>
						</view>
						<view class="top-fold-bar" @click="onFoldBarClick" @touchstart="onFoldTouchStart" @touchend="onFoldTouchEnd">
							<text class="top-fold-dot" :class="{ on: !topCollapsed }"></text>
							<text class="top-fold-text">{{ topCollapsed ? ('顶部功能区已收起 · 点亮 ' + ownedCount + '/' + totalCharacters) : '顶部功能区 · 点此向上收起' }}</text>
							<text class="top-fold-move" v-if="!topCollapsed" @click.stop="setTopRegionInDrawer(true)">⇤ 收进抽屉</text>
							<text class="top-fold-arrow" :class="{ up: !topCollapsed }">⌃</text>
						</view>
					</block>
				</view>

				<scroll-view class="boxes-scroll-view" :class="{ 'is-top-folded': topCollapsed && !topRegionInDrawer, 'is-top-in-drawer': topRegionInDrawer }" :style="boxesScrollHeight ? ('height: ' + boxesScrollHeight + 'px') : ''" scroll-y @scrolltolower="loadMore" @scroll="onPageScroll" :scroll-top="scrollTop" :scroll-into-view="scrollTargetId">

					<view class="box-section" v-for="(box, index) in displayBoxes" :key="box && (box.Box_id || index)" :id="boxSectionDomId(box && box.Box_id)" :class="{'box-highlight': box && box.Box_id === highlightBoxId}">

						<view class="box-header"><text class="box-title">盒号 {{ box && box.Box_id || '未知' }}</text><text class="box-type-tag" :class="'type-' + getBoxTypeClass(box && box.Box_type)">{{ getBoxTypeText(box && box.Box_type) }}</text><button class="select-box-btn" @click="toggleSelectBox(box && box.Box_id)">{{ isBoxAllSelected(box) ? '取消全选' : '全选点亮' }}</button></view>

						<view class="characters-grid">

							<view class="character-item" v-for="(character, charIndex) in getFilteredCharacters(box)" :key="charIndex" @click="toggleCharacterOwnership(character, box && box.Box_id)" @longpress="showCharacterActionSheet(character, box && box.Box_id)">

								<view class="character-avatar-wrapper"><image class="character-avatar" :class="{ 'owned': isCharacterOwnedAtLevel(character && character.name, 'elite1', box && box.Box_id) || isCharacterOwnedAtLevel(character && character.name, 'elite2', box && box.Box_id), 'grayed': !isCharacterOwnedAtLevel(character && character.name, 'elite1', box && box.Box_id) && !isCharacterOwnedAtLevel(character && character.name, 'elite2', box && box.Box_id), 'wanted': isCharacterWanted(character && character.name), 'for-trade': isCharacterForTrade(character && character.name) }" :src="imgSrc(displayAvatar(character, box))" mode="aspectFit" @error="onAvatarError(character, box)" /><view class="avatar-level-switch" v-if="canSwitchAvatar(character, box)" @click.stop="toggleAvatarLevel(character, box)"><text class="avatar-level-text">{{ avatarLevelOf(character, box) === 2 ? '精二' : '精一' }}</text></view><view class="status-indicators"><view class="owned-indicator elite1-indicator" v-if="isCharacterOwnedAtLevel(character && character.name, 'elite1', box && box.Box_id)">★</view><view class="owned-indicator elite2-indicator" v-if="isCharacterOwnedAtLevel(character && character.name, 'elite2', box && box.Box_id) && !isCharacterOwnedAtLevel(character && character.name, 'pair', box && box.Box_id)">★★</view><view class="want-indicator" v-if="isCharacterWanted(character && character.name)">💜</view><view class="trade-indicator" v-if="isCharacterForTrade(character && character.name)">📋</view><view class="hot-indicator" v-if="character && character.hotcharacter">🔥</view><view class="pair-indicator" v-if="getCharacterPairCount(character && character.name, box && box.Box_id) > 0"><text class="pair-text">一对</text><text class="pair-count">×{{ getCharacterPairCount(character && character.name, box && box.Box_id) }}</text></view></view><view class="quantity-display" v-if="showQuantityStepper && currentMode !== 'trade' && charHasAnyCount(character && character.name, box && box.Box_id)"><view class="quantity-item" v-if="canHavePair(character)"><text class="elite-label pair-label">对</text><view class="quantity-controls"><text class="quantity-btn" @click.stop="adjustQuantity(character && character.name, 'pair', -1, box && box.Box_id)">-</text><text class="quantity-value">{{ getCharacterPairCount(character && character.name, box && box.Box_id) }}</text><text class="quantity-btn" @click.stop="adjustQuantity(character && character.name, 'pair', 1, box && box.Box_id)">+</text></view></view><view class="quantity-item"><text class="elite-label elite1-label">一</text><view class="quantity-controls"><text class="quantity-btn" @click.stop="adjustQuantity(character && character.name, 'elite1', -1, box && box.Box_id)">-</text><text class="quantity-value">{{ getCharacterLooseCount(character && character.name, 'elite1', box && box.Box_id) }}</text><text class="quantity-btn" @click.stop="adjustQuantity(character && character.name, 'elite1', 1, box && box.Box_id)">+</text></view></view><view class="quantity-item" v-if="canHavePair(character)"><text class="elite-label elite2-label">二</text><view class="quantity-controls"><text class="quantity-btn" @click.stop="adjustQuantity(character && character.name, 'elite2', -1, box && box.Box_id)">-</text><text class="quantity-value">{{ getCharacterLooseCount(character && character.name, 'elite2', box && box.Box_id) }}</text><text class="quantity-btn" @click.stop="adjustQuantity(character && character.name, 'elite2', 1, box && box.Box_id)">+</text></view></view></view></view>

								<text class="character-name">{{ (character && character.name) || '未知角色' }}</text><text class="loose-line" v-if="currentMode !== 'trade' && looseSummary(character && character.name, box && box.Box_id)">{{ looseSummary(character && character.name, box && box.Box_id) }}</text>
								<view class="trade-marks" v-if="currentMode === 'trade' && getTradeMarks(character && character.name).length > 0"><text class="trade-mark" v-for="(m, mi) in getTradeMarks(character && character.name)" :key="mi" :class="'mk-' + m.status">{{ m.short }}</text></view>

							</view>

						</view>

					</view>

					<view class="load-more" v-if="hasMore && filteredBoxes.length > displayBoxes.length"><text>加载更多...</text></view>

					<view class="empty-state" v-if="displayBoxes.length === 0"><image class="empty-icon" src="/static/empty-icon.png" /><text class="empty-text">{{ emptyText }}</text><button class="sync-btn" @click="syncWithFavorites">✅从收藏同步</button><button class="debug-btn" @click="debugLocalStorage">🔍 调试数据</button><button class="load-btn" @click="forceLoadData">🔍 强制加载数据</button></view>

				</scroll-view>

				<view class="back-to-top" v-if="showBackToTop" @click="scrollToTop"><text class="back-to-top-icon">⬆</text></view>

				<!-- 完整概览页面 -->

				<view class="overview-page" v-if="showSharePreviewModal" id="overviewContent" :class="'overview-' + currentMode">

					<!-- 分享图预览区域 -->
					<view class="share-image-preview-section" v-if="shareImageReady && shareImagePaths.length > 0">
						<view class="share-preview-header">
							<text class="share-preview-title">分享图已生成 ({{ currentShareIndex + 1 }}/{{ shareImagePaths.length }})</text>
						</view>
						<scroll-view class="share-preview-scroll" scroll-y>
							<image 
								class="share-preview-img" 
								:src="shareImagePaths[currentShareIndex]" 
								mode="widthFix"
								@load="onShareImageLoad"
								@error="onShareImageError"
							/>
							<view style="height: 40rpx;"></view>
						</scroll-view>
						<!-- 翻页控件 -->
						<view class="share-pagination" v-if="shareImagePaths.length > 1">
							<button class="page-btn" :disabled="currentShareIndex === 0" @click="prevShareImage">上一页</button>
							<text class="page-indicator">{{ currentShareIndex + 1 }} / {{ shareImagePaths.length }}</text>
							<button class="page-btn" :disabled="currentShareIndex === shareImagePaths.length - 1" @click="nextShareImage">下一页</button>
						</view>
						<view class="share-preview-actions">
							<button class="share-action-btn primary" @click="shareImage">分享当前图片</button>
							<button class="share-action-btn secondary" @click="saveAllShareImages" v-if="shareImagePaths.length > 1">保存全部({{ shareImagePaths.length }}张)</button>
							<button class="share-action-btn secondary" @click="saveShareImage">保存当前</button>
							<button class="share-action-btn secondary" @click="closeSharePreviewModal">返回概览</button>
						</view>
					</view>

					<scroll-view class="overview-scroll" scroll-y v-else>

						<!-- 顶部导航 -->

						<view class="overview-header">

							<button class="overview-close-btn" @click="closeSharePreviewModal">← 返回</button>

							<text class="overview-title">

								{{ currentMode === 'show' ? '🏆 收集概览' : '📋 出物概览' }}

							</text>

							<view style="width: 100rpx;"></view>

						</view>

						<!-- 概览统计卡片 -->

						<view class="overview-cards">

							<view class="overview-card-item">

								<image class="card-icon-img" src="/static/elite0.png" mode="aspectFit" />

								<text class="card-value">{{ currentMode === 'show' ? ownedCount : tradeOutCount }}</text>

								<text class="card-label">{{ currentMode === 'show' ? '总收藏' : '出' }}</text>

							</view>

							<view class="overview-card-item elite1-card">

								<image class="card-icon-img" src="/static/elite0.png" mode="aspectFit" />

								<text class="card-value">{{ currentMode === 'show' ? elite1Count : tradeSwapCount }}</text>

								<text class="card-label">{{ currentMode === 'show' ? '精一' : '换' }}</text>

							</view>

							<view class="overview-card-item wanted-card">

								<text class="card-icon">{{ currentMode === 'show' ? '💜' : '🙏' }}</text>

								<text class="card-value">{{ currentMode === 'show' ? wantedCount : tradeNeedCount }}</text>

								<text class="card-label">{{ currentMode === 'show' ? '想要' : '要' }}</text>

							</view>

							<view class="overview-card-item pair-card">

								<image class="card-icon-img" src="/static/elite2_golden.png" mode="aspectFit" />

								<text class="card-value">{{ currentMode === 'show' ? pairCount : tradeCount }}</text>

								<text class="card-label">{{ currentMode === 'show' ? '一对' : '出物干员' }}</text>

							</view>

						</view>

						<!-- 收集进度 -->

						<view class="progress-ring-section" v-if="currentMode === 'show'">

							<text class="section-title">📊 收集进度</text>

							<view class="progress-ring-wrapper">

								<view class="progress-ring">

									<view class="progress-ring-bg"></view>

									<view class="progress-ring-fill" :style="{ '--progress': overallProgressPercent + '%' }"></view>

									<view class="progress-ring-text">

										<text class="progress-percent">{{ overallProgressPercent }}%</text>

										<text class="progress-sublabel">完成度</text>

									</view>

								</view>

								<view class="progress-details">

									<view class="progress-detail-item">

										<text class="detail-label">已收集</text>

										<text class="detail-value">{{ ownedCount }}/{{ totalCharacterCount }}</text>

									</view>

									<view class="progress-detail-item">

										<text class="detail-label">精一对</text>

										<text class="detail-value">{{ pairCount }}个</text>

									</view>

									<view class="progress-detail-item">

										<text class="detail-label">已完成盒子</text>

										<text class="detail-value">{{ completedBoxCount }}/{{ totalBoxCount }}</text>

									</view>

									<view class="progress-detail-item" v-if="wantedCount > 0">

										<text class="detail-label">想要</text>

										<text class="detail-value wanted-highlight">{{ wantedCount }}个</text>

									</view>

								</view>

							</view>

						</view>

						<!-- 盒子完成度排行 -->

						<view class="box-ranking-section">

							<text class="section-title">🏆 盒子完成度排行</text>

							<scroll-view class="box-ranking-scroll" scroll-y :scroll-top="boxRankingScrollTop" @scroll="onBoxRankingScroll">

								<view class="box-ranking-list">

									<view class="box-ranking-item" v-for="(box, index) in boxRankingList.slice(0, boxRankingShowCount)" :key="index">

										<view class="box-rank" :class="'rank-' + (index + 1)">{{ index + 1 }}</view>

										<view class="box-info">

											<text class="box-name">{{ box.name }}</text>

											<text class="box-type-tag" :class="'type-' + box.type">{{ box.typeText }}</text>

										</view>

										<view class="box-progress-bar">

											<view class="box-progress-fill" :style="{ width: box.progressPercent + '%' }" :class="{ complete: box.progressPercent === 100 }"></view>

										</view>

										<text class="box-count">{{ box.collected }}/{{ box.total }}</text>

									</view>

									<view class="empty-tip" v-if="boxRankingList.length === 0">

										<text>暂无收集数据</text>

									</view>

								</view>

								<view class="load-more-boxes" v-if="boxRankingList.length > boxRankingShowCount" @click="boxRankingShowCount += 10">

									<text class="load-more-text">展开更多 (还有{{ boxRankingList.length - boxRankingShowCount }}盒)</text>

								</view>

								<view class="load-more-boxes collapsed" v-else-if="boxRankingList.length > 10" @click="boxRankingShowCount = 10">

									<text class="load-more-text">收起</text>

								</view>

							</scroll-view>

						</view>

						<!-- 精英分布 -->

						<view class="distribution-section" v-if="currentMode === 'show'">

							<text class="section-title">📈 精英分布</text>

							<view class="distribution-bars">

								<view class="distribution-item">

									<text class="dist-label">精一(单卡)</text>

									<view class="dist-bar-wrapper">

										<view class="dist-bar dist-bar-elite1" :style="{ width: elite1Percent + '%' }"></view>

									</view>

									<text class="dist-value">{{ singleElite1Count }}</text>

								</view>

								<view class="distribution-item">

									<text class="dist-label">精二</text>

									<view class="dist-bar-wrapper">

										<view class="dist-bar dist-bar-elite2" :style="{ width: elite2Percent + '%' }"></view>

									</view>

									<text class="dist-value">{{ singleElite2Count }}</text>

								</view>

								<view class="distribution-item">

									<text class="dist-label">一对</text>

									<view class="dist-bar-wrapper">

										<view class="dist-bar dist-bar-pair" :style="{ width: pairPercent + '%' }"></view>

									</view>

									<text class="dist-value">{{ pairCount }}</text>

								</view>

							</view>

						</view>

						<!-- 收集时间线 -->

						<view class="timeline-section" v-if="currentMode === 'show'">

							<text class="section-title">📅 收集记录</text>

							<view class="timeline-list">

								<view class="timeline-item">

									<text class="timeline-label">首次收集</text>

									<text class="timeline-value">{{ firstCollectTime || '暂无记录' }}</text>

								</view>

								<view class="timeline-item">

									<text class="timeline-label">最近收集</text>

									<text class="timeline-value">{{ lastCollectTime || '暂无记录' }}</text>

								</view>

								<view class="timeline-item">

									<text class="timeline-label">收集天数</text>

									<text class="timeline-value">{{ collectDays > 0 ? (collectDays + '天') : '暂无记录' }}</text>

								</view>

								<view class="timeline-item">

									<text class="timeline-label">想要的角色</text>

									<text class="timeline-value">{{ wantedCount }}个</text>

								</view>

								<view class="timeline-item">

									<text class="timeline-label">出物中</text>

									<text class="timeline-value">{{ tradeCount }}个</text>

								</view>

							</view>

						</view>

						<!-- 出物列表（出物模式外显示） -->

						<view class="overview-list" v-if="currentMode === 'trade'">

							<text class="section-title">📋 出 / 换 / 要 清单</text>

							<view class="owned-list-items">

								<view class="owned-list-item" v-for="(item, index) in getTradeList()" :key="index">

									<image class="item-avatar" :src="imgSrc(item.avatar)" />

									<view class="item-info">

										<text class="item-name">{{ item.name }}</text>

										<view class="item-badges">

											<text class="item-badge" v-for="(m, mi) in item.marks" :key="mi" :class="'mk-' + m.status">{{ m.short }}</text>

										</view>

									</view>

								</view>

							</view>

						</view>

						<!-- 分享选项 -->

						<view class="share-options-section">

							<view class="share-option-item" @click="onlyShowProgressBoxes = !onlyShowProgressBoxes">

								<view class="share-option-checkbox" :class="{ checked: onlyShowProgressBoxes }">

									<text v-if="onlyShowProgressBoxes">✓</text>

								</view>

								<view class="share-option-text">

									<text class="share-option-title">只生成有进度的收集盒号</text>

									<text class="share-option-desc">{{ onlyShowProgressBoxes ? '仅显示有收集进度的盒子' : '显示全部盒子（空盒折叠成网格）' }}</text>

								</view>

							</view>

						</view>

						<!-- 底部按钮 -->

						<view class="overview-footer">

							<button class="generate-btn" @click="openBoxesPoster" :disabled="isGeneratingShareImage">

								{{ isGeneratingShareImage ? '生成中..' : (onlyShowProgressBoxes ? '📊 生成进度长图' : '📊 生成进度长图（可能多张）') }}

							</button>

						</view>

						<view style="height: 40rpx;"></view>

					</scroll-view>

				</view>

				<canvas canvas-id="shareCanvas" style="position: absolute; left: -9999px; top: -9999px; width: 750rpx; height: 4000rpx;" id="shareCanvas"></canvas>

				<!-- App 端海报用 canvas 重绘，不再截屏（截屏在 Android 上只能出黑图） -->
				<!-- #ifdef APP-PLUS -->
				<canvas canvas-id="posterCanvas" id="posterCanvas" style="position: absolute; left: -9999px; top: -9999px; width: 750rpx; height: 3200rpx;"></canvas>
				<!-- #endif -->

				<view class="bottom-action-bar"><view class="quick-actions" v-if="currentMode !== 'trade'"><button class="quick-btn elite1-btn" @click="selectAllElite1"><text>全选精一</text></button><button class="quick-btn elite2-btn" @click="selectAllElite2"><text>全选精二</text></button><button class="quick-btn pair-btn" @click="selectAllPair"><text>全选一对</text></button><button class="quick-btn clear-btn" @click="clearAll"><text>清空所有</text></button></view><view class="trade-bar-tip" v-else><text class="trade-bar-tip-text">出物模式 · 点角色编辑「出 / 换 / 要」，长按可改点亮状态</text></view><view class="summary-info"><text class="summary-text">已点亮 {{ ownedCount }} | 想要: {{ wantedCount }}</text><text class="summary-text" v-if="currentMode === 'trade'">出物: {{ tradeCount }}</text><text class="qty-toggle" :class="{ on: showQuantityStepper }" v-if="currentMode !== 'trade'" @click.stop="toggleQuantityStepper">{{ showQuantityStepper ? '隐藏数量' : '显示数量' }}</text></view></view>

				<!-- 出物标记编辑层：点角色弹出，按 精一/精二/一对 分别标 出/换/要 -->
				<view class="trade-sheet-mask" v-if="tradeSheetOpen" @click="closeTradeSheet">
					<view class="trade-sheet" @click.stop>
						<view class="trade-sheet-head">
							<image class="trade-sheet-avatar" :src="imgSrc(tradeSheetAvatar)" mode="aspectFit" />
							<view class="trade-sheet-title-box">
								<text class="trade-sheet-name">{{ tradeSheetName }}</text>
								<text class="trade-sheet-sub">{{ tradeSheetBoxLabel }}{{ tradeSheetBoxLabel ? ' · ' : '' }}点亮后才能标「出 / 换」</text>
							</view>
							<text class="trade-sheet-close" @click="closeTradeSheet">×</text>
						</view>
						<view class="trade-sheet-body">
							<view class="trade-sheet-row" v-for="row in tradeSheetRows" :key="row.level">
								<view class="trade-row-head">
									<text class="trade-row-level">{{ row.text }}</text>
									<text class="trade-row-own" :class="{ on: row.owned }" @click="toggleTradeRowOwned(row)">{{ row.owned ? '已点亮' : '未点亮' }}</text>
									<text class="trade-row-price" v-if="row.status === 'out' && !(row.price > 0)">未定价</text>
								</view>
								<view class="trade-row-opts">
									<text class="trade-opt mk-out" :class="{ active: row.status === 'out', off: !row.owned }" @click="pickTradeStatus(row.level, 'out')">出</text>
									<text class="trade-opt mk-swap" :class="{ active: row.status === 'swap', off: !row.owned }" @click="pickTradeStatus(row.level, 'swap')">换</text>
									<text class="trade-opt mk-want" :class="{ active: row.status === 'want' }" @click="pickTradeStatus(row.level, 'want')">要</text>
									<input
										v-if="row.status === 'out'"
										class="trade-price-field"
										type="number"
										:value="row.price ? row.price : ''"
										placeholder="售价 ¥"
										@input="onTradeSheetPrice(row.level, $event)"
									/>
								</view>
							</view>
							<view class="trade-sheet-empty" v-if="!tradeSheetRows.length"><text>这个角色暂无可标记的等级</text></view>
						</view>
						<view class="trade-sheet-foot">
							<text class="trade-sheet-tip">同一等级只能选一种状态，再点一次取消；价格只对「出」生效</text>
							<button class="trade-sheet-done" @click="closeTradeSheet">完成</button>
						</view>
					</view>
				</view>


				<view class="debug-modal" v-if="showDebugModal" @tap="closeDebugModal"><view class="debug-modal-content" @tap.stop><view class="debug-modal-header"><text class="debug-modal-title">调试信息</text><button class="debug-close-btn" @click="closeDebugModal">×</button></view><scroll-view class="debug-body" scroll-y><view class="debug-section"><text class="debug-section-title">数据状态</text><view class="debug-item"><text class="debug-label">过滤后盒号数:</text><text class="debug-value">{{ filteredBoxes.length }}</text></view><view class="debug-item"><text class="debug-label">显示盒子数:</text><text class="debug-value">{{ displayBoxes.length }}</text></view><view class="debug-item"><text class="debug-label">空状态:</text><text class="debug-value">{{ emptyText }}</text></view></view><view class="debug-section"><text class="debug-section-title">用户数据</text><view class="debug-item"><text class="debug-label">已点亮:</text><text class="debug-value">{{ ownedCount }}个</text></view><view class="debug-item"><text class="debug-label">想要:</text><text class="debug-value">{{ wantedCount }}个</text></view><view class="debug-item"><text class="debug-label">出物:</text><text class="debug-value">{{ tradeCount }}个</text></view></view><view class="debug-section"><text class="debug-section-title">本地存储</text><view class="debug-item"><text class="debug-label">arknightsData:</text><text class="debug-value">{{ localStorage.arknightsData ? '存在' : '不存在' }}</text></view><view class="debug-item"><text class="debug-label">收藏:</text><text class="debug-value">{{ localStorage.favoriteCharacterNames ? (localStorage.favoriteCharacterNames.length + '个') : '不存在' }}</text></view></view><view class="debug-section"><button class="test-btn" @click="testCharacterExtraction">测试角色提取</button><view class="test-result" v-if="testResult"><text class="test-result-text">{{ testResult }}</text></view></view></scroll-view><view class="debug-modal-footer"><button class="debug-action-btn" @click="clearLocalStorage">🗑️ 清除所有数据</button><button class="debug-action-btn" @click="reloadData">🔄 重新加载</button></view></view></view>

			<!-- 分享预览：展柜名片铺满整屏，App 端截图即成品 -->
			<view class="shot-layer" v-if="sharePosterOpen">
				<view class="shot-page">
					<view class="shot-card">
						<view class="shot-head">
							<text class="shot-title">我的通行证展柜</text>
							<text class="shot-sub">点亮 {{ ownedCount }}<text v-if="totalCharacters > 0"> / {{ totalCharacters }}</text> · 想要 {{ wantedCount }} · 出物 {{ tradeCount }}</text>
						</view>

						<view class="shot-list" v-if="shareNameList.length">
							<view class="shot-chip" :class="{ gold: c.elite2 }" v-for="(c, i) in shareNameList" :key="i">
								<text class="shot-chip-name">{{ c.name }}</text>
								<text class="shot-chip-tag" v-if="c.elite2">精二</text>
							</view>
							<view class="shot-chip more" v-if="shareNameRest > 0">
								<text class="shot-chip-name">+{{ shareNameRest }}</text>
							</view>
						</view>
						<view class="shot-note" v-if="shareNameList.length">
							<text class="shot-note-text">共 {{ shareOwnedList.length }} 位干员已点亮</text>
							<text class="shot-note-text">精二 {{ shareElite2Count }} 位</text>
						</view>
						<view class="shot-empty" v-else>
							<text class="shot-empty-text">还没有点亮任何通行证，去展柜勾一勾吧</text>
						</view>
					</view>
				</view>

				<view class="shot-foot">
					<image class="shot-qr" src="/static/wechat-code.jpg" mode="aspectFit"></image>
					<view class="shot-foot-text">
						<text class="shot-foot-title">微信扫码 · 记录你的每一张通行证</text>
						<text class="shot-foot-sub">数据只存在本机，不上传服务器</text>
					</view>
				</view>

				<view class="shot-bar" v-if="!capturing">
					<view class="poster-btn" @click="closeSharePoster"><text>关闭</text></view>
					<!-- #ifdef APP-PLUS -->
					<view class="poster-btn" @click="sendPoster"><text>发送图片</text></view>
					<view class="poster-btn primary" @click="savePoster"><text>保存图片</text></view>
					<!-- #endif -->
					<!-- #ifdef MP-WEIXIN -->
					<button class="poster-btn poster-btn-share" open-type="share"><text>小程序卡片</text></button>
					<view class="poster-btn primary" @click="copyPosterText"><text>复制文案</text></view>
					<!-- #endif -->
					<!-- #ifdef H5 -->
					<view class="poster-btn primary" @click="copyPosterText"><text>复制文案</text></view>
					<!-- #endif -->
				</view>

				<view class="poster-hint" v-if="!capturing">
					<text>{{ posterHint }}</text>
				</view>
			</view>


		</view>

	</template>



<script>

import errorLog from "@/utils/errorLog.js";
import { isShell } from "@/utils/wideLayout.js";
import { markDailyTask } from "@/utils/dailyTask.js";
import { openLink, linkActionText } from "@/utils/openLink.js";
import { elite2Url } from "@/utils/eliteAvatar.js";
import { openMpShareShot } from "@/utils/screenshot.js";
// #ifdef APP-PLUS
import { renderGalleryPoster } from "@/utils/posterCanvas.js";
// #endif
import { mergeGuessData, pruneStoredGuessData } from "@/utils/guessData.js";
export default {

	data() {

		return {

			// 主题模式
			themeMode: 'simple',
			filteredBoxes: [],

			displayBoxes: [], // 当前显示的盒子（分页）
			highlightBoxId: '',
			scrollTargetId: '',
			barcodeMap: {},

			// 隐私协议（官方API）
			showPrivacyModal: false,
			privacyContractName: '《隐私保护指引》',
			pendingScanAction: false,

			// 用户数据 - 支持数量和成对状态总数

			ownedCharacters: {}, // {角色名 {boxId: 'xxx', elite1: {owned: bool, count: num, paired: bool}, elite2: {owned: bool, count: num, paired: bool}, pair: {owned: bool, count: num}, lastUpdated: timestamp}}

			wantedCharacters: [], // 想要的角色

			tradeCharacters: {}, // 出物的角色{角色名 {elite1: {price: 0, note: ''}, elite2: {price: 0, note: ''}}}

			// 出物编辑层（点角色弹出）：出 / 换 / 要 × 精一 / 精二 / 一对
			tradeSheetOpen: false,
			tradeSheetName: '',
			tradeSheetBoxId: '',
			tradeSheetAvatar: '',
			tradeSheetBoxLabel: '',
			tradeSheetPairOk: true,
			tradeSheetCharacter: null, // 原始角色对象（带 boxType，别用 findCharacterByName 重查丢了字段）
			tradeOutCount: 0, // 「出」的项数（精一/精二/一对各算一项）
			tradeSwapCount: 0, // 「换」的项数
			tradeNeedCount: 0, // 「要」的项数



			// 界面状态

			currentMode: 'show', // 'show'或'trade'

			leftDrawerOpen: false,

			topCollapsed: false,

			topRegionInDrawer: false, // 顶部功能区是否整体收进左抽屉

			boxesScrollHeight: 0, // 列表实测高度（0 = 用 CSS 兜底）

			boxHeightTimer: null,

			foldTouchY: 0,

			foldSwipeAt: 0,

			wantMode: false,

			showOverviewPage: false, // 是否显示概览页面

			eliteLevel: 'all', // 'all', 'elite1', 'elite2'



			avatarPref: {}, // 一对时显示精一还是精二：{'角色名@盒号': 1|2}



			avatarFailed: {}, // 加载失败的头像地址，避免反复请求坏图

			boxFilterIndex: 0, // 默认选择全部

			boxFilterOptions: ['全部', '常规款', '白名单', '特别通行', '联动款', '音律联觉'],

			boxFilterMap: {

				0: null, // 全部

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

		showBackToTop: false,



			// 分享预览弹窗

			showSharePreviewModal: false,

			showShareImagePreview: false, // 是否显示分享图片预览

			isGeneratingShareImage: false,

			shareImagePath: '',

			shareImageReady: false,

			shareImagePaths: [], // 多张分享图路径（分片模式）

				currentShareIndex: 0, // 当前显示的图片索引

			onlyShowProgressBoxes: true, // 默认只显示有进度的盒子

			showShareOptions: false, // 是否显示分享选项

			currentShareImageIndex: 0, // 当前显示的图片索引

			pendingSlices: [], // 待生成的分片

			shareShowPrices: true,

			shareShowWanted: true,

			watermarkText: '明日方舟通行证收集',

			shareGeneratedTime: '',



			// 点击检测相关

			clickTimers: {}, // {characterName: timerId}

			clickCounts: {}, // {characterName: count}

			lastClickTime: {}, // {characterName: timestamp}

			CLICK_INTERVAL: 300, // 多次点击判定间隔（毫秒）

			longPressTimer: null,

			LONG_PRESS_DURATION: 500, // 长按判定时长（秒）



			// 搜索相关

			searchKeyword: '',

			searchResults: [],

			showSearchResults: false,



			// 默认头像

			defaultAvatar: '/static/default-avatar.png',



			// 统计数据

			ownedCount: 0,

			wantedCount: 0,

			tradeCount: 0,

			totalCharacters: 0,

			// pairCount 由 computed 属性计算

			// 卡片上的数量步进默认隐藏（6 列栅格里太挤、容易误触），按需在底部打开
			showQuantityStepper: false,

			singleElite1Count: 0, // 单精一数量

			singleElite2Count: 0, // 单精二数量

			// 概览增强统计

			completionRate: 0,

			// 分享海报

			sharePosterOpen: false,

			capturing: false,


			completedBoxes: 0,

			totalBoxes: 0,

			ownedItems: 0, // 收集项总数（精一+精二+一对分别数）

			elite1Percent: 0,

			elite2Percent: 0,

			pairPercent: 0,

			firstCollectTime: '',

			lastCollectTime: '',

			collectDays: 0,

			boxRankings: [],

			boxRankingShowCount: 10, // 每次展开10个盒号

			boxRankingScrollTop: 0, // 排行榜滚动位置

			// 空状态文本

			emptyText: '暂无数据或没有符合条件的角色',



			// 调试弹窗

			showDebugModal: false,

			localStorage: {},

			testResult: '',



			// 加载状态

			isLoading: false,



			// 当前操作的角色

			currentActionCharacter: null,



			// 成就系统

			achievements: [], // 成就定义列表

			unlockedAchievements: [], // 已解锁的成就ID

			showAchievementPopup: false, // 是否显示成就弹窗

			currentAchievement: null, // 当前解锁的成就

			showAchievementList: false, // 是否显示成就列表
				unreadAchievement: 0, // 未读成就数量

		}

	},

	computed: {

		// 出物编辑层的三行（精一 / 精二 / 一对）
		tradeSheetRows() {
			const name = this.tradeSheetName;
			if (!name) return [];
			const levels = this.tradeSheetPairOk ? ['elite1', 'elite2', 'pair'] : ['elite1'];
			return levels.map((level) => {
				const entry = this.getTradeEntry(name, level);
				const owned = this.isTradeLevelOwned(name, level, this.tradeSheetBoxId);
				return {
					level: level,
					text: this.tradeLevelText(level),
					owned: owned,
					status: entry.status,
					price: entry.price
				};
			});
		},

		// 当前筛选的盒子类型

		currentBoxFilter() {

			return this.boxFilterMap[this.boxFilterIndex] || 'all';

		},



		// 是否全部选中（高级操作）

		isAllSelected() {

			const allCharacters = this.getAllCharactersList();

			if (allCharacters.length === 0) return false;

			return allCharacters.every(char => char && this.isCharacterOwned(char.name));

		},

		

		// 精一数量

		elite1Count() {

			let count = 0;

			Object.values(this.ownedCharacters).forEach(charData => {

				Object.values(charData).forEach(boxData => {

					if (boxData.elite1?.owned) count++;

				});

			});

			return count;

		},



		// 一对数量

		// 分享海报用的已点亮名单。
		//
		// 口径必须和 ownedCount 一致：走「全部盒子 + 用户点亮记录」，
		// 不受盒子筛选、分页、精一/精二筛选影响。
		// 旧实现遍历 displayBoxes（一页只有 3 个盒子），会出现图上写
		// 「点亮 12 / 40」、名单却只列 2 个干员的自相矛盾。
		shareOwnedList() {
			const owned = this.ownedCharacters || {};
			// 先算 name -> 状态，避免在盒子循环里反复遍历点亮记录
			const state = {};
			for (const name in owned) {
				const rec = owned[name] || {};
				let any = false;
				let e2 = false;
				for (const bid in rec) {
					const d = rec[bid];
					if (!d) continue;
					const n1 = this.getCharacterEliteCount(name, 'elite1', bid);
					const n2 = this.getCharacterEliteCount(name, 'elite2', bid);
					if (n1 > 0 || n2 > 0) any = true;
					if (n2 > 0) e2 = true;
				}
				if (any) state[name] = { elite2: e2 };
			}

			const out = [];
			const seen = {};
			const push = (name, st) => {
				if (!name || seen[name] || !st) return;
				seen[name] = true;
				out.push({ name: name, elite2: !!st.elite2 });
			};

			// 先按盒子顺序走，名单顺序和展柜一致；
			// 用 getBoxCharacters 的原始角色表，不套 eliteLevel 筛选
			const boxes = Array.isArray(this.characterData) ? this.characterData : [];
			for (let i = 0; i < boxes.length; i++) {
				const chars = this.getBoxCharacters(boxes[i]) || [];
				for (let j = 0; j < chars.length; j++) {
					const n = chars[j] && chars[j].name;
					if (n && !seen[n]) push(n, state[n]);
				}
			}
			// 兜底：点亮记录里有、但当前盒子数据里找不到的干员（活动下线/数据更新），
			// 不补进来的话「点亮数」和名单长度还是会差
			for (const name in state) {
				if (!seen[name]) push(name, state[name]);
			}
			return out;
		},
		shareNameList() {
			// 分享图是拿来看的，精二优先展示；截断 32 个时也不会把精二挤出画面。
			// 不用 sort，避免依赖排序稳定性，直接分组拼。
			const list = this.shareOwnedList;
			const gold = [];
			const plain = [];
			for (let i = 0; i < list.length; i++) {
				(list[i].elite2 ? gold : plain).push(list[i]);
			}
			return gold.concat(plain).slice(0, 20);
		},
		shareNameRest() {
			// 与 shareNameList 的 20 个上限保持一致
			return Math.max(0, this.shareOwnedList.length - 20);
		},
		shareElite2Count() {
			return this.shareOwnedList.filter(item => item.elite2).length;
		},

		posterHint() {
			// #ifdef APP-PLUS
			return '点「保存图片」会把这张卡片存进相册';
			// #endif
			// #ifdef MP-WEIXIN
			return '点「保存图片」会调用 Skyline 截图生成图片，需微信 8.0.33 以上';
			// #endif
			// #ifdef H5
			return '用系统截图保存这张卡片，或复制文案分享';
			// #endif
			return '';
		},
		pairCount() {
			// 与 getCharacterPairCount 同源：对数 = min(精一件数, 精二件数)
			let count = 0;
			Object.values(this.ownedCharacters).forEach(charData => {
				if (!charData) return;
				Object.values(charData).forEach(boxData => {
					if (!boxData) return;
					const p = (boxData.pair && boxData.pair.count) || 0;
					const e1 = boxData.elite1 && typeof boxData.elite1.count === 'number' ? boxData.elite1.count : p;
					const e2 = boxData.elite2 && typeof boxData.elite2.count === 'number' ? boxData.elite2.count : p;
					count += Math.min(e1, e2);
				});
			});
			return count;
		},

		

		// 总卡片数（考虑nolyELITE1）

		totalCharacterCount() {

			let total = 0;

			this.filteredBoxes.forEach(box => {

				total += this.getBoxTotalCardCount(box);

			});

			return total;

		},



		// 总盒子数

		totalBoxCount() {

			return this.filteredBoxes.length;

		},



		// 已完成盒子数（盒子内所有卡片都点亮）

		completedBoxCount() {

			let completed = 0;

			this.filteredBoxes.forEach(box => {

				const total = this.getBoxTotalCardCount(box);

				const owned = this.getBoxOwnedCardCount(box);

				if (total > 0 && owned >= total) {

					completed++;

				}

			});

			return completed;

		},

		

		// 总体完成百分比

		overallProgressPercent() {

			if (this.totalCharacterCount === 0) return 0;

			return Math.round((this.ownedCount / this.totalCharacterCount) * 100);

		},

		

		// 盒子完成度排行列表

		boxRankingList() {

			const list = [];

			this.filteredBoxes.forEach(box => {

				const total = this.getBoxTotalCardCount(box);

				const collected = this.getBoxOwnedCardCount(box);

				list.push({

					name: box.Box_id || box.box_id || '未知',

					type: this.getBoxTypeClass(box.Box_type),

					typeText: this.getBoxTypeText(box.Box_type),

					total: total,

					collected: collected,

					progressPercent: total > 0 ? Math.round((collected / total) * 100) : 0

				});

			});

			// 按完成度从高到低排序

			return list.sort((a, b) => b.progressPercent - a.progressPercent);

		}

	},

	onLoad() {

		console.log('我的收藏页面加载');



		// 动态设置导航栏

		uni.setNavigationBarTitle({

			title: '通行证展柜'

		});

		
		// 从扫码页跳转：获取要高亮的盒号
		const hlBoxId = uni.getStorageSync('highlightBoxId');
		if (hlBoxId) {
			this.highlightBoxId = hlBoxId;
			uni.removeStorageSync('highlightBoxId');
			// 延迟滚动到对应盒号
			setTimeout(() => {
				this.scrollToBox(hlBoxId);
			}, 500);
		}



		// 精一/精二显示偏好

		this.loadAvatarPref();

		// 数量步进控件的显示偏好
		this.loadQuantityStepper();

		// 初始化系统

		this.initAchievements();



		// 开始加载数据

		this.initData();

	},

	onShow() {

		this.loadTopCollapsed();
		this.loadTopRegionMode();

		this.loadThemeSetting();

		this.refreshBoxesHeight(120);

		setTimeout(() => this.refreshBoxesHeight(0), 700);
		// 页面显示重新加载用户数据		this.loadUserData();

	},

	onShareAppMessage() {

		return {

			title: this.posterShareTitle(),

			path: '/pages/card_share/card_share',

			imageUrl: ''

		}

	},

	onShareTimeline() {

		return {

			title: this.posterShareTitle(),

			imageUrl: ''

		}

	},

	methods: {
	/* ================= 分享展柜 ================= */
	openSharePoster() {
		this.leftDrawerOpen = false;
		// #ifdef MP-WEIXIN
		openMpShareShot(this.buildShotPayload());
		return;
		// #endif
		this.sharePosterOpen = true;
	},

	// 传给 Skyline 截图页的数据
	buildShotPayload() {
		const total = this.totalCharacters > 0 ? (' / ' + this.totalCharacters) : '';
		return {
			type: 'gallery',
			sub: '点亮 ' + this.ownedCount + total + ' · 想要 ' + this.wantedCount + ' · 出物 ' + this.tradeCount,
			// 结构化字段：画布顶部要突出「已点亮 / 总数」大数字 + 进度条，
			// 不能再从 sub 那串文字里抠数字
			owned: this.ownedCount,
			totalChars: this.totalCharacters || 0,
			wanted: this.wantedCount,
			trade: this.tradeCount,
			names: this.shareNameList,
			rest: this.shareNameRest,
			total: this.shareOwnedList.length,
			elite2Count: this.shareElite2Count,
			qrTitle: '微信扫码 · 记录你的每一张通行证',
			shareTitle: this.posterShareTitle(),
			sharePath: '/pages/card_share/card_share'
		};
	},

	/* ================= 每盒进度长图 =================
	 * 小程序走 share_shot 整屏页（和「分享卡片」同一套：保存 / 发送 / 分享小程序卡片），
	 * 这样两个分享入口的体验才一致；非小程序没有那个页面，仍用页内 canvas 生成。
	 */
	openBoxesPoster() {
		// #ifdef MP-WEIXIN
		openMpShareShot(this.buildBoxesPayload());
		return;
		// #endif
		this.generateShareImageFromOverview();
	},

		buildBoxesPayload() {
			const allBoxes = (this.getFilteredBoxesForOverview ? this.getFilteredBoxesForOverview() : (this.filteredBoxes || [])) || [];
			const isTrade = this.currentMode === 'trade';
			let sumOwned = 0;
			let sumTotal = 0;
			let opened = 0;
			allBoxes.forEach((box) => {
				const total = this.getBoxTotalCount(box);
				const owned = this.getBoxOwnedCount(box);
				sumTotal += total;
				sumOwned += owned;
				if (owned > 0) opened++;
			});

			// 点亮模式：按收集进度筛；出物模式：只保留有出/换/要标记的盒
			let src = allBoxes;
			if (isTrade) {
				src = allBoxes.filter((box) => this.boxTradeMarks(box).length > 0);
			} else if (this.onlyShowProgressBoxes) {
				src = allBoxes.filter((box) => this.getBoxOwnedCount(box) > 0);
			}

			const list = [];
			const seenMark = {}; // 出物模式下角色级标记去重（跨盒）
			src.forEach((box) => {
				const boxId = box.Box_id || box.box_id;
				const owned = this.getBoxOwnedCount(box);
				const total = this.getBoxTotalCount(box);
				const chars = this.getBoxCharacters(box) || [];
				const seen = {};
				const chips = [];
				let hidden = 0;
				let outN = 0;
				let swapN = 0;
				let needN = 0;

				chars.forEach((ch) => {
					if (!ch || !ch.name || seen[ch.name]) return;
					seen[ch.name] = 1;

					if (isTrade) {
						// 标记是角色级的（不区分盒），同一个干员的标记只挂在他出现的第一个盒里
						if (seenMark[ch.name]) return;
						const marks = this.getTradeMarks(ch.name);
						if (!marks.length) { hidden++; return; }
						seenMark[ch.name] = 1;
						for (let i = 0; i < marks.length; i++) {
							const m = marks[i];
							const priceTxt = (m.status === 'out' && m.price > 0) ? (' ¥' + m.price) : '';
							chips.push({ name: ch.name + ' ' + m.text + priceTxt, st: m.status });
							if (m.status === 'out') outN++;
							else if (m.status === 'swap') swapN++;
							else needN++;
						}
						return;
					}

					const e1 = this.isCharacterOwnedAtLevel ? this.isCharacterOwnedAtLevel(ch.name, 'elite1', boxId) : false;
					const e2 = this.isCharacterOwnedAtLevel ? this.isCharacterOwnedAtLevel(ch.name, 'elite2', boxId) : false;
					const pr = this.isCharacterOwnedAtLevel ? this.isCharacterOwnedAtLevel(ch.name, 'pair', boxId) : false;
					let st = 'none';
					if (pr || (e1 && e2)) st = 'pair';
					else if (e2) st = 'e2';
					else if (e1) st = 'e1';
					// 未点亮的只计数，画成末尾一个「+N」灰片
					if (st === 'none') { hidden++; return; }
					chips.push({ name: ch.name, st: st });
				});

				list.push({
					name: box.Box_name || box.Box_id || box.box_id || '?',
					owned: owned,
					total: total,
					pct: total > 0 ? Math.round(owned / total * 100) : 0,
					chips: chips,
					hidden: hidden,
					active: chips.length > 0,
					outN: outN,
					swapN: swapN,
					needN: needN
				});
			});

			// 出物模式下，去重后可能剩下空壳盒，直接不列
			const finalBoxes = isTrade ? list.filter((b) => b.active) : list;

			return {
				type: 'boxes',
				mode: isTrade ? 'trade' : 'show',
				ownedCount: this.ownedCount,
				opened: opened,
				boxCount: allBoxes.length,
				emptyCount: isTrade ? Math.max(0, allBoxes.length - finalBoxes.length) : Math.max(0, allBoxes.length - opened),
				sumOwned: sumOwned,
				sumTotal: sumTotal,
				tradeOut: this.tradeOutCount || 0,
				tradeSwap: this.tradeSwapCount || 0,
				tradeWant: this.tradeNeedCount || 0,
				boxes: finalBoxes,
				qrTitle: '微信扫码 · 记录你的每一张通行证',
				shareTitle: this.posterShareTitle(),
				sharePath: '/pages/card_share/card_share'
			};
		},

		// 一个盒里所有角色的出/换/要标记（按角色去重）
		boxTradeMarks(box) {
			const chars = this.getBoxCharacters(box) || [];
			const seen = {};
			const out = [];
			for (let i = 0; i < chars.length; i++) {
				const ch = chars[i];
				if (!ch || !ch.name || seen[ch.name]) continue;
				seen[ch.name] = 1;
				const marks = this.getTradeMarks(ch.name);
				for (let k = 0; k < marks.length; k++) {
					out.push({ name: ch.name, mark: marks[k] });
				}
			}
			return out;
		},


	closeSharePoster() {
		this.sharePosterOpen = false;
	},

	posterShareTitle() {
		if (this.totalCharacters > 0) {
			return '我的明日方舟通行证点亮了 ' + this.ownedCount + '/' + this.totalCharacters + ' 个，你收了多少？';
		}
		return '明日方舟通行证谷子查询工具 · 记录你的每一张通行证';
	},

	posterShareText() {
		const lines = ['【通行证展柜】'];
		if (this.totalCharacters > 0) lines.push('已点亮 ' + this.ownedCount + '/' + this.totalCharacters + '（' + this.completionRate + '%）');
		else lines.push('已点亮 ' + this.ownedCount);
		lines.push('想要 ' + this.wantedCount + ' · 出物 ' + this.tradeCount);
		lines.push('来「方舟通行证谷子查询工具」记录你的通行证');
		return lines.join('\n');
	},

	copyPosterText() {
		uni.setClipboardData({
			data: this.posterShareText(),
			success: () => { uni.showToast({ title: '分享文案已复制', icon: 'none' }); }
		});
	},

	// App 端不再截屏：canvas 重绘成图（utils/posterCanvas.js）
	// 整段只在 App 端存在 —— 小程序端没有 renderGalleryPoster，留着是死代码
	// #ifdef APP-PLUS
	async capturePoster() {
		this.capturing = true;
		try {
			const path = await renderGalleryPoster(this.buildShotPayload(), 'posterCanvas', this);
			this.capturing = false;
			return path;
		} catch (e) {
			this.capturing = false;
			throw e;
		}
	},
	// #endif

	async savePoster() {
		// #ifdef APP-PLUS
		try {
			const path = await this.capturePoster();
			uni.saveImageToPhotosAlbum({
				filePath: path,
				success: () => { uni.showToast({ title: '已保存到相册', icon: 'none' }); },
				fail: (err) => {
					const msg = (err && err.errMsg) || '';
					if (msg.indexOf('auth') > -1 || msg.indexOf('authorize') > -1) {
						uni.showModal({
							title: '需要相册权限',
							content: '请在设置里允许保存图片到相册，然后再试一次',
							confirmText: '去设置',
							success: (r) => { if (r.confirm && uni.openSetting) uni.openSetting({}); }
						});
						return;
					}
					if (msg.indexOf('cancel') === -1) uni.showToast({ title: '保存失败，请用系统截图', icon: 'none' });
				}
			});
		} catch (e) {
			this.logError(e, 'card_share.share.capture');
			uni.showToast({ title: '截图失败，请用系统截图', icon: 'none' });
		}
		// #endif
		// #ifndef APP-PLUS
		this.copyPosterText();
		// #endif
	},

	async sendPoster() {
		// #ifdef APP-PLUS
		try {
			const path = await this.capturePoster();
			uni.shareWithSystem({
				type: 2,
				imageUrl: path,
				fail: () => { uni.showToast({ title: '分享失败，可先保存图片', icon: 'none' }); }
			});
		} catch (e) {
			this.logError(e, 'card_share.share.capture');
			uni.showToast({ title: '截图失败，请用系统截图', icon: 'none' });
		}
		// #endif
		// #ifndef APP-PLUS
		this.copyPosterText();
		// #endif
	},

toggleLeftDrawer() {
		this.leftDrawerOpen = !this.leftDrawerOpen;
},

closeLeftDrawer() {
		this.leftDrawerOpen = false;
},

drawerAction(kind) {
		this.leftDrawerOpen = false;
		if (kind === 'overview') this.openSharePreview();
		else if (kind === 'want') this.toggleWantMode();
		else if (kind === 'scan') this.startScan();
},

loadTopCollapsed() {
		try { this.topCollapsed = uni.getStorageSync('cardShareTopCollapsed') === 'true'; } catch (e) { this.topCollapsed = false; }
},

toggleTopCollapsed() {
		this.topCollapsed = !this.topCollapsed;
		try { uni.setStorageSync('cardShareTopCollapsed', this.topCollapsed ? 'true' : 'false'); } catch (e) {}
		// 收起时顺带关掉搜索结果，避免下次展开残留
		if (this.topCollapsed) this.showSearchResults = false;
		this.refreshBoxesHeight(80);
},

openLeftDrawer() {
		this.leftDrawerOpen = true;
},

loadTopRegionMode() {
		try { this.topRegionInDrawer = uni.getStorageSync('cardShareTopRegionInDrawer') === 'true'; } catch (e) { this.topRegionInDrawer = false; }
},

setTopRegionInDrawer(v) {
		const next = !!v;
		this.topRegionInDrawer = next;
		try { uni.setStorageSync('cardShareTopRegionInDrawer', next ? 'true' : 'false'); } catch (e) {}
		if (next) {
			this.showSearchResults = false;
			this.leftDrawerOpen = true; // 收进抽屉后直接打开，功能位置一眼可见
		} else {
			this.leftDrawerOpen = false;
		}
		this.refreshBoxesHeight(60);
		uni.showToast({ title: next ? '已收进左侧抽屉' : '已放回页面顶部', icon: 'none' });
},

// 列表高度实测：顶区 + 底部操作栏都是固定的，折起来后列表自动补满
refreshBoxesHeight(delay) {
		if (this.boxHeightTimer) { clearTimeout(this.boxHeightTimer); this.boxHeightTimer = null; }
		const run = () => {
			this.$nextTick(() => {
				try {
					const winH = (uni.getSystemInfoSync() || {}).windowHeight || 0;
					if (!winH) return;
					const q = uni.createSelectorQuery().in(this);
					q.select('.top-region').boundingClientRect();
					q.select('.bottom-action-bar').boundingClientRect();
					q.exec((res) => {
						const region = res && res[0];
						const bar = res && res[1];
						if (!region || !region.height || typeof region.bottom !== 'number') return;
						const barH = (bar && bar.height) ? bar.height : 0;
						const avail = winH - region.bottom - barH - 14;
						this.boxesScrollHeight = Math.max(280, Math.floor(avail));
					});
				} catch (e) { /* 量不到就退回 CSS 高度 */ }
			});
		};
		if (delay) this.boxHeightTimer = setTimeout(run, delay);
		else run();
},

onFoldBarClick() {
		// 刚做过上下滑动，忽略随之而来的 click，避免连环折叠
		if (Date.now() - this.foldSwipeAt < 400) return;
		this.toggleTopCollapsed();
},

onFoldTouchStart(e) {
		const t = (e && e.touches && e.touches[0]) || null;
		this.foldTouchY = t ? t.clientY : 0;
},

onFoldTouchEnd(e) {
		const t = (e && e.changedTouches && e.changedTouches[0]) || null;
		const startY = this.foldTouchY;
		this.foldTouchY = 0;
		if (!t || !startY) return;
		const dy = t.clientY - startY;
		if (dy < -30 && !this.topCollapsed) { this.foldSwipeAt = Date.now(); this.toggleTopCollapsed(); }
		else if (dy > 30 && this.topCollapsed) { this.foldSwipeAt = Date.now(); this.toggleTopCollapsed(); }
},

onSearchResultClick(result) {
		if (!result || !result.character) return;
		this.showSearchResults = false;
		if (this.leftDrawerOpen) this.leftDrawerOpen = false;
		if (result.boxId) this.scrollToBox(result.boxId);
},

onSearchResultToggle(result) {
		if (!result || !result.character) return;
		this.toggleCharacterOwnership(result.character, result.boxId);
},

loadThemeSetting() {
			try {
				const t = uni.getStorageSync('themeMode');
				this.themeMode = (t && ['simple', 'ak', 'ark', 'jieyuan', 'liquid', 'liquidDark'].indexOf(t) > -1) ? t : 'simple';
			} catch (e) {
				this.themeMode = 'simple';
			}
		},
		logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},

		// 初始化系统

		initAchievements() {

			const saved = uni.getStorageSync('unlockedAchievements') || [];

			this.unlockedAchievements = saved;



			this.achievements = [

				{ id: 'first_light', name: '首次点亮', desc: '点亮第一个角色', icon: '🏆', condition: () => this.ownedCount >= 1 },

				{ id: 'first_elite2', name: '精锐之证', desc: '获得第一个精二角色', icon: '⚔️', condition: () => this.pairCount >= 1 },

				{ id: 'collector_10', name: '收藏新手', desc: '收集10个不同角色', icon: '💎', condition: () => this.ownedCount >= 10 },

				{ id: 'collector_50', name: '收藏达人', desc: '收集50个不同角色', icon: '⭐', condition: () => this.ownedCount >= 50 },

				{ id: 'collector_100', name: '收藏大师', desc: '收集100个不同角色', icon: '🔥', condition: () => this.ownedCount >= 100 },

				{ id: 'pair_1', name: '成双成对', desc: '获得第一对角色', icon: '👫', condition: () => this.pairCount >= 1 },

				{ id: 'pair_10', name: '配对高手', desc: '拥有10对角色', icon: '🏅', condition: () => this.pairCount >= 10 },

				{ id: 'box_clear_1', name: '一盒制霸', desc: '完成任意一个盒子全部收集', icon: '📦', condition: () => this.checkBoxCompletion() >= 1 },

				{ id: 'box_clear_3', name: '三盒征服', desc: '完成3个盒子全部收集', icon: '🎯', condition: () => this.checkBoxCompletion() >= 3 },

				{ id: 'trade_5', name: '交易新手', desc: '标记5个角色出物', icon: '🔄', condition: () => this.tradeCount >= 5 },

				{ id: 'want_10', name: '愿望清单', desc: '标记10个想要的角色', icon: '💜', condition: () => this.wantedCount >= 10 },

				{ id: 'first_april', name: '界园访客', desc: '在4月1日使用过界园主题', icon: '🌸', condition: () => uni.getStorageSync('aprilFoolTriggered') === `${new Date().getFullYear()}-4-1` },

			];

		},



		// 检查盒子完成度

		checkBoxCompletion() {

			let completed = 0;

			for (const box of this.characterData) {

				if (!box) continue;

				const characters = this.getBoxCharacters(box);

				let boxTotal = 0, boxOwned = 0;

				characters.forEach(char => {

					if (char && char.name) {

						boxTotal++;

						if (this.isCharacterOwned(char.name)) boxOwned++;

					}

				});

				if (boxTotal > 0 && boxOwned >= boxTotal) completed++;

			}

			return completed;

		},



		// 检查并解锁新成就

		checkAchievements() {

			for (const achievement of this.achievements) {

				if (this.unlockedAchievements.includes(achievement.id)) continue;

				if (achievement.condition && achievement.condition()) {

					this.unlockAchievement(achievement);

				}

			}

		},



		// 解锁成就

		unlockAchievement(achievement) {

			this.unlockedAchievements.push(achievement.id);

			uni.setStorageSync('unlockedAchievements', this.unlockedAchievements);



			this.currentAchievement = achievement;

			this.showAchievementPopup = true;



			// 3.5秒后自姩关闭

			setTimeout(() => {

				this.showAchievementPopup = false;

			}, 3500);

		},



		// 查看成就列表

		viewAchievements() {

			// 显示成就列表弹窗

			this.showAchievementList = true;

		},



		// 初始化数据

		async initData() {

			console.log('开始初始化数据...');



			// 先加载用户数据（点亮状态等），避免渲染时 undefined

			this.loadUserData();



			// 检查本地存储

			await this.checkLocalStorage();



			// 加载角色数据

			await this.loadData();



			console.log('数据初始化完成');

		},



		// 检查本地存储

		async checkLocalStorage() {

			try {

				console.log('检查本地存储...');



				// 检查所有相关存储

				const storageKeys = [

					'arknightsData',

					'arknightsDataString',

					'favoriteCharacterNames',

					'enableGuessData',

					'guessData',

					'dataUrl',

					'dataVersion'

				];



				const storageData = {};

				storageKeys.forEach(key => {

					try {

						const value = uni.getStorageSync(key);

						storageData[key] = value;

						console.log(`${key}:`, value ? (Array.isArray(value) ? `数组[${value.length}]` : typeof value) : '空');

					} catch (e) {
						this.logError(e);

						console.error(`读取${key}失败:`, e);

						storageData[key] = null;

					}

				});



				this.localStorage = storageData;



				// 如果有数据，显示数据信息

				if (storageData.arknightsData && Array.isArray(storageData.arknightsData) && storageData.arknightsData.length > 0) {

					console.log('可用本地数据:', storageData.arknightsData.length, '个盒号');

					const firstBox = storageData.arknightsData[0];

					console.log('第一个盒号:', {

						Box_id: firstBox.Box_id,

						Box_type: firstBox.Box_type,

						character1: firstBox.character1,

						character1_type: typeof firstBox.character1

					});

				} else if (storageData.arknightsDataString) {

					console.log('可用字符串格式数据');

				} else {

					console.log('未发现本地数据');

					this.emptyText = '请先返回列表页面加载数据';

				}



			} catch (e) {
				this.logError(e);

				console.error('检查本地存储失败', e);

			}

		},



		// 加载角色数据 - 副本

		async loadData() {

			if (this.isLoading) return;



			this.isLoading = true;

			console.log('开始加载角色数据...');



			try {

				// 尝试多种方式加载数据

				let data = [];

				let source = '';



				// 调试：直接从 uni.getStorageSync 读取，不依赖 this.localStorage

				const directData = uni.getStorageSync('arknightsData');

				console.log('[调试] 直接读取 arknightsData:', directData ? (Array.isArray(directData) ? '数组[' + directData.length + ']' : typeof directData) : '空');

				const directString = uni.getStorageSync('arknightsDataString');

				console.log('[调试] 直接读取 arknightsDataString:', directString ? (typeof directString === 'string' ? '字符串[' + directString.length + ']' : typeof directString) : '空');



				// 方法1: 从arknightsData加载（优先使用直接读取的值）

				const arknightsData = directData || this.localStorage.arknightsData;

				if (arknightsData && Array.isArray(arknightsData) && arknightsData.length > 0) {

					data = [...arknightsData];

					source = 'arknightsData';

					console.log(`从${source}加载，数量 ${data.length}`);

				}

				// 方法2: 从字符串加载

				else if (directString || this.localStorage.arknightsDataString) {

					try {

						const raw = directString || this.localStorage.arknightsDataString;

						const parsedData = JSON.parse(typeof raw === 'string' ? raw : JSON.stringify(raw));

						if (Array.isArray(parsedData) && parsedData.length > 0) {

							data = parsedData;

							source = 'arknightsDataString';

							console.log(`从${source}加载，数量 ${data.length}`);



							// 保存回arknightsData

							uni.setStorageSync('arknightsData', parsedData);

							this.localStorage.arknightsData = parsedData;

						}

					} catch (parseError) {
						this.logError(parseError);

						console.error('解析字符串数据失败', parseError);

					}

				}

				// 方法3: 从网络加载

				else {

					console.log('本地无数据，尝试从网络加载...');

					await this.loadDataFromNetwork();

					this.isLoading = false;

					return;

				}



				// 加载预测数据

				if (this.localStorage.enableGuessData === 'true' &&

					this.localStorage.guessData &&

					Array.isArray(this.localStorage.guessData) &&

					this.localStorage.guessData.length > 0) {

					console.log('合并预测数据，数量', this.localStorage.guessData.length);

					data = mergeGuessData(data, this.localStorage.guessData).data;

				}



				if (data.length > 0) {

					console.log(`总数据量: ${data.length}`);



					// 验证和修复数据格式

					const validatedData = this.validateAndFixData(data);

					console.log(`验证后数据量: ${validatedData.length}`);



					this.characterData = validatedData;

					this.applyFilter();

					this.calculateStatistics();



					// 更新空状态文本

					if (validatedData.length === 0) {

						this.emptyText = '数据格式不正确，请重新加载';

					} else if (this.filteredBoxes.length === 0) {

						this.emptyText = '娌℃湁符合条′欢的角色';

					} else {

						this.emptyText = `已加载{validatedData.length}个盒号`;

					}



					// Windows 壳：展柜数据进入页面就自动加载，这条「加载成功」纯打扰，不弹（其它端保持原样，方便确认条数）
					if (!isShell()) {
						uni.showToast({

							title: `加载成功 (${validatedData.length}个盒号`,

							icon: 'success',

							duration: 1500

						});
					}

				} else {

					console.log('无数据可用');

					this.showNoDataMessage();

				}

			} catch (e) {
				this.logError(e);

				console.error('加载数据失败:', e);

				uni.showToast({

					title: '加载数据失败',

					icon: 'none'

				});

				this.emptyText = '加载数据失败，请重试';

			} finally {

				this.isLoading = false;

			}

		},



		// 从网络加载数据

		async loadDataFromNetwork() {

			try {

				uni.showLoading({ title: '加载中...' });



				// 使用list.vue中的URL

				const dataUrl = this.localStorage.dataUrl ||

					'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';



				console.log('从网络加载数据，URL:', dataUrl);



				const res = await new Promise((resolve, reject) => {

					uni.request({

						url: dataUrl,

						method: 'GET',

						timeout: 10000,

						success: resolve,

						fail: reject

					});

				});



				uni.hideLoading();



				if (res.statusCode === 200) {

					let data = res.data;



					// 处理字符串数据

					if (typeof data === 'string') {

						try {

							data = JSON.parse(data);

						} catch (e) {
							this.logError(e);

							console.error('JSON解析失败:', e);

							throw new Error('数据格式错误');

						}

					}



					if (Array.isArray(data) && data.length > 0) {

						// 保存到本地存储

							uni.setStorageSync('arknightsData', data);

						this.localStorage.arknightsData = data;



						this.characterData = this.validateAndFixData(data);

						this.applyFilter();

						this.calculateStatistics();



						// Windows 壳：展柜数据进入页面就自动加载，这条「加载成功」纯打扰，不弹（其它端保持原样，方便确认条数）
						if (!isShell()) {
							uni.showToast({

								title: `网络加载成功 (${data.length}个盒号`,

								icon: 'success',

							});
						}



						this.emptyText = `已加载个盒号`;

					} else {

						throw new Error('数据格式不正确');

					}

				} else {

					throw new Error(`服务器错误 ${res.statusCode}`);

				}

			} catch (error) {
				this.logError(error);

				uni.hideLoading();

				console.error('网络加载失败:', error);

				this.showNoDataMessage();

				this.emptyText = '网络加载失败';

			}

		},



		// 验证和修复数据格式

		validateAndFixData(data) {

			if (!Array.isArray(data)) {

				console.warn('数据不是数组，请转换为空数组');

				return [];

			}



			const validatedData = [];



			data.forEach((item, index) => {

				// 过滤无效数据

				if (!item || typeof item !== 'object') {

					console.warn(`索引${index}: 数据项为空或不是对象`);

					return;

				}



				// 确保Box_id

				if (!item.Box_id) {

					item.Box_id = `未知${index}`;

				}



				// 确保Box_type

				if (!item.Box_type) {

					item.Box_type = 'normal';

				}



				// 验证角色字段

				let hasCharacters = false;

				for (let i = 1; i <= 10; i++) {

					const charKey = `character${i}`;

					if (item[charKey]) {

						hasCharacters = true;

						break;

					}

				}



				if (hasCharacters) {

					validatedData.push(item);

				} else {

					console.warn(`盒子${item.Box_id}: 无角色数据`);

				}

			});



			console.log(`验证结果: ${validatedData.length}/${data.length} 个有效盒号`);

			return validatedData;

		},



		// 显示无数据消息

		showNoDataMessage() {

			this.emptyText = '请先返回列表页面加载数据';



			uni.showModal({

				title: '提示',

				content: '暂无数据，请先返回列表页面加载数据',

				showCancel: false,

				confirmText: '确定',

				success: (res) => {

					if (res.confirm) {

						// 可互选择跳转到列表页面

						// uni.navigateTo({

						// 	url: '/pages/list/list'

						// });

					}

				}

			});

		},



		// 强制加载数据

		forceLoadData() {

			this.emptyText = '正在强制加载数据...';

			this.loadDataFromNetwork();

		},



		// 加载用户数据

		loadUserData() {

			try {

				// 加载拥有的角色

				let owned = uni.getStorageSync('ownedCharacters') || {};



				// 数据格式迁移：将旧格式转换为新版格式

				if (owned && typeof owned === 'object' && !Array.isArray(owned)) {

					owned = this.migrateOwnedCharactersData(owned);

				} else {

					owned = {};

				}



				this.ownedCharacters = owned;



				// 加载想要的角色

				const wantedChars = uni.getStorageSync('wantedCharacters') || [];

				this.wantedCharacters = Array.isArray(wantedChars) ? wantedChars : [];



				// 加载出物的角色

				const tradeChars = uni.getStorageSync('tradeCharacters') || {};

				this.tradeCharacters = (tradeChars && typeof tradeChars === 'object' && !Array.isArray(tradeChars)) ? tradeChars : {};



				console.log('用户数据加载完成，已点亮角色数', Object.keys(owned).length);

				this.calculateStatistics();

			} catch (e) {
				this.logError(e);

				console.error('加载用户数据失败:', e);

				// 确保即使出错也不会导致 undefined

				if (!this.ownedCharacters || typeof this.ownedCharacters !== 'object') {

					this.ownedCharacters = {};

				}

				if (!Array.isArray(this.wantedCharacters)) {

					this.wantedCharacters = [];

				}

				if (!this.tradeCharacters || typeof this.tradeCharacters !== 'object') {

					this.tradeCharacters = {};

				}

			}

		},



		// 数据格式迁移：将旧格式转换为新版格式

		migrateOwnedCharactersData(oldData) {

			const newData = {};



			for (const charName in oldData) {

				const oldCharData = oldData[charName];

				if (!oldCharData || typeof oldCharData !== 'object') continue;



				// 检查是否已经是新格式（新格式: charData[boxId] 存在，且包含 elite1/elite2/pair）

				const keys = Object.keys(oldCharData);

				const hasBoxIdKey = keys.some(k => {

					const val = oldCharData[k];

					return val && typeof val === 'object' && (val.elite1 !== undefined || val.elite2 !== undefined || val.pair !== undefined);

				});



				if (hasBoxIdKey) {

					// 已经是新格式，直接保留

					newData[charName] = oldCharData;

					continue;

				}



				// 旧格式: {name: {boxId: '', elite1: {...}, elite2: {...}}}

				// 转为新格式: {name: {boxId: {elite1: {...}, elite2: {...}}}}

				const boxId = oldCharData.boxId || '';

				newData[charName] = { [boxId]: {

					elite1: oldCharData.elite1 ? { owned: oldCharData.elite1.owned || oldCharData.elite1 === true, count: oldCharData.elite1.count || 1, paired: oldCharData.elite1.paired || false } : { owned: false, count: 0, paired: false },

					elite2: oldCharData.elite2 ? { owned: oldCharData.elite2.owned || oldCharData.elite2 === true, count: oldCharData.elite2.count || 1, paired: oldCharData.elite2.paired || false } : { owned: false, count: 0, paired: false },

					pair: oldCharData.pair ? { owned: oldCharData.pair.owned || false, count: oldCharData.pair.count || 0 } : { owned: false, count: 0 },

					lastUpdated: oldCharData.lastUpdated || Date.now()

				}};

			}



			return newData;

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

				const favoriteCharacterNames = this.localStorage.favoriteCharacterNames || [];



				if (favoriteCharacterNames.length === 0) {

					uni.showToast({

						title: '暂无收藏的角色',

						icon: 'none'

					});

					return;

				}



				console.log('从收藏同步，收藏角色数：', favoriteCharacterNames.length);



				// 将收藏的角色标记为拥有（默认精一）

				favoriteCharacterNames.forEach(characterName => {

					if (!this.ownedCharacters[characterName]) {

						this.ownedCharacters = {

							...this.ownedCharacters,

							[characterName]: {

								boxId: this.findCharacterBox(characterName) || '',

								elite1: { owned: true, count: 1, paired: false },

								elite2: { owned: false, count: 0, paired: false },

								pair: { owned: false, count: 0 },

								lastUpdated: Date.now()

							}

						};

					} else {

						// 如果已存在，确保精一被选中

						const currentData = this.ownedCharacters[characterName];

						this.ownedCharacters = {

							...this.ownedCharacters,

							[characterName]: {

								...currentData,

								elite1: { ...currentData.elite1, owned: true, count: currentData.elite1?.count || 1 },

								lastUpdated: Date.now()

							}

						};

					}

				});



				this.saveUserData();

				this.calculateStatistics();

				uni.showToast({

					title: `已从收藏同步${favoriteCharacterNames.length}个角色`,

					icon: 'success'

				});

			} catch (e) {
				this.logError(e);

				console.error('启屾失败:', e);

				uni.showToast({

					title: '启屾失败',

					icon: 'none'

				});

			}

		},



		// 查找角色所在的盒子

		findCharacterBox(characterName) {

			if (!characterName) return '';



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



		// 获得盒子的所有角色- 关键词方法

		getBoxCharacters(box) {

			if (!box || typeof box !== 'object') {

				console.warn('getBoxCharacters: box为空或不是对象');

				return [];

			}



			const characters = [];



			// 尝试从character1到character12获得角色

			for (let i = 1; i <= 12; i++) {

				const charKey = `character${i}`;

				const charValue = box[charKey];



				// 如果角色间不存在或为空，跳过

				if (!charValue) continue;



				let characterName = '';

				let avatarUrl = '';

				let nolyELITE1 = false;

				let hotcharacter = false;

				let marketPrice = null;



				// 处理不同类型的数据格式

				if (typeof charValue === 'string') {

					// 如果是字符串，直接作为角色名

					characterName = charValue.trim();

					if (!characterName) continue; // 跳过空字符串

				} else if (charValue && typeof charValue === 'object') {

					// 如果是对象，提取信息

					characterName = charValue.name || '';

					avatarUrl = charValue.imageUrl || charValue.avatar || '';

					nolyELITE1 = charValue.nolyELITE1 === true || charValue.nolyELITE1 === 'true';

					hotcharacter = charValue.hotcharacter === true || charValue.hotcharacter === 'true';

					marketPrice = charValue.market_price || null;



					if (!characterName) {

						// 如果对象中没有name，尝试其他可能的字段

						characterName = charValue.characterName || charValue.CharacterName || '';

					}



					if (!characterName) continue; // 跳过没有角色名的数据

				} else {

					// 其他类型，跳过

					continue;

				}



				// 确保角色名不为空

				characterName = characterName.trim();

				if (!characterName) continue;



				// 添加到角色列表

				characters.push({

					name: characterName,

					avatar: avatarUrl || this.defaultAvatar,

					avatarElite1: avatarUrl || '',

					avatarElite2: '',

					nolyELITE1: nolyELITE1,

					hotcharacter: hotcharacter,

					market_price: marketPrice,

					boxId: box.Box_id || '',

					boxType: box.Box_type || 'normal'

				});

			}



			// 根据盒子类型设置nolyELITE1（音律联觉、特别通行认证、白名单凭证的角色没有精二）

			const boxType = box.Box_type || 'normal';

			const nolyELITE1BoxTypes = ['ambience', 'special', 'whitelist'];

			if (nolyELITE1BoxTypes.includes(boxType)) {

				characters.forEach(char => {

					char.nolyELITE1 = true;

				});

			}

			// 精二头像：仅当该角色确实有精二（不是「仅精一」、不是无精二盒型、且有精二市价）时才准备

			characters.forEach(char => {

				const mp = char.market_price;

				const hasPrice2 = !mp || mp.ELITE2 !== undefined;

				char.avatarElite2 = (char.nolyELITE1 || !hasPrice2)

					? ''

					: (elite2Url(char.avatarElite1) || '');

			});





			return characters;

		},



		// 计算盒子的总卡片数（考虑nolyELITE1）

		// 有nolyELITE1的角色=1张，无nolyELITE1的角色=2张（精一+精二）

		getBoxTotalCardCount(box) {

			const characters = this.getBoxCharacters(box);

			let total = 0;

			characters.forEach(char => {

				if (char.nolyELITE1) {

					total += 1; // 只能精一

				} else {

					total += 2; // 精一+精二

				}

			});

			return total;

		},



		// 计算盒子已收集的卡片数

		getBoxOwnedCardCount(box) {

			const boxId = box && box.Box_id;

			const characters = this.getBoxCharacters(box);

			let owned = 0;

			characters.forEach(char => {

				if (this.isCharacterOwnedAtLevel(char.name, 'elite1', boxId)) {

					owned += 1;

				}

				if (!char.nolyELITE1 && this.isCharacterOwnedAtLevel(char.name, 'elite2', boxId)) {

					owned += 1;

				}

			});

			return owned;

		},



		// 获得过滤后的角色（按精英化级别）

		getFilteredCharacters(box) {

			if (!box) {

				console.warn('getFilteredCharacters: box为空');

				return [];

			}



			const boxId = box.Box_id;

			const characters = this.getBoxCharacters(box);



			if (this.eliteLevel === 'all') {

				return characters;

			}



			return characters.filter(char => {

				if (!char) return false;



				if (this.eliteLevel === 'elite1') {

					// 显示仅精一的角色（nolyELITE1为true的）

					return char.nolyELITE1 === true || char.nolyELITE1 === 'true';

				} else if (this.eliteLevel === 'elite2') {

					// 显示可精二的角色（nolyELITE1为false的）

					return this.canHavePair(char);

				}



				return true;

			});

		},



		// 检查角色是否可以拥有精二

		canHavePair(character) {

			if (!character) return false;

			// nolyELITE1 === true 代表该角色只有精一，没有一对

			if (character.nolyELITE1 === true || character.nolyELITE1 === 'true') return false;

			// 检查盒子类型（多种可能的字段名）

			const boxType = character.boxType || character.Box_type || character.box_type || character.type || '';

			const noPairTypes = ['ambience', 'special', 'whitelist', 'Ambience', 'Special', 'Whitelist'];

			return !noPairTypes.includes(boxType);

		},



		// 获得角色显示启嶏紙用于分享回撅級

		getCharacterDisplayName(name) {

			if (!name) return '';

			// 返回完整启嶅字

			return name;

		},



		// 获得所有角色列表

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

			if (!this.characterData || !Array.isArray(this.characterData)) {

				console.warn('characterData为空或不是数组');

				this.filteredBoxes = [];

				this.displayBoxes = [];

				return;

			}



			let filtered = this.characterData.filter(item => item && typeof item === 'object');



			// 按盒子类型筛选（根据 boxFilterIndex）

			const filterType = this.boxFilterMap[this.boxFilterIndex];

			console.log('applyFilter - boxFilterIndex:', this.boxFilterIndex, 'filterType:', filterType);

			if (filterType) {

				filtered = filtered.filter(box => {

					const boxType = box.Box_type || 'normal';

					console.log('Box:', box.Box_id, 'Box_type:', boxType, 'match:', boxType === filterType);

					if (filterType === 'normal') {

						return !boxType || boxType === 'normal';

					} else {

						return boxType === filterType;

					}

				});

			}

			// filterType 为 null 时不过滤，显示全部



			this.filteredBoxes = filtered;

			this.currentPage = 1;

			this.loadDisplayBoxes();

			this.calculateStatistics();



			console.log(`筛选后盒子数 ${filtered.length}`);

		},



		// 加载显示盒子（分页）

		// 扫码查盒
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
		}
,

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

		// 检查是否需要弹隐私协议（页面加载时调用）
		checkPrivacyAgreement() {
			// #ifdef MP-WEIXIN
			if (typeof wx !== 'undefined' && wx.getPrivacySetting) {
				wx.getPrivacySetting({
					success: (res) => {
						if (res.needAuthorization) {
							this.privacyContractName = res.privacyContractName || '《隐私保护指引》';
							this.showPrivacyModal = true;
						}
					},
					fail: () => {}
				});
			}
			// #endif
		},

		// 弹出隐私协议（被动触发）
		showPrivacyDialog() {
			this.showPrivacyModal = true;
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
				this.boxFilterIndex = 0;
				this.applyFilter();
				this.displayBoxes = this.filteredBoxes;
				this.highlightBoxId = boxId;
				this.scrollTargetId = this.boxSectionDomId(boxId);
				uni.showToast({ title: '已定位盒号 ' + boxId, icon: 'none' });
			} else {
				this.showQuestionnaire(barcode);
			}
		},

		// 盒区块 DOM id（scroll-into-view 需要合法 id）
		boxSectionDomId(boxId) {
			// 将盒号转为合法ID：中文转拼音首字母+数字，或直接用索引
			const str = String(boxId == null ? '' : boxId);
			// 如果是纯数字格式（如"1.0"），直接使用
			if (/^[\d.]+$/.test(str)) {
				return 'boxsec-' + str.replace(/\./g, '_');
			}
			// 中文盒号：使用字符编码
			let hash = 0;
			for (let i = 0; i < str.length; i++) {
				hash = ((hash << 5) - hash) + str.charCodeAt(i);
				hash = hash & hash;
			}
			return 'boxsec-' + Math.abs(hash);
		},

		showQuestionnaire(barcode) {
			const url = 'https://wj.qq.com/s2/27523743/l0g2/';
			uni.showModal({
				title: '条码未收录',
				content: '条码 ' + barcode + ' 暂未录入数据库。\n欢迎填写问卷补充：\n' + url,
				confirmText: linkActionText(),
				cancelText: '关闭',
				success: (res) => {
					if (res.confirm) {
						openLink(url);
					}
				}
			});
		},

		loadDisplayBoxes() {

			if (!this.filteredBoxes || !Array.isArray(this.filteredBoxes)) {

				this.displayBoxes = [];

				this.hasMore = false;

				return;

			}



			const startIndex = 0;

			const endIndex = this.currentPage * this.pageSize;

			this.displayBoxes = this.filteredBoxes.slice(startIndex, endIndex);

			this.hasMore = endIndex < this.filteredBoxes.length;



			console.log(`显示盒子: ${this.displayBoxes.length}, 总盒号: ${this.filteredBoxes.length}`);

		},



		// 加载更多

		// loadMore(),

		// 页面滚动事件

		onPageScroll(e) {

			this.showBackToTop = e.detail.scrollTop > 800;

		},



		// 排行榜滚动事件

		onBoxRankingScroll(e) {

			// 可以在这里添加滚动到底部自动展开更多的逻辑

		},



		// 回到顶部

		scrollToTop() {

			this.scrollTop = 1;

			this.$nextTick(() => {

				this.scrollTop = 0;

			});

			// this.(() => {

			// 	this.scrollTop = 1;

			// });

		},

		// 滚动到指定盒号
		scrollToBox(boxId) {
			if (!boxId) return;
			console.log('[scrollToBox] 开始定位盒号:', boxId);
			
			// 重置筛选为全部，确保盒号可见
			this.boxFilterIndex = 0;
			this.applyFilter();
			
			console.log('[scrollToBox] 筛选后总盒数:', this.filteredBoxes.length);
			console.log('[scrollToBox] 前10个盒号:', this.filteredBoxes.slice(0, 10).map(b => b.Box_id));
			
			// 找到目标盒号在 filteredBoxes 中的索引
			const targetIndex = this.filteredBoxes.findIndex(box => box && box.Box_id === boxId);
			console.log('[scrollToBox] 目标盒号索引:', targetIndex, '目标Box_id:', boxId);
			
			if (targetIndex === -1) {
				// 尝试模糊匹配（SP系列可能有不同格式）
				const fuzzyIndex = this.filteredBoxes.findIndex(box => box && box.Box_id && String(box.Box_id).includes(String(boxId)));
				console.log('[scrollToBox] 模糊匹配索引:', fuzzyIndex);
				if (fuzzyIndex !== -1) {
					const actualBoxId = this.filteredBoxes[fuzzyIndex].Box_id;
					console.log('[scrollToBox] 模糊匹配成功，实际Box_id:', actualBoxId);
					this.doScrollToIndex(fuzzyIndex, actualBoxId);
					return;
				}
				uni.showToast({ title: '未找到盒号 ' + boxId, icon: 'none' });
				return;
			}
			
			this.doScrollToIndex(targetIndex, boxId);
		},
		
		// 执行滚动到指定索引
		doScrollToIndex(targetIndex, boxId) {
			console.log('[scrollToBox] 直接加载全部数据并滚动');
			
			// 直接加载全部数据，确保目标元素存在
			this.currentPage = Math.ceil(this.filteredBoxes.length / this.pageSize);
			this.displayBoxes = [...this.filteredBoxes];
			this.hasMore = false;
			console.log('[scrollToBox] 已加载全部盒数:', this.displayBoxes.length);
			
			// 设置高亮
			this.highlightBoxId = boxId;
			
			// 等待DOM渲染后滚动
			this.$nextTick(() => {
				setTimeout(() => {
					const domId = this.boxSectionDomId(boxId);
					console.log('[scrollToBox] 查询元素:', domId);
					
					// 先尝试使用scroll-into-view
					this.scrollTargetId = '';
					this.$nextTick(() => {
						this.scrollTargetId = domId;
						console.log('[scrollToBox] 设置scrollTargetId:', domId);
						
						// 备用方案：用selectorQuery获取位置并设置scrollTop
						setTimeout(() => {
							uni.createSelectorQuery().in(this).select('#' + domId).boundingClientRect((rect) => {
								console.log('[scrollToBox] 元素位置:', rect);
								if (rect) {
									uni.createSelectorQuery().in(this).select('.boxes-scroll-view').scrollOffset((scrollOffset) => {
										uni.createSelectorQuery().in(this).select('.boxes-scroll-view').boundingClientRect((scrollRect) => {
											if (scrollRect && scrollOffset) {
												const scrollTop = scrollOffset.scrollTop + rect.top - scrollRect.top;
												console.log('[scrollToBox] 计算scrollTop:', scrollTop);
												this.scrollTop = scrollTop;
											}
										}).exec();
									}).exec();
								}
							}).exec();
						}, 300);
					});
				}, 300);
			});
			
			uni.showToast({ title: '已定位盒号 ' + boxId, icon: 'none' });
		},

		loadMore() {

			if (!this.hasMore || !this.filteredBoxes || this.filteredBoxes.length === 0) {

				return;

			}



			this.currentPage++;

			const startIndex = 0;

			const endIndex = this.currentPage * this.pageSize;

			this.displayBoxes = this.filteredBoxes.slice(startIndex, endIndex);

			this.hasMore = endIndex < this.filteredBoxes.length;

		},



		// 盒子筛选按钮区域		// 设置盒子筛选条件

		setBoxFilter(index) {

			this.boxFilterIndex = index;

			this.applyFilter();

		},



		onBoxFilterChange(e) {

			const index = parseInt(e.detail.value) || 0;

			this.boxFilterIndex = index;

			this.applyFilter();

		},



		// 切换模式

		changeMode(mode) {

			this.currentMode = mode;

			if (mode === 'trade') {

				uni.showToast({

					title: '出物模式：点击角色标记为可出物，长按弹出操作菜单',

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

					title: '长按角色弹出操作菜单',

					icon: 'none',

					duration: 2000

				});

			}

		},



		// 设置精英化级别选择

		setEliteLevel(level) {

			// 切换精英等级，如果是当前等级则切回全部

			if (this.eliteLevel === level) {

				this.eliteLevel = 'all';

			} else {

				this.eliteLevel = level;

			}

		},



		// 设置盒子类型筛选

		setBoxFilter(index) {

			this.boxFilterIndex = index;

			this.applyFilter();

		},



		// 角色头像错误处理

		onAvatarError(character, box) {

			// 记下失败的地址，displayAvatar 会自动回退（精二 → 精一 → 默认图）

			const url = character ? (this.displayAvatar(character, box) || '') : '';

			if (url) this.avatarFailed[url] = true;

		},



		// ===== 精二头像 =====

		// 一个格子只显示一张图：只点亮精一 → 精一图；只点亮精二 → 精二图；

		// 点亮一对（或精一精二都在）→ 用头像左下角的小胶囊自行切换。

		avatarPrefKey(name, boxId) {

			return (name || '') + '@' + (boxId || '');

		},



		loadAvatarPref() {

			try {

				const raw = uni.getStorageSync('card_avatar_pref');

				if (raw && typeof raw === 'object') this.avatarPref = raw;

			} catch (e) {}

		},



		loadQuantityStepper() {
			try {
				const v = uni.getStorageSync('card_show_qty');
				this.showQuantityStepper = v === true || v === 1 || v === '1';
			} catch (e) {}
		},

		toggleQuantityStepper() {
			this.showQuantityStepper = !this.showQuantityStepper;
			try { uni.setStorageSync('card_show_qty', this.showQuantityStepper); } catch (e) {}
			if (this.showQuantityStepper) {
				uni.showToast({ title: '对 = 一对 · 一 = 单精一 · 二 = 单精二', icon: 'none', duration: 2600 });
			}
		},

		// 这个角色在某个盒里是否已经有任何件数（有才显示步进器）
		charHasAnyCount(characterName, boxId) {
			const e1 = this.getCharacterEliteCount(characterName, 'elite1', boxId);
			const e2 = this.getCharacterEliteCount(characterName, 'elite2', boxId);
			return e1 + e2 > 0;
		},

		saveAvatarPref() {

			try { uni.setStorageSync('card_avatar_pref', this.avatarPref); } catch (e) {}

		},



		canSwitchAvatar(character, box) {

			if (!character || !character.avatarElite2) return false;

			const name = character.name;

			const boxId = box && box.Box_id;

			const pair = this.getCharacterPairCount(name, boxId) > 0;

			return pair || (this.isCharacterOwnedAtLevel(name, 'elite1', boxId) &&

				this.isCharacterOwnedAtLevel(name, 'elite2', boxId));

		},



		avatarLevelOf(character, box) {

			if (!character || !character.avatarElite2) return 1;

			const pref = this.avatarPref[this.avatarPrefKey(character.name, box && box.Box_id)];

			return (pref === 2 || pref === '2') ? 2 : 1;

		},



		toggleAvatarLevel(character, box) {

			if (!this.canSwitchAvatar(character, box)) return;

			const key = this.avatarPrefKey(character.name, box && box.Box_id);

			this.avatarPref[key] = this.avatarLevelOf(character, box) === 2 ? 1 : 2;

			this.saveAvatarPref();

		},



		displayAvatar(character, box) {

			if (!character) return this.defaultAvatar;

			const e1 = character.avatarElite1 || character.avatar || '';

			const e2 = character.avatarElite2 || '';

			if (!e2) return e1 || this.defaultAvatar;



			const name = character.name;

			const boxId = box && box.Box_id;

			const owned1 = this.isCharacterOwnedAtLevel(name, 'elite1', boxId);

			const owned2 = this.isCharacterOwnedAtLevel(name, 'elite2', boxId);

			const pair = this.getCharacterPairCount(name, boxId) > 0;



			let url = e1;

			if (owned2 && !owned1 && !pair) {

				url = e2; // 只点亮精二

			} else if (pair || (owned1 && owned2)) {

				url = this.avatarLevelOf(character, box) === 2 ? e2 : e1; // 一对 → 可切换

			}



			if (url && this.avatarFailed[url]) {

				return this.avatarFailed[e1] ? this.defaultAvatar : (e1 || this.defaultAvatar);

			}

			return url || this.defaultAvatar;

		},



		// 检查角色是否拥有（任意级别）

		isCharacterOwned(characterName, boxId) {
			if (!characterName) return false;
			const owned = this.ownedCharacters;
			if (!owned || typeof owned !== 'object') return false;
			const charBoxData = owned[characterName];
			if (!charBoxData) return false;
			// 按件数判断（一对 = 精一 + 精二 各一件），不用旧的 pair.owned 标记，
			// 免得取消精一后残留的 pair.owned 让角色看起来还拥有着
			if (boxId) {
				return this.getCharacterEliteCount(characterName, 'elite1', boxId) > 0 ||
					this.getCharacterEliteCount(characterName, 'elite2', boxId) > 0;
			}
			return Object.keys(charBoxData).some((bid) =>
				this.getCharacterEliteCount(characterName, 'elite1', bid) > 0 ||
				this.getCharacterEliteCount(characterName, 'elite2', bid) > 0
			);
		},



		// 检查角色是否在特定级别拥有

		isCharacterOwnedAtLevel(characterName, level, boxId) {

			if (!characterName) return false;

			const owned = this.ownedCharacters;

			if (!owned || typeof owned !== 'object') return false;

			const charBoxData = owned[characterName];

			if (!charBoxData) return false;

			if (boxId) {

				const data = charBoxData[boxId];

				if (!data) return false;

				if (level === 'elite1') return data.elite1?.owned || false;

				if (level === 'elite2') return data.elite2?.owned || false;

				if (level === 'pair') return this.getCharacterPairCount(characterName, boxId) > 0;

				return false;

			}

			if (level === 'pair') return this.getCharacterPairCount(characterName) > 0;

			return Object.values(charBoxData).some(d => {

				if (level === 'elite1') return d.elite1?.owned;

				if (level === 'elite2') return d.elite2?.owned;

				return false;

			});

		},



		// 获得角色特定级别的数量

		getCharacterEliteCount(characterName, level, boxId) {
			if (!characterName) return 0;
			if (level !== 'elite1' && level !== 'elite2') return 0;
			const charBoxData = this.ownedCharacters[characterName];
			if (!charBoxData) return 0;
			// 一件「一对」= 精一 1 件 + 精二 1 件。
			// 历史数据可能只写了 pair.count 没写 elite*.count，这里取 max 兜住。
			// 有 count 字段就以它为准（写操作会同步维护）；
			// 只有老数据缺 count 时才退回 pair.count
			const countOfBox = (d) => {
				if (!d) return 0;
				if (d[level] && typeof d[level].count === 'number') return d[level].count;
				return (d.pair && d.pair.count) || 0;
			};
			if (boxId) return countOfBox(charBoxData[boxId]);
			let total = 0;
			Object.values(charBoxData).forEach((d) => { total += countOfBox(d); });
			return total;
		},



		// 获得角色一对的数量（同时拥有精一和精二也算一对）

		// 一对的数量 = 精一件数与精二件数的较小值。
		// 「一对凯尔希 + 单精一凯尔希」(精一2 / 精二1) => 1 对 + 1 件散精一，
		// 不会再像旧逻辑那样只要两者都点亮就固定算 1 对、多出来的那件凭空消失。
		getCharacterPairCount(characterName, boxId) {
			if (!characterName) return 0;
			const charBoxData = this.ownedCharacters[characterName];
			if (!charBoxData) return 0;
			const pairOfBox = (d) => {
				if (!d) return 0;
				const p = (d.pair && d.pair.count) || 0;
				const e1 = d.elite1 && typeof d.elite1.count === 'number' ? d.elite1.count : p;
				const e2 = d.elite2 && typeof d.elite2.count === 'number' ? d.elite2.count : p;
				return Math.min(e1, e2);
			};
			if (boxId) return pairOfBox(charBoxData[boxId]);
			let total = 0;
			Object.values(charBoxData).forEach((d) => { total += pairOfBox(d); });
			return total;
		},

		// 散件数量：精一 = 精一件数 - 对数
		getCharacterLooseCount(characterName, level, boxId) {
			if (level !== 'elite1' && level !== 'elite2') return 0;
			const own = this.getCharacterEliteCount(characterName, level, boxId);
			const pair = this.getCharacterPairCount(characterName, boxId);
			return Math.max(0, own - pair);
		},

		// 角标上的散件摘要，例如「精一×1 · 精二×2」；没有散件返回空串
		looseSummary(characterName, boxId) {
			const l1 = this.getCharacterLooseCount(characterName, 'elite1', boxId);
			const l2 = this.getCharacterLooseCount(characterName, 'elite2', boxId);
			const parts = [];
			if (l1 > 0) parts.push('精一×' + l1);
			if (l2 > 0) parts.push('精二×' + l2);
			return parts.join(' · ');
		},



		// 检查角色是否标记为想要

		isCharacterWanted(characterName) {

			if (!characterName) return false;

			return this.wantedCharacters.includes(characterName);

		},



		// ===== 出物标记：出 / 换 / 要 × 精一 / 精二 / 一对 =====
		tradeLevelText(level) {
			if (level === 'elite1') return '精一';
			if (level === 'elite2') return '精二';
			if (level === 'pair') return '一对';
			return '';
		},

		tradeStatusText(status) {
			if (status === 'out') return '出';
			if (status === 'swap') return '换';
			if (status === 'want') return '要';
			return '';
		},

		// 读一条标记。老数据 {price, note} 没有 status，按「出」处理，
		// 否则升级后原有出物记录会全部消失。
		getTradeEntry(characterName, level) {
			const data = characterName ? this.tradeCharacters[characterName] : null;
			const raw = data ? data[level] : null;
			if (!raw) return { status: '', price: 0 };
			return {
				status: raw.status || 'out',
				price: Number(raw.price) || 0
			};
		},

		// 某等级是否算「已点亮」：一对除了显式标记，精一 + 精二都在也算
		// （与卡片上的「一对」角标保持同一口径，否则编辑层会出现「头像是橙色但一对是灰的」）
		isTradeLevelOwned(characterName, level, boxId) {
			if (!characterName) return false;
			if (level !== 'pair') return this.isCharacterOwnedAtLevel(characterName, level, boxId);
			if (this.isCharacterOwnedAtLevel(characterName, 'pair', boxId)) return true;
			return this.isCharacterOwnedAtLevel(characterName, 'elite1', boxId)
				&& this.isCharacterOwnedAtLevel(characterName, 'elite2', boxId);
		},

		getTradeStatus(characterName, level) {
			return this.getTradeEntry(characterName, level).status;
		},

		// 卡片上的小标签：[{ level, status, price, text:'出·精一', short:'出精一 ¥25' }]
		getTradeMarks(characterName) {
			if (!characterName) return [];
			const data = this.tradeCharacters[characterName];
			if (!data) return [];
			const marks = [];
			const levels = ['elite1', 'elite2', 'pair'];
			for (let i = 0; i < levels.length; i++) {
				const level = levels[i];
				const raw = data[level];
				if (!raw) continue;
				const status = raw.status || 'out';
				const price = Number(raw.price) || 0;
				const stText = this.tradeStatusText(status);
				const lvText = this.tradeLevelText(level);
				marks.push({
					level: level,
					status: status,
					price: price,
					text: stText + '·' + lvText,
					short: stText + lvText + (status === 'out' && price > 0 ? ' ¥' + price : '')
				});
			}
			return marks;
		},

		// 检查角色是否「出货 / 换货」中（只有「要」不算，头像不该亮成出物色）
		isCharacterForTrade(characterName) {
			return this.getTradeMarks(characterName).some((m) => m.status !== 'want');
		},

		// 兼容旧接口：取某等级价格（0 视作未定价）
		getTradePrice(characterName, level) {
			const price = this.getTradeEntry(characterName, level).price;
			return price > 0 ? price : '';
		},

		writeTradeMark(characterName, level, status, price) {
			if (!characterName || !level || !status) return;
			const prev = this.tradeCharacters[characterName] || {};
			const entry = {
				status: status,
				price: status === 'out' ? (Number(price) || 0) : 0,
				note: (prev[level] && prev[level].note) || ''
			};
			this.tradeCharacters = {
				...this.tradeCharacters,
				[characterName]: { ...prev, [level]: entry }
			};
		},

		removeTradeMark(characterName, level) {
			const prev = this.tradeCharacters[characterName];
			if (!prev) return;
			const next = { ...prev };
			delete next[level];
			const all = { ...this.tradeCharacters };
			if (Object.keys(next).length > 0) all[characterName] = next;
			else delete all[characterName];
			this.tradeCharacters = all;
		},

		// 兼容旧接口：直接设价格（价格只挂在「出」上）
		setTradePrice(characterName, level, price) {
			const entry = this.getTradeEntry(characterName, level);
			const num = parseFloat(price);
			this.writeTradeMark(characterName, level, entry.status || 'out', isNaN(num) ? 0 : num);
			this.saveUserData();
			this.calculateStatistics();
		},

		// ---------- 出物编辑层 ----------
		openTradeSheet(character, boxId) {
			if (!character || !character.name) return;
			const boxes = this.displayBoxes || [];
			let box = null;
			for (let i = 0; i < boxes.length; i++) {
				const b = boxes[i];
				if (b && (b.Box_id || b.box_id) === boxId) { box = b; break; }
			}
			this.tradeSheetName = character.name;
			this.tradeSheetBoxId = boxId || '';
			this.tradeSheetAvatar = this.displayAvatar(character, box);
			this.tradeSheetBoxLabel = boxId ? ('盒号 ' + boxId) : '';
			this.tradeSheetPairOk = this.canHavePair(character);
			this.tradeSheetCharacter = character;
			this.tradeSheetOpen = true;
		},

		closeTradeSheet() {
			this.tradeSheetOpen = false;
			this.saveUserData();
			this.calculateStatistics();
		},

		pickTradeStatus(level, status) {
			const name = this.tradeSheetName;
			if (!name) return;
			const entry = this.getTradeEntry(name, level);
			// 再点一次同状态 = 取消
			if (entry.status === status) {
				this.removeTradeMark(name, level);
				this.saveUserData();
				this.calculateStatistics();
				return;
			}
			// 「出 / 换」得先有这个等级，「要」不受点亮限制
			if (status !== 'want' && !this.isTradeLevelOwned(name, level, this.tradeSheetBoxId)) {
				uni.showToast({
					title: '先点亮「' + this.tradeLevelText(level) + '」才能标' + this.tradeStatusText(status),
					icon: 'none'
				});
				return;
			}
			this.writeTradeMark(name, level, status, entry.price);
			this.saveUserData();
			this.calculateStatistics();
		},

		toggleTradeRowOwned(row) {
			const name = this.tradeSheetName;
			if (!name || !row) return;
			const ch = this.tradeSheetCharacter || this.findCharacterByName(name);
			if (!ch) return;
			const boxId = this.tradeSheetBoxId;
			// 熄灭前先把该等级的出物标记清掉，避免「没点亮还在出」
			if (row.owned && this.tradeCharacters[name] && this.tradeCharacters[name][row.level]) {
				this.removeTradeMark(name, row.level);
			}
			if (row.level === 'elite1') this.toggleElite1(ch, boxId);
			else if (row.level === 'elite2') this.toggleElite2(ch, boxId);
			else this.togglePair(ch, boxId);
			this.saveUserData();
			this.calculateStatistics();
		},

		onTradeSheetPrice(level, e) {
			const name = this.tradeSheetName;
			if (!name) return;
			const raw = e && e.detail ? e.detail.value : '';
			const num = parseFloat(raw);
			const entry = this.getTradeEntry(name, level);
			this.writeTradeMark(name, level, entry.status || 'out', isNaN(num) ? 0 : num);
			this.saveUserData();
			this.calculateStatistics();
		},


		// 切换角色拥有状态

		toggleCharacterOwnership(character, boxId) {
		// 每日任务打点
		markDailyTask('star_try');

			if (!character || !character.name || !boxId) {

				console.warn('角色数据格式错误');

				return;

			}



			const characterName = character.name;



			if (this.wantMode) {

				// 想要模式下直接标记为想要

				this.toggleCharacterWant(characterName);

				return;

			// 出物模式：点角色不再是点亮，而是编辑「出 / 换 / 要」
			if (this.currentMode === 'trade') {
				this.openTradeSheet(character, boxId);
				return;
			}

			}



			// 处理多次点击逻辑

			this.handleCharacterClick(character, boxId);

		},



		// 生成角色专属卡（按盒分区排列）

		getCharacterKey(characterName, boxId) {

			return `${characterName}_${boxId}`;

		},



		// 处理角色点击（敮持多出绘娴嬶級

		handleCharacterClick(character, boxId) {

			const now = Date.now();

			const characterName = character.name;

			const characterKey = this.getCharacterKey(characterName, boxId);



			// 到底部弹出遮罩层

			if (!this.clickCounts[characterKey]) {

				this.clickCounts[characterKey] = 0;

			}



			// 判断是否在出物模式内

			const lastTime = this.lastClickTime[characterKey] || 0;

			if (now - lastTime > this.CLICK_INTERVAL) {

				// 读取数量

				this.clickCounts[characterKey] = 1;

			} else {

				// 长按点击

				this.clickCounts[characterKey]++;

			}



			this.lastClickTime[characterKey] = now;



			// 娓呴櫎之前的勫畾无跺櫒

			if (this.clickTimers[characterKey]) {

				clearTimeout(this.clickTimers[characterKey]);

			}



			// 设置新的定时器

			this.clickTimers[characterKey] = setTimeout(() => {

				this.executeClickAction(character, boxId);

				this.clickCounts[characterKey] = 0;

			}, this.CLICK_INTERVAL);

		},



		// 执行点击动作

		executeClickAction(character, boxId) {

			const characterKey = this.getCharacterKey(character.name, boxId);

			const clickCount = this.clickCounts[characterKey] || 1;



			if (this.currentMode === 'trade') {

				// 出物模式下，如果已拥有，则弹出出价输入框

				if (this.isCharacterOwned(character.name)) {

					return;

				} else {

					// 已拥有，点击精一

					this.toggleElite1(character, boxId);

				}

				return;

			}



			// 点击一次→精一，点击两次→精二，双击→一对

			// 注意：nolyELITE1角色（音律联觉/特别通行认证/白名单凭证）没有精二，双击不触发任何操作

			switch (clickCount) {

				case 1:

					this.toggleElite1(character, boxId);

					break;

				case 2:

					// 只有可精二角色才响应双击精二

					if (this.canHavePair(character)) {

						this.toggleElite2(character, boxId);

					}

					break;

				default:

					this.togglePair(character, boxId);

					break;

			}

		},



		// 切换精一状态

		toggleElite1(character, boxId) {

			if (!character || !character.name || !boxId) return;



			const characterName = character.name;

			const charBoxData = this.ownedCharacters[characterName] || {};

			const boxData = charBoxData[boxId] || {};

			const elite1Data = boxData.elite1 || { owned: false, count: 0, paired: false };



			if (elite1Data.owned) {

				const newCharBoxData = { ...charBoxData };

				// 精一归零 -> 一对必然不存在；剩下的散精二保留
				const nE2 = this.getCharacterEliteCount(characterName, 'elite2', boxId);

				newCharBoxData[boxId] = {

					...boxData,

					elite1: { owned: false, count: 0, paired: false },

					elite2: { owned: nE2 > 0, count: nE2, paired: false },

					pair: { owned: false, count: 0 }

				};

				if (nE2 <= 0) {

					delete newCharBoxData[boxId];

				}

				const newOwned = { ...this.ownedCharacters };

				if (Object.keys(newCharBoxData).length === 0) {

					delete newOwned[characterName];

				} else {

					newOwned[characterName] = newCharBoxData;

				}

				this.ownedCharacters = newOwned;

				uni.showToast({ title: `已取消${characterName}精一`, icon: 'success', duration: 1500 });

			} else {

				this.ownedCharacters = {

					...this.ownedCharacters,

					[characterName]: {

						...charBoxData,

						[boxId]: { ...boxData, elite1: { owned: true, count: 1, paired: false } }

					}

				};

				uni.showToast({ title: `已点亮${characterName}精一`, icon: 'success', duration: 1500 });

			}



			this.saveUserData();

			this.calculateStatistics();

			this.displayBoxes = [...this.displayBoxes];

			this.$forceUpdate();

		},



		// 切换一对状态





		// 切换一对状态

		togglePair(character, boxId) {
			if (!character || !character.name || !boxId) return;
			if (!this.canHavePair(character)) {
				uni.showToast({ title: `${character.name}可能无精二可点亮`, icon: 'none' });
				return;
			}
			const characterName = character.name;
			const pair = this.getCharacterPairCount(characterName, boxId);
			const loose1 = this.getCharacterLooseCount(characterName, 'elite1', boxId);
			const loose2 = this.getCharacterLooseCount(characterName, 'elite2', boxId);
			// 有对就减一对，没对就加一对；散件始终保留（一对凯尔希 + 单精一凯尔希 不会被清掉）
			this.adjustQuantity(characterName, 'pair', pair > 0 ? -1 : 1, boxId);
			uni.showToast({
				title: pair > 0 ? `已取消${characterName}一对` : `已点亮${characterName}一对`,
				icon: 'success',
				duration: 1500
			});
			if (pair > 0 && loose1 + loose2 > 0) {
				uni.showToast({ title: `保留散件 ${loose1} 精一 / ${loose2} 精二`, icon: 'none', duration: 1800 });
			}
			this.displayBoxes = [...this.displayBoxes];
			this.$forceUpdate();
		},

		toggleElite2(character, boxId) {

			if (!character || !character.name || !boxId) return;



			const characterName = character.name;

			const charBoxData = this.ownedCharacters[characterName] || {};

			const boxData = charBoxData[boxId] || {};



			const isElite2Owned = boxData.elite2?.owned;



			// 切换精二状态（不自动设置精一）

			this.ownedCharacters = {

				...this.ownedCharacters,

				[characterName]: {

					...charBoxData,

					[boxId]: {

						...boxData,

						elite2: { owned: !isElite2Owned, count: isElite2Owned ? 0 : 1, paired: false },

						pair: { owned: false, count: 0 }

					}

				}

			};



			this.saveUserData();

			this.calculateStatistics();

			this.displayBoxes = [...this.displayBoxes];

			this.$forceUpdate();

			uni.showToast({ title: (isElite2Owned ? '已取消' : '已点亮') + characterName + '精二', icon: 'success', duration: 1500 });

		},

		selectCharacterPair(characterName, boxId, characterData) {
			if (!characterName || !boxId) return;
			const character = characterData || this.findCharacterByName(characterName);
			const canPair = character ? this.canHavePair(character) : false;
			if (!canPair) {
				uni.showToast({ title: '该角色不能拥有一对', icon: 'none' });
				return;
			}
			const pair = this.getCharacterPairCount(characterName, boxId);
			// 加减一对都只动「对数」，散件保留
			this.adjustQuantity(characterName, 'pair', pair > 0 ? -1 : 1, boxId);
			uni.showToast({ title: (pair > 0 ? '已取消' : '已选择') + characterName + '一对', icon: 'success', duration: 1500 });
			this.displayBoxes = [...this.displayBoxes];
			this.$forceUpdate();
		},

		cancelCharacterElite1(characterName, boxId) {
			if (!characterName || !boxId) return;
			const charBoxData = this.ownedCharacters[characterName];
			if (!charBoxData) return;
			const boxData = charBoxData[boxId];
			if (!boxData || !boxData.elite1?.owned) {
				uni.showToast({ title: '该角色未拥有精一', icon: 'none' });
				return;
			}
			// 精一归零后一对必然不存在（一对需要精一 + 精二各一件）；
			// 剩下的散精二保留，不会被一起清掉
			const nE2 = this.getCharacterEliteCount(characterName, 'elite2', boxId);
			const newCharBoxData = { ...charBoxData };
			newCharBoxData[boxId] = {
				...boxData,
				elite1: { owned: false, count: 0, paired: false },
				elite2: { owned: nE2 > 0, count: nE2, paired: false },
				pair: { owned: false, count: 0 }
			};
			if (nE2 <= 0) delete newCharBoxData[boxId];
			const newOwned = { ...this.ownedCharacters };
			if (Object.keys(newCharBoxData).length === 0) {
				delete newOwned[characterName];
			} else {
				newOwned[characterName] = newCharBoxData;
			}
			this.ownedCharacters = newOwned;
			this.saveUserData();
			this.calculateStatistics();
			this.displayBoxes = [...this.displayBoxes];
			this.$forceUpdate();
			uni.showToast({ title: '已取消' + characterName + '精一', icon: 'success', duration: 1500 });
		},

		// 取消角色一对

		cancelCharacterPair(characterName) {

			if (!characterName) return;



			const currentData = this.ownedCharacters[characterName];

			if (!currentData || !currentData.pair?.owned) {

				uni.showToast({

					title: '该角色未拥有一对',

					icon: 'none'

				});

				return;

			}



			this.ownedCharacters = {

				...this.ownedCharacters,

				[characterName]: {

					...currentData,

						elite1: { owned: false, count: 0, paired: false },

						elite2: { owned: false, count: 0, paired: false },

						pair: { owned: false, count: 0 }

				}

			};

			uni.showToast({

				title: '已取消' + characterName + '一对',

				icon: 'success',

				duration: 1500

			});



			this.saveUserData();

			this.calculateStatistics();

		},





		// 取消角色全部拥有状态

		cancelCharacterAll(characterName) {

			if (!characterName) return;



			const currentData = this.ownedCharacters[characterName];

			if (!currentData) {

				uni.showToast({

					title: '该角色未拥有',

					icon: 'none'

				});

				return;

			}



			// 到犻櫎角色

			const newOwned = { ...this.ownedCharacters };

			delete newOwned[characterName];

			this.ownedCharacters = newOwned;



			// 同时取消出物状态

			const newTrade = { ...this.tradeCharacters };

			delete newTrade[characterName];

			this.tradeCharacters = newTrade;



			uni.showToast({

				title: `已取消${characterName}全部拥有状态`,

				icon: 'success',

				duration: 1500

			});



			this.saveUserData();

			this.calculateStatistics();

		},



		// 显示角色操作菜单

		showCharacterActionSheet(character, boxId) {

			if (!character || !character.name) return;



			this.currentActionCharacter = character;

			const characterName = character.name;

			const isOwned = this.isCharacterOwned(characterName);

			const hasElite1 = this.isCharacterOwnedAtLevel(characterName, 'elite1', boxId);

			const hasElite2 = this.isCharacterOwnedAtLevel(characterName, 'elite2', boxId);

			const hasPair = this.isCharacterOwnedAtLevel(characterName, 'pair', boxId);

			const canHavePair = this.canHavePair(character);

			const isWanted = this.isCharacterWanted(characterName);



			// 判断是否显示一对状态（同时拥有精一和精二）

			const hasBothElite1AndElite2 = hasElite1 && hasElite2;

			// 显示一对的情况：显式设置了一对，或者同时拥有精一和精二

			const showPairStatus = hasPair || hasBothElite1AndElite2;



			const itemList = [];

			const itemColors = [];



			// 拥有状态操作

			if (isOwned) {

				// 如果显示一对状态，隐藏单独的精一/精二选项

				if (showPairStatus) {

					// 只显示"取消一对"选项

					itemList.push('取消一对');

					itemColors.push('#FF6B35');

				} else {

					// 正常显示精一/精二选项

					if (hasElite1) {

						itemList.push('取消精一');

						itemColors.push('#E6A23C');

					} else if (canHavePair) {

						// 有精二选项时，显示"选择精一"

						itemList.push('选择精一');

						itemColors.push('#E6A23C');

					} else {

						// nolyELITE1角色（音律联觉/特别通行认证/白名单凭证），无精二，只显示"点亮干员"

						itemList.push('点亮干员');

						itemColors.push('#E6A23C');

					}



					if (canHavePair) {

						if (hasElite2) {

							itemList.push('取消精二');

							itemColors.push('#409EFF');

						} else {

							itemList.push('选择精二');

							itemColors.push('#409EFF');

						}

					}

				}



				itemList.push('修改数量');

				itemColors.push('#67C23A');



				itemList.push('取消全部拥有');

				itemColors.push('#F56C6C');

			} else {

				if (canHavePair) {

					// 有精二选项时，显示"选择精一"和"选择精二"等选项

					itemList.push('选择精一');

					itemColors.push('#E6A23C');

					itemList.push('选择精二');

					itemColors.push('#409EFF');

					itemList.push('选择一对');

					itemColors.push('#FF6B35');

				} else {

					// nolyELITE1角色（音律联觉/特别通行认证/白名单凭证），无精二，只显示"点亮干员"

					itemList.push('点亮干员');

					itemColors.push('#E6A23C');

				}

			}



			// 想要状态操作

			if (isWanted) {

				itemList.push('取消想要');

				itemColors.push('#FF6B6B');

			} else {

				itemList.push('标记为想要');

				itemColors.push('#FF6B6B');

			}



			uni.showActionSheet({

				itemList: itemList,

				itemColor: itemColors[0],

				success: (res) => {

					const index = res.tapIndex;

					const action = itemList[index];



					switch (action) {

						case '选择精一':

							this.toggleElite1(character, boxId);

							break;

						case '取消精一':

							this.toggleElite1(character, boxId);

							break;

						case '选择精二':

							this.toggleElite2(character, boxId);

							break;

						case '取消精二':

							this.toggleElite2(character, boxId);

							break;

						case '选择一对':

							this.togglePair(character, boxId);

							break;

						case '取消一对':

							this.togglePair(character, boxId);

							break;

						case '修改数量':

							this.showQuantityModifyModal(character, boxId);

							break;

						case '取消全部拥有':

							this.cancelCharacterAll(characterName);

							break;

						case '标记为想要':

							this.toggleCharacterWant(characterName);

							break;

						case '取消想要':

							this.toggleCharacterWant(characterName);

							break;

					}

				}

			});

		},



		// 显示数量修改模式

		showQuantityModifyModal(character, boxId) {
			if (!character || !character.name || !boxId) return;
			const n = character.name;
			const pair = this.getCharacterPairCount(n, boxId);
			const l1 = this.getCharacterLooseCount(n, 'elite1', boxId);
			const l2 = this.getCharacterLooseCount(n, 'elite2', boxId);
			if (pair + l1 + l2 <= 0) {
				uni.showToast({ title: '该角色还没有数量，先点亮再说', icon: 'none' });
				return;
			}
			// 一对 / 单精一 / 单精二 分开改，混搭不会被互相覆盖
			const options = ['一对 ×' + pair, '单精一 ×' + l1, '单精二 ×' + l2];
			uni.showActionSheet({
				itemList: options,
				success: (res) => {
					if (res.tapIndex === 0) this.showQuantityInput(n, 'pair', pair, boxId);
					else if (res.tapIndex === 1) this.showQuantityInput(n, 'elite1', l1, boxId);
					else this.showQuantityInput(n, 'elite2', l2, boxId);
				}
			});
		},



		// 显示数量输入框

		showQuantityInput(characterName, level, currentCount, boxId) {
			const label = level === 'elite1' ? '单精一' : level === 'elite2' ? '单精二' : '一对';
			uni.showModal({
				title: '修改' + characterName + '的' + label + '数量',
				content: '当前 ' + currentCount + '，请输入新数量',
				editable: true,
				placeholderText: '请输入新的数量',
				success: (res) => {
					if (!res.confirm) return;
					const newCount = parseInt(res.content, 10);
					if (isNaN(newCount) || newCount < 0) {
						uni.showToast({ title: '请输入 0 或正整数', icon: 'none' });
						return;
					}
					this.adjustQuantity(characterName, level, newCount - currentCount, boxId);
					this.displayBoxes = [...this.displayBoxes];
					this.$forceUpdate();
				}
			});
		},



		// 调整数量

		// 调整数量。level: pair / elite1 / elite2
		// pair 只改「对数」，散件保持不动；elite1/elite2 只改散件，且减不穿已有的对
		adjustQuantity(characterName, level, delta, boxId) {
			if (!characterName || !boxId || !delta) return;
			const charBoxData = this.ownedCharacters[characterName] || {};
			const boxData = charBoxData[boxId] || {};
			let e1 = this.getCharacterEliteCount(characterName, 'elite1', boxId);
			let e2 = this.getCharacterEliteCount(characterName, 'elite2', boxId);
			const pair0 = Math.min(e1, e2);
			const loose1 = Math.max(0, e1 - pair0);
			const loose2 = Math.max(0, e2 - pair0);
			if (level === 'pair') {
				const nextPair = Math.max(0, pair0 + delta);
				e1 = nextPair + loose1;
				e2 = nextPair + loose2;
			} else if (level === 'elite1') {
				e1 = Math.max(pair0, e1 + delta);
			} else if (level === 'elite2') {
				e2 = Math.max(pair0, e2 + delta);
			} else {
				return;
			}
			const nextPair = Math.min(e1, e2);
			const mk = (n) => ({ owned: n > 0, count: n, paired: n > 0 && nextPair > 0 });
			const newBoxData = { ...boxData, elite1: mk(e1), elite2: mk(e2), pair: { owned: nextPair > 0, count: nextPair } };
			const newCharBoxData = { ...charBoxData };
			if (e1 === 0 && e2 === 0) {
				delete newCharBoxData[boxId];
			} else {
				newCharBoxData[boxId] = newBoxData;
			}
			const newOwned = { ...this.ownedCharacters };
			if (Object.keys(newCharBoxData).length === 0) {
				delete newOwned[characterName];
			} else {
				newOwned[characterName] = newCharBoxData;
			}
			this.ownedCharacters = newOwned;
			this.saveUserData();
			this.calculateStatistics();
		},



		// 根据启嶇О查有壘角色

		findCharacterByName(characterName) {

			if (!characterName) return null;



			// 使用Set去重，只返回第一个找到的角色

			const seenNames = new Set();



			for (const box of this.characterData) {

				if (!box) continue;

				const characters = this.getBoxCharacters(box);

				for (const char of characters) {

					if (char && char.name === characterName && !seenNames.has(characterName)) {

						seenNames.add(characterName);

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

				uni.showToast({

					title: '已取消想要',

					icon: 'success'

				});

			} else {

				this.wantedCharacters.push(characterName);

				uni.showToast({

					title: '已标记为想要',

					icon: 'success'

				});

			}



			this.saveUserData();

			this.calculateStatistics();

		},



		// 处理搜索

		handleSearch() {

			if (!this.searchKeyword.trim()) {

				this.searchResults = [];

				this.showSearchResults = false;

				this.refreshBoxesHeight(80);

				return;

			}



			const keyword = this.searchKeyword.toLowerCase();

			const results = [];



			// 遍历所有盒子和角色进行搜索

			for (const box of this.characterData) {

				if (!box) continue;



				const characters = this.getBoxCharacters(box);

				for (const character of characters) {

					if (!character) continue;



					// 区归厤目掑彿

					if (box.Box_id && box.Box_id.toString().includes(keyword)) {

						results.push({

							type: 'box',

							boxId: box.Box_id,

							character: character,

							matchField: '目掑彿'

						});

					}



					// 区归厤角色名

					if (character.name && character.name.toLowerCase().includes(keyword)) {

						results.push({

							type: 'character',

							boxId: box.Box_id,

							character: character,

							matchField: '角色名'

						});

					}

				}

			}



			// 去婚噸处理

			this.searchResults = this.deduplicateSearchResults(results);

			this.showSearchResults = true;

			this.refreshBoxesHeight(80);

		},



		// 去婚噸搜索结果灉

		deduplicateSearchResults(results) {

			const seen = new Set();

			const unique = [];



			for (const result of results) {

				const key = `${result.character?.name}_${result.boxId}`;

				if (!seen.has(key)) {

					seen.add(key);

					unique.push(result);

				}

			}



			return unique;

		},



		// 清空┖搜索

		clearSearch() {

			this.searchKeyword = '';

			this.searchResults = [];

			this.showSearchResults = false;

			this.refreshBoxesHeight(80);

		},



		// 获得已点亮殑角色到楄〃

		getOwnedList() {

			const list = [];

			// 遍历每个角色

			for (const [name, charBoxData] of Object.entries(this.ownedCharacters)) {

				if (!charBoxData) continue;

				// 遍历该角色在所有盒中的状态

				let totalElite1 = 0, totalElite2 = 0, totalPair = 0;

				for (const boxId of Object.keys(charBoxData)) {

					// 一对 = 精一 + 精二各一件，剩下的才是散件
					const n1 = this.getCharacterEliteCount(name, 'elite1', boxId);

					const n2 = this.getCharacterEliteCount(name, 'elite2', boxId);

					const p = Math.min(n1, n2);

					totalPair += p;

					totalElite1 += Math.max(0, n1 - p);

					totalElite2 += Math.max(0, n2 - p);

				}

				if (totalElite1 > 0 || totalElite2 > 0 || totalPair > 0) {

					const character = this.findCharacterByName(name);

					list.push({

						name,

						avatar: character?.avatar || this.defaultAvatar,

						elite1Count: totalElite1,

						elite2Count: totalElite2,

						pairCount: totalPair,

					});

				}

			}

			return list.sort((a, b) => a.name.localeCompare(b.name));

		},



		// 获得概览过滤后的盒子列表（按Box_id排序）

		getFilteredBoxesForOverview() {

			if (!this.characterData || !Array.isArray(this.characterData)) {

				return [];

			}



			const filterType = this.boxFilterMap[this.boxFilterIndex];

			let result = [...this.characterData]; // 复制数组，避免修改原数据

			if (filterType) {

				result = result.filter(box => {

					if (!box) return false;

					const boxType = box.Box_type || 'normal';

					if (filterType === 'normal') {

						return !boxType || boxType === 'normal';

					} else {

						return boxType === filterType;

					}

				});

			}

			// 按Box_id排序（数字ID在前，字符串ID在后）

			return result.sort((a, b) => {

				const idA = a.Box_id;

				const idB = b.Box_id;

				const numA = parseFloat(idA);

				const numB = parseFloat(idB);

				const isNumA = !isNaN(numA) && String(numA) === String(idA);

				const isNumB = !isNaN(numB) && String(numB) === String(idB);

				// 数字ID排在前面，字符串ID排在后面

				if (isNumA && !isNumB) return -1;

				if (!isNumA && isNumB) return 1;

				// 都是数字，按数字排序

				if (isNumA && isNumB) return numA - numB;

				// 都是字符串，按字母排序

				return String(idA).localeCompare(String(idB));

			});

		},



		// 获得出物的角色列表（出 / 换 / 要 全部列出）
		getTradeList() {
			const list = [];
			const names = Object.keys(this.tradeCharacters || {});
			for (let i = 0; i < names.length; i++) {
				const name = names[i];
				const marks = this.getTradeMarks(name);
				if (!marks.length) continue;
				const character = this.findCharacterByName(name);
				list.push({
					name: name,
					avatar: (character && character.avatar) || this.defaultAvatar,
					marks: marks
				});
			}
			return list.sort((a, b) => a.name.localeCompare(b.name));
		},


		// 切换全选

		toggleSelectAll() {

			// 检查是否至少有2个角色

			if (this.ownedCount < 2) {

				uni.showToast({

					title: '需要至少点亮一个角色才能取消全选',

					icon: 'none'

				});

				return;

			}



			// 取消全选 - 清空所有拥有状态

			this.ownedCharacters = {};

			// 同时清空出物状态

			this.tradeCharacters = {};

			uni.showToast({

				title: '宸插彇娑堝叏选',

				icon: 'success'

			});



			this.saveUserData();

			this.calculateStatistics();

		},



		// 检查盒子是否全部选中

		// 检查该盒所有角色是否都被点亮（该盒 character+boxId 都有记录）

		isBoxAllSelected(box) {

			if (!box) return false;

			const boxId = box.Box_id || box.box_id;

			if (!boxId) return false;

			const characters = this.getBoxCharacters(box);

			if (characters.length === 0) return false;



			const seenNames = new Set();

			for (const char of characters) {

				if (char && char.name && !seenNames.has(char.name)) {

				seenNames.add(char.name);

				const charBoxData = this.ownedCharacters[char.name];

				if (!charBoxData || !charBoxData[boxId] ||

					(!charBoxData[boxId].elite1?.owned &&

						!charBoxData[boxId].elite2?.owned &&

						!charBoxData[boxId].pair?.owned)) {

					return false;

				}

			}

			}

			return seenNames.size > 0;

		},



		// 切换盒子全选

		// 切换盒子全选（只操作该盒的角色）

		toggleSelectBox(boxId) {

			if (!boxId) return;



			const box = this.characterData.find(b => b && b.Box_id === boxId);

			if (!box) return;



			const characters = this.getBoxCharacters(box);

			if (characters.length === 0) {

				uni.showToast({ title: '该盒号无角色', icon: 'none' });

				return;

			}



			const isAllSelected = this.isBoxAllSelected(box);



			if (isAllSelected) {

				const newOwned = JSON.parse(JSON.stringify(this.ownedCharacters));

				const seenNames = new Set();

				characters.forEach(char => {

					if (char && char.name && !seenNames.has(char.name)) {

						seenNames.add(char.name);

						if (newOwned[char.name] && newOwned[char.name][boxId]) {

							delete newOwned[char.name][boxId];

							if (Object.keys(newOwned[char.name]).length === 0) {

								delete newOwned[char.name];

							}

						}

					}

				});

				this.ownedCharacters = newOwned;

				uni.showToast({ title: '已取消全选盒子', icon: 'success' });

			} else {

				const newOwned = JSON.parse(JSON.stringify(this.ownedCharacters));

				const seenNames = new Set();

				characters.forEach(char => {

					if (char && char.name && !seenNames.has(char.name)) {

						seenNames.add(char.name);

						const charBoxData = newOwned[char.name] || {};

						const boxData = charBoxData[boxId] || {};

						if (!boxData.elite1?.owned) {

							newOwned[char.name] = {

								...charBoxData,

								[boxId]: { ...boxData, elite1: { owned: true, count: 1, paired: false } }

							};

						}

					}

				});

				this.ownedCharacters = newOwned;

				uni.showToast({ title: `已全选盒子${characters.length}个角色`, icon: 'success' });

			}



			this.saveUserData();

			this.calculateStatistics();

			this.displayBoxes = [...this.displayBoxes];

			this.$forceUpdate();

		},



		// 全选精一（只点亮精一，不点亮精二）

			selectAllElite1() {

				const newOwned = JSON.parse(JSON.stringify(this.ownedCharacters));

				let count = 0;



				for (const box of this.characterData) {

					if (!box) continue;

					const boxId = box.Box_id || box.box_id;

					if (!boxId) continue;

					const characters = this.getBoxCharacters(box);

					for (const char of characters) {

						if (!char || !char.name) continue;

						const charBoxData = newOwned[char.name] || {};

						const boxData = charBoxData[boxId] || {};

						if (!boxData.elite1?.owned) {

							newOwned[char.name] = {

								...charBoxData,

								[boxId]: { ...boxData, elite1: { owned: true, count: 1, paired: false } }

							};

							count++;

						}

					}

				}



				this.ownedCharacters = newOwned;

				this.saveUserData();

				this.calculateStatistics();

				this.displayBoxes = [...this.displayBoxes];

				this.$forceUpdate();

				uni.showToast({ title: `已点亮 ${count} 个精一`, icon: 'success' });

			},



		// 全选精二（只点亮可精二角色的精二，不点亮精一）

		selectAllElite2() {

			const newOwned = JSON.parse(JSON.stringify(this.ownedCharacters));

			let count = 0;



			for (const box of this.characterData) {

				if (!box) continue;

				const boxId = box.Box_id || box.box_id;

				if (!boxId) continue;

				const characters = this.getBoxCharacters(box);

				for (const char of characters) {

					if (!char || !char.name || !this.canHavePair(char)) continue;

					const charBoxData = newOwned[char.name] || {};

					const boxData = charBoxData[boxId] || {};

					if (!boxData.elite2?.owned) {

						newOwned[char.name] = {

							...charBoxData,

							[boxId]: { ...boxData, elite2: { owned: true, count: 1, paired: false } }

						};

						count++;

					}

				}

			}



			this.ownedCharacters = newOwned;

			this.saveUserData();

			this.calculateStatistics();

			this.displayBoxes = [...this.displayBoxes];

			this.$forceUpdate();

			uni.showToast({ title: `已点亮 ${count} 个精二`, icon: 'success' });

		},



		// 全选一对

		selectAllPair() {

			const newOwned = JSON.parse(JSON.stringify(this.ownedCharacters));

			let count = 0;



			for (const box of this.characterData) {

				if (!box) continue;

				const boxId = box.Box_id || box.box_id;

				if (!boxId) continue;

				const characters = this.getBoxCharacters(box);

				for (const char of characters) {

					if (!char || !char.name || !this.canHavePair(char)) continue;

					const charBoxData = newOwned[char.name] || {};

					const boxData = charBoxData[boxId] || {};

					if (!boxData.pair?.owned) {

						newOwned[char.name] = {

							...charBoxData,

							[boxId]: {

								...boxData,

								elite1: { owned: true, count: 1, paired: true },

								elite2: { owned: true, count: 1, paired: true },

								pair: { owned: true, count: 1 }

							}

						};

						count++;

					}

				}

			}



			this.ownedCharacters = newOwned;

			this.saveUserData();

			this.calculateStatistics();

			this.displayBoxes = [...this.displayBoxes];

			this.$forceUpdate();

			uni.showToast({ title: '已全选可配对角色 (' + count + '个)', icon: 'success' });

		},



		// 清空所有

		clearAll() {

			uni.showModal({

				title: '确认清空',

				content: '确定要清空所有点亮状态吗',

				success: (res) => {

					if (res.confirm) {

						this.ownedCharacters = {};

						this.wantedCharacters = [];

						this.tradeCharacters = {};

						this.saveUserData();

						this.calculateStatistics();

						this.$forceUpdate();

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

				// 计算拥有角色数（去重）、一对、单精二、单精一

				const seenNames = new Set();

				let pairCount = 0;
				let elite1Count = 0; // 散精一件数
				let elite2Count = 0; // 散精二件数

				for (const charName in this.ownedCharacters) {
					const charBoxData = this.ownedCharacters[charName];
					if (!charBoxData) continue;
					let hasAny = false;
					for (const boxId in charBoxData) {
						const e1 = this.getCharacterEliteCount(charName, 'elite1', boxId);
						const e2 = this.getCharacterEliteCount(charName, 'elite2', boxId);
						if (e1 > 0 || e2 > 0) hasAny = true;
						const p = Math.min(e1, e2);
						pairCount += p;
						elite1Count += Math.max(0, e1 - p);
						elite2Count += Math.max(0, e2 - p);
					}
					if (hasAny) seenNames.add(charName);
				}

				this.ownedCount = seenNames.size;
				// pairCount 由 computed 属性计算，不再手动设置
				this.singleElite1Count = elite1Count;
				this.singleElite2Count = elite2Count;



				// 计算已拥有的总件数（一对算 2 件，散件按实际数量累加）
				let ownedItemCount = 0;
				for (const charName in this.ownedCharacters) {
					const charBoxData = this.ownedCharacters[charName];
					if (!charBoxData) continue;
					for (const boxId in charBoxData) {
						ownedItemCount += this.getCharacterEliteCount(charName, 'elite1', boxId);
						ownedItemCount += this.getCharacterEliteCount(charName, 'elite2', boxId);
					}
				}
				this.ownedItems = ownedItemCount;



				// 计算想要的角色数

				this.wantedCount = this.wantedCharacters.length;



				// 计算出物的角色数（去重）

				// 出 / 换 / 要 分开计数（精一 / 精二 / 一对 各算一项），三个数加起来才是总项数
				let tradeOut = 0;
				let tradeSwap = 0;
				let tradeNeed = 0;
				let tradeChars = 0;
				const tradeNames = Object.keys(this.tradeCharacters || {});
				for (let i = 0; i < tradeNames.length; i++) {
					const raw = this.tradeCharacters[tradeNames[i]];
					if (!raw) continue;
					const levels = ['elite1', 'elite2', 'pair'];
					let hit = false;
					for (let k = 0; k < levels.length; k++) {
						const item = raw[levels[k]];
						if (!item) continue;
						hit = true;
						const st = item.status || 'out';
						if (st === 'out') tradeOut++;
						else if (st === 'swap') tradeSwap++;
						else if (st === 'want') tradeNeed++;
					}
					if (hit) tradeChars++;
				}
				this.tradeCount = tradeChars;
				this.tradeOutCount = tradeOut;
				this.tradeSwapCount = tradeSwap;
				this.tradeNeedCount = tradeNeed;



				// 计算总角色数（按数量：可精二×2，仅精一×1）

				const seenCharNames = new Set();

				let totalItemCount = 0;

				for (const box of this.characterData) {

					if (!box) continue;

					const characters = this.getBoxCharacters(box);

					characters.forEach(char => {

						if (char && char.name && !seenCharNames.has(char.name)) {

							seenCharNames.add(char.name);

							if (this.canHavePair(char)) {

								totalItemCount += 2;

							} else {

								totalItemCount += 1;

							}

						}

					});

				}

				this.totalCharacters = totalItemCount;



				// ===== 增强统计（概览页专用）=====

				// 完成度按角色数量计算（每个角色1单位，不论精一/精二）

				this.completionRate = totalItemCount > 0 ? Math.round(ownedItemCount / totalItemCount * 100) : 0;

				this.ownedItems = ownedItemCount;



				// 盒子进度（可精二×2，仅精一×1）

				const boxRankings = [];

				for (const box of this.characterData) {

					if (!box) continue;

					const boxTotal = this.getBoxTotalCount(box);

					const boxOwned = this.getBoxOwnedCount(box);

					if (boxTotal === 0) continue;

					const rate = Math.round(boxOwned / boxTotal * 100);

					boxRankings.push({

						name: box.box_name || box.Box_id || box.box_id || '未知',

						type: box.Box_type || box.box_type || box.type || 'normal',

						typeText: this.getBoxTypeText(box.Box_type || box.box_type || box.type),

						owned: boxOwned,

						total: boxTotal,

						rate

					});

				}

				boxRankings.sort((a, b) => b.rate - a.rate || b.owned - a.owned);

				this.boxRankings = boxRankings;

				this.completedBoxes = boxRankings.filter(b => b.rate === 100).length;

				this.totalBoxes = this.characterData.filter(b => b).length;



				// 精英分布占比

				const maxElite = Math.max(this.singleElite1Count, this.singleElite2Count, this.pairCount, 1);

				this.elite1Percent = Math.round(this.singleElite1Count / maxElite * 100);

				this.elite2Percent = Math.round(this.singleElite2Count / maxElite * 100);

				this.pairPercent = Math.round(this.pairCount / maxElite * 100);



				// 收集时间线

				let firstTime = Infinity, lastTime = 0;

				for (const charName in this.ownedCharacters) {

					const charBoxData = this.ownedCharacters[charName];

					if (!charBoxData) continue;

					for (const boxId in charBoxData) {

						const data = charBoxData[boxId];

						if (data && data.lastUpdated) {

							if (data.lastUpdated < firstTime) firstTime = data.lastUpdated;

							if (data.lastUpdated > lastTime) lastTime = data.lastUpdated;

						}

					}

				}

				this.firstCollectTime = firstTime < Infinity ? this.formatDate(firstTime) : '';

				this.lastCollectTime = lastTime > 0 ? this.formatDate(lastTime) : '';

				this.collectDays = (firstTime < Infinity && lastTime > 0) ? Math.ceil((lastTime - firstTime) / 86400000) + 1 : 0;



				console.log('统计: 拥有' + this.ownedCount + ', 一对' + this.pairCount + ', 单精一' + this.singleElite1Count + ', 单精二' + this.singleElite2Count + ', 完成度' + this.completionRate + '%');



				// 检查成就

				this.checkAchievements();

			},



		// 日期格式化区域

		formatDate(timestamp) {

			if (!timestamp) return '';

			const d = new Date(timestamp);

			if (isNaN(d.getTime())) return '';

			return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

		},



		// 获得盒子类型文本

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



		// 获得盒子类型文本描述

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



		// ============= 调试动熻兘 =============



		// 调试本地存储

		debugLocalStorage() {

			console.log('=== 开始尝试本地存储 ===');

			this.testResult = '';



			try {

				// 检查本地存储键

				const keys = [

					'arknightsData',

					'arknightsDataString',

					'favoriteCharacterNames',

					'enableGuessData',

					'guessData',

					'dataUrl',

					'dataVersion',

					'localUpdateTime',

					'cloudUpdateTime'

				];



				let debugInfo = '本地存储检查结果：\n\n';



				keys.forEach(key => {

					try {

						const value = uni.getStorageSync(key);

						let valueStr = '';



						if (value === null || value === undefined) {

							valueStr = '空';

						} else if (Array.isArray(value)) {

							valueStr = `数组[${value.length}]`;

							if (value.length > 0 && key === 'arknightsData') {

								const firstItem = value[0];

								valueStr += `\n  第一个盒号: ${firstItem.Box_id || '无營D'}`;

								valueStr += `\n  角色1: ${typeof firstItem.character1 === 'string' ? firstItem.character1 : (firstItem.character1?.name || '无')}`;

							}

						} else if (typeof value === 'object') {

							valueStr = `瀵硅薄 ${JSON.stringify(value).substring(0, 50)}...`;

						} else if (typeof value === 'string') {

							valueStr = `字符为"${value.substring(0, 50)}${value.length > 50 ? '...' : ''}"`;

						} else {

							valueStr = String(value);

						}



						debugInfo += `${key}: ${valueStr}\n`;

					} catch (e) {
						this.logError(e);

						debugInfo += `${key}: 读取失败\n`;

					}

				});



				console.log(debugInfo);

				this.testResult = debugInfo;



				// 显示调试模式

				this.showDebugModal = true;



			} catch (e) {
				this.logError(e);

				console.error('调试失败:', e);

				this.testResult = `调试失败: ${e.message}`;

			}

		},



		// 测试角色提愬彇

		testCharacterExtraction() {

			console.log('=== 测试角色提愬彇 ===');



			if (this.characterData.length === 0) {

				this.testResult = '无角色数据可测试';

				return;

			}



			let testResult = '角色提愬彇测试:\n\n';



			// 测试前3个盒号

			for (let i = 0; i < Math.min(3, this.characterData.length); i++) {

				const box = this.characterData[i];

				testResult += `盒子${i+1} (${box.Box_id}):\n`;



				const characters = this.getBoxCharacters(box);

				testResult += `  提取到：${characters.length} 个角色\n`;



				// 显示前几个角色

				for (let j = 0; j < Math.min(3, characters.length); j++) {

					const char = characters[j];

					testResult += `  ${j+1}. ${char.name} (${char.nolyELITE1 ? '仅精一' : '可精二'})\n`;

				}



				testResult += '\n';

			}



			console.log(testResult);

			this.testResult = testResult;

		},



		// 清除所有数据

		clearLocalStorage() {

			uni.showModal({

				title: '确娓呴櫎',

				content: '确定要清除所有本地数据吗',

				success: (res) => {

					if (res.confirm) {

						try {

							const keys = [

								'arknightsData',

								'arknightsDataString',

								'favoriteCharacterNames',

								'ownedCharacters',

								'wantedCharacters',

								'tradeCharacters',

								'enableGuessData',

								'guessData'

							];



							keys.forEach(key => {

								uni.removeStorageSync(key);

							});



							// 读嶇疆数据

							this.characterData = [];

							this.filteredBoxes = [];

							this.displayBoxes = [];

							this.ownedCharacters = {};

							this.wantedCharacters = [];

							this.tradeCharacters = {};

							this.localStorage = {};



							this.calculateStatistics();

							this.emptyText = '数据已清除，请重新加载';



							uni.showToast({

								title: '数据宸叉竻除',

								icon: 'success'

							});



							this.showDebugModal = false;

						} catch (e) {
							this.logError(e);

							console.error('娓呴櫎数据失败:', e);

							uni.showToast({

								title: '娓呴櫎失败',

								icon: 'none'

							});

						}

					}

				}

			});

		},



		// 重新加载数据

		reloadData() {

			this.showDebugModal = false;

			this.initData();

		},



		// 关闭调试模式

		closeDebugModal() {

			this.showDebugModal = false;

		},



		// ============= 分享动熻兘 =============



		// 找撳紑分享预览

		openSharePreview() {

			if (this.ownedCount === 0) {

				uni.showToast({

					title: '请先点亮一些角色',

					icon: 'none'

				});

				return;

			}



			// 找撳紑概览页面

			this.showSharePreviewModal = true;

		},



		// 关闭概览页面

		closeOverviewPage() {

			this.showSharePreviewModal = false;

		},



		

		// 上一张分享图
		prevShareImage() {
			if (this.currentShareIndex > 0) {
				this.currentShareIndex--;
			}
		},

		// 下一张分享图
		nextShareImage() {
			if (this.currentShareIndex < this.shareImagePaths.length - 1) {
				this.currentShareIndex++;
			}
		},

		// 分享当前图片（微信小程序使用预览图片+长按保存/分享）
		shareImage() {
			const path = this.shareImagePaths[this.currentShareIndex];
			uni.previewImage({
				urls: this.shareImagePaths,
				current: path,
				success: () => {
					console.log('预览图片成功，用户可长按保存或分享');
				},
				fail: (err) => {
					console.error('预览图片失败:', err);
					uni.showToast({ title: '预览失败', icon: 'none' });
				}
			});
		},

		// 保存所有分享图
		saveAllShareImages() {
			let savedCount = 0;
			const saveNext = (index) => {
				if (index >= this.shareImagePaths.length) {
					uni.showToast({ title: `已保存 ${savedCount} 张图片`, icon: 'success' });
					return;
				}
				uni.saveImageToPhotosAlbum({
					filePath: this.shareImagePaths[index],
					success: () => {
						savedCount++;
						saveNext(index + 1);
					},
					fail: (err) => {
						console.error('保存图片失败:', err);
						saveNext(index + 1);
					}
				});
			};
			saveNext(0);
		},

		// 保存当前分享图
		saveShareImage() {
			const path = this.shareImagePaths[this.currentShareIndex];
			uni.saveImageToPhotosAlbum({
				filePath: path,
				success: () => {
					uni.showToast({ title: '保存成功', icon: 'success' });
				},
				fail: (err) => {
					console.error('保存图片失败:', err);
					uni.showToast({ title: '保存失败', icon: 'none' });
				}
			});
		},

		// 关闭分享预览弹窗
		closeSharePreviewModal() {
			this.showSharePreviewModal = false;
			this.shareImagePaths = [];
			this.currentShareIndex = 0;
			this.shareImageReady = false;
		},


		// 获得盒子的点亮数量（按盒独立）
		getBoxOwnedCount(box) {
			if (!box) return 0;

			const boxId = box.Box_id || box.box_id;
			const characters = this.getBoxCharacters(box);
			let count = 0;

			for (const char of characters) {
				if (!char || !char.name) continue;

				// 按「精一槽 / 精二槽」计，每角色最多 2，混搭数量不会把进度顶超 100%
				const e1 = this.getCharacterEliteCount(char.name, 'elite1', boxId);
				const e2 = this.getCharacterEliteCount(char.name, 'elite2', boxId);
				if (e1 <= 0 && e2 <= 0) continue;
				if (e1 > 0) count += 1;
				if (e2 > 0 && this.canHavePair(char)) count += 1;
			}

			return count;
		},

		// 获得盒子的总数量
		getBoxTotalCount(box) {
			if (!box) return 0;

			const characters = this.getBoxCharacters(box);
			const seenNames = new Set();
			let count = 0;

			for (const char of characters) {
				if (char && char.name && !seenNames.has(char.name)) {
					seenNames.add(char.name);
					count += this.canHavePair(char) ? 2 : 1;
				}
			}

			return count;
		},

		// 获得盒子的进度百分比
		getBoxProgress(box) {
			const total = this.getBoxTotalCount(box);
			if (total === 0) return 0;

			const owned = this.getBoxOwnedCount(box);
			return Math.round(owned / total * 100);
		},

		// ===== 展柜概览长图（canvas 重绘，2026-10-26 重做）=====
		// 旧实现的三处硬伤：
		//   ① 视觉是 #409EFF 蓝色横幅 + 12px 细字的老后台风格，和现在的卡片完全两套语言；
		//   ② 口径打架：顶部「总收藏 2」是「位」（角色去重），每盒「2/14」是「件」
		//      （可配对角色算 2 件），两个数字相加对不上，用户一眼就觉得数据错了；
		//   ③ 每片固定 40 盒，盒子多、标签多时会超出画布元素（4000rpx = 2000px）被截断。
		// 现在：顶部主数字统一用「件」，与每盒数字可直接相加；分片按内容高度动态切。
		generateShareImageFromOverview() {
			const self = this;
			this.isGeneratingShareImage = true;
			this.shareImagePaths = [];
			this.shareImageReady = false;
			this.currentShareIndex = 0;

			// ---------- 排版常量（逻辑像素，导出统一 2 倍）----------
			const W = 375;
			const CANVAS_H = 2000;        // 画布元素 750rpx x 4000rpx，375pt 屏 = 375x2000
			const MAX_H = 1700;           // 单片上限，留出余量避免截断
			const CARD_X = 12;
			const CARD_W = W - CARD_X * 2;
			const CARD_TOP = 12;
			const PAD = 15;
			const CX = CARD_X + PAD;      // 内容左边界 27
			const CW = CARD_W - PAD * 2;  // 内容宽 321
			const HEAD_REL = 157;         // 卡片顶 -> 第一个盒块（含标题区）
			const FOOT_REL = 30;          // 最后一个盒块 -> 卡片底
			const BASE_H = CARD_TOP + HEAD_REL + FOOT_REL + 44; // 无盒子时的整图高

			const C = {
				page: '#f2f3f5',
				card: '#ffffff',
				border: '#ececef',
				ink: '#1c1c1e',
				sub: '#3a3a40',
				faint: '#9a9aa0',
				faint2: '#b0b0b8',
				barBg: '#eceef0',
				blue: '#2f7ed8',
				pairText: '#3f9c5a', pairBg: '#eaf7ee', pairBd: '#cfe9d8',
				e2Text: '#2f7ed8', e2Bg: '#eef4fd', e2Bd: '#d5e4f7',
				e1Text: '#b07a1a', e1Bg: '#fdf3e2', e1Bd: '#f0e0c0',
				noneText: '#9a9aa0', noneBg: '#f4f5f7', noneBd: '#e8e9ec'
			};

			function roundRect(ctx, x, y, w, h, r) {
				const rad = Math.min(r, w / 2, h / 2);
				ctx.beginPath();
				ctx.moveTo(x + rad, y);
				ctx.lineTo(x + w - rad, y);
				ctx.arc(x + w - rad, y + rad, rad, -Math.PI / 2, 0);
				ctx.lineTo(x + w, y + h - rad);
				ctx.arc(x + w - rad, y + h - rad, rad, 0, Math.PI / 2);
				ctx.lineTo(x + rad, y + h);
				ctx.arc(x + rad, y + h - rad, rad, Math.PI / 2, Math.PI);
				ctx.lineTo(x, y + rad);
				ctx.arc(x + rad, y + rad, rad, Math.PI, Math.PI * 1.5);
				ctx.closePath();
			}

			// 旧 CanvasContext 没有 measureText 的可靠实现，按字宽估算（中文 1em，英文 0.55em）
			function textW(str, size) {
				const s = String(str == null ? '' : str);
				let w = 0;
				for (let i = 0; i < s.length; i++) {
					w += s.charCodeAt(i) < 256 ? size * 0.56 : size;
				}
				return w;
			}

			function chipStyle(st) {
				if (st === 'pair') return { text: C.pairText, bg: C.pairBg, bd: C.pairBd };
				if (st === 'e2') return { text: C.e2Text, bg: C.e2Bg, bd: C.e2Bd };
				if (st === 'e1') return { text: C.e1Text, bg: C.e1Bg, bd: C.e1Bd };
				return { text: C.noneText, bg: C.noneBg, bd: C.noneBd };
			}

			// ---------- 单个盒子的数据 ----------
			function buildInfo(box) {
				const boxId = box && (box.Box_id || box.box_id);
				const name = (box && (box.Box_name || box.Box_id || box.box_id)) || '?';
				const owned = self.getBoxOwnedCount ? self.getBoxOwnedCount(box) : 0;
				const total = self.getBoxTotalCount ? self.getBoxTotalCount(box) : 0;
				const chars = (self.getBoxCharacters ? self.getBoxCharacters(box) : []) || [];
				const seen = {};
				const list = [];
				chars.forEach(function (ch) {
					if (!ch || !ch.name || seen[ch.name]) return;
					seen[ch.name] = 1;
					const e1 = self.isCharacterOwnedAtLevel ? self.isCharacterOwnedAtLevel(ch.name, 'elite1', boxId) : false;
					const e2 = self.isCharacterOwnedAtLevel ? self.isCharacterOwnedAtLevel(ch.name, 'elite2', boxId) : false;
					const pr = self.isCharacterOwnedAtLevel ? self.isCharacterOwnedAtLevel(ch.name, 'pair', boxId) : false;
					let st = 'none';
					if (pr || (e1 && e2)) st = 'pair';
					else if (e2) st = 'e2';
					else if (e1) st = 'e1';
					list.push({ name: ch.name, st: st });
				});
				return {
					box: box,
					name: name,
					owned: owned,
					total: total,
					pct: total > 0 ? Math.round(owned / total * 100) : 0,
					list: list
				};
			}

			// 只把已点亮的干员排进 chip 墙，未点亮的收成一个「+N」灰片，
			// 否则一盒 7 个角色全是灰片，图又长又没重点。
			function chipRows(info) {
				const items = [];
				info.list.forEach(function (c) {
					if (c.st !== 'none') items.push(c);
				});
				const hidden = info.list.length - items.length;
				if (hidden > 0) items.push({ name: '+' + hidden, st: 'none' });

				const rows = [];
				let row = [];
				let x = 0;
				items.forEach(function (c) {
					const w = textW(c.name, 10) + 12;
					if (row.length && x + w > CW) {
						rows.push(row);
						row = [];
						x = 0;
					}
					row.push({ name: c.name, st: c.st, w: w });
					x += w + 5;
				});
				if (row.length) rows.push(row);
				return rows;
			}

			// 与下面 drawBlock 的推进量严格一致
			function blockH(info) {
				if (!info.owned) return 24;
				return 44 + chipRows(info).length * 21;
			}

			function drawChips(ctx, rows, x0, y0) {
				let y = y0;
				rows.forEach(function (row) {
					let x = x0;
					row.forEach(function (c) {
						const cs = chipStyle(c.st);
						roundRect(ctx, x, y, c.w, 16, 4);
						ctx.fillStyle = cs.bg;
						ctx.fill();
						ctx.strokeStyle = cs.bd;
						ctx.lineWidth = 1;
						ctx.stroke();
						ctx.fillStyle = cs.text;
						ctx.font = '10px sans-serif';
						ctx.textAlign = 'center';
						ctx.fillText(c.name, x + c.w / 2, y + 11);
						x += c.w + 5;
					});
					y += 21;
				});
				ctx.textAlign = 'left';
				return y;
			}

			// ---------- 数据准备 ----------
			const allBoxes = (self.getFilteredBoxesForOverview ? self.getFilteredBoxesForOverview() : (self.filteredBoxes || [])) || [];
			const infosAll = [];
			for (let i = 0; i < allBoxes.length; i++) infosAll.push(buildInfo(allBoxes[i]));

			let sumOwned = 0;
			let sumTotal = 0;
			let opened = 0;
			infosAll.forEach(function (info) {
				sumOwned += info.owned;
				sumTotal += info.total;
				if (info.owned > 0) opened++;
			});

			let drawInfos = infosAll;
			if (self.onlyShowProgressBoxes) {
				drawInfos = infosAll.filter(function (info) { return info.owned > 0; });
			}

			// ---------- 动态分片 ----------
			const pages = [];
			let page = [];
			let used = BASE_H;
			drawInfos.forEach(function (info) {
				const h = blockH(info);
				if (page.length && used + h > MAX_H) {
					pages.push(page);
					page = [];
					used = BASE_H;
				}
				page.push(info);
				used += h;
			});
			pages.push(page);

			const pageCount = pages.length;
			console.log('[overview_shot] 盒子 ' + allBoxes.length + ' / 有进度 ' + opened + ' / 分片 ' + pageCount);

			// ---------- 单页绘制 ----------
			function drawPage(infos, pageIndex) {
				return new Promise(function (resolve, reject) {
					const contentH = infos.reduce(function (s, info) { return s + blockH(info); }, 0) + (infos.length ? 0 : 76);
					const cardH = HEAD_REL + contentH + FOOT_REL;
					const totalH = CARD_TOP + cardH + 52;

					const ctx = uni.createCanvasContext('shareCanvas', self);
					ctx.clearRect(0, 0, W, CANVAS_H);
					ctx.fillStyle = C.page;
					ctx.fillRect(0, 0, W, CANVAS_H);

					// 卡片底板
					roundRect(ctx, CARD_X, CARD_TOP, CARD_W, cardH, 12);
					ctx.fillStyle = C.card;
					ctx.fill();
					ctx.strokeStyle = C.border;
					ctx.lineWidth = 1;
					ctx.stroke();

					let y = CARD_TOP + PAD;
					ctx.textAlign = 'left';

					// eyebrow
					ctx.fillStyle = C.faint;
					ctx.font = '10px sans-serif';
					ctx.fillText('通行证展柜', CX, y + 8);
					y += 18;

					// 大数字：件数（可与每盒数字直接相加）
					const numStr = String(sumOwned);
					ctx.fillStyle = C.ink;
					ctx.font = 'bold 32px sans-serif';
					ctx.fillText(numStr, CX, y + 28);
					ctx.fillStyle = C.faint;
					ctx.font = '12px sans-serif';
					ctx.fillText('/ ' + sumTotal + ' 件', CX + textW(numStr, 32) + 7, y + 28);

					const pct = sumTotal > 0 ? Math.round(sumOwned / sumTotal * 100) : 0;
					ctx.textAlign = 'right';
					ctx.fillStyle = C.blue;
					ctx.font = 'bold 12px sans-serif';
					ctx.fillText(pct + '%', CX + CW, y + 28);
					ctx.textAlign = 'left';
					y += 42;

					// 总进度条
					roundRect(ctx, CX, y, CW, 5, 2.5);
					ctx.fillStyle = C.barBg;
					ctx.fill();
					if (sumTotal > 0) {
						const fw = Math.max(4, CW * Math.min(1, sumOwned / sumTotal));
						roundRect(ctx, CX, y, fw, 5, 2.5);
						ctx.fillStyle = C.blue;
						ctx.fill();
					}
					y += 15;

					// meta 行
					ctx.fillStyle = C.faint;
					ctx.font = '10px sans-serif';
					ctx.fillText(self.ownedCount + ' 位干员 · 有进度 ' + opened + '/' + allBoxes.length + ' 个盒', CX, y + 8);
					y += 24;

					// 分隔线
					ctx.strokeStyle = C.border;
					ctx.lineWidth = 1;
					ctx.beginPath();
					ctx.moveTo(CX, y);
					ctx.lineTo(CX + CW, y);
					ctx.stroke();
					y += 14;

					// 小节标题
					ctx.fillStyle = C.ink;
					ctx.font = 'bold 11px sans-serif';
					ctx.fillText('每盒进度', CX, y + 9);
					if (pageCount > 1) {
						ctx.fillStyle = C.faint2;
						ctx.font = '10px sans-serif';
						ctx.textAlign = 'right';
						ctx.fillText((pageIndex + 1) + ' / ' + pageCount, CX + CW, y + 9);
						ctx.textAlign = 'left';
					}
					y += 28;

					// 空展柜：至少给一句人话，别只留个「每盒进度」标题
					if (!infos.length) {
						roundRect(ctx, CX, y, CW, 64, 8);
						ctx.fillStyle = '#f7f7f9';
						ctx.fill();
						ctx.textAlign = 'center';
						ctx.fillStyle = C.faint;
						ctx.font = '11px sans-serif';
						ctx.fillText('还没有点亮任何通行证', W / 2, y + 28);
						ctx.fillStyle = C.faint2;
						ctx.font = '10px sans-serif';
						ctx.fillText('去展柜勾选你已收集的干员', W / 2, y + 46);
						ctx.textAlign = 'left';
						y += 76;
					}

					// 盒块
					infos.forEach(function (info) {
						// 0 进度：压成一行，避免几十个空盒把图撑爆
						if (!info.owned) {
							ctx.fillStyle = C.sub;
							ctx.font = '11px sans-serif';
							ctx.textAlign = 'left';
							ctx.fillText(info.name, CX, y + 10);
							ctx.fillStyle = C.faint2;
							ctx.font = '10px sans-serif';
							ctx.textAlign = 'right';
							ctx.fillText('0/' + info.total, CX + CW, y + 10);
							ctx.textAlign = 'left';
							y += 24;
							return;
						}

						// 盒名 + 件数 + 百分比
						ctx.fillStyle = C.ink;
						ctx.font = 'bold 11px sans-serif';
						ctx.textAlign = 'left';
						ctx.fillText(info.name, CX, y + 10);

						ctx.textAlign = 'right';
						ctx.fillStyle = C.blue;
						ctx.font = 'bold 10px sans-serif';
						ctx.fillText(info.pct + '%', CX + CW, y + 10);
						ctx.fillStyle = C.sub;
						ctx.font = '10px sans-serif';
						ctx.fillText(info.owned + '/' + info.total, CX + CW - 36, y + 10);
						ctx.textAlign = 'left';

						// 盒进度条
						y += 16;
						roundRect(ctx, CX, y, CW, 6, 3);
						ctx.fillStyle = C.barBg;
						ctx.fill();
						const bw = info.total > 0 ? Math.max(4, CW * Math.min(1, info.owned / info.total)) : 0;
						if (bw > 0) {
							roundRect(ctx, CX, y, bw, 6, 3);
							ctx.fillStyle = info.owned >= info.total && info.total > 0 ? C.pairText : C.blue;
							ctx.fill();
						}
						y += 14;

						y = drawChips(ctx, chipRows(info), CX, y);
						y += 14;
					});

					// 卡片内：分隔线 + 生成时间
					y += 4;
					ctx.strokeStyle = C.border;
					ctx.beginPath();
					ctx.moveTo(CX, y);
					ctx.lineTo(CX + CW, y);
					ctx.stroke();

					const now = new Date();
					const ts = now.getFullYear() + '-' + String(now.getMonth() + 1).padStart(2, '0') + '-' +
						String(now.getDate()).padStart(2, '0') + ' ' + String(now.getHours()).padStart(2, '0') + ':' +
						String(now.getMinutes()).padStart(2, '0');
					ctx.fillStyle = C.faint2;
					ctx.font = '9px sans-serif';
					ctx.textAlign = 'center';
					ctx.fillText('生成时间 ' + ts + (pageCount > 1 ? ' · ' + (pageIndex + 1) + '/' + pageCount : ''), W / 2, y + 18);

					// 卡片外：品牌行
					const cardBottom = CARD_TOP + cardH;
					ctx.fillStyle = C.faint;
					ctx.font = '10px sans-serif';
					ctx.fillText('方舟通行证谷子查询工具', W / 2, cardBottom + 22);
					ctx.fillStyle = C.faint2;
					ctx.font = '9px sans-serif';
					ctx.fillText('数据只存在本机，不上传服务器', W / 2, cardBottom + 38);
					ctx.textAlign = 'left';

					ctx.draw(false, function () {
						// 旧版 canvas 的 draw 回调与像素落盘之间需要一点时间，太快导出会是空白
						setTimeout(function () {
							uni.canvasToTempFilePath({
								canvasId: 'shareCanvas',
								x: 0,
								y: 0,
								width: W,
								height: totalH,
								destWidth: W * 2,
								destHeight: totalH * 2,
								fileType: 'png',
								quality: 1,
								success: function (res) {
									console.log('[overview_shot] 第 ' + (pageIndex + 1) + '/' + pageCount + ' 张完成', res.tempFilePath);
									resolve(res.tempFilePath);
								},
								fail: function (err) {
									console.error('[overview_shot] 导出失败', err);
									reject(err);
								}
							}, self);
						}, 260);
					});
				});
			}

			function drawAll(index) {
				if (index >= pageCount) {
					self.shareImageReady = true;
					self.isGeneratingShareImage = false;
					console.log('[overview_shot] 全部完成，共 ' + self.shareImagePaths.length + ' 张');
					return;
				}
				drawPage(pages[index], index)
					.then(function (path) {
						self.shareImagePaths.push(path);
						drawAll(index + 1);
					})
					.catch(function (err) {
						console.error('[overview_shot] 绘制失败', err);
						self.isGeneratingShareImage = false;
						uni.showToast({ title: '生成失败', icon: 'none' });
					});
			}

			drawAll(0);
		}
	}
};

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



.action-btn.primary {

	background-color: #67C23A;

	color: #fff;

	border: none;

}



.action-btn.secondary {

	background-color: #409EFF;

	color: #fff;

	border: none;

}



.action-btn.danger {

	background-color: #f56c6c;

	color: #fff;

	border: none;

}



.action-btn.danger:disabled {

	background-color: #ccc;

	color: #999;

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



/* 搜索框样式*/

.search-section {

	background-color: #fff;

	border-radius: 16rpx;

	padding: 20rpx;

	margin-bottom: 20rpx;

	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);

}



.search-input-wrapper {

	display: flex;

	align-items: center;

	background-color: #f8f8f8;

	border-radius: 10rpx;

	padding: 12rpx 16rpx;

	border: 2rpx solid #e0e0e0;

	transition: all 0.3s;

}



.search-input-wrapper:focus-within {
	border-color: var(--ak-focus-border, #2f7ed8);
	box-shadow: var(--ak-focus-shadow, 0 0 0 4rpx rgba(64, 158, 255, 0.18), 0 10rpx 24rpx -6rpx rgba(31, 94, 168, 0.28), inset 0 0 0 2rpx rgba(64, 158, 255, 0.45), inset 0 2rpx 8rpx rgba(31, 94, 168, 0.10));
}



.search-icon {

	font-size: 28rpx;

	margin-right: 10rpx;

	color: #999;

}



.search-input {

	flex: 1;

	font-size: 26rpx;

	color: #333;

	outline: none;

	border: none;

	background: transparent;

}



.search-input::placeholder {

	color: #999;

}



.clear-icon {

	font-size: 28rpx;

	color: #999;

	margin-left: 10rpx;

	padding: 5rpx;

	cursor: pointer;

}



.clear-icon:active {

	color: #666;

}



/* 搜索结果灉 */

.search-results {

	margin-top: 20rpx;

	max-height: 400rpx;

	overflow-y: auto;

}



.search-result-item {

	display: flex;

	justify-content: space-between;

	align-items: center;

	padding: 15rpx;

	background-color: #f8f8f8;

	border-radius: 10rpx;

	margin-bottom: 10rpx;

	border-left: 4rpx solid #409EFF;

}



.result-info {

	flex: 1;

	display: flex;

	flex-direction: column;

}



.result-character-name {

	font-size: 26rpx;

	font-weight: bold;

	color: #333;

	margin-bottom: 5rpx;

}



.result-box-id {

	font-size: 22rpx;

	color: #666;

	margin-bottom: 3rpx;

}



.result-match-field {

	font-size: 20rpx;

	color: #999;

}



.result-actions {

	margin-left: 15rpx;

}



.result-action-btn {

	padding: 10rpx 20rpx;

	background-color: #409EFF;

	color: #fff;

	border-radius: 8rpx;

	font-size: 22rpx;

	white-space: nowrap;

}



.result-action-btn:active {

	background-color: #66B1FF;

}



/* 无搜索结果提示 */

.no-search-results {

	text-align: center;

	padding: 40rpx 20rpx;

	color: #999;

	font-size: 26rpx;

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



/* 盒子到嗙被按钮挳 */

.box-filter-buttons {

	display: flex;

	flex-wrap: wrap;

	gap: 10rpx;

	background-color: #fff;

	border-radius: 16rpx;

	padding: 15rpx;

	margin-bottom: 20rpx;

	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);

}



.filter-btn {

	flex: 1;

	min-width: 100rpx;

	padding: 12rpx 15rpx;

	border-radius: 10rpx;

	background-color: #f8f8f8;

	color: #666;

	font-size: 24rpx;

	border: none;

	transition: all 0.3s;

}



.filter-btn.active {

	background-color: #409EFF;

	color: #fff;

	font-weight: bold;

}



.filter-btn:active {

	transform: scale(0.95);

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
	position: relative; /* 数量/价格浮层都挂在这张卡上，别飞到整个盒区块底部 */
	min-width: 0;

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



/* 精一 / 精二 切换胶囊：只在点亮「一对」时出现 */

.avatar-level-switch {

	position: absolute;

	left: 4rpx;

	top: 92rpx;

	z-index: 3;

	padding: 0 8rpx;

	height: 26rpx;

	line-height: 26rpx;

	border-radius: 13rpx;

	background: rgba(0, 0, 0, 0.55);

}



.avatar-level-text {

	font-size: 18rpx;

	color: #ffffff;

	line-height: 26rpx;

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



/* 成就标记样式 */

.pair-indicator {
	position: absolute;
	top: -6rpx;
	right: -6rpx;
	background-color: #FF6B35;
	color: #fff;
	padding: 3rpx 7rpx;
	border-radius: 8rpx;
	font-size: 16rpx;
	line-height: 1.15;
	display: flex;
	flex-direction: row;
	align-items: center;
	white-space: nowrap;
	border: 2rpx solid #fff;
	box-shadow: 0 2rpx 8rpx rgba(255, 107, 53, 0.3);
	z-index: 10;
}

/* 散件摘要挂在名字下面：塞进右上角小角标会逐字换行成竖条 */
.loose-line {
	max-width: 130rpx;
	font-size: 14rpx;
	line-height: 1.2;
	color: #FF6B35;
	text-align: center;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}



.pair-text {

	font-weight: bold;

	font-size: 14rpx;

}



.pair-count {

	font-size: 12rpx;

}



/* 数量显示区域 */

.quantity-display {
	position: absolute;
	bottom: 0;
	left: 0;
	right: 0;
	display: flex;
	flex-direction: column;
	gap: 2rpx;
	background-color: rgba(255, 255, 255, 0.96);
	padding: 3rpx 3rpx;
	border-radius: 0 0 10rpx 10rpx;
	max-height: 120rpx;
	overflow-y: auto;
	z-index: 5;
}



.quantity-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 1rpx 2rpx;
	background-color: #f8f8f8;
	border-radius: 5rpx;
	font-size: 18rpx;
	line-height: 1.1;
}



.elite-label {

	font-weight: bold;

	font-size: 16rpx;

	min-width: 18rpx;

	text-align: center;

}



.elite1-label {

	color: #E6A23C;

}



.elite2-label {

	color: #409EFF;

}



.pair-label {

	color: #FF6B35;

}



.quantity-controls {

	display: flex;

	align-items: center;

	gap: 2rpx;

	margin-left: 2rpx;

}



.quantity-btn {

	width: 20rpx;

	height: 20rpx;

	display: flex;

	align-items: center;

	justify-content: center;

	background-color: #409EFF;

	color: #fff;

	border-radius: 4rpx;

	font-size: 14rpx;

	font-weight: bold;

	cursor: pointer;

	user-select: none;

}



.quantity-btn:active {

	background-color: #66B1FF;

	transform: scale(0.95);

}



.quantity-value {

	min-width: 16rpx;

	text-align: center;

	font-weight: bold;

	color: #333;

	font-size: 16rpx;

}

.loose-count {
	font-size: 14rpx;
	line-height: 1.25;
	opacity: 0.95;
}

/* 底部「显示 / 隐藏数量」开关 */
.qty-toggle {
	flex: 0 0 auto;
	font-size: 22rpx;
	line-height: 1.6;
	padding: 0 16rpx;
	border-radius: 20rpx;
	color: #409EFF;
	background-color: rgba(64, 158, 255, 0.12);
	border: 1rpx solid rgba(64, 158, 255, 0.35);
}

.qty-toggle.on {
	color: #fff;
	background-color: #409EFF;
	border-color: #409EFF;
}



/* 价格显示区域 */

.price-display {

	position: absolute;

	bottom: 0;

	left: 0;

	right: 0;

	display: flex;

	flex-direction: column;

	align-items: center;

	gap: 2rpx;

	background-color: rgba(255, 255, 255, 0.9);

	padding: 5rpx 0;

	border-radius: 0 0 12rpx 12rpx;

}



.price-text {

	font-size: 18rpx;

	color: #E6A23C;

	font-weight: bold;

	white-space: nowrap;

	overflow: hidden;

	text-overflow: ellipsis;

	max-width: 100rpx;

}



/* 出物模式价格杈撳叆 */

.trade-price-inputs {

	position: absolute;

	bottom: 0;

	left: 0;

	right: 0;

	background-color: rgba(255, 255, 255, 0.9);

	padding: 5rpx;

	border-radius: 0 0 12rpx 12rpx;

	z-index: 10;

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

	background-color: #fff;

	border-radius: 16rpx;

	margin-top: 20rpx;

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

	text-align: center;

}



.sync-btn,

.debug-btn,

.load-btn {

	background-color: #409EFF;

	color: #fff;

	border-radius: 50rpx;

	font-size: 26rpx;

	padding: 20rpx 40rpx;

	margin-bottom: 15rpx;

	width: 80%;

}



.debug-btn {

	background-color: #E6A23C;

}



.load-btn {

	background-color: #67C23A;

}



/* 收集概览样式 - 全新设计 */

.overview-page {

	position: fixed;

	top: 0;

	left: 0;

	right: 0;

	bottom: 0;

	background-color: #fff;

	z-index: 10000;

}



.overview-scroll {

	width: 100%;

	height: 100%;

}



.overview-header {

	display: flex;

	justify-content: space-between;

	align-items: center;

	padding: 30rpx;

	background-color: #fff;

	border-bottom: 1rpx solid #f0f0f0;

}



.overview-close-btn {

	background-color: #409EFF;

	color: #fff;

	font-size: 28rpx;

	padding: 12rpx 24rpx;

	border-radius: 30rpx;

	border: none;

}



.overview-title {

	font-size: 32rpx;

	font-weight: bold;

	color: #333;

}



/* 概览统计卡片 */

.overview-cards {

	display: flex;

	justify-content: space-between;

	padding: 30rpx 24rpx;

	gap: 16rpx;

	background-color: #fff;

}



.overview-card-item {

	flex: 1;

	background-color: #f8f9fa;

	border-radius: 12rpx;

	padding: 20rpx 12rpx;

	display: flex;

	flex-direction: column;

	align-items: center;

	gap: 8rpx;

}



.card-icon-img {

	width: 40rpx;

	height: 40rpx;

}



.card-icon {

	font-size: 32rpx;

}



.card-value {

	font-size: 36rpx;

	font-weight: bold;

	color: #333;

}



.card-label {

	font-size: 22rpx;

	color: #999;

}



/* 收集进度区 */

.progress-ring-section {

	padding: 30rpx 24rpx;

	background-color: #fff;

	margin-top: 20rpx;

}



.section-title {

	font-size: 28rpx;

	font-weight: bold;

	color: #333;

	margin-bottom: 20rpx;

	display: block;

}



.progress-ring-wrapper {

	display: flex;

	gap: 30rpx;

	align-items: center;

}



.progress-ring {

	width: 140rpx;

	height: 140rpx;

	border-radius: 50%;

	border: 12rpx solid #e8f4ff;

	display: flex;

	align-items: center;

	justify-content: center;

	position: relative;

}



.progress-ring-text {

	display: flex;

	flex-direction: column;

	align-items: center;

}



.progress-percent {

	font-size: 36rpx;

	font-weight: bold;

	color: #409EFF;

}



.progress-sublabel {

	font-size: 20rpx;

	color: #999;

}



.progress-details {

	flex: 1;

	display: flex;

	flex-direction: column;

	gap: 16rpx;

}



.progress-detail-item {

	display: flex;

	justify-content: space-between;

}



.detail-label {

	font-size: 26rpx;

	color: #666;

}



.detail-value {

	font-size: 26rpx;

	font-weight: bold;

	color: #333;

}



.wanted-highlight {

	color: #9b59b6;

}



/* 盒子完成度排行 */

.box-ranking-section {

	padding: 30rpx 24rpx;

	background-color: #fff;

	margin-top: 20rpx;

}



.box-ranking-scroll {

	max-height: 500rpx;

}



.box-ranking-list {

	/* max-height: 500rpx;

	overflow-y: auto; */

}



.box-ranking-item {

	display: flex;

	align-items: center;

	padding: 16rpx 0;

	border-bottom: 1rpx solid #f5f5f5;

	gap: 12rpx;

}



.box-rank {

	width: 40rpx;

	height: 40rpx;

	border-radius: 50%;

	background-color: #f0f0f0;

	display: flex;

	align-items: center;

	justify-content: center;

	font-size: 22rpx;

	font-weight: bold;

	color: #666;

}



.box-rank.rank-1 {

	background-color: #ffd700;

	color: #fff;

}



.box-rank.rank-2 {

	background-color: #c0c0c0;

	color: #fff;

}



.box-rank.rank-3 {

	background-color: #cd7f32;

	color: #fff;

}



.box-info {

	flex: 1;

}



.box-name {

	font-size: 26rpx;

	font-weight: bold;

	color: #333;

}



.box-type-tag {

	font-size: 18rpx;

	padding: 4rpx 8rpx;

	border-radius: 4rpx;

	margin-left: 8rpx;

}



.box-progress-bar {

	flex: 1;

	height: 16rpx;

	background-color: #f0f0f0;

	border-radius: 8rpx;

	overflow: hidden;

}



.box-progress-fill {

	height: 100%;

	background-color: #409EFF;

	border-radius: 8rpx;

}



.box-progress-fill.complete {

	background-color: #67C23A;

}



.box-count {

	font-size: 24rpx;

	color: #409EFF;

	font-weight: bold;

	width: 80rpx;

	text-align: right;

}



.empty-tip {

	text-align: center;

	padding: 40rpx;

	color: #999;

}



.load-more-boxes {

	text-align: center;

	padding: 20rpx;

	color: #409EFF;

	font-size: 26rpx;

}



/* 精英分布 */

.distribution-section {

	padding: 30rpx 24rpx;

	background-color: #fff;

	margin-top: 20rpx;

}



.distribution-bars {

	display: flex;

	flex-direction: column;

	gap: 16rpx;

}



.distribution-item {

	display: flex;

	align-items: center;

	gap: 16rpx;

}



.dist-label {

	width: 120rpx;

	font-size: 24rpx;

	color: #666;

}



.dist-bar-wrapper {

	flex: 1;

	height: 24rpx;

	background-color: #f0f0f0;

	border-radius: 12rpx;

	overflow: hidden;

}



.dist-bar {

	height: 100%;

	border-radius: 12rpx;

}



.dist-bar-elite1 {

	background-color: #409EFF;

}



.dist-bar-elite2 {

	background-color: #67C23A;

}



.dist-bar-pair {

	background-color: #e6a23c;

}



.dist-value {

	width: 60rpx;

	font-size: 24rpx;

	font-weight: bold;

	color: #333;

	text-align: right;

}



/* 收集时间线 */

.timeline-section {

	padding: 30rpx 24rpx;

	background-color: #fff;

	margin-top: 20rpx;

}



.timeline-list {

	display: flex;

	flex-direction: column;

	gap: 16rpx;

}



.timeline-item {

	display: flex;

	justify-content: space-between;

	padding: 12rpx 0;

	border-bottom: 1rpx solid #f5f5f5;

}



.timeline-label {

	font-size: 26rpx;

	color: #666;

}



.timeline-value {

	font-size: 26rpx;

	font-weight: bold;

	color: #333;

}



/* 出物列表 */

.overview-list {

	padding: 30rpx 24rpx;

	background-color: #fff;

	margin-top: 20rpx;

}



.owned-list-items {

	display: flex;

	flex-direction: column;

	gap: 16rpx;

}



.owned-list-item {

	display: flex;

	align-items: center;

	gap: 16rpx;

	padding: 16rpx;

	background-color: #f8f9fa;

	border-radius: 12rpx;

}



.item-avatar {

	width: 60rpx;

	height: 60rpx;

	border-radius: 50%;

}



.item-info {

	flex: 1;

}



.item-name {

	font-size: 26rpx;

	font-weight: bold;

	color: #333;

}



.item-badges {

	display: flex;

	gap: 8rpx;

	margin-top: 8rpx;

}



.item-badge {

	font-size: 20rpx;

	padding: 4rpx 8rpx;

	border-radius: 4rpx;

}



.item-badge.elite1 {

	background-color: #e8f4ff;

	color: #409EFF;

}



.item-badge.elite2 {

	background-color: #e8f9e8;

	color: #67C23A;

}



.item-badge.pair {

	background-color: #fdf6ec;

	color: #e6a23c;

}



.item-badge.price {

	background-color: #fef0f0;

	color: #f56c6c;

}



/* 底部按钮 */

.overview-footer {

	padding: 30rpx 24rpx;

	background-color: #fff;

	margin-top: 20rpx;

}



.generate-btn {

	width: 100%;

	background-color: #409EFF;

	color: #fff;

	font-size: 28rpx;

	padding: 24rpx;

	border-radius: 12rpx;

	border: none;

}



/* 调试模式 */

.debug-modal {

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



.debug-modal-content {

	background-color: #fff;

	border-radius: 16rpx;

	width: 100%;

	max-width: 800rpx;

	max-height: 90vh;

	display: flex;

	flex-direction: column;

	overflow: hidden;

}



.debug-modal-header {

	display: flex;

	justify-content: space-between;

	align-items: center;

	padding: 30rpx;

	border-bottom: 1rpx solid #f0f0f0;

}



.debug-modal-title {

	font-size: 30rpx;

	font-weight: bold;

	color: #333;

}



.debug-close-btn {

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



.debug-body {

	flex: 1;

	padding: 20rpx;

	overflow-y: auto;

	max-height: 60vh;

}



.debug-section {

	margin-bottom: 30rpx;

	padding-bottom: 20rpx;

	border-bottom: 1rpx solid #f0f0f0;

}



.debug-section:last-child {

	border-bottom: none;

}



.debug-section-title {

	font-size: 24rpx;

	font-weight: bold;

	color: #333;

	margin-bottom: 15rpx;

	display: block;

}



.debug-item {

	display: flex;

	justify-content: space-between;

	margin-bottom: 10rpx;

}



.debug-label {

	font-size: 22rpx;

	color: #666;

}



.debug-value {

	font-size: 22rpx;

	color: #333;

	font-weight: bold;

}



.test-btn {

	background-color: #409EFF;

	color: #fff;

	border-radius: 10rpx;

	padding: 15rpx;

	font-size: 22rpx;

	width: 100%;

	margin-bottom: 15rpx;

}



.test-result {

	background-color: #f8fafc;

	border-radius: 10rpx;

	padding: 15rpx;

	border: 1rpx solid #e0e0e0;

}



.test-result-text {

	font-size: 20rpx;

	color: #333;

	white-space: pre-wrap;

	word-break: break-all;

}

.operation-bar {

  display: flex;

  align-items: center;

  width: 100%;

  height: 120rpx;

  background-color: #F0F7FF;

  border: 4rpx solid #B3D8FF;

  border-radius: 60rpx;

  padding: 0 20rpx;

  box-sizing: border-box;

  gap: 20rpx; /* 按钮和提示区之间的间距 */

}



/* 左侧成就榜按钮（全圆角矩形） */

.achievement-btn {

  display: flex;

  align-items: center;

  justify-content: center;

  height: 88rpx;

  padding: 0 32rpx;

  background-color: #FFC107;

  border-radius: 44rpx; /* 全圆角，高度的一半，完美圆角 */

  box-shadow: 0 2rpx 8rpx rgba(0,0,0,0.1);

  transition: all 0.2s ease;

  flex-shrink: 0; /* 禁止按钮被压缩 */

}

.achievement-btn:active {

  opacity: 0.85;

  transform: scale(0.98);

}

.trophy-icon {

  font-size: 48rpx;

  margin-right: 12rpx;

}

.achievement-text {

  font-size: 32rpx;

  font-weight: bold;

  color: #333;

  margin-right: 8rpx;

}

.achievement-count {

  font-size: 24rpx;

  color: #666;

  background: #f0f0f0;

  padding: 4rpx 12rpx;

  border-radius: 20rpx;

  margin-right: 12rpx;

}

.badge {

  width: 48rpx;

  height: 48rpx;

  background-color: #FF4757;

  border-radius: 50%;

  display: flex;

  align-items: center;

  justify-content: center;

  box-shadow: 0 2rpx 4rpx rgba(0,0,0,0.2);

}

.badge-num {

  font-size: 28rpx;

  font-weight: bold;

  color: #fff;

  line-height: 1;

}



/* 右侧操作提示区 */

.tip-area {

  flex: 1;

  height: 100%;

  display: flex;

  align-items: center;

  justify-content: center;

  gap: 24rpx;

}

.tip-text {

  font-size: 25rpx;

  color: #409EFF;

  font-weight: 500;

}

.tip-divider {

  font-size: 28rpx;

  color: #C0C4CC;

}

.debug-modal-footer {

	padding: 20rpx;

	border-top: 1rpx solid #f0f0f0;

	display: flex;

	gap: 15rpx;

}



.debug-action-btn {

	flex: 1;

	padding: 20rpx;

	border-radius: 10rpx;

	font-size: 22rpx;

}



.debug-action-btn:first-child {

	background-color: #F56C6C;

	color: #fff;

}



.debug-action-btn:last-child {

	background-color: #409EFF;

	color: #fff;

}



/* 分享预览模式 */

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



/* 已点亮列表样式*/

.owned-list {

	display: flex;

	flex-direction: column;

	gap: 15rpx;

}



.owned-item {

	display: flex;

	align-items: center;

	padding: 15rpx;

	background-color: #f8f8f8;

	border-radius: 10rpx;

	border-left: 4rpx solid #409EFF;

}



.owned-avatar {

	width: 60rpx;

	height: 60rpx;

	border-radius: 8rpx;

	margin-right: 15rpx;

	object-fit: cover;

}



.owned-info {

	flex: 1;

	display: flex;

	flex-direction: column;

}



.owned-name {

	font-size: 26rpx;

	font-weight: bold;

	color: #333;

	margin-bottom: 8rpx;

}



.owned-badges {

	display: flex;

	gap: 8rpx;

	flex-wrap: wrap;

}



.badge {

	display: inline-block;

	padding: 4rpx 10rpx;

	border-radius: 6rpx;

	font-size: 18rpx;

	font-weight: bold;

	color: #fff;

}



.elite1-badge {

	background-color: #E6A23C;

}



.elite2-badge {

	background-color: #409EFF;

}



.pair-badge {

	background-color: #FF6B35;

}



.no-characters {

	text-align: center;

	padding: 40rpx 20rpx;

	color: #999;

}



.no-characters-text {

	font-size: 26rpx;

}



/* 分享内容预览区域样式 */

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

	justify-content: space-between;

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

	align-items: center;

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



/* 分享预览底部操作按钮挳 */

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



/* 底部操作栏*/

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

	padding: 15rpx 10rpx;

	border-radius: 12rpx;

	font-size: 22rpx;

	border: 2rpx solid #e0e0e0;

	transition: all 0.2s;

}



.quick-btn:active {

	transform: scale(0.95);

}



.elite1-btn {

	background: linear-gradient(135deg, #e8f5e9, #c8e6c9);

	color: #2e7d32;

	border-color: #a5d6a7;

}



.elite2-btn {

	background: linear-gradient(135deg, #fff3e0, #ffe0b2);

	color: #e65100;

	border-color: #ffcc80;

}



.pair-btn {

	background: linear-gradient(135deg, #fce4ec, #f8bbd9);

	color: #c2185b;

	border-color: #f48fb1;

}



.clear-btn {

	background: linear-gradient(135deg, #fafafa, #eeeeee);

	color: #d32f2f;

	border-color: #e0e0e0;

}



.summary-info {

	text-align: center;

	display: flex;

	flex-direction: row;

	flex-wrap: wrap;

	justify-content: center;

	align-items: center;

	gap: 4rpx 14rpx;

}



.summary-text {

	font-size: 22rpx;

	color: #333;

	font-weight: bold;

}



/* 响应式调整*/

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



/* 概览页面区域样式 */

.overview-page {

	position: fixed;

	top: 0;

	left: 0;

	right: 0;

	bottom: 0;

	background-color: #f5f5f5;

	z-index: 9999;

	display: flex;

	flex-direction: column;

}



.overview-scroll {

	flex: 1;

	height: 100%;

}



.overview-header {

	display: flex;

	justify-content: space-between;

	align-items: center;

	padding: 20rpx;

	background-color: #fff;

	border-bottom: 1rpx solid #e0e0e0;

	position: sticky;

	top: 0;

	z-index: 10000;

}



.overview-close-btn {

	padding: 10rpx 20rpx;

	background-color: #409EFF;

	color: #fff;

	border-radius: 8rpx;

	font-size: 24rpx;

}



.overview-title {

	font-size: 32rpx;

	font-weight: bold;

	color: #333;

}



/* 概览统计分$墖 */

.overview-cards {

	display: flex;

	justify-content: space-between;

	padding: 20rpx 24rpx;

	background: #fff;

	margin-bottom: 20rpx;

}



.overview-card-item {

	flex: 1;

	background: #fff;

	border-radius: 16rpx;

	padding: 24rpx 12rpx;

	text-align: center;

	margin: 0 8rpx;

	box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

}



.overview-card-item:first-child { margin-left: 0; }

.overview-card-item:last-child { margin-right: 0; }



.card-icon-img {

	width: 56rpx;

	height: 56rpx;

	display: block;

	margin: 0 auto 8rpx auto;

}



.card-value {

	font-size: 40rpx;

	font-weight: bold;

	color: #333;

	display: block;

}



.card-label {

	font-size: 22rpx;

	color: #999;

	display: block;

	margin-top: 4rpx;

}



.elite1-card { border-top: 4rpx solid #4FC3F7; }

.elite2-card { border-top: 4rpx solid #FFD54F; }

.pair-card { border-top: 4rpx solid #FF8A65; }



.section-title {

	font-size: 32rpx;

	font-weight: bold;

	color: #333;

	display: block;

	margin-bottom: 20rpx;

	padding-left: 8rpx;

}



/* 收集进度 */

.progress-ring-section,

.box-ranking-section,

.distribution-section,

.timeline-section {

	background: #fff;

	border-radius: 16rpx;

	padding: 28rpx;

	margin: 0 20rpx 20rpx 20rpx;

	box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

}



.progress-ring-wrapper {

	display: flex;

	align-items: center;

}



.progress-ring {

	width: 200rpx;

	height: 200rpx;

	border-radius: 50%;

	position: relative;

	margin-right: 32rpx;

	flex-shrink: 0;

}



.progress-ring-bg {

	width: 100%;

	height: 100%;

	border-radius: 50%;

	background: conic-gradient(#eee 0% 100%);

	position: absolute;

}



.progress-ring-fill {

	width: 100%;

	height: 100%;

	border-radius: 50%;

	background: conic-gradient(#409EFF 0% var(--progress, 0%) #eee var(--progress, 0%) 100%);

	position: absolute;

	transition: all 0.8s ease;

}



.progress-ring-text {

	position: absolute;

	top: 50%;

	left: 50%;

	transform: translate(-50%, -50%);

	text-align: center;

	background: #fff;

	width: 140rpx;

	height: 140rpx;

	border-radius: 50%;

	display: flex;

	flex-direction: column;

	align-items: center;

	justify-content: center;

}



.progress-percent {

	font-size: 36rpx;

	font-weight: bold;

	color: #409EFF;

}



.progress-sublabel {

	font-size: 20rpx;

	color: #999;

}



.progress-details {

	flex: 1;

}



.progress-detail-item {

	display: flex;

	justify-content: space-between;

	padding: 12rpx 0;

	border-bottom: 1rpx solid #f0f0f0;

}



.progress-detail-item:last-child { border-bottom: none; }



.detail-label { font-size: 26rpx; color: #666; }

.detail-value { font-size: 26rpx; color: #333; font-weight: bold; }

.wanted-highlight { color: #FF6B35; }



/* 盒子完成度排行*/

.box-ranking-list { }



.box-ranking-item {

	display: flex;

	align-items: center;

	padding: 16rpx 0;

	border-bottom: 1rpx solid #f5f5f5;

}



.box-ranking-item:last-child { border-bottom: none; }



.box-rank {

	width: 40rpx;

	height: 40rpx;

	border-radius: 50%;

	background: #eee;

	color: #999;

	font-size: 24rpx;

	display: flex;

	align-items: center;

	justify-content: center;

	margin-right: 16rpx;

	flex-shrink: 0;

}



.box-rank.rank-1 { background: #FFD700; color: #fff; }

.box-rank.rank-2 { background: #C0C0C0; color: #fff; }

.box-rank.rank-3 { background: #CD7F32; color: #fff; }



.box-info {

	flex: 1;

	min-width: 0;

	margin-right: 16rpx;

}



.box-name {

	font-size: 26rpx;

	color: #333;

	display: block;

	overflow: hidden;

	text-overflow: ellipsis;

	white-space: nowrap;

}



.box-type-tag {

	font-size: 18rpx;

	padding: 2rpx 8rpx;

	border-radius: 6rpx;

	background: #f0f0f0;

	color: #999;

	display: inline-block;

	margin-top: 4rpx;

}



.type-whitelist { background: #E3F2FD; color: #1976D2; }

.type-special { background: #FFF3E0; color: #E65100; }

.type-cooperation { background: #F3E5F5; color: #7B1FA2; }

.type-ambience { background: #E8F5E9; color: #388E3C; }



.box-progress-bar {

	width: 200rpx;

	height: 12rpx;

	background: #eee;

	border-radius: 6rpx;

	margin-right: 16rpx;

	overflow: hidden;

	flex-shrink: 0;

}



.box-progress-fill {

	height: 100%;

	border-radius: 6rpx;

	background: linear-gradient(90deg, #409EFF, #66B1FF);

	transition: width 0.8s ease;

}



.box-progress-fill.complete { background: linear-gradient(90deg, #4CAF50, #66BB6A); }



.box-count {

	font-size: 24rpx;

	color: #999;

	width: 80rpx;

	text-align: right;

	flex-shrink: 0;

}



.empty-tip {

	text-align: center;

	padding: 40rpx;

	color: #ccc;

	font-size: 26rpx;

}



.load-more-boxes {

	text-align: center;

	padding: 20rpx 0 10rpx 0;

	margin-top: 10rpx;

	border-top: 1rpx solid #f0f0f0;

}



.load-more-text {

	font-size: 26rpx;

	color: #409EFF;

	padding: 10rpx 40rpx;

}



/* 精英分布 */

.distribution-bars { }



.distribution-item {

	display: flex;

	align-items: center;

	margin-bottom: 16rpx;

}



.distribution-item:last-child { margin-bottom: 0; }



.dist-label {

	font-size: 24rpx;

	color: #666;

	width: 140rpx;

	flex-shrink: 0;

}



.dist-bar-wrapper {

	flex: 1;

	height: 16rpx;

	background: #f0f0f0;

	border-radius: 8rpx;

	overflow: hidden;

	margin: 0 16rpx;

}



.dist-bar {

	height: 100%;

	border-radius: 8rpx;

	transition: width 0.8s ease;

}



.dist-bar-elite1 { background: linear-gradient(90deg, #4FC3F7, #29B6F6); }

.dist-bar-elite2 { background: linear-gradient(90deg, #FFD54F, #FFCA28); }

.dist-bar-pair { background: linear-gradient(90deg, #FF8A65, #FF7043); }



.dist-value {

	font-size: 24rpx;

	color: #333;

	font-weight: bold;

	width: 60rpx;

	text-align: right;

}



/* 收集时间线*/

.timeline-list { }



.timeline-item {

	display: flex;

	justify-content: space-between;

	padding: 16rpx 0;

	border-bottom: 1rpx solid #f5f5f5;

}



.timeline-item:last-child { border-bottom: none; }



.timeline-label { font-size: 26rpx; color: #666; }

.timeline-value { font-size: 26rpx; color: #333; font-weight: 500; }



/* 出物列表 */

.overview-list {

	background: #fff;

	border-radius: 16rpx;

	padding: 28rpx;

	margin: 0 20rpx 20rpx 20rpx;

	box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

}



.owned-list-items {

	display: flex;

	flex-direction: column;

	gap: 10rpx;

}



.owned-list-item {

	display: flex;

	align-items: center;

	padding: 12rpx;

	background-color: #f8f8f8;

	border-radius: 10rpx;

	border-left: 4rpx solid #409EFF;

}



.item-avatar {

	width: 50rpx;

	height: 50rpx;

	border-radius: 8rpx;

	margin-right: 12rpx;

	object-fit: cover;

}



.item-info {

	flex: 1;

	display: flex;

	flex-direction: column;

}



.item-name {

	font-size: 24rpx;

	font-weight: bold;

	color: #333;

	margin-bottom: 6rpx;

}



.item-badges {

	display: flex;

	gap: 6rpx;

	flex-wrap: wrap;

}



.item-badge {

	display: inline-block;

	padding: 3rpx 8rpx;

	border-radius: 4rpx;

	font-size: 16rpx;

	font-weight: bold;

	color: #fff;

}



.item-badge.elite1 {

	background-color: #E6A23C;

}



.item-badge.elite2 {

	background-color: #409EFF;

}



.item-badge.pair {

	background-color: #FF6B35;

}



.item-badge.price {

	background-color: #67C23A;

}



.overview-footer {

	padding: 20rpx;

	background-color: #fff;

}



/* 分享选项区域 */

.share-options-section {

	padding: 20rpx 30rpx;

	background-color: #fff;

	margin: 0 20rpx 20rpx 20rpx;

	border-radius: 16rpx;

	box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

}



.share-option-item {

	display: flex;

	align-items: flex-start;

	padding: 16rpx 0;

}



.share-option-checkbox {

	width: 40rpx;

	height: 40rpx;

	border: 2rpx solid #409EFF;

	border-radius: 8rpx;

	margin-right: 20rpx;

	display: flex;

	align-items: center;

	justify-content: center;

	flex-shrink: 0;

	margin-top: 4rpx;

}



.share-option-checkbox.checked {

	background-color: #409EFF;

}



.share-option-checkbox text {

	color: #fff;

	font-size: 24rpx;

	font-weight: bold;

}



.share-option-text {

	flex: 1;

	display: flex;

	flex-direction: column;

}



.share-option-title {

	font-size: 28rpx;

	font-weight: bold;

	color: #333;

	margin-bottom: 8rpx;

}



.share-option-desc {

	font-size: 22rpx;

	color: #999;

	line-height: 1.4;

}



.generate-btn {

	width: 100%;

	padding: 20rpx;

	background: linear-gradient(135deg, #409EFF 0%, #66B1FF 100%);

	color: #fff;

	border-radius: 40rpx;

	font-size: 28rpx;

	font-weight: bold;

	box-shadow: 0 4rpx 16rpx rgba(64, 158, 255, 0.3);

}



.generate-btn:active {

	opacity: 0.85;

	transform: scale(0.98);

}



/* 出物模式选傞厤 */

.overview-trade .overview-card-item { border-top-color: #FF6B35; }

.overview-trade .progress-ring-fill {

	background: conic-gradient(#FF6B35 0% var(--progress, 0%) #eee var(--progress, 0%) 100%);

}

.overview-trade .progress-percent { color: #FF6B35; }

.overview-trade .box-progress-fill {

	background: linear-gradient(90deg, #FF6B35, #FFA07A);

}



/* 分享图片弹窗 */

.share-image-preview-modal {

	position: fixed;

	top: 0;

	left: 0;

	right: 0;

	bottom: 0;

	background-color: rgba(0, 0, 0, 0.8);

	display: flex;

	align-items: center;

	justify-content: center;

	z-index: 10001;

	padding: 40rpx;

}



.share-image-preview-content {

	background-color: #fff;

	border-radius: 16rpx;

	width: 100%;

	max-width: 600rpx;

	max-height: 90vh;

	display: flex;

	flex-direction: column;

	overflow: hidden;

}



.preview-header {

	display: flex;

	justify-content: space-between;

	align-items: center;

	padding: 30rpx;

	border-bottom: 1rpx solid #f0f0f0;

}



.preview-title {

	font-size: 30rpx;

	font-weight: bold;

	color: #333;

}



.preview-close-btn {

	width: 60rpx;

	height: 60rpx;

	border-radius: 50%;

	background-color: #f0f0f0;

	color: #666;

	font-size: 36rpx;

	display: flex;

	align-items: center;

	justify-content: center;

	border: none;

}



.preview-body {

	flex: 1;

	padding: 20rpx;

	overflow-y: auto;

	display: flex;

	flex-direction: column;

	align-items: center;

}



.preview-image {

	width: 100%;

	max-width: 500rpx;

	border-radius: 12rpx;

	margin-bottom: 20rpx;

}



.preview-hint {

	font-size: 20rpx;

	color: #999;

	text-align: center;

	margin-bottom: 20rpx;

}



.preview-footer {

	display: flex;

	gap: 15rpx;

	padding: 20rpx;

	border-top: 1rpx solid #f0f0f0;

}



.preview-btn {

	flex: 1;

	padding: 15rpx;

	border-radius: 10rpx;

	font-size: 24rpx;

	font-weight: bold;

	border: none;

	color: #fff;

	width: 100%;

}



.preview-btn-primary {

	background-color: #FF6B35;

}



.preview-btn-primary:active {

	background-color: #FF8C5A;

}



.save-btn {

	background-color: #409EFF;

}



.save-btn:active {

	background-color: #66B1FF;

}



/* 概览页面模式特定样式 */

.overview-trade .box-progress-item {

	border-left-color: #FF6B35;

}



.overview-trade .stat-card {

	background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);

}



.overview-trade .owned-list-item {

	border-left-color: #FF6B35;

}



.overview-trade .generate-btn {

	background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);

}



.overview-trade .generate-btn:active {

	background: linear-gradient(135deg, #FF8C5A 0%, #FFA070 100%);

}



/* 操作提示 + 成就入口横条 */

.header-guide-bar {

	display: flex;

	align-items: center;

	justify-content: space-between;

	gap: 20rpx;

	margin-bottom: 24rpx;

	padding: 0 10rpx;

}



.operation-guide-box {

	display: flex;

	align-items: center;

	justify-content: center;

	background: #f0f0f0;

	border-radius: 12rpx;

	padding: 10rpx 16rpx;

}



.guide-text {

	font-size: 22rpx;

	color: #666;

}



/* 成就系统区域样式 */

.achievement-entry-btn {

	display: flex;

	align-items: center;

	justify-content: center;

	background: #fff;

	border: 2rpx solid #409EFF;

	border-radius: 12rpx;

	padding: 10rpx 16rpx;

	box-shadow: none;

}



.achievement-entry-icon {

	font-size: 28rpx;

	margin-right: 6rpx;

}



.achievement-entry-text {

	font-size: 22rpx;

	font-weight: normal;

	color: #409EFF;

}



.achievement-entry-badge {

	background-color: #FF6B6B;

	color: #fff;

	font-size: 18rpx;

	padding: 4rpx 10rpx;

	border-radius: 20rpx;

	margin-left: 8rpx;

}



.achievement-popup {

	position: fixed;

	top: 0;

	left: 0;

	right: 0;

	bottom: 0;

	z-index: 9999;

	display: flex;

	align-items: center;

	justify-content: center;

	animation: achievementFadeIn 0.5s ease;

}



.achievement-popup-content {

	background: linear-gradient(135deg, #FFD700, #FFA500);

	border-radius: 24rpx;

	padding: 48rpx;

	text-align: center;

	box-shadow: 0 8rpx 32rpx rgba(255, 165, 0, 0.4);

	animation: achievementBounce 0.5s ease;

	width: 80%;

}



.achievement-unlock-text {

	font-size: 28rpx;

	color: #8B4513;

	margin-bottom: 16rpx;

	display: block;

}



.achievement-popup-icon {

	font-size: 80rpx;

	display: block;

	margin-bottom: 16rpx;

}



.achievement-popup-name {

	font-size: 36rpx;

	font-weight: bold;

	color: #8B4513;

	display: block;

	margin-bottom: 8rpx;

}



.achievement-popup-desc {

	font-size: 24rpx;

	color: #A0522D;

	display: block;

}



.achievement-list-mask {

	position: fixed;

	top: 0;

	left: 0;

	right: 0;

	bottom: 0;

	z-index: 9998;

	background: rgba(0, 0, 0, 0.5);

	display: flex;

	align-items: center;

	justify-content: center;

}



.achievement-list-panel {

	background: #fff;

	border-radius: 24rpx;

	padding: 32rpx;

	width: 85%;

	max-height: 80vh;

}



.achievement-scroll {

	max-height: 60vh;

}



.achievement-list-header {

	display: flex;

	justify-content: space-between;

	align-items: center;

	margin-bottom: 24rpx;

}



.achievement-list-title {

	font-size: 36rpx;

	font-weight: bold;

}



.achievement-list-close {

	font-size: 36rpx;

	color: #999;

	padding: 8rpx 16rpx;

}



.achievement-list-scroll {

	max-height: 60vh;

}



.achievement-item {

	display: flex;

	align-items: center;

	padding: 20rpx;

	margin-bottom: 12rpx;

	border-radius: 16rpx;

	background: #f8f8f8;

}



.achievement-item.unlocked {

	background: linear-gradient(135deg, #FFF8DC, #FFEFD5);

	border: 1rpx solid #FFD700;

}



.achievement-icon {

	font-size: 48rpx;

	margin-right: 20rpx;

}



.achievement-info {

	flex: 1;

}



.achievement-name {

	font-size: 28rpx;

	font-weight: bold;

	display: block;

	color: #333;

}



.achievement-desc {

	font-size: 22rpx;

	color: #999;

	display: block;

}



.achievement-status {

	font-size: 22rpx;

	color: #FFD700;

	font-weight: bold;

}



.achievement-item:not(.unlocked) .achievement-status {

	color: #ccc;

}



.achievement-progress-text {

	display: block;

	text-align: center;

	font-size: 24rpx;

	color: #999;

	margin-top: 16rpx;

}



@keyframes achievementFadeIn {

	from { opacity: 0; }

	to { opacity: 1; }

}



@keyframes achievementBounce {

	0% { transform: scale(0.3); opacity: 0; }

	50% { transform: scale(1.05); }

	100% { transform: scale(1); opacity: 1; }

}



/* ========== 界园主题 (Jieyuan) ========== */

.container.theme-jieyuan {

	background: linear-gradient(180deg, #FFF8E7 0%, #FAF3E0 100%);

	min-height: 100vh;

}



.container.theme-jieyuan .search-bar {

	background: rgba(255, 255, 255, 0.85);

	border: 2rpx solid rgba(226, 88, 132, 0.2);

	border-radius: 32rpx;

}



.container.theme-jieyuan .search-bar input {

	color: #4A5568;

}



.container.theme-jieyuan .box-filter-row {

	background: rgba(255, 255, 255, 0.85);

	border-radius: 32rpx;

	padding: 16rpx 20rpx;

	box-shadow: 0 4rpx 16rpx rgba(57, 147, 131, 0.1);

}



.container.theme-jieyuan .filter-btn {

	background: rgba(226, 88, 132, 0.08);

	color: #e25884;

	border-radius: 24rpx;

	font-size: 24rpx;

	padding: 10rpx 24rpx;

	border: 2rpx solid transparent;

	transition: all 0.3s;

}



.container.theme-jieyuan .filter-btn.active {

	background: linear-gradient(135deg, #e25884, #399383);

	color: #fff;

	border-color: transparent;

	box-shadow: 0 4rpx 12rpx rgba(226, 88, 132, 0.3);

}



.container.theme-jieyuan .overview-card,

.container.theme-jieyuan .overview-section {

	background: rgba(255, 255, 255, 0.85);

	border-radius: 32rpx;

	box-shadow: 0 4rpx 16rpx rgba(57, 147, 131, 0.12);

	border: 2rpx solid rgba(57, 147, 131, 0.08);

}



.container.theme-jieyuan .character-card {

	background: rgba(255, 255, 255, 0.9);

	border-radius: 24rpx;

	box-shadow: 0 2rpx 12rpx rgba(57, 147, 131, 0.1);

	border: 2rpx solid rgba(226, 88, 132, 0.08);

	transition: all 0.3s;

}



.container.theme-jieyuan .character-card.owned {

	border-color: rgba(57, 147, 131, 0.25);

	box-shadow: 0 4rpx 16rpx rgba(57, 147, 131, 0.18);

}



.container.theme-jieyuan .character-card .card-name {

	color: #4A5568;

}



.container.theme-jieyuan .character-card.owned .card-name {

	color: #399383;

	font-weight: bold;

}



.container.theme-jieyuan .elite-badge {

	border-radius: 16rpx;

}



.container.theme-jieyuan .elite-badge.elite1 {

	background: linear-gradient(135deg, #FFF0F5, #FFE4E9);

	color: #e25884;

}



.container.theme-jieyuan .elite-badge.elite2 {

	background: linear-gradient(135deg, #F0FFF4, #E0F7EC);

	color: #399383;

}



.container.theme-jieyuan .elite-badge.pair {

	background: linear-gradient(135deg, #e25884, #399383);

	color: #fff;

}



.container.theme-jieyuan .btn-action {

	background: linear-gradient(135deg, #e25884, #399383);

	color: #fff;

	border-radius: 32rpx;

	border: none;

	box-shadow: 0 4rpx 16rpx rgba(226, 88, 132, 0.25);

}



.container.theme-jieyuan .btn-action:active {

	opacity: 0.85;

	transform: scale(0.97);

}



.container.theme-jieyuan .btn-secondary {

	background: rgba(255, 255, 255, 0.9);

	color: #e25884;

	border: 2rpx solid rgba(226, 88, 132, 0.3);

	border-radius: 32rpx;

}



.container.theme-jieyuan .btn-secondary:active {

	background: rgba(226, 88, 132, 0.08);

}



.container.theme-jieyuan .modal-overlay .modal-content {

	background: #FAF3E0;

	border-radius: 32rpx;

	border-top: 6rpx solid;

	border-image: linear-gradient(90deg, #e25884, #399383) 1;

}



.container.theme-jieyuan .modal-overlay .modal-title {

	color: #e25884;

}



.container.theme-jieyuan .modal-overlay .modal-btn-primary {

	background: linear-gradient(135deg, #e25884, #399383);

	color: #fff;

	border-radius: 32rpx;

}



.container.theme-jieyuan .modal-overlay .modal-btn-cancel {

	background: rgba(226, 88, 132, 0.08);

	color: #e25884;

	border: 2rpx solid rgba(226, 88, 132, 0.2);

	border-radius: 32rpx;

}



.container.theme-jieyuan .progress-bar .progress-fill {

	background: linear-gradient(90deg, #e25884, #399383);

	border-radius: 16rpx;

}



.container.theme-jieyuan .progress-bar .progress-bg {

	background: rgba(226, 88, 132, 0.1);

	border-radius: 16rpx;

}



.container.theme-jieyuan .stat-number {

	color: #e25884;

}



.container.theme-jieyuan .stat-label {

	color: #718096;

}



.container.theme-jieyuan .section-title {

	color: #4A5568;

}



.container.theme-jieyuan .tag-wanted {

	background: rgba(226, 88, 132, 0.1);

	color: #e25884;

	border-radius: 16rpx;

}



.container.theme-jieyuan .tag-trade {

	background: rgba(57, 147, 131, 0.1);

	color: #399383;

	border-radius: 16rpx;

}



.container.theme-jieyuan .tab-bar {

	background: rgba(255, 255, 255, 0.9);

	border-top: 2rpx solid rgba(226, 88, 132, 0.1);

}



.container.theme-jieyuan .tab-item.active {

	color: #e25884;

}



.container.theme-jieyuan .tab-item.active::after {

	background: linear-gradient(90deg, #e25884, #399383);

}



/* 界园主题 - 成就弹窗 */

.container.theme-jieyuan .achievement-popup-content {

	background: linear-gradient(135deg, #FFF0F5, #F0FFF4);

	border-radius: 32rpx;

	border: 4rpx solid;

	border-image: linear-gradient(135deg, #e25884, #399383) 1;

	box-shadow: 0 8rpx 32rpx rgba(226, 88, 132, 0.3);

}



.container.theme-jieyuan .achievement-popup-name {

	color: #e25884;

}



.container.theme-jieyuan .achievement-popup-desc {

	color: #4A5568;

}



/* 界园主题 - 角色信息弹窗 */

.container.theme-jieyuan .manage-modal {

	background: #FAF3E0;

	border-radius: 32rpx;

}



.container.theme-jieyuan .manage-modal .manage-title {

	color: #e25884;

}



.container.theme-jieyuan .manage-modal input,

.container.theme-jieyuan .manage-modal textarea {

	background: rgba(255, 255, 255, 0.9);

	border: 2rpx solid rgba(226, 88, 132, 0.15);

	border-radius: 16rpx;

	color: #4A5568;

}



.container.theme-jieyuan .manage-modal .manage-option {

	background: rgba(226, 88, 132, 0.06);

	border-radius: 20rpx;

	border: 2rpx solid transparent;

}



.container.theme-jieyuan .manage-modal .manage-option.active {

	background: rgba(57, 147, 131, 0.1);

	border-color: #399383;

}



/* 界园主题 - 概览页面增强 */

.container.theme-jieyuan .overview-page {

	background: linear-gradient(180deg, #FFF8E7 0%, #FAF3E0 100%);

}



.container.theme-jieyuan .overview-header {

	background: rgba(255, 255, 255, 0.9);

	border-bottom: 2rpx solid rgba(226, 88, 132, 0.1);

}



.container.theme-jieyuan .overview-close-btn {

	background: linear-gradient(135deg, #e25884, #399383);

}



.container.theme-jieyuan .overview-card-item {

	background: rgba(255, 255, 255, 0.9);

	box-shadow: 0 2rpx 12rpx rgba(57, 147, 131, 0.1);

	border: 2rpx solid rgba(226, 88, 132, 0.08);

}



.container.theme-jieyuan .card-value {

	color: #e25884;

}



.container.theme-jieyuan .progress-ring-section,

.container.theme-jieyuan .box-ranking-section,

.container.theme-jieyuan .distribution-section,

.container.theme-jieyuan .timeline-section,

.container.theme-jieyuan .overview-list {

	background: rgba(255, 255, 255, 0.9);

	box-shadow: 0 4rpx 16rpx rgba(57, 147, 131, 0.1);

	border: 2rpx solid rgba(226, 88, 132, 0.06);

}



.container.theme-jieyuan .progress-ring-fill {

	background: conic-gradient(#e25884 0% var(--progress, 0%) #eee var(--progress, 0%) 100%);

}



.container.theme-jieyuan .progress-percent {

	color: #e25884;

}



.container.theme-jieyuan .box-progress-fill {

	background: linear-gradient(90deg, #e25884, #399383);

}



.container.theme-jieyuan .box-progress-fill.complete {

	background: linear-gradient(90deg, #399383, #5BB5A6);

}



.container.theme-jieyuan .dist-bar-elite1 { background: linear-gradient(90deg, #e25884, #F08090); }

.container.theme-jieyuan .dist-bar-elite2 { background: linear-gradient(90deg, #399383, #5BB5A6); }

.container.theme-jieyuan .dist-bar-pair { background: linear-gradient(90deg, #e25884, #399383); }



.container.theme-jieyuan .generate-btn {

	background: linear-gradient(135deg, #e25884, #399383);

	box-shadow: 0 4rpx 16rpx rgba(226, 88, 132, 0.25);

}



.container.theme-jieyuan .section-title {

	color: #4A5568;

}



/* 界园主题 - 概览分享区域 */

.container.theme-jieyuan .share-preview {

	background: linear-gradient(135deg, #FFF0F5, #F0FFF4);

	border-radius: 32rpx;

	border: 4rpx solid rgba(226, 88, 132, 0.15);

}



.container.theme-jieyuan .share-btn {

	background: linear-gradient(135deg, #e25884, #399383);

	color: #fff;

	border-radius: 40rpx;

	box-shadow: 0 6rpx 20rpx rgba(226, 88, 132, 0.3);

}



/* 回到顶部按钮 */

.back-to-top {

	position: fixed;

	right: 30rpx;

	bottom: 120rpx;

	width: 80rpx;

	height: 80rpx;

	border-radius: 50%;

	background: rgba(64, 158, 255, 0.85);

	display: flex;

	align-items: center;

	justify-content: center;

	box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.2);

	z-index: 999;

	transition: opacity 0.3s, transform 0.3s;

}



.back-to-top-icon {

	color: #fff;

	font-size: 36rpx;

	font-weight: bold;

}



.back-to-top:active {

	transform: scale(0.9);

	opacity: 0.8;

}



/* 分享图预览区域 */

.share-pagination {
				display: flex;
				justify-content: space-between;
				align-items: center;
				padding: 20rpx 30rpx;
				background: #ffffff;
				border-top: 1rpx solid #e0e0e0;
			}
			.page-btn {
				background: #409EFF;
				color: #ffffff;
				font-size: 24rpx;
				padding: 12rpx 30rpx;
				border-radius: 8rpx;
				border: none;
			}
			.page-btn[disabled] {
				background: #c0c4cc;
				color: #ffffff;
			}
			.page-indicator {
				font-size: 26rpx;
				color: #333333;
				font-weight: bold;
			}
			.share-image-preview-section {

	height: 100vh;

	background: #f5f7fa;

	display: flex;

	flex-direction: column;

}

.share-preview-scroll {

	flex: 1;

	padding: 20rpx;

	overflow-y: auto;

}

.share-preview-actions {

	padding: 20rpx 40rpx;

	background: #fff;

	box-shadow: 0 -2rpx 10rpx rgba(0,0,0,0.05);

}

.share-preview-header {

	text-align: center;

	padding: 20rpx 0;

}

.share-preview-title {

	font-size: 32rpx;

	font-weight: bold;

	color: #409EFF;

}

.share-preview-img {

	width: 100%;

	border-radius: 16rpx;

	margin: 20rpx 0;

	box-shadow: 0 4rpx 20rpx rgba(0,0,0,0.1);

}

.share-preview-actions {

	display: flex;

	flex-direction: column;

	gap: 20rpx;

	padding: 20rpx 40rpx;

}

.share-action-btn {

	width: 100%;

	height: 88rpx;

	line-height: 88rpx;

	border-radius: 44rpx;

	font-size: 28rpx;

	margin: 10rpx 0;

}

.share-action-btn.primary {

	background: linear-gradient(135deg, #409EFF, #66b1ff);

	color: #fff;

}

.share-action-btn.secondary {

	background: #f5f7fa;

	color: #666;

}




		/* 扫码查盒按钮 */
		.action-btn.secondary {
			/* 保持原有样式 */
		}
		.box-highlight {
			box-shadow: 0 0 0 4rpx #409EFF;
			border-radius: 16rpx;
			animation: boxHighlightFlash 1.2s ease-in-out 2;
		}
		@keyframes boxHighlightFlash {
			0%, 100% { background-color: transparent; }
			50% { background-color: rgba(64, 158, 255, 0.12); }
		}


	/* 隐私协议弹窗 */

/* ===== 左侧抽屉 ===== */
.left-drawer-handle {
	position: fixed;
	left: 0;
	top: 12%;
	z-index: 900;
	width: 44rpx;
	height: 112rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 0 14rpx 14rpx 0;
	background: rgba(127, 127, 127, 0.14);
	box-shadow: 2rpx 2rpx 10rpx rgba(0, 0, 0, 0.08);
}

.left-drawer-handle-icon {
	font-size: 24rpx;
	color: #7a7f85;
	line-height: 1;
}

.left-drawer-mask {
	position: fixed;
	left: 0;
	right: 0;
	top: 0;
	bottom: 0;
	z-index: 1200;
	background: rgba(0, 0, 0, 0.35);
}

.left-drawer {
	position: fixed;
	left: 0;
	top: 0;
	bottom: 0;
	width: 480rpx;
	z-index: 1201;
	padding: calc(var(--status-bar-height, 0px) + 60rpx) 24rpx 40rpx;
	box-sizing: border-box;
	background: #fff;
	border-radius: 0 24rpx 24rpx 0;
	box-shadow: 8rpx 0 30rpx rgba(0, 0, 0, 0.12);
	transform: translateX(-100%);
	transition: transform 0.24s ease;
}

.left-drawer.open {
	transform: translateX(0);
}

.left-drawer-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0 8rpx 22rpx;
	border-bottom: 1rpx solid #f0f0f0;
}

.left-drawer-title {
	font-size: 30rpx;
	font-weight: 600;
	color: #333;
}

.left-drawer-close {
	font-size: 28rpx;
	color: #999;
	padding: 0 8rpx;
}

.left-drawer-item {
	display: flex;
	align-items: center;
	padding: 26rpx 10rpx;
	border-bottom: 1rpx solid #f5f5f5;
}

.left-drawer-item-icon {
	width: 48rpx;
	font-size: 28rpx;
}

.left-drawer-item-text {
	font-size: 27rpx;
	color: #333;
}

.left-drawer-tip {
	display: block;
	margin-top: 22rpx;
	padding: 0 10rpx;
	font-size: 21rpx;
	line-height: 1.6;
	color: #9aa0a6;
}

/* ===== 顶部功能区：整体折叠 / 收进左抽屉 ===== */
.top-region {
	display: block;
}

.top-region-entry {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 16rpx 20rpx;
	margin-bottom: 20rpx;
	background-color: #fff;
	border-radius: 16rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}

.tre-main {
	display: flex;
	align-items: center;
	flex: 1;
	min-width: 0;
}

.tre-icon {
	font-size: 26rpx;
	color: #409EFF;
	margin-right: 12rpx;
}

.tre-text {
	font-size: 24rpx;
	color: #333;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}

.tre-meta {
	font-size: 21rpx;
	color: #9aa0a6;
	margin-left: 12rpx;
	flex-shrink: 0;
}

.tre-action {
	font-size: 22rpx;
	color: #409EFF;
	padding: 6rpx 4rpx 6rpx 16rpx;
	flex-shrink: 0;
}

.top-fold-bar {
	display: flex;
	align-items: center;
	padding: 14rpx 20rpx;
	margin-bottom: 20rpx;
	background-color: #fff;
	border-radius: 16rpx;
	box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}

.top-fold-dot {
	width: 10rpx;
	height: 10rpx;
	border-radius: 50%;
	background-color: #d0d4d9;
	margin-right: 12rpx;
	flex-shrink: 0;
}

.top-fold-dot.on {
	background-color: #409EFF;
}

.top-fold-text {
	flex: 1;
	min-width: 0;
	font-size: 22rpx;
	color: #8a9096;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}

.top-fold-move {
	font-size: 21rpx;
	color: #409EFF;
	padding: 4rpx 12rpx;
	margin-right: 6rpx;
	flex-shrink: 0;
}

.top-fold-arrow {
	font-size: 24rpx;
	line-height: 1;
	color: #9aa0a6;
	transform: rotate(180deg);
	transition: transform 0.2s ease;
	flex-shrink: 0;
}

.top-fold-arrow.up {
	transform: rotate(0deg);
}

/* 折叠 / 抽屉模式下列表高度兜底（实测成功时用行内高度覆盖） */
.boxes-scroll-view.is-top-folded {
	height: calc(100vh - 150rpx);
}

.boxes-scroll-view.is-top-in-drawer {
	height: calc(100vh - 150rpx);
}

/* ===== 左抽屉内的顶部功能区 ===== */
.left-drawer {
	display: flex;
	flex-direction: column;
}

.left-drawer.has-top-fns {
	width: 600rpx;
}

.left-drawer-scroll {
	flex: 1;
	min-height: 0;
}

.drawer-block {
	padding: 18rpx 0 10rpx;
	border-bottom: 1rpx solid #f5f5f5;
}

.drawer-block:last-child {
	border-bottom: none;
}

.drawer-block-title {
	display: block;
	font-size: 21rpx;
	color: #9aa0a6;
	margin: 0 8rpx 14rpx;
	letter-spacing: 1rpx;
}

.drawer-mode-tabs {
	display: flex;
	gap: 12rpx;
	margin-bottom: 14rpx;
}

.drawer-mode-tab {
	flex: 1;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 16rpx 8rpx;
	border-radius: 12rpx;
	background-color: #f8f8f8;
	color: #666;
}

.drawer-mode-tab.active {
	background-color: #409EFF;
	color: #fff;
}

.drawer-mode-tab .mode-icon {
	display: inline;
	font-size: 26rpx;
	margin-bottom: 0;
	margin-right: 8rpx;
}

.drawer-mode-tab .mode-text {
	font-size: 24rpx;
}

.drawer-search {
	margin-bottom: 6rpx;
}

.drawer-search-results {
	max-height: 300rpx;
	margin-bottom: 6rpx;
}

.drawer-ach-row {
	display: flex;
	flex-direction: column;
	gap: 10rpx;
	margin: 8rpx 0 16rpx;
}

.drawer-ach-btn {
	align-self: flex-start;
	height: 68rpx;
	padding: 0 22rpx;
}

.drawer-ach-btn .trophy-icon {
	font-size: 32rpx;
	margin-right: 8rpx;
}

.drawer-ach-btn .achievement-text {
	font-size: 26rpx;
}

.drawer-ach-tip {
	font-size: 20rpx;
	color: #9aa0a6;
	padding-left: 4rpx;
}

.drawer-filters {
	display: flex;
	flex-wrap: wrap;
	gap: 10rpx;
	margin-bottom: 12rpx;
}

.drawer-filters .filter-btn {
	flex: 0 0 auto;
	min-width: 0;
	padding: 12rpx 20rpx;
	font-size: 22rpx;
}

.drawer-stats {
	display: flex;
	flex-wrap: wrap;
	gap: 16rpx;
	margin-bottom: 14rpx;
}

.drawer-stat {
	font-size: 21rpx;
	color: #8a9096;
}

.drawer-elite {
	display: flex;
	gap: 8rpx;
}

.drawer-elite .elite-option {
	padding: 12rpx 0;
}

.drawer-seg {
	display: flex;
	gap: 10rpx;
	margin-bottom: 10rpx;
}

.drawer-seg-item {
	flex: 1;
	text-align: center;
	padding: 16rpx 0;
	border-radius: 12rpx;
	background-color: #f8f8f8;
	font-size: 24rpx;
	color: #666;
}

.drawer-seg-item.active {
	background-color: #409EFF;
	color: #fff;
}

/* ================= 分享展柜海报 ================= */
.shot-layer {
	position: fixed;
	left: 0;
	right: 0;
	top: 0;
	bottom: 0;
	/* 盖住 H5 页面标题栏（uni-page-head z-index 998） */
	z-index: 9999;
	background: #f5f5f5;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding: calc(24rpx + env(safe-area-inset-top)) 24rpx calc(16rpx + env(safe-area-inset-bottom));
}
.shot-page {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
	justify-content: flex-start;
	padding-top: 24rpx;
}
.shot-card {
	width: 100%;
	flex: 0 0 auto;
	min-height: 0;
	box-sizing: border-box;
	background: #ffffff;
	border-radius: 24rpx;
	padding: 36rpx 30rpx 28rpx;
	display: flex;
	flex-direction: column;
}
.shot-head {
	padding-bottom: 24rpx;
	border-bottom: 1rpx solid #ececef;
}
.shot-title {
	display: block;
	font-size: 38rpx;
	font-weight: 700;
	color: #1c1c1e;
}
.shot-sub {
	display: block;
	margin-top: 10rpx;
	font-size: 24rpx;
	color: #8e8e93;
}
.shot-list {
	flex: 0 0 auto;
	min-height: 0;
	display: flex;
	flex-wrap: wrap;
	align-content: flex-start;
	gap: 16rpx;
	margin-top: 26rpx;
}
.shot-note {
	margin-top: 20rpx;
	padding-top: 20rpx;
	border-top: 1rpx solid #ececef;
	display: flex;
	justify-content: space-between;
}
.shot-note-text {
	font-size: 22rpx;
	color: #9a9aa0;
}
.shot-chip {
	display: flex;
	align-items: center;
	gap: 8rpx;
	padding: 10rpx 18rpx;
	border-radius: 12rpx;
	background: #f5f6f8;
	border: 1rpx solid #ececef;
}
.shot-chip.gold {
	background: #fff8e8;
	border-color: #f3dfb6;
}
.shot-chip.more {
	background: #eef1f6;
	border-color: #e4e8ef;
}
.shot-chip-name {
	font-size: 24rpx;
	color: #1c1c1e;
}
.shot-chip.gold .shot-chip-name { color: #8a6112; }
.shot-chip-tag {
	font-size: 18rpx;
	color: #b07d17;
	background: #ffeec7;
	border-radius: 6rpx;
	padding: 0 8rpx;
}
.shot-empty {
	padding: 60rpx 0;
	text-align: center;
}
.shot-empty-text {
	font-size: 25rpx;
	color: #a0a0a8;
}
.shot-bar {
	display: flex;
	gap: 16rpx;
	margin-top: 20rpx;
}
.shot-foot {
	display: flex;
	align-items: center;
	gap: 18rpx;
	margin-top: 22rpx;
	padding: 0 6rpx;
}
.shot-qr {
	width: 140rpx;
	height: 140rpx;
	flex-shrink: 0;
	border-radius: 12rpx;
	background: #ffffff;
}
.shot-foot-text { flex: 1; min-width: 0; }
.shot-foot-title {
	display: block;
	font-size: 24rpx;
	font-weight: 600;
	color: #3a3a40;
}
.shot-foot-sub {
	display: block;
	margin-top: 4rpx;
	font-size: 21rpx;
	color: #9a9aa0;
}

.poster-bar {
	display: flex;
	gap: 16rpx;
	margin-top: 22rpx;
}
.poster-btn {
	flex: 1;
	height: 92rpx;
	border-radius: 46rpx;
	background: #ffffff;
	border: 1rpx solid #e2e2e7;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 27rpx;
	font-weight: 600;
	color: #1c1c1e;
}
.poster-btn.primary {
	background: #1c1c1e;
	border-color: #1c1c1e;
	color: #ffffff;
}
.poster-btn-share {
	padding: 0;
	margin: 0;
	line-height: normal;
	font-size: 27rpx;
}
.poster-btn-share::after { border: none; }
.poster-hint {
	margin-top: 14rpx;
	text-align: center;
	font-size: 20rpx;
	color: #a8a8ae;
}

/* ===================== 出物标记（出 / 换 / 要） ===================== */
.trade-marks {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 3rpx;
	width: 100%;
	margin-top: 4rpx;
}

.trade-mark {
	font-size: 16rpx;
	line-height: 1.4;
	padding: 1rpx 6rpx;
	border-radius: 4rpx;
	white-space: nowrap;
	max-width: 100%;
	overflow: hidden;
	text-overflow: ellipsis;
}

.trade-mark.mk-out,
.item-badge.mk-out {
	color: #b4550f;
	background-color: #fff1e2;
}

.trade-mark.mk-swap,
.item-badge.mk-swap {
	color: #1f6fbf;
	background-color: #e8f2fd;
}

.trade-mark.mk-want,
.item-badge.mk-want {
	color: #7a4bc4;
	background-color: #f3ecfd;
}

.trade-bar-tip {
	flex: 1;
	min-width: 0;
	display: flex;
	align-items: center;
}

.trade-bar-tip-text {
	font-size: 20rpx;
	line-height: 1.4;
	color: #8d908e;
}

/* ===================== 出物编辑层 ===================== */
.trade-sheet-mask {
	position: fixed;
	left: 0;
	top: 0;
	right: 0;
	bottom: 0;
	z-index: 9998;
	background-color: rgba(0, 0, 0, 0.45);
	display: flex;
	align-items: flex-end;
}

.trade-sheet {
	width: 100%;
	box-sizing: border-box;
	max-height: 80vh;
	overflow-y: auto;
	background-color: var(--ak-surface-panel, #ffffff);
	color: var(--ak-text-primary, #1b1d1f);
	border-radius: 24rpx 24rpx 0 0;
	padding: 24rpx 24rpx 28rpx;
	box-shadow: 0 -8rpx 32rpx rgba(0, 0, 0, 0.12);
}

.trade-sheet-head {
	display: flex;
	align-items: center;
	gap: 16rpx;
	padding-bottom: 18rpx;
	border-bottom: 1rpx solid var(--ak-hairline-soft, #ececec);
}

.trade-sheet-avatar {
	width: 88rpx;
	height: 88rpx;
	border-radius: 12rpx;
	flex-shrink: 0;
	background-color: var(--ak-surface-muted, #f2f2f2);
}

.trade-sheet-title-box {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 4rpx;
}

.trade-sheet-name {
	font-size: 30rpx;
	font-weight: 600;
	color: var(--ak-text-primary, #1b1d1f);
}

.trade-sheet-sub {
	font-size: 21rpx;
	color: var(--ak-text-muted, #8d908e);
}

.trade-sheet-close {
	font-size: 40rpx;
	line-height: 1;
	padding: 0 8rpx;
	color: var(--ak-text-muted, #8d908e);
}

.trade-sheet-body {
	padding: 6rpx 0 0;
}

.trade-sheet-row {
	padding: 16rpx 0;
	border-bottom: 1rpx solid var(--ak-hairline-soft, #f0f0f0);
}

.trade-row-head {
	display: flex;
	align-items: center;
	gap: 12rpx;
	margin-bottom: 10rpx;
}

.trade-row-level {
	font-size: 26rpx;
	font-weight: 600;
	width: 76rpx;
	color: var(--ak-text-primary, #1b1d1f);
}

.trade-row-own {
	font-size: 20rpx;
	padding: 2rpx 14rpx;
	border-radius: 999rpx;
	color: var(--ak-text-muted, #8d908e);
	background-color: var(--ak-surface-muted, #f2f2f2);
}

.trade-row-own.on {
	color: #3f9c5a;
	background-color: #eaf7ee;
}

.trade-row-price {
	margin-left: auto;
	font-size: 22rpx;
	font-weight: 600;
	color: #e6a23c;
}

.trade-row-opts {
	display: flex;
	align-items: center;
	gap: 12rpx;
}

.trade-opt {
	width: 76rpx;
	height: 56rpx;
	line-height: 56rpx;
	text-align: center;
	font-size: 24rpx;
	border-radius: 10rpx;
	border: 1rpx solid var(--ak-hairline-soft, #e6e6e6);
	color: var(--ak-text-secondary, #666666);
}

.trade-opt.mk-out {
	color: #b4550f;
	background-color: #fff1e2;
	border-color: #f6d5b3;
}

.trade-opt.mk-swap {
	color: #1f6fbf;
	background-color: #e8f2fd;
	border-color: #c8e0f8;
}

.trade-opt.mk-want {
	color: #7a4bc4;
	background-color: #f3ecfd;
	border-color: #dccbf8;
}

.trade-opt.off {
	opacity: 0.42;
}

.trade-opt.active {
	opacity: 1;
	color: #ffffff;
}

.trade-opt.active.mk-out {
	background-color: #e6a23c;
	border-color: #e6a23c;
}

.trade-opt.active.mk-swap {
	background-color: #409eff;
	border-color: #409eff;
}

.trade-opt.active.mk-want {
	background-color: #9b6bd6;
	border-color: #9b6bd6;
}

.trade-price-field {
	flex: 1;
	min-width: 0;
	height: 56rpx;
	line-height: 56rpx;
	padding: 0 14rpx;
	font-size: 24rpx;
	border-radius: 10rpx;
	box-sizing: border-box;
	background-color: var(--ak-surface-muted, #f5f5f5);
	color: var(--ak-text-primary, #1b1d1f);
	border: none;
	outline: none;
}

.trade-sheet-empty {
	padding: 28rpx 0;
	text-align: center;
	font-size: 24rpx;
	color: var(--ak-text-muted, #8d908e);
}

.trade-sheet-foot {
	padding-top: 18rpx;
}

.trade-sheet-tip {
	display: block;
	font-size: 20rpx;
	line-height: 1.5;
	margin-bottom: 14rpx;
	color: var(--ak-text-muted, #8d908e);
}

.trade-sheet-done {
	width: 100%;
	border: none;
	display: block;
	height: 84rpx;
	line-height: 84rpx;
	text-align: center;
	border-radius: 12rpx;
	font-size: 28rpx;
	background-color: #409eff;
	color: #ffffff;
}

.trade-sheet-done::after {
	border: none;
}

</style>

