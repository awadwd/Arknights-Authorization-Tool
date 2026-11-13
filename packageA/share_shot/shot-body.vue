<template>
	<!-- 分享卡片正文（今日宜挂 / 展柜）+ 小程序码脚注
	     canvas 导出前用户在屏上看到的就是这份，改样式务必与 share_shot.vue 的 paint() 同步 -->
	<view class="shot-card">
		<view class="card">
			<block v-if="payload.type === 'daily'">
				<view class="card-head">
					<text class="eyebrow">今日宜挂</text>
					<text class="card-title">{{ payload.catLabel || '今日推荐' }}</text>
				</view>

				<view class="op-row" v-if="payload.special !== 'lucky'">
					<view class="op-avatar">
						<image
							v-if="payload.avatar"
							class="op-img"
							:src="payload.avatar"
							mode="aspectFill"
							@load="onImgLoad"
							@error="onImgLoad"
						></image>
						<text v-else class="op-emoji">?</text>
					</view>
					<view class="op-info">
						<text class="op-name">{{ payload.name }}</text>
						<view class="tag-row" v-if="payload.tag">
							<text class="tag">{{ payload.tag }}</text>
						</view>
						<view class="box-row" v-if="payload.box">
							<text class="box-label">推荐盒号：</text>
							<text class="box-id">{{ payload.box }}</text>
						</view>
					</view>
				</view>

				<view class="op-row special" v-else>
					<text class="op-emoji-big">✨</text>
				</view>

				<view class="reason-box">
					<text class="reason-label">推荐理由</text>
					<text class="reason-text">{{ payload.reason || '' }}</text>
				</view>

				<view class="warn-box" v-if="payload.warn">
					<text class="warn-label">⚠ 慎重</text>
					<text class="warn-text">{{ payload.warn }}</text>
				</view>

				<text class="card-tip">结果随机生成，仅供娱乐</text>
			</block>

			<block v-else>
				<view class="card-head">
					<text class="eyebrow">通行证展柜</text>
					<text class="card-title">我的通行证展柜</text>
				</view>

				<!-- 与 canvas 导出版一一对应：大数字 + 百分比 + 进度条 -->
				<block v-if="payload.totalChars > 0">
					<view class="stat-row">
						<view class="stat-main">
							<text class="stat-num">{{ ownedCount }}</text>
							<text class="stat-total">/ {{ payload.totalChars }}</text>
						</view>
						<text class="stat-pct">{{ percent }}%</text>
					</view>
					<view class="bar">
						<view class="bar-fill" :style="{ width: percent + '%' }"></view>
					</view>
				</block>
				<text class="card-sub" v-else>{{ payload.sub || '' }}</text>

				<text class="meta-line" v-if="metaText">{{ metaText }}</text>

				<view class="chip-list" v-if="payload.names && payload.names.length">
					<view class="chip" v-for="(item, idx) in payload.names" :key="idx" :class="{ gold: item.elite2 }">
						<text class="chip-name">{{ item.name }}</text>
						<text class="chip-tag" v-if="item.elite2">精二</text>
					</view>
					<view class="chip more" v-if="payload.rest > 0">
						<text class="chip-name">+{{ payload.rest }}</text>
					</view>
				</view>

				<!-- 底部不再重复「共 N 位已点亮」，改说精一/精二构成；
				     空态紧跟在 v-if 之后，中间不能夹注释 -->
				<view class="sum-row" v-if="payload.names && payload.names.length">
					<text class="sum-text">精一 {{ plainCount }} 位</text>
					<text class="sum-text">精二 {{ payload.elite2Count }} 位</text>
				</view>
				<view class="empty-box" v-else>
					<text class="empty-text">还没有点亮任何通行证</text>
					<text class="empty-sub">去展柜勾选你已收集的干员</text>
				</view>
			</block>
		</view>

		<view class="foot">
			<image class="foot-qr" src="/static/wechat-code.jpg" mode="aspectFit" @load="onImgLoad" @error="onImgLoad"></image>
			<view class="foot-text">
				<text class="foot-title">{{ payload.qrTitle || '微信扫码 · 方舟通行证谷子查询工具' }}</text>
				<text class="foot-sub">数据只存在本机，不上传服务器</text>
			</view>
		</view>
	</view>
</template>

<script>
	export default {
		name: 'shot-body',
		props: {
			payload: {
				type: Object,
				default: () => ({})
			}
		},
		computed: {
			ownedCount() {
				const p = this.payload || {};
				return typeof p.owned === 'number' ? p.owned : (p.total || 0);
			},
			percent() {
				const p = this.payload || {};
				const t = p.totalChars || 0;
				if (!t) return 0;
				return Math.max(0, Math.min(100, Math.round(this.ownedCount / t * 100)));
			},
			plainCount() {
				return Math.max(0, this.ownedCount - ((this.payload && this.payload.elite2Count) || 0));
			},
			metaText() {
				const p = this.payload || {};
				const bits = [];
				if (p.wanted != null) bits.push('想要 ' + p.wanted);
				if (p.trade != null) bits.push('出物 ' + p.trade);
				return bits.join(' · ');
			}
		},
		methods: {
			onImgLoad() {
				this.$emit('imgload');
			}
		}
	};
</script>

<style>
	/* Skyline 约束：不用 CSS 变量 / Grid / position:fixed / 伪类 */
	.shot-card {
		width: 100%;
		/* 与页面底色同色：预览看不出区别，但导出的 PNG 是不透明底，
		   否则卡片以外（小程序码那一行）会是透明，相册里发白/发黑 */
		background-color: #f2f3f5;
		display: flex;
		flex-direction: column;
	}

	.card {
		width: 100%;
		background-color: #ffffff;
		border-radius: 24rpx;
		border: 1rpx solid #ececef;
		padding: 36rpx 30rpx 28rpx;
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
	}

	.card-head {
		display: flex;
		flex-direction: column;
	}

	.eyebrow {
		font-size: 22rpx;
		letter-spacing: 4rpx;
		color: #9a9aa0;
	}

	.card-title {
		font-size: 44rpx;
		font-weight: bold;
		color: #1c1c1e;
		margin-top: 8rpx;
	}

	.card-sub {
		font-size: 24rpx;
		color: #8a8a90;
		margin-top: 10rpx;
	}

	/* ===== 展柜数据行（尺寸对着 paint() 的逻辑像素 ×2）===== */
	.stat-row {
		display: flex;
		flex-direction: row;
		align-items: flex-end;
		justify-content: space-between;
		margin-top: 20rpx;
	}

	.stat-main {
		display: flex;
		flex-direction: row;
		align-items: flex-end;
	}

	.stat-num {
		font-size: 60rpx;
		font-weight: bold;
		color: #1c1c1e;
		line-height: 1;
	}

	.stat-total {
		font-size: 24rpx;
		color: #9a9aa0;
		margin-left: 10rpx;
		margin-bottom: 6rpx;
	}

	.stat-pct {
		font-size: 24rpx;
		color: #9a9aa0;
		margin-bottom: 6rpx;
	}

	.bar {
		height: 10rpx;
		border-radius: 5rpx;
		background-color: #eceef0;
		margin-top: 20rpx;
	}

	.bar-fill {
		height: 10rpx;
		border-radius: 5rpx;
		background-color: #2f7ed8;
	}

	.meta-line {
		font-size: 22rpx;
		color: #9a9aa0;
		margin-top: 20rpx;
	}

	.empty-box {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		margin-top: 24rpx;
		padding: 40rpx 0;
		background-color: #f7f7f9;
		border-radius: 16rpx;
	}

	.empty-text {
		font-size: 26rpx;
		color: #8a8a90;
	}

	.empty-sub {
		font-size: 22rpx;
		color: #b0b0b8;
		margin-top: 10rpx;
	}

	.op-row {
		display: flex;
		flex-direction: row;
		align-items: center;
		margin-top: 28rpx;
		padding: 24rpx;
		background-color: #f7f7f9;
		border-radius: 18rpx;
	}

	.op-row.special {
		justify-content: center;
		padding: 40rpx 24rpx;
	}

	.op-avatar {
		width: 108rpx;
		height: 108rpx;
		border-radius: 54rpx;
		background-color: #eceef2;
		display: flex;
		align-items: center;
		justify-content: center;
		overflow: hidden;
	}

	.op-img {
		width: 108rpx;
		height: 108rpx;
	}

	.op-emoji {
		font-size: 44rpx;
		color: #b0b0b8;
	}

	.op-emoji-big {
		font-size: 88rpx;
		line-height: 100rpx;
	}

	.op-info {
		flex: 1;
		margin-left: 24rpx;
		display: flex;
		flex-direction: column;
	}

	.op-name {
		font-size: 34rpx;
		font-weight: bold;
		color: #1c1c1e;
	}

	.tag-row {
		display: flex;
		flex-direction: row;
		margin-top: 10rpx;
	}

	.tag {
		font-size: 20rpx;
		color: #2f7ed8;
		background-color: #eaf3ff;
		border-radius: 8rpx;
		padding: 4rpx 12rpx;
	}

	.box-row {
		display: flex;
		flex-direction: row;
		align-items: center;
		margin-top: 10rpx;
	}

	.box-label {
		font-size: 22rpx;
		color: #8a8a90;
	}

	.box-id {
		font-size: 24rpx;
		font-weight: bold;
		color: #1c1c1e;
	}

	.reason-box {
		margin-top: 24rpx;
		padding: 22rpx 24rpx;
		background-color: #f7f7f9;
		border-radius: 16rpx;
		display: flex;
		flex-direction: column;
	}

	.reason-label {
		font-size: 22rpx;
		color: #9a9aa0;
	}

	.reason-text {
		font-size: 26rpx;
		line-height: 40rpx;
		color: #3a3a40;
		margin-top: 8rpx;
	}

	.warn-box {
		margin-top: 16rpx;
		padding: 18rpx 24rpx;
		background-color: #fff7e8;
		border-radius: 16rpx;
		display: flex;
		flex-direction: column;
	}

	.warn-label {
		font-size: 22rpx;
		color: #b07a1a;
	}

	.warn-text {
		font-size: 24rpx;
		line-height: 36rpx;
		color: #8a6112;
		margin-top: 6rpx;
	}

	.card-tip {
		margin-top: 22rpx;
		font-size: 20rpx;
		color: #b0b0b8;
		text-align: center;
	}

	.chip-list {
		display: flex;
		flex-direction: row;
		flex-wrap: wrap;
		margin-top: 26rpx;
	}

	.chip {
		display: flex;
		flex-direction: row;
		align-items: center;
		padding: 8rpx 16rpx;
		border-radius: 12rpx;
		background-color: #f5f6f8;
		border: 1rpx solid #ececef;
		margin-right: 12rpx;
		margin-bottom: 12rpx;
	}

	.chip.gold {
		background-color: #fdf3d8;
		border-color: #f0dfb0;
	}

	.chip.more {
		background-color: #eef1f5;
	}

	.chip-name {
		font-size: 22rpx;
		color: #3a3a40;
	}

	.chip.gold .chip-name {
		color: #8a6112;
	}

	.chip-tag {
		font-size: 18rpx;
		color: #8a6112;
		margin-left: 6rpx;
	}

	.sum-row {
		display: flex;
		flex-direction: row;
		justify-content: space-between;
		margin-top: 20rpx;
		padding-top: 20rpx;
		border-top: 1rpx solid #ececef;
	}

	.sum-text {
		font-size: 22rpx;
		color: #9a9aa0;
	}

	.foot {
		display: flex;
		flex-direction: row;
		align-items: center;
		padding: 22rpx 8rpx 0;
	}

	.foot-qr {
		width: 132rpx;
		height: 132rpx;
		border-radius: 12rpx;
		background-color: #ffffff;
	}

	.foot-text {
		flex: 1;
		margin-left: 18rpx;
		display: flex;
		flex-direction: column;
	}

	.foot-title {
		font-size: 24rpx;
		color: #3a3a40;
		line-height: 34rpx;
	}

	.foot-sub {
		font-size: 20rpx;
		color: #9a9aa0;
		margin-top: 8rpx;
	}
</style>
