// utils/notify.js
//
// 统一通知层 —— 一个入口，四种落地方式。
// ---------------------------------------------------------------------------
// 为什么各端不一样（这是能力边界，不是偷懒）：
//
//   App（Android）  系统通知栏可自己弹，不需要推送服务、不需要联网。
//                   走 plus.android 直接调 NotificationManager，不依赖 UniPush / 厂商通道，
//                   也就不需要任何云打包插件配置。Android 13+ 需要 POST_NOTIFICATIONS 权限，
//                   用户没给的时候我们跳到「应用通知设置」让他手动开。
//   PC（WinUI 壳）  没有 Web 通知权限，页面里 new Notification() 在 WebView2 里会被静默丢弃。
//                   真正的系统级弹窗只能由外壳做：postMessage 给 C#，用 Windows App SDK
//                   的 AppNotificationManager 出 Toast。
//   微信小程序      没有任何"本地通知"API。个人主体能用的只有「订阅消息」：
//                   必须先在公众平台建模板、拿到模板 ID，且每次授权只能发一条。
//                   所以这里做的是「引导订阅 + 模板 ID 可配」，没配模板就退回应用内提示。
//   普通浏览器 H5   有 Notification API，但需要用户手势触发授权，且必须 HTTPS。
//                   能用就用，不能用就退回应用内 toast。
//
// PushPlus 是独立的第三方通道（微信服务号推送），走 HTTP，跟本地通知互不影响，
// 所以给它单独的开关和 token，断网/没权限时它是唯一还能到手机的通道。
// ---------------------------------------------------------------------------

const K_ENABLE = 'notifyEnable';
const K_APP = 'notifyAppUpdate';
const K_DATA = 'notifyDataUpdate';
const K_TOKEN = 'notifyPushToken';
const K_MP_SUB = 'notifyMpSubscribed';

const PUSHPLUS_API = 'https://www.pushplus.plus/send';

// 微信订阅消息模板 ID：在微信公众平台 → 订阅消息 → 选用模板 后填进来（可多个）。
// 留空时小程序端只显示「需要在公众平台配置模板」的说明，不会报错。
const MP_TMPL_IDS = [];

// ---------------------------------------------------------------------------
// 平台判定
// ---------------------------------------------------------------------------

export function platformKind() {
	// #ifdef APP-PLUS
	try {
		if (plus.os.name === 'Android') return 'android';
		if (plus.os.name === 'iOS') return 'ios';
	} catch (e) {}
	return 'app';
	// #endif
	// #ifdef MP-WEIXIN
	return 'mp';
	// #endif
	// #ifdef H5
	try {
		if (window.chrome && window.chrome.webview) return 'win';
	} catch (e) {}
	return 'web';
	// #endif
	// eslint-disable-next-line no-unreachable
	return 'unknown';
}

/** 当前平台能做什么 —— 设置页拿它决定显示哪些开关 */
export function capabilities() {
	const kind = platformKind();
	return {
		kind,
		/** 能出系统通知（通知栏 / Toast） */
		system: kind === 'android' || kind === 'win',
		/** 能用 PushPlus */
		pushplus: kind === 'android' || kind === 'win' || kind === 'web' || kind === 'ios',
		/** 能用微信订阅消息 */
		subscribe: kind === 'mp',
		/** 需要用户手动授权 */
		needPermission: kind === 'android' || kind === 'web'
	};
}

// ---------------------------------------------------------------------------
// 配置读写
// ---------------------------------------------------------------------------

function flag(key, def) {
	try {
		const v = uni.getStorageSync(key);
		return v === '' || v === null || v === undefined ? def : v === '1' || v === 1 || v === true;
	} catch (e) {
		return def;
	}
}

export function getNotifySettings() {
	return {
		enabled: flag(K_ENABLE, true),
		appUpdate: flag(K_APP, true),
		dataUpdate: flag(K_DATA, false),
		pushToken: (() => {
			try {
				return String(uni.getStorageSync(K_TOKEN) || '');
			} catch (e) {
				return '';
			}
		})(),
		mpSubscribed: flag(K_MP_SUB, false)
	};
}

export function setNotifySettings(patch) {
	try {
		const p = patch || {};
		if ('enabled' in p) uni.setStorageSync(K_ENABLE, p.enabled ? '1' : '0');
		if ('appUpdate' in p) uni.setStorageSync(K_APP, p.appUpdate ? '1' : '0');
		if ('dataUpdate' in p) uni.setStorageSync(K_DATA, p.dataUpdate ? '1' : '0');
		if ('pushToken' in p) uni.setStorageSync(K_TOKEN, String(p.pushToken || '').trim());
		if ('mpSubscribed' in p) uni.setStorageSync(K_MP_SUB, p.mpSubscribed ? '1' : '0');
	} catch (e) {}
}

// ---------------------------------------------------------------------------
// Android 本地通知
// ---------------------------------------------------------------------------

let andIdSeq = 1000;

function andPermissionGranted() {
	// #ifdef APP-PLUS
	try {
		const main = plus.android.runtimeMainActivity();
		plus.android.importClass(main);
		if (parseInt(plus.os.version) < 13) return true;
		return main.checkSelfPermission('android.permission.POST_NOTIFICATIONS') === 0;
	} catch (e) {
		return true;
	}
	// #endif
	return true;
}

/** 跳到系统的「应用通知」页，让用户自己开权限（比 requestPermissions 稳，不用碰 Java 数组） */
export function openSystemNotifySettings() {
	// #ifdef APP-PLUS
	try {
		const main = plus.android.runtimeMainActivity();
		const Intent = plus.android.importClass('android.content.Intent');
		const Settings = plus.android.importClass('android.provider.Settings');
		const intent = new Intent(Settings.ACTION_APP_NOTIFICATION_SETTINGS);
		intent.putExtra(Settings.EXTRA_APP_PACKAGE, main.getPackageName());
		main.startActivity(intent);
		return true;
	} catch (e) {
		try {
			const main = plus.android.runtimeMainActivity();
			const Intent = plus.android.importClass('android.content.Intent');
			const Settings = plus.android.importClass('android.provider.Settings');
			const intent = new Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS);
			intent.setData(plus.android.invoke('android.net.Uri', 'parse', 'package:' + main.getPackageName()));
			main.startActivity(intent);
			return true;
		} catch (e2) {
			return false;
		}
	}
	// #endif
	return false;
}

/**
 * Android 通知栏消息。
 * 不依赖 UniPush / 厂商通道：直接 new Notification.Builder，所以离线也能弹。
 */
function andLocal(title, body) {
	// #ifdef APP-PLUS
	const main = plus.android.runtimeMainActivity();
	const Context = plus.android.importClass('android.content.Context');
	const Intent = plus.android.importClass('android.content.Intent');
	const PendingIntent = plus.android.importClass('android.app.PendingIntent');
	const Builder = plus.android.importClass('android.app.Notification.Builder');

	const sdk = parseInt(plus.os.version) || 30;
	const nm = main.getSystemService(Context.NOTIFICATION_SERVICE);
	plus.android.importClass(nm);

	const CH_ID = 'akp_notify';
	if (sdk >= 26) {
		const Channel = plus.android.importClass('android.app.NotificationChannel');
		const ch = new Channel(CH_ID, '通行证提醒', 3); // IMPORTANCE_DEFAULT
		ch.setDescription('版本更新、数据更新提醒');
		ch.enableVibration(true);
		nm.createNotificationChannel(ch);
	}

	// 点通知回到 App（不新建实例）
	const intent = new Intent(main, main.getClass());
	intent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
	let piFlags = PendingIntent.FLAG_UPDATE_CURRENT;
	if (sdk >= 23) piFlags = piFlags | PendingIntent.FLAG_IMMUTABLE;
	const pi = PendingIntent.getActivity(main, 0, intent, piFlags);

	const builder = sdk >= 26 ? new Builder(main, CH_ID) : new Builder(main);
	builder.setContentTitle(String(title || ''));
	builder.setContentText(String(body || ''));
	builder.setSmallIcon(main.getApplicationInfo().icon);
	builder.setAutoCancel(true);
	builder.setContentIntent(pi);
	if (sdk < 26) builder.setPriority(0); // PRIORITY_DEFAULT

	nm.notify(andIdSeq++, builder.build());
	return true;
	// #endif
	// eslint-disable-next-line no-unreachable
	return false;
}

// ---------------------------------------------------------------------------
// 各端发送
// ---------------------------------------------------------------------------

/** 应用内兜底提示：系统通知发不出去时用，至少别让消息丢了没人知道 */
function inApp(title, body) {
	try {
		uni.showToast({
			title: String(title || '').slice(0, 16),
			icon: 'none',
			duration: 2600
		});
	} catch (e) {}
	console.log('[notify]', title, body);
}

/** 直接发系统通知（不看开关，调用方自己判断） */
export function sendSystem(title, body) {
	const kind = platformKind();

	if (kind === 'android') {
		// #ifdef APP-PLUS
		try {
			if (!andPermissionGranted()) {
				return { ok: false, needPermission: true, error: '没有通知权限' };
			}
			andLocal(title, body);
			return { ok: true };
		} catch (e) {
			return { ok: false, error: (e && e.message) || '通知发送失败' };
		}
		// #endif
	}

	if (kind === 'win') {
		// #ifdef H5
		try {
			window.chrome.webview.postMessage(
				JSON.stringify({ type: 'toast', title: String(title || ''), body: String(body || '') })
			);
			return { ok: true };
		} catch (e) {
			return { ok: false, error: '外壳未响应' };
		}
		// #endif
	}

	if (kind === 'web') {
		// #ifdef H5
		try {
			if (typeof Notification !== 'undefined' && Notification.permission === 'granted') {
				new Notification(String(title || ''), { body: String(body || '') });
				return { ok: true };
			}
			if (typeof Notification !== 'undefined' && Notification.permission === 'default') {
				Notification.requestPermission();
			}
		} catch (e) {}
		// #endif
	}

	return { ok: false, fallback: true };
}

/** 把一条提醒发给所有已启用的通道 */
export function fire(kind, title, body) {
	const cfg = getNotifySettings();
	if (!cfg.enabled) return { ok: false, skipped: 'disabled' };
	if (kind === 'app' && !cfg.appUpdate) return { ok: false, skipped: 'off' };
	if (kind === 'data' && !cfg.dataUpdate) return { ok: false, skipped: 'off' };

	const results = [];
	const sys = sendSystem(title, body);
	results.push(sys);

	if (cfg.pushToken) {
		// 不等待：PushPlus 走网络，失败也不该拖住 UI
		sendPushPlus(title, body, cfg.pushToken).then((r) => {
			if (!r.ok && r.error) console.warn('[notify] pushplus:', r.error);
		});
	}

	if (!sys.ok && sys.fallback) inApp(title, body);
	return { ok: sys.ok || results.some((r) => r.ok), system: sys };
}

// ---------------------------------------------------------------------------
// PushPlus（微信服务号推送）
// ---------------------------------------------------------------------------

/**
 * @returns {Promise<{ok:boolean, error?:string, raw?:any}>}
 */
export function sendPushPlus(title, content, token) {
	const tk = String(token || getNotifySettings().pushToken || '').trim();
	if (!tk) return Promise.resolve({ ok: false, error: '没有填写 PushPlus token' });

	const payload = {
		token: tk,
		title: String(title || '').slice(0, 60),
		content: String(content || ''),
		template: 'txt'
	};

	// Windows 壳里是 https://app.local 源，直连会被 CORS 拦；交给外壳发
	if (platformKind() === 'win') {
		// #ifdef H5
		try {
			window.chrome.webview.postMessage(
				JSON.stringify({
					type: 'pushplus',
					token: payload.token,
					title: payload.title,
					content: payload.content
				})
			);
			return Promise.resolve({ ok: true, via: 'shell' });
		} catch (e) {}
		// #endif
	}

	return new Promise((resolve) => {
		uni.request({
			url: PUSHPLUS_API,
			method: 'POST',
			timeout: 12000,
			header: { 'content-type': 'application/json' },
			data: payload,
			success: (res) => {
				const d = res.data || {};
				const code = d.code;
				const ok = res.statusCode === 200 && (code === 200 || code === undefined);
				resolve({ ok, error: ok ? '' : d.msg || '返回 ' + res.statusCode, raw: d });
			},
			fail: (err) => resolve({ ok: false, error: (err && err.errMsg) || '网络请求失败' })
		});
	});
}

// ---------------------------------------------------------------------------
// 微信小程序订阅消息
// ---------------------------------------------------------------------------

export function mpSubscribeReady() {
	return MP_TMPL_IDS.length > 0;
}

/**
 * 申请订阅消息授权。个人主体的小程序也能用「一次性订阅」，一次授权发一条。
 * 没有配置模板 ID 时返回 not-configured，由页面给说明文案。
 */
export function requestMpSubscribe() {
	// #ifdef MP-WEIXIN
	if (!MP_TMPL_IDS.length) return Promise.resolve({ ok: false, error: 'not-configured' });
	return new Promise((resolve) => {
		uni.requestSubscribeMessage({
			tmplIds: MP_TMPL_IDS,
			success: (res) => {
				const accepted = MP_TMPL_IDS.filter((id) => res[id] === 'accept');
				if (accepted.length) {
					setNotifySettings({ mpSubscribed: true });
					resolve({ ok: true });
				} else {
					resolve({ ok: false, error: '你拒绝了订阅' });
				}
			},
			fail: (err) => resolve({ ok: false, error: (err && err.errMsg) || '订阅失败' })
		});
	});
	// #endif
	// eslint-disable-next-line no-unreachable
	return Promise.resolve({ ok: false, error: 'not-mp' });
}

// ---------------------------------------------------------------------------
// 权限
// ---------------------------------------------------------------------------

export function hasSystemPermission() {
	const kind = platformKind();
	if (kind === 'android') return andPermissionGranted();
	return true;
}
