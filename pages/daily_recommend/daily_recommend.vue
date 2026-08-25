<template>
	<view class="page-root">
		<!-- <view class="top-bar">
			<view class="back-btn" @click="goBack"><text>‹</text></view>
			<text class="title">今日宜挂</text>
			<view class="top-placeholder"></view>
		</view> -->

		<view class="step-select" v-if="currentStep === 'select'">
			<view class="barrage-stage">
				<view class="prompt">
				<text class="prompt-main">今天想做些什么？</text>
			</view>
				<view class="barrage-row" v-for="(track, ti) in barrageTracks" :key="'row'+ti">
					<view class="barrage-track" :class="{ reverse: track.reverse }">
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

			<view class="bottom-action">
				<view class="confirm-btn" :class="{ disabled: !selectedOption || usedToday >= 2 }" @click="confirmSelection">
					<text v-if="usedToday < 2">{{ selectedOption ? '确定 >' : '请先选择一个' }}</text>
					<text v-else>今日已用完</text>
				</view>
				<text class="random-tip" @click="onDebugReset">结果随机生成，仅供娱乐</text>
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
				<view class="result-actions">
					<view class="action-btn" :class="{ disabled: usedToday >= 2 }" @click="reset"><text>重新选择</text></view>
					<view class="action-btn action-primary" :class="{ disabled: usedToday >= 2 }" @click="recommendAgain">
						<text v-if="usedToday < 2">再来一次(额外)</text>
						<text v-else>今日已用完</text>
					</view>
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
						<image v-if="currentResult.avatar" :src="currentResult.avatar" mode="aspectFill" class="op-img"></image>
						<text v-else class="op-emoji">{{ currentResult.emoji || '?' }}</text>
					</view>
					<view class="op-info">
						<text class="op-name">{{ currentResult.name }}</text>
						<view class="op-tags">
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

				<view class="result-actions">
					<view class="action-btn" :class="{ disabled: usedToday >= 2 }" @click="reset"><text>重新选择</text></view>
					<view class="action-btn action-primary" :class="{ disabled: usedToday >= 2 }" @click="recommendAgain">
						<text v-if="usedToday < 2">再来一次(额外)</text>
						<text v-else>今日已用完</text>
					</view>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
export default {
	data() {
		return {
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
			debugTapCount: 0, // 调试: 连点提示文案计数
			debugTapTimer: null,

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
					theme: [{ name: '杰西卡', reason: '守序推荐：低星战神，稳稳当当不惹事。' }],
					others: [
						{ name: '星熊', reason: '万象交错的捍卫者，做事有板有眼。' },
						{ name: '诗怀雅', reason: '龙门富家千金，做事讲规矩。' },
						{ name: '陈', reason: '龙门近卫局长，铁面无私。' },
						{ name: '菲莱', reason: '守序·割韭：生生不息。' }
					],
					ban: ['W', '暮落']
				},
				hunxie: {
					theme: [
						{ name: 'W', reason: '混邪推荐：负负得正，灾厄本身即是庇护。' },
						{ name: '炎客', reason: '混邪推荐：萨卡兹佣兵，危险即正义。' }
					],
					others: [
						{ name: 'U-Official', reason: '混邪推荐：灾厄化身，负负得正。' },
						{ name: '塔露拉(愚人节单领)', reason: '混邪推荐：整合运动领袖，灾厄与火焰的化身。' }
					],
					ban: ['星熊', '陈']
				},
				laicai: {
					theme: [
						{ name: '坎诺特', reason: '来财：投资回报率拉满。' },
						{ name: '老鲤', reason: '来财：江湖老手，现金流稳定。' }
					],
					others: [
						{ name: '菲莱', reason: '来财：韭菜本韭，财富之源生生不息。' },
						{ name: '裁度', reason: '来财：裁断亏损，裁定收益。' },
						{ name: '德克萨斯', reason: '龙门企鹅物流合伙人，业务广泛。' },
						{ name: '诗怀雅', reason: '龙门富家千金，财源滚滚。' },
						{ name: '龙舌兰', reason: '酒桌财神爷，谈钱不伤感情。' }
					],
					ban: []
				},
				shuini: {
					theme: [
						{ name: '维什戴尔', reason: '水逆退散：以爆裂黎明轰碎一切霉运的萨卡兹战神。' }
					],
					others: [
						{ name: '流明', reason: '逆境中点亮光芒。' }
					],
					ban: ['赫德雷']
				},
				haoyun: {
					theme: [
						{ name: '杜林', reason: '好运常驻：小小的福气持续在线。' }
					],
					others: [
						{ name: '青枳', reason: '好运常驻：青色生机，福泽绵长。' },
						{ name: '小满', reason: '圆满吉兆。' },
					],
					ban: ['赫德雷']
				},
				jiankang: {
					theme: [
						{ name: '初雪', reason: '健康幸福：圣洁治愈，驱散病痛。' }
					],
					others: [
						{ name: '闪灵', reason: '萨卡兹天使，守护健康。' },
						{ name: '夜莺', reason: '圣光护佑，远离病痛。' }
					],
					ban: []
				},
				kaoshi_mf: {
					theme: [
						{ name: '小满', reason: '满分buff：花好月圆，诸事圆满。' }
					],
					others: [
						{ name: '令', reason: '考运加持。' },
						{ name: '斥罪', reason: '判断力满点。' }
					],
					ban: ['赫德雷', '夜魔']
				},
				kaoshi_jf: {
					theme: [
						{ name: '提丰', reason: '加分buff：精准提分，稳稳提升。' }
					],
					others: [
						{ name: '早露', reason: '晨光先照，先发制人。' },
						{ name: '云迹', reason: '云端之上抢跑。' }
					],
					ban: []
				},
				jiazhao: {
					theme: [
						{ name: '德克萨斯', reason: '驾照buff：老司机上路，一路顺风。' },
						{ name: '拜松', reason: '驾照buff：科科一把过。' },
						{ name: '芙兰卡', reason: '驾照buff：倒车入库满分。' },
						{ name: '雷蛇', reason: '驾照buff：雷霆科一。' }
					],
					others: [
						{ name: '鸿雪', reason: '侧方停车一把进。' },
						{ name: '玛恩纳', reason: '稳重驾驶，文明出行。' }
					],
					ban: ['拉普兰德', '新约能天使', '惊蛰']
				},
				shangan: {
					theme: [
						{ name: '左乐', reason: '上岸buff：考公考编稳稳上岸。' },
						{ name: '陈', reason: '上岸buff：龙门警官，一举上岸。' },
						{ name: '星熊', reason: '上岸buff：干员标杆，命运眷顾。' },
						{ name: '诗怀雅', reason: '上岸buff：龙门大小姐，offer接到手软。' },
						{ name: '银灰', reason: '上岸buff：谢拉格掌权者，offer敲门。' }
					],
					others: [
						{ name: '维娜·维多利亚', reason: '上岸buff：维多利亚之光，前途璀璨。' },
						{ name: '惊蛰', reason: '一声惊雷，岸在前头。', warn: '需慎重：惊蛰带电，请评估自身属性' }
					],
					ban: []
				},
				zhexue: {
					theme: [
						{ name: '闪灵', reason: '伦理学：天使的伦理，永恒的善。' }
					],
					others: [
						{ name: '塑心', reason: '哲学·心理：塑心即塑己，知行合一。' },
						{ name: '月禾', reason: '温柔的哲学思辨。' }
					],
					ban: []
				},
				jingji: {
					theme: [
						{ name: '诗怀雅', reason: '经济学：龙门财阀，资本的化身。' },
						{ name: '老鲤', reason: '经济学：江湖老手，现金流稳定。' },
						{ name: '锡人', reason: '经济学：投资回报率拉满。' }
					],
					others: [
						{ name: '龙舌兰', reason: '酒桌经济学大师。' },
						{ name: '银灰', reason: '谢拉格掌权者，宏观经济一盘棋。' }
					],
					ban: []
				},
				faxue: {
					theme: [
						{ name: '斥罪', reason: '法学：罪与罚的裁决者。' },
						{ name: '惊蛰', reason: '法学：惊雷一响，法庭站我这边。' }
					],
					others: [
						{ name: '但书', reason: '法学：卡西米尔法律专精，以律法凝滞纷争。' },
						{ name: '真理', reason: '法学：以真理之名，明断是非。' }
					],
					ban: []
				},
				wenxue: {
					theme: [
						{ name: '鸿雪', reason: '文学·文字：长空一字，惊艳四座。' },
						{ name: '令', reason: '文学·文字：刹那灵感，永恒佳作。' }
					],
					others: [
						{ name: '苏苏洛', reason: '文学·文字：温润笔触，治愈文风。' },
						{ name: '夕', reason: '文学·色彩诗篇。' }
					],
					ban: []
				},
				lishi: {
					theme: [
						{ name: '行箸', reason: '历史学：以史为鉴，通晓古今。' },
						{ name: '霍尔海雅', reason: '历史学：岁月沉淀，智慧留香。' },
						{ name: '赫德雷', reason: '历史学：见证过无数史诗的男人。' }
					],
					others: [
						{ name: '锡人', reason: '历史的厚重感。' }
					],
					ban: ['妮芙', '佩佩']
				},
				xinli: {
					theme: [
						{ name: '夜魔', reason: '心理学：梦境解析，潜意识。' },
						{ name: '月禾', reason: '心理学：温和抚慰心结。' },
						{ name: '蕾缪安', reason: '心理学：拉特兰枢机，跨境追缉洞悉人心。' }
					],
					others: [
						{ name: '塑心', reason: '心理学：塑心即塑己，认知重构。' },
						{ name: '提丰', reason: '精准的认知行为分析。' }
					],
					ban: []
				},
				gongxue: {
					theme: [
						{ name: '白铁', reason: '工学·电力：铁匠精神，电流掌控。' },
						{ name: '异客', reason: '工学·电力：电学的诗与远方。' },
						{ name: '森蚺', reason: '工学·机械：机械工程学大师。' },
						{ name: '白面鸮', reason: '工学·计算机：医疗数据一把抓。' },
						{ name: '云迹', reason: '工学·航空航天：云端之上，任我翱翔。' }
					],
					others: [
						{ name: '提丰', reason: '精准工程学。' }
					],
					ban: []
				},
				nongxue: {
					theme: [
						{ name: '黍', reason: '农学：岁岁丰收，五谷丰登。' },
						{ name: '万顷', reason: '农学：沃野千里。' },
						{ name: '小满', reason: '农学：小满胜万全。' },
						{ name: '空弦', reason: '农学：植物一箭开花。' }
					],
					others: [
						{ name: '初雪', reason: '春日融雪好耕种。' }
					],
					ban: []
				},
				yixue: {
					theme: [
						{ name: '凯尔希', reason: '医学·药学：医药双修，Mon3tr护体。' },
						{ name: '流明', reason: '医学·护理：护理之光。' },
						{ name: '夜莺', reason: '医学·护理：圣光护佑。' },
						{ name: '诺威尔', reason: '医学·眼视光：眼清目明。' }
					],
					others: [
						{ name: '闪灵', reason: '医学权威。' }
					],
					ban: []
				},
				guanli: {
					theme: [
						{ name: '拜松', reason: '管理学·HR：识人善任。' },
						{ name: '早露', reason: '管理学·HR：晨光先照。' },
						{ name: '龙舌兰', reason: '管理学·商务：酒桌谈判专家。' }
					],
					others: [
						{ name: '银灰', reason: '战略管理。' },
						{ name: '诗怀雅', reason: '家族企业管理。' }
					],
					ban: []
				},
				yishu: {
					theme: [
						{ name: '车尔尼', reason: '艺术学·音乐：练习曲之神。' },
						{ name: '稀音', reason: '艺术学·摄影：定格瞬间之美。' },
						{ name: '夕', reason: '艺术学·绘画：色彩诗篇。' },
						{ name: '塑心', reason: '艺术学·心理：塑心即塑艺。' }
					],
					others: [
						{ name: '令', reason: '艺术学·综合：刹那永恒。' }
					],
					ban: []
				},
				ketongshi: {
					theme: [
						{ name: 'W', reason: '克同事/领导：爆炸艺术。萨卡兹佣兵' },
						{ name: '炎客', reason: '克同事/领导：危险。' },
						{ name: '暮落', reason: '克同事/领导：黑帮教父。' },
						{ name: 'U-Official', reason: '克同事/领导：灾厄压迫，敬畏。' },
						{ name: '塔露拉(愚人节单领)', reason: '克同事/领导：整合运动领袖气场。' }
					],
					others: [
						{ name: '青枳', reason: '克同事/领导：青出于蓝，气场压制。' },
						{ name: '异客', reason: '电流让你冷静。' },
						{ name: '惊蛰', reason: '一声惊雷镇场。' }
					],
					ban: ['星熊', '左乐', '陈']
				},
				zhichang: {
					theme: [
						{ name: '玛恩纳', reason: '职场buff·反卷：临光家前家主，绝不内卷。' },
						{ name: '银灰', reason: '职场buff·利公司：谢拉格掌权者。' },
						{ name: '掠风', reason: '职场buff·找工作：风一样的offer。' }
					],
					others: [
						{ name: '诗怀雅', reason: '职场高起点。' }
					],
					ban: []
				},
				zhiye: {
					theme: [
						{ name: '澄闪', reason: '职业·美食：甜品大师。' },
						{ name: '玛恩纳', reason: '职业·社畜：临光家前家主，打工魂。' },
						{ name: '锡人', reason: '职业·社畜：上班不累。' }
					],
					others: [
						{ name: '菲莱', reason: '职业·市场：韭皇归来。' },
						{ name: '裁度', reason: '职业·律师：裁断之道。' },
						{ name: '老鲤', reason: '职业·会计：及时行乐,自然就会万事大吉。', warn: '不宜：会计专（专精会计者慎挂）' },
						{ name: '拜松', reason: '职业·烘焙：面包香气。' }
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
		}
	},
	onLoad() {
		let n = 0;
		this.dotsTimer = setInterval(() => {
			n = (n + 1) % 4;
			this.loadingDots = '.'.repeat(n);
		}, 400);
		this.loadDailyState();
		this.loadBoxesData();
		
		
		// 设置分享配置
		uni.showShareMenu({
		    withShareTicket: true,
		    menus: ['shareAppMessage', 'shareTimeline']
		});
		
		
	},
	// 分享给好友
	onShareAppMessage() {
	    return {
	        title: '来看看你今天适合挂什么明日方舟通行证? -方舟通行证谷子查询工具',
	        path: '/pages/daily_recommend/daily_recommend',
	        imageUrl: ''
	    }
	},
	
	// 分享到朋友圈
	onShareTimeline() {
	    return {
	        title: '来看看你今天适合挂什么明日方舟通行证? -方舟通行证谷子查询工具',
	        imageUrl: ''
	    }
	},
	onUnload() {
		if (this.dotsTimer) clearInterval(this.dotsTimer);
		if (this.loadingTimer) clearTimeout(this.loadingTimer);
	},
	methods: {
		logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},
		goBack() { uni.navigateBack(); },

		getTodayKey() {
			const d = new Date();
			return d.getFullYear() + '-' + (d.getMonth()+1) + '-' + d.getDate();
		},

		loadDailyState() {
			try {
				const rec = uni.getStorageSync('dailyRecState') || {};
				const today = this.getTodayKey();
				if (rec.date === today) {
					this.todayDate = rec.date;
					this.usedToday = rec.usedToday || 0;
					this.extraUsed = !!rec.extraUsed;
					if (this.usedToday >= 2) {
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
		},

		consumeDraw() {
			this.usedToday++;
			if (this.usedToday >= 2) this.extraUsed = true;
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

		// 调试: 2 秒内连点 10 次「结果随机生成，仅供娱乐」重置今日次数
		onDebugReset() {
			this.debugTapCount++;
			if (this.debugTapCount >= 10) {
				this.debugTapCount = 0;
				if (this.debugTapTimer) { clearTimeout(this.debugTapTimer); this.debugTapTimer = null; }
				try { uni.removeStorageSync('dailyRecState'); } catch (e) {
					this.logError(e);
					
				}
				this.usedToday = 0;
				this.extraUsed = false;
				this.selectedOption = '';
				this.currentResult = null;
				this.luckyBoost = 0;
				this.currentStep = 'select';
				uni.showToast({ title: '已重置今日次数', icon: 'none' });
				return;
			}
			if (this.debugTapTimer) clearTimeout(this.debugTapTimer);
			this.debugTapTimer = setTimeout(() => { this.debugTapCount = 0; }, 2000);
		},

		selectOption(key) { this.selectedOption = key; },

		confirmSelection() {
			if (!this.selectedOption) {
				uni.showToast({ title: '请先选择一个', icon: 'none' });
				return;
			}
			if (this.usedToday >= 2) {
				uni.showToast({ title: '今日次数已用完', icon: 'none' });
				return;
			}
			this.currentStep = 'loading';
			const delay = 1600 + Math.random() * 800;
			this.loadingTimer = setTimeout(() => this.doRecommend(), delay);
		},

		async doRecommend() {
			if (this.usedToday >= 2) {
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
							avatar: ch.imageUrl || '',
							hot: !!ch.hotcharacter
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
			const tags = ['精一', '精二'];
			const tag = tags[Math.floor(Math.random() * tags.length)];

			return {
				...cand,
				avatar: chosen.avatar,
				box_id: chosen.box_id,
				tag: tag,
				hot: chosen.hot
			};
		},

		recommendAgain() {
			if (this.usedToday >= 2) {
				uni.showToast({ title: '今日已用完额外 1 次', icon: 'none' });
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
}
.barrage-track {
	display: inline-flex;
	gap: 16rpx;
	padding-left: 32rpx;
	animation: scrollBarrage 24s linear infinite;
}
.barrage-track.reverse {
	animation-direction: reverse;
	animation-duration: 30s;
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

@keyframes scrollBarrage {
	from { transform: translateX(0); }
	to { transform: translateX(-50%); }
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
</style>
