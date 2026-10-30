import AppKit
import WebKit

/// 把消息处理器弱引用包一层，避免 WKUserContentController 强持有造成循环引用。
private final class WeakScriptHandler: NSObject, WKScriptMessageHandler {
    weak var target: WKScriptMessageHandler?

    init(target: WKScriptMessageHandler) {
        self.target = target
    }

    func userContentController(_ controller: WKUserContentController, didReceive message: WKScriptMessage) {
        target?.userContentController(controller, didReceive: message)
    }
}

/// WKWebView 外壳：承载 H5 产物、注入补丁、处理 JS 桥与外部链接。
/// 对应 Windows 版 MainWindow.xaml.cs 里与 WebView2 相关的那部分。
final class WebShell: NSObject {
    let webView: WKWebView

    /// 前端主题变化（simple / ak / ark / jieyuan / liquid / liquidDark）
    var onThemeChange: ((String) -> Void)?
    /// H5 请求打开外部链接（<a target="_blank">、window.open）
    var onExternalLink: ((URL) -> Void)?
    /// H5 内部路由变化（裸路径，形如 pages/list/list）
    var onRouteChange: ((String) -> Void)?
    /// 页面加载完成。原生设置页要靠它触发第一次读 localStorage ——
    /// 首帧之前 `evaluateJavaScript` 读到的全是空值。
    var onLoad: (() -> Void)?

    private let port: UInt16
    private var snapshotTaken = false

    init(port: UInt16, devMode: Bool) {
        self.port = port

        let configuration = WKWebViewConfiguration()
        configuration.defaultWebpagePreferences.allowsContentJavaScript = true
        configuration.suppressesIncrementalRendering = false

        let controller = WKUserContentController()
        for source in Patches.all(port: port) {
            controller.addUserScript(WKUserScript(source: source,
                                                 injectionTime: .atDocumentStart,
                                                 forMainFrameOnly: true))
        }
        configuration.userContentController = controller

        webView = WKWebView(frame: .zero, configuration: configuration)

        super.init()

        controller.add(WeakScriptHandler(target: self), name: "akShell")
        webView.navigationDelegate = self
        webView.uiDelegate = self
        webView.allowsBackForwardNavigationGestures = true
        webView.underPageBackgroundColor = ShellTheme.named(nil).canvas
        if devMode, #available(macOS 13.3, *) {
            webView.isInspectable = true
        }
    }

    func load(route: String) {
        guard let url = URL(string: "http://127.0.0.1:\(port)/#/\(route)") else { return }
        Diag.log("load \(url.absoluteString)")
        webView.load(URLRequest(url: url))
    }

    func reload() {
        webView.reload()
    }

    /// 侧边栏点击：把 H5 带到指定路由。
    ///
    /// 优先 `uni.reLaunch`（和 Windows 版同一策略，能正确重置页面栈），
    /// uni 尚未就绪时退回直接改 hash —— uni-app 的 hash 路由同样会响应。
    /// 两者都失败时返回 false，由调用方决定是否补一次整页加载。
    func navigate(to route: String, completion: ((Bool) -> Void)? = nil) {
        let escaped = route.replacingOccurrences(of: "\\", with: "\\\\")
            .replacingOccurrences(of: "'", with: "\\'")
        let script = """
        (function(){
          try { if (typeof uni !== 'undefined' && uni.reLaunch) { uni.reLaunch({ url: '/\(escaped)' }); return 'reLaunch'; } } catch (e) {}
          try { location.hash = '#/\(escaped)'; return 'hash'; } catch (e2) { return 'fail:' + e2.message; }
        })()
        """
        webView.evaluateJavaScript(script) { result, error in
            if let error {
                Diag.log("navigate \(route) failed: \(error.localizedDescription)")
                completion?(false)
                return
            }
            let outcome = (result as? String) ?? "nil"
            Diag.log("navigate \(route) -> \(outcome)")
            completion?(outcome != "nil" && !outcome.hasPrefix("fail"))
        }
    }

    func goBack() {
        if webView.canGoBack { webView.goBack() }
    }

    func goForward() {
        if webView.canGoForward { webView.goForward() }
    }

    private func isLocal(_ url: URL) -> Bool {
        guard let host = url.host else { return false }
        if host == "127.0.0.1" || host == "localhost" {
            return url.port == nil || url.port == Int(port)
        }
        return false
    }
}

// MARK: - WKScriptMessageHandler

extension WebShell: WKScriptMessageHandler {
    func userContentController(_ controller: WKUserContentController, didReceive message: WKScriptMessage) {
        guard let body = message.body as? [String: Any],
              let type = body["type"] as? String else {
            Diag.log("bridge: unexpected payload \(message.body)")
            return
        }

        switch type {
        case "theme":
            let name = body["value"] as? String ?? ""
            Diag.log("bridge theme: \(name)")
            onThemeChange?(name)
        case "route":
            let path = body["value"] as? String ?? ""
            onRouteChange?(path)
        case "log":
            let level = body["level"] as? String ?? "log"
            let value = body["value"] as? String ?? ""
            Diag.log("js[\(level)] \(value)")
        default:
            Diag.log("bridge: \(body)")
        }
    }
}

// MARK: - WKNavigationDelegate

extension WebShell: WKNavigationDelegate {
    func webView(_ webView: WKWebView,
                 decidePolicyFor navigationAction: WKNavigationAction,
                 decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
        guard let url = navigationAction.request.url else {
            decisionHandler(.cancel)
            return
        }

        if isLocal(url) || url.scheme == "about" || url.scheme == "data" {
            decisionHandler(.allow)
            return
        }

        // 其它一切（外链、mailto、支付跳转）交给系统浏览器，壳内不做导航
        Diag.log("open external: \(url.absoluteString)")
        onExternalLink?(url)
        decisionHandler(.cancel)
    }

    func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
        let hash = webView.url?.fragment ?? ""
        Diag.log("didFinish \(hash)")
        onLoad?()
        captureSnapshotIfRequested()
        probeImagesIfRequested()
    }

    /// 诊断工具：`AKPASSPORT_IMG_PROBE=6` 表示「加载后 6 秒」把页面里所有图片的
    /// 实际情况打进日志 —— 既扫 `<img>`（看 naturalWidth），也扫 `background-image`
    /// （uni-app 的 `<image>` 是靠背景图画的，不在 `document.images` 里）。
    /// `naturalWidth == 0` / 背景为空，就能区分「没请求」「被防盗链拒了」「CSS 藏了」，
    /// 比对着截图猜快得多。首页横幅那次就是靠它定位到 `background-image: none` 的。
    private func probeImagesIfRequested() {
        guard let value = ProcessInfo.processInfo.environment["AKPASSPORT_IMG_PROBE"] else { return }
        let delay = Double(value) ?? 6
        let script = #"""
        (function(){
          var out = [];
          var imgs = document.images;
          for (var i = 0; i < imgs.length; i++) {
            var img = imgs[i];
            var src = img.currentSrc || img.getAttribute('src') || '';
            var rect = img.getBoundingClientRect();
            out.push({
              kind: 'img',
              src: String(src).slice(0, 200),
              w: img.naturalWidth, h: img.naturalHeight,
              done: !!img.complete,
              cw: Math.round(rect.width), ch: Math.round(rect.height)
            });
          }
          // uni-app 的 <image mode="aspectFill"> 在 H5 下是用 background-image 画的，
          // 这类图不在 document.images 里，得单独扫。
          var all = document.querySelectorAll('*');
          for (var j = 0; j < all.length; j++) {
            var el = all[j];
            var bg = '';
            try { bg = getComputedStyle(el).backgroundImage || ''; } catch(e) {}
            if (bg.indexOf('url(') === 0) {
              var rect2 = el.getBoundingClientRect();
              out.push({
                kind: 'bg',
                tag: el.tagName.toLowerCase(),
                src: bg.slice(0, 200),
                w: -1, h: -1, done: true,
                cw: Math.round(rect2.width), ch: Math.round(rect2.height)
              });
            }
          }
          // 请求了但没拿到任何字节的资源（WebKit 对跨域请求的 transferSize 常为 0，
          // 只作为线索，不当结论用）
          try {
            var zeros = 0;
            var entries = performance.getEntriesByType('resource');
            for (var k = 0; k < entries.length && zeros < 8; k++) {
              var en = entries[k];
              if (en.transferSize !== 0 || en.decodedBodySize > 0) continue;
              if (en.name.indexOf('127.0.0.1') === 0) continue;
              zeros++;
              out.push({
                kind: 'res', tag: '',
                src: en.name.slice(0, 180),
                w: 1, h: 1, done: true, cw: 0, ch: 0
              });
            }
          } catch(e) {}
          return JSON.stringify(out);
        })();
        """#
        DispatchQueue.main.asyncAfter(deadline: .now() + delay) { [weak self] in
            guard let self else { return }
            self.webView.evaluateJavaScript(script) { result, error in
                if let error {
                    Diag.log("imgprobe error \(error.localizedDescription)")
                    return
                }
                guard let text = result as? String,
                      let data = text.data(using: .utf8),
                      let list = try? JSONSerialization.jsonObject(with: data) as? [[String: Any]] else {
                    Diag.log("imgprobe: 无结果")
                    return
                }
                let broken = list.filter { ($0["w"] as? Int ?? 0) == 0 }.count
                Diag.log("imgprobe total=\(list.count) broken=\(broken)")
                for item in list {
                    let kind = item["kind"] as? String ?? "img"
                    let tag = item["tag"] as? String ?? ""
                    let w = item["w"] as? Int ?? 0
                    let h = item["h"] as? Int ?? 0
                    let cw = item["cw"] as? Int ?? 0
                    let ch = item["ch"] as? Int ?? 0
                    let done = item["done"] as? Bool ?? false
                    let src = item["src"] as? String ?? ""
                    let flag = w >= 0 && w == 0 ? "FAIL" : "ok  "
                    Diag.log("img \(flag) [\(kind)/\(tag)] \(w)x\(h) box=\(cw)x\(ch) complete=\(done) \(src)")
                }
            }
        }
    }

    /// 回归工具：设 AKPASSPORT_SNAPSHOT=/path/shot.png 时，首屏加载完自动存一张 PNG。
    /// 走 WKWebView.takeSnapshot，不需要「屏幕录制」权限，适合 CI 与无头核对。
    /// 默认只截 WebView；设 AKPASSPORT_SNAPSHOT_MODE=window 则截整个窗口
    /// （含原生侧边栏，用于核对分栏布局）。
    private func captureSnapshotIfRequested() {
        guard !snapshotTaken,
              let path = ProcessInfo.processInfo.environment["AKPASSPORT_SNAPSHOT"] else { return }
        snapshotTaken = true
        let delay = Double(ProcessInfo.processInfo.environment["AKPASSPORT_SNAPSHOT_DELAY"] ?? "") ?? 3.0
        let wholeWindow = ProcessInfo.processInfo.environment["AKPASSPORT_SNAPSHOT_MODE"] == "window"
        DispatchQueue.main.asyncAfter(deadline: .now() + delay) { [weak self] in
            guard let self else { return }
            if wholeWindow, self.writeWindowSnapshot(to: path) { return }
            self.webView.takeSnapshot(with: WKSnapshotConfiguration()) { image, error in
                guard let image,
                      let tiff = image.tiffRepresentation,
                      let rep = NSBitmapImageRep(data: tiff),
                      let png = rep.representation(using: .png, properties: [:]) else {
                    Diag.log("snapshot failed: \(error?.localizedDescription ?? "no image")")
                    return
                }
                let url = URL(fileURLWithPath: path)
                try? png.write(to: url)
                Diag.log("snapshot -> \(path) (\(png.count) bytes)")
                if ProcessInfo.processInfo.environment["AKPASSPORT_EXIT_AFTER_SNAPSHOT"] == "1" {
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) { NSApp.terminate(nil) }
                }
            }
        }
    }

    /// 整窗截图：把 WebView 快照手动贴到窗口骨架画布上。
    ///
    /// 为什么不能直接 `cacheDisplay(in:to:)`：WKWebView 的像素由独立进程渲染，
    /// AppKit 这条路径拿不到网页内容，只会得到一块空白。所以这里先画出原生部分
    /// （侧边栏、卡片底、边框），再把 `takeSnapshot` 的结果按真实坐标合成上去。
    private func writeWindowSnapshot(to path: String) -> Bool {
        guard let window = webView.window, let root = window.contentView else { return false }
        let bounds = root.bounds
        guard let canvas = NSBitmapImageRep(
            bitmapDataPlanes: nil,
            pixelsWide: Int(bounds.width * (window.backingScaleFactor)),
            pixelsHigh: Int(bounds.height * (window.backingScaleFactor)),
            bitsPerSample: 8,
            samplesPerPixel: 4,
            hasAlpha: true,
            isPlanar: false,
            colorSpaceName: .deviceRGB,
            bytesPerRow: 0,
            bitsPerPixel: 0
        ) else { return false }
        canvas.size = bounds.size

        guard let context = NSGraphicsContext(bitmapImageRep: canvas) else { return false }
        NSGraphicsContext.saveGraphicsState()
        NSGraphicsContext.current = context
        root.cacheDisplay(in: bounds, to: canvas)

        func finish() {
            NSGraphicsContext.restoreGraphicsState()
            try? canvas.representation(using: .png, properties: [:])?
                .write(to: URL(fileURLWithPath: path))
            Diag.log("window snapshot -> \(path)")
            if ProcessInfo.processInfo.environment["AKPASSPORT_EXIT_AFTER_SNAPSHOT"] == "1" {
                DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) { NSApp.terminate(nil) }
            }
        }

        // 原生设置页盖在 WebView 上时，WebView 是 hidden 的；这时候再贴它的快照，
        // 会把已经被遮住的网页内容又画回来（第一次截图时就是这样骗过了眼睛）。
        guard !webView.isHidden else {
            finish()
            return true
        }

        // WebView 在窗口坐标系里的位置，转成「距顶部」以便贴图
        let inWindow = webView.convert(webView.bounds, to: nil)
        let config = WKSnapshotConfiguration()
        config.rect = webView.bounds
        webView.takeSnapshot(with: config) { image, _ in
            defer { finish() }
            guard let image else { return }
            let y = bounds.height - inWindow.maxY
            image.draw(in: NSRect(x: inWindow.minX, y: y,
                                  width: inWindow.width, height: inWindow.height))
        }
        return true
    }

    func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) {
        Diag.log("didFail \(error.localizedDescription)")
    }

    func webView(_ webView: WKWebView, didFailProvisionalNavigation navigation: WKNavigation!, withError error: Error) {
        Diag.log("didFailProvisional \(error.localizedDescription)")
    }

    func webViewWebContentProcessDidTerminate(_ webView: WKWebView) {
        Diag.log("content process terminated, reloading")
        webView.reload()
    }
}

// MARK: - WKUIDelegate

extension WebShell: WKUIDelegate {
    func webView(_ webView: WKWebView,
                 createWebViewWith configuration: WKWebViewConfiguration,
                 for navigationAction: WKNavigationAction,
                 windowFeatures: WKWindowFeatures) -> WKWebView? {
        if let url = navigationAction.request.url {
            Diag.log("window.open -> \(url.absoluteString)")
            onExternalLink?(url)
        }
        return nil
    }
}
