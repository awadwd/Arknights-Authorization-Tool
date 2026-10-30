import AppKit

/// 「干员头像个性化」卡片。
///
/// 对应 Windows 版 `SettingsView.xaml` 里 `CardAvatar` 那一段。写回的是
/// `localStorage.avatarPrefSettings`，格式与手机端 `utils/avatarPref.js` 完全一致，
/// 前端页面读到后会自己 invalidate 头像缓存 —— 所以这里只管把 JSON 写对。
final class AvatarSettingsCard: NSView {
    /// 整份设置变了（任何一个字段）。
    var onChange: ((AvatarPref) -> Void)?

    private var pref = AvatarPref()
    private var theme = ShellTheme.named(nil)
    private var suspend = false

    private let levelControl = NSSegmentedControl(labels: ["优先精一", "优先精二"],
                                                  trackingMode: .selectOne,
                                                  target: nil,
                                                  action: nil)
    private let rulesStack = NSStackView()
    private let rulesEmpty = NSTextField(labelWithString: "暂无规则，全部干员使用上方默认优先级")
    private let whiteFlow = FlowView()
    private let blackFlow = FlowView()
    private let whiteEmpty = NSTextField(labelWithString: "暂无干员")
    private let blackEmpty = NSTextField(labelWithString: "暂无干员")

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        wantsLayer = true
        layer?.cornerRadius = 10
        layer?.borderWidth = 1
        build()
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) { fatalError() }

    // MARK: - 构建

    private func build() {
        let inner = NSStackView()
        inner.orientation = .vertical
        inner.alignment = .leading
        inner.spacing = 10
        inner.translatesAutoresizingMaskIntoConstraints = false
        addSubview(inner)

        let title = NSTextField(labelWithString: "干员头像个性化")
        title.font = .systemFont(ofSize: 14, weight: .semibold)
        let hint = NSTextField(wrappingLabelWithString:
            "判定顺序：精一名单 > 精二名单 > 条件规则（自上而下首条命中）> 默认优先级。没有精二头像的干员始终回落精一。")
        hint.font = .systemFont(ofSize: 11.5)
        hint.textColor = .secondaryLabelColor
        hint.preferredMaxLayoutWidth = 840

        inner.addArrangedSubview(title)
        inner.addArrangedSubview(hint)
        inner.setCustomSpacing(6, after: hint)

        // 默认档位
        levelControl.target = self
        levelControl.action = #selector(levelChanged)
        levelControl.controlSize = .small
        levelControl.segmentStyle = .rounded
        inner.addArrangedSubview(row(title: "默认头像优先级",
                                     detail: "没有被下面任何规则或名单命中时使用",
                                     accessory: levelControl))

        inner.addArrangedSubview(separator())

        // 条件规则
        let addRule = ActionButton(title: "＋ 新增规则") { [weak self] in self?.editRule(nil) }
        addRule.controlSize = .small
        inner.addArrangedSubview(row(title: "条件规则",
                                     detail: "按干员属性或盒属性决定档位，自上而下第一条命中的规则生效",
                                     accessory: addRule))

        rulesStack.orientation = .vertical
        rulesStack.spacing = 6
        rulesStack.alignment = .leading
        inner.addArrangedSubview(rulesStack)

        rulesEmpty.font = .systemFont(ofSize: 11.5)
        rulesEmpty.textColor = .tertiaryLabelColor
        inner.addArrangedSubview(rulesEmpty)

        inner.addArrangedSubview(separator())

        // 精二名单
        let addWhite = ActionButton(title: "＋ 添加") { [weak self] in self?.pickNames(white: true) }
        addWhite.controlSize = .small
        inner.addArrangedSubview(row(title: "精二名单",
                                     detail: "名单内的干员无视规则，始终优先精二头像",
                                     accessory: addWhite))
        configure(flow: whiteFlow)
        inner.addArrangedSubview(whiteFlow)
        style(empty: whiteEmpty)
        inner.addArrangedSubview(whiteEmpty)

        inner.addArrangedSubview(separator())

        // 精一名单
        let addBlack = ActionButton(title: "＋ 添加") { [weak self] in self?.pickNames(white: false) }
        addBlack.controlSize = .small
        inner.addArrangedSubview(row(title: "精一名单",
                                     detail: "名单内的干员无视规则，始终展示精一头像",
                                     accessory: addBlack))
        configure(flow: blackFlow)
        inner.addArrangedSubview(blackFlow)
        style(empty: blackEmpty)
        inner.addArrangedSubview(blackEmpty)

        inner.addArrangedSubview(separator())

        // 导入 / 导出
        let buttons = NSStackView()
        buttons.orientation = .horizontal
        buttons.spacing = 6
        for (title, handler) in [("导出", #selector(exportPref)),
                                 ("导入", #selector(importPref)),
                                 ("恢复默认", #selector(resetPref))] {
            let button = ActionButton(title: title) { [weak self] in
                guard let self else { return }
                NSApp.sendAction(handler, to: self, from: nil)
            }
            button.controlSize = .small
            buttons.addArrangedSubview(button)
        }
        inner.addArrangedSubview(row(title: "导入 / 导出",
                                     detail: "整份设置导出成 JSON 文本，换机后粘贴导入即可",
                                     accessory: buttons))

        NSLayoutConstraint.activate([
            inner.topAnchor.constraint(equalTo: topAnchor, constant: 16),
            inner.bottomAnchor.constraint(equalTo: bottomAnchor, constant: -14),
            inner.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 18),
            inner.trailingAnchor.constraint(equalTo: trailingAnchor, constant: -18),
            whiteFlow.widthAnchor.constraint(equalTo: inner.widthAnchor),
            blackFlow.widthAnchor.constraint(equalTo: inner.widthAnchor),
            rulesStack.widthAnchor.constraint(equalTo: inner.widthAnchor),
        ])
    }

    private func configure(flow: FlowView) {
        flow.itemSpacing = 6
        flow.itemHeight = 24
    }

    private func style(empty: NSTextField) {
        empty.font = .systemFont(ofSize: 11.5)
        empty.textColor = .tertiaryLabelColor
    }

    // MARK: - 绘制

    func render(_ pref: AvatarPref, theme: ShellTheme) {
        self.theme = theme
        self.pref = pref
        layer?.backgroundColor = theme.panel.cgColor
        layer?.borderColor = theme.border.cgColor

        suspend = true
        defer { suspend = false }

        levelControl.selectedSegment = pref.def == AvatarPref.elite2 ? 1 : 0

        rulesStack.arrangedSubviews.forEach { $0.removeFromSuperview() }
        for (index, rule) in pref.rules.enumerated() {
            rulesStack.addArrangedSubview(ruleRow(rule, index: index, total: pref.rules.count))
        }
        rulesEmpty.isHidden = !pref.rules.isEmpty

        rebuildChips(whiteFlow, names: pref.white, empty: whiteEmpty, white: true)
        rebuildChips(blackFlow, names: pref.black, empty: blackEmpty, white: false)
    }

    func applyTheme(_ theme: ShellTheme) {
        self.theme = theme
        layer?.backgroundColor = theme.panel.cgColor
        layer?.borderColor = theme.border.cgColor
        refreshSeparators(in: self)
    }

    private func refreshSeparators(in node: NSView) {
        for sub in node.subviews {
            if sub.identifier?.rawValue == "separator" {
                sub.layer?.backgroundColor = theme.border.cgColor
            }
            refreshSeparators(in: sub)
        }
    }

    private func rebuildChips(_ flow: FlowView, names: [String], empty: NSTextField, white: Bool) {
        flow.subviews.forEach { $0.removeFromSuperview() }
        for name in names {
            let chip = NSButton(title: name + "  ×", target: self,
                                action: white ? #selector(removeWhite(_:)) : #selector(removeBlack(_:)))
            chip.isBordered = false
            chip.wantsLayer = true
            chip.layer?.cornerRadius = 11
            chip.layer?.borderWidth = 1
            chip.layer?.borderColor = theme.border.cgColor
            chip.layer?.backgroundColor = theme.canvas.cgColor
            chip.font = .systemFont(ofSize: 11.5)
            chip.contentTintColor = .labelColor
            chip.identifier = NSUserInterfaceItemIdentifier(name)
            flow.addSubview(chip)
        }
        flow.needsLayout = true
        empty.isHidden = !names.isEmpty
    }

    private func ruleRow(_ rule: AvatarRule, index: Int, total: Int) -> NSView {
        let row = NSStackView()
        row.orientation = .horizontal
        row.spacing = 8
        row.alignment = .centerY

        let badge = NSTextField(labelWithString: rule.levelLabel)
        badge.font = .systemFont(ofSize: 10.5, weight: .semibold)
        badge.textColor = .white
        badge.alignment = .center
        badge.wantsLayer = true
        badge.layer?.cornerRadius = 4
        badge.layer?.backgroundColor = (rule.isElite2
            ? NSColor.systemOrange : NSColor.secondaryLabelColor).cgColor
        badge.translatesAutoresizingMaskIntoConstraints = false
        badge.widthAnchor.constraint(equalToConstant: 34).isActive = true
        badge.heightAnchor.constraint(equalToConstant: 18).isActive = true

        let summary = NSTextField(labelWithString: rule.cond.summary)
        summary.font = .systemFont(ofSize: 12)
        summary.lineBreakMode = .byTruncatingTail

        let up = miniButton("↑") { [weak self] in self?.move(index, by: -1) }
        let down = miniButton("↓") { [weak self] in self?.move(index, by: 1) }
        let edit = miniButton("编辑") { [weak self] in self?.editRule(index) }
        let remove = miniButton("删除") { [weak self] in self?.removeRule(index) }
        up.isEnabled = index > 0
        down.isEnabled = index < total - 1

        row.addArrangedSubview(badge)
        row.addArrangedSubview(summary)
        row.addArrangedSubview(NSView())
        row.addArrangedSubview(up)
        row.addArrangedSubview(down)
        row.addArrangedSubview(edit)
        row.addArrangedSubview(remove)
        return row
    }

    private func miniButton(_ title: String, action: @escaping () -> Void) -> NSButton {
        let button = ActionButton(title: title, handler: action)
        button.isBordered = false
        button.font = .systemFont(ofSize: 11.5)
        button.contentTintColor = .secondaryLabelColor
        button.setContentHuggingPriority(.required, for: .horizontal)
        return button
    }

    // MARK: - 行

    private func row(title: String, detail: String, accessory: NSView) -> NSView {
        let texts = NSStackView()
        texts.orientation = .vertical
        texts.spacing = 2
        texts.alignment = .leading
        let titleLabel = NSTextField(labelWithString: title)
        titleLabel.font = .systemFont(ofSize: 13.5)
        let detailLabel = NSTextField(wrappingLabelWithString: detail)
        detailLabel.font = .systemFont(ofSize: 11.5)
        detailLabel.textColor = .secondaryLabelColor
        detailLabel.preferredMaxLayoutWidth = 600
        texts.addArrangedSubview(titleLabel)
        texts.addArrangedSubview(detailLabel)

        let row = NSStackView()
        row.orientation = .horizontal
        row.spacing = 12
        row.alignment = .centerY
        row.addArrangedSubview(texts)
        row.addArrangedSubview(NSView())
        row.addArrangedSubview(accessory)
        accessory.setContentHuggingPriority(.required, for: .horizontal)
        return row
    }

    private func separator() -> NSView {
        let line = NSView()
        line.identifier = NSUserInterfaceItemIdentifier("separator")
        line.wantsLayer = true
        line.translatesAutoresizingMaskIntoConstraints = false
        line.heightAnchor.constraint(equalToConstant: 1).isActive = true
        line.layer?.backgroundColor = theme.border.cgColor
        return line
    }

    // MARK: - 变更

    private func commit() {
        guard !suspend else { return }
        pref = pref.normalized()
        render(pref, theme: theme)
        onChange?(pref)
    }

    @objc private func levelChanged() {
        guard !suspend else { return }
        pref.def = levelControl.selectedSegment == 1 ? AvatarPref.elite2 : AvatarPref.elite1
        commit()
    }

    private func move(_ index: Int, by offset: Int) {
        let target = index + offset
        guard pref.rules.indices.contains(index), pref.rules.indices.contains(target) else { return }
        pref.rules.swapAt(index, target)
        commit()
    }

    private func removeRule(_ index: Int) {
        guard pref.rules.indices.contains(index) else { return }
        pref.rules.remove(at: index)
        commit()
    }

    @objc private func removeWhite(_ sender: NSButton) {
        guard let name = sender.identifier?.rawValue else { return }
        pref.white.removeAll { $0 == name }
        commit()
    }

    @objc private func removeBlack(_ sender: NSButton) {
        guard let name = sender.identifier?.rawValue else { return }
        pref.black.removeAll { $0 == name }
        commit()
    }

    @objc private func resetPref() {
        let alert = NSAlert()
        alert.messageText = "恢复默认头像设置？"
        alert.informativeText = "会清空所有规则与两份名单，默认优先级回到「优先精一」。"
        alert.addButton(withTitle: "恢复默认")
        alert.addButton(withTitle: "取消")
        guard run(alert) == .alertFirstButtonReturn else { return }
        pref = AvatarPref()
        commit()
    }

    @objc private func exportPref() {
        let text = NSTextView(frame: NSRect(x: 0, y: 0, width: 520, height: 200))
        text.string = pref.json
        text.isEditable = false
        text.font = .monospacedSystemFont(ofSize: 11, weight: .regular)
        text.drawsBackground = false

        let scroll = NSScrollView(frame: NSRect(x: 0, y: 0, width: 520, height: 200))
        scroll.documentView = text
        scroll.hasVerticalScroller = true
        scroll.borderType = .bezelBorder

        let alert = NSAlert()
        alert.messageText = "导出头像个性化设置"
        alert.informativeText = "复制下面这段 JSON，换设备后粘贴到「导入」。"
        alert.accessoryView = scroll
        alert.addButton(withTitle: "复制")
        alert.addButton(withTitle: "关闭")
        if run(alert) == .alertFirstButtonReturn {
            NSPasteboard.general.clearContents()
            NSPasteboard.general.setString(pref.json, forType: .string)
        }
    }

    @objc private func importPref() {
        let text = NSTextView(frame: NSRect(x: 0, y: 0, width: 520, height: 200))
        text.font = .monospacedSystemFont(ofSize: 11, weight: .regular)
        text.isRichText = false
        text.drawsBackground = false

        let scroll = NSScrollView(frame: NSRect(x: 0, y: 0, width: 520, height: 200))
        scroll.documentView = text
        scroll.hasVerticalScroller = true
        scroll.borderType = .bezelBorder

        let alert = NSAlert()
        alert.messageText = "导入头像个性化设置"
        alert.informativeText = "粘贴之前导出的 JSON。导入会覆盖当前这份设置（规则 / 名单 / 默认档位）。"
        alert.accessoryView = scroll
        alert.addButton(withTitle: "导入")
        alert.addButton(withTitle: "取消")
        guard run(alert) == .alertFirstButtonReturn else { return }

        let raw = text.string.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !raw.isEmpty, let data = raw.data(using: .utf8),
              let parsed = try? JSONDecoder().decode(AvatarPref.self, from: data) else {
            let failed = NSAlert()
            failed.messageText = "这段 JSON 解析不了"
            failed.informativeText = "请确认粘贴的是本程序「导出」出来的完整内容。"
            failed.addButton(withTitle: "好")
            _ = run(failed)
            return
        }
        pref = parsed
        commit()
    }

    private func pickNames(white: Bool) {
        let existing = white ? pref.white : pref.black
        let picker = RosterPicker(title: white ? "添加到精二名单" : "添加到精一名单", picked: existing)
        guard let picked = picker.run(window: window) else { return }
        if white { pref.white = picked } else { pref.black = picked }
        commit()
    }

    // MARK: - 规则编辑

    private func editRule(_ index: Int?) {
        let existing = index.flatMap { pref.rules.indices.contains($0) ? pref.rules[$0] : nil }
        var draft = existing ?? AvatarRule(level: pref.def, cond: AvatarCond())
        let editor = RuleEditor(rule: draft)
        guard editor.run(window: window) else { return }
        draft = editor.rule
        if let index, pref.rules.indices.contains(index) {
            pref.rules[index] = draft
        } else {
            pref.rules.append(draft)
        }
        commit()
    }

    private func run(_ alert: NSAlert) -> NSApplication.ModalResponse {
        if let window {
            return alert.runModal()
        }
        return alert.runModal()
    }
}

// MARK: - 流式布局

/// 自动换行的横向排布。名单 chip 与规则编辑里的选项都靠它 ——
/// AppKit 没有现成的流式容器，而 NSStackView 只会把子视图越挤越窄。
final class FlowView: NSView {
    var itemSpacing: CGFloat = 6
    var itemHeight: CGFloat = 24
    private var contentHeight: CGFloat = 0

    override var isFlipped: Bool { true }

    override func layout() {
        super.layout()
        guard bounds.width > 0 else { return }
        var x: CGFloat = 0
        var y: CGFloat = 0
        for sub in subviews {
            let width = min(max(sub.fittingSize.width, 44), bounds.width)
            if x + width > bounds.width, x > 0 {
                x = 0
                y += itemHeight + itemSpacing
            }
            sub.frame = NSRect(x: x, y: y, width: width, height: itemHeight)
            x += width + itemSpacing
        }
        let total = subviews.isEmpty ? 0 : y + itemHeight
        if abs(total - contentHeight) > 0.5 {
            contentHeight = total
            invalidateIntrinsicContentSize()
        }
    }

    override var intrinsicContentSize: NSSize {
        NSSize(width: NSView.noIntrinsicMetric, height: max(contentHeight, 1))
    }
}

// MARK: - 规则编辑对话框

/// 一条规则的编辑器：选档位 + 按分组勾条件。
/// 分组之间「且」、同组多选「或」、空组不限 —— 和手机端的语义一致。
final class RuleEditor: NSObject {
    private(set) var rule: AvatarRule
    private let alert = NSAlert()
    private let level = NSSegmentedControl(labels: ["精一", "精二"], trackingMode: .selectOne,
                                           target: nil, action: nil)
    private var groupButtons: [String: [NSButton]] = [:]
    private var groupFlows: [String: FlowView] = [:]

    init(rule: AvatarRule) {
        self.rule = rule
        super.init()
        alert.messageText = "条件规则"
        alert.informativeText = "不同分组之间是「且」，同一组里多选是「或」。留空的分组不限。"
        alert.addButton(withTitle: "保存")
        alert.addButton(withTitle: "取消")
        build()
    }

    private func build() {
        let body = FlippedView(frame: NSRect(x: 0, y: 0, width: 620, height: 430))

        var y: CGFloat = 8
        let levelLabel = NSTextField(labelWithString: "展示档位")
        levelLabel.font = .systemFont(ofSize: 12.5)
        levelLabel.frame = NSRect(x: 0, y: y + 2, width: 70, height: 20)
        body.addSubview(levelLabel)

        level.selectedSegment = rule.level == AvatarPref.elite2 ? 1 : 0
        level.controlSize = .small
        level.frame = NSRect(x: 76, y: y, width: 140, height: 24)
        body.addSubview(level)
        y += 36

        for (key, label) in CharRoster.groups {
            let heading = NSTextField(labelWithString: label)
            heading.font = .systemFont(ofSize: 11.5, weight: .medium)
            heading.textColor = .secondaryLabelColor
            heading.frame = NSRect(x: 0, y: y, width: 620, height: 16)
            body.addSubview(heading)
            y += 20

            let flow = FlowView(frame: NSRect(x: 0, y: y, width: 620, height: 26))
            flow.itemSpacing = 5
            flow.itemHeight = 24
            body.addSubview(flow)
            groupFlows[key] = flow

            let picked = rule.cond.values(of: key)
            var buttons: [NSButton] = []
            for value in CharRoster.options(of: key, pickedProfessions: rule.cond.prof) {
                let button = NSButton(checkboxWithTitle: CharRoster.label(of: key, value: value),
                                      target: self, action: #selector(toggleOption(_:)))
                button.identifier = NSUserInterfaceItemIdentifier(key + "\u{1}" + value)
                button.font = .systemFont(ofSize: 11.5)
                button.state = picked.contains(value) ? .on : .off
                button.sizeToFit()
                flow.addSubview(button)
                buttons.append(button)
            }
            groupButtons[key] = buttons

            // 分支那一组选项多，给两行的高度
            let rows = max(1, flow.subviews.count / 8 + 1)
            y += CGFloat(rows) * 26 + 6
        }

        let scroll = NSScrollView(frame: NSRect(x: 0, y: 0, width: 620, height: 430))
        scroll.hasVerticalScroller = true
        scroll.borderType = .noBorder
        scroll.drawsBackground = false

        let content = FlippedView(frame: NSRect(x: 0, y: 0, width: 620, height: y))
        for sub in body.subviews {
            content.addSubview(sub)
        }
        scroll.documentView = content

        alert.accessoryView = scroll
    }

    @objc private func toggleOption(_ sender: NSButton) {
        guard let raw = sender.identifier?.rawValue else { return }
        let parts = raw.split(separator: "\u{1}", maxSplits: 1).map(String.init)
        guard parts.count == 2 else { return }
        rule.cond.toggle(parts[0], parts[1])

        // 改过「职业」之后，「分支」的可选项会变，得重建那一行
        if parts[0] == "prof" { rebuildSubGroup() }
    }

    private func rebuildSubGroup() {
        guard let flow = groupFlows["sub"] else { return }
        flow.subviews.forEach { $0.removeFromSuperview() }
        var buttons: [NSButton] = []
        for value in CharRoster.options(of: "sub", pickedProfessions: rule.cond.prof) {
            let button = NSButton(checkboxWithTitle: value, target: self, action: #selector(toggleOption(_:)))
            button.identifier = NSUserInterfaceItemIdentifier("sub\u{1}" + value)
            button.font = .systemFont(ofSize: 11.5)
            button.state = rule.cond.sub.contains(value) ? .on : .off
            button.sizeToFit()
            flow.addSubview(button)
            buttons.append(button)
        }
        groupButtons["sub"] = buttons
        flow.needsLayout = true
    }

    func run(window: NSWindow?) -> Bool {
        let response = window.map { alert.beginSheetModal(for: $0) } ?? nil
        _ = response
        let result: NSApplication.ModalResponse
        if window == nil {
            result = alert.runModal()
        } else {
            // beginSheetModal 是异步的，这里退回同步 runModal 更简单可靠
            result = alert.runModal()
        }
        guard result == .alertFirstButtonReturn else { return false }
        rule.level = level.selectedSegment == 1 ? AvatarPref.elite2 : AvatarPref.elite1
        rule.cond = rule.cond.normalized()
        return true
    }
}

// MARK: - 干员选择器

/// 从全部干员里多选。带搜索框，勾选状态即最终名单。
final class RosterPicker: NSObject {
    private var picked: Set<String>
    private var filtered: [String] = CharRoster.all
    private let table = NSTableView()
    private let search = NSSearchField()
    private let alert = NSAlert()

    init(title: String, picked: [String]) {
        self.picked = Set(picked)
        super.init()
        alert.messageText = title
        alert.informativeText = "勾选要加入名单的干员，取消勾选即移出。"
        alert.addButton(withTitle: "确定")
        alert.addButton(withTitle: "取消")
        build()
    }

    private func build() {
        let body = NSView(frame: NSRect(x: 0, y: 0, width: 420, height: 400))

        search.frame = NSRect(x: 0, y: 376, width: 420, height: 24)
        search.placeholderString = "搜索干员名（支持中文 / 英文）"
        search.target = self
        search.action = #selector(searchChanged)
        search.sendsSearchStringImmediately = true
        body.addSubview(search)

        let column = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("name"))
        column.width = 400
        table.addTableColumn(column)
        table.headerView = nil
        table.rowHeight = 24
        table.style = .inset
        table.dataSource = self
        table.delegate = self

        let scroll = NSScrollView(frame: NSRect(x: 0, y: 0, width: 420, height: 368))
        scroll.documentView = table
        scroll.hasVerticalScroller = true
        scroll.borderType = .bezelBorder
        body.addSubview(scroll)

        alert.accessoryView = body
    }

    @objc private func searchChanged() {
        let keyword = search.stringValue.trimmingCharacters(in: .whitespaces)
        filtered = keyword.isEmpty ? CharRoster.all : CharRoster.all.filter { $0.localizedCaseInsensitiveContains(keyword) }
        table.reloadData()
    }

    func run(window: NSWindow?) -> [String]? {
        _ = window
        guard alert.runModal() == .alertFirstButtonReturn else { return nil }
        return picked.sorted { lhs, rhs in
            let l = CharRoster.all.firstIndex(of: lhs) ?? Int.max
            let r = CharRoster.all.firstIndex(of: rhs) ?? Int.max
            return l < r
        }
    }
}

extension RosterPicker: NSTableViewDataSource, NSTableViewDelegate {
    func numberOfRows(in tableView: NSTableView) -> Int { filtered.count }

    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        let id = NSUserInterfaceItemIdentifier("roster-cell")
        let cell = tableView.makeView(withIdentifier: id, owner: self) as? NSTableCellView
            ?? NSTableCellView(identifier: id)
        let name = filtered[row]
        let button = NSButton(checkboxWithTitle: name, target: self, action: #selector(toggleRow(_:)))
        button.font = .systemFont(ofSize: 12)
        button.state = picked.contains(name) ? .on : .off
        button.identifier = NSUserInterfaceItemIdentifier(name)
        button.frame = NSRect(x: 2, y: 2, width: 380, height: 20)
        cell.subviews.forEach { $0.removeFromSuperview() }
        cell.addSubview(button)
        return cell
    }

    @objc private func toggleRow(_ sender: NSButton) {
        guard let name = sender.identifier?.rawValue else { return }
        if sender.state == .on { picked.insert(name) } else { picked.remove(name) }
    }
}

private extension NSTableCellView {
    convenience init(identifier: NSUserInterfaceItemIdentifier) {
        self.init(frame: .zero)
        self.identifier = identifier
    }
}
