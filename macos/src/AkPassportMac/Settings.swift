import AppKit

/// 原生设置页。
///
/// 为什么不在 H5 里改：`packageA/Setting/Setting.vue` 本体很大，大半是小程序 / App
/// 专属逻辑（自动更新、桌面小组件、自定义数据上传、内购……），桌面上既跑不通也不该出现。
/// 这里只挑「在 macOS 上真正有效」的项，值仍然写回 uni 的 `localStorage`，
/// 底层逻辑和手机端完全共用 —— 包括「干员头像个性化」那份 `avatarPrefSettings`。
///
/// 布局对应 Windows 版 `Views/SettingsView.xaml`：一列圆角分组卡片。
final class SettingsViewController: NSViewController {
    /// 用户挑了主题。由 AppDelegate 负责把外壳、侧边栏、卡片一起换色。
    var onThemePicked: ((String) -> Void)?
    /// 需要跳到某个 H5 页面（自定义搜索词、关于我们）。
    var onNavigate: ((String) -> Void)?
    var onOpenURL: ((URL) -> Void)?
    /// 菜单栏图标开关（存在 UserDefaults 里，不在 localStorage）。
    var onTrayToggle: ((Bool) -> Void)?
    var trayEnabled: Bool = true

    private let store: SettingsStore
    private var settings = WebSettings()
    private var avatarPref = AvatarPref()
    private var theme = ShellTheme.named(nil)

    private let scroll = NSScrollView()
    private let stack = NSStackView()
    private let searchRows = NSStackView()
    private let dataRows = NSStackView()
    private let windowRows = NSStackView()
    private let dataSourceLabel = NSTextField(labelWithString: "—")
    private let versionLabel = NSTextField(labelWithString: "—")
    private let updateLabel = NSTextField(wrappingLabelWithString: "")
    private var themeTiles: [ThemeTile] = []
    private var cardViews: [NSView] = []
    private var avatarCard: AvatarSettingsCard?

    /// 载入时置位，避免「程序化回填 → 触发写回 → 又去刷新页面」的抖动。
    private var suspend = false

    init(store: SettingsStore) {
        self.store = store
        super.init(nibName: nil, bundle: nil)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    // MARK: - 生命周期

    override func loadView() {
        let root = FlippedView()
        root.wantsLayer = true
        view = root

        scroll.translatesAutoresizingMaskIntoConstraints = false
        scroll.drawsBackground = false
        scroll.hasVerticalScroller = true
        scroll.autohidesScrollers = true
        scroll.borderType = .noBorder
        scroll.contentView.drawsBackground = false
        root.addSubview(scroll)

        let document = FlippedView()
        document.translatesAutoresizingMaskIntoConstraints = false
        scroll.documentView = document

        stack.orientation = .vertical
        stack.alignment = .leading
        stack.spacing = 12
        stack.translatesAutoresizingMaskIntoConstraints = false
        document.addSubview(stack)

        NSLayoutConstraint.activate([
            scroll.topAnchor.constraint(equalTo: root.topAnchor),
            scroll.bottomAnchor.constraint(equalTo: root.bottomAnchor),
            scroll.leadingAnchor.constraint(equalTo: root.leadingAnchor),
            scroll.trailingAnchor.constraint(equalTo: root.trailingAnchor),

            document.widthAnchor.constraint(equalTo: scroll.contentView.widthAnchor),

            stack.topAnchor.constraint(equalTo: document.topAnchor, constant: 26),
            stack.bottomAnchor.constraint(equalTo: document.bottomAnchor, constant: -40),
            stack.centerXAnchor.constraint(equalTo: document.centerXAnchor),
            stack.widthAnchor.constraint(lessThanOrEqualToConstant: 920),
            stack.leadingAnchor.constraint(greaterThanOrEqualTo: document.leadingAnchor, constant: 28),
        ])

        buildContent()
        applyTheme(theme)

        // 截图核对用：AKPASSPORT_SETTINGS_SCROLL=900 直接把页面滚到那个高度。
        if let raw = ProcessInfo.processInfo.environment["AKPASSPORT_SETTINGS_SCROLL"],
           let offset = Double(raw) {
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.8) { [weak self] in
                guard let self else { return }
                self.view.layoutSubtreeIfNeeded()
                self.scroll.contentView.scroll(to: NSPoint(x: 0, y: offset))
                self.scroll.reflectScrolledClipView(self.scroll.contentView)
            }
        }
    }

    // MARK: - 内容

    private func buildContent() {
        let title = label("设置", size: 24, weight: .semibold)
        let subtitle = label("macOS 版 · 只列出在桌面端真正有效的项目；改动会立即写回并刷新页面",
                             size: 12, color: .secondaryLabelColor)
        subtitle.maximumNumberOfLines = 2
        subtitle.preferredMaxLayoutWidth = 860

        stack.addArrangedSubview(title)
        stack.addArrangedSubview(subtitle)
        stack.setCustomSpacing(16, after: subtitle)

        addCard(buildThemeCard())
        addCard(buildSearchCard())
        let avatar = AvatarSettingsCard()
        avatar.onChange = { [weak self] pref in
            guard let self, !self.suspend else { return }
            self.avatarPref = pref
            self.store.setAvatarPref(pref)
            Diag.log("settings: avatar pref updated rules=\(pref.rules.count) white=\(pref.white.count) black=\(pref.black.count)")
        }
        avatarCard = avatar
        addCard(avatar)
        addCard(buildDataCard())
        addCard(buildWindowCard())
        addCard(buildAboutCard())

        // 标题列宽度固定，行内容撑满
        stack.arrangedSubviews.forEach {
            $0.widthAnchor.constraint(equalTo: stack.widthAnchor).isActive = true
        }
    }

    private func addCard(_ card: NSView) {
        cardViews.append(card)
        stack.addArrangedSubview(card)
    }

    // MARK: - 外观

    private static let themes: [(id: String, name: String, desc: String, pane: Int, panel: Int, accent: Int)] = [
        ("liquid", "液态玻璃", "默认 · iOS 26 冷白", 0xECF1F7, 0xFAFCFF, 0x0A84FF),
        ("liquidDark", "液态·深色", "深底玻璃控件", 0x0F1114, 0x171A1E, 0x0A84FF),
        ("ak", "工业", "ak-ui 纸面工业", 0xEFEFEC, 0xFDFDFB, 0xC2410C),
        ("ark", "深色", "石墨底 + 信号色", 0x131415, 0x1B1D1E, 0xF5C86B),
        ("jieyuan", "界园", "米纸底 · 暖褐", 0xFAF3E0, 0xFFFDF7, 0x8C5A2B),
    ]

    private func buildThemeCard() -> NSView {
        // 两行三列。用等分 NSStackView 而不是 NSGridView：后者会自己管 cell 的
        // frame，和瓦片内部的约束打架（实测高度被压成一条，文字全裁掉）。
        var lines: [NSStackView] = []
        var index = 0
        while index < SettingsViewController.themes.count {
            let line = NSStackView()
            line.orientation = .horizontal
            line.spacing = 10
            line.distribution = .fillEqually
            for offset in 0..<3 {
                if index + offset < SettingsViewController.themes.count {
                    let tile = ThemeTile(option: SettingsViewController.themes[index + offset])
                    tile.target = self
                    tile.action = #selector(pickTheme(_:))
                    tile.translatesAutoresizingMaskIntoConstraints = false
                    tile.heightAnchor.constraint(equalToConstant: 64).isActive = true
                    themeTiles.append(tile)
                    line.addArrangedSubview(tile)
                } else {
                    // 末行不足三个时留空位，保证上面的瓦片不被拉宽
                    line.addArrangedSubview(NSView())
                }
            }
            index += 3
            lines.append(line)
        }

        let body = NSStackView(views: lines)
        body.orientation = .vertical
        body.spacing = 10
        body.alignment = .leading
        for line in lines {
            line.widthAnchor.constraint(equalTo: body.widthAnchor).isActive = true
        }

        return card(title: "外观",
                    hint: "主题同时作用于窗口外壳、侧边栏与页面内容。",
                    body: body)
    }

    @objc private func pickTheme(_ sender: ThemeTile) {
        guard !suspend else { return }
        settings.theme = sender.themeID
        store.setTheme(sender.themeID)
        onThemePicked?(sender.themeID)
        refreshThemeSelection(sender.themeID)
        Diag.log("settings: theme -> \(sender.themeID)")
    }

    /// 自动化验证用：等价于点一下那块主题瓦片。
    func pickThemeForTest(_ id: String) {
        guard let tile = themeTiles.first(where: { $0.themeID == id }) else {
            Diag.log("theme test: 未找到 \(id)")
            return
        }
        pickTheme(tile)
    }

    private func refreshThemeSelection(_ id: String) {
        for tile in themeTiles { tile.setSelected(tile.themeID == id, theme: theme) }
    }

    // MARK: - 显示与搜索

    private func buildSearchCard() -> NSView {
        searchRows.orientation = .vertical
        searchRows.spacing = 0
        searchRows.alignment = .leading

        let body = NSStackView()
        body.orientation = .vertical
        body.spacing = 10
        body.alignment = .leading
        body.addArrangedSubview(searchRows)
        body.addArrangedSubview(separator())
        body.addArrangedSubview(actionRow(
            title: "自定义搜索词",
            detail: "给自己常用的外号 / 简称建别名，搜索时一并命中",
            buttonTitle: "管理"
        ) { [weak self] in
            // 方舟的自定义搜索词页面（终末地那个是同名但不同路径的页面）
            self?.onNavigate?("packageA/custom-words/custom-words")
        })

        return card(title: "显示与搜索",
                    hint: "控制列表与查询页显示哪些信息、支持哪些关键词。",
                    body: body)
    }

    // MARK: - 数据

    private func buildDataCard() -> NSView {
        dataRows.orientation = .vertical
        dataRows.spacing = 0
        dataRows.alignment = .leading

        dataSourceLabel.font = .systemFont(ofSize: 11.5)
        dataSourceLabel.textColor = .secondaryLabelColor
        dataSourceLabel.maximumNumberOfLines = 2

        let sourceStack = NSStackView(views: [label("当前数据源", size: 13.5), dataSourceLabel])
        sourceStack.orientation = .vertical
        sourceStack.spacing = 2
        sourceStack.alignment = .leading

        let body = NSStackView()
        body.orientation = .vertical
        body.spacing = 10
        body.alignment = .leading
        body.addArrangedSubview(sourceStack)
        body.addArrangedSubview(dataRows)

        return card(title: "数据",
                    hint: "数据随程序一起分发；这里只控制更新行为与来源显示。",
                    body: body)
    }

    // MARK: - 窗口

    private func buildWindowCard() -> NSView {
        windowRows.orientation = .vertical
        windowRows.spacing = 0
        windowRows.alignment = .leading

        let body = NSStackView()
        body.orientation = .vertical
        body.spacing = 10
        body.alignment = .leading
        body.addArrangedSubview(windowRows)

        let reloadRow = actionRow(
            title: "重新加载页面",
            detail: "H5 卡住或数据没刷新时用这个，等价于菜单里的「重新加载」",
            buttonTitle: "重新加载"
        ) { [weak self] in
            self?.store.shell?.reload()
        }
        body.addArrangedSubview(separator())
        body.addArrangedSubview(reloadRow)

        return card(title: "窗口",
                    hint: "桌面端独有的窗口行为。开机自启与全局快捷键尚未实现。",
                    body: body)
    }

    // MARK: - 关于

    private func buildAboutCard() -> NSView {
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0.1.0"
        versionLabel.stringValue = "macOS 版 \(version) · Swift + AppKit + WKWebView"
        versionLabel.font = .systemFont(ofSize: 11.5)
        versionLabel.textColor = .secondaryLabelColor

        let nameStack = NSStackView(views: [label("方舟通行证谷子查询工具", size: 13.5), versionLabel])
        nameStack.orientation = .vertical
        nameStack.spacing = 2
        nameStack.alignment = .leading

        let buttons = NSStackView()
        buttons.orientation = .horizontal
        buttons.spacing = 8
        buttons.addArrangedSubview(button("检查更新") { [weak self] in self?.checkUpdate() })
        buttons.addArrangedSubview(button("关于我们") { [weak self] in
            self?.onNavigate?("packageA/aboutus/aboutus")
        })
        buttons.addArrangedSubview(button("项目主页") { [weak self] in
            self?.onOpenURL?(URL(string: "https://github.com/awadwd/Arknights-Authorization-Tool")!)
        })

        let row = NSStackView(views: [nameStack, NSView(), buttons])
        row.orientation = .horizontal
        row.spacing = 12

        updateLabel.font = .systemFont(ofSize: 12)
        updateLabel.textColor = .secondaryLabelColor
        updateLabel.isHidden = true
        updateLabel.preferredMaxLayoutWidth = 860

        let disclaimer = label("本程序为第三方工具，与鹰角网络无关。数据版权归原作者所有。",
                               size: 11.5, color: .secondaryLabelColor)
        disclaimer.maximumNumberOfLines = 2

        let body = NSStackView()
        body.orientation = .vertical
        body.spacing = 10
        body.alignment = .leading
        body.addArrangedSubview(row)
        body.addArrangedSubview(updateLabel)
        body.addArrangedSubview(separator())
        body.addArrangedSubview(disclaimer)

        return card(title: "关于", hint: nil, body: body)
    }

    private func checkUpdate() {
        updateLabel.isHidden = false
        updateLabel.stringValue = "macOS 版目前跟随主仓库源码分发，没有独立的更新通道；请到项目主页的 Releases 查看。"
        Diag.log("settings: check update")
    }

    // MARK: - 回填

    /// 进入设置页时调用：先读 localStorage，再填到控件上。
    func refresh() {
        store.readSettings { [weak self] settings in
            guard let self else { return }
            guard let settings else {
                Diag.log("settings: 读取 localStorage 失败（H5 可能还没加载完）")
                return
            }
            self.settings = settings
            self.store.readAvatarPref { [weak self] pref in
                guard let self else { return }
                self.avatarPref = pref
                self.render()
            }
        }
    }

    private func render() {
        suspend = true
        defer { suspend = false }
        // 每次重建都会造一批新开关。旧条目不清掉，ObjectIdentifier 被复用后
        // 会拨到上一个开关的闭包上去。
        toggles.removeAll()
        toggleActions.removeAll()

        settings.theme = SettingsViewController.themes.contains { $0.id == settings.theme }
            ? settings.theme : "liquid"
        refreshThemeSelection(settings.theme)

        rebuild(searchRows, rows: [
            ("显示市场价", "在列表与详情里显示干员通行证的市场参考价", settings.showMarketPrice,
             "showMarketPrice"),
            ("英文名搜索", "支持用英文名 / 英文别名检索干员", settings.enableEnglishSearch,
             "enableEnglishSearch"),
            ("日文名搜索", "支持用日文名检索干员", settings.enableJapaneseSearch,
             "enableJapaneseSearch"),
            ("外号搜索", "支持社区常用外号（仅部分干员）", settings.enableNicknameSearch,
             "enableNicknameSearch"),
            ("预测通行证", "显示官方未公布、由社区预测的新盒号数据", settings.enableGuessData,
             "enableGuessData"),
        ])

        rebuild(windowRows, rows: [
            ("显示菜单栏图标", "在系统菜单栏放一个图标，方便随时唤起主窗口", trayEnabled, "@tray"),
        ])

        // 数据卡片：自动更新走的不是布尔，单独接
        dataRows.arrangedSubviews.forEach { $0.removeFromSuperview() }
        dataRows.addArrangedSubview(toggleRow(
            title: "自动更新数据",
            detail: "启动时静默检查并更新本地盒号数据",
            value: settings.autoUpdate
        ) { [weak self] value in
            guard let self, !self.suspend else { return }
            self.settings.autoUpdate = value
            self.store.setAutoUpdate(value)
        })
        dataRows.addArrangedSubview(toggleRow(
            title: "数据更新提示",
            detail: "发现新版本数据时提醒，避免静默错过更新",
            value: settings.updateNotify
        ) { [weak self] value in
            guard let self, !self.suspend else { return }
            self.settings.updateNotify = value
            self.store.setBool("updateNotifyEnabled", value)
        })

        dataSourceLabel.stringValue = describeSource(settings.dataSource) + " · 数据 "
            + fallback(settings.dataVersion) + " · 更新于 " + fallback(settings.lastUpdate)

        avatarCard?.render(avatarPref, theme: theme)
    }

    private func rebuild(_ target: NSStackView, rows: [(String, String, Bool, String)]) {
        target.arrangedSubviews.forEach { $0.removeFromSuperview() }
        for (index, row) in rows.enumerated() {
            let view = toggleRow(title: row.0, detail: row.1, value: row.2, last: index == rows.count - 1) {
                [weak self] value in
                guard let self, !self.suspend else { return }
                if row.3 == "@tray" {
                    self.trayEnabled = value
                    self.onTrayToggle?(value)
                    return
                }
                self.store.setBool(row.3, value)
            }
            target.addArrangedSubview(view)
        }
    }

    private func describeSource(_ key: String) -> String {
        switch key {
        case "knowCloud": return "知晓云源"
        case "domestic": return "国内源（GitCode）"
        case "github": return "GitHub 源"
        case "cdn": return "CDN 源"
        case "local": return "本地数据"
        case "custom": return "自定义数据"
        default: return "默认源"
        }
    }

    private func fallback(_ value: String) -> String {
        value.isEmpty ? "未知" : value
    }

    // MARK: - 主题

    func applyTheme(_ newTheme: ShellTheme) {
        theme = newTheme
        view.appearance = NSAppearance(named: newTheme.dark ? .darkAqua : .aqua)
        view.layer?.backgroundColor = newTheme.canvas.cgColor
        for card in cardViews {
            card.layer?.backgroundColor = newTheme.panel.cgColor
            card.layer?.borderColor = newTheme.border.cgColor
        }
        refreshThemeSelection(settings.theme)
        avatarCard?.applyTheme(newTheme)
    }

    // MARK: - 组件工厂

    private func label(_ text: String, size: CGFloat,
                       weight: NSFont.Weight = .regular,
                       color: NSColor = .labelColor) -> NSTextField {
        let field = NSTextField(labelWithString: text)
        field.font = .systemFont(ofSize: size, weight: weight)
        field.textColor = color
        field.lineBreakMode = .byTruncatingTail
        return field
    }

    private func button(_ title: String, action: @escaping () -> Void) -> NSButton {
        let view = ActionButton(title: title, handler: action)
        view.controlSize = .regular
        return view
    }

    private func separator() -> NSView {
        let line = NSView()
        line.wantsLayer = true
        line.translatesAutoresizingMaskIntoConstraints = false
        line.heightAnchor.constraint(equalToConstant: 1).isActive = true
        line.layer?.backgroundColor = theme.border.cgColor
        return line
    }

    /// 一张分组卡片：标题 + 说明 + 内容。
    private func card(title: String, hint: String?, body: NSView) -> NSView {
        let container = NSView()
        container.wantsLayer = true
        container.layer?.cornerRadius = 10
        container.layer?.borderWidth = 1
        container.layer?.backgroundColor = theme.panel.cgColor
        container.layer?.borderColor = theme.border.cgColor

        let inner = NSStackView()
        inner.orientation = .vertical
        inner.alignment = .leading
        inner.spacing = 10
        inner.translatesAutoresizingMaskIntoConstraints = false
        inner.addArrangedSubview(label(title, size: 14, weight: .semibold))
        if let hint {
            let hintLabel = label(hint, size: 11.5, color: .secondaryLabelColor)
            hintLabel.maximumNumberOfLines = 3
            hintLabel.preferredMaxLayoutWidth = 840
            inner.addArrangedSubview(hintLabel)
            inner.setCustomSpacing(6, after: hintLabel)
        }
        inner.addArrangedSubview(body)

        container.addSubview(inner)
        NSLayoutConstraint.activate([
            inner.topAnchor.constraint(equalTo: container.topAnchor, constant: 16),
            inner.bottomAnchor.constraint(equalTo: container.bottomAnchor, constant: -14),
            inner.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 18),
            inner.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -18),
            body.widthAnchor.constraint(equalTo: inner.widthAnchor),
        ])
        return container
    }

    /// 「标题 + 副标题 …… 开关」一行。
    private func toggleRow(title: String, detail: String, value: Bool,
                           last: Bool = false,
                           onChange: @escaping (Bool) -> Void) -> NSView {
        let row = NSStackView()
        row.orientation = .horizontal
        row.spacing = 12
        row.alignment = .centerY

        let texts = NSStackView()
        texts.orientation = .vertical
        texts.spacing = 2
        texts.alignment = .leading
        texts.addArrangedSubview(label(title, size: 13.5))
        let detailLabel = label(detail, size: 11.5, color: .secondaryLabelColor)
        detailLabel.maximumNumberOfLines = 2
        detailLabel.preferredMaxLayoutWidth = 620
        texts.addArrangedSubview(detailLabel)

        let toggle = NSSwitch()
        toggle.state = value ? .on : .off
        toggle.controlSize = .small
        toggle.setContentHuggingPriority(.required, for: .horizontal)
        toggle.target = self
        toggle.action = #selector(toggleChanged(_:))
        toggle.identifier = NSUserInterfaceItemIdentifier(title)
        toggleActions[ObjectIdentifier(toggle)] = onChange
        toggles[title] = toggle

        row.addArrangedSubview(texts)
        row.addArrangedSubview(NSView())
        row.addArrangedSubview(toggle)
        // 两行文字本身只有三十来点高，不给下限的话 NSStackView 会把每行压到
        // 紧贴在一起 —— 五六个开关连成一坨，完全看不出是一项一项的。
        row.heightAnchor.constraint(greaterThanOrEqualToConstant: last ? 40 : 46).isActive = true
        return row
    }

    /// 开关的动作闭包。NSSwitch 只能带一个 target/action，用表来存各自回调。
    private var toggleActions: [ObjectIdentifier: (Bool) -> Void] = [:]
    /// 按标题索引的开关，供自动化验证触发。
    private var toggles: [String: NSSwitch] = [:]

    /// 自动化验证用：等价于手动拨一下某个开关（走完整的 action 链路）。
    func setToggleForTest(_ title: String, on: Bool) {
        guard let toggle = toggles[title] else {
            Diag.log("toggle test: 未找到「\(title)」")
            return
        }
        toggle.state = on ? .on : .off
        toggleChanged(toggle)
    }

    @objc private func toggleChanged(_ sender: NSSwitch) {
        toggleActions[ObjectIdentifier(sender)]?(sender.state == .on)
    }

    /// 「标题 + 副标题 …… 按钮」一行。
    private func actionRow(title: String, detail: String, buttonTitle: String,
                           action: @escaping () -> Void) -> NSView {
        let row = NSStackView()
        row.orientation = .horizontal
        row.spacing = 12
        row.alignment = .centerY

        let texts = NSStackView()
        texts.orientation = .vertical
        texts.spacing = 2
        texts.alignment = .leading
        texts.addArrangedSubview(label(title, size: 13.5))
        let detailLabel = label(detail, size: 11.5, color: .secondaryLabelColor)
        detailLabel.maximumNumberOfLines = 2
        detailLabel.preferredMaxLayoutWidth = 620
        texts.addArrangedSubview(detailLabel)

        let actionButton = button(buttonTitle, action: action)
        actionButton.setContentHuggingPriority(.required, for: .horizontal)

        row.addArrangedSubview(texts)
        row.addArrangedSubview(NSView())
        row.addArrangedSubview(actionButton)
        row.heightAnchor.constraint(greaterThanOrEqualToConstant: 46).isActive = true
        return row
    }
}

// MARK: - 小控件

/// 从底部向上排的容器。NSScrollView 的 documentView 必须是翻转坐标系，
/// 否则内容会贴着底部、滚动条位置也不对。
final class FlippedView: NSView {
    override var isFlipped: Bool { true }
}

/// 带闭包的按钮。AppKit 的 target/action 需要 @objc 方法，
/// 用闭包包一层比给每个按钮写一个 selector 干净。
final class ActionButton: NSButton {
    private let handler: () -> Void

    init(title: String, handler: @escaping () -> Void) {
        self.handler = handler
        super.init(frame: .zero)
        self.title = title
        self.bezelStyle = .rounded
        self.target = self
        self.action = #selector(invoke)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    @objc private func invoke() { handler() }
}

/// 主题瓦片：色板预览 + 名称 + 描述，选中时描一圈强调色。
final class ThemeTile: NSView {
    let themeID: String
    private let preview = NSView()
    private let accentDot = NSView()
    private let nameLabel = NSTextField(labelWithString: "")
    private let descLabel = NSTextField(labelWithString: "")
    private let pane: NSColor
    private let panel: NSColor
    private let accent: NSColor
    private var selected = false
    private var theme: ShellTheme?

    var target: AnyObject?
    var action: Selector?

    init(option: (id: String, name: String, desc: String, pane: Int, panel: Int, accent: Int)) {
        themeID = option.id
        pane = ThemeTile.color(option.pane)
        panel = ThemeTile.color(option.panel)
        accent = ThemeTile.color(option.accent)
        super.init(frame: .zero)

        wantsLayer = true
        layer?.cornerRadius = 8
        layer?.borderWidth = 1

        preview.wantsLayer = true
        preview.layer?.cornerRadius = 5
        preview.translatesAutoresizingMaskIntoConstraints = false

        accentDot.wantsLayer = true
        accentDot.layer?.cornerRadius = 4
        accentDot.translatesAutoresizingMaskIntoConstraints = false

        nameLabel.stringValue = option.name
        nameLabel.font = .systemFont(ofSize: 12.5, weight: .medium)
        nameLabel.translatesAutoresizingMaskIntoConstraints = false
        descLabel.stringValue = option.desc
        descLabel.font = .systemFont(ofSize: 10.5)
        descLabel.textColor = .secondaryLabelColor
        descLabel.translatesAutoresizingMaskIntoConstraints = false

        addSubview(preview)
        addSubview(accentDot)
        addSubview(nameLabel)
        addSubview(descLabel)

        NSLayoutConstraint.activate([
            preview.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 10),
            preview.topAnchor.constraint(equalTo: topAnchor, constant: 10),
            preview.bottomAnchor.constraint(equalTo: bottomAnchor, constant: -10),
            preview.widthAnchor.constraint(equalToConstant: 38),

            accentDot.trailingAnchor.constraint(equalTo: preview.trailingAnchor, constant: -4),
            accentDot.bottomAnchor.constraint(equalTo: preview.bottomAnchor, constant: -4),
            accentDot.widthAnchor.constraint(equalToConstant: 8),
            accentDot.heightAnchor.constraint(equalToConstant: 8),

            nameLabel.leadingAnchor.constraint(equalTo: preview.trailingAnchor, constant: 10),
            nameLabel.trailingAnchor.constraint(lessThanOrEqualTo: trailingAnchor, constant: -10),
            nameLabel.topAnchor.constraint(equalTo: topAnchor, constant: 14),

            descLabel.leadingAnchor.constraint(equalTo: nameLabel.leadingAnchor),
            descLabel.trailingAnchor.constraint(lessThanOrEqualTo: trailingAnchor, constant: -10),
            descLabel.topAnchor.constraint(equalTo: nameLabel.bottomAnchor, constant: 2),
        ])

        preview.layer?.backgroundColor = pane.cgColor
        accentDot.layer?.backgroundColor = accent.cgColor
        descLabel.textColor = .secondaryLabelColor
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    private static func color(_ rgb: Int) -> NSColor {
        NSColor(srgbRed: CGFloat((rgb >> 16) & 0xFF) / 255.0,
                green: CGFloat((rgb >> 8) & 0xFF) / 255.0,
                blue: CGFloat(rgb & 0xFF) / 255.0,
                alpha: 1.0)
    }

    override func mouseDown(with event: NSEvent) {
        guard let target, let action else { return }
        NSApp.sendAction(action, to: target, from: self)
    }

    func setSelected(_ value: Bool, theme: ShellTheme) {
        self.theme = theme
        selected = value
        layer?.borderWidth = value ? 2 : 1
        layer?.borderColor = value ? NSColor.controlAccentColor.cgColor : theme.border.cgColor
        layer?.backgroundColor = value
            ? NSColor.controlAccentColor.withAlphaComponent(0.07).cgColor
            : theme.panel.cgColor
        nameLabel.textColor = .labelColor
        descLabel.textColor = .secondaryLabelColor
    }
}
