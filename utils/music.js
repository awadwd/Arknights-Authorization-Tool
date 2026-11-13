// utils/music.js
//
// 塞壬唱片（Monster Siren Records）纯音乐播放 —— 曲库 + 播放内核。
// ---------------------------------------------------------------------------
// 为什么分成"曲库"和"音源"两步：
//   官方接口 /api/songs 只给曲目元信息（cid、标题、专辑），真实音频地址要再打一次
//   /api/song/<cid> 才拿得到，而且那个地址是带签名的、几分钟到几小时就失效（实测两次
//   请求前缀都不同）。所以曲库可以打成静态 JSON 随包走（离线能看列表），音源必须
//   播放前现取。
//
// 版权与分发：
//   音频一律不随包分发，也不做转存，全部由官方 CDN 直出，属于"客户端播放官方公开
//   资源"。曲库里只保留曲名 / 专辑名 / 封面 URL 这些元信息。这样既不用背版权风险，
//   也不用把 APK 撑大几十 MB。
//
// 平台差异：
//   App / 小程序  uni.request 是原生请求，没有同源策略，直接取。
//   Windows 壳    页面源是 https://app.local，直连是跨域；壳里已把
//                 monster-siren.hypergryph.com 加进 RemoteProxy（补 CORS 头）。
//   普通浏览器      拿不到（对方不返回 ACAO），会给出提示，属于预期行为。
// ---------------------------------------------------------------------------

import CATALOG from '@/static/music/catalog.json';

const API = 'https://monster-siren.hypergryph.com/api';
const URL_TTL = 6 * 60 * 1000; // 签名地址有效期按 6 分钟保守缓存，过期重取

const urlCache = new Map(); // cid -> { url, ts }

export function getCatalog() {
	const list = (CATALOG && Array.isArray(CATALOG.list)) ? CATALOG.list : [];
	return list;
}

export function catalogCount() {
	return getCatalog().length;
}

export function searchCatalog(kw) {
	const q = String(kw || '').trim().toLowerCase();
	const list = getCatalog();
	if (!q) return list;
	return list.filter(
		(t) =>
			t.name.toLowerCase().includes(q) ||
			(t.album || '').toLowerCase().includes(q)
	);
}

/** 按 cid 找曲目 */
export function findTrack(cid) {
	return getCatalog().find((t) => String(t.cid) === String(cid)) || null;
}

/**
 * 取可直接播放的音频地址（官方 CDN，wav）。
 * @returns {Promise<{ok:boolean,url?:string,error?:string}>}
 */
export function resolveSource(cid) {
	const key = String(cid || '');
	if (!key) return Promise.resolve({ ok: false, error: '曲目 cid 为空' });

	const hit = urlCache.get(key);
	if (hit && Date.now() - hit.ts < URL_TTL) {
		return Promise.resolve({ ok: true, url: hit.url, cached: true });
	}

	return new Promise((resolve) => {
		uni.request({
			url: `${API}/song/${key}`,
			method: 'GET',
			timeout: 15000,
			success: (res) => {
				const d = (res.data && res.data.data) || null;
				const url = d && d.sourceUrl;
				if (res.statusCode === 200 && url) {
					urlCache.set(key, { url, ts: Date.now() });
					resolve({ ok: true, url });
				} else {
					resolve({ ok: false, error: '官方接口没有返回音源（' + res.statusCode + '）' });
				}
			},
			fail: (err) => {
				const msg = (err && err.errMsg) || '';
				resolve({
					ok: false,
					error: /cross|CORS|origin/i.test(msg) ? '当前环境不允许跨域取音源' : '网络请求失败'
				});
			}
		});
	});
}

export function clearUrlCache() {
	urlCache.clear();
}

// ---------------------------------------------------------------------------
// 播放内核（三端统一走 innerAudioContext）
// ---------------------------------------------------------------------------

/**
 * @param {{onState?:Function,onProgress?:Function,onEnded?:Function,onError?:Function}} cb
 * @returns {{
 *   play:(track:object)=>Promise<boolean>, toggle:()=>void, pause:()=>void, resume:()=>void,
 *   seek:(sec:number)=>void, stop:()=>void, destroy:()=>void,
 *   setVolume:(v:number)=>void, state:()=>({playing:boolean,track:object|null,buffering:boolean}),
 *   current:()=>number, duration:()=>number
 * }}
 */
export function createPlayer(cb) {
	const h = cb || {};
	let audio = null;
	let track = null;
	let playing = false;
	let buffering = false;
	let seq = 0; // 防止快速切歌时旧回调覆盖新状态

	function ensure() {
		if (audio) return audio;
		audio = uni.createInnerAudioContext();
		audio.autoplay = false;
		audio.onPlay(() => {
			playing = true;
			buffering = false;
			h.onState && h.onState(state());
		});
		audio.onPause(() => {
			playing = false;
			h.onState && h.onState(state());
		});
		audio.onStop(() => {
			playing = false;
			h.onState && h.onState(state());
		});
		audio.onEnded(() => {
			playing = false;
			h.onState && h.onState(state());
			h.onEnded && h.onEnded(track);
		});
		audio.onWaiting(() => {
			buffering = true;
			h.onState && h.onState(state());
		});
		audio.onCanplay(() => {
			buffering = false;
			h.onState && h.onState(state());
		});
		audio.onTimeUpdate(() => {
			h.onProgress &&
				h.onProgress({
					current: Number(audio.currentTime) || 0,
					duration: Number(audio.duration) || 0
				});
		});
		audio.onError((e) => {
			playing = false;
			buffering = false;
			h.onError && h.onError((e && e.errMsg) || '播放失败');
			h.onState && h.onState(state());
		});
		return audio;
	}

	function state() {
		return { playing, buffering, track, current: current(), duration: duration() };
	}

	function current() {
		try {
			return Number(audio && audio.currentTime) || 0;
		} catch (e) {
			return 0;
		}
	}

	function duration() {
		try {
			return Number(audio && audio.duration) || 0;
		} catch (e) {
			return 0;
		}
	}

	function play(t) {
		const mySeq = ++seq;
		const a = ensure();
		track = t || null;
		playing = false;
		buffering = true;
		h.onState && h.onState(state());

		const fail = (msg) => {
			buffering = false;
			h.onError && h.onError(msg);
			h.onState && h.onState(state());
			return false;
		};

		if (!t || !t.cid) return Promise.resolve(fail('没有选中曲目'));

		return resolveSource(t.cid).then((r) => {
			if (mySeq !== seq) return false; // 已经切到别的歌了
			if (!r.ok) return fail(r.error || '取音源失败');
			try {
				a.src = r.url;
				a.play();
				return true;
			} catch (e) {
				return fail((e && e.message) || '播放失败');
			}
		});
	}

	function toggle() {
		if (!audio || !track) return;
		if (playing) audio.pause();
		else audio.play();
	}

	return {
		play,
		toggle,
		pause() {
			try {
				audio && audio.pause();
			} catch (e) {}
		},
		resume() {
			try {
				audio && audio.play();
			} catch (e) {}
		},
		seek(sec) {
			try {
				if (audio) audio.seek(Math.max(0, Number(sec) || 0));
			} catch (e) {}
		},
		stop() {
			seq++;
			playing = false;
			buffering = false;
			try {
				audio && audio.stop();
			} catch (e) {}
		},
		setVolume(v) {
			try {
				if (audio) audio.volume = Math.min(1, Math.max(0, Number(v) || 0));
			} catch (e) {}
		},
		state,
		current,
		duration,
		destroy() {
			seq++;
			try {
				audio && audio.destroy();
			} catch (e) {}
			audio = null;
			track = null;
			playing = false;
		}
	};
}

/** 秒数格式化 03:21 */
export function fmtTime(sec) {
	const s = Math.max(0, Math.floor(Number(sec) || 0));
	const m = Math.floor(s / 60);
	const r = s % 60;
	return String(m).padStart(2, '0') + ':' + String(r).padStart(2, '0');
}
