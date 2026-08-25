<template>
	<view class="container">
		<!-- 搜索区域 -->
		<view class="search-header">
			<view class="search-box">
				<image class="search-icon" src="/static/search-icon.png"></image>
				<input 
					class="search-input" 
					v-model="searchText" 
					placeholder="搜索功能..."
					@input="onSearch"
					focus
				/>
				<text class="clear-btn" v-if="searchText" @click="clearSearch">×</text>
			</view>
			<text class="cancel-btn" @click="goBack">取消</text>
		</view>

		<!-- 搜索结果 -->
		<view class="result-list" v-if="searchText">
			<!-- 快捷开关（设置项） -->
			<view 
				class="setting-item" 
				v-for="(item, index) in settingResults" 
				:key="'setting-'+index"
				@click="onSettingItemClick(item)"
			>
				<view class="setting-info">
					<text class="setting-name">{{ item.name }}</text>
					<text class="setting-desc">{{ item.desc }}</text>
					<text class="setting-warning" v-if="item.warning">{{ item.warning }}</text>
				</view>
				<switch v-if="!item.isButton" :checked="item.value" @change="item.onChange" />
				<text v-else class="result-arrow">›</text>
			</view>

			<!-- 页面跳转项 -->
			<view 
				class="result-item" 
				v-for="(item, index) in pageResults" 
				:key="'page-'+index"
				@click="goToPage(item)"
			>
				<view class="result-info">
					<text class="result-name">{{ item.name }}</text>
					<text class="result-desc">{{ item.desc }}</text>
				</view>
				<text class="result-arrow">›</text>
			</view>

			<view class="no-result" v-if="settingResults.length === 0 && pageResults.length === 0">
				<text>未找到相关功能</text>
			</view>
		</view>

		<!-- 默认列表 -->
		<view class="default-list" v-else>
			<view class="list-title">快捷开关</view>
			<view 
				class="setting-item" 
				v-for="(item, index) in settingItems" 
				:key="'default-setting-'+index"
				@click="onSettingItemClick(item)"
			>
				<view class="setting-info">
					<text class="setting-name">{{ item.name }}</text>
					<text class="setting-desc">{{ item.desc }}</text>
					<text class="setting-warning" v-if="item.warning">{{ item.warning }}</text>
				</view>
				<switch v-if="!item.isButton" :checked="item.value" @change="item.onChange" />
				<text v-else class="result-arrow">›</text>
			</view>

			<view class="list-title">常用功能</view>
			<view 
				class="result-item" 
				v-for="(item, index) in pageItems" 
				:key="'default-page-'+index"
				@click="goToPage(item)"
			>
				<view class="result-info">
					<text class="result-name">{{ item.name }}</text>
					<text class="result-desc">{{ item.desc }}</text>
				</view>
				<text class="result-arrow">›</text>
			</view>
		</view>
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
export default {
	data() {
		return {
			searchText: '',
			
			// 设置项状态
			enableEnglishSearch: false,
			enableNicknameSearch: false,
			enableGuessData: false,
			enableAutoUpdate: false,
			showMarketPrice: false,
			
			// 数据计数（用于判断是否需要下载）
			searchWordCount: 0,
			guessDataCount: 0,
			
			// 自定义搜索词（跳转到设置页管理）
			customSearchWords: []
		};
	},
	
	computed: {
		// 设置项配置（带当前值和回调）
		settingItems() {
			return [
				{
					name: '启用英文名搜索',
					desc: 'Enable English Name search',
					warning: '',
					value: this.enableEnglishSearch,
					onChange: (e) => this.toggleSetting('enableEnglishSearch', e.detail.value, '英文名搜索'),
					keywords: '英文名,搜索,english'
				},
				{
					name: '启用外号搜索',
					desc: '仅支持部分干员',
					warning: '',
					value: this.enableNicknameSearch,
					onChange: (e) => this.toggleSetting('enableNicknameSearch', e.detail.value, '外号搜索'),
					keywords: '外号,昵称,搜索'
				},
				{
					name: '启用预测通行证',
					desc: '显示官方未公布的新通行证预测信息',
					warning: '',
					value: this.enableGuessData,
					onChange: (e) => this.toggleSetting('enableGuessData', e.detail.value, '', true),
					keywords: '预测,通行证,guess'
				},
				{
					name: '干员数据自动更新',
					desc: '检测到数据更新时自动下载，建议只在Wi-Fi状态下启用',
					warning: '',
					value: this.enableAutoUpdate,
					onChange: (e) => this.toggleSetting('enableAutoUpdate', e.detail.value, '', true),
					keywords: '自动更新,数据,下载'
				},
				{
					name: '开启市价展示功能',
					desc: '在干员列表中显示市场价格信息',
					warning: '',
					value: this.showMarketPrice,
					onChange: (e) => this.toggleSetting('showMarketPrice', e.detail.value, '市价展示'),
					keywords: '市价,价格,市场'
				},
				{
					name: '自定义搜索词',
					desc: '添加个人常用的干员别名和搜索词',
					warning: '',
					value: false,
					onChange: () => this.goToSettingCustom(),
					keywords: '自定义,搜索词,别名',
					isButton: true
				}
			];
		},
		
		// 页面跳转项
		pageItems() {
			return [
				{ name: '按干员名查询盒号', path: '/pages/Search/Search?type=character', desc: '输入干员名查找所在盒号', keywords: '干员,查询,盒号,角色,character' },
				{ name: '按盒号查询干员', path: '/pages/Search/Search?type=box', desc: '输入盒号查看盒内干员', keywords: '盒号,查询,干员,box' },
				{ name: '通行证查询', path: '/pages/Search/Search', desc: '通过干员名或盒号查询', keywords: '查询,搜索,search,prts' },
				{ name: '通行证列表', path: '/pages/list/list', desc: '查看所有通行证列表', keywords: '列表,全部,list' },
				{ name: '扫码查盒', path: '/pages/scan_barcode/scan_barcode', desc: '扫描条形码查询盒号', keywords: '扫码,条形码,barcode,scan' },
				{ name: '模拟抽卡', path: '/pages/gacha_simulated/gacha_simulated', desc: '模拟抽卡体验', keywords: '抽卡,模拟,gacha,十连' },
				{ name: '我的展柜', path: '/pages/card_share/card_share', desc: '查看已点亮的通行证', keywords: '展柜,点亮,收藏' },
				{ name: '数据源设置', path: '/packageA/Setting/Setting?tab=data', desc: '切换数据来源', keywords: '数据源,github,gitcode,设置' },
				{ name: '主题设置', path: '/packageA/Setting/Setting?tab=theme', desc: '切换界面主题', keywords: '主题,颜色,皮肤,theme' },
				{ name: '关于', path: '/packageA/aboutus/aboutus', desc: '关于本小程序', keywords: '关于,版本,about' },
				{ name: '友情小工具', path: '/pages/friend_links/friend_links', desc: '友情链接与小工具', keywords: '友情,链接,工具' },
				{ name: '反馈纠错', path: '/pages/box_feedback/box_feedback', desc: '反馈数据问题', keywords: '反馈,纠错,错误,问题' }
			];
		},
		
		// 搜索结果 - 设置项
		settingResults() {
			if (!this.searchText) return [];
			const keyword = this.searchText.toLowerCase();
			return this.settingItems.filter(item => {
				return item.name.toLowerCase().includes(keyword) ||
					   item.desc.toLowerCase().includes(keyword) ||
					   item.keywords.toLowerCase().includes(keyword);
			});
		},
		
		// 搜索结果 - 页面项
		pageResults() {
			if (!this.searchText) return [];
			const keyword = this.searchText.toLowerCase();
			return this.pageItems.filter(item => {
				return item.name.toLowerCase().includes(keyword) ||
					   item.desc.toLowerCase().includes(keyword) ||
					   item.keywords.toLowerCase().includes(keyword);
			});
		}
	},
	
	onLoad() {
		console.log('[func_search] onLoad triggered');
		this.loadSettings();
	},
	
	onShow() {
		console.log('[func_search] onShow triggered');
		// 每次显示页面时重新加载设置，保持与 Setting 页面同步
		this.loadSettings();
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
		
		goToSettingCustom() {
			uni.navigateTo({
				url: '/packageA/Setting/Setting?tab=custom'
			});
		},
		
		onSearch(e) {
			this.searchText = e.detail.value;
		},
		
		clearSearch() {
			this.searchText = '';
		},
		
		goToPage(item) {
			uni.navigateTo({ url: item.path });
		},
		
		onSettingItemClick(item) {
			if (item.isButton && typeof item.onChange === 'function') {
				item.onChange();
			}
		},
		
		// 加载设置
		loadSettings() {
			try {
				console.log('[func_search] loadSettings start');
				// 优先读 Setting.vue 使用的独立 key，兼容旧版 searchSettings 对象
				const en1 = uni.getStorageSync('enableEnglishSearch');
				console.log('[func_search] raw en1 from storage =', en1, typeof en1);
				const nick1 = uni.getStorageSync('enableNicknameSearch');
				if (en1 !== '' && en1 !== undefined && en1 !== null) {
					this.enableEnglishSearch = en1 === true || en1 === 'true' || en1 === true;
				} else {
					const ss = uni.getStorageSync('searchSettings');
					if (ss) this.enableEnglishSearch = ss.enableEnglishSearch || false;
				}
				if (nick1 !== '' && nick1 !== undefined && nick1 !== null) {
					this.enableNicknameSearch = nick1 === true || nick1 === 'true' || nick1 === true;
				} else {
					const ss = uni.getStorageSync('searchSettings');
					if (ss) this.enableNicknameSearch = ss.enableNicknameSearch || false;
				}
				
				const gd1 = uni.getStorageSync('enableGuessData');
				if (gd1 !== '' && gd1 !== undefined && gd1 !== null) {
					this.enableGuessData = gd1 === true || gd1 === 'true' || gd1 === true;
				} else {
					const guessData = uni.getStorageSync('guessDataEnabled');
					this.enableGuessData = guessData === true || guessData === true || guessData === 'true';
				}
				
				const autoUpdateRaw = uni.getStorageSync('autoUpdateSetting');
			const autoUpdate = (typeof autoUpdateRaw === 'object' && autoUpdateRaw !== null) ? (autoUpdateRaw.enabled === true || autoUpdateRaw.enabled === 'true') : (autoUpdateRaw === true || autoUpdateRaw === 'true');
				this.enableAutoUpdate = autoUpdate === true || autoUpdate === true || autoUpdate === 'true';
				
				const marketPrice = uni.getStorageSync('showMarketPrice');
				this.showMarketPrice = marketPrice === true || marketPrice === true || marketPrice === 'true';
				
				const words = uni.getStorageSync('customSearchWords');
				this.customSearchWords = words || [];
				
				// 加载数据计数（用于判断是否需要下载）
				const searchWords = uni.getStorageSync('searchWords');
				this.searchWordCount = searchWords ? searchWords.length : 0;
				const guessDataList = uni.getStorageSync('guessData');
				this.guessDataCount = guessDataList ? guessDataList.length : 0;
				console.log('[func_search] loadSettings done', {
					enableEnglishSearch: this.enableEnglishSearch,
					enableNicknameSearch: this.enableNicknameSearch,
					enableGuessData: this.enableGuessData,
					searchWordCount: this.searchWordCount,
					guessDataCount: this.guessDataCount
				});
			} catch (e) {
				this.logError(e);
				console.error('加载设置失败:', e);
			}
		},
		
		// 切换设置
		toggleSetting(key, value, name, needRestart = false) {
			console.log('[func_search] toggleSetting called', { key, value, name });
			this[key] = value;
			
			// 保存到 storage（与 Setting.vue 保持一致的 key）
			if (key === 'enableEnglishSearch') {
				uni.setStorageSync('enableEnglishSearch', String(value));
				console.log('[func_search] wrote enableEnglishSearch =', String(value));
				uni.setStorageSync('searchSettings', {
					enableEnglishSearch: value,
					enableJapaneseSearch: false,
					enableNicknameSearch: this.enableNicknameSearch
				});
				
				// 如果启用搜索且本地无数据，跳转到设置页触发下载
				if (value && this.searchWordCount === 0) {
					uni.showToast({ title: '正在跳转设置页下载数据...', icon: 'none', duration: 2000 });
					setTimeout(() => {
						uni.navigateTo({ url: '/packageA/Setting/Setting?tab=search' });
					}, 500);
					return;
				}
			} else if (key === 'enableNicknameSearch') {
				uni.setStorageSync('enableNicknameSearch', String(value));
				console.log('[func_search] wrote enableNicknameSearch =', String(value));
				uni.setStorageSync('searchSettings', {
					enableEnglishSearch: this.enableEnglishSearch,
					enableJapaneseSearch: false,
					enableNicknameSearch: value
				});
			} else if (key === 'enableGuessData') {
				uni.setStorageSync('enableGuessData', String(value));
				console.log('[func_search] wrote enableGuessData =', String(value));
				uni.setStorageSync('guessDataEnabled', value);
				// 如果启用预测且本地无数据，跳转到设置页
				if (value && this.guessDataCount === 0) {
					uni.showToast({ title: '正在跳转设置页下载数据...', icon: 'none', duration: 2000 });
					setTimeout(() => {
						uni.navigateTo({ url: '/packageA/Setting/Setting?tab=data' });
					}, 500);
					return;
				}
			} else if (key === 'enableAutoUpdate') {
				uni.setStorageSync('autoUpdateSetting', value);
				console.log('[func_search] wrote autoUpdateSetting =', value);
			} else if (key === 'showMarketPrice') {
				uni.setStorageSync('showMarketPrice', value);
				console.log('[func_search] wrote showMarketPrice =', value);
			}
			
			// 提示
			if (needRestart) {
				uni.showToast({ title: '设置已保存，重启生效', icon: 'none' });
			} else if (name) {
				uni.showToast({ title: (value ? '已开启' : '已关闭') + name, icon: 'none' });
			}
		},
		
	}
};
</script>

<style>
.container {
	background: #f5f5f5;
	min-height: 100vh;
}

/* 搜索头部 */
.search-header {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	z-index: 999;
	background: #ffffff;
	padding: 10px 15px;
	padding-top: calc(10px + env(safe-area-inset-top));
	display: flex;
	align-items: center;
	border-bottom: 1rpx solid #e8e8e8;
}

.search-box {
	flex: 1;
	height: 36px;
	background: #f5f5f5;
	border: 1rpx solid #dcdcdc;
	border-radius: 18px;
	display: flex;
	align-items: center;
	padding: 0 12px;
	margin-right: 12px;
}

.search-icon {
	width: 16px;
	height: 16px;
	margin-right: 8px;
	opacity: 0.6;
}

.search-input {
	flex: 1;
	font-size: 15px;
	height: 36px;
	background: transparent;
}

.clear-btn {
	font-size: 20px;
	color: #999;
	padding: 0 5px;
}

.cancel-btn {
	font-size: 15px;
	color: #007AFF;
}

/* 结果列表 */
.result-list, .default-list {
	padding-top: 60px;
}

.list-title {
	font-size: 14px;
	color: #666;
	padding: 15px;
	background: #f5f5f5;
}

/* 设置项（带开关）- 与 Setting.vue 保持一致 */
.setting-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 25rpx;
	background: #fff;
	border-bottom: 1rpx solid #f0f0f0;
}

.setting-item:last-child {
	border-bottom: none;
}

.setting-info {
	flex: 1;
	margin-right: 10px;
}

.setting-name {
	font-size: 30rpx;
	color: #333;
	display: block;
}

.setting-desc {
	font-size: 24rpx;
	color: #999;
	display: block;
	margin-top: 8rpx;
}

.setting-warning {
	font-size: 22rpx;
	color: #F56C6C;
	display: block;
	margin-top: 8rpx;
}

/* 页面跳转项 - 与 Setting.vue 保持一致 */
.result-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 25rpx;
	background: #fff;
	border-bottom: 1rpx solid #f0f0f0;
}

.result-item:last-child {
	border-bottom: none;
}

.result-info {
	flex: 1;
}

.result-name {
	font-size: 30rpx;
	color: #333;
	display: block;
}

.result-desc {
	font-size: 24rpx;
	color: #999;
	display: block;
	margin-top: 8rpx;
}

.result-arrow {
	font-size: 20px;
	color: #ccc;
}

.no-result {
	text-align: center;
	padding: 100px 0;
	color: #999;
	font-size: 15px;
}
</style>
