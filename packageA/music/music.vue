<template>
	<view :class="['container', 'theme-' + themeMode, 'music-page']">
		<!-- 顶部品牌条 -->
		<view class="msr-hero">
			<text class="msr-kicker">MONSTER SIREN RECORDS</text>
			<view class="msr-title-row">
				<text class="msr-title">塞壬唱片</text>
				<text class="msr-title-en">INSTRUMENTAL</text>
			</view>
			<text class="msr-sub">{{ total }} 首纯音乐 · 官方音源直出，不随包分发</text>
		</view>

		<!-- 正在播放 -->
		<view class="np-card" :class="{ 'np-idle': !current }">
			<view class="np-main">
				<view class="np-cover" @click="toggle">
					<image v-if="coverSrc" class="np-cover-img" :src="coverSrc" mode="aspectFill"></image>
					<view v-else class="np-cover-fallback">
						<text class="np-cover-glyph">MSR</text>
					</view>
					<view class="np-cover-mask" v-if="busy">
						<text class="np-cover-busy">载入中…</text>
					</view>
				</view>

				<view class="np-info">
					<text class="np-name">{{ current ? current.name : '还没选曲' }}</text>
					<text class="np-album">{{ current ? current.album || '塞壬唱片' : '从下面挑一首试试' }}</text>
					<view class="np-progress">
						<slider
							class="np-slider"
							:value="percent"
							:min="0"
							:max="100"
							:step="0.1"
							:block-size="12"
							activeColor="#f1c644"
							backgroundColor="rgba(255,255,255,0.18)"
							block-color="#f1c644"
							@changing="onSeekChanging"
							@change="onSeekChange"
						/>
						<view class="np-times">
							<text>{{ fmtTime(shownPosition) }}</text>
							<text>{{ fmtTime(duration) }}</text>
						</view>
					</view>
				</view>
			</view>

			<view class="np-controls">
				<view class="np-btn ghost" :class="{ active: loop }" @click="toggleLoop">
					<text class="np-btn-t">循环</text>
				</view>
				<view class="np-btn" @click="playPrev">
					<text class="np-btn-ic">⏮</text>
				</view>
				<view class="np-btn primary" @click="toggle">
					<text class="np-btn-ic big">{{ playing ? '⏸' : '▶' }}</text>
				</view>
				<view class="np-btn" @click="playNext">
					<text class="np-btn-ic">⏭</text>
				</view>
				<view class="np-btn ghost" :class="{ active: shuffle }" @click="toggleShuffle">
					<text class="np-btn-t">随机</text>
				</view>
			</view>

			<text class="np-err" v-if="errorText">{{ errorText }}</text>
		</view>

		<!-- 搜索 -->
		<view class="msr-search">
			<input
				class="msr-input"
				v-model="keyword"
				placeholder="搜曲名或专辑"
				placeholder-class="msr-ph"
				confirm-type="search"
			/>
			<text class="msr-clear" v-if="keyword" @click="keyword = ''">✕</text>
		</view>

		<!-- 曲目列表 -->
		<view class="msr-list">
			<view
				class="msr-row"
				v-for="(t, i) in filtered"
				:key="t.cid"
				:class="{ on: current && current.cid === t.cid }"
				@click="playTrack(t)"
			>
				<text class="msr-idx">{{ playing && current && current.cid === t.cid ? (busy ? '·' : '▮') : i + 1 }}</text>
				<view class="msr-row-main">
					<text class="msr-row-name">{{ t.name }}</text>
					<text class="msr-row-album">{{ t.album || '—' }}</text>
				</view>
				<text class="msr-row-badge">纯音乐</text>
			</view>

			<view class="msr-empty" v-if="!filtered.length">
				<text>没有匹配的曲目</text>
			</view>
		</view>

		<view class="msr-foot">
			<text>曲库来自塞壬唱片官方站（monster-siren.hypergryph.com），版权归原厂所有；本工具仅做播放，不存储、不分发音频文件。</text>
		</view>
	</view>
</template>

<script>
	import {
		getCatalog,
		searchCatalog,
		createPlayer,
		fmtTime
	} from '@/utils/music.js';

	const K_LAST = 'musicLastCid';

	export default {
		data() {
			return {
				themeMode: 'simple',
				keyword: '',
				tracks: [],
				current: null,
				playing: false,
				busy: false,
				position: 0,
				seekPreview: 0,
				duration: 0,
				loop: false,
				shuffle: false,
				errorText: ''
			};
		},
		computed: {
			total() {
				return this.tracks.length;
			},
			filtered() {
				return searchCatalog(this.keyword);
			},
			coverSrc() {
				return (this.current && this.current.cover) || '';
			},
			shownPosition() {
				return this.seekPreview > 0 ? this.seekPreview : this.position;
			},
			percent() {
				if (this.duration && this.seekPreview > 0) {
					return Math.min(100, (this.seekPreview / this.duration) * 100);
				}
				if (!this.duration) return 0;
				return Math.min(100, (this.position / this.duration) * 100);
			}
		},
		onLoad() {
			try {
				const tm = uni.getStorageSync('themeMode');
				const ok = ['simple', 'ak', 'ark', 'jieyuan', 'liquid', 'liquidDark'];
				this.themeMode = ok.indexOf(tm) >= 0 ? tm : 'simple';
			} catch (e) {}

			this.tracks = getCatalog();
			this.player = createPlayer({
				onState: (s) => {
					this.playing = !!s.playing;
					this.busy = !!s.buffering;
					if (s.track) this.current = s.track;
				},
				onProgress: (p) => {
					this.position = p.current || 0;
					this.duration = p.duration || 0;
				},
				onEnded: () => {
					if (this.loop && this.current) {
						this.player.play(this.current);
						return;
					}
					this.playNext();
				},
				onError: (msg) => {
					this.errorText = this.describeError(msg);
					this.busy = false;
				}
			});

			// 恢复上次听到的曲目（只恢复选中，不自动播 —— 冷启动自动出声很讨嫌）
			try {
				const last = uni.getStorageSync(K_LAST);
				if (last) {
					const t = this.tracks.find((x) => String(x.cid) === String(last));
					if (t) this.current = t;
				}
			} catch (e) {}
		},
		onUnload() {
			try {
				this.player && this.player.destroy();
			} catch (e) {}
			this.player = null;
		},
		methods: {
			fmtTime,
			describeError(msg) {
				const m = String(msg || '');
				if (/cross|CORS|origin/i.test(m)) return '这个环境不允许跨域取音源，请在 App 或桌面端收听。';
				if (/timeout|网络|network/i.test(m)) return '网络不给力，音源加载失败，稍后再试。';
				return m || '播放失败';
			},
			playTrack(t) {
				if (!t) return;
				this.errorText = '';
				if (this.current && this.current.cid === t.cid && this.playing) {
					this.player.pause();
					return;
				}
				this.current = t;
				this.busy = true;
				try {
					uni.setStorageSync(K_LAST, t.cid);
				} catch (e) {}
				this.player.play(t);
			},
			toggle() {
				if (!this.current) {
					const first = this.filtered[0];
					if (first) this.playTrack(first);
					return;
				}
				if (!this.player) return;
				if (!this.playing && this.position === 0) {
					this.player.play(this.current);
					return;
				}
				this.player.toggle();
			},
			queue() {
				return this.filtered.length ? this.filtered : this.tracks;
			},
			playNext() {
				const q = this.queue();
				if (!q.length) return;
				let idx = this.current ? q.findIndex((x) => x.cid === this.current.cid) : -1;
				if (this.shuffle) {
					let n = idx;
					if (q.length > 1) {
						while (n === idx) n = Math.floor(Math.random() * q.length);
					}
					this.playTrack(q[Math.max(0, n)]);
					return;
				}
				this.playTrack(q[(idx + 1 + q.length) % q.length]);
			},
			playPrev() {
				const q = this.queue();
				if (!q.length) return;
				const idx = this.current ? q.findIndex((x) => x.cid === this.current.cid) : 0;
				if (this.shuffle) {
					this.playNext();
					return;
				}
				this.playTrack(q[(idx - 1 + q.length) % q.length]);
			},
			toggleLoop() {
				this.loop = !this.loop;
				if (this.loop) this.shuffle = false;
			},
			toggleShuffle() {
				this.shuffle = !this.shuffle;
				if (this.shuffle) this.loop = false;
			},
			onSeekChanging(e) {
				// 拖动时只更新显示，不狂发 seek（真机上会很卡）
				this.seekPreview = (Number(e.detail.value) / 100) * this.duration;
			},
			onSeekChange(e) {
				this.seekPreview = 0;
				if (!this.duration) return;
				const ratio = Math.min(1, Math.max(0, Number(e.detail.value) / 100));
				this.player.seek(this.duration * ratio);
			}
		}
	};
</script>

<style scoped>
	.music-page {
		padding-bottom: 120rpx;
	}

	/* ---------- hero ---------- */
	.msr-hero {
		padding: 40rpx 32rpx 18rpx;
	}
	.msr-kicker {
		font-size: 20rpx;
		letter-spacing: 0.3em;
		color: var(--ak-text-muted);
	}
	.msr-title-row {
		display: flex;
		align-items: baseline;
		margin-top: 10rpx;
	}
	.msr-title {
		font-size: 52rpx;
		font-weight: 800;
		letter-spacing: 0.06em;
		color: var(--ak-text-primary);
	}
	.msr-title-en {
		margin-left: 16rpx;
		font-size: 22rpx;
		letter-spacing: 0.22em;
		color: var(--ak-color-accent);
	}
	.msr-sub {
		display: block;
		margin-top: 12rpx;
		font-size: 24rpx;
		color: var(--ak-text-secondary);
	}

	/* ---------- 正在播放 ---------- */
	.np-card {
		margin: 18rpx 24rpx 24rpx;
		padding: 26rpx;
		border-radius: 24rpx;
		background: linear-gradient(160deg, #22262a 0%, #14171a 100%);
		border: 1rpx solid rgba(255, 255, 255, 0.08);
		box-shadow: 0 18rpx 48rpx rgba(12, 14, 16, 0.28);
	}
	.np-main {
		display: flex;
	}
	.np-cover {
		position: relative;
		width: 196rpx;
		height: 196rpx;
		border-radius: 16rpx;
		overflow: hidden;
		background: #2c3136;
		flex-shrink: 0;
	}
	.np-cover-img {
		width: 100%;
		height: 100%;
	}
	.np-cover-fallback {
		width: 100%;
		height: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		background: repeating-linear-gradient(135deg, #2a2f33 0 12rpx, #22262a 12rpx 24rpx);
	}
	.np-cover-glyph {
		font-size: 30rpx;
		letter-spacing: 0.2em;
		color: rgba(255, 255, 255, 0.42);
	}
	.np-cover-mask {
		position: absolute;
		top: 0;
		right: 0;
		bottom: 0;
		left: 0;
		background: rgba(10, 12, 14, 0.45);
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.np-cover-busy {
		font-size: 22rpx;
		color: #f1c644;
	}
	.np-info {
		flex: 1;
		min-width: 0;
		padding-left: 22rpx;
	}
	.np-name {
		display: block;
		font-size: 34rpx;
		font-weight: 700;
		color: #f4f5f2;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
	.np-album {
		display: block;
		margin-top: 8rpx;
		font-size: 23rpx;
		color: rgba(244, 245, 242, 0.55);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
	.np-progress {
		margin-top: 22rpx;
	}
	.np-slider {
		margin: 0 -18rpx;
	}
	.np-times {
		display: flex;
		justify-content: space-between;
		margin-top: 10rpx;
		font-size: 20rpx;
		color: rgba(244, 245, 242, 0.45);
	}
	.np-controls {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 24rpx;
	}
	.np-btn {
		min-width: 92rpx;
		height: 88rpx;
		border-radius: 44rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		background: rgba(255, 255, 255, 0.07);
		border: 1rpx solid rgba(255, 255, 255, 0.09);
	}
	.np-btn.primary {
		width: 108rpx;
		height: 108rpx;
		border-radius: 54rpx;
		background: linear-gradient(160deg, #f1c644, #e6a02c);
		border: none;
	}
	.np-btn.ghost {
		background: transparent;
	}
	.np-btn.active {
		border-color: rgba(241, 198, 68, 0.7);
	}
	.np-btn.active .np-btn-t {
		color: #f1c644;
	}
	.np-btn-ic {
		font-size: 34rpx;
		color: #e8eae6;
	}
	.np-btn-ic.big {
		font-size: 42rpx;
		color: #1b1d1f;
	}
	.np-btn-t {
		font-size: 22rpx;
		color: rgba(232, 234, 230, 0.7);
	}
	.np-idle .np-name {
		color: rgba(244, 245, 242, 0.7);
	}
	.np-err {
		display: block;
		margin-top: 18rpx;
		font-size: 22rpx;
		color: #ff8c7a;
	}

	/* ---------- 搜索 ---------- */
	.msr-search {
		position: relative;
		margin: 0 24rpx 16rpx;
	}
	.msr-input {
		height: 82rpx;
		padding: 0 68rpx 0 26rpx;
		border-radius: 16rpx;
		font-size: 27rpx;
		background: var(--ak-surface-panel);
		color: var(--ak-text-primary);
		border: 1rpx solid var(--ak-hairline-soft);
	}
	.msr-ph {
		color: var(--ak-text-muted);
	}
	.msr-clear {
		position: absolute;
		right: 24rpx;
		top: 24rpx;
		font-size: 26rpx;
		color: var(--ak-text-muted);
	}

	/* ---------- 列表 ---------- */
	.msr-list {
		margin: 0 24rpx;
		border-radius: 20rpx;
		overflow: hidden;
		background: var(--ak-surface-panel);
		border: 1rpx solid var(--ak-hairline-soft);
	}
	.msr-row {
		display: flex;
		align-items: center;
		padding: 22rpx 24rpx;
		border-bottom: 1rpx solid var(--ak-hairline-soft);
	}
	.msr-row:last-child {
		border-bottom: none;
	}
	.msr-row.on {
		background: rgba(246, 84, 14, 0.07);
	}
	.msr-row.on .msr-row-name {
		color: var(--ak-color-accent);
	}
	.msr-idx {
		width: 52rpx;
		font-size: 22rpx;
		color: var(--ak-text-muted);
		font-variant-numeric: tabular-nums;
	}
	.msr-row-main {
		flex: 1;
		min-width: 0;
	}
	.msr-row-name {
		display: block;
		font-size: 28rpx;
		font-weight: 600;
		color: var(--ak-text-primary);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
	.msr-row-album {
		display: block;
		margin-top: 6rpx;
		font-size: 22rpx;
		color: var(--ak-text-muted);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
	.msr-row-badge {
		margin-left: 16rpx;
		padding: 4rpx 12rpx;
		border-radius: 6rpx;
		font-size: 19rpx;
		color: var(--ak-text-secondary);
		background: var(--ak-surface-muted);
		flex-shrink: 0;
	}
	.msr-empty {
		padding: 60rpx 0;
		text-align: center;
		font-size: 25rpx;
		color: var(--ak-text-muted);
	}
	.msr-foot {
		padding: 26rpx 32rpx 0;
		font-size: 20rpx;
		line-height: 1.7;
		color: var(--ak-text-muted);
	}
</style>
