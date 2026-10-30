import Foundation

/// 所有落盘路径集中在这里，对应 Windows 版的 AppPaths.cs。
enum ShellPaths {
    /// ~/Library/Application Support/AkPassport
    static let dataRoot: URL = {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        return base.appendingPathComponent("AkPassport", isDirectory: true)
    }()

    static let logFile = dataRoot.appendingPathComponent("shell.log")

    /// H5 产物目录（随 .app 一起分发）。
    static var webRoot: URL {
        if let res = Bundle.main.resourceURL {
            let candidate = res.appendingPathComponent("wwwroot", isDirectory: true)
            if FileManager.default.fileExists(atPath: candidate.appendingPathComponent("index.html").path) {
                return candidate
            }
        }
        // 开发期兜底：可执行文件同级目录
        return Bundle.main.bundleURL.appendingPathComponent("wwwroot", isDirectory: true)
    }

    static func ensureCreated() {
        try? FileManager.default.createDirectory(at: dataRoot, withIntermediateDirectories: true)
    }
}
