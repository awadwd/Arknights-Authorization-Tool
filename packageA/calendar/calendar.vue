<template>
	<view class="container calendar-container" :class="'theme-' + themeMode">
		<!-- 视图切换：只在窄屏出现；宽屏下左右两栏同时展示，无需切换 -->
		<view class="view-switch" v-if="!wideLayout">
			<view class="switch-item" :class="{ active: viewMode === 'calendar' }" @click="switchView('calendar')">
				<text class="switch-text">日历</text>
			</view>
			<view class="switch-item" :class="{ active: viewMode === 'timeline' }" @click="switchView('timeline')">
				<text class="switch-text">时间线</text>
			</view>
		</view>

		<!-- 统计概览（两栏共用） -->
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
				<text class="stat-label">已收集</text>
			</view>
		</view>

		<!-- 宽屏：左「日历 + 当天盒子」/ 右「月份 + 当月时间线」同时展示；窄屏只渲染当前视图 -->
		<view class="calendar-split" :class="{ 'is-wide': wideLayout }">
			<view
				class="calendar-col"
				:class="'col-' + pane"
				v-for="pane in visiblePanes"
				:key="pane"
			>
				<!-- 月份选择（时间线栏） -->
				<view class="month-selector" v-if="pane === 'timeline'">
					<view class="month-arrow" hover-class="month-arrow-hover" @click="prevMonth">
						<view class="chev chev-left"></view>
					</view>
					<view class="month-text" @click="showMonthPicker">
						<text class="month-text-label">{{ currentYear }}年{{ currentMonth }}月</text>
						<view class="caret-down"></view>
					</view>
					<view class="month-arrow" hover-class="month-arrow-hover" @click="nextMonth">
						<view class="chev chev-right"></view>
					</view>
				</view>

				<!-- 日历面板（日历栏，uni-calendar） -->
				<view class="calendar-panel" v-if="pane === 'calendar'">
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

				<!-- 盒子列表：日历栏=选中日期当天；时间线栏=当月全部 -->
				<view class="timeline" :class="{ 'timeline-flat': pane === 'calendar' }">
					<view class="timeline-item" v-for="(box, index) in paneData[pane].boxes" :key="index" @click="goToBox(box)">
						<block v-if="pane === 'timeline'">
							<view class="timeline-dot" :class="'dot-' + getBoxTypeClass(box.Box_type)"></view>
							<view class="timeline-line" v-if="index < paneData[pane].boxes.length - 1"></view>
						</block>
						<view class="timeline-card">
							<view class="card-header">
								<view class="card-title-row">
									<text class="card-name">{{ box.Box_id || box.box_id }}</text>
									<text class="card-action-tag">{{ box._tag }}</text>
								</view>
								<text class="card-date">{{ box._dateText }}</text>
							</view>
							<view class="card-subheader">
								<text class="card-type-tag" :class="'tag-' + getBoxTypeClass(box.Box_type)">{{ getBoxTypeText(box.Box_type) }}</text>
							</view>
							<view class="card-body">
								<text class="card-characters">包含 {{ getBoxCharacters(box).length }} 个角色</text>
								<!-- 角色名字标签 -->
								<view class="card-char-tags" v-if="getBoxCharacters(box).length > 0">
									<text
										class="char-tag"
										v-for="(char, ci) in getBoxCharacters(box)"
										:key="ci"
									>{{ char.name }}</text>
								</view>
								<view class="card-replicate">
									<text class="card-replicate-label">复刻记录</text>
									<view class="card-replicate-list" v-if="getReplicateRecords(box).length">
										<text class="card-replicate-line" v-for="(rec, ri) in getReplicateRecords(box)" :key="ri">{{ rec }}</text>
									</view>
									<text class="card-replicate-empty" v-else>暂无记录</text>
								</view>
								<view class="card-progress">
									<view class="mini-progress-bar">
										<view class="mini-progress-fill" :style="{ width: getBoxRate(box) + '%' }"></view>
									</view>
									<text class="card-progress-text">{{ getBoxOwnedCount(box) }}/{{ getBoxCharacters(box).length }}</text>
								</view>
							</view>
						</view>
					</view>

					<view class="empty-month" v-if="paneData[pane].boxes.length === 0">
						<view class="empty-mark">
							<view class="empty-mark-line"></view>
						</view>
						<text class="empty-text">{{ paneData[pane].empty }}</text>
					</view>
				</view>
			</view>
		</view>

		<!-- 图例 -->
		<view class="legend">
			<text class="legend-title">图例</text>
			<view class="legend-items">
				<view class="legend-item">
					<view class="legend-dot dot-normal"></view>
					<text>常规款</text>
				</view>
				<view class="legend-item">
					<view class="legend-dot dot-whitelist"></view>
					<text>白名单</text>
				</view>
				<view class="legend-item">
					<view class="legend-dot dot-special"></view>
					<text>特别通行</text>
				</view>
				<view class="legend-item">
					<view class="legend-dot dot-cooperation"></view>
					<text>联动款</text>
				</view>
				<view class="legend-item">
					<view class="legend-dot dot-ambience"></view>
					<text>音律联觉</text>
				</view>
			</view>
		</view>

		<!-- 月份快捷选择弹窗 -->
		<view class="month-picker-mask" v-if="showMonthPickerFlag" @click="closeMonthPicker">
			<view class="month-picker-sheet" @click.stop>
				<view class="picker-header">
					<text class="picker-title">选择年月</text>
					<view class="picker-close" @click="closeMonthPicker">
					<view class="x-line x-line-a"></view>
					<view class="x-line x-line-b"></view>
				</view>
				</view>
				<scroll-view class="picker-list" scroll-y>
					<view
						class="picker-item"
						:class="{ active: item.year === currentYear && item.month === currentMonth }"
						v-for="item in availableMonths"
						:key="item.year + '-' + item.month"
						@click="selectMonth(item.year, item.month)"
					>
						<text>{{ item.year }}年{{ item.month }}月</text>
						<text class="picker-count">{{ item.count }}个盒子</text>
					</view>
				</scroll-view>
			</view>
		</view>
		<ak-music-bar />
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
import { markDailyTask } from "@/utils/dailyTask.js";
import uniCalendar from '@/uni_modules/uni-calendar/components/uni-calendar/uni-calendar.vue';
import wideLayout from "@/utils/wideLayout.js";

export default {
	mixins: [wideLayout],
	components: { uniCalendar },
	data() {
		return {
			// 主题模式
			themeMode: 'simple',
			boxes: [],
			ownedCharacters: {},
			currentYear: 2025,
			currentMonth: 1,
			minMonth: { year: 2024, month: 1 },
			maxMonth: { year: 2026, month: 12 },
			showMonthPickerFlag: false,
			// 视图模式：calendar（日历样式）/ timeline（时间线样式）
			viewMode: 'calendar',
			// 日历视图选中的日期（YYYY-MM-DD）
			selectedDate: '',
			// 传给 uni-calendar 的受控日期，用于切回日历视图时同步月份
			calendarSyncDate: ''
		};
	},
	computed: {
		monthBoxes() {
			return this.boxes.filter(box => {
				const months = this.getBoxMonths(box);
				return months.some(m => m.year === this.currentYear && m.month === this.currentMonth);
			});
		},
		monthCharacters() {
			return this.monthBoxes.reduce((sum, box) => sum + (box.characters || []).length, 0);
		},
		monthOwned() {
			return this.monthBoxes.reduce((sum, box) => {
				return sum + (box.characters || []).filter(c => c && c.name && this.ownedCharacters[c.name]).length;
			}, 0);
		},
		// 日历打点：所有盒子的上线 + 复刻日期
		calendarMarks() {
			const map = {};
			this.boxes.forEach(box => {
				this.boxDates(box).forEach(d => {
					if (!map[d.key]) map[d.key] = { date: d.key, info: '' };
				});
			});
			return Object.keys(map).map(k => map[k]);
		},
		// 宽屏：左右两栏同时展示；窄屏：只展示当前视图
		visiblePanes() {
			return this.wideLayout ? ['calendar', 'timeline'] : [this.viewMode];
		},
		// 每一栏自己的数据（互不影响）
		paneData() {
			const map = {};
			this.visiblePanes.forEach(mode => {
				map[mode] = { boxes: this.boxesFor(mode), empty: this.emptyTextFor(mode) };
			});
			return map;
		},
		// 收集所有有数据的年月，按最新在前排列
		availableMonths() {
			const map = {};
			this.boxes.forEach(box => {
				const months = this.getBoxMonths(box);
				months.forEach(m => {
					const key = m.year + '-' + m.month;
					if (!map[key]) {
						map[key] = { year: m.year, month: m.month, count: 0 };
					}
					map[key].count++;
				});
			});
			return Object.values(map).sort((a, b) => {
				if (a.year !== b.year) return b.year - a.year;
				return b.month - a.month;
			});
		}
	},
	onLoad() {
		this.syncWideLayout();
		// 每日任务打点
		markDailyTask('view_calendar');
		this.loadData();
		const now = new Date();
		const y = now.getFullYear();
		const m = now.getMonth() + 1;
		this.selectedDate = y + '-' + this.pad2(m) + '-' + this.pad2(now.getDate());
		this.calendarSyncDate = y + '-' + this.pad2(m) + '-01';
	},
	onShow() {
		this.loadThemeSetting();
		this.ownedCharacters = uni.getStorageSync('ownedCharacters') || {};
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
		loadData() {
			// 检查多个数据存储位置（和 index.vue、list.vue 一致）
			const data1 = uni.getStorageSync('arknightsData');
			const data2 = uni.getStorageSync('githubCharacters');
			const data3 = uni.getStorageSync('currentData');
			
			let rawData = [];
			if (data1 && Array.isArray(data1) && data1.length > 0) {
				rawData = data1;
			} else if (data3 && Array.isArray(data3) && data3.length > 0) {
				rawData = data3;
			} else if (data2 && Array.isArray(data2) && data2.length > 0) {
				rawData = data2;
			}
			
			if (!rawData.length) {
				uni.showToast({ title: '暂无通行证数据', icon: 'none' });
				return;
			}
			// 把 character1/character2/... 规范化为 characters 数组
			const data = rawData.map(box => {
				const chars = [];
				for (let i = 1; i <= 20; i++) {
					const key = 'character' + i;
					if (box[key]) chars.push(box[key]);
				}
				return { ...box, characters: chars };
			});
			this.boxes = data;
			// 默认显示当前年月（而非跳转到最新盒子）
			const now = new Date();
			this.currentYear = now.getFullYear();
			this.currentMonth = now.getMonth() + 1;
		},

		// ===== 日期工具 =====
		pad2(n) {
			const v = Number(n) || 0;
			return v < 10 ? '0' + v : '' + v;
		},

		parseDateParts(str) {
			const m = String(str || '').match(/^(\d{4})\/(\d{1,2})\/(\d{1,2})/);
			return m ? { y: Number(m[1]), m: Number(m[2]), d: Number(m[3]) } : null;
		},

		dateKeyOf(parts) {
			return parts.y + '-' + this.pad2(parts.m) + '-' + this.pad2(parts.d);
		},

		// 盒子的全部日期（上线 + 每次复刻）
		boxDates(box) {
			const fields = ['release_date', 'replicate_date', 'replicate_date_01', 'replicate_date_02', 'replicate_date_03', 'replicate_date_04', 'replicate_date_05'];
			const list = [];
			fields.forEach(field => {
				if (!box || !box[field]) return;
				const parts = this.parseDateParts(box[field]);
				if (parts) list.push({ field: field, text: box[field], parts: parts, key: this.dateKeyOf(parts) });
			});
			return list;
		},

		// 日期字段 -> 中文标签
		dateFieldLabel(field) {
			if (field === 'release_date') return '上线';
			if (field === 'replicate_date') return '第一次复刻';
			const m = String(field).match(/^replicate_date_(\d+)$/);
			if (m) return '第' + this.toChineseOrdinal(Number(m[1]) + 1) + '次复刻';
			return '上线';
		},

		// 指定视图的盒子列表：时间线=当月全部；日历=选中日期当天
		boxesFor(mode) {
			if (mode === 'timeline') {
				return this.monthBoxes.map(box => Object.assign({}, box, {
					_tag: this.getMonthTag(box),
					_dateText: this.getMonthDate(box)
				}));
			}
			const target = this.selectedDate;
			if (!target) return [];
			const list = [];
			this.boxes.forEach(box => {
				const hit = this.boxDates(box).find(d => d.key === target);
				if (hit) {
					list.push(Object.assign({}, box, {
						_tag: this.dateFieldLabel(hit.field),
						_dateText: hit.text
					}));
				}
			});
			return list.sort((a, b) => String(a._dateText).localeCompare(String(b._dateText)));
		},
		// 指定视图的空状态文案
		emptyTextFor(mode) {
			if (mode === 'timeline') return '该月没有通行证盒子';
			if (!this.selectedDate) return '请选择日期查看当天的通行证盒子';
			const total = this.monthBoxes.length;
			return total ? '这一天没有通行证盒子，本月共 ' + total + ' 个' : '这一天没有通行证盒子';
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

		getBoxMonth(box) {
			// 复刻盒子使用 replicate_date，普通盒子使用 release_date
			const dateStr = box.replicate === 'true' ? (box.replicate_date || '') : (box.release_date || '');
			// 从日期字符串解析年月，格式如 "2025/07/15" 或 "2025/7/15"
			const match = dateStr.match(/^(\d{4})\/(\d{1,2})/);
			if (match) {
				return { year: parseInt(match[1]), month: parseInt(match[2]) };
			}
			// 无日期的盒子，返回一个特殊标记（不要用 fallback 计算错误日期）
			return { year: null, month: null };
		},

		prevMonth() {
			this.currentMonth--;
			if (this.currentMonth < 1) {
				this.currentMonth = 12;
				this.currentYear--;
			}
		},

		nextMonth() {
			this.currentMonth++;
			if (this.currentMonth > 12) {
				this.currentMonth = 1;
				this.currentYear++;
			}
		},

		showMonthPicker() {
			this.showMonthPickerFlag = true;
		},

		closeMonthPicker() {
			this.showMonthPickerFlag = false;
		},

		selectMonth(year, month) {
			this.currentYear = year;
			this.currentMonth = month;
			this.showMonthPickerFlag = false;
		},

		getBoxTypeText(type) {
			const map = {
				'whitelist': '白名单',
				'special': '特别通行',
				'cooperation': '联动款',
				'ambience': '音律联觉',
				'normal': '常规款'
			};
			return map[type] || '常规款';
		},

		getBoxTypeClass(type) {
			return type || 'normal';
		},

		// 获取盒子所有年月（上线日期+复刻日期），用于年月选择器和月份过滤
		// 获取复刻记录列表
		getReplicateRecords(box) {
			if (!box) return [];
			const records = [];
			if (box.replicate_date) {
				records.push(box.replicate_date + ' 第一次复刻');
			}
			const extras = ['replicate_date_01', 'replicate_date_02', 'replicate_date_03', 'replicate_date_04', 'replicate_date_05'];
			for (let i = 0; i < extras.length; i++) {
				const key = extras[i];
				if (box[key]) {
					records.push(box[key] + ' 第' + this.toChineseOrdinal(i + 2) + '次复刻');
				}
			}
			return records;
		},

		// 数字转中文序数
		toChineseOrdinal(n) {
			const map = ['零', '一', '二', '三', '四', '五', '六', '七', '八', '九', '十'];
			return map[n] || String(n);
		},

		getBoxMonths(box) {
			const months = [];
			const dates = [];
			if (box.release_date) dates.push(box.release_date);
			if (box.replicate_date) dates.push(box.replicate_date);
			const extras = ['replicate_date_01', 'replicate_date_02', 'replicate_date_03', 'replicate_date_04', 'replicate_date_05'];
			for (let i = 0; i < extras.length; i++) {
				if (box[extras[i]]) dates.push(box[extras[i]]);
			}
			for (let d = 0; d < dates.length; d++) {
				const match = dates[d].match(/^(\d{4})\/(\d{1,2})/);
				if (match) months.push({ year: parseInt(match[1]), month: parseInt(match[2]) });
			}
			return months;
		},

		// 获取盒子在卡片上显示的日期文本（同时有复刻日期和上线日期时都显示）
		getBoxDisplayDate(box) {
			const release = box.release_date || '';
			const replicate = box.replicate_date || '';
			if (replicate && release) {
				return release + ' / ' + replicate;
			} else if (replicate) {
				return replicate;
			} else if (release) {
				return release;
			} else {
				return '暂无数据';
			}
		},

		// 获取卡片标签（根据当前月份显示"上线"或"复刻"）
		getMonthTag(box) {
			const months = this.getBoxMonths(box);
			const curMonth = months.find(m => m.year === this.currentYear && m.month === this.currentMonth);
			if (!curMonth) return '上线';
			// 判断当前年月匹配的是哪个日期（上线/第N次复刻）
			const dates = [];
			if (box.release_date) dates.push({ d: box.release_date, label: '上线' });
			if (box.replicate_date) dates.push({ d: box.replicate_date, label: '第一次复刻' });
			const extras = ['replicate_date_01', 'replicate_date_02', 'replicate_date_03', 'replicate_date_04', 'replicate_date_05'];
			for (let i = 0; i < extras.length; i++) {
				if (box[extras[i]]) dates.push({ d: box[extras[i]], label: '第' + this.toChineseOrdinal(i + 2) + '次复刻' });
			}
			for (let k = 0; k < dates.length; k++) {
				const m = dates[k].d.match(/^(\d{4})\/(\d{1,2})/);
				if (m && parseInt(m[1]) === curMonth.year && parseInt(m[2]) === curMonth.month) {
					return dates[k].label;
				}
			}
			return '上线';
		},

		// 获取卡片显示的具体日期
		getMonthDate(box) {
			const dates = [];
			if (box.release_date) dates.push(box.release_date);
			if (box.replicate_date) dates.push(box.replicate_date);
			const extras = ['replicate_date_01', 'replicate_date_02', 'replicate_date_03', 'replicate_date_04', 'replicate_date_05'];
			for (let i = 0; i < extras.length; i++) {
				if (box[extras[i]]) dates.push(box[extras[i]]);
			}
			for (let d = 0; d < dates.length; d++) {
				const m = dates[d].match(/^(\d{4})\/(\d{1,2})/);
				if (m && parseInt(m[1]) === this.currentYear && parseInt(m[2]) === this.currentMonth) {
					return dates[d];
				}
			}
			return '';
		},

		getBoxOwnedCount(box) {
			if (!box || !box.characters) return 0;
			return box.characters.filter(c => c && c.name && this.ownedCharacters[c.name]).length;
		},

		getBoxCharacters(box) {
			return box.characters || [];
		},

		getBoxRate(box) {
			const total = (box?.characters || []).length;
			if (total === 0) return 0;
			return Math.round(this.getBoxOwnedCount(box) / total * 100);
		},

		goToBox(box) {
			// 跳转到搜索页面并自动搜索该盒号
			const boxId = box.Box_id || box.box_id || '';
			// 存储要搜索的盒号
			uni.setStorageSync('calendarSearchBoxId', boxId);
			uni.navigateTo({
				url: '/pages/Search/Search?fromCalendar=true'
			});
		}
	}
};
</script>

<style scoped>
.calendar-container {
    padding: 24rpx;
    min-height: 100vh;
    background: linear-gradient(180deg, #f8f9ff, #fff);
}

.month-selector {
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 8rpx 16rpx;
    background: var(--cal-card-bg, #fff);
    border-radius: 16rpx;
    margin-bottom: 24rpx;
    box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}

/* 翻页箭头：纯 CSS 绘制（不使用左右三角字符，iOS 会把它们渲染成 emoji） */
.month-arrow {
    width: 88rpx;
    height: 88rpx;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
}

.month-arrow-hover {
    background: var(--cal-switch-bg, #f2f3f5);
}

.chev {
    width: 18rpx;
    height: 18rpx;
    border-left: 4rpx solid var(--cal-sub, #666);
    border-bottom: 4rpx solid var(--cal-sub, #666);
    border-radius: 3rpx;
}

.chev-left {
    transform: rotate(45deg);
    margin-left: 6rpx;
}

.chev-right {
    transform: rotate(-135deg);
    margin-right: 6rpx;
}

.caret-down {
    width: 0;
    height: 0;
    border-left: 10rpx solid transparent;
    border-right: 10rpx solid transparent;
    border-top: 12rpx solid var(--cal-sub, #999);
    margin-left: 12rpx;
}

.month-text {
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 240rpx;
    padding: 12rpx 8rpx;
}

.month-text-label {
    font-size: 32rpx;
    font-weight: bold;
    color: var(--cal-text, #333);
}

.calendar-stats {
    display: flex;
    justify-content: space-around;
    background: #fff;
    border-radius: 16rpx;
    padding: 24rpx;
    margin-bottom: 24rpx;
    box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}

.stat-item {
    text-align: center;
}

.stat-num {
    font-size: 40rpx;
    font-weight: bold;
    color: #FF6B35;
    display: block;
}

.stat-label {
    font-size: 22rpx;
    color: #999;
    display: block;
    margin-top: 4rpx;
}

.timeline {
    padding-left: 20rpx;
}

.timeline-item {
    position: relative;
    padding-left: 40rpx;
    padding-bottom: 24rpx;
}

.timeline-dot {
    position: absolute;
    left: 0;
    top: 24rpx;
    width: 20rpx;
    height: 20rpx;
    border-radius: 50%;
    z-index: 2;
}

.dot-normal { background: #4CAF50; }
.dot-whitelist { background: #2196F3; }
.dot-special { background: #FF9800; }
.dot-cooperation { background: #9C27B0; }
.dot-ambience { background: #E91E63; }

.timeline-line {
    position: absolute;
    left: 8rpx;
    top: 48rpx;
    bottom: 0;
    width: 4rpx;
    background: #e0e0e0;
}

.timeline-card {
    background: #fff;
    border-radius: 16rpx;
    padding: 24rpx;
    box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.06);
}

.card-header {
    display: flex;
    flex-direction: column;
    margin-bottom: 12rpx;
}

.card-title-row {
    display: flex;
    align-items: center;
    gap: 10rpx;
    margin-bottom: 4rpx;
}

.card-name {
    font-size: 28rpx;
    font-weight: bold;
    color: #333;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    flex: 1;
}

.card-action-tag {
    font-size: 20rpx;
    color: #fff;
    background: #FF6B35;
    border-radius: 6rpx;
    padding: 2rpx 10rpx;
    flex-shrink: 0;
}

.card-date {
    font-size: 20rpx;
    color: #999;
    margin-top: 4rpx;
}

.card-subheader {
    display: flex;
    align-items: center;
    margin-bottom: 12rpx;
}

.card-type-tag {
    font-size: 20rpx;
    padding: 4rpx 12rpx;
    border-radius: 8rpx;
    flex-shrink: 0;
    margin-left: 12rpx;
}

.tag-normal { background: #E8F5E9; color: #388E3C; }
.tag-whitelist { background: #E3F2FD; color: #1976D2; }
.tag-special { background: #FFF3E0; color: #E65100; }
.tag-cooperation { background: #F3E5F5; color: #7B1FA2; }
.tag-ambience { background: #FCE4EC; color: #C2185B; }

.card-body {
    display: flex;
    flex-direction: column;
    gap: 12rpx;
}

.card-characters {
    font-size: 24rpx;
    color: #999;
    margin-bottom: 10rpx;
}

.card-char-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 8rpx;
    margin-bottom: 12rpx;
}

.char-tag {
    font-size: 22rpx;
    color: #FF6B35;
    background: #FFF0EB;
    border-radius: 8rpx;
    padding: 4rpx 12rpx;
    border: 1rpx solid #FFD0C4;
}

.card-replicate {
    display: flex;
    flex-direction: column;
    margin: 10rpx 0;
}

.card-replicate-label {
    font-size: 22rpx;
    color: #67C23A;
    font-weight: bold;
    margin-bottom: 4rpx;
}

.card-replicate-list {
    display: flex;
    flex-direction: column;
}

.card-replicate-line {
    font-size: 22rpx;
    color: #67C23A;
    line-height: 1.6;
}

.card-replicate-empty {
    font-size: 22rpx;
    color: #999;
}

.card-progress {
    display: flex;
    align-items: center;
    gap: 12rpx;
}

.mini-progress-bar {
    flex: 1;
    height: 8rpx;
    background: #f0f0f0;
    border-radius: 4rpx;
    overflow: hidden;
}

.mini-progress-fill {
    height: 100%;
    background: linear-gradient(90deg, #FF6B35, #FFA07A);
    border-radius: 4rpx;
    transition: width 0.5s ease;
}

.card-progress-text {
    font-size: 22rpx;
    color: #999;
    flex-shrink: 0;
}

.empty-month {
    text-align: center;
    padding: 80rpx 0;
}

.empty-icon {
    font-size: 64rpx;
    display: block;
    margin-bottom: 16rpx;
}

.empty-text {
    font-size: 26rpx;
    color: #ccc;
}

.legend {
    background: #fff;
    border-radius: 16rpx;
    padding: 24rpx;
    margin-top: 24rpx;
}

.legend-title {
    font-size: 26rpx;
    font-weight: bold;
    color: #333;
    display: block;
    margin-bottom: 16rpx;
}

.legend-items {
    display: flex;
    flex-wrap: wrap;
    gap: 20rpx;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 8rpx;
    font-size: 22rpx;
    color: #666;
}

.legend-dot {
    width: 16rpx;
    height: 16rpx;
    border-radius: 50%;
}

/* 月份快捷选择弹窗 */
.month-picker-mask {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.45);
    z-index: 999;
    display: flex;
    align-items: flex-end;
}

.month-picker-sheet {
    width: 100%;
    max-height: 70vh;
    background: #fff;
    border-radius: 24rpx 24rpx 0 0;
    overflow: hidden;
}

.picker-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 32rpx 32rpx 24rpx;
    border-bottom: 1rpx solid #f0f0f0;
}

.picker-title {
    font-size: 30rpx;
    font-weight: bold;
    color: #333;
}

.picker-close {
    font-size: 32rpx;
    color: #999;
    padding: 8rpx;
}

.picker-list {
    max-height: 60vh;
    padding: 16rpx 0;
}

.picker-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 24rpx 32rpx;
    font-size: 28rpx;
    color: #333;
}

.picker-item:active {
    background: #f5f5f5;
}

.picker-item.active {
    color: #FF6B35;
    font-weight: bold;
}

.picker-count {
    font-size: 24rpx;
    color: #999;
}

.picker-item.active .picker-count {
    color: #FF6B35;
}

/* ================= 主题变量（简约 / 工业浅色 / 工业深色 / 界园） ================= */
.container.calendar-container {
    --cal-card-bg: #ffffff;
    --cal-text: #333333;
    --cal-sub: #999999;
    --cal-border: #f0f0f0;
    --cal-accent: #FF6B35;
    --cal-switch-bg: #f2f3f5;
    --cal-switch-active-bg: #ffffff;
    --cal-today-bg: rgba(255, 107, 53, 0.12);
    --cal-dot: #FF6B35;
    --cal-disabled: #c8c9cc;
}

.container.calendar-container.theme-ak,
.container.calendar-container.theme-ark {
    --cal-card-bg: var(--ak-surface-panel, #fdfdfb);
    --cal-text: var(--ak-text-primary, #1b1d1f);
    --cal-sub: var(--ak-text-muted, #8d908e);
    --cal-border: var(--ak-hairline-soft, rgba(0, 0, 0, 0.08));
    --cal-accent: var(--ak-color-accent, #f6540e);
    --cal-switch-bg: var(--ak-surface-muted, #ecece9);
    --cal-switch-active-bg: var(--ak-surface-panel, #fdfdfb);
    --cal-today-bg: rgba(255, 106, 38, 0.16);
    --cal-dot: var(--ak-color-accent, #f6540e);
    --cal-disabled: var(--ak-text-muted, #8b8f8c);
}

.container.calendar-container.theme-jieyuan {
    --cal-card-bg: rgba(255, 255, 255, 0.92);
    --cal-text: #333333;
    --cal-sub: #8a8a8a;
    --cal-border: rgba(226, 88, 132, 0.15);
    --cal-accent: #e25884;
    --cal-switch-bg: #f7ecdd;
    --cal-switch-active-bg: #ffffff;
    --cal-today-bg: rgba(226, 88, 132, 0.12);
    --cal-dot: #e25884;
    --cal-disabled: #cbbfb4;
}

/* ================= 视图切换 ================= */
.view-switch {
    display: flex;
    padding: 6rpx;
    background: var(--cal-switch-bg, #f2f3f5);
    border-radius: 14rpx;
    margin-bottom: 24rpx;
}

.switch-item {
    flex: 1;
    height: 64rpx;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 10rpx;
}

.switch-item.active {
    background: var(--cal-switch-active-bg, #fff);
    box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);
}

.switch-text {
    font-size: 26rpx;
    color: var(--cal-sub, #888);
}

.switch-item.active .switch-text {
    color: var(--cal-accent, #FF6B35);
    font-weight: 600;
}

/* ================= 日历面板（uni-calendar 主题变量） ================= */
.calendar-panel {
    background: var(--cal-card-bg, #fff);
    border-radius: 16rpx;
    padding: 8rpx 12rpx 16rpx;
    margin-bottom: 24rpx;
    overflow: hidden;
    box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);

    --uc-bg: var(--cal-card-bg, #fff);
    --uc-text: var(--cal-text, #333);
    --uc-sub: var(--cal-sub, #9aa0a6);
    --uc-border: var(--cal-border, #f0f0f0);
    --uc-border-soft: var(--cal-border, #f5f5f5);
    --uc-primary: var(--cal-accent, #FF6B35);
    --uc-on-primary: #ffffff;
    --uc-today-bg: var(--cal-today-bg, rgba(255, 107, 53, 0.12));
    --uc-dot: var(--cal-dot, #FF6B35);
    --uc-disabled: var(--cal-disabled, #c8c9cc);
    --uc-hover-bg: var(--cal-switch-bg, #f2f3f5);
    --uc-watermark: var(--cal-border, #ededed);
}

/* ================= 日历视图下的列表（去掉时间线缩进） ================= */
.timeline-flat {
    padding-left: 0;
}

.timeline-flat .timeline-item {
    padding-left: 0;
    padding-bottom: 20rpx;
}

/* ================= 空状态图形（替换 emoji） ================= */
.empty-mark {
    width: 96rpx;
    height: 96rpx;
    border-radius: 50%;
    border: 4rpx dashed var(--cal-disabled, #d9d9d9);
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 0 auto 20rpx;
}

.empty-mark-line {
    width: 36rpx;
    height: 4rpx;
    border-radius: 2rpx;
    background: var(--cal-disabled, #d9d9d9);
}

/* ================= 关闭按钮（CSS 十字，替换叉号字符） ================= */
.picker-close {
    width: 64rpx;
    height: 64rpx;
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
}

.x-line {
    position: absolute;
    width: 26rpx;
    height: 3rpx;
    border-radius: 2rpx;
    background: var(--cal-sub, #999);
}

.x-line-a {
    transform: rotate(45deg);
}

.x-line-b {
    transform: rotate(-45deg);
}
/* ================= 宽屏两栏：左日历 / 右时间线 ================= */
.calendar-split.is-wide {
    display: flex;
    align-items: flex-start;
    gap: 24rpx;
    margin-bottom: 24rpx;
}

.calendar-split.is-wide .calendar-col {
    min-width: 0;
}

/* 左栏放日历，宽度占比略小；右栏时间线更长 */
.calendar-split.is-wide .col-calendar {
    flex: 0 0 45%;
}

.calendar-split.is-wide .col-timeline {
    flex: 1 1 0;
}

/* 两栏内部不再叠加底部外边距，避免左右高低错位 */
.calendar-split.is-wide .calendar-panel,
.calendar-split.is-wide .month-selector {
    margin-bottom: 20rpx;
}

.calendar-split.is-wide .timeline {
    margin-bottom: 0;
}

/* 窄屏保持原来的单栏排布 */
.calendar-split:not(.is-wide) {
    display: block;
}
</style>