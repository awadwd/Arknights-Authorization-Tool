// AE（终末地）头像/图片域名兼容 + 压缩
// 经 wsrv.nl 代理时附带尺寸/格式参数，显著加快列表加载

const USE_PROXY = true;
const PROXY_PREFIX = 'https://wsrv.nl/?url=';

const PROXY_HOSTS = [
  'bbs.hycdn.cn',
  'hycdn.cn'
];

export function needsProxy(url) {
  if (!url || typeof url !== 'string') return false;
  return PROXY_HOSTS.some(host => url.includes(host));
}

/**
 * @param {string} url
 * @param {{w?:number,h?:number,fit?:string,q?:number}} [opts] 代理缩放参数
 */
export function normalizeImageUrl(url, opts) {
  if (!url || typeof url !== 'string') return '';
  if (!USE_PROXY) return url;
  if (!needsProxy(url)) return url;
  if (url.startsWith(PROXY_PREFIX)) return url;
  let target = PROXY_PREFIX + encodeURIComponent(url);
  if (opts) {
    if (opts.w) target += '&w=' + opts.w;
    if (opts.h) target += '&h=' + opts.h;
    if (opts.fit) target += '&fit=' + opts.fit;
    target += '&output=webp&q=' + (opts.q || 80);
  }
  return target;
}

// 干员头像：小图
export function normalizeAvatarUrl(url) {
  return normalizeImageUrl(url, { w: 160, h: 160, fit: 'cover', q: 75 });
}

// 盒官方图：中等宽度
export function normalizeBoxImageUrl(url) {
  return normalizeImageUrl(url, { w: 750, q: 80 });
}

// 批量规范化盒数据里的干员头像与盒图
export function normalizeBoxImageUrls(boxes) {
  if (!Array.isArray(boxes)) return boxes;
  return boxes.map(box => {
    if (!box || typeof box !== 'object') return box;
    let changed = false;
    const next = { ...box };
    for (let i = 1; i <= 12; i++) {
      const key = 'character' + i;
      const ch = next[key];
      if (ch && typeof ch === 'object' && ch.imageUrl) {
        const mapped = normalizeAvatarUrl(ch.imageUrl);
        if (mapped !== ch.imageUrl) {
          next[key] = { ...ch, imageUrl: mapped };
          changed = true;
        }
      }
    }
    if (next.Box_ImageUrl) {
      const mapped = normalizeBoxImageUrl(next.Box_ImageUrl);
      if (mapped !== next.Box_ImageUrl) {
        next.Box_ImageUrl = mapped;
        changed = true;
      }
    }
    return changed ? next : box;
  });
}
