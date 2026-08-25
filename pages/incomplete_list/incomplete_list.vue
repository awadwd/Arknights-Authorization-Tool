<template>
  <view class="list-page">
    <!-- 说明 -->
    <view class="notice-bar">
      <text class="notice-text">以下盒号扫码数据不完整，建议优先补充多条码以提升识别率</text>
    </view>

    <!-- 三行三列表格 -->
    <view class="table-wrap">
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
    <view class="summary-bar">
      <text class="summary-text">
        共 {{ totalBoxes }} 盒 · 完全缺失 {{ missing_regular.length + missing_sp.length }} 盒 · 仅单条码 {{ single_regular.length + single_sp.length }} 盒
      </text>
    </view>

    <!-- 提示 -->
    <view class="tip-section">
      <text class="tip-text">点击任意盒号可跳转到反馈页面补充条码数据</text>
    </view>
  </view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
export default {
  data() {
    return {
      // 常规款 - 仅单条码
      single_regular: [
        { bid: '1.0', bname: '', barcode: '6972643691935' },
        { bid: '4.0', bname: '', barcode: '6975194631917' },
        { bid: '12.0', bname: '', barcode: '6975194634116' },
        { bid: '26.0', bname: '', barcode: '6975194634550' },
        { bid: '34.0', bname: '', barcode: '6975194636455' },
        { bid: '35.0', bname: '', barcode: '6975194637476' },
        { bid: '36.0', bname: '', barcode: '6975194637865' },
        { bid: '38.0', bname: '', barcode: '6975194639876' },
        { bid: '43.0', bname: '', barcode: '6977427182036' },
        { bid: '47.0', bname: '', barcode: '6977427186591' },
        { bid: '48.0', bname: '', barcode: '6977427186607' },
        { bid: '49.0', bname: '', barcode: '6977427188847' },
        { bid: '52.0', bname: '', barcode: '6978899534309' },
      ],
      // SP - 仅单条码
      single_sp: [
        { bid: 'CanNot Wait For', bname: '', barcode: '6972643696046' },
        { bid: 'ManiFesto:', bname: '', barcode: '6972643696053' },
        { bid: '前航远歌', bname: '', barcode: '6972643696060' },
        { bid: '灯华梦踏', bname: '', barcode: '6972643696077' },
        { bid: '零号特工', bname: '', barcode: '6972643698262' },
        { bid: '梦人', bname: '', barcode: '6975194638275' },
        { bid: '行者', bname: '', barcode: '6975194638299' },
        { bid: '集成映射', bname: '', barcode: '6977427185105' },
        { bid: '水晶箭行动', bname: '', barcode: '6975194637445' },
        { bid: '无忧梦呓', bname: '', barcode: '6977427188458' },
        { bid: '渡星客', bname: '', barcode: '6977427189967' },
      ],
      // 常规款 - 完全缺失
      missing_regular: [
        { bid: '14.0', bname: '' },
        { bid: '44.0', bname: '' },
        { bid: '53.0', bname: '' },
        { bid: '54.0', bname: '' },
      ],
      // SP - 完全缺失
      missing_sp: [
        { bid: '梦源之地', bname: '' },
        { bid: '浮生听风', bname: '' },
        { bid: '象限解构者', bname: '' },
        { bid: '迷航追踪', bname: '' },
        { bid: '琅环玄机', bname: '' },
        { bid: '踏沙寻影', bname: '' },
        { bid: '缉速狂飙', bname: '' },
        { bid: '战术交汇', bname: '' },
        { bid: '第一期设定集赠品', bname: '' },
        { bid: '嘉年华2026', bname: '' },
      ],
    };
  },
  computed: {
    totalBoxes() {
      return this.single_regular.length + this.single_sp.length +
             this.missing_regular.length + this.missing_sp.length;
    },
  },
  onLoad() {
    uni.setNavigationBarTitle({ title: '数据不全盒号列表' });
  },
  methods: {
    logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
    goToFeedback(item) {
      uni.setStorageSync('feedbackBoxId', item.bid);
      uni.setStorageSync('feedbackBoxName', item.bname || item.bid);
      uni.navigateTo({
        url: '/pages/box_feedback/box_feedback',
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
</style>
