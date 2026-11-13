<template>











	<view :class="['container', 'theme-' + themeMode]">







		<!-- 数据源设置 -->







		<view class="section" id="section-data">







			<view class="section-header">







				<text class="section-title">数据源设置</text>
				<text class="ds-order-tip" v-if="offlineSourceActive">当前使用本地/自定义数据，远程数据源已停用下载排序</text>







			</view>







			<view class="section-content">







				<view class="data-source-options">







					<view 
					class="data-source-item"
					v-for="(option, index) in dataSourceOptions"
					:key="option.value"
					:style="itemStyle(index, option.value)"
					:class="{
						'disabled': option.disabled || (option.value === 'github' && isDownloading),
						'loading': option.value === 'github' && isDownloading
					}"
					>
					<view class="drag-handle"
						@touchstart.stop="onDataSourceDragStart($event, index)"
						@touchmove.stop.prevent="onDataSourceDragMove($event)"
						@touchend.stop="onDataSourceDragEnd"
						@touchcancel.stop="onDataSourceDragEnd"
						:class="{ dragging: dragIndex === index }">
						<text class="drag-icon">≡</text>
					</view>
					<view class="data-source-info">
						<text class="data-source-label">
							{{ option.label }}
							<text v-if="option.value === 'github' && isDownloading" class="loading-text"> (下载中...)</text>
						</text>
						<text class="data-source-desc">{{ option.desc }}</text>
						<text class="data-source-warning" v-if="option.warning">{{ option.warning }}</text>
					</view>					<view class="loading-spinner" v-if="option.value === 'github' && isDownloading"></view>
				</view>







				</view>







				







				<!-- 自定义本地数据管理 -->







				<view class="custom-data-management">







					<view class="custom-data-actions">







						<button class="upload-btn" @click="uploadCustomData" :disabled="isUploading">







							<text v-if="isUploading">上传中...</text>







							<text v-else>上传自定义JSON数据</text>







						</button>







						<button class="export-btn" @click="exportToLocal" :disabled="!hasDomesticData">







							导出并分享数据







						</button>







					</view>







					







					<view class="custom-data-info" v-if="customDataInfo">







						<text class="info-title">自定义数据信息</text>







						<view class="info-item">







							<text class="info-label">数据来源:</text>







							<text class="info-value">{{ customDataInfo.source }}</text>







						</view>







						<view class="info-item">







							<text class="info-label">上传时间:</text>







							<text class="info-value">{{ customDataInfo.uploadTime }}</text>







						</view>







						<view class="info-item">







							<text class="info-label">数据大小:</text>







							<text class="info-value">{{ customDataInfo.size }}</text>







						</view>







						<view class="info-item">







							<text class="info-label">盒号数量:</text>







							<text class="info-value">{{ customDataInfo.boxCount }} 个</text>







						</view>







						<view class="info-item" v-if="customDataInfo.validation">







							<text class="info-label">数据验证:</text>







							<text class="info-value" :class="customDataInfo.validation.valid ? 'valid' : 'invalid'">







								{{ customDataInfo.validation.message }}







							</text>







						</view>







					</view>







					







					<view class="custom-data-warning" v-if="dataSource === 'custom'">







						<text class="warning-title">⚠️ 使用自定义数据请注意</text>







						<text class="warning-text">1. 自定义数据需符合特定格式要求才能正常使用</text>







						<text class="warning-text">2. 上传后系统会自动验证数据格式</text>







						<text class="warning-text">3. 格式错误可能导致功能无法正常使用</text>







						<text class="warning-text">4. 建议使用"导出国内源数据到本地"功能获取标准数据</text>
						<text class="warning-text">5. 使用第三方/自定义数据源可能带来数据不准确、失效等风险，请谨慎使用</text>
						<text class="warning-text">6. 若您不清楚用途，建议使用默认数据源（知晓云源）</text>







					</view>







				</view>







				







				<view class="data-source-tips">







					<text class="tip-text">*推荐使用国内源，速度最快最稳定</text>







					<text class="tip-text">*GitHub源无法展示分盒信息、市价信息、角色头像及热度标记等特殊功能</text>







					<text class="tip-text" v-if="dataSource === 'local'">当前使用本地数据，版本：{{ currentVersion || '未知' }}</text>







					<text class="tip-text" v-if="dataSource === 'custom'">当前使用自定义数据</text>







					<text class="tip-text" v-if="dataSource === 'github'">当前使用GitHub源，仅支持基础数据</text>







					<text class="tip-text" v-if="isDownloading">正在下载GitHub数据，请稍候...</text>







				</view>







			</view>







		</view>







		







		<!-- 数据管理 -->







		<view class="section" id="section-custom">







			<view class="section-header">







				<text class="section-title">数据管理</text>







			</view>







			<view class="section-content">







				<view class="setting-item">







					<view class="setting-info">







						<text class="setting-name">删除本地数据</text>







						<text class="setting-desc">清除已下载的干员数据，释放存储空间</text>







						<text class="data-info" v-if="localDataSize">当前数据大小: {{ localDataSize }}</text>







						<text class="tip-text">*删除数据后，您需要重新下载数据才能使用其他功能</text>







						<text class="tip-text">*删除数据后会自动切换回国内源</text>







					</view>







					<button class="danger-btn" @click="showDeleteConfirm" :disabled="isDownloading">删除数据</button>







				</view>







			</view>







		</view>







		







		<!-- 搜索功能设置 -->







		<view class="section" id="section-search">







			<view class="section-header">







				<text class="section-title">搜索功能设置</text>







			</view>







			<view class="section-content">







				<!-- 三个搜索开关放在一行 -->







				<view class="search-switches-row">







					<view class="search-switch-item">







						<view class="switch-info">







							<text class="switch-name">启用英文名搜索</text>







							<text class="switch-desc-small">Enable English Name search</text>







							<switch 







								:checked="enableEnglishSearch" 







								@change="onEnglishSearchChange"







								class="search-switch" 







								:disabled="dataSource === 'github' || isDownloading"







							/>







							<text class="warning-text-small" v-if="dataSource === 'github'">GitHub源不支持</text>







						</view>







					</view>







					







					<view class="search-switch-item disabled">







						<view class="switch-info">







							<text class="switch-name">启用日文名搜索</text>







							<text class="switch-desc-small">日本語名検索を有効にする</text>







							<text class="dev-tag-small">开发中</text>







							<switch 







								:checked="enableJapaneseSearch" 







								@change="onJapaneseSearchChange"







								class="search-switch" 







								disabled







							/>







						</view>







					</view>







					







					<view class="search-switch-item">







						<view class="switch-info">







							<text class="switch-name">启用外号搜索</text>







							<text class="switch-desc-small">仅支持部分干员</text>







							<switch 







								:checked="enableNicknameSearch" 







								@change="onNicknameSearchChange"







								class="search-switch" 







								:disabled="dataSource === 'github' || isDownloading"







							/>







							<text class="warning-text-small" v-if="dataSource === 'github'">GitHub源不支持</text>







						</view>







					</view>







				</view>







				







				<!-- 搜索数据状态 -->







				<view class="search-data-status" v-if="enableEnglishSearch || enableNicknameSearch">







					<text class="search-status-text" :class="searchDataStatusClass">







						{{ searchDataStatusMessage }}







					</text>







					<button 







						class="retry-btn" 







						@click="downloadSearchData"







						:disabled="dataSource === 'github' || isDownloading"







					>







						{{ searchDataStatusMessage.includes('失败') ? '重试下载' : '更新数据' }}







					</button>







				</view>







				







				<!-- 自定义搜索词管理 -->







				<view class="setting-item">







					<view class="setting-info">







						<text class="setting-name">自定义搜索词</text>







						<text class="setting-desc">添加个人常用的干员别名和搜索词</text>







						<text class="data-info" v-if="customWordsCount > 0">已添加 {{ customWordsCount }} 个自定义搜索词</text>







						<text class="warning-text" v-if="dataSource === 'github'">GitHub源无法使用自定义搜索词</text>







					</view>







					<button class="secondary-btn" @click="manageCustomSearchWords" :disabled="dataSource === 'github' || isDownloading">管理</button>







				</view>







			</view>







		</view>







		







		<!-- 功能设置 -->







		<view class="section" id="section-function">







			<view class="section-header">







				<text class="section-title">功能设置</text>







			</view>







			<view class="section-content">







				<!-- 首页功能展示样式 -->
				<view class="setting-item">
					<view class="setting-info">
						<text class="setting-name">首页功能展示样式</text>
						<text class="setting-desc">选择首页工具箱的呈现方式，三种模式随时可切换</text>
					</view>
				</view>
				<view class="layout-picker">
					<view
						class="layout-option"
						:class="{ active: homeLayout === opt.value }"
						v-for="opt in homeLayoutOptions"
						:key="opt.value"
						@click="selectHomeLayout(opt.value)"
					>
						<view class="layout-preview" :class="'lp-' + opt.value">
							<view class="lp-a"></view>
							<view class="lp-b"></view>
							<view class="lp-c"></view>
							<view class="lp-d"></view>
						</view>
						<text class="layout-name">{{ opt.label }}</text>
						<text class="layout-tip">{{ opt.tip }}</text>
					</view>
				</view>

				<!-- 预测通行证功能 -->







				<view class="setting-item">







					<view class="setting-info">







						<text class="setting-name">启用预测通行证</text>







						<text class="setting-desc">显示官方未公布的新通行证预测信息</text>







						<text class="warning-text">请以官方最终发布为准</text>







						<text class="red-text">*重启程序生效</text>







						<text class="warning-text" v-if="dataSource === 'github'">GitHub源不支持预测数据</text>







					</view>







					<switch 







						:checked="enableGuessData" 







						@change="onGuessDataChange"







						class="setting-switch" 







						:disabled="dataSource === 'github' || isDownloading"







					/>







				</view>







				







				<!-- 预测数据状态 -->







				<view class="guess-data-status" v-if="enableGuessData && dataSource !== 'github'">







					<text class="guess-status-text" :class="guessDataStatusClass">







						{{ guessDataStatusMessage }}







					</text>







					<button 







						class="retry-btn" 







						@click="downloadGuessData"







						:disabled="isDownloading"







					>







						{{ guessDataStatusMessage.includes('失败') ? '重试下载' : '更新数据' }}







					</button>







				</view>







				







				<!-- 数据更新提示（独立于自动更新，开启后首页发现新版本会弹窗+横幅） -->
				<view class="setting-item">
					<view class="setting-info">
						<text class="setting-name">数据更新提示</text>
						<text class="setting-desc">发现新版本数据时弹窗并显示横幅提醒，避免静默错过更新</text>
					</view>
					<switch
						:checked="updateNotifyEnabled"
						@change="onUpdateNotifyChange"
						class="setting-switch"
						color="#409EFF"
					/>
				</view>

				<!-- 干员数据自动更新 -->







				<view class="setting-item">







					<view class="setting-info">







						<text class="setting-name">干员数据自动更新</text>







						<text class="setting-desc">检测到数据更新时自动下载，建议只在Wi-Fi状态下启用</text>







						<text class="red-text">*重启程序生效</text>







						<text class="warning-text" v-if="dataSource === 'github' || dataSource === 'custom'">GitHub源和自定义数据暂不支持自动更新</text>







						<!-- 自动更新状态显示 -->







						<view class="auto-update-status" v-if="enableAutoUpdate">







							<text class="auto-update-status-text" :class="autoUpdateStatusClass">







								{{ autoUpdateStatusMessage }}







							</text>







							<text class="auto-update-time" v-if="lastAutoUpdateTime">







								上次检查: {{ lastAutoUpdateTime }}







							</text>







						</view>







					</view>







					<switch 







						:checked="enableAutoUpdate" 







						@change="onAutoUpdateChange"







						class="setting-switch" 







						:disabled="dataSource === 'github' || dataSource === 'custom' || isDownloading"







					/>







				</view>







				







				<!-- 手动检查更新按钮 -->







				<view class="manual-update-check" v-if="enableAutoUpdate && dataSource !== 'github' && dataSource !== 'custom'">







					<button 







						class="secondary-btn" 







						@click="manualCheckUpdate" 







						:disabled="isDownloading || isUploading"







					>







						立即检查更新







					</button>







				</view>







				







				<!-- 市价展示功能 -->







				<view class="setting-item">







					<view class="setting-info">







						<text class="setting-name">开启市价展示功能</text>







						<text class="setting-desc">在干员列表中显示市场价格信息</text>







						<text class="warning-text" v-if="dataSource === 'github'">GitHub源不支持市价展示</text>







						<text class="warning-text" v-if="dataSource === 'custom' && !customDataInfo?.validation?.valid">自定义数据格式可能不支持市价</text>







					</view>







					<switch 







						:checked="showMarketPrice" 







						@change="onMarketPriceChange"







						class="setting-switch" 







						:disabled="dataSource === 'github' || isDownloading"







					/>







				</view>







			</view>







		</view>















		<!-- 桌面小组件入口（仅 Android App 可见） -->
		<!-- #ifdef APP-PLUS -->
		<view class="setting-item" v-if="widgetEntryVisible" @click="onAddHomeWidget">
			<view class="setting-info">
				<text class="setting-name">添加到桌面</text>
				<text class="setting-desc">在桌面添加「盒号列表」小组件，不用打开 App 就能快速查看盒号并直达详情</text>
				<text class="warning-text" v-if="!widgetPinSupported">当前设备或启动器不支持一键添加，请长按桌面空白处手动添加</text>
			</view>
			<text class="ak-widget-entry-btn">{{ widgetPinSupported ? '添加' : '说明' }}</text>
		</view>
		<view class="setting-item" v-if="widgetEntryVisible" @click="onAddGachaWidget">
			<view class="setting-info">
				<text class="setting-name">添加抽卡小组件到桌面</text>
				<text class="setting-desc">在桌面直接「单抽 / 十连」，卡池取自 App 的盒号数据；纯粹是模拟，不影响 App 里的抽卡进度与盈亏记录</text>
				<text class="warning-text" v-if="!widgetPinSupported">当前设备或启动器不支持一键添加，请长按桌面空白处手动添加「模拟抽卡」</text>
			</view>
			<text class="ak-widget-entry-btn">{{ widgetPinSupported ? '添加' : '说明' }}</text>
		</view>
		<!-- #endif -->

		<!-- #ifdef APP-PLUS -->

		<!-- 小程序无桌面小组件，整块不参与编译 -->

		<!-- 小组件图例：说明干员头像右下角两个徽标的含义 -->
		<!-- 图标必须用深色底托：#1B1D1E 与小组件卡片底色一致，白色空心星在浅色页面上才看得见 -->
		<view class="ak-widget-legend" v-if="widgetEntryVisible">
			<text class="ak-widget-legend-title">小组件图例</text>
			<text class="ak-widget-legend-sub">盒号小组件 · 干员头像右下角</text>
			<view class="ak-widget-legend-row">
				<image class="ak-widget-legend-icon" src="/static/elite0.png" mode="aspectFit" />
				<text class="ak-widget-legend-text">单精一 · 该干员只有精一，没有一对</text>
			</view>
			<view class="ak-widget-legend-row">
				<image class="ak-widget-legend-icon" src="/static/elite2_golden.png" mode="aspectFit" />
				<text class="ak-widget-legend-text">有一对 · 精一 + 精二 都齐了</text>
			</view>

			<text class="ak-widget-legend-sub">抽卡小组件 · 抽到结果的头像外圈</text>
			<view class="ak-widget-legend-row">
				<view class="ak-widget-legend-ring ak-widget-legend-ring1"></view>
				<text class="ak-widget-legend-text">淡白圈 · 抽到的是精一</text>
			</view>
			<view class="ak-widget-legend-row">
				<view class="ak-widget-legend-ring ak-widget-legend-ring2"></view>
				<text class="ak-widget-legend-text">金圈 · 抽到的是精二</text>
			</view>

			<text class="ak-widget-legend-note">盒号卡片高度只够一行时会出现「第1/2组」按钮，点一下可翻看另外 4 位干员。</text>
		</view>

		<!-- #endif -->


		<!-- 干员头像个性化：默认档位 / 条件规则 / 名单 / 导入导出 -->
		<view class="section" id="section-avatar-pref">
			<view class="section-header">
				<text class="section-title">干员头像个性化</text>
			</view>
			<view class="section-content">
				<view class="setting-item">
					<view class="setting-info">
						<text class="setting-name">默认头像优先级</text>
						<text class="setting-desc">列表、查询、盒号详情等页面默认展示哪一档头像；没有精二头像的干员始终显示精一</text>
					</view>
				</view>
				<view class="avp-seg">
					<view class="avp-seg-item" :class="{ active: avatarPref.def === 0 }" @click="setAvatarDefault(0)">优先精一</view>
					<view class="avp-seg-item" :class="{ active: avatarPref.def === 2 }" @click="setAvatarDefault(2)">优先精二</view>
				</view>

				<view class="setting-item avp-head">
					<view class="setting-info">
						<text class="setting-name">条件规则</text>
						<text class="setting-desc">按干员属性或盒属性决定档位，自上而下第一条命中的规则生效</text>
					</view>
					<text class="avp-add-btn" @click="openRuleModal(null)">＋ 新增</text>
				</view>
				<view class="avp-empty" v-if="!avatarPref.rules.length">暂无规则，全部干员使用上方默认优先级</view>
				<view class="avp-rule" v-for="(rule, ruleIdx) in avatarPref.rules" :key="rule.id">
					<view class="avp-rule-top">
						<text class="avp-rule-idx">{{ ruleIdx + 1 }}</text>
						<text class="avp-rule-level" :class="rule.level === 2 ? 'lv2' : 'lv1'">{{ rule.level === 2 ? '精二' : '精一' }}</text>
						<text class="avp-rule-summary">{{ avatarCondSummary(rule.cond) }}</text>
					</view>
					<view class="avp-rule-ops">
						<text class="avp-op" @click="openRuleModal(rule)">编辑</text>
						<text class="avp-op" v-if="ruleIdx > 0" @click="moveRule(ruleIdx, -1)">上移</text>
						<text class="avp-op" v-if="ruleIdx < avatarPref.rules.length - 1" @click="moveRule(ruleIdx, 1)">下移</text>
						<text class="avp-op danger" @click="removeRule(ruleIdx)">删除</text>
					</view>
				</view>

				<view class="setting-item avp-head">
					<view class="setting-info">
						<text class="setting-name">精二名单</text>
						<text class="setting-desc">名单内的干员无视上面的规则，始终优先展示精二头像</text>
					</view>
					<text class="avp-add-btn" @click="openPicker('white')">＋ 添加</text>
				</view>
				<view class="avp-name-list" v-if="avatarPref.white.length">
					<view class="avp-name-chip" v-for="nm in avatarPref.white" :key="'w' + nm" @click="removeName('white', nm)">
						<text class="avp-name-text">{{ nm }}</text>
						<text class="avp-name-x">✕</text>
					</view>
				</view>
				<view class="avp-empty" v-else>暂无干员</view>

				<view class="setting-item avp-head">
					<view class="setting-info">
						<text class="setting-name">精一名单</text>
						<text class="setting-desc">名单内的干员无视上面的规则，始终展示精一头像</text>
					</view>
					<text class="avp-add-btn" @click="openPicker('black')">＋ 添加</text>
				</view>
				<view class="avp-name-list" v-if="avatarPref.black.length">
					<view class="avp-name-chip" v-for="nm in avatarPref.black" :key="'b' + nm" @click="removeName('black', nm)">
						<text class="avp-name-text">{{ nm }}</text>
						<text class="avp-name-x">✕</text>
					</view>
				</view>
				<view class="avp-empty" v-else>暂无干员</view>

				<view class="setting-item avp-head">
					<view class="setting-info">
						<text class="setting-name">导入 / 导出</text>
						<text class="setting-desc">导出为 JSON 文本，换设备后粘贴导入；导入会覆盖当前这份设置</text>
					</view>
				</view>
				<view class="avp-io-row">
					<text class="avp-io-btn" @click="openIo('export')">导出</text>
					<text class="avp-io-btn" @click="openIo('import')">导入</text>
					<text class="avp-io-btn danger" @click="resetAvatarPref">恢复默认</text>
				</view>
			</view>
		</view>

		<!-- 规则编辑弹窗 -->
		<view class="avp-mask" v-if="ruleModalVisible" @tap="closeRuleModal">
			<view class="avp-modal" @tap.stop>
				<view class="avp-modal-head">
					<text class="avp-modal-title">{{ editingRuleId ? '编辑规则' : '新增规则' }}</text>
					<text class="avp-modal-x" @click="closeRuleModal">✕</text>
				</view>
				<scroll-view scroll-y class="avp-modal-body">
					<view class="avp-group" v-for="g in avatarCondGroups" :key="g.key">
						<view class="avp-group-head">
							<text class="avp-group-title">{{ g.label }}</text>
							<text class="avp-group-clear" v-if="ruleDraft[g.key].length" @click="clearRuleGroup(g.key)">清除</text>
						</view>
						<view class="avp-chips">
							<text
								class="avp-chip"
								v-for="opt in ruleOptions(g.key)"
								:key="g.key + opt"
								:class="{ active: ruleDraft[g.key].indexOf(opt) >= 0 }"
								@click="toggleRuleCond(g.key, opt)"
							>{{ avatarOptionLabel(g.key, opt) }}</text>
						</view>
					</view>
					<view class="avp-tip">不同分组之间同时满足，同一分组内多选为任一满足；留空表示不限</view>
				</scroll-view>
				<view class="avp-modal-foot">
					<view class="avp-seg avp-seg-sm">
						<view class="avp-seg-item" :class="{ active: ruleDraftLevel === 0 }" @click="ruleDraftLevel = 0">精一</view>
						<view class="avp-seg-item" :class="{ active: ruleDraftLevel === 2 }" @click="ruleDraftLevel = 2">精二</view>
					</view>
					<view class="avp-foot-btns">
						<text class="avp-btn" @click="closeRuleModal">取消</text>
						<text class="avp-btn primary" @click="confirmRule">保存</text>
					</view>
				</view>
			</view>
		</view>

		<!-- 干员选择弹窗 -->
		<view class="avp-mask" v-if="pickerVisible" @tap="closePicker">
			<view class="avp-modal" @tap.stop>
				<view class="avp-modal-head">
					<text class="avp-modal-title">{{ pickerTarget === 'white' ? '添加到精二名单' : '添加到精一名单' }}</text>
					<text class="avp-modal-x" @click="closePicker">✕</text>
				</view>
				<view class="avp-search-wrap">
					<input class="avp-search" v-model="pickerKeyword" placeholder="搜索干员名" confirm-type="search" />
				</view>
				<scroll-view scroll-y class="avp-picker-list">
					<view class="avp-picker-item" v-for="nm in pickerList()" :key="nm" @click="togglePickerValue(nm)">
						<text class="avp-picker-name">{{ nm }}</text>
						<text class="avp-picker-check" :class="{ on: pickerSelected.indexOf(nm) >= 0 }">{{ pickerSelected.indexOf(nm) >= 0 ? '✓' : '' }}</text>
					</view>
					<view class="avp-empty" v-if="!pickerList().length">没有匹配的干员</view>
				</scroll-view>
				<view class="avp-modal-foot">
					<text class="avp-picked-count">已选 {{ pickerSelected.length }}</text>
					<view class="avp-foot-btns">
						<text class="avp-btn" @click="closePicker">取消</text>
						<text class="avp-btn primary" @click="confirmPicker">确定</text>
					</view>
				</view>
			</view>
		</view>

		<!-- 导入 / 导出弹窗 -->
		<view class="avp-mask" v-if="ioVisible" @tap="closeIo">
			<view class="avp-modal" @tap.stop>
				<view class="avp-modal-head">
					<text class="avp-modal-title">{{ ioTitle() }}</text>
					<text class="avp-modal-x" @click="closeIo">✕</text>
				</view>
				<view class="avp-io-body">
					<textarea
						class="avp-textarea"
						v-model="ioText"
						:disabled="!ioIsImport()"
						:maxlength="-1"
						placeholder="粘贴之前导出的 JSON 文本"
					/>
					<text class="avp-tip">{{ ioHint() }}</text>
				</view>
				<view class="avp-modal-foot">
					<view class="avp-foot-btns">
						<text class="avp-btn" @click="closeIo">取消</text>
						<text class="avp-btn" v-if="!ioIsImport()" @click="copyIo">复制</text>
						<text class="avp-btn primary" v-else @click="confirmImport">导入</text>
					</view>
				</view>
			</view>
		</view>

<!-- 个性化设置备份（整机迁移 / 分享） -->
		<view class="section" id="section-pref-backup">
			<view class="section-header">
				<text class="section-title">个性化设置备份</text>
			</view>
			<view class="section-content">
				<view class="setting-item">
					<view class="setting-info">
						<text class="setting-name">导出 / 导入全部个性化设置</text>
						<text class="setting-desc">主题、首页布局、搜索开关、头像优先级与名单、收藏等一次打包；换设备或分享给朋友时用它快速部署</text>
					</view>
				</view>
				<view class="avp-io-row">
					<text class="avp-io-btn" @click="openBackupIo('backup')">导出全部</text>
					<text class="avp-io-btn" @click="openBackupIo('restore')">导入全部</text>
				</view>
				<view class="setting-item" @click="openProfileShare">
					<view class="setting-info">
						<text class="setting-name">口令 / 短码分享</text>
						<text class="setting-desc">压成一段口令或 6 位短码，别人粘进去就能还原你的偏好；短码 30 天后自动失效</text>
					</view>
					<text class="avp-io-btn pref-share-entry">打开</text>
				</view>
			</view>
		</view>

		<!-- 界面主题设置 -->







		<view class="section" id="section-theme">







			<view class="section-header">







				<text class="section-title">界面主题</text><text class="danger-btn">测试中功能，谨慎使用</text>				







			</view>







			<view class="section-content">







				<view class="theme-options">







					<view 







						class="theme-item" 







						v-for="option in themeOptions" 







						:key="option.value"







						:class="{ 'active': themeMode === option.value }"







						@click="selectTheme(option.value)"







					>







						<view class="theme-preview" :class="'theme-' + option.value">







							<view class="preview-box"></view>







							<view class="preview-text"></view>







						</view>







						<view class="theme-info">







							<text class="theme-label">{{ option.label }}</text>







							<text class="theme-desc">{{ option.desc }}</text>







						</view>







						<view class="theme-radio">







							<view class="radio-inner" v-if="themeMode === option.value"></view>







						</view>







					</view>







				</view>







				<text class="theme-tip">*主题设置将在重启应用后生效</text>







			</view>







		</view>







		







		<!-- 数据状态信息 -->







		<view class="data-status-section">







			<text class="status-title">当前数据状态</text>







			<view class="status-list">







				<view class="status-item">







					<text class="status-label">首选数据源:</text>







					<text class="status-value">{{ getCurrentDataSourceName() }}</text>







				</view>







				<view class="status-item" v-if="localUpdateTime && dataSource !== 'github'">







					<text class="status-label">本地上次更新:</text>







					<text class="status-value">{{ localUpdateTime }}</text>







				</view>







				<view class="status-item" v-if="cloudUpdateTime && dataSource !== 'github'">







					<text class="status-label">云端最新更新:</text>







					<text class="status-value">{{ formatCloudTime(cloudUpdateTime) }}</text>







				</view>







				<view class="status-item" v-if="currentVersion">







					<text class="status-label">数据版本:</text>







					<text class="status-value">{{ currentVersion }}</text>







				</view>







				<view class="status-item" v-if="dataCount > 0 && dataSource !== 'github'">







					<text class="status-label">数据量:</text>







					<text class="status-value">{{ dataCount }} 个盒号</text>







				</view>







				<view class="status-item" v-if="enableGuessData && guessDataCount > 0 && dataSource !== 'github'">







					<text class="status-label">预测数据:</text>







					<text class="status-value">{{ guessDataCount }} 个预测盒号</text>







				</view>







				<view class="status-item" v-if="searchWordCount > 0 && dataSource !== 'github'">







					<text class="status-label">搜索词数据:</text>







					<text class="status-value">{{ searchWordCount }} 个干员</text>







				</view>







				<view class="status-item" v-if="dataSource === 'github' && githubCharactersCount > 0">







					<text class="status-label">GitHub角色数据:</text>







					<text class="status-value">{{ githubCharactersCount }} 个干员</text>







				</view>







				<view class="status-item" v-if="customDataInfo && dataSource === 'custom'">







					<text class="status-label">自定义数据:</text>







					<text class="status-value">{{ customDataInfo.boxCount }} 个盒号</text>







				</view>







				<view class="status-item" v-if="enableAutoUpdate && dataSource !== 'github' && dataSource !== 'custom'">







					<text class="status-label">自动更新:</text>







					<text class="status-value">已启用</text>







				</view>







				<view class="status-item" v-if="isDownloading">







					<text class="status-label">下载状态:</text>







					<text class="status-value loading-status">正在下载GitHub数据...</text>







				</view>







			</view>







		</view>







		









		<!-- 通知与提醒 -->
		<view class="section" id="section-notify">
			<view class="section-header">
				<text class="section-title">通知与提醒</text>
			</view>
			<view class="section-content">
				<view class="setting-item">
					<view class="setting-info">
						<text class="setting-name">启用通知</text>
						<text class="setting-desc">{{ notifyCapsDesc }}</text>
					</view>
					<switch
						:checked="notifyCfg.enabled"
						@change="onNotifySwitch('enabled', $event)"
						class="setting-switch"
						color="#f6540e"
					/>
				</view>

				<view class="setting-item" v-if="notifyCfg.enabled && notifyCaps.system">
					<view class="setting-info">
						<text class="setting-name">版本更新提醒</text>
						<text class="setting-desc">检测到新版安装包时弹一条系统通知</text>
					</view>
					<switch
						:checked="notifyCfg.appUpdate"
						@change="onNotifySwitch('appUpdate', $event)"
						class="setting-switch"
						color="#f6540e"
					/>
				</view>

				<view class="setting-item" v-if="notifyCfg.enabled && notifyCaps.system">
					<view class="setting-info">
						<text class="setting-name">数据更新提醒</text>
						<text class="setting-desc">盒号数据或公告有新内容时提醒</text>
					</view>
					<switch
						:checked="notifyCfg.dataUpdate"
						@change="onNotifySwitch('dataUpdate', $event)"
						class="setting-switch"
						color="#f6540e"
					/>
				</view>

				<view class="setting-item" v-if="notifyCfg.enabled && notifyCaps.system" @click="testNotify">
					<view class="setting-info">
						<text class="setting-name">发一条测试通知</text>
						<text class="setting-desc">{{ notifyTestDesc }}</text>
					</view>
					<text class="setting-action">{{ notifyTesting ? '发送中…' : '测试' }}</text>
				</view>

				<view
					class="setting-item"
					v-if="notifyCfg.enabled && notifyCaps.needPermission && !notifyHasPerm"
					@click="openNotifyPermission"
				>
					<view class="setting-info">
						<text class="setting-name">通知权限未开启</text>
						<text class="setting-desc">系统里把本应用的通知关掉了，点这里去打开</text>
					</view>
					<text class="setting-action">去开启</text>
				</view>

				<!--
					PushPlus 输入框不跟 notifyCaps.pushplus 绑定：那个能力位一旦判错（平台识别失败、
					自定义基座等），用户就彻底找不到填 token 的地方 —— v1.50.54 就是这个毛病。
					现在只要「通知总开关」开着就显示，只有小程序端隐藏（请求域名不在白名单里，填了也发不出去）。
				-->
				<view class="setting-item" v-if="notifyCfg.enabled && notifyCaps.kind !== 'mp'">
					<view class="setting-info">
						<text class="setting-name">PushPlus 第三方推送</text>
						<text class="setting-desc">在 pushplus.plus 微信登录后复制 token 粘到下面，系统通知被限制时也能收到</text>
					</view>
				</view>
				<view class="notify-token-row" v-if="notifyCfg.enabled && notifyCaps.kind !== 'mp'">
					<input
						class="notify-token-input"
						v-model="notifyCfg.pushToken"
						placeholder="粘贴 PushPlus token"
						placeholder-class="notify-token-ph"
						:adjust-position="true"
						confirm-type="done"
						@blur="savePushToken"
						@confirm="savePushToken"
					/>
					<text class="notify-token-btn" @click="testPushPlus">{{ pushTesting ? '发送中…' : '测试推送' }}</text>
				</view>
				<view class="notify-token-hint" v-if="notifyCfg.enabled && notifyCaps.kind !== 'mp'">
					<text class="notify-token-hint-txt">{{ notifyCfg.pushToken ? '已保存，换 token 后失焦即自动保存' : '还没有填 token，这条通道现在是关闭状态' }}</text>
				</view>

				<view class="setting-item" v-if="notifyCfg.enabled && notifyCaps.subscribe" @click="onMpSubscribe">
					<view class="setting-info">
						<text class="setting-name">订阅更新提醒</text>
						<text class="setting-desc">{{ mpSubReady ? '授权后我们能给你发一次更新提醒（一次性订阅）' : '小程序端需要先在公众平台配置订阅消息模板' }}</text>
					</view>
					<text class="setting-action">{{ notifyCfg.mpSubscribed ? '已订阅' : '订阅' }}</text>
				</view>
			</view>
		</view>

		<!-- 塞壬唱片：首页底部迷你播放条开关（默认关） -->
		<!-- #ifndef MP-WEIXIN -->
		<view class="section" id="section-music">
			<view class="section-header">
				<text class="section-title">塞壬唱片</text>
			</view>
			<view class="section-content">
				<view class="setting-item">
					<view class="setting-info">
						<text class="setting-name">首页音乐条</text>
						<text class="setting-desc">在首页底部显示迷你播放器，放塞壬唱片纯音乐；默认关闭，音频在线播放、不占安装包体积</text>
					</view>
					<switch :checked="musicBarOn" @change="onMusicBarChange" color="#409EFF" />
				</view>
			</view>
		</view>
		<!-- #endif -->

		<!-- #ifdef MP-WEIXIN -->

		<!-- 小程序版本更新：微信不会主动推送新版本，这里给一个手动入口 -->

		<view class="section" id="section-app-update">

			<view class="section-header">

				<text class="section-title">版本更新</text>

			</view>

			<view class="section-content">

				<view class="setting-item">

					<view class="setting-info">

						<text class="setting-name">当前版本</text>

						<text class="setting-desc">与安卓、Windows 安装包使用同一个版本号</text>

					</view>

					<text class="setting-action">{{ mpVersion || '—' }}</text>

				</view>

				<view class="setting-item" @click="checkAppUpdate">

					<view class="setting-info">

						<text class="setting-name">检测新版本</text>

						<text class="setting-desc">检查小程序是否有新版本，有新版本时会提示重启</text>

					</view>

					<text class="setting-action">{{ appUpdateChecking ? '检测中…' : '检测' }}</text>

				</view>

			</view>

		</view>

		<!-- #endif -->

		<!-- #ifdef APP-PLUS -->
		<!-- App 版本更新：检测 → 多线路下载（带测速换线）→ 调起系统安装器 -->
		<view class="section" id="section-app-update-app">
			<view class="section-header">
				<text class="section-title">版本更新</text>
			</view>
			<view class="section-content">
				<view class="setting-item" @click="onAppUpdateTap">
					<view class="setting-info">
						<text class="setting-name">{{ appUpdateTitle }}</text>
						<text class="setting-desc">{{ appUpdateDesc }}</text>
					</view>
					<text class="setting-action">{{ appUpdateActionText }}</text>
				</view>

				<view class="app-upd-progress" v-if="appUpdateDownloading">
					<view class="app-upd-track">
						<view class="app-upd-fill" :style="{ width: appUpdatePercent + '%' }"></view>
					</view>
					<view class="app-upd-meta">
						<text>{{ appUpdatePercent.toFixed(0) }}%</text>
						<text>{{ appUpdateSpeedText }}</text>
					</view>
					<view class="app-upd-row">
						<text class="app-upd-src">{{ appUpdateSource }}</text>
						<view class="app-upd-links">
							<text class="app-upd-link" @click.stop="switchAppUpdateSource">换线路</text>
							<text class="app-upd-link" @click.stop="cancelAppUpdate">取消</text>
						</view>
					</view>
				</view>

				<view class="app-upd-error" v-if="appUpdateError && !appUpdateDownloading">
					<text class="app-upd-error-text">{{ appUpdateError }}</text>
					<text class="app-upd-link" @click.stop="openAppReleasePage">网页下载</text>
				</view>
			</view>
		</view>
		<!-- #endif -->

		<!-- 报错日志功能区（与上方设置项同风格） -->
		<view class="section" id="section-error-log">
			<view class="section-header">
				<text class="section-title">报错日志</text>
			</view>
			<view class="section-content">
				<view class="setting-item" @click="viewErrorLogs">
					<view class="setting-info">
						<text class="setting-name">查看报错日志</text>
						<text class="setting-desc">在没有错误日志的情况下诊断任何问题无异于闭眼开车</text>
					</view>
					<text class="setting-action">查看</text>
				</view>
				<view class="setting-item" @click="clearErrorLogs">
					<view class="setting-info">
						<text class="setting-name">清空报错日志</text>
						<text class="setting-desc">删除本地全部日志，此操作不可恢复</text>
					</view>
					<text class="setting-action danger">清空</text>
				</view>
			</view>
		</view>


		<!-- 数据验证结果弹窗 -->







		<view class="validation-modal" v-if="showValidationModal">







			<view class="validation-content">







				<text class="validation-title">数据验证结果</text>







				







				<view class="validation-result" :class="validationResult.valid ? 'valid' : 'invalid'">







					<text class="result-text">{{ validationResult.message }}</text>







					<view class="result-details" v-if="validationResult.details">







						<text class="detail-item" v-for="(detail, index) in validationResult.details" :key="index">







							{{ detail }}







						</text>







					</view>







				</view>







				







				<view class="validation-actions">







					<button class="cancel-btn" @click="closeValidationModal" v-if="!validationResult.valid">取消</button>







					<button class="confirm-btn" @click="confirmUseCustomData" :disabled="!validationResult.valid">







						{{ validationResult.valid ? '使用此数据' : '数据格式错误' }}







					</button>







				</view>







			</view>







		</view>







		







		<!-- 上传进度提示 -->







		<view class="upload-progress" v-if="isUploading">







			<view class="progress-content">







				<text class="progress-title">正在上传数据</text>







				<text class="progress-tip">正在验证数据格式，请稍候...</text>







				<view class="progress-bar">







					<view class="progress-inner" :style="{ width: uploadProgress + '%' }"></view>







				</view>







				<text class="progress-text">{{ uploadProgress }}%</text>







			</view>







		</view>







		







		<!-- 下载进度提示 -->







		<view class="download-progress" v-if="isDownloading">







			<view class="progress-content">







				<text class="progress-title">正在下载GitHub数据</text>







				<text class="progress-tip">数据量较大，可能需要一些时间，请耐心等待...</text>







				<view class="progress-bar">







					<view class="progress-inner" :style="{ width: downloadProgress + '%' }"></view>







				</view>







				<text class="progress-text">已下载: {{ downloadProgress }}%</text>







				<button class="cancel-btn" @click="cancelDownload">取消下载</button>







			</view>







		</view>







		







		<!-- 自动更新设置弹窗 -->







		<view class="auto-update-modal" v-if="showAutoUpdateModal">







			<view class="modal-content">







				<text class="modal-title">自动更新设置</text>







				







				<view class="modal-settings">







					<view class="setting-item">







						<text class="setting-label">仅Wi-Fi下自动更新</text>







						<switch 







							:checked="autoUpdateConfig.wifiOnly" 







							@change="onWifiOnlyChange"







							class="setting-switch"







						/>







					</view>







					







					<view class="setting-item">







						<text class="setting-label">检查频率</text>







						<picker 







							:range="updateFrequencies" 







							:value="updateFrequencyIndex" 







							@change="onFrequencyChange"







							class="frequency-picker"







						>







							<view class="picker-view">







								{{ updateFrequencies[updateFrequencyIndex] }}







								<text class="picker-arrow">▼</text>







							</view>







						</picker>







					</view>







					







					<view class="setting-item">







						<text class="setting-label">自动下载更新</text>







						<switch 







							:checked="autoUpdateConfig.autoDownload" 







							@change="onAutoDownloadChange"







							class="setting-switch"







						/>







					</view>







				</view>







				







				<view class="modal-tips">







					<text class="tip-text">*自动更新只在应用启动时检查</text>







					<text class="tip-text">*建议保持"仅Wi-Fi下自动更新"开启以节省流量</text>







				</view>







				







				<view class="modal-actions">







					<button class="cancel-btn" @click="closeAutoUpdateModal">取消</button>







					<button class="confirm-btn" @click="saveAutoUpdateConfig">保存设置</button>







				</view>







			</view>







		</view>







	</view>











<!-- 报错日志查看弹层 -->
		<view v-if="showErrorLogViewer" class="error-log-mask" @click="closeErrorLogViewer">
			<view class="error-log-modal" @click.stop>
				<view class="error-log-modal-header">
					<text class="error-log-modal-title">报错日志</text>
					<text class="error-log-modal-close" @click="closeErrorLogViewer">✕</text>
				</view>
				<scroll-view scroll-y="true" class="error-log-content">
					<text class="error-log-text" user-select="true">{{ errorLogText }}</text>
				</scroll-view>
				<view class="error-log-modal-actions">
					<view class="error-log-action-row" @click="copyErrorLog">
						<text class="error-log-action-name">一键复制</text>
						<text class="error-log-action-value">复制</text>
					</view>
					<view class="error-log-action-row" @click="clearErrorLogs">
						<text class="error-log-action-name">清空报错日志</text>
						<text class="error-log-action-value danger">清空</text>
					</view>
					<view class="error-log-action-row" @click="closeErrorLogViewer">
						<text class="error-log-action-name">关闭</text>
						<text class="error-log-action-value">关闭</text>
					</view>
				</view>
			</view>
		</view>

</template>















<script>







	import errorLog from "@/utils/errorLog.js";
import { markDailyTask } from "@/utils/dailyTask.js";
	import { fetchSelfHosted } from "@/utils/knowCloudProxy.js";
	import boxWidget from "@/utils/boxWidget.js";
	import { haptic } from "@/utils/haptic.js";
	import { copyText } from "@/utils/openLink.js";
	import { COND_GROUPS, emptyCond, defaultSettings, getSettings, saveSettings, invalidate, resetSettings, optionsOf, optionLabel, condSummary as buildCondSummary, exportSettings, importSettings } from "@/utils/avatarPref.js";
	import { allCharNames } from "@/utils/charMeta.js";
	import { checkUpdate, applyUpdate, onUpdateReady, consumePrompt } from "@/utils/mpUpdate.js";
	import {
		getNotifySettings, setNotifySettings, capabilities as notifyCapabilities,
		sendSystem, sendPushPlus, hasSystemPermission, requestMpSubscribe,
		mpSubscribeReady, openSystemNotifySettings
	} from "@/utils/notify.js";

	// #ifdef APP-PLUS
	import { checkForUpdate, downloadUpdate, installPackage, getCurrentVersion,
		findCachedApk, openReleasePage } from "@/utils/appUpdate.js";
	// #endif

	import { pruneStoredGuessData } from "@/utils/guessData.js";
	// 开源版本：知晓云配置已禁用，使用 GitHub 直链







	const GITHUB_DATA_SOURCES = {







		searchWordUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/searchWord.json',







		guessNewBoxUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/guessNew_Box_Id.json'







	};







	







	// 知晓云配置 - 开源版本clientId为空，商业版请配置知晓云







	const KNOW_CLOUD_CONFIG = {







		clientId: '21680db9b1362913357c',







		baseUrl: 'https://21680db9b1362913357c.myminapp.com/hserve/v2.2',







		tableNames: {







			SearchWord_Version: 'SearchWord_Version',







			Guess_Version: 'Guess_Version'







		}







	};







	







	// 数据源配置







	const DATA_SOURCES = {







		domestic: {







			name: '国内源',







			mainUrl: '', //干员数据URL从知晓云获取







			// guessUrl 不再硬编码，改为从知晓云获取







			searchUrl: '' // 搜索词数据URL将从知晓云获取







		},







		github: {







			name: 'GitHub源',







			characterUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/Box_Id.json',







			warning: '仅支持基础角色数据，可能无法显示分盒信息、市价、头像及热度标记'







		},







		local: {







			name: '本地数据',







			desc: '使用已下载的本地数据'







		},







		custom: {







			name: '自定义数据',







			desc: '使用上传的自定义数据'







		}







	};







	







	// 固定搜索词数据URL（备用）







	const FIXED_SEARCH_URL = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/blobs/11e970795ce8a6e94ac5baa0d5d0458c4103c7e2/searchWord.json';







	







	// 备用预测数据URL（仅当知晓云查询失败时使用）







	const FALLBACK_GUESS_URL = 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/blobs/d9ade5c72ca1f9c45a8405cba0c4957fa9887291/guessNew_Box_Id.json';







	







	export default {







		data() {







			return {

				// ===== 干员头像个性化 =====
				avatarPref: defaultSettings(),
				avatarCondGroups: COND_GROUPS,
				ruleModalVisible: false,
				editingRuleId: '',
				ruleDraft: emptyCond(),
				ruleDraftLevel: 0,
				pickerVisible: false,
				pickerTarget: 'white',
				pickerKeyword: '',
				pickerSelected: [],
				pickerAll: [],
				ioVisible: false,
				ioMode: 'export',
				ioText: '',

				// 参与「全部个性化设置」备份的本地存储键
				prefBackupKeys: [
					'themeMode', 'homeLayout', 'toolsConfig', 'showMarketPrice',
					'enableEnglishSearch', 'enableJapaneseSearch', 'enableNicknameSearch', 'enableGuessData',
					'avatarPrefSettings', 'autoUpdateSetting', 'updateNotifyEnabled',
					'favoriteBoxIds', 'favoriteCharacterNames',
					'dataSource', 'dataSourceOrder', 'dataSourceOrderFull', 'cardShareTopCollapsed'
				],

			// 桌面小组件：本机启动器是否支持「一键添加到桌面」（Android 8.0+）
			widgetPinSupported: true,
			widgetEntryVisible: false,

			appUpdateChecking: false,
			// #ifdef MP-WEIXIN
			mpVersion: '',
			// #endif

			// #ifdef APP-PLUS
			// App 端更新（Android 直接下载安装；小程序走上面的 mpUpdate）
			appCurrentVersion: '',
			appUpdateInfo: null,
			appUpdateDownloading: false,
			appUpdatePercent: 0,
			appUpdateSpeed: 0,
			appUpdateSource: '',
			appUpdateError: '',
			appUpdateReadyPath: '',
			appUpdateHandle: null,
			// #endif






			showErrorLogViewer: false,
			errorLogText: '',
				localUpdateTime: '',







				cloudUpdateTime: '',







				currentVersion: '',















				// 隐私协议（官方API）







				showPrivacyModal: false,







				privacyContractName: '《隐私保护指引》',







				pendingFileAction: false,







				dataCount: 0,







				localDataSize: '',







				enableGuessData: false,







				guessDataStatusMessage: '',







				guessDataCount: 0,







				// 新增搜索功能相关数据







				enableEnglishSearch: false,







				enableJapaneseSearch: false,







				enableNicknameSearch: false,







				searchDataStatusMessage: '',







				searchWordCount: 0,







				customWordsCount: 0,







				searchDataUrl: FIXED_SEARCH_URL, // 初始化使用固定URL







				showMarketPrice: false,







				// 数据源相关数据







				dataSource: 'knowCloud',
			dragIndex: -1,
			dragStartIndex: -1,
			dragCurrentIndex: -1,
			dragYStart: 0,
			dragOffsetY: 0,
			itemHeight: 0,







				dataSourceOptions: [
					{
						value: 'knowCloud', 
						label: '知晓云源', 
						desc: '官方实时数据，支持全部功能',
						warning: ''
					},
					{
						value: 'domestic', 
						label: '国内源', 
						desc: 'GitCode镜像，支持全部功能',
						warning: '试运行阶段，数据可能不稳定'
					},
					{ 
						value: 'github', 
						label: 'GitHub源', 
						desc: '官方游戏数据，仅基础角色信息',
						warning: '可能不支持分盒、市价、头像、热度标记等功能'
					},
				{ value: 'cdn', label: 'CDN源', desc: 'jsDelivr 等 CDN 加速下载', warning: '' },
					{ 
						value: 'local', 
						label: '本地数据', 
						desc: '使用已下载的本地数据，无需网络',
						warning: '需先下载数据'
					},
					{ 
						value: 'custom', 
						label: '自定义数据', 
						desc: '上传自定义JSON数据',
						warning: '第三方数据可能存在不准确或失效风险，请谨慎使用；不清楚用途建议使用默认数据源'
					},
				],







				// GitHub源相关数据







				githubCharactersCount: 0,







				githubDataStatusMessage: '',







				// 下载状态







				isDownloading: false,







				downloadProgress: 0,







				downloadTask: null,







				isDownloadCancelled: false,







				// 自定义数据相关







				customDataInfo: null,







				isUploading: false,







				uploadProgress: 0,







				showValidationModal: false,







				validationResult: {







					valid: false,







					message: '',







					details: []







				},







				tempCustomData: null,







				hasDomesticData: false,







				// 统一的存储变量名







				unifiedData: null, // 统一的数据存储







				unifiedDataVersion: '', // 统一的数据版本







				unifiedUpdateTime: '', // 统一的更新时间







				







				// ===================== 自动更新相关数据 =====================







				updateNotifyEnabled: true,
				enableAutoUpdate: false,







				autoUpdateStatusMessage: '',







				lastAutoUpdateTime: '',







				showAutoUpdateModal: false,







				// 自动更新配置







				autoUpdateConfig: {







					enabled: false,







					lastCheckTime: '',







					wifiOnly: true,







					autoDownload: true,







					checkFrequency: 24, // 小时







					checkInterval: 24 * 60 * 60 * 1000 // 24小时对应的毫秒数







				},







				// 检查频率选项







				updateFrequencies: [







					'每次启动时检查',







					'每12小时检查一次',







					'每24小时检查一次',







					'每48小时检查一次',







					'每周检查一次'







				],







				updateFrequencyIndex: 2, // 默认选择24小时







				updateFrequencyValues: [







					0,  // 0表示每次启动都检查







					12 * 60 * 60 * 1000,  // 12小时







					24 * 60 * 60 * 1000,  // 24小时







					48 * 60 * 60 * 1000,  // 48小时







					7 * 24 * 60 * 60 * 1000  // 7天







				],







				// 主题设置







				themeMode: 'simple',
				notifyCfg: { enabled: true, appUpdate: true, dataUpdate: false, pushToken: '', mpSubscribed: false },
				notifyCaps: { kind: 'unknown', system: false, pushplus: false, subscribe: false, needPermission: false },
				notifyCapsDesc: '',
				notifyTestDesc: '',
				notifyHasPerm: true,
				notifyTesting: false,
				pushTesting: false,
				mpSubReady: false, // 'simple' 简约风格, 'ark' 科技风格
				// 首页底部音乐条是否开启（默认关）
				musicBarOn: (function () {
					try {
						// '1' 是本页写的；'true' 是 Windows 原生设置页写的，两边都要认
						const v = uni.getStorageSync('ak_music_bar_enabled');
						return v === '1' || v === 1 || v === true || v === 'true';
					} catch (e) {
						return false;
					}
				})(),
				// 首页功能展示样式：grid 宫格 / list 列表 / minimal 极简（其余收纳）
				homeLayout: (function () {
					try {
						const v = uni.getStorageSync('homeLayout');
						return ['grid', 'list', 'minimal'].indexOf(v) >= 0 ? v : 'grid';
					} catch (e) {
						return 'grid';
					}
				})(),
				homeLayoutOptions: [
					{ value: 'grid', label: '宫格', tip: '两列卡片' },
					{ value: 'list', label: '列表', tip: '单列横排' },
					{ value: 'minimal', label: '极简', tip: '收纳次要' }
				],







				themeOptions: [







					{ value: 'simple', label: '简约风格', desc: '清新简洁的界面设计' },







					{ value: 'ak', label: '工业风格', desc: 'ak-ui 纸面工业设计语言' },







					{ value: 'ark', label: '深色风格', desc: '工业深色：石墨底 + 信号色' },







					{ value: 'jieyuan', label: '类界园风格', desc: '参考岁的界园志异风格' },
					{ value: 'liquid', label: '液态玻璃', desc: 'iOS 26 风格：纯色底 + 玻璃控件' },
					{ value: 'liquidDark', label: '液态玻璃·深色', desc: '深底玻璃控件，夜间友好' }







				]







			}







		},







		computed: {
			// #ifdef APP-PLUS
			appUpdateTitle() {
				if (this.appUpdateDownloading) {
					return '正在下载 v' + (this.appUpdateInfo ? this.appUpdateInfo.latest : '');
				}
				if (this.appUpdateReadyPath) {
					return '安装 v' + (this.appUpdateInfo ? this.appUpdateInfo.latest : '');
				}
				if (this.appUpdateInfo && this.appUpdateInfo.hasUpdate) {
					return '发现新版本 v' + this.appUpdateInfo.latest;
				}
				return '软件更新（v' + (this.appCurrentVersion || '未知') + '）';
			},
			appUpdateActionText() {
				if (this.appUpdateChecking) return '检测中…';
				if (this.appUpdateDownloading) return '下载中';
				if (this.appUpdateReadyPath) return '安装';
				if (this.appUpdateInfo && this.appUpdateInfo.hasUpdate) return '下载';
				return '检测';
			},
			appUpdateSpeedText() {
				const v = this.appUpdateSpeed || 0;
				if (v >= 1048576) return (v / 1048576).toFixed(1) + ' MB/s';
				if (v > 0) return Math.round(v / 1024) + ' KB/s';
				return '连接中…';
			},
			// #endif







			guessDataStatusClass() {







				if (this.guessDataStatusMessage.includes('成功')) {







					return 'status-success';







				} else if (this.guessDataStatusMessage.includes('失败') || this.guessDataStatusMessage.includes('错误')) {







					return 'status-error';







				} else {







					return 'status-loading';







				}







			},







			searchDataStatusClass() {







				if (this.searchDataStatusMessage.includes('成功')) {







					return 'status-success';







				} else if (this.searchDataStatusMessage.includes('失败') || this.searchDataStatusMessage.includes('错误')) {







					return 'status-error';







				} else {







					return 'status-loading';







				}







			},







			// 自动更新状态样式







			autoUpdateStatusClass() {







				if (this.autoUpdateStatusMessage.includes('已开启') || this.autoUpdateStatusMessage.includes('成功')) {







					return 'status-success';







				} else if (this.autoUpdateStatusMessage.includes('失败') || this.autoUpdateStatusMessage.includes('错误')) {







					return 'status-error';







				} else if (this.autoUpdateStatusMessage.includes('检查中')) {







					return 'status-loading';







				} else {







					return '';







				}







			},







			// 计算启用的搜索选项数量







			enabledSearchCount() {







				let count = 0;







				if (this.enableEnglishSearch) count++;







				if (this.enableJapaneseSearch) count++;







				if (this.enableNicknameSearch) count++;







				return count;







			}







		},








		onShow() {
			// 音乐条可能刚在别的页面被 × 关掉了，回到设置页开关要同步成"关"
			try {
				const v = uni.getStorageSync('ak_music_bar_enabled');
				this.musicBarOn = v === '1' || v === 1 || v === true || v === 'true';
			} catch (e) {}
		},
		onLoad(options) {
			this.initNotify();

			this.loadAvatarPref();

			// #ifdef APP-PLUS
			// 从「发现新版本」弹窗跳过来的强制查一次，别被 24 小时节流挡住
			this.initAppUpdateEntry(
				!!(options && (options.checkUpdate === '1' || options.checkUpdate === 1))
			);
			// #endif

			// #ifdef MP-WEIXIN
			// 线上版返回版本号（如 1.50.47）；开发版/体验版返回 develop/trial
			try {
				const acc = wx.getAccountInfoSync ? wx.getAccountInfoSync() : null;
				const v = (acc && acc.miniProgram && acc.miniProgram.version) || '';
				this.mpVersion = v === 'develop' ? '开发版' : v === 'trial' ? '体验版' : (v || '未知');
			} catch (e) {
				this.mpVersion = '';
			}
			// #endif







			this.loadDataSourceSetting();
			this.initWidgetEntry();







			this.loadDataInfo();







			this.loadMarketPriceSetting(); // 新增：加载市价设置







			this.calculateDataSize();







				try { this.loadGuessDataSetting(); } catch (e) { console.warn('loadGuessDataSetting failed:', e); this.logError(e); }







			this.loadSearchSettings();







			this.checkDomesticData();







			this.loadCustomDataInfo();







			







			// 初始化统一数据存储







			this.unifyDataStorage();















			// 加载自动更新设置







			this.loadAutoUpdateSetting();















			// 加载主题设置







			this.loadThemeSetting();







			







			// func_search 跳转：滚动到指定 tab







			if (options && options.tab) {







				this.scrollToTab(options.tab);







			}







			







			// 设置分享配置







			wx.showShareMenu({







				withShareTicket: true,







				menus: ['shareAppMessage', 'shareTimeline']







			});







		},







		







		onUnload() {







			// 页面卸载时取消下载







			if (this.downloadTask) {







				this.downloadTask.abort();







			}







		},







		







		onShareAppMessage() {







			return {







				title: '方舟通行证谷子查询工具-设置',







				path: '/packageA/Setting/Setting',







				imageUrl: ''







			}







		},







		







		onShareTimeline() {







			return {







				title: '方舟通行证谷子查询工具-设置',







				imageUrl: ''







			}







		},







		







		methods: {
			// #ifdef APP-PLUS
			/** 进设置页时初始化：当前版本 + 24 小时一次的静默检查 */
			initAppUpdateEntry(force) {
				this.appCurrentVersion = getCurrentVersion();
				const KEY = 'appUpdateAutoCheckTs';
				try {
					const last = Number(uni.getStorageSync(KEY) || 0);
					if (!force && last && Date.now() - last < 24 * 3600 * 1000) return;
					uni.setStorageSync(KEY, Date.now());
				} catch (e) {}
				checkForUpdate().then((r) => {
					if (!r || !r.ok) return;
					this.appCurrentVersion = r.current || this.appCurrentVersion;
					if (!r.hasUpdate) return;
					this.appUpdateInfo = r;
					this.appUpdateDesc = '发现新版本 v' + r.latest + '，点此下载';
				});
			},

			onAppUpdateTap() {
				if (this.appUpdateDownloading) return;
				if (this.appUpdateReadyPath) {
					this.installAppUpdate();
					return;
				}
				if (this.appUpdateChecking) return;
				if (this.appUpdateInfo && this.appUpdateInfo.hasUpdate) {
					this.confirmAppUpdate(this.appUpdateInfo);
					return;
				}
				this.doAppUpdateCheck(false);
			},

			doAppUpdateCheck(silent) {
				if (this.appUpdateChecking) return;
				this.appUpdateChecking = true;
				this.appUpdateError = '';
				if (!silent) this.appUpdateDesc = '正在连接 GitHub…';
				checkForUpdate().then((r) => {
					this.appUpdateChecking = false;
					this.appUpdateInfo = null;
					this.appUpdateReadyPath = '';
					this.appCurrentVersion = (r && r.current) || this.appCurrentVersion;

					if (!r || !r.ok) {
						this.appUpdateError = (r && r.error) || '检查失败';
						this.appUpdateDesc = '检查更新失败，可稍后重试';
						if (!silent) {
							uni.showToast({ title: this.appUpdateError, icon: 'none', duration: 2500 });
						}
						return;
					}
					if (!r.hasUpdate) {
						this.appUpdateDesc = '当前已是最新版本 v' + r.current;
						if (!silent) {
							uni.showModal({
								title: '已是最新版本',
								content: '当前版本 v' + r.current + '，无需更新。',
								showCancel: false
							});
						}
						return;
					}
					this.appUpdateInfo = r;
					this.appUpdateDesc = '发现新版本 v' + r.latest + '，点此下载';
					if (!silent) this.confirmAppUpdate(r);
				});
			},

			confirmAppUpdate(r) {
				// 先看本地有没有下好的同版本包（参考 MAA-Meow：按版本缓存 apk）
				findCachedApk(r.latest, r.size).then((path) => {
					if (path) {
						this.appUpdateReadyPath = path;
						this.appUpdateDesc = 'v' + r.latest + ' 已下载，点此安装';
						this.askInstallAppUpdate(r);
						return;
					}
					const lines = String(r.notes || '')
						.replace(/\r/g, '')
						.split('\n')
						.filter((x) => x.trim())
						.slice(0, 6)
						.join('\n');
					uni.showModal({
						title: '发现新版本 v' + r.latest,
						content: lines || ('当前 v' + r.current + ' → 最新 v' + r.latest),
						confirmText: '下载更新',
						cancelText: '稍后',
						success: (res) => {
							if (res.confirm) this.startAppUpdateDownload();
						}
					});
				});
			},

			startAppUpdateDownload() {
				const info = this.appUpdateInfo;
				if (!info || this.appUpdateDownloading) return;
				this.appUpdateDownloading = true;
				this.appUpdateError = '';
				this.appUpdatePercent = 0;
				this.appUpdateSpeed = 0;
				this.appUpdateDesc = '正在下载 v' + info.latest;
				this.appUpdateHandle = downloadUpdate(info, {
					onSource: (label) => {
						this.appUpdateSource = label;
					},
					onProgress: (p) => {
						this.appUpdatePercent = p.percent || 0;
						this.appUpdateSpeed = p.speed || 0;
					},
					onDone: (path, label) => {
						this.appUpdateDownloading = false;
						this.appUpdatePercent = 100;
						this.appUpdateReadyPath = path;
						this.appUpdateSource = label || this.appUpdateSource;
						this.appUpdateDesc = 'v' + info.latest + ' 已下载，点此安装';
						this.appUpdateHandle = null;
						this.askInstallAppUpdate(info);
					},
					onError: (msg) => {
						this.appUpdateDownloading = false;
						this.appUpdateHandle = null;
						this.appUpdateError = msg || '下载失败';
						this.appUpdateDesc = '下载失败，可重试或前往网页下载';
					}
				});
			},

			askInstallAppUpdate(info) {
				uni.showModal({
					title: '安装 v' + info.latest,
					content: '下载完成。系统可能要求「允许安装未知应用」，请在弹窗中允许。',
					confirmText: '立即安装',
					cancelText: '稍后',
					success: (res) => {
						if (res.confirm) this.installAppUpdate();
					}
				});
			},

			installAppUpdate() {
				const path = this.appUpdateReadyPath;
				if (!path) return;
				uni.showLoading({ title: '正在打开安装器…', mask: true });
				installPackage(path)
					.then(() => {
						uni.hideLoading();
					})
					.catch((e) => {
						uni.hideLoading();
						const msg = (e && e.message) || '安装失败';
						uni.showModal({
							title: '无法调起安装器',
							content: msg + '。请在「设置 → 应用 → 特殊权限」允许安装未知应用，或直接去网页下载。',
							confirmText: '网页下载',
							cancelText: '知道了',
							success: (r2) => {
								if (r2.confirm) this.openAppReleasePage();
							}
						});
					});
			},

			switchAppUpdateSource() {
				if (!this.appUpdateHandle) return;
				const ok = this.appUpdateHandle.switchSource();
				uni.showToast({ title: ok ? '已切换线路' : '已是最后一条线路', icon: 'none' });
			},

			cancelAppUpdate() {
				if (this.appUpdateHandle) this.appUpdateHandle.cancel();
				this.appUpdateHandle = null;
				this.appUpdateDownloading = false;
				this.appUpdateDesc = '已取消下载';
			},

			openAppReleasePage() {
				openReleasePage(this.appUpdateInfo && this.appUpdateInfo.pageUrl);
			},
			// #endif

			// ===================== 干员头像个性化 =====================
			loadAvatarPref() {
				try {
					invalidate();
					this.avatarPref = getSettings();
					this.pickerAll = allCharNames();
				} catch (e) {
					this.avatarPref = defaultSettings();
					this.pickerAll = [];
				}
			},

			persistAvatarPref() {
				try {
					this.avatarPref = saveSettings(this.avatarPref);
				} catch (e) {
					console.error('[avatarPref] 保存失败:', e);
				}
			},

			setAvatarDefault(level) {
				const next = JSON.parse(JSON.stringify(this.avatarPref));
				next.def = Number(level) === 2 ? 2 : 0;
				this.avatarPref = next;
				this.persistAvatarPref();
			},

			openRuleModal(rule) {
				if (rule) {
					this.editingRuleId = rule.id;
					this.ruleDraft = JSON.parse(JSON.stringify(rule.cond));
					this.ruleDraftLevel = rule.level;
				} else {
					this.editingRuleId = '';
					this.ruleDraft = emptyCond();
					this.ruleDraftLevel = this.avatarPref.def;
				}
				this.ruleModalVisible = true;
			},

			closeRuleModal() {
				this.ruleModalVisible = false;
			},

			ruleOptions(key) {
				return optionsOf(key, this.ruleDraft.prof);
			},

			avatarOptionLabel(key, value) {
				return optionLabel(key, value);
			},

			avatarCondSummary(cond) {
				return buildCondSummary(cond);
			},

			toggleRuleCond(key, value) {
				if (!this.ruleDraft[key]) this.$set(this.ruleDraft, key, []);
				const arr = this.ruleDraft[key];
				const i = arr.indexOf(value);
				if (i >= 0) arr.splice(i, 1);
				else arr.push(value);
				// 分支随职业联动：职业改动后剔除不再适用的分支
				if (key === 'prof') {
					const valid = this.ruleOptions('sub');
					this.ruleDraft.sub = (this.ruleDraft.sub || []).filter((x) => valid.indexOf(x) >= 0);
				}
			},

			clearRuleGroup(key) {
				this.ruleDraft[key] = [];
			},

			confirmRule() {
				const used = this.avatarCondGroups.some((g) => (this.ruleDraft[g.key] || []).length > 0);
				if (!used) {
					uni.showToast({ title: '至少选择一个条件', icon: 'none' });
					return;
				}
				const next = JSON.parse(JSON.stringify(this.avatarPref));
				const cond = JSON.parse(JSON.stringify(this.ruleDraft));
				if (this.editingRuleId) {
					const i = next.rules.findIndex((r) => r.id === this.editingRuleId);
					if (i >= 0) next.rules[i] = { id: this.editingRuleId, level: this.ruleDraftLevel, cond: cond };
					else next.rules.push({ id: this.editingRuleId, level: this.ruleDraftLevel, cond: cond });
				} else {
					next.rules.push({ id: 'r' + Date.now().toString(36), level: this.ruleDraftLevel, cond: cond });
				}
				this.avatarPref = next;
				this.persistAvatarPref();
				this.ruleModalVisible = false;
				uni.showToast({ title: '规则已保存', icon: 'none' });
			},

			removeRule(idx) {
				const next = JSON.parse(JSON.stringify(this.avatarPref));
				next.rules.splice(idx, 1);
				this.avatarPref = next;
				this.persistAvatarPref();
			},

			moveRule(idx, delta) {
				const to = idx + delta;
				if (to < 0 || to >= this.avatarPref.rules.length) return;
				const next = JSON.parse(JSON.stringify(this.avatarPref));
				const item = next.rules.splice(idx, 1)[0];
				next.rules.splice(to, 0, item);
				this.avatarPref = next;
				this.persistAvatarPref();
			},

			openPicker(target) {
				this.pickerTarget = target === 'black' ? 'black' : 'white';
				this.pickerKeyword = '';
				this.pickerSelected = [];
				this.pickerVisible = true;
			},

			closePicker() {
				this.pickerVisible = false;
			},

			pickerList() {
				const kw = (this.pickerKeyword || '').trim().toLowerCase();
				let list = this.pickerAll || [];
				if (kw) list = list.filter((n) => String(n).toLowerCase().indexOf(kw) >= 0);
				return list.slice(0, 80);
			},

			togglePickerValue(name) {
				const i = this.pickerSelected.indexOf(name);
				if (i >= 0) this.pickerSelected.splice(i, 1);
				else this.pickerSelected.push(name);
			},

			confirmPicker() {
				if (!this.pickerSelected.length) {
					uni.showToast({ title: '请先选择干员', icon: 'none' });
					return;
				}
				const key = this.pickerTarget === 'black' ? 'black' : 'white';
				const other = key === 'white' ? 'black' : 'white';
				const next = JSON.parse(JSON.stringify(this.avatarPref));
				this.pickerSelected.forEach((n) => {
					if (next[key].indexOf(n) < 0) next[key].push(n);
					const j = next[other].indexOf(n);
					if (j >= 0) next[other].splice(j, 1);
				});
				this.avatarPref = next;
				this.persistAvatarPref();
				this.pickerVisible = false;
			},

			removeName(target, name) {
				const key = target === 'black' ? 'black' : 'white';
				const next = JSON.parse(JSON.stringify(this.avatarPref));
				const i = next[key].indexOf(name);
				if (i >= 0) next[key].splice(i, 1);
				this.avatarPref = next;
				this.persistAvatarPref();
			},

			openIo(mode) {
				this.ioMode = mode === 'import' ? 'import' : 'export';
				this.ioText = this.ioMode === 'export' ? exportSettings() : '';
				this.ioVisible = true;
			},

			closeIo() {
				this.ioVisible = false;
			},

			copyIo() {
				copyText(this.ioText, '设置已复制');
			},


			// ---- 全部个性化设置备份 ----
			ioIsImport() {
				return this.ioMode === 'import' || this.ioMode === 'restore';
			},

			ioTitle() {
				if (this.ioMode === 'backup') return '导出全部个性化设置';
				if (this.ioMode === 'restore') return '导入全部个性化设置';
				return this.ioMode === 'export' ? '导出头像设置' : '导入头像设置';
			},

			ioHint() {
				if (this.ioMode === 'backup') return '复制后保存到网盘或发到新设备，导入即可还原全部偏好';
				if (this.ioMode === 'restore') return '导入会覆盖当前已包含的全部个性化项，数据类内容（盒号、搜索词）不在其中';
				return this.ioMode === 'export' ? '复制后保存到备忘录，或直接发到另一台设备' : '导入会覆盖当前全部规则与名单';
			},

			buildBackupPayload() {
				const data = {};
				this.prefBackupKeys.forEach((k) => {
					try {
						const v = uni.getStorageSync(k);
						if (v !== '' && v !== undefined && v !== null) data[k] = v;
					} catch (e) { /* 单项失败不影响整体 */ }
				});
				return {
					app: 'ak-passport',
					kind: 'preferences',
					v: 1,
					exportedAt: new Date().toISOString(),
					data: data
				};
			},

			openProfileShare() {
				uni.navigateTo({ url: '/packageA/profile_share/profile_share' });
			},

			openBackupIo(mode) {
				this.ioMode = mode === 'restore' ? 'restore' : 'backup';
				this.ioText = this.ioMode === 'backup' ? JSON.stringify(this.buildBackupPayload(), null, 2) : '';
				this.ioVisible = true;
			},

			applyBackupText(text) {
				let raw;
				try { raw = JSON.parse(text); } catch (e) { return { ok: false, msg: '不是合法的 JSON' }; }
				if (!raw || raw.kind !== 'preferences' || !raw.data) {
					return { ok: false, msg: '不是本应用的个性化设置备份' };
				}
				if (Number(raw.v) > 1) return { ok: false, msg: '备份版本过新，请升级 App' };
				let n = 0;
				this.prefBackupKeys.forEach((k) => {
					if (raw.data[k] === undefined) return;
					try { uni.setStorageSync(k, raw.data[k]); n++; } catch (e) {}
				});
				// 让本页与服务端缓存状态同步刷新
				try { this.homeLayout = uni.getStorageSync('homeLayout') || this.homeLayout; } catch (e) {}
				try { this.loadThemeSetting(); } catch (e) {}
				try { this.loadDataSourceSetting(); } catch (e) {}
				try { this.loadMarketPriceSetting(); } catch (e) {}
				try { this.loadSearchSettings(); } catch (e) {}
				try { invalidate(); this.loadAvatarPref(); } catch (e) {}
				return { ok: true, msg: '已恢复 ' + n + ' 项个性化设置' };
			},
			confirmImport() {
				if (this.ioMode === 'restore') {
					const br = this.applyBackupText(this.ioText);
					if (!br.ok) { uni.showToast({ title: br.msg, icon: 'none', duration: 2500 }); return; }
					this.ioVisible = false;
					uni.showToast({ title: br.msg, icon: 'none', duration: 2500 });
					return;
				}
				const r = importSettings(this.ioText);
				if (!r.ok) {
					uni.showToast({ title: r.msg, icon: 'none', duration: 2500 });
					return;
				}
				this.avatarPref = saveSettings(r.settings);
				this.ioVisible = false;
				uni.showToast({ title: r.msg, icon: 'none', duration: 2500 });
			},

			resetAvatarPref() {
				uni.showModal({
					title: '恢复默认',
					content: '将清空所有规则与名单，确定继续？',
					success: (res) => {
						if (!res.confirm) return;
						this.avatarPref = resetSettings();
						uni.showToast({ title: '已恢复默认', icon: 'none' });
					}
				});
			},


		// ===== 桌面小组件 =====


		/** 小程序：手动检测新版本（其它平台不会显示入口） */

		checkAppUpdate() {

			if (this.appUpdateChecking) return;

			this.appUpdateChecking = true;



			// 首页已经监听并弹过的，这里不再重复弹窗

			onUpdateReady(() => {});



			checkUpdate(2500).then((hasUpdate) => {

				this.appUpdateChecking = false;

				if (hasUpdate === true) {

					if (!consumePrompt()) {

						uni.showToast({ title: '新版本已就绪，重启小程序即可生效', icon: 'none', duration: 2500 });

						return;

					}

					uni.showModal({

						title: '发现新版本',

						content: '新版本已下载完成，重启后生效。现在就重启吗？',

						confirmText: '立即重启',

						cancelText: '稍后',

						success: (res) => {

							if (res.confirm) applyUpdate();

						}

					});

				} else if (hasUpdate === false) {

					uni.showModal({

						title: '已是最新版本',

						content: '当前使用的就是最新版本。',

						showCancel: false

					});

				} else {

					uni.showToast({ title: '检查失败，请稍后重试', icon: 'none', duration: 2000 });

				}

			});

		},



		/** 进入设置页时探测本机是否支持一键添加 */
		initWidgetEntry() {
			// #ifdef APP-PLUS
			try {
				const info = uni.getSystemInfoSync();
				const isAndroid = String((info && info.platform) || "").toLowerCase() === "android";
				this.widgetEntryVisible = isAndroid;
				this.widgetPinSupported = isAndroid ? boxWidget.isWidgetSupported() : false;
			} catch (e) {
				this.widgetEntryVisible = false;
				this.widgetPinSupported = false;
			}
			// #endif
		},

		/** 请求把「盒号列表」小组件固定到桌面 */
		onAddHomeWidget() {
			// #ifdef APP-PLUS
			let res = { ok: false, reason: "error" };
			try {
				res = boxWidget.requestPinWidget();
			} catch (e) {
				res = { ok: false, reason: "error" };
			}
			if (res.ok) {
				uni.showToast({ title: "已发送请求，请在桌面确认", icon: "none", duration: 2500 });
				return;
			}
			if (res.reason === "unsupported" || res.reason === "rejected") {
				this.widgetPinSupported = false;
				uni.showModal({
					title: "需要手动添加",
					content: "当前系统或桌面启动器不支持一键添加。\n\n请长按桌面空白处 → 选择「小组件」或「小部件」→ 找到「方舟通行证谷子查询工具」→ 把「盒号列表」拖到桌面。",
					showCancel: false,
					confirmText: "知道了"
				});
				return;
			}
			uni.showToast({ title: "当前环境不支持桌面小组件", icon: "none" });
			// #endif
			// #ifndef APP-PLUS
			uni.showToast({ title: "桌面小组件仅支持 Android App", icon: "none" });
			// #endif
		},

		/** 请求把「模拟抽卡」小组件固定到桌面 */
		onAddGachaWidget() {
			// #ifdef APP-PLUS
			let res = { ok: false, reason: "error" };
			try {
				res = boxWidget.requestPinGachaWidget();
			} catch (e) {
				res = { ok: false, reason: "error" };
			}
			if (res.ok) {
				uni.showToast({ title: "已发送请求，请在桌面确认", icon: "none", duration: 2500 });
				return;
			}
			if (res.reason === "unsupported" || res.reason === "rejected") {
				this.widgetPinSupported = false;
				uni.showModal({
					title: "需要手动添加",
					content: "当前系统或桌面启动器不支持一键添加。\n\n请长按桌面空白处 → 选择「小组件」或「小部件」→ 找到「方舟通行证谷子查询工具」→ 把「模拟抽卡」拖到桌面。",
					showCancel: false,
					confirmText: "知道了"
				});
				return;
			}
			uni.showToast({ title: "当前环境不支持桌面小组件", icon: "none" });
			// #endif
			// #ifndef APP-PLUS
			uni.showToast({ title: "桌面小组件仅支持 Android App", icon: "none" });
			// #endif
		},
		logError(e, ctx) {
			try {
				errorLog.logError(e, ctx);
			} catch (logErr) {
				console.warn('[logError] storage failed:', logErr);
			}
		},

		viewErrorLogs() {
			try {
				const logs = errorLog.getErrorLogs() || [];
				if (!logs.length) {
					uni.showModal({
						title: '报错日志',
						content: '当前没有任何错误日志记录。',
						showCancel: false
					});
					return;
				}
				let text = '【报错日志（共 ' + logs.length + ' 条）】\r\n\r\n';
				logs.forEach((item, idx) => {
					text += ('第 ' + (idx + 1) + ' 条\r\n');
					text += ('时间：' + (item.time || '') + '\r\n');
					if (item.context) text += ('上下文：' + item.context + '\r\n');
					if (item.source) text += ('来源：' + item.source + '\r\n');
					if (item.name) text += ('类型：' + item.name + '\r\n');
					text += ('信息：' + (item.message || '') + '\r\n');
					if (item.stack) text += ('堆栈：' + item.stack + '\r\n');
					text += '--------------------------------\r\n';
				});
				this.errorLogText = text;
				this.showErrorLogViewer = true;
			} catch (e) {
				this.logError(e);
				console.warn('[viewErrorLogs] failed:', e);
			}
		},

		copyErrorLog() {
			try {
				if (!this.errorLogText) {
					uni.showToast({ title: '没有可复制的日志', icon: 'none' });
					return;
				}
				uni.setClipboardData({
					data: this.errorLogText,
					success: () => {
						uni.showToast({ title: '已复制到剪贴板', icon: 'success' });
					},
					fail: () => {
						uni.showToast({ title: '复制失败', icon: 'none' });
					}
				});
			} catch (e) {
				this.logError(e);
				console.warn('[copyErrorLog] failed:', e);
			}
		},

		closeErrorLogViewer() {
			this.showErrorLogViewer = false;
		},

		clearErrorLogs() {
			uni.showModal({
				title: '清空报错日志',
				content: '确定要清空所有报错日志吗？此操作不可恢复。',
				success: (res) => {
					if (res.confirm) {
						errorLog.clearErrorLogs();
						this.errorLogText = '';
						uni.showToast({ title: '已清空', icon: 'success' });
					}
				}
			});
		},

			loadGuessDataSetting() {
				try {
					const enableGuessData = uni.getStorageSync('enableGuessData');
					if (enableGuessData === true || enableGuessData === 'true') {
						this.enableGuessData = true;
					} else {
					this.enableGuessData = false;
					}
					const guessData = uni.getStorageSync('guessData');
					this.guessDataCount = (guessData && Array.isArray(guessData)) ? guessData.length : 0;
					const savedStatus = uni.getStorageSync('guessDataStatusMessage');
					if (savedStatus) {
						this.guessDataStatusMessage = savedStatus;
					}
				} catch (e) {
					this.logError(e);
					console.warn('[loadGuessDataSetting] failed:', e);
				}
			},

		// 本地/自定义数据激活时，远程数据源不参与下载排序
		offlineSourceActive() {
			return this.dataSource === 'local' || this.dataSource === 'custom';
		},
		isOfflineSource() {
			try {
				const ds = uni.getStorageSync('dataSource');
				return ds === 'local' || ds === 'custom';
			} catch (e) { return false; }
		},
		persistDataSourceOrder(order) {
			try {
				// 全量用户排序始终保留，便于切回远程源时恢复
				uni.setStorageSync('dataSourceOrderFull', order);
				const ds = uni.getStorageSync('dataSource');
				uni.setStorageSync('dataSourceOrder', (ds === 'local' || ds === 'custom') ? [ds] : order);
			} catch (e) { this.logError(e); }
		},
		syncDataSourceOrderWithPolicy() {
			try {
				const full = uni.getStorageSync('dataSourceOrderFull');
				const order = (Array.isArray(full) && full.length) ? full : this.dataSourceOptions.map(o => o.value);
				this.persistDataSourceOrder(order);
			} catch (e) { this.logError(e); }
		},

		onDataSourceDragStart(e, index) {
			if (this.isOfflineSource()) {
				uni.showToast({ title: '本地/自定义数据下远程源不参与下载排序', icon: 'none' });
				return;
			}
			this.dragIndex = index;
			this.dragStartIndex = index;
			this.dragCurrentIndex = index;
			this.dragYStart = (e.touches && e.touches[0] ? e.touches[0].clientY : 0);
			this.dragOffsetY = 0;
		},
		onDataSourceDragMove(e) {
			if (this.dragStartIndex < 0) return;
			if (!e.touches || !e.touches[0]) return;
			const curY = e.touches[0].clientY;
			this.dragOffsetY = curY - this.dragYStart;
			const step = this.itemHeight || 72;
			const delta = Math.round(this.dragOffsetY / step);
			let target = this.dragStartIndex + delta;
			const len = this.dataSourceOptions.length;
			if (target < 0) target = 0;
			if (target > len - 1) target = len - 1;
			this.dragCurrentIndex = target;
		},
		onDataSourceDragEnd() {
			if (this.dragStartIndex < 0) return;
			const from = this.dragStartIndex;
			const to = this.dragCurrentIndex;
			if (from !== to) {
				const arr = this.dataSourceOptions.slice();
				const moved = arr.splice(from, 1)[0];
				arr.splice(to, 0, moved);
				this.dataSourceOptions = arr;
			}
			const order = this.dataSourceOptions.map(o => o.value);
			this.persistDataSourceOrder(order);
			this.dragIndex = -1;
			this.dragStartIndex = -1;
			this.dragCurrentIndex = -1;
			this.dragYStart = 0;
			this.dragOffsetY = 0;
		},
		itemStyle(index, value) {
			const step = this.itemHeight || 72;
			if (this.dragStartIndex < 0) return {};
			if (index === this.dragStartIndex) {
				return { transform: 'translateY(' + this.dragOffsetY + 'px)' };
			}
			const movedTo = this.dragCurrentIndex;
			const movedFrom = this.dragStartIndex;
			let shift = 0;
			if (movedFrom < movedTo) {
				if (index > movedFrom && index <= movedTo) shift = -1;
			} else if (movedFrom > movedTo) {
				if (index >= movedTo && index < movedFrom) shift = 1;
			}
			if (shift === 0) return {};
			return { transform: 'translateY(' + (shift * step) + 'px)' };
		},

		// ---------------- 通知与提醒 ----------------
		initNotify() {
			try {
				this.notifyCfg = Object.assign({}, getNotifySettings());
				this.notifyCaps = notifyCapabilities();
				this.notifyHasPerm = hasSystemPermission();
				this.mpSubReady = mpSubscribeReady();
				const k = this.notifyCaps.kind;
				const desc = {
					android: '通知栏本地提醒，不依赖厂商推送通道，断网也能弹',
					win: '系统 Toast 通知，右下角弹出',
					mp: '小程序没有本地通知能力，只能走一次性订阅消息',
					web: '浏览器通知需要手动允许，且只在 https 下可用',
					ios: 'iOS 系统通知'
				};
				this.notifyCapsDesc = desc[k] || '当前平台不支持系统通知';
				this.notifyTestDesc = k === 'win' ? '走系统 Toast，右下角弹出' : '走通知栏，用来确认权限有没有被系统拦掉';
			} catch (e) {
				console.warn('[Setting] initNotify failed:', e);
			}
		},
		onNotifySwitch(key, e) {
			const v = e.detail.value;
			this.notifyCfg[key] = v;
			const patch = {};
			patch[key] = v;
			setNotifySettings(patch);
			if (key === 'enabled' && v) {
				// 刚打开就发一条，用户立刻能确认这条通道到底通不通
				setTimeout(() => this.testNotify(), 320);
			}
		},
		savePushToken() {
			setNotifySettings({ pushToken: this.notifyCfg.pushToken });
		},
		openNotifyPermission() {
			const ok = openSystemNotifySettings();
			if (!ok) uni.showToast({ title: '请到系统设置里手动开启', icon: 'none' });
		},
		testNotify() {
			if (this.notifyTesting) return;
			this.notifyTesting = true;
			let r = null;
			try {
				r = sendSystem('通行证工具 · 测试通知', '看到这条，说明系统通知是通的。');
			} catch (e) {
				r = { ok: false, error: (e && e.message) || '发送失败' };
			}
			setTimeout(() => {
				this.notifyTesting = false;
				this.notifyHasPerm = hasSystemPermission();
				if (r && r.ok) {
					uni.showToast({ title: '已发送', icon: 'success' });
				} else if (r && r.needPermission) {
					uni.showModal({
						title: '通知权限没开',
						content: '系统把本应用的通知拦掉了，去开启一下就能收到提醒。',
						confirmText: '去开启',
						success: (res) => {
							if (res.confirm) this.openNotifyPermission();
						}
					});
				} else {
					uni.showToast({ title: (r && r.error) || '当前环境发不出系统通知', icon: 'none' });
				}
			}, 500);
		},
		testPushPlus() {
			if (this.pushTesting) return;
			const token = String(this.notifyCfg.pushToken || '').trim();
			if (!token) {
				uni.showToast({ title: '先填 PushPlus token', icon: 'none' });
				return;
			}
			setNotifySettings({ pushToken: token });
			this.pushTesting = true;
			sendPushPlus('通行证工具 · 测试推送', 'PushPlus 通道正常。', token).then((r) => {
				this.pushTesting = false;
				uni.showToast({
					title: r.ok ? '已发送，去微信看看' : (r.error || '发送失败'),
					icon: r.ok ? 'success' : 'none'
				});
			});
		},
		onMpSubscribe() {
			if (!this.mpSubReady) {
				uni.showModal({
					title: '还没配模板',
					content: '小程序订阅消息要先去微信公众平台（订阅消息 → 选用模板）建一个模板，把模板 ID 填进 utils/notify.js 的 MP_TMPL_IDS。个人主体可以用一次性订阅。',
					showCancel: false
				});
				return;
			}
			requestMpSubscribe().then((r) => {
				if (r.ok) {
					this.notifyCfg.mpSubscribed = true;
					uni.showToast({ title: '订阅成功', icon: 'success' });
				} else {
					uni.showToast({ title: r.error || '订阅失败', icon: 'none' });
				}
			});
		},
		// 首页音乐条开关。存储键与 utils/music.js 的 K_BAR 保持一致；
		// 这里不 import music.js，避免把 catalog.json 打进设置页包体。
		onMusicBarChange(e) {
			const on = !!(e && e.detail && e.detail.value);
			this.musicBarOn = on;
			try {
				uni.setStorageSync('ak_music_bar_enabled', on ? '1' : '0');
			} catch (err) {}
			// 不弹任何提示：用户自己点的开关，不需要我们再教他去哪儿看
		},
		onGuessDataChange(e) {
			const newValue = e.detail.value;
			this.enableGuessData = newValue;
				uni.setStorageSync('enableGuessData', String(newValue));
			if (newValue) {
				this.downloadGuessData();
			} else {
				this.guessDataStatusMessage = '已关闭预测数据';
			}
		},
		moveDataSource(index, direction) {
			const newIndex = index + direction;
			if (newIndex < 0 || newIndex >= this.dataSourceOptions.length) return;
			const arr = [...this.dataSourceOptions];
			const [item] = arr.splice(index, 1);
			arr.splice(newIndex, 0, item);
			this.dataSourceOptions = arr;
			this.persistDataSourceOrder(arr.map(o => o.value));
		},







		// func_search 跳转：滚动到指定设置项







		scrollToTab(tab) {



			





			const tabMap = {



				data: 'section-data',



				theme: 'section-theme',



				search: 'section-search',



				custom: 'section-custom',



				function: 'section-function',



				about: 'section-function'



			};



			const id = tabMap[tab];



			if (!id) return;



			setTimeout(() => {



				const query = uni.createSelectorQuery().in(this);



				query.select('#' + id).boundingClientRect(rect => {



					if (rect) {



						uni.pageScrollTo({



							scrollTop: rect.top - 10,



							duration: 300



						});



					}



				}).exec();



			}, 300);



		},







			// ===================== 主题设置相关方法 =====================















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







						console.warn('加载主题设置失败:', e);







						this.themeMode = 'simple';







					}







				},







			// 选择主题







			// 切换首页功能展示样式（写入 storage，首页 onShow 时同步）
			selectHomeLayout(layout) {
				if (['grid', 'list', 'minimal'].indexOf(layout) < 0) return;
				this.homeLayout = layout;
				uni.setStorageSync('homeLayout', layout);
				haptic('light');
				const nameMap = { grid: '宫格', list: '列表', minimal: '极简' };
				uni.showToast({
					title: '已切换为' + nameMap[layout] + '模式',
					icon: 'none',
					duration: 1500
				});
			},
			selectTheme(themeValue) {







				if (this.themeMode === themeValue) {







					return;







				}















				uni.showModal({







					title: '切换主题',







					content: `确定要切换到${(this.themeOptions.find(t => t.value === themeValue) || {}).label || '所选风格'}吗？切换后需要重启应用才能生效。`,







					confirmText: '切换',







					cancelText: '取消',







					success: (res) => {







						if (res.confirm) {







							this.themeMode = themeValue;







							uni.setStorageSync('themeMode', themeValue);
							// 每日任务打点
							markDailyTask('theme_try');







							// 通知其他页面主题已更改







							uni.$emit('themeChanged', themeValue);







							uni.showToast({







								title: '主题已切换',







								icon: 'none',







								duration: 2000







							});







						}







					}







				});







			},















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







					console.warn('知晓云请求重试次数过多');







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







						console.warn('知晓云请求错误:', res.statusCode, res.data);







						throw new Error(`请求失败: ${res.statusCode}`);







					}







				} catch (error) {
					this.logError(error);







					console.warn(`知晓云请求失败 (${tableName}):`, error);







					// 如果还有重试次数，则重试







					if (retryCount < 3) {







						console.log(`第${retryCount + 1}次重试...`);







						return await this.knowCloudRequest(tableName, options, retryCount + 1);







					}







					throw error;







				}







			},







			







			// 从知晓云获取搜索词数据URL







			async getSearchDataUrlFromMinapp() {







				try {







					const res = await this.knowCloudRequest('SearchWord_Version', {







						data: {







							limit: 1,







							offset: 0,







							orderby: '-created_at'







						}







					});







					







					console.log('知晓云搜索词版本信息响应:', res);







					







					if (res && res.objects && res.objects.length > 0) {







						const versionData = res.objects[0];







						console.log('搜索词版本数据:', versionData);







						







						// 获取URL，如果为空则使用固定URL







						let searchUrl = versionData.url || '';







						if (!searchUrl) {







							console.warn('知晓云返回的URL为空，使用固定URL');







							searchUrl = FIXED_SEARCH_URL;







						}







						







						return {







							url: searchUrl,







							version: versionData.version || versionData.Version || '',







							updateTime: versionData.updated_at || versionData.created_at || ''







						};







					} else {







						console.warn('知晓云未找到搜索词数据，使用固定URL');







						return {







							url: FIXED_SEARCH_URL,







							version: '固定版本',







							updateTime: new Date().toISOString()







						};







					}







				} catch (error) {
					this.logError(error);







					console.warn('从知晓云获取搜索词URL失败:', error);







					// 如果知晓云请求失败，使用固定URL







					console.warn('知晓云请求失败，使用固定URL');







					return {







						url: FIXED_SEARCH_URL,







						version: '固定版本',







						updateTime: new Date().toISOString()







					};







				}







			},







			







			// ===================== 新增：从知晓云获取预测数据URL =====================







			// 根据 type 获取对应的预测数据URL，type 可以是 'gitcode' 或 'github'







			async getGuessDataUrlFromMinapp(type = 'gitcode') {







				try {







					// 查询 Guess_Version 表，根据 type 过滤，按创建时间倒序取最新一条







					const res = await this.knowCloudRequest('Guess_Version', {







						data: {







							where: JSON.stringify({ type: type }),







							limit: 1,







							offset: 0,







							orderby: '-created_at'







						}







					});







					







					console.log(`知晓云预测版本信息响应 (type=${type}):`, res);







					







					if (res && res.objects && res.objects.length > 0) {







						const versionData = res.objects[0];







						console.log('预测版本数据:', versionData);







						







						// 获取URL，如果为空则使用备用URL







						let guessUrl = versionData.url || '';







						if (!guessUrl) {







							console.warn(`知晓云返回的URL为空 (type=${type})，使用备用URL`);







							guessUrl = FALLBACK_GUESS_URL;







						}







						







						return {







							url: guessUrl,







							version: versionData.version || versionData.Version || '',







							updateTime: versionData.updated_at || versionData.created_at || ''







						};







					} else {







						console.warn(`知晓云未找到预测数据 (type=${type})，使用备用URL`);







						return {







							url: FALLBACK_GUESS_URL,







							version: '固定版本',







							updateTime: new Date().toISOString()







						};







					}







				} catch (error) {
					this.logError(error);







					console.warn(`从知晓云获取预测URL失败 (type=${type}):`, error);







					// 如果知晓云请求失败，使用备用URL







					console.warn('知晓云请求失败，使用备用URL');







					return {







						url: FALLBACK_GUESS_URL,







						version: '固定版本',







						updateTime: new Date().toISOString()







					};







				}







			},







			







			// 统一数据存储的辅助方法







			unifyDataStorage() {







				// 根据当前数据源，将数据统一存储到 unifiedData 中







				// 本地数据优先使用 currentData（已下载的国内源/Github源数据）







				let dataToUse = null;







				let versionToUse = '';







				let updateTimeToUse = '';







				







				switch (this.dataSource) {







					case 'domestic':







						// 国内源数据







						dataToUse = uni.getStorageSync('arknightsData') || uni.getStorageSync('currentData');







						versionToUse = uni.getStorageSync('dataVersion') || uni.getStorageSync('currentDataVersion') || '未知版本';







						updateTimeToUse = uni.getStorageSync('localUpdateTime') || uni.getStorageSync('currentUpdateTime') || '';







						break;







						







					case 'github':







						// GitHub源数据







						dataToUse = uni.getStorageSync('githubCharacters') || uni.getStorageSync('currentData');







						versionToUse = uni.getStorageSync('githubDataVersion') || uni.getStorageSync('currentDataVersion') || 'GitHub版本';







						updateTimeToUse = new Date().toISOString();







						break;







						







					case 'custom':







						// 自定义数据







						dataToUse = uni.getStorageSync('customData') || uni.getStorageSync('currentData');







						versionToUse = '自定义数据版本';







						updateTimeToUse = uni.getStorageSync('customDataInfo')?.uploadTime || uni.getStorageSync('currentUpdateTime') || '';







						break;







						







					case 'local':







						// 本地数据：优先使用 currentData（已下载的国内源/Github源数据）







						dataToUse = uni.getStorageSync('currentData') || uni.getStorageSync('arknightsData');







						versionToUse = uni.getStorageSync('currentDataVersion') || uni.getStorageSync('dataVersion') || '本地版本';







						updateTimeToUse = uni.getStorageSync('currentUpdateTime') || uni.getStorageSync('localUpdateTime') || '';







						break;







				}







				







				if (dataToUse) {







					this.unifiedData = dataToUse;







					this.unifiedDataVersion = versionToUse;







					this.unifiedUpdateTime = updateTimeToUse;







				}







				







				// 将统一数据存储到固定位置，供其他页面使用







				if (this.unifiedData) {







					uni.setStorageSync('currentData', this.unifiedData);

					// 正式数据上线后，把本地预测数据里同号盒清掉，避免角色重复
					pruneStoredGuessData();
					// 正式数据上线后，把本地预测数据里同号盒清掉，避免角色重复






					uni.setStorageSync('currentDataVersion', this.unifiedDataVersion);







					uni.setStorageSync('currentUpdateTime', this.unifiedUpdateTime);







					uni.setStorageSync('currentDataSource', this.dataSource);







					console.log('统一数据存储完成，数据源:', this.dataSource, '数据条数:', this.unifiedData.length);







				}







			},







			onPrivacyConfirm() {







				this.showPrivacyModal = false;







				if (this.pendingFileAction) {







					this.pendingFileAction = false;







					this.doSelectJsonFile();







				}







			},















			onPrivacyCancel() {







				this.showPrivacyModal = false;







				this.pendingFileAction = false;







				uni.showToast({ title: '需要同意隐私协议才能使用文件上传功能', icon: 'none' });







			},











			







			// 清除所有已下载的数据文件







			clearAllDownloadedFiles() {







				try {







					// 清除所有可能的数据存储







					const storageKeys = [







						'arknightsData',







						'githubCharacters',







						'customData',







						'currentData',







						'currentDataVersion',







						'currentUpdateTime',







						// 搜索相关数据







						'searchWords',







						'enableEnglishSearch',







						'enableJapaneseSearch',







						'enableNicknameSearch',







						'customSearchWords',







						// 预测数据







						'guessData',







						'enableGuessData',







						// 版本信息







						'dataVersion',







						'githubDataVersion',







						// 时间信息







						'localUpdateTime',







						'cloudUpdateTime',







						// 自定义数据信息







						'customDataInfo',







						// 文件路径







						'dataUrl',







						'searchDataUrl',







						// 自动更新配置







						'autoUpdateSetting'







					];







					







					storageKeys.forEach(key => {







						uni.removeStorageSync(key);







					});







					







					// 如果是在微信小程序环境中，尝试清除保存的文件







					if (typeof wx !== 'undefined') {







						try {







							// 获取文件管理器







							const fileSystemManager = wx.getFileSystemManager();







							







							// 尝试获取保存的文件列表（可能不可用）







							const savedFileList = wx.getSavedFileList ? wx.getSavedFileList() : null;







							







							if (savedFileList && savedFileList.fileList) {







								// 遍历并删除所有保存的文件







								savedFileList.fileList.forEach(file => {







									wx.removeSavedFile({







										filePath: file.filePath,







										fail: (err) => {







											console.log('删除文件失败:', err);







										}







									});







								});







							}







						} catch (fileErr) {
							this.logError(fileErr);







							console.log('清除文件失败:', fileErr);







						}







					}







					







					return true;







				} catch (e) {
					this.logError(e);







					console.warn('清除数据失败:', e);







					return false;







				}







			},







			







			// 重置所有数据状态







			resetAllDataStates() {







				this.localUpdateTime = '';







				this.cloudUpdateTime = '';







				this.currentVersion = '';







				this.dataCount = 0;







				this.localDataSize = '无数据';







				this.enableGuessData = false;







				this.guessDataCount = 0;







				this.guessDataStatusMessage = '';







				this.enableEnglishSearch = false;







				this.enableJapaneseSearch = false;







				this.enableNicknameSearch = false;







				this.searchWordCount = 0;







				this.customWordsCount = 0;







				this.searchDataStatusMessage = '';







				this.githubCharactersCount = 0;







				this.githubDataStatusMessage = '';







				this.customDataInfo = null;







				this.hasDomesticData = false;







				// 重置统一数据存储







				this.unifiedData = null;







				this.unifiedDataVersion = '';







				this.unifiedUpdateTime = '';







				// 重置自动更新状态







				this.enableAutoUpdate = false;







				this.autoUpdateStatusMessage = '';







				this.lastAutoUpdateTime = '';







			},







			







			// 检查是否有国内源数据







			checkDomesticData() {







				try {







					const domesticData = uni.getStorageSync('arknightsData');







					this.hasDomesticData = domesticData && Array.isArray(domesticData) && domesticData.length > 0;







				} catch (e) {
					this.logError(e);







					console.warn('检查国内源数据失败:', e);







					this.hasDomesticData = false;







				}







			},







			







			// 加载自定义数据信息







			loadCustomDataInfo() {







				try {







					const customDataInfo = uni.getStorageSync('customDataInfo');







					if (customDataInfo) {







						this.customDataInfo = customDataInfo;







					}







				} catch (e) {
					this.logError(e);







					console.warn('加载自定义数据信息失败:', e);







				}







			},







			







			// 加载数据源设置







			loadDataSourceSetting() {







				try {







					const dataSource = uni.getStorageSync('dataSource');







					if (dataSource) {







						this.dataSource = dataSource;







					}







					







					// 检查GitHub数据







					if (this.dataSource === 'github') {







						this.loadGithubDataInfo();







					}







				} catch (e) {
					this.logError(e);







					console.warn('加载数据源设置失败:', e);







				}







			// 恢复数据源排序（读取用户上次拖拽的顺序）
			try {
				let savedOrder = uni.getStorageSync('dataSourceOrderFull');
				if (!Array.isArray(savedOrder) || savedOrder.length === 0) savedOrder = uni.getStorageSync('dataSourceOrder');
				if (Array.isArray(savedOrder) && savedOrder.length > 0) {
					const defaultOptions = this.dataSourceOptions.slice();
					const ordered = [];
					for (const v of savedOrder) {
						const opt = defaultOptions.find(function(o) { return o.value === v; });
						if (opt) ordered.push(opt);
					}
					for (const opt of defaultOptions) {
						if (!ordered.find(function(o) { return o.value === opt.value; })) {
							ordered.push(opt);
						}
					}
					this.dataSourceOptions = ordered;
				}
			} catch (e) {
				this.logError(e);
			}

			},







			







			// 加载GitHub数据信息







			loadGithubDataInfo() {







				try {







					const githubData = uni.getStorageSync('githubCharacters');







					if (githubData && Array.isArray(githubData)) {







						this.githubCharactersCount = githubData.length;







					}







				} catch (e) {
					this.logError(e);







					console.warn('加载GitHub数据信息失败:', e);







				}







			},







			







			// 获取当前数据源名称







			getCurrentDataSourceName() {







				let order = uni.getStorageSync('dataSourceOrder');
				const values = (this.dataSourceOptions || []).map(function (o) { return o.value; });
				if (!Array.isArray(order) || order.length === 0) order = values;
				const first = (order.find(function (v) { return values.indexOf(v) >= 0; })) || values[0];
				const opt = (this.dataSourceOptions || []).find(function (o) { return o.value === first; });
				return opt ? opt.label : '未知';







			},







			







			// 选择数据源







			async selectDataSource(source) {







				// 如果正在下载中，不允许切换







				if (this.isDownloading || this.isUploading) {







					uni.showToast({







						title: '正在处理中，请稍候',







						icon: 'none'







					});







					return;







				}







				







				// 如果已经是当前数据源，不处理







				if (this.dataSource === source) {







					return;







				}







				







				// 如果选择本地数据，检查是否有本地数据（包括国内源/Github源下载的数据）







				if (source === 'local') {







					// 检查多个可能的数据存储位置







					const localData = uni.getStorageSync('arknightsData');







					const githubData = uni.getStorageSync('githubCharacters');







					const currentData = uni.getStorageSync('currentData');







					







					const hasLocalData = localData && Array.isArray(localData) && localData.length > 0;







					const hasGithubData = githubData && Array.isArray(githubData) && githubData.length > 0;







					const hasCurrentData = currentData && Array.isArray(currentData) && currentData.length > 0;







					







					if (!hasLocalData && !hasGithubData && !hasCurrentData) {







						uni.showModal({







							title: '提示',







							content: '本地暂无数据，请先从"国内源"或"GitHub源"下载数据后再选择本地数据源',







							showCancel: false







						});







						return;







					}







					this.confirmSwitchDataSource(source);







					return;







				}







				







				// 如果选择自定义数据







				if (source === 'custom') {







					const customData = uni.getStorageSync('customData');







					if (!customData || customData.length === 0) {







						uni.showModal({







							title: '提示',







							content: '暂无自定义数据，请先上传JSON文件',







							confirmText: '上传数据',







							cancelText: '取消',







							success: (res) => {







								if (res.confirm) {







									this.uploadCustomData();







								}







							}







						});







						return;







					}







					this.confirmSwitchDataSource(source);







					return;







				}







				







				// 如果选择国内源，直接切换







				if (source === 'domestic') {







					this.confirmSwitchDataSource(source);







					return;







				}







				







				// 如果选择知晓云源，直接切换







				if (source === 'knowCloud') {







					this.confirmSwitchDataSource(source);







					return;







				}















				// 如果选择GitHub源，显示警告并开始下载







				if (source === 'github') {







					this.confirmSwitchToGithub(source);







				}







			},







			







			// 确认切换到GitHub源







			async confirmSwitchToGithub(source) {







				// 显示确认对话框







				uni.showModal({







					title: '重要提示',







					content: 'GitHub源仅提供基础角色数据，无法显示分盒信息、市价信息、角色头像及热度标记等特殊功能。切换后将清除所有已下载数据。是否继续使用GitHub源？',







					confirmText: '继续使用',







					cancelText: '取消',







					success: (res) => {







						if (res.confirm) {







							this.startGithubDownload(source);







						}







					}







				});







			},







			







			// 开始GitHub下载







			async startGithubDownload(source) {







				// 清除之前的数据







				if (source !== 'knowCloud') {







				this.clearAllDownloadedFiles();







				}







				







				// 设置下载状态







				this.isDownloading = true;







				this.downloadProgress = 0;







				this.isDownloadCancelled = false;







				







				// 显示下载提示







				uni.showLoading({







					title: '正在下载GitHub数据...',







					mask: true







				});







				







				try {







					// 开始下载







					const success = await this.downloadGithubData();







					







					if (success && !this.isDownloadCancelled) {







						// 下载成功，切换数据源







						this.dataSource = source;







						uni.setStorageSync('dataSource', source);
						this.syncDataSourceOrderWithPolicy();







						







						// 统一数据存储







						this.unifyDataStorage();







						







						uni.hideLoading();







						uni.showToast({







							title: '已成功切换到GitHub源',







							icon: 'success',







							duration: 2000







						});







						







						// 重新加载数据信息







						this.loadDataInfo();







					} else if (!this.isDownloadCancelled) {







						// 下载失败







						uni.hideLoading();







						uni.showModal({







							title: '下载失败',







							content: 'GitHub数据下载失败，请检查网络连接或稍后重试',







							showCancel: false







						});







					}







				} catch (error) {
					this.logError(error);







					console.warn('下载GitHub数据失败:', error);







					if (!this.isDownloadCancelled) {







						uni.hideLoading();







						uni.showModal({







							title: '下载失败',







							content: 'GitHub数据下载失败，请检查网络连接',







							showCancel: false







						});







					}







				} finally {







					this.isDownloading = false;







					this.downloadProgress = 0;







				}







			},







			







			// 确认切换数据源







			confirmSwitchDataSource(source) {







				// 清除之前的数据







				this.clearAllDownloadedFiles();







				







				// 设置新的数据源







				this.dataSource = source;







				uni.setStorageSync('dataSource', source);
				this.syncDataSourceOrderWithPolicy();







				







				// 显示提示







				let message = '';







				if (source === 'domestic') {







					message = '已切换至国内源，下次下载将使用国内镜像';







					// 切换到国内源后，重置数据状态







					this.unifiedData = null;







					this.unifiedDataVersion = '';







					this.unifiedUpdateTime = '';







					this.dataCount = 0;







					this.localUpdateTime = '';







					this.cloudUpdateTime = '';







					this.currentVersion = '';







				} else if (source === 'local') {







					message = '已切换至本地数据，将使用已下载的本地数据';







				} else if (source === 'custom') {







					message = '已切换至自定义数据';







				} else if (source === 'knowCloud') {







					message = '已切换到知晓云源';







				}







				







				uni.showToast({







					title: message,







					icon: 'success',







					duration: 2000







				});







				







				// 统一数据存储







				setTimeout(() => {







					this.unifyDataStorage();







					this.loadDataInfo(); // 重新加载数据信息







				}, 500);







			},







			







			// 取消下载







			cancelDownload() {







				this.isDownloadCancelled = true;







				if (this.downloadTask) {







					this.downloadTask.abort();







				}







				this.isDownloading = false;







				this.downloadProgress = 0;







				







				uni.hideLoading();







				uni.showToast({







					title: '已取消下载',







					icon: 'none'







				});







			},







			







			// 上传自定义数据







			uploadCustomData() {







				if (this.isUploading || this.isDownloading) {







					return;







				}







				







				uni.showActionSheet({







					itemList: ['选择JSON文件', '粘贴JSON数据'],







					success: (res) => {







						if (res.tapIndex === 0) {







							this.selectJsonFile();







						} else {







							this.inputJsonData();







						}







					}







				});







			},







			







			// 选择JSON文件 - 兼容多个平台







			selectJsonFile() {



			// #ifdef MP-WEIXIN



			wx.requirePrivacyAuthorize({



				success: () => { this.doSelectJsonFile(); },



				fail: () => { this.onPrivacyCancel(); }



			});



			// #endif



			// #ifndef MP-WEIXIN



			this.doSelectJsonFile();



			// #endif



		},







	















			// 实际选文件







			doSelectJsonFile() {







				// #ifdef MP-WEIXIN







			    wx.chooseMessageFile({







			        count: 1,







			        type: 'file',







			        extension: ['.json', '.txt'],







			        success: (res) => {







			            const file = res.tempFiles[0];







			            if (file.size > 10 * 1024 * 1024) { // 10MB限制







			                uni.showToast({







			                    title: '文件太大，请选择小于10MB的文件',







			                    icon: 'none'







			                });







			                return;







			            }







			            







			            this.processJsonFile(file.path);







			        },







			        fail: (err) => {







			            console.warn('选择文件失败:', err);







			            uni.showToast({







			                title: '选择文件失败',







			                icon: 'none'







			            });







			        }







			    });







				// #endif







				// #ifdef H5







				uni.chooseFile({







					count: 1,







					type: 'file',







					extension: ['.json', '.txt'],







					success: (res) => {







					    const file = res.tempFiles[0];







					    if (file.size > 10 * 1024 * 1024) { // 10MB限制







					        uni.showToast({







					            title: '文件太大，请选择小于10MB的文件',







					            icon: 'none'







					        });







					        return;







					    }







					    







					    this.processJsonFile(file.path);







					},







					fail: (err) => {







					    console.warn('选择文件失败:', err);







					    uni.showToast({







					        title: '选择文件失败',







					        icon: 'none'







					    });







					}







				});







				// #endif







			},







			// 输入JSON数据







			inputJsonData() {







				uni.showActionSheet({







					itemList: ['粘贴JSON数据', '输入文件直链'],







					success: (res) => {







						if (res.tapIndex === 0) {







							this.inputJsonText();







						} else {







							this.inputJsonUrl();







						}







					}







				});







			},







			







			// 粘贴JSON文本数据







			inputJsonText() {







				uni.showModal({







					title: '输入JSON数据',







					editable: true,







					placeholderText: '请在此处粘贴JSON数据...',







					confirmText: '验证',







					cancelText: '取消',







					success: (res) => {







						if (res.confirm && res.content) {







							this.processJsonString(res.content);







						}







					}







				});







			},







			







			// 输入文件直链（仅支持 raw.githubusercontent.com 和 raw.gitcode.com）







			inputJsonUrl() {







				uni.showModal({







					title: '输入文件直链',







					editable: true,







					placeholderText: '请粘贴 raw.githubusercontent.com 或 raw.gitcode.com 的文件链接...',







					confirmText: '获取',







					cancelText: '取消',







					success: async (res) => {







						if (res.confirm && res.content) {







							const url = res.content.trim();







							// 验证域名







							const allowedDomains = ['raw.githubusercontent.com', 'raw.gitcode.com'];







							let isValid = false;







							for (const domain of allowedDomains) {







								if (url.includes(domain)) {







									isValid = true;







									break;







								}







							}







							







							if (!isValid) {







								uni.showToast({







									title: '仅支持 github/gitcode 直链',







									icon: 'none'







								});







								return;







							}







							







							// 下载并处理







							this.isUploading = true;







							this.uploadProgress = 20;







							







							try {







								const [error, result] = await this.requestUrl(url);







								







								if (error) {







									throw error;







								}







								







								this.uploadProgress = 60;







								await this.processJsonString(result.data);







							} catch (err) {
								this.logError(err);







								console.warn('获取远程JSON失败:', err);







								uni.showToast({







									title: '获取数据失败，请检查链接',







									icon: 'none'







								});







							} finally {







								this.isUploading = false;







								this.uploadProgress = 0;







							}







						}







					}







				});







			},







			







			// 请求URL（Promise封装）







			requestUrl(url) {







				return new Promise((resolve) => {







					uni.request({







						url: url,







						method: 'GET',







						dataType: 'text',







						timeout: 30000,







						success: (res) => {







							if (res.statusCode === 200) {







								resolve([null, res]);







							} else {







								resolve([new Error('HTTP ' + res.statusCode), null]);







							}







						},







						fail: (err) => {







							resolve([err, null]);







						}







					});







				});







			},







			







			// 处理JSON文件







			async processJsonFile(filePath) {







				this.isUploading = true;







				this.uploadProgress = 30;







				







				try {







					const fileContent = await this.readFile(filePath);







					this.uploadProgress = 60;







					await this.processJsonString(fileContent);







				} catch (error) {
					this.logError(error);







					console.warn('处理JSON文件失败:', error);







					uni.showToast({







						title: '读取文件失败',







						icon: 'none'







					});







				} finally {







					this.isUploading = false;







					this.uploadProgress = 0;







				}







			},







			







			// 读取文件







			// ArrayBuffer → UTF-8 字符串（优先 TextDecoder，降级为手写解码，兼容低基础库）
			decodeUtf8(buffer) {
				try {
					if (typeof TextDecoder !== "undefined") {
						return new TextDecoder('utf-8').decode(new Uint8Array(buffer));
					}
				} catch (e) {}
				const bytes = new Uint8Array(buffer);
				let out = "";
				let i = 0;
				while (i < bytes.length) {
					const b = bytes[i++];
					if (b < 0x80) { out += String.fromCharCode(b); continue; }
					if (b >= 0xC0 && b < 0xE0 && i < bytes.length) {
						out += String.fromCharCode(((b & 0x1F) << 6) | (bytes[i++] & 0x3F));
						continue;
					}
					if (b >= 0xE0 && b < 0xF0 && i + 1 < bytes.length) {
						out += String.fromCharCode(((b & 0x0F) << 12) | ((bytes[i++] & 0x3F) << 6) | (bytes[i++] & 0x3F));
						continue;
					}
					if (b >= 0xF0 && i + 2 < bytes.length) {
						let cp = ((b & 0x07) << 18) | ((bytes[i++] & 0x3F) << 12) | ((bytes[i++] & 0x3F) << 6) | (bytes[i++] & 0x3F);
						cp -= 0x10000;
						out += String.fromCharCode(0xD800 + (cp >> 10), 0xDC00 + (cp & 0x3FF));
						continue;
					}
					out += String.fromCharCode(b);
				}
				return out;
			},
			readFile(filePath) {







				return new Promise((resolve, reject) => {







					uni.getFileSystemManager().readFile({







						filePath: filePath,







						encoding: 'utf8',







						success: (res) => {







							resolve(res.data);







						},







						fail: (err) => {







							reject(err);







						}







					});







				});







			},







			







			// 处理JSON字符串







			async processJsonString(jsonString) {
				// ── 入参归一化 ──
				// 真机差异：uni.request 遇到 Content-Type: application/json 会自动把响应体解析成对象，
				// 此时若再 JSON.parse 会得到 "[object Object]" 并抛 SyntaxError；
				// 开发者工具不会自动解析，所以这个故障只在真机复现。
				try {
					if (typeof ArrayBuffer !== "undefined" && jsonString instanceof ArrayBuffer) {
						jsonString = this.decodeUtf8(jsonString);
						console.log('[导入] 入参为 ArrayBuffer，已按 UTF-8 解码');
					} else if (jsonString && typeof jsonString === "object") {
						jsonString = JSON.stringify(jsonString);
						console.log('[导入] 入参已是对象（真机自动解析），已还原为字符串');
					} else if (typeof jsonString === "string") {
						jsonString = jsonString.replace(/^\uFEFF/, '');
					}
				} catch (normErr) {
					console.warn('[导入] 入参归一化异常，按原值继续:', normErr);
				}
				if (jsonString === null || jsonString === undefined || jsonString === "") {
					this.isUploading = false;
					uni.showToast({ title: '数据为空，请检查来源', icon: 'none' });
					return;
				}








				this.isUploading = true;







				this.uploadProgress = 80;







				







				try {







					// 解析JSON







					let jsonData;







					try {







						jsonData = JSON.parse(jsonString);







					} catch (e) {
						this.logError(e);







						uni.showToast({







							title: 'JSON格式错误',







							icon: 'none'







						});







						throw new Error('JSON解析失败');







					}







					







					this.uploadProgress = 90;







					







					// 验证数据格式







					// ── 兼容「分享/导出包」的信封格式 ──
					// 导出端结构：{ version, exportTime, exportType, dataSource, dataCount, data: [...] }
					// 若不解包，validateCustomData() 会判定「数据应为数组格式」。
					// 这里自动解包到内层数组，单层/双层信封都兼容，裸数组不受影响。
					if (jsonData && typeof jsonData === 'object' && !Array.isArray(jsonData)) {
						if (Array.isArray(jsonData.data)) {
							console.log('[导入] 检测到导出信封，已解包 data 字段:', jsonData.data.length, '条');
							jsonData = jsonData.data;
						} else if (jsonData.data && Array.isArray(jsonData.data.data)) {
							console.log('[导入] 检测到双层信封，已解包至最内层数组:', jsonData.data.data.length, '条');
							jsonData = jsonData.data.data;
						}
					}

					const validationResult = this.validateCustomData(jsonData);







					







					this.uploadProgress = 100;







					







					// 保存临时数据







					this.tempCustomData = jsonData;







					this.validationResult = validationResult;







					







					// 显示验证结果







					this.showValidationModal = true;







					







				} catch (error) {
					this.logError(error);







					console.warn('处理JSON数据失败:', error);







					uni.showToast({







						title: '数据处理失败',







						icon: 'none'







					});







				} finally {







					this.isUploading = false;







					this.uploadProgress = 0;







				}







			},







			







			// 验证自定义数据格式







			validateCustomData(data) {







				const result = {







					valid: false,







					message: '',







					details: []







				};







				







				// 1. 检查是否为数组







				if (!Array.isArray(data)) {







					result.message = '数据格式错误：数据应为数组格式';







					result.details.push('❌ 数据应为数组格式');







					return result;







				}







				







				if (data.length === 0) {







					result.message = '数据为空：未找到任何盒号数据';







					result.details.push('❌ 数据为空，未找到任何盒号');







					return result;







				}







				







				result.details.push(`✅ 数据格式正确，共 ${data.length} 个元素`);







				







				// 2. 检查第一个元素的必需字段







				const sampleItem = data[0];







				const requiredFields = ['Box_id'];







				const optionalFields = ['character1', 'character2', 'character3', 'character4', 'character5', 'character6'];







				







				// 检查必需字段







				for (const field of requiredFields) {







					if (sampleItem[field] === undefined) {







						result.details.push(`❌ 缺少必需字段: ${field}`);







					} else {







						result.details.push(`✅ 包含必需字段: ${field}`);







					}







				}







				







				// 检查至少一个character字段







				let hasCharacterField = false;







				for (const field of optionalFields) {







					if (sampleItem[field] !== undefined) {







						hasCharacterField = true;







						result.details.push(`✅ 包含干员字段: ${field}`);







						break;







					}







				}







				







				if (!hasCharacterField) {







					result.details.push('❌ 未找到任何干员字段 (character1-character6)');







				}







				







				// 3. 检查数据类型







				let validTypes = true;







				data.forEach((item, index) => {







					if (item.Box_id && typeof item.Box_id !== 'string' && typeof item.Box_id !== 'number') {







						result.details.push(`❌ 第 ${index + 1} 个元素 Box_id 类型错误`);







						validTypes = false;







					}







				});







				







				if (validTypes) {







					result.details.push('✅ 所有Box_id字段类型正确');







				}







				







				// 4. 检查是否有重复Box_id







				const boxIds = new Set();







				let hasDuplicates = false;







				data.forEach(item => {







					if (item.Box_id) {







						if (boxIds.has(String(item.Box_id))) {







							hasDuplicates = true;







						}







						boxIds.add(String(item.Box_id));







					}







				});







				







				if (hasDuplicates) {







					result.details.push('⚠️ 发现重复的Box_id');







				} else {







					result.details.push('✅ 所有Box_id唯一');







				}







				







				// 5. 统计干员数量







				let characterCount = 0;







				data.forEach(item => {







					for (let i = 1; i <= 6; i++) {







						const field = `character${i}`;







						if (item[field] && item[field].name) {







							characterCount++;







						}







					}







				});







				result.details.push(`📊 共包含 ${characterCount} 个干员条目`);







				







				// 判断是否有效







				const hasRequiredFields = requiredFields.every(field => sampleItem[field] !== undefined);







				const isValid = hasRequiredFields && hasCharacterField && validTypes;







				







				result.valid = isValid;







				result.message = isValid ? 







					`数据验证通过 (${data.length} 个盒号, ${characterCount} 个干员)` : 







					'数据格式不完整，可能无法正常使用';







				







				return result;







			},







			







			// 关闭验证弹窗







			closeValidationModal() {







				this.showValidationModal = false;







				this.tempCustomData = null;







				this.validationResult = {







					valid: false,







					message: '',







					details: []







				};







			},







			







			// 确认使用自定义数据







			confirmUseCustomData() {







				if (!this.validationResult.valid || !this.tempCustomData) {







					return;







				}







				







				try {







					// 清除之前的数据







					this.clearAllDownloadedFiles();







					







					// 保存自定义数据到统一位置







					uni.setStorageSync('customData', this.tempCustomData);







					







					// 保存数据信息







					const dataSize = this.calculateDataSizeString(this.tempCustomData);







					const now = new Date();







					const uploadTime = `${now.getFullYear()}-${(now.getMonth()+1).toString().padStart(2, '0')}-${now.getDate().toString().padStart(2, '0')} ${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;







					







					this.customDataInfo = {







						source: '自定义上传',







						uploadTime: uploadTime,







						size: dataSize,







						boxCount: this.tempCustomData.length,







						validation: this.validationResult







					};







					







					uni.setStorageSync('customDataInfo', this.customDataInfo);







					







					// 切换到自定义数据源







					this.dataSource = 'custom';







					uni.setStorageSync('dataSource', 'custom');
					this.syncDataSourceOrderWithPolicy();







					







					// 更新数据统计







					this.dataCount = this.tempCustomData.length;







					







					// 统一数据存储







					this.unifyDataStorage();







					







					this.closeValidationModal();







					







					uni.showToast({







						title: '自定义数据上传成功',







						icon: 'success',







						duration: 2000







					});







					







					// 重新加载数据信息







					this.loadDataInfo();







					







				} catch (e) {
					this.logError(e);







					console.warn('保存自定义数据失败:', e);







					uni.showToast({







						title: '保存数据失败',







						icon: 'none'







					});







				}







			},







			







			// 计算数据大小字符串







			calculateDataSizeString(data) {







				try {







					const jsonString = JSON.stringify(data);







					const sizeInKB = (jsonString.length / 1024).toFixed(2);







					return `${sizeInKB} KB`;







				} catch (e) {
					this.logError(e);







					return '未知';







				}







			},







			







			// 导出国内源数据到本地







			exportToLocal() {







				if (!this.hasDomesticData) {







					uni.showToast({







						title: '暂无国内源数据',







						icon: 'none'







					});







					return;







				}







				







				uni.showActionSheet({







					itemList: ['保存为JSON文件', '分享数据给好友'],







					success: (res) => {







						if (res.tapIndex === 0) {







							this.saveDataAsJsonFile();







						} else if (res.tapIndex === 1) {







							this.shareDataFile();







						}







					}







				});







			},







			







			// 保存数据为JSON文件







			saveDataAsJsonFile() {







				try {







					// 优先使用统一的数据，如果没有则使用国内源数据







					const dataToExport = this.unifiedData || uni.getStorageSync('arknightsData');







					const version = this.unifiedDataVersion || uni.getStorageSync('dataVersion') || '未知版本';







					







					if (!dataToExport || !Array.isArray(dataToExport)) {







						uni.showToast({







							title: '导出失败：数据不存在',







							icon: 'none'







						});







						return;







					}







					







					// 显示加载提示







					uni.showLoading({







						title: '正在生成文件...',







						mask: true







					});







					







					// 创建要导出的数据对象







					const exportData = {







						version: version,







						exportTime: new Date().toISOString(),







						exportType: '方舟干员数据',







						dataSource: this.dataSource,







						dataCount: dataToExport.length,







						data: dataToExport







					};







					







					// 将数据转换为JSON字符串







					const jsonString = JSON.stringify(exportData, null, 2);







					







					// 创建文件名







					const timestamp = Date.now();







					const fileName = `arknights-data-${timestamp}.${this.exportFileExt()}`;







					







					// 保存为临时文件







					this.saveJsonToFile(jsonString, fileName);







					







				} catch (e) {
					this.logError(e);







					console.warn('导出数据失败:', e);







					uni.hideLoading();







					uni.showToast({







						title: '导出失败',







						icon: 'none'







					});







				}







			},







			







			// 分享数据文件







			shareDataFile() {







			    try {







			        const domesticData = uni.getStorageSync('arknightsData');







			        







			        if (!domesticData || !Array.isArray(domesticData)) {







			            uni.showToast({







			                title: '暂无数据可分享',







			                icon: 'none'







			            });







			            return;







			        }







			        







			        // 显示加载提示







			        uni.showLoading({







			            title: '正在准备数据...',







			            mask: true







			        });







			        







			        // 创建要分享的数据对象







			        const shareData = {







			            version: uni.getStorageSync('dataVersion') || '未知版本',







			            exportTime: new Date().toISOString(),







			            exportType: '方舟干员数据',







			            dataCount: domesticData.length,







			            data: domesticData.slice(0, 100) // 分享时只包含前100条数据，避免文件过大







			        };







			        







			        // 将数据转换为JSON字符串







			        const jsonString = JSON.stringify(shareData, null, 2);







			        







			        // 创建文件名







			        const timestamp = Date.now();







			        const fileName = `arknights-share-${timestamp}.${this.exportFileExt()}`;







			        







			        // 保存并分享







			        this.saveAndShareJsonFile(jsonString, fileName);







			        







			    } catch (e) {
				this.logError(e);







			        uni.hideLoading();







			        console.warn('分享数据失败:', e);







			        uni.showToast({







			            title: '分享失败',







			            icon: 'none'







			        });







			    }







			},







			







			// 保存JSON到文件







			// 导出文件扩展名：iOS 端微信无法预览/选择 .json 文件，改用 .txt 承载
			// （文件内容仍是标准 JSON，导入端按内容解析，不受扩展名影响）
			// 其他平台保持 .json 不变
			exportFileExt() {
				let plat = '';
				// 新版 API 优先，失败回退旧版
				try {
					const d = typeof uni.getDeviceInfo === 'function' ? uni.getDeviceInfo() : null;
					if (d && d.platform) plat = d.platform;
				} catch (e) {}
				if (!plat) {
					try {
						const info = uni.getSystemInfoSync();
						plat = (info && (info.platform || info.osName)) || '';
					} catch (e) {}
				}
				return String(plat).toLowerCase() === 'ios' ? 'txt' : 'json';
			},
			saveJsonToFile(jsonString, fileName) {







			    // 检查平台API







			    if (typeof wx !== 'undefined' && wx.getFileSystemManager) {







			        // 微信小程序环境







			        const fileSystemManager = wx.getFileSystemManager();







			        







			        // 临时文件路径







			        const tempFilePath = `${wx.env.USER_DATA_PATH}/${fileName}`;







			        







			        try {







			            // 写入文件







			            fileSystemManager.writeFileSync(tempFilePath, jsonString, 'utf8');







			            







			            // 保存到本地







			            this.saveFileToLocal(tempFilePath);







			            







			        } catch (writeError) {
				this.logError(writeError);







			            console.warn('写入文件失败:', writeError);







			            uni.hideLoading();







			            uni.showToast({







			                title: '保存失败',







			                icon: 'none'







			            });







			        }







			    } else {







			        // 其他环境，使用uni.saveFile







			        this.saveWithUniApi(jsonString, fileName);







			    }







			},







			







			// 保存文件到本地（微信小程序）







			saveFileToLocal(tempFilePath) {







			    // 使用 wx.saveFile 保存文件







			    wx.saveFile({







			        tempFilePath: tempFilePath,







			        success: (res) => {







			            uni.hideLoading();







			            







			            const savedFilePath = res.savedFilePath;







			            







			            // 显示保存成功提示







			            uni.showModal({







			                title: '保存成功',







			                content: '数据已保存为' + (this.exportFileExt() === 'txt' ? 'TXT' : 'JSON') + '文件，是否要分享给好友？',







			                confirmText: '分享',







			                cancelText: '查看文件',







			                success: (modalRes) => {







			                    if (modalRes.confirm) {







			                        // 分享文件







			                        this.shareFileMessage(savedFilePath);







			                    } else {







			                        // 打开文件







			                        this.openSavedFile(savedFilePath);







			                    }







			                }







			            });







			        },







			        fail: (saveErr) => {







			            uni.hideLoading();







			            console.warn('保存文件失败:', saveErr);







			            







			            // 尝试使用备选方案







			            this.fallbackFileSave(tempFilePath);







			        }







			    });







			},







			







			// 使用uni API保存文件







			saveWithUniApi(jsonString, fileName) {







			    // 在支持的环境中使用 uni.saveFile







			    if (typeof uni.saveFile === 'function') {







			        // 需要先将字符串转换为文件，这里使用临时方案







			        // 注意：uni.saveFile需要临时文件路径，不能直接保存字符串







			        







			        uni.hideLoading();







			        uni.showModal({







			            title: '提示',







			            content: '当前环境支持有限，请手动复制JSON数据',







			            confirmText: '复制数据',







			            cancelText: '取消',







			            success: (res) => {







			                if (res.confirm) {







			                    // 复制到剪贴板







			                    uni.setClipboardData({







			                        data: jsonString,







			                        success: () => {







			                            uni.showToast({







			                                title: '数据已复制到剪贴板',







			                                icon: 'success',







			                                duration: 2000







			                            });







			                        }







			                    });







			                }







			            }







			        });







			    } else {







			        uni.hideLoading();







			        this.showJsonData(jsonString);







			    }







			},







			







			// 备选文件保存方案







			fallbackFileSave(tempFilePath) {







			    // 尝试直接打开文件







			    this.openSavedFile(tempFilePath);







			},







			







			// 打开已保存的文件







			openSavedFile(filePath) {







			    // 在微信小程序中，尝试用 wx.openDocument 打开







			    if (typeof wx !== 'undefined' && wx.openDocument) {







			        wx.openDocument({







			            filePath: filePath,







			            showMenu: true,







			            success: () => {







			                console.log('打开文档成功');







			            },







			            fail: (openErr) => {







			                console.warn('打开文档失败:', openErr);







			                this.showFileInfo(filePath);







			            }







			        });







			    } else {







			        this.showFileInfo(filePath);







			    }







			},







			







			// 显示文件信息







			showFileInfo(filePath) {







			    // 获取文件名







			    const fileName = filePath.split('/').pop() || 'unknown.json';







			    







			    // 显示文件信息弹窗







			    uni.showModal({







			        title: '文件已保存',







			        content: `文件名: ${fileName}\n\n文件已保存到本地，您可以在文件管理中查找。`,







			        confirmText: '确定',







			        cancelText: '复制路径',







			        success: (res) => {







			            if (res.cancel) {







			                // 复制文件路径到剪贴板







			                uni.setClipboardData({







			                    data: filePath,







			                    success: () => {







			                        uni.showToast({







			                            title: '路径已复制',







			                            icon: 'success',







			                            duration: 2000







			                        });







			                    }







			                });







			            }







			        }







			    });







			},







			







			// 保存并分享JSON文件







			saveAndShareJsonFile(jsonString, fileName) {







			    // 检查是否为微信小程序环境







			    if (typeof wx !== 'undefined' && wx.getFileSystemManager) {







			        const fileSystemManager = wx.getFileSystemManager();







			        const tempFilePath = `${wx.env.USER_DATA_PATH}/${fileName}`;







			        







			        try {







			            // 写入临时文件







			            fileSystemManager.writeFileSync(tempFilePath, jsonString, 'utf8');







			            







			            // 直接分享







			            this.shareFileMessage(tempFilePath);







			            







			        } catch (writeError) {
				this.logError(writeError);







			            console.warn('写入分享文件失败:', writeError);







			            uni.hideLoading();







			            uni.showToast({







			                title: '分享失败',







			                icon: 'none'







			            });







			        }







			    } else {







			        uni.hideLoading();







			        // 非微信环境，提供复制数据选项







			        uni.showModal({







			            title: '分享数据',







			            content: '当前环境不支持直接分享文件，是否复制数据文本？',







			            confirmText: '复制',







			            cancelText: '取消',







			            success: (res) => {







			                if (res.confirm) {







			                    uni.setClipboardData({







			                        data: jsonString,







			                        success: () => {







			                            uni.showToast({







			                                title: '数据已复制',







			                                icon: 'success',







			                                duration: 2000







			                            });







			                        }







			                    });







			                }







			            }







			        });







			    }







			},







			







			// 分享文件消息







			shareFileMessage(filePath) {







			    // 检查是否为微信小程序且支持分享文件







			    if (typeof wx !== 'undefined' && wx.shareFileMessage) {







			        uni.hideLoading();







			        







			        wx.shareFileMessage({







			            filePath: filePath,







			            success: () => {







			                uni.showToast({







			                    title: '分享成功',







			                    icon: 'success',







			                    duration: 2000







			                });







			            },







			            fail: (err) => {







			                console.warn('分享失败:', err);







			                







			                // 分享失败，提供备选方案







			                uni.showModal({







			                    title: '分享失败',







			                    content: '无法直接分享文件，请尝试手动发送文件',







			                    showCancel: false,







			                    success: () => {







			                        this.showFileInfo(filePath);







			                    }







			                });







			            }







			        });







			    } else {







			        uni.hideLoading();







			        // 不支持直接分享，显示文件信息







			        this.showFileInfo(filePath);







			    }







			},







			







			// 显示JSON数据（备选方案）







			showJsonData(jsonString) {







			    // 由于数据可能很大，只显示前1000个字符







			    const previewText = jsonString.length > 1000 ? 







			        jsonString.substring(0, 1000) + '...' : 







			        jsonString;







			    







			    uni.showModal({







			        title: 'JSON数据',







			        content: previewText,







			        confirmText: '复制全部',







			        cancelText: '关闭',







			        success: (res) => {







			            if (res.confirm) {







			                uni.setClipboardData({







			                    data: jsonString,







			                    success: () => {







			                        uni.showToast({







			                            title: '数据已复制',







			                            icon: 'success',







			                            duration: 2000







			                        });







			                    }







			                });







			            }







			        }







			    });







			},







			







			// 下载GitHub数据 - 更新为新链接







			downloadGithubData() {







				return new Promise((resolve) => {







					// 创建下载任务







					this.downloadTask = uni.request({







						url: DATA_SOURCES.github.characterUrl,







						method: 'GET',







						timeout: 30000, // 30秒超时







						success: (res) => {







							if (res.statusCode === 200) {







								let data = res.data;







								







								if (typeof data === 'string') {







									try {







										data = JSON.parse(data);







									} catch (e) {
										this.logError(e);







										console.warn('GitHub数据JSON解析失败:', e);







										resolve(false);







										return;







									}







								}







								







								// 处理GitHub盒号数据（新链接提供的是盒号数据）







								if (Array.isArray(data)) {







									// 直接存储盒号数据







									uni.setStorageSync('githubCharacters', data);







									uni.setStorageSync('githubDataVersion', new Date().toISOString());







									







									this.githubCharactersCount = data.length;







									this.githubDataStatusMessage = `GitHub盒号数据下载成功 (${data.length} 个盒号)`;







									







									resolve(true);







								} else {







									// 如果不是数组，尝试按角色数据处理（兼容旧格式）







									const processedCharacters = this.processGithubCharacterData(data);







									this.githubCharactersCount = processedCharacters.length;







									







									// 保存处理后的数据







									uni.setStorageSync('githubCharacters', processedCharacters);







									uni.setStorageSync('githubDataVersion', new Date().toISOString());







									







									this.githubDataStatusMessage = `GitHub角色数据下载成功 (${processedCharacters.length} 个干员)`;







									







									resolve(true);







								}







							} else {







								console.warn('GitHub服务器错误:', res.statusCode);







								resolve(false);







							}







						},







						fail: (err) => {







							console.warn('下载GitHub数据失败:', err);







							resolve(false);







						}







					});







					







					// 监听下载进度







					this.downloadTask.onProgressUpdate = (res) => {







						if (res.progress > 0) {







							this.downloadProgress = res.progress;







						}







					};







				});







			},







			







			// 处理GitHub角色数据（兼容旧格式）







			processGithubCharacterData(rawData) {







				const processedCharacters = [];







				







				// 检查是否为对象类型（角色数据）







				if (typeof rawData === 'object' && !Array.isArray(rawData)) {







					// 遍历所有角色







					for (const charId in rawData) {







						const charData = rawData[charId];







						







						// 跳过非干员角色（如trap等）







						if (!charData.name || charData.profession === 'trap' || charData.profession === 'token') {







							continue;







						}







						







						// 创建处理后的角色对象







						const processedChar = {







							id: charId,







							name: charData.name,







							rarity: charData.rarity,







							profession: charData.profession,







							subProfessionId: charData.subProfessionId,







							onlyELITE1: this.isOnlyElite1(charData.rarity),







						};







						







						processedCharacters.push(processedChar);







					}







					







					console.log(`处理GitHub角色数据: 原始 ${Object.keys(rawData).length} 个，处理后 ${processedCharacters.length} 个干员`);







				}







				







				return processedCharacters;







			},







			







			// 判断是否仅精一（TIER_1, TIER_2, TIER_3）







			isOnlyElite1(rarity) {







				const elite1Rarities = ['TIER_1', 'TIER_2', 'TIER_3'];







				return elite1Rarities.includes(rarity);







			},







			







			// 加载数据信息







			loadDataInfo() {







				try {







					// 优先从统一存储加载







					const unifiedData = uni.getStorageSync('currentData');







					const unifiedVersion = uni.getStorageSync('currentDataVersion');







					const unifiedUpdateTime = uni.getStorageSync('currentUpdateTime');







					







					if (unifiedData) {







						this.unifiedData = unifiedData;







						this.unifiedDataVersion = unifiedVersion || '';







						this.unifiedUpdateTime = unifiedUpdateTime || '';







						this.dataCount = Array.isArray(unifiedData) ? unifiedData.length : 0;







					} else {







						// 如果没有统一数据，按原来的方式加载







						const data = uni.getStorageSync('arknightsData');







						const localUpdateTime = uni.getStorageSync('localUpdateTime');







						const cloudUpdateTime = uni.getStorageSync('cloudUpdateTime');







						const version = uni.getStorageSync('dataVersion');







						const guessData = uni.getStorageSync('guessData');







						const searchWords = uni.getStorageSync('searchWords');







						const customSearchWords = uni.getStorageSync('customSearchWords');







						const customData = uni.getStorageSync('customData');







						







						this.localUpdateTime = localUpdateTime || '';







						this.cloudUpdateTime = cloudUpdateTime || '';







						this.currentVersion = version || '';







						this.dataCount = data && Array.isArray(data) ? data.length : 0;







						this.guessDataCount = guessData && Array.isArray(guessData) ? guessData.length : 0;







						this.searchWordCount = searchWords && Array.isArray(searchWords) ? searchWords.length : 0;







						this.customWordsCount = customSearchWords && Array.isArray(customSearchWords) ? customSearchWords.length : 0;







						







						// 如果有自定义数据，更新计数







						if (customData && Array.isArray(customData) && this.dataSource === 'custom') {







							this.dataCount = customData.length;







						}







					}







					







					// 计算数据大小







					this.calculateDataSize();







					







				} catch (e) {
					this.logError(e);







					console.warn('加载数据信息失败:', e);







				}







			},







			







			// 加载搜索设置







			loadSearchSettings() {







				try {







					const enableEnglishSearch = uni.getStorageSync('enableEnglishSearch');







					const enableJapaneseSearch = uni.getStorageSync('enableJapaneseSearch');







					const enableNicknameSearch = uni.getStorageSync('enableNicknameSearch');







					







					this.enableEnglishSearch = enableEnglishSearch === 'true';







					this.enableJapaneseSearch = enableJapaneseSearch === 'true';







					this.enableNicknameSearch = enableNicknameSearch === 'true';







					







					console.log('加载搜索设置:', {







						english: this.enableEnglishSearch,







						japanese: this.enableJapaneseSearch,







						nickname: this.enableNicknameSearch







					});







				} catch (e) {
					this.logError(e);







					console.warn('加载搜索设置失败:', e);







				}







			},







			







			// 检查是否需要显示警告







			checkSearchOptionsWarning() {







				if (this.enabledSearchCount >= 2) {







					uni.showModal({







						title: '提示',







						content: '当前启用的搜索选项过多，搜索结果可能会模糊或者混乱，是否继续？',







						confirmText: '继续',







						cancelText: '取消',







						success: (res) => {







							if (res.cancel) {







								// 用户取消，恢复之前的设置







								this.enableEnglishSearch = uni.getStorageSync('enableEnglishSearch') === 'true';







								this.enableJapaneseSearch = uni.getStorageSync('enableJapaneseSearch') === 'true';







								this.enableNicknameSearch = uni.getStorageSync('enableNicknameSearch') === 'true';







							} else {







								// 用户确认，保存设置







								this.saveSearchSettings();







							}







						}







					});







				} else {







					// 直接保存设置







					this.saveSearchSettings();







				}







			},







			







			// 保存搜索设置







			saveSearchSettings() {







				uni.setStorageSync('enableEnglishSearch', this.enableEnglishSearch.toString());







				uni.setStorageSync('enableJapaneseSearch', this.enableJapaneseSearch.toString());







				uni.setStorageSync('enableNicknameSearch', this.enableNicknameSearch.toString());







				







				// 如果关闭了某个搜索功能，清理对应的缓存数据







				this.cleanupSearchData();







				







				// 如果启用了搜索功能但本地没有数据，自动下载







				if ((this.enableEnglishSearch || this.enableNicknameSearch) && this.searchWordCount === 0 && this.dataSource !== 'github') {







					this.downloadSearchData();







				}







			},







			







			// 清理搜索数据







			cleanupSearchData() {







				// 如果所有搜索功能都关闭了，清理搜索词数据







				if (!this.enableEnglishSearch && !this.enableJapaneseSearch && !this.enableNicknameSearch) {







					uni.removeStorageSync('searchWords');







					this.searchWordCount = 0;







					this.searchDataStatusMessage = '搜索功能已全部关闭，搜索词数据已清理';







				}







				// 如果关闭了英文搜索，清理英文数据







				else if (!this.enableEnglishSearch) {







					this.removeEnglishData();







				}







				// 如果关闭了外号搜索，清理外号数据







				else if (!this.enableNicknameSearch) {







					this.removeNicknameData();







				}







			},







			







			// 移除英文数据







			removeEnglishData() {







				try {







					const searchWords = uni.getStorageSync('searchWords');







					if (searchWords && Array.isArray(searchWords)) {







						const updatedWords = searchWords.map(word => {







							return {







								...word,







								englishname: '' // 清空英文名







							};







						});







						uni.setStorageSync('searchWords', updatedWords);







					}







				} catch (e) {
					this.logError(e);







					console.warn('清理英文数据失败:', e);







				}







			},







			







			// 移除外号数据







			removeNicknameData() {







				try {







					const searchWords = uni.getStorageSync('searchWords');







					if (searchWords && Array.isArray(searchWords)) {







						const updatedWords = searchWords.map(word => {







							return {







								...word,







								searchword: [] // 清空外号







							};







						});







						uni.setStorageSync('searchWords', updatedWords);







					}







				} catch (e) {
					this.logError(e);







					console.warn('清理外号数据失败:', e);







				}







			},







			







			 // 英文搜索开关变化







			      onEnglishSearchChange(e) {







			        if (this.dataSource === 'github' || this.isDownloading || this.isUploading) {







			          uni.showToast({







			            title: this.isDownloading || this.isUploading ? '正在处理中，请稍候' : 'GitHub源不支持此功能',







			            icon: 'none'







			          });







			          return;







			        }







			







			        this.enableEnglishSearch = e.detail.value;







			        this.checkSearchOptionsWarning();







			







			        // 如果启用搜索且本地无数据，触发下载







			        if (this.enableEnglishSearch && this.searchWordCount === 0 && this.dataSource !== 'github') {







			          this.downloadSearchData();







			        }







			      },







			







			// 日文搜索开关变化







			onJapaneseSearchChange(e) {







				this.enableJapaneseSearch = e.detail.value;







				this.checkSearchOptionsWarning();







			},







			







			   // 外号搜索开关变化







			      onNicknameSearchChange(e) {







			        if (this.dataSource === 'github' || this.isDownloading || this.isUploading) {







			          uni.showToast({







			            title: this.isDownloading || this.isUploading ? '正在处理中，请稍候' : 'GitHub源不支持此功能',







			            icon: 'none'







			          });







			          return;







			        }







			







			        this.enableNicknameSearch = e.detail.value;







			        this.checkSearchOptionsWarning();







			







			        if (this.enableNicknameSearch && this.searchWordCount === 0 && this.dataSource !== 'github') {







			          this.downloadSearchData();







			        }







			      },







			






			// 获取搜索词数据源列表
			getSearchWordSources(primaryUrl) {
				const sources = [];
				const githubRepo = 'awadwd/ArknightsAuthorization_Series-mirror';
				const swFile = 'searchWord.json';
				if (primaryUrl) sources.push({ name: '知晓云', url: primaryUrl });
				sources.push({ name: 'GitCode', url: FIXED_SEARCH_URL });
				sources.push({ name: 'jsDelivr(CDN)', url: 'https://cdn.jsdelivr.net/gh/' + githubRepo + '@main/' + swFile });
				sources.push({ name: 'GitHub Raw', url: 'https://raw.githubusercontent.com/' + githubRepo + '/main/' + swFile });
				return sources;
			},

			async downloadSearchData() {
				if (this.dataSource === 'github' || this.isDownloading || this.isUploading) {
					uni.showToast({
						title: this.isDownloading || this.isUploading ? '正在处理中，请稍候' : 'GitHub源不支持此功能',
						icon: 'none'
					});
					return;
				}

				this.searchDataStatusMessage = '正在获取搜索词数据...';
				let versionInfo = null;

				try {
					versionInfo = await this.getSearchDataUrlFromMinapp();
				} catch (e) {
					this.logError(e);
					console.warn('知晓云版本查询失败:', e);
				}

				const sources = this.getSearchWordSources(versionInfo ? versionInfo.url : null);
				let data = null;
				let successSource = null;

				for (let i = 0; i < sources.length; i++) {
					const src = sources[i];
					this.searchDataStatusMessage = '正在从 ' + src.name + ' 下载...';
					try {
						data = await this.downloadFromUrl(src.url);
						if (data && data.length > 0) {
							successSource = src.name;
							break;
						}
					} catch (err) {
						this.logError(err);
						console.warn('[搜索词] 源 ' + src.name + ' 失败:', err.message || err);
					}
				}

				const processedData = this.processSearchWords(data || []);

				if (processedData.length === 0) {
					this.searchDataStatusMessage = '搜索词获取失败，请重新获取';
					uni.showToast({ title: '搜索词获取失败，请重新获取', icon: 'none', duration: 3000 });
					return;
				}

				uni.setStorageSync('searchWords', processedData);
				this.searchWordCount = processedData.length;
				this.searchDataStatusMessage = '搜索词数据下载成功 (' + processedData.length + ' 个干员)';
				uni.setStorageSync('searchDataVersion', versionInfo ? (versionInfo.version || '未知版本') : '未知版本');
				uni.setStorageSync('searchDataUpdateTime', versionInfo ? (versionInfo.updateTime || new Date().toISOString()) : new Date().toISOString());
				uni.showToast({ title: '搜索词数据更新成功 [' + successSource + ']', icon: 'success', duration: 2000 });
				this.loadDataInfo();
			},

			// 获取预测数据源列表
			getGuessDataSources(primaryUrl) {
				const sources = [];
				const githubRepo = 'awadwd/ArknightsAuthorization_Series-mirror';
				const gbFile = 'guessNew_Box_Id.json';
				if (primaryUrl) sources.push({ name: '知晓云', url: primaryUrl });
				sources.push({ name: 'GitCode', url: FALLBACK_GUESS_URL });
				sources.push({ name: 'jsDelivr(CDN)', url: 'https://cdn.jsdelivr.net/gh/' + githubRepo + '@main/' + gbFile });
				sources.push({ name: 'GitHub Raw', url: 'https://raw.githubusercontent.com/' + githubRepo + '/main/' + gbFile });
				return sources;
			},
			async downloadFromUrl(targetUrl) {
				let res = await new Promise(function(resolve, reject) {
					uni.request({
						url: targetUrl,
						timeout: 15000,
						success: function(resp) {
							if (resp.statusCode === 200 && Array.isArray(resp.data)) {
								resolve(resp.data);
							} else {
								reject(new Error("Invalid data"));
							}
						},
						fail: function(err) {
							reject(new Error(err.errMsg || "Request failed"));
						}
					});
				});
				return res;
			},


			async downloadGuessData() {
				if (this.dataSource === 'github' || this.isDownloading || this.isUploading) {
					uni.showToast({
						title: this.isDownloading || this.isUploading ? '正在处理中，请稍候' : 'GitHub源不支持此功能',
						icon: 'none'
					});
					return;
				}

				this.guessDataStatusMessage = '正在获取预测数据...';
				let versionInfo = null;

				try {
					const type = this.dataSource === 'github' ? 'github' : 'gitcode';
					versionInfo = await this.getGuessDataUrlFromMinapp(type);
				} catch (e) {
					this.logError(e);
					console.warn('知晓云版本查询失败:', e);
				}

				const sources = this.getGuessDataSources(versionInfo ? versionInfo.url : null);
				let data = null;
				let successSource = null;

				for (let i = 0; i < sources.length; i++) {
					const src = sources[i];
					this.guessDataStatusMessage = '正在从 ' + src.name + ' 下载...';
					try {
						data = await this.downloadFromUrl(src.url);
						if (data && data.length > 0) {
							successSource = src.name;
							break;
						}
					} catch (err) {
						this.logError(err);
						console.warn('[预测数据] 源 ' + src.name + ' 失败:', err.message || err);
					}
				}

				if (!data || data.length === 0) {
					this.guessDataStatusMessage = '预测数据获取失败，请检查网络';
					uni.showToast({ title: '预测数据获取失败', icon: 'none', duration: 3000 });
					return;
				}

				const processedData = (data || []).map(box => {
					const cleaned = {};
					for (const key in box) {
						if (key === 'Box_id') {
							cleaned[key] = String(box[key] || '');
						} else if (key.startsWith('character')) {
							cleaned[key] = box[key];
						} else {
							cleaned[key] = String(box[key] || '');
						}
					}
					cleaned.Box_type = cleaned.Box_type || 'normal';
					return cleaned;
				});

				uni.setStorageSync('guessData', processedData);
				this.guessDataCount = processedData.length;
				this.guessDataStatusMessage = '预测数据下载成功 (' + processedData.length + ' 个预测盒号)';
				uni.setStorageSync('guessDataVersion', versionInfo ? (versionInfo.version || '未知版本') : '未知版本');
				uni.setStorageSync('guessDataUpdateTime', versionInfo ? (versionInfo.updateTime || new Date().toISOString()) : new Date().toISOString());
				uni.showToast({ title: '预测数据更新成功 [' + successSource + ']', icon: 'success', duration: 2000 });
				this.loadDataInfo();
			},// ===================== 自动更新相关方法 =====================







			







			// 加载自动更新设置







			loadAutoUpdateSetting() {







			    try {







			        // 数据更新提示开关：默认开启
			        const notifyRaw = uni.getStorageSync('updateNotifyEnabled');
			        this.updateNotifyEnabled = (notifyRaw === '' || notifyRaw === null || notifyRaw === undefined) ? true : !!notifyRaw;
			        const autoUpdateSetting = uni.getStorageSync('autoUpdateSetting');







			        console.log('从存储加载的自动更新设置:', autoUpdateSetting);







			        







			        if (autoUpdateSetting) {







			            // 更新本地数据状态







			            this.enableAutoUpdate = autoUpdateSetting.enabled || false;







			            this.lastAutoUpdateTime = autoUpdateSetting.lastCheckTime || '';







			            







			            console.log('加载后 enableAutoUpdate:', this.enableAutoUpdate);







			            







			            // 更新配置对象







			            this.autoUpdateConfig = {







			                ...this.autoUpdateConfig,







			                enabled: autoUpdateSetting.enabled || false,







			                lastCheckTime: autoUpdateSetting.lastCheckTime || '',







			                wifiOnly: autoUpdateSetting.wifiOnly !== undefined ? autoUpdateSetting.wifiOnly : true,







			                autoDownload: autoUpdateSetting.autoDownload !== undefined ? autoUpdateSetting.autoDownload : true,







			                checkFrequency: autoUpdateSetting.checkFrequency || 24,







			                checkInterval: autoUpdateSetting.checkInterval || (24 * 60 * 60 * 1000)







			            };







			            







			            // 设置频率选择器的索引







			            this.setFrequencyIndexFromConfig();







			            







			            if (this.enableAutoUpdate) {







			                this.autoUpdateStatusMessage = '自动更新已开启';







			                console.log('自动更新状态: 已开启');







			            } else {







			                this.autoUpdateStatusMessage = '自动更新已关闭';







			                console.log('自动更新状态: 已关闭');







			            }







			        } else {







			            console.log('未找到自动更新设置，使用默认值');







			            // 使用默认配置







			            this.enableAutoUpdate = false;







			            this.autoUpdateConfig = {







			                enabled: false,







			                lastCheckTime: '',







			                wifiOnly: true,







			                autoDownload: true,







			                checkFrequency: 24,







			                checkInterval: 24 * 60 * 60 * 1000







			            };







			            this.autoUpdateStatusMessage = '自动更新未配置';







			        }







			    } catch (e) {
				this.logError(e);







			        console.warn('加载自动更新设置失败:', e);







			        // 出错时使用默认值







			        this.enableAutoUpdate = false;







			        this.autoUpdateConfig = {







			            enabled: false,







			            lastCheckTime: '',







			            wifiOnly: true,







			            autoDownload: true,







			            checkFrequency: 24,







			            checkInterval: 24 * 60 * 60 * 1000







			        };







			        this.autoUpdateStatusMessage = '加载设置失败';







			    }







			},







			







			// 根据配置设置频率选择器的索引







			setFrequencyIndexFromConfig() {







				const checkInterval = this.autoUpdateConfig.checkInterval || 24 * 60 * 60 * 1000;







				this.updateFrequencyIndex = this.updateFrequencyValues.findIndex(value => value === checkInterval);







				if (this.updateFrequencyIndex === -1) {







					this.updateFrequencyIndex = 2; // 默认24小时







				}







			},







			







			// 修改 saveAutoUpdateSetting 方法







			saveAutoUpdateSetting() {







			    try {







			        // 确保配置对象结构正确







			        const settingToSave = {







			            enabled: this.enableAutoUpdate,







			            lastCheckTime: this.lastAutoUpdateTime || '',







			            wifiOnly: this.autoUpdateConfig.wifiOnly,







			            autoDownload: this.autoUpdateConfig.autoDownload,







			            checkFrequency: this.autoUpdateConfig.checkFrequency,







			            checkInterval: this.updateFrequencyValues[this.updateFrequencyIndex]







			        };







			        







			        console.log('保存自动更新设置（原始对象）:', settingToSave);







			        







			        // 将对象转换为普通对象（避免 Proxy）







			        const plainObject = JSON.parse(JSON.stringify(settingToSave));







			        console.log('保存自动更新设置（普通对象）:', plainObject);







			        







			        // 统一使用 'autoUpdateSetting' 作为存储键名







			        uni.setStorageSync('autoUpdateSetting', plainObject);







			        







			        console.log('自动更新设置已保存到存储');







			        







			        // 更新全局数据







			        if (typeof getApp !== 'undefined') {







			            const app = getApp();







			            if (app && app.globalData) {







			                app.globalData.autoUpdateSetting = plainObject;







			            }







			        }







			        







			        return true;







			    } catch (e) {
				this.logError(e);







			        console.warn('保存自动更新设置失败:', e);







			        return false;







			    }







			},







			







			// 自动更新开关变化







			onAutoUpdateChange(e) {







			    console.log('自动更新开关变化:', e.detail.value);







			    







			    if (this.dataSource === 'github' || this.dataSource === 'custom' || this.isDownloading || this.isUploading) {







			        uni.showToast({







			            title: this.isDownloading || this.isUploading ? '正在处理中，请稍候' : 'GitHub源和自定义数据不支持自动更新',







			            icon: 'none'







			        });







			        







			        // 恢复开关状态







			        this.$nextTick(() => {







			            this.enableAutoUpdate = !e.detail.value;







			        });







			        return;







			    }







			    







			    const newValue = e.detail.value;







			    console.log('新值:', newValue, '当前值:', this.enableAutoUpdate);







			    







			    // 立即更新UI状态







			    this.enableAutoUpdate = newValue;







			    







			    if (newValue) {







			        // 启用自动更新







			        setTimeout(() => {







			            this.showAutoUpdateConfirm();







			        }, 100);







			    } else {







			        // 禁用自动更新







			        setTimeout(() => {







			            this.disableAutoUpdate();







			        }, 100);







			    }







			},







			







			// 显示自动更新确认对话框







			showAutoUpdateConfirm() {







			    console.log('显示自动更新确认对话框');







			    







			    uni.showModal({







			        title: '启用自动更新',







			        content: '启用后，系统将在应用启动时检查数据更新。您可以在设置中配置检查频率和网络条件。是否启用自动更新？',







			        confirmText: '启用配置',







			        cancelText: '仅更新',







			        success: (res) => {







			            console.log('用户选择:', res.confirm ? '启用并配置' : '仅启用');







			            







			            if (res.confirm) {







			                // 用户选择启用并配置，打开配置弹窗







			                this.showAutoUpdateModal = true;







			                console.log('打开配置弹窗');







			            } else if (res.cancel) {







			                // 用户选择仅启用，使用默认配置







			                console.log('仅启用，使用默认配置');







			                this.enableAutoUpdateConfirm();







			            }







			        },







			        fail: (err) => {







			            console.warn('显示确认对话框失败:', err);







			            // 对话框失败，恢复开关状态







			            this.enableAutoUpdate = false;







			        }







			    });







			},







			







			// 确认启用自动更新







			enableAutoUpdateConfirm() {







			    console.log('确认启用自动更新，当前状态:', this.enableAutoUpdate);







			    







			    // 确保状态为 true







			    if (!this.enableAutoUpdate) {







			        this.enableAutoUpdate = true;







			    }







			    







			    // 保存设置







			    const saved = this.saveAutoUpdateSetting();







			    







			    if (saved) {







			        // 设置状态消息







			        this.autoUpdateStatusMessage = '自动更新已开启';







			        







			        // 显示成功提示







			        uni.showToast({







			            title: '自动更新已启用',







			            icon: 'success',







			            duration: 2000







			        });







			        







			        // 更新状态显示







			        this.loadDataInfo();







			        







			        // 通知其他页面







			        this.notifyAutoUpdateChanged();







			        







			        // 打开配置弹窗（如果用户选择的是"启用并配置"）







			        if (this.showAutoUpdateModal) {







			            // 已经显示弹窗，不需要再次打开







			        } else {







			            // 用户选择"仅启用"，不显示配置弹窗







			            console.log('自动更新已启用，未打开配置弹窗');







			        }







			    } else {







			        // 保存失败，恢复开关状态







			        this.enableAutoUpdate = false;







			        uni.showToast({







			            title: '保存失败',







			            icon: 'error',







			            duration: 2000







			        });







			    }







			},







			







			// 通知其他页面自动更新设置已更改







			notifyAutoUpdateChanged() {







			    // 方法1: 使用全局数据（简单方法）







			    const globalData = getApp().globalData || {};







			    globalData.autoUpdateSetting = {







			        ...this.autoUpdateConfig,







			        enabled: this.enableAutoUpdate,







			        lastCheckTime: this.lastAutoUpdateTime







			    };







			    







			    // 方法2: 触发自定义事件（如果使用事件总线）







			    // uni.$emit('autoUpdateSettingChanged', {







			    //     enabled: this.enableAutoUpdate,







			    //     config: this.autoUpdateConfig







			    // });







			    







			    console.log('已通知其他页面自动更新设置已更改');







			},







			







			// 禁用自动更新







			disableAutoUpdate() {







			    // 更新设置







			    const saved = this.saveAutoUpdateSetting();







			    







			    if (saved) {







			        // 设置状态消息







			        this.autoUpdateStatusMessage = '自动更新已关闭';







			        







			        // 显示提示







			        uni.showToast({







			            title: '自动更新已关闭',







			            icon: 'success',







			            duration: 2000







			        });







			        







			        // 清除状态消息







			        setTimeout(() => {







			            this.autoUpdateStatusMessage = '';







			        }, 3000);







			        







			        // 更新状态显示







			        this.loadDataInfo();







			    }







			},







			







			// 关闭自动更新设置弹窗







			closeAutoUpdateModal() {







				this.showAutoUpdateModal = false;







			},







			







			// 保存自动更新配置







			saveAutoUpdateConfig() {







			    console.log('保存自动更新配置');







			    







			    // 确保自动更新是启用状态







			    if (!this.enableAutoUpdate) {







			        this.enableAutoUpdate = true;







			    }







			    







			    // 保存配置







			    const saved = this.saveAutoUpdateSetting();







			    







			    if (saved) {







			        // 关闭弹窗







			        this.showAutoUpdateModal = false;







			        







			        // 设置状态消息







			        this.autoUpdateStatusMessage = '自动更新配置已保存';







			        







			        // 显示成功提示







			        uni.showToast({







			            title: '配置已保存',







			            icon: 'success',







			            duration: 2000







			        });







			        







			        // 更新状态显示







			        this.loadDataInfo();







			    } else {







			        uni.showToast({







			            title: '保存失败',







			            icon: 'error',







			            duration: 2000







			        });







			    }







			},







			







			// 仅Wi-Fi开关变化







			onWifiOnlyChange(e) {







				this.autoUpdateConfig.wifiOnly = e.detail.value;







			},

			// 数据更新提示开关
			onUpdateNotifyChange(e) {
				const enabled = !!(e.detail && e.detail.value);
				this.updateNotifyEnabled = enabled;
				try {
					uni.setStorageSync('updateNotifyEnabled', enabled);
					uni.showToast({
						title: enabled ? '已开启更新提示' : '已关闭更新提示',
						icon: 'none',
						duration: 1500
					});
				} catch (err) {
					this.logError(err, 'Setting.onUpdateNotifyChange');
				}
			},








			







			// 检查频率变化







			onFrequencyChange(e) {







				this.updateFrequencyIndex = e.detail.value;







				this.autoUpdateConfig.checkInterval = this.updateFrequencyValues[this.updateFrequencyIndex];







			},







			







			// 自动下载开关变化







			onAutoDownloadChange(e) {







				this.autoUpdateConfig.autoDownload = e.detail.value;







			},







			







			// 手动检查更新







			manualCheckUpdate() {







				if (this.dataSource === 'github' || this.dataSource === 'custom') {







					uni.showToast({







						title: '当前数据源不支持自动更新',







						icon: 'none'







					});







					return;







				}







				







				if (this.isDownloading || this.isUploading) {







					uni.showToast({







						title: '正在处理中，请稍候',







						icon: 'none'







					});







					return;







				}







				







				// 触发检查更新事件，由其他页面处理







				this.autoUpdateStatusMessage = '正在检查更新...';







				







				// 可以通过事件总线或全局状态触发其他页面的检查







				// 这里我们简单更新状态







				setTimeout(() => {







					this.autoUpdateStatusMessage = '已触发检查更新，请查看应用启动时的提示';







				}, 1000);







			},







			







			// ===================== 其他页面调用的方法 =====================







			







			// 提供给其他页面检查是否启用自动更新的方法







			getAutoUpdateConfig() {







				return this.autoUpdateConfig;







			},







			







			// 提供给其他页面更新状态的方法







			updateAutoUpdateStatus(message, lastCheckTime = '') {







				this.autoUpdateStatusMessage = message;







				if (lastCheckTime) {







					this.lastAutoUpdateTime = lastCheckTime;







				}







			},







			







			// 计算数据大小







			calculateDataSize() {







				try {







					// 优先使用统一数据







					let dataToCalculate = this.unifiedData || uni.getStorageSync('currentData');







					







					if (!dataToCalculate) {







						// 如果没有统一数据，使用所有可能的数据源







						const data = uni.getStorageSync('arknightsData');







						const guessData = uni.getStorageSync('guessData');







						const searchWords = uni.getStorageSync('searchWords');







						const customSearchWords = uni.getStorageSync('customSearchWords');







						const githubCharacters = uni.getStorageSync('githubCharacters');







						const customData = uni.getStorageSync('customData');







						







						let totalSize = 0;







						







						// 使用字符串长度来估算存储大小（UTF-8编码，中文字符约3字节）







						const calculateStringSize = (str) => {







							let size = 0;







							for (let i = 0; i < str.length; i++) {







								const code = str.charCodeAt(i);







								if (code <= 0x7F) {







									size += 1; // ASCII字符







								} else if (code <= 0x7FF) {







									size += 2; // 双字节字符







								} else {







									size += 3; // 中文字符等







								}







							}







							return size;







						};







						







						if (data && Array.isArray(data)) {







							const dataString = JSON.stringify(data);







							totalSize += calculateStringSize(dataString);







						}







						







						if (guessData && Array.isArray(guessData)) {







							const guessDataString = JSON.stringify(guessData);







							totalSize += calculateStringSize(guessDataString);







						}







						







						if (searchWords && Array.isArray(searchWords)) {







							const searchWordsString = JSON.stringify(searchWords);







							totalSize += calculateStringSize(searchWordsString);







						}







						







						if (customSearchWords && Array.isArray(customSearchWords)) {







							const customWordsString = JSON.stringify(customSearchWords);







							totalSize += calculateStringSize(customWordsString);







						}







						







						if (githubCharacters && Array.isArray(githubCharacters)) {







							const githubString = JSON.stringify(githubCharacters);







							totalSize += calculateStringSize(githubString);







						}







						







						if (customData && Array.isArray(customData)) {







							const customDataString = JSON.stringify(customData);







							totalSize += calculateStringSize(customDataString);







						}







						







						if (totalSize > 0) {







							// 转换为KB







							const sizeInKB = (totalSize / 1024).toFixed(2);







							this.localDataSize = `${sizeInKB} KB`;







						} else {







							this.localDataSize = '无数据';







						}







						







						return;







					}







					







					// 如果有统一数据，只计算统一数据的大小







					const jsonString = JSON.stringify(dataToCalculate);







					const sizeInKB = (jsonString.length / 1024).toFixed(2);







					this.localDataSize = `${sizeInKB} KB`;







					







				} catch (e) {
					this.logError(e);







					console.warn('计算数据大小失败:', e);







					this.localDataSize = '未知';







				}







			},







			







			// 格式化云端时间







			formatCloudTime(isoTime) {







				if (!isoTime) return '';







				try {







					const date = new Date(isoTime);







					return `${date.getFullYear()}-${(date.getMonth()+1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')} ${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;







				} catch (e) {
					this.logError(e);







					console.warn('格式化云端时间失败:', e);







					return isoTime;







				}







			},







			







			// 显示删除确认对话框







			showDeleteConfirm() {







				if (this.isDownloading || this.isUploading) {







					uni.showToast({







						title: '正在处理中，请稍候',







						icon: 'none'







					});







					return;







				}







				







				uni.showModal({







					title: '确认删除',







					content: '确定要删除所有本地干员数据吗？删除后需要重新下载数据才能使用查询功能。',







					confirmText: '删除',







					confirmColor: '#FA5151',







					success: (res) => {







						if (res.confirm) {







							this.deleteLocalData();







						}







					}







				});







			},







			







			// 加载市价设置







			loadMarketPriceSetting() {







			  try {







			    const showMarketPrice = uni.getStorageSync('showMarketPrice');







			    this.showMarketPrice = showMarketPrice === 'true';







			    console.log('加载市价设置:', this.showMarketPrice);







			  } catch (e) {
				this.logError(e);







			    console.warn('加载市价设置失败:', e);







			    this.showMarketPrice = false;







			  }







			},







			







			// 市价展示开关变化







			onMarketPriceChange(e) {







			  if (this.isDownloading || this.isUploading) {







			    uni.showToast({







			      title: '正在处理中，请稍候',







			      icon: 'none'







			    });







			    return;







			  }







			  







			  const newValue = e.detail.value;







			  







			  // 如果是关闭操作，直接执行







			  if (!newValue) {







			    this.showMarketPrice = false;







			    uni.setStorageSync('showMarketPrice', 'false');







			    uni.showToast({







			      title: '已关闭市价展示',







			      icon: 'success',







			      duration: 1500







			    });







			    return;







			  }







			  







			  // 如果是开启操作，显示免责声明







			  this.showMarketPriceDisclaimerModal(newValue);







			},







			







			// 显示市价展示免责声明模态框







			showMarketPriceDisclaimerModal(newValue) {







			  uni.showModal({







			    title: '免责声明',







			    content: '本程序显示的市价信息来源于第三方平台（千岛APP），数据仅供参考。\n\n请注意：\n1. 本程序不保证数据价格的准确性和真实性\n2. 价格可能因市场波动而实时变化\n3. 请以实际交易价格为准\n4. 因依赖此信息造成的任何损失，本程序概不负责\n\n是否同意上述声明并开启市价展示？',







			    confirmText: '同意开启',







			    cancelText: '取消',







			    confirmColor: '#409EFF',







			    cancelColor: '#999999',







			    success: (res) => {







			      if (res.confirm) {







			        // 用户同意，开启市价展示







			        this.showMarketPrice = true;







			        uni.setStorageSync('showMarketPrice', 'true');







			        







			        uni.showToast({







			          title: '已开启市价展示',







			          icon: 'success',







			          duration: 2000







			        });







			        







			        // 额外提示用户数据来源







			        setTimeout(() => {







			          uni.showModal({







			            title: '温馨提示',







			            content: '市价数据来源于千岛APP，仅供参考。实际价格请以交易时为准。',







			            showCancel: false,







			            confirmText: '知道了',







			            confirmColor: '#409EFF'







			          });







			        }, 1000);







			        







			      } else if (res.cancel) {







			        // 用户取消，保持关闭状态







			        this.showMarketPrice = false;







			        uni.setStorageSync('showMarketPrice', 'false');







			        







			        // 需要强制更新UI，因为switch可能已经被用户点击了







			        this.$forceUpdate();







			        







			        // 显示取消提示







			        uni.showToast({







			          title: '已取消开启市价',







			          icon: 'none',







			          duration: 1500







			        });







			      }







			    },







			    fail: (err) => {







			      console.warn('显示免责声明失败:', err);







			      // 出错时保持原状态







			      this.showMarketPrice = false;







			      this.$forceUpdate();







			    }







			  });







			},







			







			// 删除本地数据







			deleteLocalData() {







				try {







					// 清除所有数据







					const success = this.clearAllDownloadedFiles();







					







					if (!success) {







						throw new Error('清除数据失败');







					}







					







					// 重置数据源为国内源







					this.dataSource = 'domestic';







					uni.setStorageSync('dataSource', 'domestic');
					this.syncDataSourceOrderWithPolicy();







					







					// 重置所有状态变量







					this.resetAllDataStates();







					







					uni.showToast({







						title: '数据删除成功，已切换回国内源',







						icon: 'success',







						duration: 2000







					});







					







					// 延迟返回首页







					setTimeout(() => {







						uni.navigateBack();







					}, 1500);







					







				} catch (e) {
					this.logError(e);







					console.warn('删除数据失败:', e);







					uni.showToast({







						title: '删除失败',







						icon: 'error',







						duration: 2000







					});







				}







			}







		}







	}







</script>















<style scoped>

/* 报错日志功能区 */
.error-log-section {
	margin: 20rpx 0;
	background: #fff;
	border-radius: 12rpx;
	overflow: hidden;
}
.error-log-section .section-header {
	padding: 20rpx 24rpx 0;
}
	/* 通知：token 输入行 */
	.notify-token-row {
		display: flex;
		align-items: center;
		padding: 0 0 20rpx;
	}
	.notify-token-input {
		flex: 1;
		height: 78rpx;
		padding: 0 24rpx;
		border-radius: 12rpx;
		border: 1rpx solid rgba(128, 128, 128, 0.3);
		font-size: 24rpx;
		background: var(--ak-surface-muted);
		color: var(--ak-text-primary);
		margin-right: 16rpx;
	}
	.notify-token-ph {
		color: var(--ak-text-muted);
	}
	.notify-token-hint {
		padding: 0 4rpx 22rpx;
	}
	.notify-token-hint-txt {
		font-size: 22rpx;
		color: var(--ak-text-muted);
	}
	.notify-token-btn {
		padding: 0 26rpx;
		height: 78rpx;
		line-height: 78rpx;
		border-radius: 12rpx;
		font-size: 24rpx;
		background: var(--ak-surface-dark);
		color: var(--ak-text-inverse);
	}

.error-log-section .section-title {
	font-size: 30rpx;
	font-weight: bold;
	color: #333;
}
.error-log-section .section-body {
	padding: 16rpx 24rpx 24rpx;
}
.error-log-desc {
	display: block;
	font-size: 24rpx;
	color: #999;
	line-height: 1.6;
	margin-bottom: 20rpx;
}
.error-log-actions {
	display: flex;
	flex-direction: row;
	gap: 20rpx;
}
.error-log-btn {
	flex: 1;
	font-size: 26rpx;
	background: #007aff;
	color: #fff;
	border-radius: 8rpx;
	padding: 14rpx 0;
	line-height: 1.4;
}
.error-log-btn-danger {
	background: #ff3b30;
}
.error-log-mask {
	position: fixed;
	left: 0;
	top: 0;
	right: 0;
	bottom: 0;
	background: rgba(0,0,0,0.5);
	z-index: 999;
	display: flex;
	align-items: center;
	justify-content: center;
}
.error-log-modal {
	width: 86%;
	max-height: 70vh;
	background: #fff;
	border-radius: 16rpx;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}
.error-log-modal-header {
	display: flex;
	flex-direction: row;
	justify-content: space-between;
	align-items: center;
	padding: 24rpx;
	border-bottom: 1rpx solid #eee;
}
.error-log-modal-title {
	font-size: 30rpx;
	font-weight: bold;
	color: #333;
}
.error-log-modal-close {
	font-size: 32rpx;
	color: #999;
	padding: 0 10rpx;
}
.error-log-content {
	flex: 1;
	padding: 24rpx;
	max-height: 50vh;
}
.error-log-text {
	font-size: 24rpx;
	color: #333;
	line-height: 1.6;
	white-space: pre-wrap;
	word-break: break-all;
}
.error-log-modal-actions {
	margin: 16rpx 24rpx 24rpx;
	border: 1rpx solid #f0f0f0;
	border-radius: 16rpx;
	background: #fff;
	overflow: hidden;
}
.error-log-action-row {
	display: flex;
	flex-direction: row;
	align-items: center;
	justify-content: space-between;
	padding: 26rpx 30rpx;
}
.error-log-action-row + .error-log-action-row {
	border-top: 1rpx solid #f0f0f0;
}
.error-log-action-name {
	font-size: 28rpx;
	color: #333;
}
.error-log-action-value {
	font-size: 26rpx;
	color: #007aff;
}
.error-log-action-value.danger {
	color: #ff3b30;
}

	.container {

		padding: 30rpx;

		background-color: #f5f5f5;

		min-height: 100vh;

	}

	.data-source-item.dragging {
		background-color: #f0f8ff !important;
		box-shadow: 0 12rpx 30rpx rgba(64, 158, 255, 0.45);
		opacity: 0.95;
		z-index: 10;
	}
	.data-source-item.active {
		border-left: 6rpx solid #409EFF;
		background-color: #eaf4ff;
	}
	.drag-handle {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 56rpx;
		height: 56rpx;
		transition: transform 0.2s ease, background-color 0.2s ease, box-shadow 0.2s ease;
	}
	.drag-handle.dragging {
		background-color: #409EFF !important;
		color: #fff !important;
		transform: scale(1.3);
		box-shadow: 0 4rpx 12rpx rgba(64, 158, 255, 0.5);
		border-radius: 8rpx;
	}
	.drag-handle.dragging {
		background-color: #409EFF !important;
		color: #fff !important;
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

	

	/* 分区样式 */

	.section {

		background-color: #fff;

		border-radius: 16rpx;

		margin-bottom: 30rpx;

		overflow: hidden;

		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);

	}

	

	.section-header {

		padding: 25rpx 30rpx;

		border-bottom: 1rpx solid #f0f0f0;

		background-color: #fafafa;

	}

	

	.section-title {

		font-size: 30rpx;

		font-weight: bold;

		color: #333;

	}

	

	.section-content {

		padding: 0;

	}

	

	/* 数据源选择样式 */

	.data-source-options {

		display: flex;

		flex-direction: column;

		gap: 15rpx;

		padding: 30rpx;

	}

	

	.data-source-item {

		display: flex;

		align-items: center;

		justify-content: space-between;

		padding: 25rpx;

		border: 2rpx solid #f0f0f0;

		border-radius: 12rpx;

		background-color: #fff;

		transition: all 0.3s;

	}

	

	.data-source-item.active {

		border-color: #409EFF;

		background-color: #e8f4ff;

	}

	

	.data-source-item.disabled {

		opacity: 0.6;

		pointer-events: none;

	}

	

	.data-source-item.loading {

		position: relative;

		background-color: #f8f9fa;

	}

	

	.data-source-info {

		flex: 1;

	}

	

	.data-source-label {

		font-size: 28rpx;

		color: #333;

		font-weight: 500;

		display: block;

		margin-bottom: 8rpx;

	}

	

	.loading-text {

		color: #409EFF;

		font-size: 24rpx;

		font-weight: normal;

	}

	

	.data-source-desc {

		font-size: 24rpx;

		color: #999;

		display: block;

		margin-bottom: 5rpx;

	}

	

	.ds-order-tip {
		display: block;
		font-size: 22rpx;
		color: #E6A23C;
		padding: 0 30rpx 16rpx;
	}

	.data-source-warning {

		font-size: 22rpx;

		color: #F56C6C;

		display: block;

		margin-top: 5rpx;

	}

	

	

	

	.data-source-item.active .data-source-radio {

		border-color: #409EFF;

	}

	

	.radio-inner {

		width: 20rpx;

		height: 20rpx;

		border-radius: 50%;

		background-color: #409EFF;

	}

	

	.loading-spinner {

		width: 20rpx;

		height: 20rpx;

		border: 2rpx solid #f3f3f3;

		border-top: 2rpx solid #409EFF;

		border-radius: 50%;

		animation: spin 1s linear infinite;

	}

	

	@keyframes spin {

		0% { transform: rotate(0deg); }

		100% { transform: rotate(360deg); }

	}

	

	/* 自定义数据管理 */

	.custom-data-management {

		padding: 30rpx;

		border-top: 1rpx solid #f0f0f0;

	}

	

	.custom-data-actions {

		display: flex;

		gap: 20rpx;

		margin-bottom: 30rpx;

	}

	

	.upload-btn, .export-btn {

		flex: 1;

		border-radius: 10rpx;

		font-size: 26rpx;

		padding: 20rpx;

	}

	

	.upload-btn {

		background-color: #409EFF;

		color: #fff;

	}

	

	.upload-btn:disabled {

		background-color: #a0cfff;

	}

	

	.export-btn {

		background-color: #67C23A;

		color: #fff;

	}

	

	.export-btn:disabled {

		background-color: #b3e19d;

	}

	

	.custom-data-info {

		background-color: #f8f9fa;

		border-radius: 12rpx;

		padding: 25rpx;

		margin-bottom: 20rpx;

	}

	

	.info-title {

		font-size: 28rpx;

		color: #333;

		font-weight: bold;

		display: block;

		margin-bottom: 20rpx;

	}

	

	.info-item {

		display: flex;

		justify-content: space-between;

		align-items: center;

		margin-bottom: 12rpx;

	}

	

	.info-label {

		font-size: 26rpx;

		color: #666;

	}

	

	.info-value {

		font-size: 26rpx;

		color: #333;

		font-weight: 500;

	}

	

	.info-value.valid {

		color: #67C23A;

	}

	

	.info-value.invalid {

		color: #F56C6C;

	}

	

	.custom-data-warning {

		background-color: #fdf6ec;

		border-left: 8rpx solid #e6a23c;

		border-radius: 8rpx;

		padding: 20rpx;

	}

	

	.warning-title {

		font-size: 26rpx;

		color: #e6a23c;

		font-weight: bold;

		display: block;

		margin-bottom: 15rpx;

	}

	

	.warning-text {

		font-size: 24rpx;

		color: #e6a23c;

		display: block;

		margin-bottom: 8rpx;

		line-height: 1.4;

	}

	

	.data-source-tips {

		padding: 20rpx 30rpx;

		background-color: #f8f9fa;

		border-top: 1rpx solid #f0f0f0;

	}

	

	.data-source-tips .tip-text {

		font-size: 22rpx;

		color: #999;

		display: block;

		margin-bottom: 8rpx;

		line-height: 1.4;

	}

	

	/* 搜索开关行样式 */

	.search-switches-row {

		display: flex;

		justify-content: space-between;

		padding: 30rpx;

		border-bottom: 1rpx solid #f5f5f5;

	}

	

	.search-switch-item {

		flex: 1;

		display: flex;

		flex-direction: column;

		align-items: center;

		padding: 0 15rpx;

	}

	

	.search-switch-item.disabled {

		opacity: 0.6;

	}

	

	.switch-info {

		display: flex;

		flex-direction: column;

		align-items: center;

		width: 100%;

	}

	

	.switch-name {

		font-size: 26rpx;

		color: #333;

		font-weight: 500;

		margin-bottom: 10rpx;

		text-align: center;

	}

	

	.switch-desc-small {

		font-size: 22rpx;

		color: #999;

		margin-bottom: 15rpx;

		text-align: center;

	}

	

	.dev-tag-small {

		display: inline-block;

		font-size: 20rpx;

		color: #fff;

		background-color: #909399;

		padding: 3rpx 10rpx;

		border-radius: 15rpx;

		margin-bottom: 15rpx;

	}

	

	.warning-text-small {

		font-size: 20rpx;

		color: #F56C6C;

		margin-top: 5rpx;

	}

	

	.search-switch {

		transform: scale(0.8);

	}

	

	/* 搜索数据状态 */

	.search-data-status {

		padding: 20rpx 30rpx;

		background-color: #f8f9fa;

		border-top: 1rpx solid #f0f0f0;

		display: flex;

		justify-content: space-between;

		align-items: center;

	}

	

	.search-status-text {

		font-size: 24rpx;

		flex: 1;

	}

	

	/* 设置项样式 */

	.setting-item {

		display: flex;

		align-items: center;

		justify-content: space-between;

		padding: 30rpx;

		border-bottom: 1rpx solid #f5f5f5;

	}

	

	.setting-item:last-child {

		border-bottom: none;

	}

	

	.setting-item.disabled {

		opacity: 0.6;

	}

	

	.setting-info {

		flex: 1;

		display: flex;

		flex-direction: column;

	}

	

	.setting-name {

		font-size: 30rpx;

		color: #333;

		font-weight: 500;

		margin-bottom: 8rpx;

	}

	

	.setting-desc {

		font-size: 26rpx;

		color: #999;

		margin-bottom: 5rpx;

	}

	

	.data-info {

		font-size: 24rpx;

		color: #666;

		margin-top: 5rpx;

	}

	

	.warning-text {

		font-size: 24rpx;

		color: #E6A23C;

		margin-top: 5rpx;

		font-weight: 500;

	}

	

	.red-text {

		font-size: 24rpx;

		color: #ff0000;

		margin-top: 5rpx;

		font-weight: 500;

	}

	

	.dev-tag {

		display: inline-block;

		font-size: 22rpx;

		color: #fff;

		background-color: #909399;

		padding: 4rpx 12rpx;

		border-radius: 20rpx;

		margin-top: 8rpx;

		align-self: flex-start;

	}

	

	.tip-text {

		font-size: 20rpx;

		color: red;

		line-height: 1.5;

	}

	

	/* 预测数据状态 */

	.guess-data-status {

		padding: 20rpx 30rpx;

		background-color: #f8f9fa;

		border-top: 1rpx solid #f0f0f0;

		display: flex;

		justify-content: space-between;

		align-items: center;

	}

	

	.guess-status-text {

		font-size: 24rpx;

		flex: 1;

	}

	

	.status-success {

		color: #67C23A;

	}

	

	.status-error {

		color: #F56C6C;

	}

	

	.status-loading {

		color: #409EFF;

	}

	

	.retry-btn {

		background-color: #409EFF;

		color: #fff;

		border-radius: 6rpx;

		font-size: 22rpx;

		padding: 8rpx 16rpx;

		margin-left: 20rpx;

	}

	

	.retry-btn:disabled {

		opacity: 0.5;

	}

	

	/* 自动更新状态样式 */

	.auto-update-status {

		margin-top: 10rpx;

		padding: 10rpx 0;

	}

	

	.auto-update-status-text {

		font-size: 24rpx;

		display: block;

		margin-bottom: 5rpx;

	}

	

	.auto-update-time {

		font-size: 22rpx;

		color: #999;

		display: block;

	}

	

	/* 手动检查更新按钮 */

	.manual-update-check {

		padding: 0 30rpx 30rpx 30rpx;

	}

	

	.manual-update-check .secondary-btn {

		width: 100%;

		background-color: #409EFF;

		color: #fff;

		border-radius: 10rpx;

		font-size: 26rpx;

		padding: 20rpx;

	}

	

	.manual-update-check .secondary-btn:disabled {

		background-color: #a0cfff;

	}

	

	/* 按钮样式 */

	.danger-btn {

		background-color: #FA5151;

		color: #fff;

		border-radius: 10rpx;

		font-size: 26rpx;

		padding: 15rpx 25rpx;

		display: flow;

	}

	

	.danger-btn:disabled {

		opacity: 0.5;

	}

	

	.secondary-btn {

		background-color: #f0f0f0;

		color: #666;

		border-radius: 10rpx;

		font-size: 26rpx;

		padding: 15rpx 25rpx;

	}

	

	.secondary-btn:disabled {

		opacity: 0.5;

	}

	

	.setting-switch {

		transform: scale(0.8);

	}

	

	/* 数据状态区域 */

	.data-status-section {

		background-color: #fff;

		border-radius: 16rpx;

		padding: 30rpx;

		margin-bottom: 30rpx;

		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);

	}

	

	.status-title {

		font-size: 28rpx;

		color: #333;

		font-weight: bold;

		display: block;

		margin-bottom: 20rpx;

	}

	

	.status-list {

		display: flex;

		flex-direction: column;

		gap: 15rpx;

	}

	

	.status-item {

		display: flex;

		justify-content: space-between;

		align-items: center;

	}

	

	.status-label {

		font-size: 26rpx;

		color: #666;

	}

	

	.status-value {

		font-size: 26rpx;

		color: #333;

		font-weight: 500;

	}

	

	.loading-status {

		color: #409EFF;

	}

	

	/* 验证结果弹窗 */

	.validation-modal {

		position: fixed;

		top: 0;

		left: 0;

		right: 0;

		bottom: 0;

		background-color: rgba(0, 0, 0, 0.7);

		display: flex;

		align-items: center;

		justify-content: center;

		z-index: 1001;

	}

	

	.validation-content {

		width: 80%;

		max-height: 70%;

		background-color: #fff;

		border-radius: 20rpx;

		padding: 40rpx;

		display: flex;

		flex-direction: column;

	}

	

	.validation-title {

		font-size: 32rpx;

		color: #333;

		font-weight: bold;

		text-align: center;

		margin-bottom: 30rpx;

	}

	

	.validation-result {

		flex: 1;

		overflow-y: auto;

		padding: 20rpx;

		border-radius: 12rpx;

		margin-bottom: 30rpx;

	}

	

	.validation-result.valid {

		background-color: #f0f9eb;

		border: 1rpx solid #e1f3d8;

	}

	

	.validation-result.invalid {

		background-color: #fef0f0;

		border: 1rpx solid #fde2e2;

	}

	

	.result-text {

		font-size: 28rpx;

		display: block;

		margin-bottom: 20rpx;

		text-align: center;

		font-weight: bold;

	}

	

	.validation-result.valid .result-text {

		color: #67C23A;

	}

	

	.validation-result.invalid .result-text {

		color: #F56C6C;

	}

	

	.result-details {

		display: flex;

		flex-direction: column;

		gap: 12rpx;

	}

	

	.detail-item {

		font-size: 24rpx;

		color: #666;

		line-height: 1.4;

	}

	

	.validation-actions {

		display: flex;

		gap: 20rpx;

	}

	

	.cancel-btn, .confirm-btn {

		flex: 1;

		border-radius: 10rpx;

		font-size: 26rpx;

		padding: 20rpx;

	}

	

	.cancel-btn {

		background-color: #f0f0f0;

		color: #666;

	}

	

	.confirm-btn {

		background-color: #409EFF;

		color: #fff;

	}

	

	.confirm-btn:disabled {

		background-color: #a0cfff;

	}

	

	/* 上传进度提示 */

	.upload-progress {

		position: fixed;

		top: 0;

		left: 0;

		right: 0;

		bottom: 0;

		background-color: rgba(0, 0, 0, 0.7);

		display: flex;

		align-items: center;

		justify-content: center;

		z-index: 1000;

	}

	

	.upload-progress .progress-content {

		width: 70%;

		background-color: #fff;

		border-radius: 20rpx;

		padding: 40rpx;

		text-align: center;

	}

	

	.upload-progress .progress-title {

		font-size: 32rpx;

		color: #333;

		font-weight: bold;

		display: block;

		margin-bottom: 20rpx;

	}

	

	.upload-progress .progress-tip {

		font-size: 26rpx;

		color: #666;

		display: block;

		margin-bottom: 30rpx;

		line-height: 1.5;

	}

	

	.upload-progress .progress-bar {

		width: 100%;

		height: 20rpx;

		background-color: #f0f0f0;

		border-radius: 10rpx;

		margin-bottom: 20rpx;

		overflow: hidden;

	}

	

	.upload-progress .progress-inner {

		height: 100%;

		background-color: #409EFF;

		border-radius: 10rpx;

		transition: width 0.3s ease;

	}

	

	.upload-progress .progress-text {

		font-size: 24rpx;

		color: #666;

		display: block;

	}

	

	/* 下载进度提示 */

	.download-progress {

		position: fixed;

		top: 0;

		left: 0;

		right: 0;

		bottom: 0;

		background-color: rgba(0, 0, 0, 0.7);

		display: flex;

		align-items: center;

		justify-content: center;

		z-index: 1000;

	}

	

	.download-progress .progress-content {

		width: 80%;

		background-color: #fff;

		border-radius: 20rpx;

		padding: 40rpx;

		text-align: center;

	}

	

	.download-progress .progress-title {

		font-size: 32rpx;

		color: #333;

		font-weight: bold;

		display: block;

		margin-bottom: 20rpx;

	}

	

	.download-progress .progress-tip {

		font-size: 26rpx;

		color: #666;

		display: block;

		margin-bottom: 30rpx;

		line-height: 1.5;

	}

	

	.download-progress .progress-bar {

		width: 100%;

		height: 20rpx;

		background-color: #f0f0f0;

		border-radius: 10rpx;

		margin-bottom: 20rpx;

		overflow: hidden;

	}

	

	.download-progress .progress-inner {

		height: 100%;

		background-color: #409EFF;

		border-radius: 10rpx;

		transition: width 0.3s ease;

	}

	

	.download-progress .progress-text {

		font-size: 24rpx;

		color: #666;

		display: block;

		margin-bottom: 30rpx;

	}

	

	.download-progress .cancel-btn {

		background-color: #f0f0f0;

		color: #666;

		border-radius: 10rpx;

		font-size: 26rpx;

		padding: 20rpx 40rpx;

		width: 100%;

	}

	

	/* 自动更新设置弹窗 */

	.auto-update-modal {

		position: fixed;

		top: 0;

		left: 0;

		right: 0;

		bottom: 0;

		background-color: rgba(0, 0, 0, 0.7);

		display: flex;

		align-items: center;

		justify-content: center;

		z-index: 1001;

	}

	

	.modal-content {

		width: 80%;

		max-height: 80%;

		background-color: #fff;

		border-radius: 20rpx;

		padding: 40rpx;

		display: flex;

		flex-direction: column;

	}

	

	.modal-title {

		font-size: 32rpx;

		color: #333;

		font-weight: bold;

		text-align: center;

		margin-bottom: 30rpx;

	}

	

	.modal-settings {

		flex: 1;

		overflow-y: auto;

		margin-bottom: 20rpx;

	}

	

	.modal-settings .setting-item {

		display: flex;

		justify-content: space-between;

		align-items: center;

		padding: 20rpx 0;

		border-bottom: 1rpx solid #f0f0f0;

	}

	

	.modal-settings .setting-item:last-child {

		border-bottom: none;

	}

	

	.setting-label {

		font-size: 28rpx;

		color: #333;

	}

	

	.frequency-picker {

		width: 250rpx;

	}

	

	.picker-view {

		display: flex;

		justify-content: space-between;

		align-items: center;

		padding: 15rpx 20rpx;

		background-color: #f5f5f5;

		border-radius: 8rpx;

		font-size: 26rpx;

		color: #333;

	}

	

	.picker-arrow {

		color: #999;

		font-size: 20rpx;

		margin-left: 10rpx;

	}

	

	.modal-tips {

		background-color: #f8f9fa;

		border-radius: 8rpx;

		padding: 20rpx;

		margin-bottom: 30rpx;

	}

	

	.modal-tips .tip-text {

		font-size: 22rpx;

		color: #999;

		display: block;

		margin-bottom: 8rpx;

		line-height: 1.4;

	}

	

	.modal-actions {

		display: flex;

		gap: 20rpx;

	}

	

	/* 免责声明模态框样式 */

	.disclaimer-modal {

	  position: fixed;

	  top: 0;

	  left: 0;

	  right: 0;

	  bottom: 0;

	  background-color: rgba(0, 0, 0, 0.7);

	  display: flex;

	  align-items: center;

	  justify-content: center;

	  z-index: 1002;

	  padding: 40rpx;

	}

	

	.disclaimer-content {

	  background-color: #fff;

	  border-radius: 16rpx;

	  width: 100%;

	  max-width: 700rpx;

	  max-height: 80vh;

	  display: flex;

	  flex-direction: column;

	  overflow: hidden;

	}

	

	.disclaimer-header {

	  display: flex;

	  justify-content: space-between;

	  align-items: center;

	  padding: 30rpx;

	  border-bottom: 1rpx solid #f0f0f0;

	  background-color: #fafafa;

	}

	

	.disclaimer-title {

	  font-size: 32rpx;

	  font-weight: bold;

	  color: #333;

	  display: block;

	  flex: 1;

	}

	

	.close-btn {

	  width: 60rpx;

	  height: 60rpx;

	  border-radius: 50%;

	  background-color: #f0f0f0;

	  color: #666;

	  font-size: 36rpx;

	  display: flex;

	  align-items: center;

	  justify-content: center;

	}

	

	.disclaimer-body {

	  flex: 1;

	  padding: 30rpx;

	  max-height: 60vh;

	}

	

	.disclaimer-section {

	  margin-bottom: 25rpx;

	}

	

	.section-title {

	  font-size: 28rpx;

	  color: #333;

	  font-weight: bold;

	  display: block;

	  margin-bottom: 10rpx;

	}

	

	.section-content {

	  font-size: 26rpx;

	  color: #666;

	  line-height: 1.6;

	  display: block;

	}

	

	.disclaimer-footer {

	  padding: 20rpx 30rpx;

	  border-top: 1rpx solid #f0f0f0;

	  display: flex;

	  gap: 15rpx;

	}

	

	.agree-btn {

	  flex: 2;

	  background-color: #409EFF;

	  color: #fff;

	  border-radius: 10rpx;

	  font-size: 26rpx;

	  padding: 20rpx;

	}

	

	.cancel-btn {

	  flex: 1;

	  background-color: #f0f0f0;

	  color: #666;

	  border-radius: 10rpx;

	  font-size: 26rpx;

	  padding: 20rpx;

	}

	

	/* 提示区域 */

	.tip-section {

		text-align: center;

		padding: 20rpx;

	}

	/* ===================== 主题设置样式 ===================== */

	.theme-options {

		display: flex;

		flex-direction: column;

		gap: 20rpx;

		padding: 30rpx;

	}

	.theme-item {

		display: flex;

		align-items: center;

		padding: 25rpx;

		border: 2rpx solid #f0f0f0;

		border-radius: 12rpx;

		background-color: #fff;

		transition: all 0.3s;

	}

	.theme-item.active {

		border-color: #409EFF;

		background-color: #e8f4ff;

	}

	.theme-preview {

		width: 80rpx;

		height: 80rpx;

		border-radius: 8rpx;

		margin-right: 20rpx;

		position: relative;

		overflow: hidden;

		border: 1rpx solid #ddd;

	}

		/* 液态玻璃预览（浅色）—— 用 .theme-preview 前缀限定，避免样式外溢到页面容器 */
	.theme-preview.theme-liquid {
		background-color: #e5eaf3;
	}
	.theme-preview.theme-liquid .preview-box {
		position: absolute;
		top: 12rpx;
		left: 12rpx;
		right: 12rpx;
		height: 30rpx;
		background-image: linear-gradient(180deg, rgba(255,255,255,0.95) 0%, rgba(255,255,255,0.62) 100%);
		background-color: rgba(255,255,255,0.72);
		border: 1rpx solid rgba(255,255,255,0.92);
		border-radius: 10rpx;
		box-shadow: 0 3rpx 8rpx rgba(31,41,71,0.18);
	}
	.theme-preview.theme-liquid .preview-text {
		position: absolute;
		bottom: 14rpx;
		left: 12rpx;
		right: 12rpx;
		height: 16rpx;
		background-color: rgba(255,255,255,0.78);
		border: 1rpx solid rgba(255,255,255,0.92);
		border-radius: 999rpx;
		box-shadow: 0 2rpx 6rpx rgba(31,41,71,0.12);
	}
	/* 液态玻璃预览（深色） */
	.theme-preview.theme-liquidDark {
		background-color: #101219;
	}
	.theme-preview.theme-liquidDark .preview-box {
		position: absolute;
		top: 12rpx;
		left: 12rpx;
		right: 12rpx;
		height: 30rpx;
		background-image: linear-gradient(180deg, rgba(255,255,255,0.20) 0%, rgba(255,255,255,0.08) 100%);
		background-color: rgba(255,255,255,0.10);
		border: 1rpx solid rgba(255,255,255,0.28);
		border-radius: 10rpx;
		box-shadow: 0 3rpx 8rpx rgba(0,0,0,0.45);
	}
	.theme-preview.theme-liquidDark .preview-text {
		position: absolute;
		bottom: 14rpx;
		left: 12rpx;
		right: 12rpx;
		height: 16rpx;
		background-color: rgba(255,255,255,0.16);
		border: 1rpx solid rgba(255,255,255,0.28);
		border-radius: 999rpx;
		box-shadow: 0 2rpx 6rpx rgba(0,0,0,0.35);
	}

	/* 简约风格预览 */

	.theme-simple {

		background-color: #f5f5f5;

	}

	.theme-simple .preview-box {

		position: absolute;

		top: 15rpx;

		left: 15rpx;

		right: 15rpx;

		height: 25rpx;

		background-color: #fff;

		border-radius: 4rpx;

		box-shadow: 0 2rpx 4rpx rgba(0,0,0,0.1);

	}

	.theme-simple .preview-text {

		position: absolute;

		bottom: 15rpx;

		left: 15rpx;

		right: 15rpx;

		height: 15rpx;

		background-color: #409EFF;

		border-radius: 4rpx;

	}/* 界园风格预览 - 粉绿渐变风格 */

	.theme-jieyuan {

		background: linear-gradient(135deg, #1a1a1a 0%, #0f0f0f 100%);

		position: relative;

		overflow: hidden;

	}

	/* 界园风格装饰 - 多重渐变光环 */

	.theme-jieyuan::before {

		content: '';

		position: absolute;

		top: 5rpx;

		right: 5rpx;

		width: 35rpx;

		height: 35rpx;

		border: 3rpx solid #e25884;

		border-radius: 50%;

		box-shadow: 

			0 0 10rpx rgba(226, 88, 132, 0.6),

			0 0 20rpx rgba(226, 88, 132, 0.3),

			inset 0 0 8rpx rgba(226, 88, 132, 0.3);

		animation: glow-pink 2s ease-in-out infinite alternate;

	}

	.theme-jieyuan::after {

		content: '';

		position: absolute;

		bottom: 8rpx;

		left: 8rpx;

		width: 30rpx;

		height: 30rpx;

		border: 3rpx solid #399383;

		border-radius: 50%;

		box-shadow: 

			0 0 10rpx rgba(57, 147, 131, 0.6),

			0 0 20rpx rgba(57, 147, 131, 0.3),

			inset 0 0 8rpx rgba(57, 147, 131, 0.3);

		animation: glow-green 2s ease-in-out infinite alternate;

	}

	.theme-jieyuan .preview-box {

		position: absolute;

		top: 15rpx;

		left: 15rpx;

		right: 15rpx;

		height: 25rpx;

		background: linear-gradient(90deg, rgba(226, 88, 132, 0.3) 0%, rgba(57, 147, 131, 0.3) 100%);

		border-radius: 8rpx;

		border: 1rpx solid rgba(226, 88, 132, 0.4);

		box-shadow: 

			0 0 10rpx rgba(226, 88, 132, 0.2),

			inset 0 0 10rpx rgba(57, 147, 131, 0.1);

	}

	.theme-jieyuan .preview-text {

		position: absolute;

		bottom: 15rpx;

		left: 15rpx;

		right: 15rpx;

		height: 15rpx;

		background: linear-gradient(90deg, #e25884 0%, #f4a1c0 50%, #399383 100%);

		border-radius: 6rpx;

		box-shadow: 

			0 0 12rpx rgba(226, 88, 132, 0.5),

			0 0 24rpx rgba(57, 147, 131, 0.3);

		animation: gradient-shift 3s ease infinite;

	}

	@keyframes glow-pink {

		from { box-shadow: 0 0 8rpx rgba(226, 88, 132, 0.4), 0 0 15rpx rgba(226, 88, 132, 0.2); }

		to { box-shadow: 0 0 15rpx rgba(226, 88, 132, 0.7), 0 0 30rpx rgba(226, 88, 132, 0.4); }

	}

	@keyframes glow-green {

		from { box-shadow: 0 0 8rpx rgba(57, 147, 131, 0.4), 0 0 15rpx rgba(57, 147, 131, 0.2); }

		to { box-shadow: 0 0 15rpx rgba(57, 147, 131, 0.7), 0 0 30rpx rgba(57, 147, 131, 0.4); }

	}

	@keyframes gradient-shift {

		0%, 100% { background: linear-gradient(90deg, #e25884 0%, #f4a1c0 50%, #399383 100%); }

		50% { background: linear-gradient(90deg, #399383 0%, #e25884 50%, #f4a1c0 100%); }

	}

	.theme-info {

		flex: 1;

		display: flex;

		flex-direction: column;

	}

	.theme-label {

		font-size: 28rpx;

		color: #333;

		font-weight: 500;

		margin-bottom: 8rpx;

	}

	.theme-desc {

		font-size: 24rpx;

		color: #999;

	}

	.theme-radio {

		width: 40rpx;

		height: 40rpx;

		border: 2rpx solid #ddd;

		border-radius: 50%;

		display: flex;

		align-items: center;

		justify-content: center;

		margin-left: 20rpx;

	}

	.theme-item.active .theme-radio {

		border-color: #409EFF;

	}

	.theme-item.active .radio-inner {

		width: 20rpx;

		height: 20rpx;

		border-radius: 50%;

		background-color: #409EFF;

	}

	.theme-tip {

		font-size: 22rpx;

		color: #999;

		padding: 0 30rpx 20rpx;

		display: block;

	}/* ========== 界园风格 ========== */

	.container.theme-jieyuan {

		background-color: #FAF3E0;

	}

	.container.theme-jieyuan .container { background: transparent; }

	.container.theme-jieyuan .section { background: rgba(255, 255, 255, 0.9); border: 1rpx solid rgba(226, 88, 132, 0.15); }

	.container.theme-jieyuan .section-title {

		background: linear-gradient(90deg, #e25884, #399383);

		-webkit-background-clip: text;

		-webkit-text-fill-color: transparent;

	}

	.container.theme-jieyuan .data-source-item { border-bottom-color: rgba(226, 88, 132, 0.1); }

	.container.theme-jieyuan .data-source-label { color: #333; }

	.container.theme-jieyuan .data-source-desc { color: #888; }

	.container.theme-jieyuan .upload-btn,

	.container.theme-jieyuan .export-btn,

	.container.theme-jieyuan .update-btn {

		background: linear-gradient(90deg, #e25884, #399383);

		color: #fff;

		border: none;

	}

	.container.theme-jieyuan .theme-label { color: #333; }

	.container.theme-jieyuan .theme-desc { color: #888; }

	.container.theme-jieyuan .version-text { color: #888; }

	.container.theme-jieyuan .theme-radio { border-color: rgba(226, 88, 132, 0.3); }

	.container.theme-jieyuan .theme-item.active .theme-radio { border-color: #e25884; }

	.container.theme-jieyuan .info-title { color: #333; }

	.container.theme-jieyuan .info-item { color: #666; }

	.container.theme-jieyuan .loading-text { color: #e25884; }

	.container.theme-jieyuan .switch-section .section-title { color: #333; }

	.container.theme-jieyuan .switch-row { border-bottom-color: rgba(226, 88, 132, 0.1); }

	.container.theme-jieyuan .switch-label { color: #333; }

	.container.theme-jieyuan .switch-desc { color: #888; }

	.container.theme-jieyuan .theme-tip { color: #888; }

	.container.theme-jieyuan .data-source-info { border-top-color: rgba(226, 88, 132, 0.1); }

	.container.theme-jieyuan .info-label { color: #888; }

	.container.theme-jieyuan .info-value { color: #333; }

	.container.theme-jieyuan .progress-bar { background-color: rgba(226, 88, 132, 0.1); }

	.container.theme-jieyuan .progress-inner { background: linear-gradient(90deg, #e25884, #399383); }

	.container.theme-jieyuan .data-source-warning { color: #e25884; }

	/* 隐私协议弹窗 */

	

	

	

	

	

	

	

	

	.privacy-btn.cancel { background-color: #f0f0f0; }

	.privacy-btn.confirm { background-color: #409EFF; }

	.privacy-btn .btn-text { font-size: 28rpx; color: #666; }

	.privacy-btn.confirm .btn-text { color: #fff; }.data-source-item {
		transition: transform 0.22s ease, background-color 0.2s ease, box-shadow 0.2s ease;
		will-change: transform;
	}

	/* ==========================================================================
	   主题预览色板（工业浅色 ak / 工业深色 ark）——取值与 common/ak-theme.css 令牌一致
	   说明：深色色板覆盖文件前部的旧「黑橘渐变」预览样式（同权重靠后生效）
	   ========================================================================== */
	.theme-preview.theme-ak {
		background-color: #efefec;
		border-color: rgba(27, 29, 31, 0.18);
	}
	.theme-preview.theme-ak .preview-box {
		position: absolute;
		top: 15rpx;
		left: 15rpx;
		right: 15rpx;
		height: 25rpx;
		background-color: #fdfdfb;
		background-image: none;
		border: 1rpx solid rgba(27, 29, 31, 0.12);
		border-radius: 2rpx;
		box-shadow: none;
	}
	.theme-preview.theme-ak .preview-text {
		position: absolute;
		bottom: 15rpx;
		left: 15rpx;
		right: 15rpx;
		height: 15rpx;
		background-color: #f1c644;
		background-image: none;
		border-radius: 2rpx;
	}
	/* 主题预览色板 · 工业深色（取值与 common/ak-theme.css 令牌一致） */
	.theme-preview.theme-ark {
		background-color: #131415;
		border-color: rgba(255, 255, 255, 0.18);
	}
	.theme-preview.theme-ark .preview-box {
		position: absolute;
		top: 15rpx;
		left: 15rpx;
		right: 15rpx;
		height: 25rpx;
		background-color: #1b1d1e;
		background-image: none;
		border: 1rpx solid rgba(255, 255, 255, 0.14);
		border-radius: 2rpx;
		box-shadow: none;
	}
	.theme-preview.theme-ark .preview-text {
		position: absolute;
		bottom: 15rpx;
		left: 15rpx;
		right: 15rpx;
		height: 15rpx;
		background-color: #ff6a26;
		background-image: none;
		border-radius: 2rpx;
	}

	/* ========== 首页功能展示样式选择器 ========== */
	.layout-picker {
		display: flex;
		gap: 18rpx;
		padding: 0 24rpx 24rpx;
	}
	.layout-option {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 18rpx 14rpx 16rpx;
		border: 2rpx solid var(--ak-hairline, #e5e5e5);
		border-radius: 12rpx;
		background-color: #fff;
	}
	.layout-option.active {
		border-color: var(--ak-color-primary, #4aabea);
		background-color: rgba(74, 171, 234, 0.06);
	}
	.layout-preview {
		width: 100%;
		height: 88rpx;
		margin-bottom: 12rpx;
	}
	/* 宫格：2×2 */
	.lp-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		grid-template-rows: 1fr 1fr;
		gap: 6rpx;
	}
	.lp-grid .lp-a,
	.lp-grid .lp-b,
	.lp-grid .lp-c,
	.lp-grid .lp-d {
		background-color: var(--ak-color-primary, #4aabea);
		opacity: 0.7;
		border-radius: 4rpx;
	}
	/* 列表：三行横条 */
	.lp-list {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.lp-list .lp-a,
	.lp-list .lp-b,
	.lp-list .lp-c {
		flex: 1;
		background-color: var(--ak-color-primary, #4aabea);
		opacity: 0.7;
		border-radius: 4rpx;
	}
	.lp-list .lp-d {
		display: none;
	}
	/* 极简：一个主块 + 一条收纳 */
	.lp-minimal {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.lp-minimal .lp-a {
		flex: 2;
		background-color: var(--ak-color-primary, #4aabea);
		opacity: 0.7;
		border-radius: 4rpx;
	}
	.lp-minimal .lp-b {
		flex: 1;
		background-color: var(--ak-text-muted, #bbb);
		opacity: 0.45;
		border-radius: 4rpx;
	}
	.lp-minimal .lp-c,
	.lp-minimal .lp-d {
		display: none;
	}
	.layout-name {
		font-size: 24rpx;
		font-weight: 600;
		line-height: 1.4;
		color: var(--ak-text-primary, #333);
	}
	.layout-option.active .layout-name {
		color: var(--ak-color-primary, #4aabea);
	}
	.layout-tip {
		margin-top: 2rpx;
		font-size: 19rpx;
		line-height: 1.4;
		text-align: center;
		color: var(--ak-text-muted, #999);
	}

	/* ===== 桌面小组件入口 ===== */
	.ak-widget-entry-btn {
		flex-shrink: 0;
		margin-left: 20rpx;
		padding: 10rpx 26rpx;
		border-radius: 999rpx;
		font-size: 24rpx;
		line-height: 1.4;
		color: #fff;
		background-color: #409EFF;
	}
	/* ===== 桌面小组件徽标图例 ===== */
	.ak-widget-legend {
		margin-top: 12rpx;
		padding: 20rpx 24rpx;
		border-radius: 16rpx;
		background-color: rgba(0, 0, 0, 0.03);
	}

	.ak-widget-legend-title {
		font-size: 24rpx;
		color: var(--ak-text-muted, #999);
	}

	.ak-widget-legend-row {
		display: flex;
		align-items: center;
		margin-top: 14rpx;
	}

	/* 深色底托：与小组件卡片底色 #1B1D1E 一致，浅色页面上也能看清白星 */
	.ak-widget-legend-icon {
		width: 48rpx;
		height: 48rpx;
		padding: 10rpx;
		margin-right: 16rpx;
		border-radius: 10rpx;
		background-color: #1B1D1E;
		flex-shrink: 0;
	}

	.ak-widget-legend-text {
		font-size: 24rpx;
		line-height: 1.5;
		color: var(--ak-text-muted, #999);
	}

	/* 抽卡结果的外圈示意色卡（圆环，不是图片）；同样给深色底托，淡白圈才看得见 */
	.ak-widget-legend-ring {
		width: 48rpx;
		height: 48rpx;
		margin-right: 16rpx;
		border-radius: 50%;
		box-sizing: border-box;
		background-color: #1B1D1E;
		flex-shrink: 0;
	}

	.ak-widget-legend-ring1 {
		border: 4rpx solid rgba(255, 255, 255, 0.4);
	}

	.ak-widget-legend-ring2 {
		border: 4rpx solid #E8B84B;
	}

	.ak-widget-legend-sub {
		display: block;
		margin-top: 16rpx;
		font-size: 24rpx;
		color: var(--ak-text-muted, #999);
		opacity: 0.8;
	}

	.ak-widget-legend-note {
		display: block;
		margin-top: 14rpx;
		font-size: 22rpx;
		line-height: 1.6;
		color: var(--ak-text-muted, #999);
	}


	/* ===================== 干员头像个性化 ===================== */
	.avp-head {
		align-items: flex-start;
	}

	.avp-add-btn {
		flex-shrink: 0;
		margin-left: 20rpx;
		padding: 10rpx 22rpx;
		font-size: 24rpx;
		color: var(--ak-color-primary, #007aff);
		border: 1rpx solid var(--ak-color-primary, #007aff);
		border-radius: 999rpx;
	}

	.avp-seg {
		display: flex;
		margin: 0 30rpx 24rpx;
		border-radius: 12rpx;
		overflow: hidden;
		background: var(--ak-surface-muted, #f2f3f5);
	}

	.avp-seg-sm {
		margin: 0;
		flex: 1;
	}
	.avp-seg-item {
		flex: 1;
		height: 72rpx;
		line-height: 72rpx;
		text-align: center;
		font-size: 27rpx;
		color: var(--ak-text-secondary, #666);
	}

	.avp-seg-item.active {
		background: var(--ak-color-primary, #007aff);
		color: #fff;
		font-weight: 600;
	}

	.avp-empty {
		padding: 26rpx 30rpx;
		font-size: 24rpx;
		color: var(--ak-text-muted, #999);
		line-height: 1.6;
	}

	.avp-rule {
		margin: 0 30rpx 20rpx;
		padding: 22rpx 24rpx;
		border-radius: 14rpx;
		background: var(--ak-surface-muted, #f7f8fa);
	}

	.avp-rule-top {
		display: flex;
		align-items: flex-start;
	}

	.avp-rule-idx {
		flex-shrink: 0;
		width: 36rpx;
		height: 36rpx;
		line-height: 36rpx;
		margin-right: 12rpx;
		border-radius: 50%;
		text-align: center;
		font-size: 20rpx;
		color: var(--ak-text-muted, #999);
		background: rgba(127, 127, 127, 0.12);
	}

	.avp-rule-level {
		flex-shrink: 0;
		margin-right: 14rpx;
		padding: 2rpx 12rpx;
		border-radius: 6rpx;
		font-size: 20rpx;
		line-height: 32rpx;
	}

	.avp-rule-level.lv1 {
		color: #6b7280;
		background: rgba(107, 114, 128, 0.14);
	}

	.avp-rule-level.lv2 {
		color: #b07a12;
		background: rgba(241, 198, 68, 0.22);
	}

	.avp-rule-summary {
		flex: 1;
		font-size: 25rpx;
		line-height: 1.55;
		color: var(--ak-text-primary, #333);
	}

	.avp-rule-ops {
		display: flex;
		justify-content: flex-end;
		margin-top: 14rpx;
	}

	.avp-op {
		margin-left: 26rpx;
		font-size: 24rpx;
		color: var(--ak-text-secondary, #666);
	}

	.avp-op.danger {
		color: #e34d4d;
	}

	.avp-name-list {
		display: flex;
		flex-wrap: wrap;
		padding: 0 30rpx 20rpx;
	}

	.avp-name-chip {
		display: flex;
		align-items: center;
		margin: 0 14rpx 14rpx 0;
		padding: 10rpx 18rpx;
		border-radius: 999rpx;
		background: var(--ak-surface-muted, #f2f3f5);
	}

	.avp-name-text {
		font-size: 24rpx;
		color: var(--ak-text-primary, #333);
	}

	.avp-name-x {
		margin-left: 10rpx;
		font-size: 20rpx;
		color: var(--ak-text-muted, #999);
	}

	.pref-share-entry {
		flex: none;
		width: 140rpx;
		margin-right: 0;
	}

	.avp-io-row {
		display: flex;
		padding: 0 30rpx 30rpx;
	}

	.avp-io-btn {
		flex: 1;
		margin-right: 16rpx;
		height: 72rpx;
		line-height: 72rpx;
		text-align: center;
		font-size: 26rpx;
		border-radius: 12rpx;
		color: var(--ak-color-primary, #007aff);
		background: var(--ak-surface-muted, #f2f3f5);
	}

	.avp-io-btn:last-child {
		margin-right: 0;
	}

	.avp-io-btn.danger {
		color: #e34d4d;
	}

	/* ---------- 弹窗 ---------- */
	.avp-mask {
		position: fixed;
		left: 0;
		top: 0;
		right: 0;
		bottom: 0;
		z-index: 9999;
		display: flex;
		align-items: flex-end;
		justify-content: center;
		background: rgba(0, 0, 0, 0.45);
	}

	.avp-modal {
		width: 100%;
		max-height: 84vh;
		display: flex;
		flex-direction: column;
		border-radius: 24rpx 24rpx 0 0;
		background: var(--ak-surface-panel, #fff);
		padding-bottom: env(safe-area-inset-bottom);
	}

	.avp-modal-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 28rpx 30rpx;
		border-bottom: 1rpx solid var(--ak-hairline-soft, #eee);
	}

	.avp-modal-title {
		font-size: 30rpx;
		font-weight: 600;
		color: var(--ak-text-primary, #333);
	}

	.avp-modal-x {
		font-size: 30rpx;
		color: var(--ak-text-muted, #999);
		padding: 0 10rpx;
	}

	.avp-modal-body {
		flex: 1;
		max-height: 56vh;
		padding: 10rpx 30rpx;
		box-sizing: border-box;
	}

	.avp-group {
		padding: 16rpx 0;
		border-bottom: 1rpx solid var(--ak-hairline-soft, #f2f2f2);
	}

	.avp-group-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 12rpx;
	}

	.avp-group-title {
		font-size: 26rpx;
		font-weight: 600;
		color: var(--ak-text-primary, #333);
	}

	.avp-group-clear {
		font-size: 22rpx;
		color: var(--ak-text-muted, #999);
	}

	.avp-chips {
		display: flex;
		flex-wrap: wrap;
	}

	.avp-chip {
		margin: 0 12rpx 12rpx 0;
		padding: 10rpx 20rpx;
		border-radius: 8rpx;
		font-size: 23rpx;
		line-height: 1.3;
		color: var(--ak-text-secondary, #555);
		background: var(--ak-surface-muted, #f2f3f5);
		border: 1rpx solid transparent;
	}

	.avp-chip.active {
		color: var(--ak-color-primary, #007aff);
		background: rgba(0, 122, 255, 0.08);
		border-color: var(--ak-color-primary, #007aff);
	}

	.avp-tip {
		display: block;
		padding: 16rpx 0;
		font-size: 22rpx;
		line-height: 1.6;
		color: var(--ak-text-muted, #999);
	}

	.avp-modal-foot {
		display: flex;
		align-items: center;
		padding: 20rpx 30rpx 24rpx;
		border-top: 1rpx solid var(--ak-hairline-soft, #eee);
	}

	.avp-foot-btns {
		display: flex;
		align-items: center;
		margin-left: auto;
	}

	.avp-btn {
		margin-left: 16rpx;
		padding: 16rpx 36rpx;
		border-radius: 12rpx;
		font-size: 26rpx;
		color: var(--ak-text-secondary, #555);
		background: var(--ak-surface-muted, #f2f3f5);
	}

	.avp-btn.primary {
		color: #fff;
		background: var(--ak-color-primary, #007aff);
	}

	.avp-search-wrap {
		padding: 20rpx 30rpx 0;
	}

	.avp-search {
		height: 72rpx;
		padding: 0 24rpx;
		border-radius: 12rpx;
		font-size: 26rpx;
		background: var(--ak-surface-muted, #f2f3f5);
		color: var(--ak-text-primary, #333);
	}
	.avp-search-wrap:focus-within .avp-search {
		box-shadow: var(--ak-focus-shadow, 0 0 0 4rpx rgba(64, 158, 255, 0.18), 0 10rpx 24rpx -6rpx rgba(31, 94, 168, 0.28), inset 0 0 0 2rpx rgba(64, 158, 255, 0.45), inset 0 2rpx 8rpx rgba(31, 94, 168, 0.10));
	}

	.avp-picker-list {
		max-height: 50vh;
		padding: 10rpx 30rpx;
		box-sizing: border-box;
	}

	.avp-picker-item {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 22rpx 4rpx;
		border-bottom: 1rpx solid var(--ak-hairline-soft, #f2f2f2);
	}

	.avp-picker-name {
		font-size: 27rpx;
		color: var(--ak-text-primary, #333);
	}

	.avp-picker-check {
		width: 44rpx;
		height: 44rpx;
		line-height: 44rpx;
		text-align: center;
		border-radius: 50%;
		font-size: 24rpx;
		color: transparent;
		border: 1rpx solid var(--ak-hairline, #ddd);
	}

	.avp-picker-check.on {
		color: #fff;
		background: var(--ak-color-primary, #007aff);
		border-color: var(--ak-color-primary, #007aff);
	}

	.avp-picked-count {
		font-size: 24rpx;
		color: var(--ak-text-muted, #999);
	}

	.avp-io-body {
		padding: 24rpx 30rpx;
	}

	.avp-textarea {
		width: 100%;
		height: 300rpx;
		padding: 20rpx;
		box-sizing: border-box;
		border-radius: 12rpx;
		font-size: 22rpx;
		line-height: 1.5;
		color: var(--ak-text-primary, #333);
		background: var(--ak-surface-muted, #f2f3f5);
	}


	/* ===== 报错日志：与设置项统一 ===== */
	.setting-action {
		flex-shrink: 0;
		margin-left: 20rpx;
		padding: 10rpx 26rpx;
		border-radius: 999rpx;
		font-size: 24rpx;
		color: var(--ak-color-primary, #007aff);
		background: rgba(0, 122, 255, 0.08);
	}

	.setting-action.danger {
		color: #e34d4d;
		background: rgba(227, 77, 77, 0.08);
	}

	/* ===== App 版本更新 ===== */
	.app-upd-progress {
		padding: 16rpx 24rpx 20rpx;
	}

	.app-upd-track {
		height: 10rpx;
		border-radius: 999rpx;
		background: var(--ak-surface-muted, #f2f3f5);
		overflow: hidden;
	}

	.app-upd-fill {
		height: 100%;
		border-radius: 999rpx;
		background: var(--ak-color-primary, #007aff);
		transition: width 0.25s linear;
	}

	.app-upd-meta {
		display: flex;
		justify-content: space-between;
		margin-top: 10rpx;
		font-size: 22rpx;
		color: var(--ak-text-secondary, #888);
	}

	.app-upd-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-top: 6rpx;
		font-size: 22rpx;
		color: var(--ak-text-secondary, #888);
	}

	.app-upd-links {
		display: flex;
	}

	.app-upd-src {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.app-upd-link {
		margin-left: 28rpx;
		font-size: 22rpx;
		color: var(--ak-color-primary, #007aff);
	}

	.app-upd-error {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 14rpx 24rpx 20rpx;
		font-size: 22rpx;
		color: #e34d4d;
	}

	.app-upd-error-text {
		flex: 1;
		min-width: 0;
	}
</style>

