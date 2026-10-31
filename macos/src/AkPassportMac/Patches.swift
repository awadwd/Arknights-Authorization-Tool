import Foundation

/// 注入到 H5 的桌面端补丁。
///
/// 全部逐条移植自 Windows 版 `Services/DesktopPatches.cs`，只做两处 macOS 适配：
/// ① 加一层 `chrome.webview.postMessage` 兼容垫片，前端已有的桥调用零改动；
/// ② 跨域改写指向本地 `/proxy`，代替 Windows 的 WebResourceRequested + RemoteProxy。
enum Patches {
    /// 桌面端根字号。16px = 移动端 375 设计稿原尺寸，18px 略放大更适合桌面阅读。
    /// uni-app 的 rpx 按窗口宽度换算 rem，不锁死会把整页等比放大 2 倍多。
    static let rootFontPx = 18

    /// 顶层路由：由侧边栏导航，不需要 uni 自带的 44px 标题栏。
    private static let topRoutes = [
        "pages/desktop_home/desktop_home", "pages/Search/Search", "pages/list/list",
        "packageB/pages/gacha_simulated/gacha_simulated", "packageB/pages/mystarbox/mystarbox",
        "pages/card_share/card_share", "packageA/calendar/calendar",
        "pages/daily_recommend/daily_recommend", "pages/box_feedback_new/box_feedback_new",
        // Vue 设置页。桌面端的「设置」已由原生页接管，但 H5 内部仍有入口指向它
        // （比如列表页的「去设置」），外壳会把这个路由拦截成原生页；留在这里是为了
        // 万一有路径漏进去，uni 也别再叠一条标题栏。
        "packageA/Setting/Setting",
        "package_ArknightEnds/index/index", "package_ArknightEnds/Search/Search",
        "package_ArknightEnds/list/list", "package_ArknightEnds/calendar/calendar",
        "package_ArknightEnds/gacha_simulated/gacha_simulated",
        "package_ArknightEnds/custom-words/custom-words",
        "package_ArknightEnds/more-notice/more-notice",
        "package_ArknightEnds/Setting/Setting",
    ]

    /// 返回按注入顺序排列的脚本（全部 atDocumentStart）。
    static func all(port: UInt16) -> [String] {
        let proxyBase = "http://127.0.0.1:\(port)/proxy?u="
        return [
            bridgeShim(port: port),
            scaleScript(),
            zoomGuard(),
            themeDefault(),
            chromeScript(),
            themeBridge(),
            remoteRewrite(proxyBase: proxyBase),
            imageProxy(proxyBase: proxyBase),
            desktopCopyFix(),
            routeReport(),
            consoleRelay(),
        ]
    }

    // MARK: - 桥

    /// 让 Windows 壳那套 `window.chrome.webview.postMessage(...)` 在 WKWebView 里照样能用。
    private static func bridgeShim(port: UInt16) -> String {
        #"""
        (function(){
          if (window.__akShellReady) return;
          window.__akShellReady = true;
          function post(obj){ try { webkit.messageHandlers.akShell.postMessage(obj); } catch(e) {} }
          window.__AK_SHELL__ = { platform:'macos', origin:'http://127.0.0.1:\#(port)', post: post };
          if (!window.chrome) window.chrome = {};
          if (!window.chrome.webview) {
            window.chrome.webview = {
              postMessage: function(m){
                try {
                  if (typeof m === 'string') post(JSON.parse(m));
                  else post(m);
                } catch(e) { post({ type:'raw', value: String(m) }); }
              }
            };
          }
        })();
        """#
    }

    // MARK: - 字号

    private static func scaleScript() -> String {
        #"""
        (function(){
          if (window.__akScale) return;
          window.__akScale = true;
          var CSS = 'html{font-size:\#(rootFontPx)px!important}';
          function apply(){
            try {
              if (document.getElementById('ak-desktop-scale')) return;
              var s = document.createElement('style');
              s.id = 'ak-desktop-scale';
              s.textContent = CSS;
              var host = document.head || document.documentElement;
              if (host) host.appendChild(s);
            } catch(e) {}
          }
          document.addEventListener('DOMContentLoaded', apply);
          window.addEventListener('load', apply);
          apply();
        })();
        """#
    }

    // MARK: - 禁缩放

    private static func zoomGuard() -> String {
        #"""
        (function(){
          if (window.__akZoomGuard) return;
          window.__akZoomGuard = true;
          window.addEventListener('wheel', function(e){
            if (e.ctrlKey || e.metaKey) e.preventDefault();
          }, { passive:false, capture:true });
          ['gesturestart','gesturechange','gestureend'].forEach(function(t){
            window.addEventListener(t, function(e){ e.preventDefault(); }, { passive:false, capture:true });
          });
          window.addEventListener('keydown', function(e){
            if (!(e.ctrlKey || e.metaKey)) return;
            var k = e.key;
            if (k === '+' || k === '-' || k === '=' || k === '_' || k === '0') {
              e.preventDefault();
              e.stopPropagation();
            }
          }, true);
        })();
        """#
    }

    // MARK: - 主题兜底

    private static func themeDefault() -> String {
        #"""
        (function(){
          try {
            var k = 'themeMode', v = localStorage.getItem(k);
            if (!v || v === 'simple') localStorage.setItem(k, 'liquid');
          } catch(e) {}
        })();
        """#
    }

    // MARK: - 隐藏 uni 自带标题栏

    private static func chromeScript() -> String {
        let routes = topRoutes.map { "'\($0)'" }.joined(separator: ",")
        return #"""
        (function(){
          var TOPS = [\#(routes)];
          var CSS = 'html.ak-flat-head uni-page-head{display:none!important}'
                  + 'html.ak-flat-head uni-page-body{top:0!important}'
                  // 两个工具包首页都自带一个「切换到另一个包」的胶囊按钮，
                  // 和侧边栏顶部的分段控件重复，去掉它；标题仍居中（右半边还在）。
                  + '.nav-switch-btn{display:none!important}';
          function applyCss(){
            try {
              if (document.getElementById('ak-flat-head-css')) return;
              var s = document.createElement('style');
              s.id = 'ak-flat-head-css';
              s.textContent = CSS;
              var host = document.head || document.documentElement;
              if (host) host.appendChild(s);
            } catch(e) {}
          }
          function check(){
            try {
              var h = (location.hash || '').split('#/')[1] || '';
              h = h.split('?')[0];
              document.documentElement.classList.toggle('ak-flat-head', TOPS.indexOf(h) > -1);
            } catch(e) {}
          }
          document.addEventListener('DOMContentLoaded', function(){ applyCss(); check(); });
          window.addEventListener('hashchange', check);
          setInterval(check, 700);
          applyCss();
          check();
        })();
        """#
    }

    // MARK: - 主题桥

    /// 轮询 localStorage.themeMode，变化时报给外壳让窗口留白跟着换色。
    private static func themeBridge() -> String {
        #"""
        (function(){
          if (window.__akThemeBridge) return;
          window.__akThemeBridge = true;
          var last = null;
          function push(){
            try {
              var m = localStorage.getItem('themeMode') || 'simple';
              if (m !== last) {
                last = m;
                window.chrome.webview.postMessage(JSON.stringify({ type:'theme', value:m }));
              }
            } catch(e) {}
          }
          push();
          setInterval(push, 1000);
        })();
        """#
    }

    // MARK: - 跨域改写

    /// H5 构建里把数据源写死成 https://raw.gitcode.com 等绝对地址，
    /// 从本地 origin 直接请求属于跨域，对方不给 CORS 头就整体失败。
    /// 这里在 XHR / fetch 层改写到同源的 /proxy，由本地服务代取。
    private static func remoteRewrite(proxyBase: String) -> String {
        #"""
        (function(){
          if (window.__akRemoteRewrite) return;
          window.__akRemoteRewrite = true;
          var BASE = '\#(proxyBase)';
          var HOSTS = ['raw.gitcode.com','cdn.jsdelivr.net','fastly.jsdelivr.net','raw.githubusercontent.com'];
          function fix(u){
            try {
              if (typeof u !== 'string') return u;
              var i = u.indexOf('/gitcode/');
              if (i === 0) u = 'https://raw.gitcode.com/' + u.slice(9);
              // 知晓云（*.myminapp.com）不发 CORS 头，浏览器里直连必失败。
              // 走同源 /proxy，Client-ID 由本地服务从子域名推出来补上。
              if (/^https:\/\/[a-z0-9]+\.myminapp\.com\//.test(u)) return BASE + encodeURIComponent(u);
              for (var n = 0; n < HOSTS.length; n++) {
                var prefix = 'https://' + HOSTS[n] + '/';
                if (u.indexOf(prefix) === 0) return BASE + encodeURIComponent(u);
              }
              return u;
            } catch(e) { return u; }
          }
          try {
            var open = XMLHttpRequest.prototype.open;
            XMLHttpRequest.prototype.open = function(m, u){
              var a = [].slice.call(arguments);
              if (a.length > 1) a[1] = fix(a[1]);
              return open.apply(this, a);
            };
          } catch(e) {}
          try {
            var original = window.fetch;
            if (original) {
              window.fetch = function(input, init){
                try {
                  if (typeof input === 'string') {
                    input = fix(input);
                  } else if (input && input.url) {
                    var nu = fix(input.url);
                    if (nu !== input.url) input = new Request(nu, input);
                  }
                } catch(e) {}
                return original.call(this, input, init);
              };
            }
          } catch(e) {}
        })();
        """#
    }

    // MARK: - 图床代理

    /// B 站 / 微博图床有防盗链：页面来源是 127.0.0.1 时，请求一旦带上这样的 Referer 就回 403。
    /// 把图改写到本地 /proxy（带 Referer 由 URLSession 发），再把结果塞回页面。
    ///
    /// 三条路都要堵住，少一条就会漏图：
    /// ① `new Image().src = ...` —— uni-app 的 `<image>` 先预加载，加载失败就把
    ///    `background-image` 设成 `none`（首页横幅就是这么变空的，DOM 里连 `<img>` 都没有）；
    /// ② Vue 走 `setAttribute('src')` 插 `<img>`；
    /// ③ `style="background-image:url(...)"` 直接写在行内样式上。
    private static func imageProxy(proxyBase: String) -> String {
        #"""
        (function(){
          if (window.__akImageProxy) return;
          window.__akImageProxy = true;
          var BASE = '\#(proxyBase)';
          var HOSTS = '(hdslb\\.com|biliimg\\.com|sinaimg\\.cn)';
          var RE = new RegExp('^https?://([a-z0-9-]+\\.)*' + HOSTS + '/', 'i');
          var RE_BG = new RegExp('url\\(([\'"]?)(https?://[^\'")]*' + HOSTS + '/[^\'")]*)\\1\\)', 'gi');
          function proxify(src){
            if (!src || typeof src !== 'string') return src;
            if (src.indexOf(BASE) === 0) return src;
            if (!RE.test(src)) return src;
            return BASE + encodeURIComponent(src);
          }

          // ① 拦截 src 赋值。uni 的 Image 预加载器只会给 i.src 赋一次值，
          //    在这里改写，onload 拿到的就是代理地址，组件才会把图真正画出来。
          try {
            var proto = HTMLImageElement.prototype;
            var desc = Object.getOwnPropertyDescriptor(proto, 'src');
            if (desc && desc.set && desc.get) {
              Object.defineProperty(proto, 'src', {
                configurable: true,
                enumerable: desc.enumerable,
                get: function(){ return desc.get.call(this); },
                set: function(value){ desc.set.call(this, proxify(value)); }
              });
            }
          } catch(e) {}

          // ② Vue 用 setAttribute 写属性，绕过了上面的 setter
          var nativeSet = Element.prototype.setAttribute;
          try {
            Element.prototype.setAttribute = function(name, value){
              if (name === 'src' && this instanceof HTMLImageElement) value = proxify(value);
              return nativeSet.call(this, name, value);
            };
          } catch(e) {}

          // ③ 行内 background-image
          function fixStyle(el){
            if (!el || !el.getAttribute) return;
            var style = el.getAttribute('style');
            if (!style || style.indexOf('url(') < 0) return;
            var changed = false;
            var next = style.replace(RE_BG, function(m, quote, url){
              if (url.indexOf(BASE) === 0) return m;
              changed = true;
              var q = quote || '"';
              return 'url(' + q + BASE + encodeURIComponent(url) + q + ')';
            });
            if (changed && next !== style) nativeSet.call(el, 'style', next);
          }
          function fixImg(img){
            var current = img.getAttribute('src');
            var next = proxify(current);
            if (next && next !== current) nativeSet.call(img, 'src', next);
          }
          function sweep(root){
            if (!root || !root.querySelectorAll) return;
            var imgs = root.querySelectorAll('img[src]');
            for (var i = 0; i < imgs.length; i++) fixImg(imgs[i]);
            var styled = root.querySelectorAll('[style]');
            for (var j = 0; j < styled.length; j++) fixStyle(styled[j]);
          }
          function boot(){
            sweep(document);
            var mo = new MutationObserver(function(list){
              for (var i = 0; i < list.length; i++) {
                var record = list[i];
                if (record.type === 'attributes') {
                  var target = record.target;
                  if (!target || target.nodeType !== 1) continue;
                  if (target.tagName === 'IMG') fixImg(target);
                  else fixStyle(target);
                  continue;
                }
                var nodes = record.addedNodes;
                for (var j = 0; j < nodes.length; j++) {
                  var node = nodes[j];
                  if (node.nodeType !== 1) continue;
                  if (node.tagName === 'IMG') fixImg(node);
                  sweep(node);
                }
              }
            });
            var rootEl = document.documentElement || document;
            mo.observe(rootEl, { childList:true, subtree:true, attributes:true, attributeFilter:['src','style'] });
          }
          if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', boot);
          } else {
            boot();
          }
        })();
        """#
    }

    // MARK: - 桌面标识文案

    /// desktop_home 页头 / 页脚是前端硬编码的 Windows 文案（编译产物里的固定字符串），
    /// 这里按稳定的 class 改写成 macOS 版，避免出现「Windows 桌面版」这种明显错位的字。
    private static func desktopCopyFix() -> String {
        #"""
        (function(){
          if (window.__akCopyFix) return;
          window.__akCopyFix = true;
          var rounds = 0;
          function fix(){
            try {
              var sub = document.querySelector('.wh-sub');
              if (sub && sub.textContent.indexOf('Windows') > -1) sub.textContent = 'macOS 版';
              var foot = document.querySelector('.wh-foot-text');
              if (foot && foot.textContent.indexOf('Ctrl') > -1) {
                foot.textContent = '⌘R 重新加载 · ⌘[ 返回 · 点菜单栏托盘图标唤回窗口';
              }
            } catch(e) {}
            if (++rounds > 24) clearInterval(timer);
          }
          var timer = setInterval(fix, 500);
          document.addEventListener('DOMContentLoaded', fix);
        })();
        """#
    }

    // MARK: - 路由上报

    /// 把 H5 当前的 hash 路由回报给壳，供侧边栏反向对齐选中项。
    ///
    /// WKWebView 只在「整页加载」时通知导航，hash 变化（uni-app 全部内部跳转都走它）
    /// 一条回调都没有。Windows 那边靠 WebView2 的 SourceChanged 天然覆盖，
    /// macOS 只能自己挂 hashchange + history 钩子。
    private static func routeReport() -> String {
        #"""
        (function(){
          if (window.__akRouteReport) return;
          window.__akRouteReport = true;
          var last = null;
          function emit(){
            var raw = location.hash || '';
            if (raw === last) return;
            last = raw;
            // '#/pages/list/list?id=3' -> 'pages/list/list'
            var path = raw.replace(/^#/, '').replace(/^\//, '').split('?')[0];
            try { webkit.messageHandlers.akShell.postMessage({ type:'route', value: path }); } catch(e) {}
          }
          window.addEventListener('hashchange', emit);
          ['pushState','replaceState'].forEach(function(name){
            var original = history[name];
            if (typeof original !== 'function') return;
            history[name] = function(){
              var result = original.apply(this, arguments);
              // uni-app 有时先 replaceState 再改 hash，两个事件都到，靠 last 去重
              setTimeout(emit, 0);
              return result;
            };
          });
          // 首屏：DOM 就绪后再报一次，此时 uni 已完成首次路由
          setTimeout(emit, 0);
          setTimeout(emit, 260);
        })();
        """#
    }

    // MARK: - 控制台转发

    /// WKWebView 没有 DevTools 窗口，页面报错只能靠转发到 shell.log 才看得见。
    private static func consoleRelay() -> String {
        #"""
        (function(){
          if (window.__akConsoleRelay) return;
          window.__akConsoleRelay = true;
          var count = 0;
          function send(level, text){
            if (++count > 60) return;
            try {
              webkit.messageHandlers.akShell.postMessage({
                type: 'log',
                level: level,
                value: String(text).slice(0, 600)
              });
            } catch(e) {}
          }
          window.addEventListener('error', function(e){
            send('error', (e.message || '') + ' @ ' + (e.filename || '') + ':' + (e.lineno || 0));
          }, true);
          window.addEventListener('unhandledrejection', function(e){
            var r = e.reason;
            send('reject', (r && (r.stack || r.message)) || String(r));
          });
          var errorFn = console.error, warnFn = console.warn;
          console.error = function(){
            send('console.error', Array.prototype.join.call(arguments, ' '));
            return errorFn.apply(console, arguments);
          };
          console.warn = function(){
            send('console.warn', Array.prototype.join.call(arguments, ' '));
            return warnFn.apply(console, arguments);
          };
        })();
        """#
    }
}
