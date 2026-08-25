<template>
	<view :class="['container', 'theme-' + themeMode]">
		<!-- 数据源设置 -->
		<view class="section" id="section-data">
			<view class="section-header">
				<text class="section-title">数据源设置</text>
			</view>
			<view class="section-content">
				<view class="data-source-options">
					<view 
					class="data-source-item"
					v-for="(option, index) in dataSourceOptions"
					:key="option.value"
					:style="itemStyle(index, option.value)"
					:class="{
						'active': dataSource === option.value,
						'disabled': option.disabled || (option.value === 'github' && isDownloading),
						'loading': option.value === 'github' && isDownloading
					}"
					@click="selectDataSource(option.value)"
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
				<view class="custom-data-management" v-if="dataSource === 'custom' || dataSource === 'local'">
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

		<!-- 界面主题设置 -->
		<view class="section" id="section-theme">
			<view class="section-header">
				<view class="section-title-row">
				<text class="section-title">界面主题</text>
				<text class="section-warning-tag">测试中功能，谨慎使用</text>
			</view>				
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
					<text class="status-label">当前数据源:</text>
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
				<view class="section" id="section-errorlog">
				<view class="section-header">
					<text class="section-title">报错日志</text>
					<text class="errorlog-section-desc">在没有错误日志的情况下诊断任何问题无异于闭眼开车</text>
					<!-- <text v-if="errorLogCount > 0" class="errorlog-badge-inline">{{ errorLogCount }}</text> -->
				</view>
				<view class="section-content">
					
					<!-- 主操作: 查看报错日志 (大号) -->
					<button class="errorlog-primary-btn" @click="viewErrorLogs">查看报错日志</button>
					<!-- 次要操作: 清空日志 (小号) -->
					<!-- <button class="errorlog-secondary-btn" @click="clearErrorLogs">清空日志</button> -->
					<!-- 调试模式按钮: 正式发布(微信小程序)时不会编译 -->
					<!-- #ifndef MP-WEIXIN -->
					<button class="errorlog-debug-btn" @click="testLogError">插入测试日志</button>
					<!-- #endif -->
				</view>
			</view>


		<!-- 报错日志自定义弹窗 -->
		<view v-if="showErrorLogModal" class="errorlog-modal-mask" @click="closeErrorLogModal">
			<view class="errorlog-modal" @click.stop>
				<view class="errorlog-modal-title">报错日志 (共{{ currentErrorLogs.length }}条)</view>
				<scroll-view scroll-y class="errorlog-modal-body">
					<view v-for="(log, idx) in currentErrorLogs" :key="idx" class="errorlog-modal-item">
						<text class="errorlog-modal-meta">[{{ idx + 1 }}] {{ log.time }} [{{ log.context || log.source }}]</text>
						<text class="errorlog-modal-msg">{{ log.name }}: {{ log.message }}</text>
					</view>
				</scroll-view>
				<view class="errorlog-modal-btns">
					<button class="cancel-btn" @click="closeErrorLogModal">关闭</button>
					<button class="secondary-btn" @click="copyErrorLogs">复制到剪贴板</button>
					<button class="danger-btn" @click="onClearFromModal">清空日志</button>
				</view>
			</view>
		</view>
		</view>
</template>

<script>
	import errorLog from "@/utils/errorLog.js";
	// 开源版本：知晓云配置已禁用，使用 GitHub 直链
	const GITHUB_DATA_SOURCES = {
		searchWordUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/searchWord.json',
		guessNewBoxUrl: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main/guessNew_Box_Id.json'
	};
	
	// 知晓云配置 - 开源版本clientId为空，商业版请配置知晓云
	const KNOW_CLOUD_CONFIG = {
		clientId: 'YOUR_KNOW_CLOUD_CLIENT_ID',
		baseUrl: 'https://YOUR_KNOW_CLOUD_CLIENT_ID.myminapp.com/hserve/v2.2',
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
				errorLogCount: 0,
				showErrorLogModal: false,
				currentErrorLogs: [],
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
						warning: '需自行验证数据格式'
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
				themeMode: 'simple', // 'simple' 简约风格, 'ark' 科技风格
				themeOptions: [
					{ value: 'simple', label: '简约风格', desc: '清新简洁的界面设计' },
					{ value: 'ark', label: '深色风格', desc: '黑橘色主题深色界面' },
					{ value: 'jieyuan', label: '类界园风格', desc: '参考岁的界园志异风格' }
				]
			}
		},
		computed: {
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



		onLoad(options) {
			this.loadDataSourceSetting();
			this.loadDataInfo();
			this.loadMarketPriceSetting(); // 新增：加载市价设置
			this.calculateDataSize();
				try { this.loadGuessDataSetting(); } catch (e) {
					this.logError(e);
					console.warn('loadGuessDataSetting failed:', e); 
				}
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

		onShow() {
			this.refreshErrorLogCount();
			// 重新加载设置，保持与 func_search 页面同步
			this.loadSearchSettings();
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
				logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},

			viewErrorLogs() {
				const logs = errorLog.getErrorLogs();
				if (logs.length === 0) {
					uni.showToast({ title: '暂无报错记录', icon: 'none' });
					return;
				}
				this.currentErrorLogs = logs;
				this.showErrorLogModal = true;
			},

			closeErrorLogModal() {
				this.showErrorLogModal = false;
				this.currentErrorLogs = [];
			},

			copyErrorLogs() {
				const logs = this.currentErrorLogs || [];
				const text = '报错日志 (' + logs.length + '条)\n' + logs.map((log, idx) =>
					`[${log.time}] [${log.context || log.source}] ${log.name}: ${log.message}`
				).join('\n');
				uni.setClipboardData({
					data: text,
					success: () => {
						uni.showToast({ title: '已复制到剪贴板', icon: 'success' });
						this.closeErrorLogModal();
					},
					fail: () => uni.showToast({ title: '复制失败', icon: 'none' }),
				});
			},
			onClearFromModal() {
				this.closeErrorLogModal();
				this.clearErrorLogs();
			},


			refreshErrorLogCount() {
				try {
					const logs = errorLog.getErrorLogs();
					this.errorLogCount = logs ? logs.length : 0;
				} catch (e) {
					this.errorLogCount = 0;
				}
			},

			clearErrorLogs() {
				uni.showModal({
					title: '确认清空',
					content: '确定要清空所有报错日志吗？',
					success: (res) => {
					if (res.confirm) {
						errorLog.clearErrorLogs();
						uni.showToast({ title: '日志已清空', icon: 'success' });
					}
					},
				});
			},

			testLogError() {
				// 测试方法: 主动插入一条错误日志, 用于验证日志功能是否正常
				try {
					// 模拟一个真实错误
					throw new Error('这是一条测试错误日志 - 验证日志记录功能正常');
				} catch (e) {
					this.logError(e, 'Setting.testLogError');
					uni.showToast({ title: '测试日志已记录', icon: 'success' });
				}
			},

				loadGuessDataSetting() {
			console.log('[Setting] loadGuessDataSetting raw =', uni.getStorageSync('enableGuessData'));
			try {
				const enableGuessData = uni.getStorageSync('enableGuessData');
				if (typeof enableGuessData === 'boolean' || typeof enableGuessData === 'string') {
					this.enableGuessData = enableGuessData === true || enableGuessData === 'true';
				}
				if (this.enableGuessData) {
					const guessData = uni.getStorageSync('guessData');
					this.guessDataCount = guessData && Array.isArray(guessData) ? guessData.length : 0;
					const savedStatus = uni.getStorageSync('guessDataStatusMessage');
					if (savedStatus) {
						this.guessDataStatusMessage = savedStatus;
					}
				}
			} catch (e) {
				this.logError(e);
				console.warn('加载预测数据设置失败:', e);
			}
		},

		onDataSourceDragStart(e, index) {
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
			uni.setStorageSync('dataSourceOrder', order);
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

		onGuessDataChange(e) {
			const newValue = e.detail.value;
			this.enableGuessData = newValue;
			uni.setStorageSync('enableGuessData', newValue);
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
			uni.setStorageSync('dataSourceOrder', arr.map(o => o.value));
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
			// 选择主题
			selectTheme(themeValue) {
				if (this.themeMode === themeValue) {
					return;
				}

				uni.showModal({
					title: '切换主题',
					content: `确定要切换到${themeValue === 'ark' ? '科技风格' : '简约风格'}吗？切换后需要重启应用才能生效。`,
					confirmText: '切换',
					cancelText: '取消',
					success: (res) => {
						if (res.confirm) {
							this.themeMode = themeValue;
							uni.setStorageSync('themeMode', themeValue);
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
					console.error('从知晓云获取搜索词URL失败:', error);
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
					console.error(`从知晓云获取预测URL失败 (type=${type}):`, error);
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
					console.error('清除数据失败:', e);
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
					console.error('检查国内源数据失败:', e);
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
					console.error('加载自定义数据信息失败:', e);
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
					console.error('加载数据源设置失败:', e);
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
					console.error('加载GitHub数据信息失败:', e);
				}
			},
			
			// 获取当前数据源名称
			getCurrentDataSourceName() {
				return DATA_SOURCES[this.dataSource]?.name || '未知';
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
					console.error('下载GitHub数据失败:', error);
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
			        extension: ['.json'],
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
			            console.error('选择文件失败:', err);
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
					extension: ['.json'],
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
					    console.error('选择文件失败:', err);
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
								console.error('获取远程JSON失败:', err);
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
					console.error('处理JSON文件失败:', error);
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
					const validationResult = this.validateCustomData(jsonData);
					
					this.uploadProgress = 100;
					
					// 保存临时数据
					this.tempCustomData = jsonData;
					this.validationResult = validationResult;
					
					// 显示验证结果
					this.showValidationModal = true;
					
				} catch (error) {
					this.logError(error);
					console.error('处理JSON数据失败:', error);
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
					console.error('保存自定义数据失败:', e);
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
					const fileName = `arknights-data-${timestamp}.json`;
					
					// 保存为临时文件
					this.saveJsonToFile(jsonString, fileName);
					
				} catch (e) {
					this.logError(e);
					console.error('导出数据失败:', e);
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
			        const fileName = `arknights-share-${timestamp}.json`;
			        
			        // 保存并分享
			        this.saveAndShareJsonFile(jsonString, fileName);
			        
			    } catch (e) {
			    	this.logError(e);
			        uni.hideLoading();
			        console.error('分享数据失败:', e);
			        uni.showToast({
			            title: '分享失败',
			            icon: 'none'
			        });
			    }
			},
			
			// 保存JSON到文件
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
			            console.error('写入文件失败:', writeError);
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
			                content: '数据已保存为JSON文件，是否要分享给好友？',
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
			            console.error('保存文件失败:', saveErr);
			            
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
			                console.error('打开文档失败:', openErr);
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
			            console.error('写入分享文件失败:', writeError);
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
			                console.error('分享失败:', err);
			                
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
										console.error('GitHub数据JSON解析失败:', e);
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
								console.error('GitHub服务器错误:', res.statusCode);
								resolve(false);
							}
						},
						fail: (err) => {
							console.error('下载GitHub数据失败:', err);
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
					console.error('加载数据信息失败:', e);
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
					console.error('加载搜索设置失败:', e);
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
					console.error('清理英文数据失败:', e);
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
					console.error('清理外号数据失败:', e);
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
			    console.log('[Setting] loadAutoUpdateSetting raw =', uni.getStorageSync('autoUpdateSetting'));
			    try {
			        const autoUpdateSetting = uni.getStorageSync('autoUpdateSetting');
			        console.log('从存储加载的自动更新设置:', autoUpdateSetting);
			        
			        if (autoUpdateSetting) {
			            // 更新本地数据状态
			            const _auEnabled = (typeof autoUpdateSetting === 'object' && autoUpdateSetting !== null)
						? (autoUpdateSetting.enabled === true || autoUpdateSetting.enabled === 'true')
						: (autoUpdateSetting === true || autoUpdateSetting === 'true');
					this.enableAutoUpdate = _auEnabled;
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
			        console.error('加载自动更新设置失败:', e);
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
			        console.error('保存自动更新设置失败:', e);
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
			            console.error('显示确认对话框失败:', err);
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
					console.error('计算数据大小失败:', e);
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
					console.error('格式化云端时间失败:', e);
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
			  console.log('[Setting] loadMarketPriceSetting raw =', uni.getStorageSync('showMarketPrice'));
			  try {
			    const showMarketPrice = uni.getStorageSync('showMarketPrice');
			    this.showMarketPrice = showMarketPrice === true || showMarketPrice === 'true';
			    console.log('加载市价设置:', this.showMarketPrice);
			  } catch (e) {
			  	this.logError(e);
			    console.error('加载市价设置失败:', e);
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
			      console.error('显示免责声明失败:', err);
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
					console.error('删除数据失败:', e);
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
	}

	/* 科技风格预览 */
	.theme-ark {
		background: linear-gradient(135deg, #0D0D15 0%, #1A1A2E 100%);
	}

	.theme-ark::before {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		width: 15rpx;
		height: 15rpx;
		border-top: 3rpx solid #FF6B35;
		border-left: 3rpx solid #FF6B35;
	}

	.theme-ark::after {
		content: '';
		position: absolute;
		bottom: 0;
		right: 0;
		width: 15rpx;
		height: 15rpx;
		border-bottom: 3rpx solid #FF6B35;
		border-right: 3rpx solid #FF6B35;
	}

	.theme-ark .preview-box {
		position: absolute;
		top: 15rpx;
		left: 15rpx;
		right: 15rpx;
		height: 25rpx;
		background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%);
		border-radius: 2rpx;
		border: 1rpx solid #2A2A4A;
	}

	.theme-ark .preview-text {
		position: absolute;
		bottom: 15rpx;
		left: 15rpx;
		right: 15rpx;
		height: 15rpx;
		background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%);
		border-radius: 2rpx;
	}

	/* 界园风格预览 - 粉绿渐变风格 */
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
	}

	/* ========== 科技风格 ========== */
	.container.theme-ark {
		background: linear-gradient(180deg, #0D0D15 0%, #1A1A2E 100%);
	}
	.container.theme-ark .container { background: transparent; }
	.container.theme-ark .section { background: linear-gradient(135deg, #16162A 0%, #1E1E3A 100%); border: 1rpx solid #2A2A4A; }
	.container.theme-ark .section-title { color: #FF6B35; }
	.container.theme-ark .data-source-item { border-bottom-color: #2A2A4A; }
	.container.theme-ark .data-source-label { color: #e0e0e0; }
	.container.theme-ark .data-source-desc { color: #888; }
	.container.theme-ark .data-source-warning { color: #FF6B35; }
	.container.theme-ark .upload-btn,
	.container.theme-ark .export-btn,
	.container.theme-ark .update-btn { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); color: #fff; border: none; }
	.container.theme-ark .theme-item.active .theme-radio { background: linear-gradient(135deg, #FF6B35 0%, #FF8C5A 100%); }
	.container.theme-ark .theme-label { color: #e0e0e0; }
	.container.theme-ark .theme-desc { color: #888; }
	.container.theme-ark .version-text { color: #888; }
	.container.theme-ark .theme-radio { border-color: #2A2A4A; }
	.container.theme-ark .theme-item.active .theme-radio { border-color: #FF6B35; }
	.container.theme-ark .info-title { color: #e0e0e0; }
	.container.theme-ark .info-item { color: #888; }
	.container.theme-ark .loading-text { color: #FF6B35; }
	.container.theme-ark .switch-section .section-title { color: #e0e0e0; }
	.container.theme-ark .switch-row { border-bottom-color: #2A2A4A; }
	.container.theme-ark .switch-label { color: #e0e0e0; }
	.container.theme-ark .switch-desc { color: #888; }
	.container.theme-ark .theme-tip { color: #888; }
	.container.theme-ark .data-source-info { border-top-color: #2A2A4A; }
	.container.theme-ark .info-label { color: #888; }
	.container.theme-ark .info-value { color: #e0e0e0; }
	.container.theme-ark .progress-bar { background-color: #2A2A4A; }
	.container.theme-ark .progress-inner { background: linear-gradient(90deg, #FF6B35, #FF8C5A); }

	/* ========== 界园风格 ========== */
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
	.privacy-btn.confirm .btn-text { color: #fff; }
	.theme-ark 
	.theme-ark 
	.theme-ark .privacy-btn.cancel { background-color: #444; }
	.theme-ark .privacy-btn.cancel .btn-text { color: #ccc; }

	.data-source-item {
		transition: transform 0.22s ease, background-color 0.2s ease, box-shadow 0.2s ease;
		will-change: transform;
	}

				/* ========== 报错日志自定义弹窗样式 ========== */
		.errorlog-badge-inline {
			background-color: #FA5151;
			color: #fff;
			font-size: 22rpx;
			min-width: 36rpx;
			height: 36rpx;
			line-height: 36rpx;
			text-align: center;
			padding: 0 12rpx;
			border-radius: 18rpx;
			margin-left: 12rpx;
		}
		.errorlog-modal-mask {
			position: fixed;
			top: 0; left: 0; right: 0; bottom: 0;
			background: rgba(0, 0, 0, 0.5);
			display: flex;
			align-items: center;
			justify-content: center;
			z-index: 9999;
		}
		.errorlog-modal {
			width: 80%;
			max-width: 600rpx;
			max-height: 80vh;
			background: #fff;
			border-radius: 16rpx;
			display: flex;
			flex-direction: column;
			overflow: hidden;
			box-shadow: 0 8rpx 32rpx rgba(0, 0, 0, 0.2);
		}
		.errorlog-modal-title {
			padding: 28rpx 24rpx;
			font-size: 30rpx;
			font-weight: 600;
			color: #333;
			text-align: center;
			border-bottom: 1rpx solid #eee;
		}
		.errorlog-modal-body {
			flex: 1;
			padding: 16rpx 24rpx;
			max-height: 60vh;
		}
		.errorlog-modal-item {
			background: #1e1e1e;
			color: #d4d4d4;
			font-family: 'Courier New', Consolas, 'Liberation Mono', monospace;
			font-size: 22rpx;
			padding: 16rpx;
			border-radius: 8rpx;
			margin-bottom: 12rpx;
			line-height: 1.6;
			word-break: break-all;
			white-space: pre-wrap;
		}
		.errorlog-modal-item:last-child {
			margin-bottom: 0;
		}
		.errorlog-modal-meta {
			display: block;
			color: #9cdcfe;
			font-size: 20rpx;
			margin-bottom: 6rpx;
		}
		.errorlog-modal-msg {
			display: block;
			color: #ce9178;
			font-size: 22rpx;
		}
		.errorlog-modal-btns {
			display: flex;
			gap: 12rpx;
			padding: 20rpx 24rpx;
			border-top: 1rpx solid #eee;
			background: #fafafa;
		}
		.errorlog-modal-btns button {
			flex: 1;
			margin: 0;
		}
			/* ========== 报错日志 section 按钮层级 (主/次/调试) ========== */
		.errorlog-section-desc {
			display: block;
			font-size: 24rpx;
			color: #999;
			line-height: 1.5;
			margin-bottom: 24rpx;
			padding: 0 4rpx;
		}
		.errorlog-primary-btn {
			display: flex;
			align-items: center;
			justify-content: center;
			width: 100%;
			background: linear-gradient(135deg, #409EFF 0%, #2b7ed3 100%);
			color: #fff;
			border: none;
			border-radius: 12rpx;
			font-size: 30rpx;
			font-weight: 500;
			height: 88rpx;
			line-height: 88rpx;
			padding: 0;
			margin: 0 0 16rpx 0;
			box-shadow: 0 4rpx 12rpx rgba(64, 158, 255, 0.25);
		}
		.errorlog-primary-btn:active {
			opacity: 0.85;
		}
		.errorlog-secondary-btn {
			display: flex;
			align-items: center;
			justify-content: center;
			width: 100%;
			background-color: #fff;
			color: #FA5151;
			border: 1rpx solid #FA5151;
			border-radius: 12rpx;
			font-size: 26rpx;
			height: 72rpx;
			line-height: 72rpx;
			padding: 0;
			margin: 0 0 12rpx 0;
		}
		.errorlog-secondary-btn:active {
			background-color: #fef0f0;
		}
		.errorlog-debug-btn {
			display: flex;
			align-items: center;
			justify-content: center;
			width: 100%;
			background-color: transparent;
			color: #999;
			border: 1rpx dashed #ccc;
			border-radius: 8rpx;
			font-size: 22rpx;
			height: 56rpx;
			line-height: 56rpx;
			padding: 0;
			margin: 0;
		}
		.errorlog-debug-btn:active {
			background-color: #f5f5f5;
		}

		/* 标题行 (含警示标签) */
		.section-title-row {
			display: flex;
			flex-direction: row;
			align-items: center;
			justify-content: space-between;
			width: 100%;
		}
		.section-warning-tag {
			background-color: #FA5151;
			color: #fff;
			font-size: 22rpx;
			padding: 4rpx 14rpx;
			border-radius: 6rpx;
			line-height: 1.4;
			display: inline-block;
			max-width: 70%;
		}
	</style>
