/**
 * 干员头像展示优先级（个性化设置）
 * ==================================================================
 * 需求要点
 *   1. 全局默认：精一 / 精二
 *   2. 条件规则：按 PRTS 干员一览的筛选维度（职业/分支/稀有度/位置/性别/
 *      获取途径/词缀）＋ 项目自有维度（盒类型/盲抽单领/复刻状态/发布年份），
 *      逐条决定「满足该条件时优先展示哪一档头像」
 *   3. 名单：单独指定某位干员强制精二 / 强制精一
 *   4. 导入导出：整份设置以 JSON 文本搬运
 *
 * 判定优先级（高 → 低）
 *   黑名单(强制精一) > 白名单(强制精二) > 规则（按顺序，首条命中） > 全局默认
 *
 * 兜底：无论怎么判定，只要该干员没有精二头像（NO_ELITE2_NAMES、非 PRTS 图源、
 * 或盒型本身无精一精二之分），一律回落精一，不会出现裂图。
 *
 * 该模块同时给「页面筛选项」（matchCharFacets）复用同一套元数据语义。
 */

import { charMeta, PROFESSIONS, SUB_PROFESSIONS, RARITIES, POSITIONS, GENDERS, OBTAINS, TAGS } from './charMeta.js'
import { elite2Url, NO_ELITE2_NAMES, isNoPairBoxType } from './eliteAvatar.js'

export const STORAGE_KEY = 'avatarPrefSettings'
export const SCHEMA_VERSION = 1

/** 全局默认档位 */
export const LEVEL_E1 = 0
export const LEVEL_E2 = 2

/** 条件分组定义：key → 展示名 */
export const COND_GROUPS = [
	{ key: 'prof', label: '职业', multi: true },
	{ key: 'sub', label: '分支', multi: true },
	{ key: 'rar', label: '稀有度', multi: true },
	{ key: 'pos', label: '位置', multi: true },
	{ key: 'sex', label: '性别', multi: true },
	{ key: 'obt', label: '获取途径', multi: true },
	{ key: 'tag', label: '词缀', multi: true },
	{ key: 'boxType', label: '单盒类型', multi: true },
	{ key: 'sale', label: '盲抽/单领', multi: true },
	{ key: 'rep', label: '复刻状态', multi: true },
	{ key: 'year', label: '发布年份', multi: true }
]

/** 项目自有盒类型（与列表/查询页 filterMap 对齐） */
export const BOX_TYPES = [
	{ value: 'normal', label: '常规款' },
	{ value: 'whitelist', label: '白名单凭证' },
	{ value: 'special', label: '特别通行认证' },
	{ value: 'cooperation', label: '联动款' },
	{ value: 'ambience', label: '音律联觉通行证' }
]

export const SALE_TYPES = [
	{ value: 'true', label: '盲抽' },
	{ value: 'false', label: '单领/赠品' }
]

export const REPLICATE_TYPES = [
	{ value: 'true', label: '有复刻' },
	{ value: 'false', label: '无复刻' }
]

/** 元数据维度 key（用于区分「需要干员元数据」的条件） */
const META_KEYS = ['prof', 'sub', 'rar', 'pos', 'sex', 'obt', 'tag']
const BOX_KEYS = ['boxType', 'sale', 'rep', 'year']
const ALL_KEYS = META_KEYS.concat(BOX_KEYS)

/** 纯干员属性维度（页面筛选用），不含盒属性 */
export const FACET_GROUPS = COND_GROUPS.filter((g) => META_KEYS.indexOf(g.key) >= 0)

export function emptyFacets() {
	return { prof: [], sub: [], rar: [], pos: [], sex: [], obt: [], tag: [] }
}

export function emptyCond() {
	return {
		prof: [], sub: [], rar: [], pos: [], sex: [], obt: [], tag: [],
		boxType: [], sale: [], rep: [], year: []
	}
}

export function defaultSettings() {
	return { v: SCHEMA_VERSION, def: LEVEL_E1, rules: [], white: [], black: [] }
}

/* ------------------------------------------------------------------ *
 * 设置读写
 * ------------------------------------------------------------------ */

let _cache = null

function toArray(v) {
	if (!v) return []
	if (Array.isArray(v)) return v.filter((x) => x !== null && x !== undefined && x !== '')
	return [v]
}

/** 清洗外部传入（本地存储 / 导入文本）的设置，保证结构始终合法 */
export function normalizeSettings(raw) {
	const base = defaultSettings()
	if (!raw || typeof raw !== 'object') return base

	base.def = Number(raw.def) === LEVEL_E2 ? LEVEL_E2 : LEVEL_E1

	base.white = toArray(raw.white).map(String).filter(Boolean)
	base.black = toArray(raw.black).map(String).filter(Boolean)

	const rules = Array.isArray(raw.rules) ? raw.rules : []
	base.rules = rules
		.map((r, i) => {
			if (!r || typeof r !== 'object') return null
			const cond = emptyCond()
			ALL_KEYS.forEach((k) => {
				cond[k] = toArray(r.cond && r.cond[k]).map((x) => String(x)).filter(Boolean)
			})
			// 规则至少要有一个条件，否则等价于全局默认，容易误配
			const used = ALL_KEYS.some((k) => cond[k].length)
			if (!used) return null
			return {
				id: r.id || 'r' + Date.now().toString(36) + i,
				level: Number(r.level) === LEVEL_E2 ? LEVEL_E2 : LEVEL_E1,
				cond
			}
		})
		.filter(Boolean)

	return base
}

/** 读取设置（带缓存；外部改过 storage 时调用 invalidate） */
export function getSettings() {
	if (_cache) return _cache
	let raw = null
	try {
		raw = uni.getStorageSync(STORAGE_KEY)
	} catch (e) {
		raw = null
	}
	if (raw && typeof raw === 'string') {
		try { raw = JSON.parse(raw) } catch (e2) { raw = null }
	}
	_cache = normalizeSettings(raw)
	return _cache
}

export function saveSettings(s) {
	const next = normalizeSettings(s)
	_cache = next
	try {
		uni.setStorageSync(STORAGE_KEY, JSON.stringify(next))
	} catch (e) {
		/* 存储失败时至少保证本次会话生效 */
	}
	return next
}

/** 丢弃缓存（设置页保存后、目标页 onShow 时调用） */
export function invalidate() {
	_cache = null
}

export function resetSettings() {
	return saveSettings(defaultSettings())
}

/* ------------------------------------------------------------------ *
 * 条件匹配
 * ------------------------------------------------------------------ */

function hit(list, value) {
	return Array.isArray(list) && list.length > 0 && list.indexOf(value) >= 0
}

function hitAny(list, values) {
	if (!Array.isArray(list) || !list.length) return false
	return values.some((v) => list.indexOf(v) >= 0)
}

/**
 * 条件是否命中
 * - 同一分组内多选为「或」
 * - 不同分组之间为「与」
 * - 空分组视为不限
 *
 * @param {object} cond 条件
 * @param {string} name 干员名
 * @param {object} [ctx] 盒上下文 { boxType, sale, rep, year }
 */
export function matchCond(cond, name, ctx) {
	if (!cond) return false
	const c = ctx || {}

	let useMeta = false
	for (let i = 0; i < META_KEYS.length; i++) {
		if (cond[META_KEYS[i]] && cond[META_KEYS[i]].length) { useMeta = true; break }
	}

	if (useMeta) {
		const meta = charMeta(name)
		if (!meta) return false
		if (cond.prof.length && !hit(cond.prof, meta.profession)) return false
		if (cond.sub.length && !hit(cond.sub, meta.sub)) return false
		if (cond.rar.length && !hit(cond.rar, String(meta.rarity))) return false
		if (cond.pos.length && !hitAny(cond.pos, meta.positions)) return false
		if (cond.sex.length && !hit(cond.sex, meta.gender)) return false
		if (cond.obt.length && !hit(cond.obt, meta.obtain)) return false
		if (cond.tag.length && !hitAny(cond.tag, meta.tags)) return false
	}

	if (cond.boxType.length) {
		if (!c.boxType || !hit(cond.boxType, String(c.boxType))) return false
	}
	if (cond.sale.length) {
		if (c.sale === undefined || c.sale === null || c.sale === '') return false
		if (!hit(cond.sale, String(c.sale))) return false
	}
	if (cond.rep.length) {
		if (c.rep === undefined || c.rep === null || c.rep === '') return false
		if (!hit(cond.rep, String(c.rep))) return false
	}
	if (cond.year.length) {
		if (!c.year) return false
		if (!hit(cond.year, String(c.year))) return false
	}

	return true
}

/**
 * 干员维度筛选（供列表/查询页筛选用）
 * @param {object} facets { prof:[], sub:[], rar:[], pos:[], sex:[], obt:[], tag:[] }
 * @param {string} name
 */
export function matchCharFacets(facets, name) {
	if (!facets) return true
	let used = false
	for (let i = 0; i < META_KEYS.length; i++) {
		if (facets[META_KEYS[i]] && facets[META_KEYS[i]].length) { used = true; break }
	}
	if (!used) return true

	const meta = charMeta(name)
	if (!meta) return false

	if (facets.prof && facets.prof.length && !hit(facets.prof, meta.profession)) return false
	if (facets.sub && facets.sub.length && !hit(facets.sub, meta.sub)) return false
	if (facets.rar && facets.rar.length && !hit(facets.rar, String(meta.rarity))) return false
	if (facets.pos && facets.pos.length && !hitAny(facets.pos, meta.positions)) return false
	if (facets.sex && facets.sex.length && !hit(facets.sex, meta.gender)) return false
	if (facets.obt && facets.obt.length && !hit(facets.obt, meta.obtain)) return false
	if (facets.tag && facets.tag.length && !hitAny(facets.tag, meta.tags)) return false
	return true
}

/** facets 是否有任何已选条件 */
export function hasFacets(facets) {
	if (!facets) return false
	return META_KEYS.some((k) => facets[k] && facets[k].length)
}

/* ------------------------------------------------------------------ *
 * 头像解析
 * ------------------------------------------------------------------ */

/** 能否取到精二头像 */
export function canElite2(name, elite1Url) {
	if (!name || !elite1Url) return false
	if (NO_ELITE2_NAMES.indexOf(String(name)) >= 0) return false
	if (String(elite1Url).indexOf('media.prts.wiki/') < 0) return false
	return !!elite2Url(elite1Url)
}

/**
 * 判定某干员应展示的档位
 * @param {string} name
 * @param {object} [ctx] { boxType, sale, rep, year }
 * @returns {number} 0 = 精一，2 = 精二
 */
export function resolveLevel(name, ctx) {
	const s = getSettings()
	const nm = String(name || '')

	if (s.black.indexOf(nm) >= 0) return LEVEL_E1
	if (s.white.indexOf(nm) >= 0) return LEVEL_E2

	for (let i = 0; i < s.rules.length; i++) {
		if (matchCond(s.rules[i].cond, nm, ctx)) return s.rules[i].level
	}
	return s.def
}

/**
 * 取最终展示用的头像地址
 * @param {string} name 干员名
 * @param {string} elite1Url 精一头像地址
 * @param {object} [ctx] 盒上下文 { boxType, sale, rep, year }
 * @returns {string}
 */
export function avatarFor(name, elite1Url, ctx) {
	if (!elite1Url) return ''
	if (ctx && isNoPairBoxType(ctx.boxType)) return elite1Url
	if (resolveLevel(name, ctx) !== LEVEL_E2) return elite1Url
	if (!canElite2(name, elite1Url)) return elite1Url
	return elite2Url(elite1Url) || elite1Url
}

/** 直接对角色对象求值（兼容 avatar / imageUrl 两种字段） */
export function avatarOfChar(character, ctx) {
	if (!character) return ''
	const base = character.avatar || character.imageUrl || ''
	return avatarFor(character.name, base, ctx || {
		boxType: character.boxType || character.box_type || character.Box_type || '',
		sale: character.sale,
		rep: character.rep,
		year: character.year
	})
}

/** 由盒子对象构造上下文 */
export function boxCtx(box) {
	if (!box) return null
	const date = box.release_date || ''
	const year = date ? String(date).slice(0, 4) : ''
	// 盒类型：上层规整后一般写在 Box_type（normal/whitelist/special/cooperation/ambience）
	let boxType = box.Box_type || box.box_type || box.boxType || box.typeKey || ''
	if (!boxType) {
		// 盒数据里 type 表示盲抽/单领，盒类型由上层 normalize 后写入 box_type
		boxType = ''
	}
	return {
		boxType: boxType,
		sale: box.type === undefined || box.type === null ? '' : String(box.type),
		rep: box.replicate === undefined || box.replicate === null ? '' : String(box.replicate),
		year: year
	}
}

/* ------------------------------------------------------------------ *
 * 导入 / 导出
 * ------------------------------------------------------------------ */

export function exportSettings() {
	return JSON.stringify(getSettings(), null, 2)
}

/**
 * 导入设置
 * @param {string} text JSON 文本
 * @returns {{ok:boolean, msg:string, settings?:object}}
 */
export function importSettings(text) {
	if (!text || typeof text !== 'string' || !text.trim()) {
		return { ok: false, msg: '内容为空' }
	}
	let raw
	try {
		raw = JSON.parse(text)
	} catch (e) {
		return { ok: false, msg: '不是合法的 JSON' }
	}
	if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
		return { ok: false, msg: '结构不正确：应为对象' }
	}
	if (raw.rules !== undefined && !Array.isArray(raw.rules)) {
		return { ok: false, msg: '结构不正确：rules 应为数组' }
	}
	if (raw.kind === 'preferences' && raw.data) {
		return { ok: false, msg: '这是「整机个性化备份」，请用上方「导入全部」' }
	}
	if (raw.rules === undefined && raw.white === undefined && raw.black === undefined) {
		return { ok: false, msg: '不是头像优先级设置（缺少 rules / white / black）' }
	}
	if (raw.v !== undefined && Number(raw.v) > SCHEMA_VERSION) {
		return { ok: false, msg: '版本过新（v' + raw.v + '），请升级 App 后再导入' }
	}

	const next = normalizeSettings(raw)
	return {
		ok: true,
		msg: '已导入 ' + next.rules.length + ' 条规则、' + next.white.length + ' 个精二名单、' + next.black.length + ' 个精一名单',
		settings: next
	}
}

/* ------------------------------------------------------------------ *
 * 展示辅助（设置页用）
 * ------------------------------------------------------------------ */

const LABEL_MAP = {}

function labelOf(key, value) {
	if (key === 'rar') return value + '★'
	if (key === 'boxType') {
		const t = BOX_TYPES.find((x) => x.value === String(value))
		return t ? t.label : String(value)
	}
	if (key === 'sale') {
		const t = SALE_TYPES.find((x) => x.value === String(value))
		return t ? t.label : String(value)
	}
	if (key === 'rep') {
		const t = REPLICATE_TYPES.find((x) => x.value === String(value))
		return t ? t.label : String(value)
	}
	if (key === 'year') return value + '年'
	return String(value)
}

/** 规则条件的可读摘要，如「职业：术师、狙击 · 稀有度：6★」 */
export function condSummary(cond) {
	if (!cond) return '不限'
	const parts = []
	COND_GROUPS.forEach((g) => {
		const arr = cond[g.key]
		if (arr && arr.length) {
			const labels = arr.map((v) => labelOf(g.key, v))
			const text = labels.length > 4 ? labels.slice(0, 4).join('、') + ' 等' + labels.length + '项' : labels.join('、')
			parts.push(g.label + '：' + text)
		}
	})
	return parts.length ? parts.join(' · ') : '不限'
}

/** 某分组可选值（交给设置页渲染） */
export function optionsOf(key, pickedProf) {
	if (key === 'prof') return PROFESSIONS.slice()
	if (key === 'sub') {
		if (pickedProf && pickedProf.length) {
			const out = []
			pickedProf.forEach((p) => {
				;(SUB_PROFESSIONS[p] || []).forEach((s) => { if (out.indexOf(s) < 0) out.push(s) })
			})
			return out
		}
		const out = []
		Object.keys(SUB_PROFESSIONS).forEach((p) => SUB_PROFESSIONS[p].forEach((s) => { if (out.indexOf(s) < 0) out.push(s) }))
		return out
	}
	if (key === 'rar') return RARITIES.map(String)
	if (key === 'pos') return POSITIONS.slice()
	if (key === 'sex') return GENDERS.slice()
	if (key === 'obt') return OBTAINS.slice()
	if (key === 'tag') return TAGS.slice()
	if (key === 'boxType') return BOX_TYPES.map((x) => x.value)
	if (key === 'sale') return SALE_TYPES.map((x) => x.value)
	if (key === 'rep') return REPLICATE_TYPES.map((x) => x.value)
	return []
}

export function optionLabel(key, value) {
	return labelOf(key, value)
}

/** 发布年份候选（2020 起） */
export function yearOptions(nowYear) {
	const end = nowYear || new Date().getFullYear()
	const out = []
	for (let y = end; y >= 2020; y--) out.push(String(y))
	return out
}

export default {
	STORAGE_KEY,
	LEVEL_E1,
	LEVEL_E2,
	COND_GROUPS,
	FACET_GROUPS,
	emptyFacets,
	BOX_TYPES,
	SALE_TYPES,
	REPLICATE_TYPES,
	emptyCond,
	defaultSettings,
	normalizeSettings,
	getSettings,
	saveSettings,
	invalidate,
	resetSettings,
	matchCond,
	matchCharFacets,
	hasFacets,
	canElite2,
	resolveLevel,
	avatarFor,
	avatarOfChar,
	boxCtx,
	exportSettings,
	importSettings,
	condSummary,
	optionsOf,
	optionLabel,
	yearOptions
}
