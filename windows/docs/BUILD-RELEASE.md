# 打包发布（Windows Beta）

> 一句话：`bash release.sh`。
> 它会依次做「前端 H5 编译 → 同步 wwwroot → Release 发布 → 拷到 dist → 压 zip」。

---

## 0. 环境前提（本机已具备）

| 依赖 | 位置 / 说明 |
|---|---|
| .NET SDK 10 | `dotnet --version` |
| Windows App SDK | 由 NuGet 还原（`Microsoft.WindowsAppSDK 1.8.x`） |
| HBuilderX | 默认按 `HB` 变量取（脚本内默认 `D:/Program Files/HBuilderX.3.2.9.20210927/HBuilderX`，目录名不随升级变，别按名字判版本）；换机器用 `HB=/your/HBuilderX bash _build_h5.sh` |
| Python 3.13 | 只用来做版本号解析和 zip 打包，默认取 PATH 里的 `python`；也可 `PY=/path/to/python` 覆盖 |

**首次在新机器上**：先执行一次 `dotnet restore src/AkPassport.Win/AkPassport.Win.csproj`。

---

## 1. 改版本号

只改 `src/AkPassport.Win/AkPassport.Win.csproj`：

```xml
<Version>1.0.0-beta.1</Version>          <!-- 决定产物目录名/zip 名，也是设置页显示的版本 -->
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
<InformationalVersion>1.0.0-beta.1</InformationalVersion>
```

- `<Version>` 可以带 `-beta.1` 后缀，`AssemblyVersion` 不行（只能是四段数字），所以两者分开写。
- 设置页「关于」显示的是 `InformationalVersion`（已去掉 `+commit` 后缀）。
- **不要**在脚本里再写一份版本号，`release.sh` 是从 csproj 里读的。

---

## 2. 一键出包

```bash
cd windows
bash release.sh
```

产物：

```
dist/
├─ 方舟通行证谷子查询工具-1.0.0-beta.1-安装程序.exe   # ★ 给用户的主要产物（单文件，约 135 MB）
├─ AkPassport-Win-1.0.0-beta.1-win-x64.zip           # 绿色版（约 86 MB）
└─ AkPassport-Win-1.0.0-beta.1-win-x64/              # 发布目录，安装程序的内容来源
   ├─ AkPassport.Win.exe      # 主程序（约 150 KB，真正的逻辑在 dll 里）
   ├─ *.dll / *.winmd …       # .NET 运行时 + Windows App SDK（400+ 个文件）
   ├─ Assets/                 # 图标
   ├─ wwwroot/                # H5 产物（缺了这个就是白屏）
   └─ 使用说明.txt
```

> 安装程序体积 ≈ 自带运行时（~55 MB）+ 内嵌的程序本体压缩包（~86 MB），
> 所以比绿色版大。想要小包就只发绿色版 zip。

发布参数（脚本里已固定，改之前先读第 5 节）：

```bash
dotnet publish src/AkPassport.Win/AkPassport.Win.csproj \
  -c Release -r win-x64 --self-contained true -p:Platform=x64 \
  -p:WindowsAppSDKSelfContained=true \
  -p:DebugType=None -p:DebugSymbols=false \
  -o dist/AkPassport-Win-<版本>-win-x64
```

---

## 2.5 安装程序（`installer/AkPassportSetup`）

不用 Inno Setup / NSIS（本机没装，也不想让用户过 UAC），自己写了个 WinForms 安装器：

| 决策 | 原因 |
|---|---|
| 装到 `%LOCALAPPDATA%\Programs\<程序名>` | 免管理员、免 UAC。`Program Files` 下会踩权限坑 |
| payload 内嵌为 `EmbeddedResource` | 整个安装包是单个 exe，不会出现「exe 和 zip 分家就废了」 |
| 卸载入口写 `HKCU\...\Uninstall\AkPassportWin` | 「设置 → 应用」里能看到并卸载，不需要额外卸载器 |
| 卸载器 = 安装器自己加 `/uninstall` | 只维护一份代码；安装时把自己拷成安装目录下的 `卸载.exe` |
| 用户数据在 `%LOCALAPPDATA%\AkPassport` | 和安装目录分离，覆盖安装不丢；卸载时才问要不要一起删 |

源码两个文件：

- `Program.cs` —— 路径解析、解压 payload、快捷方式、注册表、卸载
- `SetupForm.cs` —— 那一页向导 UI（目录 / 4 个勾选项 / 进度条）

**改 UI 或加勾选项**：改 `SetupForm.cs`，然后 `dotnet build installer/AkPassportSetup/AkPassportSetup.csproj` 单独验证，
不用跑完整 `release.sh`。没放 `payload.zip` 时「安装」按钮会自己置灰，方便单独调试界面。

安装参数（脚本里已固定）：

```bash
dotnet publish installer/AkPassportSetup/AkPassportSetup.csproj \
  -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:Version="$VER" \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false \
  -o dist/方舟通行证谷子查询工具-<版本>-安装程序
```

---

## 3. 发布前冒烟检查（必做，约 3 分钟）

打包脚本**不会**替你验证。双击 `dist/.../AkPassport.Win.exe`，按顺序过一遍：

| # | 检查项 | 期望 |
|---|---|---|
| 0 | **首次启动** | 弹出「欢迎使用」向导；点「开始使用」后桌面出现快捷方式，开始菜单「所有应用」里有入口 |
| 1 | 首页 | 工具宫格正常、主题为液态玻璃、图标可见（`theme-ark` 会反色，属正常）；顶部品牌名为「方舟通行证谷子查询工具」 |
| 2 | 设置 → 关于 | 版本号显示 `1.0.0-beta.1` |
| 3 | 查询页 | 左右两栏；左查「阿米娅」出盒号、右查「1.0」出干员；两栏结果互不覆盖 |
| 4 | 查询页 → 筛选 | 弹层打开；点「全部年份 / 单盒类型」**选项面板要盖在弹层之上** |
| 5 | 日历页 | 左日历 / 右时间线并排；把窗口拖窄 → 变回「日历 | 时间线」切换 |
| 6 | 抽卡模拟 | 普通 / 对战并排；点「开始对战」后左侧仍在，不互相挤坏 |
| 7 | 数据反馈 | 「反馈须知」标题与正文可见（不是白底白字） |
| 8 | 界面缩放 | Ctrl + 滚轮、双指捏合**都不能放大页面** |
| 9 | 托盘 | 右下角通知区域有程序图标（可能在「^」溢出里）；左键唤出、右键可退出 |
| 10 | 关闭窗口 | 默认最小化到托盘；托盘「退出」能真正退出 |

> 只看图不看行为容易漏。要抓行为用 Debug 构建 + CDP（见 `docs/UPDATE-VUE.md` 第 5 节）。

---

## 4. 发布后

1. zip 给出去即可。**解压目录要可写**（托盘、日志、WebView2 缓存都写 `%LOCALAPPDATA%`，程序目录本身不需要写）。
2. 用户从旧版升级：直接覆盖程序目录，`%LOCALAPPDATA%\AkPassport` 不要删，否则收藏/设置/抽卡记录全丢。
3. 留存 `shell.log` 路径说明，方便用户反馈。

---

## 5. 参数为什么这么写（踩过的坑）

- **`--self-contained true`**：不装 .NET 运行时也能跑。代价是体积大；如果确定用户都装了 .NET 10 Desktop Runtime，可以去掉，包会小很多。
- **`WindowsAppSDKSelfContained=true`**：不依赖系统预装的 Windows App Runtime，否则用户第一次启动会弹「需要安装 Windows App Runtime」。
- **★ 不能用 `PublishSingleFile`**：单文件模式会把 `Content`（`Assets/`、`wwwroot/`）丢掉，
  发布目录里只剩一个 exe —— 装完打开是**纯白窗口**（WebView 找不到 index.html）。
  这个坑踩过两次：`ls dist/...` 里必须能看到 `wwwroot/index.html` 和 `Assets/app.ico`，
  `release.sh` 已经把这两项写成硬断言。启动速度上文件夹模式也更快（不用解压到 `%TEMP%`）。
- **`DebugType=None`**：不把 pdb 打进包里（否则多几十 MB）。
- **Release 下 `AreDevToolsEnabled=false`**：正式包禁 F12；Debug 构建仍然开，并用 CDP 的 9222 端口做自动化验证。
- **一定要先关掉正在运行的 exe**：`dotnet publish` 在 exe 被占用时直接报 `MSB3027`。脚本第一行已经 `taskkill`。

常见报错对照：

| 报错 | 原因 | 处理 |
|---|---|---|
| `MSB3027 / MSB3021 文件被锁定` | 程序还在跑 | 关掉窗口（或托盘退出），重跑 |
| `WMC0001 Unknown type 'SettingsView'` | XAML 编译器缓存错乱 | `dotnet clean` 后重编；仍不行删 `obj/` |
| `WMC9999 未将对象引用设置到对象的实例` | 同上，通常是前一个编译错误的连带 | 先修前面的 C# 错误 |
| 发布成功但打开是白屏 | 缺 `wwwroot`（多见于加了 PublishSingleFile） | 检查 `dist/.../wwwroot/index.html` 是否存在 |
| 窗口正常但页面白屏 | WebView2 运行时缺失 | 装 Evergreen Runtime |
| 桌面快捷方式建到开始菜单里 | 桌面路径返回空串（OneDrive / 精简系统） | 已修：`ShortcutService.ResolveDesktop()` 有兜底 |
| vite 报 `Build failed in 5s` 但 `index.html` 还在 | 清空 `dist-h5` 被批量删除保护拦了 | 清理失败脚本会退化为「改名搬到 `_trash/`」；仍不行就手工删（见下） |
| `wwwroot` 越编越大（几十 MB、上千个文件） | 同上，旧 assets 一直攒着 | 用 `_clean_wwwroot.py` 分批删；真实产物只有 ~2.5 MB |
| 安装程序体积异常大（>200 MB） | `payload.zip` 里混进了上一版残留 | 先确认 `dist-h5` / `wwwroot` 是干净的再打包 |
| 卸载后安装目录还在 | 卸载器自己就在这个目录里，删不掉自身 | 已处理：清完其它文件后用 `cmd /c ping -n 3 & rmdir` 延迟删 |
