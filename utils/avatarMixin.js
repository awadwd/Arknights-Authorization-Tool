/**
 * 头像偏好 mixin
 * ------------------------------------------------------------------
 * 给「需要按个性化设置展示干员头像」的页面复用两个方法：
 *   pAvatar(character)        取最终要渲染的头像地址（精一 / 精二）
 *   refreshAvatarPref()       从设置页返回时让缓存失效并重渲染
 *
 * 页面里只需把 :src="imgSrc(character.avatar)" 换成 :src="imgSrc(pAvatar(character))"。
 * character 上的 `_avatarCtx`（建对象时写入的盒上下文）会参与条件规则匹配；
 * 没有 ctx 时，仅「干员属性」类规则生效，盒属性类规则不命中。
 */

import { avatarFor, invalidate } from './avatarPref.js'

export default {
	methods: {
		/**
		 * @param {object} character 至少含 { name, avatar|imageUrl }
		 * @returns {string} 头像地址；无法取精二时回落到精一
		 */
		pAvatar(character) {
			if (!character) return ''
			const base = character.avatar || character.imageUrl || ''
			if (!base) return base
			return avatarFor(character.name, base, character._avatarCtx || null)
		},

		/** 设置变更后调用：清缓存 + 强制重渲染 */
		refreshAvatarPref() {
			invalidate()
			if (this && typeof this.$forceUpdate === 'function') this.$forceUpdate()
		}
	}
}
