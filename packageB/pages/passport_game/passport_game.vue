<template>
	<view class="pg" :class="isDark ? 'pg-dark' : 'pg-light'">

		<!-- ===================== 加载中 ===================== -->
		<view class="pg-loading" v-if="loading">
			<view class="pg-loading-bar"><view class="pg-loading-fill"></view></view>
			<text class="pg-loading-text">正在读取盒号数据…</text>
		</view>

		<!-- ===================== 数据异常 ===================== -->
		<view class="pg-empty" v-else-if="!ready">
			<text class="pg-empty-title">数据还没准备好</text>
			<text class="pg-empty-desc">先回首页让工具同步一次盒号数据，再回来玩。</text>
			<view class="btn-solid" @click="loadData"><text>重新加载</text></view>
		</view>

		<!-- ===================== 大厅 ===================== -->
		<view v-else-if="scene === 'home'">
			<view class="pg-hero">
				<text class="hero-kicker">PASSPORT PLAYGROUND</text>
				<text class="hero-title">通行证游乐场</text>
				<text class="hero-desc">题目全部由工具收录的 {{ stats.boxCount }} 个盒号现场生成，答案不写死，每局都不一样。</text>
				<view class="hero-quota">
					<text class="hero-quota-num">{{ quotaLabel }}</text>
					<text class="hero-quota-hint">{{ quotaHint }}</text>
				</view>
				<view class="hero-unlock" v-if="!quota.unlimited" @click="openQuotaSheet">
					<text>次数不够？点这里</text>
				</view>
			</view>

			<view class="game-list">
				<view class="game-card" v-for="g in games" :key="g.key" @click="startGame(g.key)">
					<view class="game-no"><text>{{ g.no }}</text></view>
					<view class="game-main">
						<text class="game-name">{{ g.name }}</text>
						<text class="game-desc">{{ g.desc }}</text>
						<text class="game-meta">{{ g.meta }}</text>
					</view>
					<view class="game-chev"></view>
				</view>
			</view>

			<view class="home-extra" @click="goGacha">
				<view class="extra-main">
					<text class="extra-name">模拟抽卡</text>
					<text class="extra-desc">按盒抽卡 / 线下对战，老地方还在</text>
				</view>
				<view class="game-chev"></view>
			</view>

			<view class="home-foot">
				<text class="foot-line">每天 {{ basePlays }} 次基础机会，分享战绩 / 当日首胜各 +1，最多 {{ maxPlays }} 次</text>
				<text class="foot-line">多玩不宜 —— 次日 0 点自动重置</text>
				<text class="foot-line foot-credit">题目数据来源：本工具收录的通行证盒号数据</text>
			</view>
		</view>

		<!-- ===================== 游戏一：通行证竞猜 ===================== -->
		<view v-else-if="scene === 'quiz' && quizQ">
			<view class="bar">
				<view class="bar-left">
					<text class="bar-kicker">通行证竞猜</text>
					<text class="bar-title">第 {{ quizIdx + 1 }} / {{ quiz.qs.length }} 题</text>
				</view>
				<view class="bar-right">
					<text class="bar-score">{{ quiz.correct }}</text>
					<text class="bar-score-unit">答对</text>
				</view>
			</view>

			<view class="progress">
				<view class="progress-fill" :style="{ width: quizProgress }"></view>
			</view>

			<view class="q-card">
				<view class="q-tag"><text>{{ quizQ.tag }}</text></view>
				<text class="q-prompt">{{ quizQ.prompt }}</text>
			</view>

			<view class="opt-list">
				<view
					class="opt"
					v-for="(opt, i) in quizQ.options"
					:key="i"
					:class="optClass(i)"
					@click="chooseQuiz(i)"
				>
					<text class="opt-key">{{ letters[i] }}</text>
					<view class="opt-body">
						<text class="opt-text">{{ opt.text }}</text>
						<text class="opt-sub" v-if="opt.sub">{{ opt.sub }}</text>
					</view>
					<text class="opt-mark" v-if="quizLocked && i === quizQ.answer">✓</text>
					<text class="opt-mark" v-else-if="quizLocked && i === quizSelected && i !== quizQ.answer">×</text>
				</view>
			</view>

			<view class="explain" v-if="quizLocked">
				<text class="explain-title">{{ quizSelected === quizQ.answer ? '答对了' : '答错了' }}</text>
				<text class="explain-text">{{ quizQ.explain }}</text>
			</view>

			<view class="quiz-foot">
				<view class="link-btn" @click="openFeedback(quizQ)"><text>这题有问题？反馈一下</text></view>
				<view class="btn-solid next-btn" :class="{ disabled: !quizLocked }" @click="nextQuiz">
					<text>{{ quizIdx + 1 >= quiz.qs.length ? '看结果' : '下一题' }}</text>
				</view>
			</view>
		</view>

		<!-- ===================== 游戏二：盒号速配 ===================== -->
		<view v-else-if="scene === 'duel' && duel.q">
			<view class="bar">
				<view class="bar-left">
					<text class="bar-kicker">盒号速配</text>
					<text class="bar-title">{{ duel.score }} 分 · 连对 {{ duel.streak }}</text>
				</view>
				<view class="bar-right">
					<text class="bar-score">{{ duelLeftText }}</text>
					<text class="bar-score-unit">剩余</text>
				</view>
			</view>

			<view class="progress">
				<view class="progress-fill" :class="{ urgent: duel.left <= 10 }" :style="{ width: duelProgress }"></view>
			</view>

			<view class="duel-stage">
				<text class="duel-ask">这位干员在盒号里吗？</text>
				<text class="duel-name">{{ duel.q.name }}</text>
				<text class="duel-box">盒号 {{ duel.q.boxId }}</text>
			</view>

			<view class="duel-actions">
				<view class="duel-btn yes" @click="answerDuel(true)"><text>在</text></view>
				<view class="duel-btn no" @click="answerDuel(false)"><text>不在</text></view>
			</view>

			<view class="duel-flash" v-if="duel.flash">
				<text class="duel-flash-text">{{ duel.flash }}</text>
			</view>

			<view class="quiz-foot">
				<view class="link-btn" @click="quitGame"><text>结束这一局</text></view>
				<view class="foot-tip" v-if="duelBest > 0"><text>最好成绩 {{ duelBest }} 分</text></view>
			</view>
		</view>

		<!-- ===================== 游戏三：通行证接龙 ===================== -->
		<view v-else-if="scene === 'chain' && chain.round">
			<view class="bar">
				<view class="bar-left">
					<text class="bar-kicker">通行证接龙</text>
					<text class="bar-title">已接 {{ chain.steps }} 环</text>
				</view>
				<view class="bar-right">
					<text class="bar-score">盒号</text>
					<text class="bar-score-unit">{{ chain.currentId }}</text>
				</view>
			</view>

			<view class="chain-box">
				<text class="chain-label">当前盒号里的干员</text>
				<view class="chip-wrap">
					<view class="chip" v-for="(n, i) in chain.round.currentNames" :key="i">
						<text class="chip-text">{{ n }}</text>
					</view>
				</view>
			</view>

			<view class="q-card">
				<view class="q-tag"><text>接下一环</text></view>
				<text class="q-prompt">下面哪个盒号和它有共同干员？</text>
			</view>

			<view class="chain-list">
				<view
					class="chain-opt"
					v-for="(opt, i) in chain.round.options"
					:key="i"
					:class="chainOptClass(opt.id)"
					@click="answerChain(opt.id)"
				>
					<view class="chain-opt-body">
						<text class="chain-opt-text">{{ opt.id }}</text>
						<text class="chain-opt-names">{{ (opt.names || []).join(' · ') }}</text>
					</view>
					<text class="opt-mark" v-if="chain.reveal && opt.id === chain.round.answerId">✓</text>
					<text class="opt-mark" v-else-if="chain.reveal && opt.id === chain.picked">×</text>
				</view>
			</view>

			<text class="chain-note" v-if="chain.note">{{ chain.note }}</text>

			<view class="path-line" v-if="chain.path.length">
				<text class="path-text">{{ chainPathText }}</text>
			</view>

			<view class="quiz-foot">
				<view class="link-btn" @click="quitGame"><text>结束这一局</text></view>
				<view class="foot-tip" v-if="chainBest > 0"><text>最好成绩 {{ chainBest }} 环</text></view>
			</view>
		</view>

		<!-- ===================== 结算 ===================== -->
		<view v-else-if="scene === 'result' && result">
			<view class="result">
				<text class="result-kicker">{{ result.kicker }}</text>
				<text class="result-title">{{ result.title }}</text>
				<text class="result-score">{{ result.scoreText }}</text>
				<text class="result-sub">{{ result.sub }}</text>

				<view class="result-lines" v-if="result.lines.length">
					<text class="result-line" v-for="(l, i) in result.lines" :key="i">{{ l }}</text>
				</view>

				<view class="result-badge" v-if="result.newBest"><text>新纪录</text></view>

				<view class="result-actions">
					<!-- #ifdef MP-WEIXIN -->
					<button class="btn-ghost btn-share" open-type="share" @click="onShareTap">
						<text>分享战绩 +1 次</text>
					</button>
					<!-- #endif -->
					<!-- #ifndef MP-WEIXIN -->
					<view class="btn-ghost btn-share" @click="shareFallback">
						<text>复制战绩 +1 次</text>
					</view>
					<!-- #endif -->

					<view class="btn-solid" :class="{ disabled: quotaLeft <= 0 }" @click="playAgain">
						<text>{{ quotaLeft > 0 ? '再来一局' : '今日次数已用完' }}</text>
					</view>
				</view>

				<view class="result-more" v-if="quotaLeft <= 0 && !quota.unlimited" @click="openQuotaSheet">
					<text>看广告解锁今日不限次数</text>
				</view>

				<view class="result-back" @click="backHome"><text>返回游乐场</text></view>
			</view>
		</view>

		<!-- ===================== 题目反馈弹层 ===================== -->
		<view class="mask" v-if="feedback.open" @click="closeFeedback">
			<view class="sheet" @click.stop>
				<text class="sheet-title">题目反馈</text>
				<text class="sheet-desc">把这道题原样交给我们就好，题目会连同数据一起核对。</text>
				<view class="feedback-preview">
					<text class="fb-line">{{ feedback.text }}</text>
				</view>
				<view class="sheet-actions">
					<view class="btn-ghost" @click="copyFeedback"><text>复制题目</text></view>
					<view class="btn-solid" @click="goSurvey"><text>{{ surveyReady ? '去填写问卷' : '知道了' }}</text></view>
				</view>
			</view>
		</view>

		<!-- ===================== 次数弹层 ===================== -->
		<view class="mask" v-if="quotaSheet" @click="closeQuotaSheet">
			<view class="sheet" @click.stop>
				<text class="sheet-title">今日游戏机会</text>
				<text class="sheet-desc">{{ quota.unlimited ? '今日已解锁不限次数，随意玩。' : '每天基础 ' + basePlays + ' 次，最多 ' + maxPlays + ' 次。多玩不宜，明日自动重置。' }}</text>

				<view class="quota-rows">
					<view class="quota-row">
						<text class="quota-row-k">基础</text>
						<text class="quota-row-v">{{ basePlays }} 次</text>
					</view>
					<view class="quota-row">
						<text class="quota-row-k">今日分享</text>
						<text class="quota-row-v">{{ quota.shareUsed ? '已领 +1' : '完成分享 +1' }}</text>
					</view>
					<view class="quota-row">
						<text class="quota-row-k">今日首胜</text>
						<text class="quota-row-v">{{ quota.winUsed ? '已领 +1' : '赢一局 +1' }}</text>
					</view>
					<view class="quota-row">
						<text class="quota-row-k">剩余</text>
						<text class="quota-row-v strong">{{ quota.unlimited ? '不限' : quota.remaining + ' 次' }}</text>
					</view>
				</view>

				<view class="sheet-actions">
					<view class="btn-ghost" @click="closeQuotaSheet"><text>关闭</text></view>
					<view class="btn-solid" v-if="!quota.unlimited" @click="watchAd"><text>看广告 · 今日不限</text></view>
				</view>
				<text class="sheet-note" v-if="!quota.unlimited">广告仅在微信小程序内可用，其他端可以等明天。</text>
			</view>
		</view>

		<text class="global-credit" v-if="!loading && ready">题目由本地数据随机生成，仅供娱乐 · 与官方无关</text>
		<ak-music-bar />
	</view>
</template>

<script>
	import { loadBoxData } from '@/utils/firstRunData.js';
	import { openLink, copyText } from '@/utils/openLink.js';
	import errorLog from '@/utils/errorLog.js';
	import {
		buildIndex,
		indexReady,
		makeQuizRound,
		makeDuelQuestion,
		makeChainRound,
		pickChainStart,
		sharedCharsOf,
		getQuota,
		syncQuota,
		beginPlay,
		grantBonus,
		unlockUnlimited,
		getBest,
		saveBest,
		BASE_PLAYS,
		MAX_PLAYS,
		SURVEY_URL
	} from '@/utils/passportGame.js';

	const DUEL_SECONDS = 45;
	const DUEL_SCORE = 10;
	const DUEL_PENALTY = 3;

	export default {
		data() {
			return {
				loading: true,
				ready: false,
				idx: null,
				isDark: false,

				scene: 'home',
				basePlays: BASE_PLAYS,
				maxPlays: MAX_PLAYS,
				surveyReady: !!SURVEY_URL,
				letters: ['A', 'B', 'C', 'D'],

				quota: { total: BASE_PLAYS, remaining: BASE_PLAYS, unlimited: false, shareUsed: false, winUsed: false },
				quotaSheet: false,

				best: { quiz: 0, duel: 0, chain: 0 },

				// 竞猜
				quiz: { qs: [], correct: 0 },
				quizIdx: 0,
				quizSelected: -1,
				quizLocked: false,

				// 速配
				duel: { q: null, score: 0, streak: 0, bestStreak: 0, left: DUEL_SECONDS, flash: '', flashOk: true },
				duelTimer: null,

				// 接龙
				chain: { round: null, currentId: '', steps: 0, path: [], reveal: false, picked: '', note: '', locked: false },

				// 结算
				result: null,

				// 反馈
				feedback: { open: false, text: '', q: null },

				videoAd: null,
				adLoading: false
			};
		},

		computed: {
			stats() {
				return {
					boxCount: this.idx ? this.idx.boxCount : 0,
					charCount: this.idx ? this.idx.charCount : 0
				};
			},
			games() {
				return [
					{
						key: 'quiz',
						no: '01',
						name: '通行证竞猜',
						desc: '盒里是谁、谁不在盒里、哪个盒复刻过 —— 5 道随机题',
						meta: this.best.quiz > 0 ? '最佳 ' + this.best.quiz + ' / 5' : '还没玩过'
					},
					{
						key: 'duel',
						no: '02',
						name: '盒号速配',
						desc: DUEL_SECONDS + ' 秒内快速判断「这位干员在这个盒号里吗」',
						meta: this.best.duel > 0 ? '最佳 ' + this.best.duel + ' 分' : '还没玩过'
					},
					{
						key: 'chain',
						no: '03',
						name: '通行证接龙',
						desc: '顺着共同干员一盒接一盒，看你能接多远',
						meta: this.best.chain > 0 ? '最佳 ' + this.best.chain + ' 环' : '还没玩过'
					}
				];
			},
			quotaLabel() {
				if (this.quota.unlimited) return '今日不限次';
				return '今日剩 ' + (this.quota.remaining > 0 ? this.quota.remaining : 0) + ' 次';
			},
			quotaHint() {
				if (this.quota.unlimited) return '广告已解锁，随便玩';
				return '共 ' + this.quota.total + ' 次 · 多玩不宜';
			},
			quotaLeft() {
				return this.quota.unlimited ? 99 : this.quota.remaining;
			},
			quizQ() {
				return this.quiz.qs[this.quizIdx] || null;
			},
			quizProgress() {
				const total = this.quiz.qs.length || 1;
				return Math.round(((this.quizIdx + (this.quizLocked ? 1 : 0)) / total) * 100) + '%';
			},
			duelLeftText() {
				const v = Math.max(0, this.duel.left);
				return v.toFixed(1) + 's';
			},
			duelProgress() {
				return Math.max(0, Math.min(100, (this.duel.left / DUEL_SECONDS) * 100)) + '%';
			},
			duelBest() {
				return this.best.duel;
			},
			chainBest() {
				return this.best.chain;
			},
			chainPathText() {
				const p = this.chain.path || [];
				if (!p.length) return '';
				const shown = p.slice(0, 10).join(' → ');
				return '已走过：' + shown + (p.length > 10 ? ' …' : '');
			}
		},

		async onLoad() {
			this.loadTheme();
			this.refreshQuota();
			this.loadBest();
			this.loadData();
			// 次数以服务端为准：本地缓存被清掉后，这里会把真实用量拉回来
			this.syncCloudQuota();
		},

		onShow() {
			this.refreshQuota();
			// 从广告 / 外部返回时，计时器不该继续跑
			if (this.scene !== 'duel') this.stopDuelTimer();
		},

		onHide() {
			this.stopDuelTimer();
		},

		onUnload() {
			this.stopDuelTimer();
			this.destroyVideoAd();
		},

		onShareAppMessage() {
			// 只在结算页分享时才给奖励次数，避免从菜单随手转发也刷次数
			if (this.result) {
				grantBonus('share');
				this.refreshQuota();
			}
			return {
				title: this.shareTitle(),
				path: '/packageB/pages/passport_game/passport_game'
			};
		},

		methods: {
			logError(e, where) {
				try {
					errorLog.logError(e, where || 'passport_game');
				} catch (err) {
					console.error(e);
				}
			},

			// 次数以服务端为准。放在 methods 里，否则 this 上拿不到（挂在顶层选项会被 Vue 忽略）
			async syncCloudQuota() {
				try {
					const q = await syncQuota();
					if (q) this.quota = q;
				} catch (e) {
					this.logError(e, 'passport_game.syncCloudQuota');
				}
			},

			/* ---------------- 主题 / 数据 ---------------- */
			loadTheme() {
				try {
					const t = uni.getStorageSync('themeMode');
					this.isDark = ['ark', 'ak', 'liquidDark'].indexOf(t) > -1;
				} catch (e) {
					this.isDark = false;
				}
			},

			async loadData() {
				this.loading = true;
				try {
					const res = await loadBoxData();
					const boxes = res && res.data ? res.data : [];
					const idx = buildIndex(boxes);
					this.idx = idx;
					this.ready = indexReady(idx);
				} catch (e) {
					this.logError(e, 'passport_game.loadData');
					this.ready = false;
				} finally {
					this.loading = false;
				}
			},

			refreshQuota() {
				try {
					this.quota = getQuota();
				} catch (e) {
					this.logError(e, 'passport_game.refreshQuota');
				}
			},

			loadBest() {
				try {
					this.best = getBest();
				} catch (e) {
					this.best = { quiz: 0, duel: 0, chain: 0 };
				}
			},

			/* ---------------- 次数 ---------------- */
			openQuotaSheet() {
				this.refreshQuota();
				this.quotaSheet = true;
			},
			closeQuotaSheet() {
				this.quotaSheet = false;
			},
			// 激励视频在小程序里是「页面内单例」，跨页面 / 跨页面实例都不能复用，
			// 旧写法在 onLoad 里预创建，退出再进时会拿到绑定在已销毁页面上的实例，
			// show() 直接报 "you can only invoke show() on the page where rewardedVideoAd is created"。
			// 因此改为：用户点击时用 multiton 多例模式创建，离开页面时 destroy。
			createVideoAd() {
				// #ifdef MP-WEIXIN
				try {
					if (typeof wx === 'undefined' || !wx.createRewardedVideoAd) return null;
					const ad = wx.createRewardedVideoAd({
						adUnitId: 'adunit-d11c06a97af3e479',
						multiton: true
					});
					ad.onError((err) => {
						console.warn('[通行证游乐场] 激励视频错误', err);
						this.adLoading = false;
					});
					ad.onClose((res) => {
						this.adLoading = false;
						if (res && res.isEnded) this.handleAdReward();
						else uni.showToast({ title: '完整看完广告才能解锁哦', icon: 'none' });
					});
					return ad;
				} catch (e) {
					this.logError(e, 'passport_game.createVideoAd');
				}
				// #endif
				return null;
			},
			destroyVideoAd() {
				try {
					if (this.videoAd && this.videoAd.destroy) this.videoAd.destroy();
				} catch (e) { /* 销毁失败不影响退出 */ }
				this.videoAd = null;
				this.adLoading = false;
			},
			watchAd() {
				if (this.quota.unlimited) {
					uni.showToast({ title: '今日已解锁不限次', icon: 'none' });
					return;
				}
				if (this.adLoading) return;

				let ad = this.videoAd;
				if (!ad || !ad.show) {
					ad = this.createVideoAd();
					this.videoAd = ad;
				}
				if (!ad) {
					uni.showModal({
						title: '广告暂不可用',
						content: '激励视频只在微信小程序里可用，其他端暂时只能等明天重置，或者分享一次也能多玩一局。',
						showCancel: false,
						confirmText: '知道了'
					});
					return;
				}

				this.adLoading = true;
				ad.show().catch(() => {
					// 没预加载成功时先 load 再 show；仍失败就提示，不让按钮一直转
					ad.load().then(() => ad.show()).catch((err) => {
						console.warn('[通行证游乐场] 广告展示失败', err);
						this.adLoading = false;
						uni.showToast({ title: '广告暂时拉不到，稍后再试', icon: 'none' });
					});
				});
			},
			handleAdReward() {
				try {
					unlockUnlimited();
					this.refreshQuota();
					this.quotaSheet = false;
					uni.showToast({ title: '已解锁今日不限次数', icon: 'success' });
				} catch (e) {
					this.logError(e, 'passport_game.handleAdReward');
				}
			},

			/* ---------------- 入口 ---------------- */
			startGame(key) {
				if (!this.ready) {
					uni.showToast({ title: '数据还没准备好', icon: 'none' });
					return;
				}
				if (!beginPlay()) {
					this.openQuotaSheet();
					return;
				}
				this.refreshQuota();
				if (key === 'quiz') this.beginQuiz();
				else if (key === 'duel') this.beginDuel();
				else if (key === 'chain') this.beginChain();
			},

			playAgain() {
				if (this.quotaLeft <= 0 && !this.quota.unlimited) {
					this.openQuotaSheet();
					return;
				}
				const kind = this.result ? this.result.kind : 'quiz';
				this.startGame(kind);
			},

			backHome() {
				this.stopDuelTimer();
				this.scene = 'home';
				this.result = null;
				this.quiz = { qs: [], correct: 0 };
				this.quizIdx = 0;
				this.quizSelected = -1;
				this.quizLocked = false;
				this.duel.q = null;
				this.chain.round = null;
				this.chain.path = [];
				this.refreshQuota();
				this.loadBest();
			},

			quitGame() {
				if (this.scene === 'duel') {
					this.stopDuelTimer();
					this.finishDuel();
					return;
				}
				if (this.scene === 'chain') {
					this.finishChain();
					return;
				}
				this.backHome();
			},

			goGacha() {
				uni.navigateTo({
					url: '/packageB/pages/gacha_simulated/gacha_simulated',
					fail: () => uni.showToast({ title: '抽卡页面打开失败', icon: 'none' })
				});
			},

			/* ---------------- 游戏一：竞猜 ---------------- */
			beginQuiz() {
				const qs = makeQuizRound(this.idx, 5);
				if (!qs.length) {
					uni.showToast({ title: '数据不足，出不了题', icon: 'none' });
					this.scene = 'home';
					return;
				}
				this.quiz = { qs: qs, correct: 0 };
				this.quizIdx = 0;
				this.quizSelected = -1;
				this.quizLocked = false;
				this.scene = 'quiz';
			},

			optClass(i) {
				if (!this.quizLocked) return '';
				if (i === this.quizQ.answer) return 'is-correct';
				if (i === this.quizSelected) return 'is-wrong';
				return 'is-muted';
			},

			chooseQuiz(i) {
				if (this.quizLocked) return;
				this.quizSelected = i;
				this.quizLocked = true;
				if (i === this.quizQ.answer) this.quiz.correct += 1;
			},

			nextQuiz() {
				if (!this.quizLocked) {
					uni.showToast({ title: '先选一个答案', icon: 'none' });
					return;
				}
				if (this.quizIdx + 1 >= this.quiz.qs.length) {
					this.finishQuiz();
					return;
				}
				this.quizIdx += 1;
				this.quizSelected = -1;
				this.quizLocked = false;
			},

			finishQuiz() {
				const score = this.quiz.correct;
				const isNew = saveBest('quiz', score);
				const gotWin = score >= 4 ? grantBonus('win') : false;
				this.loadBest();
				this.refreshQuota();
				const titles = ['还需补课', '还需补课', '刚够格', '合格干员', '资深博士', '凭证鉴定大师'];
				let sub = '答对 ' + score + ' 题，再来一局找找手感';
				if (gotWin) sub = '答对 ' + score + ' 题，今日首胜次数 +1';
				else if (score >= 4) sub = '答对 ' + score + ' 题，今日首胜奖励已经领过了';
				this.result = {
					kind: 'quiz',
					kicker: '通行证竞猜',
					title: titles[score] || '合格干员',
					scoreText: score + ' / ' + this.quiz.qs.length,
					sub: sub,
					lines: ['题库来自 ' + this.stats.boxCount + ' 个盒号 · ' + this.stats.charCount + ' 位干员', '每局题目都是现场生成的'],
					newBest: isNew
				};
				this.scene = 'result';
			},

			/* ---------------- 游戏二：速配 ---------------- */
			beginDuel() {
				const first = makeDuelQuestion(this.idx);
				if (!first) {
					uni.showToast({ title: '数据不足，出不了题', icon: 'none' });
					this.scene = 'home';
					return;
				}
				this.duel = { q: first, score: 0, streak: 0, bestStreak: 0, left: DUEL_SECONDS, flash: '', flashOk: true };
				this.scene = 'duel';
				this.startDuelTimer();
			},

			startDuelTimer() {
				this.stopDuelTimer();
				this.duelTimer = setInterval(() => {
					this.duel.left = Math.max(0, this.duel.left - 0.1);
					if (this.duel.left <= 0) {
						this.stopDuelTimer();
						this.finishDuel();
					}
				}, 100);
			},

			stopDuelTimer() {
				if (this.duelTimer) {
					clearInterval(this.duelTimer);
					this.duelTimer = null;
				}
			},

			answerDuel(choice) {
				const q = this.duel.q;
				if (!q) return;
				if (choice === q.answer) {
					this.duel.streak += 1;
					this.duel.bestStreak = Math.max(this.duel.bestStreak, this.duel.streak);
					const bonus = Math.min(10, this.duel.streak - 1) * 2;
					this.duel.score += DUEL_SCORE + bonus;
					this.duel.flash = '+' + (DUEL_SCORE + bonus) + (bonus > 0 ? ' 连对 ' + this.duel.streak : '');
					this.duel.flashOk = true;
				} else {
					this.duel.streak = 0;
					this.duel.left = Math.max(0, this.duel.left - DUEL_PENALTY);
					this.duel.flash = '错了 · 扣 ' + DUEL_PENALTY + ' 秒';
					this.duel.flashOk = false;
				}
				const next = makeDuelQuestion(this.idx);
				if (!next) {
					this.stopDuelTimer();
					this.finishDuel();
					return;
				}
				this.duel.q = next;
				setTimeout(() => {
					if (this.scene === 'duel') this.duel.flash = '';
				}, 700);
			},

			finishDuel() {
				if (this.scene !== 'duel') return;
				const score = this.duel.score;
				const isNew = saveBest('duel', score);
				const gotWin = score >= 100 ? grantBonus('win') : false;
				this.loadBest();
				this.refreshQuota();
				this.result = {
					kind: 'duel',
					kicker: '盒号速配',
					title: score >= 200 ? '人形数据库' : score >= 120 ? '手速在线' : score >= 60 ? '还算熟练' : '再练练',
					scoreText: score + ' 分',
					sub: '最长连对 ' + this.duel.bestStreak + ' 次' + (gotWin ? ' · 今日首胜 +1 次' : ''),
					lines: ['答对 +10 分，连对每次额外 +2（上限 +20）', '答错扣 ' + DUEL_PENALTY + ' 秒'],
					newBest: isNew
				};
				this.scene = 'result';
			},

			/* ---------------- 游戏三：接龙 ---------------- */
			beginChain() {
				const start = pickChainStart(this.idx);
				const round = makeChainRound(this.idx, start);
				if (!round) {
					uni.showToast({ title: '数据不足，接不下去', icon: 'none' });
					this.scene = 'home';
					return;
				}
				this.chain = {
					round: round,
					currentId: start,
					steps: 0,
					path: [start],
					reveal: false,
					picked: '',
					note: '',
					locked: false
				};
				this.scene = 'chain';
			},

			chainOptClass(id) {
				if (!this.chain.reveal) return '';
				if (id === this.chain.round.answerId) return 'is-correct';
				if (id === this.chain.picked) return 'is-wrong';
				return 'is-muted';
			},

			answerChain(id) {
				if (this.chain.locked) return;
				const round = this.chain.round;
				this.chain.locked = true;
				this.chain.reveal = true;
				this.chain.picked = id;

				if (id !== round.answerId) {
					const shared = sharedCharsOf(this.idx, this.chain.currentId, round.answerId);
					this.chain.note = '接错了 —— 正确答案是「' + round.answerId + '」，共同干员：' + (shared.join('、') || round.sharedChar);
					setTimeout(() => this.finishChain(), 1400);
					return;
				}

				const shared = sharedCharsOf(this.idx, this.chain.currentId, id);
				this.chain.steps += 1;
				this.chain.note = '共同干员：' + shared.join('、');
				this.chain.path = this.chain.path.concat([id]);

				setTimeout(() => {
					if (this.scene !== 'chain') return;
					const nextRound = makeChainRound(this.idx, id);
					if (!nextRound) {
						this.finishChain();
						return;
					}
					this.chain.currentId = id;
					this.chain.round = nextRound;
					this.chain.reveal = false;
					this.chain.picked = '';
					this.chain.note = '';
					this.chain.locked = false;
				}, 900);
			},

			finishChain() {
				if (this.scene !== 'chain') return;
				const steps = this.chain.steps;
				const isNew = saveBest('chain', steps);
				const gotWin = steps >= 2 ? grantBonus('win') : false;
				this.loadBest();
				this.refreshQuota();
				this.result = {
					kind: 'chain',
					kicker: '通行证接龙',
					title: steps >= 6 ? '接龙老手' : steps >= 4 ? '有点东西' : steps >= 2 ? '刚热身' : '一轮就断',
					scoreText: steps + ' 环',
					sub: '路线：' + this.chain.path.slice(0, 8).join(' → ') + (this.chain.path.length > 8 ? ' …' : '') + (gotWin ? ' · 今日首胜 +1 次' : ''),
					lines: ['每一环都要和上一个盒号有共同干员', '环数越多，说明你对盒内成员越熟'],
					newBest: isNew
				};
				this.scene = 'result';
			},

			/* ---------------- 分享 ---------------- */
			shareTitle() {
				if (!this.result) return '通行证游乐场 · 猜猜这盒里都有谁';
				return '我在通行证游乐场拿了 ' + this.result.scoreText + '（' + this.result.kicker + '），来试试？';
			},

			onShareTap() {
				// 真正的转发由 open-type="share" 触发，奖励在 onShareAppMessage 里发
			},

			shareFallback() {
				const text = this.shareTitle() + '\n' + '方舟通行证谷子查询工具 · 通行证游乐场';
				copyText(text, '战绩已复制，去分享吧');
				if (grantBonus('share')) {
					this.refreshQuota();
					setTimeout(() => uni.showToast({ title: '今日分享次数 +1', icon: 'none' }), 1200);
				}
			},

			/* ---------------- 反馈 ---------------- */
			openFeedback(question) {
				const lines = [];
				lines.push('【通行证竞猜·题目反馈】');
				lines.push('题目：' + question.prompt);
				question.options.forEach((o, i) => {
					const mark = i === question.answer ? '（工具答案）' : '';
					lines.push(this.letters[i] + '. ' + o.text + (o.sub ? ' ' + o.sub : '') + mark);
				});
				lines.push('我选择：' + (this.quizSelected >= 0 ? this.letters[this.quizSelected] : '未作答'));
				lines.push('说明：' + question.explain.replace(/\n/g, '；'));
				this.feedback = { open: true, text: lines.join('\n'), q: question };
			},

			closeFeedback() {
				this.feedback.open = false;
			},

			copyFeedback() {
				copyText(this.feedback.text, '题目已复制，粘贴到问卷即可');
			},

			goSurvey() {
				if (!SURVEY_URL) {
					copyText(this.feedback.text, '题目已复制');
					this.feedback.open = false;
					return;
				}
				// 先把题目快照塞进剪贴板，用户到问卷里直接粘贴第三题即可
				copyText(this.feedback.text, '题目已复制，问卷第 3 题粘贴即可');
				this.feedback.open = false;
				setTimeout(() => {
					openLink(SURVEY_URL, { copyTip: '问卷链接已复制，请到浏览器打开' });
				}, 700);
			}
		}
	};
</script>

<style>
	/* ---------------- 主题变量 ---------------- */
	.pg {
		min-height: 100vh;
		padding: 24rpx 32rpx calc(40rpx + env(safe-area-inset-bottom));
		box-sizing: border-box;
	}
	.pg-light {
		--bg: #f7f7f9;
		--card: #ffffff;
		--text: #1c1c1e;
		--sub: #8e8e93;
		--line: #e3e3e8;
		--soft: #f0f0f3;
		--accent: #1c1c1e;
		--accent-text: #ffffff;
		background: var(--bg);
		color: var(--text);
	}
	.pg-dark {
		--bg: #0f0f12;
		--card: #1a1a1f;
		--text: #f2f2f7;
		--sub: #8e8e93;
		--line: #2c2c33;
		--soft: #232329;
		--accent: #f2f2f7;
		--accent-text: #101014;
		background: var(--bg);
		color: var(--text);
	}

	/* ---------------- 加载 / 空态 ---------------- */
	.pg-loading,
	.pg-empty {
		padding: 160rpx 24rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 24rpx;
	}
	.pg-loading-bar {
		width: 320rpx;
		height: 4rpx;
		background: var(--soft);
		overflow: hidden;
		border-radius: 4rpx;
	}
	.pg-loading-fill {
		width: 40%;
		height: 100%;
		background: var(--text);
		animation: slide 1.1s ease-in-out infinite;
	}
	@keyframes slide {
		0% { transform: translateX(-100%); }
		100% { transform: translateX(260%); }
	}
	.pg-loading-text,
	.pg-empty-desc {
		font-size: 24rpx;
		color: var(--sub);
		text-align: center;
		line-height: 1.7;
	}
	.pg-empty-title {
		font-size: 34rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
	}

	/* ---------------- 通用按钮 ---------------- */
	.btn-solid {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 88rpx;
		padding: 0 40rpx;
		background: var(--accent);
		border-radius: 44rpx;
	}
	.btn-solid text {
		font-size: 28rpx;
		font-weight: 600;
		color: var(--accent-text);
		letter-spacing: 1rpx;
	}
	.btn-solid.disabled {
		opacity: 0.35;
	}
	.btn-ghost {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 88rpx;
		padding: 0 36rpx;
		border: 1rpx solid var(--line);
		border-radius: 44rpx;
		background: transparent;
	}
	.btn-ghost text {
		font-size: 28rpx;
		color: var(--text);
	}
	button.btn-share {
		margin: 0;
		width: 100%;
		line-height: 1;
		font-size: 28rpx;
		color: var(--text);
		background: transparent;
	}
	button.btn-share::after {
		border: none;
	}

	/* ---------------- 大厅 ---------------- */
	.pg-hero {
		padding: 16rpx 8rpx 40rpx;
		border-bottom: 1rpx solid var(--line);
	}
	.hero-kicker {
		display: block;
		font-size: 20rpx;
		letter-spacing: 6rpx;
		color: var(--sub);
	}
	.hero-title {
		display: block;
		margin-top: 12rpx;
		font-size: 52rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
	}
	.hero-desc {
		display: block;
		margin-top: 16rpx;
		font-size: 24rpx;
		line-height: 1.7;
		color: var(--sub);
	}
	.hero-quota {
		margin-top: 28rpx;
		display: flex;
		align-items: baseline;
		gap: 16rpx;
	}
	.hero-quota-num {
		font-size: 36rpx;
		font-weight: 700;
		letter-spacing: 1rpx;
	}
	.hero-quota-hint {
		font-size: 22rpx;
		color: var(--sub);
	}
	.hero-unlock {
		margin-top: 12rpx;
	}
	.hero-unlock text {
		font-size: 22rpx;
		color: var(--sub);
		text-decoration: underline;
	}

	.game-list {
		margin-top: 8rpx;
	}
	.game-card {
		display: flex;
		align-items: center;
		padding: 32rpx 0;
		border-bottom: 1rpx solid var(--line);
	}
	.game-no text {
		font-size: 22rpx;
		color: var(--sub);
		letter-spacing: 2rpx;
	}
	.game-no {
		width: 68rpx;
	}
	.game-main {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.game-name {
		font-size: 32rpx;
		font-weight: 600;
		letter-spacing: 1rpx;
	}
	.game-desc {
		font-size: 23rpx;
		color: var(--sub);
		line-height: 1.5;
	}
	.game-meta {
		font-size: 21rpx;
		color: var(--sub);
		opacity: 0.8;
	}
	.game-chev {
		width: 14rpx;
		height: 14rpx;
		border-top: 2rpx solid var(--sub);
		border-right: 2rpx solid var(--sub);
		transform: rotate(45deg);
		margin-left: 20rpx;
	}

	.home-extra {
		display: flex;
		align-items: center;
		margin-top: 32rpx;
		padding: 28rpx 0;
		border-bottom: 1rpx solid var(--line);
	}
	.extra-main {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 6rpx;
	}
	.extra-name {
		font-size: 28rpx;
		font-weight: 600;
	}
	.extra-desc {
		font-size: 22rpx;
		color: var(--sub);
	}
	.home-foot {
		margin-top: 40rpx;
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	.foot-line {
		font-size: 21rpx;
		color: var(--sub);
		line-height: 1.6;
	}
	.foot-credit {
		opacity: 0.7;
	}

	/* ---------------- 顶部信息条 ---------------- */
	.bar {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		padding: 8rpx 0 24rpx;
	}
	.bar-left {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.bar-kicker {
		font-size: 20rpx;
		letter-spacing: 4rpx;
		color: var(--sub);
	}
	.bar-title {
		font-size: 34rpx;
		font-weight: 700;
		letter-spacing: 1rpx;
	}
	.bar-right {
		display: flex;
		align-items: baseline;
		gap: 8rpx;
	}
	.bar-score {
		font-size: 40rpx;
		font-weight: 700;
	}
	.bar-score-unit {
		font-size: 21rpx;
		color: var(--sub);
	}
	.progress {
		height: 4rpx;
		background: var(--soft);
		border-radius: 4rpx;
		overflow: hidden;
	}
	.progress-fill {
		height: 100%;
		background: var(--text);
		transition: width 0.25s ease;
	}
	.progress-fill.urgent {
		background: #b03a2e;
	}

	/* ---------------- 题目卡 ---------------- */
	.q-card {
		margin-top: 40rpx;
		padding: 40rpx 32rpx;
		background: var(--card);
		border: 1rpx solid var(--line);
		border-radius: 20rpx;
	}
	.q-tag {
		display: inline-flex;
		padding: 6rpx 16rpx;
		border: 1rpx solid var(--line);
		border-radius: 6rpx;
		margin-bottom: 20rpx;
	}
	.q-tag text {
		font-size: 20rpx;
		color: var(--sub);
		letter-spacing: 2rpx;
	}
	.q-prompt {
		font-size: 34rpx;
		font-weight: 600;
		line-height: 1.6;
		letter-spacing: 1rpx;
	}

	/* ---------------- 选项 ---------------- */
	.opt-list {
		margin-top: 28rpx;
		display: flex;
		flex-direction: column;
		gap: 16rpx;
	}
	.opt {
		display: flex;
		align-items: center;
		padding: 26rpx 28rpx;
		background: var(--card);
		border: 1rpx solid var(--line);
		border-radius: 16rpx;
		transition: all 0.15s;
	}
	.opt-key {
		width: 48rpx;
		font-size: 24rpx;
		color: var(--sub);
		font-weight: 600;
	}
	.opt-body {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 6rpx;
	}
	.opt-text {
		font-size: 30rpx;
		line-height: 1.4;
	}
	.opt-sub {
		font-size: 21rpx;
		color: var(--sub);
	}
	.opt-mark {
		font-size: 32rpx;
		font-weight: 700;
		margin-left: 12rpx;
	}
	.opt.is-correct {
		background: var(--accent);
		border-color: var(--accent);
	}
	.opt.is-correct .opt-text,
	.opt.is-correct .opt-key,
	.opt.is-correct .opt-sub,
	.opt.is-correct .opt-mark {
		color: var(--accent-text);
	}
	.opt.is-wrong {
		border-style: dashed;
		border-color: var(--text);
	}
	.opt.is-wrong .opt-text {
		text-decoration: line-through;
	}
	.opt.is-muted {
		opacity: 0.42;
	}

	.explain {
		margin-top: 28rpx;
		padding: 24rpx 28rpx;
		background: var(--soft);
		border-radius: 16rpx;
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	.explain-title {
		font-size: 24rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
	}
	.explain-text {
		font-size: 23rpx;
		color: var(--sub);
		line-height: 1.7;
		white-space: pre-wrap;
	}

	.quiz-foot {
		margin-top: 40rpx;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 20rpx;
	}
	.link-btn text {
		font-size: 22rpx;
		color: var(--sub);
		text-decoration: underline;
	}
	.next-btn {
		min-width: 220rpx;
	}
	.foot-tip text {
		font-size: 22rpx;
		color: var(--sub);
	}

	/* ---------------- 速配 ---------------- */
	.duel-stage {
		margin-top: 56rpx;
		padding: 64rpx 32rpx;
		background: var(--card);
		border: 1rpx solid var(--line);
		border-radius: 24rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 18rpx;
	}
	.duel-ask {
		font-size: 22rpx;
		color: var(--sub);
		letter-spacing: 2rpx;
	}
	.duel-name {
		font-size: 72rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
		text-align: center;
	}
	.duel-box {
		font-size: 28rpx;
		color: var(--sub);
		letter-spacing: 1rpx;
	}
	.duel-actions {
		margin-top: 40rpx;
		display: flex;
		gap: 24rpx;
	}
	.duel-btn {
		flex: 1;
		height: 128rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 20rpx;
		border: 1rpx solid var(--line);
	}
	.duel-btn text {
		font-size: 40rpx;
		font-weight: 700;
		letter-spacing: 6rpx;
	}
	.duel-btn.yes {
		background: var(--accent);
		border-color: var(--accent);
	}
	.duel-btn.yes text {
		color: var(--accent-text);
	}
	.duel-flash {
		margin-top: 28rpx;
		display: flex;
		justify-content: center;
	}
	.duel-flash-text {
		font-size: 24rpx;
		color: var(--sub);
		letter-spacing: 1rpx;
	}

	/* ---------------- 接龙 ---------------- */
	.chain-box {
		margin-top: 32rpx;
		padding: 28rpx;
		background: var(--card);
		border: 1rpx solid var(--line);
		border-radius: 20rpx;
	}
	.chain-label {
		font-size: 20rpx;
		color: var(--sub);
		letter-spacing: 2rpx;
	}
	.chip-wrap {
		margin-top: 18rpx;
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
	}
	.chip {
		padding: 10rpx 20rpx;
		border: 1rpx solid var(--line);
		border-radius: 30rpx;
	}
	.chip-text {
		font-size: 23rpx;
	}
	.chain-list {
		margin-top: 24rpx;
		display: flex;
		flex-direction: column;
		gap: 16rpx;
	}
	.chain-opt {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 30rpx 32rpx;
		background: var(--card);
		border: 1rpx solid var(--line);
		border-radius: 16rpx;
	}
	.chain-opt-body {
		flex: 1;
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.chain-opt-text {
		font-size: 32rpx;
		font-weight: 600;
		letter-spacing: 1rpx;
	}
	.chain-opt-names {
		font-size: 21rpx;
		color: var(--sub);
		line-height: 1.5;
	}
	.chain-opt.is-correct {
		background: var(--accent);
		border-color: var(--accent);
	}
	.chain-opt.is-correct .chain-opt-text,
	.chain-opt.is-correct .opt-mark {
		color: var(--accent-text);
	}
	.chain-opt.is-correct .chain-opt-names {
		color: var(--accent-text);
		opacity: 0.72;
	}
	.chain-opt.is-wrong {
		border-style: dashed;
	}
	.chain-opt.is-wrong .chain-opt-text {
		text-decoration: line-through;
	}
	.chain-opt.is-muted {
		opacity: 0.42;
	}
	.chain-note {
		display: block;
		margin-top: 24rpx;
		font-size: 24rpx;
		color: var(--sub);
		line-height: 1.6;
	}
	.path-line {
		margin-top: 24rpx;
		padding-top: 20rpx;
		border-top: 1rpx solid var(--line);
	}
	.path-text {
		font-size: 21rpx;
		color: var(--sub);
		line-height: 1.6;
	}

	/* ---------------- 结算 ---------------- */
	.result {
		margin-top: 40rpx;
		padding: 56rpx 40rpx;
		background: var(--card);
		border: 1rpx solid var(--line);
		border-radius: 24rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
	}
	.result-kicker {
		font-size: 20rpx;
		letter-spacing: 4rpx;
		color: var(--sub);
	}
	.result-title {
		margin-top: 16rpx;
		font-size: 44rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
	}
	.result-score {
		margin-top: 12rpx;
		font-size: 64rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
	}
	.result-sub {
		margin-top: 12rpx;
		font-size: 23rpx;
		color: var(--sub);
		text-align: center;
		line-height: 1.6;
	}
	.result-lines {
		margin-top: 32rpx;
		padding-top: 24rpx;
		border-top: 1rpx solid var(--line);
		width: 100%;
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	.result-line {
		font-size: 21rpx;
		color: var(--sub);
		line-height: 1.6;
		text-align: center;
	}
	.result-badge {
		margin-top: 24rpx;
		padding: 8rpx 22rpx;
		border: 1rpx solid var(--text);
		border-radius: 30rpx;
	}
	.result-badge text {
		font-size: 20rpx;
		letter-spacing: 3rpx;
	}
	.result-actions {
		margin-top: 40rpx;
		width: 100%;
		display: flex;
		flex-direction: column;
		gap: 20rpx;
	}
	.result-more {
		margin-top: 24rpx;
	}
	.result-more text {
		font-size: 22rpx;
		color: var(--sub);
		text-decoration: underline;
	}
	.result-back {
		margin-top: 28rpx;
	}
	.result-back text {
		font-size: 22rpx;
		color: var(--sub);
	}

	/* ---------------- 弹层 ---------------- */
	.mask {
		position: fixed;
		left: 0;
		top: 0;
		right: 0;
		bottom: 0;
		background: rgba(0, 0, 0, 0.42);
		display: flex;
		align-items: flex-end;
		z-index: 90;
	}
	.sheet {
		width: 100%;
		background: var(--card);
		border-radius: 28rpx 28rpx 0 0;
		padding: 40rpx 36rpx calc(40rpx + env(safe-area-inset-bottom));
		box-sizing: border-box;
	}
	.sheet-title {
		display: block;
		font-size: 34rpx;
		font-weight: 700;
		letter-spacing: 2rpx;
	}
	.sheet-desc {
		display: block;
		margin-top: 14rpx;
		font-size: 23rpx;
		color: var(--sub);
		line-height: 1.6;
	}
	.feedback-preview {
		margin-top: 24rpx;
		padding: 24rpx;
		background: var(--soft);
		border-radius: 16rpx;
		max-height: 460rpx;
		overflow: hidden;
	}
	.fb-line {
		font-size: 21rpx;
		color: var(--sub);
		line-height: 1.8;
		white-space: pre-wrap;
	}
	.sheet-actions {
		margin-top: 32rpx;
		display: flex;
		gap: 20rpx;
	}
	.sheet-actions .btn-ghost,
	.sheet-actions .btn-solid {
		flex: 1;
		padding: 0;
	}
	.sheet-note {
		display: block;
		margin-top: 20rpx;
		font-size: 20rpx;
		color: var(--sub);
		text-align: center;
	}
	.quota-rows {
		margin-top: 24rpx;
		border-top: 1rpx solid var(--line);
	}
	.quota-row {
		display: flex;
		justify-content: space-between;
		padding: 22rpx 0;
		border-bottom: 1rpx solid var(--line);
	}
	.quota-row-k {
		font-size: 24rpx;
		color: var(--sub);
	}
	.quota-row-v {
		font-size: 24rpx;
	}
	.quota-row-v.strong {
		font-weight: 700;
	}

	.global-credit {
		display: block;
		margin-top: 40rpx;
		text-align: center;
		font-size: 20rpx;
		color: var(--sub);
		opacity: 0.75;
		line-height: 1.5;
	}
</style>
