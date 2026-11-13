<template>
	<view class="ef-page calendar-page">
		<!-- 视图切换：日历 / 时间线 -->
		<view class="view-switch">
			<view class="switch-item" :class="{ active: viewMode === 'calendar' }" @click="switchView('calendar')">
				<text class="switch-text">日历</text>
			</view>
			<view class="switch-item" :class="{ active: viewMode === 'timeline' }" @click="switchView('timeline')">
				<text class="switch-text">时间线</text>
			</view>
		</view>

		<!-- 月份选择（时间线视图） -->
		<view class="month-selector" v-if="viewMode === 'timeline'">
			<view class="month-arrow" hover-class="month-arrow-hover" @click="prevMonth">
				<view class="chev chev-left"></view>
			</view>
			<view class="month-text" @click="showMonthPicker">{{ currentYear }}年{{ currentMonth }}月</view>
			<view class="month-arrow" hover-class="month-arrow-hover" @click="nextMonth">
				<view class="chev chev-right"></view>
			</view>
		</view>

		<!-- 日历面板（日历视图，uni-calendar） -->
		<view class="calendar-panel" v-else>
			<uni-calendar
				:insert="true"
				:lunar="false"
				:show-month="false"
				:selected="calendarMarks"
				:date="calendarSyncDate"
				@change="onCalendarChange"
				@monthSwitch="onCalendarMonthSwitch"
			></uni-calendar>
		</view>

		<!-- 统计 -->
		<view class="calendar-stats">
			<view class="stat-item">
				<text class="stat-num">{{ monthBoxes.length }}</text>
				<text class="stat-label">本月盒子</text>
			</view>
			<view class="stat-item">
				<text class="stat-num">{{ monthCharacters }}</text>
				<text class="stat-label">本月角色</text>
			</view>
			<view class="stat-item">
				<text class="stat-num">{{ monthOwned }}</text>
				<text class="stat-label">已收藏</text>
			</view>
		</view>

		<!-- 列表：时间线视图=当月全部；日历视图=选中日期当天 -->
		<view class="timeline" v-if="displayBoxes.length">
			<view class="timeline-item" v-for="(box, index) in displayBoxes" :key="index" @click="goToList(box)">
				<view class="timeline-card">
					<view class="card-header">
						<text class="card-name">白名单凭证 {{ box.Box_id }}</text>
						<text class="card-tag">{{ box._tag }}</text>
					</view>
					<text class="card-date">{{ box._dateText }} · {{ getBoxTypeText(box.Box_type) }}</text>
					<view class="card-char-tags">
						<text class="char-tag" v-for="(ch, ci) in getBoxCharacters(box)" :key="ci">{{ ch.name }}</text>
					</view>
				</view>
			</view>
		</view>
		<view class="empty-state" v-else>
			<text class="empty-text">{{ emptyText }}</text>
			<button class="reload-btn" v-if="viewMode === 'timeline'" @click="loadData">重新加载数据</button>
		</view>
	</view>
</template>

<script>
// AE 专用日历：缓存键全部 ae_ 前缀
const AE_DATA_KEYS = ['ae_arknightsData', 'ae_currentData', 'ae_githubCharacters'];

import uniCalendar from '@/uni_modules/uni-calendar/components/uni-calendar/uni-calendar.vue';

export default {
	components: { uniCalendar },
	data() {
		return {
			currentYear: new Date().getFullYear(),
			currentMonth: new Date().getMonth() + 1,
			allBoxes: [],
			favoriteCharacterNames: [],
			ownedCharacters: {},
			// 视图模式：calendar（日历样式）/ timeline（时间线样式）
			viewMode: 'calendar',
			// 日历视图选中日期（YYYY-MM-DD）
			selectedDate: '',
			// 传给 uni-calendar 的受控日期，用于切回日历视图时同步月份
			calendarSyncDate: ''
		};
	},
	computed: {
		monthBoxes() {
			const y = this.currentYear;
			const m = this.currentMonth;
			return this.allBoxes.filter(box => {
				const d1 = this.parseDate(box.release_date);
				const d2 = this.parseDate(box.replicate_date);
				const d3 = this.parseDate(box.replicate_date_01);
				return this.inMonth(d1, y, m) || this.inMonth(d2, y, m) || this.inMonth(d3, y, m);
			});
		},
		monthCharacters() {
			return this.monthBoxes.reduce((sum, b) => sum + this.getBoxCharacters(b).length, 0);
		},
		monthOwned() {
			const set = new Set(this.favoriteCharacterNames);
			let n = 0;
			this.monthBoxes.forEach(b => {
				this.getBoxCharacters(b).forEach(c => {
					if (set.has(c.name) || this.ownedCharacters[c.name]) n++;
				});
			});
			return n;
		}
		,
		// 日历打点：所有盒子的发售 + 复刻日期
		calendarMarks() {
			const map = {};
			this.allBoxes.forEach(box => {
				this.boxDateList(box).forEach(d => {
					if (!map[d.key]) map[d.key] = { date: d.key, info: '' };
				});
			});
			return Object.keys(map).map(k => map[k]);
		},
		// 当前列表数据：时间线=当月全部；日历=选中日期当天
		displayBoxes() {
			if (this.viewMode === 'timeline') {
				return this.monthBoxes.map(box => Object.assign({}, box, {
					_tag: this.getMonthTag(box),
					_dateText: this.getMonthDate(box)
				}));
			}
			const target = this.selectedDate;
			if (!target) return [];
			const list = [];
			this.allBoxes.forEach(box => {
				const hit = this.boxDateList(box).find(d => d.key === target);
				if (hit) {
					list.push(Object.assign({}, box, {
						_tag: this.dateFieldLabel(hit.field),
						_dateText: hit.text
					}));
				}
			});
			return list.sort((a, b) => String(a._dateText).localeCompare(String(b._dateText)));
		},
		// 空状态文案
		emptyText() {
			if (this.viewMode === 'timeline') return '本月暂无发售 / 复刻记录';
			if (!this.selectedDate) return '请选择日期查看当天的记录';
			const total = this.monthBoxes.length;
			return total ? '这一天没有记录，本月共 ' + total + ' 个' : '这一天没有记录';
		}
	},
	onLoad() {
		this.loadData();
		const now = new Date();
		this.selectedDate = this.toKey(now);
		this.calendarSyncDate = this.toKey(now).slice(0, 8) + '01';
	},
	onShow() {
		try {
			this.favoriteCharacterNames = uni.getStorageSync('ae_favoriteCharacterNames') || [];
			this.ownedCharacters = uni.getStorageSync('ae_ownedCharacters') || {};
		} catch (e) { /* ignore */ }
	},
	methods: {
		loadData() {
			let data = [];
			for (const key of AE_DATA_KEYS) {
				try {
					const d = uni.getStorageSync(key);
					if (Array.isArray(d) && d.length) {
						data = d;
						break;
					}
				} catch (e) { /* ignore */ }
			}
			this.allBoxes = data || [];
			if (!this.allBoxes.length) {
				uni.showToast({ title: '请先在首页下载终末地数据', icon: 'none' });
			}
		},
		// ===== 日期工具 =====
		pad2(n) {
			const v = Number(n) || 0;
			return v < 10 ? '0' + v : '' + v;
		},
		toKey(d) {
			return d.getFullYear() + '-' + this.pad2(d.getMonth() + 1) + '-' + this.pad2(d.getDate());
		},
		// 盒子的全部日期（发售 + 两次复刻）
		boxDateList(box) {
			const fields = ['release_date', 'replicate_date', 'replicate_date_01'];
			const list = [];
			fields.forEach(field => {
				const d = this.parseDate(box[field]);
				if (d) {
					const key = this.toKey(d);
					list.push({ field: field, text: key.replace(/-/g, '/'), key: key });
				}
			});
			return list;
		},
		dateFieldLabel(field) {
			if (field === 'release_date') return '发售';
			if (field === 'replicate_date') return '复刻';
			if (field === 'replicate_date_01') return '再复刻';
			return '';
		},

		// ===== 视图切换与日历联动 =====
		switchView(mode) {
			if (mode === this.viewMode) return;
			this.viewMode = mode;
			if (mode === 'calendar') {
				const now = new Date();
				const d = (this.currentYear || now.getFullYear()) + '-' + this.pad2(this.currentMonth || (now.getMonth() + 1)) + '-01';
				if (d !== this.calendarSyncDate) this.calendarSyncDate = d;
			}
		},
		onCalendarChange(e) {
			const fd = String((e && e.fulldate) || '');
			const parts = fd.split('-');
			if (parts.length === 3) {
				this.selectedDate = parts[0] + '-' + this.pad2(parts[1]) + '-' + this.pad2(parts[2]);
			}
		},
		onCalendarMonthSwitch(e) {
			if (!e || !e.year) return;
			this.currentYear = Number(e.year);
			this.currentMonth = Number(e.month);
			// 翻月后清空选中日期，避免列表仍停留在上个月某一天
			this.selectedDate = '';
		},

		inMonth(d, y, m) {
			return !!d && d.getFullYear() === y && (d.getMonth() + 1) === m;
		},
		parseDate(str) {
			if (!str) return null;
			const s = String(str).replace(/-/g, '/');
			const d = new Date(s);
			return isNaN(d.getTime()) ? null : d;
		},
		prevMonth() {
			if (this.currentMonth === 1) {
				this.currentMonth = 12;
				this.currentYear--;
			} else this.currentMonth--;
		},
		nextMonth() {
			if (this.currentMonth === 12) {
				this.currentMonth = 1;
				this.currentYear++;
			} else this.currentMonth++;
		},
		showMonthPicker() {
			const months = [];
			for (let i = 1; i <= 12; i++) months.push(i + '月');
			uni.showActionSheet({
				itemList: months,
				success: (res) => { this.currentMonth = res.tapIndex + 1; }
			});
		},
		getMonthTag(box) {
			if (this.inMonth(this.parseDate(box.release_date), this.currentYear, this.currentMonth)) return '发售';
			if (this.inMonth(this.parseDate(box.replicate_date), this.currentYear, this.currentMonth)) return '复刻';
			if (this.inMonth(this.parseDate(box.replicate_date_01), this.currentYear, this.currentMonth)) return '再复刻';
			return '';
		},
		getMonthDate(box) {
			const y = this.currentYear, m = this.currentMonth;
			const candidates = [box.release_date, box.replicate_date, box.replicate_date_01];
			for (const c of candidates) {
				const d = this.parseDate(c);
				if (this.inMonth(d, y, m)) {
					return `${d.getFullYear()}/${String(d.getMonth() + 1).padStart(2, '0')}/${String(d.getDate()).padStart(2, '0')}`;
				}
			}
			return '';
		},
		getBoxTypeText(t) {
			const map = {
				whitelist: '白名单凭证',
				special: '特别通行认证',
				cooperation: '联动款',
				ambience: '音律联觉',
				normal: '常规款'
			};
			return map[t] || '白名单凭证';
		},
		getBoxCharacters(box) {
			const list = [];
			for (let i = 1; i <= 12; i++) {
				const ch = box['character' + i];
				if (!ch) continue;
				const name = typeof ch === 'string' ? ch : (ch.name || '');
				if (name && name.trim()) list.push({ name });
			}
			return list;
		},
		goToList(box) {
			try { uni.setStorageSync('ae_calendarSearchBoxId', box.Box_id); } catch (e) { /* ignore */ }
			uni.navigateTo({ url: '/package_ArknightEnds/list/list?boxId=' + encodeURIComponent(box.Box_id || '') });
		}
	}
};
</script>

<style lang="scss" scoped>
/* endspace: #fafafa / #fff / #e4e4e7 / #18181b / #FBFB45 */
.calendar-page {
	min-height: 100vh;
	background: #fafafa;
	color: #18181b;
	padding: 20rpx 24rpx 60rpx;
	box-sizing: border-box;
	position: relative;
	z-index: 1;
}
.calendar-page::before {
	content: '';
	position: fixed;
	inset: 0;
	pointer-events: none;
	z-index: 0;
	background-image: 
		linear-gradient(rgba(0,0,0,0.03) 1px, transparent 1px),
		linear-gradient(90deg, rgba(0,0,0,0.03) 1px, transparent 1px);
	background-size: 40px 40px;
}

.month-selector {
	position: relative;
	z-index: 1;
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 8rpx 16rpx;
	background: #ffffff;
	border: 1rpx solid #e4e4e7;
	border-radius: 12rpx;
	box-shadow: 0 1px 2px rgba(0,0,0,.04), 0 2px 4px rgba(0,0,0,.04);
	margin-bottom: 16rpx;
}
/* 翻页箭头：纯 CSS 绘制（不使用三角字符，iOS 会渲染成 emoji） */
.month-arrow {
	width: 72rpx;
	height: 72rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 50%;
}
.month-arrow-hover {
	background: #f4f4f5;
}
.chev {
	width: 16rpx;
	height: 16rpx;
	border-left: 3rpx solid #52525b;
	border-bottom: 3rpx solid #52525b;
	border-radius: 2rpx;
}
.chev-left {
	transform: rotate(45deg);
	margin-left: 5rpx;
}
.chev-right {
	transform: rotate(-135deg);
	margin-right: 5rpx;
}
.month-text {
	color: #18181b;
	font-size: 30rpx;
	font-weight: 700;
	padding: 8rpx 4rpx;
}

.calendar-stats {
	position: relative;
	z-index: 1;
	display: flex;
	gap: 12rpx;
	margin-bottom: 24rpx;
}
.stat-item {
	flex: 1;
	padding: 24rpx 12rpx;
	background: #ffffff;
	border: 1rpx solid #e4e4e7;
	border-radius: 12rpx;
	box-shadow: 0 1px 2px rgba(0,0,0,.04);
	text-align: center;
	display: flex;
	flex-direction: column;
	gap: 8rpx;
}
.stat-num {
	color: #18181b;
	font-size: 40rpx;
	font-weight: 800;
	line-height: 1.1;
}
.stat-label {
	color: #a1a1aa;
	font-size: 20rpx;
	font-weight: 500;
}

.timeline {
	position: relative;
	z-index: 1;
	display: flex;
	flex-direction: column;
	gap: 12rpx;
}
.timeline-item { width: 100%; }
.timeline-card {
	padding: 24rpx;
	background: #ffffff;
	border: 1rpx solid #e4e4e7;
	border-radius: 12rpx;
	box-shadow: 0 1px 2px rgba(0,0,0,.04), 0 2px 4px rgba(0,0,0,.04);
}
.timeline-card:active {
	border-color: #FBFB45;
	box-shadow: 0 4px 8px rgba(0,0,0,.08), 0 0 0 1rpx #FBFB45;
}
.card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 10rpx;
	gap: 12rpx;
}
.card-name {
	color: #18181b;
	font-size: 28rpx;
	font-weight: 700;
	flex: 1;
	min-width: 0;
}
.card-tag {
	flex-shrink: 0;
	font-size: 20rpx;
	font-weight: 700;
	padding: 4rpx 14rpx;
	border-radius: 6rpx;
	background: #FBFB45;
	color: #000;
	border: 1rpx solid #000;
}
.card-date {
	display: block;
	color: #a1a1aa;
	font-size: 22rpx;
	margin-bottom: 14rpx;
	font-weight: 500;
}
.card-char-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 8rpx;
}
.char-tag {
	font-size: 20rpx;
	color: #52525b;
	background: #f4f4f5;
	border: 1rpx solid #e4e4e7;
	padding: 6rpx 14rpx;
	border-radius: 6rpx;
	font-weight: 500;
}

.empty-state {
	position: relative;
	z-index: 1;
	padding: 80rpx 20rpx;
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 24rpx;
}
.empty-text {
	color: #a1a1aa;
	font-size: 26rpx;
}
.reload-btn {
	background: #ffffff;
	color: #18181b;
	border: 1rpx solid #e4e4e7;
	border-radius: 10rpx;
	font-size: 24rpx;
	font-weight: 600;
	padding: 14rpx 32rpx;
	line-height: 1.4;
	min-height: auto;
}
.reload-btn::after { border: none; }

/* ================= 视图切换（日历 / 时间线） ================= */
.view-switch {
	position: relative;
	z-index: 1;
	display: flex;
	gap: 8rpx;
	padding: 6rpx;
	background: #ffffff;
	border: 1rpx solid #e4e4e7;
	border-radius: 12rpx;
	margin-bottom: 16rpx;
}
.switch-item {
	flex: 1;
	height: 68rpx;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 8rpx;
	border: 1rpx solid transparent;
}
.switch-item.active {
	background: #FBFB45;
	border-color: #000000;
}
.switch-text {
	font-size: 26rpx;
	color: #52525b;
	font-weight: 600;
}
.switch-item.active .switch-text {
	color: #000000;
	font-weight: 700;
}

/* ================= 日历面板（uni-calendar 主题变量，终末地白底风格） ================= */
.calendar-panel {
	position: relative;
	z-index: 1;
	background: #ffffff;
	border: 1rpx solid #e4e4e7;
	border-radius: 12rpx;
	padding: 8rpx 12rpx 16rpx;
	margin-bottom: 16rpx;
	overflow: hidden;
	box-shadow: 0 1px 2px rgba(0,0,0,.04), 0 2px 4px rgba(0,0,0,.04);

	--uc-bg: #ffffff;
	--uc-text: #18181b;
	--uc-sub: #a1a1aa;
	--uc-border: #e4e4e7;
	--uc-border-soft: #f4f4f5;
	--uc-primary: #18181b;
	--uc-on-primary: #FBFB45;
	--uc-today-bg: rgba(251, 251, 69, 0.5);
	--uc-dot: #18181b;
	--uc-disabled: #d4d4d8;
	--uc-hover-bg: #f4f4f5;
	--uc-watermark: #f4f4f5;
}
</style>
