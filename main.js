import App from './App'
import * as imageCache from './utils/imageCache.js'
// #ifndef MP-WEIXIN
// H5 / App 的 wx 是空壳，页面里裸调 wx.showShareMenu 会抛错并截断 onLoad（主题不生效）
import { installWxShim } from './utils/wxShim.js'
installWxShim()
// #endif

// ── 全局图片缓存读取方法 ──
// 盒图 / 干员头像这类稳定图片：命中本地缓存直接读本地文件，未命中先用远程地址
// 展示并在后台落地，落地完成后触发一次重渲染，后续即走本地（网络差时显著减少白屏）。
const _imgUpdateTimers = new WeakMap()
function _scheduleImgUpdate(vm) {
  try {
    if (!vm || _imgUpdateTimers.has(vm)) return
    const t = setTimeout(() => {
      _imgUpdateTimers.delete(vm)
      if (vm.$forceUpdate) vm.$forceUpdate()
    }, 120)
    _imgUpdateTimers.set(vm, t)
  } catch (e) {}
}
const imageCacheMixin = {
  methods: {
    imgSrc(url) {
      if (!url || typeof url !== 'string') return url || ''
      const local = imageCache.getLocal(url)
      if (local) return local
      if (url.indexOf('http') === 0) {
        const p = imageCache.prefetch(url)
        if (p && typeof p.then === 'function') {
          p.then((path) => { if (path) _scheduleImgUpdate(this) }).catch(() => {})
        }
      }
      return url
    }
  }
}


// #ifndef VUE3
import Vue from 'vue'
import './uni.promisify.adaptor'
Vue.config.productionTip = false
Vue.mixin(imageCacheMixin)
App.mpType = 'app'
const app = new Vue({
  ...App
})
app.$mount()
// #endif

// #ifdef VUE3
import { createSSRApp } from 'vue'
export function createApp() {
  const app = createSSRApp(App)
  app.mixin(imageCacheMixin)
  return {
    app
  }
}
// #endif