<template>
  <view :class="['container', 'theme-' + themeMode]">
    <view class="content-wrapper">
      <!-- 左侧标题列表 -->
      <view class="left-panel" :class="{ collapsed: isLeftPanelCollapsed }">
        <scroll-view class="title-list" scroll-y>
          <view class="panel-header">
            <text class="panel-title">公告列表</text>
            <button class="collapse-btn" @click="toggleLeftPanel">
              <image class="collapse-icon" src="/static/collapse-icon.png"></image>
            </button>
          </view>
          <view 
            v-for="(notice, index) in sortedNoticeList" 
            :key="notice.objectId"
            class="title-item"
            :class="{ active: currentNoticeId === notice.objectId, 'top-item': notice.Top }"
            @click="selectNotice(notice)"
          >
            <view class="title-content">
              <text class="title-text">{{ notice.title }}</text>
              <view class="title-tags">
                <text class="tag top-tag" v-if="notice.Top">置顶</text>
              </view>
            </view>
          </view>
        </scroll-view>
      </view>
      
      <!-- 右侧内容区域 -->
      <view class="right-panel" :class="{ expanded: isLeftPanelCollapsed }">
        <scroll-view class="content-scroll" scroll-y>
          <view class="content-container" v-if="currentNotice">
            <!-- 标题 -->
            <view class="content-header">
              <view class="title-row">
                <text class="content-title">{{ currentNotice.title }}</text>
                <view class="content-tags">
                  <text class="tag top-tag" v-if="currentNotice.Top">置顶</text>
                </view>
              </view>
              <view class="time-info">
                <text class="update-time">
                  更新于: {{ formatTime(currentNotice.updatedAt) }}
                </text>
                <text class="edited-text" v-if="showEditedTag(currentNotice)">（已编辑）</text>
              </view>
            </view>
            
            <!-- 内容区域 -->
            <view class="content-body">
              <rich-text 
                class="content-text" 
                :nodes="parseContent(currentNotice.content)"
              ></rich-text>
            </view>
          </view>
          
          <!-- 空状态 -->
          <view class="empty-state" v-else>
            <image class="empty-icon" src="/static/notice-icon.png"></image>
            <text class="empty-text">请选择公告查看详情</text>
          </view>
        </scroll-view>
        
        <!-- 展开侧边栏按钮（折叠状态下显示） -->
        <button class="expand-panel-btn" v-if="isLeftPanelCollapsed" @click="toggleLeftPanel">
          <text class="expand-text">展开列表</text>
		  <image class="expand-icon" src="/static/right-white.png"></image>
        </button>
      </view>
    </view>
  </view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
// 开源版本：知晓云配置已禁用，商业版请配置知晓云
const KNOW_CLOUD_CONFIG = {
  clientId: 'YOUR_KNOW_CLOUD_CLIENT_ID',
  baseUrl: 'https://YOUR_KNOW_CLOUD_CLIENT_ID.myminapp.com/hserve/v2.2',
  tableNames: {
    more_notice: 'more_notice'
  }
};

export default {
  data() {
    return {
      noticeList: [], // 公告列表
      currentNotice: null, // 当前选中的公告
      currentNoticeId: '', // 当前选中的公告ID
      loading: false,
      isLeftPanelCollapsed: false, // 左侧面板是否折叠
      // 主题模式
      themeMode: 'simple'
    }
  },
  
  computed: {
    // 排序后的公告列表
    sortedNoticeList() {
      const list = [...this.noticeList];
      
      // 排序：置顶的在前，然后按更新时间降序
      return list.sort((a, b) => {
        // 如果a置顶而b不置顶，a在前
        if (a.Top && !b.Top) return -1;
        // 如果b置顶而a不置顶，b在前
        if (!a.Top && b.Top) return 1;
        // 如果都置顶或都不置顶，按更新时间降序
        const timeA = this.parseTimeToTimestamp(a.updatedAt || a.createdAt);
        const timeB = this.parseTimeToTimestamp(b.updatedAt || b.createdAt);
        return timeB - timeA;
      });
    }
  },
  
  onLoad(options) {
    this.loadThemeSetting();

    if (options.id) {
      this.currentNoticeId = options.id;
    }

    // 获取其他页面传入的isLeftPanelCollapsed参数
    if (options.isLeftPanelCollapsed) {
      this.isLeftPanelCollapsed = options.isLeftPanelCollapsed === 'true';
    }

    this.fetchNoticeList();
    
    // 设置分享配置
    wx.showShareMenu({
      withShareTicket: true,
      menus: ['shareAppMessage', 'shareTimeline']
    });
  },
  
  onShareAppMessage() {
    return {
      title: '方舟通行证谷子查询工具-公告',
      path: '/packageA/more-notice/more-notice',
      imageUrl: ''
    }
  },
  
  onShareTimeline() {
    return {
      title: '方舟通行证谷子查询工具-公告',
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
    
    // 切换左侧面板折叠状态
    toggleLeftPanel() {
      this.isLeftPanelCollapsed = !this.isLeftPanelCollapsed;
    },
    
    // 获取公告列表
    async fetchNoticeList() {
      this.loading = true;
      try {
        const res = await this.knowCloudRequest('more_notice', {
          data: {
            limit: 100, // 获取最多100条公告
            offset: 0
          }
        });
        
        console.log('知晓云公告列表响应:', res);
        
        if (res && res.objects) {
          // 知晓云返回的数据格式是 { objects: [...] }
          // 转换数据格式，将知晓云的字段名转换为前端使用的字段名
          const transformedList = res.objects.map(item => {
            // 知晓云返回的字段通常是下划线格式，转换为驼峰格式
            return {
              objectId: item.id || '', // 知晓云的id字段
              title: item.title || '',
              content: item.content || '',
              Top: item.Top || false, // 置顶字段
              createdAt: item.created_at || '', // 创建时间
              updatedAt: item.updated_at || '' // 更新时间
            };
          });
          
          this.noticeList = transformedList;
          console.log('获取公告列表成功:', this.noticeList);
          
          // 如果有传入的ID，自动选中对应的公告
          if (this.currentNoticeId) {
            const targetNotice = this.noticeList.find(notice => notice.objectId === this.currentNoticeId);
            if (targetNotice) {
              this.selectNotice(targetNotice);
            }
          }
          
          // 如果没有选中任何公告，默认选中第一个
          if (!this.currentNotice && this.sortedNoticeList.length > 0) {
            this.selectNotice(this.sortedNoticeList[0]);
          }
        }
      } catch (error) {
      	this.logError(error);
        console.error('获取公告列表失败:', error);
        uni.showToast({
          title: '获取公告失败',
          icon: 'none'
        });
      } finally {
        this.loading = false;
      }
    },
    
    // 选择公告
    selectNotice(notice) {
      this.currentNotice = notice;
      this.currentNoticeId = notice.objectId;
    },
    
    // 检查是否显示"已编辑"标签
    showEditedTag(notice) {
      if (!notice.createdAt || !notice.updatedAt) return false;
      
      const createdAt = this.parseTimeToTimestamp(notice.createdAt);
      const updatedAt = this.parseTimeToTimestamp(notice.updatedAt);
      
      // 如果更新时间比创建时间晚（允许1分钟的误差），则认为已编辑
      return updatedAt - createdAt > 60000;
    },
    
    // 解析时间字符串或时间戳为时间戳（毫秒）
    parseTimeToTimestamp(time) {
      if (!time) return 0;
      
      try {
        // 如果是时间戳（数字或数字字符串）
        if ((typeof time === 'number' || /^\d+$/.test(time.toString())) && 
            (time.toString().length === 10 || time.toString().length === 13)) {
          
          const timestamp = parseInt(time);
          // 如果是10位时间戳（秒），转换为13位（毫秒）
          return timestamp.toString().length === 10 ? timestamp * 1000 : timestamp;
        } else {
          // 尝试作为ISO字符串解析
          const date = new Date(time);
          return date.getTime();
        }
      } catch (e) {
      	this.logError(e);
        console.error('解析时间失败:', e, '原始时间:', time);
        return 0;
      }
    },
    
    // 格式化时间 - 支持时间戳和ISO字符串
    formatTime(time) {
      if (!time) return '';
      
      try {
        // 先解析为时间戳
        const timestamp = this.parseTimeToTimestamp(time);
        
        if (timestamp === 0) {
          return '未知时间';
        }
        
        const date = new Date(timestamp);
        
        // 检查日期是否有效
        if (isNaN(date.getTime())) {
          console.warn('无效的时间格式:', time);
          return '未知时间';
        }
        
        return `${date.getFullYear()}-${(date.getMonth()+1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')} ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
      } catch (e) {
      	this.logError(e);
        console.error('格式化时间失败:', e, '原始时间:', time);
        return typeof time === 'string' ? time : '时间格式错误';
      }
    },
    
    // 解析内容（支持简单的HTML标签）- 修复换行符问题
    parseContent(content) {
      if (!content) return '';
      
      // 首先，将字符串中的转义换行符 \n 转换为真正的换行符
      let processedContent = content;
      
      // 处理转义的换行符（两种情况：\\n 或 \n）
      processedContent = processedContent.replace(/\\n/g, '\n');
      
      // 然后，将真正的换行符替换为 <br>
      processedContent = processedContent.replace(/\n/g, '<br>');
      
      // 处理其他的换行符格式
      processedContent = processedContent.replace(/\r\n/g, '<br>'); // Windows换行符
      processedContent = processedContent.replace(/\r/g, '<br>');   // Mac换行符
      
      // 统一处理<br>标签
      processedContent = processedContent.replace(/<br\s*\/?>/gi, '<br>');
      
      // 简单的HTML标签转换
      return processedContent
        .replace(/<p>/gi, '<div>')
        .replace(/<\/p>/gi, '</div>')
        .replace(/<strong>(.*?)<\/strong>/gi, '<b>$1</b>')
        .replace(/<b>(.*?)<\/b>/gi, '<b>$1</b>')
        .replace(/<em>(.*?)<\/em>/gi, '<i>$1</i>')
        .replace(/<i>(.*?)<\/i>/gi, '<i>$1</i>')
        .replace(/<u>(.*?)<\/u>/gi, '<span style="text-decoration: underline;">$1</span>');
    }
  }
}
</script>

<style scoped>
.container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f5f5f5;
}

.content-wrapper {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 左侧面板样式 - 固定宽度 */
.left-panel {
  width: 300rpx;
  background-color: #f8f9fa;
  border-right: 1rpx solid #e0e0e0;
  flex-shrink: 0;
  transition: all 0.3s ease;
}

.left-panel.collapsed {
  width: 0;
  border-right: none;
  overflow: hidden;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20rpx;
  border-bottom: 1rpx solid #e0e0e0;
  background-color: #fff;
}

.panel-title {
  font-size: 28rpx;
  font-weight: bold;
  color: #333;
}

.collapse-btn {
  background: none;
  border: none;
  padding: 8rpx;
  border-radius: 6rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.collapse-icon {
  width: 50rpx;
  height: 50rpx;
}

.title-list {
  height: 100%;
}

.title-item {
  margin: 0;
  border-bottom: 1rpx solid #e8e8e8;
  background-color: #fff;
  overflow: hidden;
  transition: all 0.3s;
}

.title-item.active {
  background-color: #e8f4ff;
  border-left: 4rpx solid #409EFF;
}

/* 置顶项的特殊样式 */
.title-item.top-item {
  background-color: #fff;
  border-left: 4rpx solid #ffc53d;
}

.title-item.top-item.active {
  background-color: #e8f4ff;
  border-left: 4rpx solid #409EFF;
}

.title-content {
  display: flex;
  flex-direction: column;
  padding: 20rpx;
  cursor: pointer;
}

.title-text {
  font-size: 28rpx;
  color: #333;
  line-height: 1.4;
  font-weight: 500;
  margin-bottom: 10rpx;
}

.title-item.active .title-text {
  color: #409EFF;
  font-weight: 600;
}

.title-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8rpx;
}

.tag {
  font-size: 20rpx;
  padding: 4rpx 8rpx;
  border-radius: 6rpx;
  font-weight: 500;
}

.top-tag {
  background-color: #ffc53d;
  color: #d46b08;
  font-weight: bold;
}

/* 右侧面板样式 - 自适应宽度 */
.right-panel {
  flex: 1;
  background-color: #fff;
  display: flex;
  flex-direction: column;
  transition: all 0.3s ease;
}

.right-panel.expanded {
  width: 100%;
}

.content-scroll {
  flex: 1;
  height: 100%;
}

.content-container {
  padding: 40rpx;
  min-height: 100%;
}

.content-header {
  margin-bottom: 40rpx;
  padding-bottom: 30rpx;
  border-bottom: 1rpx solid #e0e0e0;
}

.title-row {
  display: flex;
  align-items: center;
  margin-bottom: 15rpx;
}

.content-title {
  font-size: 40rpx;
  font-weight: bold;
  color: #333;
  line-height: 1.4;
  margin-right: 20rpx;
}

.content-tags {
  display: flex;
}

.time-info {
  display: flex;
  align-items: center;
  font-size: 24rpx;
  color: #999;
}

.update-time {
  margin-right: 10rpx;
}

.edited-text {
  color: #999;
  font-size: 24rpx;
}

.content-body {
  line-height: 1.8;
}

.content-text {
  font-size: 30rpx;
  color: #333;
  line-height: 1.8;
}

.content-text >>> b {
  font-weight: bold;
  color: #333;
}

.content-text >>> i {
  font-style: italic;
}

.content-text >>> div {
  margin-bottom: 20rpx;
}

.content-text >>> br {
  content: '';
  display: block;
  margin-bottom: 10rpx;
}

/* 空状态样式 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 60vh;
  text-align: center;
  padding: 40rpx;
}

.empty-icon {
  width: 200rpx;
  height: 200rpx;
  margin-bottom: 40rpx;
  opacity: 0.5;
}

.empty-text {
  font-size: 28rpx;
  color: #999;
}

/* 展开侧边栏按钮 */
.expand-panel-btn {
  position: fixed;
  left: 20rpx;
  bottom: 40rpx;
  background-color: #409EFF;
  color: #fff;
  border: none;
  border-radius: 50rpx;
  padding: 20rpx 30rpx;
  display: flex;
  align-items: center;
  gap: 15rpx;
  box-shadow: 0 4rpx 20rpx rgba(64, 158, 255, 0.3);
  z-index: 100;
}

.expand-icon {
  width: 24rpx;
  height: 24rpx;
}

.expand-text {
  font-size: 24rpx;
  font-weight: 500;
}

/* ========== 科技风格 ========== */
.container.theme-ark {
	background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
}
.container.theme-ark .left-panel,
.container.theme-ark .content-panel {
	background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
	border: 1rpx solid #2A2A4A;
}
.container.theme-ark .panel-title { color: #FF6B35; }
.container.theme-ark .notice-item { border-bottom-color: #2A2A4A; }
.container.theme-ark .notice-item.active { background: rgba(255,107,53,0.1); }
.container.theme-ark .notice-title-text { color: #e0e0e0; }
.container.theme-ark .notice-time { color: #888; }
.container.theme-ark .top-badge { background: #FF6B35; color: #fff; }
.container.theme-ark .content-title { color: #FF6B35; }
.container.theme-ark .content-text { color: #e0e0e0; }
.container.theme-ark .collapse-btn { background: rgba(255,107,53,0.1); }
.container.theme-ark .expand-panel-btn { background: rgba(255,107,53,0.1); color: #FF6B35; }
.container.theme-ark .expand-text { color: #FF6B35; }
.container.theme-ark .collapse-icon { filter: brightness(0) invert(1); opacity: 0.7; }

/* ========== 界园风格 ========== */
.container.theme-jieyuan {
	background-color: #FAF3E0;
}
.container.theme-jieyuan .left-panel,
.container.theme-jieyuan .content-panel {
	background: rgba(255, 255, 255, 0.9);
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}
.container.theme-jieyuan .panel-title {
	background: linear-gradient(90deg, #e25884, #399383);
	-webkit-background-clip: text;
	-webkit-text-fill-color: transparent;
}
.container.theme-jieyuan .notice-item { border-bottom-color: rgba(226, 88, 132, 0.1); }
.container.theme-jieyuan .notice-item.active { background: rgba(226, 88, 132, 0.08); }
.container.theme-jieyuan .notice-title-text { color: #333; }
.container.theme-jieyuan .notice-time { color: #888; }
.container.theme-jieyuan .top-badge { background: linear-gradient(90deg, #e25884, #399383); color: #fff; }
.container.theme-jieyuan .content-title { color: #399383; }
.container.theme-jieyuan .content-text { color: #333; }
.container.theme-jieyuan .collapse-btn { background: rgba(226, 88, 132, 0.1); }
.container.theme-jieyuan .expand-panel-btn { background: rgba(226, 88, 132, 0.1); color: #e25884; }
.container.theme-jieyuan .expand-text { color: #e25884; }
</style>