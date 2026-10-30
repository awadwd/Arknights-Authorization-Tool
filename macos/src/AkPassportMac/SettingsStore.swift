import Foundation

// MARK: - 设置模型

/// 从 H5 的 localStorage 读出来的一份快照。
/// 字段与 Windows 版 `SettingsView.WebSettings` 一一对应。
struct WebSettings {
    var theme = "liquid"
    var showMarketPrice = false
    var enableEnglishSearch = false
    var enableJapaneseSearch = false
    var enableNicknameSearch = false
    var enableGuessData = false
    var updateNotify = true
    var autoUpdate = false
    var dataSource = ""
    var dataVersion = ""
    var lastUpdate = ""
}

// MARK: - 干员头像个性化

/// 与手机端 `utils/avatarPref.js` 完全同构：
/// `localStorage.avatarPrefSettings` = `{v:1,def:0|2,rules:[…],white:[…],black:[…]}`。
///
/// 判定优先级（高 → 低）：精一名单 > 精二名单 > 条件规则（首条命中）> 默认档位。
struct AvatarPref: Codable {
    var v = 1
    /// 默认档位：0 = 优先精一，2 = 优先精二（与游戏里的精英化等级同值，不是 1/2 序号）。
    var def = 0
    var rules: [AvatarRule] = []
    var white: [String] = []
    var black: [String] = []

    static let elite1 = 0
    static let elite2 = 2

    /// 清洗成手机端 `normalizeSettings` 认可的形状：脏数据不该让页面崩。
    func normalized() -> AvatarPref {
        var clean = AvatarPref()
        clean.v = 1
        clean.def = def == AvatarPref.elite2 ? AvatarPref.elite2 : AvatarPref.elite1
        clean.white = AvatarPref.cleanList(white)
        clean.black = AvatarPref.cleanList(black)
        clean.rules = rules.compactMap { rule in
            let cond = rule.cond.normalized()
            // 空条件等价于默认档位，手机端也会丢掉
            guard cond.hasAny else { return nil }
            return AvatarRule(id: rule.id.isEmpty ? AvatarRule.newID() : rule.id,
                              level: rule.level == AvatarPref.elite2 ? AvatarPref.elite2 : AvatarPref.elite1,
                              cond: cond)
        }
        return clean
    }

    var json: String {
        let encoder = JSONEncoder()
        encoder.outputFormatting = []
        guard let data = try? encoder.encode(normalized()),
              let text = String(data: data, encoding: .utf8) else { return "{}" }
        return text
    }

    static func decode(_ json: String?) -> AvatarPref {
        guard let json, !json.isEmpty, let data = json.data(using: .utf8),
              let parsed = try? JSONDecoder().decode(AvatarPref.self, from: data) else {
            return AvatarPref()
        }
        return parsed.normalized()
    }

    private static func cleanList(_ src: [String]) -> [String] {
        var seen = Set<String>()
        var result: [String] = []
        for raw in src {
            let value = raw.trimmingCharacters(in: .whitespacesAndNewlines)
            guard !value.isEmpty, seen.insert(value).inserted else { continue }
            result.append(value)
        }
        return result
    }
}

struct AvatarRule: Codable {
    var id: String = AvatarRule.newID()
    var level: Int = AvatarPref.elite1
    var cond: AvatarCond = AvatarCond()

    var isElite2: Bool { level == AvatarPref.elite2 }
    var levelLabel: String { isElite2 ? "精二" : "精一" }

    static func newID() -> String {
        "r" + UUID().uuidString.replacingOccurrences(of: "-", with: "").prefix(8).lowercased()
    }
}

/// 一条规则的条件。不同分组之间「且」，同组多选「或」，空分组不限。
/// 字段名必须和手机端一致（都是三个字母以内的短键）。
struct AvatarCond: Codable {
    var prof: [String] = []
    var sub: [String] = []
    var rar: [String] = []
    var pos: [String] = []
    var sex: [String] = []
    var obt: [String] = []
    var tag: [String] = []
    var boxType: [String] = []
    var sale: [String] = []
    var rep: [String] = []
    var year: [String] = []

    func values(of key: String) -> [String] {
        switch key {
        case "prof": return prof
        case "sub": return sub
        case "rar": return rar
        case "pos": return pos
        case "sex": return sex
        case "obt": return obt
        case "tag": return tag
        case "boxType": return boxType
        case "sale": return sale
        case "rep": return rep
        case "year": return year
        default: return []
        }
    }

    mutating func set(_ key: String, _ list: [String]) {
        switch key {
        case "prof": prof = list
        case "sub": sub = list
        case "rar": rar = list
        case "pos": pos = list
        case "sex": sex = list
        case "obt": obt = list
        case "tag": tag = list
        case "boxType": boxType = list
        case "sale": sale = list
        case "rep": rep = list
        case "year": year = list
        default: break
        }
    }

    var hasAny: Bool {
        CharRoster.groups.contains { !values(of: $0.key).isEmpty } || !year.isEmpty
    }

    func normalized() -> AvatarCond {
        var copy = AvatarCond()
        for (key, _) in CharRoster.groups {
            copy.set(key, AvatarPref.cleanValues(values(of: key)))
        }
        copy.year = AvatarPref.cleanValues(year)
        return copy
    }

    /// 勾选 / 取消一个值。改「职业」时同步剔除不再适用的「分支」，与手机端一致。
    mutating func toggle(_ key: String, _ value: String) {
        var list = values(of: key)
        if let index = list.firstIndex(of: value) {
            list.remove(at: index)
        } else {
            list.append(value)
        }
        set(key, list)

        if key == "prof" {
            let allowed = Set(CharRoster.options(of: "sub", pickedProfessions: prof))
            set("sub", sub.filter { allowed.contains($0) })
        }
    }

    /// 「职业：术师、狙击 · 稀有度：6★」，空条件返回「不限」。
    var summary: String {
        var parts: [String] = []
        for (key, label) in CharRoster.groups {
            let picked = values(of: key)
            guard !picked.isEmpty else { continue }
            let text = picked.map { CharRoster.label(of: key, value: $0) }.joined(separator: "、")
            parts.append("\(label)：\(text)")
        }
        if !year.isEmpty {
            parts.append("发布年份：" + year.map { $0 + "年" }.joined(separator: "、"))
        }
        return parts.isEmpty ? "不限" : parts.joined(separator: " · ")
    }
}

extension AvatarPref {
    fileprivate static func cleanValues(_ src: [String]) -> [String] {
        cleanList(src)
    }
}

// MARK: - localStorage 读写

/// 设置页与 H5 之间的唯一通道：读写 `localStorage`。
///
/// 存储格式必须和 uni-app 的 `uni.setStorageSync` 一致 —— 字符串原样存，
/// 其余类型包成 `{"type":"boolean","data":true}`。这一点 Windows 版踩过坑：
/// 直接 `setItem(k, true)` 只会存成字符串 "true"，前端 `typeof` 判断走不到布尔分支，
/// 表现就是「开关点了、页面没反应」。另外 `window.uni` 在内嵌 H5 里是空对象，
/// 只能操作裸 `localStorage`。
final class SettingsStore {
    weak var shell: WebShell?

    /// 写完之后延迟刷新页面用的句柄。连续勾几个开关只刷一次，
    /// 否则每勾一下整页重载，看着像卡死。
    private var pendingReload: DispatchWorkItem?

    init(shell: WebShell?) {
        self.shell = shell
    }

    // MARK: 读

    func readSettings(_ completion: @escaping (WebSettings?) -> Void) {
        let script = """
        (function(){try{
        \(SettingsStore.helper)
        var au = g('autoUpdateSetting'); if (typeof au !== 'object' || au === null) au = {};
        var un = g('updateNotifyEnabled');
        return JSON.stringify({
          theme: g('themeMode') || 'liquid',
          showMarketPrice: b('showMarketPrice'),
          enableEnglishSearch: b('enableEnglishSearch'),
          enableJapaneseSearch: b('enableJapaneseSearch'),
          enableNicknameSearch: b('enableNicknameSearch'),
          enableGuessData: b('enableGuessData'),
          updateNotify: (un === null || un === '') ? true : (un === true || un === 'true'),
          autoUpdate: !!au.enabled,
          dataSource: g('dataSource') || '',
          dataVersion: g('dataVersion') || '',
          lastUpdate: g('localUpdateTime') || ''
        });
        }catch(e){ return null; }})()
        """
        evaluate(script) { result in
            guard let text = result as? String,
                  let data = text.data(using: .utf8),
                  let raw = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
                completion(nil)
                return
            }
            var settings = WebSettings()
            settings.theme = raw["theme"] as? String ?? "liquid"
            settings.showMarketPrice = raw["showMarketPrice"] as? Bool ?? false
            settings.enableEnglishSearch = raw["enableEnglishSearch"] as? Bool ?? false
            settings.enableJapaneseSearch = raw["enableJapaneseSearch"] as? Bool ?? false
            settings.enableNicknameSearch = raw["enableNicknameSearch"] as? Bool ?? false
            settings.enableGuessData = raw["enableGuessData"] as? Bool ?? false
            settings.updateNotify = raw["updateNotify"] as? Bool ?? true
            settings.autoUpdate = raw["autoUpdate"] as? Bool ?? false
            settings.dataSource = raw["dataSource"] as? String ?? ""
            settings.dataVersion = raw["dataVersion"] as? String ?? ""
            settings.lastUpdate = raw["lastUpdate"] as? String ?? ""
            completion(settings)
        }
    }

    func readAvatarPref(_ completion: @escaping (AvatarPref) -> Void) {
        let script = """
        (function(){try{
        \(SettingsStore.helper)
        var v = g('avatarPrefSettings');
        if (typeof v === 'string') { try { v = JSON.parse(v); } catch(e) { v = null; } }
        return JSON.stringify(v || null);
        }catch(e){ return 'null'; }})()
        """
        evaluate(script) { result in
            // 返回值是「JSON 文本的 JSON」：先还原成字符串，再解析设置本身
            guard let text = result as? String else {
                completion(AvatarPref())
                return
            }
            guard let outer = text.data(using: .utf8),
                  let inner = try? JSONDecoder().decode(String.self, from: outer) else {
                completion(AvatarPref())
                return
            }
            completion(AvatarPref.decode(inner == "null" ? nil : inner))
        }
    }

    // MARK: 写

    /// 这些开关手机端是按字符串 `'true'` / `'false'` 存的（页面里普遍写 `=== 'true'`），
    /// 写成真布尔会「看着改了、实际没生效」。
    private static let stringBoolKeys: Set<String> = [
        "showMarketPrice", "enableEnglishSearch", "enableJapaneseSearch",
        "enableNicknameSearch", "enableGuessData",
    ]

    func setBool(_ key: String, _ value: Bool, reload: Bool = true) {
        let literal: String
        if SettingsStore.stringBoolKeys.contains(key) {
            literal = value ? "'true'" : "'false'"
        } else {
            literal = value ? "true" : "false"
        }
        run("s(\(SettingsStore.quote(key)), \(literal));") { [weak self] in
            guard reload else { return }
            self?.scheduleReload()
        }
    }

    func setString(_ key: String, _ value: String) {
        run("s(\(SettingsStore.quote(key)), \(SettingsStore.quote(value)));") { [weak self] in
            self?.scheduleReload()
        }
    }

    /// `autoUpdateSetting` 是个对象，读出来改 `enabled` 再写回，不能整个覆盖 ——
    /// 里面还有其它页面的字段。
    func setAutoUpdate(_ value: Bool) {
        let script = """
        (function(){try{
        \(SettingsStore.helper)
        var v = g('autoUpdateSetting'); if (!v || typeof v !== 'object') v = {};
        v.enabled = \(value ? "true" : "false");
        s('autoUpdateSetting', v);
        return true;
        }catch(e){ return false; }})()
        """
        evaluate(script) { [weak self] _ in self?.scheduleReload() }
    }

    func setAvatarPref(_ pref: AvatarPref) {
        run("s('avatarPrefSettings', \(SettingsStore.quote(pref.json)));") { [weak self] in
            self?.scheduleReload()
        }
    }

    /// 主题只写存储、不刷新页面：前端换主题本来就要重排版，
    /// 用户此刻停在原生设置页，刷新看不见也感觉不到，回 H5 时自然是新配色。
    func setTheme(_ name: String) {
        run("s('themeMode', \(SettingsStore.quote(name)));", completion: nil)
    }

    /// 调试用：把几个 key 的原始值打进日志。写入格式对不对，看一眼就知道。
    func probe(keys: [String]) {
        let list = keys.map { "'\($0)'" }.joined(separator: ",")
        let script = """
        (function(){try{
        \(SettingsStore.helper)
        var out = [];
        [\(list)].forEach(function(k){ out.push('  ' + k + ' = ' + localStorage.getItem(k)); });
        return out.join('\\n');
        }catch(e){ return 'probe error: ' + e.message; }})()
        """
        evaluate(script) { result in
            Diag.log("storage probe:\n\(result as? String ?? "nil")")
        }
    }

    // MARK: 内部

    private func run(_ body: String, completion: (() -> Void)?) {
        evaluate("(function(){try{\(SettingsStore.helper) \(body) return true;}catch(e){return false;}})()") { [weak self] result in
            if let ok = result as? Bool, !ok {
                Diag.log("settings: 写入失败 \(body)")
            }
            self.map { _ in completion?() }
        }
    }

    private func evaluate(_ script: String, completion: @escaping (Any?) -> Void) {
        guard let webView = shell?.webView else {
            completion(nil)
            return
        }
        webView.evaluateJavaScript(script) { result, error in
            if let error {
                Diag.log("settings js error: \(error.localizedDescription)")
            }
            completion(result)
        }
    }

    /// 存进去、刷出来。防抖 350ms：连着勾三下只重载一次。
    private func scheduleReload() {
        pendingReload?.cancel()
        let item = DispatchWorkItem { [weak self] in
            self?.pendingReload = nil
            Diag.log("settings: reload web for apply")
            self?.shell?.reload()
        }
        pendingReload = item
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.35, execute: item)
    }

    private static func quote(_ text: String) -> String {
        guard let data = try? JSONSerialization.data(withJSONObject: [text]),
              let json = String(data: data, encoding: .utf8) else { return "'\(text)'" }
        // ["x"] → "x"
        return String(json.dropFirst().dropLast())
    }

    /// uni-app 的 storage 编解码。`g` 会把 `{type,data}` 拆开，`s` 按类型包回去。
    private static let helper = """
    var g=function(k){try{var v=localStorage.getItem(k);if(v===null)return null;\
    try{var o=JSON.parse(v);if(o&&typeof o==='object'&&'type' in o&&'data' in o)return o.data;}catch(e){}\
    return v;}catch(e){return null;}};\
    var s=function(k,v){var t=typeof v;localStorage.setItem(k,t==='string'?v:JSON.stringify({type:t,data:v}));};\
    var b=function(k){var v=g(k);return v===true||v==='true';};
    """
}
