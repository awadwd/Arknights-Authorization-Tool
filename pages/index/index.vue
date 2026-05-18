<template>
	<view :class="['container', 'theme-' + themeMode]">
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
						show-get-more @getmore="gotoMoreNotice()" more-text="查看更多>" more-color="#409EFF"
						:text="moreNoticeContent"
						speed="75"
						background-color='#e8f4ff'
						color="#409EFF"
						show-close
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
				<text class="tools-title">明日方舟通行证-工具箱</text>
				<view class="tools-manage-btn" @click="toggleToolManagement">
					<text class="manage-text">{{ isEditingTools ? '完成' : '管理' }}</text>
					<image class="manage-icon" src="/static/Setting-tool.png"></image>
				</view>
			</view>

			<!-- 普通模式下的工具箱 -->
			<view class="tools-grid" v-if="!isEditingTools">
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
								color="#409EFF"
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

		<!-- 更新数据按钮 -->
		<view class="update-section">

		  <button class="update-btn" @click="downloadData"><image src="../../static/download.753e06.png" class="update-btn-img"></image>更新干员数据</button>
		</view>

		<!-- 数据状态 - 整合所有信息 -->
		<view class="data-status" v-if="dataStatusMessage || characterData.length > 0 || localUpdateTime || cloudUpdateTime || currentVersion">
			<!-- 更新提示区域（当有更新且自动更新关闭时显示） -->
			<view class="update-hint" v-if="hasUpdate && !autoUpdateEnabled">
				<text class="hint-text">当前数据版本有更新，您可以点击上方的更新干员数据按钮来手动更新</text>
			</view>

			<view class="data-status-main" v-if="dataStatusMessage">
				<view class="status-text" style="color: #333;">{{ dataStatusMessage }}</view>
				<view class="data-status-actions">
					<button v-if="dataStatusMessage && (dataStatusMessage.includes('失败') || dataStatusMessage.includes('错误'))"
							class="update-btn small" @click="downloadData">重试下载</button>
				</view>
			</view>

			<view class="data-details" v-if="localUpdateTime || cloudUpdateTime || currentVersion || characterData.length > 0">
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
		<!-- 调研问卷模态框 -->
		<view class="modal-mask" v-if="showQuestionnaireModal" @click="closeQuestionnaireModal">
			<view class="modal-content questionnaire-modal" @click.stop>
				<view class="modal-header">
					<text class="modal-title">调研问卷</text>
					<text class="modal-close" @click="closeQuestionnaireModal">×</text>
				</view>
				<scroll-view class="modal-body" scroll-y>
					<view
						class="questionnaire-item"
						v-for="(item, index) in questionnaireList"
						:key="index"
						@click="goToQuestionnaireItem(item)"
					>
						<view class="questionnaire-info">
							<text class="questionnaire-title">{{ item.title }}</text>
							<view class="questionnaire-status" v-if="item.status !== undefined">
								<text v-if="item.status === 0" class="status-tag tag-paused">暂停回收</text>
								<text v-else-if="item.status === 1" class="status-tag tag-open">开放填写中</text>
								<text v-else-if="item.status === 2" class="status-tag tag-permanent">永久开放</text>
							</view>
							<text class="questionnaire-content" v-if="item.content">{{ item.content }}</text>
						</view>
						<view class="questionnaire-right">
							<image
								v-if="index === 0"
								class="questionnaire-new"
								src="/static/new.png"
								mode="aspectFit"
							></image>
							<image class="questionnaire-arrow" src="/static/right.png" mode="aspectFit"></image>
						</view>
					</view>
					<view class="empty-tip" v-if="questionnaireList.length === 0">
						<text>暂无调研问卷</text>
					</view>
				</scroll-view>
			</view>
		</view>
	</view>
</template>

<script>
	// 注意：主题配置已移至 index.vue 内部实现，不再使用外部 theme.js 文件

	const getBaseUrl = () => {
	  // #ifdef H5
	  console.log('当前是 H5 环境，使用代理前缀 /api');
	  return '/api';
	  // #endif
	  // #ifndef H5
	  console.log('当前是非 H5 环境，使用 GitHub 直链');
	  return 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main';
	  // #endif
	};

	// GitHub 直链数据源配置（开源版本使用）
	const GITHUB_DATA_SOURCES = {
	  boxIdUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/Box_Id.json',
	  guessNewBoxUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/guessNew_Box_Id.json',
	  searchWordUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/searchWord.json'
	};

	// 开源版本：知晓云配置已禁用，商业版请配置知晓云
	const getClientId = () => {
	  return ''; // 开源版本不包含密钥
	};

	// 知晓云配置（开源版本表名为空，商业版请配置知晓云表名）
	const KNOW_CLOUD_CONFIG = {
	  clientId: '',
	  tableNames: {
	    choearth_notice: '',      // 开源版本禁用
	    more_notice: '',          // 开源版本禁用
	    Version: '',              // 开源版本使用 GitHub 直链
	    AiToolsConfig: 'AiToolsConfig',  // 商业版功能
	    questionnaire: 'questionnaire'  // 商业版功能
	  }
	};

export default {
    data() {
        return {
            // 主题模式
            themeMode: 'simple', // 默认简约风格，会在 onLoad 中被 loadThemeSetting 覆盖
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
            // 调研问卷
            questionnaireList: [],
            showQuestionnaireModal: false,
            isDownloading: false,
			// 彩蛋相关数据
			            easterEggs: [], // 您将在这里填写彩蛋数组
			            showEasterEgg: false, // 是否显示彩蛋
			            easterEggText: '', // 当前显示的彩蛋文本
			            copyrightClickCount: 0, // 版权点击次数
			            originalCopyrightTexts: [ // 原始版权文本
			                '方舟通行证谷子查询工具',
			                '本工具为玩家自制，与官方无关'
			            ],
        }
    },

    // 页面生命周期 - 下拉刷新
    onPullDownRefresh() {
        this.handleRefresh();
    },

    onLoad() {
        console.log('首页加载，开始初始化数据...');
        this.loadLocalData();
        this.fetchNotice();
        this.fetchMoreNotice(); // 新增：获取更多公告
        this.fetchCarouselImages(); // 新增：获取轮播图数据

		// 新增：加载自动更新设置
        this.loadAutoUpdateSetting();

		// 加载主题设置
		this.loadThemeSetting();

		// 监听主题变化事件
		uni.$on('themeChanged', (theme) => {
			this.themeMode = theme;
		});

		// 初始化彩蛋数组
		this.initEasterEggs();

		// 初始化工具箱配置（只调用一次，会自动读取本地保存的配置）
		this.initDefaultToolsConfig();
		this.loadAiToolsConfig();

		// 从本地存储读取彩蛋点击次数（可选）
		const savedCount = uni.getStorageSync('copyrightClickCount');
		if (savedCount) {
		    this.copyrightClickCount = savedCount;
		}

        // 检查是否已关闭公告
        const noticeClosed = uni.getStorageSync('noticeClosed');
        const noticeClosedUntilUpdate = uni.getStorageSync('noticeClosedUntilUpdate');

        if (noticeClosed) {
            this.showNotice = false;
        }

        if (noticeClosedUntilUpdate) {
            this.noticeClosedUntilUpdate = noticeClosedUntilUpdate;
        }

        // 从本地存储读取公告栏展开状态
        const noticeExpandedState = uni.getStorageSync('noticeSectionExpanded');
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
        uni.showShareMenu({
            withShareTicket: true,
            menus: ['shareAppMessage', 'shareTimeline']
        });

        // 显示一次长按提示
        const hasShownHint = uni.getStorageSync('hasShownLongPressHint');
        if (!hasShownHint) {
            setTimeout(() => {
                this.showLongPressHint = true;
                // 3秒后隐藏提示
                setTimeout(() => {
                    this.showLongPressHint = false;
                    uni.setStorageSync('hasShownLongPressHint', true);
                }, 3000);
            }, 2000);
        }
    },

    // 分享给好友
    onShareAppMessage() {
        return {
            title: '方舟通行证谷子查询工具',
            path: '/pages/index/index',
            imageUrl: ''
        }
    },

    // 分享到朋友圈
    onShareTimeline() {
        return {
            title: '方舟通行证谷子查询工具',
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

               // 动态获取基础 URL
               const baseUrl = getBaseUrl();

               const defaultOptions = {
                   url: `${baseUrl}/table/${tableNameKey}/record/`,
                   method: 'GET',
                   header: {
                       'X-Hydrogen-Client-ID': KNOW_CLOUD_CONFIG.clientId,
                       'Content-Type': 'application/json'
                   },
                   timeout: 15000
               };

               // 合并参数，确保查询参数正确传递
               const finalOptions = { ...defaultOptions, ...options };

               // 处理查询参数 - 使用兼容方式替代 URLSearchParams
               if (options.data) {
                   const params = [];
                   Object.keys(options.data).forEach(key => {
                       params.push(`${encodeURIComponent(key)}=${encodeURIComponent(options.data[key])}`);
                   });
                   finalOptions.url = `${defaultOptions.url}?${params.join('&')}`;
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
		    const enabled = await this.fetchAiToolsConfig();
		    const index = this.toolsConfig.findIndex(item => item.id === 'questionnaire');
		    if (index !== -1) {
		        const tool = this.toolsConfig[index];
		        if (enabled) {
					// #ifdef MP-WEIXIN
		            // 启用新配置（AI帮查）
		            tool.name = 'AI帮查';
		            tool.icon = '/static/aisearch-icon.png';
		            tool.url = '/pages/Ai_Search/Ai_Search';
		            tool.specialAction = '';
					// #endif
					// #ifdef H5 || APP-PLUS
					tool.url = '';
					tool.specialAction = 'gotoWebAiTools';
					// #endif
		        } else {
		            // 使用原配置（前往元宝小程序）
		            tool.name = '前往元宝小程序帮查>';
		            tool.desc = '元宝小程序提供服务，仅供娱乐，精度较低、无法联系上下文，慎用！！';
		            tool.icon = '';
		            tool.url = '';
		            tool.specialAction = 'gotoquestionnaire';
		        }
		    }
		    this.toolsConfig = [...this.toolsConfig];
		},

		// 获取调研问卷列表
		async fetchQuestionnaireList() {
		    try {
		        const tableName = KNOW_CLOUD_CONFIG.tableNames.questionnaire;
		        const res = await uni.request({
		            url: `${getBaseUrl()}/table/${tableName}/record/?limit=50`,
		            method: 'GET',
		            header: {
		                'X-Hydrogen-Client-ID': KNOW_CLOUD_CONFIG.clientId,
		                'Content-Type': 'application/json'
                }
            });
		        if (res.data.objects && res.data.objects.length > 0) {
		            // 按创建时间倒序排列，最新的在前
		            const sorted = res.data.objects.sort((a, b) => (b.created_at || 0) - (a.created_at || 0));
		            return sorted;
		        }
		        return [];
		    } catch (error) {
		        console.error('获取调研问卷列表失败', error);
		        return [];
		    }
		},

		// 加载调研问卷数据并弹出模态框
		async gotoquestionnaire() {
		    // 先加载问卷列表
		    if (this.questionnaireList.length === 0) {
		        this.questionnaireList = await this.fetchQuestionnaireList();
		    }
		    // 弹出模态框
		    this.showQuestionnaireModal = true;
		},

		// 关闭调研问卷模态框
		closeQuestionnaireModal() {
		    this.showQuestionnaireModal = false;
		},

		// 跳转到指定问卷小程序
		goToQuestionnaireItem(item) {
		    if (!item.appId || !item.path) {
		        uni.showToast({ title: '问卷路径信息不完整', icon: 'none' });
		        return;
		    }
		    this.closeQuestionnaireModal();
		    // #ifdef MP-WEIXIN
		    uni.openEmbeddedMiniProgram({
		        appId: item.appId,
		        path: item.path,
		    });
		    // #endif
		    // #ifdef APP-PLUS
		    plus.runtime.openURL('https://wj.qq.com/s2/25648071/skl8/');
		    // #endif
		    // #ifdef H5
		    uni.showToast({ title: '请在微信小程序中打开', icon: 'none' });
		    // #endif
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
            uni.setStorageSync('noticeSectionExpanded', this.noticeSectionExpanded);

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
                        uni.setStorageSync('noticeClosedUntilUpdate', true);
                        this.showNotice = false;
                        uni.setStorageSync('noticeClosed', true);
                        console.log('公告已关闭，仅本次');
                    } else if (res.cancel) {
                        this.showNotice = false;
                        uni.setStorageSync('noticeClosed', true);
                        console.log('公告已关闭，直到下次更新');
                    }
                },
                fail: (err) => {
                    console.error('显示确认对话框失败:', err);
                    this.showNotice = false;
                    uni.setStorageSync('noticeClosed', true);
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

        // 获取轮播图数据（从知晓云 choearth_notice 表）
        async fetchCarouselImages() {
            try {
                const res = await this.knowCloudRequest('choearth_notice', {
                    data: { limit: 10, offset: 0 }
                });

                console.log('轮播图API响应:', res);

                if (res && res.objects && res.objects.length > 0) {
                    const images = res.objects
                        .filter(item => item.imageurl)
                        .map(item => ({
                            imageUrl: item.imageurl || '',
                            linkUrl: item.linkUrl || '',
                            title: item.title || '',
                            id: item.id || ''
                        }));
                    console.log('轮播图数据:', images);
                    this.carouselImages = [...images];
                } else {
                    console.log('未找到轮播图数据');
                }
            } catch (error) {
                console.error('获取轮播图数据失败:', error);
                this.carouselImages = [];
            }
        },

        // 轮播图点击事件
        onCarouselItemClick(item) {
            const linkUrl = item.linkUrl || '';
            if (!linkUrl) {
                uni.showToast({ title: '暂无链接', icon: 'none' });
                return;
            }
            // #ifdef MP-WEIXIN
            uni.setClipboardData({
                data: linkUrl,
                success: () => {
                    uni.showToast({
                        title: '链接复制成功',
                        icon: 'success'
                    });
                },
                fail: (err) => {
                    console.error('复制链接失败:', err);
                    uni.showToast({
                        title: '复制失败，请手动复制链接',
                        icon: 'none'
                    });
                }
            });
            // #endif

            // #ifdef H5
            window.open(linkUrl);
            // #endif

            // #ifdef APP-PLUS
            plus.runtime.openURL(linkUrl);
            // #endif
        },


		gotoWebAiTools() {
			// #ifdef H5
			window.open('https://yuanqi.tencent.com/webim/#/chat/GVkzfU?appid=2011824824720938752&experience=true')
			// #endif
			// #ifdef APP-PLUS
			plus.runtime.openURL('https://yuanqi.tencent.com/webim/#/chat/GVkzfU?appid=2011824824720938752&experience=true')
			// #endif
		},

		// 跳转至AI工具小程序
		gotoaitools() {
			// #ifdef MP-WEIXIN
		    uni.openEmbeddedMiniProgram({
		    appId: 'wxd5201eb08d2fa15c',
		    path: 'pages/agentChat/index?showAuthDirectly=1&agentId=0aRyBMBEs8rL',});
			// #endif
			// #ifdef H5
			window.open('https://yuanqi.tencent.com/webim/#/chat/GVkzfU?appid=2011824824720938752&experience=true')
			// #endif
			// #ifdef APP-PLUS
			plus.runtime.openURL('https://yuanqi.tencent.com/webim/#/chat/GVkzfU?appid=2011824824720938752&experience=true')
			// #endif
		},

        // 获取更多公告内容 - 开源版本使用静态公告
        async fetchMoreNotice() {
            try {
                console.log('开源版本：使用静态默认公告');
                // 开源版本不请求知晓云，使用默认公告
                this.moreNoticeContent = "欢迎使用明日方舟通行证查询工具开源版本！如有问题请访问 GitHub Issues 反馈。";
                this.moreNoticeId = '';
                this.updateScrollNotice();
            } catch (error) {
                console.error('获取更多公告失败:', error);
                this.moreNoticeContent = "欢迎使用明日方舟通行证查询工具开源版本！";
                this.moreNoticeId = '';
            }
        },
        // 跳转到更多公告页面
        gotoMoreNotice() {
            if (this.moreNoticeId) {
                uni.navigateTo({
                    url: `/packageA/more-notice/more-notice?id=${this.moreNoticeId}`
                });
            } else {
                uni.navigateTo({
                    url: '/packageA/more-notice/more-notice'
                });
            }
        },

        // 获取公告内容 - 开源版本使用静态公告
        async fetchNotice() {
            try {
                console.log('开源版本：使用静态默认公告');
                // 开源版本不请求知晓云，使用默认公告
                const newNoticeContent = "欢迎使用明日方舟通行证查询工具开源版本！数据来源于 GitHub 仓库，如需最新功能请关注商业版更新。";

                // 检查公告内容是否有更新
                if (this.noticeClosedUntilUpdate && this.noticeContent !== newNoticeContent) {
                    // 公告已更新，重置关闭状态
                    this.noticeClosedUntilUpdate = false;
                    this.showNotice = true;
                    uni.removeStorageSync('noticeClosedUntilUpdate');
                    uni.removeStorageSync('noticeClosed');
                }

                this.noticeContent = newNoticeContent;
                console.log('获取公告成功:', this.noticeContent);

                // 内容更新后重新检查是否需要折叠
                this.$nextTick(() => {
                    setTimeout(() => {
                        this.checkNoticeOverflow();
                    }, 300);
                });
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
                const guessData = uni.getStorageSync('guessData');

                let allData = [];

                // 优先级：arknightsData > currentData > githubCharacters
                if (data && Array.isArray(data) && data.length > 0) {
                    allData = [...data];
                } else if (currentData && Array.isArray(currentData) && currentData.length > 0) {
                    allData = [...currentData];
                } else if (githubData && Array.isArray(githubData) && githubData.length > 0) {
                    allData = [...githubData];
                }

                if (enableGuessData === 'true' && guessData && Array.isArray(guessData) && guessData.length > 0) {
                    allData = [...allData, ...guessData];
                }

                if (allData.length > 0) {
                    this.characterData = allData;
                    this.localUpdateTime = localUpdateTime || '';
                    this.cloudUpdateTime = cloudUpdateTime || '';
                    this.currentVersion = currentVersion || version || '';
                    this.dataUrl = url || '';

                    // 显示数据状态
                    if (!this.dataStatusMessage) {
                        this.dataStatusMessage = '数据已加载';
                    }

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
                const autoUpdateSetting = uni.getStorageSync('autoUpdateSetting');
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
                const autoUpdateSetting = uni.getStorageSync('autoUpdateSetting');
                const enabled = autoUpdateSetting?.enabled || false;

                if (!enabled) {
                    return; // 自动更新未启用
                }

                // 获取当前数据源
                const dataSource = uni.getStorageSync('dataSource') || 'domestic';

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
                        uni.setStorageSync('autoUpdateSetting', autoUpdateSetting);

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
                        const autoUpdateSetting = uni.getStorageSync('autoUpdateSetting');

                        // 更新检查时间
                        autoUpdateSetting.lastCheckTime = new Date().toISOString();
                        uni.setStorageSync('autoUpdateSetting', autoUpdateSetting);

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
                const localVersion = uni.getStorageSync('dataVersion') || '';

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
                const localData = uni.getStorageSync('arknightsData');
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
                    this.dataUrl = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
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
                                key: 'arknightsData',
                                data: cleanedData,
                                success: () => {
                                    const now = new Date();
                                    this.localUpdateTime = `${now.getFullYear()}-${(now.getMonth()+1).toString().padStart(2, '0')}-${now.getDate().toString().padStart(2, '0')} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;

                                    uni.setStorage({ key: 'localUpdateTime', data: this.localUpdateTime });
                                    uni.setStorage({ key: 'cloudUpdateTime', data: this.cloudUpdateTime });
                                    uni.setStorage({ key: 'dataVersion', data: this.currentVersion });
                                    uni.setStorage({ key: 'dataUrl', data: this.dataUrl });

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
            // 确保toolsConfig包含完整数据（name/icon/desc/url等）
            this.initDefaultToolsConfig();

            this.editingTools = JSON.parse(JSON.stringify(this.toolsConfig));
            this.isEditingTools = true;

            // 移除拖拽相关数据
            if (uni.vibrateShort) {
                uni.vibrateShort();
            }
        },

        // 取消编辑
        cancelEditing() {
            this.isEditingTools = false;
            this.editingTools = [];
        },

        // 保存工具箱配置
        saveToolsConfig() {
            // 只保存 id、order、visible 三个字段，不保存 name/icon/desc/url 等会变化的数据
            const orderMap = this.editingTools.map((tool, i) => ({
                id: tool.id,
                order: i + 1,
                visible: tool.visible !== false
            }));

            uni.setStorageSync('toolsConfig', orderMap);
            console.log('【保存工具配置】保存了', orderMap.length, '个工具的顺序/可见性');

            // 重新加载完整配置（包含name/icon/desc等），按保存的顺序排列
            this.initDefaultToolsConfig();

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
        initDefaultToolsConfig(forceReset = false) {
            // 生成默认工具配置（保持所有字段最新）
            const defaultTools = [
                {
                    id: 'search',
                    name: '通行证查询',
                    desc: '查询干员所在的盒号&盒内信息',
                    icon: '/static/search-icon.png',
                    url: '/pages/Search/Search',
                    visible: true,
                    order: 1,
                    showNew: false
                },
                {
                    id: 'list',
                    name: '通行证列表',
                    desc: '查看所有通行证及干员列表信息',
                    icon: '/static/list-tool.png',
                    url: '/pages/list/list',
                    visible: true,
                    order: 2,
                    showNew: false
                },
                {
                    id: 'gacha',
                    name: '模拟抽卡',
                    desc: '模拟通行证抽卡/线下对战',
                    icon: '/static/gacha_simulated-tool.png',
                    url: '/packageB/pages/gacha_simulated/gacha_simulated',
                    visible: true,
                    order: 3,
                    showNew: false
                },
                {
                    id: 'mystarbox',
                    name: '我的收藏',
                    desc: '查看您收藏的通行证&干员',
                    icon: '/static/mystarbox-tool.png',
                    url: '/packageB/pages/mystarbox/mystarbox',
                    visible: true,
                    order: 4,
                    showNew: false
                },
                {
                    id: 'questionnaire',
                    name: '前往元宝小程序帮查>',
                    desc: '元宝小程序提供服务，仅供娱乐，精度较低、无法联系上下文，慎用！！',
                    icon: '',
                    url: '',
                    visible: true,
                    order: 5,
                    showNew: true,
                    specialAction: 'gotoaitools'
                },
                {
                    id: 'share',
                    name: '分享我的通行证',
                    desc: '对自己拥有的通行证进行炫耀/出物发布/换物分享',
                    icon: '/static/share.png',
                    url: '/pages/card_share/card_share',
                    visible: true,
                    order: 6,
                    showNew: true
                },
                {
                    id: 'survey',
                    name: '帮助我们完善工具',
                    desc: '为了让工具更好用，欢迎各位博士们让我们提交通行证相关信息~',
                    icon: '/static/questionnaire-tool.png',
                    url: '',
                    visible: true,
                    order: 7,
                    showNew: false,
                    specialAction: 'gotoquestionnaire'
                },
                {
                    id: 'setting',
                    name: '设置',
                    desc: '部分特别功能个性化',
                    icon: '/static/Setting-tool.png',
                    url: '/packageA/Setting/Setting',
                    visible: true,
                    order: 8,
                    showNew: false
                },
                {
                    id: 'aboutus',
                    name: '关于我们',
                    desc: '了解我们及社媒账号',
                    icon: '/static/aboutus-tool.png',
                    url: '/packageA/aboutus/aboutus',
                    visible: true,
                    order: 9,
                    showNew: false
                },
                {
                    id: 'calendar',
                    name: '通行证日历',
                    desc: '以日历形式查看盒子上线时间线',
                    icon: '/static/calendar-tool.png',
					url: '/packageA/calendar/calendar',
                    // specialAction: 'comingsoon',
                    visible: true,
                    order: 10,
                    showNew: true
                },

                // {
                //     id: 'trade-board',
                //     name: '交换信息',
                //     desc: '发布求购/出售/交换通行证信息',
                //     icon: '/static/trade-board-tool.png',
                //     specialAction: 'comingsoon',
                //     visible: true,
                //     order: 11,
                //     showNew: true
                // },
                {
                    id: 'comingsoon',
                    name: '敬请期待',
                    desc: '新功能即将上线',
                    icon: '/static/threepoint.png',
                    url: '',
                    visible: true,
                    order: 12,
                    showNew: false,
                    specialAction: 'update'
                }
            ];

            // 如果不是强制重置，尝试从缓存读取 order/visible
            if (!forceReset) {
                const savedOrder = uni.getStorageSync('toolsConfig');
                console.log('【初始化工具配置】读取本地缓存, forceReset=false, savedOrder存在:', !!savedOrder);
                if (savedOrder && Array.isArray(savedOrder) && savedOrder.length > 0) {
                    // 用 id 做索引，把缓存的 order 和 visible 合并到默认配置
                    const savedMap = {};
                    savedOrder.forEach(item => { savedMap[item.id] = item; });

                    defaultTools.forEach(tool => {
                        if (savedMap[tool.id]) {
                            tool.order = savedMap[tool.id].order || tool.order;
                            tool.visible = savedMap[tool.id].visible !== false ? true : false;
                        }
                    });

                    // 按 order 排序
                    defaultTools.sort((a, b) => a.order - b.order);
                    this.toolsConfig = defaultTools;
                    console.log('【初始化工具配置】使用已缓存的工具顺序，共', defaultTools.length, '个');
                    return;
                }
            }

            // 没有缓存或强制重置，使用默认顺序
            this.toolsConfig = defaultTools;
            uni.setStorageSync('toolsConfig', defaultTools.map(t => ({ id: t.id, order: t.order, visible: t.visible })));
            console.log('使用默认工具箱配置');
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
                        // 强制重置为默认配置
                        this.initDefaultToolsConfig(true);
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
                if (uni.vibrateShort) {
                    uni.vibrateShort();
                }
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
                if (uni.vibrateShort) {
                    uni.vibrateShort();
                }
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
                if (uni.vibrateShort) {
                    uni.vibrateShort();
                }
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
                if (uni.vibrateShort) {
                    uni.vibrateShort();
                }
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
                return this.toolsConfig.filter(tool => tool.visible).length;
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
                } else if (tool.specialAction === 'gotoWebAiTools') {  // ← 新增
                    this.gotoWebAiTools();
                } else if (tool.specialAction === 'comingsoon') {
                    uni.showToast({
                        title: '该功能正在内测中，敬请期待',
                        icon: 'none',
                        duration: 2000
                    });
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
		            '3 2 5 ，你应该知道是什么意思吧？(本彩蛋灵感来源MAA)',
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
		        uni.setStorageSync('copyrightClickCount', this.copyrightClickCount);

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

    }
}
</script>

<style>
	/* ========== 简约风格（默认） ========== */
	.container {
		padding: 0 20rpx 30rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}

	/* ========== 科技风格（明日方舟主题） ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
		position: relative;
	}

	/* 科技感网格背景 */
	.container.theme-ark::before {
		content: '';
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-image:
			linear-gradient(rgba(255, 107, 53, 0.03) 1px, transparent 1px),
			linear-gradient(90deg, rgba(255, 107, 53, 0.03) 1px, transparent 1px);
		background-size: 50rpx 50rpx;
		pointer-events: none;
		z-index: 0;
	}

	/* ========== 界园风格（粉绿渐变风格） ========== */
	.container.theme-jieyuan {
		background: linear-gradient(180deg, #fdf6f0 0%, #f5ede8 100%);
		position: relative;
	}

	/* 界园风格淡纹背景 */
	.container.theme-jieyuan::before {
		content: '';
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-image:
			radial-gradient(circle at 15% 25%, rgba(226, 88, 132, 0.07) 0%, transparent 45%),
			radial-gradient(circle at 85% 75%, rgba(57, 147, 131, 0.07) 0%, transparent 45%);
		pointer-events: none;
		z-index: 0;
	}

	/* ========== 公告栏样式 ==========

	/* 简约风格 */
	.notice-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
		border-left: 8rpx solid #409EFF;
		margin-top: 20rpx;
		overflow: hidden;
	}

	/* 科技风格 */
	.container.theme-ark .notice-section {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		border: 1rpx solid #2A2A4A;
		border-left: none;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.3), inset 0 1rpx 0 rgba(255, 255, 255, 0.05);
		position: relative;
	}

	.container.theme-ark .notice-section::before {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		width: 30rpx;
		height: 30rpx;
		border-top: 3rpx solid #FF6B35;
		border-left: 3rpx solid #FF6B35;
	}

	.container.theme-ark .notice-section::after {
		content: '';
		position: absolute;
		bottom: 0;
		right: 0;
		width: 30rpx;
		height: 30rpx;
		border-bottom: 3rpx solid #FF6B35;
		border-right: 3rpx solid #FF6B35;
	}

	/* 界园风格 */
	.container.theme-jieyuan .notice-section {
		background: rgba(255, 255, 255, 0.85);
		border-radius: 12rpx;
		border: none;
		border-left: 6rpx solid #e25884;
		box-shadow: 0 4rpx 20rpx rgba(226, 88, 132, 0.12);
		position: relative;
	}

	.container.theme-jieyuan .notice-section::before {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		width: 30rpx;
		height: 30rpx;
		border-top: 3rpx solid #e25884;
		border-left: none;
	}

	.container.theme-jieyuan .notice-section::after {
		content: '';
		position: absolute;
		bottom: 0;
		right: 0;
		width: 30rpx;
		height: 30rpx;
		border-bottom: 3rpx solid #399383;
		border-right: 3rpx solid #399383;
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
		background-color: #f0f0f0;
	}

	.container.theme-ark .notice-close:active {
		background-color: rgba(255, 107, 53, 0.2);
	}

	.close-btn {
		width: 32rpx;
		height: 32rpx;
		font-size: 32rpx;
		color: #999;
		display: flex;
		align-items: center;
		justify-content: center;
		line-height: 1;
	}

	.container.theme-ark .close-btn {
		color: #8A8AA0;
	}

	.notice-icon {
		width: 36rpx;
		height: 36rpx;
		margin-right: 15rpx;
	}

	.notice-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #409EFF;
		flex: 1;
	}

	.container.theme-ark .notice-title {
		color: #FF6B35;
		text-shadow: 0 0 10rpx rgba(255, 107, 53, 0.5);
	}

	.container.theme-jieyuan .notice-title {
		color: #e25884;
		text-shadow: 0 0 10rpx rgba(226, 88, 132, 0.4);
	}

	/* 展开/收起按钮样式 */
	.notice-toggle {
		display: flex;
		align-items: center;
		justify-content: flex-end;
		color: #409EFF;
		font-size: 26rpx;
		padding: 8rpx;
		border-radius: 8rpx;
		transition: all 0.3s;
	}

	.notice-toggle:active {
		background-color: #f0f0f0;
	}

	.container.theme-ark .notice-toggle {
		color: #00D4AA;
	}

	.container.theme-ark .notice-toggle:active {
		background-color: rgba(0, 212, 170, 0.1);
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
		color: #666;
		line-height: 1.6;
		display: block;
		word-break: break-word;
		transition: all 0.3s ease-in-out;
	}

	.container.theme-ark .notice-text {
		color: #E8E8F0;
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

	/* ========== 展开/收起按钮样式 ========== */

	.expand-btn {
		background-color: #f0f0f0;
		color: #666;
		font-size: 24rpx;
		border-radius: 10rpx;
		padding: 15rpx 20rpx;
		border: none;
		line-height: 1;
		transition: all 0.2s ease;
	}

	.expand-btn:active {
		background-color: #e0e0e0;
		transform: scale(0.95);
	}

	.container.theme-ark .expand-btn {
		background: linear-gradient(135deg, #2A2A4A 0%, #1A1A2E 100%);
		color: #E8E8F0;
		border-radius: 4rpx;
		border: 1rpx solid #2A2A4A;
	}

	.container.theme-ark .expand-btn:active {
		background: linear-gradient(135deg, #3A3A5A 0%, #2A2A4A 100%);
		border-color: #FF6B35;
		color: #FF6B35;
	}

	/* ========== 轮播图样式 ========== */

	/* 简约风格 */
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

	/* 科技风格 */
	.container.theme-ark .carousel-section {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		border: 1rpx solid #2A2A4A;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.3);
		position: relative;
	}

	.container.theme-ark .carousel-section::before,
	.container.theme-ark .carousel-section::after {
		content: '';
		position: absolute;
		width: 20rpx;
		height: 20rpx;
		border: 2rpx solid #FF6B35;
	}

	.container.theme-ark .carousel-section::before {
		top: 10rpx;
		left: 10rpx;
		border-right: none;
		border-bottom: none;
	}

	.container.theme-ark .carousel-section::after {
		bottom: 10rpx;
		right: 10rpx;
		border-left: none;
		border-top: none;
	}

	.container.theme-ark .carousel {
		border-radius: 4rpx;
		border: 1rpx solid #2A2A4A;
	}

	.container.theme-ark .carousel-image {
		border-radius: 4rpx;
	}

	/* ========== 工具箱区域样式 ========== */

	/* 简约风格 */
	.tools-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
		margin-bottom: 30rpx;
	}

	/* 科技风格 */
	.container.theme-ark .tools-section {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		border: 1rpx solid #2A2A4A;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.3);
		position: relative;
	}

	.container.theme-ark .tools-section::before {
		content: '';
		position: absolute;
		top: 0;
		left: 30rpx;
		right: 30rpx;
		height: 2rpx;
		background: linear-gradient(90deg, transparent, #FF6B35, transparent);
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
		background-color: #f0f0f0;
		border-radius: 8rpx;
		transition: background-color 0.3s;
	}

	.tools-manage-btn:active {
		background-color: #e0e0e0;
	}

	.manage-text {
		font-size: 24rpx;
		color: #666;
		margin-right: 10rpx;
	}

	.container.theme-ark .tools-manage-btn {
		background: linear-gradient(135deg, #2A2A4A 0%, #1A1A2E 100%);
		border-radius: 4rpx;
		border: 1rpx solid #FF6B35;
	}

	.container.theme-ark .tools-manage-btn:active {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
	}

	.container.theme-ark .manage-text {
		color: #E8E8F0;
	}

	.manage-icon {
		width: 24rpx;
		height: 24rpx;
	}

	.tools-title {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
	}

	.container.theme-ark .tools-title {
		color: #FF6B35;
		text-shadow: 0 0 10rpx rgba(255, 107, 53, 0.3);
		letter-spacing: 2rpx;
	}

	.tools-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 20rpx;
	}

	/* ========== 工具卡片样式 ========== */

	/* 简约风格 */
	.tool-card {
		display: flex;
		flex-direction: column;
		align-items: center;
		background-color: #f8f8f8;
		border-radius: 16rpx;
		padding: 30rpx 20rpx;
		transition: all 0.3s;
		text-align: center;
		height: 200rpx;
		justify-content: flex-start;
		box-shadow: 0 4rpx 10rpx rgba(0, 0, 0, 0.05);
		position: relative;
	}

	.tool-card.editing-card {
		height: auto;
		min-height: 240rpx;
		padding: 30rpx 20rpx 40rpx 20rpx;
	}

	.tool-card:active {
		background-color: #e8f4ff;
		transform: scale(0.98);
	}

	/* 科技风格 */
	.container.theme-ark .tool-card {
		background: linear-gradient(135deg, #1E1E3A 0%, #2A2A4A 100%);
		border-radius: 4rpx;
		border: 1rpx solid #2A2A4A;
		box-shadow: none;
		overflow: hidden;
	}

	.container.theme-ark .tool-card::before {
		content: '';
		position: absolute;
		top: 0;
		left: -100%;
		width: 100%;
		height: 100%;
		background: linear-gradient(90deg, transparent, rgba(255, 107, 53, 0.1), transparent);
		transition: left 0.5s;
	}

	.container.theme-ark .tool-card:active {
		background: linear-gradient(135deg, #2A2A4A 0%, #3A3A5A 100%);
		border-color: #FF6B35;
	}

	.container.theme-ark .tool-card:active::before {
		left: 100%;
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

	/* 科技风格工具图标反转（深色背景下白色图标反转回正常色） */
	.container.theme-ark .tool-icon {
		filter: invert(1);
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
		color: #333;
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
		color: #999;
		line-height: 1.4;
		width: 100%;
		text-align: center;
		word-wrap: break-word;
		word-break: break-word;
	}

	.container.theme-ark .tool-name {
		color: #E8E8F0;
	}

	.container.theme-ark .tool-desc {
		color: #8A8AA0;
	}

	/* ========== 数据状态区域 ========== */

	/* 简约风格 */
	.data-status {
		background-color: #e8f4ff;
		border-radius: 16rpx;
		padding: 25rpx;
		margin-bottom: 30rpx;
		font-size: 24rpx;
		color: #409EFF;
	}

	/* 科技风格 */
	.container.theme-ark .data-status {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		color: #00D4AA;
		border: 1rpx solid #2A2A4A;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.3);
		position: relative;
	}

	.container.theme-ark .data-status::before {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		width: 40rpx;
		height: 40rpx;
		border-top: 3rpx solid #00D4AA;
		border-left: 3rpx solid #00D4AA;
	}

	/* ========== 更新提示样式 ========== */

	/* 简约风格 */
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

	/* 科技风格 */
	.container.theme-ark .update-hint {
		background: linear-gradient(135deg, #2A2A4A 0%, #1A1A2E 100%);
		border-left: 4rpx solid #FF6B35;
		border-radius: 4rpx;
	}

	.container.theme-ark .hint-text {
		color: #FF6B35;
		text-shadow: 0 0 8rpx rgba(255, 107, 53, 0.3);
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
		align-items: flex-start;
		margin-bottom: 15rpx;
		flex-wrap: wrap;
	}
	
	.status-text {
		font-size: 26rpx;
		font-weight: bold;
		color: #333 !important;
		white-space: normal;
		flex: 1;
		min-width: 300rpx;
	}

	.container.theme-ark .status-text {
		color: #E8E8F0;
	}
	
	.data-detail-item {
		color: #333 !important;
		font-size: 24rpx;
		margin-right: 20rpx;
		margin-bottom: 10rpx;
		white-space: normal;
		display: inline-block;
	}
	
	.data-details {
		display: flex;
		flex-wrap: wrap;
		line-height: 1.6;
	}

	.data-status-actions {
		display: flex;
		align-items: center;
		gap: 15rpx;
	}

	/* ========== 更新按钮样式 ========== */

	.update-btn.small {
		background-color: #409EFF;
		color: #fff;
		border-radius: 50rpx;
		font-size: 22rpx;
		padding: 8rpx 20rpx;
		line-height: 1.4;
	}

	.container.theme-ark .update-btn.small {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		border-radius: 4rpx;
		border: 1rpx solid #FF6B35;
	}

	.update-btn-img {
		width: 36rpx;
		height: 36rpx;
		margin-right: 12rpx;
		vertical-align: middle;
	}

	/* 更新按钮文字和图标居中对齐 */
	.update-btn {
		background-color: #67C23A;
		color: #fff;
		border-radius: 50rpx;
		font-size: 26rpx;
		padding: 20rpx 40rpx;
		margin-bottom: 20rpx;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.container.theme-ark .data-detail-item {
		color: #8A8AA0;
	}

	.update-section {
		text-align: center;
		margin-top: 40rpx;
	}

	/* 科技风格 */
	.container.theme-ark .update-btn {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		border-radius: 4rpx;
		border: 2rpx solid #FF6B35;
		box-shadow: 0 0 20rpx rgba(255, 107, 53, 0.3);
		position: relative;
		overflow: hidden;
	}

	.container.theme-ark .update-btn::before {
		content: '';
		position: absolute;
		top: 0;
		left: -100%;
		width: 100%;
		height: 100%;
		background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
		animation: scanLine 2s infinite;
	}

	@keyframes scanLine {
		0% { left: -100%; }
		100% { left: 100%; }
	}

	/* 关于我们链接 */
	.about-link-section {
		text-align: center;
	}

	.about-link {
		font-size: 26rpx;
		color: #409EFF;
		text-decoration: underline;
	}

	.white-button {
		width: 80%;
		border: 0 rgba(0, 0, 0, 0);
	}

/* ========== 底部版权样式 ========== */

	.footer-section {
		text-align: center;
		padding: 30rpx 0;
	}

	.copyright-text {
		font-size: 24rpx;
		color: #999;
		display: block;
		margin-bottom: 10rpx;
	}

	.copyright-text:last-child {
		margin-bottom: 0;
	}

	/* 科技风格 */
	.container.theme-ark .footer-section {
		position: relative;
	}

	.container.theme-ark .footer-section::before {
		content: '';
		position: absolute;
		top: 0;
		left: 20%;
		right: 20%;
		height: 1rpx;
		background: linear-gradient(90deg, transparent, #2A2A4A, transparent);
	}

	.container.theme-ark .copyright-text {
		color: #8A8AA0;
		letter-spacing: 1rpx;
	}

	/* ========== 工具箱编辑模式样式（新增） ========== */
	/* 编辑模式下的工具箱 */
	.tools-grid.editing {
		position: relative;
		min-height: 400rpx;
	}

	/* ========== 编辑模式下的工具卡片 ========== */

	.tool-card.editing-card {
		position: relative;
		transition: transform 0.2s ease;
		background-color: #fff;
		border: 2rpx solid #f0f0f0;
		z-index: 1;
	}

	.container.theme-ark .tool-card.editing-card {
		background: linear-gradient(135deg, #1E1E3A 0%, #2A2A4A 100%);
		border: 2rpx solid #2A2A4A;
	}

	/* ========== 删除按钮样式 ========== */

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

	.container.theme-ark .delete-btn {
		background: linear-gradient(135deg, #FF4444 0%, #CC0000 100%);
		border-radius: 4rpx;
		border: 1rpx solid #FF4444;
		box-shadow: 0 0 10rpx rgba(255, 68, 68, 0.4);
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
		min-height: 80rpx;
		padding-bottom: 8rpx;
	}

	/* ========== 编辑控制样式 ========== */

	.tool-edit-controls {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 8rpx;
		padding: 8rpx 0;
		border-top: 1rpx solid #f0f0f0;
	}

	.visibility-label {
		font-size: 22rpx;
		color: #666;
	}

	.container.theme-ark .tool-edit-controls {
		border-top: 1rpx solid #2A2A4A;
	}

	.container.theme-ark .visibility-label {
		color: #8A8AA0;
	}

	.visibility-switch {
		transform: scale(0.7);
	}

	/* ========== 长按提示样式 ========== */

	.longpress-hint {
		position: absolute;
		bottom: 10rpx;
		left: 0;
		right: 0;
		text-align: center;
	}

	.hint-text {
		font-size: 20rpx;
		color: #409EFF;
		background-color: rgba(64, 158, 255, 0.1);
		padding: 4rpx 12rpx;
		border-radius: 20rpx;
		display: inline-block;
	}

	.container.theme-ark .hint-text {
		color: #00D4AA;
		background-color: rgba(0, 212, 170, 0.1);
		border-radius: 4rpx;
		border: 1rpx solid rgba(0, 212, 170, 0.3);
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

	/* ========== 排序箭头按钮样式 ========== */

	.arrow-btn {
		position: absolute;
		width: 48rpx;
		height: 48rpx;
		background-color: rgba(64, 158, 255, 0.8);
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		pointer-events: auto;
		box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.2);
		z-index: 10;
		transition: all 0.2s;
	}

	.arrow-btn:active {
		background-color: rgba(64, 158, 255, 1);
		transform: scale(0.9);
	}

	.container.theme-ark .arrow-btn {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		border-radius: 4rpx;
		border: 1rpx solid #FF6B35;
		box-shadow: 0 0 10rpx rgba(255, 107, 53, 0.4);
	}

	.container.theme-ark .arrow-btn:active {
		background: linear-gradient(135deg, #FF8C5A 0%, #FFAA80 100%);
	}

	.arrow-up {
		top: -26rpx;
		left: 50%;
		margin-left: -24rpx;
	}

	.arrow-down {
		bottom: -26rpx;
		left: 50%;
		margin-left: -24rpx;
	}

	.arrow-left {
		top: 50%;
		left: -26rpx;
		margin-top: -24rpx;
	}

	.arrow-right {
		top: 50%;
		right: -26rpx;
		margin-top: -24rpx;
	}

	.arrow-icon {
		width: 20rpx;
		height: 20rpx;
	}

	/* ========== 编辑模式控制栏样式 ========== */

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
		color: #666;
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
		background-color: #f0f0f0;
		color: #666;
	}

	.edit-btn.save {
		background-color: #409EFF;
		color: #fff;
	}

	/* 科技风格 */
	.container.theme-ark .edit-controls-bar {
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 8rpx;
		border: 1rpx solid #2A2A4A;
	}

	.container.theme-ark .info-text {
		color: #8A8AA0;
	}

	.container.theme-ark .edit-btn {
		border-radius: 4rpx;
		border: 1rpx solid;
		transition: all 0.3s;
	}

	.container.theme-ark .edit-btn.reset {
		background: linear-gradient(135deg, #2A2A4A 0%, #1A1A2E 100%);
		color: #FF6B6B;
		border-color: #FF6B6B;
	}

	.container.theme-ark .edit-btn.reset:active {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		color: #fff;
	}

	.container.theme-ark .edit-btn.cancel {
		background: linear-gradient(135deg, #2A2A4A 0%, #1A1A2E 100%);
		color: #8A8AA0;
		border-color: #2A2A4A;
	}

	.container.theme-ark .edit-btn.cancel:active {
		background: linear-gradient(135deg, #3A3A5A 0%, #2A2A4A 100%);
	}

	.container.theme-ark .edit-btn.save {
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		color: #fff;
		border-color: #FF6B35;
	}

	.container.theme-ark .edit-btn.save:active {
		background: linear-gradient(135deg, #FF8C5A 0%, #FFAA80 100%);
	}

	/* ========== 界园风格完整样式 ========== */

	/* 轮播图 */
	.container.theme-jieyuan .carousel-section {
		background: rgba(255, 255, 255, 0.85);
		border-radius: 12rpx;
		border: 1rpx solid rgba(226, 88, 132, 0.15);
		box-shadow: 0 4rpx 20rpx rgba(226, 88, 132, 0.08);
	}

	/* 工具箱区域 */
	.container.theme-jieyuan .tools-section {
		background: rgba(255, 255, 255, 0.85);
		border-radius: 12rpx;
		border: 1rpx solid rgba(57, 147, 131, 0.15);
		box-shadow: 0 4rpx 20rpx rgba(57, 147, 131, 0.08);
		position: relative;
		overflow: hidden;
	}

	.container.theme-jieyuan .tools-section::before {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		height: 3rpx;
		background: linear-gradient(90deg, #e25884, #399383);
	}

	/* 工具箱标题 */
	.container.theme-jieyuan .tools-title {
		background: linear-gradient(90deg, #e25884, #399383);
		-webkit-background-clip: text;
		-webkit-text-fill-color: transparent;
		background-clip: text;
		font-weight: bold;
	}

	/* 管理按钮 */
	.container.theme-jieyuan .tools-manage-btn {
		background: linear-gradient(90deg, rgba(226, 88, 132, 0.1), rgba(57, 147, 131, 0.1));
		border-radius: 8rpx;
		border: 1rpx solid rgba(226, 88, 132, 0.3);
	}

	.container.theme-jieyuan .tools-manage-btn:active {
		background: linear-gradient(90deg, #e25884, #399383);
	}

	.container.theme-jieyuan .manage-text {
		color: #e25884;
	}

	.container.theme-jieyuan .tools-manage-btn:active .manage-text {
		color: #fff;
	}

	/* 工具卡片 */
	.container.theme-jieyuan .tool-card {
		background: rgba(255, 255, 255, 0.9);
		border-radius: 12rpx;
		border: 1rpx solid rgba(226, 88, 132, 0.12);
		box-shadow: 0 2rpx 12rpx rgba(226, 88, 132, 0.06);
	}

	.container.theme-jieyuan .tool-card:active {
		background: linear-gradient(135deg, rgba(226, 88, 132, 0.08), rgba(57, 147, 131, 0.08));
		border-color: rgba(226, 88, 132, 0.3);
		transform: scale(0.98);
	}

	.container.theme-jieyuan .tool-name {
		color: #333;
	}

	.container.theme-jieyuan .tool-desc {
		color: #888;
	}

	/* 数据状态区域 */
	.container.theme-jieyuan .data-status {
		background: rgba(255, 255, 255, 0.85);
		border-radius: 12rpx;
		border-left: 6rpx solid #399383;
		color: #399383;
		box-shadow: 0 4rpx 20rpx rgba(57, 147, 131, 0.1);
	}

	.container.theme-jieyuan .status-text {
		color: #333;
	}

	.container.theme-jieyuan .data-detail-item {
		color: #666;
	}

	/* 更新提示 */
	.container.theme-jieyuan .update-hint {
		background: rgba(255, 255, 255, 0.9);
		border-left: 6rpx solid #e25884;
		border-radius: 8rpx;
	}

	.container.theme-jieyuan .update-hint .hint-text {
		color: #e25884;
	}

	/* 更新按钮 */
	.container.theme-jieyuan .update-btn {
		background: linear-gradient(90deg, #e25884, #399383);
		border-radius: 50rpx;
		border: none;
		box-shadow: 0 4rpx 20rpx rgba(226, 88, 132, 0.3);
	}

	.container.theme-jieyuan .update-btn.small {
		background: linear-gradient(90deg, #e25884, #399383);
		border-radius: 50rpx;
		border: none;
	}

	/* 展开按钮 */
	.container.theme-jieyuan .expand-btn {
		background: linear-gradient(90deg, rgba(226, 88, 132, 0.1), rgba(57, 147, 131, 0.1));
		color: #e25884;
		border: 1rpx solid rgba(226, 88, 132, 0.3);
		border-radius: 10rpx;
	}

	.container.theme-jieyuan .expand-btn:active {
		background: linear-gradient(90deg, #e25884, #399383);
		color: #fff;
	}

	/* 公告关闭按钮 */
	.container.theme-jieyuan .notice-close:active {
		background-color: rgba(226, 88, 132, 0.1);
	}

	.container.theme-jieyuan .close-btn {
		color: #e25884;
	}

	/* 公告展开收起 */
	.container.theme-jieyuan .notice-toggle {
		color: #399383;
	}

	.container.theme-jieyuan .notice-toggle:active {
		background-color: rgba(57, 147, 131, 0.1);
	}

	/* 公告正文 */
	.container.theme-jieyuan .notice-text {
		color: #555;
	}

	/* 长按提示 */
	.container.theme-jieyuan .hint-text {
		color: #399383;
		background-color: rgba(57, 147, 131, 0.1);
		border-radius: 20rpx;
	}

	/* 排序箭头按钮 */
	.container.theme-jieyuan .arrow-btn {
		background: linear-gradient(90deg, #e25884, #399383);
		border-radius: 50%;
		border: none;
		box-shadow: 0 2rpx 8rpx rgba(226, 88, 132, 0.3);
	}

	.container.theme-jieyuan .arrow-btn:active {
		opacity: 0.8;
		transform: scale(0.9);
	}

	/* 编辑模式控制栏 */
	.container.theme-jieyuan .edit-controls-bar {
		background: rgba(255, 255, 255, 0.85);
		border-radius: 12rpx;
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}

	.container.theme-jieyuan .info-text {
		color: #666;
	}

	.container.theme-jieyuan .edit-btn {
		border-radius: 8rpx;
		border: 1rpx solid;
	}

	.container.theme-jieyuan .edit-btn.reset {
		background: rgba(226, 88, 132, 0.08);
		color: #e25884;
		border-color: rgba(226, 88, 132, 0.3);
	}

	.container.theme-jieyuan .edit-btn.reset:active {
		background: #e25884;
		color: #fff;
	}

	.container.theme-jieyuan .edit-btn.cancel {
		background: rgba(0, 0, 0, 0.04);
		color: #666;
		border-color: rgba(0, 0, 0, 0.1);
	}

	.container.theme-jieyuan .edit-btn.cancel:active {
		background: rgba(0, 0, 0, 0.08);
	}

	.container.theme-jieyuan .edit-btn.save {
		background: linear-gradient(90deg, #e25884, #399383);
		color: #fff;
		border-color: transparent;
		box-shadow: 0 4rpx 12rpx rgba(226, 88, 132, 0.3);
	}

	.container.theme-jieyuan .edit-btn.save:active {
		opacity: 0.85;
	}

	/* 编辑卡片 */
	.container.theme-jieyuan .tool-card.editing-card {
		background: rgba(255, 255, 255, 0.95);
		border: 2rpx solid rgba(226, 88, 132, 0.2);
	}

	.container.theme-jieyuan .tool-edit-controls {
		border-top: 1rpx solid rgba(226, 88, 132, 0.1);
	}

	.container.theme-jieyuan .visibility-label {
		color: #666;
	}

	/* 底部版权 */
	.container.theme-jieyuan .footer-section::before {
		content: '';
		position: absolute;
		top: 0;
		left: 20%;
		right: 20%;
		height: 1rpx;
		background: linear-gradient(90deg, transparent, rgba(226, 88, 132, 0.3), rgba(57, 147, 131, 0.3), transparent);
	}

	.container.theme-jieyuan .footer-section {
		position: relative;
	}

	.container.theme-jieyuan .copyright-text {
		color: #888;
	}

	/* 关于链接 */
	.container.theme-jieyuan .about-link {
		color: #e25884;
	}

	/* 调研问卷模态框 */
	.modal-mask {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: rgba(0, 0, 0, 0.5);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 9999;
	}
	.questionnaire-modal {
		width: 80%;
		max-height: 70%;
		background: #fff;
		border-radius: 16rpx;
		overflow: hidden;
	}
	.questionnaire-modal .modal-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 30rpx;
		border-bottom: 1rpx solid #eee;
	}
	.questionnaire-modal .modal-title {
		font-size: 32rpx;
		font-weight: bold;
	}
	.questionnaire-modal .modal-close {
		font-size: 48rpx;
		color: #999;
	}
	.questionnaire-modal .modal-body {
		max-height: 70vh;
		padding: 20rpx;
	}
	.questionnaire-modal .questionnaire-item {
		padding: 24rpx;
		border-bottom: 1rpx solid #f0f0f0;
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
	.questionnaire-modal .questionnaire-info {
		flex: 1;
		overflow: hidden;
	}
	.questionnaire-modal .questionnaire-title {
		font-size: 28rpx;
		color: #333;
		font-weight: bold;
	}
	.questionnaire-modal .questionnaire-content {
		font-size: 24rpx;
		color: #666;
		margin-top: 8rpx;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.questionnaire-modal .questionnaire-arrow {
		width: 32rpx;
		height: 32rpx;
		margin-left: 20rpx;
		flex-shrink: 0;
	}
	.questionnaire-modal .questionnaire-right {
		display: flex;
		align-items: center;
		flex-shrink: 0;
	}
	.questionnaire-modal .questionnaire-new {
		width: 60rpx;
		height: 40rpx;
		margin-left: 12rpx;
	}
	.questionnaire-modal .questionnaire-status {
		display: inline-flex;
		align-items: center;
		margin-left: 10rpx;
	}
	.questionnaire-modal .status-tag {
		font-size: 20rpx;
		padding: 4rpx 12rpx;
		border-radius: 8rpx;
		white-space: nowrap;
	}
	.questionnaire-modal .tag-paused {
		background-color: #FFEBEE;
		color: #E53935;
	}
	.questionnaire-modal .tag-open {
		background-color: #E8F5E9;
		color: #43A047;
	}
	.questionnaire-modal .tag-permanent {
		background-color: #FFF8E1;
		color: #FF8F00;
	}
	.questionnaire-modal .empty-tip {
		text-align: center;
		padding: 60rpx;
		color: #999;
	}
</style>
