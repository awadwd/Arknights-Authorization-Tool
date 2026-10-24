# 以后更新 Vue（前端）怎么做

> Windows 版**没有**自己的一套界面代码。它只是把手机端的 H5 产物装进 WinUI 外壳里。
> 所以「更新 Vue」= 改前端工程 → 重新编译 H5 → 覆盖外壳里的 `wwwroot` → 重新打包。

---

## 0. 两个目录，别搞混

| 角色 | 路径 |
|---|---|
| 前端源码（Vue / uni-app） | 仓库根目录（`windows/` 的上一层），可用 `FRONTEND_SRC` 覆盖 |
| Windows 外壳（C# / WinUI 3） | `windows/` |

日常改界面只动**前端源码**；`windows/src/AkPassport.Win/wwwroot` 是产物，**不要手改**（下次编译就被覆盖）。

---

## 1. 改完前端，编译 H5

```bash
cd windows
bash _build_h5.sh
```

它做三件事：

1. 用 HBuilderX 内置编译器把前端编译到 `windows/dist-h5`
2. 覆盖同步到 `src/AkPassport.Win/wwwroot`
3. **再同步一份到 `src/.../bin/x64/Debug/.../wwwroot`**

> 第 3 步不能省。Debug 运行时读的是 `bin` 下那份，只同步 `src` 会出现
> 「明明改完了，界面没变」的假象 —— 这个坑踩过。

然后重开壳（或让它自动重载）就能看到。

---

## 2. 只想看页面效果，不启动壳

```bash
cd windows/dist-h5
python -m http.server 8900 --bind 127.0.0.1
# 浏览器打开 http://127.0.0.1:8900/index.html#/pages/Search/Search
```

**注意**：浏览器里没有壳注入的 `html{font-size:18px}`，rpx 会随窗口宽度缩放，
两栏布局的断点也不一样（见第 4 节）。**布局问题必须在壳里看，不能只看浏览器。**

---

## 3. 在壳里调试（推荐）

```bash
# 编译 + 启动到指定路由
bash _dev.sh pages/Search/Search _shot.png 9
```

- Debug 构建会开 CDP：`http://127.0.0.1:9222`
- 常用脚本（都在 `windows/`）：

| 脚本 | 用途 |
|---|---|
| `_cdp_eval.js "<js>"` | 在壳里执行 JS 并取回结果（最常用） |
| `_cdp_shot.js out.png` | 壳内截图 |
| `_cdp_click.js click-picker` | CDP 真实鼠标事件（picker 这类浮层必须用它，`element.click()` 不可靠） |
| `_probe_*.js` | 历史探针，按需复制改造 |
| `_check_sfc.js <file.vue>` | 改完 `.vue` 先做 SFC 静态校验（标签配平 / 编译错误） |

日志：`%LOCALAPPDATA%\AkPassport\shell.log`。

---

## 4. 改了界面就必须知道的约定

### 4.1 条件编译：`.js` / `.vue` 里只能用 `APP-PLUS`

`APP-ANDROID` / `APP-IOS` **只在 `.uts` 文件里生效**。写进 `.vue` / `.js` 会被判为未知平台，
`#ifdef` 里的内容**静默丢弃**、`#ifndef` 里的内容**反而保留**，编译不报错。

要区分安卓/iOS，用运行时判断：`uni.getSystemInfoSync().platform`。
**验证方式：去产物里 grep 关键字**，别靠肉眼读源码。

### 4.2 H5 里 `wx` 和 `uni.showShareMenu` 都不存在

`onLoad` 里直接调用会抛异常，**后面所有代码都不执行**（表现：页面主题不加载、数据不加载）。
已经做了兜底：

- `utils/wxShim.js` 补 `wx.showShareMenu` / `wx.hideShareMenu`
- 所有 `uni.showShareMenu(...)` 必须写成 `uni.showShareMenu && uni.showShareMenu(...)`

新写的页面照这个规矩来。

### 4.3 新页面必须进主题白名单

`loadThemeSetting()` 里的 `||` 链和数组、加上 21 页的路由名清单，都要同步加。
漏了就是「这个页面切换主题不生效」。

### 4.4 新块的根节点要在 `.container` 里面

页面左右边距全部来自 `.container{padding:30rpx}`。
写到容器闭合标签后面 → 贴屏幕边缘、没有圆角卡片。
**用 `@vue/compiler-sfc` 的 AST 查父子关系**，标签配平过了也发现不了这个问题。

### 4.5 液态主题会把「渐变底」压平

`common/liquid-theme.css` 对 `.notice-section` 等一批区块强制：

```css
background-color: var(--lg-surface) !important;
background-image: none !important;
```

如果某块原本是「渐变 + 白字」，白字就会**没人管** → 白底白字。
正确做法是**在页面自己的 scoped 样式里**补文字色：

```css
.theme-liquid .notice-section .notice-title,
.theme-liquidDark .notice-section .notice-title { color: var(--ak-text-primary); }
.theme-liquid .notice-section .notice-text,
.theme-liquidDark .notice-section .notice-text { color: var(--ak-text-secondary); }
```

不要去改 `common/liquid-theme.css`（那是全站主题层）。

### 4.6 picker 在自定义弹层里会被盖住

uni 的 `.uni-picker-container` 是 `z-index:999`，项目里自定义弹层普遍 `10000`。
`App.vue` 已全局抬到 `10001`。**新写弹层如果 z-index 再往上加，要同步抬 picker。**

### 4.7 一页两功能 → 宽屏两栏

用 `utils/wideLayout.js` mixin：

```js
mixins: [wideLayout]
```

```html
<view v-if="!wideLayout" class="mode-switch">…</view>
<view class="split" :class="{ 'is-wide': wideLayout }">
  <view v-if="wideLayout || mode === 'a'">A</view>
  <view v-if="wideLayout || mode === 'b'">B</view>
</view>
```

**断点分环境**（这点很关键）：

- 壳里：`ScaleScript` 把根字号钉死 18px，rpx 不随窗口放大 → 720px 就能并排
- 纯浏览器/手机：<960px 时 uni-app H5 按「750rpx = 满屏」等比放大整页 → 两栏会被挤爆

所以 `wideLayout` 用 `window.chrome.webview` 判断运行环境：壳 720、其它 960。

### 4.8 其它零碎但会被打回的

- 链接统一走 `utils/openLink.js`，禁止散写 `uni.setClipboardData`
- 禁止裸 `uni.vibrateShort()`（iOS 是系统通知震动）→ 用 `utils/haptic.js`
- App 端 `scroll-view` 必须有确定高度
- `.tool-card` 不能加 `overflow:hidden`（角标在卡片外）
- 改 `.vue` 用 Python 按 `newline=''` 读写，**别用会把整文件换成 LF 的编辑器**（工程是 CRLF）
- 不要运行时抓 PRTS（403），干员属性用离线 `charMeta.js`

---

## 5. 改完的标准收尾

```bash
# 1. 静态校验改过的 .vue
node _check_sfc.js "<file.vue>"

# 2. 编译 + 同步
bash _build_h5.sh

# 3. 壳里看一眼（关键页面）
bash _dev.sh pages/Search/Search _check.png 9
```

要出包就跑 `bash release.sh`，冒烟清单见 `docs/BUILD-RELEASE.md` 第 3 节。

---

## 6. 回滚

源码用 git 回滚 → 重跑 `_build_h5.sh` → 重新打包。
**不要**去手改 `wwwroot` 里的文件「快速修一下」，下一次编译全部丢失，而且产物与源码不一致后很难查。
