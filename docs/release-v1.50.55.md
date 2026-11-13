# v1.50.55

修 1.50.54 反馈的两个硬 bug，音乐条改为全站常驻。

## 修复

**1. 安卓「测试通知」报 `i is not a constructor`，通知一条也弹不出来**

`plus.android.importClass('android.app.Notification.Builder')` 拿到的是 `null` ——
`Notification.Builder` 是 Java 的静态内部类，反射名必须用 `$`：
`android.app.Notification$Builder`。拿到 null 再 `new`，压缩后就成了
「i is not a constructor」。

现在改为：
- 首选 `plus.push.createMessage()`（官方本地消息，不需要任何推送服务/厂商通道）；
- 只有它不可用时才退回反射，且用 `$` 写法，小图标也包了 try（部分 ROM 拿应用图标当小图标会崩）。

**2. 设置里找不到填 PushPlus token 的位置**

输入框原本挂在 `notifyCaps.pushplus` 这个能力位上，一旦平台判定异常整行就不渲染。
现在只要通知总开关开着就显示（仅小程序端隐藏，请求域名不在白名单里填了也发不出去），
并加了「已保存/未填写」状态行，输入框加了描边，失焦或键盘回车都会保存。

**3. Windows：每次更新成功后更新器会把自己删掉**

`AkPassport.Updater` 收尾无条件 `SelfDelete`，结果每成功更新一次
`AkPassport.Updater.exe` 就消失一次，下一次点更新必然报
「更新程序缺失（AkPassport.Updater.exe）」。

现在只有「自己不在安装目录内」（临时副本）时才自删，安装目录里那份保留。

> 已装过的机器如果现在缺这个文件：用 1.50.55 的安装程序覆盖装一次即可，
> 之后就能一直走应用内更新了。

## 音乐条

- 从「只挂首页」改成**全站页面都挂**（25 个页面）。播放器改为模块级单例，
  切页面不会再断歌，回到旧页面时进度/按钮每秒同步一次，不会停在过期状态。
- 播放条右侧加了 `×`：点了静默收起并写回开关为「关」，**不弹任何「去哪里再打开」的提示**。
- 设置页里的「首页音乐条」开关，回到设置页时会重新读一次存储 —— 在别处按 `×` 关掉后
  开关会同步成关，不会再出现「开关开着但条没了」。
- 截图/渲染类页面（看图、分享图、截图导出、桌面小组件预览）和设置页本身不挂，
  避免被截进图片里或盖住开关。

## 版本

- `versionName` 1.50.55，`versionCode` 166
- Windows：csproj 四个版本字段同步 1.50.55

## 出包

- 安卓：HBuilderX 云打包（自有证书，别名 `1`），**取消勾选开屏广告**；
  产物按规范命名 `com.ArknightsAuthorizationSeries1.50.55.apk`
- Windows：推 `v1.50.55` tag 由 CI 出 zip + Setup
- macOS：环境已预留（设置页开关 + 代理白名单），未出包
