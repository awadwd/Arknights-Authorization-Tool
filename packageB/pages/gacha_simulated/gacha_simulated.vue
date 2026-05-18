<template>
  <view :class="['container', 'theme-' + themeMode]">
    <!-- 页面标题 -->
    <view class="header">
      <uni-notice-bar scrollable single showIcon text="温馨提示：本功能为模拟抽卡，结果由系统随机生成，仅供娱乐，与现实无关。严禁用于盈利、赌博及其他违法违规行为，违者将永久封禁。"></uni-notice-bar>
    </view>

   <view class="mode-switch" v-if="!battleStarted">
     <text class="mode-label">普通模式</text>
     <switch @change="onModeChange" :checked="battleMode" color="#409EFF" />
     <text class="mode-label">对战模式</text>
   </view>

    <!-- 普通模式 -->
    <view v-if="!battleMode">
      <view class="gacha-section">
        <!-- 分类选择器（混池时隐藏） -->
        <view class="selector-group" v-if="!isSuperMixPool">
          <view class="selector-item">
            <text class="selector-label">选择分类：</text>
            <picker class="type-picker" @change="onTypeChange" :value="selectedTypeIndex" :range="typeOptions" range-key="label">
              <view class="picker-display">
                <text class="picker-text">{{ selectedType ? selectedType.label : '请选择分类' }}</text>
                <text class="picker-arrow">▼</text>
              </view>
            </picker>
          </view>
          <view class="selector-item">
            <text class="selector-label">选择盒号：</text>
            <picker class="box-picker" @change="onBoxChange" :value="selectedBoxIndex" :range="filteredBoxes" range-key="displayName" :disabled="!selectedType">
              <view class="picker-display" :class="{ disabled: !selectedType }">
                <text class="picker-text">{{ selectedBox ? selectedBox.displayName : '请先选择分类' }}</text>
                <text class="picker-arrow">▼</text>
              </view>
            </picker>
          </view>
        </view>

        <!-- 超级大混池选项 -->
        <view class="super-mix-option">
          <view class="checkbox-item">
            <checkbox-group @change="onSuperMixPoolChange">
              <label class="checkbox-label">
                <checkbox :value="1" :checked="isSuperMixPool" color="#409EFF" />
                <text class="checkbox-text">超 级 大 混 池</text>
              </label>
            </checkbox-group>
          </view>
          <view class="mix-filter-options" v-if="isSuperMixPool">
            <view class="filter-title">过滤选项：</view>
            <view class="filter-grid">
              <view class="filter-item">
                <checkbox-group @change="onFilterChange('ambience', $event)">
                  <label class="filter-label">
                    <checkbox :value="1" :checked="!superMixFilters.excludeAmbience" color="#67C23A" />
                    <text class="filter-text">包含音律联觉</text>
                  </label>
                </checkbox-group>
              </view>
              <view class="filter-item">
                <checkbox-group @change="onFilterChange('special', $event)">
                  <label class="filter-label">
                    <checkbox :value="1" :checked="!superMixFilters.excludeSpecial" color="#E6A23C" />
                    <text class="filter-text">包含特别通行证</text>
                  </label>
                </checkbox-group>
              </view>
              <view class="filter-item">
                <checkbox-group @change="onFilterChange('cooperation', $event)">
                  <label class="filter-label">
                    <checkbox :value="1" :checked="!superMixFilters.excludeCooperation" color="#409EFF" />
                    <text class="filter-text">包含联动款</text>
                  </label>
                </checkbox-group>
              </view>
              <view class="filter-item">
                <checkbox-group @change="onFilterChange('whitelist', $event)">
                  <label class="filter-label">
                    <checkbox :value="1" :checked="!superMixFilters.excludeWhitelist" color="#F56C6C" />
                    <text class="filter-text">包含白名单凭证</text>
                  </label>
                </checkbox-group>
              </view>
            </view>
            <view class="filter-stats">
              <text class="filter-stat-text">当前混入盒数: {{ filteredBoxCount }}</text>
              <text class="filter-stat-text">可用卡片数: {{ superMixTotalCards }}</text>
            </view>
          </view>
          <view class="mix-pool-desc" v-if="isSuperMixPool">
            <text class="desc-text">从所有盒号中随机抽取任意一张卡片，不考虑盒号限制</text>
          </view>
        </view>

        <!-- 盒号信息和控制 -->
        <view class="box-controls" v-if="selectedBox && !isSuperMixPool">
          <view class="box-info">
            <text class="box-type">{{ getBoxTypeText(selectedBox.Box_type) }}</text>
            <text class="character-count">剩余: {{ remainingCount }}/{{ totalCount }}</text>
          </view>
          <button class="reset-btn" @click="resetCurrentBox">开新盒</button>
        </view>

        <!-- 超级大混池信息 -->
        <view class="mix-pool-info" v-if="isSuperMixPool">
          <text class="mix-pool-title">超级大混池模式</text>
          <text class="mix-pool-stats">可用卡片数: {{ superMixTotalCards }}</text>
        </view>

        <!-- 抽卡按钮区域 -->
        <view class="gacha-buttons">
          <button class="gacha-btn" :disabled="(!selectedBox && !isSuperMixPool) || (selectedBox && remainingCount === 0) || isAnimating" @click="doGacha">
            {{ gachaButtonText }}
          </button>
          <button v-if="isSuperMixPool" class="gacha-btn gacha-ten-btn" :disabled="superMixTotalCards === 0 || isAnimating" @click="doSuperMixTenGacha">
            混池十连
          </button>
        </view>

        <!-- 单抽结果 -->
        <view class="gacha-result" v-if="showResult">
          <view class="card-container" :class="{ flipping: isAnimating }">
            <view class="card-back" v-if="isAnimating">
              <view class="card-back-content">
                <text class="card-back-text">?</text>
              </view>
            </view>
            <view class="result-card" :class="getEliteClass(currentResult.eliteLevel)" v-if="!isAnimating">
              <image class="character-avatar" :src="currentResult.avatar" mode="aspectFit" @error="onAvatarError(currentResult)"></image>
              <text class="character-name">{{ currentResult.name }}</text>
              <text class="character-elite" v-if="currentResult && currentResult.eliteLevel">{{ getEliteText(currentResult.eliteLevel) }}</text>
              <text class="character-source" v-if="isSuperMixPool">来源: {{ currentResult.sourceBox }}</text>
              <text class="hot-tag" v-if="currentResult.hotcharacter">热门</text>
              <text class="character-price" v-if="currentResult.price">￥{{ currentResult.price }}</text>
            </view>
          </view>
        </view>

        <!-- 历史记录按钮 -->
        <!-- <button class="history-btn" @click="showHistory = true">查看抽卡历史</button> -->
      </view>
    </view>

    <!-- 对战模式界面 -->
    <view v-else class="battle-mode">
      <!-- 公共设置区域 -->
      <view class="battle-setup-area" v-if="!battleStarted">
        <view class="setup-title">对战设置（双方共用同一卡池）</view>
		<view class="setup-waringtitle">注意：部分干员可能没有市价，仅供娱乐，不计入欧非度统计</view>
        <!-- 分类选择器（混池时隐藏） -->
        <view class="selector-group" v-if="!battleIsSuperMixPool">
          <view class="selector-item">
            <text class="selector-label">分类：</text>
            <picker @change="onBattleTypeChange" :value="battleSelectedTypeIndex" :range="typeOptions" range-key="label">
              <view class="picker-display">
                <text>{{ battleSelectedType ? battleSelectedType.label : '请选择分类' }}</text>
              </view>
            </picker>
          </view>
          <view class="selector-item">
            <text class="selector-label">盒号：</text>
            <picker @change="onBattleBoxChange" :value="battleSelectedBoxIndex" :range="battleFilteredBoxes" range-key="displayName" :disabled="!battleSelectedType">
              <view class="picker-display" :class="{ disabled: !battleSelectedType }">
                <text>{{ battleSelectedBox ? battleSelectedBox.displayName : '请先选择分类' }}</text>
              </view>
            </picker>
          </view>
        </view>

        <!-- 超级大混池选项 -->
        <view class="super-mix-option">
          <checkbox-group @change="onBattleSuperMixChange">
            <label class="checkbox-label">
              <checkbox :checked="battleIsSuperMixPool" color="#409EFF" />
              <text>超级大混池</text>
            </label>
          </checkbox-group>
          <view v-if="battleIsSuperMixPool" class="mix-filter-options">
            <view class="filter-grid">
              <view v-for="filter in filterOptions" :key="filter.key" class="filter-item">
                <checkbox-group @change="(e) => onBattleFilterChange(filter.key, e)">
                  <label>
                    <checkbox :checked="!battleSuperMixFilters[filter.excludeKey]" :color="filter.color" />
                    <text class="filter-text">{{ filter.label }}</text>
                  </label>
                </checkbox-group>
              </view>
            </view>
          </view>
        </view>

        <button class="battle-start-btn" @click="startBattle" :disabled="!canStartBattle">开始对战</button>
      </view>

      <!-- 对战进行中界面 -->
      <view v-else class="battle-play-area">
        <!-- 玩家1 (镜像) - 最终修复版 -->
                <view class="player-area player-top" :class="{ mirror: true }">
                  <view class="player-header">
                    <text class="player-title">玩家1 (镜像)</text>
                    <text class="player-status" :class="{ waiting: player1.drew && !player2.drew }">
                      {{ player1.drew ? '已抽' : '未抽' }}
                    </text>
                  </view>
                  <view class="pool-info">剩余: {{ player1.remainingCount }}/{{ player1.totalCount }} (有效卡)</view>
                  <view class="battle-result-area">
                    <view v-if="player1.lastResult" class="result-card battle-card" :class="getEliteClass(player1.lastResult.eliteLevel)">
                      <image class="battle-avatar" :src="player1.lastResult.avatar" mode="aspectFit"></image>
                      <text class="battle-name">{{ player1.lastResult.name }}</text>
                      <text class="battle-elite">{{ getEliteText(player1.lastResult.eliteLevel) }}</text>
                      <text class="battle-price">￥{{ player1.lastResult.price || 0 }}</text>
                    </view>
                  </view>
                  <button class="battle-draw-btn" @click="playerDraw('player1')" :disabled="player1.drew || player1.remainingCount === 0">
                    玩家1抽卡
                  </button>
                </view>

        <!-- 对战结果横幅 -->
        <view class="battle-result-banner" v-if="battleRoundResult">
          <text class="round-result" v-if="battleRoundResult === 'win'">玩家1胜！</text>
          <text class="round-result" v-else-if="battleRoundResult === 'lose'">玩家2胜！</text>
          <text class="round-result" v-else-if="battleRoundResult === 'draw'">平局</text>
        </view>

        <!-- 玩家2 (正常) -->
        <view class="player-area player-bottom">
          <view class="player-header">
            <text class="player-title">玩家2</text>
            <text class="player-status" :class="{ waiting: player2.drew && !player1.drew }">
              {{ player2.drew ? '已抽' : '未抽' }}
            </text>
          </view>
          <view class="pool-info">剩余: {{ player2.remainingCount }}/{{ player2.totalCount }} (有效卡)</view>
          <view class="battle-result-area">
            <view v-if="player2.lastResult" class="result-card battle-card" :class="getEliteClass(player2.lastResult.eliteLevel)">
              <image class="battle-avatar" :src="player2.lastResult.avatar" mode="aspectFit"></image>
              <text class="battle-name">{{ player2.lastResult.name }}</text>
              <text class="battle-elite">{{ getEliteText(player2.lastResult.eliteLevel) }}</text>
              <text class="battle-price">￥{{ player2.lastResult.price || 0 }}</text>
            </view>
          </view>
          <button class="battle-draw-btn" @click="playerDraw('player2')" :disabled="player2.drew || player2.remainingCount === 0">
            玩家2抽卡
          </button>
        </view>
      </view>
    </view>

    <!-- 历史记录按钮（放在最外层） -->
    <button v-if="battleMode && battleStarted" class="history-btn global-history exit-battle-btn" @click="exitBattle">退出对战</button>
    <button v-else class="history-btn global-history" @click="showHistory = true">查看抽卡历史</button>

    <!-- 抽卡历史弹窗（完整版） -->
    <view class="history-modal" v-if="showHistory">
      <view class="modal-mask" @click="showHistory = false"></view>
      <view class="modal-content">
        <view class="modal-header">
          <text class="modal-title">抽卡历史&统计</text>
          <button class="close-btn" @click="showHistory = false">×</button>
        </view>

        <!-- 统计信息面板 -->
        <view class="stats-panel">
          <view class="stats-section">
            <text class="stats-title">抽卡统计</text>
            <view class="stats-grid">
              <view class="stat-item">
                <text class="stat-label">总抽数</text>
                <text class="stat-value">{{ totalDraws }}</text>
              </view>
              <view class="stat-item">
                <text class="stat-label">热门次数</text>
                <text class="stat-value">{{ hotDraws }}</text>
              </view>
              <view class="stat-item">
                <text class="stat-label">混池次数</text>
                <text class="stat-value">{{ superMixDraws }}</text>
              </view>
              <view class="stat-item">
                <text class="stat-label">普通次数</text>
                <text class="stat-value">{{ normalDraws }}</text>
              </view>
            </view>
          </view>

          <!-- 欧非度统计和操作按钮整合 -->
          <view class="stats-section combined-section">
            <view class="combined-header">
              <view class="luck-section">
                <text class="stats-title">欧非度统计</text>
                <view class="luck-display" v-if="qualifiedDraws > 0">
                  <view class="luck-item">
                    <text class="luck-label">当前等级：</text>
                    <view class="luck-value-container" :class="getLuckClass(luckLevel)">
                      <text class="luck-value">{{ luckLevel }} ({{ luckRatio }}%)</text>
                    </view>
                  </view>
                  <view class="luck-details">
                    <text class="luck-detail-text">统计基数：{{ qualifiedDraws }} 次有效抽卡</text>
                    <text class="luck-detail-text">热门率：{{ hotDrawsRate }}%</text>
                  </view>
                </view>
                <view class="luck-display" v-else>
                  <text class="no-data-text">暂无有效记录</text>
                  <text class="no-data-text">(音律联觉、特别通行证、白名单凭证不计入)</text>
                </view>
              </view>

              <view class="action-buttons">
                <button class="share-btn" @click="shareHistory">
                  <text class="share-icon">📤</text>
                  <text class="button-text">分享记录</text>
                </button>
                <button class="clear-btn" @click="clearHistory">
                  <text class="button-text">清空历史</text>
                </button>
              </view>
            </view>
          </view>
        </view>

        <scroll-view class="history-list" scroll-y>
          <view v-for="(record, index) in gachaHistory" :key="index" class="history-item">
            <view class="history-card" :class="getEliteClass(record.eliteLevel)">
              <view class="history-card-header">
                <text class="history-index">#{{ index + 1 }}</text>
                <text class="history-mode-tag" v-if="record.isSuperMixPool">混池</text>
                <text class="history-first-tag" v-if="record.isFirstInBox">首抽</text>
                <text class="history-last-tag" v-if="record.isLastInBox">尾抽</text>
                <text class="history-hot-tag" v-if="record.character.hotcharacter">热门</text>
                <text class="history-exclude-tag" v-if="isExcludedFromStats(record)">不计统计</text>
              </view>
              <image class="history-avatar" :src="record.character.avatar" mode="aspectFit"></image>
              <view class="history-info">
                <text class="history-name">{{ record.character.name }}</text>
                <text class="history-box" v-if="record.isSuperMixPool">来源: 混池</text>
                <text class="history-box" v-else>盒号: {{ record.boxId }}</text>
                <text class="history-elite" v-if="shouldShowEliteLevelInHistory(record)">{{ getEliteText(record.eliteLevel) }}</text>
                <text class="history-time">{{ record.time }}</text>
              </view>
            </view>
          </view>
          <view class="empty-history" v-if="gachaHistory.length === 0">
            <text>暂无抽卡记录</text>
          </view>
        </scroll-view>
      </view>
    </view>

    <!-- 十连结果模态框 -->
    <view class="ten-modal" v-if="showTenGachaModal">
      <view class="modal-mask" @click="closeTenModal"></view>
      <view class="ten-modal-content">
        <view class="ten-modal-header">
          <text class="ten-modal-title">混池十连结果</text>
          <button class="close-btn" @click="closeTenModal">×</button>
        </view>
        <scroll-view class="ten-modal-body" scroll-y>
          <view class="ten-grid" v-if="tenGachaResults.length > 0">
            <view v-for="(card, index) in tenGachaResults" :key="index" class="ten-card-item">
              <view class="result-card small-card" :class="getEliteClass(card.eliteLevel)">
                <image class="character-avatar small-avatar" :src="card.avatar" mode="aspectFit" @error="onAvatarError(card)"></image>
                <text class="character-name small-name">{{ card.name }}</text>
                <text class="character-elite small-elite" v-if="shouldShowEliteLevel(card)">{{ getEliteText(card.eliteLevel) }}</text>
                <text class="hot-tag small-hot" v-if="card.hotcharacter">热门</text>
                <text class="character-price small-price" v-if="card.price">￥{{ card.price }}</text>
              </view>
            </view>
          </view>
          <view v-else class="ten-empty">暂无卡片数据</view>
        </scroll-view>
        <button class="ten-modal-confirm" @click="closeTenModal">确 定</button>
      </view>
    </view>
  </view>
</template>

<script>
export default {
  data() {
    return {
      // 主题模式
      themeMode: 'simple',
      characterData: [],
      availableBoxes: [],
      typeOptions: [
        { label: '全部盒类型', value: 'all' },
        { label: '常规款', value: 'normal' },
        { label: '白名单凭证', value: 'whitelist' },
        { label: '联动款', value: 'cooperation' },
        { label: '音律联觉通行证', value: 'ambience' }
      ],
      selectedTypeIndex: 0,
      selectedType: null,
      selectedBoxIndex: 0,
      selectedBox: null,
      filteredBoxes: [],
      currentResult: null,
      gachaHistory: [],
      showHistory: false,
      defaultAvatar: '/static/default-avatar.png',
      currentBoxPool: [],
      drawnCards: [],
      currentIndex: 0,
      isAnimating: false,
      showResult: false,
      boxStartMarkers: {},
      boxEndMarkers: {},
      isSuperMixPool: false,
      superMixAllCards: [],
      superMixTotalCards: 0,
      superMixFilters: {
        excludeAmbience: false,
        excludeSpecial: false,
        excludeCooperation: false,
        excludeWhitelist: false
      },
      filteredBoxCount: 0,
      tenGachaResults: [],
      showTenGachaModal: false,

      // 对战模式数据
      battleMode: false,
      battleStarted: false,
      battleIsSuperMixPool: false,
      battleSelectedTypeIndex: 0,
      battleSelectedType: null,
      battleSelectedBoxIndex: 0,
      battleSelectedBox: null,
      battleFilteredBoxes: [],
      battleSuperMixFilters: {
        excludeAmbience: false,
        excludeSpecial: false,
        excludeCooperation: false,
        excludeWhitelist: false
      },
      battleSuperMixAllCards: [],
      battlePool: [],
      battleIndex: 0,
      battleTotal: 0,
      player1: { lastResult: null, drew: false, remainingCount: 0, totalCount: 0 },
      player2: { lastResult: null, drew: false, remainingCount: 0, totalCount: 0 },
      battleRoundResult: null,
      filterOptions: [
        { key: 'ambience', excludeKey: 'excludeAmbience', label: '音律联觉', color: '#67C23A' },
        { key: 'special', excludeKey: 'excludeSpecial', label: '特别通行证', color: '#E6A23C' },
        { key: 'cooperation', excludeKey: 'excludeCooperation', label: '联动款', color: '#409EFF' },
        { key: 'whitelist', excludeKey: 'excludeWhitelist', label: '白名单凭证', color: '#F56C6C' }
      ]
    }
  },

  computed: {
    remainingCount() { return this.currentBoxPool.length - this.currentIndex; },
    totalCount() { return this.currentBoxPool.length; },
    gachaButtonText() {
      if (this.isAnimating) return '抽卡中...';
      if (!this.selectedBox && !this.isSuperMixPool) return '请选择盒号或开启混池';
      if (this.selectedBox && this.remainingCount === 0) return '已抽完所有结果';
      if (this.isSuperMixPool) return '混池抽取';
      return `抽取 (${this.remainingCount}/${this.totalCount})`;
    },
    isNoEliteBox() {
      return this.selectedBox && 
        (this.selectedBox.Box_type === 'whitelist' || this.selectedBox.Box_type === 'ambience');
    },
    totalDraws() { return this.gachaHistory.length; },
    hotDraws() { return this.gachaHistory.filter(r => r.character.hotcharacter).length; },
    superMixDraws() { return this.gachaHistory.filter(r => r.isSuperMixPool).length; },
    normalDraws() { return this.gachaHistory.filter(r => !r.isSuperMixPool).length; },
    qualifiedDraws() {
      return this.gachaHistory.filter(r => !['ambience','special','whitelist'].includes(r.boxType)).length;
    },
    qualifiedHotDraws() {
      return this.gachaHistory.filter(r => !['ambience','special','whitelist'].includes(r.boxType) && r.character.hotcharacter).length;
    },
    hotDrawsRate() { return this.totalDraws ? ((this.hotDraws / this.totalDraws) * 100).toFixed(2) : 0; },
    luckRatio() { return this.qualifiedDraws ? ((this.qualifiedHotDraws / this.qualifiedDraws) * 100).toFixed(2) : 0; },
    luckLevel() {
      if (this.qualifiedDraws === 0) return null;
      const ratio = this.qualifiedHotDraws / this.qualifiedDraws;
      if (ratio >= 0.8) return '点石成金欧皇';
      if (ratio >= 0.5) return '欧皇附体';
      if (ratio >= 0.3) return '薛定谔的小欧皇';
      else if (ratio >= 0.1) return '平平淡淡';
      else return '非入骨髓';
    },
    canStartBattle() {
      if (this.battleIsSuperMixPool) {
        return this.battleSuperMixAllCards && this.battleSuperMixAllCards.length > 0;
      } else {
        return this.battleSelectedBox && this.battleFilteredBoxes && this.battleFilteredBoxes.length > 0;
      }
    }
  },

  onLoad() {
    // 加载主题设置
    this.loadThemeSetting();

    uni.setNavigationBarTitle({ title: '通行证模拟抽卡工具' });
    this.loadLocalData();
    this.loadGachaHistory();
    this.selectedType = this.typeOptions[0];
    this.battleSelectedType = this.typeOptions[0];
    wx.showShareMenu({ withShareTicket: true, menus: ['shareAppMessage', 'shareTimeline'] });
  },
  
  onShareAppMessage() {
    return {
      title: '方舟通行证谷子查询工具-模拟抽卡',
      path: '/packageB/pages/gacha_simulated/gacha_simulated',
      imageUrl: ''
    }
  },
  
  onShareTimeline() {
    return {
      title: '方舟通行证谷子查询工具-模拟抽卡',
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

    // 获取欧非度样式类
    getLuckClass(level) {
      if (level === '点石成金欧皇') return 'luck-gold';
      if (level === '欧皇附体') return 'luck-eu';
      if (level === '薛定谔的小欧皇') return 'luck-green';
      if (level === '平平淡淡') return 'luck-normal';
      if (level === '非入骨髓') return 'luck-poor';
      return '';
    },
    
    // 判断记录是否不计入统计
    isExcludedFromStats(record) {
      return ['ambience', 'special', 'whitelist'].includes(record.boxType);
    },
    
    // 判断是否显示精英等级
    shouldShowEliteLevel(card) {
      if (!card) return false;
      // 优先使用卡片自身的 boxType 字段，其次回退到 selectedBox
      const boxType = card.boxType || (this.selectedBox ? this.selectedBox.Box_type : 'normal');
      return boxType === 'normal' || boxType === 'cooperation';
    },
    
    shouldShowEliteLevelInHistory(record) {
      if (!record) return false;
      const boxType = record.boxType || 'normal';
      return boxType === 'normal' || boxType === 'cooperation';
    },
    
    // 加载本地数据
    async loadLocalData() {
      try {
        const data = uni.getStorageSync('arknightsData');
        const enableGuessData = uni.getStorageSync('enableGuessData');
        const guessData = uni.getStorageSync('guessData');
        
        let allData = [];
        if (data && Array.isArray(data) && data.length > 0) allData = [...data];
        if (enableGuessData === 'true' && guessData && Array.isArray(guessData) && guessData.length > 0) {
          allData = [...allData, ...guessData];
        }
        
        if (allData.length > 0) {
          this.characterData = allData;
          this.prepareAvailableBoxes();
          this.filterBoxesByType();
          this.prepareSuperMixAllCards();
        } else {
          uni.showToast({ title: '请先更新干员数据', icon: 'none' });
          setTimeout(() => uni.navigateBack(), 1500);
        }
      } catch (e) {
        console.error('加载数据失败:', e);
        uni.showToast({ title: '数据加载失败', icon: 'none' });
      }
    },

    // 准备可用盒号列表
    prepareAvailableBoxes() {
      this.availableBoxes = this.characterData.map(box => ({
        ...box,
        displayName: `${box.Box_id} (${this.getBoxTypeText(box.Box_type)})`
      }));
    },

    // 准备超级大混池的所有卡片（根据过滤条件）
    prepareSuperMixAllCards() {
      this.superMixAllCards = [];
      let filteredBoxes = [...this.availableBoxes];
      if (this.superMixFilters.excludeAmbience) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'ambience');
      if (this.superMixFilters.excludeSpecial) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'special');
      if (this.superMixFilters.excludeCooperation) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'cooperation');
      if (this.superMixFilters.excludeWhitelist) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'whitelist');
      this.filteredBoxCount = filteredBoxes.length;

      filteredBoxes.forEach(box => {
        const characters = this.getBoxCharacters(box);
        if (box.Box_type === 'whitelist' || box.Box_type === 'ambience' || box.Box_type === 'special') {
          characters.forEach(character => {
            const card = this.createCard(character, 1, box.Box_id, box.Box_type);
            if (card) this.superMixAllCards.push(card);
          });
        } else {
          characters.forEach(character => {
            if (character.nolyELITE1) {
              const card = this.createCard(character, 1, box.Box_id, box.Box_type);
              if (card) this.superMixAllCards.push(card);
            } else {
              const card1 = this.createCard(character, 1, box.Box_id, box.Box_type);
              const card2 = this.createCard(character, 2, box.Box_id, box.Box_type);
              if (card1) this.superMixAllCards.push(card1);
              if (card2) this.superMixAllCards.push(card2);
            }
          });
        }
      });
      this.superMixTotalCards = this.superMixAllCards.length;
    },

    // 辅助函数：根据角色、精英等级生成卡片对象，并设置价格
    createCard(character, eliteLevel, sourceBox, boxType) {
      let price = 0;
      if (character.market_price) {
        if (eliteLevel === 1 && character.market_price.ELITE1 !== undefined) {
          price = character.market_price.ELITE1;
        } else if (eliteLevel === 2 && character.market_price.ELITE2 !== undefined) {
          price = character.market_price.ELITE2;
        }
      }
      return {
        ...character,
        eliteLevel,
        price,
        sourceBox,
        boxType
      };
    },

    // 超级大混池选择变化
    onSuperMixPoolChange(e) {
      this.isSuperMixPool = e.detail.value.length > 0;
      if (this.isSuperMixPool) {
        this.selectedBox = null;
        this.currentResult = null;
        this.showResult = false;
        this.currentBoxPool = [];
        this.currentIndex = 0;
        this.drawnCards = [];
        this.prepareSuperMixAllCards();
      } else {
        if (this.filteredBoxes.length > 0) {
          this.selectedBox = this.filteredBoxes[this.selectedBoxIndex];
          this.loadBoxProgress();
        }
      }
    },

    // 过滤选项变化
    onFilterChange(type, e) {
      const isChecked = e.detail.value.length > 0;
      const filterKey = `exclude${type.charAt(0).toUpperCase() + type.slice(1)}`;
      this.superMixFilters[filterKey] = !isChecked;
      if (this.isSuperMixPool) this.prepareSuperMixAllCards();
    },

    // 根据分类筛选盒号
    filterBoxesByType() {
      if (!this.selectedType) { this.filteredBoxes = []; return; }
      if (this.selectedType.value === 'all') {
        this.filteredBoxes = [...this.availableBoxes];
      } else {
        this.filteredBoxes = this.availableBoxes.filter(box => (box.Box_type || 'normal') === this.selectedType.value);
      }
      this.selectedBoxIndex = 0;
      if (this.filteredBoxes.length > 0) {
        this.selectedBox = this.filteredBoxes[0];
        this.loadBoxProgress();
      } else {
        this.selectedBox = null;
        this.currentResult = null;
        this.showResult = false;
      }
    },

    onTypeChange(e) {
      const index = parseInt(e.detail.value);
      this.selectedTypeIndex = index;
      this.selectedType = this.typeOptions[index];
      this.filterBoxesByType();
    },

    onBoxChange(e) {
      const index = parseInt(e.detail.value);
      this.selectedBoxIndex = index;
      this.selectedBox = this.filteredBoxes[index];
      this.currentResult = null;
      this.showResult = false;
      this.loadBoxProgress();
    },

    // 重置当前盒
    resetCurrentBox() {
      if (!this.selectedBox) return;
      uni.showModal({
        title: '确认开新盒吗',
        content: '确定开启当前盒新盒吗？',
        success: (res) => {
          if (res.confirm) {
            this.markBoxEnd();
            this.initializeBox();
            this.saveBoxProgress();
            this.showResult = false;
            this.markBoxStart();
            uni.showToast({ title: '开新盒成功', icon: 'success' });
          }
        }
      });
    },

    autoResetCurrentBox() {
      if (!this.selectedBox) return;
      this.markBoxEnd();
      this.initializeBox();
      this.saveBoxProgress();
      this.markBoxStart();
      uni.showToast({ title: '本盒已全部抽取完毕，已自动开启新盒', icon: 'success', duration: 3000 });
    },

    initializeBox() {
      if (!this.selectedBox) return;
      const allCards = this.generateAllCards();
      this.currentBoxPool = this.shuffleArray([...allCards]);
      this.currentIndex = 0;
      this.drawnCards = [];
    },

    generateAllCards() {
      if (!this.selectedBox) return [];
      const characters = this.getBoxCharacters(this.selectedBox);
      const allCards = [];
      if (this.isNoEliteBox || this.selectedBox.Box_type === 'special') {
        characters.forEach(character => {
          const card = this.createCard(character, 1, null, this.selectedBox.Box_type);
          if (card) allCards.push(card);
        });
      } else {
        characters.forEach(character => {
          if (character.nolyELITE1) {
            const card = this.createCard(character, 1, null, this.selectedBox.Box_type);
            if (card) allCards.push(card);
          } else {
            const card1 = this.createCard(character, 1, null, this.selectedBox.Box_type);
            const card2 = this.createCard(character, 2, null, this.selectedBox.Box_type);
            if (card1) allCards.push(card1);
            if (card2) allCards.push(card2);
          }
        });
      }
      return allCards;
    },

    shuffleArray(array) {
      const newArray = [...array];
      for (let i = newArray.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [newArray[i], newArray[j]] = [newArray[j], newArray[i]];
      }
      return newArray;
    },

    loadBoxProgress() {
      if (!this.selectedBox) return;
      const boxId = this.selectedBox.Box_id;
      const progressKey = `gacha_progress_${boxId}`;
      const progress = uni.getStorageSync(progressKey);
      if (progress && progress.currentBoxPool && progress.currentIndex !== undefined) {
        this.currentBoxPool = progress.currentBoxPool;
        this.currentIndex = progress.currentIndex;
        this.drawnCards = progress.drawnCards || [];
      } else {
        this.initializeBox();
        this.markBoxStart();
      }
    },

    saveBoxProgress() {
      if (!this.selectedBox) return;
      const boxId = this.selectedBox.Box_id;
      const progressKey = `gacha_progress_${boxId}`;
      const progress = { currentBoxPool: this.currentBoxPool, currentIndex: this.currentIndex, drawnCards: this.drawnCards };
      uni.setStorageSync(progressKey, progress);
    },

    async doGacha() {
      this.tenGachaResults = [];
      if (this.isAnimating) return;
      if (this.isSuperMixPool) {
        await this.doSuperMixGacha();
      } else {
        if (!this.selectedBox || this.currentIndex >= this.currentBoxPool.length) return;
        const isFirstInBox = this.currentIndex === 0;
        const isLastInBox = this.currentIndex === this.currentBoxPool.length - 1;
        this.startGachaAnimation();
        await new Promise(resolve => setTimeout(resolve, 1500));
        const result = this.currentBoxPool[this.currentIndex];
        this.currentIndex++;
        this.currentResult = result;
        this.drawnCards.push(result);
        this.saveBoxProgress();
        this.addToHistory(result, isFirstInBox, isLastInBox, false);
        if (isLastInBox) this.markBoxEnd();
        this.endGachaAnimation();
        if (this.currentIndex >= this.currentBoxPool.length) {
          setTimeout(() => this.autoResetCurrentBox(), 1500);
        }
      }
    },
	
	exitBattle() {
	  uni.showModal({
	    title: '确认退出',
	    content: '确定要退出对战模式吗？当前对战进度将不会保存。',
	    success: (res) => {
	      if (res.confirm) {
	        this.resetBattle();
	      }
	    }
	  });
	},

    async doSuperMixGacha() {
      this.tenGachaResults = [];
      if (this.superMixAllCards.length === 0) {
        uni.showToast({ title: '没有可抽取的卡片，请调整过滤选项', icon: 'none' });
        return;
      }
      this.startGachaAnimation();
      await new Promise(resolve => setTimeout(resolve, 1500));
      const randomIndex = Math.floor(Math.random() * this.superMixAllCards.length);
      const result = { ...this.superMixAllCards[randomIndex] };
      this.currentResult = result;
      this.addToHistory(result, false, false, true);
      this.endGachaAnimation();
    },
	
	// 退出对战，返回设置界面
	exitBattle() {
	  uni.showModal({
	    title: '确认退出',
	    content: '确定要退出对战模式吗？当前对战进度将不会保存。',
	    success: (res) => {
	      if (res.confirm) {
	        this.resetBattle();
	      }
	    }
	  });
	},

    startGachaAnimation() { this.isAnimating = true; this.showResult = true; },
    endGachaAnimation() { this.isAnimating = false; },

    markBoxStart() { if (this.selectedBox) this.boxStartMarkers[this.selectedBox.Box_id] = true; },
    markBoxEnd() { if (this.selectedBox) this.boxEndMarkers[this.selectedBox.Box_id] = true; },

    addToHistory(result, isFirstInBox = false, isLastInBox = false, isSuperMixPool = false) {
      const record = {
        character: result,
        boxId: isSuperMixPool ? '超级大混池' : this.selectedBox.Box_id,
        boxType: isSuperMixPool ? result.boxType : this.selectedBox.Box_type,
        eliteLevel: result.eliteLevel,
        time: this.formatTime(new Date()),
        isFirstInBox, isLastInBox, isSuperMixPool
      };
      this.gachaHistory.unshift(record);
      if (this.gachaHistory.length > 100) this.gachaHistory = this.gachaHistory.slice(0, 100);
      this.saveGachaHistory();
    },

    getEliteText(eliteLevel) { return eliteLevel === 1 ? '精一' : '精二'; },
    getEliteClass(eliteLevel) { return eliteLevel === 1 ? 'elite-one' : 'elite-two'; },

    loadGachaHistory() {
      const history = uni.getStorageSync('gacha_history');
      if (history && Array.isArray(history)) this.gachaHistory = history;
      const boxMarkers = uni.getStorageSync('gacha_box_markers');
      if (boxMarkers) {
        this.boxStartMarkers = boxMarkers.startMarkers || {};
        this.boxEndMarkers = boxMarkers.endMarkers || {};
      }
      const mixFilters = uni.getStorageSync('super_mix_filters');
      if (mixFilters) this.superMixFilters = mixFilters;
    },

    saveGachaHistory() {
      uni.setStorageSync('gacha_history', this.gachaHistory);
      uni.setStorageSync('gacha_box_markers', { startMarkers: this.boxStartMarkers, endMarkers: this.boxEndMarkers });
      uni.setStorageSync('super_mix_filters', this.superMixFilters);
    },

    clearHistory() {
      uni.showModal({
        title: '确认清空',
        content: '确定要清空所有抽卡历史记录吗？',
        success: (res) => {
          if (res.confirm) {
            this.gachaHistory = [];
            this.boxStartMarkers = {};
            this.boxEndMarkers = {};
            uni.removeStorageSync('gacha_history');
            uni.removeStorageSync('gacha_box_markers');
            uni.showToast({ title: '历史记录已清空', icon: 'success' });
          }
        }
      });
    },
    
    shareHistory() {
      if (this.gachaHistory.length === 0) {
        uni.showToast({ title: '暂无抽卡记录', icon: 'none' });
        return;
      }
      let shareText = '方舟通行证模拟抽卡记录\n';
      shareText += `总抽数: ${this.totalDraws}\n热门次数: ${this.hotDraws}\n混池次数: ${this.superMixDraws}\n普通次数: ${this.normalDraws}\n`;
      if (this.qualifiedDraws > 0) {
        shareText += `欧非度: ${this.luckLevel} (${this.luckRatio}%)\n有效统计次数: ${this.qualifiedDraws}\n\n`;
      } else {
        shareText += `欧非度: 暂无有效记录\n\n`;
      }
      shareText += '最近5次抽卡记录:\n';
      const recentRecords = this.gachaHistory.slice(0, 5);
      recentRecords.forEach((record, index) => {
        shareText += `${index + 1}. ${record.character.name} `;
        if (record.character.hotcharacter) shareText += '[热门] ';
        if (record.isSuperMixPool) {
          shareText += `(混池) ${record.time}\n`;
        } else {
          shareText += `(${record.boxId}) ${record.time}\n`;
        }
      });
      shareText += '微信搜索"方舟通行证谷子查询工具"获取更多哟~\n';
      uni.setClipboardData({
        data: shareText,
        success: () => uni.showToast({ title: '记录已复制到剪贴板', icon: 'success' }),
        fail: () => uni.showToast({ title: '复制失败', icon: 'none' })
      });
    },

    formatTime(date) {
      const year = date.getFullYear();
      const month = (date.getMonth() + 1).toString().padStart(2, '0');
      const day = date.getDate().toString().padStart(2, '0');
      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      const seconds = date.getSeconds().toString().padStart(2, '0');
      return `${year}年${month}月${day}日 ${hours}:${minutes}:${seconds}`;
    },

    // 获取盒子的所有干员，并保留原始 market_price 对象
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
            nolyELITE1 = box.Box_type === 'whitelist' || box.Box_type === 'ambience' || box.Box_type === 'special';
            hotcharacter = false;
          } else if (box[charKey].name) {
            characterName = box[charKey].name;
            imageUrl = box[charKey].imageUrl || '';
            nolyELITE1 = box[charKey].nolyELITE1 === true || box.Box_type === 'whitelist' || box.Box_type === 'ambience' || box.Box_type === 'special';
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

    getBoxTypeText(boxType) {
      const typeMap = {
        'whitelist': '白名单凭证',
        'special': '特别通行认证',
        'cooperation': '联动款',
        'ambience': '音律联觉通行证',
        'normal': '常规款',
        'mixed': '超级大混池'
      };
      return typeMap[boxType] || '常规款';
    },

    // ---------- 对战模式方法 ----------
    onModeChange(e) {
      this.battleMode = e.detail.value;
      if (this.battleMode) {
        this.resetBattle();
      } else {
        this.battleStarted = false;
      }
    },

    resetBattle() {
      this.battleStarted = false;
      this.battleIsSuperMixPool = false;
      this.battleSelectedTypeIndex = 0;
      this.battleSelectedType = this.typeOptions[0];
      this.battleSelectedBoxIndex = 0;
      this.battleSelectedBox = null;
      this.battleSuperMixFilters = { excludeAmbience: false, excludeSpecial: false, excludeCooperation: false, excludeWhitelist: false };
      this.battleSuperMixAllCards = [];
      this.battlePool = [];
      this.battleIndex = 0;
      this.battleTotal = 0;
      this.player1 = { lastResult: null, drew: false, remainingCount: 0, totalCount: 0 };
      this.player2 = { lastResult: null, drew: false, remainingCount: 0, totalCount: 0 };
      this.battleRoundResult = null;
      this.filterBattleBoxes();
    },

    filterBattleBoxes() {
      if (!this.battleSelectedType) { this.battleFilteredBoxes = []; return; }
      if (this.battleSelectedType.value === 'all') {
        this.battleFilteredBoxes = [...this.availableBoxes];
      } else {
        this.battleFilteredBoxes = this.availableBoxes.filter(box => (box.Box_type || 'normal') === this.battleSelectedType.value);
      }
      if (this.battleFilteredBoxes.length > 0) {
        this.battleSelectedBoxIndex = 0;
        this.battleSelectedBox = this.battleFilteredBoxes[0];
      } else {
        this.battleSelectedBox = null;
      }
    },

    onBattleTypeChange(e) {
      const index = parseInt(e.detail.value);
      this.battleSelectedTypeIndex = index;
      this.battleSelectedType = this.typeOptions[index];
      this.filterBattleBoxes();
    },

    onBattleBoxChange(e) {
      const index = parseInt(e.detail.value);
      this.battleSelectedBoxIndex = index;
      this.battleSelectedBox = this.battleFilteredBoxes[index];
    },

    onBattleSuperMixChange(e) {
      this.battleIsSuperMixPool = e.detail.value.length > 0;
      if (this.battleIsSuperMixPool) {
        this.battleSelectedBox = null;
        this.prepareBattleSuperMix();
      } else {
        if (this.battleFilteredBoxes.length > 0) {
          this.battleSelectedBox = this.battleFilteredBoxes[this.battleSelectedBoxIndex];
        }
      }
    },

    onBattleFilterChange(type, e) {
      const isChecked = e.detail.value.length > 0;
      const filterKey = `exclude${type.charAt(0).toUpperCase() + type.slice(1)}`;
      this.battleSuperMixFilters[filterKey] = !isChecked;
      if (this.battleIsSuperMixPool) this.prepareBattleSuperMix();
    },

    prepareBattleSuperMix() {
      let filteredBoxes = [...this.availableBoxes];
      if (this.battleSuperMixFilters.excludeAmbience) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'ambience');
      if (this.battleSuperMixFilters.excludeSpecial) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'special');
      if (this.battleSuperMixFilters.excludeCooperation) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'cooperation');
      if (this.battleSuperMixFilters.excludeWhitelist) filteredBoxes = filteredBoxes.filter(b => b.Box_type !== 'whitelist');

      const cards = [];
      filteredBoxes.forEach(box => {
        const chars = this.getBoxCharacters(box);
        if (box.Box_type === 'whitelist' || box.Box_type === 'ambience' || box.Box_type === 'special') {
          chars.forEach(c => {
            const card = this.createCard(c, 1, box.Box_id, box.Box_type);
            if (card) cards.push(card);
          });
        } else {
          chars.forEach(c => {
            if (c.nolyELITE1) {
              const card = this.createCard(c, 1, box.Box_id, box.Box_type);
              if (card) cards.push(card);
            } else {
              const card1 = this.createCard(c, 1, box.Box_id, box.Box_type);
              const card2 = this.createCard(c, 2, box.Box_id, box.Box_type);
              if (card1) cards.push(card1);
              if (card2) cards.push(card2);
            }
          });
        }
      });
      this.battleSuperMixAllCards = cards;
    },

    startBattle() {
      if (this.battleIsSuperMixPool) {
        this.battlePool = this.battleSuperMixAllCards.filter(c => c.price > 0);
        this.battleTotal = this.battlePool.length;
        this.player1.totalCount = this.battleTotal;
        this.player2.totalCount = this.battleTotal;
        this.player1.remainingCount = this.battleTotal;
        this.player2.remainingCount = this.battleTotal;
        this.battleIndex = 0;
      } else {
        const box = this.battleSelectedBox;
        if (!box) return;
        const chars = this.getBoxCharacters(box);
        const allCards = [];
        if (box.Box_type === 'whitelist' || box.Box_type === 'ambience' || box.Box_type === 'special') {
          chars.forEach(c => {
            const card = this.createCard(c, 1, null, box.Box_type);
            if (card) allCards.push(card);
          });
        } else {
          chars.forEach(c => {
            if (c.nolyELITE1) {
              const card = this.createCard(c, 1, null, box.Box_type);
              if (card) allCards.push(card);
            } else {
              const card1 = this.createCard(c, 1, null, box.Box_type);
              const card2 = this.createCard(c, 2, null, box.Box_type);
              if (card1) allCards.push(card1);
              if (card2) allCards.push(card2);
            }
          });
        }
        this.battlePool = this.shuffleArray(allCards.filter(c => c.price > 0));
        this.battleTotal = this.battlePool.length;
        this.battleIndex = 0;
        this.player1.totalCount = this.battleTotal;
        this.player2.totalCount = this.battleTotal;
        this.player1.remainingCount = this.battleTotal;
        this.player2.remainingCount = this.battleTotal;
      }
      this.battleStarted = true;
      this.player1.drew = false;
      this.player2.drew = false;
      this.player1.lastResult = null;
      this.player2.lastResult = null;
      this.battleRoundResult = null;
    },

    async playerDraw(player) {
      if (this.battleStarted && ((player === 'player1' && this.player1.drew) || (player === 'player2' && this.player2.drew))) return;

      let card;
      if (this.battleIsSuperMixPool) {
        const randomIndex = Math.floor(Math.random() * this.battlePool.length);
        card = { ...this.battlePool[randomIndex] };
      } else {
        if (this.battleIndex >= this.battlePool.length) return;
        card = { ...this.battlePool[this.battleIndex] };
        this.battleIndex++;
        this.player1.remainingCount = this.battlePool.length - this.battleIndex;
        this.player2.remainingCount = this.battlePool.length - this.battleIndex;
      }

      if (player === 'player1') {
        this.player1.lastResult = card;
        this.player1.drew = true;
      } else {
        this.player2.lastResult = card;
        this.player2.drew = true;
      }

      if (this.player1.drew && this.player2.drew) {
        await this.showBothResults();
        this.compareAndShowResult();
      }
    },

    async showBothResults() {
      this.isAnimating = true;
      await new Promise(resolve => setTimeout(resolve, 500));
      this.isAnimating = false;
    },

    compareAndShowResult() {
      const price1 = this.player1.lastResult.price || 0;
      const price2 = this.player2.lastResult.price || 0;
      if (price1 > price2) {
        this.battleRoundResult = 'win';
      } else if (price1 < price2) {
        this.battleRoundResult = 'lose';
      } else {
        this.battleRoundResult = 'draw';
      }
      this.player1.drew = false;
      this.player2.drew = false;
    },

    async doSuperMixTenGacha() {
      this.showResult = false;
      this.currentResult = null;
      if (this.superMixAllCards.length === 0) {
        uni.showToast({ title: '没有可抽取的卡片', icon: 'none' });
        return;
      }
      this.tenGachaResults = [];
      for (let i = 0; i < 10; i++) {
        if (i >= this.superMixAllCards.length) break;
        const randomIndex = Math.floor(Math.random() * this.superMixAllCards.length);
        const card = { ...this.superMixAllCards[randomIndex] };
        this.tenGachaResults.push(card);
        this.addToHistory(card, false, false, true);
      }
      this.showTenGachaModal = true;
    },

    closeTenModal() {
      this.showTenGachaModal = false;
      this.tenGachaResults = [];
    },

    onAvatarError(character) {
      character.avatar = this.defaultAvatar;
    }
  }
}
</script>

<style scoped>
/* ========== 基础样式 ========== */
.container {
  padding: 30rpx;
  background-color: #f5f5f5;
  min-height: 100vh;
}
.header {
  text-align: center;
  margin-bottom: 40rpx;
}
.title {
  font-size: 36rpx;
  font-weight: bold;
  color: #333;
}

/* 普通模式样式（保留原样） */
.gacha-section {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 30rpx;
  box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
}
.selector-group {
  margin-bottom: 20rpx;
}
.selector-item {
  display: flex;
  align-items: center;
  margin-bottom: 20rpx;
}
.selector-label {
  font-size: 28rpx;
  color: #333;
  margin-right: 20rpx;
  flex-shrink: 0;
  width: 160rpx;
}
.type-picker,
.box-picker {
  flex: 1;
}
.picker-display {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background-color: #f8f8f8;
  border-radius: 10rpx;
  padding: 20rpx;
  border: 1rpx solid #e0e0e0;
  min-height: 40rpx;
}
.picker-display.disabled {
  background-color: #f5f5f5;
  color: #999;
  border: 1rpx solid #dcdcdc;
}
.picker-text {
  font-size: 28rpx;
  color: #333;
}
.picker-display.disabled .picker-text {
  color: #999;
}
.picker-arrow {
  font-size: 24rpx;
  color: #999;
}

/* 超级大混池选项 */
.super-mix-option {
  margin-bottom: 25rpx;
  padding: 15rpx;
  background-color: #f0f8ff;
  border-radius: 12rpx;
  border-left: 4rpx solid #409EFF;
}
.checkbox-item {
  display: flex;
  align-items: center;
  margin-bottom: 10rpx;
}
.checkbox-label {
  display: flex;
  align-items: center;
}
.checkbox-text {
  font-size: 28rpx;
  color: #333;
  font-weight: bold;
  margin-left: 10rpx;
}
.mix-filter-options {
  padding: 15rpx;
  background-color: #fff;
  border-radius: 8rpx;
  margin-top: 10rpx;
  border: 1rpx solid #e0e0e0;
}
.filter-title {
  font-size: 26rpx;
  color: #333;
  font-weight: 500;
  margin-bottom: 10rpx;
}
.filter-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10rpx;
  margin-bottom: 10rpx;
}
.filter-item {
  display: flex;
  align-items: center;
}
.filter-label {
  display: flex;
  align-items: center;
  width: 100%;
}
.filter-text {
  font-size: 24rpx;
  color: #333;
  margin-left: 8rpx;
}
.filter-stats {
  display: flex;
  justify-content: space-between;
  margin-top: 10rpx;
  padding-top: 10rpx;
  border-top: 1rpx dashed #e0e0e0;
}
.filter-stat-text {
  font-size: 22rpx;
  color: #666;
}
.mix-pool-desc {
  padding-left: 40rpx;
  margin-top: 10rpx;
}
.desc-text {
  font-size: 24rpx;
  color: #666;
  line-height: 1.4;
}

/* 盒号控制区域 */
.box-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30rpx;
  padding: 20rpx;
  background-color: #f8f9fa;
  border-radius: 12rpx;
}
.box-info {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.box-type {
  font-size: 26rpx;
  color: #409EFF;
  font-weight: 500;
}
.character-count {
  font-size: 24rpx;
  color: #666;
}
.reset-btn {
  background-color: #E6A23C;
  color: #fff;
  border-radius: 8rpx;
  font-size: 26rpx;
  padding: 15rpx 25rpx;
  white-space: nowrap;
}

/* 超级大混池信息 */
.mix-pool-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 30rpx;
  padding: 25rpx;
  background: linear-gradient(135deg, #4481eb 0%, #04befe 100%);
  border-radius: 12rpx;
  color: #fff;
  text-align: center;
}
.mix-pool-title {
  font-size: 30rpx;
  font-weight: bold;
  margin-bottom: 10rpx;
  text-shadow: 0 2rpx 4rpx rgba(0, 0, 0, 0.3);
}
.mix-pool-stats {
  font-size: 26rpx;
  opacity: 0.9;
}

/* 抽卡按钮 */
.gacha-btn {
  background-color: #409EFF;
  color: #fff;
  border-radius: 50rpx;
  font-size: 32rpx;
  padding: 25rpx;
  margin-bottom: 30rpx;
  transition: all 0.3s ease;
}
.gacha-btn:disabled {
  background-color: #c0c4cc;
  color: #fff;
}
.gacha-btn:not(:disabled) {
  background-color: #409EFF;
}

/* 抽卡结果 */
.gacha-result {
  margin-bottom: 30rpx;
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400rpx;
}
.card-container {
  width: 300rpx;
  height: 400rpx;
  position: relative;
  perspective: 1000rpx;
  margin: 0 auto;
}
.card-container.flipping {
  animation: flipCard 1.5s ease-in-out;
}
@keyframes flipCard {
  0% {
    transform: rotateY(0deg);
  }
  50% {
    transform: rotateY(90deg);
  }
  100% {
    transform: rotateY(0deg);
  }
}
.card-back {
  width: 100%;
  height: 100%;
  border-radius: 20rpx;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  justify-content: center;
  align-items: center;
  box-shadow: 0 8rpx 30rpx rgba(0, 0, 0, 0.3);
  position: absolute;
  top: 0;
  left: 0;
  backface-visibility: hidden;
}
.card-back-content {
  text-align: center;
}
.card-back-text {
  font-size: 80rpx;
  color: #fff;
  font-weight: bold;
  text-shadow: 0 4rpx 8rpx rgba(0, 0, 0, 0.5);
}
.result-card {
  width: 100%;
  height: 100%;
  border-radius: 20rpx;
  padding: 0rpx;
  text-align: center;
  color: #fff;
  box-shadow: 0 8rpx 30rpx rgba(0, 0, 0, 0.3);
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  backface-visibility: hidden;
}
.result-card.elite-one {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
.result-card.elite-two {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  transform: scale(1.02);
  box-shadow: 0 12rpx 40rpx rgba(245, 87, 108, 0.4);
}
.character-avatar {
  width: 120rpx;
  height: 120rpx;
  border-radius: 50%;
  margin-bottom: 20rpx;
  border: 4rpx solid #fff;
}
.character-name {
  display: block;
  font-size: 32rpx;
  font-weight: bold;
  margin-bottom: 10rpx;
  max-width: 90%;
  word-break: break-all;
}
.character-elite {
  display: block;
  font-size: 28rpx;
  font-weight: bold;
  opacity: 0.9;
  text-shadow: 0 2rpx 4rpx rgba(0, 0, 0, 0.3);
  margin-bottom: 5rpx;
}
.character-source {
  display: block;
  font-size: 24rpx;
  opacity: 0.8;
  margin-bottom: 5rpx;
  text-shadow: 0 1rpx 2rpx rgba(0, 0, 0, 0.3);
}
.hot-tag {
  display: inline-block;
  background-color: #ff4d4f;
  color: #fff;
  font-size: 20rpx;
  padding: 6rpx 12rpx;
  border-radius: 20rpx;
  margin-top: 10rpx;
}
.character-price {
  font-size: 24rpx;
  font-weight: bold;
  color: #ffd700;
  margin-top: 5rpx;
}

/* 历史记录按钮 */
.history-btn {
  background-color: #f0f0f0;
  color: #666;
  border-radius: 50rpx;
  font-size: 28rpx;
  padding: 20rpx;
}

/* 历史记录弹窗 */
.history-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1000;
}
.modal-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
}
.modal-content {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 90%;
  max-width: 650rpx;
  background-color: #fff;
  border-radius: 20rpx;
  overflow: hidden;
  max-height: 85vh;
}
.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 30rpx;
  border-bottom: 1rpx solid #f0f0f0;
  background-color: #409EFF;
}
.modal-title {
  font-size: 32rpx;
  font-weight: bold;
  color: #fff;
}
.close-btn {
  width: 60rpx;
  height: 60rpx;
  border-radius: 50%;
  background-color: #fff;
  color: #409EFF;
  font-size: 36rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 50px;
}
.close-btn:active {
  background-color: #f0f0f0;
}
.stats-panel {
  padding: 20rpx;
  background-color: #f8f9fa;
  border-bottom: 1rpx solid #e0e0e0;
}
.stats-section {
  margin-bottom: 20rpx;
}
.stats-section:last-child {
  margin-bottom: 0;
}
.stats-title {
  display: block;
  font-size: 26rpx;
  font-weight: bold;
  color: #333;
  margin-bottom: 15rpx;
  padding-bottom: 10rpx;
  border-bottom: 2rpx solid #409EFF;
}
.stats-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 15rpx;
}
.stat-item {
  background-color: #fff;
  border-radius: 10rpx;
  padding: 15rpx;
  text-align: center;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.05);
}
.stat-label {
  display: block;
  font-size: 22rpx;
  color: #666;
  margin-bottom: 5rpx;
}
.stat-value {
  display: block;
  font-size: 28rpx;
  font-weight: bold;
  color: #409EFF;
}
.combined-section {
  margin-top: 10rpx;
}
.combined-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20rpx;
}
.luck-section {
  flex: 1;
}
.luck-display {
  background-color: #fff;
  border-radius: 10rpx;
  padding: 15rpx;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.05);
}
.luck-item {
  display: flex;
  align-items: center;
  margin-bottom: 10rpx;
}
.luck-label {
  font-size: 22rpx;
  color: #666;
  margin-right: 10rpx;
}
.luck-value-container {
  padding: 5rpx 12rpx;
  border-radius: 15rpx;
  min-width: 120rpx;
  text-align: center;
}
.luck-value {
  font-size: 24rpx;
  font-weight: bold;
  color: #fff;
}
.luck-gold {
  background: linear-gradient(135deg, #FFD700 0%, #FFA500 100%);
  color: #333;
}
.luck-eu {
  background-color: #ffd700;
  color: #333;
}
.luck-green {
  background-color: #67C23A;
  color: #fff;
}
.luck-normal {
  background-color: #409EFF;
  color: #fff;
}
.luck-poor {
  background-color: #F56C6C;
  color: #fff;
}
.luck-details {
  display: flex;
  flex-direction: column;
  margin-top: 10rpx;
  padding-top: 10rpx;
  border-top: 1rpx dashed #e0e0e0;
}
.luck-detail-text {
  font-size: 20rpx;
  color: #666;
  margin-bottom: 3rpx;
}
.no-data-text {
  font-size: 20rpx;
  color: #999;
  text-align: center;
  padding: 3rpx;
  display: block;
}
.action-buttons {
  display: flex;
  flex-direction: column;
  gap: 10rpx;
  min-width: 180rpx;
}
.share-btn,
.clear-btn {
  border-radius: 8rpx;
  font-size: 24rpx;
  padding: 12rpx 20rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 50rpx;
}
.share-btn {
  background-color: #409EFF;
  color: #fff;
}
.clear-btn {
  background-color: #ff4d4f;
  color: #fff;
}
.share-icon {
  font-size: 22rpx;
  margin-right: 8rpx;
}
.button-text {
  font-size: 24rpx;
}
.history-list {
  max-height: 50vh;
  padding: 20rpx;
}
.history-item {
  margin-bottom: 15rpx;
}
.history-card {
  display: flex;
  align-items: center;
  border-radius: 16rpx;
  padding: 20rpx;
  transition: all 0.2s ease;
  position: relative;
  overflow: hidden;
}
.history-card-header {
  position: absolute;
  top: 5rpx;
  left: 5rpx;
  display: flex;
  gap: 5rpx;
  z-index: 2;
}
.history-index {
  font-size: 20rpx;
  color: #666;
  background-color: rgba(255, 255, 255, 0.8);
  padding: 2rpx 6rpx;
  border-radius: 4rpx;
}
.history-mode-tag {
  font-size: 18rpx;
  color: #fff;
  background-color: #764ba2;
  padding: 2rpx 6rpx;
  border-radius: 4rpx;
}
.history-first-tag {
  font-size: 18rpx;
  color: #fff;
  background-color: #409EFF;
  padding: 2rpx 6rpx;
  border-radius: 4rpx;
}
.history-last-tag {
  font-size: 18rpx;
  color: #fff;
  background-color: #E6A23C;
  padding: 2rpx 6rpx;
  border-radius: 4rpx;
}
.history-hot-tag {
  font-size: 18rpx;
  color: #fff;
  background-color: #ff4d4f;
  padding: 2rpx 6rpx;
  border-radius: 4rpx;
}
.history-exclude-tag {
  font-size: 18rpx;
  color: #fff;
  background-color: #909399;
  padding: 2rpx 6rpx;
  border-radius: 4rpx;
}
.history-card.elite-one {
  background-color: #f0f4ff;
  border-left: 4rpx solid #667eea;
  border-right: 4rpx solid #667eea;
  margin-right: 10px;
}
.history-card.elite-two {
  background-color: #fff0f5;
  border-left: 4rpx solid #f5576c;
  border-right: 4rpx solid #f5576c;
  margin-right: 10px;
}
.history-avatar {
  width: 80rpx;
  height: 80rpx;
  border-radius: 50%;
  margin-right: 20rpx;
  flex-shrink: 0;
}
.history-info {
  flex: 1;
}
.history-name {
  display: block;
  font-size: 28rpx;
  font-weight: bold;
  color: #333;
  margin-bottom: 5rpx;
}
.history-box,
.history-elite,
.history-time {
  display: block;
  font-size: 22rpx;
  color: #666;
  margin-bottom: 2rpx;
}
.history-elite {
  font-weight: bold;
}
.history-card.elite-one .history-elite {
  color: #667eea;
}
.history-card.elite-two .history-elite {
  color: #f5576c;
}
.empty-history {
  text-align: center;
  padding: 60rpx;
  color: #999;
  font-size: 28rpx;
}

/* 模式切换 */
.mode-switch {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 20rpx;
  background-color: #fff;
  margin-bottom: 20rpx;
  border-radius: 16rpx;
}
.mode-label {
  font-size: 28rpx;
  margin: 0 20rpx;
}

/* 抽卡按钮组 */
.gacha-buttons {
  display: flex;
  gap: 20rpx;
  margin-bottom: 30rpx;
}
.gacha-btn {
  flex: 1;
  background-color: #409EFF;
  color: #fff;
  border-radius: 50rpx;
  font-size: 32rpx;
  padding: 25rpx;
  height: 100rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}
/* 混池十连按钮 - 金色渐变 */
.gacha-ten-btn {
  background: linear-gradient(135deg, #FFD700 0%, #FFA500 100%) !important;
  color: #fff !important;
  font-weight: bold !important;
  text-shadow: 0 1rpx 2rpx rgba(0, 0, 0, 0.3);
  box-shadow: 0 4rpx 15rpx rgba(255, 165, 0, 0.4);
}
.gacha-ten-btn:active {
  background: linear-gradient(135deg, #FFC107 0%, #FF9800 100%) !important;
  box-shadow: 0 2rpx 10rpx rgba(255, 165, 0, 0.3);
}

/* 十连模态框 */
.ten-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 2000;
}
.ten-modal .modal-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
}
.ten-modal-content {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 90%;
  max-width: 700rpx;
  background-color: #fff;
  border-radius: 20rpx;
  overflow: hidden;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
}
.ten-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 30rpx;
  border-bottom: 1rpx solid #f0f0f0;
  background-color: #409EFF;
}
.ten-modal-title {
  font-size: 32rpx;
  font-weight: bold;
  color: #fff;
}
.ten-modal-body {
  flex: 1;
  padding: 30rpx;
  overflow-y: auto;
  min-height: 200rpx;
}
.ten-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 20rpx;
  justify-items: center;
  align-items: start;
}
.ten-card-item {
  flex-shrink: 0;            /* 卡片不被压缩 */
  width: 110rpx;             /* 保持固定宽度 */
  display: flex;
  justify-content: center;
}
.small-card {
  width: 110rpx;
  height: 160rpx;
  padding: 10rpx;
  position: relative;
}
.small-avatar {
  width: 60rpx;
  height: 60rpx;
  flex-shrink: 0;            /* 防止被 flex 压缩 */
  border-radius: 50%;        /* 保持圆形 */
}
.small-name {
  font-size: 20rpx;
}
.small-elite {
  font-size: 16rpx;
}
.small-hot {
  font-size: 14rpx;
  padding: 2rpx 6rpx;
}
.small-price {
  font-size: 16rpx;
  margin-top: 2rpx;
}
.ten-empty {
  text-align: center;
  padding: 60rpx 0;
  color: #999;
  font-size: 28rpx;
}
.ten-modal-confirm {
  background-color: #409EFF;
  color: #fff;
  border-radius: 0;
  font-size: 28rpx;
  padding: 25rpx;
  margin: 0;
  border-top: 1rpx solid #e0e0e0;
}

/* ===== 对战模式样式（最终简化版，无通配符） ===== */
.battle-mode { background-color: #fff; border-radius: 16rpx; padding: 20rpx; }
.battle-setup-area { background-color: #fff; border-radius: 16rpx; padding: 30rpx; margin-bottom: 20rpx; }
.setup-title { font-size: 28rpx; font-weight: bold; color: #333; margin-bottom: 20rpx; text-align: center; }
.setup-waringtitle { font-size: 20rpx; font-weight: bold; color: #ff4d4f; margin-bottom: 20rpx; text-align: center; }
.battle-play-area { display: flex; flex-direction: column; gap: 20rpx; }
.player-area { background-color: #fff; border-radius: 16rpx; padding: 20rpx; border: 2rpx solid #e0e0e0; position: relative; }

/* 玩家1镜像：整体垂直翻转，内部所有元素自然倒立（完美镜像） */
.player-top.mirror {
  transform: rotate(180deg) !important;
}
/* 玩家2保持正常 */
.player-bottom {
  transform: none !important;
}

/* 通用内部样式 */
.player-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10rpx; }
.player-title { font-size: 28rpx; font-weight: bold; }
.player-status { font-size: 24rpx; color: #409EFF; }
.player-status.waiting { color: #E6A23C; }
.pool-info { text-align: center; font-size: 24rpx; color: #666; margin: 10rpx 0; }
.battle-result-area { min-height: 280rpx; display: flex; justify-content: center; align-items: center; margin: 10rpx 0; }
.battle-card { width: 200rpx; height: 260rpx; padding: 20rpx; }
.battle-avatar { width: 90rpx; height: 90rpx; border-radius: 50%; margin-bottom: 10rpx; }
.battle-name { font-size: 26rpx; font-weight: bold; }
.battle-elite { font-size: 22rpx; opacity: 0.9; }
.battle-price { font-size: 28rpx; font-weight: bold; color: #ff4d4f; }
.battle-draw-btn {
  background-color: #409EFF;
  color: #fff;
  border-radius: 50rpx;
  font-size: 26rpx;
  padding: 15rpx 0;
  width: 80%;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: center;
}
.battle-result-banner { text-align: center; padding: 10rpx; background-color: #f0f0f0; border-radius: 10rpx; margin: 10rpx 0; }
.round-result { font-size: 32rpx; font-weight: bold; color: #F56C6C; }
.battle-start-btn { background-color: #67C23A; color: #fff; border-radius: 50rpx; font-size: 28rpx; padding: 20rpx; width: 60%; margin: 20rpx auto 0; }
.global-history { margin-top: 20rpx; }
/* 退出对战按钮样式 */
.exit-battle-btn {
  background-color: #F56C6C !important;
  color: #fff !important;
}
.exit-battle-btn:active {
  background-color: #ff7875 !important;
}

/* ========== 科技风格 ========== */
.container.theme-ark {
	background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
}
.container.theme-ark .search-section,
.container.theme-ark .result-card,
.container.theme-ark .history-section {
	background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
	border: 1rpx solid #2A2A4A;
}
.container.theme-ark .search-input-container { background-color: #16162A; border-color: #2A2A4A; }
.container.theme-ark .search-input-container.focus { border-color: #FF6B35; }
.container.theme-ark .search-input { color: #e0e0e0; }
.container.theme-ark .search-icon { filter: brightness(0) invert(1); opacity: 0.7; }
.container.theme-ark .result-name { color: #FF6B35; }
.container.theme-ark .result-info { color: #e0e0e0; }
.container.theme-ark .send-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
.container.theme-ark .clear-btn { background: rgba(255,107,53,0.1); color: #FF6B35; }
.container.theme-ark .history-item { border-bottom-color: #2A2A4A; }
.container.theme-ark .history-name { color: #e0e0e0; }
.container.theme-ark .history-time { color: #888; }

/* ========== 界园风格 ========== */
.container.theme-jieyuan {
	background-color: #FAF3E0;
}
.container.theme-jieyuan .search-section,
.container.theme-jieyuan .result-card,
.container.theme-jieyuan .history-section {
	background: rgba(255, 255, 255, 0.9);
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}
.container.theme-jieyuan .search-input-container { background: rgba(0,0,0,0.05); border-color: rgba(226, 88, 132, 0.2); }
.container.theme-jieyuan .search-input-container.focus { border-color: #e25884; box-shadow: 0 0 10rpx rgba(226, 88, 132, 0.2); }
.container.theme-jieyuan .search-input { color: #333; }
.container.theme-jieyuan .result-name {
	background: linear-gradient(90deg, #e25884, #399383);
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
}
.container.theme-jieyuan .result-info { color: #333; }
.container.theme-jieyuan .send-btn { background: linear-gradient(90deg, #e25884, #399383); color: #fff; border: none; }
.container.theme-jieyuan .clear-btn { background: rgba(226, 88, 132, 0.1); color: #e25884; }
.container.theme-jieyuan .history-item { border-bottom-color: rgba(226, 88, 132, 0.1); }
.container.theme-jieyuan .history-name { color: #333; }
.container.theme-jieyuan .history-time { color: #888; }
</style>
