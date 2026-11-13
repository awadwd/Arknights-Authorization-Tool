/**
 * App 端海报出图（canvas 重绘）
 *
 * 为什么不用 webview.draw() 截图：
 *   1. uni-app 的 vue 页面在 Android 硬件加速下，webview.draw() 拿到的是空白合成层，
 *      导出的 PNG 尺寸正确但内容全黑（2026-10-27 真机两次复现，设置软件图层也无效）；
 *   2. 页面级 "hardwareAcceleration" 不是 uni-app 支持的配置项 —— 写进 pages.json
 *      会被静默丢弃（HBuilderX 里只有浏览器 DOM 的类型定义，没有这个字段）。
 * 所以改成 canvas 自己画。这套「375 逻辑宽度 + 2 倍导出」的做法在本项目已验证：
 * pages/card_share 的「生成进度长图」就是同一套路。
 *
 * 页面只需要放一个离屏 <canvas canvas-id="xxx">，然后调用 renderXxxPoster()。
 */

const LOGIC_W = 375;
const PAD = 12;
const CARD_W = LOGIC_W - PAD * 2;
const CARD_PAD = 15;
const CW = CARD_W - CARD_PAD * 2;

const C = {
	page: '#f2f3f5',
	card: '#ffffff',
	border: '#ececef',
	ink: '#1c1c1e',
	sub: '#3a3a40',
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
	moreBg: '#eef1f5',
	blue: '#2f7ed8',
	barBg: '#eceef0'
};

/* ---------------- 文本度量（app-vue 旧 canvas 不保证 measureText，按字宽估算） ---------------- */
function charW(ch, size) {
	const code = ch.charCodeAt(0);
	if (code < 128) return size * 0.56;
	return size;
}

function textW(str, size) {
	const s = String(str == null ? '' : str);
	let w = 0;
	for (let i = 0; i < s.length; i++) w += charW(s.charAt(i), size);
	return w;
}

function wrapText(str, maxW, size) {
	const s = String(str == null ? '' : str);
	const out = [];
	let line = '';
	let w = 0;
	for (let i = 0; i < s.length; i++) {
		const ch = s.charAt(i);
		if (ch === '\n') {
			out.push(line);
			line = '';
			w = 0;
			continue;
		}
		const cw = charW(ch, size);
		if (line && w + cw > maxW) {
			out.push(line);
			line = ch === ' ' ? '' : ch;
			w = ch === ' ' ? 0 : cw;
		} else {
			line += ch;
			w += cw;
		}
	}
	if (line) out.push(line);
	return out.length ? out : [''];
}

/* ---------------- 绘图原语 ---------------- */
function setFont(ctx, size, bold) {
	ctx.font = (bold ? 'bold ' : '') + size + 'px sans-serif';
}

function txt(ctx, s, x, y, size, color, bold, align) {
	setFont(ctx, size, bold);
	ctx.fillStyle = color;
	ctx.textAlign = align || 'left';
	ctx.textBaseline = 'top';
	ctx.fillText(String(s == null ? '' : s), x, y);
}

// canvas 没有 letter-spacing，只能逐字画
function spaced(ctx, s, x, y, size, color, gap) {
	setFont(ctx, size, false);
	ctx.fillStyle = color;
	ctx.textAlign = 'left';
	ctx.textBaseline = 'top';
	const t = String(s || '');
	let dx = x;
	for (let i = 0; i < t.length; i++) {
		ctx.fillText(t.charAt(i), dx, y);
		dx += charW(t.charAt(i), size) + gap;
	}
}

function rr(ctx, x, y, w, h, r) {
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
}

function box(ctx, x, y, w, h, r, color) {
	rr(ctx, x, y, w, h, r);
	ctx.fillStyle = color;
	ctx.fill();
}

function strokeBox(ctx, x, y, w, h, r, color) {
	rr(ctx, x, y, w, h, r);
	ctx.strokeStyle = color;
	ctx.lineWidth = 1;
	ctx.stroke();
}

function drawImg(ctx, path, x, y, w, h) {
	if (!path) return false;
	try {
		ctx.drawImage(path, x, y, w, h);
		return true;
	} catch (e) {
		return false;
	}
}

/* ---------------- 图片预加载 ---------------- */
// 本地包内路径 / 网络地址都先过一遍 getImageInfo，拿到可直接 drawImage 的本地路径，
// 否则 drawImage 会因为图片还没解码而静默画不出来。
function preload(src) {
	return new Promise((resolve) => {
		if (!src) {
			resolve('');
			return;
		}
		try {
			uni.getImageInfo({
				src: src,
				success: (res) => resolve((res && res.path) || src),
				fail: () => resolve('')
			});
		} catch (e) {
			resolve('');
		}
	});
}

function toLocalUrl(path) {
	if (!path) return '';
	try {
		if (typeof plus !== 'undefined' && plus.io && plus.io.convertLocalFileSystemURL) {
			return plus.io.convertLocalFileSystemURL(path);
		}
	} catch (e) {}
	return path;
}

/* ---------------- 卡片头 / 页脚（两个海报共用） ---------------- */
function paintHeader(ctx, eyebrow, title, titleColor) {
	const CX = PAD + CARD_PAD;
	spaced(ctx, eyebrow, CX, PAD + 18, 10, C.faint, 2);
	txt(ctx, title, CX, PAD + 32, titleColor === 'big' ? 21 : 19, C.ink, true);
	return PAD + 32 + 30; // 内容起点
}

function paintFooter(ctx, qr, qrTitle, qrSub, topY) {
	// 页脚固定高度 72：分隔线 + 44 二维码 + 上下留白
	const CX = PAD + CARD_PAD;
	ctx.strokeStyle = C.border;
	ctx.lineWidth = 1;
	ctx.beginPath();
	ctx.moveTo(CX, topY);
	ctx.lineTo(CX + CW, topY);
	ctx.stroke();

	const qs = 44;
	const qy = topY + 14;
	if (!drawImg(ctx, qr, CX, qy, qs, qs)) {
		box(ctx, CX, qy, qs, qs, 6, C.block);
		txt(ctx, '码', CX + qs / 2, qy + 16, 14, C.faint2, false, 'center');
	}
	const tx = CX + qs + 12;
	txt(ctx, qrTitle || '', tx, qy + 8, 10.5, C.ink, true);
	txt(ctx, qrSub || '', tx, qy + 25, 9.5, C.faint);
}

const FOOT_H = 72;

/* ============================================================
 * ① 今日宜挂海报
 * ============================================================ */
export function renderDailyPoster(payload, canvasId, owner) {
	const p = payload || {};
	return Promise.all([
		preload(p.avatar),
		preload('/static/wechat-code.jpg')
	]).then((imgs) => {
		const qr = imgs[1];
		const avatar = imgs[0];
		const ctx = uni.createCanvasContext(canvasId, owner);
		const CX = PAD + CARD_PAD;
		const CARD_Y = PAD;

		// 先算高度：理由/慎重提示是变长文本
		const lucky = p.special === 'lucky';
		const reasonLines = wrapText(p.reason || '', CW - 24, 12.5);
		const warnLines = p.warn ? wrapText(p.warn, CW - 24, 11.5) : [];
		const opH = lucky ? 96 : 88;
		const reasonH = 11 + 15 + 4 + reasonLines.length * 19 + 11;
		const warnH = warnLines.length ? (9 + 15 + 3 + warnLines.length * 17 + 9) : 0;
		// 头部：卡片顶 -> 角色块顶（标题 34 + 26 行高 + 14 间距）
		const HEAD_H = 74;
		const cardH = HEAD_H + opH + 12 + reasonH
			+ (warnH ? 8 + warnH : 0)
			+ 34              // 免责一句
			+ FOOT_H
			+ 14;             // 卡片底留白
		const totalH = CARD_Y + cardH + 46;

		// 底色
		ctx.fillStyle = C.page;
		ctx.fillRect(0, 0, LOGIC_W, totalH);
		box(ctx, PAD, CARD_Y, CARD_W, cardH, 14, C.card);
		strokeBox(ctx, PAD, CARD_Y, CARD_W, cardH, 14, C.border);

		// 标题
		spaced(ctx, '今日宜挂', CX, CARD_Y + 18, 10, C.faint, 2);
		txt(ctx, p.catLabel || '今日推荐', CX, CARD_Y + 34, 21, C.ink, true);

		// 角色块
		let cy = CARD_Y + 34 + 30 + 14;
		box(ctx, CX, cy, CW, opH, 10, C.block);
		if (lucky) {
			txt(ctx, '✨', CX + CW / 2, cy + (opH - 40) / 2 + 4, 36, C.ink, false, 'center');
		} else {
			const av = 56;
			const acx = CX + 12 + av / 2;
			const acy = cy + opH / 2;
			ctx.save();
			ctx.beginPath();
			ctx.arc(acx, acy, av / 2, 0, Math.PI * 2);
			ctx.closePath();
			ctx.fillStyle = '#eceef2';
			ctx.fill();
			ctx.clip();
			const ok = drawImg(ctx, avatar, acx - av / 2, acy - av / 2, av, av);
			ctx.restore();
			if (!ok) txt(ctx, '?', acx, acy - 12, 22, C.faint2, false, 'center');

			const ix = CX + 12 + av + 13;
			const nameH = 22;
			const tagH = p.tag ? 19 : 0;
			const boxH = p.box ? 17 : 0;
			const infoH = nameH + (tagH ? tagH + 5 : 0) + (boxH ? boxH + 5 : 0);
			let iy = cy + (opH - infoH) / 2;
			txt(ctx, p.name || '', ix, iy, 17, C.ink, true);
			iy += nameH;
			if (tagH) {
				iy += 5;
				const tw = textW(p.tag, 9.5) + 12;
				box(ctx, ix, iy, tw, tagH, 4, C.tagBg);
				txt(ctx, p.tag, ix + 6, iy + 2.5, 9.5, C.tagText);
				iy += tagH;
			}
			if (boxH) {
				iy += 5;
				const label = '推荐盒号：';
				txt(ctx, label, ix, iy, 10.5, C.sub);
				txt(ctx, p.box, ix + textW(label, 10.5), iy - 1, 11.5, C.ink, true);
			}
		}
		cy += opH + 12;

		// 推荐理由
		box(ctx, CX, cy, CW, reasonH, 8, C.block);
		txt(ctx, '推荐理由', CX + 12, cy + 11, 10, C.faint);
		for (let i = 0; i < reasonLines.length; i++) {
			txt(ctx, reasonLines[i], CX + 12, cy + 30 + i * 19, 12.5, C.sub);
		}
		cy += reasonH;

		// 慎重提示
		if (warnLines.length) {
			cy += 8;
			box(ctx, CX, cy, CW, warnH, 8, C.warnBg);
			txt(ctx, '⚠ 慎重', CX + 12, cy + 9, 10, C.warnLabel);
			for (let i = 0; i < warnLines.length; i++) {
				txt(ctx, warnLines[i], CX + 12, cy + 27 + i * 17, 11.5, C.warnText);
			}
			cy += warnH;
		}

		// 免责
		txt(ctx, '结果随机生成，仅供娱乐', LOGIC_W / 2, cy + 14, 9.5, C.faint2, false, 'center');
		cy += 34;

		// 页脚
		paintFooter(ctx, qr, p.qrTitle || '微信扫码 · 测测你今天宜挂什么', '方舟通行证谷子查询工具', cy);

		return exportCanvas(canvasId, totalH, owner, ctx);
	});
}

/* ============================================================
 * ② 展柜海报
 * ============================================================ */
export function renderGalleryPoster(payload, canvasId, owner) {
	const p = payload || {};
	return preload('/static/wechat-code.jpg').then((qr) => {
		const ctx = uni.createCanvasContext(canvasId, owner);
		const CX = PAD + CARD_PAD;
		const CARD_Y = PAD;

		// chips 排版：先按行计算
		const names = p.names || [];
		const chipRows = [];
		let row = [];
		let rowW = 0;
		for (let i = 0; i < names.length; i++) {
			const c = names[i];
			const w = textW(c.name, 10) + (c.elite2 ? textW('精二', 8.5) + 10 : 0) + 16;
			if (row.length && rowW + w > CW) {
				chipRows.push(row);
				row = [];
				rowW = 0;
			}
			row.push({ name: c.name, elite2: !!c.elite2, w: w });
			rowW += w + 5;
		}
		if (p.rest > 0) {
			const w = textW('+' + p.rest, 10) + 16;
			if (row.length && rowW + w > CW) {
				chipRows.push(row);
				row = [];
			}
			row.push({ name: '+' + p.rest, w: w, more: true });
			rowW += w + 5;
		}
		if (row.length) chipRows.push(row);

		const CHIP_H = 22;
		const CHIP_GAP = 5;
		const chipsH = chipRows.length ? (chipRows.length * CHIP_H + (chipRows.length - 1) * CHIP_GAP) : 0;
		const noteH = names.length ? 36 : 48;

		// 头部：大数字区 98 + 分隔线占位 16
		const HEAD_H = 98 + 16;
		const cardH = HEAD_H + (chipsH ? chipsH + 14 : 0) + noteH + FOOT_H + 14;
		const totalH = CARD_Y + cardH + 46;

		ctx.fillStyle = C.page;
		ctx.fillRect(0, 0, LOGIC_W, totalH);
		box(ctx, PAD, CARD_Y, CARD_W, cardH, 14, C.card);
		strokeBox(ctx, PAD, CARD_Y, CARD_W, cardH, 14, C.border);

		spaced(ctx, '通行证展柜', CX, CARD_Y + 18, 10, C.faint, 2);

		// 大数字：已点亮
		const ownedStr = String(p.owned == null ? 0 : p.owned);
		txt(ctx, ownedStr, CX, CARD_Y + 34, 34, C.ink, true);
		const numW = textW(ownedStr, 34);
		const totalStr = p.totalChars ? ('/ ' + p.totalChars + ' 位') : '位';
		txt(ctx, totalStr, CX + numW + 7, CARD_Y + 51, 11.5, C.faint);

		// 统计行
		const stat = [];
		if (p.wanted) stat.push('想要 ' + p.wanted);
		if (p.trade) stat.push('出物 ' + p.trade);
		if (stat.length) txt(ctx, stat.join(' · '), CX, CARD_Y + 78, 11, C.sub);

		let cy = CARD_Y + 34 + 34 + 30;

		// 分隔线
		ctx.strokeStyle = C.border;
		ctx.lineWidth = 1;
		ctx.beginPath();
		ctx.moveTo(CX, cy);
		ctx.lineTo(CX + CW, cy);
		ctx.stroke();
		cy += 16;

		// 名册
		if (chipsH) {
			let yy = cy;
			for (let r = 0; r < chipRows.length; r++) {
				let xx = CX;
				const items = chipRows[r];
				for (let i = 0; i < items.length; i++) {
					const it = items[i];
					const bg = it.more ? C.moreBg : (it.elite2 ? C.goldBg : C.chipBg);
					box(ctx, xx, yy, it.w, CHIP_H, 6, bg);
					if (it.elite2) strokeBox(ctx, xx, yy, it.w, CHIP_H, 6, C.goldBorder);
					const nameColor = it.more ? C.faint : (it.elite2 ? C.goldText : C.chipText);
					txt(ctx, it.name, xx + 8, yy + 6, 10, nameColor, false);
					if (it.elite2) {
						txt(ctx, '精二', xx + it.w - 8 - textW('精二', 8.5), yy + 7, 8.5, C.goldText, false);
					}
					xx += it.w + 5;
				}
				yy += CHIP_H + CHIP_GAP;
			}
			cy += chipsH + 14;
		}

		// 备注
		if (names.length) {
			txt(ctx, '共 ' + (p.total || 0) + ' 位干员已点亮', CX, cy, 10.5, C.faint);
			txt(ctx, '精二 ' + (p.elite2Count || 0) + ' 位', CX + CW, cy, 10.5, C.faint, false, 'right');
		} else {
			txt(ctx, '还没有点亮任何通行证，去展柜勾一勾吧', CX, cy + 10, 11, C.faint2);
		}
		cy += noteH;

		// 页脚
		paintFooter(ctx, qr, p.qrTitle || '微信扫码 · 记录你的每一张通行证', '数据只存在本机，不上传服务器', cy);

		return exportCanvas(canvasId, totalH, owner, ctx);
	});
}

/* ---------------- 导出 ---------------- */
function exportCanvas(canvasId, totalH, owner, ctx) {
	return new Promise((resolve, reject) => {
		ctx.draw(false, function () {
			// 旧 canvas 的 draw 回调与像素落盘之间要留一点时间，太快导出会是空白
			setTimeout(function () {
				uni.canvasToTempFilePath({
					canvasId: canvasId,
					x: 0,
					y: 0,
					width: LOGIC_W,
					height: totalH,
					destWidth: LOGIC_W * 2,
					destHeight: totalH * 2,
					fileType: 'png',
					quality: 1,
					success: function (res) {
						resolve(toLocalUrl(res.tempFilePath));
					},
					fail: function (err) {
						reject(err || { code: 'EXPORT_FAILED', message: '导出图片失败' });
					}
				}, owner);
			}, 260);
		});
	});
}

export default { renderDailyPoster, renderGalleryPoster, LOGIC_W };
