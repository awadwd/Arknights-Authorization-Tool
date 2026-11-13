<template>
	<view class="shot-wrap">
		<!-- 自定义导航（本页 navigationStyle 为 custom） -->
		<view class="shot-nav">
			<view class="nav-back" @click="goBack"><text class="nav-back-icon">‹</text></view>
			<text class="nav-title">分享图片</text>
			<view class="nav-holder"></view>
		</view>

		<view class="shot-body">
			<!-- 进度长图：屏幕上直接放导出的成品，不再用 CSS 复刻一份，
			     省掉「预览一套、导出另一套」的同步负担。
			     卡片类（今日宜挂 / 晒展柜）走 v-else 的普通视图预览，导出图由 canvas 重绘；
			     注意：v-if / v-else 之间不能夹注释，所以两段说明都写在这里 -->
			<scroll-view class="shot-scroll" v-if="payload.type === 'boxes'" scroll-y>
				<image
					v-if="shotPaths.length"
					class="shot-preview-img"
					:src="shotPaths[shotIndex]"
					mode="widthFix"
				></image>
				<view class="shot-preview-ph" v-else>
					<text class="ph-text">正在排版…</text>
				</view>
			</scroll-view>
			<view class="snap-on" v-else>
				<shot-body :payload="payload" @imgload="onImgLoad"></shot-body>
			</view>
		</view>

		<view class="shot-pager" v-if="payload.type === 'boxes' && shotPaths.length > 1">
			<view class="pager-btn" :class="{ disabled: shotIndex <= 0 }" @click="prevShot">
				<text class="pager-btn-text">上一张</text>
			</view>
			<text class="pager-num">{{ shotIndex + 1 }} / {{ shotPaths.length }}</text>
			<view class="pager-btn" :class="{ disabled: shotIndex >= shotPaths.length - 1 }" @click="nextShot">
				<text class="pager-btn-text">下一张</text>
			</view>
		</view>

		<view class="shot-tip" @click="manualRetry">
			<text class="tip-text">{{ statusText }}</text>
			<text class="tip-retry" v-if="canRetry && !shotPath">点此重试</text>
		</view>

		<view class="shot-bar">
			<view class="btn ghost" @click="goBack"><text class="btn-text">返回</text></view>
			<view class="btn" @click="onSave" :class="{ disabled: !shotPath }"><text class="btn-text">保存图片</text></view>
			<view class="btn main" @click="onShareImage" :class="{ disabled: !shotPath }"><text class="btn-text">发送图片</text></view>
		</view>
		<view class="shot-bar2">
			<button class="btn ghost card-btn" open-type="share"><text class="btn-text">分享小程序卡片</text></button>
		</view>

		<!-- 离屏画布：导出的 PNG 由它绘制。
		     不再用 Skyline 的 <snapshot>/takeSnapshot —— 真机上会因 AB 实验回退到 WebView
		     而报「webview renderer is not supported」，或因 Skyline 版本差异回调永不返回。
		     canvas 2D 与渲染引擎无关，WebView / Skyline / PC 都能出图。 -->
		<!-- #ifdef MP-WEIXIN -->
		<canvas type="2d" id="posterCanvas" class="poster-canvas"></canvas>
		<!-- #endif -->
	</view>
</template>

<script>
	import ShotBody from './shot-body.vue';
	// 小程序码内联 base64（自动生成）：canvas 加载包内路径在真机上不保证成功，
	// 这条是兜底，保证导出图里一定有码。见 scripts/gen_qr_base64.py
	import QR_DATA_URL from './qr-code.js';

	// 海报排版常量（逻辑像素，1 逻辑像素 = 2rpx，与 shot-body.vue 的样式一一对应）
	const LOGIC_W = 375;
	const PAD = 12;                       // 卡片外留白 24rpx
	const CARD_W = LOGIC_W - PAD * 2;
	const CARD_PAD_X = 15;                // 卡片内留白 30rpx
	const CONTENT_W = CARD_W - CARD_PAD_X * 2;

	const COLORS = {
		page: '#f2f3f5',
		card: '#ffffff',
		border: '#ececef',
		ink: '#1c1c1e',
		sub: '#8a8a90',
		faint: '#9a9aa0',
		faint2: '#b0b0b8',
		block: '#f7f7f9',
		tagBg: '#eaf3ff',
		tagText: '#2f7ed8',
		warnBg: '#fff7e8',
		warnLabel: '#b07a1a',
		warnText: '#8a6112',
		chipBg: '#f5f6f8',
		chipText: '#3a3a40',
		goldBg: '#fdf3d8',
		goldBorder: '#f0dfb0',
		goldText: '#8a6112',
		moreBg: '#eef1f5'
	};

	// 分享截图页（仅微信小程序走这里）
	//
	// 历史教训：这里曾用 Skyline 的 <snapshot> + takeSnapshot 截「屏上节点」，
	// 真机上翻车两次 ——
	//   ① 「takeSnapshot:fail webview renderer is not supported」：AB 实验把页面分到了 WebView；
	//   ② 回调永远不返回（Skyline 1.4.21，node=ok / hasApi=true / size 正常）。
	// 现在改成 canvas 2D 重绘：与渲染引擎无关，哪个环境都能出图。
	export default {
		components: { ShotBody },
		data() {
			return {
				payload: {},
				shotPath: '',
				shotPaths: [],
				shotIndex: 0,
				statusText: '正在生成图片…',
				canRetry: false,
				busy: false,
				attempt: 0,
				timer: null
			};
		},
		onLoad() {
			let raw = null;
			try {
				raw = uni.getStorageSync('share_shot_payload');
			} catch (e) {}
			if (raw && typeof raw === 'string') {
				try { raw = JSON.parse(raw); } catch (e2) { raw = null; }
			}
			this.payload = raw || {};
		},
		onReady() {
			if (!this.payload || !this.payload.type) {
				this.statusText = '没有可分享的内容，请返回重新生成';
				return;
			}
			// 给图片 / 字体一点时间就位再开画
			this.timer = setTimeout(() => {
				this.capture();
			}, 300);
		},
		onUnload() {
			if (this.timer) clearTimeout(this.timer);
		},
		methods: {
			onImgLoad() {},
			capture() {
				if (this.busy || !this.payload.type) return;
				this.busy = true;
				this.statusText = '正在生成图片…';
				// #ifdef MP-WEIXIN
				this.drawPoster().then((paths) => {
					this.busy = false;
					this.attempt = 0;
					const list = (Array.isArray(paths) ? paths : [paths]).filter(Boolean);
					if (!list.length) {
						this.fail('没有拿到图片文件');
						return;
					}
					this.shotPaths = list;
					this.shotIndex = 0;
					this.shotPath = list[0];
					this.canRetry = false;
					this.statusText = list.length > 1
						? ('已生成 ' + list.length + ' 张，可翻页查看后逐张保存')
						: '图片已生成，可保存到相册或发送给朋友';
					console.log('[share_shot] 已生成 ' + list.length + ' 张');
				}).catch((err) => {
					const msg = (err && (err.message || err.errMsg)) || '未知错误';
					// 画布还没挂载好属于时序问题，自己再试一次，不让用户手点
					if (msg.indexOf('画布未就绪') > -1 && this.attempt < 2) {
						this.attempt = (this.attempt || 0) + 1;
						console.warn('[share_shot] 画布未就绪，第 ' + this.attempt + ' 次重试');
						this.timer = setTimeout(() => {
							this.busy = false;
							this.capture();
						}, 400);
						return;
					}
					this.busy = false;
					this.fail(msg, err);
				});
				return;
				// #endif
				// #ifndef MP-WEIXIN
				// H5 / App 不走这一页（入口仅小程序跳转），这里只做兜底
				this.busy = false;
				this.statusText = '当前环境不支持生成图片，可用系统截屏或下方「分享小程序卡片」';
				// #endif
			},
			fail(msg, err) {
				try {
					console.warn('[share_shot] 生成失败', msg, err && (err.errMsg || err.message || ''));
				} catch (e) {}
				this.canRetry = true;
				this.statusText = '生成图片失败：' + msg + '，可点重试或用下方「分享小程序卡片」';
				uni.showToast({ title: this.statusText, icon: 'none', duration: 3600 });
			},
			getCanvasNode() {
				const pick = (res) => {
					const item = res && res[0];
					return item && item.node ? item.node : null;
				};
				return new Promise((resolve) => {
					// 先按页内作用域查；个别版本在页面级 .in(this) 查不到，退到全局再试一次
					uni.createSelectorQuery().in(this)
						.select('#posterCanvas')
						.fields({ node: true, size: true })
						.exec((res) => {
							const node = pick(res);
							if (node) {
								resolve(node);
								return;
							}
							uni.createSelectorQuery()
								.select('#posterCanvas')
								.fields({ node: true, size: true })
								.exec((res2) => resolve(pick(res2)));
						});
				});
			},
			/**
			 * 把图片加载成 canvas 可用的 Image。
			 * srcs 传数组时按顺序尝试，第一个成功的算；全部失败才返回 null。
			 *
			 * 为什么本地图要给多个候选：canvas.createImage() 加载 /static/ 包内路径
			 * 在真机上不一定成功（分包页面尤其），而 getImageInfo 返回的临时路径
			 * 形如 http://tmp/xxx，会被 canvas 当成网络请求拦掉。
			 * 所以本地图最后一定挂 base64 data URI 兜底。
			 */
			loadCanvasImage(canvas, srcs) {
				const list = (Array.isArray(srcs) ? srcs : [srcs]).filter(Boolean);
				if (!list.length) return Promise.resolve(null);
				return new Promise((resolve) => {
					let settled = false;
					const done = (img) => {
						if (settled) return;
						settled = true;
						resolve(img || null);
					};
					const tryAt = (i) => {
						if (i >= list.length) {
							console.warn('[share_shot] 图片全部失败：' + list.length + ' 个候选');
							done(null);
							return;
						}
						const src = String(list[i]);
						// base64 可能很长，日志里只留前缀
						const tag = src.slice(0, 32);
						let img = null;
						try {
							img = canvas.createImage();
						} catch (e) {
							console.warn('[share_shot] createImage 失败 ' + tag);
							tryAt(i + 1);
							return;
						}
						// 单张图最多等 3 秒，超时就换下一个候选，别让整页卡住
						const timer = setTimeout(() => {
							console.warn('[share_shot] 加载超时，换下一个 ' + tag);
							tryAt(i + 1);
						}, 3000);
						img.onload = () => {
							clearTimeout(timer);
							done(img);
						};
						img.onerror = (e) => {
							clearTimeout(timer);
							console.warn('[share_shot] 加载失败，换下一个 ' + tag
								+ ' ' + ((e && e.errMsg) || ''));
							tryAt(i + 1);
						};
						img.src = src;
					};
					tryAt(0);
				});
			},
			drawPoster() {
				return this.getCanvasNode().then((canvas) => {
					if (!canvas) return Promise.reject(new Error('画布未就绪'));
					const ctx = canvas.getContext('2d');
					if (!ctx) return Promise.reject(new Error('画布初始化失败'));

					const p = this.payload || {};
					let dpr = 2;
					try {
						const info = wx.getWindowInfo ? wx.getWindowInfo() : wx.getSystemInfoSync();
						dpr = Math.min(info.pixelRatio || 2, 3);
					} catch (e) {}
					// 进度长图动辄 1000+ 逻辑像素高，dpr 压到 2：
					// iOS 画布上限约 4096，1700 * 3 = 5100 会直接绘制失败
					if (p.type === 'boxes') dpr = 2;

					return Promise.all([
						this.loadCanvasImage(canvas, p.avatar),
						this.loadCanvasImage(canvas, ['/static/wechat-code.jpg', QR_DATA_URL])
					]).then((imgs) => {
						const res = { avatar: imgs[0], qr: imgs[1] };
						console.log('[share_shot] avatar=' + (imgs[0] ? 'ok' : 'none')
							+ ' qr=' + (imgs[1] ? 'ok' : 'none') + ' type=' + p.type);

						// 进度长图按高度切多片；卡片类只有一片
						const pages = p.type === 'boxes' ? this.planBoxPages(ctx, p) : [[]];
						const out = [];
						let chain = Promise.resolve();
						pages.forEach((page, i) => {
							chain = chain
								.then(() => this.renderPage(canvas, ctx, p, page, i, pages.length, dpr, res))
								.then((path) => {
									out.push(path);
									// 同一个画布连着导出，太快偶发空文件，隔一拍再画下一页
									return new Promise((r) => setTimeout(r, 120));
								});
						});
						return chain.then(() => {
							console.log('[share_shot] 出图 ' + out.length + ' 张');
							return out;
						});
					});
				});
			},
			/**
			 * 画一页并导出。
			 * 换页必须重设 canvas.width/height —— 赋值会清空画布并重置变换，
			 * 所以每页都从 setTransform(1,0,0,1,0,0) 重新开始，避免 scale 累积。
			 */
			renderPage(canvas, ctx, p, page, index, total, dpr, res) {
				ctx.setTransform(1, 0, 0, 1, 0, 0);
				const mm = this.paint(ctx, p, false, null, null, { page: page, index: index, total: total });
				canvas.width = Math.ceil(LOGIC_W * dpr);
				canvas.height = Math.ceil(mm.totalH * dpr);
				ctx.setTransform(1, 0, 0, 1, 0, 0);
				ctx.scale(dpr, dpr);
				this.paint(ctx, p, true, mm, res, { page: page, index: index, total: total });
				return new Promise((resolve, reject) => {
					wx.canvasToTempFilePath({
						canvas: canvas,
						x: 0,
						y: 0,
						width: canvas.width,
						height: canvas.height,
						destWidth: canvas.width,
						destHeight: canvas.height,
						fileType: 'png',
						success: (r) => resolve(r.tempFilePath),
						fail: (e) => reject(new Error('导出失败 ' + ((e && e.errMsg) || '')))
					});
				});
			},

			// ---------- 进度长图：chip 分行 / 块高 / 分片 ----------
			boxChipStyle(st) {
				if (st === 'out') return { text: '#b4550f', bg: '#fff1e2', bd: '#f6d5b3' };
				if (st === 'swap') return { text: '#1f6fbf', bg: '#e8f2fd', bd: '#c8e0f8' };
				if (st === 'want') return { text: '#7a4bc4', bg: '#f3ecfd', bd: '#dccbf8' };
				if (st === 'pair') return { text: '#3f9c5a', bg: '#eaf7ee', bd: '#cfe9d8' };
				if (st === 'e2') return { text: '#2f7ed8', bg: '#eef4fd', bd: '#d5e4f7' };
				if (st === 'e1') return { text: '#b07a1a', bg: '#fdf3e2', bd: '#f0e0c0' };
				return { text: '#9a9aa0', bg: '#f4f5f7', bd: '#e8e9ec' };
			},
			boxChipRows(ctx, b) {
				const chips = (b.chips || []).slice();
				if (b.hidden > 0) chips.push({ name: '+' + b.hidden, st: 'none' });
				const rows = [];
				let row = [];
				let x = 0;
				for (let i = 0; i < chips.length; i++) {
					const c = chips[i];
					ctx.font = '10px sans-serif';
					const w = Math.ceil(ctx.measureText(String(c.name)).width) + 12;
					if (row.length && x + w > CONTENT_W) {
						rows.push(row);
						row = [];
						x = 0;
					}
					row.push({ name: c.name, st: c.st, w: w });
					x += w + 5;
				}
				if (row.length) rows.push(row);
				return rows;
			},
			// 出物模式下判断「有没有内容」看标记，不看点亮数
			boxIsActive(b) {
				if (!b) return false;
				if (typeof b.active === 'boolean') return b.active;
				return !!b.owned;
			},

			// 与 paint() 里 boxes 分支的推进量严格一致
			boxBlockH(ctx, b) {
				if (!this.boxIsActive(b)) return 22;
				return 40 + this.boxChipRows(ctx, b).length * 21;
			},
			/** 返回 [[box, ...], ...]，每片一个数组。有进度的盒在前，空盒网格挂最后一页 */
			planBoxPages(ctx, p) {
				const HEAD_FOOT = 254;   // 页头 151 + 页脚 103（含卡片外留白）
				const MAX = 1900;        // dpr=2 时 y 方向 3800 < iOS 4096 上限
				const all = p.boxes || [];
				const active = [];
				const empty = [];
				for (let i = 0; i < all.length; i++) {
					if (this.boxIsActive(all[i])) active.push(all[i]);
					else empty.push(all[i]);
				}
				// 空盒用 3 列网格：92 个空盒 ≈ 31 行 ≈ 558，比每盒一行（2024）省太多
				const gridH = empty.length ? (24 + Math.ceil(empty.length / 3) * 18) : 0;

				const pages = [];
				let page = [];
				let used = HEAD_FOOT;
				for (let i = 0; i < active.length; i++) {
					const h = this.boxBlockH(ctx, active[i]);
					if (page.length && used + h > MAX) {
						pages.push(page);
						page = [];
						used = HEAD_FOOT;
					}
					page.push(active[i]);
					used += h;
				}
				if (empty.length) {
					if (page.length && used + gridH > MAX) {
						pages.push(page);
						page = [];
					}
					for (let i = 0; i < empty.length; i++) page.push(empty[i]);
				}
				pages.push(page);
				return pages;
			},
			/**
			 * 卡片排版 + 绘制。
			 * draw=false：只走一遍坐标计算并返回 { cardH, totalH }（不落笔）
			 * draw=true ：m 为上一遍的结果，按真实尺寸铺底色、卡片、正文、页脚
			 */
			paint(ctx, p, draw, m, res, extra) {
				const C = COLORS;
				const cx = PAD + CARD_PAD_X;
				const cw = CONTENT_W;
				const right = cx + cw;

				const setFont = (size, bold) => {
					ctx.font = (bold ? 'bold ' : '') + size + 'px sans-serif';
				};
				const txt = (s, x, y, size, color, bold, align) => {
					if (!draw) return;
					setFont(size, bold);
					ctx.fillStyle = color;
					ctx.textAlign = align || 'left';
					ctx.textBaseline = 'top';
					ctx.fillText(String(s == null ? '' : s), x, y);
				};
				// 字间距（canvas 没有 letter-spacing，只能逐字画）
				const spaced = (s, x, y, size, color, gap) => {
					if (!draw) return;
					setFont(size, false);
					ctx.fillStyle = color;
					ctx.textAlign = 'left';
					ctx.textBaseline = 'top';
					const t = String(s || '');
					let dx = x;
					for (let i = 0; i < t.length; i++) {
						ctx.fillText(t.charAt(i), dx, y);
						dx += ctx.measureText(t.charAt(i)).width + gap;
					}
				};
				const measure = (s, size, bold) => {
					setFont(size, bold);
					return ctx.measureText(String(s == null ? '' : s)).width;
				};
				const wrap = (s, maxW, size, bold) => {
					setFont(size, bold);
					const out = [];
					const t = String(s == null ? '' : s);
					let line = '';
					for (let i = 0; i < t.length; i++) {
						const ch = t.charAt(i);
						if (ch === '\n') {
							out.push(line);
							line = '';
							continue;
						}
						const test = line + ch;
						if (line && ctx.measureText(test).width > maxW) {
							out.push(line);
							line = ch === ' ' ? '' : ch;
						} else {
							line = test;
						}
					}
					if (line) out.push(line);
					return out;
				};
				const rr = (x, y, w, h, r) => {
					const rad = Math.min(r, w / 2, h / 2);
					ctx.beginPath();
					ctx.moveTo(x + rad, y);
					ctx.lineTo(x + w - rad, y);
					ctx.arc(x + w - rad, y + rad, rad, -Math.PI / 2, 0);
					ctx.lineTo(x + w, y + h - rad);
					ctx.arc(x + w - rad, y + h - rad, rad, 0, Math.PI / 2);
					ctx.lineTo(x + rad, y + h);
					ctx.arc(x + rad, y + h - rad, rad, Math.PI / 2, Math.PI);
					ctx.lineTo(x, y + rad);
					ctx.arc(x + rad, y + rad, rad, Math.PI, Math.PI * 1.5);
					ctx.closePath();
				};
				const box = (x, y, w, h, r, color) => {
					if (!draw) return;
					rr(x, y, w, h, r);
					ctx.fillStyle = color;
					ctx.fill();
				};
				const stroke = (x, y, w, h, r, color) => {
					if (!draw) return;
					rr(x, y, w, h, r);
					ctx.strokeStyle = color;
					ctx.lineWidth = 1;
					ctx.stroke();
				};

				if (draw) {
					ctx.fillStyle = C.page;
					ctx.fillRect(0, 0, LOGIC_W, m.totalH);
					box(PAD, PAD, CARD_W, m.cardH, 12, C.card);
					stroke(PAD, PAD, CARD_W, m.cardH, 12, C.border);
				}

				let cy = PAD + 18;

				if (p.type === 'daily') {
					spaced('今日宜挂', cx, cy, 11, C.faint, 2);
					cy += 16;
					txt(p.catLabel || '今日推荐', cx, cy + 4, 22, C.ink, true);
					cy += 34;

					if (p.special === 'lucky') {
						const rowTop = cy + 14;
						const rowH = 100;
						box(cx, rowTop, cw, rowH, 9, C.block);
						txt('✨', cx + cw / 2, rowTop + (rowH - 46) / 2, 40, C.ink, false, 'center');
						cy = rowTop + rowH;
					} else {
						const rowTop = cy + 14;
						const pad = 12;
						const av = 54;
						const infoX = cx + pad + av + 12;
						const nameH = 24;
						const tagH = p.tag ? 20 : 0;
						const boxLineH = p.box ? 18 : 0;
						const infoH = nameH + (tagH ? tagH + 5 : 0) + (boxLineH ? boxLineH + 5 : 0);
						const rowH = Math.max(av, infoH) + pad * 2;
						box(cx, rowTop, cw, rowH, 9, C.block);

						// 头像：圆形裁切，没图就用灰底问号
						const acx = cx + pad + av / 2;
						const acy = rowTop + rowH / 2;
						if (draw) {
							ctx.save();
							ctx.beginPath();
							ctx.arc(acx, acy, av / 2, 0, Math.PI * 2);
							ctx.closePath();
							ctx.fillStyle = '#eceef2';
							ctx.fill();
							ctx.clip();
							if (res && res.avatar) {
								ctx.drawImage(res.avatar, acx - av / 2, acy - av / 2, av, av);
							}
							ctx.restore();
							if (!(res && res.avatar)) {
								txt('?', acx, acy - 12, 22, C.faint2, false, 'center');
							}
						}

						let iy = rowTop + (rowH - infoH) / 2;
						txt(p.name || '', infoX, iy, 17, C.ink, true);
						iy += nameH;
						if (tagH) {
							iy += 5;
							const tw = measure(p.tag, 10, false) + 12;
							box(infoX, iy, tw, tagH, 4, C.tagBg);
							txt(p.tag, infoX + 6, iy + 3, 10, C.tagText);
							iy += tagH;
						}
						if (boxLineH) {
							iy += 5;
							const label = '推荐盒号：';
							txt(label, infoX, iy, 11, C.sub);
							txt(p.box, infoX + measure(label, 11, false), iy - 1, 12, C.ink, true);
							iy += boxLineH;
						}
						cy = rowTop + rowH;
					}

					// 推荐理由
					const rTop = cy + 12;
					const rLines = wrap(p.reason || '', cw - 24, 13, false);
					const rCount = Math.max(1, rLines.length);
					const rH = 11 + 15 + 4 + rCount * 20 + 11;
					box(cx, rTop, cw, rH, 8, C.block);
					txt('推荐理由', cx + 12, rTop + 11, 11, C.faint);
					for (let i = 0; i < rCount; i++) {
						txt(rLines[i] || '', cx + 12, rTop + 30 + i * 20, 13, '#3a3a40');
					}
					cy = rTop + rH;

					// 慎重提示
					if (p.warn) {
						const wTop = cy + 8;
						const wLines = wrap(p.warn, cw - 24, 12, false);
						const wCount = Math.max(1, wLines.length);
						const wH = 9 + 15 + 3 + wCount * 18 + 9;
						box(cx, wTop, cw, wH, 8, C.warnBg);
						txt('⚠ 慎重', cx + 12, wTop + 9, 11, C.warnLabel);
						for (let i = 0; i < wCount; i++) {
							txt(wLines[i] || '', cx + 12, wTop + 27 + i * 18, 12, C.warnText);
						}
						cy = wTop + wH;
					}

					txt('结果随机生成，仅供娱乐', cx + cw / 2, cy + 11, 10, C.faint2, false, 'center');
					cy += 25;
				} else if (p.type === 'boxes') {
					// ===== 每盒进度（长图，可能多片）=====
					// 口径：顶部只讲「位干员 + 覆盖几个盒」，不拿「件 / 全服总量」当分母 ——
					// 全服 975 件时，4 件会四舍五入成 0%，看着像算错了。
					const page = (extra && extra.page) || [];
					const pageIndex = (extra && extra.index) || 0;
					const pageTotal = (extra && extra.total) || 1;

					const isTrade = p.mode === 'trade';
					const tOut = p.tradeOut || 0;
					const tSwap = p.tradeSwap || 0;
					const tNeed = p.tradeWant || 0;
					const covered = isTrade ? ((p.boxes || []).length) : (p.opened || 0);

					spaced(isTrade ? '出物清单' : '通行证展柜', cx, cy, 11, C.faint, 2);
					cy += 16;
					const bigVal = isTrade ? (tOut + tSwap + tNeed) : (p.ownedCount || 0);
					const bigStr = String(bigVal);
					txt(bigStr, cx, cy + 2, 30, C.ink, true);
					txt(isTrade ? '项标记' : '位干员', cx + measure(bigStr, 30, true) + 6, cy + 13, 12, C.faint);
					cy += 34;

					txt(isTrade
						? ('出 ' + tOut + ' · 换 ' + tSwap + ' · 要 ' + tNeed + ' · 覆盖 ' + covered + '/' + (p.boxCount || 0) + ' 个盒')
						: ('覆盖 ' + covered + '/' + (p.boxCount || 0) + ' 个盒 · 共 ' + (p.sumOwned || 0) + ' 件'),
						cx, cy, 11, C.faint);
					cy += 18;

					const barH = 5;
					box(cx, cy, cw, barH, barH / 2, '#eceef0');
					const ratio = p.boxCount > 0 ? Math.min(1, covered / p.boxCount) : 0;
					if (ratio > 0) box(cx, cy, Math.max(barH, cw * ratio), barH, barH / 2, C.tagText);
					cy += barH + 12;

					if (draw) {
						ctx.beginPath();
						ctx.moveTo(cx, cy);
						ctx.lineTo(right, cy);
						ctx.strokeStyle = C.border;
						ctx.lineWidth = 1;
						ctx.stroke();
					}
					cy += 14;

					txt(isTrade ? '盒内标记' : '每盒进度', cx, cy, 11, C.ink, true);
					if (pageTotal > 1) {
						txt((pageIndex + 1) + ' / ' + pageTotal, right, cy + 1, 10, C.faint2, false, 'right');
					}
					cy += 22;

					let gridDone = false;
					for (let i = 0; i < page.length; i++) {
						const b = page[i];

						// 空盒只画一次，统一用 3 列网格，别一盒一行
						if (!this.boxIsActive(b)) {
							if (gridDone) continue;
							gridDone = true;
							const rest = page.length - i;
							const allEmpty = (typeof p.emptyCount === 'number' && p.emptyCount > rest) ? p.emptyCount : rest;
							txt('另有 ' + allEmpty + (isTrade ? ' 个盒没有出物标记' : ' 个盒还没有开张'), cx, cy, 11, C.faint);
							cy += 24;
							const colW = Math.floor(cw / 3);
							for (let k = i; k < page.length; k += 3) {
								for (let c = 0; c < 3 && k + c < page.length; c++) {
									const eb = page[k + c];
									const ex = cx + c * colW;
									txt(eb.name, ex, cy, 10, C.faint);
									txt('0/' + eb.total, ex + colW - 10, cy, 9, C.faint2, false, 'right');
								}
								cy += 18;
							}
							cy += 6;
							break;
						}

						txt(b.name, cx, cy, 11, C.ink, true);
						if (isTrade) {
							const seg = [];
							if (b.outN > 0) seg.push('出 ' + b.outN);
							if (b.swapN > 0) seg.push('换 ' + b.swapN);
							if (b.needN > 0) seg.push('要 ' + b.needN);
							txt(seg.join(' · '), right, cy + 1, 10, C.sub, false, 'right');
							cy += 20;
						} else {
							txt((b.pct || 0) + '%', right, cy, 10, C.tagText, true, 'right');
							txt(b.owned + '/' + b.total, right - 38, cy + 1, 10, C.sub, false, 'right');
							cy += 15;

							box(cx, cy, cw, 6, 3, '#eceef0');
							const bw = b.total > 0 ? Math.max(3, cw * Math.min(1, b.owned / b.total)) : 0;
							if (bw > 0) box(cx, cy, bw, 6, 3, b.owned >= b.total ? '#3f9c5a' : C.tagText);
							cy += 13;
						}

						const rows = this.boxChipRows(ctx, b);
						const chipH = 16;
						for (let r = 0; r < rows.length; r++) {
							const row = rows[r];
							let x = cx;
							for (let k = 0; k < row.length; k++) {
								const c = row[k];
								const cs = this.boxChipStyle(c.st);
								box(x, cy, c.w, chipH, 4, cs.bg);
								stroke(x, cy, c.w, chipH, 4, cs.bd);
								txt(c.name, x + c.w / 2, cy + (chipH - 13) / 2, 10, cs.text, false, 'center');
								x += c.w + 5;
							}
							cy += 21;
						}
						cy += 12;
					}

					// 空展柜：给一句人话，别只留一个「每盒进度」标题
					if (!page.length) {
						const eTop = cy;
						box(cx, eTop, cw, 64, 8, C.block);
						txt(isTrade ? '出物清单还是空的' : '还没有点亮任何通行证', cx + cw / 2, eTop + 22, 12, C.sub, false, 'center');
						txt(isTrade ? '在出物模式点干员标「出 / 换 / 要」' : '去展柜勾选你已收集的干员', cx + cw / 2, eTop + 44, 10, C.faint2, false, 'center');
						cy = eTop + 64;
					} else if (!gridDone && p.emptyCount > 0) {
						// 勾了「只生成有进度」时 page 里没有空盒，也要告诉用户还剩多少没开张
						txt('另有 ' + p.emptyCount + (isTrade ? ' 个盒没有出物标记' : ' 个盒还没有开张'), cx, cy, 11, C.faint2);
						cy += 20;
					}
				} else {
					// ===== 展柜概览 =====
					// 分享图只承担一件事：让人一眼看到「收集了多少」。所以数字要大、
					// 完成度要可视化，chip 只是佐证。旧版把数字塞在一行灰字里，
					// 底部又重复一遍「共 N 位已点亮」，信息密度低还自相重复。
					spaced('通行证展柜', cx, cy, 11, C.faint, 2);
					cy += 16;
					txt('我的通行证展柜', cx, cy + 4, 22, C.ink, true);
					cy += 36;

					// owned：已点亮数；totalChars：干员总数（0 表示还没加载到名单）
					const owned = typeof p.owned === 'number' ? p.owned : (p.total || 0);
					const totalChars = p.totalChars || 0;
					const names = (p.names && p.names.length) ? p.names : [];

					if (totalChars > 0) {
						const bigY = cy + 2;
						const ownedStr = String(owned);
						txt(ownedStr, cx, bigY, 30, C.ink, true);
						const wBig = measure(ownedStr, 30, true);
						txt('/ ' + totalChars, cx + wBig + 5, bigY + 13, 12, C.faint);
						const pct = Math.max(0, Math.min(100, Math.round(owned / totalChars * 100)));
						txt(pct + '%', right, bigY + 13, 12, C.faint, false, 'right');
						cy = bigY + 38;

						// 进度条：用主题蓝填充，不引入新配色
						const barH = 5;
						box(cx, cy, cw, barH, barH / 2, '#eceef0');
						const fillW = Math.max(0, Math.min(cw, cw * owned / totalChars));
						if (fillW > 1) box(cx, cy, Math.max(barH, fillW), barH, barH / 2, C.tagText);
						cy += barH + 11;
					} else {
						// 拿不到总名单（老数据 / 还没加载完），退回纯文字，不做假进度
						if (p.sub) {
							txt(p.sub, cx, cy + 4, 12, C.sub);
							cy += 21;
						}
					}

					// 想要 / 出物：一行小字，和点亮数分开
					const bits = [];
					if (p.wanted != null) bits.push('想要 ' + p.wanted);
					if (p.trade != null) bits.push('出物 ' + p.trade);
					if (bits.length) {
						txt(bits.join(' · '), cx, cy, 11, C.faint);
						cy += 19;
					}

					if (names.length) {
						const chipH = 26;
						const gapX = 6;
						const gapY = 6;
						let lineTop = cy + 10;
						let x = cx;
						const drawChip = (label, gold, wide) => {
							const nameW = measure(label, 11, false);
							const tagW = gold ? (6 + measure('精二', 9, false)) : 0;
							const w = 8 + nameW + tagW + 8;
							if (x + w > right && x > cx) {
								x = cx;
								lineTop += chipH + gapY;
							}
							box(x, lineTop, w, chipH, 6, gold ? C.goldBg : (wide ? C.moreBg : C.chipBg));
							if (!gold && !wide) stroke(x, lineTop, w, chipH, 6, C.border);
							if (gold) stroke(x, lineTop, w, chipH, 6, C.goldBorder);
							txt(label, x + 8, lineTop + (chipH - 15) / 2, 11, gold ? C.goldText : C.chipText);
							if (gold) {
								txt('精二', x + 8 + nameW + 6, lineTop + (chipH - 13) / 2, 9, C.goldText);
							}
							x += w + gapX;
						};
						for (let i = 0; i < names.length; i++) {
							const item = names[i] || {};
							drawChip(String(item.name || ''), !!item.elite2, false);
						}
						if (p.rest > 0) drawChip('+' + p.rest, false, true);

						const chipsBottom = lineTop + chipH;
						// marginTop 20rpx + 上边框 + paddingTop 20rpx
						const sumTop = chipsBottom + 20;
						if (draw) {
							ctx.beginPath();
							ctx.moveTo(cx, sumTop);
							ctx.lineTo(right, sumTop);
							ctx.strokeStyle = C.border;
							ctx.lineWidth = 1;
							ctx.stroke();
						}
						// 底部锁死在「精一 / 精二」构成上：总数和大数字已经说过了，
						// 再写一遍「共 N 位已点亮」纯属重复
						const plainCount = Math.max(0, owned - (p.elite2Count || 0));
						txt('精一 ' + plainCount + ' 位', cx, sumTop + 10, 11, C.faint);
						txt('精二 ' + (p.elite2Count || 0) + ' 位', right, sumTop + 10, 11, C.faint, false, 'right');
						cy = sumTop + 25;
					} else {
						// 空态：一个都没点亮时，别留一块白
						const eTop = cy + 12;
						const eH = 88;
						box(cx, eTop, cw, eH, 8, C.block);
						txt('还没有点亮任何通行证', cx + cw / 2, eTop + 28, 13, C.sub, false, 'center');
						txt('去展柜勾选你已收集的干员', cx + cw / 2, eTop + 52, 11, C.faint2, false, 'center');
						cy = eTop + eH;
					}
				}

				// 卡片下边界 + 页脚（小程序码）
				const cardBottom = cy + 14;
				const realCardH = cardBottom - PAD;
				const footTop = cardBottom + 11;
				const qrSize = 66;
				const qrX = PAD + 4;
				const tX = qrX + qrSize + 9;
				const tMax = (LOGIC_W - PAD) - tX;
				const titleLines = wrap(p.qrTitle || '微信扫码 · 方舟通行证谷子查询工具', tMax, 12, false);
				const textH = Math.max(1, titleLines.length) * 17 + 4 + 14;
				const footH = Math.max(qrSize, textH);
				const totalH = footTop + footH + PAD;

				if (draw) {
					if (res && res.qr) {
						box(qrX, footTop, qrSize, qrSize, 6, '#ffffff');
						const iw = res.qr.width || qrSize;
						const ih = res.qr.height || qrSize;
						const s = Math.min(qrSize / iw, qrSize / ih);
						const dw = iw * s;
						const dh = ih * s;
						ctx.drawImage(res.qr, qrX + (qrSize - dw) / 2, footTop + (qrSize - dh) / 2, dw, dh);
					} else {
						// 包内路径和 base64 都挂了才会走到这（正常不该出现），
						// 画个占位块，避免导出图里留一块莫名空白
						box(qrX, footTop, qrSize, qrSize, 6, '#eceef1');
						txt('小程序码', qrX + qrSize / 2, footTop + qrSize / 2 - 5, 9, '#9a9ba5', false, 'center');
					}
					let ty = footTop + Math.max(0, (footH - textH) / 2);
					for (let i = 0; i < titleLines.length; i++) {
						txt(titleLines[i], tX, ty, 12, '#3a3a40');
						ty += 17;
					}
					txt('数据只存在本机，不上传服务器', tX, ty + 4, 10, C.faint);
				}

				return { cardH: realCardH, totalH: totalH };
			},
			prevShot() {
				if (this.shotIndex <= 0) return;
				this.shotIndex -= 1;
				this.shotPath = this.shotPaths[this.shotIndex] || '';
			},
			nextShot() {
				if (this.shotIndex >= this.shotPaths.length - 1) return;
				this.shotIndex += 1;
				this.shotPath = this.shotPaths[this.shotIndex] || '';
			},
			manualRetry() {
				if (this.busy || this.shotPath || !this.payload.type) return;
				this.capture();
			},
			onSave() {
				if (!this.shotPath) {
					uni.showToast({ title: '图片还没生成好', icon: 'none' });
					return;
				}
				uni.saveImageToPhotosAlbum({
					filePath: this.shotPath,
					success: () => { uni.showToast({ title: '已保存到相册' }); },
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
						if (msg.indexOf('cancel') === -1) uni.showToast({ title: '保存失败', icon: 'none' });
					}
				});
			},
			onShareImage() {
				if (!this.shotPath) {
					uni.showToast({ title: '图片还没生成好', icon: 'none' });
					return;
				}
				// #ifdef MP-WEIXIN
				if (wx.showShareImageMenu) {
					wx.showShareImageMenu({
						path: this.shotPath,
						fail: () => { uni.showToast({ title: '可先保存图片再发送', icon: 'none' }); }
					});
					return;
				}
				// #endif
				uni.showToast({ title: '当前版本不支持，可先保存图片', icon: 'none' });
			},
			goBack() {
				const pages = getCurrentPages();
				if (pages.length > 1) uni.navigateBack();
				else uni.reLaunch({ url: '/pages/daily_recommend/daily_recommend' });
			}
		},
		onShareAppMessage() {
			const p = this.payload || {};
			return {
				title: p.shareTitle || '方舟通行证谷子查询工具',
				path: p.sharePath || '/pages/daily_recommend/daily_recommend'
			};
		}
	};
</script>

<style>
	/* 视图层约束：不用 CSS 变量 / Grid，便于 WebView 与 Skyline 两种渲染器下表现一致 */
	.shot-wrap {
		position: relative;
		width: 750rpx;
		height: 100vh;
		background-color: #f2f3f5;
		display: flex;
		flex-direction: column;
		box-sizing: border-box;
	}

	.shot-nav {
		width: 750rpx;
		padding: 20rpx 24rpx;
		box-sizing: border-box;
		display: flex;
		flex-direction: row;
		align-items: center;
		justify-content: space-between;
		background-color: #f2f3f5;
	}

	.nav-back {
		width: 64rpx;
		height: 64rpx;
		border-radius: 32rpx;
		background-color: #ffffff;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.nav-back-icon {
		font-size: 40rpx;
		line-height: 40rpx;
		color: #1c1c1e;
	}

	.nav-title {
		font-size: 30rpx;
		font-weight: bold;
		color: #1c1c1e;
	}

	.nav-holder {
		width: 64rpx;
		height: 64rpx;
	}

	.shot-body {
		position: relative;
		flex: 1;
		width: 750rpx;
		padding: 8rpx 24rpx 0;
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
		justify-content: center;
	}

	/* 屏上预览：只是给人看的，导出图由下面的 canvas 重绘，两者互不影响 */
	.snap-on {
		width: 100%;
	}

	/* 进度长图：直接看成品，必须 flex:1 + height:0 才能在父级里滚动 */
	.shot-scroll {
		flex: 1;
		width: 100%;
		height: 0;
	}

	.shot-preview-img {
		width: 100%;
		border-radius: 16rpx;
		display: block;
	}

	.shot-preview-ph {
		width: 100%;
		height: 400rpx;
		border-radius: 16rpx;
		background-color: #eceef1;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.ph-text {
		font-size: 24rpx;
		color: #9a9aa5;
	}

	.shot-pager {
		width: 750rpx;
		padding: 8rpx 24rpx;
		box-sizing: border-box;
		display: flex;
		flex-direction: row;
		align-items: center;
		justify-content: center;
	}

	.pager-btn {
		min-width: 132rpx;
		height: 56rpx;
		border-radius: 28rpx;
		background-color: #ffffff;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.pager-btn.disabled {
		opacity: 0.4;
	}

	.pager-btn-text {
		font-size: 22rpx;
		color: #3a3a40;
	}

	.pager-num {
		font-size: 22rpx;
		color: #8a8a90;
		margin: 0 24rpx;
	}

	/* 离屏画布：只用于绘制导出图，必须已挂载且有尺寸（不能 display:none） */
	.poster-canvas {
		position: fixed;
		left: -2000px;
		top: 0;
		width: 375px;
		height: 700px;
	}

	.shot-tip {
		width: 750rpx;
		padding: 16rpx 32rpx;
		box-sizing: border-box;
	}

	.shot-tip {
		display: flex;
		flex-direction: row;
		align-items: center;
	}

	.tip-text {
		flex: 1;
		font-size: 22rpx;
		color: #8a8a90;
	}

	.tip-retry {
		font-size: 22rpx;
		color: #2f7ed8;
		margin-left: 12rpx;
	}

	.shot-bar {
		width: 750rpx;
		padding: 0 24rpx 12rpx;
		box-sizing: border-box;
		display: flex;
		flex-direction: row;
	}

	.shot-bar2 {
		width: 750rpx;
		padding: 0 24rpx 24rpx;
		box-sizing: border-box;
		display: flex;
		flex-direction: row;
	}

	.btn {
		flex: 1;
		height: 82rpx;
		border-radius: 41rpx;
		background-color: #ffffff;
		border: 1rpx solid #e2e4e8;
		display: flex;
		align-items: center;
		justify-content: center;
		margin-right: 16rpx;
	}

	.btn.main {
		background-color: #1c1c1e;
		border-color: #1c1c1e;
		margin-right: 0;
	}

	.btn.ghost {
		background-color: #eef1f5;
		border-color: #e2e4e8;
	}

	.btn.card-btn {
		width: 100%;
		flex: none;
		line-height: 82rpx;
		margin-right: 0;
		font-size: 26rpx;
	}

	.btn.disabled {
		opacity: 0.45;
	}

	.btn-text {
		font-size: 26rpx;
		color: #1c1c1e;
	}

	.btn.main .btn-text {
		color: #ffffff;
	}
</style>
