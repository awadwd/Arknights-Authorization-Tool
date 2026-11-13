<template>
  <view class="list-page" :class="'theme-' + themeMode">
    <!-- 说明 -->
    <view class="notice-bar">
      <text class="notice-text">以下盒号扫码数据不完整（按当前盒号数据自动筛选），建议优先补充多条码以提升识别率</text>
    </view>

    <!-- 加载中 -->
    <view v-if="loading" class="il-state">
      <view class="il-ring"></view>
      <text class="il-text">正在读取盒号数据…</text>
    </view>

    <!-- 加载失败 -->
    <view v-else-if="loadError" class="il-state">
      <view class="il-glyph"></view>
      <text class="il-text">{{ loadError }}</text>
      <view class="il-btn" @click="loadData">
        <text class="il-btn-text">重新加载</text>
      </view>
    </view>

    <!-- 没有不完整盒号 -->
    <view v-else-if="!hasData" class="il-state">
      <view class="il-glyph ok"></view>
      <text class="il-text">当前所有盒号的条码数据都完整</text>
    </view>

    <!-- 三行三列表格 -->
    <view v-else class="table-wrap">
      <!-- 表头 -->
      <view class="table-row header">
        <view class="table-cell header-cell" style="width:200rpx;"></view>
        <view class="table-cell header-cell" style="flex:1;">
          <text class="header-text">常规款</text>
        </view>
        <view class="table-cell header-cell" style="flex:1;">
          <text class="header-text">SP / 特别通行认证</text>
        </view>
      </view>

      <!-- 仅单条码行（缺少整盒/单抽/复刻盒） -->
      <view class="table-row">
        <view class="table-cell label-cell">
          <text class="label-text">仅单条码</text>
          <text class="label-sub">缺少整盒/单抽/复刻盒</text>
        </view>
        <!-- 常规款 - 仅单条码 -->
        <view class="table-cell">
          <view class="box-list">
            <view
              v-for="item in single_regular"
              :key="item.bid"
              class="box-item"
              @click="goToFeedback(item)"
            >
              <text class="box-id">{{ item.bid }}</text>
              <text class="box-bc">{{ item.barcode }}</text>
            </view>
            <text v-if="single_regular.length === 0" class="empty-tip">无</text>
          </view>
        </view>
        <!-- SP - 仅单条码 -->
        <view class="table-cell">
          <view class="box-list">
            <view
              v-for="item in single_sp"
              :key="item.bid"
              class="box-item"
              @click="goToFeedback(item)"
            >
              <text class="box-id">{{ item.bid }}</text>
              <text class="box-bc">{{ item.barcode }}</text>
            </view>
            <text v-if="single_sp.length === 0" class="empty-tip">无</text>
          </view>
        </view>
      </view>

      <!-- 数据完全缺失行 -->
      <view class="table-row">
        <view class="table-cell label-cell">
          <text class="label-text">完全缺失</text>
          <text class="label-sub">无任何条码数据</text>
        </view>
        <!-- 常规款 - 完全缺失 -->
        <view class="table-cell">
          <view class="box-list">
            <view
              v-for="item in missing_regular"
              :key="item.bid"
              class="box-item missing"
              @click="goToFeedback(item)"
            >
              <text class="box-id">{{ item.bid }}</text>
            </view>
            <text v-if="missing_regular.length === 0" class="empty-tip">无</text>
          </view>
        </view>
        <!-- SP - 完全缺失 -->
        <view class="table-cell">
          <view class="box-list">
            <view
              v-for="item in missing_sp"
              :key="item.bid"
              class="box-item missing"
              @click="goToFeedback(item)"
            >
              <text class="box-id">{{ item.bid }}</text>
            </view>
            <text v-if="missing_sp.length === 0" class="empty-tip">无</text>
          </view>
        </view>
      </view>
    </view>

    <!-- 统计摘要 -->
    <view v-if="!loading && !loadError && hasData" class="summary-bar">
      <text class="summary-text">
        共 {{ totalBoxes }} 盒 · 完全缺失 {{ missing_regular.length + missing_sp.length }} 盒 · 仅单条码 {{ single_regular.length + single_sp.length }} 盒
      </text>
    </view>

    <!-- 提示 -->
    <view v-if="!loading && !loadError && hasData" class="tip-section">
      <text class="tip-text">点击任意盒号可跳转到反馈页面补充条码数据</text>
    </view>
  </view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
import { markDailyTask } from "@/utils/dailyTask.js";

// 盒号数据源：与「扫码识别」页保持一致
const BOX_DATA_URL = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';

export default {
  data() {
    return {
      // 主题模式
      themeMode: 'simple',
      // 盒号原始数据（本地缓存优先，缺失时远程兜底）
      arknightsData: [],
      loading: true,
      loadError: ''
    };
  },
  computed: {
    /**
     * 从 arknightsData 自动筛选出不完整盒号，不再依赖手工维护的清单。
     * 判定规则（一个盒子的条码数量 = barcodes 数组 + barcode 单值，去重去空）：
     *   · 0 条  → 完全缺失（无任何条码数据）
     *   · 1 条  → 仅单条码（缺少整盒 / 单抽 / 复刻盒条码）
     *   · >1 条 → 数据完整，不在本页展示
     * 分栏规则：Box_id 为纯数字（如 1.0 / 52.0）→ 常规款；文字盒号 → SP / 特别通行认证
     */
    incompleteGroups() {
      const groups = {
        single_regular: [],
        single_sp: [],
        missing_regular: [],
        missing_sp: []
      };
      const list = Array.isArray(this.arknightsData) ? this.arknightsData : [];
      for (let i = 0; i < list.length; i++) {
        const box = list[i] || {};
        const rawId = box.Box_id;
        const bid = String(rawId === undefined || rawId === null ? '' : rawId).trim();
        if (!bid) continue;

        const codes = this.collectBarcodes(box);
        const isRegular = /^\d+(\.\d+)?$/.test(bid);
        const item = {
          bid: bid,
          bname: box.Box_name || '',
          barcode: codes.length ? codes[0] : '',
          barcodes: codes,
          sortKey: isRegular ? parseFloat(bid) : 0
        };

        if (codes.length === 0) {
          (isRegular ? groups.missing_regular : groups.missing_sp).push(item);
        } else if (codes.length === 1) {
          (isRegular ? groups.single_regular : groups.single_sp).push(item);
        }
      }
      // 常规款按盒号数值升序；SP 保持数据原始顺序
      const byNum = (a, b) => a.sortKey - b.sortKey;
      groups.single_regular.sort(byNum);
      groups.missing_regular.sort(byNum);
      return groups;
    },
    single_regular() {
      return this.incompleteGroups.single_regular;
    },
    single_sp() {
      return this.incompleteGroups.single_sp;
    },
    missing_regular() {
      return this.incompleteGroups.missing_regular;
    },
    missing_sp() {
      return this.incompleteGroups.missing_sp;
    },
    totalBoxes() {
      return Array.isArray(this.arknightsData) ? this.arknightsData.length : 0;
    },
    hasData() {
      return this.totalBoxes > 0;
    }
  },
  onLoad() {
    this.loadThemeSetting();
    uni.setNavigationBarTitle({ title: '数据不全盒号列表' });
    this.loadData();
  },
  methods: {
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
    /**
     * 汇总单个盒子的全部条码：barcodes 数组 + barcode 单值，去重、去空、去空格
     */
    collectBarcodes(box) {
      const list = [];
      const seen = {};
      const push = (val) => {
        if (val === undefined || val === null) return;
        const code = String(val).trim().replace(/\s+/g, '');
        if (!code || seen[code]) return;
        seen[code] = true;
        list.push(code);
      };
      const arr = box.barcodes;
      if (Array.isArray(arr)) {
        for (let i = 0; i < arr.length; i++) push(arr[i]);
      }
      push(box.barcode);
      return list;
    },
    /**
     * 数据加载：本地缓存优先（秒开、可离线），无缓存时远程兜底
     */
    loadData() {
      this.loadError = '';
      let local = null;
      try {
        local = uni.getStorageSync('arknightsData');
      } catch (e) {
        this.logError(e, 'incomplete_list.storage');
      }
      if (Array.isArray(local) && local.length > 0) {
        this.arknightsData = local;
        this.loading = false;
        return;
      }
      this.loading = true;
      this.downloadFromGitcode().then((data) => {
        if (Array.isArray(data) && data.length > 0) {
          this.arknightsData = data;
          try {
            uni.setStorageSync('arknightsData', data);
          } catch (e) {
            this.logError(e, 'incomplete_list.cache');
          }
        } else {
          this.loadError = '未获取到盒号数据，请检查网络后重试';
        }
        this.loading = false;
      }).catch((e) => {
        this.logError(e, 'incomplete_list.loadData');
        this.loadError = '盒号数据加载失败，请检查网络后重试';
        this.loading = false;
      });
    },
    downloadFromGitcode() {
      return new Promise((resolve, reject) => {
        uni.request({
          url: BOX_DATA_URL,
          method: 'GET',
          timeout: 15000,
          success: (res) => {
            if (res && res.statusCode === 200 && Array.isArray(res.data)) {
              resolve(res.data);
            } else {
              resolve(null);
            }
          },
          fail: (err) => reject(err)
        });
      });
    },
    goToFeedback(item) {
    // 每日任务打点
    markDailyTask('feedback_try');
      uni.setStorageSync('feedbackBoxId', item.bid);
      uni.setStorageSync('feedbackBoxName', item.bname || item.bid);
      uni.navigateTo({
        url: '/pages/box_feedback_new/box_feedback_new?boxId=' + encodeURIComponent(item.bid),
      });
    },
  },
};
</script>

<style>
.list-page {
  min-height: 100vh;
  background-color: #f5f6f8;
  padding-bottom: 60rpx;
}

.notice-bar {
  padding: 24rpx 30rpx;
  background-color: #fdf6ec;
  border-bottom: 1rpx solid #f5e3c4;
}

.notice-text {
  font-size: 24rpx;
  color: #b38a4a;
  line-height: 1.5;
}

/* 表格 */
.table-wrap {
  margin: 24rpx 20rpx;
  background-color: #fff;
  border-radius: 16rpx;
  overflow: hidden;
  box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.06);
}

.table-row {
  display: flex;
  border-bottom: 1rpx solid #f0f0f0;
}

.table-row:last-child {
  border-bottom: none;
}

.table-row.header {
  background-color: #409EFF;
}

.table-cell {
  padding: 20rpx 16rpx;
  font-size: 24rpx;
  border-right: 1rpx solid #f0f0f0;
  overflow: hidden;
}

.table-cell:last-child {
  border-right: none;
}

.header-cell {
  display: flex;
  align-items: center;
  justify-content: center;
}

.header-text {
  color: #fff;
  font-size: 26rpx;
  font-weight: bold;
  text-align: center;
}

.label-cell {
  width: 200rpx;
  background-color: #f8f8fa;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  border-right: 1rpx solid #e8e8e8 !important;
}

.label-text {
  font-size: 26rpx;
  font-weight: bold;
  color: #333;
  text-align: center;
}

.label-sub {
  font-size: 20rpx;
  color: #999;
  text-align: center;
  margin-top: 6rpx;
}

/* 盒号列表 */
.box-list {
  display: flex;
  flex-direction: column;
  gap: 10rpx;
}

.box-item {
  padding: 12rpx 16rpx;
  background-color: #f5f7fa;
  border-radius: 8rpx;
  border-left: 4rpx solid #409EFF;
}

.box-item.missing {
  border-left-color: #f56c6c;
  background-color: #fef0f0;
}

.box-id {
  display: block;
  font-size: 26rpx;
  font-weight: bold;
  color: #333;
}

.box-bc {
  display: block;
  font-size: 20rpx;
  color: #999;
  margin-top: 4rpx;
  font-family: monospace;
}

.empty-tip {
  font-size: 24rpx;
  color: #c0c4cc;
  text-align: center;
  padding: 20rpx 0;
}

/* 统计 */
.summary-bar {
  margin: 24rpx 20rpx 16rpx;
  padding: 20rpx;
  background-color: #fff;
  border-radius: 12rpx;
  text-align: center;
}

.summary-text {
  font-size: 24rpx;
  color: #666;
}

/* 提示 */
.tip-section {
  margin: 0 20rpx;
  padding: 16rpx 20rpx;
  background-color: #ecf5ff;
  border-radius: 12rpx;
}

.tip-text {
  font-size: 24rpx;
  color: #409EFF;
}

/* ================= 加载 / 失败 / 空态 ================= */
.il-state {
  margin: 40rpx 20rpx;
  padding: 60rpx 30rpx;
  background-color: var(--ak-surface-panel, #ffffff);
  border-radius: 16rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.il-ring {
  width: 48rpx;
  height: 48rpx;
  border-radius: 50%;
  border: 4rpx solid var(--ak-hairline-soft, #e4e7ed);
  border-top-color: var(--ak-signal-info, #409EFF);
  animation: il-spin 0.9s linear infinite;
}

@keyframes il-spin {
  to {
    transform: rotate(360deg);
  }
}

.il-glyph {
  width: 40rpx;
  height: 40rpx;
  border-radius: 50%;
  border: 4rpx solid var(--ak-hairline, #c0c4cc);
  box-sizing: border-box;
}

.il-glyph.ok {
  border-color: var(--ak-color-primary, #67c23a);
}

.il-text {
  margin-top: 24rpx;
  font-size: 26rpx;
  line-height: 1.6;
  text-align: center;
  color: var(--ak-text-muted, #909399);
}

.il-btn {
  margin-top: 30rpx;
  padding: 14rpx 48rpx;
  border-radius: 32rpx;
  background-color: var(--ak-signal-action, #409EFF);
}

.il-btn-text {
  font-size: 26rpx;
  color: var(--ak-text-on-action, #ffffff);
}
</style>
