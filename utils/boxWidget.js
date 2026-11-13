/**
 * 桌面小组件桥接层（仅 Android）
 *
 * 职责：
 *   1. 把 App 侧已有的盒号数据整理成小组件需要的结构并推给原生
 *   2. 提供「一键添加到桌面」
 *   3. 解析 scheme 启动参数，落到对应盒号详情页 / 搜索页
 *
 * 设计要点：
 *   - 原生插件只在 APP-PLUS 下 import。小程序 / H5 编译时这行会被预处理器整行移除，
 *     因此不会给主平台引入任何编译风险。
 *   - 注意：uni-app（非 uni-app x）项目中 APP-ANDROID / APP-IOS 只在 .uts 文件内生效，
 *     .js / .vue 里无法用条件编译区分 Android 与 iOS，必须用 uni.getSystemInfo()。
 *   - 推送做了「内容签名」去重：数据没变就不触发原生重绘，避免每次切前台都无谓刷新。
 */

import { elite2Url } from './eliteAvatar.js'

// #ifdef APP-PLUS
import * as boxWidgetNative from '@/uni_modules/ak-box-widget'
// #endif

import * as boxDataSync from '@/utils/boxDataSync.js'

/**
 * 推送条数上限。
 * 桌面端是「翻页逐个看」，条数不足会出现「共 93 盒但只能翻到 40」的割裂感，
 * 所以这里放宽到 200（当前全量 93 盒，留足增量余量）。整包 JSON 约 70~90KB，
 * 落在 SharedPreferences 与 JS->原生字符串传参都很轻松的范围内。
 */
const MAX_ITEMS = 200
/** 每盒最多带几个干员，必须与原生 AkWidgetRender.MAX_CHAR 一致 */
const MAX_CHARS = 8
/**
 * 抽卡小组件每盒最多带几个干员。
 * 页面 getBoxCharacters() 是按 character1..character10 扫的，这里保持一致（10），
 * 否则会和页面的卡池张数对不上。
 */
const GACHA_MAX_CHARS = 10
/** 抽卡卡池的盒数安全上限；整包 JSON 约 150KB，超限会被截断并在控制台告警 */
const GACHA_MAX_BOXES = 400
/** 首发 / 复刻在此天数内标记角标 */
const NEW_DAYS = 30
/** 非强制推送的最小间隔 */
const SYNC_THROTTLE_MS = 3 * 60 * 1000
const HEADER = '盒号列表'
const SCHEME_PREFIX = 'akpassport://'

let _lastSyncAt = 0
let _lastSig = ''
let _lastRouteKey = ''
let _lastRouteAt = 0

/** 运行时平台判断：条件编译只能区分「App / 非 App」，Android 与 iOS 要靠这里细分 */
function isAndroidRuntime() {
	try {
		const info = uni.getSystemInfoSync()
		return String((info && info.platform) || '').toLowerCase() === 'android'
	} catch (e) {
		return false
	}
}

function pad2(v) {
	const s = String(v === undefined || v === null ? '' : v).trim()
	return s.length >= 2 ? s : '0' + s
}

/** "2023/5/26" -> "2023/05/26"；同时满足展示与字符串排序 */
function normDate(raw) {
	const s = String(raw === undefined || raw === null ? '' : raw).trim()
	if (!s) return ''
	const p = s.split('/')
	if (p.length < 3) return s
	return p[0].trim() + '/' + pad2(p[1]) + '/' + pad2(p[2])
}

/** 距今天数；无法解析时返回 Infinity（即不标记角标） */
function daysSince(dateStr) {
	const m = /^(\d{4})\/(\d{2})\/(\d{2})$/.exec(dateStr || '')
	if (!m) return Infinity
	const t = Date.parse(m[1] + '-' + m[2] + '-' + m[3] + 'T00:00:00')
	if (isNaN(t)) return Infinity
	return (Date.now() - t) / 86400000
}

/**
 * 盒型 —— 这些盒子的角色「没有精二」，整盒都只有精一。
 * 与 pages/card_share/card_share.vue 的 nolyELITE1BoxTypes / noPairTypes 保持一致。
 */
const NO_PAIR_BOX_TYPES = ['ambience', 'special', 'whitelist']

/** 'true' / true / 1 都当作真（上游数据里既有布尔也有字符串） */
function isTrueFlag(v) {
	return v === true || v === 'true' || v === 1 || v === '1'
}

/** 盒型是否整盒只有精一 */
function isNoPairBox(b) {
	const t = String((b && (b.Box_type || b.box_type || b.BoxType)) || '').trim().toLowerCase()
	return NO_PAIR_BOX_TYPES.indexOf(t) >= 0
}

/**
 * 某盒里某位干员的卡片状态：
 *   'single' —— 只有精一（没有一对）：nolyELITE1 为真，或整盒是无精二盒型
 *   'pair'   —— 精一 + 精二 = 有一对
 * 语义与 card_share 的 canHavePair() 完全对齐。
 */
function charState(c, noPairBox) {
	if (noPairBox) return 'single'
	if (isTrueFlag(c && c.nolyELITE1)) return 'single'
	return 'pair'
}
/**
 * 抽取一盒的干员列表（头像地址 + 名称）。
 * 兼容两种写法：character1 = {name, imageUrl} 或 character1 = "阿米娅"。
 */
function readChars(b) {
	const out = []
	const noPairBox = isNoPairBox(b)
	for (let i = 1; i <= 9; i++) {
		const c = b['character' + i]
		if (!c) continue
		let name = ''
		let url = ''
		if (typeof c === 'string') {
			name = c.trim()
		} else if (typeof c === 'object') {
			name = String(c.name || '').trim()
			url = String(c.imageUrl || c.imageURL || c.avatar || '').trim()
		}
		if (!name && !url) continue
		out.push({ name: name, url: url, state: charState(c, noPairBox) })
		if (out.length >= MAX_CHARS) break
	}
	return out
}

/**
 * 取盒号原始记录。
 * 优先用增量引擎缓存的「上游原始记录」——它字段最全，
 * 且不受各页面清洗逻辑（数字转字符串等）影响。
 */
function readBoxes() {
	try {
		const raw = boxDataSync.peek()
		if (Array.isArray(raw) && raw.length) return raw
	} catch (e) {}

	// 退而求其次：页面落库的数据
	try {
		let d = uni.getStorageSync('arknightsData')
		if (typeof d === 'string') {
			try { d = JSON.parse(d) } catch (e2) { d = null }
		}
		if (Array.isArray(d) && d.length) return d
	} catch (e) {}

	return null
}

/**
 * 抽卡小组件用的**完整卡池**。
 *
 * 为什么不能复用上面那份 items：抽卡要照搬页面的逻辑，必须拿到三个字段，
 * 而现有盒号 payload 里一个都没有 ——
 *   t      Box_type        分类筛选 / 混池过滤 / 「整盒无精二」判定
 *   e1/e2  market_price    ELITE1 / ELITE2 市价：￥25/抽 的盈亏与卡面价格
 *   h      hotcharacter    热门（欧非度统计）
 * 另外 items 被 MAX_ITEMS / MAX_CHARS 截断过，这里必须全量。
 *
 * 逐字对齐页面 gacha_simulated.vue：
 *   getBoxCharacters()  取 character1..character10，兼容字符串与对象两种写法
 *   createCard()        price 取 ELITE1 / ELITE2，缺省为 0（0 = 无市价 = 免费）
 *   nolyELITE1         干员自身标记 或 整盒为 ambience/special/whitelist
 *
 * 字段用短名（n/u/e1/e2/h/s）压体积 —— 全量约 1000+ 张牌，长名会让整包体积翻倍。
 */
function readGachaBoxes(boxes) {
	const out = []
	for (let i = 0; i < boxes.length; i++) {
		if (out.length >= GACHA_MAX_BOXES) break
		const b = boxes[i]
		if (!b || typeof b !== 'object') continue

		const id = String(b.Box_id || b.box_id || b.id || '').trim()
		if (!id) continue

		const type = String((b.Box_type || b.box_type || b.BoxType) || '').trim().toLowerCase()
		const noPairBox = NO_PAIR_BOX_TYPES.indexOf(type) >= 0

		const chars = []
		for (let k = 1; k <= GACHA_MAX_CHARS; k++) {
			const c = b['character' + k]
			if (!c) continue

			let name = ''
			let url = ''
			let e1 = 0
			let e2 = 0
			let hot = false
			let noly = noPairBox

			if (typeof c === 'string') {
				name = c.trim()
			} else if (typeof c === 'object') {
				name = String(c.name || '').trim()
				url = String(c.imageUrl || c.imageURL || c.avatar || '').trim()
				const mp = c.market_price
				if (mp && typeof mp === 'object') {
					e1 = Number(mp.ELITE1 || 0) || 0
					e2 = Number(mp.ELITE2 || 0) || 0
				}
				hot = isTrueFlag(c.hotcharacter)
				if (isTrueFlag(c.nolyELITE1)) noly = true
			}
			if (!name && !url) continue

			chars.push({
				n: name,
				u: url,
				// 精二头像地址（原生侧只有精二卡才用；无精二的干员留空）
				u2: noly ? '' : elite2Url(url),
				e1: e1,
				e2: e2,
				h: hot ? 1 : 0,
				s: noly ? 1 : 0
			})
		}
		if (!chars.length) continue
		out.push({ id: id, t: type, c: chars })
	}
	return out
}

/**
 * 原始记录 -> 小组件 payload
 *
 * 每条 item 同时服务两种展示模式：
 *   列表模式 用 title / sub / badge
 *   卡片模式 用 release / replicate / size / price / chars
 *
 * @returns {{updated:number,total:number,header:string,items:Array}|null}
 */
export function buildPayload(boxes) {
	if (!Array.isArray(boxes) || !boxes.length) return null

	const rows = []
	for (let i = 0; i < boxes.length; i++) {
		const b = boxes[i]
		if (!b || typeof b !== 'object') continue

		const id = String(b.Box_id || b.box_id || b.id || '').trim()
		if (!id) continue

		const release = normDate(b.release_date)
		const replicate = normDate(b.replicate_date_01) || normDate(b.replicate_date)
		const price = String(b.retail_price || '').trim()
		const size = String(b.size || '').trim()

		rows.push({
			id: id,
			release: release,
			replicate: replicate,
			price: price,
			size: size,
			chars: readChars(b),
			sortKey: release || '0000/00/00'
		})
	}
	if (!rows.length) return null

	// 最新的排在最前——这是「逐个翻看盒号」最常用的视角
	rows.sort(function(a, b) {
		if (a.sortKey === b.sortKey) return 0
		return a.sortKey < b.sortKey ? 1 : -1
	})

	const items = []
	for (let i = 0; i < rows.length && i < MAX_ITEMS; i++) {
		const r = rows[i]
		const sub = []
		if (r.release) sub.push('首发 ' + r.release)
		if (r.price) sub.push(r.price)

		let badge = ''
		if (daysSince(r.release) <= NEW_DAYS) {
			badge = 'NEW'
		} else if (r.replicate && daysSince(r.replicate) <= NEW_DAYS) {
			badge = '复刻'
		}

		items.push({
			id: r.id,
			title: '盒号 ' + r.id,
			sub: sub.join(' · '),
			badge: badge,
			release: r.release,
			replicate: r.replicate,
			price: r.price,
			size: r.size,
			chars: r.chars
		})
	}

	return {
		updated: Date.now(),
		total: rows.length,
		header: HEADER,
		items: items,
		// 抽卡小组件用的完整卡池（含市价 / 热度 / 盒类型），与 items 相互独立
		gacha: {
			updated: Date.now(),
			boxes: readGachaBoxes(boxes)
		}
	}
}

/** 内容签名（不含 updated，否则每次都会判定为「有变化」） */
function signatureOf(payload) {
	try {
		// 卡池也要进签名：只改了市价 / 热度页时同样要重推一次
		return payload.total + '|' + JSON.stringify(payload.items) +
			'|' + (payload.gacha ? JSON.stringify(payload.gacha.boxes) : '')
	} catch (e) {
		return ''
	}
}

/**
 * 把当前数据推送到桌面小组件。
 * @param {boolean} force 忽略节流与去重（用户手动触发时用）
 * @returns {boolean} 是否真正执行了推送
 */
export function syncNow(force) {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return false
	try {
		const now = Date.now()
		if (!force && now - _lastSyncAt < SYNC_THROTTLE_MS) return false

		const payload = buildPayload(readBoxes())
		if (!payload) return false

		const sig = signatureOf(payload)
		if (!force && sig && sig === _lastSig) {
			_lastSyncAt = now
			return false
		}

		boxWidgetNative.sync(JSON.stringify(payload))
		_lastSig = sig
		_lastSyncAt = now
		console.log(
			'[小组件] 已推送', payload.items.length, '条 / 共', payload.total, '盒',
			'· 抽卡卡池', payload.gacha ? payload.gacha.boxes.length : 0, '盒'
		)
		return true
	} catch (e) {
		console.warn('[小组件] 推送失败', e)
		return false
	}
	// #endif

	// #ifndef APP-PLUS
	return false
	// #endif
}

/** 清空小组件数据 */
export function clearWidget() {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return false
	try {
		boxWidgetNative.clear()
		_lastSig = ''
		_lastSyncAt = 0
		return true
	} catch (e) {
		return false
	}
	// #endif

	// #ifndef APP-PLUS
	return false
	// #endif
}

/** 仅要求重绘，不改变数据 */
export function refreshWidget() {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return false
	try {
		boxWidgetNative.refresh()
		return true
	} catch (e) {
		return false
	}
	// #endif

	// #ifndef APP-PLUS
	return false
	// #endif
}

/**
 * 请求把小组件固定到桌面。
 * @returns {{ok:boolean, reason:string}} reason: '' | 'platform' | 'unsupported' | 'rejected' | 'error'
 */
export function requestPinWidget() {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return { ok: false, reason: 'platform' }
	try {
		// 先确保组件里有数据，否则加完是空卡片
		syncNow(true)
		if (!boxWidgetNative.isSupported()) {
			return { ok: false, reason: 'unsupported' }
		}
		const ok = boxWidgetNative.requestPin()
		return { ok: !!ok, reason: ok ? '' : 'rejected' }
	} catch (e) {
		console.warn('[小组件] 请求固定失败', e)
		return { ok: false, reason: 'error' }
	}
	// #endif

	// #ifndef APP-PLUS
	return { ok: false, reason: 'platform' }
	// #endif
}

/**
 * 请求把「模拟抽卡」小组件固定到桌面。
 *
 * 抽卡组件与盒号组件是两个独立的桌面组件，所以要分别发起固定请求。
 * 卡池与盒号数据一起推（payload 里独立的 gacha 段，含盒型/市价/热度），
 * 因此这里先 syncNow(true) 保证盒号数据已经在本地。
 *
 * @returns {{ok:boolean, reason:string}} reason: '' | 'platform' | 'unsupported' | 'rejected' | 'error'
 */
export function requestPinGachaWidget() {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return { ok: false, reason: 'platform' }
	try {
		// 卡池随盒号数据一起推：先确保已推送，否则加完是空卡片
		syncNow(true)
		if (!boxWidgetNative.isSupported()) {
			return { ok: false, reason: 'unsupported' }
		}
		const ok = boxWidgetNative.requestPinGacha()
		return { ok: !!ok, reason: ok ? '' : 'rejected' }
	} catch (e) {
		console.warn('[小组件] 请求固定抽卡组件失败', e)
		return { ok: false, reason: 'error' }
	}
	// #endif

	// #ifndef APP-PLUS
	return { ok: false, reason: 'platform' }
	// #endif
}

/** 当前环境是否支持桌面小组件 */
export function isWidgetSupported() {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return false
	try {
		return !!boxWidgetNative.isSupported()
	} catch (e) {
		return false
	}
	// #endif

	// #ifndef APP-PLUS
	return false
	// #endif
}

/** 打开某个页面；navigateTo 失败（页面栈满等）时降级为 reLaunch */
function openPage(url, retry) {
	uni.navigateTo({
		url: url,
		fail: function() {
			if (retry > 0) {
				setTimeout(function() { openPage(url, retry - 1) }, 400)
			} else {
				uni.reLaunch({ url: url, fail: function() {} })
			}
		}
	})
}

/** 跳转到盒号详情页 */
function openBoxDetail(boxId, retry) {
	openPage('/pages/box_info/box_info?boxId=' + encodeURIComponent(boxId), retry)
}

/**
 * 解析并处理 scheme 启动参数。
 *
 * 小组件点击后唤起 App 的 URI 形如：
 *   akpassport://widget                 点头部 / 空态 -> 只唤起，不跳页
 *   akpassport://widget?boxId=1.0       点卡片 / 列表项 -> 直达该盒号详情
 *   akpassport://widget?action=search   点「搜索」      -> 直达搜索页
 *   akpassport://widget?action=gacha    点抽卡组件标题  -> 直达模拟抽卡页
 *
 * @param {string} args plus.runtime.arguments
 * @returns {boolean} 是否识别为本插件的 scheme
 */
export function handleSchemeArgs(args) {
	const s = String(args === undefined || args === null ? '' : args).trim()
	if (!s || s.indexOf(SCHEME_PREFIX) !== 0) return false

	const rest = s.substring(SCHEME_PREFIX.length)
	const qIndex = rest.indexOf('?')
	const path = (qIndex >= 0 ? rest.substring(0, qIndex) : rest).replace(/\/+$/, '')
	const queryStr = qIndex >= 0 ? rest.substring(qIndex + 1) : ''

	const query = {}
	if (queryStr) {
		queryStr.split('&').forEach(function(pair) {
			if (!pair) return
			const eq = pair.indexOf('=')
			const k = eq >= 0 ? pair.substring(0, eq) : pair
			const v = eq >= 0 ? pair.substring(eq + 1) : ''
			if (!k) return
			try {
				query[decodeURIComponent(k)] = decodeURIComponent(v)
			} catch (e) {
				query[k] = v
			}
		})
	}

	if (path && path !== 'widget') return false

	const boxId = query.boxId || query.box_id || ''
	const action = query.action || ''

	// 冷启动时 onLaunch / onShow 会各触发一次，做一次短窗口去重
	const key = boxId ? 'box:' + boxId : (action ? 'act:' + action : 'home')
	const now = Date.now()
	if (key === _lastRouteKey && now - _lastRouteAt < 1500) return true
	_lastRouteKey = key
	_lastRouteAt = now

	if (boxId) {
		// 首屏尚未就绪时 navigateTo 会失败，留一点时间再跳（内部还带重试）
		setTimeout(function() { openBoxDetail(boxId, 2) }, 400)
		console.log('[小组件] 跳转盒号详情', boxId)
	} else if (action === 'search') {
		setTimeout(function() { openPage('/pages/Search/Search', 2) }, 400)
		console.log('[小组件] 跳转搜索页')
	} else if (action === 'gacha') {
		setTimeout(function() { openPage('/packageB/pages/gacha_simulated/gacha_simulated', 2) }, 400)
		console.log('[小组件] 跳转模拟抽卡页')
	}

	return true
}

// ==================== 抽卡回写：小组件 -> App 页面 ====================
//
// 桌面小组件里抽的卡无法直接写 App 的 Storage（不在同一个 JS 上下文），
// 所以原生侧把每次抽卡追加进一份日志；App 启动 / 回到前台时由这里取走合并。
//
// 合并遵循「页面是唯一事实来源」：
//   1. 读日志（原生 append-only，id 单调递增 -> 天然幂等）
//   2. 逐条还原成页面 createCard() 形状的卡片，写进 gacha_history（新的在前）
//   3. 累加 gacha_pnl_daily / gacha_pnl_total
//   4. 把「整池顺序」还原成 gacha_progress_<boxId>（原生抽的是同一份池子）
//   5. 用页面算出的权威盈亏回推原生，两边收敛
//   6. 清空日志

/** 与页面 addToHistory() 的上限保持一致 */
const GACHA_HISTORY_MAX = 100
/** 防止 App.onShow 连续触发时重入 */
let _draining = false

/** yyyy-MM-dd，与页面 getTodayStr() 一致 */
function todayStr() {
	const d = new Date()
	const m = pad2(d.getMonth() + 1)
	const day = pad2(d.getDate())
	return d.getFullYear() + '-' + m + '-' + day
}

/** YYYY年MM月DD日 HH:mm:ss，与页面 formatTime() 一致 */
function formatTime(ts) {
	const d = new Date(ts)
	return d.getFullYear() + '年' + pad2(d.getMonth() + 1) + '月' + pad2(d.getDate()) + '日 ' +
		pad2(d.getHours()) + ':' + pad2(d.getMinutes()) + ':' + pad2(d.getSeconds())
}

/**
 * 把一盒展开成页面 generateAllCards() 的卡片列表。
 *
 * 顺序必须与原生 AkGachaStore.savePoolFromPayload 的展开顺序**逐位一致** ——
 * 日志里的整池顺序是按这个下标记的，错一位就会「抽到的是 A，页面记成 B」。
 * 展开规则（两边同源）：
 *   整盒无精二（ambience/special/whitelist）或干员自带 nolyELITE1 -> 只有精一
 *   否则 -> 精一 + 精二，且精一在前
 */
function buildBoxCards(box) {
	const out = []
	if (!box || typeof box !== 'object') return out
	const noPairBox = isNoPairBox(box)
	for (let k = 1; k <= GACHA_MAX_CHARS; k++) {
		const c = box['character' + k]
		if (!c) continue

		let name = ''
		let url = ''
		let e1 = 0
		let e2 = 0
		let hot = false
		let noly = noPairBox

		if (typeof c === 'string') {
			name = c.trim()
		} else if (typeof c === 'object') {
			name = String(c.name || '').trim()
			url = String(c.imageUrl || c.imageURL || c.avatar || '').trim()
			const mp = c.market_price
			if (mp && typeof mp === 'object') {
				e1 = Number(mp.ELITE1 || 0) || 0
				e2 = Number(mp.ELITE2 || 0) || 0
			}
			hot = isTrueFlag(c.hotcharacter)
			if (isTrueFlag(c.nolyELITE1)) noly = true
		}
		if (!name && !url) continue

		const base = { name: name, avatar: url, hotcharacter: hot, market_price: null }
		if (noly) {
			out.push(Object.assign({}, base, { nolyELITE1: true, eliteLevel: 1, price: e1 }))
		} else {
			out.push(Object.assign({}, base, { nolyELITE1: false, eliteLevel: 1, price: e1 }))
			out.push(Object.assign({}, base, { nolyELITE1: false, eliteLevel: 2, price: e2 }))
		}
	}
	return out
}

/** 日志里的紧凑卡片 -> 页面 character 对象：[名称,头像,精炼,盒号,盒型,市价,热门] */
function cardFromJournal(a) {
	return {
		name: String(a[0] || ''),
		avatar: String(a[1] || ''),
		hotcharacter: a[6] === 1,
		market_price: null,
		eliteLevel: a[2] || 1,
		price: a[5] || 0,
		sourceBox: String(a[3] || ''),
		boxType: String(a[4] || 'normal')
	}
}

/**
 * 取走并合并小组件的抽卡日志。
 *
 * @returns {{ok:boolean, merged:number, reason:string}}
 *          reason: '' | 'platform' | 'empty' | 'error'
 */
export function drainGachaJournal() {
	// #ifdef APP-PLUS
	if (!isAndroidRuntime()) return { ok: false, merged: 0, reason: 'platform' }
	if (_draining) return { ok: false, merged: 0, reason: 'busy' }
	_draining = true
	try {
		let raw = ''
		try {
			raw = boxWidgetNative.gachaJournal() || ''
		} catch (e) {
			return { ok: false, merged: 0, reason: 'error' }
		}
		raw = String(raw).trim()
		if (!raw || raw === '[]') return { ok: false, merged: 0, reason: 'empty' }

		let entries = null
		try { entries = JSON.parse(raw) } catch (e) { entries = null }
		if (!Array.isArray(entries) || !entries.length) {
			// 解析不了也要清掉，否则每次前台都白跑一遍
			safeClearJournal()
			return { ok: false, merged: 0, reason: 'error' }
		}

		// ---- 1. 抽卡历史 ----
		const history = Array.isArray(uni.getStorageSync('gacha_history'))
			? uni.getStorageSync('gacha_history') : []
		const markers = uni.getStorageSync('gacha_box_markers') || {}
		const startMarkers = markers.startMarkers || {}
		const endMarkers = markers.endMarkers || {}

		// ---- 2. 今日 / 累计盈亏 ----
		const today = todayStr()
		let daily = uni.getStorageSync('gacha_pnl_daily')
		if (!daily || daily.date !== today) {
			daily = { date: today, balance: 1000, today: 0, drawCount: 0 }
		}
		const total = uni.getStorageSync('gacha_pnl_total') || {}
		const pnlTotal = {
			cost: typeof total.cost === 'number' ? total.cost : 0,
			gain: typeof total.gain === 'number' ? total.gain : 0,
			count: typeof total.count === 'number' ? total.count : 0
		}

		// ---- 3. 盒进度（同一盒可能有多条，取最后一条的下标；整池顺序只在首条里） ----
		const progressWrites = {}
		const boxes = readBoxes()

		let merged = 0
		for (let i = 0; i < entries.length; i++) {
			const e = entries[i]
			if (!e || typeof e !== 'object') continue
			const kind = String(e.k || '')
			const isMix = kind === 'mix' || kind === 'mix10'
			const boxId = String(e.b || '')

			// 开新盒：只重置进度，不计历史/盈亏
			if (kind === 'boxreset') {
				if (boxId) {
					progressWrites[boxId] = { order: e.o || null, index: 0, boxId: boxId }
				}
				continue
			}

			let cards = []
			try { cards = JSON.parse(e.dc || '[]') } catch (err) { cards = [] }

			// 还原成页面卡片对象，并倒序 unshift -> 最终最新的在最前
			for (let k = 0; k < cards.length; k++) {
				const character = cardFromJournal(cards[k] || [])
				if (!character.name) continue
				const inBox = isMix ? 0 : (e.i || 0)
				history.unshift({
					character: character,
					boxId: isMix ? '超级大混池' : (boxId || '超级大混池'),
					boxType: isMix ? character.boxType : String(e.bt || 'normal'),
					eliteLevel: character.eliteLevel,
					time: formatTime(e.ts || Date.now()),
					isFirstInBox: !isMix && inBox === 1,
					isLastInBox: false,
					isSuperMixPool: isMix
				})
			}
			if (history.length > GACHA_HISTORY_MAX) history.length = GACHA_HISTORY_MAX

			daily.balance += (e.gain || 0) - (e.cost || 0)
			daily.today += (e.gain || 0) - (e.cost || 0)
			daily.drawCount += e.cnt || 0
			pnlTotal.cost += e.cost || 0
			pnlTotal.gain += e.gain || 0
			pnlTotal.count += e.cnt || 0

			if (!isMix && boxId) {
				const prev = progressWrites[boxId]
				progressWrites[boxId] = {
					order: (prev && prev.order) || e.o || null,
					index: e.i || 0,
					boxId: boxId
				}
			}
			merged++
		}
		if (merged === 0) {
			// 只有 boxreset 之类的条目，也要把进度落下去
			applyBoxProgress(progressWrites, boxes, startMarkers, endMarkers)
			safeClearJournal()
			return { ok: true, merged: 0, reason: '' }
		}

		// ---- 4. 落盘 ----
		try {
			uni.setStorageSync('gacha_history', history)
			uni.setStorageSync('gacha_pnl_daily', {
				date: today, balance: daily.balance, today: daily.today, drawCount: daily.drawCount
			})
			uni.setStorageSync('gacha_pnl_total', pnlTotal)
		} catch (e) {
			console.warn('[小组件] 抽卡回写落盘失败', e)
		}
		applyBoxProgress(progressWrites, boxes, startMarkers, endMarkers)
		try {
			uni.setStorageSync('gacha_box_markers', { startMarkers: startMarkers, endMarkers: endMarkers })
		} catch (e) {}

		// ---- 5. 用页面权威盈亏回推原生，两边收敛 ----
		try {
			boxWidgetNative.gachaSetPnl(
				daily.balance, daily.today, daily.drawCount,
				pnlTotal.cost, pnlTotal.gain, pnlTotal.count
			)
		} catch (e) {}

		safeClearJournal()
		console.log('[小组件] 已合并抽卡记录', merged, '条')
		return { ok: true, merged: merged, reason: '' }
	} catch (e) {
		console.warn('[小组件] 合并抽卡记录失败', e)
		return { ok: false, merged: 0, reason: 'error' }
	} finally {
		_draining = false
	}
	// #endif

	// #ifndef APP-PLUS
	return { ok: false, merged: 0, reason: 'platform' }
	// #endif
}

/** 把日志里的整池顺序还原成页面的 gacha_progress_<boxId> */
function applyBoxProgress(writes, boxes, startMarkers, endMarkers) {
	const ids = Object.keys(writes || {})
	for (let i = 0; i < ids.length; i++) {
		const id = ids[i]
		const w = writes[id]
		const box = findBox(boxes, id)
		if (!box) continue
		const pool = buildBoxCards(box)
		if (!pool.length) continue

		// order 是「按下标抽取的次序」。三道防线，避免把页面池序覆盖错：
		//   1) 有 order 且长度对得上 -> 按它重建（权威，原生刚洗过牌）
		//   2) 没有 order -> 沿用页面已有的池序（只推进下标，不动池子）
		//   3) 都没有 -> 才退回按原序构建
		const key = 'gacha_progress_' + id
		const existing = uni.getStorageSync(key)
		let ordered = pool
		if (Array.isArray(w.order) && w.order.length === pool.length) {
			ordered = w.order.map(function(idx) {
				return pool[idx] || pool[0]
			})
		} else if (existing && Array.isArray(existing.currentBoxPool) &&
			existing.currentBoxPool.length === pool.length) {
			ordered = existing.currentBoxPool
		}
		const index = Math.max(0, Math.min(w.index || 0, ordered.length))
		try {
			uni.setStorageSync(key, {
				currentBoxPool: ordered,
				currentIndex: index,
				drawnCards: ordered.slice(0, index)
			})
		} catch (e) {}

		if (index > 0) startMarkers[id] = true
		else delete startMarkers[id]
		if (index >= ordered.length) endMarkers[id] = true
		else delete endMarkers[id]
	}
}

function findBox(boxes, id) {
	if (!Array.isArray(boxes)) return null
	for (let i = 0; i < boxes.length; i++) {
		const b = boxes[i]
		if (!b || typeof b !== 'object') continue
		const bid = String(b.Box_id || b.box_id || b.id || '').trim()
		if (bid === id) return b
	}
	return null
}

/** 清空原生日志；失败也不能让调用方炸掉 */
function safeClearJournal() {
	try {
		boxWidgetNative.clearGachaJournal()
	} catch (e) {}
}

export default {
	buildPayload,
	drainGachaJournal,
	syncNow,
	clearWidget,
	refreshWidget,
	requestPinWidget,
	requestPinGachaWidget,
	isWidgetSupported,
	handleSchemeArgs,
	drainGachaJournal
}
