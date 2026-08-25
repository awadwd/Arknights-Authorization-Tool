<template>
	<view class="calendar-container">
		<!-- 月份选择 -->
		<view class="month-selector">
			<text class="month-arrow" @click="prevMonth">◀</text>
			<text class="month-text" @click="showMonthPicker">{{ currentYear }}年{{ currentMonth }}月 ▼</text>
			<text class="month-arrow" @click="nextMonth">▶</text>
		</view>

		<!-- 统计概览 -->
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

		<!-- 盒子时间线 -->
		<view class="timeline">
			<view class="timeline-item" v-for="(box, index) in monthBoxes" :key="index" @click="goToBox(box)">
				<view class="timeline-dot" :class="'dot-' + getBoxTypeClass(box.Box_type)"></view>
				<view class="timeline-line" v-if="index < monthBoxes.length - 1"></view>
				<view class="timeline-card">
					<view class="card-header">
						<view class="card-title-row">
							<text class="card-name">{{ box.Box_id || box.box_id }}</text>
							<text class="card-action-tag">{{ getMonthTag(box) }}</text>
						</view>
						<text class="card-date">{{ getMonthDate(box) }}</text>
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

			<view class="empty-month" v-if="monthBoxes.length === 0">
				<text class="empty-icon">📭</text>
				<text class="empty-text">该月没有通行证盒子</text>
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
					<text class="picker-close" @click="closeMonthPicker">✕</text>
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
	</view>
</template>

<script>
import errorLog from "@/utils/errorLog.js";
export default {
	data() {
		return {
			boxes: [],
			ownedCharacters: {},
			currentYear: 2025,
			currentMonth: 1,
			minMonth: { year: 2024, month: 1 },
			maxMonth: { year: 2026, month: 12 },
			showMonthPickerFlag: false
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
		this.loadData();
	},
	onShow() {
		this.ownedCharacters = uni.getStorageSync('ownedCharacters') || {};
	},
	methods: {
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
    padding: 24rpx;
    background: #fff;
    border-radius: 16rpx;
    margin-bottom: 24rpx;
    box-shadow: 0 2rpx 12rpx rgba(0, 0, 0, 0.05);
}

.month-arrow {
    font-size: 28rpx;
    color: #666;
    padding: 12rpx 24rpx;
}

.month-text {
    font-size: 32rpx;
    font-weight: bold;
    color: #333;
    min-width: 240rpx;
    text-align: center;
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
</style>