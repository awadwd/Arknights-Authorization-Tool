<template>
	<view class="wh" :class="'theme-' + themeMode">
		<view class="wh-inner">

			<!-- 顶部：品牌 + 搜索 -->
			<view class="wh-top">
				<view class="wh-brand">
					<image class="wh-logo" src="/static/app-logo.png" mode="aspectFit"></image>
					<view>
						<text class="wh-title">方舟通行证谷子查询工具</text>
						<text class="wh-sub">Windows 桌面版</text>
					</view>
				</view>

				<view class="wh-search" @click="goSearch">
					<image class="wh-search-icon" src="/static/search-icon.png" mode="aspectFit"></image>
					<text class="wh-search-text">搜索干员、盒号或条码…</text>
				</view>
			</view>

			<!-- 首屏：轮播 / 数据概览 / 公告 三栏 -->
			<view class="wh-hero">
				<view v-if="banners.length" class="wh-banner">
					<swiper
						class="wh-swiper"
						:style="{ aspectRatio: bannerRatio }"
						:indicator-dots="banners.length > 1"
						indicator-color="rgba(255,255,255,0.45)"
						indicator-active-color="#ffffff"
						autoplay
						:interval="4500"
						:duration="420"
						circular
					>
						<swiper-item v-for="(item, i) in banners" :key="i">
							<image
								class="wh-banner-img"
								:src="item.img"
								mode="aspectFit"
								@load="onBannerLoad"
								@click="openBanner(item)"
							></image>
						</swiper-item>
					</swiper>
				</view>

				<view class="wh-stats">
					<view class="wh-stat">
						<text class="wh-stat-n">{{ boxCount }}</text>
						<text class="wh-stat-l">收录盒号</text>
					</view>
					<view class="wh-stat">
						<text class="wh-stat-n">{{ charCount }}</text>
						<text class="wh-stat-l">收录干员</text>
					</view>
					<view class="wh-stat">
						<text class="wh-stat-n">{{ starCount }}</text>
						<text class="wh-stat-l">我的收藏</text>
					</view>
				</view>

			</view>

			<view class="wh-grid">
				<!-- 快速开始 -->
				<view class="wh-card">
					<text class="wh-card-title">快速开始</text>
					<view class="wh-actions">
						<view v-for="item in actions" :key="item.url" class="wh-action" @click="open(item.url)">
							<text class="wh-action-name">{{ item.name }}</text>
							<text class="wh-action-desc">{{ item.desc }}</text>
						</view>
					</view>
				</view>

				<!-- 公告 -->
				<view class="wh-card wh-notice-card">
					<view class="wh-card-head">
						<text class="wh-card-title">公告</text>
						<text v-if="noticeVersion" class="wh-badge">{{ noticeVersion }}</text>
					</view>

					<scroll-view class="wh-notice-body" scroll-y>
						<text class="wh-notice-text">{{ noticeContent || '暂无公告' }}</text>
					</scroll-view>

					<text class="wh-more" @click="open('/packageA/more-notice/more-notice')">查看全部公告</text>
				</view>
			</view>

			<!-- 收藏预览 -->
			<view v-if="favorites.length" class="wh-card wh-fav">
				<view class="wh-card-head">
					<text class="wh-card-title">最近收藏</text>
					<text class="wh-more" @click="open('/packageB/pages/mystarbox/mystarbox')">查看全部</text>
				</view>
				<view class="wh-fav-list">
					<view v-for="box in favorites" :key="box.id" class="wh-fav-item" @click="openBox(box.id)">
						<text class="wh-fav-id">{{ box.id }}</text>
						<text class="wh-fav-count">{{ box.count }} 位干员</text>
					</view>
				</view>
			</view>

			<!-- 加载 / 失败 -->
			<view v-if="dataLoading" class="wh-state">
				<text class="wh-state-text">正在获取盒号数据…</text>
				<text class="wh-state-hint">首次启动需要联网拉取一次，之后直接读本地缓存</text>
			</view>

			<view v-else-if="dataError" class="wh-state">
				<text class="wh-state-text">{{ dataError }}</text>
				<text class="wh-retry" @click="bootstrapData(true)">重新获取</text>
			</view>

			<view class="wh-foot">
				<text class="wh-foot-text">侧边栏切换功能 · Ctrl + Alt + A 唤回窗口 · 关闭窗口收进托盘</text>
			</view>

		</view>
	</view>
</template>

<script>
import { allCharNames } from '../../utils/charMeta.js'
import { fetchSelfHosted } from '../../utils/knowCloudProxy.js'
import { loadBoxData, boxCharacterCount } from '../../utils/firstRunData.js'
import { openLink } from '../../utils/openLink.js'

export default {
	data() {
		return {
			themeMode: 'simple',
			boxCount: 0,
			charCount: 0,
			starCount: 0,
			favorites: [],
			noticeContent: '',
			noticeVersion: '',
			banners: [],
			bannerRatio: 2.15,
			bannerHeight: 168,
			bannerWidth: 720,
			dataLoading: false,
			dataError: '',
			actions: [
				{ name: '干员查询', desc: '按干员找盒号与盒内信息', url: '/pages/Search/Search' },
				{ name: '盒号列表', desc: '浏览全部通行证与干员', url: '/pages/list/list' },
				{ name: '模拟抽卡', desc: '试试手气，记录盈亏', url: '/packageB/pages/gacha_simulated/gacha_simulated' },
				{ name: '通行证展柜', desc: '按盒号整理你的收藏', url: '/pages/card_share/card_share' },
				{ name: '今日宜挂', desc: '看看今天适合挂什么', url: '/pages/daily_recommend/daily_recommend' },
				// 桌面端原先漏了这两个入口：PRTS 帮查做了 PC 适配但没有门，游戏入口只在手机首页有
				{ name: '通行证游乐场', desc: '竞猜 / 速配 / 接龙，每天几局', url: '/packageB/pages/passport_game/passport_game' },
				{ name: 'PRTS 帮查', desc: '用问答方式查通行证与干员', url: '/packageA/PRTS_Search/PRTS_Search?from=index' },
				{ name: '数据反馈', desc: '提交缺失或错误的信息', url: '/pages/box_feedback_new/box_feedback_new' }
			]
		}
	},

	onLoad() {
		this.themeMode = uni.getStorageSync('themeMode') || 'simple'
		this.loadStats()
		this.loadNotice()
		this.loadBanners()
		this.bootstrapData()
	},

	onShow() {
		this.loadStats()
	},

	methods: {
		onBannerLoad(e) {
			const d = (e && e.detail) || {}
			if (d.width && d.height) {
				this.bannerRatio = d.width / d.height
			}
		},

		async bootstrapData(force) {
			this.dataLoading = true
			this.dataError = ''
			try {
				const result = await loadBoxData({ force: !!force })
				if (result && result.data && result.data.length) {
					this.applyBoxes(result.data)
				} else {
					this.dataError = '暂时没能取到盒号数据'
				}
			} catch (e) {
				console.error('[desktop_home] bootstrap', e)
				this.dataError = '暂时没能取到盒号数据'
			}
			this.dataLoading = false
		},

		loadStats() {
			try {
				const data = uni.getStorageSync('currentData')
				const boxes = Array.isArray(data) ? data : (uni.getStorageSync('arknightsData') || [])
				if (Array.isArray(boxes) && boxes.length) this.applyBoxes(boxes)
			} catch (e) {
				console.error('[desktop_home] loadStats', e)
			}
		},

		applyBoxes(boxes) {
			this.boxCount = boxes.length

			try {
				this.charCount = allCharNames().length
			} catch (e) {
				this.charCount = 0
			}

			const favIds = uni.getStorageSync('favoriteBoxIds')
			const ids = Array.isArray(favIds) ? favIds : []
			this.starCount = ids.length

			const byId = {}
			boxes.forEach(b => {
				if (b && b.Box_id !== undefined) byId[String(b.Box_id)] = b
			})

			this.favorites = ids.slice(-6).reverse().map(id => ({
				id,
				count: boxCharacterCount(byId[String(id)])
			}))
		},

		async loadNotice() {
			try {
				const res = await fetchSelfHosted('Version', { data: { limit: 1, offset: 0 } })
				if (res && Array.isArray(res.objects) && res.objects.length) {
					const row = res.objects[0]
					this.noticeContent = row.notice || ''
					this.noticeVersion = row.Version || ''
				}
			} catch (e) {
				console.error('[desktop_home] notice', e)
			}
		},

		async loadBanners() {
			try {
				const res = await fetchSelfHosted('choearth_notice', { data: { limit: 10, offset: 0 } })
				const list = res && Array.isArray(res.objects) ? res.objects : []
				this.banners = list
					.filter(o => o && o.imageurl)
					.map(o => ({ img: o.imageurl, link: o.linkUrl || '', title: o.title || '' }))
			} catch (e) {
				console.error('[desktop_home] banners', e)
			}
		},

		openBanner(item) {
			if (item && item.link) openLink(item.link)
		},

		goSearch() {
			uni.navigateTo({ url: '/pages/func_search/func_search' })
		},

		open(url) {
			uni.reLaunch({ url })
		},

		openBox(id) {
			uni.navigateTo({ url: '/pages/box_info/box_info?boxId=' + encodeURIComponent(id) })
		}
	}
}
</script>

<style scoped>
.wh {
	min-height: 100vh;
	box-sizing: border-box;
	padding: 26px 30px 32px;
	background: #f4f5f7;
}

.wh-inner {
	max-width: 1180px;
	margin: 0 auto;
}

/* 顶部 */
.wh-top {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 24px;
	margin-bottom: 22px;
}

.wh-brand {
	display: flex;
	align-items: center;
	gap: 12px;
}

.wh-logo {
	width: 38px;
	height: 38px;
	border-radius: 10px;
}

.wh-title {
	display: block;
	font-size: 17px;
	font-weight: 600;
	color: #1b1e23;
	line-height: 1.25;
}

.wh-sub {
	display: block;
	font-size: 12px;
	color: #8b909a;
	margin-top: 2px;
}

.wh-search {
	display: flex;
	align-items: center;
	gap: 9px;
	flex: 0 1 380px;
	height: 42px;
	padding: 0 14px;
	background: #ffffff;
	border: 1px solid #e5e7eb;
	border-radius: 11px;
	cursor: pointer;
	transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.wh-search:hover {
	border-color: #c9ced6;
	box-shadow: 0 1px 6px rgba(15, 23, 42, 0.06);
}

.wh-search-icon {
	width: 15px;
	height: 15px;
	opacity: 0.55;
}

.wh-search-text {
	font-size: 13px;
	color: #9aa0a9;
}

/* 概览 */
.wh-stats {
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 10px;
}

.wh-stat {
	background: #ffffff;
	border: 1px solid #eceef2;
	border-radius: 13px;
	padding: 12px 16px;
	flex: 1;
	display: flex;
	flex-direction: column;
	justify-content: center;
	min-height: 0;
}

.wh-stat-n {
	display: block;
	font-size: 26px;
	font-weight: 600;
	color: #1b1e23;
	line-height: 1.2;
}

.wh-stat-l {
	display: block;
	font-size: 12px;
	color: #8b909a;
	margin-top: 3px;
}

/* 主区 */
.wh-grid {
	display: grid;
	grid-template-columns: 1.3fr 1fr;
	gap: 16px;
	margin-bottom: 16px;
	align-items: stretch;
}

.wh-card {
	background: #ffffff;
	border: 1px solid #eceef2;
	border-radius: 14px;
	padding: 18px 20px 20px;
	display: flex;
	flex-direction: column;
}

.wh-card-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 14px;
}

.wh-card-title {
	display: block;
	font-size: 14px;
	font-weight: 600;
	color: #1b1e23;
	margin-bottom: 14px;
}

.wh-card-head .wh-card-title {
	margin-bottom: 0;
}

.wh-badge {
	font-size: 11px;
	color: #6b7280;
	background: #f3f4f6;
	border-radius: 6px;
	padding: 2px 7px;
}

.wh-actions {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 10px;
}

.wh-action {
	padding: 13px 14px;
	border: 1px solid #eef0f3;
	border-radius: 10px;
	background: #fafbfc;
	cursor: pointer;
	transition: border-color 0.15s ease, background 0.15s ease;
}

.wh-action:hover {
	background: #f4f7fb;
	border-color: #d6e2f2;
}

.wh-action-name {
	display: block;
	font-size: 13px;
	font-weight: 500;
	color: #23272e;
}

.wh-action-desc {
	display: block;
	font-size: 11px;
	color: #949aa4;
	margin-top: 3px;
	line-height: 1.5;
}

/* 公告 */
.wh-notice-body {
	height: 176px;
	overflow-y: auto;
	overflow-x: hidden;
}

.wh-notice-body::-webkit-scrollbar {
	width: 6px;
}

.wh-notice-body::-webkit-scrollbar-thumb {
	background: rgba(120, 128, 140, 0.28);
	border-radius: 3px;
}

.wh-notice-text {
	font-size: 12px;
	color: #5f646d;
	line-height: 1.75;
	white-space: pre-wrap;
	word-break: break-word;
}

.wh-more {
	font-size: 12px;
	color: #4a7fd4;
	cursor: pointer;
	margin-top: 10px;
}

/* 收藏 */
.wh-fav-list {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
	gap: 10px;
}

.wh-fav-item {
	border: 1px solid #eef0f3;
	border-radius: 10px;
	padding: 12px 14px;
	cursor: pointer;
	background: #fafbfc;
}

.wh-fav-item:hover {
	border-color: #d6e2f2;
	background: #f4f7fb;
}

.wh-fav-id {
	display: block;
	font-size: 13px;
	font-weight: 500;
	color: #23272e;
}

.wh-fav-count {
	display: block;
	font-size: 11px;
	color: #949aa4;
	margin-top: 3px;
}

/* 状态 */
.wh-state {
	padding: 22px;
	text-align: center;
}

.wh-state-text {
	display: block;
	font-size: 13px;
	color: #5f646d;
}

.wh-state-hint {
	display: block;
	font-size: 11px;
	color: #a2a7b0;
	margin-top: 6px;
}

.wh-retry {
	display: inline-block;
	margin-top: 10px;
	font-size: 12px;
	color: #4a7fd4;
	cursor: pointer;
}

.wh-foot {
	margin-top: 18px;
	text-align: center;
}

.wh-foot-text {
	font-size: 11px;
	color: #a8adb6;
}

/* 深色主题 */
.theme-ark,
.theme-liquidDark {
	background: #14171a;
}

.theme-ark .wh-stat,
.theme-ark .wh-card,
.theme-liquidDark .wh-stat,
.theme-liquidDark .wh-card {
	background: #1c2024;
	border-color: #282d33;
}

.theme-ark .wh-title,
.theme-ark .wh-stat-n,
.theme-ark .wh-card-title,
.theme-ark .wh-action-name,
.theme-ark .wh-fav-id,
.theme-liquidDark .wh-title,
.theme-liquidDark .wh-stat-n,
.theme-liquidDark .wh-card-title,
.theme-liquidDark .wh-action-name,
.theme-liquidDark .wh-fav-id {
	color: #e8eaed;
}

.theme-ark .wh-search,
.theme-ark .wh-action,
.theme-ark .wh-fav-item,
.theme-liquidDark .wh-search,
.theme-liquidDark .wh-action,
.theme-liquidDark .wh-fav-item {
	background: #22272c;
	border-color: #2e343b;
}

.theme-ark .wh-sub,
.theme-ark .wh-stat-l,
.theme-ark .wh-action-desc,
.theme-ark .wh-fav-count,
.theme-ark .wh-notice-text,
.theme-liquidDark .wh-sub,
.theme-liquidDark .wh-stat-l,
.theme-liquidDark .wh-action-desc,
.theme-liquidDark .wh-fav-count,
.theme-liquidDark .wh-notice-text {
	color: #8d949e;
}

/* 轮播图 */
/* 首屏一行：左轮播 + 右统计 */
.wh-hero {
	display: grid;
	grid-template-columns: minmax(0, 680px) 200px;
	justify-content: center;
	gap: 14px;
	align-items: stretch;
	margin-top: 20px;
	margin-bottom: 16px;
}

.wh-banner {
	min-width: 0;
	display: flex;
	align-self: start;
}

.wh-swiper {
	width: 100%;
	height: auto;
	border-radius: 14px;
	overflow: hidden;
	background: #e9ebef;
	box-shadow: 0 2px 10px rgba(0, 0, 0, 0.06);
}

.wh-banner-img {
	width: 100%;
	height: 100%;
	display: block;
}

/* 下方内容与首屏对齐到同一列宽（首屏：680 + 14 + 200 = 894） */
.wh-grid,
.wh-fav {
	max-width: 894px;
	margin-left: auto;
	margin-right: auto;
}

/* 三栏在窄窗口退化为两栏：公告落到下一行 */
@media (min-width: 1000px) {
	html.is-win-shell .wh-actions {
		grid-template-columns: repeat(3, minmax(0, 1fr)) !important;
	}
}

@media (min-width: 1000px) {
	html.is-win-shell .wh-actions {
		grid-template-columns: repeat(3, minmax(0, 1fr)) !important;
	}
}

@media (max-width: 980px) {
	html.is-win-shell .wh-hero {
		grid-template-columns: minmax(0, 1fr) 180px;
	}

	html.is-win-shell .wh-banner {
		align-self: stretch;
	}

	html.is-win-shell .wh-swiper {
		height: 100%;
	}
}
</style>
