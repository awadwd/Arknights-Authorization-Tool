# 明日方舟通行证查询工具（开源版）

[![UniApp](https://img.shields.io/badge/UniApp-v3.97+-blue)](https://uniapp.dcloud.net.cn/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

基于 UniApp 的明日方舟谷子（周边）通行证查询工具，支持微信小程序、H5、App 多端。

支持查询盲盒/亚克力挂件/立牌等多种周边商品的详细信息、价格与稀有度，并提供「今日宜挂」抽签、「模拟抽卡」等趣味功能。

---

## 部署方法

### 前提条件
- [HBuilderX](https://www.dcloud.io/hbuilderx.html)（最新版本）
- Node.js（用于 npm install）

### 步骤

```bash
# 1. 克隆仓库
git clone https://github.com/awadwd/Arknights-Authorization-Tool.git
cd Arknights-Authorization-Tool

# 2. 安装依赖（如果 package.json 存在）
npm install

# 3. 用 HBuilderX 打开项目根目录

# 4. 运行
# - 微信小程序：发行 -> 微信小程序
# - H5：运行 -> 运行到浏览器 -> Chrome
# - App：运行 -> 运行到手机或模拟器 -> 选择你的设备

# 5. 发行前请先阅读下方「配置要求」
```

---

## 配置要求

### 1. 微信小程序 AppID

编辑 `manifest.json`，将以下占位符替换为你的小程序 AppID：

```json
{
  "appid": "__UNI__XXXXXXX",   // 替换为你的微信小程序 AppID
  ...
}
```

> 如果使用"微信开发者工具"导入，请在导入时直接填写 AppID。

### 2. 知晓云后台配置（可选，启用高级功能时需要）

部分高级功能（如英文名/外号搜索、预测通行证数据）依赖知晓云（KnowCloud）实时数据服务。

编辑以下文件，将 `YOUR_KNOW_CLOUD_CLIENT_ID` 替换为你的知晓云 ClientID：

- `pages/index/index.vue`（顶部 `getClientId()` 函数）
- `pages/Search/Search.vue`（顶部 `KNOW_CLOUD_CONFIG`）
- `pages/list/list.vue`（顶部 `KNOW_CLOUD_CONFIG`）
- `packageA/Setting/Setting.vue`（顶部 `KNOW_CLOUD_CONFIG`）
- `packageA/more-notice/more-notice.vue`（顶部 `KNOW_CLOUD_CONFIG`）
- `pages/scan_barcode/scan_barcode.vue`（扫码功能）

**不填写知晓云 ClientID 也能正常使用**，工具默认会使用 GitHub/GitCode 镜像作为数据源。

### 3. 服务器域名配置（微信小程序）

如果使用微信小程序，在 [微信公众平台](https://mp.weixin.qq.com/) -> 开发 -> 开发管理 -> 服务器域名 中添加以下合法域名：

| 域名 | 用途 |
|------|------|
| `https://cdn.jsdelivr.net` | jsDelivr CDN（镜像数据源）|
| `https://raw.githubusercontent.com` | GitHub Raw |
| `https://raw.gitcode.com` | GitCode Raw |
| `https://api.github.com` | GitHub API |

> 注意：知晓云数据使用 `uni.request`（非 `downloadFile`），归属 **request 合法域名**，无需额外配置。

### 4. 数据更新

工具默认从 GitHub 镜像获取数据，无需手动更新。知晓云用户可在 Setting 页面手动触发数据刷新。

### 5. daily_recommend页面开发

如果您需要对daily_recommend页面的随机抽取逻辑、Theme列表进行了修改,需要使用TestTool下的工具进行重跑概率;确保概率基本无过大偏差即可~

---

## 线上版本与开源版本差异

本仓库为**开源版本**，与线上实际运行的版本存在以下差异：

| 功能 | 开源版本 | 线上版本 |
|------|----------|----------|
| 主数据源 | GitHub/GitCode 镜像 | 知晓云（实时推送，更新更快）|
| 错误日志收集 | ❌ 无 | ✅ 完整日志系统（Setting 页面可查看/清空）|
| 数据更新频率 | 依赖 GitHub 同步（可能有延迟）| 知晓云实时更新 |
| 已知问题修复 | 跟随仓库更新 | 线上先行修复 |
| Python 辅助脚本 | ✅ 包含乱码修复等脚本 | ❌ 不含（内部使用）|

线上版本可能包含尚未同步到本仓库的最新修复和功能调整，欢迎提交 PR 或 Issue。

---

## 技术栈

- **框架**：UniApp (Vue 3)
- **数据源**：GitHub/GitCode Mirror + 可选知晓云
- **构建工具**：Vite
- **样式**：SCSS
- **许可证**：MIT

---

## 数据来源

- 通行证周边数据来源：[awadwd/ArknightsAuthorization-Series-mirror](https://github.com/awadwd/ArknightsAuthorization_Series-mirror)
- 感谢所有贡献者

---

## 许可证

[MIT License](LICENSE)
