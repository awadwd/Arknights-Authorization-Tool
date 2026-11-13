// 图片持久缓存
// 解决的问题：盒号图片、干员头像这类「不常变化」的图，每次进页面都重新走网络，
//             网络差时大面积白屏。
// 做法：首次加载后把图片写进小程序用户目录，之后直接读本地文件，不再联网。
//
// 缓存目录：${USER_DATA_PATH}/akimg/
// 索引：storage['imgCacheIndex'] → { [url]: { p: 本地路径, s: 字节, t: 最近使用时间戳, h: 命中次数 } }
// 策略：容量上限 60MB / 1200 条，超限按「命中次数升序 + 时间升序」淘汰（LRU 变体）
//
// ⚠️ 需在微信公众平台「downloadFile 合法域名」中加入图片域名，否则下载会失败。
//    当前使用：https://wsrv.nl、https://bbs.hycdn.cn
//    未配置时会自动降级为直接使用远程 URL，功能不受影响，只是没有缓存加速。

const CACHE_DIR = 'akimg';
const INDEX_KEY = 'imgCacheIndex';
const MAX_BYTES = 60 * 1024 * 1024;
const MAX_ENTRIES = 1200;
// 单张图超过这个大小就不缓存（避免超大图挤占配额）
const MAX_FILE_BYTES = 3 * 1024 * 1024;
// 并发下载数
const CONCURRENCY = 3;

let _fsm = null;
let _dirReady = false;
let _index = null;
let _running = 0;
const _queue = [];

function getFsm() {
  if (_fsm) return _fsm;
  try {
    // #ifdef MP-WEIXIN
    _fsm = wx.getFileSystemManager();
    // #endif
  } catch (e) {
    _fsm = null;
  }
  return _fsm;
}

// 缓存根目录（仅微信小程序可用）
function getBaseDir() {
  try {
    // #ifdef MP-WEIXIN
    return wx.env.USER_DATA_PATH + '/' + CACHE_DIR;
    // #endif
  } catch (e) {}
  return '';
}

export function isSupported() {
  return !!(getFsm() && getBaseDir());
}

// FNV-1a → 16 进制，用于把 URL 变成短文件名
function hashUrl(str) {
  let h = 0x811c9dc5;
  for (let i = 0; i < str.length; i++) {
    h ^= str.charCodeAt(i);
    h = (h + ((h << 1) + (h << 4) + (h << 7) + (h << 8) + (h << 24))) >>> 0;
  }
  return ('00000000' + h.toString(16)).slice(-8);
}

function extOf(url) {
  const m = String(url).split('?')[0].match(/\.(png|jpe?g|webp|gif|bmp)$/i);
  return m ? '.' + m[1].toLowerCase() : '.img';
}

function readIndex() {
  if (_index) return _index;
  try {
    const raw = uni.getStorageSync(INDEX_KEY);
    _index = raw && typeof raw === 'object' ? raw : {};
  } catch (e) {
    _index = {};
  }
  return _index;
}

let _saveTimer = null;
function saveIndex() {
  // 合并写入，避免高频 setStorage
  if (_saveTimer) clearTimeout(_saveTimer);
  _saveTimer = setTimeout(() => {
    try {
      uni.setStorageSync(INDEX_KEY, readIndex());
    } catch (e) {}
  }, 400);
}

function ensureDir() {
  return new Promise((resolve) => {
    if (_dirReady) return resolve(true);
    const fsm = getFsm();
    const dir = getBaseDir();
    if (!fsm || !dir) return resolve(false);
    fsm.mkdir({
      dirPath: dir,
      recursive: true,
      success: () => {
        _dirReady = true;
        resolve(true);
      },
      fail: () => {
        // 目录已存在也会走 fail，这里当作可用
        _dirReady = true;
        resolve(true);
      }
    });
  });
}

function exists(path) {
  return new Promise((resolve) => {
    const fsm = getFsm();
    if (!fsm || !path) return resolve(false);
    fsm.access({
      path,
      success: () => resolve(true),
      fail: () => resolve(false)
    });
  });
}

/**
 * 同步取本地缓存路径（命中即返回，未命中返回空串）
 * 命中时会累加使用次数，供淘汰策略参考
 */
export function getLocal(url) {
  if (!url || typeof url !== 'string') return '';
  if (url.indexOf('http') !== 0 && url.indexOf('wxfile') !== 0 && url.indexOf('http') !== 0) {
    // 已经是本地路径
    if (url.indexOf('/') === 0 || url.indexOf('wxfile') === 0) return url;
  }
  const idx = readIndex();
  const hit = idx[url];
  if (!hit || !hit.p) return '';
  hit.h = (hit.h || 0) + 1;
  hit.t = Date.now();
  saveIndex();
  return hit.p;
}

/**
 * 解析：优先本地缓存，未命中返回远程 URL 并后台下载
 * @param {string} url 远程图片地址
 * @returns {string} 可直接用于 <image src> 的地址
 */
export function resolve(url) {
  if (!url) return '';
  const local = getLocal(url);
  if (local) return local;
  prefetch(url);
  return url;
}

/**
 * 批量解析（同步版）：返回 { [远程URL]: 可用地址 }
 */
export function resolveMap(urls) {
  const map = {};
  if (!Array.isArray(urls)) return map;
  urls.forEach((u) => {
    if (u && !map[u]) map[u] = resolve(u);
  });
  return map;
}

/**
 * 预取单张图到本地缓存
 * @returns {Promise<string>} 本地路径，失败返回空串
 */
export function prefetch(url) {
  if (!url || typeof url !== 'string' || url.indexOf('http') !== 0) return Promise.resolve('');
  if (!isSupported()) return Promise.resolve('');

  const idx = readIndex();
  if (idx[url] && idx[url].p) return Promise.resolve(idx[url].p);

  return new Promise((resolve) => {
    _queue.push({ url, resolve });
    _pump();
  });
}

function _pump() {
  while (_running < CONCURRENCY && _queue.length) {
    const job = _queue.shift();
    _running++;
    _download(job.url)
      .then((p) => job.resolve(p))
      .catch(() => job.resolve(''))
      .then(() => {
        _running--;
        _pump();
      });
  }
}

async function _download(url) {
  const ok = await ensureDir();
  if (!ok) return '';

  const localPath = `${getBaseDir()}/${hashUrl(url)}${extOf(url)}`;

  // 二次检查（并发下可能已被别的任务下载）
  if (await exists(localPath)) {
    const idx = readIndex();
    idx[url] = { p: localPath, s: (idx[url] && idx[url].s) || 0, t: Date.now(), h: (idx[url] && idx[url].h) || 0 };
    saveIndex();
    return localPath;
  }

  const tmp = await new Promise((resolve) => {
    uni.downloadFile({
      url,
      timeout: 20000,
      success: (res) => resolve(res.statusCode === 200 ? res.tempFilePath : ''),
      fail: () => resolve('')
    });
  });
  if (!tmp) return '';

  const fsm = getFsm();
  const size = await new Promise((resolve) => {
    fsm.stat({
      path: tmp,
      success: (s) => resolve((s.stats && s.stats.size) || 0),
      fail: () => resolve(0)
    });
  });
  // 过大不缓存
  if (size > MAX_FILE_BYTES) return '';

  try {
    await new Promise((resolve, reject) => {
      fsm.copyFile({ srcPath: tmp, destPath: localPath, success: resolve, fail: reject });
    });
  } catch (e) {
    return '';
  }

  const idx = readIndex();
  idx[url] = { p: localPath, s: size, t: Date.now(), h: 0 };
  saveIndex();
  sweep();
  return localPath;
}

/**
 * 批量预取（按需调用，建议只在关键页面首屏使用）
 */
export function prefetchAll(urls, onDone) {
  if (!Array.isArray(urls) || !urls.length) return Promise.resolve(0);
  const list = urls.filter((u) => u && typeof u === 'string' && u.indexOf('http') === 0);
  let done = 0;
  return Promise.all(
    list.map((u) =>
      prefetch(u).then((p) => {
        done++;
        if (p && typeof onDone === 'function') onDone(u, p, done, list.length);
        return p;
      })
    )
  ).then(() => done);
}

/**
 * 容量淘汰：超过上限时，优先删「命中次数少 + 最久未使用」的
 */
export function sweep() {
  if (!isSupported()) return 0;
  const idx = readIndex();
  const keys = Object.keys(idx);
  let total = 0;
  keys.forEach((k) => (total += idx[k].s || 0));

  if (total <= MAX_BYTES && keys.length <= MAX_ENTRIES) return 0;

  const sorted = keys.sort((a, b) => {
    const A = idx[a];
    const B = idx[b];
    if ((A.h || 0) !== (B.h || 0)) return (A.h || 0) - (B.h || 0);
    return (A.t || 0) - (B.t || 0);
  });

  const fsm = getFsm();
  let removed = 0;
  for (const k of sorted) {
    if (total <= MAX_BYTES * 0.8 && keys.length - removed <= MAX_ENTRIES * 0.8) break;
    const item = idx[k];
    try {
      fsm.unlink({ filePath: item.p, fail: () => {} });
    } catch (e) {}
    total -= item.s || 0;
    delete idx[k];
    removed++;
  }
  saveIndex();
  return removed;
}

/**
 * 缓存统计
 */
export function stats() {
  const idx = readIndex();
  const keys = Object.keys(idx);
  let bytes = 0;
  keys.forEach((k) => (bytes += idx[k].s || 0));
  return {
    count: keys.length,
    bytes,
    mb: (bytes / 1024 / 1024).toFixed(2),
    limitMB: (MAX_BYTES / 1024 / 1024).toFixed(0),
    supported: isSupported()
  };
}

/**
 * 清空缓存目录
 */
export function clear() {
  return new Promise((resolve) => {
    const fsm = getFsm();
    const dir = getBaseDir();
    const idx = readIndex();
    const paths = Object.keys(idx).map((k) => idx[k].p).filter(Boolean);
    let i = 0;
    const next = () => {
      if (i >= paths.length) {
        _index = {};
        try {
          uni.setStorageSync(INDEX_KEY, {});
        } catch (e) {}
        try {
          fsm.rmdir({ dirPath: dir, recursive: true, fail: () => {} });
        } catch (e) {}
        _dirReady = false;
        resolve(true);
        return;
      }
      const p = paths[i++];
      try {
        fsm.unlink({ filePath: p, success: next, fail: next });
      } catch (e) {
        next();
      }
    };
    next();
  });
}
