<template>
	<view class="page-root" :class="'theme-' + themeMode">
		<!-- <view class="top-bar">
			<view class="back-btn" @click="goBack"><text>‹</text></view>
			<text class="title">今日宜挂</text>
			<view class="top-placeholder"></view>
		</view> -->

		<view class="step-select" v-if="currentStep === 'select'">
			<view class="friend-banner" v-if="friendResult">
				<text class="friend-banner-text">{{ friendResult.text }}</text>
				<view class="friend-banner-close" @click="friendResult = null">
					<text>×</text>
				</view>
			</view>
			<view class="barrage-stage">
				<view class="prompt">
				<text class="prompt-main">今天想做些什么？</text>
			</view>
				<view
					class="barrage-row"
					v-for="(track, ti) in barrageTracks"
					:key="'row'+ti"
					@touchstart="onDragStart($event, ti)"
					@touchmove.stop.prevent="onDragMove($event, ti)"
					@touchend="onDragEnd($event, ti)"
					@touchcancel="onDragEnd($event, ti)"
					@mousedown.stop="onDragStart($event, ti)"
				>
					<view class="barrage-shift" :style="shiftStyle(ti)">
						<view class="barrage-track" :class="{ reverse: track.reverse, dragging: isDragging(ti) }" :style="trackStyle(ti)">
							<view
								v-for="(opt, i) in track.items"
								:key="ti+'-'+i"
								class="barrage"
								:class="{ active: selectedOption === opt.key }"
								@click="selectOption(opt.key)"
							>
								<text>{{ opt.label }}</text>
							</view>
						</view>
					</view>
				</view>
			</view>

			<view class="bottom-action">
				<view class="confirm-btn" :class="{ disabled: !selectedOption || remainingDraws <= 0 }" @click="confirmSelection">
					<text v-if="remainingDraws > 0">{{ selectedOption ? '确定 >' : '请先选择一个' }}</text>
					<text v-else>今日次数已用完</text>
				</view>
				<text class="random-tip">结果随机生成，仅供娱乐 · 一天内多次重试容易抽到重复内容，建议明天再来</text>
				<view class="daily-toggle" @click="toggleTaskPanel">
					<text class="daily-toggle-text">{{ taskToggleText }}</text>
				</view>
			</view>
		</view>

		<!-- 全局致谢: 选择页与结果页都显示 -->
		<text v-if="currentStep !== 'loading'" class="global-credit">部分数据来源于小红书：瓜瓜不吃香菜 整理的表格，如有侵权请联系删除</text>

		<view class="step-loading" v-if="currentStep === 'loading'">
			<view class="loading-wrap">
				<view class="loading-spinner"></view>
				<text class="loading-text">正在计算适合的通行证</text>
				<text class="loading-dots">{{ loadingDots }}</text>
			</view>
		</view>

		<view class="step-result" v-if="currentStep === 'result' && currentResult && currentResult.special === 'lucky'">
			<view class="result-card special-card">
				<view class="result-header">
					<text class="result-eyebrow">今日宜挂</text>
					<text class="result-title">{{ selectedOptionLabel }}</text>
				</view>
				<view class="op-card special-op">
					<text class="op-emoji-big">✨</text>
				</view>
				<view class="reason-box">
					<text class="reason-text">您的运气今天爆棚，似乎不需要悬挂通行证，放心大胆去做吧~</text>
				</view>
				<text class="result-tip">结果随机生成，仅供娱乐</text>
				<view class="retry-warn" v-if="usedToday >= 3 && !unlimited">
					<text class="retry-warn-text">⚠ 不推荐一天内多次重试：推荐池有限，重试次数越多越容易抽到重复内容</text>
				</view>
				<view class="result-actions">
					<view class="action-btn" :class="{ disabled: remainingDraws <= 0 }" @click="reset"><text>重新选择</text></view>
					<view class="action-btn action-primary" :class="{ disabled: remainingDraws <= 0 }" @click="recommendAgain">
						<text v-if="remainingDraws > 0">再来一次{{ remainingSuffix }}</text>
						<text v-else>今日已用完</text>
					</view>
				</view>
				<view class="more-draw-btn" v-if="remainingDraws <= 0" @click="toggleTaskPanel">
					<text class="more-draw-text">次数用完了？做任务或看广告可以换次数</text>
				</view>
				<view class="share-entry" @click="openSharePoster">
					<text class="share-entry-text">分享这次结果</text>
					<text class="share-entry-sub">存成图片 / 小程序卡片</text>
				</view>
			</view>
		</view>

		<view class="step-result" v-if="currentStep === 'result' && currentResult && !currentResult.special">
			<view class="result-card">
				<view class="result-header">
					<text class="result-eyebrow">今日宜挂</text>
					<text class="result-title">{{ selectedOptionLabel }}</text>
				</view>

				<view class="op-card">
					<view class="op-avatar">
						<image v-if="currentResult.avatar" :src="imgSrc(currentResult.avatar)" mode="aspectFill" class="op-img"></image>
						<text v-else class="op-emoji">{{ currentResult.emoji || '?' }}</text>
					</view>
					<view class="op-info">
						<text class="op-name">{{ currentResult.name }}</text>
						<view class="op-tags" v-if="currentResult.tag">
							<text class="op-tag">{{ currentResult.tag }}</text>
						</view>
						<view class="op-box" v-if="currentResult.box_id">
							<text class="op-box-text">推荐盒号：</text>
							<text class="op-box-id">{{ currentResult.box_id }}</text>
						</view>
					</view>
				</view>
				<view class="reason-box">
					<text class="reason-label">推荐理由</text>
					<text class="reason-text">{{ currentResult.reason }}</text>
				</view>
				<view class="warn-box" v-if="currentResult.warn">
					<text class="warn-label">⚠ 慎重</text>
					<text class="warn-text">{{ currentResult.warn }}</text>
				</view>

				<text class="result-tip">结果随机生成，仅供娱乐</text>
				<view class="retry-warn" v-if="usedToday >= 3 && !unlimited">
					<text class="retry-warn-text">⚠ 不推荐一天内多次重试：推荐池有限，重试次数越多越容易抽到重复内容</text>
				</view>

				<view class="result-actions">
					<view class="action-btn" :class="{ disabled: remainingDraws <= 0 }" @click="reset"><text>重新选择</text></view>
					<view class="action-btn action-primary" :class="{ disabled: remainingDraws <= 0 }" @click="recommendAgain">
						<text v-if="remainingDraws > 0">再来一次{{ remainingSuffix }}</text>
						<text v-else>今日已用完</text>
					</view>
				</view>
				<view class="more-draw-btn" v-if="remainingDraws <= 0" @click="toggleTaskPanel">
					<text class="more-draw-text">次数用完了？做任务或看广告可以换次数</text>
				</view>
				<view class="share-entry" @click="openSharePoster">
					<text class="share-entry-text">分享这次结果</text>
					<text class="share-entry-sub">存成图片 / 小程序卡片</text>
				</view>
			</view>
		</view>
	<!-- App 端出图用的离屏画布：海报由 canvas 重绘，不再截屏
	     （Android 硬件加速下 webview.draw() 只能拿到纯黑图） -->
	<!-- #ifdef APP-PLUS -->
	<canvas canvas-id="dailyPosterCanvas" id="dailyPosterCanvas" style="position: absolute; left: -9999px; top: -9999px; width: 750rpx; height: 2600rpx;"></canvas>
	<!-- #endif -->

	<!-- 分享预览：结果卡片原样铺满整屏 -->
		<view class="shot-layer" v-if="sharePosterOpen">
			<view class="shot-page">
				<view class="result-card" v-if="currentResult && currentResult.special !== 'lucky'">
					<view class="result-header">
						<text class="result-eyebrow">今日宜挂</text>
						<text class="result-title">{{ selectedOptionLabel }}</text>
					</view>
					<view class="op-card">
						<view class="op-avatar">
							<image v-if="currentResult.avatar" :src="imgSrc(currentResult.avatar)" mode="aspectFill" class="op-img"></image>
							<text v-else class="op-emoji">{{ currentResult.emoji || '?' }}</text>
						</view>
						<view class="op-info">
							<text class="op-name">{{ currentResult.name }}</text>
							<view class="op-tags" v-if="currentResult.tag">
								<text class="op-tag">{{ currentResult.tag }}</text>
							</view>
							<view class="op-box" v-if="currentResult.box_id">
								<text class="op-box-text">推荐盒号：</text>
								<text class="op-box-id">{{ currentResult.box_id }}</text>
							</view>
						</view>
					</view>
					<view class="reason-box">
						<text class="reason-label">推荐理由</text>
						<text class="reason-text">{{ currentResult.reason }}</text>
					</view>
					<view class="warn-box" v-if="currentResult.warn">
						<text class="warn-label">⚠ 慎重</text>
						<text class="warn-text">{{ currentResult.warn }}</text>
					</view>
					<text class="result-tip">结果随机生成，仅供娱乐</text>
					<view class="shot-actions">
						<view class="action-btn"><text>重新选择</text></view>
						<view class="action-btn action-primary"><text>再来一次</text></view>
					</view>
				</view>

				<view class="result-card special-card" v-else>
					<view class="result-header">
						<text class="result-eyebrow">今日宜挂</text>
						<text class="result-title">{{ selectedOptionLabel }}</text>
					</view>
					<view class="op-card special-op">
						<text class="op-emoji-big">✨</text>
					</view>
					<view class="reason-box">
						<text class="reason-text">您的运气今天爆棚，似乎不需要悬挂通行证，放心大胆去做吧~</text>
					</view>
					<text class="result-tip">结果随机生成，仅供娱乐</text>
					<view class="shot-actions">
						<view class="action-btn"><text>重新选择</text></view>
						<view class="action-btn action-primary"><text>再来一次</text></view>
					</view>
				</view>
			</view>

			<view class="shot-foot">
				<image class="shot-qr" src="/static/wechat-code.jpg" mode="aspectFit"></image>
				<view class="shot-foot-text">
					<text class="shot-foot-title">微信扫码 · 测测你今天宜挂什么</text>
					<text class="shot-foot-sub">方舟通行证谷子查询工具</text>
				</view>
			</view>

			<view class="shot-bar" v-if="!capturing">
				<view class="poster-btn" @click="closeSharePoster"><text>关闭</text></view>
				<!-- #ifdef APP-PLUS -->
				<view class="poster-btn" @click="sendPoster"><text>发送图片</text></view>
				<view class="poster-btn primary" @click="savePoster"><text>保存图片</text></view>
				<!-- #endif -->
				<!-- #ifdef MP-WEIXIN -->
				<button class="poster-btn poster-btn-share" open-type="share"><text>小程序卡片</text></button>
				<view class="poster-btn primary" @click="copyShareText"><text>复制文案</text></view>
				<!-- #endif -->
				<!-- #ifdef H5 -->
				<view class="poster-btn primary" @click="copyShareText"><text>复制文案</text></view>
				<!-- #endif -->
			</view>

			<view class="poster-hint" v-if="!capturing">
				<text>{{ posterHint }}</text>
			</view>
		</view>

	<!-- 每日任务抽屉 -->
		<view class="task-mask" v-if="panelOpen" @click="toggleTaskPanel"></view>
		<view class="task-sheet" :class="{ open: panelOpen }">
			<view class="task-sheet-head">
				<view class="task-sheet-title-wrap">
					<text class="task-sheet-title">今日额外机会</text>
					<text class="task-sheet-sub">{{ taskSheetSub }}</text>
				</view>
				<view class="task-sheet-close" @click="toggleTaskPanel">
					<view class="x-line a"></view>
					<view class="x-line b"></view>
				</view>
			</view>

			<view class="task-rule">
				<text class="task-rule-text">完成任意任务 +1 次重测机会，当天任务全部完成再 +1 次；看完一次激励视频广告，今日内不限次数</text>
			</view>

			<view class="task-list">
				<view
					class="task-item"
					:class="{ done: t.done }"
					v-for="t in dailyTasks"
					:key="t.id"
					@click="goTask(t)"
				>
					<view class="task-check">
						<view class="task-check-ring" :class="{ on: t.done }"></view>
					</view>
					<view class="task-body">
						<text class="task-title">{{ t.title }}</text>
						<text class="task-desc">{{ t.desc }}</text>
					</view>
					<text class="task-action">{{ t.done ? '已完成' : '去完成 >' }}</text>
				</view>
			</view>

			<view class="ad-area">
				<view class="ad-btn" v-if="!unlimited" @click="watchAd">
					<text class="ad-btn-text">{{ adLoading ? '广告加载中…' : '看一次广告 · 今日无限次重试' }}</text>
				</view>
				<view class="ad-done" v-else>
					<text class="ad-done-text">已解锁今日无限次重试</text>
				</view>
				<text class="ad-note">广告收益用于维持数据更新，感谢支持</text>
			</view>
		</view>
		<ak-music-bar />
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
import { getDailyProgress, unlockUnlimited, saveDailyState, syncDailyQuota, reportDailyConsume } from "@/utils/dailyTask.js";
import { todayStr } from "@/utils/cloudSync.js";
import { hasElite2, avatarOf } from "@/utils/eliteAvatar.js";
import { openMpShareShot } from "@/utils/screenshot.js";
// #ifdef APP-PLUS
import { renderDailyPoster } from "@/utils/posterCanvas.js";
// #endif
export default {
	data() {
		return {
			// 主题模式
			themeMode: 'simple',
			currentStep: 'select',
			loadingDots: '',
			dotsTimer: null,
			loadingTimer: null,
			selectedOption: '',
			currentResult: null,
			allBoxes: [],
			boxesLoaded: false,
			// 每日限制: 1 次正式 + 1 次额外, 次日重置
			todayDate: '',
			usedToday: 0, // 今日已使用次数 (0/1/2)
			extraUsed: false, // 今日是否已用过额外 1 次
			luckyBoost: 0, // 额外抽取时临时提高彩蛋概率
			// 每日任务
			taskVersion: 0, // 触发 computed 重算（任务完成 / 广告解锁时 +1）
			panelOpen: false, // 任务抽屉是否展开
			adLoading: false,
			videoAd: null,
			// 弹幕：手动拖动 + JS 驱动的自动滚动
			dragRows: {}, // ti -> { dragging, lastX, lastT, vx, moved }
			trackPos: [], // ti -> transform 字符串（小程序端靠它渲染）
			activeDragRow: null,
			suppressClickUntil: 0,
			// 结果分享
			sharePosterOpen: false,
			capturing: false, // 截图期间隐藏操作条，避免把按钮拍进图里
			friendResult: null,

			options: [
				{ key: 'shunxu', label: '守序' },
				{ key: 'hunxie', label: '混邪' },
				{ key: 'laicai', label: '来财' },
				{ key: 'shuini', label: '水逆退散' },
				{ key: 'haoyun', label: '好运常驻' },
				{ key: 'jiankang', label: '健康' },
				{ key: 'kaoshi_mf', label: '满分' },
				{ key: 'kaoshi_jf', label: '加分' },
				{ key: 'jiazhao', label: '驾照' },
				{ key: 'shangan', label: '上岸' },
				{ key: 'zhexue', label: '哲学' },
				{ key: 'jingji', label: '经济' },
				{ key: 'faxue', label: '法学' },
				{ key: 'wenxue', label: '文学' },
				{ key: 'lishi', label: '历史' },
				{ key: 'xinli', label: '心理' },
				{ key: 'gongxue', label: '工学' },
				{ key: 'nongxue', label: '农学' },
				{ key: 'yixue', label: '医学' },
				{ key: 'guanli', label: '管理' },
				{ key: 'yishu', label: '艺术' },
				{ key: 'ketongshi', label: '克同事' },
				{ key: 'zhichang', label: '职场' },
				{ key: 'zhiye', label: '职业' }
			],
			recommendDB: {
				shunxu: {
					theme: [
						{ name: '杰西卡', reason: '守序推荐：低星战神，稳稳当当不惹事。' },
						{ name: '塞雷娅', reason: '守序推荐：莱茵生命防卫科主任，规则就是底线。' },
						{ name: '凯尔希', reason: '守序推荐：罗德岛医疗主管，一切按流程办事。' },
						{ name: '送葬人', reason: '守序推荐：拉特兰公证所执行者，程序即正义。' },
						{ name: '临光', reason: '守序推荐：卡西米尔骑士，恪守骑士守则。' },
						{ name: '阿米娅', reason: '守序推荐：罗德岛领袖，责任在肩不逾矩。' }
					],
					others: [
						{ name: '星熊', reason: '万象交错的捍卫者，做事有板有眼。' },
						{ name: '诗怀雅', reason: '龙门富家千金，做事讲规矩。' },
						{ name: '陈', reason: '龙门近卫局长，铁面无私。' },
						{ name: '菲莱', reason: '守序·割韭：生生不息。' },
						{ name: '杜宾', reason: '教官出身，纪律严明。' },
						{ name: '真理', reason: '理性克制，凡事讲逻辑。' },
						{ name: '推进之王', reason: '维多利亚王者，规矩自成。' },
						{ name: '清道夫', reason: '秩序维护者，专收烂摊子。' },
						{ name: '玛恩纳', reason: '临光家前家主，守规矩的社畜。' }
					],
					ban: ['W', '暮落']
				},
				hunxie: {
					theme: [
						{ name: 'W', reason: '混邪推荐：负负得正，灾厄本身即是庇护。' },
						{ name: '炎客', reason: '混邪推荐：萨卡兹佣兵，危险即正义。' },
						{ name: '弑君者', reason: '混邪推荐：混乱本身的代言人。' },
						{ name: '爱国者(愚人节单领)', reason: '混邪推荐：萨卡兹战神的压迫感。' },
						{ name: '死芒', reason: '混邪推荐：厄运缠绕，负负得正。' }
					],
					others: [
						{ name: 'U-Official', reason: '混邪推荐：灾厄化身，负负得正。' },
						{ name: '塔露拉(愚人节单领)', reason: '混邪推荐：整合运动领袖，灾厄与火焰的化身。' },
						{ name: '幽灵鲨', reason: '深海猎手，危险即是庇护。' },
						{ name: '浊心斯卡蒂', reason: '深海低语，灾厄共鸣。' },
						{ name: '焰尾', reason: '烈火烧尽霉运。' },
						{ name: '熔泉', reason: '以熔岩之姿轰散不祥。' },
						{ name: '深靛', reason: '深海深处，不祥即吉。' }
					],
					ban: ['星熊', '陈']
				},
				laicai: {
					theme: [
						{ name: '老鲤', reason: '来财：江湖老手，现金流稳定。' },
						{ name: '大帝', reason: '来财：企鹅物流创始人，门路通四方。' },
						{ name: '可露希尔', reason: '来财：罗德岛采购，赚差价的好手。' },
						{ name: '银灰', reason: '来财：喀兰贸易总裁，生意遍布雪境。' }
					],
					others: [
						{ name: '菲莱', reason: '来财：韭菜本韭，财富之源生生不息。' },
						{ name: '裁度', reason: '来财：裁断亏损，裁定收益。' },
						{ name: '德克萨斯', reason: '龙门企鹅物流合伙人，业务广泛。' },
						{ name: '诗怀雅', reason: '龙门富家千金，财源滚滚。' },
						{ name: '龙舌兰', reason: '酒桌财神爷，谈钱不伤感情。' },
						{ name: '角峰', reason: '喀兰贸易的稳健账房。' },
						{ name: '锡人', reason: '来财：账目清楚，收益长红。' }
					],
					ban: []
				},
				shuini: {
					theme: [
						{ name: '维什戴尔', reason: '水逆退散：以爆裂黎明轰碎一切霉运的萨卡兹战神。' },
						{ name: '耀骑士临光', reason: '水逆退散：圣光所至，阴霾尽散。' },
						{ name: '圣约送葬人', reason: '水逆退散：以誓约涤净不祥。' }
					],
					others: [
						{ name: '流明', reason: '逆境中点亮光芒。' },
						{ name: '闪灵', reason: '光之疗愈，驱散厄运。' },
						{ name: '夜莺', reason: '圣光护佑，霉运退避。' },
						{ name: '澄闪', reason: '闪光照破阴云。' },
						{ name: '焰影苇草', reason: '焰影灼尽晦气。' }
					],
					ban: ['赫德雷']
				},
				haoyun: {
					theme: [
						{ name: '杜林', reason: '好运常驻：小小的福气持续在线。' },
						{ name: '吉星', reason: '好运常驻：名字里就写着吉星高照。' },
						{ name: '年', reason: '好运常驻：新春瑞兽，喜气自生。' },
						{ name: '铃兰', reason: '好运常驻：幸福之花，福气绵长。' }
					],
					others: [
						{ name: '青枳', reason: '好运常驻：青色生机，福泽绵长。' },
						{ name: '小满', reason: '圆满吉兆。' },
						{ name: '桃金娘', reason: '花开满枝，好运自来。' },
						{ name: '蜜莓', reason: '甜甜蜜蜜，好事发生。' },
						{ name: '桑葚', reason: '硕果累累，福气满满。' },
						{ name: '夕', reason: '丹青纳福。' }
					],
					ban: ['赫德雷']
				},
				jiankang: {
					theme: [
						{ name: '初雪', reason: '健康幸福：圣洁治愈，驱散病痛。' },
						{ name: '华法琳', reason: '健康：血魔医生，疗效有保障。' },
						{ name: '嘉维尔', reason: '健康：罗德岛医疗干员，力气也大。' },
						{ name: '亚叶', reason: '健康：医疗部骨干，值得信赖。' },
						{ name: '蜜莓', reason: '健康：温柔的看护。' }
					],
					others: [
						{ name: '闪灵', reason: '萨卡兹天使，守护健康。' },
						{ name: '夜莺', reason: '圣光护佑，远离病痛。' },
						{ name: '安赛尔', reason: '医疗干员，随叫随到。' },
						{ name: '芙蓉', reason: '健康轻食，身体轻盈。' },
						{ name: '末药', reason: '药到病除。' },
						{ name: '白面鸮', reason: '医疗数据一手掌握。' },
						{ name: '赫默', reason: '医学研究者，讲究证据。' }
					],
					ban: []
				},
				kaoshi_mf: {
					theme: [
						{ name: '小满', reason: '满分buff：花好月圆，诸事圆满。' },
						{ name: '真理', reason: '满分buff：知识就是分数。' },
						{ name: '星极', reason: '满分buff：天体学者，思路如星空般清晰。' },
						{ name: '艾雅法拉', reason: '满分buff：火山学者，写论文猛如火山。' }
					],
					others: [
						{ name: '令', reason: '考运加持。' },
						{ name: '斥罪', reason: '判断力满点。' },
						{ name: '白面鸮', reason: '数据分析满分。' },
						{ name: '麦哲伦', reason: '科研精神，实验必成。' },
						{ name: '锡兰', reason: '冷静推理，答题不慌。' }
					],
					ban: ['赫德雷', '夜魔']
				},
				kaoshi_jf: {
					theme: [
						{ name: '提丰', reason: '加分buff：精准提分，稳稳提升。' },
						{ name: '刻俄柏', reason: '加分buff：会的都答对，不会的也蒙对。' },
						{ name: '棘刺', reason: '加分buff：精准命中每一分。' }
					],
					others: [
						{ name: '早露', reason: '晨光先照，先发制人。' },
						{ name: '云迹', reason: '云端之上抢跑。' },
						{ name: '艾丽妮', reason: '加分buff：优雅拿分。' },
						{ name: '莱恩哈特', reason: '工程施工，分数稳步提升。' }
					],
					ban: []
				},
				jiazhao: {
					theme: [
						{ name: '德克萨斯', reason: '驾照buff：老司机上路，一路顺风。' },
						{ name: '拜松', reason: '驾照buff：科科一把过。' },
						{ name: '芙兰卡', reason: '驾照buff：倒车入库满分。' },
						{ name: '雷蛇', reason: '驾照buff：雷霆科一。' },
						{ name: '能天使', reason: '驾照buff：送快递的老司机，车技一流。' },
						{ name: '推进之王', reason: '驾照buff：开得又快又稳。' }
					],
					others: [
						{ name: '鸿雪', reason: '侧方停车一把进。' },
						{ name: '玛恩纳', reason: '稳重驾驶，文明出行。' },
						{ name: '大帝', reason: '驾照buff：老板的车，稳稳的。' },
						{ name: '可露希尔', reason: '驾照buff：油门刹车都门清。' }
					],
					ban: ['拉普兰德', '新约能天使', '惊蛰']
				},
				shangan: {
					theme: [
						{ name: '左乐', reason: '上岸buff：考公考编稳稳上岸。' },
						{ name: '陈', reason: '上岸buff：龙门警官，一举上岸。' },
						{ name: '星熊', reason: '上岸buff：干员标杆，命运眷顾。' },
						{ name: '诗怀雅', reason: '上岸buff：龙门大小姐，offer接到手软。' },
						{ name: '银灰', reason: '上岸buff：谢拉格掌权者，offer敲门。' },
						{ name: '锏', reason: '上岸buff：卡西米尔骑士，稳步上岸。' },
						{ name: '阿米娅', reason: '上岸buff：罗德岛领袖，责任成就未来。' }
					],
					others: [
						{ name: '维娜·维多利亚', reason: '上岸buff：维多利亚之光，前途璀璨。' },
						{ name: '惊蛰', reason: '一声惊雷，岸在前头。', warn: '需慎重：惊蛰带电，请评估自身属性' },
						{ name: '玛恩纳', reason: '上岸buff：临光家前家主，终于上岸。' },
						{ name: '真理', reason: '上岸buff：理性备考，稳过线。' }
					],
					ban: []
				},
				zhexue: {
					theme: [
						{ name: '闪灵', reason: '伦理学：天使的伦理，永恒的善。' },
						{ name: '逻各斯', reason: '哲学：名字本身就是逻各斯。' },
						{ name: '令', reason: '哲学·道家：一梦逍遥，物我两忘。' }
					],
					others: [
						{ name: '塑心', reason: '哲学·心理：塑心即塑己，知行合一。' },
						{ name: '月禾', reason: '温柔的哲学思辨。' },
						{ name: '夕', reason: '水墨哲思，虚实之间。' },
						{ name: '真理', reason: '以真理之名思辨。' },
						{ name: '锡人', reason: '阅尽千帆后的通透。' }
					],
					ban: []
				},
				jingji: {
					theme: [
						{ name: '诗怀雅', reason: '经济学：龙门财阀，资本的化身。' },
						{ name: '老鲤', reason: '经济学：江湖老手，现金流稳定。' },
						{ name: '锡人', reason: '经济学：投资回报率拉满。' },
						{ name: '大帝', reason: '经济学：企鹅物流创始人，生意经念得最响。' },
						{ name: '可露希尔', reason: '经济学：罗德岛采购，成本控制大师。' }
					],
					others: [
						{ name: '龙舌兰', reason: '酒桌经济学大师。' },
						{ name: '银灰', reason: '谢拉格掌权者，宏观经济一盘棋。' },
						{ name: '角峰', reason: '经济学：喀兰贸易的账本守护者。' },
					],
					ban: []
				},
				faxue: {
					theme: [
						{ name: '斥罪', reason: '法学：罪与罚的裁决者。' },
						{ name: '惊蛰', reason: '法学：惊雷一响，法庭站我这边。' },
						{ name: '送葬人', reason: '法学：拉特兰公证所，执行即法律。' },
						{ name: '圣约送葬人', reason: '法学：以圣约之名，判决不容置疑。' }
					],
					others: [
						{ name: '但书', reason: '法学：卡西米尔法律专精，以律法凝滞纷争。' },
						{ name: '真理', reason: '法学：以真理之名，明断是非。' },
						{ name: '耀骑士临光', reason: '法学·骑士法：以誓约立规矩。' },
						{ name: '玛恩纳', reason: '法学：老练的规矩人。' }
					],
					ban: []
				},
				wenxue: {
					theme: [
						{ name: '鸿雪', reason: '文学·文字：长空一字，惊艳四座。' },
						{ name: '令', reason: '文学·文字：刹那灵感，永恒佳作。' },
						{ name: '逻各斯', reason: '文学·语言学：词源与修辞的专家。' },
						{ name: '诗怀雅', reason: '文学：龙门名门的文采。' }
					],
					others: [
						{ name: '苏苏洛', reason: '文学·文字：温润笔触，治愈文风。' },
						{ name: '夕', reason: '文学·色彩诗篇。' },
						{ name: '锡兰', reason: '文学：冷静记叙，笔下有据。' },
						{ name: '月见夜', reason: '文学：夜话与故事。' }
					],
					ban: []
				},
				lishi: {
					theme: [
						{ name: '行箸', reason: '历史学：以史为鉴，通晓古今。' },
						{ name: '霍尔海雅', reason: '历史学：岁月沉淀，智慧留香。' },
						{ name: '赫德雷', reason: '历史学：见证过无数史诗的男人。' },
						{ name: '凯尔希', reason: '历史学：见证过太多历史的老人。' },
						{ name: '逻各斯', reason: '历史学：考据词源，溯源求真。' }
					],
					others: [
						{ name: '锡人', reason: '历史的厚重感。' },
						{ name: '维娜·维多利亚', reason: '历史学：维多利亚王室的血脉记忆。' },
						{ name: '锡兰', reason: '历史学：记录在案的才算数。' }
					],
					ban: ['妮芙', '佩佩']
				},
				xinli: {
					theme: [
						{ name: '夜魔', reason: '心理学：梦境解析，潜意识。' },
						{ name: '月禾', reason: '心理学：温和抚慰心结。' },
						{ name: '蕾缪安', reason: '心理学：拉特兰枢机，跨境追缉洞悉人心。' },
						{ name: '赫默', reason: '心理学：以证据说话的研究者。' }
					],
					others: [
						{ name: '塑心', reason: '心理学：塑心即塑己，认知重构。' },
						{ name: '提丰', reason: '精准的认知行为分析。' },
						{ name: '桑葚', reason: '心理学：温和而耐心。' },
						{ name: '真理', reason: '心理学：理性剖析。' }
					],
					ban: []
				},
				gongxue: {
					theme: [
						{ name: '白铁', reason: '工学·电力：铁匠精神，电流掌控。' },
						{ name: '异客', reason: '工学·电力：电学的诗与远方。' },
						{ name: '森蚺', reason: '工学·机械：机械工程学大师。' },
						{ name: '白面鸮', reason: '工学·计算机：医疗数据一把抓。' },
						{ name: '云迹', reason: '工学·航空航天：云端之上，任我翱翔。' },
						{ name: '麦哲伦', reason: '工学·无人机：探索未知的工程师。' },
						{ name: '梅尔', reason: '工学·机械：机械与咪波双修。' },
						{ name: '莱恩哈特', reason: '工学·工程：施工现场的主力。' }
					],
					others: [
						{ name: '提丰', reason: '精准工程学。' },
						{ name: '可露希尔', reason: '工学·系统：罗德岛的后勤工程师。' }
					],
					ban: []
				},
				nongxue: {
					theme: [
						{ name: '黍', reason: '农学：岁岁丰收，五谷丰登。' },
						{ name: '万顷', reason: '农学：沃野千里。' },
						{ name: '小满', reason: '农学：小满胜万全。' },
						{ name: '空弦', reason: '农学：植物一箭开花。' },
						{ name: '桑葚', reason: '农学：颗粒饱满，收成可期。' },
						{ name: '铃兰', reason: '农学：花开满田。' }
					],
					others: [
						{ name: '初雪', reason: '春日融雪好耕种。' },
						{ name: '苏苏洛', reason: '农学：细心照料每一株。' },
						{ name: '苇草', reason: '农学：野火烧不尽，春风吹又生。' }
					],
					ban: []
				},
				yixue: {
					theme: [
						{ name: '凯尔希', reason: '医学·药学：医药双修，Mon3tr护体。' },
						{ name: '流明', reason: '医学·护理：护理之光。' },
						{ name: '夜莺', reason: '医学·护理：圣光护佑。' },
						{ name: '诺威尔', reason: '医学·眼视光：眼清目明。' },
						{ name: '华法琳', reason: '医学·血魔：疗法独特，效果拔群。' },
						{ name: '嘉维尔', reason: '医学·外科：罗德岛医疗干员。' },
						{ name: '亚叶', reason: '医学：医疗部中坚。' },
						{ name: '蜜莓', reason: '医学·康复：温柔看护。' },
						{ name: '赫默', reason: '医学·研究：循证医学。' },
						{ name: '安赛尔', reason: '医学：可靠的医疗干员。' },
						{ name: '芙蓉', reason: '医学·营养：健康饮食安排上了。' },
						{ name: '末药', reason: '医学·药房：药到病除。' }
					],
					others: [
						{ name: '闪灵', reason: '医学权威。' },
						{ name: '白面鸮', reason: '医学：病历数据专家。' },
						{ name: '苏苏洛', reason: '医学：耐心细致的看护。' }
					],
					ban: []
				},
				guanli: {
					theme: [
						{ name: '拜松', reason: '管理学·HR：识人善任。' },
						{ name: '早露', reason: '管理学·HR：晨光先照。' },
						{ name: '龙舌兰', reason: '管理学·商务：酒桌谈判专家。' },
						{ name: '玛恩纳', reason: '管理学：临光家前家主，管理经验拉满。' },
						{ name: '凯尔希', reason: '管理学：医疗与决策双线统领。' },
						{ name: '陈', reason: '管理学：龙门近卫局长，管得明白。' }
					],
					others: [
						{ name: '银灰', reason: '战略管理。' },
						{ name: '诗怀雅', reason: '家族企业管理。' },
						{ name: '锡兰', reason: '管理学：把事务理清楚。' },
						{ name: '维娜·维多利亚', reason: '管理学：王者之治。' }
					],
					ban: []
				},
				yishu: {
					theme: [
						{ name: '车尔尼', reason: '艺术学·音乐：练习曲之神。' },
						{ name: '稀音', reason: '艺术学·摄影：定格瞬间之美。' },
						{ name: '夕', reason: '艺术学·绘画：色彩诗篇。' },
						{ name: '塑心', reason: '艺术学·心理：塑心即塑艺。' },
						{ name: '鸿雪', reason: '艺术学·书法：一笔惊艳。' },
						{ name: '逻各斯', reason: '艺术学·修辞：语言的艺术。' }
					],
					others: [
						{ name: '令', reason: '艺术学·综合：刹那永恒。' },
						{ name: '苏苏洛', reason: '艺术学：温柔的笔触。' },
						{ name: '月禾', reason: '艺术学：清雅的审美。' }
					],
					ban: []
				},
				ketongshi: {
					theme: [
						{ name: 'W', reason: '克同事/领导：爆炸艺术。萨卡兹佣兵' },
						{ name: '炎客', reason: '克同事/领导：危险。' },
						{ name: '暮落', reason: '克同事/领导：黑帮教父。' },
						{ name: 'U-Official', reason: '克同事/领导：灾厄压迫，敬畏。' },
						{ name: '塔露拉(愚人节单领)', reason: '克同事/领导：整合运动领袖气场。' },
						{ name: '弑君者', reason: '克同事/领导：混乱制造者，谁都不想遇上。' },
						{ name: '爱国者(愚人节单领)', reason: '克同事/领导：萨卡兹战神的压迫感。' },
						{ name: '幽灵鲨', reason: '克同事/领导：危险气息拉满。' }
					],
					others: [
						{ name: '青枳', reason: '克同事/领导：青出于蓝，气场压制。' },
						{ name: '异客', reason: '电流让你冷静。' },
						{ name: '惊蛰', reason: '一声惊雷镇场。' },
						{ name: '浊心斯卡蒂', reason: '深海气场，无人敢多言。' },
						{ name: '焰尾', reason: '一点就燃，别去招惹。' },
						{ name: '熔泉', reason: '暴躁但可靠。' }
					],
					ban: ['星熊', '左乐', '陈']
				},
				zhichang: {
					theme: [
						{ name: '玛恩纳', reason: '职场buff·反卷：临光家前家主，绝不内卷。' },
						{ name: '银灰', reason: '职场buff·利公司：谢拉格掌权者。' },
						{ name: '掠风', reason: '职场buff·找工作：风一样的offer。' },
						{ name: '老鲤', reason: '职场buff·事务所：江湖老手，公务门清。' },
						{ name: '可露希尔', reason: '职场buff·采购：跟谁都能谈。' },
						{ name: '德克萨斯', reason: '职场buff·物流：效率至上。' }
					],
					others: [
						{ name: '诗怀雅', reason: '职场高起点。' },
						{ name: '拜松', reason: '职场新人的靠谱前辈。' },
						{ name: '大帝', reason: '职场buff·老板：跟对老板很重要。' },
						{ name: '角峰', reason: '职场buff·财务：账目干净。' }
					],
					ban: []
				},
				zhiye: {
					theme: [
						{ name: '澄闪', reason: '职业·美食：甜品大师。' },
						{ name: '玛恩纳', reason: '职业·社畜：临光家前家主，打工魂。' },
						{ name: '锡人', reason: '职业·社畜：上班不累。' },
						{ name: '车尔尼', reason: '职业·音乐：练习曲之神。' },
						{ name: '稀音', reason: '职业·摄影：定格瞬间之美。' },
						{ name: '大帝', reason: '职业·老板：创业大佬。' }
					],
					others: [
						{ name: '菲莱', reason: '职业·市场：韭皇归来。' },
						{ name: '裁度', reason: '职业·律师：裁断之道。' },
						{ name: '老鲤', reason: '职业·会计：及时行乐,自然就会万事大吉。', warn: '不宜：会计专（专精会计者慎挂）' },
						{ name: '拜松', reason: '职业·烘焙：面包香气。' },
						{ name: '可露希尔', reason: '职业·采购：把成本打下来。' },
						{ name: '角峰', reason: '职业·财务：稳重可靠。' },
						{ name: '亚叶', reason: '职业·医疗：专业对口。' }
					],
					ban: []
				}
			}
		};
	},
	computed: {
		selectedOptionLabel() {
			const f = this.options.find(o => o.key === this.selectedOption);
			return f ? f.label : '';
		},
		barrageTracks() {
			const per = 8;
			const all = this.options;
			return [
				{ reverse: false, items: [...all.slice(0, per), ...all.slice(0, per)] },
				{ reverse: true, items: [...all.slice(per, 2 * per), ...all.slice(per, 2 * per)] },
				{ reverse: false, items: [...all.slice(2 * per), ...all.slice(2 * per)] }
			];
		},

		// 每日任务进度（依赖 taskVersion，任务完成或广告解锁后自动重算）
		dailyProgress() {
			this.taskVersion; // 建立响应式依赖
			return getDailyProgress(this.usedToday);
		},
		dailyTasks() {
			return this.dailyProgress.tasks;
		},
		unlimited() {
			return this.dailyProgress.unlimited;
		},
		// 今日剩余重测次数（无限时返回 Infinity）
		remainingDraws() {
			return this.dailyProgress.remaining;
		},
		remainingSuffix() {
			const p = this.dailyProgress;
			if (p.unlimited) return '（无限）';
			if (p.remaining <= 0) return '';
			return '（剩 ' + p.remaining + ' 次）';
		},
		taskToggleText() {
			const p = this.dailyProgress;
			if (p.unlimited) return '今日额外机会 · 已解锁无限次';
			return '今日额外机会 · 剩余 ' + p.remaining + ' 次 · ' + p.doneCount + '/' + p.total + ' 任务已完成';
		},
		taskSheetSub() {
			const p = this.dailyProgress;
			if (p.unlimited) return '今日无限次重试已解锁';
			return '今日剩余 ' + p.remaining + ' 次 · 完成 ' + p.doneCount + '/' + p.total + ' 个任务已获得 ' + p.rewardDraws + ' 次额外机会';
		},
		posterHint() {
			// #ifdef APP-PLUS
			return '点「保存图片」会把这张卡片存进相册';
			// #endif
			// #ifdef MP-WEIXIN
			return '点「保存图片」会调用 Skyline 截图生成图片，需微信 8.0.33 以上';
			// #endif
			// #ifdef H5
			return '用系统截图保存这张卡片，或复制文案分享';
			// #endif
			return '';
		}
	},
	onLoad(options) {
		this.loadThemeSetting();
		this.readShareQuery(options);
		let n = 0;
		this.dotsTimer = setInterval(() => {
			n = (n + 1) % 4;
			this.loadingDots = '.'.repeat(n);
		}, 400);
		this.loadDailyState();
		this.loadBoxesData();
		this.initRewardedVideoAd();
		
		
		// 设置分享配置
		uni.showShareMenu && uni.showShareMenu({
		    withShareTicket: true,
		    menus: ['shareAppMessage', 'shareTimeline']
		});
		
		
	},
	// 首次渲染完成后量一次每行宽度，供松手时把位移折算成动画相位
	onReady() {
		this.$nextTick(() => this.measureTracks());
	},

	onShow() {
		// 从任务页返回后刷新任务完成情况
		this.taskVersion++;
		// 页面可见时恢复弹幕滚动（切后台就停，不白烧电）
		if (this._barrage && this._barrage.started) this.startBarrage();
	},
	onHide() {
		this.stopBarrage();
	},
	watch: {
		// 结果页 -> 选择页时 DOM 重建，节点引用要重新取，位移量保留
		currentStep(v) {
			if (v === 'select') this.$nextTick(() => this.measureTracks(true));
		}
	},
	// 分享给好友：标题 / 路径都带上本次抽到的结果
	onShareAppMessage() {
		return {
			title: this.shareCardTitle(),
			path: this.shareCardPath()
		};
	},

	// 分享到朋友圈
	onShareTimeline() {
		const p = this.shareCardPath();
		const idx = p.indexOf('?');
		return {
			title: this.shareCardTitle(),
			query: idx > -1 ? p.slice(idx + 1) : ''
		};
	},
	onUnload() {
		if (this.dotsTimer) clearInterval(this.dotsTimer);
		if (this.loadingTimer) clearTimeout(this.loadingTimer);
		this.unbindMouseDoc && this.unbindMouseDoc();
	},
	methods: {
loadThemeSetting() {
			try {
				const t = uni.getStorageSync('themeMode');
				this.themeMode = (t && ['simple', 'ak', 'ark', 'jieyuan', 'liquid', 'liquidDark'].indexOf(t) > -1) ? t : 'simple';
			} catch (e) {
				this.themeMode = 'simple';
			}
		},
		logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
		goBack() { uni.navigateBack(); },

		getTodayKey() {
			return todayStr();
		},

		loadDailyState() {
			try {
				const rec = uni.getStorageSync('dailyRecState') || {};
				const today = this.getTodayKey();
				if (rec.date === today) {
					this.todayDate = rec.date;
					this.usedToday = rec.usedToday || 0;
					this.extraUsed = !!rec.extraUsed;
					// 已无剩余次数才直接回到结果页；若通过任务 / 广告获得了新次数，则回到选择页
					if (getDailyProgress(this.usedToday).remaining <= 0) {
						this.selectedOption = rec.selectedOption || '';
						this.currentResult = rec.lastResult || { special: 'lucky', note: '今日已抽完啦' };
						this.currentStep = 'result';
					}
				} else {
					// 跨天 -> 重置
					this.todayDate = today;
					this.usedToday = 0;
					this.extraUsed = false;
					uni.setStorageSync('dailyRecState', { date: today, usedToday: 0, extraUsed: false });
				}
			} catch (e) {
				this.logError(e);
				this.usedToday = 0;
				this.extraUsed = false;
			}
			// 本地值只是显示缓存，真实用量在服务端；清缓存后这里会把次数拉回来
			this.syncCloudQuota();
		},

		async syncCloudQuota() {
			try {
				const remote = await syncDailyQuota();
				if (!remote) return;
				if (remote.used > this.usedToday) {
					this.usedToday = remote.used;
					this.extraUsed = this.usedToday >= 2;
					const prev = uni.getStorageSync('dailyRecState') || {};
					uni.setStorageSync('dailyRecState', Object.assign({}, prev, {
						date: this.todayDate,
						usedToday: this.usedToday,
						extraUsed: this.extraUsed
					}));
				}
				if (remote.unlimited && !this.unlimited) {
					saveDailyState({ unlimited: true });
				}
			} catch (e) {
				this.logError(e);
			}
		},

		consumeDraw() {
			this.usedToday++;
			if (this.usedToday >= 2) this.extraUsed = true;
			reportDailyConsume();
			try {
				uni.setStorageSync('dailyRecState', {
					date: this.todayDate,
					usedToday: this.usedToday,
					extraUsed: this.extraUsed
				});
			} catch (e) {
				this.logError(e);
				
			}
		},

		persistResult() {
			try {
				const prev = uni.getStorageSync('dailyRecState') || {};
				uni.setStorageSync('dailyRecState', {
					date: this.todayDate,
					usedToday: this.usedToday,
					extraUsed: this.extraUsed,
					selectedOption: this.selectedOption,
					lastResult: this.currentResult
				});
			} catch (e) {
				this.logError(e);
				
			}
		},

		async loadBoxesData() {
			// 直接使用 index 页已缓存的数据, 不再远程拉取(避免 gitcode 429 限流)
			try {
				const cached = uni.getStorageSync('arknightsData');
				if (cached && Array.isArray(cached) && cached.length) {
					this.allBoxes = cached;
					this.boxesLoaded = true;
					console.log('[今日宜挂] 使用首页缓存数据', this.allBoxes.length, '盒');
					return true;
				}
			} catch (e) {
				this.logError(e);
				
			}
			this.allBoxes = [];
			this.boxesLoaded = false;
			console.warn('[今日宜挂] 首页无缓存数据, 请先进入首页加载');
			return false;
		},

		toggleTaskPanel() {
			this.panelOpen = !this.panelOpen;
		},

		// 跳去完成某个任务
		goTask(t) {
			if (!t || !t.path) return;
			this.panelOpen = false;
			uni.navigateTo({
				url: t.path,
				fail: (err) => {
					this.logError(err, 'daily_recommend.goTask');
					uni.showToast({ title: '页面打开失败', icon: 'none' });
				}
			});
		},

		// 创建激励视频广告实例
		initRewardedVideoAd() {
			// #ifdef MP-WEIXIN
			try {
				if (typeof wx !== 'undefined' && wx.createRewardedVideoAd) {
					this.videoAd = wx.createRewardedVideoAd({
						adUnitId: 'adunit-d11c06a97af3e479'
					});
					this.videoAd.onLoad(() => {
						this.adLoading = false;
					});
					this.videoAd.onError((err) => {
						console.error('激励视频广告加载失败', err);
						this.adLoading = false;
					});
					this.videoAd.onClose((res) => {
						if (res && res.isEnded) {
							this.handleAdReward();
						} else {
							uni.showToast({ title: '完整看完广告才能解锁哦', icon: 'none' });
						}
					});
				}
			} catch (e) {
				this.logError(e, 'daily_recommend.initRewardedVideoAd');
			}
			// #endif
		},

		// 用户点击「看一次广告」
		watchAd() {
			if (this.unlimited) {
				uni.showToast({ title: '今日已解锁无限次', icon: 'none' });
				return;
			}
			if (!this.videoAd) {
				uni.showModal({
					title: '广告暂不可用',
					content: '当前环境（如开发者工具）暂不支持激励视频广告，请在真机上体验。开发者工具中可切换基础库版本后重试。',
					showCancel: false,
					confirmText: '知道了'
				});
				return;
			}
			this.adLoading = true;
			this.videoAd.show().catch(() => {
				// 失败重试一次
				this.videoAd.load()
					.then(() => this.videoAd.show())
					.catch((err) => {
						console.error('激励视频 广告显示失败', err);
						this.adLoading = false;
						uni.showToast({ title: '广告加载失败，请稍后再试', icon: 'none' });
					});
			});
		},

		// 广告看完：解锁当日无限次
		handleAdReward() {
			this.adLoading = false;
			unlockUnlimited();
			this.taskVersion++;
			uni.showToast({ title: '已解锁今日无限次重试', icon: 'success' });
		},

		// ── 弹幕横向滚动（JS 驱动）─────────────────────────────────────────
		// 为什么不用 CSS 动画：手拖得能「往回拉」，而 animation-delay 只能单向累加，
		// 往回拉会被浏览器卡在起点，跨周期还会被归一化，位置直接跳一屏。
		// 改成每帧算位移：自动滚动是恒定速度；拖动时手指位移直接并进位移量（自动速度
		// 暂停，所以拖得住）；松手把甩动速度按阻尼衰减，滑一段再收回自动速度。
		initBarrage() {
			if (!this._barrage) {
				this._barrage = { rows: [], raf: 0, running: false, last: 0, started: false, retry: 0 };
			}
			return this._barrage;
		},

		measureTracks(force) {
			const b = this.initBarrage();
			if (b.started && !force) return;
			try {
				const q = uni.createSelectorQuery().in(this);
				q.selectAll('.barrage-track').boundingClientRect();
				q.exec((res) => {
					const list = (res && res[0]) || [];
					if (!list.length) {
						b.retry += 1;
						if (b.retry <= 3) setTimeout(() => this.measureTracks(true), 150);
						return;
					}
					let nodes = [];
					// #ifdef H5 || APP-PLUS
					try {
						const root = (this.$el && this.$el.querySelectorAll) ? this.$el : document;
						nodes = Array.prototype.slice.call(root.querySelectorAll('.barrage-track'));
					} catch (e) {
						nodes = [];
					}
					// #endif
					const prev = b.rows || [];
					b.rows = this.barrageTracks.map((t, i) => {
						const w = (list[i] && list[i].width) || 0;
						const half = w > 60 ? w / 2 : 600;
						const dur = (t && t.reverse) ? 30000 : 24000;
						return {
							reverse: !!t.reverse,
							half: half,
							autoV: half / dur, // px/ms，沿内容前进方向
							off: (prev[i] && prev[i].off) || 0,
							extraV: 0,
							dragging: false,
							node: nodes[i] || null
						};
					});
					b.started = true;
					this.paintAll();
					this.startBarrage();
				});
			} catch (e) {
				this.logError(e, 'measureTracks');
			}
		},

		paint(i) {
			const b = this._barrage;
			if (!b || !b.rows[i]) return;
			const row = b.rows[i];
			let off = row.off % row.half;
			if (off < 0) off += row.half;
			const x = row.reverse ? (off - row.half) : -off;
			const t = 'translate3d(' + x.toFixed(2) + 'px, 0, 0)';
			this.trackPos[i] = t;
			if (row.node) {
				row.node.style.transform = t;
			} else {
				this.$set(this.trackPos, i, t);
			}
		},

		paintAll() {
			const b = this._barrage;
			if (!b) return;
			for (let i = 0; i < b.rows.length; i++) this.paint(i);
		},

		nextFrame(cb) {
			if (typeof requestAnimationFrame === 'function') return requestAnimationFrame(cb);
			return setTimeout(() => cb(Date.now()), 33);
		},

		startBarrage() {
			const b = this.initBarrage();
			if (b.running || !b.rows.length) return;
			b.running = true;
			b.last = Date.now();
			const step = () => {
				if (!b.running) return;
				const now = Date.now();
				let dt = now - b.last;
				b.last = now;
				if (dt > 60) dt = 60;
				const rows = b.rows;
				for (let i = 0; i < rows.length; i++) {
					const row = rows[i];
					if (!row) continue;
					if (!row.dragging) {
						row.off += (row.autoV + row.extraV) * dt;
						if (row.extraV) {
							row.extraV *= Math.exp(-dt / 160);
							if (Math.abs(row.extraV) < 0.004) row.extraV = 0;
						}
					}
					this.paint(i);
				}
				b.raf = this.nextFrame(step);
			};
			b.raf = this.nextFrame(step);
		},

		stopBarrage() {
			const b = this._barrage;
			if (!b) return;
			b.running = false;
			if (b.raf) {
				if (typeof cancelAnimationFrame === 'function') {
					try { cancelAnimationFrame(b.raf); } catch (e) { /* 忽略 */ }
				} else {
					try { clearTimeout(b.raf); } catch (e) { /* 忽略 */ }
				}
				b.raf = 0;
			}
		},

		trackStyle(ti) {
			return { transform: this.trackPos[ti] || 'translate3d(0px, 0, 0)' };
		},

		shiftStyle(ti) {
			return {};
		},

		isDragging(ti) {
			const st = this.dragRows[ti];
			return !!(st && st.dragging);
		},

		pointOf(e) {
			if (!e) return null;
			if (e.touches && e.touches.length) return { x: e.touches[0].clientX, y: e.touches[0].clientY };
			if (e.changedTouches && e.changedTouches.length) return { x: e.changedTouches[0].clientX, y: e.changedTouches[0].clientY };
			if (typeof e.clientX === 'number') return { x: e.clientX, y: e.clientY };
			return null;
		},

		onDragStart(e, ti) {
			if (this.currentStep !== 'select') return;
			const b = this.initBarrage();
			if (!b.started) { this.measureTracks(); return; }
			const row = b.rows[ti];
			if (!row || row.dragging) return;
			const p = this.pointOf(e);
			if (!p) return;
			if (e && e.type === 'mousedown' && e.preventDefault) e.preventDefault();
			row.dragging = true;
			row.extraV = 0;
			this.activeDragRow = ti;
			this.$set(this.dragRows, ti, {
				dragging: true,
				lastX: p.x,
				lastT: Date.now(),
				acc: 0,
				vx: 0,
				moved: false
			});
			this.bindMouseDoc();
		},

		onDragMove(e, ti) {
			const b = this._barrage;
			const row = b && b.rows[ti];
			const st = this.dragRows[ti];
			if (!row || !row.dragging || !st) return;
			const p = this.pointOf(e);
			if (!p) return;
			const now = Date.now();
			const dx = p.x - st.lastX;
			st.acc = (st.acc || 0) + dx;
			if (Math.abs(st.acc) > 6) st.moved = true;
			const dt = now - st.lastT;
			if (dt > 0) st.vx = st.vx * 0.6 + (dx / dt) * 0.4;
			st.lastX = p.x;
			st.lastT = now;
			// 手指往右 = 内容右移：正行 off 减少，反向行 off 增加
			row.off += row.reverse ? dx : -dx;
			this.paint(ti);
			this.$set(this.dragRows, ti, Object.assign({}, st));
		},

		onDragEnd(e, ti) {
			const b = this._barrage;
			const row = b && b.rows[ti];
			const st = this.dragRows[ti];
			if (!row || !row.dragging || !st) return;
			this.activeDragRow = null;
			const now = Date.now();
			// 松手前停住超过 90ms 就不给惯性（避免「拖到位又自己滑走」）
			const vx = (now - st.lastT > 90) ? 0 : st.vx;
			row.dragging = false;
			// 内容速度与屏幕方向相反；甩太快会飞过头，限到 ±2px/ms
			let v = vx;
			if (v > 2) v = 2;
			if (v < -2) v = -2;
			row.extraV = (row.reverse ? v : -v) * 0.55;
			if (Math.abs(row.extraV) < 0.01) row.extraV = 0;
			if (st.moved) this.suppressClickUntil = Date.now() + 300;
			this.$set(this.dragRows, ti, Object.assign({}, st, { dragging: false, vx: 0 }));
		},

		bindMouseDoc() {
			// #ifdef H5
			if (this._mouseBound) return;
			this._mouseBound = true;
			this._onDocMove = (ev) => {
				if (this.activeDragRow !== null) this.onDragMove(ev, this.activeDragRow);
			};
			this._onDocUp = (ev) => {
				if (this.activeDragRow !== null) this.onDragEnd(ev, this.activeDragRow);
			};
			document.addEventListener('mousemove', this._onDocMove);
			document.addEventListener('mouseup', this._onDocUp);
			// #endif
		},

		unbindMouseDoc() {
			// #ifdef H5
			if (this._onDocMove) document.removeEventListener('mousemove', this._onDocMove);
			if (this._onDocUp) document.removeEventListener('mouseup', this._onDocUp);
			this._mouseBound = false;
			// #endif
		},

		selectOption(key) {
			// 拖动结束时浏览器还会补发一次 click，这段时间内忽略
			if (this.suppressClickUntil && Date.now() < this.suppressClickUntil) return;
			this.selectedOption = key;
		},

		confirmSelection() {
			if (!this.selectedOption) {
				uni.showToast({ title: '请先选择一个', icon: 'none' });
				return;
			}
			if (this.remainingDraws <= 0) {
				uni.showToast({ title: '今日次数已用完，做任务或看广告可换次数', icon: 'none' });
				return;
			}
			this.currentStep = 'loading';
			const delay = 1600 + Math.random() * 800;
			this.loadingTimer = setTimeout(() => this.doRecommend(), delay);
		},

		async doRecommend() {
			if (this.remainingDraws <= 0) {
				uni.showToast({ title: '今日次数已用完', icon: 'none' });
				return;
			}
			// 排除上次结果干员名（仅盒号有差异），避免再来一次出同一个人
			const _excludeNames = new Set();
			if (this.currentResult && this.currentResult.name) {
				_excludeNames.add(this.currentResult.name);
			}

			// 彩蛋(运气爆棚) 基础概率 0.01%, 通过 luckyBoost 临时提高(额外抽取场景)
			const luckyP = 0.0001 + (this.luckyBoost || 0);
			if (Math.random() < luckyP) {
				this.currentResult = { special: 'lucky' };
				this.currentStep = 'result';
				this.friendResult = null;
				this.consumeDraw();
				this.persistResult();
				return;
			}

			const conf = this.recommendDB[this.selectedOption];
			if (!conf) {
				uni.showToast({ title: '该类别暂未配置', icon: 'none' });
				this.reset();
				return;
			}

			// 数据未就绪时先尝试读取首页缓存
			if (!this.allBoxes || !this.allBoxes.length) {
				await this.loadBoxesData();
			}
			// 无缓存数据时明确提示, 避免落到彩蛋兜底卡造成误解
			if (!this.allBoxes || !this.allBoxes.length) {
				// 无数据，跳回首页并高亮下载按钮
				uni.showToast({ title: '数据未加载，请先打开首页', icon: 'none', duration: 2000 });
				setTimeout(() => {
					try { uni.setStorageSync('needHighlight', true); } catch (storageErr) { this.logError(storageErr, 'daily_recommend.setNeedHighlight'); }
					// 判断页面栈, 只有非首屏才回退, 否则直接 reLaunch 避免 cannot navigate back 报错
					const pages = (typeof getCurrentPages === 'function') ? getCurrentPages() : [];
					if (pages && pages.length > 1) {
						try {
							uni.navigateBack({ delta: 1 });
						} catch (navErr) {
							this.logError(navErr, 'daily_recommend.navigateBack');
							uni.reLaunch({ url: '/pages/index/index' });
						}
					} else {
						uni.reLaunch({ url: '/pages/index/index' });
					}
				}, 1800);
				return;
				}
			// 收集全量角色名(来自 Box_id.json), 供内置过滤与外置随机使用
			const allNames = new Set();
			for (const box of (this.allBoxes || [])) {
				for (let i = 1; i <= 8; i++) {
					const ch = box['character' + i];
					if (ch && ch.name) allNames.add(ch.name);
				}
			}
			const banSet = new Set(conf.ban || []);
			const themeSet = new Set((conf.theme || []).map(c => c.name));
			const othersSet = new Set((conf.others || []).map(c => c.name));

			// 三个池配平: theme / others / 外置随机 各约 1/3 概率, 彩蛋 0.01% 最低
			// 按 0.33/0.33/0.34 抽签选池, 再从该池随机; 池为空时回退到其他池
			const themeList = (conf.theme || []).filter(c => !banSet.has(c.name) && allNames.has(c.name) && !_excludeNames.has(c.name));
			const othersList = (conf.others || []).filter(c => !banSet.has(c.name) && allNames.has(c.name) && !_excludeNames.has(c.name));
			const randomPool = [...allNames].filter(n => !banSet.has(n) && !themeSet.has(n) && !othersSet.has(n) && !_excludeNames.has(n));
			let pick = null;
			const r = Math.random();
			if (r < 0.3333 && themeList.length) {
				pick = themeList[Math.floor(Math.random() * themeList.length)];
			} else if (r < 0.6666 && othersList.length) {
				pick = othersList[Math.floor(Math.random() * othersList.length)];
			} else if (randomPool.length) {
				pick = { name: randomPool[Math.floor(Math.random() * randomPool.length)], reason: '今日随机评分，试试这次通行证' };
			} else if (themeList.length) {
				pick = themeList[Math.floor(Math.random() * themeList.length)];
			} else if (othersList.length) {
				pick = othersList[Math.floor(Math.random() * othersList.length)];
			}
			// 兜底: theme 列表首项(不过滤) 保证至少能出结果
			if (!pick) pick = (conf.theme || [])[0] || { name: '？？？' };
			
			const enriched = this.enrichWithBoxData(pick);
			this.currentResult = enriched;
			this.currentStep = 'result';
			this.friendResult = null;
			this.consumeDraw();
			this.persistResult();
		},

		enrichWithBoxData(cand) {
			const all = this.allBoxes || [];
			const found = [];
			for (const box of all) {
				for (let i = 1; i <= 8; i++) {
					const ch = box['character' + i];
					if (ch && ch.name === cand.name) {
						found.push({
							box_id: box.Box_id,
							box_type: box.Box_type || 'normal',
							avatar: ch.imageUrl || '',
							hot: !!ch.hotcharacter,
							nolyELITE1: ch.nolyELITE1 === true,
							market_price: ch.market_price || null
						});
						break;
					}
				}
			}

			if (!found.length) {
				return {
					special: 'lucky',
					...cand,
					note: cand.name + ' 暂未收录在通行证盒号中'
				};
			}

			const chosen = found[Math.floor(Math.random() * found.length)];
			// 无精一精二之分: 白名单凭证/音律款/特别通行认证 一人一卡, 不显示精英等级;
			// nolyELITE1 为 true（或确实没有精二图）的干员只有精一, 不应随机出精二
			const avatarTarget = {
				name: cand.name,
				avatar: chosen.avatar,
				nolyELITE1: chosen.nolyELITE1,
				market_price: chosen.market_price
			};
			const canElite2 = hasElite2(avatarTarget, chosen.box_type);
			let tag = '';
			if (['whitelist', 'ambience', 'special'].includes(chosen.box_type)) {
				tag = '';
			} else if (!canElite2) {
				tag = '精一';
			} else {
				const tags = ['精一', '精二'];
				tag = tags[Math.floor(Math.random() * tags.length)];
			}
			const eliteLevel = tag === '精二' ? 2 : 1;

			return {
				...cand,
				// 抽到精二就显示精二头像，精一则精一头像
				avatar: avatarOf(avatarTarget, eliteLevel, chosen.box_type),
				eliteLevel: canElite2 ? eliteLevel : 0,
				box_id: chosen.box_id,
				tag: tag,
				hot: chosen.hot
			};
		},

		/* ================= 结果分享 ================= */
		buildShareInfo() {
			const r = this.currentResult || {};
			const lucky = r.special === 'lucky';
			return {
				lucky: lucky,
				catLabel: this.selectedOptionLabel || '今日宜挂',
				name: lucky ? '' : (r.name || ''),
				tag: lucky ? '' : (r.tag || ''),
				box: lucky ? '' : (r.box_id || ''),
				reason: lucky
					? '您的运气今天爆棚，似乎不需要悬挂通行证，放心大胆去做吧~'
					: (r.reason || '')
			};
		},

		shareCardTitle() {
			const info = this.buildShareInfo();
			if (info.lucky) return '我今天运气爆棚，不用挂通行证！你适合挂什么？';
			return '我今日宜挂「' + info.name + '」'
				+ (info.box ? '（盒号 ' + info.box + '）' : '')
				+ '，你适合挂什么？';
		},

		shareCardText() {
			const info = this.buildShareInfo();
			if (info.lucky) {
				return '【今日宜挂】' + info.catLabel + '：今天运气爆棚，连通行证都不用挂。\n'
					+ '来「方舟通行证谷子查询工具」抽一张你自己的。';
			}
			return '【今日宜挂】' + info.catLabel + '\n'
				+ '我抽到的通行证：' + info.name
				+ (info.tag ? ' · ' + info.tag : '')
				+ (info.box ? '\n推荐盒号：' + info.box : '')
				+ (info.reason ? '\n推荐理由：' + info.reason : '')
				+ '\n来「方舟通行证谷子查询工具」看看你适合挂什么。';
		},

		shareCardPath() {
			const r = this.currentResult || {};
			if (!r || !r.name) return '/pages/daily_recommend/daily_recommend';
			return '/pages/daily_recommend/daily_recommend?share=1'
				+ '&cat=' + encodeURIComponent(this.selectedOption || '')
				+ '&name=' + encodeURIComponent(r.name)
				+ '&box=' + encodeURIComponent(r.box_id || '');
		},

		// 好友从分享卡片进来时，把 TA 抽到的结果显示出来
		readShareQuery(query) {
			if (!query || !query.share) return;
			const dec = (v) => {
				if (!v) return '';
				try { return decodeURIComponent(v); } catch (e) { return String(v); }
			};
			const name = dec(query.name);
			if (!name) return;
			const catKey = dec(query.cat);
			const cat = this.options.find(o => o.key === catKey);
			const box = dec(query.box);
			this.friendResult = {
				name: name,
				text: '好友今天宜挂「' + name + '」'
					+ (box ? '（盒号 ' + box + '）' : '')
					+ (cat ? ' · ' + cat.label : '')
					+ '，你也来抽一张'
			};
		},

		toast(title) {
			uni.showToast({ title: title, icon: 'none' });
		},

		openSharePoster() {
			if (!this.currentResult) return;
			// #ifdef MP-WEIXIN
			// 小程序没有「截取某个 view」的能力，交给 Skyline 截图页出真图
			openMpShareShot(this.buildShotPayload());
			return;
			// #endif
			this.sharePosterOpen = true;
		},

		// 传给 Skyline 截图页的数据
		buildShotPayload() {
			const r = this.currentResult || {};
			const lucky = r.special === 'lucky';
			return {
				type: 'daily',
				catLabel: this.selectedOptionLabel || '',
				name: r.name || '',
				tag: r.tag || '',
				box: r.box_id || '',
				reason: lucky
					? '您的运气今天爆棚，似乎不需要悬挂通行证，放心大胆去做吧~'
					: (r.reason || ''),
				warn: r.warn || '',
				avatar: (!lucky && r.avatar) ? this.imgSrc(r.avatar) : '',
				special: r.special || '',
				qrTitle: '微信扫码 · 测测你今天宜挂什么',
				shareTitle: this.shareCardTitle(),
				sharePath: this.shareCardPath()
			};
		},

		closeSharePoster() {
			this.sharePosterOpen = false;
		},

		// App 端不再截屏：canvas 重绘成图（utils/posterCanvas.js）
		// 整段只在 App 端存在 —— 小程序端没有 renderDailyPoster，留着是死代码
		// #ifdef APP-PLUS
		async capturePoster() {
			this.capturing = true;
			try {
				const path = await renderDailyPoster(this.buildShotPayload(), 'dailyPosterCanvas', this);
				this.capturing = false;
				return path;
			} catch (e) {
				this.capturing = false;
				throw e;
			}
		},
		// #endif

		async savePoster() {
			// #ifdef APP-PLUS
			try {
				const path = await this.capturePoster();
				uni.saveImageToPhotosAlbum({
					filePath: path,
					success: () => { this.toast('已保存到相册'); },
					fail: (err) => {
						const msg = (err && err.errMsg) || '';
						if (msg.indexOf('auth') > -1 || msg.indexOf('authorize') > -1) {
							uni.showModal({
								title: '需要相册权限',
								content: '请在设置里允许保存图片到相册，然后再试一次',
								confirmText: '去设置',
								success: (r) => { if (r.confirm && uni.openSetting) uni.openSetting({}); }
							});
							return;
						}
						if (msg.indexOf('cancel') === -1) this.toast('保存失败，请用系统截图');
					}
				});
			} catch (e) {
				this.logError(e, 'daily_recommend.share.capture');
				this.toast('截图失败，请用系统截图');
			}
			// #endif
			// #ifndef APP-PLUS
			this.copyShareText();
			// #endif
		},

		async sendPoster() {
			// #ifdef APP-PLUS
			try {
				const path = await this.capturePoster();
				uni.shareWithSystem({
					type: 2,
					imageUrl: path,
					fail: (err) => {
						this.logError(err, 'daily_recommend.share.system');
						this.toast('分享失败，可先保存图片');
					}
				});
			} catch (e) {
				this.logError(e, 'daily_recommend.share.capture');
				this.toast('截图失败，请用系统截图');
			}
			// #endif
			// #ifndef APP-PLUS
			this.copyShareText();
			// #endif
		},

		copyShareText() {
			uni.setClipboardData({
				data: this.shareCardText(),
				success: () => { this.toast('分享文案已复制'); }
			});
		},

		recommendAgain() {
			if (this.remainingDraws <= 0) {
				uni.showToast({ title: '今日次数已用完，做任务或看广告可换次数', icon: 'none' });
				return;
			}
			// 额外抽取: 临时把彩蛋概率从 0.5% 提到 5% (仍属低概率, 不易抽到)
			this.luckyBoost = 0.045;
			this.currentStep = 'loading';
			setTimeout(() => {
				this.doRecommend();
				this.luckyBoost = 0;
			}, 1200);
		},

		reset() {
			this.selectedOption = '';
			this.currentResult = null;
			this.currentStep = 'select';
		}
	}
};
</script>

<style>
.page-root {
	min-height: 100vh;
	background: #f7f7f9;
	color: #1c1c1e;
	padding-bottom: env(safe-area-inset-bottom);
}

.top-bar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 24rpx 32rpx;
	padding-top: calc(24rpx + env(safe-area-inset-top));
	background: #fff;
	border-bottom: 1rpx solid #ececef;
}
.back-btn {
	width: 56rpx;
	height: 56rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	color: #1c1c1e;
	font-size: 48rpx;
	font-weight: 300;
}
.title {
	font-size: 32rpx;
	font-weight: 600;
	color: #1c1c1e;
	letter-spacing: 2rpx;
}
.top-placeholder { width: 56rpx; }

.step-select {
	display: flex;
	flex-direction: column;
	height: calc(100vh - 105rpx - env(safe-area-inset-top) - env(safe-area-inset-bottom));
}
.prompt {
	padding: 16rpx 48rpx 8rpx;
	text-align: center;
}
.prompt-main {
	display: block;
	font-size: 36rpx;
	font-weight: 700;
	color: #1c1c1e;
	letter-spacing: 2rpx;
}.global-credit {
	display: block;
	text-align: center;
	font-size: 20rpx;
	color: #aeaeb2;
	padding: 12rpx 48rpx calc(12rpx + env(safe-area-inset-bottom));
	letter-spacing: 0.5rpx;
	line-height: 1.5;
}
.confirm-btn.disabled,
.action-btn.disabled {
	background: #d1d1d6 !important;
	color: #fff !important;
	border-color: #d1d1d6 !important;
}

.barrage-stage {
	flex: 1;
	display: flex;
	flex-direction: column;
	justify-content: center;
	overflow: hidden;
	padding: 32rpx 0;
}
.barrage-row {
	overflow: hidden;
	white-space: nowrap;
	height: 80rpx;
	margin: 6rpx 0;
	/* 水平方向交给 JS 拖动，竖直方向仍归页面 */
	touch-action: pan-y;
}
.barrage-shift {
	display: flex;
	width: 100%;
	will-change: transform;
	-webkit-user-select: none;
	user-select: none;
}
.barrage-track {
	display: inline-flex;
	gap: 16rpx;
	padding-left: 32rpx;
	/* 位移由 JS 每帧写 transform（手拖和惯性都要双向可控） */
	will-change: transform;
	transform: translate3d(0px, 0, 0);
}
.barrage {
	flex-shrink: 0;
	height: 72rpx;
	padding: 0 32rpx;
	display: inline-flex;
	align-items: center;
	background: transparent;
	border: 1rpx solid #d1d1d6;
	border-radius: 36rpx;
	transition: all 0.2s;
}
.barrage text {
	font-size: 26rpx;
	color: #3a3a3c;
	white-space: nowrap;
}
.barrage.active {
	background: #1c1c1e;
	border-color: #1c1c1e;
}
.barrage.active text {
	color: #fff;
	font-weight: 600;
}

.bottom-action {
	padding: 0 48rpx 16rpx;
}
.confirm-btn {
	width: 100%;
	height: 88rpx;
	background: #1c1c1e;
	border-radius: 44rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 28rpx;
	font-weight: 600;
	color: #fff;
	letter-spacing: 2rpx;
}
.confirm-btn.disabled {
	background: #d1d1d6;
}
.random-tip {
	display: block;
	text-align: center;
	margin-top: 16rpx;
	font-size: 22rpx;
	color: #8e8e93;
}

.step-loading {
	display: flex;
	align-items: center;
	justify-content: center;
	height: calc(100vh - 105rpx - env(safe-area-inset-top) - env(safe-area-inset-bottom));
}
.loading-wrap { text-align: center; }
.loading-spinner {
	width: 56rpx;
	height: 56rpx;
	border: 4rpx solid #ececef;
	border-top-color: #1c1c1e;
	border-radius: 50%;
	margin: 0 auto 32rpx;
	animation: spin 0.9s linear infinite;
}
@keyframes spin { from { transform: rotate(0); } to { transform: rotate(360deg); } }
.loading-text {
	display: block;
	font-size: 28rpx;
	color: #1c1c1e;
	letter-spacing: 2rpx;
}
.loading-dots {
	display: block;
	font-size: 36rpx;
	color: #8e8e93;
	font-weight: 700;
	margin-top: 8rpx;
	height: 44rpx;
}

.step-result {
	padding: 40rpx 32rpx 80rpx;
}
.result-card {
	background: #fff;
	border: 1rpx solid #ececef;
	border-radius: 24rpx;
	padding: 40rpx 32rpx;
}
.result-header {
	text-align: center;
	padding-bottom: 28rpx;
	border-bottom: 1rpx solid #ececef;
	margin-bottom: 28rpx;
}
.result-eyebrow {
	display: block;
	font-size: 22rpx;
	color: #8e8e93;
	letter-spacing: 4rpx;
}
.result-title {
	display: block;
	font-size: 40rpx;
	font-weight: 700;
	color: #1c1c1e;
	margin-top: 8rpx;
}

.op-card {
	display: flex;
	align-items: center;
	gap: 24rpx;
	padding: 24rpx;
	background: #f7f7f9;
	border-radius: 16rpx;
	margin-bottom: 20rpx;
}
.op-avatar {
	width: 112rpx;
	height: 112rpx;
	border-radius: 20rpx;
	background: #ececef;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
	overflow: hidden;
}
.op-img {
	width: 112rpx;
	height: 112rpx;
}
.op-emoji {
	font-size: 56rpx;
}
.op-emoji-big {
	font-size: 96rpx;
}
.op-info {
	flex: 1;
	overflow: hidden;
}
.op-name {
	display: block;
	font-size: 32rpx;
	font-weight: 700;
	color: #1c1c1e;
}
.op-tags {
	display: flex;
	gap: 12rpx;
	margin-top: 8rpx;
}
.op-tag {
	font-size: 22rpx;
	padding: 4rpx 14rpx;
	border-radius: 12rpx;
	background: #1c1c1e;
	color: #fff;
	font-weight: 500;
}
.op-box {
	margin-top: 8rpx;
	display: flex;
	align-items: center;
	gap: 4rpx;
}
.op-box-text {
	font-size: 22rpx;
	color: #8e8e93;
}
.op-box-id {
	font-size: 24rpx;
	color: #1c1c1e;
	font-weight: 600;
	background: #ececef;
	padding: 2rpx 14rpx;
	border-radius: 8rpx;
}

.special-card {
	text-align: center;
}
.special-op {
	justify-content: center;
	padding: 48rpx 24rpx;
	background: linear-gradient(135deg, #fff8eb 0%, #fef3c7 100%);
}

.reason-box,
.warn-box {
	padding: 20rpx 24rpx;
	background: #f7f7f9;
	border-radius: 16rpx;
	margin-bottom: 16rpx;
}
.reason-label,
.warn-label {
	display: block;
	font-size: 22rpx;
	color: #8e8e93;
	margin-bottom: 6rpx;
}
.warn-label { color: #b45309; }
.reason-text,
.warn-text {
	font-size: 26rpx;
	color: #1c1c1e;
	line-height: 1.6;
}
.warn-text { color: #92400e; }
.warn-box {
	background: #fff8eb;
	border: 1rpx solid #fde68a;
}

.result-tip {
	text-align: center;
	margin: 24rpx 0;
	font-size: 22rpx;
	color: #8e8e93;
}
.result-actions {
	display: flex;
	gap: 16rpx;
}
.action-btn {
	flex: 1;
	height: 80rpx;
	border-radius: 40rpx;
	background: #f7f7f9;
	border: 1rpx solid #ececef;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 26rpx;
	font-weight: 500;
	color: #1c1c1e;
}
.action-primary {
	background: #1c1c1e;
	border-color: #1c1c1e;
	color: #fff;
}

/* 高亮下载按钮动画（被其他页面触发） */
.highlight-flash {
  animation: highlightFlash 1s ease-in-out 4;
  border: 2px solid #ff6b35 !important;
  border-radius: 12rpx;
  box-shadow: 0 0 12rpx rgba(255, 107, 53, 0.6) !important;
}
@keyframes highlightFlash {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(1.03); }
}

/* ================= 每日额外机会入口 ================= */
.daily-toggle {
	margin-top: 18rpx;
	height: 72rpx;
	border-radius: 36rpx;
	border: 1rpx solid #e2e2e7;
	background: #fff;
	display: flex;
	align-items: center;
	justify-content: center;
}
.daily-toggle:active {
	background: #f2f2f5;
}
.daily-toggle-text {
	font-size: 24rpx;
	color: #4a4a4f;
}

/* 结果页：次数用完时的补救入口 */
.more-draw-btn {
	margin-top: 20rpx;
	height: 72rpx;
	border-radius: 36rpx;
	border: 1rpx dashed #c9c9d0;
	display: flex;
	align-items: center;
	justify-content: center;
}
.more-draw-text {
	font-size: 24rpx;
	color: #6c6c72;
}

/* 多次重试提醒 */
.retry-warn {
	margin-top: 16rpx;
	padding: 16rpx 20rpx;
	background: #fff8e6;
	border: 1rpx solid #f3e2b3;
	border-radius: 12rpx;
}
.retry-warn-text {
	font-size: 22rpx;
	line-height: 1.6;
	color: #9a7b28;
}

/* ================= 任务抽屉 ================= */
.task-mask {
	position: fixed;
	left: 0;
	right: 0;
	top: 0;
	bottom: 0;
	background: rgba(0, 0, 0, 0.45);
	z-index: 100;
}
.task-sheet {
	position: fixed;
	left: 0;
	right: 0;
	bottom: 0;
	z-index: 101;
	background: #fff;
	border-radius: 28rpx 28rpx 0 0;
	padding: 36rpx 32rpx calc(36rpx + env(safe-area-inset-bottom));
	max-height: 78vh;
	overflow-y: auto;
	transform: translateY(102%);
	transition: transform 0.26s ease;
}
.task-sheet.open {
	transform: translateY(0);
}
.task-sheet-head {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
}
.task-sheet-title-wrap {
	flex: 1;
	min-width: 0;
}
.task-sheet-title {
	display: block;
	font-size: 32rpx;
	font-weight: 700;
	color: #1c1c1e;
}
.task-sheet-sub {
	display: block;
	margin-top: 8rpx;
	font-size: 22rpx;
	line-height: 1.5;
	color: #8e8e93;
}
.task-sheet-close {
	width: 56rpx;
	height: 56rpx;
	position: relative;
	flex-shrink: 0;
	margin-left: 16rpx;
}
.x-line {
	position: absolute;
	left: 50%;
	top: 50%;
	width: 28rpx;
	height: 3rpx;
	background: #8e8e93;
	border-radius: 2rpx;
}
.x-line.a { transform: translate(-50%, -50%) rotate(45deg); }
.x-line.b { transform: translate(-50%, -50%) rotate(-45deg); }

.task-rule {
	margin-top: 22rpx;
	padding: 18rpx 20rpx;
	background: #f7f7f9;
	border-radius: 14rpx;
}
.task-rule-text {
	font-size: 22rpx;
	line-height: 1.6;
	color: #6c6c72;
}

.task-list {
	margin-top: 22rpx;
}
.task-item {
	display: flex;
	align-items: center;
	padding: 22rpx 20rpx;
	border: 1rpx solid #ececef;
	border-radius: 16rpx;
	margin-bottom: 16rpx;
}
.task-item:active {
	background: #fafafc;
}
.task-item.done {
	background: #f4faf5;
	border-color: #d8ecdc;
}
.task-check {
	width: 44rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-shrink: 0;
}
.task-check-ring {
	width: 26rpx;
	height: 26rpx;
	border-radius: 50%;
	border: 3rpx solid #c9c9d0;
	box-sizing: border-box;
}
.task-check-ring.on {
	border-color: #2e9e5b;
	background: #2e9e5b;
}
.task-body {
	flex: 1;
	min-width: 0;
	margin: 0 16rpx;
}
.task-title {
	display: block;
	font-size: 27rpx;
	font-weight: 600;
	color: #1c1c1e;
}
.task-item.done .task-title {
	color: #2e9e5b;
}
.task-desc {
	display: block;
	margin-top: 6rpx;
	font-size: 21rpx;
	line-height: 1.5;
	color: #8e8e93;
}
.task-action {
	font-size: 23rpx;
	color: #4a4a4f;
	flex-shrink: 0;
}
.task-item.done .task-action {
	color: #2e9e5b;
}

.ad-area {
	margin-top: 26rpx;
	padding-bottom: 10rpx;
}
.ad-btn {
	height: 88rpx;
	border-radius: 44rpx;
	background: #1c1c1e;
	display: flex;
	align-items: center;
	justify-content: center;
}
.ad-btn:active {
	opacity: 0.86;
}
.ad-btn-text {
	font-size: 27rpx;
	font-weight: 600;
	color: #fff;
	letter-spacing: 1rpx;
}
.ad-done {
	height: 88rpx;
	border-radius: 44rpx;
	background: #f4faf5;
	border: 1rpx solid #d8ecdc;
	display: flex;
	align-items: center;
	justify-content: center;
}
.ad-done-text {
	font-size: 27rpx;
	font-weight: 600;
	color: #2e9e5b;
}
.ad-note {
	display: block;
	margin-top: 14rpx;
	text-align: center;
	font-size: 20rpx;
	color: #a8a8ae;
}

/* ================= 结果分享 ================= */
.share-entry {
	margin-top: 16rpx;
	height: 88rpx;
	border-radius: 44rpx;
	background: linear-gradient(135deg, #2f7ed8 0%, #52a0f5 100%);
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 14rpx;
}
.share-entry:active { opacity: 0.88; }
.share-entry-text {
	font-size: 27rpx;
	font-weight: 600;
	color: #ffffff;
}
.share-entry-sub {
	font-size: 21rpx;
	color: rgba(255, 255, 255, 0.78);
}

/* 整屏海报：截图即成品 */
.shot-layer {
	position: fixed;
	left: 0;
	right: 0;
	top: 0;
	bottom: 0;
	/* 盖住 H5 页面标题栏（uni-page-head z-index 998） */
	z-index: 9999;
	background: #f7f7f9;
	display: flex;
	flex-direction: column;
	box-sizing: border-box;
	padding: calc(24rpx + env(safe-area-inset-top)) 24rpx calc(16rpx + env(safe-area-inset-bottom));
}
.shot-page {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
	justify-content: flex-start;
	padding-top: 24rpx;
}
.shot-page .result-card {
	width: 100%;
	box-sizing: border-box;
}
.shot-actions {
	display: flex;
	gap: 16rpx;
	margin-top: 24rpx;
}
.shot-page { pointer-events: none; }

.shot-bar {
	display: flex;
	gap: 16rpx;
	margin-top: 20rpx;
}
.shot-foot {
	display: flex;
	align-items: center;
	gap: 18rpx;
	margin-top: 22rpx;
	padding: 0 6rpx;
}
.shot-qr {
	width: 140rpx;
	height: 140rpx;
	flex-shrink: 0;
	border-radius: 12rpx;
	background: #ffffff;
}
.shot-foot-text { flex: 1; min-width: 0; }
.shot-foot-title {
	display: block;
	font-size: 24rpx;
	font-weight: 600;
	color: #3a3a40;
}
.shot-foot-sub {
	display: block;
	margin-top: 4rpx;
	font-size: 21rpx;
	color: #9a9aa0;
}

.poster-bar {
	display: flex;
	gap: 16rpx;
	margin-top: 22rpx;
}
.poster-btn {
	flex: 1;
	height: 92rpx;
	border-radius: 46rpx;
	background: #ffffff;
	border: 1rpx solid #e2e2e7;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 27rpx;
	font-weight: 600;
	color: #1c1c1e;
}
.poster-btn.primary {
	background: #1c1c1e;
	border-color: #1c1c1e;
	color: #ffffff;
}
.poster-btn-share {
	padding: 0;
	margin: 0;
	line-height: normal;
	font-size: 27rpx;
}
.poster-btn-share::after { border: none; }
.poster-hint {
	margin-top: 14rpx;
	text-align: center;
	font-size: 20rpx;
	color: #a8a8ae;
}

/* 好友分享进来的结果提示 */
.friend-banner {
	margin: 8rpx 32rpx 0;
	padding: 18rpx 22rpx;
	border-radius: 18rpx;
	background: #eef5ff;
	border: 1rpx solid #d3e4fb;
	display: flex;
	align-items: center;
}
.friend-banner-text {
	flex: 1;
	min-width: 0;
	font-size: 23rpx;
	line-height: 1.5;
	color: #2b5f9e;
}
.friend-banner-close {
	width: 44rpx;
	height: 44rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 34rpx;
	color: #7fa6cf;
}
</style>
