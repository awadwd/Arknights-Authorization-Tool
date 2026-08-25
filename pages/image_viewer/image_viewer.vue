<template>
  <view class="viewer-page">
    <!-- 自定义导航栏 -->
    <view class="nav-bar">
      <view class="nav-back" @click="goBack">
        <text class="back-icon">‹</text>
      </view>
      <text class="nav-title">图片预览</text>
      <view class="nav-placeholder"></view>
    </view>

    <!-- 图片缩放区域（支持双指缩放） -->
    <movable-area
      class="img-area"
      scale-area="true"
    >
      <movable-view
        class="img-movable"
        direction="all"
        scale="true"
        :scale-min="minScale"
        :scale-max="maxScale"
        :style="{ width: imgWidth + 'px', height: imgHeight + 'px' }"
        @scale="onScale"
      >
        <image
          class="preview-img"
          :src="imageSrc"
          mode="widthFix"
          @load="onImgLoad"
          @error="onImgError"
          :style="{ width: imgWidth + 'px', height: imgHeight + 'px' }"
          :show-menu-by-longpress="false"
        ></image>
        <!-- 图片底部红色版权声明 -->
        <view class="copyright-bar" :style="{ width: imgWidth + 'px' }">
          <text class="copyright-text">以上部分图片素材来源于网络 如有侵权请联系2726269007@qq.com删除</text>
        </view>
      </movable-view>
    </movable-area>

    <!-- 全屏水印层 -->
    <view class="watermark-layer">
      <!-- 多行斜向水印 -->
      <view
        class="watermark-row"
        v-for="row in watermarkRows"
        :key="row"
        :style="{ top: (row * 120) + 'px' }"
      >
        <text class="watermark-text" v-for="col in watermarkCols" :key="col">
          <text class="wm-line">{{ WATERMARK_LINES[0] }}</text>
          <text class="wm-line">{{ WATERMARK_LINES[1] }}</text>
        </text>
      </view>
    </view>

    <!-- 加载中 -->
    <view class="loading-mask" v-if="loading">
      <view class="loading-spinner"></view>
      <text class="loading-text">图片加载中...</text>
    </view>

    <!-- 加载失败 -->
    <view class="error-mask" v-if="loadError">
      <text class="error-icon">⚠</text>
      <text class="error-text">图片加载失败</text>
      <text class="error-hint">请检查网络后重试</text>
      <view class="retry-btn" @click="retryLoad">
        <text>重新加载</text>
      </view>
    </view>

    <!-- 底部操作栏 -->
    <view class="tool-bar" v-if="!loading && !loadError && showToolBar">
      <view class="tool-btn" @click="toggleToolBar">
        <text class="tool-icon">🔻</text>
        <text class="tool-label">沉浸模式</text>
      </view>
      <!-- <view class="tool-btn" @click="saveImage">
        <text class="tool-icon">💾</text>
        <text class="tool-label">保存图片</text>
      </view> -->
      <!-- <view class="tool-btn" @click="openInBrowser">
        <text class="tool-icon">🔗</text>
        <text class="tool-label">原图链接</text>
      </view> -->
    </view>

    <!-- 显示工具栏的浮点（隐藏后点击重新出现） -->
    <view v-if="!loading && !loadError && !showToolBar" class="show-toolbar-btn" @click="toggleToolBar">
      <text class="show-icon">🔺</text>
    </view>
  </view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
export default {
  data() {
    return {
      imageSrc: '',
      imgWidth: 0,
      imgHeight: 0,
      loading: true,
      loadError: false,
      showToolBar: true,
      WATERMARK_LINES: ['方舟通行证谷子查询工具', '本图片不可作为产生纠纷时的证据'],
      // 水印行数/列数由容器大小动态计算
      watermarkRows: 20,
      watermarkCols: 6,
      // 原始图片自然尺寸
      naturalWidth: 702,
      naturalHeight: 2097,
      // 缩放比例
      scale: 1,
      minScale: 0.5,
      maxScale: 5,
    };
  },

  onLoad(options) {
    if (options.src) {
      this.imageSrc = decodeURIComponent(options.src);
    }
    uni.setNavigationBarTitle({ title: '图片预览' });
  },

  onReady() {
    this.calculateWatermarkLayout();
  },

  methods: {
    logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
    goBack() {
      uni.navigateBack();
    },

    onImgLoad(e) {
      this.loading = false;
      this.naturalWidth = e.detail.width;
      this.naturalHeight = e.detail.height;
      // 设置显示尺寸（宽度撑满屏幕）
      const screenWidth = uni.getSystemInfoSync().windowWidth;
      this.imgWidth = screenWidth;
      this.imgHeight = (e.detail.height / e.detail.width) * screenWidth;
      console.log('图片加载成功:', this.naturalWidth, 'x', this.naturalHeight);
    },

    onScale(e) {
      // 记录当前缩放值（movable-view 内部已处理双指缩放手势）
      if (e && e.detail && e.detail.scale) {
        this.scale = e.detail.scale;
      }
    },

    retryLoad() {
      this.loading = true;
      this.loadError = false;
      // 刷新图片 src 触发重新加载
      const ts = Date.now();
      this.imageSrc = this.imageSrc + (this.imageSrc.includes('?') ? '&' : '?') + '_=' + ts;
    },

    toggleToolBar() {
      this.showToolBar = !this.showToolBar;
    },

    saveImage() {
      uni.showLoading({ title: '保存中...' });
      uni.downloadFile({
        url: this.imageSrc,
        success: (res) => {
          uni.saveImageToPhotosAlbum({
            filePath: res.tempFilePath,
            success: () => {
              uni.hideLoading();
              uni.showToast({ title: '已保存到相册', icon: 'success' });
            },
            fail: () => {
              uni.hideLoading();
              uni.showToast({ title: '保存失败，请授权', icon: 'none' });
            },
          });
        },
        fail: () => {
          uni.hideLoading();
          uni.showToast({ title: '下载失败', icon: 'none' });
        },
      });
    },

    openInBrowser() {
      uni.setClipboardData({
        data: this.imageSrc,
        success: () => {
          uni.showToast({ title: '链接已复制，请到浏览器打开', icon: 'none', duration: 2500 });
        },
      });
    },

    calculateWatermarkLayout() {
      // 动态计算水印行列数，覆盖整个屏幕
      const screenWidth = uni.getSystemInfoSync().windowWidth;
      const screenHeight = uni.getSystemInfoSync().windowHeight;
      // 每行高度约 120px，每列宽度约 300px
      this.watermarkRows = Math.ceil(screenHeight / 120) + 2;
      this.watermarkCols = Math.ceil(screenWidth / 300) + 2;
    },
  },
};
</script>

<style>
.viewer-page {
  width: 100vw;
  height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #111;
  overflow: hidden;
}

/* 导航栏 */
.nav-bar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 88rpx;
  background-color: rgba(20, 20, 20, 0.85);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: env(safe-area-inset-top);
  z-index: 100;
}

.nav-back {
  width: 80rpx;
  height: 88rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.back-icon {
  font-size: 56rpx;
  color: #fff;
  line-height: 1;
}

.nav-title {
  font-size: 30rpx;
  color: #fff;
  font-weight: bold;
}

.nav-placeholder {
  width: 80rpx;
}

/* 图片缩放区域 */
.img-area {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100vw;
  height: 100vh;
  z-index: 1;
}

.img-movable {
  display: block;
}

.preview-img {
  display: block;
  /* 宽度撑满，高度自适应 */
  background-color: #f0f0f0;
}

/* 图片底部红色版权声明 */
.copyright-bar {
  background-color: #f0f0f0;
  padding: 24rpx 30rpx 40rpx;
  display: flex;
  justify-content: center;
  align-items: center;
}

.copyright-text {
  font-size: 26rpx;
  color: #ff0000;
  font-weight: bold;
  text-align: center;
  line-height: 1.6;
}

/* 全屏水印层 */
.watermark-layer {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 50;
  pointer-events: none;
  overflow: hidden;
  /* 让水印覆盖整个可滚动区域 */
  width: 200vw;
  height: 400vh;
  margin-left: -50vw;
  margin-top: -50vh;
}

.watermark-row {
  position: absolute;
  left: 0;
  right: 0;
  display: flex;
  flex-direction: row;
  /* 斜向排列 */
  transform: rotate(-30deg);
  transform-origin: center center;
  white-space: nowrap;
}

.watermark-text {
  display: flex;
  flex-direction: column;
  align-items: center;
  font-size: 28rpx;
  color: rgba(255, 255, 255, 0.25);
  padding: 0 40rpx;
  letter-spacing: 4rpx;
  line-height: 1.5;
  /* 加粗描边让水印更醒目 */
  text-shadow:
    0 0 1px rgba(255,255,255,0.5),
    0 0 2px rgba(0,0,0,0.3);
  font-weight: bold;
  word-break: keep-all;
}

.wm-line {
  display: block;
  white-space: nowrap;
}

/* 加载中 */
.loading-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.7);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 200;
}

.loading-spinner {
  width: 60rpx;
  height: 60rpx;
  border: 4rpx solid rgba(255, 255, 255, 0.2);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.loading-text {
  margin-top: 20rpx;
  font-size: 28rpx;
  color: #fff;
}

/* 加载失败 */
.error-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.8);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 200;
}

.error-icon {
  font-size: 80rpx;
  margin-bottom: 20rpx;
}

.error-text {
  font-size: 32rpx;
  color: #fff;
  font-weight: bold;
}

.error-hint {
  font-size: 26rpx;
  color: rgba(255, 255, 255, 0.6);
  margin-top: 12rpx;
}

.retry-btn {
  margin-top: 40rpx;
  padding: 16rpx 60rpx;
  background-color: #409EFF;
  border-radius: 40rpx;
}

.retry-btn text {
  font-size: 28rpx;
  color: #fff;
}

/* 底部操作栏 */
.tool-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  height: 120rpx;
  background-color: rgba(20, 20, 20, 0.9);
  display: flex;
  align-items: center;
  justify-content: space-around;
  padding-bottom: env(safe-area-inset-bottom);
  z-index: 100;
}

.tool-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6rpx;
}

.tool-icon {
  font-size: 40rpx;
}

.tool-label {
  font-size: 22rpx;
  color: rgba(255, 255, 255, 0.7);
}

/* 隐藏后显示的浮点按钮（右下角） */
.show-toolbar-btn {
  position: fixed;
  bottom: 40rpx;
  right: 40rpx;
  width: 80rpx;
  height: 80rpx;
  border-radius: 50%;
  background-color: rgba(20, 20, 20, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  padding-bottom: env(safe-area-inset-bottom);
}

.show-icon {
  font-size: 40rpx;
}
</style>
