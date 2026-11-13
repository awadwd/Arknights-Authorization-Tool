<template>
  <view :class="['chat-page', 'theme-' + themeMode]" v-if="allowedAccess">
    <!-- 顶部栏 -->
    <view class="chat-header">
      <view class="header-left">
        <view class="menu-btn" @click="toggleSidebar">
          <view class="menu-bar"></view>
          <view class="menu-bar"></view>
          <view class="menu-bar"></view>
        </view>
        <text class="header-title">PRTS帮查</text>
      </view>
      <view class="header-right">
        <view class="new-chat-btn" @click="startNewChat">
          <text class="new-chat-icon">+</text>
          <text>新对话</text>
        </view>
      </view>
    </view>

    <!-- 模式选择：名称/速度/说明全部来自 CHANNEL_META，模板不再写死文案 -->
    <view class="mode-bar">
      <view class="mode-tabs">
        <view
          v-for="tab in channelTabs"
          :key="tab.key"
          class="mode-tab"
          :class="{ active: channel === tab.key }"
          @click="setChannel(tab.key)"
        >
          <text class="mode-name">{{ tab.name }}</text>
          <text class="mode-speed">{{ tab.speed }}</text>
        </view>
      </view>
      <text class="mode-desc">{{ modeNotice }}</text>
    </view>

    <!-- 主体 -->
    <view class="chat-body">
      <!-- 侧边栏遮罩 -->
      <view class="sidebar-mask" v-if="sidebarOpen" @click="toggleSidebar"></view>

      <!-- 左侧历史对话 -->
      <view class="sidebar" :class="{ open: sidebarOpen }">
        <view class="sidebar-header">
          <text class="sidebar-title">历史对话</text>
          <view class="sidebar-close" @click="toggleSidebar">✕</view>
        </view>
        <scroll-view class="session-list" scroll-y>
          <view class="session-item new-session" @click="startNewChat">
            <text class="new-session-text">+ 新建对话</text>
          </view>
          <view
            v-for="s in sessions"
            :key="s.id"
            class="session-item"
            :class="{ active: s.id === currentSessionId }"
            @click="selectSession(s)"
          >
            <view class="session-main">
              <text class="session-title">{{ s.title || '新对话' }}</text>
              <text class="session-time">{{ formatTime(s.updatedAt) }}</text>
            </view>
            <view class="session-del" @click.stop="deleteSession(s)">删除</view>
          </view>
          <view v-if="sessions.length === 0" class="session-empty">
            <text>暂无历史对话</text>
          </view>
        </scroll-view>
      </view>

      <!-- 右侧聊天区 -->
      <view class="chat-main">
        <scroll-view
          class="message-list"
          scroll-y
          :scroll-into-view="scrollTarget"
          :scroll-with-animation="true"
        >
          <!-- 空状态 + 提问示例 -->
          <view v-if="currentMessages.length === 0" class="empty-state">
            <view class="empty-avatar">🤖</view>
            <text class="empty-title">你好，我是 PRTS 帮查助手</text>
            <text class="empty-sub">可以问我关于干员、盒号、通行证的问题</text>

            <view class="example-grid">
              <view
                v-for="(ex, i) in examples"
                :key="i"
                class="example-card"
                @click="useExample(ex)"
              >
                <text class="example-card-text">{{ ex }}</text>
              </view>
            </view>

            <view class="empty-footer">
              <text>{{ dataUpdateTip }}</text>
            </view>
          </view>

          <!-- 消息列表 -->
          <view
            v-for="(msg, idx) in currentMessages"
            :key="msg.id"
            :id="'msg-' + msg.id"
            class="msg-row"
            :class="msg.role"
          >
            <view v-if="msg.role === 'assistant'" class="avatar ai">🤖</view>
            <view class="bubble" :class="msg.role" @longpress="copyMessage(msg)">
              <text class="bubble-text" v-if="msg.role === 'user'">{{ msg.content }}</text>
              <block v-else>
                <text class="bubble-text" v-if="msg.content">{{ msg.content }}</text>
                <text class="channel-tag" v-if="msg.channel && !msg.loading">{{ channelBadge(msg.channel) }}</text>
                <view v-if="msg.loading" class="typing">
                  <view class="dot"></view>
                  <view class="dot"></view>
                  <view class="dot"></view>
                  <text v-if="isClawMode" class="typing-hint">{{ stageText(msg) }}</text>
                </view>
              </block>
            </view>
            <view v-if="msg.role === 'user'" class="avatar user">我</view>
          </view>

          <view class="list-bottom-pad"></view>
        </scroll-view>

        <!-- 输入栏 -->
        <view class="input-bar">
          <textarea
            class="input-box"
            v-model="inputText"
            :placeholder="isStreaming ? '正在回复...' : '输入你的问题，回车发送'"
            :disabled="isStreaming"
            auto-height
            :maxlength="500"
            @confirm="sendMessage"
          />
          <view
            class="send-btn"
            :class="{ disabled: isStreaming || !inputText.trim() }"
            @click="sendMessage"
          >发送</view>
        </view>
      </view>
    </view>
  </view>
</template>

<script>
  import errorLog from "@/utils/errorLog.js";
  import {
    CHANNEL_YUANQI,
    CHANNEL_CLAW,
    CHANNEL_META,
    channelMeta,
    genConversationId,
    askClaw,
    askYuanqi,
    YUANQI_ASSISTANT_ID,
    YUANQI_BASE_URL
  } from "@/utils/prtsChannel.js";
  import {
    loadSessions as loadStoredSessions,
    saveSessions as persistSessions
  } from "@/utils/prtsStore.js";

  // 错误码 -> 用户可读文案。码由 utils/prtsChannel.js 两条链路统一给出，
  // 页面只负责翻译，不再靠 includes('请求失败') 这种字符串猜。
  const PRTS_ERROR_TEXT = {
    timeout: '响应超时了，可以试试切换到另一个模式重试',
    domain: '小程序后台未配置合法域名：Claw 模式需要 wss.lke.cloud.tencent.com，普通模式需要 yuanqi.tencent.com。请到微信公众平台「开发管理 → 服务器域名」添加 request 合法域名',
    abort: '请求已取消',
    net: '网络连接失败，请检查网络后重试',
    auth: 'API 认证失败，请检查 AppKey 与智能体 ID 配置',
    notfound: '智能体不存在，请检查 AssistantId 配置',
    rate: '请求过于频繁，请稍后再试'
  };

  export default {
    data() {
      return {
        themeMode: 'simple',
        // 查询模式：yuanqi=普通（元器，快/精度低）；claw=Claw（ADP，准/较慢）
        channel: 'yuanqi',
        inputText: '',
        isStreaming: false,
        sidebarOpen: false,
        scrollTarget: '',
        // 会话列表（每个会话是一条完整对话线程）
        sessions: [],
        currentSessionId: '',
        // 提问示例
        examples: [
          '干员艾雅法拉在哪个盒？',
          '通行证1.0有哪些干员？',
          '凯尔希的盒号是多少？',
          '一共有多少个盒？',
          '嘉年华是什么类型的通行证？',
          '已复刻的通行证有哪些？'
        ],
        // 数据更新日期（空则不显示「数据更新：」，免得露出过期的假日期）
        dataUpdateDate: '',
        // 腾讯元器配置：默认值来自 utils/prtsChannel.js，本地设置里可覆盖
        yuanqiAssistantId: YUANQI_ASSISTANT_ID,
        yuanqiBaseUrl: YUANQI_BASE_URL,
        // 存储写满只提醒一次，避免每次查询都弹
        storageWarned: false,
        // 访问控制：是否允许访问（仅从首页进入才允许）
        allowedAccess: false
      }
    },

    computed: {
      currentSession() {
        return this.sessions.find(s => s.id === this.currentSessionId) || null;
      },
      currentMessages() {
        return this.currentSession ? this.currentSession.messages : [];
      },
      isClawMode() {
        return this.channel === CHANNEL_CLAW;
      },
      // 模式切换项的顺序与文案集中在这里，模板只做遍历
      channelTabs() {
        return [CHANNEL_META[CHANNEL_YUANQI], CHANNEL_META[CHANNEL_CLAW]];
      },
      modeNotice() {
        return channelMeta(this.channel).notice;
      },
      dataUpdateTip() {
        const base = '本服务由腾讯元器 / 腾讯云 ADP 提供';
        return this.dataUpdateDate ? base + ' · 数据更新：' + this.dataUpdateDate : base;
      }
    },

    onLoad(options) {
      console.log('PRTS帮查页面加载', options);
      // 访问控制：仅允许从首页（index.vue）携带 from=index 参数进入
      if (options && options.from === 'index') {
        this.allowedAccess = true;
        this.loadThemeSetting();
        this.loadYuanqiConfig();
        this.loadDataUpdateDate();
        this.loadSessions();
        this.loadChannel();
      } else {
        // 非首页进入，直接白屏（不加载任何内容）
        this.allowedAccess = false;
        console.warn('PRTS帮查页面：非首页进入，拒绝访问');
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
          if (themeMode && (themeMode === 'simple' || themeMode === 'ak' || themeMode === 'ark' || themeMode === 'jieyuan' || themeMode === 'liquid' || themeMode === 'liquidDark')) {
            this.themeMode = themeMode;
          } else {
            this.themeMode = 'simple';
          }
          const now = new Date();
          const month = now.getMonth() + 1;
          const day = now.getDate();
          const year = now.getFullYear();
          if (month === 4 && day === 1) {
            const aprilFoolsTriggered = uni.getStorageSync('aprilFoolsTriggered');
            const triggeredDate = aprilFoolsTriggered ? aprilFoolsTriggered.date : null;
            const today = `${year}-04-01`;
            if (triggeredDate !== today) {
              this.themeMode = 'jieyuan';
              uni.showToast({ title: '博士，愚人节快乐~', icon: 'error' });
              uni.setStorageSync('aprilFoolsTriggered', { date: today, triggered: true });
            }
          }
        } catch (e) {
        	this.logError(e);
          this.themeMode = 'simple';
        }
      },

      // 加载元器配置
      loadYuanqiConfig() {
        try {
          const config = uni.getStorageSync('yuanqiConfig');
          if (config && config.assistantId) this.yuanqiAssistantId = config.assistantId;
          if (config && config.baseUrl) this.yuanqiBaseUrl = config.baseUrl;
        } catch (e) {
        	this.logError(e);
        	
        }
      },

      // 加载上次选择的模式（默认普通模式）
      loadChannel() {
        try {
          const saved = uni.getStorageSync('prtsChannel');
          if (saved === CHANNEL_CLAW || saved === CHANNEL_YUANQI) {
            this.channel = saved;
          }
        } catch (e) {
          this.logError(e);
        }
      },

      // 用户点选模式
      setChannel(c) {
        this.applyChannel(c, false);
      },

      applyChannel(c, silent) {
        if (c !== CHANNEL_CLAW && c !== CHANNEL_YUANQI) return;
        const changed = this.channel !== c;
        this.channel = c;
        try {
          uni.setStorageSync('prtsChannel', c);
        } catch (e) {
          this.logError(e);
        }
        if (!silent && changed) {
          uni.showToast({
            title: '已切换到' + this.channelName(c),
            icon: 'none',
            duration: 1500
          });
        }
      },

      channelName(c) {
        return channelMeta(c).name;
      },

      channelHint(c) {
        return '（' + channelMeta(c).hint + '）';
      },

      channelBadge(c) {
        return channelMeta(c).badge;
      },

      // 加载数据更新日期
      loadDataUpdateDate() {
        try {
          const updateTime = uni.getStorageSync('localUpdateTime');
          if (updateTime) {
            const dateMatch = updateTime.match(/\d{4}-\d{2}-\d{2}/);
            if (dateMatch) this.dataUpdateDate = dateMatch[0];
          }
        } catch (e) {
        	this.logError(e);
        	
        }
      },

      // 加载历史会话：清洗、去存档、配额裁剪都在 utils/prtsStore.js
      loadSessions() {
        this.sessions = loadStoredSessions();
        this.currentSessionId = this.sessions.length ? this.sessions[0].id : '';
      },

      // 保存会话：写不进去时内部会收紧配额重试，仍失败才提示一次
      saveSessions() {
        const ok = persistSessions(this.sessions);
        if (!ok && !this.storageWarned) {
          this.storageWarned = true;
          uni.showToast({
            title: '本地存储空间不足，旧对话可能保存失败',
            icon: 'none',
            duration: 2500
          });
        }
        return ok;
      },

      genId(prefix) {
        return prefix + '_' + Date.now() + '_' + Math.random().toString(36).slice(2, 7);
      },

      createSession() {
        return {
          id: this.genId('s'),
          title: '',
          messages: [],
          updatedAt: Date.now()
        };
      },

      // 新建对话
      startNewChat() {
        const s = this.createSession();
        this.sessions.unshift(s);
        this.currentSessionId = s.id;
        this.sidebarOpen = false;
        this.saveSessions();
        this.scrollToBottom();
      },

      // 选择历史会话
      selectSession(s) {
        this.currentSessionId = s.id;
        this.sidebarOpen = false;
        this.scrollToBottom();
      },

      // 删除会话
      deleteSession(s) {
        uni.showModal({
          title: '删除对话',
          content: '确定删除该对话记录吗？',
          success: (res) => {
            if (res.confirm) {
              this.sessions = this.sessions.filter(x => x.id !== s.id);
              if (this.currentSessionId === s.id) {
                this.currentSessionId = this.sessions.length ? this.sessions[0].id : '';
              }
              this.saveSessions();
            }
          }
        });
      },

      toggleSidebar() {
        this.sidebarOpen = !this.sidebarOpen;
      },

      // 使用示例提问
      useExample(ex) {
        this.inputText = ex;
        this.sendMessage();
      },

      // 复制消息
      copyMessage(msg) {
        if (!msg.content) return;
        uni.setClipboardData({
          data: msg.content,
          success: () => uni.showToast({ title: '已复制', icon: 'success' })
        });
      },

      // 发送消息
      async sendMessage() {
        const text = (this.inputText || '').trim();
        if (!text) return;
        if (this.isStreaming) return;

        let session = this.currentSession;
        if (!session) {
          session = this.createSession();
          this.sessions.unshift(session);
          this.currentSessionId = session.id;
        }

        this.inputText = '';
        this.sidebarOpen = false;

        const userMsg = { id: this.genId('m'), role: 'user', content: text, time: Date.now() };
        const aiMsg = { id: this.genId('m'), role: 'assistant', content: '', loading: true, stage: '', time: Date.now() };
        session.messages.push(userMsg);
        session.messages.push(aiMsg);

        const userCount = session.messages.filter(m => m.role === 'user').length;
        if (userCount === 1) {
          session.title = text.length > 14 ? text.slice(0, 14) + '…' : text;
        }
        session.updatedAt = Date.now();
        this.isStreaming = true;
        this.scrollToBottom();

        this.runQuery(text, session, aiMsg);
      },

      // 执行一次查询；失败则询问是否换模式重试
      async runQuery(text, session, aiMsg) {
        try {
          const result = await this.sendMessageToPRTS(text, session, (stage) => {
            if (aiMsg.loading) aiMsg.stage = stage;
          });
          aiMsg.content = (result && result.content) || '（PRTS 没有返回内容）';
          aiMsg.channel = (result && result.channel) || '';
          aiMsg.stage = '';
          aiMsg.loading = false;
          // 置顶当前会话
          this.sessions = [session, ...this.sessions.filter(x => x.id !== session.id)];
          this.saveSessions();
          this.scrollToBottom();
        } catch (error) {
        	this.logError(error, 'prts_query:' + this.channel);
          console.error('PRTS查询失败:', error);
          aiMsg.content = '抱歉，查询失败：' + this.getErrorMessage(error);
          aiMsg.channel = '';
          aiMsg.stage = '';
          aiMsg.loading = false;
          this.saveSessions();
          this.scrollToBottom();
          this.askSwitchChannel(text, session, aiMsg);
        } finally {
          // 成功、失败、甚至抛异常都要解锁，否则输入框会永久禁用
          this.isStreaming = false;
        }
      },

      // 失败后询问：要不要切到另一条链路重试
      askSwitchChannel(text, session, aiMsg) {
        const from = this.channel;
        const to = from === CHANNEL_CLAW ? CHANNEL_YUANQI : CHANNEL_CLAW;
        uni.showModal({
          title: '查询失败',
          content: this.channelName(from) + '没查出来，是否切换到「' + this.channelName(to) + '」重试？' + this.channelHint(to),
          confirmText: '切换重试',
          cancelText: '算了',
          success: (res) => {
            if (!res.confirm) return;
            this.applyChannel(to, true);
            aiMsg.content = '';
            aiMsg.channel = '';
            aiMsg.stage = '';
            aiMsg.loading = true;
            this.isStreaming = true;
            this.scrollToBottom();
            this.runQuery(text, session, aiMsg);
          }
        });
      },

      // 按用户当前选择的模式走对应链路
      //   普通模式 = 腾讯元器（快、精度低、无多轮上下文）
      //   Claw 模式 = 腾讯云 ADP（准、较慢、按会话保持上下文）
      async sendMessageToPRTS(message, session, onStage) {
        if (this.channel === CHANNEL_CLAW) {
          if (session && !session.clawConversationId) {
            session.clawConversationId = genConversationId();
          }
          const cid = (session && session.clawConversationId) || genConversationId();
          return await askClaw(message, cid, this.getUserId(), onStage);
        }
        return await askYuanqi(message, {
          assistantId: this.yuanqiAssistantId,
          baseUrl: this.yuanqiBaseUrl,
          userId: this.getUserId()
        });
      },

      // 获取用户唯一标识
      getUserId() {
        let userId = uni.getStorageSync('yuanqiUserId');
        if (!userId) {
          userId = 'user_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
          uni.setStorageSync('yuanqiUserId', userId);
        }
        return userId;
      },

      // 错误信息
      // Claw 过程提示：只讲阶段，不暴露工具名 / 查询参数 / TraceId
      stageText(msg) {
        const s = (msg && msg.stage) || '';
        if (s === 'thinking') return '正在理解问题…';
        if (s === 'querying') return '正在查询盒号数据…';
        if (s === 'composing') return '已查到，正在整理…';
        if (s === 'answering') return '正在输出…';
        return 'Claw 检索中，约 10~30 秒';
      },

      getErrorMessage(error) {
        const msg = (error && (error.message || error.errMsg)) || '';
        const code = (error && error.code) || '';
        if (PRTS_ERROR_TEXT[code]) return PRTS_ERROR_TEXT[code];
        // http / server / empty：服务端自己的话更具体，直接透出
        return msg || '未知错误';
      },

      // 滚动到底部
      scrollToBottom() {
        const msgs = this.currentMessages;
        if (!msgs || !msgs.length) return;
        const target = 'msg-' + msgs[msgs.length - 1].id;
        this.scrollTarget = '';
        this.$nextTick(() => {
          this.scrollTarget = target;
        });
      },

      // 格式化时间
      formatTime(timestamp) {
        if (!timestamp) return '';
        const date = new Date(timestamp);
        const now = new Date();
        const diff = now - date;
        if (diff < 24 * 60 * 60 * 1000) {
          if (diff < 60 * 1000) return '刚刚';
          if (diff < 60 * 60 * 1000) return `${Math.floor(diff / 60000)}分钟前`;
          return `${Math.floor(diff / 3600000)}小时前`;
        }
        const month = date.getMonth() + 1;
        const day = date.getDate();
        const hour = date.getHours().toString().padStart(2, '0');
        const minute = date.getMinutes().toString().padStart(2, '0');
        return `${month}/${day} ${hour}:${minute}`;
      }
    }
  }
</script>

<style>
  page {
    height: 100%;
  }

  .chat-page {
    display: flex;
    flex-direction: column;
    height: 100vh;
    background-color: #f5f6f8;
  }

  /* 顶栏 */
  .chat-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 20rpx 30rpx;
    background-color: #ffffff;
    border-bottom: 1rpx solid #ececec;
    flex-shrink: 0;
  }

  .header-left {
    display: flex;
    align-items: center;
  }

  .menu-btn {
    width: 56rpx;
    height: 56rpx;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 8rpx;
    margin-right: 20rpx;
  }

  .menu-bar {
    width: 34rpx;
    height: 4rpx;
    background-color: #333;
    border-radius: 2rpx;
  }

  .header-title {
    font-size: 34rpx;
    font-weight: bold;
    color: #222;
  }

  .new-chat-btn {
    display: flex;
    align-items: center;
    background-color: #409EFF;
    color: #fff;
    padding: 12rpx 24rpx;
    border-radius: 30rpx;
    font-size: 26rpx;
  }

  .new-chat-icon {
    font-size: 30rpx;
    margin-right: 6rpx;
    line-height: 1;
  }

  /* 模式选择条 */
  .mode-bar {
    background-color: #fff7e6;
    padding: 12rpx 30rpx 14rpx;
    flex-shrink: 0;
  }

  .mode-tabs {
    display: flex;
    gap: 16rpx;
  }

  .mode-tab {
    display: flex;
    align-items: center;
    padding: 8rpx 22rpx;
    border-radius: 28rpx;
    background-color: rgba(255, 255, 255, 0.7);
    border: 1rpx solid rgba(212, 136, 6, 0.3);
  }

  .mode-tab.active {
    background-color: #d48806;
    border-color: #d48806;
  }

  .mode-name {
    font-size: 24rpx;
    color: #d48806;
  }

  .mode-tab.active .mode-name {
    color: #fff;
    font-weight: bold;
  }

  .mode-speed {
    font-size: 20rpx;
    margin-left: 8rpx;
    opacity: 0.75;
    color: #d48806;
  }

  .mode-tab.active .mode-speed {
    color: #fff;
  }

  .mode-desc {
    display: block;
    margin-top: 8rpx;
    font-size: 20rpx;
    line-height: 1.5;
    color: #d48806;
    opacity: 0.85;
  }

  /* 主体 */
  .chat-body {
    flex: 1;
    position: relative;
    display: flex;
    overflow: hidden;
  }

  /* 侧边栏 */
  .sidebar-mask {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.4);
    z-index: 99;
  }

  .sidebar {
    position: fixed;
    top: 0;
    left: 0;
    bottom: 0;
    width: 70vw;
    max-width: 620rpx;
    background-color: #ffffff;
    z-index: 100;
    transform: translateX(-100%);
    transition: transform 0.28s ease;
    display: flex;
    flex-direction: column;
    box-shadow: 6rpx 0 24rpx rgba(0, 0, 0, 0.08);
  }

  .sidebar.open {
    transform: translateX(0);
  }

  .sidebar-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 30rpx;
    border-bottom: 1rpx solid #eee;
  }

  .sidebar-title {
    font-size: 30rpx;
    font-weight: bold;
    color: #333;
  }

  .sidebar-close {
    font-size: 32rpx;
    color: #999;
    padding: 6rpx 16rpx;
  }

  .session-list {
    flex: 1;
    padding: 16rpx;
  }

  .session-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 24rpx 20rpx;
    border-radius: 12rpx;
    margin-bottom: 12rpx;
    background-color: #f6f7f9;
    transition: background-color 0.2s;
  }

  .session-item.active {
    background-color: #e8f3ff;
  }

  .session-main {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
  }

  .session-title {
    font-size: 28rpx;
    color: #333;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .session-time {
    font-size: 20rpx;
    color: #999;
    margin-top: 6rpx;
  }

  .session-del {
    font-size: 22rpx;
    color: #ff6b6b;
    padding: 6rpx 14rpx;
    flex-shrink: 0;
  }

  .new-session {
    background-color: #409EFF;
    justify-content: center;
  }

  .new-session-text {
    color: #fff;
    font-size: 28rpx;
    font-weight: bold;
  }

  .session-empty {
    text-align: center;
    color: #bbb;
    font-size: 24rpx;
    padding: 60rpx 0;
  }

  /* 聊天区 */
  .chat-main {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .message-list {
    flex: 1;
    padding: 24rpx;
    overflow: hidden;
  }

  .list-bottom-pad {
    height: 20rpx;
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 60rpx 40rpx;
  }

  .empty-avatar {
    width: 120rpx;
    height: 120rpx;
    border-radius: 50%;
    background: linear-gradient(135deg, #409EFF, #66b1ff);
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 60rpx;
    margin-bottom: 30rpx;
  }

  .empty-title {
    font-size: 34rpx;
    font-weight: bold;
    color: #222;
    margin-bottom: 12rpx;
  }

  .empty-sub {
    font-size: 26rpx;
    color: #999;
    margin-bottom: 40rpx;
  }

  .example-grid {
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 18rpx;
  }

  .example-card {
    background-color: #ffffff;
    border: 1rpx solid #e6e6e6;
    border-radius: 16rpx;
    padding: 26rpx 28rpx;
    transition: all 0.2s;
  }

  .example-card:active {
    background-color: #e8f3ff;
    border-color: #409EFF;
  }

  .example-card-text {
    font-size: 27rpx;
    color: #333;
  }

  .empty-footer {
    margin-top: 50rpx;
    font-size: 22rpx;
    color: #bbb;
    text-align: center;
  }

  /* 消息气泡 */
  .msg-row {
    display: flex;
    align-items: flex-start;
    margin-bottom: 28rpx;
  }

  .msg-row.user {
    flex-direction: row;
    justify-content: flex-end;
  }

  .msg-row.assistant {
    flex-direction: row;
    justify-content: flex-start;
  }

  .avatar {
    width: 64rpx;
    height: 64rpx;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 30rpx;
    flex-shrink: 0;
  }

  .avatar.ai {
    background: linear-gradient(135deg, #409EFF, #66b1ff);
    margin-right: 16rpx;
  }

  .avatar.user {
    background-color: #95a5a6;
    color: #fff;
    margin-left: 16rpx;
  }

  .bubble {
    max-width: 74%;
    padding: 22rpx 26rpx;
    border-radius: 20rpx;
    font-size: 28rpx;
    line-height: 1.6;
    word-break: break-word;
  }

  .bubble.user {
    background-color: #409EFF;
    color: #fff;
    border-top-right-radius: 6rpx;
  }

  .bubble.assistant {
    background-color: #ffffff;
    color: #333;
    border-top-left-radius: 6rpx;
    box-shadow: 0 2rpx 10rpx rgba(0, 0, 0, 0.04);
  }

  .bubble-text {
    white-space: pre-wrap;
  }

  /* 链路标识：A/B 分流验证用，弱化显示 */
  .channel-tag {
    display: block;
    margin-top: 10rpx;
    font-size: 20rpx;
    line-height: 1;
    opacity: 0.4;
  }

  /* 加载动画 */
  .typing {
    display: flex;
    gap: 10rpx;
    align-items: center;
    padding: 6rpx 0;
  }

  .typing .dot {
    width: 14rpx;
    height: 14rpx;
    background-color: #bbb;
    border-radius: 50%;
    animation: dot-pulse 1.4s infinite ease-in-out;
  }

  .typing .dot:nth-child(2) { animation-delay: 0.2s; }
  .typing .dot:nth-child(3) { animation-delay: 0.4s; }

  .typing-hint {
    font-size: 22rpx;
    color: #999;
    margin-left: 6rpx;
  }

  @keyframes dot-pulse {
    0%, 60%, 100% { transform: translateY(0); opacity: 0.5; }
    30% { transform: translateY(-10rpx); opacity: 1; }
  }

  /* 输入栏 */
  .input-bar {
    display: flex;
    align-items: flex-end;
    padding: 16rpx 20rpx;
    background-color: #ffffff;
    border-top: 1rpx solid #ececec;
    flex-shrink: 0;
  }

  .input-box {
    flex: 1;
    min-height: 72rpx;
    max-height: 200rpx;
    background-color: #f4f5f7;
    border-radius: 36rpx;
    padding: 18rpx 28rpx;
    font-size: 28rpx;
    color: #333;
    line-height: 1.4;
  }

  .send-btn {
    margin-left: 16rpx;
    background-color: #409EFF;
    color: #fff;
    font-size: 28rpx;
    padding: 0 36rpx;
    height: 72rpx;
    line-height: 72rpx;
    border-radius: 36rpx;
    flex-shrink: 0;
  }

  .send-btn.disabled {
    background-color: #c6d9f0;
  }/* ========== 界园风格 ========== */
  .chat-page.theme-jieyuan {
    background-color: #FAF3E0;
  }
  .chat-page.theme-jieyuan .chat-header {
    background-color: rgba(255,255,255,0.9);
    border-bottom-color: rgba(226,88,132,0.15);
  }
  .chat-page.theme-jieyuan .header-title { color: #333; }
  .chat-page.theme-jieyuan .notice-bar { background-color: rgba(226,88,132,0.1); }
  .chat-page.theme-jieyuan .notice-text { color: #e25884; }
  .chat-page.theme-jieyuan .sidebar { background-color: rgba(255,255,255,0.95); }
  .chat-page.theme-jieyuan .sidebar-header { border-bottom-color: rgba(226,88,132,0.1); }
  .chat-page.theme-jieyuan .sidebar-title { color: #333; }
  .chat-page.theme-jieyuan .session-item { background-color: rgba(255,255,255,0.7); }
  .chat-page.theme-jieyuan .session-item.active { background-color: rgba(226,88,132,0.12); }
  .chat-page.theme-jieyuan .session-title { color: #333; }
  .chat-page.theme-jieyuan .session-time { color: #888; }
  .chat-page.theme-jieyuan .new-session { background: linear-gradient(90deg, #e25884, #399383); }
  .chat-page.theme-jieyuan .bubble.assistant { background-color: #fff; color: #333; }
  .chat-page.theme-jieyuan .bubble.user { background: linear-gradient(90deg, #e25884, #399383); }
  .chat-page.theme-jieyuan .input-bar { background-color: rgba(255,255,255,0.9); border-top-color: rgba(226,88,132,0.15); }
  .chat-page.theme-jieyuan .input-box { background-color: rgba(0,0,0,0.04); color: #333; }
  .chat-page.theme-jieyuan .example-card { background-color: #fff; border-color: rgba(226,88,132,0.2); }
  .chat-page.theme-jieyuan .example-card-text { color: #399383; }
  .chat-page.theme-jieyuan .empty-title { color: #333; }
  .chat-page.theme-jieyuan .empty-sub { color: #888; }
</style>
