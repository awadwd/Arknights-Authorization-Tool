<template>
	<view class="trade-container">
		<!-- 筛选标签 -->
		<view class="filter-tabs">
			<view 
				class="filter-tab" 
				v-for="(tab, idx) in filterTabs" 
				:key="idx"
				:class="{ active: currentFilter === tab.value }"
				@click="currentFilter = tab.value"
			>
				<text>{{ tab.label }}</text>
			</view>
		</view>

		<!-- 发帖按钮 -->
		<view class="post-btn-wrapper">
			<button class="post-btn" @click="showPostModal = true">+ 发布信息</button>
		</view>

		<!-- 帖子列表 -->
		<scroll-view scroll-y class="post-list" @scrolltolower="loadMore">
			<view class="post-card" v-for="(post, index) in filteredPosts" :key="index">
				<view class="post-header">
					<view class="post-type-tag" :class="'type-' + post.type">
						<text>{{ getTypeText(post.type) }}</text>
					</view>
					<text class="post-time">{{ post.timeText }}</text>
				</view>
				<view class="post-body">
					<text class="post-character">{{ post.characterName }}</text>
					<view class="post-tags">
						<text class="post-level-tag" v-if="post.level">{{ post.level }}</text>
						<text class="post-quantity-tag">×{{ post.quantity }}</text>
					</view>
				</view>
				<view class="post-extra" v-if="post.description">
					<text class="post-desc">{{ post.description }}</text>
				</view>
				<view class="post-contact" v-if="post.contact">
					<text class="contact-label">联系方式：</text>
					<text class="contact-value">{{ post.contact }}</text>
				</view>
			</view>

			<view class="empty-list" v-if="filteredPosts.length === 0">
				<text class="empty-icon">💬</text>
				<text class="empty-text">暂无帖子，快来发布第一条吧</text>
			</view>
		</scroll-view>

		<!-- 发帖弹窗 -->
		<view class="modal-mask" v-if="showPostModal" @click="showPostModal = false">
			<view class="modal-content" @click.stop>
				<text class="modal-title">发布交换信息</text>
				
				<view class="form-group">
					<text class="form-label">类型</text>
					<view class="type-selector">
						<view 
							class="type-option" 
							v-for="(t, idx) in typeOptions" 
							:key="idx"
							:class="{ active: newPost.type === t.value }"
							@click="newPost.type = t.value"
						>
							<text>{{ t.label }}</text>
						</view>
					</view>
				</view>

				<view class="form-group">
					<text class="form-label">角色名称</text>
					<input class="form-input" v-model="newPost.characterName" placeholder="输入干员名称" />
				</view>

				<view class="form-group">
					<text class="form-label">精英阶段</text>
					<view class="level-selector">
						<view class="level-option" :class="{ active: newPost.level === '精一' }" @click="newPost.level = '精一'">
							<text>精一</text>
						</view>
						<view class="level-option" :class="{ active: newPost.level === '精二' }" @click="newPost.level = '精二'">
							<text>精二</text>
						</view>
						<view class="level-option" :class="{ active: newPost.level === '一对' }" @click="newPost.level = '一对'">
							<text>一对</text>
						</view>
					</view>
				</view>

				<view class="form-group">
					<text class="form-label">数量</text>
					<view class="quantity-selector">
						<text class="qty-btn" @click="newPost.quantity = Math.max(1, newPost.quantity - 1)">−</text>
						<text class="qty-value">{{ newPost.quantity }}</text>
						<text class="qty-btn" @click="newPost.quantity++">+</text>
					</view>
				</view>

				<view class="form-group">
					<text class="form-label">补充说明（选填）</text>
					<textarea class="form-textarea" v-model="newPost.description" placeholder="如：想要交换XX角色的精二" maxlength="200"></textarea>
				</view>

				<view class="form-group">
					<text class="form-label">联系方式</text>
					<input class="form-input" v-model="newPost.contact" placeholder="QQ/微信号" />
				</view>

				<view class="modal-actions">
					<button class="modal-btn cancel-btn" @click="showPostModal = false">取消</button>
					<button class="modal-btn submit-btn" @click="submitPost">发布</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
export default {
	data() {
		return {
			posts: [],
			currentFilter: 'all',
			filterTabs: [
				{ label: '全部', value: 'all' },
				{ label: '求购', value: 'buy' },
				{ label: '出售', value: 'sell' },
				{ label: '交换', value: 'exchange' }
			],
			showPostModal: false,
			typeOptions: [
				{ label: '💰 求购', value: 'buy' },
				{ label: '🏷️ 出售', value: 'sell' },
				{ label: '🔄 交换', value: 'exchange' }
			],
			newPost: {
				type: 'buy',
				characterName: '',
				level: '精二',
				quantity: 1,
				description: '',
				contact: ''
			}
		};
	},
	computed: {
		filteredPosts() {
			if (this.currentFilter === 'all') return this.posts;
			return this.posts.filter(p => p.type === this.currentFilter);
		}
	},
	onLoad() {
		this.loadPosts();
	},
	methods: {
		loadPosts() {
			try {
				const saved = uni.getStorageSync('tradeBoardPosts') || [];
				this.posts = saved.map(p => ({
					...p,
					timeText: this.formatTime(p.timestamp)
				}));
			} catch (e) {
				this.posts = [];
			}
		},

		savePosts() {
			uni.setStorageSync('tradeBoardPosts', this.posts);
		},

		submitPost() {
			if (!this.newPost.characterName.trim()) {
				uni.showToast({ title: '请输入角色名称', icon: 'none' });
				return;
			}
			if (!this.newPost.contact.trim()) {
				uni.showToast({ title: '请输入联系方式', icon: 'none' });
				return;
			}

			const post = {
				id: Date.now(),
				type: this.newPost.type,
				characterName: this.newPost.characterName.trim(),
				level: this.newPost.level,
				quantity: this.newPost.quantity,
				description: this.newPost.description.trim(),
				contact: this.newPost.contact.trim(),
				timestamp: Date.now()
			};

			this.posts.unshift(post);
			this.savePosts();

			// 重置表单
			this.newPost = {
				type: 'buy',
				characterName: '',
				level: '精二',
				quantity: 1,
				description: '',
				contact: ''
			};
			this.showPostModal = false;

			uni.showToast({ title: '发布成功', icon: 'success' });
		},

		getTypeText(type) {
			const map = { buy: '求购', sell: '出售', exchange: '交换' };
			return map[type] || type;
		},

		formatTime(timestamp) {
			if (!timestamp) return '';
			const d = new Date(timestamp);
			const now = new Date();
			const diff = now - d;

			if (diff < 60000) return '刚刚';
			if (diff < 3600000) return Math.floor(diff / 60000) + '分钟前';
			if (diff < 86400000) return Math.floor(diff / 3600000) + '小时前';
			if (diff < 604800000) return Math.floor(diff / 86400000) + '天前';

			return `${d.getMonth() + 1}/${d.getDate()}`;
		},

		loadMore() {
			// 预留分页加载
		}
	}
};
</script>

<style scoped>
.trade-container {
    min-height: 100vh;
    background: #f5f5f5;
}

.filter-tabs {
    display: flex;
    background: #fff;
    padding: 16rpx 24rpx;
    gap: 16rpx;
    box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.05);
}

.filter-tab {
    padding: 12rpx 28rpx;
    border-radius: 32rpx;
    font-size: 26rpx;
    color: #666;
    background: #f5f5f5;
    transition: all 0.3s;
}

.filter-tab.active {
    background: linear-gradient(135deg, #FF6B35, #FF8F5E);
    color: #fff;
    box-shadow: 0 4rpx 12rpx rgba(255, 107, 53, 0.3);
}

.post-btn-wrapper {
    padding: 16rpx 24rpx;
}

.post-btn {
    background: linear-gradient(135deg, #FF6B35, #FF8F5E);
    color: #fff;
    border: none;
    border-radius: 16rpx;
    font-size: 28rpx;
    padding: 20rpx;
    box-shadow: 0 4rpx 16rpx rgba(255, 107, 53, 0.3);
}

.post-list {
    padding: 0 24rpx 24rpx;
    height: calc(100vh - 200rpx);
}

.post-card {
    background: #fff;
    border-radius: 16rpx;
    padding: 24rpx;
    margin-bottom: 16rpx;
    box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);
}

.post-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16rpx;
}

.post-type-tag {
    font-size: 22rpx;
    padding: 4rpx 16rpx;
    border-radius: 8rpx;
    color: #fff;
}

.type-buy { background: #4CAF50; }
.type-sell { background: #FF9800; }
.type-exchange { background: #2196F3; }

.post-time {
    font-size: 22rpx;
    color: #ccc;
}

.post-body {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12rpx;
}

.post-character {
    font-size: 32rpx;
    font-weight: bold;
    color: #333;
}

.post-tags {
    display: flex;
    gap: 8rpx;
}

.post-level-tag {
    font-size: 20rpx;
    padding: 4rpx 12rpx;
    border-radius: 6rpx;
    background: #FFF3E0;
    color: #E65100;
}

.post-quantity-tag {
    font-size: 20rpx;
    padding: 4rpx 12rpx;
    border-radius: 6rpx;
    background: #E3F2FD;
    color: #1976D2;
}

.post-extra {
    margin-bottom: 8rpx;
}

.post-desc {
    font-size: 24rpx;
    color: #666;
    line-height: 1.6;
}

.post-contact {
    padding-top: 12rpx;
    border-top: 1rpx solid #f5f5f5;
}

.contact-label {
    font-size: 22rpx;
    color: #999;
}

.contact-value {
    font-size: 24rpx;
    color: #FF6B35;
}

.empty-list {
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

.modal-mask {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 999;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: flex-end;
}

.modal-content {
    background: #fff;
    border-radius: 32rpx 32rpx 0 0;
    padding: 32rpx;
    width: 100%;
    max-height: 85vh;
    overflow-y: auto;
}

.modal-title {
    font-size: 34rpx;
    font-weight: bold;
    color: #333;
    display: block;
    text-align: center;
    margin-bottom: 32rpx;
}

.form-group {
    margin-bottom: 24rpx;
}

.form-label {
    font-size: 26rpx;
    color: #333;
    font-weight: 500;
    display: block;
    margin-bottom: 12rpx;
}

.form-input {
    width: 100%;
    height: 80rpx;
    border: 2rpx solid #e0e0e0;
    border-radius: 16rpx;
    padding: 0 24rpx;
    font-size: 28rpx;
    box-sizing: border-box;
}

.form-textarea {
    width: 100%;
    height: 120rpx;
    border: 2rpx solid #e0e0e0;
    border-radius: 16rpx;
    padding: 16rpx 24rpx;
    font-size: 26rpx;
    box-sizing: border-box;
}

.type-selector, .level-selector {
    display: flex;
    gap: 12rpx;
}

.type-option, .level-option {
    flex: 1;
    text-align: center;
    padding: 16rpx;
    border-radius: 12rpx;
    font-size: 24rpx;
    color: #666;
    background: #f5f5f5;
    border: 2rpx solid transparent;
    transition: all 0.3s;
}

.type-option.active, .level-option.active {
    background: #FFF3E0;
    color: #FF6B35;
    border-color: #FF6B35;
}

.quantity-selector {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 32rpx;
}

.qty-btn {
    width: 64rpx;
    height: 64rpx;
    border-radius: 50%;
    background: #f5f5f5;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 36rpx;
    color: #333;
}

.qty-value {
    font-size: 36rpx;
    font-weight: bold;
    color: #333;
    min-width: 60rpx;
    text-align: center;
}

.modal-actions {
    display: flex;
    gap: 24rpx;
    margin-top: 32rpx;
}

.modal-btn {
    flex: 1;
    height: 88rpx;
    border: none;
    border-radius: 16rpx;
    font-size: 30rpx;
}

.cancel-btn {
    background: #f5f5f5;
    color: #666;
}

.submit-btn {
    background: linear-gradient(135deg, #FF6B35, #FF8F5E);
    color: #fff;
    box-shadow: 0 4rpx 12rpx rgba(255, 107, 53, 0.3);
}
</style>
