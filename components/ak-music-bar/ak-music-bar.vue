<template>
	<view class="ak-mb" :class="'ak-mb--' + theme">
		<!-- 展开的曲库列表：点封面/曲名区域收放 -->
		<view class="ak-mb-panel" v-if="listOpen">
			<view class="ak-mb-panel-head">
				<text class="ak-mb-panel-title">塞壬唱片 · 纯音乐</text>
				<text class="ak-mb-panel-count">{{ total }} 首</text>
				<view class="ak-mb-panel-close" @click="listOpen = false">
					<text class="ak-mb-panel-close-txt">×</text>
				</view>
			</view>
			<scroll-view class="ak-mb-scroll" scroll-y :scroll-into-view="scrollTarget" :scroll-with-animation="true">
				<view
					v-for="(t, i) in list"
					:key="t.cid"
					:id="'akmb-' + t.cid"
					class="ak-mb-row"
					:class="{ 'ak-mb-row--on': current && String(current.cid) === String(t.cid) }"
					@click="playTrack(t)"
				>
					<text class="ak-mb-row-idx">{{ i + 1 }}</text>
					<view class="ak-mb-row-main">
						<text class="ak-mb-row-name">{{ pretty(t) }}</text>
						<text class="ak-mb-row-album">{{ t.album || '未知专辑' }}</text>
					</view>
					<view class="ak-mb-eq" v-if="current && String(current.cid) === String(t.cid) && playing">
						<view class="ak-mb-eq-bar"></view>
						<view class="ak-mb-eq-bar"></view>
						<view class="ak-mb-eq-bar"></view>
					</view>
				</view>
				<view class="ak-mb-tip">
					曲目来自塞壬唱片官方公开资源，音频不随安装包分发，仅在线播放
				</view>
			</scroll-view>
		</view>

		<!-- 播放条本体 -->
		<view class="ak-mb-bar">
			<view class="ak-mb-track" @click="onSeek">
				<view class="ak-mb-track-in" :style="{ width: percent + '%' }"></view>
			</view>
			<view class="ak-mb-body">
				<view class="ak-mb-cover" @click="toggleList">
					<image
						v-if="showCover && current && current.cover"
						class="ak-mb-cover-img"
						:src="current.cover"
						mode="aspectFill"
					></image>
					<text v-else class="ak-mb-cover-ph">♪</text>
				</view>
				<view class="ak-mb-meta" @click="toggleList">
					<text class="ak-mb-name">{{ current ? pretty(current) : '塞壬唱片 · 纯音乐' }}</text>
					<text class="ak-mb-desc">{{ descText }}</text>
				</view>
				<view class="ak-mb-ctrl">
					<view class="ak-mb-btn" @click.stop="prev">
						<view class="ak-mb-ico-prev"></view>
					</view>
					<view class="ak-mb-btn ak-mb-btn--play" @click.stop="togglePlay">
						<view v-if="playing" class="ak-mb-ico-pause">
							<view class="ak-mb-ico-pause-bar"></view>
							<view class="ak-mb-ico-pause-bar"></view>
						</view>
						<view v-else class="ak-mb-ico-play"></view>
					</view>
					<view class="ak-mb-btn" @click.stop="next">
						<view class="ak-mb-ico-next"></view>
					</view>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
import { getCatalog, createPlayer, prettyName, fmtTime, K_LAST } from '@/utils/music.js';

export default {
	name: 'ak-music-bar',
	props: {
		// 跟随首页主题：simple / jieyuan / ak / ark
		theme: {
			type: String,
			default: 'simple'
		}
	},
	data() {
		return {
			list: [],
			total: 0,
			listOpen: false,
			current: null,
			playing: false,
			buffering: false,
			currentTime: 0,
			duration: 0,
			percent: 0,
			scrollTarget: '',
			// 封面只在 App 端用官方 CDN 原图。
			// web.hycdn.cn 有防盗链：带 Referer: https://app.local/ 会直接 403（实测），
			// PC 壳和浏览器里必然空白，不如统一走占位块，别露一个灰方块。
			showCover: false
		};
	},
	computed: {
		descText() {
			if (this.buffering) return '缓冲中…';
			if (!this.current) return '点播放，边查通行证边听';
			if (this.duration > 0) {
				return fmtTime(this.currentTime) + ' / ' + fmtTime(this.duration);
			}
			return this.playing ? '播放中…' : '已暂停';
		}
	},
	created() {
		// #ifdef APP-PLUS
		this.showCover = true;
		// #endif
		this.list = getCatalog();
		this.total = this.list.length;

		let last = null;
		try {
			last = uni.getStorageSync(K_LAST);
		} catch (e) {
			last = null;
		}
		if (last) {
			this.current = this.list.find((t) => String(t.cid) === String(last)) || null;
		}
		if (!this.current && this.list.length) this.current = this.list[0];

		this.player = createPlayer({
			onState: (s) => {
				this.playing = !!s.playing;
				this.buffering = !!s.buffering;
				if (s.track) this.current = s.track;
			},
			onProgress: (p) => {
				this.currentTime = p.current || 0;
				if (p.duration > 0) this.duration = p.duration;
				this.percent =
					this.duration > 0
						? Math.min(100, (this.currentTime / this.duration) * 100)
						: 0;
			},
			onEnded: () => {
				// 一首放完自动下一首，整张曲库循环
				this.step(1);
			},
			onError: (msg) => {
				this.percent = 0;
				uni.showToast({ title: msg || '播放失败', icon: 'none' });
			}
		});
	},
	beforeUnmount() {
		// 离开首页就停，别让背景音跟着跑到别的页面去
		try {
			this.player && this.player.pause();
			this.player && this.player.destroy();
		} catch (e) {}
	},
	methods: {
		pretty(t) {
			return prettyName(t);
		},
		indexOfCurrent() {
			if (!this.current) return -1;
			return this.list.findIndex((t) => String(t.cid) === String(this.current.cid));
		},
		playTrack(t) {
			if (!t) return;
			this.current = t;
			this.currentTime = 0;
			this.duration = 0;
			this.percent = 0;
			try {
				uni.setStorageSync(K_LAST, t.cid);
			} catch (e) {}
			this.scrollTarget = 'akmb-' + t.cid;
			this.player.play(t);
		},
		togglePlay() {
			if (!this.current) {
				if (this.list.length) this.playTrack(this.list[0]);
				return;
			}
			if (this.playing) {
				this.player.pause();
			} else if (this.player.current() > 0.5) {
				this.player.resume();
			} else {
				this.player.play(this.current);
			}
		},
		step(delta) {
			if (!this.list.length) return;
			const i = this.indexOfCurrent();
			const n = this.list.length;
			const nextIdx = i < 0 ? 0 : (i + delta + n) % n;
			this.playTrack(this.list[nextIdx]);
		},
		prev() {
			this.step(-1);
		},
		next() {
			this.step(1);
		},
		toggleList() {
			this.listOpen = !this.listOpen;
			if (this.listOpen && this.current) {
				this.scrollTarget = 'akmb-' + this.current.cid;
			}
		},
		onSeek(e) {
			if (!this.duration) return;
			const ev = e || {};
			const pick = () => {
				const d = ev.detail || {};
				if (typeof d.x === 'number') return d.x;
				if (typeof ev.clientX === 'number') return ev.clientX;
				if (typeof ev.pageX === 'number') return ev.pageX;
				const t =
					(ev.touches && ev.touches[0]) ||
					(ev.changedTouches && ev.changedTouches[0]);
				if (t && typeof t.clientX === 'number') return t.clientX;
				return null;
			};
			const x = pick();
			if (x === null) return;
			uni.createSelectorQuery()
				.in(this)
				.select('.ak-mb-track')
				.boundingClientRect((rect) => {
					if (!rect || !rect.width) return;
					const ratio = Math.min(1, Math.max(0, (x - rect.left) / rect.width));
					this.percent = ratio * 100;
					this.currentTime = ratio * this.duration;
					this.player.seek(ratio * this.duration);
				})
				.exec();
		}
	}
};
</script>

<style scoped>
/* ---------------------------------------------------------------------------
   配色走组件内私有变量。
   不直接沿用 common/ak-theme.css 里的 --ak-* 令牌：那套变量定义在 .container
   上，而音乐条是 .container 的兄弟节点（fixed 定位），继承不到。
   所以这里按主题各给一份本地令牌，取值与 ak-ui 调色板一致。
   --------------------------------------------------------------------------- */

.ak-mb {
	--mb-surface: #ffffff;
	--mb-surface-2: #f7f7f7;
	--mb-text: #333333;
	--mb-sub: #999999;
	--mb-muted: #bbbbbb;
	--mb-ctrl: #666666;
	--mb-accent: #409eff;
	--mb-on-accent: #ffffff;
	--mb-hairline: rgba(0, 0, 0, 0.06);
	--mb-hover: #f2f8ff;
	--mb-radius: 22rpx;
	--mb-shadow: 0 -4rpx 24rpx rgba(0, 0, 0, 0.1);
	--mb-font-mono: "SF Mono", SFMono-Regular, Menlo, Consolas, "Courier New", monospace;
	/* 卡片下方那条 12rpx 留白要糊住，否则页面文字会从缝里透出来 */
	--mb-plate: #f5f5f5;

	position: fixed;
	left: 0;
	right: 0;
	bottom: 0;
	z-index: 90;
	/* 整块托盘：底色取画布色，卡片外面的缝也由它兜住，
	   否则页面滚动时文字会从缝里透出来（实测很难看） */
	background: var(--mb-plate);
	border-top: 1rpx solid var(--mb-hairline);
	padding: 0 20rpx;
	padding-bottom: calc(12rpx + env(safe-area-inset-bottom));
	box-sizing: border-box;
}

/* 界园（粉绿） */
.ak-mb--jieyuan {
	--mb-surface: rgba(255, 255, 255, 0.96);
	--mb-text: #333333;
	--mb-sub: #9b8f92;
	--mb-muted: #c0b4b7;
	--mb-ctrl: #7a6a6e;
	--mb-accent: #e25884;
	--mb-on-accent: #ffffff;
	--mb-hairline: rgba(226, 88, 132, 0.16);
	--mb-hover: rgba(226, 88, 132, 0.08);
	--mb-plate: #f5ede8;
	--mb-shadow: 0 -4rpx 24rpx rgba(190, 120, 145, 0.18);
}

/* 工业浅色（ak-ui 纸面） */
.ak-mb--ak {
	--mb-surface: #fdfdfb;
	--mb-text: #1b1d1f;
	--mb-sub: rgba(27, 29, 31, 0.68);
	--mb-muted: #8d908e;
	--mb-ctrl: #3f4245;
	--mb-accent: #f6540e;
	--mb-on-accent: #f8f8f5;
	--mb-hairline: rgba(27, 29, 31, 0.16);
	--mb-hover: #f0f1ed;
	--mb-plate: #efefec;
	--mb-radius: 6rpx;
	--mb-shadow: 0 -2rpx 12rpx rgba(20, 22, 23, 0.12);
}

/* 工业深色（ak-ui 石墨） */
.ak-mb--ark {
	--mb-surface: #1b1d1e;
	--mb-text: #ecebe6;
	--mb-sub: rgba(236, 235, 230, 0.66);
	--mb-muted: #8b8f8c;
	--mb-ctrl: #c9c8c2;
	--mb-accent: #ff6a26;
	--mb-on-accent: #131415;
	--mb-hairline: rgba(255, 255, 255, 0.14);
	--mb-hover: #2c3032;
	--mb-plate: #131415;
	--mb-radius: 6rpx;
	--mb-shadow: 0 -4rpx 24rpx rgba(0, 0, 0, 0.5);
}

/* ---------- 曲库面板 ---------- */
.ak-mb-panel {
	margin-bottom: 12rpx;
	border-radius: var(--mb-radius);
	overflow: hidden;
	background: var(--mb-surface);
	border: 1rpx solid var(--mb-hairline);
	box-shadow: var(--mb-shadow);
}

.ak-mb-panel-head {
	display: flex;
	align-items: center;
	padding: 20rpx 24rpx;
	border-bottom: 1rpx solid var(--mb-hairline);
}

.ak-mb-panel-title {
	font-size: 28rpx;
	font-weight: 600;
	color: var(--mb-text);
	flex: 1;
}

.ak-mb-panel-count {
	font-size: 22rpx;
	color: var(--mb-sub);
	margin-right: 16rpx;
	font-family: var(--mb-font-mono);
}

.ak-mb-panel-close {
	width: 52rpx;
	height: 52rpx;
	display: flex;
	align-items: center;
	justify-content: center;
}

.ak-mb-panel-close-txt {
	font-size: 40rpx;
	line-height: 1;
	color: var(--mb-sub);
}

.ak-mb-scroll {
	max-height: 46vh;
	box-sizing: border-box;
}

.ak-mb-row {
	display: flex;
	align-items: center;
	padding: 18rpx 24rpx;
	border-bottom: 1rpx solid var(--mb-hairline);
}

.ak-mb-row--on {
	background: var(--mb-hover);
}

.ak-mb-row-idx {
	width: 52rpx;
	font-size: 22rpx;
	color: var(--mb-muted);
	flex-shrink: 0;
	font-family: var(--mb-font-mono);
}

.ak-mb-row-main {
	flex: 1;
	overflow: hidden;
	display: flex;
	flex-direction: column;
}

.ak-mb-row-name {
	font-size: 26rpx;
	color: var(--mb-text);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.ak-mb-row--on .ak-mb-row-name,
.ak-mb-row--on .ak-mb-row-idx {
	color: var(--mb-accent);
}

.ak-mb-row-album {
	font-size: 20rpx;
	color: var(--mb-muted);
	margin-top: 4rpx;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

/* 播放中的跳动小柱 */
.ak-mb-eq {
	display: flex;
	align-items: flex-end;
	height: 26rpx;
	margin-left: 12rpx;
}

.ak-mb-eq-bar {
	width: 4rpx;
	margin-left: 3rpx;
	background: var(--mb-accent);
	border-radius: 1rpx;
	animation: ak-mb-jump 0.9s ease-in-out infinite;
}

.ak-mb-eq-bar:nth-child(1) {
	height: 10rpx;
	animation-delay: 0s;
}

.ak-mb-eq-bar:nth-child(2) {
	height: 22rpx;
	animation-delay: 0.18s;
}

.ak-mb-eq-bar:nth-child(3) {
	height: 14rpx;
	animation-delay: 0.36s;
}

@keyframes ak-mb-jump {
	0%,
	100% {
		transform: scaleY(0.45);
	}
	50% {
		transform: scaleY(1);
	}
}

.ak-mb-tip {
	padding: 18rpx 24rpx 24rpx;
	font-size: 20rpx;
	color: var(--mb-muted);
	line-height: 1.5;
	text-align: center;
}

/* ---------- 播放条本体 ---------- */
.ak-mb-bar {
	position: relative;
	border-radius: var(--mb-radius);
	background: var(--mb-surface);
	border: 1rpx solid var(--mb-hairline);
	box-shadow: var(--mb-shadow);
	overflow: hidden;
}

/* 进度条：视觉 4rpx，热区 16rpx，方便手指点 */
.ak-mb-track {
	position: absolute;
	top: 0;
	left: 0;
	right: 0;
	height: 16rpx;
	display: flex;
	align-items: flex-start;
	z-index: 2;
}

.ak-mb-track-in {
	height: 4rpx;
	background: var(--mb-accent);
	transition: width 0.2s linear;
}

.ak-mb-body {
	display: flex;
	align-items: center;
	padding: 18rpx 16rpx 14rpx;
}

.ak-mb-cover {
	width: 76rpx;
	height: 76rpx;
	border-radius: calc(var(--mb-radius) - 8rpx);
	overflow: hidden;
	background: var(--mb-hover);
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
}

.ak-mb-cover-img {
	width: 100%;
	height: 100%;
	display: block;
}

.ak-mb-cover-ph {
	font-size: 34rpx;
	line-height: 1;
	color: var(--mb-accent);
	opacity: 0.7;
}

.ak-mb-meta {
	flex: 1;
	overflow: hidden;
	padding: 0 16rpx;
	display: flex;
	flex-direction: column;
}

.ak-mb-name {
	font-size: 26rpx;
	color: var(--mb-text);
	font-weight: 600;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.ak-mb-desc {
	font-size: 21rpx;
	color: var(--mb-sub);
	margin-top: 4rpx;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	font-family: var(--mb-font-mono);
}

.ak-mb-ctrl {
	display: flex;
	align-items: center;
	flex-shrink: 0;
}

.ak-mb-btn {
	width: 56rpx;
	height: 56rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--mb-ctrl);
}

.ak-mb-btn--play {
	width: 68rpx;
	height: 68rpx;
	border-radius: 50%;
	background: var(--mb-accent);
	color: var(--mb-on-accent);
	margin: 0 6rpx;
}

/* 工业两套主题去掉圆胶囊，改方形按钮 */
.ak-mb--ak .ak-mb-btn--play,
.ak-mb--ark .ak-mb-btn--play {
	border-radius: 4rpx;
}

/* CSS 画的三角/方块图标，避免依赖图片资源 */
.ak-mb-ico-play {
	width: 0;
	height: 0;
	border-style: solid;
	border-width: 13rpx 0 13rpx 20rpx;
	border-color: transparent transparent transparent currentColor;
	margin-left: 6rpx;
}

.ak-mb-ico-pause {
	display: flex;
	align-items: center;
	justify-content: center;
}

.ak-mb-ico-pause-bar {
	width: 6rpx;
	height: 22rpx;
	background: currentColor;
	border-radius: 1rpx;
	margin: 0 3rpx;
}

.ak-mb-ico-prev {
	width: 0;
	height: 0;
	border-style: solid;
	border-width: 11rpx 16rpx 11rpx 0;
	border-color: transparent currentColor transparent transparent;
}

.ak-mb-ico-next {
	width: 0;
	height: 0;
	border-style: solid;
	border-width: 11rpx 0 11rpx 16rpx;
	border-color: transparent transparent transparent currentColor;
}
</style>
