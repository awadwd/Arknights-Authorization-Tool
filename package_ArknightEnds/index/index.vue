<template>
	<view class="custom-nav-bar" :style="'padding-top: ' + statusBarHeight + 'px'">
		<view class="nav-content">
			<view class="nav-left">
				<view class="nav-switch-btn" @click="goToArknights">
					<text class="nav-switch-text">明日方舟</text>
				</view>
			</view>
			<view class="nav-title-box">
				<text class="nav-title-text">终末地工具箱</text>
			</view>
			<view class="nav-right"></view>
		</view>
	</view>
	<view class="container ef-page" :style="{ paddingTop: (statusBarHeight + 44) + 'px' }">
		<!-- 公告栏 -->
		<view class="notice-section" v-if="noticeContent && showNotice">
			<view class="notice-header">
				<!-- 关闭按钮放在最前面 -->
				<view class="notice-close" @click.stop="closeNotice">
					<text class="close-btn">×</text>
				</view>
				<image class="notice-icon" src="/static/notice-icon.png"></image>
				<text class="notice-title">公告</text>
				<!-- 展开/收起按钮 -->
				<view class="notice-toggle" @click.stop="toggleNoticeSection">
					<text class="toggle-text">{{ noticeSectionExpanded ? '收起' : '展开' }}</text>
					<image class="toggle-icon" :class="{ 'rotate': !noticeSectionExpanded }" src="/static/arrow-down.png"></image>
				</view>
			</view>
			
			<!-- 滚动公告栏和公告内容在展开时显示 -->
			<view class="notice-expandable" :class="{ 'expanded': noticeSectionExpanded, 'collapsed': !noticeSectionExpanded }">
				<!-- 滚动公告栏 -->
				<view class="notice-bar-container">
					<uni-notice-bar 
						show-icon 
						scrollable 
						show-get-more @getmore="gotoMoreNotice()" more-text="查看更多>" more-color="#52525b"
						:text="moreNoticeContent" 
						speed="75" 
						background-color='#FDFDE6' 
						color="#18181b"
					></uni-notice-bar>
				</view>
				
				<view class="notice-content">
					<text 
						class="notice-text" 
						:class="{ 'text-collapsed': !noticeExpanded && noticeOverflow, 'text-expanded': noticeExpanded }"
						ref="noticeText" user-select
					>
						{{ noticeContent }}
					</text>
					<view class="notice-expand">
						<!-- 这个button用于将展开收起顶至右侧 -->
						<button
							style="width: 80%;"
						></button>
						<button
							class="expand-btn" 
							v-if="noticeOverflow"
							@click="toggleNoticeExpand"
						>
							{{ noticeExpanded  ? '收起 ↑' : '展开 ↓' }}
						</button>
					</view>
				</view>
			</view>
		</view>
		
		<!-- 轮播图 -->
		<view class="carousel-section" v-if="carouselImages.length > 0">
			<swiper class="carousel" indicator-dots="true" autoplay="true" interval="3000" duration="500" circular="true">
				<swiper-item v-for="(image, index) in carouselImages" :key="index">
					<image class="carousel-image" :src="image.imageUrl" mode="aspectFill" @click="onCarouselItemClick(image)"></image>
				</swiper-item>
			</swiper>
		</view>
		
		<!-- 工具箱功能区域 -->
		<view class="tools-section">
			<view class="tools-header">
				<text class="tools-title">终末地凭证-工具箱</text>
				<view class="tools-manage-btn" @click="toggleToolManagement">
					<text class="manage-text">{{ isEditingTools ? '完成' : '管理' }}</text>
					<image class="manage-icon" src="/static/Setting-tool.png"></image>
				</view>
			</view>
			
			<!-- 普通模式下的工具箱 -->
			<view class="tools-grid" v-if="!isEditingTools">
				
				<!-- <view class="empty-placeholder">
				    <image src="/static/empty-icon.png" class="test-icon"></image>
				    <text class="empty-text">内部测试中，暂未开放，请使用已开放的独立版本</text>
				</view> -->
				<!-- 动态渲染工具卡片 -->
				<view 
					class="tool-card" 
					v-for="tool in getVisibleTools()" 
					:key="tool.id"
					@click="handleToolClick(tool)"
					@longpress="startToolEditing(tool)"
				>
					<view class="tool-icon-container">
						<image class="tool-icon" :src="tool.icon"></image>
						<!-- 显示New图标 -->
						<image class="new-badge" v-if="tool.showNew" src="/static/new.png"></image>
					</view>
					<view class="tool-info">
						<text class="tool-name">{{ tool.name }}</text>
						<text class="tool-desc">{{ tool.desc }}</text>
					</view>
					
					<!-- 长按提示 -->
					<view class="longpress-hint" v-if="!isEditingTools && showLongPressHint">
						<text class="hint-text">长按进入编辑</text>
					</view>
				</view>
			</view>
			
			<!-- 编辑模式下的工具箱 -->
			<view class="tools-grid editing" v-else>
				<!-- 编辑模式下的工具卡片 -->
				<view 
					class="tool-card editing-card" 
					v-for="(tool, index) in editingTools" 
					:key="tool.id"
				>
					<view class="tool-icon-container editing">
						<image class="tool-icon" :src="tool.icon"></image>
						<!-- 显示New图标 -->
						<image class="new-badge" v-if="tool.showNew" src="/static/new.png"></image>
					</view>
					<view class="tool-info editing">
						<text class="tool-name">{{ tool.name }}</text>
						<text class="tool-desc">{{ tool.desc }}</text>
						
						<!-- 编辑模式下的开关 -->
						<view class="tool-edit-controls">
							<text class="visibility-label">{{ tool.visible ? '显示' : '隐藏' }}</text>
							<switch 
								:checked="tool.visible" 
								@change="toggleToolVisibility(index)"
								color="#FBFB45"
								class="visibility-switch"
							/>
						</view>
					</view>
					
					<!-- 删除按钮（仅在编辑模式显示） -->
					<view 
						class="delete-btn" 
						@click.stop="deleteTool(index)"
						v-if="editingTools.length > 1"
					>
						<text class="delete-text">×</text>
					</view>
					
					<!-- 排序箭头按钮 -->
					<view class="sort-arrows">
						<!-- 上箭头 -->
						<view 
							class="arrow-btn arrow-up" 
							@click.stop="moveToolUp(index)"
							v-if="index > 0"
						>
							<image class="arrow-icon" src="/static/right.png" style="transform: rotate(-90deg);"></image>
						</view>
						
						<!-- 下箭头 -->
						<view 
							class="arrow-btn arrow-down" 
							@click.stop="moveToolDown(index)"
							v-if="index < editingTools.length - 1"
						>
							<image class="arrow-icon" src="/static/right.png" style="transform: rotate(90deg);"></image>
						</view>
						
						<!-- 左箭头 -->
						<view 
							class="arrow-btn arrow-left" 
							@click.stop="moveToolLeft(index)"
							v-if="index % 2 === 1"
						>
							<image class="arrow-icon" src="/static/right.png" style="transform: rotate(180deg);"></image>
						</view>
						
						<!-- 右箭头 -->
						<view 
							class="arrow-btn arrow-right" 
							@click.stop="moveToolRight(index)"
							v-if="index % 2 === 0 && index < editingTools.length - 1"
						>
							<image class="arrow-icon" src="/static/right.png"></image>
						</view>
					</view>
				</view>
			</view>
			
			<!-- 编辑模式控制栏 -->
			<view class="edit-controls-bar" v-if="isEditingTools">
				<view class="edit-info">
					<text class="info-text">点击箭头调整顺序，开关控制显示/隐藏</text>
					<text class="info-text">当前显示 {{ getVisibleCount() }}/{{ editingTools.length }} 个功能</text>
				</view>
				
				<view class="edit-buttons">
					<button class="edit-btn reset" @click="resetToolsConfig">
						重置
					</button>
					<button class="edit-btn cancel" @click="cancelEditing">
						取消
					</button>
					<button class="edit-btn save" @click="saveToolsConfig">
						保存
					</button>
				</view>
			</view>
		</view>
		
		<!-- 更新数据按钮（MaaEnd 风格：亮黄底 + 黑描边 + 切角） -->
		<view class="update-section">
			<view class="update-frame">
				<button class="update-btn" @click="downloadData">更新干员数据</button>
			</view>
		</view>
		
		<!-- 数据状态 - 整合所有信息 -->
		<view class="data-status" v-if="dataStatusMessage">
			<!-- 更新提示区域（当有更新且自动更新关闭时显示） -->
			<view class="update-hint" v-if="hasUpdate && !autoUpdateEnabled">
				<text class="hint-text">当前数据版本有更新，您可以点击上方的更新干员数据按钮来手动更新</text>
			</view>
			
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
				<text class="data-detail-item" v-if="latestVersion && latestVersion !== currentVersion">最新版本: {{ latestVersion }}</text>
			</view>
		</view>
		
		<view class="about-link-section">
			<!-- 底部版权 -->
			<view class="footer-section">
			    <text 
			        class="copyright-text" 
			        @click="handleCopyrightClick"
			    >{{ originalCopyrightTexts[0] }}</text>
			    <text 
			        class="copyright-text" 
			        @click="handleCopyrightClick"
			    >{{ originalCopyrightTexts[1] }}</text>
			</view>
		</view>
		<ak-music-bar />
	</view>
</template>

<script>
import { fetchSelfHosted } from "../utils/knowCloudProxy_ae.js";
import { haptic } from "@/utils/haptic.js";
import { openLink } from "@/utils/openLink.js";
	const getBaseUrl = () => {
	  // #ifdef H5
	  console.log('当前是 H5 环境，使用代理前缀 /api');
	  return '/api';
	  // #endif
	  // #ifndef H5
	  console.log('当前是非 H5 环境，使用完整 URL');
	  return 'https://9f1083b4adad066a3603.myminapp.com/hserve/v2.2';
	  // #endif
	};
	
// 知晓云配置 - 使用ClientID认证
const KNOW_CLOUD_CONFIG = {
  clientId: '9f1083b4adad066a3603',
  tableNames: {
    choearth_notice: 'choearth_notice',
    more_notice: 'more_notice',
    Version: 'Version',
    AiToolsConfig: 'AiToolsConfig'
  }
};

export default {
    data() {
        return {
            statusBarHeight: 0,
            noticeContent: '', // 公告内容
            noticeExpanded: false, // 公告文本是否展开
            noticeOverflow: false, // 公告是否溢出（超过3行）
            showNotice: true, // 是否显示公告栏
            noticeSectionExpanded: true, // 控制整个公告栏的展开/收起状态
            characterData: [],
            dataStatusMessage: '',
            cloudUpdateTime: '',
            localUpdateTime: '',
            currentVersion: '',
            dataUrl: '',
            latestVersion: '', // 新增：存储最新的云端版本
            moreNoticeContent: '', // 从 more_notice 表获取的公告内容
            moreNoticeId: '', // 公告ID，用于跳转详情
            carouselImages: [], // 轮播图图片列表
            noticeClosedUntilUpdate: false, // 是否在下次更新前保持关闭
            isRefreshing: false,
            // 工具箱管理相关数据
            toolsConfig: [], // 工具箱配置
            tempToolsConfig: [], // 临时工具箱配置（用于管理界面）
            // 新增编辑模式相关数据
            editingTools: [], // 编辑模式下的工具箱配置
            isEditingTools: false, // 是否处于编辑模式
            showLongPressHint: false, // 是否显示长按提示
            // 长按计时器
            longPressTimer: null,
            isLongPressing: false,
            // 知晓云重试计数
            retryCount: 0,
            // 新增：是否有更新标志
            hasUpdate: false,
            // 新增：自动更新设置
            autoUpdateEnabled: false,
            // 新增：自动更新配置
            autoUpdateConfig: {
                enabled: false,
                checkInterval: 24 * 60 * 60 * 1000, // 24小时
                wifiOnly: true,
                autoDownload: false
            },
            lastAutoUpdateCheckTime: '',
            nextAutoUpdateTime: '',
            showAutoUpdateStatus: false,
            autoUpdateStatusMessage: '',
            autoUpdateStatusClass: '',
            showAutoUpdatePrompt: false,
            autoUpdatePromptMessage: '',
            isCheckingUpdate: false,
            isDownloading: false,
			// 彩蛋相关数据
			            easterEggs: [], // 您将在这里填写彩蛋数组
			            showEasterEgg: false, // 是否显示彩蛋
			            easterEggText: '', // 当前显示的彩蛋文本
			            copyrightClickCount: 0, // 版权点击次数
			            originalCopyrightTexts: [ // 原始版权文本
			                '终末地白名单凭证小助手',
			                '本工具为玩家自制，与官方无关'
			            ]
        }
    },
    
    // 页面生命周期 - 下拉刷新
    onPullDownRefresh() {
        this.handleRefresh();
    },
        
    onLoad() {
        console.log('首页加载，开始初始化数据...');
        try {
            const systemInfo = uni.getSystemInfoSync();
            this.statusBarHeight = systemInfo.statusBarHeight || 0;
        } catch (e) {
            console.error('获取状态栏高度失败:', e);
            this.statusBarHeight = 0;
        }
        this.loadLocalData();
        this.fetchNotice();
        this.fetchMoreNotice(); // 新增：获取更多公告
        this.fetchCarouselImages(); // 新增：获取轮播图数据
        // 初始化工具箱配置
        this.initDefaultToolsConfig();
		
		// 新增：加载自动更新设置
        this.loadAutoUpdateSetting();
		
		// 初始化彩蛋数组
		  this.initEasterEggs();
		  
		  this.initDefaultToolsConfig();        // 先初始化默认配置
		      this.loadAiToolsConfig();    
		
		// 从本地存储读取彩蛋点击次数（可选）
		  const savedCount = uni.getStorageSync('ae_copyrightClickCount');
		  if (savedCount) {
		      this.copyrightClickCount = savedCount;
		  }
        
        // 检查是否已关闭公告
        const noticeClosed = uni.getStorageSync('ae_noticeClosed');
        const noticeClosedUntilUpdate = uni.getStorageSync('ae_noticeClosedUntilUpdate');
        
        if (noticeClosed) {
            this.showNotice = false;
        }
        
        if (noticeClosedUntilUpdate) {
            this.noticeClosedUntilUpdate = noticeClosedUntilUpdate;
        }
        
        // 从本地存储读取公告栏展开状态
        const noticeExpandedState = uni.getStorageSync('ae_noticeSectionExpanded');
        if (noticeExpandedState !== null && noticeExpandedState !== undefined) {
            this.noticeSectionExpanded = noticeExpandedState;
            
            // 如果公告栏是展开状态，检查文本溢出
            if (this.noticeSectionExpanded) {
                this.$nextTick(() => {
                    setTimeout(() => {
                        this.checkNoticeOverflow();
                    }, 500);
                });
            }
        }
        
        // 设置分享配置
        uni.showShareMenu && uni.showShareMenu({
            withShareTicket: true,
            menus: ['shareAppMessage', 'shareTimeline']
        });
        
        // 显示一次长按提示
        const hasShownHint = uni.getStorageSync('ae_hasShownLongPressHint');
        if (!hasShownHint) {
            setTimeout(() => {
                this.showLongPressHint = true;
                // 3秒后隐藏提示
                setTimeout(() => {
                    this.showLongPressHint = false;
                    uni.setStorageSync('ae_hasShownLongPressHint', true);
                }, 3000);
            }, 2000);
        }
    },
    
    // 分享给好友
    onShareAppMessage() {
        return {
            title: '终末地白名单凭证小助手',
            path: '/package_ArknightEnds/index/index',
            imageUrl: ''
        }
    },
    
    // 分享到朋友圈
    onShareTimeline() {
        return {
            title: '终末地白名单凭证小助手',
            imageUrl: ''
        }
    },

    
    onReady() {
        // 页面渲染完成后检查公告内容是否需要折叠
        this.$nextTick(() => {
            setTimeout(() => {
                this.checkNoticeOverflow();
            }, 500);
        });
    },
    
    methods: {
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
                
                // 关键修改：动态获取基础 URL
                    const baseUrl = getBaseUrl();
                
                    const defaultOptions = {
                      url: `${baseUrl}/table/${tableNameKey}/record/`,  // 这里变了！
                      method: 'GET',
                      header: {
                        'X-Hydrogen-Client-ID': KNOW_CLOUD_CONFIG.clientId,
                        'Content-Type': 'application/json'
                      },
                      timeout: 15000
                    };

                
                // 合并参数，确保查询参数正确传递
                const finalOptions = { ...defaultOptions, ...options };
                
                // 处理查询参数
                if (options.data) {
                    const params = new URLSearchParams();
                    Object.keys(options.data).forEach(key => {
                        params.append(key, options.data[key]);
                    });
                    finalOptions.url = `${defaultOptions.url}?${params.toString()}`;
                }
                
                console.log('发送知晓云请求:', finalOptions.url);
                
                const res = await uni.request(finalOptions);
                
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
        
        // 切换工具箱管理/编辑状态
        toggleToolManagement() {
            if (this.isEditingTools) {
                // 如果已经在编辑状态，点击后保存并退出编辑
                this.saveToolsConfig();
            } else {
                // 如果不在编辑状态，点击后进入编辑
                this.startEditingMode();
            }
        },
		
		// 从知晓云获取 AI 工具配置（Enabled 字段）
		async fetchAiToolsConfig() {
		  try {
		    const res = await uni.request({
		      url: `${getBaseUrl()}/table/AiToolsConfig/record/?limit=1`,
		      method: 'GET',
		      header: {
		        'X-Hydrogen-Client-ID': KNOW_CLOUD_CONFIG.clientId,
		        'Content-Type': 'application/json'
		      },
		      timeout: 10000
		    });
		        if (res.statusCode === 200 && res.data && res.data.objects && res.data.objects.length > 0) {
		            const config = res.data.objects[0];
		            // 假设 Enabled 字段为布尔值 true/false
		            return config.Enabled === true;
		        }
		        return false;
		    } catch (error) {
		        console.error('获取 AI 工具配置失败', error);
		        return false; // 失败时使用原配置
		    }
		},
		
		// 根据云端配置更新工具箱中对应的卡片
		async loadAiToolsConfig() {
		//     const enabled = await this.fetchAiToolsConfig();
		//     const index = this.toolsConfig.findIndex(item => item.id === 'questionnaire');
		//     if (index !== -1) {
		//         const tool = this.toolsConfig[index];
		//         if (enabled) {
		//             // 启用新配置（AI帮查）
		//             tool.name = 'AI帮查';
		//             tool.icon = '/static/aisearch-icon.png';
		//             tool.url = '/package_ArknightEnds/Ai_Search/Ai_Search';
		//             tool.specialAction = '';
		//         } else {
		//             // 使用原配置（前往元宝小程序）
		//             tool.name = '前往元宝小程序帮查>';
		//             tool.icon = '/static/search-icon.png';
		//             tool.url = '';
		//             tool.specialAction = 'gotoaitools';
		//         }
		        // 保持 visible, order, showNew 等字段不变
		        // 触发视图更新
		//         this.toolsConfig = [...this.toolsConfig];
		//         // 保存到本地存储
		//         uni.setStorageSync('toolsConfig', this.toolsConfig);
		//     }
		},
        
        // 下拉刷新处理函数
        handleRefresh() {
            if (this.isRefreshing) {
                return;
            }
            
            this.isRefreshing = true;
            console.log('开始下拉刷新');
            
            // 显示刷新状态
            uni.showLoading({
                title: '刷新中...'
            });
            
            // 执行刷新操作
            Promise.all([
                this.fetchNotice(),
                this.fetchMoreNotice(),
                this.fetchCarouselImages(),
                this.checkForDataUpdates(true, false)
            ]).then(() => {
                uni.showToast({
                    title: '刷新成功',
                    icon: 'success',
                    duration: 1500
                });
            }).catch(err => {
                console.error('刷新失败:', err);
                uni.showToast({
                    title: '刷新失败',
                    icon: 'none',
                    duration: 2000
                });
            }).finally(() => {
                uni.stopPullDownRefresh();
                uni.hideLoading();
                this.isRefreshing = false;
            });
        },
        
        // 切换整个公告栏的展开/收起状态
        toggleNoticeSection() {
            this.noticeSectionExpanded = !this.noticeSectionExpanded;
            uni.setStorageSync('ae_noticeSectionExpanded', this.noticeSectionExpanded);
            
            if (this.noticeSectionExpanded) {
                this.$nextTick(() => {
                    setTimeout(() => {
                        this.checkNoticeOverflow();
                    }, 300);
                });
            }
        },
        
        // 关闭公告栏
        closeNotice() {
            console.log('关闭按钮被点击');
            
            uni.showModal({
                title: '关闭公告',
                content: '是否在公告下次更新前保持关闭？',
                confirmText: '本次关闭',
                cancelText: '保持关闭',
                success: (res) => {
                    console.log('用户选择:', res.confirm ? '仅本次关闭' : '保持关闭');
                    
                    if (res.confirm) {
                        this.noticeClosedUntilUpdate = true;
                        uni.setStorageSync('ae_noticeClosedUntilUpdate', true);
                        this.showNotice = false;
                        uni.setStorageSync('ae_noticeClosed', true);
                        console.log('公告已关闭，仅本次');
                    } else if (res.cancel) {
                        this.showNotice = false;
                        uni.setStorageSync('ae_noticeClosed', true);
                        console.log('公告已关闭，直到下次更新');
                    }
                },
                fail: (err) => {
                    console.error('显示确认对话框失败:', err);
                    this.showNotice = false;
                    uni.setStorageSync('ae_noticeClosed', true);
                }
            });
        },
        
        // 检查公告内容是否溢出（超过3行）
        checkNoticeOverflow() {
            const query = uni.createSelectorQuery().in(this);
            query.select('.notice-text').boundingClientRect(data => {
                if (data) {
                    const lineHeight = 14 * 1.6;
                    const maxHeight = lineHeight * 3;
                    
                    this.noticeOverflow = data.height > maxHeight;
                    console.log('公告高度:', data.height, '阈值:', maxHeight, '需要折叠:', this.noticeOverflow);
                    
                    if (!this.noticeOverflow) {
                        this.noticeExpanded = true;
                    }
                }
            }).exec();
        },
        
        // 切换公告文本展开状态
        toggleNoticeExpand() {
            this.noticeExpanded = !this.noticeExpanded;
        },
        
        // 获取轮播图数据
        async fetchCarouselImages() {
            try {
                const res = await this.knowCloudRequest('choearth_notice', {
                    data: {
                        limit: 10,
                        offset: 0
                    }
                });
                
                console.log('轮播图API响应:', res);
                
                if (res && res.objects && res.objects.length > 0) {
                    console.log('原始轮播图数据:', res.objects);
                    
                    const images = res.objects
                        .filter(item => {
                            const hasImage = item.imageurl || item.imageUrl;
                            console.log('检查图片项:', item, '是否有图片:', hasImage);
                            return hasImage;
                        })
                        .map(item => {
                            const imageData = {
                                imageUrl: item.imageurl || item.imageUrl || '',
                                linkUrl: item.linkUrl || '',
                                title: item.title || '',
                                id: item.id || ''
                            };
                            console.log('映射后的图片数据:', imageData);
                            return imageData;
                        });
                    
                    console.log('处理后的轮播图数据:', images);
                    this.carouselImages = [...images];
                    console.log('最终carouselImages:', this.carouselImages);
                } else {
                    console.log('未找到轮播图数据或数据格式不正确');
                }
            } catch (error) {
                console.error('获取轮播图数据失败:', error);
                this.carouselImages = [];
            }
        },
        
        // 轮播图点击事件
        onCarouselItemClick(item) {
            const fixedLink = "https://arknightsendfieldyx.tmall.com/";
            openLink(fixedLink);
        },
        
        // 跳转至微信小程序问卷页面
        gotoquestionnaire() {
            uni.openEmbeddedMiniProgram({
            appId: 'wxebadf544ddae62cb',
            path: 'pages/webview/index?sid=25648071&hash=skl8&navigateBackMiniProgram=true',});
        },
		
		// 跳转至AI工具小程序
		gotoaitools() {
		    uni.openEmbeddedMiniProgram({
		    appId: 'wxd5201eb08d2fa15c',
		    path: 'pages/agentChat/index?showAuthDirectly=1&agentId=0aRyBMBEs8rL',});
		},
        
        // 获取更多公告内容 - 修复版
        async fetchMoreNotice() {
            try {
                console.log('开始获取更多公告...');
                
                // 使用正确的ClientID认证
                let res;
                const sh_more_notice = await fetchSelfHosted("more_notice", { data: { limit: 1, offset: 0, order_by: "-created_at" } });
                if (sh_more_notice) {
                  res = { statusCode: 200, data: sh_more_notice };
                } else {
                res = await uni.request({
                    url: 'https://9f1083b4adad066a3603.myminapp.com/hserve/v2.2/table/more_notice/record/?limit=1&offset=0&order_by=-created_at',
                    method: 'GET',
                    header: {
                        'X-Hydrogen-Client-ID': '9f1083b4adad066a3603',
                        'Content-Type': 'application/json'
                    },
                    timeout: 10000
                });
                }
                
                console.log('获取更多公告响应:', res);
                
                if (res.statusCode === 200 && res.data && res.data.objects && res.data.objects.length > 0) {
                    const noticeData = res.data.objects[0];
                    this.moreNoticeContent = noticeData.content || '';
                    this.moreNoticeId = noticeData.id || '';
                    console.log('获取更多公告成功:', this.moreNoticeContent);
                    
                    // 更新滚动公告栏
                    this.updateScrollNotice();
                } else {
                    console.log('未找到公告数据或数据格式不正确');
                    this.moreNoticeContent = "当前外号、英文名搜索及烫度标记、仅精一显示均为内测阶段，如有显示异常，可在小程序首页->关于我们->问题反馈中提交问卷，也可以在右上角联系客服解决，感谢您的理解与支持！";
                    this.moreNoticeId = '';
                }
            } catch (error) {
                console.error('获取更多公告失败:', error);
                // 设置默认公告内容
                this.moreNoticeContent = "当前外号、英文名搜索及烫度标记、仅精一显示均为内测阶段，如有显示异常，可在小程序首页->关于我们->问题反馈中提交问卷，也可以在右上角联系客服解决，感谢您的理解与支持！";
                this.moreNoticeId = '';
            }
        },
        // 跳转到更多公告页面
        gotoMoreNotice() {
            if (this.moreNoticeId) {
                uni.navigateTo({
                    url: `/package_ArknightEnds/more-notice/more-notice?id=${this.moreNoticeId}`
                });
            } else {
                uni.navigateTo({
                    url: '/package_ArknightEnds/more-notice/more-notice'
                });
            }
        },
        
        // 获取轮播图数据 - 修复版
        async fetchCarouselImages() {
            try {
                console.log('开始获取轮播图数据...');
                
                let res;
                const sh_choearth = await fetchSelfHosted("choearth_notice", { data: { limit: 10, offset: 0 } });
                if (sh_choearth) {
                  res = { statusCode: 200, data: sh_choearth };
                } else {
                res = await uni.request({
                    url: 'https://9f1083b4adad066a3603.myminapp.com/hserve/v2.2/table/choearth_notice/record/?limit=10&offset=0',
                    method: 'GET',
                    header: {
                        'X-Hydrogen-Client-ID': '9f1083b4adad066a3603',
                        'Content-Type': 'application/json'
                    },
                    timeout: 10000
                });
                }
                
                console.log('轮播图API响应:', res);
                
                if (res.statusCode === 200 && res.data && res.data.objects) {
                    console.log('原始轮播图数据:', res.data.objects);
                    
                    const images = res.data.objects
                        .filter(item => {
                            // 检查是否有图片URL
                            const hasImage = item.imageurl || item.imageUrl;
                            console.log('检查图片项:', item, '是否有图片:', hasImage);
                            return hasImage;
                        })
                        .map(item => {
                            const imageData = {
                                imageUrl: item.imageurl || item.imageUrl || '',
                                linkUrl: item.linkUrl || '',
                                title: item.title || '',
                                id: item.id || ''
                            };
                            console.log('映射后的图片数据:', imageData);
                            return imageData;
                        });
                    
                    console.log('处理后的轮播图数据:', images);
                    this.carouselImages = [...images];
                    console.log('最终carouselImages:', this.carouselImages);
                    
                    // 如果获取到图片数据，更新本地存储
                    if (images.length > 0) {
                        uni.setStorageSync('carouselImages', images);
                    }
                } else {
                    console.log('未找到轮播图数据或数据格式不正确');
                    
                    // 尝试从本地存储加载
                    const localImages = uni.getStorageSync('carouselImages');
                    if (localImages && localImages.length > 0) {
                        this.carouselImages = localImages;
                    }
                }
            } catch (error) {
                console.error('获取轮播图数据失败:', error);
                
                // 尝试从本地存储加载
                const localImages = uni.getStorageSync('carouselImages');
                if (localImages && localImages.length > 0) {
                    this.carouselImages = localImages;
                }
            }
        },
        
        // 获取公告内容 - 修复版
        async fetchNotice() {
            try {
                console.log('开始获取公告内容...');
                
                let res;
                const sh_Version = await fetchSelfHosted("Version", { data: { limit: 1, offset: 0 } });
                if (sh_Version) {
                  res = { statusCode: 200, data: sh_Version };
                } else {
                res = await uni.request({
                    url: 'https://9f1083b4adad066a3603.myminapp.com/hserve/v2.2/table/Version/record/?limit=1&offset=0',
                    method: 'GET',
                    header: {
                        'X-Hydrogen-Client-ID': '9f1083b4adad066a3603',
                        'Content-Type': 'application/json'
                    },
                    timeout: 10000
                });
                }
                
                console.log('公告API响应:', res);
                
                if (res.statusCode === 200 && res.data && res.data.objects && res.data.objects.length > 0) {
                    const versionData = res.data.objects[0];
                    const newNoticeContent = versionData.notice || '';
                    
                    // 检查公告内容是否有更新
                    if (this.noticeClosedUntilUpdate && this.noticeContent !== newNoticeContent) {
                        // 公告已更新，重置关闭状态
                        this.noticeClosedUntilUpdate = false;
                        this.showNotice = true;
                        uni.removeStorageSync('ae_noticeClosedUntilUpdate');
                        uni.removeStorageSync('ae_noticeClosed');
                    }
                    
                    this.noticeContent = newNoticeContent;
                    console.log('获取公告成功:', this.noticeContent);
                    
                    // 内容更新后重新检查是否需要折叠
                    this.$nextTick(() => {
                        setTimeout(() => {
                            this.checkNoticeOverflow();
                        }, 300);
                    });
                } else {
                    console.error('获取公告失败: 数据格式不正确');
                }
            } catch (error) {
                console.error('获取公告失败:', error);
            }
        },
        
        // 更新滚动公告栏内容
        updateScrollNotice() {
            if (this.moreNoticeContent) {
                // 这里可以设置滚动公告栏的内容
                console.log('滚动公告栏内容已更新:', this.moreNoticeContent);
            }
        },
        
        navigateTo(url) {
            uni.navigateTo({
                url: url
            });
        },

        // 返回明日方舟主包首页
        goToArknights() {
            uni.reLaunch({
                url: '/pages/index/index'
            });
        },
        
        // 格式化云端时间 - 修改：支持时间戳转换
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
                console.error('格式化云端时间失败:', e, '原始时间:', time);
                return typeof time === 'string' ? time : '时间格式错误';
            }
        },
        
        // 加载本地数据
        async loadLocalData() {
            try {
                const data = uni.getStorageSync('ae_arknightsData');
                const localUpdateTime = uni.getStorageSync('ae_localUpdateTime');
                const cloudUpdateTime = uni.getStorageSync('ae_cloudUpdateTime');
                const version = uni.getStorageSync('ae_dataVersion');
                const url = uni.getStorageSync('ae_dataUrl');
                const enableGuessData = uni.getStorageSync('ae_enableGuessData');
                const guessData = uni.getStorageSync('ae_guessData');
                
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
                    
                    // 检查是否有更新
                    this.checkForDataUpdates(false, false);
                } else {
                    this.dataStatusMessage = '正在获取数据信息...';
                    setTimeout(() => {
                        this.checkForDataUpdates(false, false);
                    }, 2000);
                }
            } catch (e) {
                console.error('加载本地数据失败:', e);
                this.dataStatusMessage = '加载本地数据失败，正在尝试获取最新数据...';
                setTimeout(() => {
                    this.checkForDataUpdates(false, false);
                }, 2000);
            }
        },
        
        // 从知晓云获取数据URL和版本信息
        async getDataUrlFromMinapp() {
            try {
                const res = await this.knowCloudRequest('Version');
                console.log('知晓云版本信息响应:', res);
                if (res && res.objects && res.objects.length > 0) {
                    const versionData = res.objects[0];
                    console.log('版本数据:', versionData);
                    
                    // 获取更新时间，可能是时间戳或ISO字符串
                    let cloudUpdateTime = versionData.updated_at || versionData.created_at || '';
                    
                    // 如果获取到的时间是时间戳（数字），在这里不需要转换，由formatCloudTime函数处理
                    return {
                        url: versionData.url || '',
                        version: versionData.version || versionData.Version || '',
                        cloudUpdateTime: cloudUpdateTime
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
        
        async update() {
            uni.showToast({
                title: "敬请期待...",
                icon: 'error'
            })
        },
        
        // ===================== 自动更新相关方法 =====================
                
        // 加载自动更新设置
        loadAutoUpdateSetting() {
            try {
                const autoUpdateSetting = uni.getStorageSync('ae_autoUpdateSetting');
                console.log('Index页面加载自动更新设置:', autoUpdateSetting);
                
                if (autoUpdateSetting) {
                    this.autoUpdateEnabled = autoUpdateSetting.enabled || false;
                    this.autoUpdateConfig = autoUpdateSetting;
                    this.lastAutoUpdateCheckTime = autoUpdateSetting.lastCheckTime || '';
                    
                    // 更新状态显示
                    this.showAutoUpdateStatus = this.autoUpdateEnabled;
                    
                    if (this.autoUpdateEnabled) {
                        this.autoUpdateStatusMessage = '自动更新已开启';
                        this.autoUpdateStatusClass = 'status-success';
                        
                        // 计算下次检查时间
                        this.calculateNextUpdateTime();
                        
                        console.log('Index页面: 自动更新已开启');
                        
                        // 延迟执行自动更新检查
                        setTimeout(() => {
                            this.checkAndPerformAutoUpdate();
                        }, 3000);
                    } else {
                        this.autoUpdateStatusMessage = '自动更新已关闭';
                        this.autoUpdateStatusClass = 'status-error';
                        console.log('Index页面: 自动更新已关闭');
                    }
                } else {
                    console.log('Index页面: 未找到自动更新设置');
                    this.autoUpdateEnabled = false;
                    this.autoUpdateStatusMessage = '自动更新未配置';
                }
            } catch (e) {
                console.error('Index页面加载自动更新设置失败:', e);
                this.autoUpdateEnabled = false;
            }
        },
        
        // 计算下次检查时间
        calculateNextUpdateTime() {
            if (!this.lastAutoUpdateCheckTime) {
                this.nextAutoUpdateTime = '待检查';
                return;
            }
            
            try {
                const lastCheck = new Date(this.lastAutoUpdateCheckTime).getTime();
                const nextCheck = lastCheck + this.autoUpdateConfig.checkInterval;
                const now = new Date().getTime();
                
                if (now >= nextCheck) {
                    this.nextAutoUpdateTime = '立即';
                } else {
                    const diff = nextCheck - now;
                    const hours = Math.floor(diff / (1000 * 60 * 60));
                    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
                    
                    if (hours > 0) {
                        this.nextAutoUpdateTime = `${hours}小时${minutes}分钟后`;
                    } else {
                        this.nextAutoUpdateTime = `${minutes}分钟后`;
                    }
                }
            } catch (e) {
                console.error('计算下次检查时间失败:', e);
                this.nextAutoUpdateTime = '未知';
            }
        },
        
        // 检查并执行自动更新
        async checkAndPerformAutoUpdate() {
            try {
                // 获取自动更新设置
                const autoUpdateSetting = uni.getStorageSync('ae_autoUpdateSetting');
                const enabled = autoUpdateSetting?.enabled || false;
                
                if (!enabled) {
                    return; // 自动更新未启用
                }
                
                // 获取当前数据源
                const dataSource = uni.getStorageSync('ae_dataSource') || 'domestic';
                
                // 只有国内源或本地数据支持自动更新
                if (dataSource !== 'domestic' && dataSource !== 'local') {
                    console.log('当前数据源不支持自动更新');
                    return;
                }
                
                // 检查距离上次更新的时间
                const lastCheckTime = autoUpdateSetting.lastCheckTime;
                const now = new Date().getTime();
                let shouldCheckNow = true;
                
                if (lastCheckTime) {
                    const lastCheck = new Date(lastCheckTime).getTime();
                    const timeDiff = now - lastCheck;
                    const checkInterval = autoUpdateSetting.checkInterval || (24 * 60 * 60 * 1000);
                    shouldCheckNow = timeDiff >= checkInterval;
                }
                
                if (shouldCheckNow) {
                    console.log('执行自动更新检查');
                    
                    // 检查网络状态
                    const networkType = await this.getNetworkType();
                    const isWifi = networkType === 'wifi';
                    
                    // 如果设置为仅Wi-Fi且当前不是Wi-Fi，跳过检查
                    if (autoUpdateSetting.wifiOnly && !isWifi) {
                        console.log('当前为移动网络，跳过自动更新检查');
                        
                        // 更新检查时间，避免频繁检查
                        autoUpdateSetting.lastCheckTime = new Date().toISOString();
                        uni.setStorageSync('ae_autoUpdateSetting', autoUpdateSetting);
                        
                        // 更新状态显示
                        this.lastAutoUpdateCheckTime = this.formatLocalTime(new Date());
                        this.autoUpdateStatusMessage = '当前为移动网络，跳过检查';
                        this.autoUpdateStatusClass = 'status-warning';
                        this.calculateNextUpdateTime();
                        
                        return;
                    }
                    
                    // 执行检查
                    await this.checkForDataUpdates(true, true);
                }
            } catch (e) {
                console.error('检查自动更新失败:', e);
            }
        },
        
        // 获取网络类型
        getNetworkType() {
            return new Promise((resolve, reject) => {
                uni.getNetworkType({
                    success: (res) => {
                        resolve(res.networkType);
                    },
                    fail: (err) => {
                        console.error('获取网络类型失败:', err);
                        resolve('unknown');
                    }
                });
            });
        },
        
        // 检查数据更新（修复版）
        async checkForDataUpdates(forceCheck = false, isAutoUpdate = false) {
            console.log('检查数据更新，强制检查:', forceCheck, '自动更新:', isAutoUpdate);
            
            // 如果是自动更新检查，设置状态
            if (isAutoUpdate) {
                this.isCheckingUpdate = true;
                this.autoUpdateStatusMessage = '正在检查更新...';
                this.autoUpdateStatusClass = 'status-loading';
            }
            
            try {
                // 获取云端数据信息
                const versionInfo = await this.getDataUrlFromMinapp();
                
                if (versionInfo) {
                    this.dataUrl = versionInfo.url;
                    this.latestVersion = versionInfo.version;
                    this.cloudUpdateTime = versionInfo.cloudUpdateTime || this.cloudUpdateTime;
                    
                    // 检查是否有更新
                    const hasUpdate = await this.checkVersionUpdate(versionInfo.version);
                    
                    // 更新是否有更新的标志
                    this.hasUpdate = hasUpdate;
                    
                    // 如果是自动更新，根据设置处理
                    if (isAutoUpdate) {
                        const autoUpdateSetting = uni.getStorageSync('ae_autoUpdateSetting');
                        
                        // 更新检查时间
                        autoUpdateSetting.lastCheckTime = new Date().toISOString();
                        uni.setStorageSync('ae_autoUpdateSetting', autoUpdateSetting);
                        
                        // 更新状态显示
                        this.lastAutoUpdateCheckTime = this.formatLocalTime(new Date());
                        this.calculateNextUpdateTime();
                        
                        if (hasUpdate) {
                            // 检查网络类型
                            const networkType = await this.getNetworkType();
                            const isWifi = networkType === 'wifi';
                            
                            // 如果设置为自动下载或Wi-Fi环境
                            if (autoUpdateSetting.autoDownload || isWifi) {
                                // 自动下载更新
                                this.autoUpdateStatusMessage = '发现更新，正在自动下载...';
                                this.autoUpdateStatusClass = 'status-loading';
                                
                                await this.performAutoUpdate();
                            } else {
                                // 移动网络且不自动下载，显示提示
                                this.showAutoUpdatePrompt = true;
                                this.autoUpdatePromptMessage = '检测到新版本数据，当前为移动网络，是否立即下载更新？';
                                this.autoUpdateStatusMessage = '发现更新，等待用户确认';
                                this.autoUpdateStatusClass = 'status-warning';
                            }
                        } else {
                            this.autoUpdateStatusMessage = '数据已是最新版本';
                            this.autoUpdateStatusClass = 'status-success';
                        }
                    } else {
                        // 手动检查或强制检查
                        if (hasUpdate) {
                            // 自动更新关闭时，显示更新提示
                            if (!this.autoUpdateEnabled) {
                                console.log('自动更新关闭，有更新，显示更新提示');
                            }
                            
                            // 提示用户有更新
                            if (forceCheck) {
                                // 强制检查时直接提示
                                uni.showModal({
                                    title: '发现更新',
                                    content: `发现新版本数据 (${versionInfo.version})，是否立即更新？`,
                                    confirmText: '立即更新',
                                    cancelText: '稍后',
                                    success: (res) => {
                                        if (res.confirm) {
                                            this.downloadData();
                                        }
                                    }
                                });
                            }
                        } else if (forceCheck) {
                            // 已经是最新版本
                            uni.showToast({
                                title: '当前已是最新版本',
                                icon: 'success',
                                duration: 2000
                            });
                        }
                    }
                }
            } catch (error) {
                console.error('检查数据更新失败:', error);
                
                if (isAutoUpdate) {
                    this.autoUpdateStatusMessage = '检查更新失败';
                    this.autoUpdateStatusClass = 'status-error';
                } else if (forceCheck) {
                    uni.showToast({
                        title: '检查更新失败，请重试',
                        icon: 'none'
                    });
                }
            } finally {
                if (isAutoUpdate) {
                    this.isCheckingUpdate = false;
                    
                    // 3秒后清除状态消息
                    if (!this.autoUpdateStatusMessage.includes('发现更新')) {
                        setTimeout(() => {
                            this.autoUpdateStatusMessage = '';
                        }, 3000);
                    }
                }
            }
        },
        
        // 检查版本更新
        async checkVersionUpdate(cloudVersion) {
            try {
                // 获取本地版本
                const localVersion = uni.getStorageSync('ae_dataVersion') || '';
                
                console.log('版本检查:', {
                    localVersion,
                    cloudVersion,
                    areEqual: localVersion === cloudVersion
                });
                
                // 如果有本地版本，比较版本号
                if (localVersion && cloudVersion) {
                    return localVersion !== cloudVersion;
                }
                
                // 如果没有本地版本，但有云端版本，说明需要更新
                if (!localVersion && cloudVersion) {
                    return true;
                }
                
                // 如果都没有版本号，检查本地是否有数据
                const localData = uni.getStorageSync('ae_arknightsData');
                if (!localData || localData.length === 0) {
                    return true; // 没有本地数据，需要下载
                }
                
                return false;
            } catch (e) {
                console.error('检查版本更新失败:', e);
                return false;
            }
        },
        
        // 执行自动更新
        async performAutoUpdate() {
            try {
                // 下载数据
                await this.downloadData();
                
                // 更新状态
                this.autoUpdateStatusMessage = '数据更新成功';
                this.autoUpdateStatusClass = 'status-success';
                
                // 3秒后清除状态
                setTimeout(() => {
                    this.autoUpdateStatusMessage = '';
                }, 3000);
                
            } catch (error) {
                console.error('执行自动更新失败:', error);
                this.autoUpdateStatusMessage = '自动更新失败';
                this.autoUpdateStatusClass = 'status-error';
            }
        },
        
        // 手动检查更新
        manualCheckUpdate() {
            if (this.isCheckingUpdate || this.isDownloading) {
                uni.showToast({
                    title: '正在处理中，请稍候',
                    icon: 'none'
                });
                return;
            }
            
            // 触发检查更新
            this.checkForDataUpdates(true, false);
        },
        
        // 取消自动更新
        cancelAutoUpdate() {
            this.showAutoUpdatePrompt = false;
            this.autoUpdatePromptMessage = '';
            this.autoUpdateStatusMessage = '更新已取消';
            this.autoUpdateStatusClass = 'status-error';
            
            // 3秒后清除状态
            setTimeout(() => {
                this.autoUpdateStatusMessage = '';
            }, 3000);
        },
        
        // 确认自动更新
        confirmAutoUpdate() {
            this.showAutoUpdatePrompt = false;
            this.performAutoUpdate();
        },
        
        // 格式化本地时间
        formatLocalTime(date) {
            if (!date) return '';
            
            try {
                const d = date instanceof Date ? date : new Date(date);
                if (isNaN(d.getTime())) {
                    return '无效时间';
                }
                
                return `${d.getFullYear()}-${(d.getMonth()+1).toString().padStart(2, '0')}-${d.getDate().toString().padStart(2, '0')} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
            } catch (e) {
                console.error('格式化本地时间失败:', e);
                return '时间格式错误';
            }
        },
        
        // 清理自动更新定时器
        cleanupAutoUpdateTimer() {
            if (this.autoUpdateTimer) {
                clearInterval(this.autoUpdateTimer);
                this.autoUpdateTimer = null;
            }
        },
        
        // 下载数据
        async downloadData() {
			// #ifdef H5
			if (this.dataUrl && this.dataUrl.includes('raw.gitcode.com')) {
			    this.dataUrl = this.dataUrl.replace('https://raw.gitcode.com', '/gitcode');
			}
			// #endif
			
            // 确保有最新的版本信息
            if (!this.latestVersion) {
                try {
                    const versionInfo = await this.getDataUrlFromMinapp();
                    this.dataUrl = versionInfo.url;
                    this.latestVersion = versionInfo.version;
                    this.cloudUpdateTime = versionInfo.cloudUpdateTime || this.cloudUpdateTime;
                } catch (error) {
                    console.error('获取数据URL失败:', error);
                    this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series_End/blobs/3af48773f6c47271355834ab68b09092af2fec21/Box_Id.json';
                    this.latestVersion = this.latestVersion || '未知版本';
                }
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
                            const cleanedData = data.map(box => {
                                const cleanedBox = {};
                                for (const key in box) {
                                    if (key === 'Box_id') {
                                        cleanedBox[key] = String(box[key] || '');
                                    } else if (key.startsWith('character')) {
                                        cleanedBox[key] = box[key];
                                    } else {
                                        cleanedBox[key] = String(box[key] || '');
                                    }
                                }
                                return cleanedBox;
                            });
                            
                            this.characterData = cleanedData;
                            
                            this.currentVersion = this.latestVersion || this.currentVersion || '未知版本';
                            
                            uni.setStorage({
                                key: 'ae_arknightsData',
                                data: cleanedData,
                                success: () => {
                                    const now = new Date();
                                    this.localUpdateTime = `${now.getFullYear()}-${(now.getMonth()+1).toString().padStart(2, '0')}-${now.getDate().toString().padStart(2, '0')} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;
                                    
                                    uni.setStorage({ key: 'ae_localUpdateTime', data: this.localUpdateTime });
                                    uni.setStorage({ key: 'ae_cloudUpdateTime', data: this.cloudUpdateTime });
                                    uni.setStorage({ key: 'ae_dataVersion', data: this.currentVersion });
                                    uni.setStorage({ key: 'ae_dataUrl', data: this.dataUrl });
                                    
                                    // 下载成功后，清除更新提示
                                    this.hasUpdate = false;
                                    
                                    this.dataStatusMessage = `数据更新成功 ${this.currentVersion} (${cleanedData.length} 个盒号)`;
                                    uni.showToast({
                                        title: '数据更新成功',
                                        icon: 'success',
                                        duration: 2000
                                    });
                                    
                                    // 如果有自动更新，更新状态
                                    if (this.autoUpdateEnabled) {
                                        this.lastAutoUpdateCheckTime = this.formatLocalTime(new Date());
                                        this.calculateNextUpdateTime();
                                    }
                                },
                                fail: (e) => {
                                    console.error('存储数据失败:', e);
                                    this.dataStatusMessage = '数据下载成功但存储失败';
                                }
                            });
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
                    
                    if (this.characterData.length === 0) {
                        setTimeout(() => {
                            uni.showModal({
                                title: '网络错误',
                                content: '无法下载干员数据，请检查网络连接后重试',
                                showCancel: false
                            });
                        }, 500);
                    }
                }
            });
        },
        
        // ========== 工具箱管理相关方法 ==========
        // 开始编辑模式
        startEditingMode() {
            const savedConfig = uni.getStorageSync('ae_toolsConfig');
            if (savedConfig && savedConfig.length > 0) {
                this.toolsConfig = savedConfig;
            } else {
                this.initDefaultToolsConfig();
            }
            
            this.editingTools = JSON.parse(JSON.stringify(this.toolsConfig));
            this.isEditingTools = true;
            
            // 移除拖拽相关数据
            haptic('medium');
        },
        
        // 取消编辑
        cancelEditing() {
            this.isEditingTools = false;
            this.editingTools = [];
        },
        
        // 保存工具箱配置
        saveToolsConfig() {
            // 更新每个工具的order属性
            this.editingTools.forEach((tool, i) => {
                tool.order = i + 1;
            });
            
            // 保存到toolsConfig
            this.toolsConfig = JSON.parse(JSON.stringify(this.editingTools));
            
            // 保存到本地存储
            // 保存到本地存储（ae_ 前缀）
            uni.setStorageSync('ae_toolsConfig', this.toolsConfig);

            // 退出编辑模式
            this.isEditingTools = false;
            this.editingTools = [];
            
            uni.showToast({
                title: '配置已保存',
                icon: 'success',
                duration: 1500
            });
        },
        
        // 初始化默认工具箱配置
        initDefaultToolsConfig() {
            this.toolsConfig = [
                { 
                    id: 'search', 
                    name: '通行证查询', 
                    desc: '查询干员所在的盒号&盒内信息', 
                    icon: '/static/search-icon.png',
                    url: '/package_ArknightEnds/Search/Search',
                    visible: true,
                    order: 1,
                    showNew: false
                },
                {
                    id: 'list',
                    name: '通行证列表',
                    desc: '查看所有通行证及干员列表信息',
                    icon: '/static/list-tool.png',
                    url: '/package_ArknightEnds/list/list',
                    visible: true,
                    order: 2,
                    showNew: false
                },
                {
                    id: 'ae_calendar',
                    name: '终末地日历',
                    desc: '发售 / 复刻时间线',
                    icon: '/static/calendar-tool.png',
                    url: '/package_ArknightEnds/calendar/calendar',
                    visible: true,
                    order: 2.5,
                    showNew: true
                },
                { 
                    id: 'gacha', 
                    name: '模拟抽卡', 
                    desc: '模拟通行证抽卡', 
                    icon: '/static/gacha_simulated-tool.png',
                    url: '/package_ArknightEnds/gacha_simulated/gacha_simulated',
                    visible: true,
                    order: 3,
                    showNew: false
                },
                { 
                    id: 'questionnaire', 
                    name: '1月满意度调研', 
                    desc: '26年1月新功能满意度调查问卷', 
                    icon: '/static/questionnaire-tool.png',
                    url: '',
                    visible: true,
                    order: 4,
                    showNew: false,
                    specialAction: 'gotoquestionnaire'
                },
                { 
                    id: 'setting', 
                    name: '设置', 
                    desc: '部分特别功能个性化', 
                    icon: '/static/Setting-tool.png',
                    url: '/package_ArknightEnds/Setting/Setting',
                    visible: true,
                    order: 5,
                    showNew: false
                },
                { 
                    id: 'aboutus', 
                    name: '关于我们', 
                    desc: '了解我们及社媒账号', 
                    icon: '/static/aboutus-tool.png',
                    url: '/packageA/aboutus/aboutus',
                    visible: true,
                    order: 6,
                    showNew: false
                },
                { 
                    id: 'comingsoon', 
                    name: '敬请期待', 
                    desc: '新功能即将上线', 
                    icon: '/static/threepoint.png',
                    url: '',
                    visible: true,
                    order: 7,
                    showNew: false,
                    specialAction: 'update'
                }
            ];
            
            // 保存到本地存储（ae_ 前缀，避免与主包 toolsConfig 冲突）
            uni.setStorageSync('ae_toolsConfig', this.toolsConfig);
        },
        
        // 长按开始编辑
        startToolEditing(tool) {
            this.startEditingMode();
        },
        
        // 工具卡片触摸开始（简化版本，只保留长按功能）
        handleToolTouchStart(tool, event) {
            if (this.longPressTimer) {
                clearTimeout(this.longPressTimer);
            }
            
            this.longPressTimer = setTimeout(() => {
                this.startToolEditing(tool);
                this.isLongPressing = true;
            }, 800);
        },
        
        // 工具卡片触摸结束（简化版本）
        handleToolTouchEnd() {
            if (this.longPressTimer) {
                clearTimeout(this.longPressTimer);
                this.longPressTimer = null;
            }
            
            if (!this.isLongPressing) {
                this.isLongPressing = false;
            }
        },
        
        // 切换工具可见性
        toggleToolVisibility(index) {
            if (this.isEditingTools) {
                this.editingTools[index].visible = !this.editingTools[index].visible;
            } else {
                this.tempToolsConfig[index].visible = !this.tempToolsConfig[index].visible;
            }
        },
        
        // 删除工具
        deleteTool(index) {
            if (this.editingTools.length <= 1) {
                uni.showToast({
                    title: '至少保留一个工具',
                    icon: 'none'
                });
                return;
            }
            
            uni.showModal({
                title: '确认删除',
                content: '确定要删除这个功能吗？删除后可以重置恢复',
                success: (res) => {
                    if (res.confirm) {
                        this.editingTools.splice(index, 1);
                        uni.showToast({
                            title: '删除成功',
                            icon: 'success'
                        });
                    }
                }
            });
        },
        
        // 重置工具箱配置
        resetToolsConfig() {
            uni.showModal({
                title: '确认重置',
                content: '确定要重置工具箱配置为默认设置吗？',
                success: (res) => {
                    if (res.confirm) {
                        uni.removeStorageSync('ae_toolsConfig');
                        this.initDefaultToolsConfig();
                        if (this.isEditingTools) {
                            this.startEditingMode();
                        }
                        uni.showToast({
                            title: '已重置为默认配置',
                            icon: 'success'
                        });
                    }
                }
            });
        },
        
        // 上移工具
        moveToolUp(index) {
            if (index > 1) { // 因为两列布局，需要上移2位
                const targetIndex = index - 2;
                const temp = this.editingTools[index];
                this.editingTools[index] = this.editingTools[targetIndex];
                this.editingTools[targetIndex] = temp;
                
                // 轻微震动反馈
                haptic('light');
            }
        },

        // 下移工具
        moveToolDown(index) {
            if (index < this.editingTools.length - 2) { // 因为两列布局，需要下移2位
                const targetIndex = index + 2;
                const temp = this.editingTools[index];
                this.editingTools[index] = this.editingTools[targetIndex];
                this.editingTools[targetIndex] = temp;
                
                // 轻微震动反馈
                haptic('light');
            }
        },
        
        // 左移工具（从右侧移到左侧）
        moveToolLeft(index) {
            if (index % 2 === 1) { // 只有右侧卡片可以左移
                const targetIndex = index - 1;
                const temp = this.editingTools[index];
                this.editingTools[index] = this.editingTools[targetIndex];
                this.editingTools[targetIndex] = temp;
                
                // 轻微震动反馈
                haptic('light');
            }
        },

        // 右移工具（从左侧移到右侧）
        moveToolRight(index) {
            if (index % 2 === 0 && index < this.editingTools.length - 1) { // 只有左侧卡片且不是最后一个可以右移
                const targetIndex = index + 1;
                const temp = this.editingTools[index];
                this.editingTools[index] = this.editingTools[targetIndex];
                this.editingTools[targetIndex] = temp;
                
                // 轻微震动反馈
                haptic('light');
            }
        },
        
        // 获取可见的工具
        getVisibleTools() {
            return this.toolsConfig
                .filter(tool => tool.visible)
                .sort((a, b) => a.order - b.order);
        },

        // 获取可见工具数量
        getVisibleCount() {
            if (this.isEditingTools) {
                return this.editingTools.filter(tool => tool.visible).length;
            } else {
                return this.tempToolsConfig.filter(tool => tool.visible).length;
            }
        },

        // 处理工具点击
        handleToolClick(tool) {
            if (this.isEditingTools) return;
            if (tool.specialAction) {
                if (tool.specialAction === 'gotoquestionnaire') {
                    this.gotoquestionnaire();
                } else if (tool.specialAction === 'gotoaitools') {
                    this.gotoaitools();
                } else if (tool.specialAction === 'update') {
                    this.update();
                }
            } else if (tool.url) {
                this.navigateTo(tool.url);
            }
        },
		
		// 初始化彩蛋数组 - 请您在这里填写彩蛋内容
		    initEasterEggs() {
		        // 请在这里填写您的彩蛋文本数组
		        // 示例格式（请替换为您的实际彩蛋）：
		        this.easterEggs = [
		            '你今天拉电线了嘛？',
		            'big~super~cup!!!',
		            '正在连接至神经网络...',
		            '终于可以像别人一样在这里大胆写一些彩蛋了~',
		            '不管你是谁，请支持明日方舟和罗小黑战记二联！'
		        ];
		        // 注意：请至少保留一条彩蛋文本，否则随机选择时会出错
		    },
		    
		    // 处理版权文本点击
		    // 修改后：
		    handleCopyrightClick() {
		        this.copyrightClickCount++;
		        
		        // 存储点击次数（可选）
		        uni.setStorageSync('ae_copyrightClickCount', this.copyrightClickCount);
		        
		        // 每3次触发彩蛋
		        if (this.copyrightClickCount % 3 === 0) {
		            this.showEasterEggText(); // 改为调用新的方法名
		        }
		        
		        // 显示点击反馈
		        if (this.copyrightClickCount === 1 || this.copyrightClickCount === 2) {
		            uni.showToast({
		                title: `再点击${3 - this.copyrightClickCount}次`,
		                icon: 'none',
		                duration: 1000
		            });
		        }
		        
		        // 重置彩蛋计时器
		        // this.resetEasterEggTimer();
		    },
		    
		    // 修改后：
		    showEasterEggText() {
		        if (this.easterEggs.length === 0) {
		            console.warn('彩蛋数组为空，请先在 initEasterEggs 方法中填写彩蛋内容');
		            return;
		        }
		        
		        // 随机选择一条彩蛋
		        const randomIndex = Math.floor(Math.random() * this.easterEggs.length);
		        this.easterEggText = this.easterEggs[randomIndex];
		        this.showEasterEgg = true;
		        
		        // 将两条版权文本都替换为彩蛋文本
		        this.originalCopyrightTexts = [
		            this.easterEggText,
		            this.easterEggText
		        ];
		    },
		    
		    // 重置彩蛋计时器
		 //    resetEasterEggTimer() {
		 //        // 清除之前的计时器
		 //        if (this.easterEggTimer) {
		 //            clearTimeout(this.easterEggTimer);
		 //            this.easterEggTimer = null;
		 //        }
			// }
		        
		    //     // 设置新的计时器（1分钟后恢复）
		    //     this.easterEggTimer = setTimeout(() => {
		    //         this.restoreCopyright();
		    //     }, 60000); // 60秒 = 60000毫秒
		    // },
		    
		    // 恢复原始版权文本
		   //  restoreCopyright() {
		   //      this.originalCopyrightTexts = [
		   //          '终末地白名单凭证小助手',
		   //          '本工具为玩家自制，与官方无关'
		   //      ];
		   //      this.showEasterEgg = false;
		   //      this.copyrightClickCount = 0;
		        
		   //      // 清除本地存储的点击次数（可选）
		   //      uni.removeStorageSync('ae_copyrightClickCount');
		        
		   //      // 清除计时器
		   //      if (this.easterEggTimer) {
		   //          clearTimeout(this.easterEggTimer);
		   //          this.easterEggTimer = null;
		   //      }
		        
		   //      console.log('版权文本已恢复');
		   //  },
		    
		   // // 页面卸载时清理计时器
		   // onUnload() {
		   //     if (this.easterEggTimer) {
		   //         clearTimeout(this.easterEggTimer);
		   //         this.easterEggTimer = null;
		   //     }
		   // }
    }
}
</script>

<style scoped>
	/* 自定义导航栏 */
	.custom-nav-bar {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		z-index: 999;
		background: #ffffff;
		box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.04);
	}
	.nav-content {
		height: 44px;
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 10px;
	}
	.nav-left {
		width: auto;
		min-width: 10px;
		display: flex;
		align-items: center;
		flex-shrink: 0;
	}
	.nav-switch-btn {
		padding: 6px 12px;
		background: #EEF6FF;
		border: 1rpx solid #93C5FD;
		border-radius: 8px;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.nav-switch-text {
		font-size: 12px;
		color: #2563EB;
		font-weight: 500;
		line-height: 1.2;
		white-space: nowrap;
	}
	.nav-title-box {
		flex: 1;
		text-align: center;
		padding: 0 8px;
	}
	.nav-title-text {
		font-size: 15px;
		font-weight: 600;
		color: #18181b;
	}
	.nav-right {
		width: 72px;
	}

	.container {
		padding: 0 20rpx 30rpx;
		background-color: #f4f4f5;
		min-height: 100vh;
		box-sizing: border-box;
	}
	
	/* 公告栏样式 - 修改顶部间距 */
	.notice-section {
	    background-color: #fff;
	    border-radius: 16rpx;
	    padding: 30rpx;
	    box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
	    margin-bottom: 30rpx;
	    margin-top: 20rpx;
	    overflow: hidden;
	    position: relative; /* 为伪元素提供定位 */
	}
	
	.notice-section::before {
	    content: '';
	    position: absolute;
	    left: 0;
	    top: 0;
	    width: 8rpx; /* 边框宽度 */
	    height: 100%;
	    background: linear-gradient(
	        to bottom,
	        #FFFF00 0%,    /* 黄色，占前1/3 */
	        #FFFF00 33.33%,
	        #00FFFF 33.33%, /* 青色，占中间1/3 */
	        #00FFFF 66.66%,
	        #FF00FF 66.66%, /* 品红色，占后1/3 */
	        #FF00FF 100%
	    );
	    border-radius: 16rpx 0 0 16rpx; /* 与容器圆角匹配 */
	}
	
	.notice-header {
		display: flex;
		align-items: center;
		margin-bottom: 0rpx;
		position: relative;
	}
	
	/* 关闭按钮样式 */
	.notice-close {
		margin-right: 15rpx;
		padding: 8rpx;
		border-radius: 50%;
		transition: background-color 0.3s;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	
	.notice-close:active {
		background-color: #f4f4f5;
	}
	
	.close-btn {
		width: 32rpx;
		height: 32rpx;
		font-size: 32rpx;
		color: #a1a1aa;
		display: flex;
		align-items: center;
		justify-content: center;
		line-height: 1;
	}
	
	.notice-icon {
		width: 36rpx;
		height: 36rpx;
		margin-right: 15rpx;
	}
	
	.notice-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #18181b;
		flex: 1;
	}
	
	/* 展开/收起按钮样式 */
	.notice-toggle {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		color: #18181b;
		font-size: 26rpx;
		padding: 8rpx;
		border-radius: 8rpx;
		transition: background-color 0.3s;
	}
	
	.notice-toggle:active {
		background-color: #f4f4f5;
	}
	
	.toggle-text {
		margin-right: 10rpx;
	}
	
	.toggle-icon {
		width: 24rpx;
		height: 24rpx;
		transition: transform 0.3s ease;
	}
	
	.toggle-icon.rotate {
		transform: rotate(180deg);
	}
	
	/* 整个公告栏展开/收起过渡动画 */
	.notice-expandable {
		max-height: 0;
		opacity: 0;
		overflow: hidden;
		transition: all 0.3s ease-in-out;
	}
	
	.notice-expandable.expanded {
		max-height: 1000rpx; /* 足够大的值，确保能容纳展开的内容 */
		opacity: 1;
	}
	
	.notice-expandable.collapsed {
		max-height: 0;
		opacity: 0;
	}
	
	/* 滚动公告栏样式调整 */
	.notice-bar-container {
		margin-bottom: 20rpx;
		margin-top: 20rpx;
		transition: all 0.3s ease-in-out;
	}
	
	.notice-content {
		position: relative;
		transition: all 0.3s ease-in-out;
	}
	
	.notice-text {
		font-size: 28rpx;
		color: #52525b;
		line-height: 1.6;
		display: block;
		word-break: break-word;
		transition: all 0.3s ease-in-out;
	}
	
	/* 文本展开/收起过渡动画 */
	.text-collapsed {
		display: -webkit-box;
		-webkit-line-clamp: 3;
		-webkit-box-orient: vertical;
		overflow: hidden;
		text-overflow: ellipsis;
		max-height: 150rpx; /* 3行高度估算 */
		opacity: 0.9;
	}
	
	.text-expanded {
		max-height: 1000rpx; /* 足够大的值，确保能容纳所有文本 */
		opacity: 1;
	}
	
	/* 展开/收起按钮 */
	.notice-expand {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		margin-top: 15rpx;
		padding-top: 10rpx;
		width: 100%;
		transition: all 0.3s ease-in-out;
	}
	
	.expand-btn {
		background-color: #f4f4f5;
		color: #52525b;
		font-size: 24rpx;
		border-radius: 10rpx;
		padding: 15rpx 20rpx;
		border: none;
		line-height: 1;
		transition: all 0.2s ease;
	}
	
	.expand-btn:active {
		background-color: #e4e4e7;
		transform: scale(0.95);
	}
	
	/* 轮播图样式 */
	.carousel-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 20rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	.carousel {
		height: 300rpx;
		border-radius: 12rpx;
		overflow: hidden;
	}
	
	.carousel-image {
		width: 100%;
		height: 100%;
		border-radius: 12rpx;
	}
	
	/* 工具箱区域样式 - 修改为两列布局 */
	.tools-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}
	
	/* 工具箱标题区域 */
	.tools-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 30rpx;
	}
	
	.tools-manage-btn {
		display: flex;
		align-items: center;
		padding: 10rpx 20rpx;
		background-color: #f4f4f5;
		border-radius: 8rpx;
		transition: background-color 0.3s;
	}
	
	.tools-manage-btn:active {
		background-color: #e4e4e7;
	}
	
	.manage-text {
		font-size: 24rpx;
		color: #52525b;
		margin-right: 10rpx;
	}
	
	.manage-icon {
		width: 24rpx;
		height: 24rpx;
	}
	.empty-placeholder {
	    grid-column: 1 / -1; /* 占满两列 */
	    display: flex;
	    flex-direction: column;
	    align-items: center;
	    justify-content: center;
	    padding: 60rpx 0;
	    background-color: #fafafa;
	    border-radius: 16rpx;
	}
	
	.test-icon {
	    width: 120rpx;
	    height: 120rpx;
	    margin-bottom: 20rpx;
	}
	
	.empty-text {
	    font-size: 28rpx;
	    color: #a1a1aa;
	    text-align: center;
	    line-height: 1.6;
	}
	
	.tools-title {
		font-size: 32rpx;
		font-weight: bold;
		color: #18181b;
	}
	
	.tools-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 20rpx;
	}
	
	.tool-card {
		display: flex;
		flex-direction: column;
		align-items: center;
		background-color: #fafafa;
		border-radius: 16rpx;
		padding: 30rpx 20rpx;
		transition: all 0.3s;
		text-align: center;
		height: 200rpx;
		justify-content: flex-start;
		box-shadow: 0 4rpx 10rpx rgba(0, 0, 0, 0.05);
		position: relative;
	}
	
	.tool-card:active {
		background-color: #FDFDE6;
		transform: scale(0.98);
	}
	
	.tool-icon-container {
		position: relative; /* 为New图标提供定位上下文 */
		width: 80rpx;
		height: 80rpx;
		border-radius: 16rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-bottom: 15rpx;
		flex-shrink: 0;
	}
	
	.tool-icon {
		width: 50rpx;
		height: 50rpx;
	}
	
	/* New图标样式 */
	.new-badge {
		position: absolute;
		top: -40rpx;
		right: 120rpx;
		width: 70rpx;
		height: 70rpx;
		z-index: 10;
	}
	
	.tool-info {
		display: flex;
		flex-direction: column;
		align-items: center;
		width: 100%;
		height: 90rpx;
		justify-content: flex-start;
	}
	
	.tool-name {
		font-size: 28rpx;
		font-weight: bold;
		color: #18181b;
		margin-bottom: 8rpx;
		line-height: 1.4;
		height: 40rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
	}
	
	.tool-desc {
		font-size: 24rpx;
		color: #a1a1aa;
		line-height: 1.4;
		width: 100%;
		text-align: center;
		/* 自适应调整字体大小 */
		word-wrap: break-word;
		word-break: break-word;
	}
	
	/* 数据状态区域 — 白底细边 */
	.data-status {
		background: #ffffff !important;
		background-image: none !important;
		border: 1rpx solid #E5E7EB;
		border-radius: 12rpx;
		padding: 20rpx 24rpx;
		margin-bottom: 24rpx;
		font-size: 24rpx;
		color: #374151;
		font-weight: normal;
	}
	
	/* 更新提示样式 */
	.update-hint {
		background-color: #fff6e6;
		border-left: 8rpx solid #ff9900;
		padding: 20rpx;
		margin-bottom: 20rpx;
		border-radius: 8rpx;
		animation: fadeIn 0.5s ease-in-out;
	}
	
	.hint-text {
		color: #ff9900;
		font-size: 26rpx;
		font-weight: bold;
	}
	
	@keyframes fadeIn {
		from {
			opacity: 0;
			transform: translateY(-10px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
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
		background-color: #FBFB45;
		color: #000;
		border-radius: 50rpx;
		font-size: 22rpx;
		padding: 8rpx 20rpx;
		line-height: 1.4;
	}
	
	.data-details {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
		border-top: 1rpx solid rgba(251, 251, 69, 0.2);
		padding-top: 15rpx;
	}
	
	.data-detail-item {
		font-size: 22rpx;
		color: #52525b;
	}
	
	.update-section {
		text-align: center;
		margin-top: 32rpx;
	}

	/* 终末地主按钮：亮黄底 + 黑字 */
	.update-btn {
		width: 100%;
		background: #FBFB45 !important;
		background-image: none !important;
		color: #000 !important;
		border: 2rpx solid #000 !important;
		border-radius: 12rpx !important;
		font-size: 28rpx !important;
		font-weight: 400 !important;
		padding: 22rpx 0 !important;
		margin: 0 !important;
		line-height: 1.4 !important;
		min-height: auto !important;
	}
	
	/* 关于我们链接 */
	.about-link-section {
		text-align: center;
	}
	
	.about-link {
		font-size: 26rpx;
		color: #18181b;
		text-decoration: underline;
	}
	
	.white-button {
		width: 80%;
		border: 0 rgba(0, 0, 0, 0);
	}
	
	/* 底部版权 */
	.footer-section {
		text-align: center;
		padding: 30rpx 0;
	}
	
	.copyright-text {
		font-size: 24rpx;
		color: #a1a1aa;
		display: block;
		margin-bottom: 10rpx;
	}
	
	.copyright-text:last-child {
		margin-bottom: 0;
	}
	
	/* ========== 工具箱编辑模式样式（新增） ========== */
	/* 编辑模式下的工具箱 */
	.tools-grid.editing {
		position: relative;
		min-height: 400rpx;
	}
	
	/* 编辑模式下的工具卡片 */
	.tool-card.editing-card {
		position: relative;
		transition: transform 0.2s ease;
		background-color: #fff;
		border: 2rpx solid #f4f4f5;
		z-index: 1;
	}
	
	/* 删除按钮 */
	.delete-btn {
		position: absolute;
		right: -10rpx;
		top: -10rpx;
		width: 40rpx;
		height: 40rpx;
		background-color: #FF6B6B;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 10;
		box-shadow: 0 2rpx 8rpx rgba(255, 107, 107, 0.3);
	}
	
	.delete-text {
		color: #fff;
		font-size: 24rpx;
		font-weight: bold;
		line-height: 1;
	}
	
	/* 编辑模式下的工具图标容器 */
	.tool-icon-container.editing {
		margin-bottom: 10rpx;
	}
	
	/* 编辑模式下的工具信息 */
	.tool-info.editing {
		height: auto;
		min-height: 90rpx;
	}
	
	/* 编辑控制 */
	.tool-edit-controls {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 10rpx;
		padding-top: 10rpx;
		border-top: 1rpx solid #f4f4f5;
	}
	
	.visibility-label {
		font-size: 22rpx;
		color: #52525b;
	}
	
	.visibility-switch {
		transform: scale(0.7);
	}
	
	/* 长按提示 */
	.longpress-hint {
		position: absolute;
		bottom: 10rpx;
		left: 0;
		right: 0;
		text-align: center;
	}
	
	.hint-text {
		font-size: 20rpx;
		color: #18181b;
		background-color: rgba(251, 251, 69, 0.1);
		padding: 4rpx 12rpx;
		border-radius: 20rpx;
		display: inline-block;
	}
	
	/* 排序箭头按钮 */
	.sort-arrows {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		pointer-events: none; /* 让点击事件穿透到下层 */
	}
	
	.arrow-btn {
		position: absolute;
		width: 40rpx;
		height: 40rpx;
		background-color: rgba(251, 251, 69, 0.8);
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		pointer-events: auto; /* 恢复点击事件 */
		box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.2);
		z-index: 5;
		transition: all 0.2s;
	}
	
	.arrow-btn:active {
		background-color: rgba(251, 251, 69, 1);
		transform: scale(0.9);
	}
	
	.arrow-up {
		top: -20rpx;
		left: 50%;
		transform: translateX(-50%);
	}
	
	.arrow-down {
		bottom: -20rpx;
		left: 50%;
		transform: translateX(-50%);
	}
	
	.arrow-left {
		top: 50%;
		left: -20rpx;
		transform: translateY(-50%);
	}
	
	.arrow-right {
		top: 50%;
		right: -20rpx;
		transform: translateY(-50%);
	}
	
	.arrow-icon {
		width: 20rpx;
		height: 20rpx;
	}
	
	/* 编辑模式控制栏 */
	.edit-controls-bar {
		margin-top: 30rpx;
		padding: 20rpx;
		background-color: #f8f9fa;
		border-radius: 12rpx;
	}
	
	.edit-info {
		margin-bottom: 20rpx;
	}
	
	.info-text {
		font-size: 24rpx;
		color: #52525b;
		display: block;
		margin-bottom: 8rpx;
	}
	
	.edit-buttons {
		display: flex;
		gap: 15rpx;
	}
	
	.edit-btn {
		flex: 1;
		border-radius: 8rpx;
		padding: 15rpx;
		font-size: 26rpx;
		border: none;
	}
	
	.edit-btn.reset {
		background-color: #FFE6E6;
		color: #FF6B6B;
	}
	
	.edit-btn.cancel {
		background-color: #f4f4f5;
		color: #52525b;
	}
	
	.edit-btn.save {
		background-color: #FBFB45;
		color: #000;
	}


/* ===== endspace-theme 对齐 ===== */
.container.ef-page,
.ef-page {
	min-height: 100vh;
	background: #fafafa !important;
	color: #18181b !important;
	position: relative;
	z-index: 1;
}
/* 微网格 */
.container.ef-page::before,
.ef-page::before {
	content: '';
	position: fixed;
	inset: 0;
	pointer-events: none;
	z-index: 0;
	background-image: 
		linear-gradient(rgba(0,0,0,0.03) 1px, transparent 1px),
		linear-gradient(90deg, rgba(0,0,0,0.03) 1px, transparent 1px);
	background-size: 40px 40px;
}

/* 卡片 */
.tool-card,
.notice-section,
.data-status,
.ef-card,
.result-item,
.box-item,
.box-card,
.gacha-section,
.search-section,
.timeline-card,
.stat-item,
.month-selector {
	background: #ffffff !important;
	background-image: none !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 12rpx !important;
	box-shadow: 0 1px 2px rgba(0,0,0,.04), 0 2px 4px rgba(0,0,0,.04) !important;
	color: #18181b !important;
	position: relative;
	z-index: 1;
}
.tool-card::after,
.ef-card::before,
.box-item::before { display: none !important; }
.tool-card:active {
	transform: none !important;
	border-color: #FBFB45 !important;
	box-shadow: 0 4px 8px rgba(0,0,0,.08), 0 0 0 1rpx #FBFB45 !important;
}

/* 文字 */
.tool-name,
.card-name,
.month-text,
.character-name-main,
.box-card-title,
.result-title,
.box-id,
.status-text {
	color: #18181b !important;
	font-weight: 700 !important;
}
.tool-desc,
.character-count,
.section-label,
.info-label,
.stat-label,
.card-date,
.card-type,
.label {
	color: #a1a1aa !important;
}
.value,
.info-value { color: #18181b !important; }

/* 导航 */
.custom-nav-bar {
	background: #ffffff !important;
	border-bottom: 1rpx solid #e4e4e7 !important;
	box-shadow: none !important;
}
.nav-title-text { color: #18181b !important; font-weight: 700 !important; }
.nav-switch-btn {
	background: #EEF6FF !important;
	border: 1rpx solid #93C5FD !important;
	border-radius: 8px !important;
}
.nav-switch-text { color: #2563EB !important; }

/* 主按钮：#FBFB45 亮柠檬黄 + 黑字黑边 */
.update-btn,
.search-btn,
.reset-btn,
.gacha-btn,
.ef-btn-primary,
.go-to-settings-btn {
	background: #FBFB45 !important;
	background-image: none !important;
	color: #000000 !important;
	border: 1rpx solid #000000 !important;
	border-radius: 10rpx !important;
	font-weight: 700 !important;
	box-shadow: none !important;
	clip-path: none !important;
	min-height: auto !important;
}
.update-btn::after,
.search-btn::after,
.reset-btn::after,
.gacha-btn::after { border: none; }
.update-btn:active,
.search-btn:active,
.gacha-btn:active { opacity: 0.88; transform: none; }

/* 次要按钮 */
.option-btn,
.view-info-btn,
.favorite-btn,
.ef-btn,
.page-btn,
.sort-order-btn,
.history-btn,
.action-btn {
	background: #ffffff !important;
	color: #18181b !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 10rpx !important;
	box-shadow: none !important;
	font-weight: 600 !important;
}
.option-btn.active {
	background: #18181b !important;
	color: #ffffff !important;
	border-color: #18181b !important;
}
.option-btn::after,
.page-btn::after,
.view-info-btn::after { border: none; }

/* 盒类型标签 */
.box-card-type,
.box-type {
	background: #f4f4f5 !important;
	color: #52525b !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 6rpx !important;
}

/* 本盒卡面 chips */
.box-card-panel {
	background: #fafafa !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 10rpx !important;
}
.box-card-panel-title { color: #52525b !important; font-weight: 700 !important; }
.box-card-panel-meta { color: #a1a1aa !important; }
.box-card-item {
	background: #ffffff !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 6rpx !important;
}
.box-card-name { color: #18181b !important; }
.box-card-item.drawn {
	background: #f4f4f5 !important;
	opacity: 0.7;
}
.box-card-item.drawn .box-card-name {
	color: #a1a1aa !important;
	text-decoration: line-through;
}

/* 更新提示条 */
.update-hint {
	background: rgba(251,251,69,0.15) !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 10rpx !important;
}
.update-hint .hint-text { color: #52525b !important; font-weight: 600 !important; }

/* 抽卡舞台：白底 + 亮黄描边 */
.card-back {
	background: 
		linear-gradient(145deg, rgba(251,251,69,0.12) 0%, transparent 40%),
		#ffffff !important;
	border: 2rpx solid #FBFB45 !important;
}
.card-back-text { color: #18181b !important; }
.card-front {
	background: 
		radial-gradient(circle at 30% 20%, rgba(251,251,69,0.18), transparent 45%),
		#ffffff !important;
	border: 2rpx solid #FBFB45 !important;
	color: #18181b !important;
}
.character-name { color: #18181b !important; text-shadow: none !important; }
.hot-tag {
	background: #18181b !important;
	color: #FBFB45 !important;
	border-radius: 4rpx !important;
}
.card-stage.is-revealed .card-rarity-ring {
	background: radial-gradient(circle, rgba(251,251,69,0.35) 0%, transparent 70%) !important;
}

/* 列表 */
.box-card-header {
	border-bottom: 1rpx solid #e4e4e7 !important;
	padding-bottom: 12rpx;
	margin-bottom: 16rpx;
}
.box-card-title { color: #18181b !important; font-size: 30rpx !important; }
.box-image-wrapper {
	background: #f4f4f5 !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 10rpx !important;
}
.expand-text { color: #52525b !important; }
.box-info-grid {
	background: #fafafa !important;
	border-radius: 10rpx !important;
}
.replicate-line { color: #52525b !important; }
.character-card {
	background: #ffffff !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 10rpx !important;
}
.elite-tag {
	color: #52525b !important;
	background: #f4f4f5 !important;
	border: 1rpx solid #e4e4e7 !important;
}
.price-tag {
	color: #52525b !important;
	background: #f4f4f5 !important;
	border: none !important;
}

/* 日历 */
.timeline-dot { background: #18181b !important; box-shadow: none !important; }
.timeline-line { background: #e4e4e7 !important; }
.stat-num { color: #18181b !important; }
.month-arrow { color: #52525b !important; }

/* 筛选输入 */
.picker-text,
.picker-display,
.search-input-container {
	background: #ffffff !important;
	border: 1rpx solid #e4e4e7 !important;
	border-radius: 10rpx !important;
	color: #18181b !important;
}

/* 列表信息网格颜色统一 */
.section-title { color: #18181b !important; }

</style>