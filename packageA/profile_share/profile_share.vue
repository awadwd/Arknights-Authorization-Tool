<template>
	<view class="ps" :class="dark ? 'ps-dark' : 'ps-light'">

		<!-- ===================== 头部 ===================== -->
		<view class="ps-head">
			<text class="ps-kicker">PREFERENCES SYNC</text>
			<text class="ps-title">个性化设置分享</text>
			<text class="ps-desc">
				把主题、布局、头像规则、收藏这些偏好打包，换设备不用重新点一遍。
				数据类内容（盒号、搜索词）不参与分享。
			</text>
		</view>

		<!-- ===================== 待应用（从链接 / 短码进来） ===================== -->
		<view class="card" v-if="incoming">
			<view class="card-head">
				<text class="card-title">{{ sourceLabel }}</text>
				<text class="card-tag">{{ changeCount }} 项变化</text>
			</view>
			<view class="diff">
				<view class="diff-row" v-for="(d, i) in diffList" :key="i">
					<view class="diff-name">
						<text class="diff-label">{{ d.label }}</text>
						<text class="diff-group">{{ d.group }}</text>
					</view>
					<view class="diff-value">
						<text class="diff-before" v-if="!d.same">{{ d.currentText }}</text>
						<text class="diff-arrow" v-if="!d.same">→</text>
						<text class="diff-after" :class="{ muted: d.same }">{{ d.same ? d.incomingText + '（不变）' : d.incomingText }}</text>
					</view>
				</view>
			</view>
			<view class="btn-solid" :class="{ disabled: applying }" @click="applyIncoming">
				<text>{{ applying ? '正在写入…' : '应用这份设置' }}</text>
			</view>
			<view class="btn-ghost" @click="resetIncoming"><text>取消，回到我的设置</text></view>
		</view>

		<!-- ===================== 分享我的 ===================== -->
		<view class="card" v-else>
			<view class="card-head">
				<text class="card-title">我当前的偏好</text>
				<text class="card-tag">{{ ownList.length }} 项</text>
			</view>
			<view class="diff">
				<view class="diff-row" v-for="(d, i) in ownList" :key="i">
					<view class="diff-name">
						<text class="diff-label">{{ d.label }}</text>
						<text class="diff-group">{{ d.group }}</text>
					</view>
					<text class="diff-after">{{ d.text }}</text>
				</view>
			</view>

			<view class="btn-solid" @click="generateToken">
				<text>{{ token ? '重新生成口令' : '生成分享口令' }}</text>
			</view>

			<view class="token-box" v-if="token">
				<text class="token-text" selectable>{{ token }}</text>
				<view class="token-meta">
					<text class="token-len">{{ token.length }} 字符</text>
					<text class="token-copy" @click="copyToken">复制</text>
				</view>
			</view>

			<view class="row-actions" v-if="token">
				<view class="btn-ghost small" @click="copyShareLink"><text>复制链接</text></view>
				<view class="btn-ghost small" :class="{ disabled: uploading }" @click="uploadCode">
					<text>{{ uploading ? '上传中…' : '换 6 位短码' }}</text>
				</view>
			</view>

			<view class="code-box" v-if="code">
				<text class="code-label">短码（30 天有效）</text>
				<text class="code-value" selectable>{{ code }}</text>
				<view class="code-foot">
					<text class="code-del">撤销码 {{ delCode }}</text>
					<text class="token-copy" @click="copyCode">复制</text>
				</view>
				<text class="code-hint">在新设备上输入短码即可恢复；撤销码只在你需要提前作废时用，别一起分享出去。</text>
			</view>
		</view>

		<!-- ===================== 恢复 ===================== -->
		<view class="card">
			<view class="card-head">
				<text class="card-title">恢复别人的设置</text>
			</view>
			<text class="card-desc">粘贴口令、6 位短码，或整条分享链接都可以。</text>
			<textarea
				class="input"
				v-model="input"
				placeholder="AKP1…  /  K7M2QX  /  https://…?p=AKP1…"
				placeholder-class="input-ph"
				:maxlength="-1"
				auto-height
			/>
			<view class="btn-solid" :class="{ disabled: loading }" @click="restore">
				<text>{{ loading ? '读取中…' : '读取并预览' }}</text>
			</view>
			<text class="card-note">
				应用后会覆盖你当前同名的偏好项，收藏与头像名单也会一并替换。数据缓存不受影响。
			</text>
		</view>

		<view class="card">
			<view class="card-head">
				<text class="card-title">我的云存档</text>
				<text class="card-tag" v-if="archiveCode">{{ archiveCode }}</text>
			</view>
			<text class="card-desc">换设备、重装系统时，用存档码把设置取回来。存档不会过期；填了 PIN 之后别人光有存档码也改不了。</text>
			<input
				class="input input-line"
				v-model="archiveCode"
				placeholder="存档码，留空则自动生成（如 K7M2-QX9P）"
				placeholder-class="input-ph"
				:maxlength="12"
			/>
			<input
				class="input input-line"
				v-model="archivePin"
				placeholder="PIN，4~8 位数字，可留空"
				placeholder-class="input-ph"
				:maxlength="8"
				type="number"
			/>
			<view class="row-actions">
				<view class="btn-solid" :class="{ disabled: archiveBusy }" @click="saveArchive">
					<text>{{ archiveBusy ? '处理中…' : '上传当前设置' }}</text>
				</view>
			</view>
			<view class="btn-ghost" @click="loadArchive"><text>从存档恢复</text></view>
			<text class="card-note">
				存档只含偏好设置和成绩摘要，不含缓存数据。上传会覆盖同码存档，所以设了 PIN 记得留住。
			</text>
		</view>

		<text class="ps-foot">口令在你和设备之间直接传递，不经过服务器；短码版会把口令存在云端 KV，30 天后自动删除。</text>
	</view>
</template>

<script>
	import {
		PREF_FIELDS,
		collectPrefs,
		applyPrefs,
		diffPrefs,
		encodePrefs,
		decodePrefs,
		buildShareLink,
		uploadProfile,
		fetchProfile,
		isShortCode,
		extractToken,
		extractShortCode
	} from '@/utils/profileShare.js';
	import { invalidate } from '@/utils/avatarPref.js';
	import { copyText, openLink } from '@/utils/openLink.js';
	import { archiveSave, archiveLoad } from '@/utils/cloudSync.js';
	import errorLog from '@/utils/errorLog.js';

	const CODE_CACHE = 'profileShareLast';

	export default {
		data() {
			return {
				dark: false,
				ownList: [],
				token: '',
				code: '',
				delCode: '',
				input: '',
				loading: false,
				uploading: false,
				applying: false,
				incoming: null,
				diffList: [],
				changeCount: 0,
				sourceLabel: '从分享内容读取',
				archiveCode: '',
				archivePin: '',
				archiveBusy: false
			};
		},

		onLoad(query) {
			this.loadTheme();
			this.loadOwn();
			try {
				this.archiveCode = uni.getStorageSync('archiveCode') || '';
			} catch (e) { /* 读不到就算了 */ }

			const q = query || {};
			if (q.c) {
				this.input = String(q.c);
				this.sourceLabel = '短码 ' + String(q.c).toUpperCase();
				this.loading = true;
				this.readShortCode(String(q.c));
				return;
			}
			if (q.p) {
				this.input = String(q.p);
				this.sourceLabel = '来自分享链接';
				this.previewToken(String(q.p));
			}
		},

		methods: {
			loadTheme() {
				try {
					const t = uni.getStorageSync('themeMode');
					this.dark = ['ark', 'ak', 'liquidDark'].indexOf(t) > -1;
				} catch (e) {
					this.dark = false;
				}
			},

			loadOwn() {
				try {
					const cur = collectPrefs();
					this.ownList = PREF_FIELDS.filter((f) => cur[f.key] !== undefined).map((f) => ({
						group: f.group,
						label: f.label,
						text: f.fmt(cur[f.key])
					}));
				} catch (e) {
					errorLog.logError(e, 'profile_share.loadOwn');
					this.ownList = [];
				}
			},

			/* ---------------- 云存档 ---------------- */
			collectStat() {
				try {
					const best = uni.getStorageSync('passportGameBest') || {};
					const fav = uni.getStorageSync('favoriteCharacterNames');
					const box = uni.getStorageSync('favoriteBoxIds');
					return {
						best: best && typeof best === 'object' ? best : {},
						chars: Array.isArray(fav) ? fav.length : 0,
						boxes: Array.isArray(box) ? box.length : 0
					};
				} catch (e) {
					return null;
				}
			},

			async saveArchive() {
				if (this.archiveBusy) return;
				this.archiveBusy = true;
				try {
					if (!this.token) this.token = encodePrefs(collectPrefs());
					const res = await archiveSave(this.token, {
						code: this.archiveCode,
						pin: this.archivePin,
						stat: this.collectStat()
					});
					if (!res || res.ok !== true) {
						uni.showToast({ title: (res && res.error) || '保存失败，检查一下网络', icon: 'none' });
						return;
					}
					this.archiveCode = res.code;
					try {
						uni.setStorageSync('archiveCode', res.code);
					} catch (e) { /* 忽略 */ }
					copyText(res.code, '存档码已复制：' + res.code);
				} catch (e) {
					errorLog.logError(e, 'profile_share.saveArchive');
					uni.showToast({ title: '保存失败', icon: 'none' });
				} finally {
					this.archiveBusy = false;
				}
			},

			async loadArchive() {
				const code = String(this.archiveCode || '').trim();
				if (!code) {
					uni.showToast({ title: '先填存档码', icon: 'none' });
					return;
				}
				this.loading = true;
				try {
					const res = await archiveLoad(code, this.archivePin);
					if (!res || res.ok !== true) {
						const msg = res && res.error === 'not_found' ? '没有这个存档' : ((res && res.error) || '读不到存档');
						uni.showToast({ title: msg, icon: 'none' });
						return;
					}
					this.sourceLabel = '云存档 ' + String(res.code || code);
					this.previewToken(res.p);
				} catch (e) {
					errorLog.logError(e, 'profile_share.loadArchive');
					uni.showToast({ title: '读取失败', icon: 'none' });
				} finally {
					this.loading = false;
				}
			},

			/* ---------------- 生成 ---------------- */
			generateToken() {
				try {
					this.token = encodePrefs(collectPrefs());
					this.code = '';
					this.delCode = '';
				} catch (e) {
					errorLog.logError(e, 'profile_share.generateToken');
					uni.showToast({ title: '生成失败，稍后再试', icon: 'none' });
				}
			},

			copyToken() {
				if (!this.token) return;
				// 只复制一串口令，对方根本不知道这是什么、往哪粘 —— 带上说明再复制
				const text = [
					'【明日方舟通行证查询工具 · 个性化设置口令】',
					'这是我打包好的界面偏好（主题 / 布局 / 收藏…），导入方法：',
					'打开工具 →「我的」→「个性化设置分享」→ 粘贴到「恢复别人的设置」→ 读取并预览',
					'----- 口令开始 -----',
					this.token,
					'----- 口令结束 -----'
				].join('\n');
				copyText(text, '口令已复制（含说明）');
			},

			copyShareLink() {
				if (!this.token) return;
				const link = buildShareLink(this.token);
				if (!link) {
					// 当前端拿不到网页地址，退化成直接复制口令（同样带说明）
					this.copyToken();
					return;
				}
				const text = [
					'【明日方舟通行证查询工具 · 个性化设置分享】',
					'点开链接就能导入我的界面偏好（主题 / 布局 / 收藏…）：',
					link
				].join('\n');
				copyText(text, '链接已复制（含说明）');
			},

			copyCode() {
				if (!this.code) return;
				const text = [
					'【明日方舟通行证查询工具 · 个性化设置短码】' + this.code,
					'导入方法：打开工具 →「我的」→「个性化设置分享」→ 粘贴到「恢复别人的设置」→ 读取并预览（30 天内有效）'
				].join('\n');
				copyText(text, '短码已复制（含说明）');
			},

			async uploadCode() {
				if (this.uploading) return;
				if (!this.token) this.generateToken();
				if (!this.token) return;
				this.uploading = true;
				try {
					const r = await uploadProfile(this.token);
					if (!r.ok) {
						uni.showModal({
							title: '短码没生成',
							content: r.msg + '。可以直接用上面的口令分享，效果一样。',
							showCancel: false,
							confirmText: '知道了'
						});
						return;
					}
					this.code = r.code;
					this.delCode = r.del || '';
					try {
						uni.setStorageSync(CODE_CACHE, { code: r.code, del: r.del, at: Date.now() });
					} catch (e) { /* 记不住不影响使用 */ }
					uni.showToast({ title: '短码已生成', icon: 'success' });
				} catch (e) {
					errorLog.logError(e, 'profile_share.uploadCode');
					uni.showToast({ title: '上传失败', icon: 'none' });
				} finally {
					this.uploading = false;
				}
			},

			/* ---------------- 恢复 ---------------- */
			async restore() {
				const text = String(this.input || '').trim();
				if (!text) {
					uni.showToast({ title: '先粘贴口令或短码', icon: 'none' });
					return;
				}
				if (isShortCode(text)) {
					this.loading = true;
					await this.readShortCode(text);
					return;
				}
				// 复制出去的口令带了一行说明，粘回来时要能从整段文字里认出来
				const token = extractToken(text);
				if (token) {
					this.previewToken(token);
					return;
				}
				const code = extractShortCode(text);
				if (code) {
					await this.readShortCode(code);
					return;
				}
				uni.showToast({ title: '没认出这段内容', icon: 'none' });
			},

			async readShortCode(code) {
				this.loading = true;
				try {
					const r = await fetchProfile(code);
					if (!r.ok) {
						uni.showToast({ title: r.msg, icon: 'none' });
						return;
					}
					this.sourceLabel = '短码 ' + String(code).toUpperCase();
					this.previewToken(r.p);
				} catch (e) {
					errorLog.logError(e, 'profile_share.readShortCode');
					uni.showToast({ title: '读取失败', icon: 'none' });
				} finally {
					this.loading = false;
				}
			},

			previewToken(token) {
				const data = decodePrefs(token);
				if (!data) {
					uni.showToast({ title: '口令无效或已损坏', icon: 'none' });
					return;
				}
				const cur = collectPrefs();
				const list = diffPrefs(data, cur);
				if (!list.length) {
					uni.showToast({ title: '这份口令里没有可识别的设置', icon: 'none' });
					return;
				}
				this.incoming = data;
				this.diffList = list;
				this.changeCount = list.filter((d) => !d.same).length;
				this.loading = false;
				uni.pageScrollTo({ scrollTop: 0, duration: 200 });
			},

			applyIncoming() {
				if (!this.incoming || this.applying) return;
				this.applying = true;
				try {
					const r = applyPrefs(this.incoming);
					if (!r.ok) {
						uni.showToast({ title: '没有写入任何设置', icon: 'none' });
						return;
					}
					// 头像设置模块有内存缓存，写入 storage 后必须让它失效
					if (r.keys.indexOf('avatarPrefSettings') >= 0) {
						try { invalidate(); } catch (e) { /* 旧版本没有该导出 */ }
					}
					this.loadTheme();
					this.loadOwn();
					this.incoming = null;
					this.diffList = [];
					uni.showModal({
						title: '已应用',
						content: '写入了 ' + r.n + ' 项个性化设置，返回首页或设置页即可看到效果。',
						showCancel: false,
						confirmText: '好'
					});
				} catch (e) {
					errorLog.logError(e, 'profile_share.applyIncoming');
					uni.showToast({ title: '写入失败', icon: 'none' });
				} finally {
					this.applying = false;
				}
			},

			resetIncoming() {
				this.incoming = null;
				this.diffList = [];
				this.input = '';
				this.loadOwn();
			},

			/* 兼容外部跳转（预留） */
			openRaw(text) {
				openLink(text);
			}
		}
	};
</script>

<style>
	.ps {
		min-height: 100vh;
		padding: 24rpx 32rpx calc(48rpx + env(safe-area-inset-bottom));
		box-sizing: border-box;
	}
	.ps-light {
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
	.ps-dark {
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

	.ps-head {
		display: flex;
		flex-direction: column;
		gap: 12rpx;
		padding: 24rpx 8rpx 32rpx;
	}
	.ps-kicker {
		font-size: 20rpx;
		letter-spacing: 4rpx;
		color: var(--sub);
	}
	.ps-title {
		font-size: 46rpx;
		font-weight: 700;
		letter-spacing: 1rpx;
	}
	.ps-desc {
		font-size: 25rpx;
		line-height: 1.75;
		color: var(--sub);
	}

	.card {
		background: var(--card);
		border-radius: 28rpx;
		padding: 32rpx 28rpx;
		margin-bottom: 24rpx;
		display: flex;
		flex-direction: column;
		gap: 20rpx;
	}
	.card-head {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
	.card-title {
		font-size: 30rpx;
		font-weight: 700;
		letter-spacing: 1rpx;
	}
	.card-tag {
		font-size: 21rpx;
		color: var(--sub);
	}
	.card-desc,
	.card-note {
		font-size: 23rpx;
		line-height: 1.7;
		color: var(--sub);
	}

	.diff {
		display: flex;
		flex-direction: column;
	}
	.diff-row {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 20rpx;
		padding: 18rpx 0;
		border-bottom: 1rpx solid var(--line);
	}
	.diff-row:last-child {
		border-bottom: none;
	}
	.diff-name {
		display: flex;
		flex-direction: column;
		gap: 6rpx;
		flex-shrink: 0;
	}
	.diff-label {
		font-size: 26rpx;
		font-weight: 500;
	}
	.diff-group {
		font-size: 19rpx;
		color: var(--sub);
		letter-spacing: 1rpx;
	}
	.diff-value {
		display: flex;
		align-items: center;
		gap: 10rpx;
		flex: 1;
		justify-content: flex-end;
		flex-wrap: wrap;
	}
	.diff-before {
		font-size: 23rpx;
		color: var(--sub);
		text-decoration: line-through;
	}
	.diff-arrow {
		font-size: 22rpx;
		color: var(--sub);
	}
	.diff-after {
		font-size: 24rpx;
		text-align: right;
	}
	.diff-after.muted {
		color: var(--sub);
	}

	.btn-solid {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 88rpx;
		border-radius: 44rpx;
		background: var(--accent);
		margin-top: 8rpx;
	}
	.btn-solid text {
		color: var(--accent-text);
		font-size: 28rpx;
		font-weight: 600;
		letter-spacing: 2rpx;
	}
	.btn-solid.disabled {
		opacity: 0.55;
	}
	.btn-ghost {
		display: flex;
		align-items: center;
		justify-content: center;
		height: 80rpx;
		border-radius: 40rpx;
		border: 1rpx solid var(--line);
	}
	.btn-ghost text {
		font-size: 25rpx;
		color: var(--sub);
	}
	.btn-ghost.small {
		flex: 1;
		height: 72rpx;
	}
	.btn-ghost.disabled {
		opacity: 0.5;
	}
	.row-actions {
		display: flex;
		gap: 20rpx;
	}

	.token-box {
		background: var(--soft);
		border-radius: 20rpx;
		padding: 22rpx;
		display: flex;
		flex-direction: column;
		gap: 14rpx;
	}
	.token-text {
		font-size: 22rpx;
		line-height: 1.65;
		word-break: break-all;
		font-family: ui-monospace, Menlo, monospace;
	}
	.token-meta {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}
	.token-len {
		font-size: 20rpx;
		color: var(--sub);
	}
	.token-copy {
		font-size: 22rpx;
		color: var(--text);
		text-decoration: underline;
	}

	.code-box {
		border: 1rpx dashed var(--line);
		border-radius: 20rpx;
		padding: 24rpx;
		display: flex;
		flex-direction: column;
		gap: 10rpx;
		align-items: center;
	}
	.code-label {
		font-size: 21rpx;
		color: var(--sub);
	}
	.code-value {
		font-size: 56rpx;
		font-weight: 700;
		letter-spacing: 10rpx;
		font-family: ui-monospace, Menlo, monospace;
	}
	.code-foot {
		display: flex;
		align-items: center;
		gap: 24rpx;
	}
	.code-del {
		font-size: 20rpx;
		color: var(--sub);
	}
	.code-hint {
		font-size: 20rpx;
		line-height: 1.6;
		color: var(--sub);
		text-align: center;
		margin-top: 6rpx;
	}

	.input {
		width: 100%;
		box-sizing: border-box;
		min-height: 140rpx;
		background: var(--soft);
		border-radius: 20rpx;
		padding: 22rpx;
		font-size: 24rpx;
		line-height: 1.6;
		color: var(--text);
	}
	.input-ph {
		color: var(--sub);
		font-size: 23rpx;
	}
	/* 单行输入：textarea 那套 min-height 会把 input 撑成一整块 */
	.input-line {
		min-height: 0;
		height: 88rpx;
		padding: 0 22rpx;
		margin-bottom: 14rpx;
		font-size: 25rpx;
	}

	.ps-foot {
		display: block;
		font-size: 20rpx;
		line-height: 1.7;
		color: var(--sub);
		padding: 8rpx 12rpx 0;
		text-align: center;
	}
</style>
