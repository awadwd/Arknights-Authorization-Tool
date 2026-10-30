import Foundation
import Network

/// 本地 HTTP 服务：把 H5 产物用 http://127.0.0.1:<port> 提供出去，并代取跨域资源。
///
/// 为什么要本地起服务，而不是 WKWebView.loadFileURL：
/// ① 产物是 `type="module"` 的 ES module，file:// 下会被 CORS 直接判死（Origin null）；
/// ② 固定端口 = 固定 origin，localStorage（收藏 / 设置 / 抽卡记录）才会跨启动保留，
///    这也是 Windows 版强调「WebView2 用户数据目录必须固定」的同一件事。
/// ③ 页面同源 = 前端的 fetch / XHR 打到 /proxy 时没有 CORS 问题，
///    相当于用同源代理实现了 Windows 版 RemoteProxy 的能力。
final class LocalServer {
    /// 固定端口。改这个值等于换站点，用户所有本地数据都会「消失」。
    static let port: UInt16 = 23517

    static let userAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 "
        + "(KHTML, like Gecko) Version/18.0 Safari/605.1.15"

    private let webRoot: URL
    private let queue = DispatchQueue(label: "akpassport.http")
    private let session: URLSession
    private var listener: NWListener?

    var onReady: ((UInt16) -> Void)?
    var onFailure: ((String) -> Void)?

    init(webRoot: URL) {
        self.webRoot = webRoot.standardizedFileURL
        let config = URLSessionConfiguration.ephemeral
        config.timeoutIntervalForRequest = 20
        config.httpAdditionalHeaders = ["User-Agent": LocalServer.userAgent]
        self.session = URLSession(configuration: config)
    }

    var baseURL: URL { URL(string: "http://127.0.0.1:\(LocalServer.port)/")! }

    func start() {
        do {
            let params = NWParameters.tcp
            params.allowLocalEndpointReuse = true
            params.requiredLocalEndpoint = .hostPort(
                host: .ipv4(.loopback),
                port: NWEndpoint.Port(rawValue: LocalServer.port)!
            )
            let listener = try NWListener(using: params)
            listener.newConnectionHandler = { [weak self] connection in
                self?.accept(connection)
            }
            listener.stateUpdateHandler = { [weak self] state in
                switch state {
                case .ready:
                    Diag.log("http server ready on \(LocalServer.port)")
                    self?.onReady?(LocalServer.port)
                case .failed(let error):
                    let message = LocalServer.describe(error)
                    Diag.log("http server failed: \(message)")
                    self?.onFailure?(message)
                default:
                    break
                }
            }
            listener.start(queue: queue)
            self.listener = listener
        } catch {
            onFailure?("本地服务启动失败：\(error.localizedDescription)")
        }
    }

    private static func describe(_ error: Error) -> String {
        if let nwError = error as? NWError, case .posix(let code) = nwError, code == .EADDRINUSE {
            return "端口 \(port) 已被占用。请退出占用该端口的程序后重试"
                + "（终端执行 lsof -nP -iTCP:\(port) 可查看）。"
        }
        return error.localizedDescription
    }

    // MARK: - 连接处理

    private func accept(_ connection: NWConnection) {
        connection.start(queue: queue)
        read(connection, accumulated: Data())
    }

    private func read(_ connection: NWConnection, accumulated: Data) {
        connection.receive(minimumIncompleteLength: 1, maximumLength: 32 * 1024) { [weak self] chunk, _, isComplete, error in
            guard let self else {
                connection.cancel()
                return
            }
            var buffer = accumulated
            if let chunk { buffer.append(chunk) }

            if let end = buffer.range(of: Data([13, 10, 13, 10])) {
                let head = String(decoding: buffer[..<end.lowerBound], as: UTF8.self)
                self.route(connection: connection, head: head)
                return
            }
            if error != nil || isComplete || buffer.count > 64 * 1024 {
                connection.cancel()
                return
            }
            self.read(connection, accumulated: buffer)
        }
    }

    private func route(connection: NWConnection, head: String) {
        guard let requestLine = head.split(separator: "\r\n").first else {
            connection.cancel()
            return
        }
        let parts = requestLine.split(separator: " ").map(String.init)
        guard parts.count >= 2, parts[0] == "GET" else {
            respond(connection, status: 405, headers: [:], body: Data())
            return
        }

        let (path, query) = LocalServer.split(target: parts[1])
        if path == "/proxy" {
            proxy(connection, query: query)
        } else {
            serveStatic(connection, path: path)
        }
    }

    /// 把请求行里的 target 拆成 path / query，兼容绝对形式（http://host/path）与带查询串的路径。
    private static func split(target: String) -> (String, String) {
        if target.hasPrefix("http://") || target.hasPrefix("https://"),
           let components = URLComponents(string: target) {
            return (components.path.isEmpty ? "/" : components.path, components.percentEncodedQuery ?? "")
        }
        guard let index = target.firstIndex(of: "?") else { return (target, "") }
        return (String(target[..<index]), String(target[target.index(after: index)...]))
    }

    // MARK: - 静态文件

    private func serveStatic(_ connection: NWConnection, path: String) {
        var relative = path.hasPrefix("/") ? String(path.dropFirst()) : path
        if relative.isEmpty { relative = "index.html" }
        let decoded = relative.removingPercentEncoding ?? relative

        guard !decoded.contains(".."), !decoded.hasPrefix("/") else {
            respond(connection, status: 403, headers: [:], body: Data())
            return
        }

        var fileURL = webRoot.appendingPathComponent(decoded)
        var isDirectory: ObjCBool = false
        if FileManager.default.fileExists(atPath: fileURL.path, isDirectory: &isDirectory), isDirectory.boolValue {
            fileURL.appendPathComponent("index.html")
        }

        guard fileURL.standardizedFileURL.path.hasPrefix(webRoot.path),
              let body = try? Data(contentsOf: fileURL) else {
            Diag.log("404 \(path)")
            respond(connection, status: 404,
                    headers: ["Content-Type": "text/plain; charset=utf-8"],
                    body: Data("404 \(path)".utf8))
            return
        }

        respond(connection, status: 200,
                headers: ["Content-Type": LocalServer.mimeType(for: fileURL.pathExtension)],
                body: body)
    }

    private static func mimeType(for ext: String) -> String {
        switch ext.lowercased() {
        case "html", "htm": return "text/html; charset=utf-8"
        case "js", "mjs": return "text/javascript; charset=utf-8"
        case "css": return "text/css; charset=utf-8"
        case "json", "map": return "application/json; charset=utf-8"
        case "png": return "image/png"
        case "jpg", "jpeg": return "image/jpeg"
        case "gif": return "image/gif"
        case "webp": return "image/webp"
        case "svg": return "image/svg+xml"
        case "ico": return "image/x-icon"
        case "ttf": return "font/ttf"
        case "otf": return "font/otf"
        case "woff": return "font/woff"
        case "woff2": return "font/woff2"
        case "mp3": return "audio/mpeg"
        case "mp4": return "video/mp4"
        case "wasm": return "application/wasm"
        case "txt": return "text/plain; charset=utf-8"
        default: return "application/octet-stream"
        }
    }

    // MARK: - 跨域代理

    /// 前端把 https://raw.gitcode.com/... 改写成 /proxy?u=<encoded>，
    /// 由这里带 UA 取回并补上 CORS 头，等价于 Windows 版的 RemoteProxy。
    private func proxy(_ connection: NWConnection, query: String) {
        guard let components = URLComponents(string: "http://localhost/?\(query)"),
              let value = components.queryItems?.first(where: { $0.name == "u" })?.value,
              let url = URL(string: value),
              let scheme = url.scheme?.lowercased(),
              scheme == "http" || scheme == "https" else {
            respond(connection, status: 400,
                    headers: ["Content-Type": "text/plain; charset=utf-8"],
                    body: Data("bad proxy target".utf8))
            return
        }

        var request = URLRequest(url: url)
        request.timeoutInterval = 20
        request.setValue(LocalServer.userAgent, forHTTPHeaderField: "User-Agent")
        if let host = url.host, let referer = LocalServer.referer(for: host) {
            request.setValue(referer, forHTTPHeaderField: "Referer")
        }

        session.dataTask(with: request) { [weak self] data, response, error in
            guard let self else {
                connection.cancel()
                return
            }
            if let error {
                Diag.log("proxy failed: \(url.absoluteString) -> \(error.localizedDescription)")
                self.respond(connection, status: 502, headers: [:], body: Data())
                return
            }
            let http = response as? HTTPURLResponse
            let contentType = http?.value(forHTTPHeaderField: "Content-Type") ?? "application/octet-stream"
            self.respond(connection, status: http?.statusCode ?? 200,
                         headers: ["Content-Type": contentType],
                         body: data ?? Data())
        }.resume()
    }

    /// 防盗链图床：必须带它认的 Referer 才肯给图。
    private static func referer(for host: String) -> String? {
        let name = host.lowercased()
        if name.hasSuffix("hdslb.com") || name.hasSuffix("biliimg.com") {
            return "https://www.bilibili.com/"
        }
        if name.hasSuffix("sinaimg.cn") {
            return "https://weibo.com/"
        }
        return nil
    }

    // MARK: - 响应

    private func respond(_ connection: NWConnection, status: Int, headers: [String: String], body: Data) {
        var head = "HTTP/1.1 \(status) \(LocalServer.reason(status))\r\n"
        head += "Connection: close\r\n"
        head += "Access-Control-Allow-Origin: *\r\n"
        head += "Cache-Control: no-cache\r\n"
        for (key, value) in headers { head += "\(key): \(value)\r\n" }
        head += "Content-Length: \(body.count)\r\n\r\n"

        var payload = Data(head.utf8)
        payload.append(body)
        connection.send(content: payload, completion: .contentProcessed { _ in
            connection.cancel()
        })
    }

    private static func reason(_ status: Int) -> String {
        switch status {
        case 200: return "OK"
        case 400: return "Bad Request"
        case 403: return "Forbidden"
        case 404: return "Not Found"
        case 405: return "Method Not Allowed"
        case 502: return "Bad Gateway"
        default: return "Status"
        }
    }
}
