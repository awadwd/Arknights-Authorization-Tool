<template>
  <view :class="['container', 'theme-' + themeMode]">
    <!-- 页面标题 -->
    <view class="header">
      <uni-notice-bar scrollable single showIcon text="温馨提示：本功能为模拟抽卡，结果由系统随机生成，仅供娱乐，与现实无关。严禁用于盈利、赌博及其他违法违规行为，违者将永久封禁。"></uni-notice-bar>
    </view>

   <!-- 模式切换：仅窄屏显示，宽屏下两种模式左右并列 -->
   <view class="mode-switch" v-if="!wideLayout && !battleStarted">
     <view class="mode-seg">
       <view class="mode-seg-item" :class="{ active: !battleMode }" @click="setBattleMode(false)">
         <text>普通模式</text>
       </view>
       <view class="mode-seg-item" :class="{ active: battleMode }" @click="setBattleMode(true)">
         <text>对战模式</text>
       </view>
     </view>
   </view>

    <!-- 宽屏：普通模式 / 对战模式 左右并列；窄屏只渲染当前模式 -->
    <view class="gacha-split" :class="{ 'is-wide': wideLayout }">

    <!-- 普通模式 -->
    <view class="gacha-col col-normal" v-if="wideLayout || !battleMode">
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
          <view class="box-controls-header">
            <view class="box-info">
              <text class="box-type">{{ getBoxTypeText(selectedBox.Box_type) }}</text>
              <text class="character-count">剩余 {{ remainingCount }}/{{ totalCount }}</text>
            </view>
            <button class="reset-btn" @click="resetCurrentBox">开新盒</button>
          </view>
          <!-- 当前盒卡面：已抽出灰掉。白名单/音律/特别款无精一精二，只显示干员名 -->
          <view class="box-card-panel" v-if="boxCardList.length">
            <view class="box-card-panel-head">
              <text class="box-card-panel-title">本盒卡面</text>
              <text class="box-card-panel-meta">{{ drawnCount }}/{{ totalCount }} 已抽</text>
            </view>
            <view class="box-card-list">
              <view
                class="box-card-item"
                :class="{ drawn: card.drawn }"
                v-for="card in boxCardList"
                :key="card.key"
              >
                <text class="box-card-name">{{ card.label }}</text>
              </view>
            </view>
          </view>
        </view>

        <!-- 超级大混池信息 -->
        <view class="mix-pool-info" v-if="isSuperMixPool">
          <text class="mix-pool-title">超级大混池模式</text>
          <text class="mix-pool-stats">可用卡片数: {{ superMixTotalCards }}</text>
        </view>

        <!-- 盈亏展示信息区（紧凑版） -->
        <view class="pnl-panel">
          <view class="pnl-row">
            <view class="pnl-item">
              <text class="pnl-label">今日</text>
              <text class="pnl-value" :class="pnlToday > 0 ? 'up' : (pnlToday < 0 ? 'down' : '')">{{ pnlToday > 0 ? '+' : '' }}{{ pnlToday }}</text>
            </view>
            <view class="pnl-item">
              <text class="pnl-label">余额</text>
              <text class="pnl-value pnl-balance">￥{{ pnlBalance }}</text>
            </view>
            <view class="pnl-item">
              <text class="pnl-label">最近</text>
              <text class="pnl-value" :class="pnlLastChangeNet > 0 ? 'up' : (pnlLastChangeNet < 0 ? 'down' : '')">{{ pnlLastChangeNet === 0 && !pnlLastChange ? '--' : (pnlLastChangeNet > 0 ? '+' : '') + pnlLastChangeNet }}</text>
            </view>
            <view class="pnl-item">
              <text class="pnl-label">已抽</text>
              <text class="pnl-value">{{ pnlDrawCount }}</text>
            </view>
          </view>
          <text class="pnl-note">每抽￥25 · 日重置￥1000 · 无市价免费 · 市价来自第三方仅供参考</text>
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
          <view
            class="card-stage"
            :class="{
              'is-flipping': isAnimating,
              'is-revealed': !isAnimating && currentResult,
              'is-hot': !isAnimating && currentResult && currentResult.hotcharacter,
              [getEliteClass(currentResult && currentResult.eliteLevel)]: !isAnimating && currentResult
            }"
          >
            <view class="card-rarity-ring"></view>
            <!-- 蓄力阶段：卡背 -->
            <view class="card-face card-back" v-if="isAnimating">
              <view class="card-back-shine"></view>
              <view class="card-back-inner">
                <text class="card-back-text">?</text>
                <text class="card-back-sub">开启中</text>
              </view>
            </view>
            <!-- 揭示阶段：卡面 -->
            <view
              class="card-face card-front"
              :class="getEliteClass(currentResult && currentResult.eliteLevel)"
              v-if="!isAnimating && currentResult"
            >
              <view class="card-front-shine"></view>
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
    <view class="battle-mode gacha-col col-battle" v-if="wideLayout || battleMode">
      <!-- 公共设置区域 -->
      <view class="battle-setup-area" v-if="!battleStarted">
        <view class="setup-title">对战设置（双方共用同一卡池）</view>
		<view class="setup-waringtitle">注意：部分干员可能没有市价，仅供娱乐，不计入欧非度统计</view>
        <!-- 分类选择器（混池时隐藏） -->
        <view class="selector-group" v-if="!isShell && !battleIsSuperMixPool">
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
        <view class="super-mix-option" v-if="!isShell">
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

        <!-- 线下对战（本机双人镜像）：手机端主入口，必须排在联机区块之前 -->
        <view v-if="!isShell && !battleNet && netState === 'idle'" class="local-battle-block">
          <text class="local-battle-title">线下对战（本机双人）</text>
          <text class="local-battle-desc">同一台设备两人轮流抽卡；上半区会自动上下翻转，方便坐对面的人操作</text>
          <button class="battle-start-btn" @click="startBattle" :disabled="!canStartBattle">{{ netStartLabel }}</button>
        </view>

        <!-- 联机对战：两条独立线（我的世界式）——
             线1 P2P 局域网直连（房主=Windows 壳监听 23515，对方填 IP:23515 加入，无需服务端）
             线2 独立服务端（BattleLobbyServer 23516：连服务器 → 大厅列表 → 邀请对战）
             微信小程序端不上联机（条件编译隐藏整个区块） -->
        <!-- #ifndef MP-WEIXIN -->
        <view class="net-battle-section">
          <view class="net-battle-title" @click="toggleNetPanel">
            <text>联机对战</text>
            <text v-if="!isShell" class="net-panel-toggle">{{ netState === 'idle' ? (netPanelOpen ? '收起 ▴' : '展开 ▾') : '' }}</text>
          </view>

          <!-- 教程入口：页面内展开，不用弹窗（弹窗里的 scroll-view 在壳里滚不动） -->
          <view class="net-guide-toggle" @click.stop="showNetGuide = !showNetGuide">
            <text>{{ showNetGuide ? '收起联机说明 ▴' : '如何远程联机？ ▾' }}</text>
          </view>

          <view v-if="netPanelExpanded" class="net-panel-body">

            <!-- 未连接：填昵称，两条线各自入口 -->
            <view v-if="netState === 'idle'" class="net-actions">
              <view class="net-field-row">
                <text class="net-field-label">昵称</text>
                <input class="net-input" v-model="netName" placeholder="你的昵称（16字内）" maxlength="16" />
              </view>

              <!-- 线1：P2P 局域网直连 -->
              <view class="net-line-block">
                <view class="net-line-title">局域网直连（无需服务器）</view>
                <view class="net-line-desc">房主在电脑上开房，对方填房主 IP 加入；双方需同一局域网或房主做端口转发</view>
                <button v-if="isShell" class="net-btn net-connect-btn" @click="hostDirect">创建房间（本机开房）</button>
                <text v-else class="net-line-tip">本机（{{ netPlatformName }}）不支持开房，仅可加入房主（Windows 端）的房间</text>
                <view class="net-join-row">
                  <input class="net-input" v-model="netPeerAddr" placeholder="房主 IP:23515" />
                  <button class="net-btn net-join-btn" @click="joinDirect">加入房间</button>
                </view>
              </view>

              <!-- 线2：独立服务端 -->
              <view class="net-line-block">
                <view class="net-line-title">连接服务器（自建大厅）</view>
                <view class="net-line-desc">连接自建的大厅服务端，在服务器内邀请在线玩家对战</view>
                <view class="net-field-row">
                  <text class="net-field-label">服务器</text>
                  <input class="net-input" v-model="netServerAddr" placeholder="服务器 IP:端口（默认 23516）" />
                </view>
                <button class="net-btn net-connect-btn" @click="connectLobby">连接大厅</button>
              </view>
            </view>

            <!-- 连接中：握手未完成，不显示「已连接」，失败会回到 idle -->
            <view v-else-if="netState === 'connecting'" class="net-wait-start">
              <text>正在连接服务器…</text>
              <button class="net-disconnect" @click="leaveNet">取消</button>
            </view>

            <!-- 大厅：在线玩家列表（线2） -->
            <view v-else-if="netState === 'lobby'" class="net-lobby">
              <view class="net-status">
                <text class="net-status-text">已连接大厅（{{ netMyName }}）</text>
                <button class="net-disconnect" @click="leaveNet">断开</button>
              </view>
              <view class="net-lobby-title">在线玩家（{{ netPlayers.length }}）</view>
              <view v-if="netPlayers.length === 0" class="net-empty">暂无其他在线玩家</view>
              <view v-for="pl in netPlayers" :key="pl.id" class="net-player-row">
                <text class="net-player-name">{{ pl.name }}<text v-if="pl.busy" class="net-player-busy">（对战中）</text></text>
                <button class="net-invite-btn" :disabled="pl.busy" @click="invitePlayer(pl.id)">邀请</button>
              </view>
            </view>

            <!-- 房间：房主可开始/踢人，guest 等待（两条线共用） -->
            <view v-else-if="netState === 'room'" class="net-room">
              <view class="net-status">
                <text class="net-status-text">{{ netRoomText }}</text>
                <button class="net-disconnect" @click="leaveRoom">离开</button>
              </view>
              <view v-if="netRoomRole === 'host'" class="net-room-actions">
                <button class="net-btn" @click="startBattle" :disabled="isNetWaitingPeer">开始对战</button>
                <button class="net-btn net-kick-btn" @click="kickPeer">踢出对方</button>
              </view>
              <view v-else class="net-wait-start">
                <text>已进入房间，等待房主开始对战…</text>
              </view>
            </view>

            <!-- 对战中：顶部提示（可离开） -->
            <view v-else-if="netState === 'playing'" class="net-status">
              <text class="net-status-text">{{ netRoomText }}</text>
              <button class="net-disconnect" @click="leaveRoom">离开</button>
            </view>
          </view>
          <!-- 联机说明：直接铺在页面里，跟着页面一起滚；放在操作区之后，展开时不会把昵称输入顶走 -->
          <view v-if="showNetGuide" class="net-guide-inline">
            <view class="guide-sec guide-lead">
              <text class="guide-p">只有「Windows 电脑版」能开房，手机 / 小程序 / 浏览器只能加入。两边盒号数据要同一版本（设置 → 数据管理 → 重新下载）。</text>
            </view>

            <view class="guide-sec">
              <text class="guide-h">① 同一 WiFi / 同一局域网（最简单，推荐先试这个）</text>
              <text class="guide-p">1. 房主：Windows 电脑版 → 模拟抽卡 → 联机对战 → 填昵称 → 点「创建房间（本机开房）」。</text>
              <text class="guide-p">2. 房主：房间提示里会显示一串地址，形如 192.168.1.7:23515，复制发给对方。</text>
              <text class="guide-p">3. 对方：同一个 WiFi 下打开模拟抽卡 → 填昵称 → 在「加入房间」框里粘贴这串地址 → 点「加入房间」。</text>
              <text class="guide-p">4. 房主看到「对方 xxx 已加入房间」→ 点「开始对战」。之后两边各点各的抽卡，牌库严格同步。</text>
              <text class="guide-tip">首次开房 Windows 会弹防火墙提示，必须点「允许访问」，否则对方一定连不上。</text>
            </view>

            <view class="guide-sec">
              <text class="guide-h">② 异地直连（两边不在同一个网里）</text>
              <text class="guide-p">A. 虚拟局域网（最省事）：双方各装 Tailscale 或 ZeroTier，登录同一个网络，各自会拿到一个 100.x 或 10.x 开头的虚拟 IP。房主正常开房，把「虚拟 IP:23515」发给对方即可，不用碰路由器。</text>
              <text class="guide-p">B. 端口转发：在房主的路由器上把 TCP 23515 转发到房主电脑的内网 IP，对方填「房主公网 IP:23515」。</text>
              <text class="guide-tip">跨网络时填 192.168.x.x 一定连不上——那是内网地址，出不了路由器。</text>
            </view>

            <view class="guide-sec">
              <text class="guide-h">③ 自建大厅服务端（多人 / 长期开服）</text>
              <text class="guide-p">1. 找一台常开的机器（云服务器最合适，Windows / Linux 都行），把 BattleLobbyServer 跑起来，默认端口 23516。</text>
              <text class="guide-p">2. 所有人：模拟抽卡 → 联机对战 →「服务器」填「IP:23516」（本机就是 127.0.0.1:23516）→ 点「连接大厅」。</text>
              <text class="guide-p">3. 大厅会列出所有在线玩家，点某人后面的「邀请」，对方接受后双方进入同一房间。</text>
              <text class="guide-p">4. 房主点「开始对战」；不合意可以「踢出对方」。房间结束后回到大厅，可以继续邀别人。</text>
              <text class="guide-tip">大厅只做「找人 + 建房」，不收集账号密码；服务器地址由开服的人自己填，官方不提供公共服务器。</text>
            </view>

            <view class="guide-sec">
              <text class="guide-h">常见问题</text>
              <text class="guide-p">· 提示「开房失败」：端口 23515 被占用了，把另一个正在开房的窗口关掉再试。</text>
              <text class="guide-p">· 提示「该地址不是房主房间」：对方还没点「创建房间」，或者 IP 填成了别的机器。</text>
              <text class="guide-p">· 两边抽到的卡不一样：盒号数据版本不同，双方都去设置里重新下载一次。</text>
              <text class="guide-p">· 中途断了：直连房间会直接解散（不支持断线重连），重新创建 / 加入即可。</text>
            </view>
          </view>
        </view>
        <!-- #endif -->

        <!-- 壳下未联机时的提示 -->
        <view v-if="isShell && netState === 'idle'" class="net-wait-start net-shell-hint">
          <text>Windows 端仅支持联机对战：填昵称后「创建房间」开房，或连对方 IP / 连接服务器大厅</text>
        </view>
      </view>

      <!-- 对战进行中界面 -->
      <view v-else class="battle-play-area">
        <view v-if="isShell && !battleNet" class="net-wait-start">
          <text>联机未连接，请先创建或加入房间</text>
        </view>
        <!-- 玩家1：本地镜像对战上下翻转，联机对战双方同向（正常朝上） -->
        <view v-if="!isShell || battleNet" class="battle-shell-local">
                <view class="player-area player-top" :class="{ mirror: !battleNet }">
                  <view class="player-header">
                    <text class="player-title">{{ battleNet ? netHostName : '玩家1 (镜像)' }}</text>
                    <text class="player-status" :class="{ waiting: player1.drew && !player2.drew }">
                      {{ player1.drew ? (player2.drew ? '已翻开' : '已抽·待对方') : '未抽' }}
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
                  <button class="battle-draw-btn" @click="playerDraw('player1')" :disabled="player1.drew || player1.remainingCount === 0 || isNetOpponent('player1')">
                    {{ battleNet ? (isNetOpponent('player1') ? '等待对方抽卡' : (player1.drew ? '已抽，等待对方' : '我抽卡')) : '玩家1抽卡' }}
                  </button>
                </view>

        <!-- 对战结果横幅 -->
        <view class="battle-result-banner" v-if="battleRoundResult">
          <text class="round-result" v-if="battleRoundResult === 'win'">{{ battleNet ? netHostName : '玩家1' }}胜！</text>
          <text class="round-result" v-else-if="battleRoundResult === 'lose'">{{ battleNet ? netGuestName : '玩家2' }}胜！</text>
          <text class="round-result" v-else-if="battleRoundResult === 'draw'">平局</text>
        </view>

        <!-- 玩家2 (正常) -->
        <view class="player-area player-bottom">
          <view class="player-header">
            <text class="player-title">{{ battleNet ? netGuestName : '玩家2' }}</text>
            <text class="player-status" :class="{ waiting: player2.drew && !player1.drew }">
              {{ player2.drew ? (player1.drew ? '已翻开' : '已抽·待对方') : '未抽' }}
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
          <button class="battle-draw-btn" @click="playerDraw('player2')" :disabled="player2.drew || player2.remainingCount === 0 || isNetOpponent('player2')">
            {{ battleNet ? (isNetOpponent('player2') ? '等待对方抽卡' : (player2.drew ? '已抽，等待对方' : '我抽卡')) : '玩家2抽卡' }}
          </button>
        </view>
        </view>
      </view>
    </view>

    </view>

    <!-- 历史记录按钮（放在最外层） -->
    <button v-if="battleStarted" class="history-btn global-history exit-battle-btn" @click="exitBattle">退出对战</button>
    <button v-else class="history-btn global-history" @click="showHistory = true">查看抽卡历史</button>

    <!-- 抽卡历史弹窗（完整版） -->
    <view class="history-modal" v-if="showHistory">
      <view class="modal-mask" @click="showHistory = false"></view>
      <view class="modal-content">
        <view class="modal-header">
          <text class="modal-title">抽卡历史&统计</text>
          <button class="close-btn" @click="showHistory = false">×</button>
        </view>

        <!-- 统计信息面板（可折叠） -->
        <view class="stats-panel">
          <view class="stats-collapse-head" @click="statsExpanded = !statsExpanded">
            <text class="stats-collapse-title">统计信息</text>
            <text class="stats-collapse-summary">总抽数 {{ totalDraws }}</text>
            <text class="stats-collapse-summary" :class="pnlTotalNet > 0 ? 'pnl-up' : (pnlTotalNet < 0 ? 'pnl-down' : '')">累计盈亏 {{ pnlTotalNet > 0 ? '+' : '' }}{{ pnlTotalNet }}</text>
            <text class="stats-collapse-toggle">{{ statsExpanded ? '收起 ▲' : '展开 ▼' }}</text>
          </view>
          <block v-if="statsExpanded">
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

          <!-- 累计盈亏统计（不随天重置，可手动清空） -->
          <view class="stats-section">
            <view class="pnl-total-head">
              <text class="stats-title">累计盈亏</text>
              <text class="pnl-total-note">不每日重置 · 每抽￥25 · 无市价免费</text>
            </view>
            <view class="stats-grid">
              <view class="stat-item">
                <text class="stat-label">累计投入</text>
                <text class="stat-value">￥{{ pnlTotal.cost }}</text>
              </view>
              <view class="stat-item">
                <text class="stat-label">累计回款</text>
                <text class="stat-value">￥{{ pnlTotal.gain }}</text>
              </view>
              <view class="stat-item">
                <text class="stat-label">净盈亏</text>
                <text class="stat-value" :class="pnlTotalNet > 0 ? 'pnl-up' : (pnlTotalNet < 0 ? 'pnl-down' : '')">{{ pnlTotalNet > 0 ? '+' : '' }}{{ pnlTotalNet }}</text>
              </view>
              <view class="stat-item">
                <text class="stat-label">付费抽数</text>
                <text class="stat-value">{{ pnlTotal.count }}</text>
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
                <button class="pnl-clear-btn" @click="clearPnlTotal">
                  <text class="button-text">清空盈亏</text>
                </button>
              </view>
            </view>
          </view>
          </block>
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
    <ak-music-bar />
  </view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
import { markDailyTask } from "@/utils/dailyTask.js";
import boxWidget from "@/utils/boxWidget.js";
import { avatarOf } from "@/utils/eliteAvatar.js";
import wideLayout, { isShell } from "@/utils/wideLayout.js";
export default {
  mixins: [wideLayout],
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

      // 盈亏系统数据（每天重置）
      pnlBalance: 1000,      // 今日剩余资金
      pnlToday: 0,           // 今日累计盈亏
      pnlDrawCount: 0,       // 今日抽数
      pnlLastChange: null,   // 最近一抽 { cost, gain, ten }

      // 累计盈亏统计（不随天重置，可手动清空）
      pnlTotal: { cost: 0, gain: 0, count: 0 },

      // 历史弹窗统计区折叠状态
      statsExpanded: false,

      // 联机教程弹窗
      showNetGuide: false,

      // 对战模式数据
      battleMode: false,
      isShell: isShell(),
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
      // 联机对战（大厅服务端模式）
      netRole: '',
      netWs: null,
      netServerAddr: '',
      netPeerAddr: '',      // P2P 直连：对方 IP:端口
      netMode: '',          // '' | 'p2p' | 'lobby' —— 区分两条线的协议
      netIntent: '',        // '' | 'host' | 'join' —— P2P 本次连接意图，用于校验服务端返回的角色
      netHostAddr: '',
      netPeerName: '',
      netHostName: '房主',
      netGuestName: '挑战者',
      netHostCard: null,   // 联机：房主抽到的卡（双方都抽完前暂存，不翻开）
      netGuestCard: null,  // 联机：挑战者抽到的卡（同上）
      netPeerJoined: false,
      // 大厅模式状态
      netState: 'idle',       // idle | lobby | room | playing
      netPanelOpen: false,    // 手机端联机区块是否手动展开（壳内恒展开）
      netName: '',
      netMyId: '',
      netPlayers: [],
      netRoomRole: '',        // host | guest
      netRoomId: '',
      netPendingFrom: '',     // 收到的邀请来源 id
      netPendingFromName: '', // 收到的邀请来源昵称
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
    pnlLastChangeNet() {
      if (!this.pnlLastChange) return 0;
      return (this.pnlLastChange.gain || 0) - (this.pnlLastChange.cost || 0);
    },
    pnlTotalNet() {
      return (this.pnlTotal.gain || 0) - (this.pnlTotal.cost || 0);
    },
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
    // 当前盒全部卡面及抽取状态（已抽出灰掉）
    // 常规/联动：双精拆成「精一」「精二」；白名单/音律/特别款：一人一卡，只显示干员名
    boxCardList() {
      if (!this.selectedBox) return [];
      const boxType = this.selectedBox.Box_type;
      const noEliteLabel =
        boxType === 'whitelist' || boxType === 'ambience' || boxType === 'special';
      const characters = this.getBoxCharacters(this.selectedBox);
      const list = [];
      const drawnKeys = new Set(
        (this.drawnCards || []).map(c => `${c.name}|${c.eliteLevel}`)
      );
      characters.forEach(character => {
        if (noEliteLabel) {
          // 此类盒号无精一精二之分，且每盒单角色只有一张卡
          const key = `${character.name}|1`;
          list.push({
            key,
            label: character.name,
            name: character.name,
            eliteLevel: 1,
            avatar: avatarOf(character, 1, boxType),
            avatarElite1: character.avatar || '',
            drawn: drawnKeys.has(key)
          });
          return;
        }
        const pushCard = (eliteLevel) => {
          const key = `${character.name}|${eliteLevel}`;
          list.push({
            key,
            label: `${character.name}·精${eliteLevel === 1 ? '一' : '二'}`,
            name: character.name,
            eliteLevel,
            avatar: avatarOf(character, eliteLevel, boxType),
            avatarElite1: character.avatar || '',
            drawn: drawnKeys.has(key)
          });
        };
        if (character.nolyELITE1) {
          pushCard(1);
        } else {
          pushCard(1);
          pushCard(2);
        }
      });
      return list;
    },
    drawnCount() {
      return this.boxCardList.filter(c => c.drawn).length;
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
    battleNet() { return !!this.netRole; },
    netPlatformName() {
      if (this.isShell) return 'Windows';
      // #ifdef MP-WEIXIN
      return '微信小程序';
      // #endif
      // #ifdef APP-PLUS
      return 'App';
      // #endif
      return 'H5';
    },
    netRoomText() {
      if (this.netRoomRole === 'host') {
        if (this.netPeerJoined) return `对方 ${this.netPeerName} 已加入房间`;
        // 房主等待：把连接地址告诉对方（P2P 直连尤其需要）
        return `房间已创建，把 ${this.netHostAddr || '127.0.0.1:23515'} 告诉对方加入`;
      }
      return `已连接房主 ${this.netHostName}，等待开始…`;
    },
    isNetGuest() { return this.netRole === 'guest'; },
    isNetWaitingPeer() { return this.battleNet && this.netRole === 'host' && !this.netPeerJoined; },
    // 联机区块展开态：Windows 壳恒展开；手机端默认收起，连接中/已连接自动展开
    netPanelExpanded() {
      return this.isShell || this.netPanelOpen || this.netState !== 'idle';
    },
    netStartLabel() {
      if (this.isNetWaitingPeer) return '等待对方加入…';
      if (this.battleNet) return '开始对战（已连接对方）';
      return '开始对战';
    },
    netStatusText() {
      if (this.netRole === 'host') {
        return this.netPeerJoined
          ? `对方 ${this.netPeerName} 已连接，点下方开始对战`
          : `房间已创建，把 ${this.netHostAddr || '127.0.0.1:23515'} 告诉对方`;
      }
      return '已连接房主，等待开始对战…';
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
    // 每日任务打点
    markDailyTask('gacha_try');
    // 加载主题设置
    this.loadThemeSetting();

    uni.setNavigationBarTitle({ title: '通行证模拟抽卡工具' });
    this.loadLocalData();
    this.loadGachaHistory();
    this.loadPnl();
    this.loadPnlTotal();
    this.selectedType = this.typeOptions[0];
    this.battleSelectedType = this.typeOptions[0];
    // 默认分类已选中，盒号列表要同步筛出来，否则会显示「请先选择分类」
    this.filterBoxesByType();
    if (this.wideLayout) this.filterBattleBoxes();
    if (wx && wx.showShareMenu) wx.showShareMenu({ withShareTicket: true, menus: ['shareAppMessage', 'shareTimeline'] });
  },

  onUnload() { this.netClose(); },

  onShow() {
    // 桌面小组件里抽的卡会攒在原生日志中（桌面侧写不了 App 的 Storage）。
    // 每次回到本页先合并一次；真实变更时才重新读盘，避免无谓重渲染。
    try {
      const r = boxWidget.drainGachaJournal();
      if (r && r.merged > 0) {
        this.loadGachaHistory();
        this.loadPnl();
        this.loadPnlTotal();
        this.loadBoxProgress();
      }
    } catch (e) {
      console.warn('[抽卡页] 合并小组件记录失败', e);
    }
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
          this.promptDownloadData();
        }
      } catch (e) {
      	this.logError(e);
        console.error('加载数据失败:', e);
        uni.showToast({ title: '数据加载失败', icon: 'none' });
      }
    },

    // 未下载数据时引导回首页（弹窗确认 + reLaunch + 高亮下载按钮）
    promptDownloadData() {
      uni.showModal({
        title: '需要先下载数据',
        content: '尚未检测到干员数据，请先到首页点击「更新干员数据」完成下载',
        confirmText: '去首页',
        cancelText: '取消',
        success: (res) => {
          if (!res.confirm) return;
          try { uni.setStorageSync('needHighlight', true); } catch (storageErr) { this.logError(storageErr); }
          uni.reLaunch({ url: '/pages/index/index' });
        }
      });
    },

    // 下载干员数据（首次打开或本地缺失时使用）
    async downloadCharacterData() {
      uni.showLoading({ title: '正在下载干员数据...' });
      // 多源降级：知晓云URL → GitCode Raw → jsDelivr → GitHub Raw
      const sourceUrls = [
        () => {
          let url = uni.getStorageSync('dataUrl');
          if (!url) return null;
          // #ifdef H5
          if (url && url.includes('raw.gitcode.com')) url = url.replace('https://raw.gitcode.com', '/gitcode');
          // #endif
          return url;
        },
        () => {
          // #ifdef H5
          return '/gitcode/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
          // #else
          return 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json';
          // #endif
        },
        () => 'https://cdn.jsdelivr.net/gh/awadwd/ArknightsAuthorization_Series-mirror@main/Box_Id.json',
        () => 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/Box_Id.json',
      ];
      let success = false;
      for (const getUrl of sourceUrls) {
        const url = getUrl();
        if (!url) continue;
        try {
          const res = await new Promise((resolve, reject) => {
            uni.request({ url, method: 'GET', timeout: 15000,
              success: r => resolve(r),
              fail: e => reject(e)
            });
          });
          if (res.statusCode === 200) {
            let data = res.data;
            if (typeof data === 'string') {
              try { data = JSON.parse(data); } catch (e) {
              	this.logError(e);
              	continue; 
              }
            }
            if (Array.isArray(data) && data.length > 0) {
              const cleanedData = data.map(box => {
                const cleanedBox = {};
                for (const key in box) {
                  if (key === 'Box_id') cleanedBox[key] = String(box[key] || '');
                  else if (key.startsWith('character')) cleanedBox[key] = box[key];
                  else cleanedBox[key] = String(box[key] || '');
                }
                return cleanedBox;
              });
              const now = new Date();
              const pad = n => (n < 10 ? '0' + n : '' + n);
              const localUpdateTime = now.getFullYear() + '-' + pad(now.getMonth() + 1) + '-' + pad(now.getDate()) + ' ' + pad(now.getHours()) + ':' + pad(now.getMinutes());
              uni.setStorageSync('arknightsData', cleanedData);
              uni.setStorageSync('localUpdateTime', localUpdateTime);
              uni.hideLoading();
              uni.showToast({ title: '数据下载成功', icon: 'success', duration: 1500 });
              this.loadLocalData();
              success = true;
              break;
            }
          }
        } catch (e) {
        	this.logError(e);
          console.warn('数据源下载失败: ' + url, e);
          continue;
        }
      }
      if (!success) {
        uni.hideLoading();
        uni.showToast({ title: '下载失败，请检查网络', icon: 'none' });
      }
    },

    // 准备可用盒号列表
    prepareAvailableBoxes() {
      this.availableBoxes = this.characterData.map(box => ({
        ...box,
        displayName: `${box.Box_id} (${this.getBoxTypeText(box.Box_type)})`
      }));
      // 宽屏下普通/对战两栏同时渲染，对战栏不经过「切换」就要能直接开抽，
      // 所以数据就绪后先把对战卡池筛出来（窄屏保持原逻辑，切过去时才筛）。
      if (this.wideLayout && this.battleSelectedType) this.filterBattleBoxes();
      if (this.selectedType) this.filterBoxesByType();
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

    // 辅助函数：根据角色、精英等级生成卡片对象，并设置价格与对应头像
    // 精一 → 精一头像，精二 → 精二头像（没有精二的干员自动退回精一）
    createCard(character, eliteLevel, sourceBox, boxType) {
      let price = 0;
      if (character.market_price) {
        if (eliteLevel === 1 && character.market_price.ELITE1 !== undefined) {
          price = character.market_price.ELITE1;
        } else if (eliteLevel === 2 && character.market_price.ELITE2 !== undefined) {
          price = character.market_price.ELITE2;
        }
      }
      const avatarElite1 = character.avatar || '';
      return {
        ...character,
        eliteLevel,
        price,
        sourceBox,
        boxType,
        avatar: avatarOf(character, eliteLevel, boxType) || avatarElite1,
        avatarElite1
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
        // 结果来自预生成卡池，点击时即已知：有市价立即扣 25，无市价免费
        const result = this.currentBoxPool[this.currentIndex];
        const hasPrice = !!(result && result.price > 0);
        const pnlCost = hasPrice ? 25 : 0;
        if (hasPrice) {
          this.pnlBalance -= 25;
          this.savePnl();
        }
        this.startGachaAnimation();
        await new Promise(resolve => setTimeout(resolve, 1500));
        this.currentIndex++;
        this.currentResult = result;
        this.drawnCards.push(result);
        this.saveBoxProgress();
        this.addToHistory(result, isFirstInBox, isLastInBox, false);
        if (isLastInBox) this.markBoxEnd();
        this.endGachaAnimation();
        // 动画展示完毕后结算回款
        const pnlGain = hasPrice ? result.price : 0;
        this.pnlBalance += pnlGain;
        this.pnlToday += pnlGain - pnlCost;
        this.pnlDrawCount++;
        this.pnlLastChange = { cost: pnlCost, gain: pnlGain };
        this.addPnlTotal(pnlCost, pnlGain, hasPrice ? 1 : 0);
        this.savePnl();
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
      // 先确定结果：有市价立即扣 25，无市价免费
      const randomIndex = Math.floor(Math.random() * this.superMixAllCards.length);
      const result = { ...this.superMixAllCards[randomIndex] };
      const hasPrice = !!(result && result.price > 0);
      const pnlCost = hasPrice ? 25 : 0;
      if (hasPrice) {
        this.pnlBalance -= 25;
        this.savePnl();
      }
      this.startGachaAnimation();
      await new Promise(resolve => setTimeout(resolve, 1500));
      this.currentResult = result;
      this.addToHistory(result, false, false, true);
      this.endGachaAnimation();
      // 动画展示完毕后结算回款
      const pnlGain = hasPrice ? result.price : 0;
      this.pnlBalance += pnlGain;
      this.pnlToday += pnlGain - pnlCost;
      this.pnlDrawCount++;
      this.pnlLastChange = { cost: pnlCost, gain: pnlGain };
      this.addPnlTotal(pnlCost, pnlGain, hasPrice ? 1 : 0);
      this.savePnl();
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
      if (history && Array.isArray(history)) {
        // 旧记录里精二抽卡存的还是精一头像，按精英等级补齐
        history.forEach(r => {
          if (!r || !r.character || Number(r.eliteLevel) !== 2) return;
          const c = r.character;
          if (!c.avatarElite1) c.avatarElite1 = c.avatar || '';
          const fixed = avatarOf({ ...c, avatar: c.avatarElite1 }, 2, r.boxType || c.boxType || '');
          if (fixed) c.avatar = fixed;
        });
        this.gachaHistory = history;
      }
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
        shareText += `欧非度: ${this.luckLevel} (${this.luckRatio}%)\n有效统计次数: ${this.qualifiedDraws}\n`;
      } else {
        shareText += `欧非度: 暂无有效记录\n`;
      }
      const totalNet = this.pnlTotalNet;
      shareText += `累计盈亏: ${totalNet >= 0 ? '+' : ''}${totalNet} 元（投入${this.pnlTotal.cost}/回款${this.pnlTotal.gain}）\n\n`;
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
    // ---------- 盈亏系统 ----------
    getTodayStr() {
      const d = new Date();
      const m = (d.getMonth() + 1).toString().padStart(2, '0');
      const day = d.getDate().toString().padStart(2, '0');
      return `${d.getFullYear()}-${m}-${day}`;
    },
    loadPnl() {
      const rec = uni.getStorageSync('gacha_pnl_daily');
      const today = this.getTodayStr();
      if (rec && rec.date === today) {
        this.pnlBalance = typeof rec.balance === 'number' ? rec.balance : 1000;
        this.pnlToday = rec.today || 0;
        this.pnlDrawCount = rec.drawCount || 0;
      } else {
        // 跨天重置：初始资金 1000
        this.pnlBalance = 1000;
        this.pnlToday = 0;
        this.pnlDrawCount = 0;
        this.pnlLastChange = null;
        this.savePnl();
      }
    },
    savePnl() {
      uni.setStorageSync('gacha_pnl_daily', {
        date: this.getTodayStr(),
        balance: this.pnlBalance,
        today: this.pnlToday,
        drawCount: this.pnlDrawCount
      });
    },
    loadPnlTotal() {
      const rec = uni.getStorageSync('gacha_pnl_total');
      if (rec && typeof rec === 'object') {
        this.pnlTotal = {
          cost: typeof rec.cost === 'number' ? rec.cost : 0,
          gain: typeof rec.gain === 'number' ? rec.gain : 0,
          count: typeof rec.count === 'number' ? rec.count : 0
        };
      }
    },
    savePnlTotal() {
      uni.setStorageSync('gacha_pnl_total', this.pnlTotal);
    },
    // 累计盈亏累加：cost/gain 为金额，count 为付费抽数
    addPnlTotal(cost, gain, count) {
      this.pnlTotal.cost += cost;
      this.pnlTotal.gain += gain;
      this.pnlTotal.count += count;
      this.savePnlTotal();
    },
    clearPnlTotal() {
      uni.showModal({
        title: '确认清空盈亏',
        content: '确定要清空累计盈亏统计吗？（不影响今日盈亏和抽卡历史）',
        success: (res) => {
          if (res.confirm) {
            this.pnlTotal = { cost: 0, gain: 0, count: 0 };
            uni.removeStorageSync('gacha_pnl_total');
            uni.showToast({ title: '累计盈亏已清空', icon: 'success' });
          }
        }
      });
    },

    // ---------- 模式切换 ----------
    setBattleMode(v) {
      if (this.battleMode === v) return;
      this.battleMode = v;
      if (v) {
        this.resetBattle();
      } else {
        this.battleStarted = false;
      }
    },

    resetBattle() {
      if (this.netWs) this.netClose();
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
      this.netHostCard = null;
      this.netGuestCard = null;
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

    // 构建对战牌库（按当前选中的盒号或混池；联机默认全部盒号）
    buildBattlePool() {
      if (this.battleIsSuperMixPool) {
        return this.battleSuperMixAllCards.filter(c => c.price > 0);
      }
      const box = this.battleSelectedBox;
      const allCards = [];
      const collect = (b) => {
        const chars = this.getBoxCharacters(b);
        const noElite = b.Box_type === 'whitelist' || b.Box_type === 'ambience' || b.Box_type === 'special';
        chars.forEach(c => {
          if (noElite || c.nolyELITE1) {
            const card = this.createCard(c, 1, null, b.Box_type);
            if (card) allCards.push(card);
          } else {
            const c1 = this.createCard(c, 1, null, b.Box_type);
            const c2 = this.createCard(c, 2, null, b.Box_type);
            if (c1) allCards.push(c1);
            if (c2) allCards.push(c2);
          }
        });
      };
      if (box) {
        collect(box);
      } else if (this.availableBoxes && this.availableBoxes.length) {
        // 壳下未选盒号（联机）：全部盒号
        this.availableBoxes.forEach(collect);
      }
      return this.shuffleArray(allCards.filter(c => c.price > 0));
    },

    startBattle() {
      // 联机对战：牌库构建交给服务器（壳下只走这里；手机端联机也走这里）
      if (this.battleNet) {
        const cards = this.buildBattlePool();
        if (!cards.length) { uni.showToast({ title: '没有可用卡牌', icon: 'none' }); return; }
        this.battlePool = cards;
        this.battleTotal = cards.length;
        this.netSendConfig();
        return;
      }
      if (this.isShell) {
        uni.showToast({ title: 'Windows 端请先创建或加入联机房间', icon: 'none' });
        return;
      }
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

    // ===== 联机对战 =====
    netSend(obj) {
      try { this.netWs && this.netWs.send({ data: JSON.stringify(obj) }); }
      catch (e) { console.warn('[联机] 发送失败', e); }
    },
    netSendConfig() {
      const cards = this.battlePool.map(c => ({ n: c.name, e: c.eliteLevel, p: c.price || 0, a: c.avatar || '' }));
      this.netSend({ t: 'cfg', super: this.battleIsSuperMixPool, cards });
      uni.showToast({ title: `牌库已就绪（${cards.length} 张）`, icon: 'none' });
    },
    netStartBattle(msg) {
      this.battleIsSuperMixPool = !!msg.super;
      this.battleTotal = msg.total;
      this.battleIndex = 0;
      this.battlePool = [];
      this.netHostName = msg.host || '房主';
      this.netGuestName = msg.guest || '挑战者';
      const mk = () => ({ lastResult: null, drew: false, remainingCount: msg.total, totalCount: msg.total });
      this.player1 = mk();
      this.player2 = mk();
      this.netHostCard = null;
      this.netGuestCard = null;
      this.battleRoundResult = null;
      this.battleStarted = true;
    },
    // ===== 大厅模式（线2：独立服务端）=====
    connectLobby() {
      const name = (this.netName || '').trim();
      if (!name) { uni.showToast({ title: '请输入昵称', icon: 'none' }); return; }
      let addr = (this.netServerAddr || '').trim();
      if (!addr) addr = '127.0.0.1:23516';
      if (addr.indexOf(':') === -1) addr += ':23516';
      // 入口拦截：主机部分不能为空（如 ":23516"），端口必须是数字
      const hostPart = addr.slice(0, addr.indexOf(':'));
      if (!hostPart) { uni.showToast({ title: '服务器 IP 不能为空', icon: 'none' }); return; }
      const portPart = addr.slice(addr.indexOf(':') + 1);
      if (!/^\d{1,5}$/.test(portPart)) { uni.showToast({ title: '端口格式不正确', icon: 'none' }); return; }
      if (this.netWs) this.netClose();
      // 连接成功前用 connecting 态，收到 welcome 才进 lobby，避免「已连接大厅」提前闪现
      const ws = uni.connectSocket({ url: 'ws://' + addr, timeout: 4000, complete: () => {} });
      this.netWs = ws;
      this.netMode = 'lobby';
      this.netState = 'connecting';
      ws.onOpen(() => this.netSend({ t: 'hello', name }));
      ws.onMessage((ev) => {
        const raw = typeof ev.data === 'string' ? ev.data : '';
        this.onNetMessage(raw);
      });
      ws.onError(() => {
        if (this.netState === 'idle') return;
        uni.showToast({ title: '连接失败，请检查服务器地址', icon: 'none' });
        this.netClose(true);
      });
      ws.onClose(() => {
        if (this.netState !== 'idle') this.netClose(true);
      });
    },
    // ===== P2P 直连（线1：房主=Windows 壳监听 23515）=====
    // 创建房间：连本机 BattleServer（壳随启动已监听 23515），成为房主
    hostDirect() {
      const name = (this.netName || '').trim();
      if (!name) { uni.showToast({ title: '请输入昵称', icon: 'none' }); return; }
      // 只有 Windows 壳（WebView2 内跑 H5）能开房；手机 App / 小程序 / 浏览器一律禁止
      if (!this.isShell) { uni.showToast({ title: '本机不支持开房，仅 Windows 端可开房', icon: 'none' }); return; }
      try {
        // #ifdef APP-PLUS
        uni.showToast({ title: '手机端不支持开房，仅可加入 Windows 端的房间', icon: 'none' }); return;
        // #endif
      } catch (e) {}
      if (this.netWs) this.netClose();
      this.netMode = 'p2p';
      this.netIntent = 'host';
      this.netState = 'connecting';   // 握手完成前不进 room，收到 welcome 才进
      // Windows 壳不再开机就监听 23515：先让它起来，再连本机。
      // 本机 Listen 通常只要几毫秒，260ms 是给跨进程消息 + WebView 主线程排队的余量。
      this.shellBattleSignal('start');
      setTimeout(() => this.openHostSocket(name), this.isShell ? 260 : 0);
    },
    // 连本机对战服务（外壳的 BattleServer）
    openHostSocket(name) {
      // 这 260ms 内用户可能已经点了「离开」：状态不在开房流程就直接放弃
      if (this.netState === 'idle' || this.netIntent !== 'host') return;
      const ws = uni.connectSocket({ url: 'ws://127.0.0.1:23515', timeout: 4000, complete: () => {} });
      this.netWs = ws;
      ws.onOpen(() => this.netSend({ t: 'hello', name }));
      ws.onMessage((ev) => {
        const raw = typeof ev.data === 'string' ? ev.data : '';
        this.onNetMessage(raw);
      });
      ws.onError(() => {
        if (this.netState === 'idle') return;
        uni.showToast({ title: '开房失败，请重试', icon: 'none' });
        this.netClose(true);
      });
      ws.onClose(() => {
        if (this.netState !== 'idle') this.netClose(true);
      });
    },
    // 加入房间：连对方（房主）IP:23515，成为挑战者
    joinDirect() {
      const name = (this.netName || '').trim();
      if (!name) { uni.showToast({ title: '请输入昵称', icon: 'none' }); return; }
      let addr = (this.netPeerAddr || '').trim();
      if (!addr) { uni.showToast({ title: '请输入房主 IP:端口', icon: 'none' }); return; }
      // 拦截畸形地址：只有端口没有 IP（如 ":23515"）会误连，要求必须含主机部分
      const hostPart = addr.indexOf(':') === -1 ? addr : addr.slice(0, addr.indexOf(':'));
      if (!hostPart) { uni.showToast({ title: 'IP 不能为空，请输入房主 IP', icon: 'none' }); return; }
      if (addr.indexOf(':') === -1) addr += ':23515';
      if (this.netWs) this.netClose();
      const ws = uni.connectSocket({ url: 'ws://' + addr, timeout: 4000, complete: () => {} });
      this.netWs = ws;
      this.netMode = 'p2p';
      this.netIntent = 'join';
      this.netState = 'connecting';   // 握手完成前不进 room，收到 welcome 才进
      ws.onOpen(() => this.netSend({ t: 'hello', name }));
      ws.onMessage((ev) => {
        const raw = typeof ev.data === 'string' ? ev.data : '';
        this.onNetMessage(raw);
      });
      ws.onError(() => {
        if (this.netState === 'idle') return;
        uni.showToast({ title: '连接失败，请检查对方 IP 和端口', icon: 'none' });
        this.netClose(true);
      });
      ws.onClose(() => {
        if (this.netState !== 'idle') this.netClose(true);
      });
    },
    invitePlayer(id) {
      if (this.netState !== 'lobby') return;
      this.netSend({ t: 'invite', to: id });
      uni.showToast({ title: '已发送邀请', icon: 'none' });
    },
    kickPeer() {
      if (this.netRoomRole !== 'host') return;
      if (this.netMode === 'p2p') {
        // P2P 直连的 BattleServer 无 kick 协议；踢人=离开房间（对方会收到 peer_left）
        uni.showModal({
          title: '结束对战', content: 'P2P 直连不支持单独踢人，离开房间会同时解散对局。确定离开？', showCancel: true,
          success: (r) => { if (r.confirm) this.leaveRoom(); }
        });
        return;
      }
      uni.showModal({
        title: '踢出玩家', content: `确定把 ${this.netPeerName} 踢出房间？`, showCancel: true,
        success: (r) => { if (r.confirm) this.netSend({ t: 'kick' }); }
      });
    },
    leaveRoom() {
      if (this.netMode === 'p2p') {
        // P2P 直连无大厅，离开即断开回 idle
        this.netClose();
        this.resetBattle();
        return;
      }
      this.netSend({ t: 'leave_room' });
      this.exitRoomToLobby();
    },
    exitRoomToLobby() {
      if (this.netMode === 'p2p') {
        this.netClose();
        return;
      }
      this.netState = 'lobby';
      this.netRoomRole = '';
      this.netRoomId = '';
      this.netPeerJoined = false;
      this.netPeerName = '';
      this.battleStarted = false;
      this.netRole = '';
    },
    hostNetBattle() { this.hostDirect(); },
    joinNetBattle() { this.joinDirect(); },
    netMyName() {
      const n = (this.netName || '').trim();
      return n || (this.netRole === 'host' ? '房主' : '挑战者');
    },
    // 手机端折叠/展开联机区块；联机进行中不允许收起
    toggleNetPanel() {
      if (this.isShell) return;
      if (this.netState !== 'idle') return;
      this.netPanelOpen = !this.netPanelOpen;
    },
    // 通知 Windows 壳开关联机端口。浏览器 / 小程序没有这个通道，静默跳过。
    shellBattleSignal(action) {
      try {
        if (!this.isShell) return;
        const chrome = typeof window !== 'undefined' ? window.chrome : null;
        const bridge = chrome && chrome.webview ? chrome.webview : null;
        if (bridge && typeof bridge.postMessage === 'function') {
          bridge.postMessage(JSON.stringify({ type: 'battle', action }));
        }
      } catch (e) {}
    },
    netClose(alreadyDead) {
      // 先把引用摘掉：onError / onClose 里再进来时不会重复 close
      const ws = this.netWs;
      this.netWs = null;
      // alreadyDead = 由 onError / onClose 触发，底层连接已断，再 close 会走 1006 而被内核拒绝
      if (ws && !alreadyDead) {
        try {
          // 微信小程序（Windows 版走 Chromium）只接受 1000 或 3000-4999；
          // 不传或传 1006 会抛 "The close code must be either 1000, or between 3000 and 4999"
          ws.close({ code: 1000, reason: 'client close' });
        } catch (e) {
          try { ws.close(); } catch (e2) {}
        }
      }
      this.shellBattleSignal('stop');   // 离开房间即关端口，不留长尾监听
      this.netRole = '';
      this.netPeerJoined = false;
      this.netPeerName = '';
      this.netState = 'idle';
      this.netRoomRole = '';
      this.netRoomId = '';
      this.netPlayers = [];
      this.netMode = '';
      this.netIntent = '';
    },
    leaveNet() {
      this.netClose();
      uni.showToast({ title: '已断开联机', icon: 'none' });
    },
    onNetMessage(raw) {
      let msg; try { msg = JSON.parse(raw); } catch (e) { return; }
      switch (msg.t) {
        case 'welcome':
          // 两条线协议分流：
          //   P2P 直连（BattleServer）：{t,welcome, role, lan} —— role 决定房主/挑战者
          //   大厅（LobbyServer）：{t,welcome, id, name, lan} —— 进大厅列表
          if (this.netMode === 'p2p') {
            // 兜底校验：本次点的是「加入房间」，服务端却回 role=host —— 说明对端把本机
            // 当成了房主（旧版服务端是「先到先得」，手机随手填个局域网 IP 连上就会命中，
            // 表现成手机也能开房）。直接断开，绝不让「加入」变成「创建房间」。
            if (this.netIntent === 'join' && msg.role === 'host') {
              uni.showToast({ title: '该地址不是房主房间，请让对方先点「创建房间」', icon: 'none' });
              this.netClose();
              return;
            }
            this.netRole = msg.role || '';
            this.netRoomRole = msg.role || '';
            this.netHostAddr = msg.lan || '127.0.0.1:23515';
            if (msg.role === 'host') {
              this.netHostName = this.netMyName();
              this.netGuestName = '挑战者';
            } else {
              this.netGuestName = this.netMyName();
              this.netHostName = '房主';
            }
            this.netPeerJoined = false;
            this.netState = 'room';
            uni.showToast({
              title: msg.role === 'host' ? '房间已创建，等待对方加入' : '已加入房间，等待房主开始',
              icon: 'none'
            });
          } else {
            // 大厅模式：真正收到 welcome 才确认连上，进入大厅列表
            this.netMyId = msg.id || '';
            this.netHostAddr = msg.lan || '';
            this.netState = 'lobby';
          }
          break;
        case 'err':
          // 服务端主动拒绝：P2P 下 code=no_host 表示「房主还没开房」。
          // 手机端到这一步就断开，不会残留一个假的「房间已创建」界面。
          uni.showToast({ title: msg.msg || '连接被拒绝', icon: 'none' });
          this.netClose();
          break;
        case 'peer':
          // P2P 直连：对方加入/重连时通知（BattleServer hello 后广播 peer）
          if (this.netMode === 'p2p') {
            this.netPeerName = msg.name || '对方';
            if (msg.role === 'guest') {
              this.netGuestName = msg.name || '挑战者';
              this.netPeerJoined = true;
              uni.showToast({ title: `对方 ${msg.name} 已加入`, icon: 'none' });
            } else {
              this.netHostName = msg.name || '房主';
              this.netPeerJoined = true;
            }
          }
          break;
        case 'list':
          this.netPlayers = (msg.players || []).filter(p => p.id !== this.netMyId);
          break;
        case 'invite':
          // 收到邀请，弹窗接受/拒绝
          this.netPendingFrom = msg.from || '';
          this.netPendingFromName = msg.fromName || '对方';
          uni.showModal({
            title: '对战邀请',
            content: `${this.netPendingFromName} 邀请你对战`,
            confirmText: '接受', cancelText: '拒绝',
            success: (r) => {
              if (r.confirm) this.netSend({ t: 'invite_accept', from: this.netPendingFrom });
              else this.netSend({ t: 'invite_decline', from: this.netPendingFrom });
              this.netPendingFrom = '';
            }
          });
          break;
        case 'invite_fail':
          uni.showToast({ title: msg.reason || '邀请失败', icon: 'none' });
          break;
        case 'invite_declined':
          uni.showToast({ title: `${msg.fromName || '对方'} 拒绝了邀请`, icon: 'none' });
          break;
        case 'invite_cancelled':
          uni.showToast({ title: `${msg.fromName || '对方'} 取消了邀请`, icon: 'none' });
          break;
        case 'room':
          // 进入房间
          this.netState = 'room';
          this.netRoomId = msg.roomId || '';
          this.netRoomRole = msg.role || '';
          this.netRole = msg.role || '';
          if (msg.role === 'host') {
            this.netHostName = this.netMyName();
            this.netGuestName = msg.peerName || '挑战者';
          } else {
            this.netGuestName = this.netMyName();
            this.netHostName = msg.peerName || '房主';
          }
          this.netPeerJoined = true;
          this.netPeerName = msg.peerName || '对方';
          uni.showToast({ title: '已进入房间', icon: 'none' });
          break;
        case 'start':
          this.netState = 'playing';
          this.netStartBattle(msg);
          break;
        case 'drew': this.onNetDrew(msg); break;
        case 'kicked':
          uni.showModal({
            title: '已被踢出', content: '房主把你踢出了房间', showCancel: false,
            success: () => this.exitRoomToLobby()
          });
          break;
        case 'kick_ok':
          uni.showToast({ title: '已踢出对方', icon: 'none' });
          this.exitRoomToLobby();
          break;
        case 'peer_left':
          uni.showModal({
            title: '对战结束', content: '对方已离开房间', showCancel: false,
            success: () => { this.exitRoomToLobby(); this.resetBattle(); }
          });
          break;
        case 'left_room':
          this.exitRoomToLobby();
          break;
        case 'reset':
          this.player1.drew = false;
          this.player2.drew = false;
          this.player1.lastResult = null;
          this.player2.lastResult = null;
          this.netHostCard = null;
          this.netGuestCard = null;
          this.battleRoundResult = null;
          break;
      }
    },
    async onNetDrew(msg) {
      const card = { name: msg.card.n, eliteLevel: msg.card.e, price: msg.card.p, avatar: msg.card.a };
      // 单方抽完后只暂存、标记已抽（不立即翻开），双方都抽完才同时翻开
      if (msg.by === 'host') { this.netHostCard = card; this.player1.drew = msg.hostDrew; }
      else { this.netGuestCard = card; this.player2.drew = msg.guestDrew; }
      this.player1.remainingCount = msg.remaining;
      this.player2.remainingCount = msg.remaining;
      if (this.player1.drew && this.player2.drew) {
        // 双方都抽完：同时翻开两张卡，再比较结果
        this.player1.lastResult = this.netHostCard;
        this.player2.lastResult = this.netGuestCard;
        this.netHostCard = null;
        this.netGuestCard = null;
        await this.showBothResults();
        this.compareAndShowResult();
      }
    },
    isNetOpponent(player) {
      if (!this.battleNet) return false;
      const iAmHost = this.netRole === 'host';
      return (player === 'player1') !== iAmHost;
    },

    async playerDraw(player) {
      if (this.battleNet) {
        if (this.isNetOpponent(player)) return;
        if ((player === 'player1' && this.player1.drew) || (player === 'player2' && this.player2.drew)) return;
        this.netSend({ t: 'draw' });
        return;
      }
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
      // 盈亏按每张结算：有市价卡扣 25 加回款，无市价卡免费
      let totalCost = 0;
      let totalGain = 0;
      let chargedCount = 0;
      for (let i = 0; i < 10; i++) {
        if (i >= this.superMixAllCards.length) break;
        const randomIndex = Math.floor(Math.random() * this.superMixAllCards.length);
        const card = { ...this.superMixAllCards[randomIndex] };
        this.tenGachaResults.push(card);
        this.addToHistory(card, false, false, true);
        if (card.price > 0) {
          totalCost += 25;
          totalGain += card.price;
          chargedCount++;
        }
      }
      this.pnlBalance += totalGain - totalCost;
      this.pnlToday += totalGain - totalCost;
      this.pnlDrawCount += chargedCount;
      this.pnlLastChange = { cost: totalCost, gain: totalGain, ten: true };
      this.addPnlTotal(totalCost, totalGain, chargedCount);
      this.savePnl();
      this.showTenGachaModal = true;
    },

    closeTenModal() {
      this.showTenGachaModal = false;
      this.tenGachaResults = [];
    },

    onAvatarError(character) {
      if (!character) return;
      // 精二头像加载失败 → 先退回精一，精一也失败才用默认图
      if (character.avatarElite1 && character.avatar !== character.avatarElite1) {
        character.avatar = character.avatarElite1;
        return;
      }
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
  border-radius: 20rpx;
  padding: 28rpx;
  box-shadow: 0 6rpx 24rpx rgba(15, 40, 80, 0.06);
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
  margin-bottom: 28rpx;
  padding: 24rpx;
  background: linear-gradient(180deg, #ffffff 0%, #f7f9fc 100%);
  border: 1rpx solid #e8eef5;
  border-radius: 16rpx;
  box-shadow: 0 4rpx 16rpx rgba(64, 158, 255, 0.06);
}
.box-controls-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16rpx;
  margin-bottom: 20rpx;
}
.box-info {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
  min-width: 0;
}
.box-type {
  font-size: 28rpx;
  color: #2b6cb0;
  font-weight: 600;
  letter-spacing: 1rpx;
}
.character-count {
  font-size: 24rpx;
  color: #8a94a6;
}
.reset-btn {
  background: linear-gradient(135deg, #f0b25a 0%, #e6a23c 100%);
  color: #fff;
  border-radius: 12rpx;
  font-size: 26rpx;
  font-weight: 500;
  padding: 14rpx 28rpx;
  white-space: nowrap;
  box-shadow: 0 4rpx 12rpx rgba(230, 162, 60, 0.28);
  line-height: 1.4;
}
.reset-btn::after {
  border: none;
}

/* 本盒卡面面板 */
.box-card-panel {
  background: #fff;
  border: 1rpx solid #eef2f7;
  border-radius: 12rpx;
  padding: 16rpx 18rpx 18rpx;
}
.box-card-panel-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14rpx;
}
.box-card-panel-title {
  font-size: 24rpx;
  font-weight: 600;
  color: #4a5568;
}
.box-card-panel-meta {
  font-size: 20rpx;
  color: #a0aec0;
}
.box-card-list {
  display: flex;
  flex-wrap: wrap;
  gap: 10rpx;
}
.box-card-item {
  padding: 8rpx 14rpx;
  background: linear-gradient(135deg, #ebf3ff 0%, #f5f9ff 100%);
  border-radius: 8rpx;
  border: 1rpx solid #d6e6ff;
  transition: opacity 0.15s ease;
}
.box-card-name {
  font-size: 22rpx;
  color: #2b6cb0;
  line-height: 1.35;
  font-weight: 500;
}
.box-card-item.drawn {
  background: #f4f5f7;
  border-color: #e4e7eb;
  opacity: 0.72;
}
.box-card-item.drawn .box-card-name {
  color: #b0b6c0;
  text-decoration: line-through;
  font-weight: 400;
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
  background: linear-gradient(135deg, #4d9fff 0%, #3b82f6 100%);
  color: #fff;
  border-radius: 50rpx;
  font-size: 32rpx;
  font-weight: 500;
  padding: 26rpx;
  margin-bottom: 30rpx;
  box-shadow: 0 8rpx 20rpx rgba(59, 130, 246, 0.25);
  transition: all 0.3s ease;
}
.gacha-btn::after {
  border: none;
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
  min-height: 460rpx;
}

/* 卡牌舞台：蓄力抖动 → 翻转收束 → 揭示弹入 */
.card-stage {
  width: 300rpx;
  height: 420rpx;
  position: relative;
  perspective: 1200rpx;
  margin: 0 auto;
}
.card-rarity-ring {
  position: absolute;
  left: 50%;
  top: 50%;
  width: 340rpx;
  height: 340rpx;
  margin: -170rpx 0 0 -170rpx;
  border-radius: 50%;
  pointer-events: none;
  opacity: 0;
  transform: scale(0.6);
}
.card-stage.is-revealed .card-rarity-ring {
  opacity: 1;
  transform: scale(1);
  transition: opacity 0.35s ease, transform 0.45s ease;
  animation: ringPulse 1.8s ease-in-out infinite;
}
.card-stage.elite-one .card-rarity-ring {
  background: radial-gradient(circle, rgba(102, 126, 234, 0.35) 0%, transparent 70%);
  box-shadow: 0 0 40rpx rgba(102, 126, 234, 0.25);
}
.card-stage.elite-two .card-rarity-ring {
  background: radial-gradient(circle, rgba(245, 87, 108, 0.4) 0%, transparent 70%);
  box-shadow: 0 0 50rpx rgba(245, 87, 108, 0.35);
}
.card-stage.is-hot.is-revealed .card-rarity-ring {
  background: radial-gradient(circle, rgba(255, 180, 50, 0.45) 0%, transparent 70%);
  box-shadow: 0 0 56rpx rgba(255, 160, 40, 0.4);
}
@keyframes ringPulse {
  0%, 100% { transform: scale(1); opacity: 0.85; }
  50% { transform: scale(1.08); opacity: 1; }
}

.card-face {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  border-radius: 20rpx;
  overflow: hidden;
  box-shadow: 0 12rpx 36rpx rgba(15, 30, 70, 0.22);
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  box-sizing: border-box;
}

/* 蓄力：弹入 + 抖动 + 扫光 */
.card-back {
  background:
    linear-gradient(145deg, rgba(255,255,255,0.12) 0%, transparent 40%),
    linear-gradient(135deg, #4c5fd7 0%, #7b4bc4 55%, #5a3fa0 100%);
  transform-origin: center center;
  animation: chargeIn 0.35s cubic-bezier(0.34, 1.4, 0.64, 1) both, chargeShake 1.1s 0.35s ease-in-out;
}
@keyframes chargeIn {
  0% { transform: scale(0.55) rotateY(-25deg); opacity: 0; }
  70% { transform: scale(1.05) rotateY(0deg); opacity: 1; }
  100% { transform: scale(1) rotateY(0deg); opacity: 1; }
}
@keyframes chargeShake {
  0%, 100% { transform: translateX(0) rotateZ(0); }
  15% { transform: translateX(-6rpx) rotateZ(-1.8deg); }
  30% { transform: translateX(6rpx) rotateZ(1.8deg); }
  45% { transform: translateX(-4rpx) rotateZ(-1.2deg); }
  60% { transform: translateX(4rpx) rotateZ(1.2deg); }
  75% { transform: translateX(-2rpx) rotateZ(-0.6deg); }
  90% { transform: translateX(2rpx) rotateZ(0.4deg); }
}
/* 蓄力收束：翻转前压扁 */
.card-stage.is-flipping .card-back {
  animation:
    chargeIn 0.35s cubic-bezier(0.34, 1.4, 0.64, 1) both,
    chargeShake 0.7s 0.35s ease-in-out,
    flipOut 0.45s 1.05s cubic-bezier(0.55, 0, 0.55, 0.2) forwards;
}
@keyframes flipOut {
  0% { transform: rotateY(0) scale(1); opacity: 1; }
  40% { transform: rotateY(50deg) scale(0.96); opacity: 1; }
  100% { transform: rotateY(95deg) scale(0.88); opacity: 0; }
}

.card-back-shine {
  position: absolute;
  left: -40%;
  top: -20%;
  width: 50%;
  height: 140%;
  background: linear-gradient(
    105deg,
    transparent 0%,
    rgba(255, 255, 255, 0.18) 45%,
    rgba(255, 255, 255, 0.38) 50%,
    rgba(255, 255, 255, 0.18) 55%,
    transparent 100%
  );
  transform: skewX(-18deg);
  pointer-events: none;
  animation: shineSweep 1s ease-in-out infinite;
}
@keyframes shineSweep {
  0% { left: -50%; }
  60% { left: 110%; }
  100% { left: 130%; }
}
.card-back-inner {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
}
.card-back-text {
  font-size: 96rpx;
  color: #fff;
  font-weight: 700;
  text-shadow: 0 6rpx 16rpx rgba(0, 0, 0, 0.35);
  line-height: 1;
  animation: qPulse 0.85s ease-in-out infinite;
}
@keyframes qPulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.1); opacity: 0.88; }
}
.card-back-sub {
  margin-top: 12rpx;
  font-size: 22rpx;
  color: rgba(255, 255, 255, 0.78);
  letter-spacing: 4rpx;
}

/* 揭示：从翻转态弹入 */
.card-front {
  color: #fff;
  padding: 24rpx 16rpx;
  animation: revealIn 0.65s cubic-bezier(0.34, 1.45, 0.64, 1) both;
}
@keyframes revealIn {
  0% {
    transform: rotateY(-90deg) scale(0.86);
    opacity: 0;
  }
  55% {
    transform: rotateY(8deg) scale(1.05);
    opacity: 1;
  }
  100% {
    transform: rotateY(0deg) scale(1);
    opacity: 1;
  }
}
.card-front-shine {
  position: absolute;
  inset: 0;
  background: linear-gradient(
    120deg,
    transparent 30%,
    rgba(255, 255, 255, 0.12) 48%,
    rgba(255, 255, 255, 0.28) 50%,
    rgba(255, 255, 255, 0.12) 52%,
    transparent 70%
  );
  transform: translateX(-120%);
  pointer-events: none;
  animation: frontShine 0.85s 0.12s ease-out both;
}
@keyframes frontShine {
  from { transform: translateX(-120%); }
  to { transform: translateX(120%); }
}

.card-front.elite-one {
  background:
    radial-gradient(circle at 30% 20%, rgba(255,255,255,0.16), transparent 45%),
    linear-gradient(160deg, #6b7ff0 0%, #7b5fd6 48%, #5b3fb8 100%);
}
.card-front.elite-two {
  background:
    radial-gradient(circle at 30% 18%, rgba(255,255,255,0.2), transparent 45%),
    linear-gradient(160deg, #ff7eb3 0%, #f5576c 48%, #d63b6a 100%);
  box-shadow: 0 14rpx 40rpx rgba(245, 87, 108, 0.35);
}
.card-stage.is-hot.is-revealed .card-front {
  box-shadow:
    0 14rpx 40rpx rgba(255, 150, 40, 0.35),
    0 0 0 2rpx rgba(255, 200, 80, 0.45);
}

/* 保留旧 class，避免对战/历史等引用失效 */
.card-container {
  width: 300rpx;
  height: 400rpx;
  position: relative;
  perspective: 1000rpx;
  margin: 0 auto;
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
  width: 132rpx;
  height: 132rpx;
  border-radius: 50%;
  margin-bottom: 18rpx;
  border: 4rpx solid rgba(255, 255, 255, 0.9);
  box-shadow: 0 6rpx 18rpx rgba(0, 0, 0, 0.25);
  background: rgba(255, 255, 255, 0.12);
  animation: avatarIn 0.55s 0.08s cubic-bezier(0.34, 1.4, 0.64, 1) both;
}
@keyframes avatarIn {
  from { transform: scale(0.7); opacity: 0.4; }
  to { transform: scale(1); opacity: 1; }
}
.character-name {
  display: block;
  font-size: 32rpx;
  font-weight: bold;
  margin-bottom: 10rpx;
  max-width: 90%;
  word-break: break-all;
  text-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.25);
}
.character-elite {
  display: block;
  font-size: 28rpx;
  font-weight: bold;
  opacity: 0.95;
  text-shadow: 0 2rpx 4rpx rgba(0, 0, 0, 0.3);
  margin-bottom: 5rpx;
  letter-spacing: 2rpx;
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
  background: linear-gradient(135deg, #ffb020 0%, #ff6a00 100%);
  color: #fff;
  font-size: 20rpx;
  font-weight: 600;
  padding: 6rpx 14rpx;
  border-radius: 20rpx;
  margin-top: 10rpx;
  box-shadow: 0 4rpx 10rpx rgba(255, 120, 0, 0.35);
  animation: hotBounce 0.7s 0.22s ease both;
}
@keyframes hotBounce {
  0% { transform: scale(0.5); opacity: 0; }
  60% { transform: scale(1.12); opacity: 1; }
  100% { transform: scale(1); opacity: 1; }
}
.character-price {
  display: inline-block;
  margin-top: 12rpx;
  font-size: 24rpx;
  padding: 4rpx 14rpx;
  border-radius: 16rpx;
  background: rgba(0, 0, 0, 0.18);
  color: #fff;
  border: 1rpx solid rgba(255, 255, 255, 0.25);
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
/* 统计区折叠头 */
.stats-collapse-head {
  display: flex;
  align-items: center;
  gap: 14rpx;
  padding: 6rpx 0;
}
.stats-collapse-title {
  font-size: 30rpx;
  font-weight: bold;
  color: #303133;
}
.stats-collapse-summary {
  flex: 1;
  font-size: 22rpx;
  color: #909399;
}
.stats-collapse-summary.pnl-up {
  color: #f56c6c;
  font-weight: bold;
  flex: none;
}
.stats-collapse-summary.pnl-down {
  color: #67C23A;
  font-weight: bold;
  flex: none;
}
.stats-collapse-toggle {
  font-size: 22rpx;
  color: #409EFF;
  flex-shrink: 0;
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
.pnl-clear-btn {
  border-radius: 8rpx;
  font-size: 24rpx;
  padding: 12rpx 20rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 50rpx;
  background-color: #E6A23C;
  color: #fff;
}
.pnl-total-head {
  display: flex;
  align-items: baseline;
  gap: 12rpx;
  margin-bottom: 12rpx;
}
.pnl-total-note {
  font-size: 20rpx;
  color: #909399;
}
.stat-value.pnl-up {
  color: #f56c6c;
}
.stat-value.pnl-down {
  color: #67C23A;
}
.share-icon {
  font-size: 22rpx;
  margin-right: 8rpx;
}
.button-text {
  font-size: 24rpx;
}
.history-list {
  max-height: 60vh;
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

/* 模式切换（胶囊分段按钮） */
.mode-switch {
  display: flex;
  justify-content: center;
  padding: 0;
  background-color: transparent;
  margin-bottom: 20rpx;
  border-radius: 16rpx;
}
/* ================= 宽屏两栏：普通模式 / 对战模式 ================= */
.gacha-split.is-wide {
  display: flex;
  align-items: flex-start;
  gap: 24rpx;
}

.gacha-split.is-wide .gacha-col {
  min-width: 0;
}

/* 会话内普通模式交互更密，给它略多一点宽度；对战区双玩家上下排布需要空间 */
.gacha-split.is-wide .col-normal {
  flex: 0 0 46%;
}

.gacha-split.is-wide .col-battle {
  flex: 1 1 0;
}

.mode-seg {
  display: flex;
  width: 100%;
  background-color: #fff;
  border-radius: 50rpx;
  padding: 8rpx;
  box-shadow: 0 2rpx 12rpx rgba(64, 158, 255, 0.12);
}
.mode-seg-item {
  flex: 1;
  text-align: center;
  padding: 18rpx 0;
  font-size: 28rpx;
  color: #606266;
  border-radius: 50rpx;
  transition: all 0.25s ease;
}
.mode-seg-item.active {
  background: linear-gradient(135deg, #409EFF 0%, #2b6cb0 100%);
  color: #fff;
  font-weight: bold;
  box-shadow: 0 4rpx 12rpx rgba(64, 158, 255, 0.35);
}

/* 盈亏展示信息区（紧凑版） */
.pnl-panel {
  background-color: #fff;
  border-radius: 16rpx;
  padding: 18rpx 24rpx;
  margin-bottom: 24rpx;
  border: 1rpx solid #e4e7ed;
  box-shadow: 0 2rpx 10rpx rgba(64, 158, 255, 0.08);
}
.pnl-row {
  display: flex;
  align-items: center;
  margin-bottom: 8rpx;
}
.pnl-item {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8rpx;
}
.pnl-item + .pnl-item {
  border-left: 1rpx solid #ebeef5;
  padding-left: 16rpx;
}
.pnl-label {
  font-size: 22rpx;
  color: #909399;
  flex-shrink: 0;
}
.pnl-value {
  font-size: 26rpx;
  font-weight: bold;
  color: #303133;
}
.pnl-value.up {
  color: #f56c6c;
}
.pnl-value.down {
  color: #67C23A;
}
.pnl-balance {
  color: #409EFF;
}
.pnl-note {
  display: block;
  font-size: 20rpx;
  color: #c0c4cc;
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
}/* ========== 界园风格 ========== */
.container.theme-jieyuan {
	background-color: #FAF3E0;
}
.container.theme-jieyuan .search-section,
.container.theme-jieyuan .result-card,
.container.theme-jieyuan .history-section,
.container.theme-jieyuan .gacha-section {
	background: rgba(255, 255, 255, 0.9);
	border: 1rpx solid rgba(226, 88, 132, 0.15);
}
.container.theme-jieyuan .box-controls {
	background: rgba(255, 255, 255, 0.92);
	border-color: rgba(226, 88, 132, 0.18);
	box-shadow: 0 4rpx 16rpx rgba(226, 88, 132, 0.08);
}
.container.theme-jieyuan .box-type { color: #e25884; }
.container.theme-jieyuan .box-card-panel {
	background: #fffdf8;
	border-color: rgba(226, 88, 132, 0.12);
}
.container.theme-jieyuan .box-card-item {
	background: linear-gradient(135deg, #fff0f5 0%, #f5fbf7 100%);
	border-color: rgba(226, 88, 132, 0.2);
}
.container.theme-jieyuan .box-card-name { color: #c94b76; }
.container.theme-jieyuan .box-card-item.drawn {
	background: #f4efe6;
	border-color: #e8dfd0;
}
.container.theme-jieyuan .box-card-item.drawn .box-card-name { color: #b0a89a; }
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

/* 联机对战 */
.net-battle-section {
  margin-top: 24rpx;
  padding: 20rpx;
  border-radius: 16rpx;
  background: rgba(64, 158, 255, 0.08);
  border: 2rpx solid rgba(64, 158, 255, 0.25);
}
/* 线下对战（本机双人镜像）：手机端主入口，Windows 壳不渲染 */
.local-battle-block {
  margin-top: 24rpx;
  padding: 22rpx 22rpx 24rpx;
  border-radius: 16rpx;
  background: #F1FAEC;
  border: 2rpx solid #B9E2A4;
}
.local-battle-title {
  display: block;
  font-size: 28rpx;
  font-weight: bold;
  color: #3F8B22;
}
.local-battle-desc {
  display: block;
  margin-top: 8rpx;
  font-size: 22rpx;
  line-height: 1.55;
  color: #6B7280;
}
.local-battle-block .battle-start-btn {
  width: 100%;
  margin-top: 18rpx;
}
.net-battle-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 28rpx;
  font-weight: 600;
  color: #409EFF;
  margin-bottom: 16rpx;
}
.net-panel-toggle {
  font-size: 22rpx;
  font-weight: 400;
  color: #7FA8D8;
}
/* 教程入口：页面内一行按钮，点开就地展开（不弹窗，避免弹层里滚不动） */
.net-guide-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 60rpx;
  margin-bottom: 16rpx;
  border-radius: 12rpx;
  border: 2rpx dashed rgba(64, 158, 255, 0.45);
  background: rgba(64, 158, 255, 0.06);
  font-size: 23rpx;
  color: #409EFF;
}
.net-guide-toggle:active {
  background: rgba(64, 158, 255, 0.14);
}
/* 展开后的说明块：普通 view，靠页面滚动，宽度锁死避免文字出界 */
.net-guide-inline {
  box-sizing: border-box;
  width: 100%;
  margin-bottom: 18rpx;
  padding: 6rpx 20rpx;
  border-radius: 12rpx;
  background: #fff;
  border: 2rpx solid rgba(64, 158, 255, 0.25);
}
.net-actions { margin-bottom: 4rpx; }
.net-btn {
  height: 64rpx;
  line-height: 64rpx;
  font-size: 26rpx;
  background: #409EFF;
  color: #fff;
  border-radius: 12rpx;
  padding: 0 24rpx;
  margin: 0;
}
.net-btn::after { border: none; }
.net-join-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-top: 16rpx;
}
.net-input {
  flex: 1;
  min-width: 0;
  height: 64rpx;
  border: 2rpx solid #dcdfe6;
  border-radius: 12rpx;
  padding: 0 16rpx;
  font-size: 26rpx;
  box-sizing: border-box;
  background: #fff;
  color: #303133;
}
.net-join-btn { flex-shrink: 0; }
.net-status {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}
.net-status-text {
  font-size: 26rpx;
  color: #67C23A;
  flex: 1;
  min-width: 0;
}
.net-disconnect {
  flex-shrink: 0;
  font-size: 24rpx;
  color: #F56C6C;
  background: transparent;
  border: none;
  padding: 0 12rpx;
  line-height: 1.8;
  margin: 0;
}
.net-disconnect::after { border: none; }
.net-wait-start {
  text-align: center;
  color: #909399;
  font-size: 26rpx;
  padding: 16rpx 0;
}
/* 大厅模式新增 */
.net-field-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-bottom: 16rpx;
}
.net-field-label {
  flex-shrink: 0;
  width: 88rpx;
  font-size: 26rpx;
  color: #606266;
}
.net-connect-btn {
  width: 100%;
  margin-top: 4rpx;
}
.net-line-block {
  padding: 16rpx;
  border-radius: 12rpx;
  background: rgba(255, 255, 255, 0.6);
  margin-bottom: 16rpx;
}
.net-line-title {
  font-size: 26rpx;
  font-weight: 600;
  color: #303133;
  margin-bottom: 6rpx;
}
.net-line-desc {
  font-size: 22rpx;
  color: #909399;
  line-height: 1.5;
  margin-bottom: 12rpx;
}
.net-line-tip {
  display: block;
  font-size: 22rpx;
  color: #E6A23C;
  line-height: 1.5;
  margin-bottom: 12rpx;
}
.net-lobby { margin-top: 4rpx; }
.net-lobby-title {
  font-size: 26rpx;
  font-weight: 600;
  color: #606266;
  margin: 16rpx 0 8rpx;
}
.net-empty {
  font-size: 24rpx;
  color: #c0c4cc;
  text-align: center;
  padding: 20rpx 0;
}
.net-player-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14rpx 12rpx;
  border-radius: 10rpx;
  background: rgba(255, 255, 255, 0.6);
  margin-bottom: 10rpx;
}
.net-player-name {
  font-size: 26rpx;
  color: #303133;
  flex: 1;
  min-width: 0;
}
.net-player-busy {
  font-size: 22rpx;
  color: #c0c4cc;
}
.net-invite-btn {
  flex-shrink: 0;
  height: 52rpx;
  line-height: 52rpx;
  font-size: 24rpx;
  background: #409EFF;
  color: #fff;
  border-radius: 10rpx;
  padding: 0 20rpx;
  margin: 0;
}
.net-invite-btn::after { border: none; }
.net-invite-btn[disabled] {
  background: #c0c4cc;
  color: #fff;
}
.net-room { margin-top: 4rpx; }
.net-room-actions {
  display: flex;
  gap: 16rpx;
  margin-top: 16rpx;
}
.net-room-actions .net-btn { flex: 1; }
.net-kick-btn {
  background: #F56C6C;
}


.guide-sec {
  padding: 18rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.guide-sec:last-child { border-bottom: none; }
.guide-lead { padding-top: 22rpx; }
.guide-h {
  display: block;
  font-size: 27rpx;
  font-weight: bold;
  color: #1F2D3D;
  margin-bottom: 10rpx;
}
.guide-p {
  display: block;
  word-break: break-word;
  font-size: 23rpx;
  line-height: 1.75;
  color: #5A6572;
  margin-bottom: 4rpx;
}
.guide-tip {
  display: block;
  margin-top: 10rpx;
  padding: 12rpx 14rpx;
  border-radius: 10rpx;
  background: rgba(230, 162, 60, 0.12);
  font-size: 22rpx;
  line-height: 1.6;
  color: #B8860B;
}
</style>
