import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    private let displayName = "方舟通行证谷子查询工具"

    /// 上次用的工具包。启动时直接落回那个包的首页，而不是每次回到方舟。
    private var activeToolkit = NavCatalog.toolkit(id: UserDefaults.standard.string(forKey: "AkPassportToolkit"))

    /// 起始页。可用 AKPASSPORT_ROUTE=pages/list/list 或 --route=... 覆盖，便于调试与回归。
    private var startRoute: String {
        if let route = ProcessInfo.processInfo.environment["AKPASSPORT_ROUTE"], !route.isEmpty {
            return route
        }
        for argument in CommandLine.arguments where argument.hasPrefix("--route=") {
            return String(argument.dropFirst("--route=".count))
        }
        return activeToolkit.home
    }

    private var window: NSWindow?
    private var shell: WebShell?
    private var server: LocalServer?
    private var statusItem: NSStatusItem?
    private var backdrop: NSView?
    private var splitVC: NSSplitViewController?
    private var sidebar: SidebarViewController?
    private var settingsVC: SettingsViewController?
    private var settingsView: NSView?
    private var settingsStore: SettingsStore?
    private let card = NSView()
    private var cardMaxWidth: NSLayoutConstraint?
    private var currentRoute = ""
    private var currentTheme = ShellTheme.named(nil)
    /// 当前由原生视图接管的路由（目前只有设置页）。非空时 H5 的路由上报一律忽略，
    /// 否则停在设置页时，后台那个旧页面会一直把侧边栏拉回它自己。
    private var nativeRoute: String?

    /// 内容区内边距，取自 MainWindow.xaml 的 ContentArea.Padding="14,10,14,14"。
    private let contentInset = NSEdgeInsets(top: 10, left: 14, bottom: 14, right: 14)

    /// 首页是真正的宽屏 Dashboard；工具页沿用移动端布局。
    /// 判定与 Windows 版 IsWideRoute 一致。
    private func isWideRoute(_ route: String) -> Bool {
        route.hasPrefix("pages/desktop_home/desktop_home")
            || route.hasPrefix("package_ArknightEnds/index/index")
    }

    private var devMode: Bool {
        ProcessInfo.processInfo.environment["AKPASSPORT_DEV"] == "1"
    }

    // MARK: - 生命周期

    func applicationDidFinishLaunching(_ notification: Notification) {
        ShellPaths.ensureCreated()
        Diag.log("launch pid=\(ProcessInfo.processInfo.processIdentifier) dev=\(devMode)")
        Diag.log("webRoot=\(ShellPaths.webRoot.path)")
        buildMenuBar()
        startServer()
    }

    func applicationWillTerminate(_ notification: Notification) {
        Diag.log("terminate")
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        // macOS 习惯：关窗不退出，点 Dock 图标回来
        false
    }

    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        if !flag { window?.makeKeyAndOrderFront(nil) }
        return true
    }

    // MARK: - 启动

    private func startServer() {
        let server = LocalServer(webRoot: ShellPaths.webRoot)
        server.onReady = { [weak self] port in
            DispatchQueue.main.async { self?.presentShell(port: port) }
        }
        server.onFailure = { [weak self] message in
            DispatchQueue.main.async { self?.presentFatal(message) }
        }
        server.start()
        self.server = server
    }

    private func presentShell(port: UInt16) {
        if let window, let shell {
            window.makeKeyAndOrderFront(nil)
            NSApp.activate(ignoringOtherApps: true)
            _ = shell
            return
        }

        let shell = WebShell(port: port, devMode: devMode)
        shell.onThemeChange = { [weak self] name in self?.applyTheme(named: name) }
        shell.onExternalLink = { url in NSWorkspace.shared.open(url) }
        shell.onRouteChange = { [weak self] route in self?.handleRouteChange(route) }
        shell.onLoad = { [weak self] in
            guard let self, self.nativeRoute != nil else { return }
            self.settingsVC?.refresh()
        }
        self.shell = shell

        // 内容区：canvas 底色 + 圆角卡片，卡片里才是 WebView。
        // 对应 MainWindow.xaml 的 ContentArea > ContentCard > ContentHost 三层。
        let backdrop = NSView()
        backdrop.wantsLayer = true
        backdrop.layer?.backgroundColor = ShellTheme.named(nil).canvas.cgColor
        self.backdrop = backdrop

        card.wantsLayer = true
        card.layer?.cornerRadius = 12
        card.layer?.borderWidth = 1
        card.layer?.masksToBounds = true
        card.translatesAutoresizingMaskIntoConstraints = false
        backdrop.addSubview(card)

        let webView = shell.webView
        webView.translatesAutoresizingMaskIntoConstraints = false
        card.addSubview(webView)

        // 原生设置页和 WebView 同级叠放，切换靠 isHidden。不换 contentViewController 是
        // 因为那会把 WebView 从视图树里摘掉，回来得整个重载 —— 丢滚动位置、丢页面状态。
        let store = SettingsStore(shell: shell)
        let settingsVC = SettingsViewController(store: store)
        settingsVC.onThemePicked = { [weak self] name in self?.applyTheme(named: name) }
        settingsVC.onNavigate = { [weak self] route in self?.navigate(to: route) }
        settingsVC.onOpenURL = { url in NSWorkspace.shared.open(url) }
        settingsVC.onTrayToggle = { [weak self] enabled in self?.setTrayEnabled(enabled) }
        settingsVC.trayEnabled = UserDefaults.standard.object(forKey: "AkPassportTrayEnabled") as? Bool ?? true
        let settingsView = settingsVC.view
        settingsView.translatesAutoresizingMaskIntoConstraints = false
        settingsView.isHidden = true
        card.addSubview(settingsView)
        self.settingsStore = store
        self.settingsVC = settingsVC
        self.settingsView = settingsView

        // 卡片宽度 = min(可用宽度, 上限)。上限随「宽屏页 / 工具页」切换：
        // 工具页超过 1060 后 rpx 会把移动端布局拉得过宽，首页则允许铺满。
        let fill = card.widthAnchor.constraint(
            equalTo: backdrop.widthAnchor,
            constant: -(contentInset.left + contentInset.right)
        )
        fill.priority = .defaultHigh
        let maxWidth = card.widthAnchor.constraint(equalToConstant: 1060)
        cardMaxWidth = maxWidth

        NSLayoutConstraint.activate([
            card.topAnchor.constraint(equalTo: backdrop.topAnchor, constant: contentInset.top),
            card.bottomAnchor.constraint(equalTo: backdrop.bottomAnchor, constant: -contentInset.bottom),
            card.centerXAnchor.constraint(equalTo: backdrop.centerXAnchor),
            // 低于 750 时 uni-app 会把根字号等比放大（375 设计宽 → 1.7 倍），
            // 宁可让卡片顶到边也不能再窄；窗口最小宽度已经保证不会走到这里。
            card.widthAnchor.constraint(greaterThanOrEqualToConstant: 560),
            maxWidth,
            fill,

            webView.topAnchor.constraint(equalTo: card.topAnchor),
            webView.bottomAnchor.constraint(equalTo: card.bottomAnchor),
            webView.leadingAnchor.constraint(equalTo: card.leadingAnchor),
            webView.trailingAnchor.constraint(equalTo: card.trailingAnchor),

            settingsView.topAnchor.constraint(equalTo: card.topAnchor),
            settingsView.bottomAnchor.constraint(equalTo: card.bottomAnchor),
            settingsView.leadingAnchor.constraint(equalTo: card.leadingAnchor),
            settingsView.trailingAnchor.constraint(equalTo: card.trailingAnchor),
        ])

        let contentVC = NSViewController()
        contentVC.view = backdrop

        // 侧边栏
        let sidebar = SidebarViewController()
        sidebar.onSelect = { [weak self] entry in self?.navigate(to: entry.route) }
        sidebar.onToolkitSwitch = { [weak self] toolkit in self?.activateToolkit(toolkit) }
        sidebar.applyToolkit(activeToolkit)
        self.sidebar = sidebar

        let sidebarItem = NSSplitViewItem(viewController: sidebar)
        sidebarItem.canCollapse = true
        // 宽度取在 minimumThickness 上：NSSplitViewController 的初始布局就是
        // 「最小宽度」，setPosition 会被内部约束打回，所以这里直接让最小值等于默认值。
        sidebarItem.minimumThickness = sidebarWidth
        sidebarItem.maximumThickness = sidebarMaxWidth
        // 不用 sidebarWithViewController：那会套一层系统 vibrancy 材质，
        // 把 Ark / 液态玻璃这些精确指定的主题色冲淡。

        let contentItem = NSSplitViewItem(viewController: contentVC)
        contentItem.canCollapse = false

        let splitVC = NSSplitViewController()
        splitVC.addSplitViewItem(sidebarItem)
        splitVC.addSplitViewItem(contentItem)
        splitVC.splitView.dividerStyle = .thin
        self.splitVC = splitVC

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 1180, height: 840),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )
        window.title = displayName
        // 侧边栏 212 + 内容区左右各 14 + 卡片下限 750（字号不放大），留点余量。
        window.minSize = NSSize(width: 1000, height: 620)
        window.isReleasedWhenClosed = false
        window.contentViewController = splitVC
        // 首次启动居中，之后恢复上次位置。用自己的标记位判断，
        // 不能依赖 NSWindow 的 frame autosave 键（setFrameAutosaveName 会立刻写入一条记录）。
        if !UserDefaults.standard.bool(forKey: "AkPassportWindowInitialized") {
            // 显式算居中：NSWindow.center() 在窗口还没上屏时结果不稳定
            if let visible = NSScreen.main?.visibleFrame {
                let size = window.frame.size
                window.setFrame(
                    NSRect(x: visible.midX - size.width / 2,
                           y: visible.midY - size.height / 2,
                           width: size.width,
                           height: size.height),
                    display: false
                )
            }
            UserDefaults.standard.set(true, forKey: "AkPassportWindowInitialized")
        }
        window.setFrameAutosaveName("AkPassportMainWindow")
        self.window = window

        buildStatusItem()
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)

        // `--route=native/settings` 这种调试入口：H5 先落到首页垫着，再把原生页盖上去。
        // 页面加载完之前读不到 localStorage，设置页的 refresh 得等 onLoad。
        let webRoute = NavCatalog.isNative(startRoute) ? activeToolkit.home : startRoute
        shell.load(route: webRoute)
        switchLayout(wide: isWideRoute(webRoute), animated: false)
        if NavCatalog.isNative(startRoute) {
            nativeRoute = startRoute
            currentRoute = startRoute
            sidebar.syncSelection(to: startRoute)
            showNativeSettings()
        }

        scheduleNavigationProbeIfNeeded()
    }

    /// 自动化回归：`AKPASSPORT_NAV_TEST=pages/list/list` 时，等首屏稳定后
    /// 真点一下侧边栏那一项，用来验证「点击 → 路由 → 选中同步」整条链路。
    private func scheduleNavigationProbeIfNeeded() {
        let env = ProcessInfo.processInfo.environment
        if let route = env["AKPASSPORT_NAV_TEST"], !route.isEmpty {
            DispatchQueue.main.asyncAfter(deadline: .now() + 2.5) { [weak self] in
                Diag.log("probe: simulate click \(route)")
                self?.sidebar?.simulateClick(route: route)
            }
        }
        if env["AKPASSPORT_SIDEBAR_TEST"] == "1" {
            DispatchQueue.main.asyncAfter(deadline: .now() + 3.0) { [weak self] in
                Diag.log("probe: toggle sidebar")
                self?.toggleSidebar()
            }
        }
        if let id = env["AKPASSPORT_TOOLKIT_TEST"], !id.isEmpty {
            DispatchQueue.main.asyncAfter(deadline: .now() + 2.2) { [weak self] in
                Diag.log("probe: switch toolkit \(id)")
                self?.sidebar?.simulateToolkitSwitch(id: id)
            }
        }
        // 验证「点侧边栏底部设置 → 原生页」整条链路（而不是靠 --route 直达）
        if env["AKPASSPORT_SETTINGS_TEST"] == "1" {
            DispatchQueue.main.asyncAfter(deadline: .now() + 2.6) { [weak self] in
                Diag.log("probe: click settings")
                self?.sidebar?.simulateSettingsClick()
            }
            // 再点回首页，验证原生页退场、WebView 重新可见
            if env["AKPASSPORT_SETTINGS_BACK"] == "1" {
                DispatchQueue.main.asyncAfter(deadline: .now() + 5.0) { [weak self] in
                    Diag.log("probe: click home to leave settings")
                    self?.sidebar?.simulateClick(route: "pages/desktop_home/desktop_home")
                }
            }
        }
        // 验证「点主题瓦片 → 写 localStorage → 外壳整体换色」与开关的写入格式
        if let id = env["AKPASSPORT_SETTINGS_THEME_TEST"], !id.isEmpty {
            DispatchQueue.main.asyncAfter(deadline: .now() + 3.0) { [weak self] in
                Diag.log("probe: pick theme \(id)")
                self?.settingsVC?.pickThemeForTest(id)
            }
            DispatchQueue.main.asyncAfter(deadline: .now() + 3.4) { [weak self] in
                Diag.log("probe: toggle 显示市场价 / 数据更新提示")
                self?.settingsVC?.setToggleForTest("显示市场价", on: true)
                self?.settingsVC?.setToggleForTest("数据更新提示", on: false)
            }
            DispatchQueue.main.asyncAfter(deadline: .now() + 4.6) { [weak self] in
                self?.settingsStore?.probe(keys: [
                    "themeMode", "showMarketPrice", "updateNotifyEnabled",
                    "autoUpdateSetting", "avatarPrefSettings",
                ])
            }
        }
    }

    // MARK: - 侧边栏 ↔ 路由

    private func navigate(to route: String) {
        Diag.log("nav click: \(route)")

        if NavCatalog.isNative(route) {
            nativeRoute = route
            currentRoute = route
            sidebar?.syncSelection(to: route)
            switchLayout(wide: false, animated: true)
            showNativeSettings()
            return
        }

        if nativeRoute != nil {
            nativeRoute = nil
            hideNativeSettings()
            // 这里得手动灭掉「设置」的高亮：如果目标路由恰好和 H5 当前页相同，
            // reLaunch 不会产生 hashchange，也就没有 handleRouteChange 来对齐侧边栏。
            sidebar?.setSettingsActive(false)
        }

        switchLayout(wide: isWideRoute(route), animated: true)
        shell?.navigate(to: route) { [weak self] ok in
            // reLaunch 与 hash 都失败（多半是 uni 还没接上），兜底整页加载
            guard !ok else { return }
            Diag.log("nav fallback: full load \(route)")
            self?.shell?.load(route: route)
        }
    }

    // MARK: - 原生设置页

    private func showNativeSettings() {
        settingsView?.isHidden = false
        shell?.webView.isHidden = true
        settingsVC?.applyTheme(currentTheme)
        settingsVC?.refresh()
        Diag.log("native settings shown")
    }

    private func hideNativeSettings() {
        settingsView?.isHidden = true
        shell?.webView.isHidden = false
        Diag.log("native settings hidden")
    }

    private func setTrayEnabled(_ enabled: Bool) {
        UserDefaults.standard.set(enabled, forKey: "AkPassportTrayEnabled")
        if enabled {
            buildStatusItem()
        } else if let statusItem {
            NSStatusBar.system.removeStatusItem(statusItem)
            self.statusItem = nil
        }
        Diag.log("tray -> \(enabled)")
    }

    /// 用户从侧边栏顶部切工具包：记住选择，然后跳到那个包的首页。
    /// 反方向（H5 内部跳到另一个包）走的是 `syncSelection` → `applyToolkit`，不经过这里，
    /// 否则会变成「页面已经跳了，外壳再跳一次」的死循环。
    private func activateToolkit(_ toolkit: Toolkit) {
        if toolkit.id != activeToolkit.id {
            activeToolkit = toolkit
            UserDefaults.standard.set(toolkit.id, forKey: "AkPassportToolkit")
            Diag.log("toolkit -> \(toolkit.id)")
        }
        navigate(to: toolkit.home)
    }

    private func handleRouteChange(_ route: String) {
        // 原生设置页开着时，H5 还停在进去之前那一页，它每次上报都会把侧边栏拽回去
        if nativeRoute != nil { return }

        // H5 里也有入口指向 Vue 设置页（列表页的「去设置」）。桌面端一律接管成
        // 原生设置页，否则用户会在同一个 App 里看到两套风格完全不同的设置界面。
        if route == "packageA/Setting/Setting" {
            Diag.log("route -> 拦截 Vue 设置页，改走原生")
            navigate(to: NavCatalog.nativeSettings)
            return
        }

        guard route != currentRoute else { return }
        currentRoute = route
        Diag.log("route -> \(route)")
        // 页面自己跳进另一个工具包时（终末地 ⇄ 方舟），把外壳的选择也带过去，
        // 否则下次启动会回到旧的那个包。
        let owner = NavCatalog.toolkit(for: route)
        if owner.id != activeToolkit.id {
            activeToolkit = owner
            UserDefaults.standard.set(owner.id, forKey: "AkPassportToolkit")
            Diag.log("toolkit <- route \(owner.id)")
        }
        sidebar?.syncSelection(to: route)
        switchLayout(wide: isWideRoute(route), animated: true)
    }

    /// 宽屏页放宽卡片上限，工具页收紧。只改常量，不碰布局结构。
    private func switchLayout(wide: Bool, animated: Bool) {
        let target: CGFloat = wide ? 1180 : 1060
        guard cardMaxWidth?.constant != target else { return }
        cardMaxWidth?.constant = target
        guard animated else { return }
        NSAnimationContext.runAnimationGroup { context in
            context.duration = 0.18
            context.allowsImplicitAnimation = true
            self.window?.contentView?.layoutSubtreeIfNeeded()
        }
    }

    private func applyTheme(named name: String) {
        // 调试用：AKPASSPORT_THEME=ark 可强制外壳主题，方便在不点 H5 设置的情况下
        // 截图核对深色主题下的侧边栏可读性。
        let forced = ProcessInfo.processInfo.environment["AKPASSPORT_THEME"]
        let theme = ShellTheme.named(forced?.isEmpty == false ? forced : name)
        currentTheme = theme
        window?.appearance = NSAppearance(named: theme.dark ? .darkAqua : .aqua)
        backdrop?.layer?.backgroundColor = theme.canvas.cgColor
        card.layer?.backgroundColor = theme.panel.cgColor
        card.layer?.borderColor = theme.border.cgColor
        sidebar?.applyTheme(theme)
        settingsVC?.applyTheme(theme)
        shell?.webView.underPageBackgroundColor = theme.panel
        window?.backgroundColor = theme.canvas
    }

    // MARK: - 菜单栏

    private func buildMenuBar() {
        let mainMenu = NSMenu()

        let appItem = NSMenuItem()
        mainMenu.addItem(appItem)
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "关于 \(displayName)", action: #selector(showAbout), keyEquivalent: "")
        appMenu.addItem(.separator())
        appMenu.addItem(withTitle: "设置…", action: #selector(openSettings), keyEquivalent: ",")
        appMenu.addItem(.separator())
        appMenu.addItem(withTitle: "打开日志", action: #selector(openLog), keyEquivalent: "")
        appMenu.addItem(.separator())
        appMenu.addItem(withTitle: "隐藏 \(displayName)", action: Selector(("hide:")), keyEquivalent: "h")
        appMenu.addItem(withTitle: "退出 \(displayName)", action: Selector(("terminate:")), keyEquivalent: "q")
        appItem.submenu = appMenu

        // 没有编辑菜单，WKWebView 里的 ⌘C / ⌘V 全部失效
        let editItem = NSMenuItem()
        mainMenu.addItem(editItem)
        let editMenu = NSMenu(title: "编辑")
        editMenu.addItem(withTitle: "撤销", action: Selector(("undo:")), keyEquivalent: "z")
        editMenu.addItem(withTitle: "重做", action: Selector(("redo:")), keyEquivalent: "Z")
        editMenu.addItem(.separator())
        editMenu.addItem(withTitle: "剪切", action: Selector(("cut:")), keyEquivalent: "x")
        editMenu.addItem(withTitle: "拷贝", action: Selector(("copy:")), keyEquivalent: "c")
        editMenu.addItem(withTitle: "粘贴", action: Selector(("paste:")), keyEquivalent: "v")
        editMenu.addItem(withTitle: "全选", action: Selector(("selectAll:")), keyEquivalent: "a")
        editItem.submenu = editMenu

        let viewItem = NSMenuItem()
        mainMenu.addItem(viewItem)
        let viewMenu = NSMenu(title: "显示")
        viewMenu.addItem(withTitle: "重新加载", action: #selector(reloadWeb), keyEquivalent: "r")
        viewMenu.addItem(withTitle: "返回", action: #selector(goBack), keyEquivalent: "[")
        viewMenu.addItem(withTitle: "前进", action: #selector(goForward), keyEquivalent: "]")
        viewMenu.addItem(.separator())
        let toggle = viewMenu.addItem(withTitle: "显示/隐藏侧边栏",
                                      action: #selector(toggleSidebar),
                                      keyEquivalent: "s")
        // ⌘⌥S 是 macOS 上折叠侧边栏的通行约定（Finder、邮件、备忘录都用它）
        toggle.keyEquivalentModifierMask = [.command, .option]

        viewMenu.addItem(.separator())
        viewMenu.addItem(withTitle: "方舟工具箱", action: #selector(switchToArk), keyEquivalent: "1")
        viewMenu.addItem(withTitle: "终末地工具箱", action: #selector(switchToEnds), keyEquivalent: "2")
        viewItem.submenu = viewMenu

        NSApp.mainMenu = mainMenu
    }

    private func buildStatusItem() {
        guard statusItem == nil else { return }
        // 设置页里可以关掉，关掉之后菜单栏不该再冒出来
        guard UserDefaults.standard.object(forKey: "AkPassportTrayEnabled") as? Bool ?? true else { return }
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let button = item.button {
            button.image = NSImage(systemSymbolName: "shield.lefthalf.filled",
                                   accessibilityDescription: displayName)
            button.image?.isTemplate = true
        }
        let menu = NSMenu()
        menu.addItem(withTitle: "显示主窗口", action: #selector(showWindowFromMenu), keyEquivalent: "")
        menu.addItem(withTitle: "重新加载", action: #selector(reloadWeb), keyEquivalent: "")
        menu.addItem(.separator())
        menu.addItem(withTitle: "退出", action: Selector(("terminate:")), keyEquivalent: "")
        item.menu = menu
        statusItem = item
    }

    // MARK: - 动作

    @objc private func showWindowFromMenu() {
        window?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    @objc private func reloadWeb() {
        shell?.reload()
    }

    @objc private func toggleSidebar() {
        // 不用 NSSplitViewController.toggleSidebar(_:)：那个动作依赖工具栏 / 响应链，
        // 在无 toolbar 的窗口里点了没反应。直接翻 isCollapsed 更直接。
        guard let item = splitVC?.splitViewItems.first else { return }
        item.animator().isCollapsed.toggle()
    }

    @objc private func switchToArk() {
        activateToolkit(NavCatalog.ark)
        sidebar?.applyToolkit(NavCatalog.ark)
    }

    @objc private func switchToEnds() {
        activateToolkit(NavCatalog.ends)
        sidebar?.applyToolkit(NavCatalog.ends)
    }

    @objc private func goBack() {
        shell?.goBack()
    }

    @objc private func goForward() {
        shell?.goForward()
    }

    @objc private func openSettings() {
        navigate(to: NavCatalog.nativeSettings)
    }

    @objc private func openLog() {
        ShellPaths.ensureCreated()
        if !FileManager.default.fileExists(atPath: ShellPaths.logFile.path) {
            FileManager.default.createFile(atPath: ShellPaths.logFile.path, contents: Data())
        }
        NSWorkspace.shared.open(ShellPaths.logFile)
    }

    @objc private func showAbout() {
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0.1.0"
        let alert = NSAlert()
        alert.messageText = displayName
        alert.informativeText = """
        macOS 预览版 \(version)

        外壳：Swift + AppKit + WKWebView
        业务：uni-app H5 产物（未改动一行前端代码）

        当前已完成：原生侧边栏与路由双向同步、方舟 / 终末地工具包切换、
        原生设置页（主题 / 显示与搜索 / 干员头像个性化 / 数据 / 窗口）、
        内容卡片与宽窄布局、菜单与托盘、跨域代理、图床防盗链、
        主题跟随、字号修正。

        尚未实现：全局快捷键、开机自启、自动更新。
        """
        alert.addButton(withTitle: "好")
        alert.runModal()
    }

    private func presentFatal(_ message: String) {
        Diag.log("fatal: \(message)")
        let alert = NSAlert()
        alert.messageText = "无法启动"
        alert.informativeText = message
        alert.alertStyle = .critical
        alert.addButton(withTitle: "退出")
        alert.addButton(withTitle: "打开日志")
        if alert.runModal() == .alertSecondButtonReturn {
            openLog()
        }
        NSApp.terminate(nil)
    }
}
