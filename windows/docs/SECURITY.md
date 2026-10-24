# 安全说明与自查指南

> 面向：安全审查者、首次接触本项目的用户、以及未来维护者。
>
> 这份文档把项目里**每一个看起来"像后门 / 像漏洞"的功能**摊开说明：它做什么、为什么要这么做、边界在哪、怎么自己验证。
> 参考背景：2026-10 [关于 PCL 存在所谓「后门 / 漏洞」的说明](https://www.bilibili.com/opus/1262353478882164752)。那件事的核心争议是
> **「服务端下发的配置能改到可执行命令的设置项」**——远程配置 + 可执行动作的组合。本项目的设计原则就是彻底避开这个组合。

---

## 一、四条硬性设计原则

| # | 原则 | 在代码里的体现 |
|---|---|---|
| 1 | **不做远程配置下发** | 程序的所有设置只来自本地 `%LOCALAPPDATA%\AkPassport\preferences.json` 与页面 localStorage。没有任何"服务端告诉我该用哪个值"的通道。 |
| 2 | **不执行远程下发的代码或命令** | 全工程没有可被远程触发的命令执行入口；自动更新落地的是"新版本的程序文件"，不是"远程指令"（见第二节第 7 条）。注入到页面的脚本全是编译期常量（`Services/DesktopPatches.cs`）。 |
| 3 | **不收集、不上传用户数据** | 没有任何埋点、统计、设备指纹上报。所有出站请求都是"取数据"，见第五节清单。 |
| 4 | **能力按需开启、用完即关** | 联机端口只在房主点「创建房间」时监听，离开即关闭（15 分钟兜底）。 |

> 一句话：**这个程序不会因为"我们服务器说了什么"而改变本机行为，更不会去执行任何东西。**

---

## 二、容易被误会的功能（逐条）

### 1. 本地请求代理（`Services/LocalProxy.cs`、`Services/RemoteProxy.cs`）

**看起来像**：一个"中间人"，能把页面请求转发出去。

**实际是**：WebView2 的 `WebResourceRequested` 拦截，**没有监听任何端口**，只在进程内把几个固定域名的跨域请求转一手，补上 CORS 头。

```
允许转发的域名（硬编码，改一行代码才能变）：
  https://raw.gitcode.com/
  https://cdn.jsdelivr.net/
  https://fastly.jsdelivr.net/
  https://raw.githubusercontent.com/
  https://*.myminapp.com/        ← 知晓云租户，数据版本/公告
```

**边界**：
- 只发 `GET`（`HttpMethod.Get`），页面就算想 POST 也发不出去；
- 只转发白名单请求头（`Accept` / `Content-Type` / `Authorization` 等），`Host`、`Origin`、`Cookie` 一律不带；
- 不落盘、不记录请求体。

---

### 2. 注入到页面的脚本（`Services/DesktopPatches.cs`）

**看起来像**："程序会往网页里注入 JS"。

**实际是**：6 段**编译期写死**的字符串，通过 `AddScriptToExecuteOnDocumentCreatedAsync` 注册，作用是：
1. 把 uni-app 的 rpx 根字号钉到 18px（否则桌面端字体放大 2 倍）；
2. 禁止 Ctrl+滚轮 / 捏合缩放；
3. 顶层页隐藏 H5 自带的标题栏；
4. 轮询 `localStorage.themeMode` 并报给外壳换色。

**边界**：脚本内容不来自网络、不来自配置文件、不含用户输入。

---

### 3. 页面导航（`MainWindow.OnNavigationStarting`）

**看起来像**：壳里嵌了个浏览器，能打开任意网站。

**实际是**：白名单之外一律 `Cancel` 并交给系统浏览器。允许的内部地址只有：

```
https://app.local/**     ← 虚拟域，映射到本地 wwwroot
about: / data: / blob:
```

这样既杜绝了"加载外部页面还往里注入脚本"的想象空间，也防止外链把用户带进一个没有侧边栏、退不回来的页面。

---

### 4. 联机对战服务（`Services/BattleServer.cs`，端口 23515）

这是**本次审查里唯一有真实暴露面的功能**，已做加固：

| 项目 | 之前 | 现在 |
|---|---|---|
| 监听时机 | 进程启动就监听 `0.0.0.0:23515` | **前端点「创建房间」才监听**，离开房间立即关闭 |
| 兜底 | 无 | 15 分钟无客户端自动关闭（页面刷新/前端异常时不留长尾） |
| `reset` 指令 | 任何连进来的人都能重置牌堆 | 只有房主（回环连接）能发 |
| 帧长度 | 无上限，64 位长度按 int 截断 | 单帧上限 64 KB，超限断开 |
| 连接数 | 无限制 | 同 IP 最多 4 条并发连接 |
| 昵称 | 不限长度，原样广播 | 24 字符上限 |
| 房主资格 | —— | 只有回环地址（壳内页面）能当房主，远端一律只能是挑战者 |

**仍存在的已知限制**（写在这里而不是藏着）：
- 协议本身**没有房间口令**。房主开房期间，同一网络里知道 `IP:23515` 的人可以抢占挑战者位。**只在对局期间开房，打完就离开**。
- 局域网 / 异地联机需要房主侧网络可达（端口映射、虚拟局域网或内网穿透），这部分安全由用户自己的网络环境决定。

---

### 5. 全局快捷键（`Services/HotkeyService.cs`）

**看起来像**：键盘钩子 / 键盘记录。

**实际是**：`RegisterHotKey` 注册了**唯一一个组合** `Ctrl + Alt + A`，只收 `WM_HOTKEY` 这一条消息。它拿不到任何按键内容，也无法记录输入。设置页里可随时关掉。

---

### 6. 开机自启（`Services/StartupService.cs`）

**看起来像**：偷偷驻留。

**实际是**：写 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 一个值，指向自己的 exe 加 `--tray`。**默认关闭**，设置页「开机自动启动」开关控制，关掉就删值。不写计划任务、不申请管理员权限。

---

### 7. 自动更新（`Services/UpdateService.cs` + `AkPassport.Updater.exe`）

**看起来像**：远程更新通道 = 后门。

**实际是**：三条边界都卡死 —— 只认固定仓库、只覆盖程序目录、不执行任何远程代码。

**检查阶段**
- 请求：`GET https://api.github.com/repos/awadwd/Arknights-Authorization-Tool/releases?per_page=10`
- 只认**带 Windows 绿色包（名字含 `Win` 的 `.zip`）**的发布——同仓库还有安卓版 `v1.50.43`，版本体系不同，不隔离会永远误报
- 只读元数据（版本号、说明、下载地址、sha256），不改本机任何设置

**下载阶段**
- 线路：GitHub 官方直链 `browser_download_url` 优先，失败后按顺序尝试第三方加速前缀（`ghfast.top` / `gh-proxy.com`，只是给原地址加前缀，不改内容）
- 落在 `%LOCALAPPDATA%\AkPassport\update\`，**不落在程序目录**
- 有 `sha256` 就校验，不通过自动换下一个线路；都失败就停，不会拿半个包去覆盖

**安装阶段**
- 交给同目录的 `AkPassport.Updater.exe`（本仓库 `src/AkPassport.Updater`，源码可读）
- 参数固定：`--pid <主进程> --zip <包> --dir <安装目录> --exe <主程序> --log <日志>`
- 更新器**不联网**、只解析这五个参数、只操作 `--dir` 指定的目录
- 解压后会先确认包里确实有 `AkPassport.Win.exe`，否则不覆盖（防止拿错包把目录洗掉）
- 覆盖前把将被替换的文件复制到 `.update\backup`，中途失败会回滚
- 全程必须用户点「下载并安装」并确认一次；没有静默安装

> 和 PCL 争议点的区别：那里是**远程配置去改"启动前执行命令"**；这里是**远程只提供一个完整版本包的地址**，
> 包内容仍需按版本号与 sha256 校验，最终落地的还是程序自己的文件。前者能改变本机行为，后者不能。

---

### 8. 桌面小组件 / 托盘图标（`Services/TrayService.cs`、`uni_modules/ak-box-widget`）

本地 AppWidget（Android 侧）与 Windows 托盘，读写的是**本机**偏好和抽卡记录，不联网。

---

## 三、出站请求完整清单

`grep -rhoE "https?://[^\"]+" --include=*.cs` 的结果，逐条说明：

| 地址 | 用途 | 触发时机 |
|---|---|---|
| `https://app.local/**` | 虚拟域，读本地 `wwwroot` | 页面加载 |
| `https://raw.gitcode.com/...` | 通行证/干员数据（JSON） | 打开查询页、手动刷新 |
| `https://*.myminapp.com/hserve/v2.2` | 知晓云：数据版本、公告 | 启动、检查数据更新 |
| `https://cdn.jsdelivr.net/` `https://fastly.jsdelivr.net/` | 静态资源（备用 CDN） | 页面按需 |
| `https://raw.githubusercontent.com/` | 备用数据源 | 按需 |
| `https://api.github.com/repos/.../releases` | 检查程序更新 | 启动 12 秒后（可关）/ 进设置页 / 手动点检查 |
| `https://github.com/awadwd/.../releases/download/...` | 下载更新包（直链） | 仅用户点「下载并安装」 |
| `https://ghfast.top/` `https://gh-proxy.com/` | 更新包加速线路（仅前缀拼接原地址） | 直链失败时才用 |
| `https://github.com/awadwd/...` | 项目主页 / 发布页 | 手动点击 |

**没有**：统计 SDK、崩溃上报、设备指纹、剪贴板读取、通讯录/文件遍历。

---

## 四、自己验证（不用信我）

```bash
# 1. 有没有偷偷监听端口？（正常应只有 23515，且只在开房期间）
netstat -ano | findstr LISTENING | findstr <程序PID>

# 2. 有没有偷偷连外网？（开房时除外）
netstat -ano | findstr ESTABLISHED | findstr <程序PID>

# 3. 有没有执行外部程序？（全工程搜，应该零结果）
grep -rn "Process.Start" src/

# 4. 有没有远程脚本？
grep -rn "AddScriptToExecuteOnDocumentCreated" src/   # 逐个看内容，全是常量

# 5. 运行日志（记录了代理请求、联机事件、异常，不含用户数据）
type "%LOCALAPPDATA%\AkPassport\shell.log"
```

---

## 五、和 PCL 那件事的区别

| 维度 | PCL 争议点 | 本项目 |
|---|---|---|
| 远程能改什么 | 服务端配置可改到「启动前执行命令」 | 远程只能被**读**（版本号、数据 JSON），改不了本机任何设置 |
| 可执行动作 | 配置项能触发任意命令 | 无任何命令执行入口 |
| 更新方式 | —— | 检查 → 下载完整版本包（sha256 校验）→ 独立更新器覆盖，必须用户确认 |
| 用户可见性 | 配置下发用户不可见 | 所有设置项都在设置页，本地 JSON 可直接查看 |

---

## 六、如果你仍不放心

1. 用 [Process Monitor](https://learn.microsoft.com/sysinternals/downloads/procmon) 过滤本程序的 `Process Create` / `TCP Connect` 事件，跑一遍主要功能；
2. 用 `netstat` 对照第三节的清单；
3. 代码全部开源，`grep` 第三节那几条命令即可自证；
4. 发现问题请走 GitHub Issues（附复现步骤），而不是先发视频——按 PCL 作者的原话说，绕过作者直接公开对任何项目都是不负责的。
