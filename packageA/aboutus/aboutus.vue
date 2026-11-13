<template>
	<view :class="['container', 'theme-' + themeMode]">
		<!-- 顶部Logo和名称 -->
		<view class="header-section">
			<view class="logo-container">
				<image class="app-logo" src="/static/app-logo.png" mode="aspectFit"></image>
			</view>
			<text class="app-name">方舟通行证谷子查询工具</text>
			<image class="qq-logo" src="/static/tencent-channel.png"></image><text class="app-version-link" @click="gotofeedbackQQgroup()">QQ交流频道</text>
			<p></p>
			<image class="qq-logo" src="/static/QQ.png"></image><text class="app-version">反馈群：128568825</text>
			<p></p>
			<text class="app-version-link" @click="gotoNotice('697a057b1f9139d2e7b9b6a7')">免责声明</text>
			<text class="app-version-link" @click="gotoNotice('697a059bf07d9206ee9a9550')">长期公告</text>
			<text class="app-version-link" @click="gotoNotice('697a04d052987431f1b5d5a7')">隐私政策</text>
			
		</view>

		<!-- 开源贡献 -->
		<view class="section">
			<view class="section-header section-header-toggle" @click="githubSectionOpen = !githubSectionOpen">
				<text class="section-title">开源贡献</text>
				<view class="header-right">
					<text class="section-summary">{{ githubSummary }}</text>
					<text class="expand-icon">{{ githubSectionOpen ? '−' : '+' }}</text>
				</view>
			</view>
			<!-- 贡献者头像：常驻显示，不参与折叠 -->
			<view class="section-content github-avatar-zone">
				<view class="grid-container github-avatar-grid" v-if="githubContributors.length">
					<view class="contributor-item" v-for="(c, ci) in githubContributors" :key="ci">
						<view class="avatar-container">
							<image class="contributor-avatar" :src="imgSrc(c.avatar)" mode="aspectFill"></image>
						</view>
						<text class="contributor-name">{{ c.login }}</text>
						<text class="contributor-role">{{ c.count }} 次提交</text>
					</view>
				</view>
				<text class="repo-updated" v-else-if="!githubLoadFailed">贡献者头像加载中…</text>
			</view>
			<!-- 仓库数据卡：可折叠 -->
			<view class="section-content github-detail-zone" v-if="githubSectionOpen">
				<view class="repo-card" v-for="(repo, index) in githubRepos" :key="index" @click.stop="gotoGithubRepo(repo)">
					<view class="repo-head">
						<image class="repo-icon" src="/static/github.png" mode="aspectFit"></image>
						<view class="repo-info">
							<text class="repo-name">{{ repo.name }}</text>
							<text class="repo-desc">{{ repo.desc }}</text>
						</view>
						<text class="repo-arrow">›</text>
					</view>
					<view class="repo-stats">
						<view class="repo-stat">
							<text class="repo-stat-num">{{ repo.stats.stars }}</text>
							<text class="repo-stat-label">Star</text>
						</view>
						<view class="repo-stat">
							<text class="repo-stat-num">{{ repo.stats.forks }}</text>
							<text class="repo-stat-label">Fork</text>
						</view>
						<view class="repo-stat">
							<text class="repo-stat-num">{{ repo.stats.commits }}</text>
							<text class="repo-stat-label">Commit</text>
						</view>
						<view class="repo-stat">
							<text class="repo-stat-num">{{ repo.stats.contributors }}</text>
							<text class="repo-stat-label">贡献者</text>
						</view>
					</view>
				</view>
				<text class="repo-updated" v-if="githubUpdatedAt">数据更新于 {{ githubUpdatedAt }} · 点击仓库卡可{{ linkActionHint() }}</text>
				<text class="repo-fail" v-if="githubLoadFailed">实时数据加载失败，当前展示最近一次缓存</text>
			</view>
		</view>

		<!-- 玩家贡献 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">玩家贡献</text>
			</view>
			<view class="section-content">
				<view class="grid-container">
					<view 
						class="contributor-item" 
						v-for="(contributor, index) in visibleContributors" 
						:key="index"
					>
						<view class="avatar-container">
							<image class="contributor-avatar" :src="imgSrc(contributor.avatar)" mode="aspectFill"></image>
						</view>
						<text class="contributor-name">{{ contributor.name }}</text>
						<text class="contributor-role">{{ contributor.contribution }}</text>
					</view>
					
					<view class="contributor-item"  @click="onCarouselItemClick()">
						<view class="avatar-container">
							<image class="contributor-avatar avatar-glyph" src="/static/add.png" mode="aspectFit"></image>
						</view>
						<text class="contributor-name">加入贡献</text>
						<text class="contributor-role">加入玩家贡献</text>
					</view>
				</view>
				<view class="expand-section" v-if="contributors.length > 4">
					<text class="expand-text" @click="toggleExpand('contributors')">
						{{ expandedSections.contributors ? '收起' : `展开全部 ${contributors.length} 位贡献者` }}
					</text>
					<text class="expand-icon">{{ expandedSections.contributors ? '−' : '+' }}</text>
				</view>
			</view>
		</view>

		<!-- 服务支持 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">服务支持</text>
			</view>
			<view class="section-content">
				<view class="support-grid">
					<view class="support-item" @click="gotofeedback()">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/questionnaire-tool.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">问题反馈</text>
						<text class="support-desc">遇到问题或有好建议？欢迎反馈</text>
					</view>
					<view class="support-item" @click="gotoquestionnaire()">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/notice-icon.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">广告反馈</text>
						<text class="support-desc">遇到广告问题？点击给我们反馈</text>
					</view>
					<view class="support-item" @click="gotoCustomSerachWordquestionnaire()">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/questionnaire-tool.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">外号搜索词征集</text>
						<text class="support-desc">参与征集干员搜索外号词问卷</text>
					</view>
					<!-- <view class="support-item">
						<view class="support-icon-container">
							<image class="support-icon" src="/static/support-icon.png" mode="aspectFit"></image>
						</view>
						<text class="support-name">技术支持</text>
						<text class="support-desc">提供稳定的服务支持</text>
					</view> -->
				</view>
			</view>
		</view>
		
		<!-- 开发者信息 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">开发者</text>
			</view>
			<view class="section-content">
				<view class="grid-container">
					<view 
						class="contributor-item" 
						v-for="(dev, index) in visibleDevelopers" 
						:key="index"
					>
						<view class="avatar-container">
							<image class="contributor-avatar" :src="imgSrc(dev.avatar)" mode="aspectFill"></image>
						</view>
						<text class="contributor-name">{{ dev.name }}</text>
						<text class="contributor-role">{{ dev.role }}</text>
					</view>
				</view>
				<view class="expand-section" v-if="developers.length > 4">
					<text class="expand-text" @click="toggleExpand('developers')">
						{{ expandedSections.developers ? '收起' : `展开全部 ${developers.length} 位开发者` }}
					</text>
					<text class="expand-icon">{{ expandedSections.developers ? '−' : '+' }}</text>
				</view>
			</view>
		</view>

		

		<!-- 特别鸣谢 -->
		<view class="section">
			<view class="section-header">
				<text class="section-title">特别鸣谢</text>
			</view>
			<view class="section-content">
				<view class="grid-container">
					<view 
						class="contributor-item" 
						v-for="(thank, index) in visibleThanks" 
						:key="index"
					>
						<view class="avatar-container">
							<image class="contributor-avatar" :src="imgSrc(thank.avatar)" mode="aspectFill"></image>
						</view>
						<text class="contributor-name">{{ thank.name }}</text>
						<text class="contributor-role">{{ thank.role }}</text>
					</view>
				</view>
				<view class="expand-section" v-if="thanks.length > 4">
					<text class="expand-text" @click="toggleExpand('thanks')">
						{{ expandedSections.thanks ? '收起' : `展开全部 ${thanks.length} 位鸣谢` }}
					</text>
					<text class="expand-icon">{{ expandedSections.thanks ? '−' : '+' }}</text>
				</view>
			</view>
		</view>

		<!-- 版权信息 -->
		<view class="footer-section">
			<text class="copyright-text">方舟通行证谷子查询工具</text>
			<text class="copyright-text">本工具为玩家自制，与官方无关</text>
		</view>
		<ak-music-bar />
	</view>
</template>

<script>
	import errorLog from "@/utils/errorLog.js";
import { markDailyTask } from "@/utils/dailyTask.js";
import { openLink, linkActionText } from "@/utils/openLink.js";
	export default {
		data() {
			return {
				expandedSections: {
					developers: false,
					contributors: false,
					thanks: false
				},
				// 主题模式
				themeMode: 'simple',
				// 开发者数据
				developers: [
					{ avatar: 'https://cdn-img.gitcode.com/ce/dc/3c5021e04dcecfe62128e57eed8c46953ee2ec07db99d6f18d3836bebd602703.png?time=1762782076962', name: '黄金洲', role: '项目开发' },
					{ avatar: 'https://cdn-img.gitcode.com/ea/fd/e88a2fd71f3d8ae99826067868c104188f434e56ec299a166f09214b04a72c7e.png?time=1762782475487', name: '小丛子', role: '多语言功能开发' },
				],
				// 玩家贡献者数据
				contributors: [
					{ avatar: '/static/default-avatar.png', name: '虚位以待', contribution: '虚位以待' },
				],
				// 特别鸣谢数据
				thanks: [
					{ avatar: '/static/default-avatar.png', name: 'PRTS Wiki', role: '提供部分干员头像/英文名' },
					{ avatar: '/static/default-avatar.png', name: '明日方舟官方', role: '数据支持' },
					{ avatar: 'https://bkimg.cdn.bcebos.com/pic/b64543a98226cffc1e170998dd565d90f603738dbcd8?x-bce-process=image', name: '森空岛', role: '提供部分非可操控干员头像' },
					{ avatar: 'https://p.qlogo.cn/gh/1045868302/1045868302/40', name: '长沙罗德岛秘密交易中心', role: '赞助/研发'}
				],
				// GitHub 开源贡献数据
				githubRepos: [
					{
						full: 'awadwd/Arknights-Authorization-Tool',
						name: 'Arknights-Authorization-Tool',
						desc: '谷子查询工具主程序仓库',
						url: 'https://github.com/awadwd/Arknights-Authorization-Tool',
						stats: { stars: '--', forks: '--', commits: '--', contributors: '--' }
					},
					{
						full: 'awadwd/ArknightsAuthorization_Series-mirror',
						name: 'Series-mirror',
						desc: '数据仓库（Series 镜像）',
						url: 'https://github.com/awadwd/ArknightsAuthorization_Series-mirror',
						stats: { stars: '--', forks: '--', commits: '--', contributors: '--' }
					}
				],
				githubUpdatedAt: '',
				githubLoadFailed: false,
				githubSectionOpen: false
			}
		},
		computed: {
			// 可见的开发者（根据展开状态）
			visibleDevelopers() {
				return this.expandedSections.developers ? this.developers : this.developers.slice(0, 4);
			},
			// 可见的贡献者（根据展开状态）
			visibleContributors() {
				return this.expandedSections.contributors ? this.contributors : this.contributors.slice(0, 4);
			},
			// 可见的鸣谢（根据展开状态）
			visibleThanks() {
				return this.expandedSections.thanks ? this.thanks : this.thanks.slice(0, 4);
			},
			// 开源贡献折叠状态下的单行摘要
			githubSummary() {
				if (this.githubRepos.some((r) => r.stats.stars === '--')) {
					return this.githubLoadFailed ? '暂无数据' : '加载中…';
				}
				const stars = this.githubRepos.reduce((acc, r) => acc + (r.stats.stars || 0), 0);
				const commits = this.githubRepos.reduce((acc, r) => acc + (r.stats.commits || 0), 0);
				const devs = this.githubContributors.length;
				return `共 ${commits} 次提交 · ${stars} Star${devs ? ` · ${devs} 位开发者` : ''}`;
			},
			// 跨仓库合并去重后的贡献者列表（按提交次数降序）
			githubContributors() {
				const map = new Map();
				this.githubRepos.forEach((repo) => {
					(repo.stats.contribList || []).forEach((c) => {
						const prev = map.get(c.login);
						if (prev) {
							prev.count += c.count;
						} else {
							map.set(c.login, { login: c.login, avatar: c.avatar, count: c.count });
						}
					});
				});
				return Array.from(map.values()).sort((a, b) => b.count - a.count);
			}
		},
		onLoad() {
			// 每日任务打点
			markDailyTask('view_about');
			this.loadThemeSetting();
			this.getAppVersion();
			this.loadGithubStats();
			// 设置分享配置
			    wx.showShareMenu({
			      withShareTicket: true,
			      menus: ['shareAppMessage', 'shareTimeline']
			    });
			  },
			  
			  onShareAppMessage() {
			    return {
			      title: '方舟通行证谷子查询工具-关于我们', // 您可以修改这个标题
			      path: '/packageA/aboutus/aboutus', // 分享路径
			      imageUrl: '' // 如果留空，会自动使用小程序截图
			    }
			  },
			  
			  onShareTimeline() {
			    return {
			      title: '方舟通行证谷子查询工具-关于我们', // 您可以修改这个标题
			      imageUrl: '' // 如果留空，会自动使用小程序截图
			    }
			  },
		
		methods: {
			logError(e, ctx) {
				try {
					errorLog.logError(e, ctx);
				} catch (logErr) {
					console.error('[logError] storage failed:', logErr);
				}
			},

			// ===== GitHub 开源贡献 =====
			loadGithubStats() {
				const CACHE_KEY = 'github_stats_cache_v2'; // v2：含贡献者头像列表
				const TTL = 3 * 60 * 60 * 1000; // 3 小时缓存
				let cached = null;
				try {
					const raw = uni.getStorageSync(CACHE_KEY);
					if (raw && raw.time && raw.data) cached = raw;
				} catch (e) {}
				// 有缓存先展示（无论新鲜与否），再决定是否刷新
				if (cached) {
					this.applyGithubStats(cached.data, cached.time);
					if (Date.now() - cached.time < TTL) return;
				}
				this.fetchGithubStats().then(({ data, time }) => {
					this.githubLoadFailed = false;
					this.applyGithubStats(data, time);
					try { uni.setStorageSync(CACHE_KEY, { time, data }); } catch (e) {}
				}).catch((err) => {
					console.error('[github] 加载开源贡献数据失败:', err);
					// 完全没有缓存可展示时提示失败；有旧缓存则静默保留
					if (!cached) this.githubLoadFailed = true;
				});
			},

			fetchGithubStats() {
				const time = Date.now();
				return Promise.all(this.githubRepos.map((repo) => this.fetchOneRepoStats(repo.full)))
					.then((list) => ({ data: list, time }));
			},

			fetchOneRepoStats(fullName) {
				// 优先走自建 Cloudflare API（域名已在白名单，服务端缓存 GitHub 数据+代理头像）
				const selfUrl = 'https://arknightsauthorizationseriesdata-editor.pages.dev/api/github-stats?repo=' + encodeURIComponent(fullName);
				return new Promise((resolve, reject) => {
					uni.request({
						url: selfUrl,
						method: 'GET',
						timeout: 10000,
						success: (res) => {
							if (res.statusCode === 200 && res.data && res.data.stars !== undefined) {
								resolve({
									stars: res.data.stars,
									forks: res.data.forks,
									commits: res.data.commits,
									contributors: res.data.contributors,
									contribList: (res.data.contribList || []).map((c) => ({
										login: c.login,
										avatar: c.avatar || '/static/default-avatar.png',
										count: c.count || 0
									}))
								});
							} else {
								reject(new Error('self api status ' + res.statusCode));
							}
						},
						fail: (err) => reject(err)
					});
				}).catch(() => this.fetchOneRepoStatsDirect(fullName)); // 自建 API 失败时回退直连 GitHub
			},

			fetchOneRepoStatsDirect(fullName) {
				const base = 'https://api.github.com/repos/' + fullName;
				const header = { 'Accept': 'application/vnd.github+json' };
				const req = (url) => new Promise((resolve) => {
					uni.request({
						url,
						method: 'GET',
						header,
						timeout: 10000,
						success: (res) => resolve(res),
						fail: () => resolve(null)
					});
				});
				return Promise.all([
					req(base),
					req(base + '/commits?per_page=1'),
					req(base + '/contributors?per_page=1&anon=1'),
					req(base + '/contributors?per_page=8')
				]).then(([info, commits, contribs, contribList]) => {
					if (!info || info.statusCode !== 200 || !info.data) {
						throw new Error('repo api failed: ' + fullName);
					}
					// 贡献者头像列表（真实账号，含头像与提交次数）
					let contribListData = [];
					if (contribList && contribList.statusCode === 200 && Array.isArray(contribList.data)) {
						contribListData = contribList.data
							.filter((c) => c && c.login)
							.map((c) => ({ login: c.login, avatar: c.avatar_url || '/static/default-avatar.png', count: c.contributions || 0 }));
					}
					return {
						stars: info.data.stargazers_count || 0,
						forks: info.data.forks_count || 0,
						commits: this.parseLastPage(commits ? commits.header : null),
						contributors: this.parseLastPage(contribs ? contribs.header : null),
						contribList: contribListData
					};
				});
			},

			// 从 Link 响应头解析总条数（per_page=1 时最后一页页码即总数）
			parseLastPage(headerObj) {
				if (!headerObj) return '--';
				const link = headerObj.Link || headerObj.link || '';
				const m = link.match(/page=(\d+)>;\s*rel="last"/);
				return m ? Number(m[1]) : 1;
			},

			applyGithubStats(data, time) {
				if (!Array.isArray(data)) return;
				this.githubRepos.forEach((repo, i) => {
					if (data[i]) this.$set ? this.$set(repo, 'stats', data[i]) : (repo.stats = data[i]);
				});
				const d = new Date(time);
				const pad = (n) => (n < 10 ? '0' + n : '' + n);
				this.githubUpdatedAt = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
			},

			// 仓库卡的点击动作文案（App / H5 = 打开链接，小程序 = 复制链接）
			linkActionHint() {
				return linkActionText();
			},
			
			gotoGithubRepo(repo) {
				openLink(repo && repo.url);
			},
			gotofeedback() {
				// #ifdef MP-WEIXIN	
				wx.openEmbeddedMiniProgram({
				appId: 'wxebadf544ddae62cb',
				path: 'pages/webview/index?sid=24775309&hash=7022&navigateBackMiniProgram=true',});
				// #endif
				
				 // #ifdef H5  
				window.open('https://wj.qq.com/s2/24775309/7022/')
				// #endif
				
				// #ifdef APP-PLUS
				plus.runtime.openURL('https://wj.qq.com/s2/24775309/7022/')
				// #endif
			},
			
			gotofeedbackQQgroup() {
				 // #ifdef MP-WEIXIN
				    uni.previewImage({
				        urls: ['https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/image%2Fb612d42fed38d243f66dfe93aec6c0e2.png'], // 直接使用静态图片
				        current: 'https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/image%2Fb612d42fed38d243f66dfe93aec6c0e2.png', // 当前预览的图片，与urls中一致
				        success: () => {
				            console.log('图片预览成功');
				        },
				        fail: (err) => {
				            console.error('图片预览失败:', err);
				            uni.showToast({
				                title: '图片预览失败',
				                icon: 'none'
				            });
				        }
				    });
				    // #endif
					
					// #ifdef APP-PLUS
					plus.runtime.openURL('https://pd.qq.com/s/3rv2ash1h?b=9');
					// #endif
					
					// #ifdef H5
					window.open('https://pd.qq.com/s/3rv2ash1h?b=9');
					// #endif
				
			},
			
			gotogithubmainrepo() {
				openLink("https://github.com/awadwd/Arknights-Authorization-Tool");
			},
			
			gotogithubdatabaserepo() {
				openLink("https://github.com/awadwd/ArknightsAuthorization_Series-mirror");
			},
			
			// 跳转到公告页面（新增方法）
			gotoNotice(id) {
				if (id) {
					uni.navigateTo({
						url: `/packageA/more-notice/more-notice?id=${id}&isLeftPanelCollapsed=true`
					});
				} else {
					uni.navigateTo({
						url: '/packageA/more-notice/more-notice'
					});
				}
			},
			
			// 获取应用版本
			getAppVersion() {
				this.appVersion = '1.4.5.3';
			},

			// 加载主题设置
				loadThemeSetting() {
					try {
						const themeMode = uni.getStorageSync('themeMode');
						if (themeMode && (themeMode === 'simple' || themeMode === 'ak' || themeMode === 'ark' || themeMode === 'jieyuan' || themeMode === 'liquid' || themeMode === 'liquidDark')) {
							this.themeMode = themeMode;
						} else {
							this.themeMode = 'simple';
						}
						// 4月1日愚人节彩蛋 - 仅在当日首次触发
						const now = new Date();
						const month = now.getMonth() + 1;
						const day = now.getDate();
						const year = now.getFullYear();
						
						if (month === 4 && day === 1) {
							// 检查是否已在今天触发过
							const aprilFoolsTriggered = uni.getStorageSync('aprilFoolsTriggered');
							const triggeredDate = aprilFoolsTriggered ? aprilFoolsTriggered.date : null;
							const today = `${year}-04-01`;
							
							// 如果今天还没触发过，则触发
							if (triggeredDate !== today) {
								this.themeMode = 'jieyuan';
								uni.showToast({
									title:'博士，愚人节快乐~',
									icon:'error'
								});
								// 记录今天已触发
								uni.setStorageSync('aprilFoolsTriggered', { date: today, triggered: true });
							}
						}
						console.log('加载主题设置:', this.themeMode);
					} catch (e) {
						this.logError(e);
						console.error('加载主题设置失败:', e);
						this.themeMode = 'simple';
					}
				},
			
			// 切换展开状态
			toggleExpand(section) {
				this.expandedSections[section] = !this.expandedSections[section];
			},
			
			
			onCarouselItemClick() {
				// 轮播图固定跳转链接（App / H5 直接打开，小程序复制）
				openLink("https://afdian.com/a/AuthorizationSeries");
			},
			
			//跳转至微信小程序问卷页面
			gotoquestionnaire() {
				// #ifdef MP-WEIXIN
				wx.openEmbeddedMiniProgram({
				appId: 'wxebadf544ddae62cb',
				path: 'pages/webview/index?sid=25128247&hash=3578&navigateBackMiniProgram=true',});
				// #endif
				// #ifdef H5
				window.open('https://wj.qq.com/s2/24775309/7022/')
				// #endif
				
				// #ifdef APP-PLUS
				plus.runtime.openURL('https://wj.qq.com/s2/24775309/7022/')
				// #endif
			},
			
			gotoCustomSerachWordquestionnaire(){
				// #ifdef MP-WEIXIN
				wx.openEmbeddedMiniProgram({
					appId: 'wxebadf544ddae62cb',
					path: 'pages/webview/index?sid=24662080&hash=738a&navigateBackMiniProgram=true',
				});
				// #endif
				
				// #ifdef H5
				window.open('https://wj.qq.com/s2/24662080/738a/')
				// #endif
				
				// #ifdef APP-PLUS
				plus.runtime.openURL('https://wj.qq.com/s2/24662080/738a/')
				// #endif
			}
		}
	}
</script>

<style scoped>
	.container {
		padding: 30rpx;
		background-color: #f5f5f5;
		min-height: 100vh;
	}

	/* 顶部区域 */
	.header-section {
		background-color: #fff;
		border-radius: 16rpx;
		padding: 40rpx 30rpx;
		text-align: center;
		margin-bottom: 30rpx;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
	}

	.logo-container {
		width: 120rpx;
		height: 120rpx;
		border-radius: 24rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		margin: 0 auto 20rpx;
	}

	.app-logo {
		width: 60rpx;
		height: 60rpx;
	}
	
	.qq-logo {
		width: 30rpx;
		height: 30rpx;
	}

	.app-name {
		font-size: 32rpx;
		font-weight: bold;
		color: #333;
		display: block;
		margin-bottom: 10rpx;
	}

	.app-version {
		font-size: 26rpx;
		color: #999;
	}
	
	.app-version-link {
		font-size: 26rpx;
		color: #409EFF;
		margin: 0 10rpx;
		text-decoration: underline;
	}
	
	.app-version-link:active {
		opacity: 0.7;
	}

	/* 分区样式 */
	.section {
		background-color: #fff;
		border-radius: 16rpx;
		margin-bottom: 30rpx;
		overflow: hidden;
		box-shadow: 0 4rpx 20rpx rgba(0, 0, 0, 0.05);
	}

	/* 开源贡献 */
	.repo-card {
		background-color: #f8f9fa;
		border: 1rpx solid #ebeef5;
		border-radius: 16rpx;
		padding: 24rpx;
		margin-bottom: 20rpx;
	}

	.repo-card:active {
		background-color: #f0f7ff;
	}

	.repo-card:last-child {
		margin-bottom: 0;
	}

	.repo-head {
		display: flex;
		align-items: center;
		margin-bottom: 20rpx;
	}

	.repo-icon {
		width: 40rpx;
		height: 40rpx;
		margin-right: 16rpx;
	}

	.repo-info {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.repo-name {
		font-size: 26rpx;
		font-weight: bold;
		color: #333;
	}

	.repo-desc {
		font-size: 22rpx;
		color: #999;
		margin-top: 4rpx;
	}

	.repo-arrow {
		font-size: 36rpx;
		color: #c0c4cc;
	}

	.repo-stats {
		display: flex;
	}

	.repo-stat {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	.repo-stat-num {
		font-size: 30rpx;
		font-weight: bold;
		color: #409EFF;
	}

	.repo-stat-label {
		font-size: 20rpx;
		color: #999;
		margin-top: 4rpx;
	}

	.repo-updated {
		display: block;
		text-align: center;
		font-size: 20rpx;
		color: #c0c4cc;
		margin-top: 16rpx;
	}

	.repo-fail {
		display: block;
		text-align: center;
		font-size: 20rpx;
		color: #E6A23C;
		margin-top: 10rpx;
	}

	/* 开源贡献折叠头 */
	.section-header-toggle {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.header-right {
		display: flex;
		align-items: center;
	}

	.section-summary {
		font-size: 22rpx;
		color: #999;
		margin-right: 12rpx;
	}

	/* 贡献者头像常驻区 */
	.github-avatar-zone {
		padding-top: 24rpx;
		padding-bottom: 10rpx;
	}

	.github-avatar-grid {
		margin-bottom: 0;
	}

	/* 可折叠的仓库数据卡区 */
	.github-detail-zone {
		padding-top: 0;
	}

	.section-header {
		padding: 25rpx 30rpx;
		border-bottom: 1rpx solid #f0f0f0;
		background-color: #fafafa;
	}

	.section-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #333;
	}

	.section-content {
		padding: 30rpx;
	}

	/* 网格布局 */
	.grid-container {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 30rpx 20rpx;
		margin-bottom: 20rpx;
	}

	/* 贡献者项目 */
	.contributor-item {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
	}

	.avatar-container {
		width: 100rpx;
		height: 100rpx;
		border-radius: 50%;
		background-color: #f0f0f0;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-bottom: 15rpx;
		overflow: hidden;
	}

	.contributor-avatar {
		width: 100%;
		height: 100%;
		display: block;
	}

	/* 图标型头像（加号占位图）：保持图形比例并留出安全边距 */
	.avatar-glyph {
		width: 60%;
		height: 60%;
	}

	.contributor-name {
		font-size: 24rpx;
		color: #333;
		font-weight: 500;
		margin-bottom: 5rpx;
		line-height: 1.2;
	}

	.contributor-role {
		font-size: 20rpx;
		color: #666;
		line-height: 1.2;
	}

	/* 展开/收起区域 */
	.expand-section {
		display: flex;
		align-items: center;
		justify-content: center;
		padding-top: 20rpx;
		border-top: 1rpx solid #f5f5f5;
		color: #409EFF;
		font-size: 26rpx;
	}

	.expand-text {
		margin-right: 10rpx;
	}

	.expand-icon {
		font-size: 24rpx;
		font-weight: bold;
	}

	/* 服务支持网格 */
	.support-grid {
		display: grid;
		grid-template-columns: repeat(2, 1fr);
		gap: 30rpx;
	}

	.support-item {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		padding: 25rpx 20rpx;
		background-color: #f8f9fa;
		border-radius: 12rpx;
	}

	.support-icon-container {
		width: 80rpx;
		height: 80rpx;
		border-radius: 16rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-bottom: 15rpx;
	}

	.support-icon {
		width: 40rpx;
		height: 40rpx;
	}

	.support-name {
		font-size: 26rpx;
		color: #333;
		font-weight: 500;
		margin-bottom: 8rpx;
	}

	.support-desc {
		font-size: 22rpx;
		color: #666;
		line-height: 1.3;
	}

	/* 底部版权 */
	.footer-section {
		text-align: center;
		padding: 30rpx 0;
	}

	.copyright-text {
		font-size: 24rpx;
		color: #999;
		display: block;
		margin-bottom: 10rpx;
	}

	.copyright-text:last-child {
		margin-bottom: 0;
	}

	/* 响应式调整 */
	@media (max-width: 750rpx) {
		.grid-container {
			grid-template-columns: repeat(2, 1fr);
			gap: 40rpx 30rpx;
		}
		
		.support-grid {
			grid-template-columns: 1fr;
		}
	}/* ========== 界园风格 ========== */
	.container.theme-jieyuan {
		background-color: #FAF3E0;
	}
	.container.theme-jieyuan .header-section,
	.container.theme-jieyuan .about-content,
	.container.theme-jieyuan .section-card,
	.container.theme-jieyuan .footer-section,
	.container.theme-jieyuan .developers-list,
	.container.theme-jieyuan .section-item {
		background: rgba(255, 255, 255, 0.9);
		border: 1rpx solid rgba(226, 88, 132, 0.15);
	}
	.container.theme-jieyuan .app-name {
		background: linear-gradient(90deg, #e25884, #399383);
		-webkit-background-clip: text;
		-webkit-text-fill-color: transparent;
	}
	.container.theme-jieyuan .app-version,
	.container.theme-jieyuan .app-version-link { color: #333; }
	.container.theme-jieyuan .section-title,
	.container.theme-jieyuan .section-text,
	.container.theme-jieyuan .section-content { color: #333; }
	.container.theme-jieyuan .section-toggle { color: #399383; }
	.container.theme-jieyuan .copyright-text { color: #888; }
	.container.theme-jieyuan .developer-name { color: #e25884; }
</style>
