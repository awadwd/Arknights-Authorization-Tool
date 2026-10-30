import AppKit

/// 侧边栏的一项。
///
/// `route` 与 Windows 版 `NavigationViewItem.Tag` 逐字一致 —— 它同时是
/// 「点击后要去的路由」和「H5 当前 hash 的前缀匹配键」，两边必须同一份数据。
struct NavEntry {
    let title: String
    let route: String
    let symbol: String
}

/// 一个工具包 = 一套彼此独立的页面集合。
///
/// 产物里本来就有两条线：方舟（`pages/` + `packageA/B`）和终末地
/// （`package_ArknightEnds/`）。前端只给了移动端首页一个隐晦入口，
/// 桌面端靠侧边栏顶部的分段控件切换 —— 比在 H5 里找入口直观得多。
struct Toolkit {
    let id: String
    /// 分段控件上的短名。
    let shortTitle: String
    /// 切到这个工具包时落地的页面。
    let home: String
    /// 归属判定用。方舟是「默认」所以前缀为空，靠 `owns` 里的取反处理。
    let routePrefix: String
    let primary: [NavEntry]
    let secondary: [NavEntry]
    let settings: NavEntry

    /// 这条路由是否属于本工具包。用来在页面内部跳转时反向切侧边栏。
    func owns(route: String) -> Bool {
        routePrefix.isEmpty ? !route.hasPrefix(NavCatalog.endsPrefix) : route.hasPrefix(routePrefix)
    }
}

/// 导航目录，方舟部分移植自 MainWindow.xaml.cs 的 AkPrimary / AkSecondary。
enum NavCatalog {
    static let endsPrefix = "package_ArknightEnds/"

    /// 原生设置页的伪路由。它不对应 H5 里的任何页面 —— 外壳看到这个值就把
    /// WebView 藏起来、换上原生设置视图。
    static let nativeSettings = "native/settings"

    /// 这个路由是不是由原生视图接管（不走 H5）。
    static func isNative(_ route: String) -> Bool {
        route == nativeSettings
    }

    /// 明日方舟。
    static let ark = Toolkit(
        id: "ark",
        shortTitle: "方舟",
        home: "pages/desktop_home/desktop_home",
        routePrefix: "",
        primary: [
            NavEntry(title: "首页", route: "pages/desktop_home/desktop_home", symbol: "house"),
            NavEntry(title: "通行证查询", route: "pages/Search/Search", symbol: "magnifyingglass"),
            NavEntry(title: "通行证列表", route: "pages/list/list", symbol: "list.bullet"),
            NavEntry(title: "模拟抽卡", route: "packageB/pages/gacha_simulated/gacha_simulated", symbol: "sparkles"),
            NavEntry(title: "我的收藏", route: "packageB/pages/mystarbox/mystarbox", symbol: "star"),
        ],
        secondary: [
            NavEntry(title: "通行证展柜", route: "pages/card_share/card_share", symbol: "photo.on.rectangle"),
            NavEntry(title: "通行证日历", route: "packageA/calendar/calendar", symbol: "calendar"),
            NavEntry(title: "今日宜挂", route: "pages/daily_recommend/daily_recommend", symbol: "clock"),
            NavEntry(title: "数据反馈", route: "pages/box_feedback_new/box_feedback_new", symbol: "bubble.left"),
        ],
        // 和 Windows 版一样，底部「设置」进的是原生设置页（不是 H5 的 Vue 页）：
        // 那个页面里大半是小程序 / App 专属逻辑，桌面上既跑不通也不该出现。
        settings: NavEntry(title: "设置", route: NavCatalog.nativeSettings, symbol: "gearshape")
    )

    /// 终末地。工具清单取自 `package_ArknightEnds/index/index` 里的跳转目标，
    /// 公告与「自定义搜索词」属于二级页，放进「更多」组。
    static let ends = Toolkit(
        id: "ends",
        shortTitle: "终末地",
        home: "package_ArknightEnds/index/index",
        routePrefix: endsPrefix,
        primary: [
            NavEntry(title: "工具箱", route: "package_ArknightEnds/index/index", symbol: "square.grid.2x2"),
            NavEntry(title: "凭证查询", route: "package_ArknightEnds/Search/Search", symbol: "magnifyingglass"),
            NavEntry(title: "凭证列表", route: "package_ArknightEnds/list/list", symbol: "list.bullet"),
            NavEntry(title: "模拟抽卡", route: "package_ArknightEnds/gacha_simulated/gacha_simulated", symbol: "sparkles"),
            NavEntry(title: "终末地日历", route: "package_ArknightEnds/calendar/calendar", symbol: "calendar"),
        ],
        secondary: [
            NavEntry(title: "自定义搜索词", route: "package_ArknightEnds/custom-words/custom-words", symbol: "text.badge.plus"),
            NavEntry(title: "公告", route: "package_ArknightEnds/more-notice/more-notice", symbol: "megaphone"),
        ],
        settings: NavEntry(title: "设置", route: "package_ArknightEnds/Setting/Setting", symbol: "gearshape")
    )

    static let all: [Toolkit] = [ark, ends]

    /// 按路由反查工具包。方舟的 `owns` 是兜底（非终末地即方舟），所以顺序不能颠倒。
    static func toolkit(for route: String) -> Toolkit {
        all.first { $0.owns(route: route) } ?? ark
    }

    static func toolkit(id: String?) -> Toolkit {
        all.first { $0.id == id } ?? ark
    }

    static func symbol(named name: String, pointSize: CGFloat = 13) -> NSImage? {
        let config = NSImage.SymbolConfiguration(pointSize: pointSize, weight: .regular)
        guard let image = NSImage(systemSymbolName: name, accessibilityDescription: nil) else {
            return nil
        }
        let scaled = image.withSymbolConfiguration(config) ?? image
        scaled.isTemplate = true
        return scaled
    }
}

// MARK: - 行视图

/// 图标 + 文字的一行。选中态由 `backgroundStyle` 驱动：
/// NSTableView 只负责告诉我们「这行被高亮了」，颜色要自己翻转，
/// 否则蓝底上会留下深色文字。
final class NavRowCell: NSTableCellView {
    private let icon = NSImageView()
    private let label = NSTextField(labelWithString: "")
    /// 当前是否深色。系统语义色（`.labelColor`）在换 appearance 之后不会自己重算 ——
    /// NSTextField 设置颜色那一刻就把动态色解析成了具体值，所以深色主题下侧边栏会
    /// 留下一串几乎看不见的深灰字。这里直接按明暗给实色。
    private var dark = false

    init(identifier: NSUserInterfaceItemIdentifier, indented: Bool) {
        super.init(frame: .zero)
        self.identifier = identifier

        icon.translatesAutoresizingMaskIntoConstraints = false
        icon.imageScaling = .scaleProportionallyDown
        addSubview(icon)
        imageView = icon

        label.translatesAutoresizingMaskIntoConstraints = false
        label.font = .systemFont(ofSize: 13)
        label.lineBreakMode = .byTruncatingTail
        label.textColor = .labelColor
        addSubview(label)
        textField = label

        let leading: CGFloat = indented ? 6 : 0
        NSLayoutConstraint.activate([
            icon.leadingAnchor.constraint(equalTo: leadingAnchor, constant: leading),
            icon.centerYAnchor.constraint(equalTo: centerYAnchor),
            icon.widthAnchor.constraint(equalToConstant: 17),
            icon.heightAnchor.constraint(equalToConstant: 17),
            label.leadingAnchor.constraint(equalTo: icon.trailingAnchor, constant: 9),
            label.trailingAnchor.constraint(lessThanOrEqualTo: trailingAnchor, constant: -6),
            label.centerYAnchor.constraint(equalTo: centerYAnchor),
        ])
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    func configure(_ entry: NavEntry, dark: Bool) {
        self.dark = dark
        label.stringValue = entry.title
        icon.image = NavCatalog.symbol(named: entry.symbol)
        applyColors()
    }

    /// 由行视图驱动反色（系统高亮关闭后 `backgroundStyle` 不再变化）。
    func setActive(_ active: Bool) {
        guard active != isActive else { return }
        isActive = active
        applyColors()
    }

    func refreshColors(dark: Bool) {
        guard self.dark != dark else { return }
        self.dark = dark
        applyColors()
    }

    private func applyColors() {
        let primary = dark ? NSColor(white: 0.93, alpha: 1) : NSColor(white: 0.10, alpha: 1)
        let secondary = dark ? NSColor(white: 0.60, alpha: 1) : NSColor(white: 0.40, alpha: 1)
        label.textColor = isActive ? .white : primary
        icon.contentTintColor = isActive ? .white : secondary
    }

    private var isActive = false
}

/// 分组标题（「更多」）。
final class NavHeaderCell: NSTableCellView {
    init(identifier: NSUserInterfaceItemIdentifier) {
        super.init(frame: .zero)
        self.identifier = identifier
        let label = NSTextField(labelWithString: "")
        label.translatesAutoresizingMaskIntoConstraints = false
        label.font = .systemFont(ofSize: 11, weight: .semibold)
        label.textColor = .secondaryLabelColor
        addSubview(label)
        textField = label
        NSLayoutConstraint.activate([
            label.leadingAnchor.constraint(equalTo: leadingAnchor),
            label.trailingAnchor.constraint(lessThanOrEqualTo: trailingAnchor),
            label.bottomAnchor.constraint(equalTo: bottomAnchor, constant: -2),
        ])
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    func setDark(_ dark: Bool) {
        textField?.textColor = dark
            ? NSColor(white: 0.58, alpha: 1)
            : NSColor(white: 0.42, alpha: 1)
    }
}

/// 选中态自己画。
///
/// `.sourceList` 样式的 NSTableView 不走 `drawSelection(in:)`（实测该方法一次都没被调用），
/// 它在内部用私有图层画高亮 —— 而那个图层和侧边栏材质是同一个，为了保住自定义底色
/// 必须把它一起摘掉。摘掉之后高亮只能自己来，于是统一在 `draw` 里画胶囊。
final class NavRowView: NSTableRowView {
    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        // 关掉系统高亮绘制，但保留 isSelected 状态（键盘导航、无障碍都靠它）
        selectionHighlightStyle = .none
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    override func draw(_ dirtyRect: NSRect) {
        if isSelected {
            let rect = bounds.insetBy(dx: 6, dy: 1)
            NSColor.controlAccentColor.withAlphaComponent(0.92).setFill()
            NSBezierPath(roundedRect: rect, xRadius: 6, yRadius: 6).fill()
        }
        super.draw(dirtyRect)
        // 系统不再把 backgroundStyle 置为 emphasized，文字反色得手动同步，
        // 否则蓝底上压着一行深灰字，基本看不清。
        for case let cell as NavRowCell in subviews {
            cell.setActive(isSelected)
        }
    }
}

// MARK: - 分栏容器

// 侧边栏宽度：用 NSSplitViewItem 的 minimumThickness 定死下限，而不是
// setPosition / autosave。后两者在 NSSplitViewController 下都会被内部约束覆盖
// （实测目标 212，final frame 永远回到 180）。最小值即默认值，用户仍可拖宽。
let sidebarWidth: CGFloat = 212
let sidebarMaxWidth: CGFloat = 320

// MARK: - 侧边栏

final class SidebarViewController: NSViewController {
    /// 用户点了某一项。只有真人点击才会触发，程序化选中不会。
    var onSelect: ((NavEntry) -> Void)?
    /// 用户在顶部切了工具包。由外层负责真正导航。
    var onToolkitSwitch: ((Toolkit) -> Void)?

    private enum Row {
        case header(String)
        case item(NavEntry)
    }

    private let switcher = NSSegmentedControl()
    private let tableView = NSTableView()
    private let scrollView = NSScrollView()
    private let footer = NSView()
    private let footerDivider = NSView()
    private let settingsButton = NSButton()
    /// 「设置」自己的选中底色。它不在表格里，拿不到 NavRowView 那套胶囊，
    /// 所以单独垫一层圆角块，视觉上和上面的选中行对上。
    private let settingsHighlight = NSView()

    private var rows: [Row] = []
    private var suppressSelection = false
    private var appliedTheme = ShellTheme.named(nil)
    private var stripAttempts = 0
    /// H5 当前所在路由。用来区分「用户点的」和「程序同步的」选中变化。
    private var currentRoute = ""
    /// 底部「设置」是否处于选中态。它不参与表格选中，只能单独记账。
    private var settingsActive = false
    /// 当前工具包。决定侧边栏列什么、底部的「设置」指向哪里。
    private(set) var activeToolkit = NavCatalog.ark

    // MARK: 生命周期

    override func loadView() {
        let root = SidebarBackgroundView()
        root.wantsLayer = true
        view = root

        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.drawsBackground = false
        scrollView.backgroundColor = .clear
        scrollView.hasVerticalScroller = true
        scrollView.autohidesScrollers = true
        scrollView.borderType = .noBorder
        // 光设 drawsBackground=false 不够：NSScrollView 的 clipView 自己还带一层
        // controlBackgroundColor（Aqua 下是 #E8E8E8），会把侧边栏底色盖成灰色。
        scrollView.contentView.drawsBackground = false
        scrollView.contentView.backgroundColor = .clear

        tableView.headerView = nil
        tableView.backgroundColor = .clear
        tableView.rowHeight = 32
        tableView.intercellSpacing = NSSize(width: 0, height: 2)
        tableView.allowsEmptySelection = true
        tableView.allowsMultipleSelection = false
        tableView.floatsGroupRows = false
        tableView.focusRingType = .none
        // .sourceList 才有 macOS 11 之后那种圆角胶囊选中态，普通样式是整行直角。
        tableView.style = .sourceList
        let column = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("nav"))
        column.resizingMask = .autoresizingMask
        tableView.addTableColumn(column)
        tableView.columnAutoresizingStyle = .lastColumnOnlyAutoresizingStyle
        tableView.dataSource = self
        tableView.delegate = self

        scrollView.documentView = tableView
        root.addSubview(scrollView)

        // 顶部工具包切换。分段控件比下拉菜单少一次点击，两个包的场景刚好合适。
        switcher.translatesAutoresizingMaskIntoConstraints = false
        switcher.segmentStyle = .rounded
        switcher.trackingMode = .selectOne
        switcher.controlSize = .small
        switcher.font = .systemFont(ofSize: 11, weight: .medium)
        switcher.segmentCount = NavCatalog.all.count
        for (index, toolkit) in NavCatalog.all.enumerated() {
            switcher.setLabel(toolkit.shortTitle, forSegment: index)
        }
        switcher.selectedSegment = NavCatalog.all.firstIndex { $0.id == activeToolkit.id } ?? 0
        switcher.target = self
        switcher.action = #selector(switchToolkit)
        root.addSubview(switcher)

        footer.translatesAutoresizingMaskIntoConstraints = false
        footer.wantsLayer = true
        root.addSubview(footer)

        footerDivider.translatesAutoresizingMaskIntoConstraints = false
        footerDivider.wantsLayer = true
        footer.addSubview(footerDivider)

        settingsButton.translatesAutoresizingMaskIntoConstraints = false
        settingsButton.title = " " + activeToolkit.settings.title
        settingsButton.image = NavCatalog.symbol(named: activeToolkit.settings.symbol)
        settingsButton.imagePosition = .imageLeading
        settingsButton.isBordered = false
        settingsButton.bezelStyle = .inline
        settingsButton.alignment = .left
        settingsButton.font = .systemFont(ofSize: 13)
        settingsButton.contentTintColor = .labelColor
        settingsButton.target = self
        settingsButton.action = #selector(clickSettings)

        settingsHighlight.translatesAutoresizingMaskIntoConstraints = false
        settingsHighlight.wantsLayer = true
        settingsHighlight.layer?.cornerRadius = 6
        settingsHighlight.layer?.backgroundColor = NSColor.controlAccentColor
            .withAlphaComponent(0.92).cgColor
        settingsHighlight.isHidden = true
        footer.addSubview(settingsHighlight)
        footer.addSubview(settingsButton)

        NSLayoutConstraint.activate([
            switcher.topAnchor.constraint(equalTo: root.topAnchor, constant: 10),
            switcher.leadingAnchor.constraint(equalTo: root.leadingAnchor, constant: 12),
            switcher.trailingAnchor.constraint(equalTo: root.trailingAnchor, constant: -12),
            switcher.heightAnchor.constraint(equalToConstant: 22),

            scrollView.topAnchor.constraint(equalTo: switcher.bottomAnchor, constant: 10),
            scrollView.leadingAnchor.constraint(equalTo: root.leadingAnchor),
            scrollView.trailingAnchor.constraint(equalTo: root.trailingAnchor),
            scrollView.bottomAnchor.constraint(equalTo: footer.topAnchor),

            footer.leadingAnchor.constraint(equalTo: root.leadingAnchor),
            footer.trailingAnchor.constraint(equalTo: root.trailingAnchor),
            footer.bottomAnchor.constraint(equalTo: root.bottomAnchor),
            footer.heightAnchor.constraint(equalToConstant: 46),

            footerDivider.topAnchor.constraint(equalTo: footer.topAnchor),
            footerDivider.leadingAnchor.constraint(equalTo: footer.leadingAnchor, constant: 14),
            footerDivider.trailingAnchor.constraint(equalTo: footer.trailingAnchor, constant: -14),
            footerDivider.heightAnchor.constraint(equalToConstant: 1),

            settingsButton.leadingAnchor.constraint(equalTo: footer.leadingAnchor, constant: 14),
            settingsButton.trailingAnchor.constraint(equalTo: footer.trailingAnchor, constant: -10),
            settingsButton.centerYAnchor.constraint(equalTo: footer.centerYAnchor, constant: 2),
            settingsButton.heightAnchor.constraint(equalToConstant: 28),

            settingsHighlight.leadingAnchor.constraint(equalTo: footer.leadingAnchor, constant: 8),
            settingsHighlight.trailingAnchor.constraint(equalTo: footer.trailingAnchor, constant: -8),
            settingsHighlight.centerYAnchor.constraint(equalTo: settingsButton.centerYAnchor),
            settingsHighlight.heightAnchor.constraint(equalToConstant: 28),
        ])

        rebuildRows()
        applyTheme(appliedTheme)
    }

    // MARK: 布局

    override func viewDidLayout() {
        super.viewDidLayout()
        // 只在最初几次布局时清扫：NSTableView 会在首次布局里才把材质层建出来，
        // 之后每帧都遍历一次纯属浪费。
        if stripAttempts < 4 {
            stripAttempts += 1
            stripMaterial(from: view)
        }
    }

    /// `.sourceList` 样式会往视图树里塞一层 NSVisualEffectView 当侧边栏材质，
    /// 它盖在自定义底色之上，导致液体玻璃（#FAFCFF）被冲成系统灰（#E4E4E4）。
    /// 选中胶囊是 NSTableRowView 画的，不依赖这层材质，摘掉不影响观感。
    private func stripMaterial(from node: NSView) {
        for sub in node.subviews {
            if sub is NSVisualEffectView {
                sub.removeFromSuperview()
            } else {
                stripMaterial(from: sub)
            }
        }
    }

    // MARK: 数据

    private func rebuildRows() {
        rows = activeToolkit.primary.map { Row.item($0) }
        rows.append(.header("更多"))
        rows.append(contentsOf: activeToolkit.secondary.map { Row.item($0) })
        tableView.reloadData()
        refreshSettingsButton()
    }

    private func refreshSettingsButton() {
        settingsButton.image = NavCatalog.symbol(named: activeToolkit.settings.symbol)
        applySettingsTitle()
    }

    /// 「设置」不在表格里，选中态得自己维护：一层圆角底 + 文字反白。
    private func applySettingsTitle() {
        let normal = appliedTheme.dark
            ? NSColor(white: 0.93, alpha: 1)
            : NSColor(white: 0.10, alpha: 1)
        let color: NSColor = settingsActive ? .white : normal
        settingsButton.attributedTitle = NSAttributedString(
            string: " " + activeToolkit.settings.title,
            attributes: [
                .foregroundColor: color,
                .font: NSFont.systemFont(ofSize: 13),
            ]
        )
        settingsButton.contentTintColor = color
    }

    /// 由外层在「进入 / 离开原生设置页」时调用。
    func setSettingsActive(_ active: Bool) {
        guard settingsActive != active else { return }
        settingsActive = active
        settingsHighlight.isHidden = !active
        applySettingsTitle()
        if active {
            suppressSelection = true
            tableView.deselectAll(nil)
            suppressSelection = false
        }
    }

    /// 切换侧边栏展示的工具包（只改列表，不负责导航）。
    /// 外层的 `onToolkitSwitch` 才决定要不要跳页 —— H5 内部跳到终末地时也会走这里，
    /// 那种情况下不能反过来再触发一次导航。
    func applyToolkit(_ toolkit: Toolkit) {
        guard toolkit.id != activeToolkit.id else { return }
        activeToolkit = toolkit
        switcher.selectedSegment = NavCatalog.all.firstIndex { $0.id == toolkit.id } ?? 0
        rebuildRows()
        // 换了一整套导航项，原来那行的选中态已经没意义了
        suppressSelection = true
        tableView.deselectAll(nil)
        suppressSelection = false
        Diag.log("sidebar toolkit -> \(toolkit.id)")
    }

    /// 按当前 H5 路由把选中项对齐。`route` 是形如 `pages/list/list?x=1` 的裸路径。
    ///
    /// 用前缀匹配（和 Windows 版一致）：详情页 `pages/list/list?id=3` 仍应让
    /// 「通行证列表」保持选中。匹配不到就不动选中态，避免用户翻详情页时侧边栏乱跳。
    func syncSelection(to route: String) {
        // 先记账再做视觉：即使这一页不在侧边栏里（详情页、图片预览），
        // 也要记住当前路由，否则用户点回原来的项时不会被识别成一次点击。
        currentRoute = route

        // 页面内部跳到另一个工具包（比如 H5 里点进了终末地），侧边栏跟着换一套。
        applyToolkit(NavCatalog.toolkit(for: route))

        // 原生设置页不在 rows 里，点亮底部那一项就够了，表格全部取消选中。
        if NavCatalog.isNative(route) {
            setSettingsActive(true)
            return
        }
        setSettingsActive(false)

        let index = rows.firstIndex { row in
            guard case .item(let entry) = row else { return false }
            return route == entry.route || route.hasPrefix(entry.route)
        }
        guard let index else { return }

        if tableView.selectedRow == index { return }
        suppressSelection = true
        tableView.selectRowIndexes(IndexSet(integer: index), byExtendingSelection: false)
        suppressSelection = false
        Diag.log("syncSelection \(route) -> row \(index), now selectedRow=\(tableView.selectedRow)")
    }

    // MARK: 动作

    @objc private func switchToolkit() {
        let index = switcher.selectedSegment
        guard index >= 0, index < NavCatalog.all.count else { return }
        let toolkit = NavCatalog.all[index]
        guard toolkit.id != activeToolkit.id else { return }
        applyToolkit(toolkit)
        onToolkitSwitch?(toolkit)
    }

    @objc private func clickSettings() {
        onSelect?(activeToolkit.settings)
    }

    /// 自动化验证用：模拟一次真人点击（走 `selectionDidChange` 的完整回调链）。
    ///
    /// 不直接用 `selectRowIndexes` 在测试里点，是因为那样得绕开 `suppressSelection`，
    /// 等于绕过了真正要验证的那段逻辑。这里先把「当前路由」清空，
    /// 让选中变化被识别成用户操作。
    func simulateClick(route: String) {
        guard let index = rows.firstIndex(where: { row in
            guard case .item(let entry) = row else { return false }
            return entry.route == route
        }) else {
            Diag.log("simulateClick: 未找到 \(route)")
            return
        }
        currentRoute = "\u{0}placeholder"
        tableView.selectRowIndexes(IndexSet(integer: index), byExtendingSelection: false)
    }

    /// 自动化验证用：走一次真实的「点底部设置」链路（settingsButton 的 action）。
    func simulateSettingsClick() {
        clickSettings()
    }

    /// 自动化验证用：模拟一次分段控件点击（走真实的 action 链路）。
    func simulateToolkitSwitch(id: String) {
        guard let index = NavCatalog.all.firstIndex(where: { $0.id == id }) else {
            Diag.log("simulateToolkitSwitch: 未知工具包 \(id)")
            return
        }
        switcher.selectedSegment = index
        switchToolkit()
    }

    // MARK: 主题

    func applyTheme(_ theme: ShellTheme) {
        appliedTheme = theme
        // 用 appearance 而不是逐个改颜色：系统控件的次要文字、选中高亮、
        // 滚动条都会自动跟着深浅切换，比手工调色更接近原生观感。
        view.appearance = NSAppearance(named: theme.dark ? .darkAqua : .aqua)
        view.layer?.backgroundColor = theme.pane.cgColor
        footer.layer?.backgroundColor = theme.pane.cgColor
        footerDivider.layer?.backgroundColor = theme.border.cgColor
        applySettingsTitle()

        // 已建好的 cell 不会因为 appearance 变化就重算颜色（NSTextField 早就把
        // 动态色解析成实色了），得把可见的那些挨个刷一遍。
        tableView.reloadData()
        for row in 0..<tableView.numberOfRows {
            guard let cell = tableView.view(atColumn: 0, row: row, makeIfNecessary: false) else { continue }
            if let item = cell as? NavRowCell {
                item.refreshColors(dark: theme.dark)
            } else if let header = cell as? NavHeaderCell {
                header.setDark(theme.dark)
            }
        }
    }
}

// MARK: - NSTableViewDataSource / Delegate

extension SidebarViewController: NSTableViewDataSource, NSTableViewDelegate {
    func numberOfRows(in tableView: NSTableView) -> Int { rows.count }

    func tableView(_ tableView: NSTableView, heightOfRow row: Int) -> CGFloat {
        if case .header = rows[row] { return 28 }
        return 32
    }

    func tableView(_ tableView: NSTableView, shouldSelectRow row: Int) -> Bool {
        if case .header = rows[row] { return false }
        return true
    }

    func tableView(_ tableView: NSTableView, rowViewForRow row: Int) -> NSTableRowView? {
        NavRowView()
    }

    /// 用「选中变化」而不是 `tableView.action` 来判断点击。
    ///
    /// action 在程序化选中、键盘导航、甚至首次布局时都会发，拿它当点击信号会
    /// 误触发导航（实测：启动后被自动点掉一次，直接跳到通行证查询页）。
    /// 这里靠「目标路由 == 当前路由」二次确认，async 派发的通知也挡得住。
    func tableViewSelectionDidChange(_ notification: Notification) {
        guard !suppressSelection else { return }
        let row = tableView.selectedRow
        guard row >= 0, row < rows.count, case .item(let entry) = rows[row] else { return }
        guard entry.route != currentRoute else { return }
        onSelect?(entry)
    }

    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        switch rows[row] {
        case .header(let title):
            let id = NSUserInterfaceItemIdentifier("nav-header")
            let cell = tableView.makeView(withIdentifier: id, owner: self) as? NavHeaderCell
                ?? NavHeaderCell(identifier: id)
            cell.textField?.stringValue = title
            cell.setDark(appliedTheme.dark)
            return cell

        case .item(let entry):
            let id = NSUserInterfaceItemIdentifier("nav-item")
            let cell = tableView.makeView(withIdentifier: id, owner: self) as? NavRowCell
                ?? NavRowCell(identifier: id, indented: true)
            cell.configure(entry, dark: appliedTheme.dark)
            return cell
        }
    }
}

/// 侧边栏底色。用图层直接填色，避免 NSSplitViewItem 默认的 vibrancy 材质
/// 把应用内主题色冲淡（Ark / 液态玻璃的色值都是精确指定的）。
private final class SidebarBackgroundView: NSView {
    override var isFlipped: Bool { true }
}
