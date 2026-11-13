# v1.9.2.8 变更说明与出包指引

> 本次改动横跨 uni-app 前端、Windows 壳（C#/WinUI3）、以及打包配置。
> 安卓包必须在 HBuilderX 里云打包（本地没有离线打包工程），Windows 包走 GitHub Actions。

## 一、这次改了什么

### 1. 安卓启动页

- 新增 `static/splash/{hdpi,xhdpi,xxhdpi}.png`（480×800 / 720×1280 / 1080×1920），
  深色工业底 + 暖橘光晕 + 图标 + 名称。
- `manifest.json`：
  - `app-plus.splashscreen.waiting` 由 `true` 改为 `false`（去掉启动图上那个菊花）；
  - `app-plus.distribute.splashscreen` 指向上面三张图，并设置
    `androidStyle: default`、`androidTranslucent: true`、`useOriginalMsgbox: false`。
- `androidTranslucent: true` 的作用是避免「先闪一下系统默认的白底 + 图标页」。
  截图里那版白底页就是 DCloud 的默认 splash，配好自定义图后不会再出现。
- 生成脚本：`scripts/make_splash.py`（改配色/文案后重跑即可）。

### 2. 通知

统一入口 `utils/notify.js`，设置页「通知与提醒」分组。

| 端 | 落地方式 | 需要做什么 |
|---|---|---|
| 安卓 | 系统通知栏，直接调 `NotificationManager`，不走厂商通道，离线也能弹 | Android 13+ 首次要给通知权限；设置页有「去开启」跳转 |
| 安卓/PC/网页 | PushPlus（微信服务号推送） | 在设置页填 pushplus.plus 的 token，点「测试推送」 |
| 微信小程序 | 一次性订阅消息 | 公众平台 → 订阅消息 → 选用模板，把模板 ID 填进 `utils/notify.js` 的 `MP_TMPL_IDS`；个人主体可用一次性订阅 |
| Windows | 系统 Toast（Windows App SDK `AppNotificationManager`） | 无需配置；未安装开始菜单快捷方式时注册会失败，安装器会建 |

触发点：`App.vue` 冷启动检查更新命中新版本时，会先落一条系统通知再弹 modal
（用户把 App 挂后台时不会错过）。

> 注意：PC 端「设置」打开的是 WinUI 原生设置页，Vue 版设置页在壳里被桥接拦截，
> 所以「通知与提醒」的表单目前只在手机 / 小程序 / 浏览器里可见；
> PC 端默认开启系统 Toast，版本更新通知照常工作。

### 3. 塞壬唱片（首页底部音乐条）

- 形态：**首页底部迷你播放条**，组件在 `components/ak-music-bar/ak-music-bar.vue`。
  点封面/曲名展开曲库列表，播放条上有上一首 / 播放暂停 / 下一首和进度条（可点跳）。
- **默认关闭**，需要在「设置 → 塞壬唱片 → 首页音乐条」打开（`switch`），
  开关落 storage `ak_music_bar_enabled`，首页 `onShow` 重新读，返回首页立即生效。
- 曲库：`static/music/catalog.json`，精选 **10 首**——
  生命流（明日方舟 OST1）、觉醒、Boiling Blood、Operation Pyrite / Blade（危机合约）、
  Renegade、Requiem、Ständchen、ManiFesto（音律联觉）、Everything's Alright。
  同名有 `(Instrumental)` 版的优先用纯音乐版。重新生成：`python scripts/build_music_catalog.py`
  （脚本里 `PICKS` 就是白名单，加歌往数组里补曲名即可）。
- 音源：播放前按 `cid` 调官方 `/api/song/<cid>` 现取（地址带签名、会过期，缓存 6 分钟）。
  **音频不随包分发、不转存**，全部由官方 CDN 直出。实测多数是 mp3（7～10MB），
  个别只给 wav（如「觉醒」43MB），播放器是流式加载、不会整包下完再放。
- 封面：`web.hycdn.cn` 有防盗链，带 `Referer: https://app.local/` 直接 403（实测）。
  所以**只在 App 端**加载官方封面，H5 / PC 壳 / 浏览器统一显示 ♪ 占位块，避免露白。
- 跨域：Windows 壳已把 `monster-siren.hypergryph.com` 加进 `RemoteProxy`；
  浏览器直开 H5 会提示「不允许跨域取音源」，属于预期。
- 小程序不启用（`#ifndef MP-WEIXIN` 把组件和 `catalog.json` 都挡在包外，主包体积零增长）。


### 4. 版本号

- `manifest.json`：`versionName` = `1.9.2.8`，`versionCode` = `1928`。
- `windows/src/AkPassport.Win/AkPassport.Win.csproj`：`Version` = `1.9.2.8`。
- **版本方案倒挂的处理**：纯数字比较里 `1.9 < 1.50`，老用户会被判成「已是最新」。
  前端 `utils/appUpdate.js` 和 C# `UpdateService.IsNewer` 都加了
  `1.50.* → 1.9.2.*` 的一次性放行，之后仍按正常语义比较。

## 二、出包

### 安卓（HBuilderX 云打包）

1. 用 HBuilderX 打开本工程 → 发行 → 原生 App-云打包。
2. 包名保持 `com.ArknightsAuthorizationSeries`（写死在 manifest 里，别改，改了会变成两个 App）。
3. 证书用原来的自有证书（**keystore 密码不入库**，见本地凭据或问项目作者）。
4. 版本号会从 manifest 读，应为 `1.9.2.8 / 1928`。
5. 打完包把 apk 传到 GitHub Release `v1.9.2.8` 下（App 内更新只认带 `.apk` 附件的正式版）。

### Windows（GitHub Actions）

- 推 tag `v1.9.2.8` 会触发 `.github/workflows/windows-release.yml`，
  产物是绿色版 zip + 安装程序。
- `wwwroot` 已经是本次重新构建的 H5 产物，**不需要在 CI 里再编一次前端**。

### 微信小程序

- 直接上传体验版/正式版。主包体积：音乐条组件与 `static/music/catalog.json`
  全部用 `#ifndef MP-WEIXIN` 挡掉了，小程序包里不包含，设置页也不显示该开关。

## 三、待办 / 注意事项

- [ ] 公众平台配订阅消息模板 → 填 `MP_TMPL_IDS`，否则小程序端只显示说明文案。
- [ ] PushPlus token 由使用者在设置页自己填，不写死进包。
- [ ] 安卓 13+ 的通知权限：首次点「测试」没反应时，走设置页的「去开启」跳到系统通知设置。
- [ ] macOS 端本次未改：通知与播放器都走 H5，`macos/` 壳里的 WKWebView 播放音频本身没问题。
