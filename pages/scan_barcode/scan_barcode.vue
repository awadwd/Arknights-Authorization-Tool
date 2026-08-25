<template>
  <view :class="['scan-page', 'theme-' + themeMode]">

        <!-- 内测提示横幅 -->
    <view class="beta-banner" v-if="showBetaBanner">
      <text class="beta-icon">🧪</text>
      <text class="beta-text">该功能尚在内测阶段，数据暂时不全，欢迎各位博士们提交问卷反馈~</text>
      <text class="beta-close" @click="showBetaBanner = false">✕</text>
    </view>

    <!-- 扫描区域 -->
    <view class="scan-section">
      <view class="scan-card">
        <image class="scan-icon-img" src="/static/scan-barcode.png" mode="aspectFit"></image>
        <text class="scan-title">扫描条形码</text>
        <text class="scan-desc">对准通行证包装盒上的条形码</text>
        <view class="scan-btn" @click="startScan">
          <text class="btn-text">{{ isScanning ? '扫描中...' : '开始扫描' }}</text>
        </view>
        <view class="manual-input" @click="showManualInput">
          <text class="manual-text">手动输入条码</text>
        </view>
      </view>
    </view>

    <!-- 调试信息 -->
    <view class="debug-info">
      <text class="debug-text">{{ debugInfo }}</text>
    </view>

    <!-- 扫描结果 - 识别成功 -->
    <view class="result-section" v-if="scanResult.show && scanResult.found">
      <view class="result-card found">
        <view class="result-header">
          <text class="result-title">识别成功</text>
          <text class="feedback-link" @click="goToFeedback">
            结果与实际不符？点我去反馈~
          </text>
        </view>
        
        <view class="result-body">
          <text class="barcode-text">条码: {{ scanResult.barcode }}</text>
          <view class="box-info">
            <text class="box-id">盒号 {{ scanResult.boxId }}</text>
            <text class="box-name" v-if="scanResult.boxName">{{ scanResult.boxName }}</text>
          </view>
        </view>

        <!-- 角色卡片列表 -->
        <view class="characters-section" v-if="scanResult.characters && scanResult.characters.length > 0">
          <view class="section-title-row">
            <text class="section-title">盒内角色</text>
            <text class="character-count">{{ scanResult.characters.length }} 位干员</text>
          </view>
          <view class="characters-grid">
            <view 
              v-for="(char, index) in scanResult.characters" 
              :key="index"
              class="character-card"
              @click="goToCharacterDetail(char)"
            >
              <view class="character-avatar-wrap">
                <image 
                  class="character-avatar" 
                  :src="char.imageUrl || char.avatar || '/static/default-avatar.png'" 
                  mode="aspectFit"
                  @error="onCharacterImageError(index)"
                ></image>
                <image 
                  v-if="char.hotcharacter" 
                  class="hot-icon" 
                  src="/static/hot.png" 
                  mode="aspectFit"
                ></image>
              </view>
              <view class="character-info">
                <text class="character-name">{{ char.name }}</text>
                <text v-if="char.nolyELITE1" class="elite-tag">仅精一</text>
              </view>
            </view>
          </view>
        </view>

        <view class="result-actions">
          <view class="action-btn primary" @click="goToBox">
            <text>查看盒详情</text>
          </view>
          <view class="action-btn highlight" @click="goToCardShare">
            <text>🏪 去展柜点亮</text>
          </view>
          <view class="action-btn" @click="clearResult">
            <text>继续扫描</text>
          </view>
        </view>
      </view>
    </view>

    <!-- 扫描结果 - 未收录 -->
    <view class="result-section" v-if="scanResult.show && !scanResult.found">
      <view class="result-card not-found">
        <view class="result-header">
          <text class="result-title">未收录</text>
        </view>
        
        <view class="result-body">
          <text class="barcode-text">条码: {{ scanResult.barcode }}</text>
          <text class="not-found-tip">该条码暂未收录，请通过问卷反馈</text>
        </view>

        <view class="result-actions">
          <view class="action-btn primary" @click="goToFeedback">
            <text>去反馈</text>
          </view>
          <view class="action-btn warning" @click="forceRefreshData">
            <text>刷新数据</text>
          </view>
          <view class="action-btn" @click="clearResult">
            <text>继续扫描</text>
          </view>
        </view>
      </view>
    </view>

    <!-- 最近扫描记录 -->
    <view class="history-section" v-if="scanHistory.length > 0 && !scanResult.show">
      <view class="section-header">
        <text class="section-title">最近扫描</text>
        <text class="clear-btn" @click="clearHistory">清空</text>
      </view>
      <view class="history-list">
        <view
          v-for="(item, index) in scanHistory"
          :key="index"
          class="history-item"
          @click="handleBarcode(item.barcode)"
        >
          <view class="history-main">
            <text class="history-barcode">{{ item.barcode }}</text>
            <text class="history-box" v-if="item.boxId">盒号 {{ item.boxId }}</text>
            <text class="history-box unknown" v-else>未收录</text>
          </view>
          <text class="history-time">{{ formatTime(item.time) }}</text>
        </view>
      </view>
    </view>

    <!-- 底部链接 -->
    <view class="bottom-links">
      <text class="bottom-link" @click="goToIncompleteList">📋 数据不全盒号列表</text>
      <text class="bottom-link" @click="goToGuide">🔍 通行证鉴假指南</text>
    </view>

    <!-- 手动输入弹窗 -->
    <view class="modal-mask" v-if="showInputModal" @click="closeModal">
      <view class="modal-content" @click.stop>
        <view class="modal-header">
          <text class="modal-title">手动输入条码</text>
          <text class="modal-close" @click="closeModal">✕</text>
        </view>
        <view class="modal-body">
          <input
            class="barcode-input"
            v-model="manualBarcode"
            placeholder="请输入13位条形码"
            type="number"
            maxlength="13"
          />
          <text class="input-hint">例如：6977427180407</text>
        </view>
        <view class="modal-footer">
          <view class="modal-btn cancel" @click="closeModal">取消</view>
          <view class="modal-btn confirm" @click="confirmManualInput">确定</view>
        </view>
      </view>
    </view>

  </view>
</template>

<script>
  import errorLog from "@/utils/errorLog.js";
  export default {
    data() {
      return {
        themeMode: 'simple',
        isScanning: false,
        showInputModal: false,
        showBetaBanner: true,
        manualBarcode: '',
        scanHistory: [],
        arknightsData: [],
        barcodeMap: {},
        debugInfo: '',
        scanResult: {
          show: false,
          found: false,
          barcode: '',
          boxId: '',
          boxName: '',
          boxData: null,
          characters: []
        },
        // 隐私协议（官方API）
        showPrivacyModal: false,
        privacyAgreed: true, // 默认true，通过API检查后再更新
        pendingScan: false
      }
    },

    onLoad() {
      this.loadThemeSetting();
      this.loadScanHistory();
      this.loadDataWithAutoUpdate();  // 包含下载+建表，只跑一次
      this.checkPrivacyAgreement();   // 检查隐私协议
    },

    onShow() {
      // 仅刷新调试信息，不重复下载
      this.refreshDebugInfo();
    },

    methods: {
      logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
      loadThemeSetting() {
        try {
          const theme = uni.getStorageSync('themeMode');
          if (theme && ['simple', 'ark', 'jieyuan'].includes(theme)) {
            this.themeMode = theme;
          }
        } catch (e) {
        	this.logError(e);
        	
        }
      },

      async loadDataWithAutoUpdate() {
        const localData = uni.getStorageSync('arknightsData');
        console.log('[本地缓存] 盒数:', localData ? localData.length : 0);
        if (localData && Array.isArray(localData) && localData.length > 0) {
          this.arknightsData = localData;
          this.buildBarcodeMap();
          console.log('从本地加载数据:', localData.length, '盒，条码:', Object.keys(this.barcodeMap).length, '条');
          // 查找本地 3.0
          const local3 = localData.find(b => b.Box_id === '3.0');
          console.log('[本地缓存] 3.0 barcodes:', local3 ? local3.barcodes : 'not found');
          // 统计所有盒的 barcodes 长度
          let totalBytes = 0;
          const boxBarcodesInfo = [];
          for (const box of localData) {
            const arr = box.barcodes || [];
            totalBytes += arr.length;
            if (arr.length > 1) {
              boxBarcodesInfo.push(box.Box_id + ':' + arr.length);
            }
          }
          console.log('[本地缓存] barcodes 总条数:', totalBytes, '多条码盒前 10:', boxBarcodesInfo.slice(0, 10));
        }
        this.refreshDebugInfo();
        this.silentCheckUpdate();
      },
      
      async silentCheckUpdate() {
        try {
          // 直接从 gitcode main 分支下载，绕过知晓云可能的 401 问题
          const data = await this.downloadFromGitcode();
          if (data) {
            const localVersion = uni.getStorageSync('dataVersion');
            const localCount = this.arknightsData.length;
            // 调试：统计远程唯一 barcode 数（与 buildBarcodeMap 一致的去重逻辑）
            const remoteUnique = new Set();
            for (const box of (data || [])) {
              for (const bc of (box.barcodes || [])) {
                if (bc) remoteUnique.add(String(bc).trim().replace(/\s+/g, ''));
              }
              if (box.barcode && typeof box.barcode === 'string') {
                remoteUnique.add(box.barcode.trim().replace(/\s+/g, ''));
              }
            }
            const remoteUniqueCount = remoteUnique.size;
            const localMapSize = Object.keys(this.barcodeMap || {}).length;
            console.log('[silentUpdate] 本地 unique:', localMapSize, '远程 unique:', remoteUniqueCount);
            // 调试：本地数据严重异常（重复太多）也触发更新
            const abnormalLocal = localMapSize > 0 && localMapSize < 100;  // 如 85 条对应老旧数据
            if (!localVersion || localCount === 0 || localMapSize !== remoteUniqueCount || abnormalLocal) {
              console.log('[silentUpdate] 检测到差异或异常，强制更新 → 写入 storage');
              uni.setStorageSync('arknightsData', data);
              uni.setStorageSync('dataVersion', 'gitcode-main');
              uni.setStorageSync('dataUrl', 'https://raw.gitcode.com/.../main/Box_Id.json');
              this.arknightsData = data;
              this.buildBarcodeMap();
              this.refreshDebugInfo();
            } else {
              console.log('[silentUpdate] 本地与远程一致，跳过');
            }
          }
        } catch (e) {
        	this.logError(e);
          console.error('静默检查更新失败:', e);
        }
      },

      refreshDebugInfo() {
        try {
          const v = uni.getStorageSync('dataVersion') || '?';
          const url = uni.getStorageSync('dataUrl') || '?';
          const shortUrl = url.replace('https://', '').substring(0, 60);
          this.debugInfo = `v${v} | 盒:${this.arknightsData.length} | 条码:${Object.keys(this.barcodeMap).length} | ${shortUrl}`;
        } catch (e) {
        	this.logError(e);
        	
        }
      },

      // 强制刷新数据（用于扫描未收录后）
      async forceRefreshData() {
        try {
          uni.showLoading({ title: '更新数据中...' });
          // 直接从 gitcode main 分支下载，绕过知晓云可能的 401 问题
          const data = await this.downloadFromGitcode();
          if (data) {
            // 1. 保存到 storage
            uni.setStorageSync('arknightsData', data);
            uni.setStorageSync('dataVersion', 'gitcode-main');
            uni.setStorageSync('dataUrl', 'https://raw.gitcode.com/.../main/Box_Id.json');
            // 2. 更新内存数据
            this.arknightsData = data;
            // 3. 构建映射表
            this.buildBarcodeMap();
            this.refreshDebugInfo();
            // 4. 调试日志
            console.log('[刷新后] 盒数:', this.arknightsData.length, '条码数:', Object.keys(this.barcodeMap).length);
            const target = '6975194631900';
            const has = !!this.barcodeMap[target];
            console.log('[刷新后] 包含', target, ':', has);
            uni.showToast({ title: '数据已更新', icon: 'success' });
          } else {
            throw new Error('gitcode 下载失败');
          }
        } catch (e) {
        	this.logError(e);
          console.error('强制刷新数据失败:', e);
          uni.showToast({ title: '更新失败', icon: 'none' });
        } finally {
          uni.hideLoading();
        }
      },
      
      async getVersionFromKnowCloud() {
        try {
          const clientId = 'YOUR_KNOW_CLOUD_CLIENT_ID';
          const baseUrl = 'https://YOUR_KNOW_CLOUD_CLIENT_ID.myminapp.com/hserve/v2.2';
          
          const res = await uni.request({
            url: `${baseUrl}/table/Version/record?limit=1&offset=0`,
            method: 'GET',
            header: {
              'X-Hydrogen-Client-ID': clientId,
              'Content-Type': 'application/json'
            },
            timeout: 10000
          });
          
          console.log('[version] status:', res.statusCode, 'objects:', res.data && res.data.objects ? res.data.objects.length : -1);
          
          if (res.statusCode === 200 && res.data && res.data.objects && res.data.objects.length > 0) {
            const versionData = res.data.objects[0];
            return {
              url: versionData.url || '',
              version: versionData.version || versionData.Version || ''
            };
          }
        } catch (error) {
        	this.logError(error);
          console.error('从知晓云获取版本失败:', error);
        }
        // 兜底：使用硬编码 URL（保证数据最新）
        console.log('[version] 知晓云失败，使用硬编码 URL');
        return {
          url: 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json',
          version: 'fallback'
        };
      },

      // 直接下载数据（使用 gitcode main 分支 URL）
      async downloadFromGitcode() {
        try {
          const url = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
          console.log('[gitcode] 下载:', url);
          const res = await uni.request({ url, method: 'GET', timeout: 15000 });
          console.log('[gitcode] status:', res.statusCode);
          if (res.statusCode === 200 && res.data && Array.isArray(res.data)) {
            return res.data;
          }
        } catch (e) {
        	this.logError(e);
          console.error('[gitcode] 下载失败:', e);
        }
        return null;
      },
      
      async downloadAndUpdateData(url, version) {
        if (!url) return;
        
        try {
          console.log('正在下载数据:', url);
          const res = await uni.request({ url, method: 'GET', timeout: 15000 });
          
          if (res.statusCode === 200 && res.data && Array.isArray(res.data)) {
            uni.setStorageSync('arknightsData', res.data);
            uni.setStorageSync('dataVersion', version);
            uni.setStorageSync('dataUrl', url);
            
            this.arknightsData = res.data;
            this.buildBarcodeMap();
            
            console.log('数据自动更新完成:', res.data.length, '盒，条码:', Object.keys(this.barcodeMap).length, '条');
          }
        } catch (e) {
        	this.logError(e);
          console.error('下载数据失败:', e);
        }
      },

      buildBarcodeMap() {
        const map = {};
        let boxCount = 0;
        let multipleBarcodesCount = 0;
        for (const box of this.arknightsData) {
          boxCount++;
          const boxId = box.Box_id;

          const barcodes = box.barcodes || [];
          if (barcodes.length > 1) multipleBarcodesCount++;
          for (const bc of barcodes) {
            if (bc) {
              const cleanBc = String(bc).trim().replace(/\s+/g, '');
              if (cleanBc) {
                map[cleanBc] = {
                  boxId: boxId,
                  boxName: box.Box_name || '',
                  boxData: box
                };
              }
            }
          }

          const singleBarcode = box.barcode;
          if (singleBarcode && typeof singleBarcode === 'string') {
            const cleanSingle = singleBarcode.trim().replace(/\s+/g, '');
            if (cleanSingle) {
              map[cleanSingle] = {
                boxId: boxId,
                boxName: box.Box_name || '',
                boxData: box
              };
            }
          }
        }
        this.barcodeMap = map;
        const keys = Object.keys(map);
        console.log('[buildBarcodeMap] 处理了', boxCount, '盒，映射表:', keys.length, '条，多条码盒:', multipleBarcodesCount);
        // 调试：打印 697519463 开头的所有条码（仅在开发模式）
        const debugKeys = keys.filter(k => k.startsWith('697519463'));
        console.log('[debug] 697519463 开头的条码:', debugKeys.length, '条');
        if (debugKeys.includes('6975194631900')) {
          console.log('[debug] ✅ 找到 6975194631900');
        } else {
          console.log('[debug] ❌ 未找到 6975194631900');
        }
      },

      // 清理扫描结果字符串
      cleanBarcode(barcode) {
        if (!barcode) return '';
        return String(barcode).trim().replace(/\s+/g, '');
      },

      loadScanHistory() {
        try {
          const history = uni.getStorageSync('barcodeScanHistory');
          if (history && Array.isArray(history)) {
            this.scanHistory = history.slice(0, 20);
          }
        } catch (e) {
        	this.logError(e);
        	
        }
      },

      saveScanHistory() {
        try {
          uni.setStorageSync('barcodeScanHistory', this.scanHistory.slice(0, 20));
        } catch (e) {
        	this.logError(e);
        	
        }
      },

      // 检查隐私协议（使用官方API）
      checkPrivacyAgreement() {
        // #ifdef MP-WEIXIN
        if (wx.getPrivacySetting) {
          wx.getPrivacySetting({
            success: (res) => {
              console.log('[隐私协议] 状态:', res);
              // needAuthorization: true 表示需要用户同意隐私协议
              this.privacyAgreed = !res.needAuthorization;
            },
            fail: (err) => {
              console.error('[隐私协议] 获取失败:', err);
              this.privacyAgreed = false;
            }
          });
        }
        // #endif
      },

      // 显示隐私协议弹窗（调用官方 API 强制弹窗）
      showPrivacyDialog() {
        if (typeof wx !== 'undefined' && wx.requirePrivacyAuthorize) {
          wx.requirePrivacyAuthorize({
            success: () => { this.onPrivacyConfirm(); },
            fail: () => { this.onPrivacyCancel(); }
          });
        } else {
          this.onPrivacyConfirm();
        }
      },

      // 同意隐私协议（官方按钮回调）
      onPrivacyConfirm() {
        console.log('[隐私协议] 用户已同意');
        this.privacyAgreed = true;
        this.showPrivacyModal = false;
        // 如果之前有挂起的扫描请求，继续执行
        if (this.pendingScan) {
          this.pendingScan = false;
          this.doStartScan();
        }
      },

      // 拒绝隐私协议
      onPrivacyCancel() {
        this.showPrivacyModal = false;
        this.pendingScan = false;
        uni.showToast({ title: '需要同意隐私协议才能使用扫描功能', icon: 'none' });
      },

      // 开始扫描（带隐私检查）
      startScan() {
        // #ifdef MP-WEIXIN
        // 检查是否需要隐私授权
        if (wx.getPrivacySetting) {
          wx.getPrivacySetting({
            success: (res) => {
              if (res.needAuthorization) {
                // 需要用户同意隐私协议
                this.pendingScan = true;
                this.showPrivacyDialog();
              } else {
                // 已同意，直接扫描
                this.doStartScan();
              }
            },
            fail: () => {
              // API调用失败，尝试直接扫描
              this.doStartScan();
            }
          });
        } else {
          // 不支持隐私API，直接扫描
          this.doStartScan();
        }
        // #endif
        // #ifndef MP-WEIXIN
        this.doStartScan();
        // #endif
      },

      // 实际开始扫描
      doStartScan() {
        if (this.isScanning) return;
        this.isScanning = true;

        uni.scanCode({
          scanType: ['barCode'],
          success: (res) => {
            console.log('扫描结果:', res);
            const barcode = this.cleanBarcode(res.result);
            if (barcode) {
              this.handleBarcode(barcode);
            }
          },
          fail: (err) => {
            console.error('扫描失败:', err);
            if (err.errMsg && !err.errMsg.includes('cancel')) {
              uni.showToast({ title: '扫描失败', icon: 'none' });
            }
          },
          complete: () => {
            this.isScanning = false;
          }
        });
      },

      // 从盒数据中提取角色列表
      extractCharacters(boxData) {
        if (!boxData) return [];
        
        const characters = [];
        for (let i = 1; i <= 10; i++) {
          const charKey = 'character' + i;
          const charData = boxData[charKey];
          
          if (charData) {
            if (typeof charData === 'object' && charData.name) {
              characters.push({
                name: charData.name,
                imageUrl: charData.imageUrl || '',
                avatar: charData.avatar || charData.imageUrl || '',
                hotcharacter: charData.hotcharacter || false,
                nolyELITE1: charData.nolyELITE1 || false,
                market_price: charData.market_price || null
              });
            } else if (typeof charData === 'string' && charData.trim()) {
              characters.push({
                name: charData,
                imageUrl: '',
                avatar: '',
                hotcharacter: false,
                nolyELITE1: false,
                market_price: null
              });
            }
          }
        }
        return characters;
      },

      handleBarcode(barcode) {
        const existingIndex = this.scanHistory.findIndex(h => h.barcode === barcode);
        if (existingIndex >= 0) {
          this.scanHistory.splice(existingIndex, 1);
        }
        
        const boxInfo = this.barcodeMap[barcode];
        const found = !!boxInfo;
        
        this.scanHistory.unshift({
          barcode: barcode,
          boxId: boxInfo ? boxInfo.boxId : null,
          time: Date.now()
        });
        this.saveScanHistory();

        // 提取角色列表
        const characters = found && boxInfo.boxData ? this.extractCharacters(boxInfo.boxData) : [];

        this.scanResult = {
          show: true,
          found: found,
          barcode: barcode,
          boxId: boxInfo ? boxInfo.boxId : '',
          boxName: boxInfo ? boxInfo.boxName : '',
          boxData: boxInfo ? boxInfo.boxData : null,
          characters: characters
        };
      },

      clearResult() {
        this.scanResult = {
          show: false,
          found: false,
          barcode: '',
          boxId: '',
          boxName: '',
          boxData: null,
          characters: []
        };
      },

      goToBox() {
        const boxId = this.scanResult.boxId;
        if (!boxId) return;
        
        uni.setStorageSync('scannedBoxId', boxId);
        
        uni.navigateTo({
          url: '/pages/Search/Search'
        });
      },

      // 跳转到展柜并高亮该盒
      goToCardShare() {
        const boxId = this.scanResult.boxId;
        if (!boxId) return;
        
        uni.setStorageSync('highlightBoxId', boxId);
        
        uni.navigateTo({
          url: '/pages/card_share/card_share'
        });
      },

      // 跳转到数据不全列表
      goToIncompleteList() {
        uni.navigateTo({
          url: '/pages/incomplete_list/incomplete_list'
        });
      },

      // 跳转到鉴假指南
      goToGuide() {
        const url = encodeURIComponent('https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/%E9%80%9A%E8%A1%8C%E8%AF%81%E9%89%B4%E5%81%87%E6%8C%87%E5%8D%97.png');
        uni.navigateTo({
          url: '/pages/image_viewer/image_viewer?src=' + url
        });
      },

      // 跳转到角色详情
      goToCharacterDetail(char) {
        if (!char || !char.name) return;
        
        // 存储角色信息，让 Search 页面可以查询该角色出现的所有盒
        uni.setStorageSync('searchCharacterName', char.name);
        
        uni.navigateTo({
          url: '/pages/Search/Search'
        });
      },

      onCharacterImageError(index) {
        if (this.scanResult.characters[index]) {
          this.scanResult.characters[index].imageUrl = '/static/default-avatar.png';
          this.scanResult.characters[index].avatar = '/static/default-avatar.png';
        }
      },

      goToFeedback() {
        // 跳转到腾讯问卷小程序
        wx.openEmbeddedMiniProgram({
          appId: 'wxebadf544ddae62cb',
          path: 'pages/webview/index?sid=27523743&hash=l0g2&navigateBackMiniProgram=true',
          success: () => {
            console.log('跳转问卷小程序成功');
          },
          fail: (err) => {
            console.error('跳转问卷小程序失败:', err);
            // 失败时回退到原反馈页面
            uni.navigateTo({
              url: '/pages/box_feedback/box_feedback'
            });
          }
        });
      },

      showManualInput() {
        this.manualBarcode = '';
        this.showInputModal = true;
      },

      closeModal() {
        this.showInputModal = false;
      },

      confirmManualInput() {
        const barcode = this.cleanBarcode(this.manualBarcode);
        if (!barcode) {
          uni.showToast({ title: '请输入条码', icon: 'none' });
          return;
        }
        if (!/^\d{13}$/.test(barcode)) {
          uni.showToast({ title: '请输入13位数字条码', icon: 'none' });
          return;
        }
        this.closeModal();
        this.handleBarcode(barcode);
      },

      clearHistory() {
        uni.showModal({
          title: '提示',
          content: '确定清空扫描历史吗？',
          success: (res) => {
            if (res.confirm) {
              this.scanHistory = [];
              this.saveScanHistory();
            }
          }
        });
      },

      formatTime(timestamp) {
        const date = new Date(timestamp);
        const now = new Date();
        const diff = now - date;
        
        if (diff < 60000) return '刚刚';
        if (diff < 3600000) return Math.floor(diff / 60000) + '分钟前';
        if (diff < 86400000) return Math.floor(diff / 3600000) + '小时前';
        
        return `${date.getMonth() + 1}/${date.getDate()} ${date.getHours()}:${String(date.getMinutes()).padStart(2, '0')}`;
      },

      goBack() {
        uni.navigateBack();
      }
    }
  }
</script>

<style>
  .scan-page {
    min-height: 100vh;
    background-color: #f5f6f8;
  }

  /* 内测提示横幅 */
  .beta-banner {
    display: flex;
    align-items: center;
    padding: 20rpx 30rpx;
    background: linear-gradient(135deg, #fff3e0, #ffe0b2);
    border-bottom: 2rpx solid #ffcc80;
  }

  .beta-icon {
    font-size: 32rpx;
    margin-right: 16rpx;
  }

  .beta-text {
    flex: 1;
    font-size: 26rpx;
    color: #e65100;
    line-height: 1.5;
  }

  .beta-close {
    font-size: 32rpx;
    color: #999;
    padding: 10rpx;
    margin-left: 16rpx;
  }

  /* 扫描区域 */
  .scan-section {
    padding: 40rpx 30rpx;
  }

  .scan-card {
    background-color: #ffffff;
    border-radius: 24rpx;
    padding: 60rpx 40rpx;
    text-align: center;
    box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.06);
  }

  .scan-icon-img {
    width: 120rpx;
    height: 120rpx;
    margin-bottom: 30rpx;
  }

  .scan-title {
    font-size: 36rpx;
    font-weight: bold;
    color: #333;
    display: block;
    margin-bottom: 16rpx;
  }

  .scan-desc {
    font-size: 26rpx;
    color: #999;
    display: block;
    margin-bottom: 50rpx;
  }

  .scan-btn {
    background: linear-gradient(135deg, #409EFF, #66b1ff);
    color: #fff;
    padding: 30rpx 80rpx;
    border-radius: 50rpx;
    display: inline-block;
  }

  .btn-text {
    font-size: 32rpx;
    font-weight: bold;
  }

  .manual-input {
    margin-top: 30rpx;
  }

  .manual-text {
    font-size: 26rpx;
    color: #409EFF;
    text-decoration: underline;
  }

  /* 结果区域 */
  .result-section {
    padding: 0 30rpx 30rpx;
  }

  .result-card {
    background-color: #ffffff;
    border-radius: 24rpx;
    padding: 40rpx;
    box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.06);
  }

  .result-card.found {
    border-left: 8rpx solid #67c23a;
  }

  .result-card.not-found {
    border-left: 8rpx solid #f56c6c;
  }

  .result-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 30rpx;
  }

  .result-title {
    font-size: 32rpx;
    font-weight: bold;
    color: #333;
  }

  .feedback-link {
    font-size: 24rpx;
    color: #409EFF;
    text-decoration: underline;
  }

  .result-body {
    margin-bottom: 40rpx;
  }

  .barcode-text {
    font-size: 28rpx;
    color: #666;
    font-family: monospace;
    display: block;
    margin-bottom: 20rpx;
  }

  .box-info {
    display: flex;
    flex-direction: column;
    gap: 10rpx;
  }

  .box-id {
    font-size: 48rpx;
    font-weight: bold;
    color: #409EFF;
  }

  .box-name {
    font-size: 26rpx;
    color: #999;
  }

  .not-found-tip {
    font-size: 28rpx;
    color: #f56c6c;
  }

  /* 角色卡片区域 */
  .characters-section {
    margin: 30rpx 0;
    padding: 30rpx;
    background-color: #f8f9fa;
    border-radius: 16rpx;
  }

  .section-title-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 24rpx;
  }

  .section-title {
    font-size: 30rpx;
    font-weight: bold;
    color: #333;
  }

  .character-count {
    font-size: 24rpx;
    color: #999;
  }

  .characters-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 20rpx;
  }

  .character-card {
    width: calc(20% - 16rpx);
    min-width: 120rpx;
    background-color: #ffffff;
    border-radius: 16rpx;
    padding: 20rpx 10rpx;
    text-align: center;
    box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.05);
    transition: transform 0.2s;
  }

  .character-card:active {
    transform: scale(0.95);
  }

  .character-avatar-wrap {
    position: relative;
    width: 80rpx;
    height: 80rpx;
    margin: 0 auto 12rpx;
  }

  .character-avatar {
    width: 80rpx;
    height: 80rpx;
    border-radius: 50%;
    background-color: #f0f0f0;
  }

  .hot-icon {
    position: absolute;
    top: -6rpx;
    right: -6rpx;
    width: 32rpx;
    height: 32rpx;
  }

  .character-info {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6rpx;
  }

  .character-name {
    font-size: 24rpx;
    color: #333;
    font-weight: 500;
  }

  .elite-tag {
    font-size: 20rpx;
    color: #ff9800;
    background-color: #fff3e0;
    padding: 4rpx 12rpx;
    border-radius: 8rpx;
  }

  /* 操作按钮 */
  .result-actions {
    display: flex;
    gap: 16rpx;
    flex-wrap: wrap;
  }

  .action-btn {
    flex: 1;
    min-width: 160rpx;
    padding: 24rpx 0;
    border-radius: 12rpx;
    text-align: center;
    background-color: #f5f6f8;
  }

  .action-btn.primary {
    background: linear-gradient(135deg, #409EFF, #66b1ff);
    color: #fff;
  }

  .action-btn.highlight {
    background: linear-gradient(135deg, #ff9800, #ffb74d);
    color: #fff;
  }

  .action-btn.warning {
    background: linear-gradient(135deg, #67c23a, #85ce61);
    color: #fff;
  }

  /* 调试信息 */
  .debug-info {
    padding: 10rpx 30rpx;
    background: #f5f5f5;
    margin: 10rpx 30rpx;
    border-radius: 8rpx;
  }

  .debug-text {
    font-size: 20rpx;
    color: #999;
    word-break: break-all;
    font-family: monospace;
  }

  /* 历史记录 */
  .history-section {
    padding: 0 30rpx 30rpx;
  }

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20rpx;
  }

  .history-list {
    background-color: #ffffff;
    border-radius: 16rpx;
    overflow: hidden;
  }

  .history-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 24rpx 30rpx;
    border-bottom: 1rpx solid #f0f0f0;
  }

  .history-item:last-child {
    border-bottom: none;
  }

  .history-main {
    flex: 1;
    display: flex;
    flex-direction: column;
  }

  .history-barcode {
    font-size: 28rpx;
    color: #333;
    font-family: monospace;
  }

  .history-box {
    font-size: 24rpx;
    color: #409EFF;
    margin-top: 6rpx;
  }

  .history-box.unknown {
    color: #999;
  }

  .history-time {
    font-size: 22rpx;
    color: #bbb;
  }

  /* 弹窗 */
  .modal-mask {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 100;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .modal-content {
    background-color: #ffffff;
    border-radius: 24rpx;
    width: 80%;
    max-width: 600rpx;
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

  .modal-close {
    font-size: 32rpx;
    color: #999;
    padding: 10rpx;
  }

  .modal-body {
    padding: 40rpx 30rpx;
  }

  .barcode-input {
    background-color: #f5f6f8;
    border-radius: 12rpx;
    padding: 24rpx 30rpx;
    font-size: 32rpx;
    font-family: monospace;
    text-align: center;
  }

  .input-hint {
    font-size: 24rpx;
    color: #999;
    text-align: center;
    margin-top: 20rpx;
    display: block;
  }

  .modal-footer {
    display: flex;
    border-top: 1rpx solid #f0f0f0;
  }

  .modal-btn {
    flex: 1;
    padding: 30rpx 0;
    text-align: center;
    font-size: 30rpx;
  }

  .modal-btn.cancel {
    color: #666;
    border-right: 1rpx solid #f0f0f0;
  }

  .modal-btn.confirm {
    color: #409EFF;
    font-weight: bold;
  }

  /* 底部链接 */
  .bottom-links {
    padding: 30rpx 40rpx 60rpx;
    display: flex;
    flex-direction: column;
    gap: 20rpx;
  }

  .bottom-link {
    font-size: 28rpx;
    color: #409EFF;
    text-align: center;
    padding: 20rpx;
    background-color: #fff;
    border-radius: 12rpx;
    border: 1rpx solid #e8e8e8;
  }

  /* 隐私协议弹窗 */
  .privacy-title {
    font-size: 32rpx;
    font-weight: bold;
    color: #fff;
  }

  .privacy-body {
    padding: 40rpx 30rpx;
  }

  .privacy-text {
    display: block;
    font-size: 28rpx;
    color: #333;
    line-height: 1.6;
    margin-bottom: 20rpx;
  }

  .privacy-item {
    display: block;
    font-size: 26rpx;
    color: #666;
    line-height: 1.8;
    padding-left: 20rpx;
  }

  .privacy-footer {
    display: flex;
    padding: 20rpx 30rpx 40rpx;
    gap: 20rpx;
  }

  .privacy-btn {
    flex: 1;
    padding: 20rpx 0;
    border-radius: 8rpx;
    text-align: center;
  }

  .privacy-btn.cancel {
    background-color: #f0f0f0;
  }

  .privacy-btn.confirm {
    background-color: #409EFF;
  }

  .privacy-btn .btn-text {
    font-size: 28rpx;
    color: #666;
  }

  .privacy-btn.confirm .btn-text {
    color: #fff;
  }

  /* 深色主题适配 */
  .theme-ark .privacy-content {
    background-color: #2a2a2a;
  }

  .theme-ark .privacy-text {
    color: #e0e0e0;
  }

  .theme-ark .privacy-item {
    color: #aaa;
  }

  .theme-ark .privacy-btn.cancel {
    background-color: #444;
  }

  .theme-ark .privacy-btn.cancel .btn-text {
    color: #ccc;
  }
</style>
