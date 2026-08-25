/**
 * daily_recommend 抽签概率验证脚本
 * 用法: node recommend_probability_test.js [次数]
 * 默认 10000 次
 *
 * 这个脚本完全模拟 daily_recommend.vue 中 doRecommend() 的抽签逻辑,
 * 用随机生成的 Box_Id.json 占位数据来验证三种池(theme/others/外置随机)的实际命中率。
 */

const fs = require('fs');
const path = require('path');

// ============== 配置 ==============
const N = parseInt(process.argv[2] || '10000', 10);

// 模拟 Box_Id.json: 92 个盒号, 每个含 8 个干员
// 用 recommendDB 里出现过的干员名 + 一些常见明日方舟干员
const SAMPLE_NAMES = [
    "阿米娅", "博士", "凯尔希", "W", "温蒂", "铸铁", "苏苏洛", "香草", "翎羽",
    "巫恋", "百炼嘉", "年", "夕", "令", "黍", "嵯峨", "山", "泥岩",
    "森蚺", "瑕光", "归溟幽灵鲨", "濯尘芙蓉", "卡涅利安", "琴柳", "极光", "澄闪",
    "灵知", "缄默德克萨斯", "缄默", "鸿雪", "百炼", "深律", "白铁", "隐德来",
    "锏", "司霆", "赫德雷", "伊内丝", "霍尔海雅", "麦哲伦", "黑键", "多萝西",
    "和弦", "明椒", "正义骑士号", "艾拉", "耶拉", "赫德雷", "止颂", "维什戴尔",
    "维娜·维多利亚", "玫拉", "妮芙", "莫兰", "蜜莓", "纯烬艾雅法拉", "艾雅法拉",
    "塞壬歌", "夜刀", "梓兰", "史都华德", "杜林", "克洛丝"
];

// 模拟 generateBoxes - 用 SAMPLE_NAMES 填充 92 个盒号, 每个盒号 8 个干员
function buildMockBoxData() {
    const boxes = [];
    for (let boxIdx = 0; boxIdx < 92; boxIdx++) {
        const box = { Box_id: `盒号${boxIdx + 1}` };
        for (let i = 1; i <= 8; i++) {
            const nameIdx = Math.floor((boxIdx * 7 + i * 3) % SAMPLE_NAMES.length);
            box['character' + i] = { name: SAMPLE_NAMES[nameIdx] };
        }
        boxes.push(box);
    }
    return boxes;
}

// 加载 recommendDB
const DB_PATH = path.join(__dirname, 'recommendDB.json');
if (!fs.existsSync(DB_PATH)) {
    console.error('未找到 recommendDB.json, 请先运行 extract_recommendDB.py 抽取');
    console.error('或下载 daily_recommend.vue 后再跑本脚本');
    process.exit(1);
}
const recommendDB = JSON.parse(fs.readFileSync(DB_PATH, 'utf-8'));

// 加载 Box_Id.json (如果有)
let allBoxes = buildMockBoxData();
const BOX_PATH = path.join(__dirname, 'Box_Id.json');
if (fs.existsSync(BOX_PATH)) {
    try {
        const data = JSON.parse(fs.readFileSync(BOX_PATH, 'utf-8'));
        if (Array.isArray(data) && data.length > 0) {
            allBoxes = data;
            console.log(`使用真实 Box_Id.json (${allBoxes.length} 个盒号)`);
        }
    } catch (e) {
        console.log('Box_Id.json 解析失败, 使用占位数据');
    }
} else {
    console.log(`使用占位 Box_Id 数据 (${allBoxes.length} 个盒号, ${SAMPLE_NAMES.length} 个干员名)`);
}

// ============== 抽取函数 (复刻 doRecommend 逻辑) ==============
function pickOne(optionKey, excludeName = null) {
    const conf = recommendDB[optionKey];
    if (!conf) return { pool: 'invalid', name: null };

    // 彩蛋 (0.01%) - 当前 always-on
    const luckyP = 0.0001;
    if (Math.random() < luckyP) {
        return { pool: 'lucky', name: '彩蛋-运气爆棚' };
    }

    // 构建 allNames
    const allNames = new Set();
    for (const box of allBoxes) {
        for (let i = 1; i <= 8; i++) {
            const ch = box['character' + i];
            if (ch && ch.name) allNames.add(ch.name);
        }
    }

    const _excludeNames = new Set();
    if (excludeName) _excludeNames.add(excludeName);

    const banSet = new Set(conf.ban || []);
    const themeSet = new Set((conf.theme || []).map(c => c.name));
    const othersSet = new Set((conf.others || []).map(c => c.name));

    // 三个池配平: 33.33% / 33.33% / 33.34%
    const themeList = (conf.theme || []).filter(c => !banSet.has(c.name) && allNames.has(c.name) && !_excludeNames.has(c.name));
    const othersList = (conf.others || []).filter(c => !banSet.has(c.name) && allNames.has(c.name) && !_excludeNames.has(c.name));
    const randomPool = [...allNames].filter(n => !banSet.has(n) && !themeSet.has(n) && !othersSet.has(n) && !_excludeNames.has(n));

    const r = Math.random();
    if (r < 0.3333 && themeList.length) {
        return { pool: 'theme', name: themeList[Math.floor(Math.random() * themeList.length)].name };
    } else if (r < 0.6666 && othersList.length) {
        return { pool: 'others', name: othersList[Math.floor(Math.random() * othersList.length)].name };
    } else if (randomPool.length) {
        return { pool: 'random', name: randomPool[Math.floor(Math.random() * randomPool.length)] };
    } else if (themeList.length) {
        return { pool: 'theme-fallback', name: themeList[Math.floor(Math.random() * themeList.length)].name };
    } else if (othersList.length) {
        return { pool: 'others-fallback', name: othersList[Math.floor(Math.random() * othersList.length)].name };
    }
    return { pool: 'fallback', name: (conf.theme || [])[0]?.name || '???' };
}

// ============== 主测试 ==============
console.log(`\n=== daily_recommend 抽签概率测试 ===`);
console.log(`抽取次数: ${N}`);
console.log(`分类数: ${Object.keys(recommendDB).length}`);
console.log('');

// 测试 1: 每个分类的池命中率 (无去重)
console.log('--- 测试 1: 每个分类池命中率 ---');
const poolStats = {};
const expected = { theme: 0.3333, others: 0.3333, random: 0.3334 };

for (const optKey of Object.keys(recommendDB)) {
    poolStats[optKey] = { theme: 0, others: 0, random: 0, lucky: 0, fallback: 0, total: 0 };
}

for (let i = 0; i < N; i++) {
    const optKey = Object.keys(recommendDB)[Math.floor(Math.random() * Object.keys(recommendDB).length)];
    const result = pickOne(optKey);
    poolStats[optKey].total++;
    if (result.pool === 'theme' || result.pool === 'theme-fallback') poolStats[optKey].theme++;
    else if (result.pool === 'others' || result.pool === 'others-fallback') poolStats[optKey].others++;
    else if (result.pool === 'random') poolStats[optKey].random++;
    else if (result.pool === 'lucky') poolStats[optKey].lucky++;
    else poolStats[optKey].fallback++;
}

// 打印每个分类的统计
for (const optKey of Object.keys(recommendDB)) {
    const s = poolStats[optKey];
    const t = s.total;
    if (t === 0) continue;
    const themeP = (s.theme / t * 100).toFixed(2);
    const othersP = (s.others / t * 100).toFixed(2);
    const randomP = (s.random / t * 100).toFixed(2);
    const luckyP = (s.lucky / t * 100).toFixed(4);
    console.log(`[${optKey.padEnd(15)}] theme=${themeP}%  others=${othersP}%  random=${randomP}%  彩蛋=${luckyP}%  (n=${t})`);
}

// 测试 2: 全局聚合
console.log('\n--- 测试 2: 全局聚合 (所有分类合计) ---');
let total = { theme: 0, others: 0, random: 0, lucky: 0, fallback: 0 };
for (const optKey of Object.keys(recommendDB)) {
    const s = poolStats[optKey];
    total.theme += s.theme;
    total.others += s.others;
    total.random += s.random;
    total.lucky += s.lucky;
    total.fallback += s.fallback;
}
const sum = total.theme + total.others + total.random + total.lucky + total.fallback;
console.log(`总计: ${sum}`);
console.log(`  theme:  ${total.theme}  ${(total.theme / sum * 100).toFixed(2)}%  (期望 33.33%)`);
console.log(`  others: ${total.others}  ${(total.others / sum * 100).toFixed(2)}%  (期望 33.33%)`);
console.log(`  random: ${total.random}  ${(total.random / sum * 100).toFixed(2)}%  (期望 33.34%)`);
console.log(`  彩蛋:  ${total.lucky}  ${(total.lucky / sum * 100).toFixed(4)}%  (期望 0.01%)`);
console.log(`  fallback: ${total.fallback}  ${(total.fallback / sum * 100).toFixed(4)}%`);

// 测试 3: 模拟用户场景: 固定一个分类连续抽 20 次
console.log('\n--- 测试 3: 模拟用户场景 - 固定分类连续抽 N 次 ---');
// 选一个有真实数据的分类 (例如 yishu 艺术, theme=11 个)
const TEST_OPT = 'yishu';
const TEST_ROUNDS = 10000;
const TEST_DRAWS = 20;
const userPoolStats = { theme: 0, others: 0, random: 0, lucky: 0 };

for (let round = 0; round < TEST_ROUNDS; round++) {
    let currentResult = null;
    for (let i = 0; i < TEST_DRAWS; i++) {
        const result = pickOne(TEST_OPT, currentResult?.name);
        if (result.pool === 'theme' || result.pool === 'theme-fallback') userPoolStats.theme++;
        else if (result.pool === 'others' || result.pool === 'others-fallback') userPoolStats.others++;
        else if (result.pool === 'random') userPoolStats.random++;
        else if (result.pool === 'lucky') userPoolStats.lucky++;
        if (result.name && result.pool !== 'lucky') currentResult = result;
    }
}
const userSum = userPoolStats.theme + userPoolStats.others + userPoolStats.random + userPoolStats.lucky;
console.log(`分类=${TEST_OPT}, ${TEST_ROUNDS} 轮 x ${TEST_DRAWS} 次 = ${userSum} 次抽取`);
console.log(`  theme:  ${userPoolStats.theme}  ${(userPoolStats.theme / userSum * 100).toFixed(2)}%`);
console.log(`  others: ${userPoolStats.others}  ${(userPoolStats.others / userSum * 100).toFixed(2)}%`);
console.log(`  random: ${userPoolStats.random}  ${(userPoolStats.random / userSum * 100).toFixed(2)}%`);
console.log(`  彩蛋:  ${userPoolStats.lucky}  ${(userPoolStats.lucky / userSum * 100).toFixed(4)}%`);

console.log('\n=== 测试完成 ===');