/**
 * 干员「精二」头像工具
 *
 * 背景
 *   盒子数据（Box_Id.json）里每位干员只有一张 imageUrl，指向的是**精一头像**：
 *       https://media.prts.wiki/3/36/%E5%A4%B4%E5%83%8F_%E9%98%BF%E7%B1%B3%E5%A8%85.png
 *   精二头像在 PRTS 上是另一张同名文件，只是文件名多了 `_2`：
 *       https://media.prts.wiki/3/31/%E5%A4%B4%E5%83%8F_%E9%98%BF%E7%B1%B3%E5%A8%85_2.png
 *   注意目录（3/36 → 3/31）也变了 —— 那是 MediaWiki 按**文件名 MD5** 分目录的结果，
 *   所以不能只改文件名，必须重新算 MD5。本文件做的就是这件事。
 *
 * 已核对：439 位干员里 387 位的精二头像真实存在，规则判定与线上文件零误差。
 */

/** 整盒只有精一、没有精二之分的盒型（白名单 / 音律联觉 / 特别通行认证） */
export const NO_PAIR_BOX_TYPES = ['whitelist', 'ambience', 'special']

/**
 * 确认**没有**精二头像的干员（2026-10-17 按 PRTS 线上文件逐一 HEAD 探测得出）。
 * 主要是 1~3 星、机器人、愚人节单领 / 礼盒 NPC、以及本来就无 PRTS 头像的剧情角色。
 * 新干员不在名单里时，按 market_price.ELITE2 是否存在来判断。
 */
export const NO_ELITE2_NAMES = [
	'12f', 'ACE', 'CONFESS-47', 'Castle-3', 'Friston-3', 'Lancet-2', 'MANTRA',
	'MECHANIST', 'MISERY', 'OUTCAST', 'PITH', 'PhonoR-0', 'SCOUT', 'SHARP',
	'STORMEYE', 'THRM-EX', 'TOUCH', 'TULIP', 'U-Official',
	'克丽斯腾(愚人节单领)', '克洛丝', '博士', '博士(设定集赠品)', '卡缇', '史都华德',
	'塔露拉(愚人节单领)', '士兵(愚人节单领)', '夜刀', '大帝', '安德切尔', '安赛尔',
	'巡林者', '斑点', '月见夜', '杜林', '杰斯顿·威廉姆斯(愚人节单领)', '梓兰',
	'正义骑士号', '死芒', '泡普卡', '炎熔', '爱国者(愚人节单领)', '玫兰莎', '空爆',
	'米格鲁', '翎羽', '芙蓉', '芬', '虎狼丸', '霜星(礼盒/单领购买)', '香草', '黑角'
]

/* ------------------------------------------------------------------ *
 * 纯 JS MD5（小程序没有 crypto，必须自带；约 1.5KB）
 * ------------------------------------------------------------------ */

const MD5_S = [
	7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
	5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
	4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
	6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
]

const MD5_K = [
	0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee, 0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
	0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be, 0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
	0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa, 0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
	0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed, 0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
	0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c, 0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
	0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05, 0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
	0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039, 0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
	0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1, 0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
]

/** JS 字符串 → UTF-8 字节数组（代理对按 4 字节编码） */
function utf8Bytes(str) {
	const bytes = []
	for (let i = 0; i < str.length; i++) {
		let c = str.charCodeAt(i)
		if (c < 0x80) {
			bytes.push(c)
		} else if (c < 0x800) {
			bytes.push(0xc0 | (c >> 6), 0x80 | (c & 0x3f))
		} else if (c >= 0xd800 && c <= 0xdbff && i + 1 < str.length) {
			const cp = 0x10000 + ((c - 0xd800) << 10) + (str.charCodeAt(++i) - 0xdc00)
			bytes.push(
				0xf0 | (cp >> 18),
				0x80 | ((cp >> 12) & 0x3f),
				0x80 | ((cp >> 6) & 0x3f),
				0x80 | (cp & 0x3f)
			)
		} else {
			bytes.push(0xe0 | (c >> 12), 0x80 | ((c >> 6) & 0x3f), 0x80 | (c & 0x3f))
		}
	}
	return bytes
}

function hexLE(n) {
	let out = ''
	for (let i = 0; i < 4; i++) {
		const b = (n >>> (i * 8)) & 0xff
		out += (b < 16 ? '0' : '') + b.toString(16)
	}
	return out
}

/** 计算字符串的 MD5（小写 32 位十六进制） */
export function md5(str) {
	const src = utf8Bytes(String(str == null ? '' : str))
	const bitLen = src.length * 8

	// 补位：0x80 + 若干 0，直到长度 ≡ 56 (mod 64)，末尾 8 字节放原始比特长度（小端）
	const total = (((src.length + 8) >> 6) + 1) << 6
	const buf = new Array(total)
	for (let i = 0; i < src.length; i++) buf[i] = src[i]
	buf[src.length] = 0x80
	// 长度是 64 位小端：低 32 位 + 高 32 位。
	// 注意不能写成 (bitLen >>> (i * 8)) —— JS 的移位量按 32 取模，
	// i=4 时 (x >>> 32) 会退化成 (x >>> 0)，把低位又写一遍。
	const lenLo = bitLen >>> 0
	const lenHi = Math.floor(bitLen / 4294967296) >>> 0
	for (let i = 0; i < 4; i++) {
		buf[total - 8 + i] = (lenLo >>> (i * 8)) & 0xff
		buf[total - 4 + i] = (lenHi >>> (i * 8)) & 0xff
	}

	let a0 = 0x67452301
	let b0 = 0xefcdab89
	let c0 = 0x98badcfe
	let d0 = 0x10325476

	const M = new Array(16)
	for (let off = 0; off < total; off += 64) {
		for (let i = 0; i < 16; i++) {
			const p = off + i * 4
			M[i] = buf[p] | (buf[p + 1] << 8) | (buf[p + 2] << 16) | (buf[p + 3] << 24)
		}
		let A = a0
		let B = b0
		let C = c0
		let D = d0
		for (let i = 0; i < 64; i++) {
			let F
			let g
			if (i < 16) {
				F = (B & C) | (~B & D)
				g = i
			} else if (i < 32) {
				F = (D & B) | (~D & C)
				g = (5 * i + 1) % 16
			} else if (i < 48) {
				F = B ^ C ^ D
				g = (3 * i + 5) % 16
			} else {
				F = C ^ (B | ~D)
				g = (7 * i) % 16
			}
			F = (F + A + MD5_K[i] + M[g]) | 0
			A = D
			D = C
			C = B
			const s = MD5_S[i]
			B = (B + ((F << s) | (F >>> (32 - s)))) | 0
		}
		a0 = (a0 + A) | 0
		b0 = (b0 + B) | 0
		c0 = (c0 + C) | 0
		d0 = (d0 + D) | 0
	}
	return hexLE(a0) + hexLE(b0) + hexLE(c0) + hexLE(d0)
}

/* ------------------------------------------------------------------ *
 * 精二头像 URL
 * ------------------------------------------------------------------ */

/** 精二地址推导结果缓存（同一张头像会被多个页面、多次渲染反复取） */
const _E2_CACHE = {}

/**
 * 由精一头像地址推出精二头像地址。
 * 非 PRTS 域名 / 非法输入 → 返回 ''（调用方回退精一）。
 *
 * @param {string} elite1Url 例：https://media.prts.wiki/3/36/%E5%A4%B4%E5%83%8F_%E9%98%BF%E7%B1%B3%E5%A8%85.png
 * @returns {string}           例：https://media.prts.wiki/3/31/%E5%A4%B4%E5%83%8F_%E9%98%BF%E7%B1%B3%E5%A8%85_2.png
 */
export function elite2Url(elite1Url) {
	if (!elite1Url || typeof elite1Url !== 'string') return ''
	const cached = _E2_CACHE[elite1Url]
	if (cached !== undefined) return cached

	const out = deriveElite2(elite1Url)
	_E2_CACHE[elite1Url] = out
	return out
}

function deriveElite2(elite1Url) {
	const at = elite1Url.indexOf('media.prts.wiki/')
	if (at < 0) return ''

	// 取最后一段做文件名；去掉 ?20250715083533 这类老版本查询串
	const tail = elite1Url.slice(at + 'media.prts.wiki/'.length).split('?')[0]
	const parts = tail.split('/')
	if (!parts.length) return ''

	let name = parts[parts.length - 1]
	try {
		name = decodeURIComponent(name)
	} catch (e) {
		return ''
	}
	if (!name) return ''
	// 已经是精二图了，原样返回（幂等，避免重复加 _2）
	if (/_2\.[a-z0-9]+$/i.test(name)) return elite1Url.split('?')[0]

	// 在扩展名之前插入 _2
	const dot = name.lastIndexOf('.')
	if (dot <= 0) return ''
	const elite2Name = name.slice(0, dot) + '_2' + name.slice(dot)

	// MediaWiki：先把空格转下划线，再对整个文件名取 MD5，前 1 位 / 前 2 位作目录
	const hash = md5(elite2Name.replace(/ /g, '_'))
	return 'https://media.prts.wiki/' + hash.charAt(0) + '/' + hash.slice(0, 2) + '/' + encodeURIComponent(elite2Name)
}

/** 该盒型是否整盒没有精一/精二之分 */
export function isNoPairBoxType(boxType) {
	const t = String(boxType || '').trim().toLowerCase()
	return NO_PAIR_BOX_TYPES.indexOf(t) >= 0
}

/**
 * 判断某干员是否有精二头像可用。
 *
 * @param {object} character 形如 { name, avatar|imageUrl, nolyELITE1, market_price }
 * @param {string} [boxType] 所在盒型（whitelist/ambience/special 一律无精二）
 */
export function hasElite2(character, boxType) {
	if (!character) return false

	const name = String(character.name || '').trim()
	const type = boxType || character.Box_type || character.box_type || character.boxType || ''
	if (isNoPairBoxType(type)) return false

	if (character.nolyELITE1 === true || character.nolyELITE1 === 'true') return false
	if (name && NO_ELITE2_NAMES.indexOf(name) >= 0) return false

	const mp = character.market_price
	if (mp && typeof mp === 'object' && mp.ELITE2 === undefined) return false

	// 头像不在 PRTS 上（部分 NPC 用外链）时无法推导
	const base = character.avatar || character.imageUrl || ''
	if (!base || base.indexOf('media.prts.wiki/') < 0) return false

	return true
}

/**
 * 取某干员在指定精英等级下的头像。
 *
 * @param {object} character
 * @param {number|string} level 1 或 2
 * @param {string} [boxType]
 * @returns {string} 头像 URL；无精二时自动回退精一
 */
export function avatarOf(character, level, boxType) {
	if (!character) return ''
	const elite1 = character.avatar || character.imageUrl || ''
	if (Number(level) === 2 && hasElite2(character, boxType)) {
		return elite2Url(elite1) || elite1
	}
	return elite1
}

/** 精一 / 精二 双头像（供需要自行切换的界面使用） */
export function avatarPair(character, boxType) {
	const elite1 = (character && (character.avatar || character.imageUrl)) || ''
	const canPair = hasElite2(character, boxType)
	return {
		elite1,
		elite2: canPair ? elite2Url(elite1) || elite1 : '',
		canPair
	}
}

export default {
	NO_PAIR_BOX_TYPES,
	NO_ELITE2_NAMES,
	md5,
	elite2Url,
	isNoPairBoxType,
	hasElite2,
	avatarOf,
	avatarPair
}
