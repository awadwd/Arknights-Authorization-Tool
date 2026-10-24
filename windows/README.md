# 通行证查询 · Windows 版（WinUI 3）

用 WinUI 3 做外壳、WebView2 承载现有 uni-app H5 产物的桌面版。
业务代码零重写，Windows 侧只负责窗口、导航、系统集成。

## 目录

```
ak-passport-win/
├─ src/AkPassport.Win/          WinUI 3 外壳（C#）
│  ├─ MainWindow.xaml(.cs)      窗口、WebView2、侧边栏导航
│  ├─ Services/
│  │   ├─ LocalProxy.cs         /gitcode 反向代理（补 CORS）
│  │   ├─ WindowStateStore.cs   窗口位置/尺寸记忆
│  │   ├─ TrayService.cs        托盘图标与右键菜单
│  │   ├─ HotkeyService.cs      Ctrl+Alt+A 全局快捷键
│  │   ├─ SingleInstance.cs     单实例 + 唤回已有窗口
│  │   ├─ StartupService.cs     开机自启（HKCU Run）
│  │   ├─ AppPaths.cs           本地路径约定
│  │   └─ Diag.cs               诊断日志
│  └─ wwwroot/                  H5 产物（由前端编译后拷入）
├─ docs/                        打包与前端更新说明
│  ├─ BUILD-RELEASE.md          发布流程 + 冒烟清单 + 参数理由
│  ├─ UPDATE-VUE.md             以后改前端怎么更新到桌面版
│  └─ 使用说明.txt              随包发给用户的说明
├─ release.sh                   一键出 Beta 包
├─ _build_h5.sh                 前端编译 + 同步 wwwroot
├─ dist/                        发布产物（zip）
└─ dist-h5/                     前端编译输出（临时）
```

前端源码就在本仓库根目录（`windows/` 的上一层），Windows 专属页面：
`pages/desktop_home/desktop_home.vue`。如果前端工程放在别处，用
`FRONTEND_SRC=/path/to/通行证查询app bash _build_h5.sh` 覆盖。

## 开发

```bash
cd src/AkPassport.Win
dotnet build -c Debug -p:Platform=x64
# 产物：bin/x64/Debug/net10.0-windows10.0.26100.0/AkPassport.Win.exe
```

调试参数：

```bash
AkPassport.Win.exe --route=pages/list/list   # 直接落到指定页面
AkPassport.Win.exe --tray                    # 静默启动到托盘
```

## 更新前端产物

用 HBuilderX 内置编译器（无需另装 @dcloudio）——已封装成脚本：

```bash
bash _build_h5.sh
```

会把产物同步到 `src/AkPassport.Win/wwwroot` **和** `bin/.../Debug/.../wwwroot`（两份都要，否则会「改完没变化」）。
注意事项见 [`docs/UPDATE-VUE.md`](docs/UPDATE-VUE.md)。

## 发布

一行出包：

```bash
bash release.sh
```

一次出两样：

```
dist/
├─ 方舟通行证谷子查询工具-<版本>-安装程序.exe   # 单文件安装器（免管理员，约 135 MB）
└─ AkPassport-Win-<版本>-win-x64.zip           # 绿色版，解压即用（约 86 MB）
```

安装器源码在 `installer/AkPassportSetup`（WinForms，装到 `%LOCALAPPDATA%\Programs\`，
卸载入口写在 HKCU 的 Uninstall 键里，「设置 → 应用」可直接卸载）。
单独改界面：`dotnet build installer/AkPassportSetup/AkPassportSetup.csproj`
（没有 `payload.zip` 时「安装」按钮会自己置灰，方便单独调 UI）。

> **主程序不要用 `PublishSingleFile`**：单文件模式会漏掉 `wwwroot` / `Assets`，装完打开是白窗。
> 安装器反而要用单文件（它要把 payload 内嵌，图的是单文件分发）。
> 细节见 `docs/BUILD-RELEASE.md`。

版本号改 `src/AkPassport.Win/AkPassport.Win.csproj` 的 `<Version>`（脚本从这里读，不要改脚本）。

- 打包细节、参数理由、发布前冒烟清单 → [`docs/BUILD-RELEASE.md`](docs/BUILD-RELEASE.md)
- 以后更新 Vue / 前端 → [`docs/UPDATE-VUE.md`](docs/UPDATE-VUE.md)

> WebView2 运行时由系统或 Edge 提供，Win11 自带；Win10 需安装
> [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)。

## 关键约定

- **WebView2 用户数据目录固定**在 `%LOCALAPPDATA%\AkPassport\WebView2`，
  换目录会导致 localStorage（收藏、设置、抽卡记录）全部丢失。
- **内容画布宽度**按页面类型区分：首页用宽画布，工具页限制在 640 DIP。
  uni-app 的 `rpx` 跟随视口宽度线性放大，不限制会把移动端布局放大 1.5 倍。
- **页面路由**走 `location.hash`（uni-app H5 为 hash 模式），
  侧边栏与页面内跳转双向同步。
- **托盘图标必须用 `System.Drawing.Icon` 同步构造**：`IconSource` 走异步解码，
  紧跟其后的 `ForceCreate()` 拿不到图 → 托盘里是空白占位。
- **首次运行向导**：`Views/SetupWizard.cs`（ContentDialog），勾选项是真开关；
  **开始菜单快捷方式属于「安装动作」，不询问、自动创建**（`ShortcutService.CreateStartMenu`）。
  走完或点「稍后再说」都写 `SetupCompleted=true`，不再纠缠；设置页可重新运行。
- 诊断日志：`%LOCALAPPDATA%\AkPassport\shell.log`。

## 已知限制

- 工具页目前仍是移动端布局（居中画布），只有首页是真正的桌面布局。
- `pages/scan_barcode` 依赖摄像头扫码，桌面端不可用。
- 桌面小组件（ak-box-widget）为 Android 专属，Windows 版没有对应实现。
