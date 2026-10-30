import AppKit

/// 壳的主题色板，逐值移植自 Windows 版 Services/ShellTheme.cs。
/// 前端有 6 套主题（simple / ak / ark / jieyuan / liquid / liquidDark），
/// macOS 侧只用到「画布色」填充内容区留白，以及是否深色的判断。
struct ShellTheme {
    let canvas: NSColor
    let pane: NSColor
    let panel: NSColor
    let border: NSColor
    let text: NSColor
    let dark: Bool

    init(hex: Int, pane: Int, panel: Int, border: Int, text: Int, dark: Bool) {
        self.canvas = ShellTheme.color(hex)
        self.pane = ShellTheme.color(pane)
        self.panel = ShellTheme.color(panel)
        self.border = ShellTheme.color(border)
        self.text = ShellTheme.color(text)
        self.dark = dark
    }

    static func named(_ raw: String?) -> ShellTheme {
        switch (raw ?? "").trimmingCharacters(in: .whitespaces).lowercased() {
        case "ark":
            return ShellTheme(hex: 0x131415, pane: 0x1B1D1E, panel: 0x1B1D1E,
                              border: 0x2C3032, text: 0xECEBE6, dark: true)
        case "liquiddark":
            return ShellTheme(hex: 0x0F1114, pane: 0x171A1E, panel: 0x171A1E,
                              border: 0x2A2F36, text: 0xE8EAED, dark: true)
        case "ak":
            return ShellTheme(hex: 0xEFEFEC, pane: 0xFDFDFB, panel: 0xFDFDFB,
                              border: 0xDBDCD6, text: 0x1B1D1F, dark: false)
        case "jieyuan":
            return ShellTheme(hex: 0xFAF3E0, pane: 0xFFFDF7, panel: 0xFFFDF7,
                              border: 0xEBDFC6, text: 0x4A3B2E, dark: false)
        default:
            // 液态玻璃（simple 在桌面端已被替换为 liquid）
            return ShellTheme(hex: 0xECF1F7, pane: 0xFAFCFF, panel: 0xFAFCFF,
                              border: 0xDDE4EE, text: 0x1C222B, dark: false)
        }
    }

    private static func color(_ rgb: Int) -> NSColor {
        NSColor(srgbRed: CGFloat((rgb >> 16) & 0xFF) / 255.0,
                green: CGFloat((rgb >> 8) & 0xFF) / 255.0,
                blue: CGFloat(rgb & 0xFF) / 255.0,
                alpha: 1.0)
    }
}
