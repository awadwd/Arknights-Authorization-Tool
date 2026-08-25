daily_recommend 抽签概率验证脚本
================================

## 文件清单
- recommend_probability_test.js  测试脚本 (Node.js)
- recommendDB.json              抽签分类配置 (从 daily_recommend.vue 抽取)
- Box_Id.json (可选)           真实盒号数据, 放在脚本同目录会自动加载

## 如何运行

### 1. 推荐: 用真实数据
把 daily_recommend 页面下载的 Box_Id.json 放到本目录 (与 .js 同目录),
然后运行:
    node recommend_probability_test.js 10000

输出 10000 次抽取的池命中率。

### 2. 没有真实数据
脚本会自动用 92 个盒号 + 63 个常用干员占位。
注意: 占位数据下很多分类的 theme/others 池会被过滤空,
导致 random 命中率虚高 (这是数据问题, 不是逻辑问题)。
拿到真实 Box_Id.json 后会准确反映线上概率。

### 3. 修改抽取次数
    node recommend_probability_test.js 50000
    node recommend_probability_test.js 100000

## 输出说明

测试 1: 每个分类的池命中率
  - theme/others/random 应接近 33.33%
  - 彩蛋 (lucky) 应接近 0.01%

测试 2: 全局聚合 (所有分类合计)
  - 真实数据下应大致:
    theme 33.33%
    others 33.33%
    random 33.34%
    彩蛋 0.01%

测试 3: 模拟用户场景 - 固定一个分类连续抽 20 次
  - 默认固定 yishu (艺术) 这个池最完整的分类
  - 模拟"再来一次"按钮的 _excludeNames 去重机制

## 如何获取 recommendDB.json

如果你修改了 daily_recommend.vue 里的 recommendDB, 需要重新生成:
可以运行我之前用过的 Python 抽取脚本, 或者手动从 vue 文件里
找到 recommendDB: { ... } 这一大块 JSON 复制出来。

## 如何获取真实 Box_Id.json

1. 在微信开发者工具里运行项目
2. daily_recommend 页面触发下载数据
3. 微信开发者工具控制台执行:
   const data = wx.getStorageSync('arknightsData');
   console.log(JSON.stringify(data));
4. 复制 console 输出到 Box_Id.json

或者直接从 GitCode Raw / GitHub Raw 下载:
https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series/raw/main/Box_Id.json
