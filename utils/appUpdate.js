// utils/appUpdate.js
//
// App（Android / iOS）版本更新
// ---------------------------------------------------------------------------
// 流程与 Windows 壳保持一致：
//   1. 读 GitHub Release 列表，挑出「带 apk 附件」的最新正式版；
//   2. 与本地 versionName 做 SemVer 比较；
//   3. 下载：GitHub 直链 + 多个加速线路，8 秒低速看门狗自动换线；
//   4. 下到 .part 临时文件，校验大小后才改名 .apk（避免半截包被安装）；
//   5. 调起系统安装器（Android）；iOS 只能跳下载页。
//
// 设计参考 MAA-Meow 的 AppDownloader：
//   - SemVer 比较（1.0.0-beta < 1.0.0）
//   - 先写临时文件、完整后改名
//   - 按版本缓存 apk，下次点更新直接装，不重复下载
//
// ★ 微信小程序端完全不参与：小程序走 wx.getUpdateManager（utils/mpUpdate.js），
//   由微信自己下载并重启，没有本地安装包这一步。
// ★ 本文件只在 APP-PLUS 下编译出实现，其它平台是空壳；
//   页面里的 import 也用 #ifdef APP-PLUS 包住，不会打进小程序包。
// ---------------------------------------------------------------------------

// #ifdef APP-PLUS

const REPO = "awadwd/Arknights-Authorization-Tool";
const RELEASES_API = `https://api.github.com/repos/${REPO}/releases?per_page=30`;
const RELEASE_PAGE = `https://github.com/${REPO}/releases/latest`;
const APK_DIR = "_downloads/";

/** 加速前缀。与 Windows 壳同一批线路、同一顺序。 */
const CDN_PREFIXES = [
	"https://ghfast.top/",
	"https://gh-proxy.com/",
	"https://ghproxy.net/"
];

/** 低速判定：8 秒平均低于 200 KB/s 就换下一条线路 */
const SLOW_WINDOW_MS = 8000;
const SLOW_BYTES_PER_SEC = 200 * 1024;
/** 连续 20 秒一个字节都没动，当作卡死 */
const STALL_MS = 20000;

// ---------------------------------------------------------------------------
// 版本号
// ---------------------------------------------------------------------------

function splitVer(v) {
	const s = String(v || "")
		.trim()
		.replace(/^[vV]/, "");
	const idx = s.indexOf("-");
	return idx >= 0 ? [s.slice(0, idx), s.slice(idx + 1)] : [s, null];
}

function cmpMain(a, b) {
	const x = a.split(".").map((n) => parseInt(n, 10) || 0);
	const y = b.split(".").map((n) => parseInt(n, 10) || 0);
	const len = Math.max(x.length, y.length);
	for (let i = 0; i < len; i++) {
		const p = x[i] || 0;
		const q = y[i] || 0;
		if (p !== q) return p > q ? 1 : -1;
	}
	return 0;
}

function cmpPre(a, b) {
	// 没有 prerelease 的版本更新：1.0.0 > 1.0.0-beta
	if (a === null && b === null) return 0;
	if (a === null) return 1;
	if (b === null) return -1;

	const x = a.split(".");
	const y = b.split(".");
	const len = Math.max(x.length, y.length);
	for (let i = 0; i < len; i++) {
		if (i >= x.length) return -1;
		if (i >= y.length) return 1;
		const n1 = parseInt(x[i], 10);
		const n2 = parseInt(y[i], 10);
		const isNum1 = !isNaN(n1) && /^\d+$/.test(x[i]);
		const isNum2 = !isNaN(n2) && /^\d+$/.test(y[i]);
		if (isNum1 && isNum2) {
			if (n1 !== n2) return n1 > n2 ? 1 : -1;
		} else if (isNum1 !== isNum2) {
			return isNum1 ? -1 : 1; // 数字段 < 字符串段
		} else if (x[i] !== y[i]) {
			return x[i] > y[i] ? 1 : -1;
		}
	}
	return 0;
}

/** a > b 返回 1；相等 0；a < b 返回 -1 */
export function compareVersion(a, b) {
	const [m1, p1] = splitVer(a);
	const [m2, p2] = splitVer(b);
	const c = cmpMain(m1, m2);
	return c !== 0 ? c : cmpPre(p1, p2);
}

/**
 * 版本号被改错过一次的兜底。
 *
 * 曾经把版本号误写成 1.9.2.x，但纯数字比较里 1.9 < 1.50 —— 装了那个测试包的人
 * 会被判成"已经是最新"，永远收不到 1.50.54 的推送。这里只放行
 * 「1.9.2.* → 1.50.*」这一次跃迁，之后仍按正常语义比较。
 *
 * 正常用户（1.50.53 → 1.50.54）走不到这里，compareVersion 就够了。
 */
function isSchemeMigration(current, latest) {
	const c = String(current || '').split('.').map((n) => parseInt(n, 10) || 0);
	const l = String(latest || '').split('.').map((n) => parseInt(n, 10) || 0);
	return c[0] === 1 && c[1] === 9 && l[0] === 1 && l[1] === 50;
}

/** 当前安装包的 versionName，例如 1.50.46 */
export function getCurrentVersion() {
	try {
		if (plus && plus.runtime && plus.runtime.version) return plus.runtime.version;
	} catch (e) {}
	return "未知";
}

// ---------------------------------------------------------------------------
// 检查更新
// ---------------------------------------------------------------------------

/**
 * @returns {Promise<{
 *   ok:boolean, hasUpdate:boolean, current:string, latest:string,
 *   notes:string, url:string, name:string, size:number, pageUrl:string, error?:string
 * }>}
 */
export function checkForUpdate() {
	const current = getCurrentVersion();

	return new Promise((resolve) => {
		uni.request({
			url: RELEASES_API,
			method: "GET",
			timeout: 12000,
			header: {
				Accept: "application/vnd.github+json",
				"User-Agent": "AkPassport-App"
			},
			success: (res) => {
				if (res.statusCode !== 200 || !Array.isArray(res.data)) {
					resolve({
						ok: false,
						hasUpdate: false,
						current,
						latest: "",
						error:
							res.statusCode === 403
								? "GitHub 接口访问过于频繁，请稍后再试"
								: `接口返回 ${res.statusCode}`
					});
					return;
				}

				// 只认「正式版 + 带 .apk 附件」；Windows 那条 release 会自动排除
				const list = [];
				res.data.forEach((r) => {
					if (!r || r.draft || r.prerelease) return;
					const assets = Array.isArray(r.assets) ? r.assets : [];
					const apk = assets.find((a) => /\.apk$/i.test(a.name || ""));
					if (!apk || !apk.browser_download_url) return;
					list.push({
						version: String(r.tag_name || "").replace(/^[vV]/, ""),
						notes: r.body || "",
						url: apk.browser_download_url,
						name: apk.name,
						size: apk.size || 0,
						pageUrl: r.html_url || RELEASE_PAGE
					});
				});

				if (!list.length) {
					resolve({
						ok: false,
						hasUpdate: false,
						current,
						latest: "",
						error: "没有找到带安装包的正式版本"
					});
					return;
				}

				// 按版本号取最大，不按发布时间 —— 补发旧版时不会选错
				list.sort((a, b) => compareVersion(b.version, a.version));
				const best = list[0];

				resolve({
					ok: true,
					hasUpdate:
						compareVersion(best.version, current) > 0 ||
						isSchemeMigration(current, best.version),
					current,
					latest: best.version,
					notes: best.notes,
					url: best.url,
					name: best.name,
					size: best.size,
					pageUrl: best.pageUrl
				});
			},
			fail: (err) => {
				resolve({
					ok: false,
					hasUpdate: false,
					current,
					latest: "",
					error: (err && err.errMsg) || "网络请求失败"
				});
			}
		});
	});
}

// ---------------------------------------------------------------------------
// 下载
// ---------------------------------------------------------------------------

function buildSources(directUrl) {
	const out = [{ url: directUrl, label: "GitHub 直链" }];
	CDN_PREFIXES.forEach((p) => {
		out.push({ url: p + directUrl, label: "加速线路 " + p.replace(/^https?:\/\//, "").replace(/\/$/, "") });
	});
	return out;
}

function apkPath(version) {
	// plus.downloader 的 filename 用相对路径即可，最终落在 _downloads/ 下
	return APK_DIR + "ak_update_" + String(version || "unknown").replace(/[^\w.-]/g, "_") + ".apk";
}

function toLocal(p) {
	let out = p || "";
	try {
		out = plus.io.convertLocalFileSystemURL(out);
	} catch (e) {}
	return out;
}

/**
 * 文件是不是真正的 APK。
 *
 * APK 就是 ZIP，头 4 字节固定是 50 4B 03 04（"PK\x03\x04"）。
 * 为什么必须查：加速线路 404 / 限流时会返回一个 HTML 页面，而下载对账用的是
 * 「响应实际大小」，几十 KB 的 HTML 也能「对得上大小」。坏包一旦改名成 .apk，
 * plus.runtime.install 拿它会走 wgt 分支，报「WGT/WGTU文件格式错误」。
 * @param {string} relPath _downloads/ 下的相对路径
 * @returns {Promise<boolean>}
 */
function isApkFile(relPath) {
	return new Promise((resolve) => {
		let settled = false;
		const finish = (v) => {
			if (settled) return;
			settled = true;
			resolve(v);
		};
		try {
			plus.io.resolveLocalFileSystemURL(
				relPath,
				(entry) => {
					entry.file(
						(file) => {
							if (!file || !file.size) return finish(false);
							try {
								const reader = new plus.io.FileReader();
								reader.onloadend = (e) => {
									const head = String((e && e.target && e.target.result) || "");
									finish(head.slice(0, 2) === "PK");
								};
								reader.onerror = () => finish(false);
								reader.readAsText(file.slice(0, 4));
							} catch (err) {
								finish(false);
							}
						},
						() => finish(false)
					);
				},
				() => finish(false)
			);
		} catch (e) {
			finish(false);
		}
	});
}

/** 查本地是否已有这个版本的完整包（大小对得上才算数） */
export function findCachedApk(version, expectSize) {
	return new Promise((resolve) => {
		const rel = apkPath(version);
		plus.io.resolveLocalFileSystemURL(
			rel,
			(entry) => {
				entry.getMetadata(
					(meta) => {
						const size = meta.size || 0;
						const ok = size > 0 && (!expectSize || Math.abs(size - expectSize) <= 1024);
						if (!ok) return resolve(null);
						// 大小对上还不够：线路返回的 HTML 错误页也可能「大小对得上」。
						// 坏包一旦被当成可用缓存，之后每次点安装都会复用它、永远装不上。
						isApkFile(rel).then((good) => {
							if (good) return resolve(toLocal(rel));
							try {
								entry.remove(() => {}, () => {});
							} catch (e) {}
							resolve(null);
						});
					},
					() => resolve(null)
				);
			},
			() => resolve(null)
		);
	});
}

/**
 * 下载安装包，自动换线。
 * @param {object} info checkForUpdate 的结果
 * @param {{onProgress?:Function,onSource?:Function,onDone?:Function,onError?:Function}} cb
 * @returns {{cancel:Function, switchSource:Function}}
 */
export function downloadUpdate(info, cb) {
	const handlers = cb || {};
	const sources = buildSources(info.url);
	const finalRel = apkPath(info.latest);
	const partRel = finalRel + ".part";
	const errors = [];
	let index = 0;
	let task = null;
	let ticker = null;
	let cancelled = false;
	/** 每次换线路都自增：旧任务的回调一律作废，避免把新任务的 UI 冲掉 */
	let gen = 0;
	// 本线路已重试过一次的标记（换线路时重置）—— 见 state===4 分支
	let retriedThisSource = false;
	let lastRetryIdx = -1;

	function cleanup() {
		if (ticker) {
			clearInterval(ticker);
			ticker = null;
		}
	}

	function fail(msg) {
		cleanup();
		if (!cancelled) handlers.onError && handlers.onError(msg);
	}

	function done(localPath) {
		cleanup();
		if (cancelled) return;
		handlers.onDone && handlers.onDone(localPath, sources[index] && sources[index].label);
	}

	/**
	 * ★ 2026-10-27 第三次真机踩坑后重构：彻底取消「.part 改名」环节。
	 *
	 * 之前的流程是下到 xxx.apk.part，校验后 moveTo 改名成 xxx.apk。
	 * 真机上四条线路全部死在 moveTo：plus.io 的 remove() 删掉目标后立刻
	 * moveTo 同名文件，安卓端目录缓存里目标「仍然存在」，moveTo 报
	 * 「目标已存在」而失败——重试多少次都一样。
	 *
	 * 现在直接下载到正式 .apk 文件名，下完只做两道校验：
	 *   1. 大小对账（state===4 分支里做）
	 *   2. PK 魔数（isApkFile）
	 * 校验不过就删掉换线。半截包风险靠「每次开下前先删目标文件 +
	 * 安装前强制 .apk 后缀」兜住（installPackage 里还有一道后缀防线）。
	 */

	function cleanupDownloaded(rel, why) {
		try {
			plus.io.resolveLocalFileSystemURL(
				rel,
				(entry) => entry.remove(() => {}, () => {}),
				() => {}
			);
		} catch (e) {}
	}

	function startOne() {
		if (index >= sources.length) {
			fail("所有下载线路都失败了：" + errors.join("；"));
			return;
		}

		const src = sources[index];
		handlers.onSource && handlers.onSource(src.label, index);
		const myGen = ++gen;

		let lastBytes = 0;
		let lastTs = Date.now();
		let winStart = Date.now();
		let winBase = 0;
		let lastMoveTs = Date.now();
		let advanced = false;
		// 换了线路就重置本线路的重试标记；同线路复调 startOne（重试）保留标记
		if (index !== lastRetryIdx) {
			lastRetryIdx = index;
			retriedThisSource = false;
		}

		// 换线路/重试前先停掉旧下载任务：不 abort 的话旧任务还在后台占着文件，
		// 新任务清不掉也写不进，表现为卡进度或下载失败
		if (task) {
			try { task.abort(); } catch (e) {}
			task = null;
		}

		// ★ 直接下到正式文件名（不再走 .part 改名）。开下前必须先删：
		// ① 上次失败留下的半截/坏包（否则 plus 断点续传会从坏尾巴接着写）
		// ② 老版本遗留的 .part（一并清掉，不碍事）
		cleanupDownloaded(finalRel);
		cleanupDownloaded(partRel);

		try {
			task = plus.downloader.createDownload(src.url, {
				filename: finalRel,
				retry: 0,
				timeout: 30
			});
		} catch (e) {
			errors.push(src.label + "：创建下载失败");
			index++;
			startOne();
			return;
		}

		task.addEventListener("statechanged", (d) => {
			if (cancelled || !d || advanced || myGen !== gen) return;

			if (d.state === 4) {
				advanced = true;
				const localPath = toLocal(d.filename || partRel);
				// 大小对账：够不上目标大小就别急着让人装
				const got = d.downloadedSize || 0;
				const total = d.totalSize || info.size || 0;
				if (total > 0 && got > 0 && got + 1024 < total) {
					// GitHub 直连在弱网下经常中途断流（4.2MB/15.9MB 这类）。
					// 同一条线路先重试一次再跳线：直连往往是网络抖动，不是线路坏。
					if (!retriedThisSource) {
						retriedThisSource = true;
						errors.push(src.label + "：下载不完整（" + got + "/" + total + "），本线路重试");
						cleanup();
						startOne();
						return;
					}
					errors.push(src.label + "：下载不完整（" + got + "/" + total + "）");
					cleanup();
					index++;
					startOne();
					return;
				}
				// 大小对上了不代表内容是 apk：先验文件头再交付。
				isApkFile(finalRel).then((good) => {
					if (cancelled || myGen !== gen) return;
					if (!good) {
						errors.push(src.label + "：下到的不是安装包（疑似被线路拦截返回了网页）");
						cleanupDownloaded(finalRel);
						cleanup();
						index++;
						setTimeout(startOne, 400);
						return;
					}
					done(toLocal(finalRel));
				});
			} else if (d.state === 5) {
				advanced = true;
				// 弱网断流也可能走 state 5（下载错误），同样给一次本线路重试
				if (!retriedThisSource) {
					retriedThisSource = true;
					errors.push(src.label + "：下载失败，本线路重试");
					cleanup();
					startOne();
					return;
				}
				errors.push(src.label + "：下载失败");
				cleanup();
				index++;
				setTimeout(startOne, 400);
			}
		});

		// 进度/测速只能定时读属性：statechanged 不是每个字节都触发
		ticker = setInterval(() => {
			if (cancelled || !task || advanced || myGen !== gen) return;

			const got = task.downloadedSize || 0;
			const total = task.totalSize || info.size || 0;
			const now = Date.now();
			const dt = (now - lastTs) / 1000;
			const speed = dt > 0 ? Math.max(0, (got - lastBytes) / dt) : 0;
			lastBytes = got;
			lastTs = now;
			if (got > 0) lastMoveTs = now;

			handlers.onProgress &&
				handlers.onProgress({
					received: got,
					total,
					percent: total > 0 ? Math.min(100, (got / total) * 100) : 0,
					speed,
					label: src.label
				});

			// 8 秒低速看门狗；末位线路没得换了就不判速
			if (index < sources.length - 1) {
				const elapsed = now - winStart;
				if (elapsed >= SLOW_WINDOW_MS) {
					const avg = ((got - winBase) / elapsed) * 1000;
					if (avg < SLOW_BYTES_PER_SEC && (total <= 0 || got < total)) {
						advanced = true;
						cleanup();
						try {
							task.abort();
						} catch (e) {}
						task = null;
						errors.push(
							src.label + "：太慢（平均 " + Math.round(avg / 1024) + " KB/s）"
						);
						index++;
						startOne();
						return;
					}
					winStart = now;
					winBase = got;
				}
			}

			// 长时间完全不动：当卡死处理
			if (now - lastMoveTs > STALL_MS && (total <= 0 || got < total)) {
				lastMoveTs = now;
				if (index < sources.length - 1) {
					advanced = true;
					cleanup();
					try {
						task.abort();
					} catch (e) {}
					task = null;
					errors.push(src.label + "：连接卡住");
					index++;
					startOne();
				}
			}
		}, 1000);

		try {
			task.start();
		} catch (e) {
			cleanup();
			errors.push(src.label + "：启动下载失败");
			index++;
			startOne();
		}
	}

	startOne();

	return {
		cancel() {
			cancelled = true;
			cleanup();
			try {
				task && task.abort();
			} catch (e) {}
		},
		/** 手动换下一条线路，返回 false 表示已经是最后一条 */
		switchSource() {
			if (index >= sources.length - 1) return false;
			cleanup();
			try {
				task && task.abort();
			} catch (e) {}
			task = null;
			index++;
			startOne();
			return true;
		}
	};
}

// ---------------------------------------------------------------------------
// 安装
// ---------------------------------------------------------------------------

/**
 * 调起系统安装器。Android 8+ 首次会要求「允许安装未知应用」。
 * @returns {Promise<void>}
 */
export function installPackage(filePath) {
	return new Promise((resolve, reject) => {
		try {
			if (plus.os.name !== "Android") {
				reject(new Error("当前平台不支持直接安装，请前往下载页"));
				return;
			}
			// 保底：不是 .apk 的文件交给 plus 会被当成 wgt 解析，
			// 报的是「WGT/WGTU文件格式错误」，很容易误判成权限问题。
			const raw = String(filePath || "").trim();
			if (!/\.apk$/i.test(raw)) {
				reject(new Error("安装包路径异常（不是 .apk），请删除后重新下载"));
				return;
			}

			// ★ plus.runtime.install 对路径形态很挑：传裸绝对路径（/storage/...，
			//   plus.io.convertLocalFileSystemURL 的返回值）它解析不到，会掉进 wgt
			//   分支报 -1202「WGT安装包中manifest.json文件不存在」。
			//   可靠形态只有两种：plus 相对路径（_downloads/...）和 file:// 绝对路径。
			//   按可靠性排出候选序列，失败自动试下一个，最后用 force:true 兜底
			//   （force 只跳过版本比对，apk 安装本来就由系统包安装器做版本校验）。
			const cands = [];
			const push = (p, force) => {
				if (p && !cands.some((c) => c.p === p && c.force === force)) cands.push({ p, force });
			};
			if (/^file:\/\//i.test(raw)) {
				push(raw, false);
				push(raw.replace(/^file:\/\//i, ""), false);
			} else if (/^\/.+/.test(raw)) {
				// 裸绝对路径：file:// 形态优先（DCloud 论坛/CSDN 验证过的可靠形态）
				push("file://" + raw, false);
				push(raw, false);
			} else {
				// plus 相对路径（_downloads/... 等）：官方文档示例的形态
				push(raw, false);
				try {
					const abs = plus.io.convertLocalFileSystemURL(raw);
					if (abs) push("file://" + abs, false);
				} catch (e) {}
			}
			// 兜底：force:true（跳过 plus 层的版本比对，交给系统安装器把关）
			cands.forEach((c) => push(c.p, true));

			let idx = 0;
			const tryNext = (lastErr) => {
				if (idx >= cands.length) {
					reject(new Error((lastErr && (lastErr.message || lastErr.errMsg)) || "安装器调用失败"));
					return;
				}
				const cur = cands[idx++];
				plus.runtime.install(
					cur.p,
					{ force: cur.force },
					() => resolve(),
					(e) => tryNext(e)
				);
			};
			tryNext(null);
		} catch (e) {
			reject(e);
		}
	});
}

/**
 * 冷启动静默检查（24 小时内只查一次，避免每次开 App 都打 GitHub 接口）。
 * 有新版本时交给调用方决定怎么提示，不在这里弹窗。
 * @param {{onUpdate?:Function}} opts
 */
export function autoCheckOnLaunch(opts) {
	const KEY = "appUpdateAutoCheckTs";
	const DAY = 24 * 3600 * 1000;
	try {
		const last = Number(uni.getStorageSync(KEY) || 0);
		if (last && Date.now() - last < DAY) return;
		uni.setStorageSync(KEY, Date.now());
	} catch (e) {}

	checkForUpdate().then((r) => {
		if (r && r.ok && r.hasUpdate && opts && typeof opts.onUpdate === "function") {
			try {
				opts.onUpdate(r);
			} catch (e) {}
		}
	});
}

/** 打不开安装器时的兜底：跳 Release 页面手动下载 */
export function openReleasePage(url) {
	const target = url || RELEASE_PAGE;
	try {
		plus.runtime.openURL(target);
	} catch (e) {
		uni.setClipboardData({ data: target });
	}
}

// #endif

// #ifndef APP-PLUS
// 非 App 平台（H5 / 小程序）页面里的 import 已被条件编译移除，
// 这里只保留同名空实现兜底，避免误引用时报错。
export function compareVersion() {
	return 0;
}
export function getCurrentVersion() {
	return "";
}
export function checkForUpdate() {
	return Promise.resolve({ ok: false, hasUpdate: false, error: "not-app" });
}
export function findCachedApk() {
	return Promise.resolve(null);
}
export function downloadUpdate() {
	return { cancel() {}, switchSource() { return false; } };
}
export function installPackage() {
	return Promise.reject(new Error("not-app"));
}
export function autoCheckOnLaunch() {}
export function openReleasePage() {}
// #endif
