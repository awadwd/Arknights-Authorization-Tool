<template>
  <view class="gn" :class="'gn-' + themeMode">

    <!-- ==================== 首页 ==================== -->
    <view class="gn-home" v-if="scene === 'home'" :style="{ paddingTop: topPad + 'px' }">
      <view class="hd-line">
        <view class="hd-back" @click="goBack">‹ 返回</view>
        <view class="hd-brand">
          <text class="hd-mark">NEXT</text>
          <text class="hd-en">ARKNIGHTS PASSPORT</text>
        </view>
      </view>

      <view class="gn-title">模拟抽卡<text>Next</text></view>
      <view class="gn-sub">一大盒 14 抽 · 拆盒洗匀 · 自己挑一格</view>

      <view class="pick">
        <view class="pick-row">
          <text class="pick-label">分类</text>
          <view class="chips">
            <view
              v-for="t in typeChips"
              :key="t.key"
              class="chip"
              :class="{ on: selType === t.key }"
              @click="pickType(t.key)"
            >{{ t.label }}</view>
          </view>
        </view>
        <view class="pick-row" v-if="!unsupported">
          <text class="pick-label">盒号</text>
          <view class="box-open" @click="sheetBox = true">
            <text class="box-open-id">{{ curBox ? curBox.id : '—' }}</text>
            <text class="box-open-meta">{{ curBox ? boxMeta : '选择盒号' }}</text>
            <text class="box-open-arrow">›</text>
          </view>
        </view>
      </view>

      <!-- 联动 / 音律联觉：版式不同，暂未适配 -->
      <view class="no-support" v-if="unsupported">
        <text class="ns-badge">{{ unsupportedLabel }}</text>
        <text class="ns-title">这一类还没适配 Next 版</text>
        <text class="ns-desc">{{ unsupportedDesc }}</text>
        <view class="ns-btn" @click="goStable">
          <text class="ns-btn-main">前往模拟抽卡稳定版</text>
          <text class="ns-btn-sub">稳定版支持全部款式</text>
        </view>
        <text class="ns-foot">常规款仍可在本页拆盒 · 14 抽洗匀自选</text>
      </view>

      <!-- 摊开的白礼盒 -->
      <view class="hero" v-if="!unsupported">
        <view class="hero-glow"></view>
        <view class="hero-box">
          <view class="hb-lid"></view>
          <view class="hb-face">
            <view class="hcol" v-for="(col, ci) in heroCols" :key="'h' + ci">
              <i v-for="(on, ni) in col" :key="'hi' + ci + ni" :class="{ off: !on }"></i>
            </view>
          </view>
        </view>
        <text class="hero-tag">{{ selType === 'mix' ? '常规款混抽' : '本期 · ' + (curBox ? curBox.id : '') }}</text>
      </view>

      <view class="draw-area" v-if="!unsupported">
        <view class="pool-stat">
          <text class="pool-stat-main">{{ remain }}<i>/</i>{{ total }}</text>
          <text class="pool-stat-label">本盒剩余 / 共 {{ total }} 包</text>
        </view>
        <view class="btn-draw" @click="draw">
          <text class="btn-draw-main">{{ remain > 0 ? '进 盒 挑 一 包' : '这 盒 空 了' }}</text>
          <text class="btn-draw-sub">{{ remain > 0 ? '¥25 · 自己选位置' : '点这里 · 换新盒' }}</text>
        </view>
        <view class="draw-links">
          <view class="link-btn" @click="openHistory">抽卡记录</view>
          <text class="dot">·</text>
          <view class="link-btn" @click="resetBox">开新盒</view>
          <text class="dot">·</text>
          <view class="link-btn" @click="pickType('mix')">超级大混池</view>
        </view>
      </view>

      <view class="home-foot">玩家自制模拟器 · 与鹰角网络官方无关 · 仅供娱乐</view>
    </view>

    <!-- ==================== 拆卡舞台 ==================== -->
    <view class="gn-stage" :class="{ 'stage-flash': flash }" v-else>
      <view class="stage-hud" :style="{ top: (sbh + 12) + 'px' }">
        <view class="hud-btn" @click="closeStage">✕</view>
        <text class="hud-mid">{{ hudText }}</text>
        <text class="hud-count">{{ remainLabel }}</text>
      </view>

      <view class="deck">

        <!-- 层 1：白色礼盒（盒顶撕拉封条 → 撕开后摊平成平面） -->
        <view
          class="case"
          :class="caseCls"
          @touchstart="topDown"
          @touchmove.stop.prevent="topMove"
          @touchend="topUp"
          @touchcancel="topUp"
        >
          <view class="case-lid">
            <view class="cl-face"></view>
            <!-- 撕口（封条撕走后露出的黑缝） -->
            <view class="ct-gap"></view>
            <!-- 盒顶撕拉封条（横向撕开） -->
            <view class="ct-rip" :style="ripStyle">
              <view class="ct-teeth top"></view>
              <view class="ct-teeth bot"></view>
              <text class="ct-rip-t">撕开此处</text>
              <view class="ct-grip"><i></i></view>
            </view>
            <text class="ct-mark">RHODES ISLAND</text>
          </view>
          <view class="case-body">
            <view class="case-inner">
              <view class="col" v-for="(col, ci) in caseCols" :key="'c' + ci">
                <view
                  v-for="s in col"
                  :key="'p' + s.i"
                  class="pk"
                  :class="{ taken: s.taken && pickedSlot !== s.i, picked: pickedSlot === s.i }"
                  @click="pickSlot(s.i)"
                >
                  <text class="pk-no">#{{ boxNo }}</text>
                  <view class="pk-lb"><text>通行认证</text></view>
                </view>
              </view>
            </view>
          </view>
          <text class="case-tip" v-if="phase === 'case' || phase === 'toptear'">
            {{ topPull > 0.02 ? '松手 · 撕开盒顶' : '横向撕开盒顶封条' }}
          </text>
        </view>

        <!-- 层 2：绿色镭射袋（摊平后 → 向上抽出卡） -->
        <view
          class="pouch"
          :class="pouchCls"
          :style="pouchStyle"
          @touchstart="pouchDown"
          @touchmove.stop.prevent="pouchMove"
          @touchend="pouchUp"
          @touchcancel="pouchUp"
        >
          <view class="p-foil"></view>
          <view class="p-tex"></view>
          <view class="p-shine"></view>
          <view class="p-seam-l"></view>
          <view class="p-seam-r"></view>

          <!-- 印刷 -->
          <view class="p-print">
            <!-- RHODES | ARKNIGHTS / ISLAND | AUTHORIZATION（黑墨两行） -->
            <view class="p-tick"></view>
            <view class="p-sub">
              <text>RHODES | ARKNIGHTS</text>
              <text>ISLAND | AUTHORIZATION</text>
            </view>

            <!-- 亮黄横条 -->
            <view class="p-band"></view>

            <!-- 明日方舟 · 朝陇山 -->
            <view class="p-brand">
              <text class="pb-ar">明日方舟</text>
              <view class="pb-co">
                <view class="pb-mascot"></view>
                <view class="pb-tag">
                  <text class="pb-cn">朝陇山</text>
                  <text class="pb-en">CHOEARTH</text>
                </view>
              </view>
            </view>

            <!-- 白标签：54 ｜ 通行认证 / 挂饰 ｜ ARKNIGHTS -->
            <view class="p-label">
              <text class="pl-no">{{ boxNo }}</text>
              <view class="pl-txt">
                <text class="pl-l1">通行认证<text class="pl-sm"> / 挂饰</text></text>
                <text class="pl-l2">ARKNIGHTS</text>
              </view>
            </view>

            <!-- 左下斜杠组 -->
            <view class="p-dash"><i></i><i></i><i></i><i></i></view>

            <text class="p-copy">© HYPERGRYPH</text>
          </view>

          <!-- 锯齿撕线 -->
          <view class="p-tear"></view>

          <!-- 撕口 -->
          <view class="p-mouth"><view class="pm-fiber"></view></view>
        </view>

        <!-- 层 3：通行证卡 -->
        <view class="card-wrap" :class="cardCls" :style="cardStyle" @click="tapCard">
          <view class="cw-card">
            <view class="face front">
              <view class="f-art">
                <image v-if="card.art" class="f-art-img" :src="card.art" mode="aspectFill" />
              </view>
              <view class="f-shade"></view>
              <view class="f-plate"></view>

              <!-- 左侧竖脊：竖排 RHODES ISLAND + code39 条码 -->
              <view class="f-spine">
                <text class="f-spine-t">RHODES ISLAND</text>
                <view class="f-barcode" :style="barcodeStyle"></view>
              </view>

              <view class="f-top">
                <text>ARKNIGHTS</text>
                <text>PASSPORT</text>
              </view>

              <view class="f-rule"></view>
              <text class="f-name">{{ card.nameEn || card.name }}</text>
              <text class="f-meta">ARKNIGHTS · {{ card.code }}</text>

              <view class="f-badge"><i></i></view>
              <text class="f-foot">Operator of Rhodes Island</text>
              <text class="f-copy">© HYPERGRYPH</text>

              <view class="f-edge"></view>
              <view class="f-sleeve"></view>
            </view>
          </view>
        </view>

      </view>

      <text class="gn-hint">{{ hint }}</text>

      <!-- 结果条 -->
      <view class="gn-result" v-if="phase === 'flipped'">
        <view class="rs-left">
          <text class="rs-name">{{ card.name }}</text>
          <text class="rs-meta">{{ card.elite === 2 ? '精二' : '精一' }} · {{ card.boxId }} · {{ card.code }}</text>
        </view>
        <view class="rs-right">
          <text class="rs-price">{{ card.price ? '¥' + card.price : '—' }}</text>
          <text class="rs-note">参考市价</text>
        </view>
        <view class="rs-actions">
          <view class="btn-ghost" @click="closeStage">收 下</view>
          <view class="btn-solid" v-if="remain > 0" @click="again">再 抽 一 包</view>
          <view class="btn-solid" v-else @click="finishBox">换 新 盒</view>
        </view>
      </view>
    </view>

    <!-- ==================== 盒号抽屉 ==================== -->
    <view v-if="sheetBox" class="sheet-mask" @click="sheetBox = false"></view>
    <view v-if="sheetBox" class="sheet sheet-tall">
      <view class="sheet-head">
        <text class="sheet-title">选择盒号<text class="sheet-title-sub">还能开 {{ openBoxCount }} 盒</text></text>
        <text class="sheet-close" @click="sheetBox = false">✕</text>
      </view>
      <view class="sheet-search">
        <input class="sheet-input" v-model="kw" placeholder="搜索盒号 / 干员名" />
      </view>
      <scroll-view class="sheet-list sheet-grow" scroll-y>
        <view v-for="b in filteredBoxes" :key="b.id" class="box-row" :class="{ on: curBox && curBox.id === b.id, empty: leftOf(b) === 0 }" @click="chooseBox(b)">
          <text class="box-row-id">{{ b.id }}</text>
          <view class="box-row-info">
            <text class="box-row-name">{{ b.names }}</text>
          </view>
          <text class="box-row-tag" :class="{ zero: leftOf(b) === 0 }">{{ leftOf(b) === 0 ? '已抽空' : '剩 ' + leftOf(b) + '/' + total }}</text>
        </view>
      </scroll-view>
    </view>

    <!-- ==================== 记录抽屉 ==================== -->
    <view v-if="sheetHist" class="sheet-mask" @click="sheetHist = false"></view>
    <view v-if="sheetHist" class="sheet sheet-tall">
      <view class="sheet-head">
        <text class="sheet-title">抽卡记录<text class="sheet-title-sub">点一条看卡面</text></text>
        <text class="sheet-close" @click="sheetHist = false">✕</text>
      </view>
      <view class="hist-stats">
        <view class="hstat"><b>{{ hist.length }}</b><text>总抽数</text></view>
        <view class="hstat"><b>{{ e2Count }}</b><text>精二</text></view>
        <view class="hstat"><b>{{ uniqueCount }}</b><text>不同干员</text></view>
      </view>
      <scroll-view class="hist-list sheet-grow" scroll-y>
        <view v-for="(h, i) in hist" :key="i" class="hrow" @click="openCard(i)">
          <text class="hrow-name">{{ h.name }}</text>
          <text v-if="h.elite === 2" class="hrow-e2">精二</text>
          <text class="hrow-meta">{{ h.boxId }} · {{ h.time }}</text>
          <text class="hrow-view">卡面 ›</text>
        </view>
        <view v-if="!hist.length" class="empty">还没有抽过卡</view>
      </scroll-view>
    </view>

    <!-- ==================== 记录：回看抽出过的卡面 ==================== -->
    <view v-if="viewCard" class="cv-mask" @click="closeCard">
      <view class="cv-stage" @click.stop>
        <view class="cv-card">
          <view class="cw-card">
            <view class="face front">
              <view class="f-art">
                <image v-if="viewCard.art" class="f-art-img" :src="viewCard.art" mode="aspectFill" />
              </view>
              <view class="f-shade"></view>
              <view class="f-plate"></view>

              <view class="f-spine">
                <text class="f-spine-t">RHODES ISLAND</text>
                <view class="f-barcode" :style="barcodeStyleFor(viewCard.code)"></view>
              </view>

              <view class="f-top">
                <text>ARKNIGHTS</text>
                <text>PASSPORT</text>
              </view>

              <view class="f-rule"></view>
              <text class="f-name">{{ viewCard.nameEn || viewCard.name }}</text>
              <text class="f-meta">ARKNIGHTS · {{ viewCard.code || '—' }}</text>

              <view class="f-badge"><i></i></view>
              <text class="f-foot">Operator of Rhodes Island</text>
              <text class="f-copy">© HYPERGRYPH</text>

              <view class="f-edge"></view>
              <view class="f-sleeve"></view>
            </view>
          </view>
        </view>
        <view class="cv-info">
          <text class="cv-name">{{ viewCard.name }}</text>
          <text class="cv-meta">{{ viewCard.elite === 2 ? '精二' : '精一' }} · {{ viewCard.boxId || '—' }}<text v-if="viewCard.code"> · {{ viewCard.code }}</text></text>
          <text class="cv-price">{{ viewCard.price ? '参考市价 ¥' + viewCard.price : '参考市价 —' }}</text>
        </view>
        <text class="cv-tip">点空白处关闭</text>
      </view>
    </view>

  </view>
</template>

<script>
import artMap from './gacha-art.js';

const STORE_DECK = 'gn2_deck_';
const STORE_SLOT = 'gn2_slots_';
const STORE_HIST = 'gn2_history';
const STORE_SEL = 'gn2_selected';

/* 一大盒 14 抽（保配），盒内两列各 7 排；抽走一抽就空一格
   注意：盒内顺序是"洗匀"的，不是官方直邮那种按编号排好的顺序 */
const PACKS = 14;

/* 盒内 14 格的存档格式版本
   v3 = 按「款」记录：一格 = { c: 角色下标, e: 精一/精二 }
   旧版按「角色」记录，会让一个 7 角色的盒子里同名角色反复出现 → 必须重铺 */
const SLOT_VER = 3;

/* 盒类型归一：只有常规款走 Next 流程，联动 / 音律联觉（whitelist·ambience·special）单独归一类 */
function toBoxType(t) {
  const v = String(t || 'normal').toLowerCase();
  if (v === 'normal' || v === 'cooperation') return v;
  return 'music';
}

function shuffle(arr) {
  const a = arr.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    const t = a[i]; a[i] = a[j]; a[j] = t;
  }
  return a;
}

/* Code 39 编码表：9 个元素，n=窄(1) w=宽(3)，从条(bar) 开始交替 */
const C39 = {
  '0': 'nnnwwnwnn', '1': 'wnnwnnnnw', '2': 'nnwwnnnnw', '3': 'wnwwnnnnn', '4': 'nnnwwnnnw',
  '5': 'wnnwwnnnn', '6': 'nnwwwnnnn', '7': 'nnnwnnwnw', '8': 'wnnwnnwnn', '9': 'nnwwnnwnn',
  'A': 'wnnnnwnnw', 'B': 'nnwnnwnnw', 'C': 'wnwnnwnnn', 'D': 'nnnnwwnnw', 'E': 'wnnnwwnnn',
  'F': 'nnwnwwnnn', 'G': 'nnnnnwwnw', 'H': 'wnnnnwwnn', 'I': 'nnwnnwwnn', 'J': 'nnnnwwwnn',
  'K': 'wnnnnnnww', 'L': 'nnwnnnnww', 'M': 'wnwnnnnwn', 'N': 'nnnnwnnww', 'O': 'wnnnwnnwn',
  'P': 'nnwnwnnwn', 'Q': 'nnnnnnwww', 'R': 'wnnnnnwwn', 'S': 'nnwnnnwwn', 'T': 'nnnnwnwwn',
  'U': 'wwnnnnnnw', 'V': 'nwwnnnnnw', 'W': 'wwwnnnnnn', 'X': 'nwnnwnnnw', 'Y': 'wwnnwnnnn',
  'Z': 'nwwnwnnnn', '-': 'nwnnnnwnw', '.': 'wwnnnnwnn', ' ': 'nwwnnnwnn', '*': 'nwnnwnwnn'
};

export default {
  data() {
    return {
      themeMode: 'dark',
      scene: 'home',
      loading: true,
      sbh: 0,               // 状态栏高度：自定义导航要自己让位

      boxes: [],
      selType: 'normal',
      selBoxId: '',
      kw: '',

      phase: 'case',        // case | toptear | opening | flat | picking | pouch | card | flipped
      topPull: 0,           // 盒顶撕拉进度 0..1
      pull: 0,
      ty: 0,
      caseOpen: false,
      pouchIn: false,
      cardIn: false,
      cardOut: false,
      flipped: false,
      flash: false,
      hint: '',
      barcodeStyle: {},

      slotState: null,      // { order:[14], taken:[14], opened } —— order 是洗匀后的结果
      slotKey: '',          // 这份盒内打乱结果存在哪个 storage key
      pickedSlot: -1,
      boxLeftMap: {},       // 每个盒号还剩几抽（抽空一盒就少一盒）

      card: { name: '', elite: 1, art: '', code: '', boxId: '', price: null, nameEn: '' },

      deck: [],
      hist: [],
      viewIdx: -1,          // 记录里正在回看的那一条（-1 = 没打开）

      sheetBox: false,
      sheetHist: false
    };
  },

  computed: {
    topPad() {
      // 状态栏高度 + 一点呼吸位；H5 / 桌面端 statusBarHeight 为 0
      return Math.max(26, (this.sbh || 0) + 12);
    },
    typeChips() {
      return [
        { key: 'normal', label: '常规' },
        { key: 'cooperation', label: '联动' },
        { key: 'music', label: '音律联觉' },
        { key: 'mix', label: '超级混池' }
      ];
    },
    /* 联动款 / 音律联觉款盒内结构与卡面版式都不同，Next 版要单独做一层 */
    unsupported() {
      return this.selType === 'cooperation' || this.selType === 'music';
    },
    unsupportedLabel() {
      return this.selType === 'music' ? '音律联觉款' : '联动款';
    },
    unsupportedDesc() {
      if (this.selType === 'music') {
        return '音律联觉款是纯中文盒号、卡面也是另一套版式，撕口和袋型都和常规款不一样，硬套会画错。这一款目前请用稳定版。';
      }
      return '联动款的盒体尺寸、内衬和卡面印刷都和常规款不同，Next 版还没做这层适配。这一款目前请用稳定版。';
    },
    poolBoxes() {
      // 混池只混已适配的常规款，避免把联动/音律的卡混进来
      if (this.selType === 'mix') return this.boxes.filter(b => b.type === 'normal');
      return this.boxes.filter(b => b.type === this.selType);
    },
    curBox() {
      const list = this.poolBoxes;
      if (!list.length) return null;
      return list.find(b => b.id === this.selBoxId) || list[0];
    },
    boxNo() {
      if (this.selType === 'mix') return 'MIX';   // 混池没有盒号，别借用第一个常规盒的编号
      const id = this.curBox ? this.curBox.id : '0';
      const m = String(id).match(/\d+/);
      return m ? String(parseInt(m[0], 10)) : String(id);
    },
    boxMeta() {
      const b = this.curBox;
      if (!b) return '';
      // 款数 = 角色 × 精一/精二（没有精一的角色只算一款），14 款正好铺满一大盒
      return this.variantCountOf(b) + ' 款 · ' + (b.names || '').split(' / ').slice(0, 3).join(' ');
    },
    /* 这一盒一共几包：7 角色 × (精一+精二) = 14；4 角色就只有 8（与稳定版 totalCount 同源） */
    poolLen() {
      const st = this.slotState;
      if (st && Array.isArray(st.order) && st.order.length) return st.order.length;
      return this.curBox ? this.variantCountOf(this.curBox) : PACKS;
    },
    total() {
      return this.poolLen;
    },
    remain() {
      return this.poolLen - this.takenCount;
    },
    /* 盒内每一抽的位置：左列前一半，右列后一半 */
    slots() {
      const st = this.slotState;
      const out = [];
      for (let i = 0; i < this.poolLen; i++) {
        out.push({ i, taken: !!(st && st.taken && st.taken[i]) });
      }
      return out;
    },
    takenCount() {
      const st = this.slotState;
      if (!st || !st.taken) return 0;
      let n = 0;
      for (let i = 0; i < this.poolLen; i++) if (st.taken[i]) n++;
      return n;
    },
    caseCols() {
      const half = Math.ceil(this.poolLen / 2);
      return [this.slots.slice(0, half), this.slots.slice(half)];
    },
    /* 首页小盒面：同一盒 14 抽的缩略，抽走一抽就熄一格 */
    heroCols() {
      const cols = this.caseCols;
      return [
        cols[0].map(s => !s.taken),
        cols[1].map(s => !s.taken)
      ];
    },
    remainLabel() {
      return this.remain + ' 抽';
    },
    hudText() {
      if (this.selType === 'mix') return '超级混池 · 共 ' + this.poolLen + ' 抽';
      return '#' + this.boxNo + ' · 共 ' + this.poolLen + ' 抽';
    },
    filteredBoxes() {
      const k = this.kw.trim();
      const list = this.poolBoxes;
      if (!k) return list;
      return list.filter(b => b.id.indexOf(k) >= 0 || (b.names || '').indexOf(k) >= 0);
    },
    openBoxCount() {
      return this.poolBoxes.filter(b => this.leftOf(b) > 0).length;
    },
    caseCls() {
      return {
        in: this.scene === 'stage',
        opened: this.caseOpen,
        out: this.pouchIn,
        pickable: this.phase === 'flat'
      };
    },
    pouchCls() {
      // cut = 已选了某一抽，袋口敞开可抽卡
      return {
        in: this.pouchIn,
        gone: this.cardOut,
        cut: this.phase === 'picking' || this.phase === 'pouch'
          || this.phase === 'card' || this.phase === 'flipped'
      };
    },
    pouchStyle() {
      return { '--pull': String(this.pull) };
    },
    ripStyle() {
      return { '--r': String(this.topPull) };
    },
    cardCls() {
      return {
        in: this.cardIn,
        front: this.cardOut,
        reveal: this.flipped
      };
    },
    cardStyle() {
      return { '--ty': this.ty + 'px' };
    },
    e2Count() {
      return this.hist.filter(h => h.elite === 2).length;
    },
    uniqueCount() {
      return new Set(this.hist.map(h => h.name)).size;
    },
    /* 记录里点开的那一条 → 还原成一张完整的卡（老记录没存图的按名字+精一/精二补） */
    viewCard() {
      if (this.viewIdx < 0) return null;
      const h = this.hist[this.viewIdx];
      if (!h) return null;
      const elite = h.elite === 2 ? 2 : 1;
      return {
        name: h.name,
        nameEn: h.nameEn || '',
        elite,
        art: h.art || this.artOf(h.name, elite),
        price: h.price == null ? null : h.price,
        code: h.code || '',
        boxId: h.boxId || ''
      };
    }
  },

  onLoad() {
    try {
      const info = uni.getSystemInfoSync();
      this.sbh = info.statusBarHeight || 0;
    } catch (e) {}
    try {
      const t = uni.getStorageSync('themeMode');
      if (t === 'light') this.themeMode = 'light';
      const h = uni.getStorageSync(STORE_HIST);
      if (Array.isArray(h)) this.hist = h;
      const sel = uni.getStorageSync(STORE_SEL);
      if (sel && sel.boxId) { this.selBoxId = sel.boxId; this.selType = sel.type || 'normal'; }
    } catch (e) {}
    this.loadData();
  },

  methods: {
    /* ---------- 数据 ---------- */
    loadData() {
      let raw = null;
      try { raw = uni.getStorageSync('arknightsData'); } catch (e) {}
      if (!raw || !Array.isArray(raw) || !raw.length) {
        this.loading = false;
        uni.showModal({
          title: '还没有盒号数据',
          content: '请先回首页点「更新干员数据」，再回来抽卡。',
          showCancel: false
        });
        return;
      }
      const map = {};
      raw.forEach(b => {
        const id = String(b.Box_id || '').trim();
        if (!id) return;
        const type = toBoxType(b.Box_type);
        if (!map[id]) map[id] = { id, type, chars: [], names: '' };
        const box = map[id];
        // 一大盒 14 抽保配 → 必须读到 character14，不能只读到 10
        for (let i = 1; i <= 20; i++) {
          const c = b['character' + i];
          if (!c || !c.name) continue;
          if (box.chars.some(x => x.name === c.name)) continue;
          box.chars.push({
            name: String(c.name).trim(),
            av: c.imageUrl || '',
            noE1: !!c.nolyELITE1,
            p1: c.market_price && c.market_price.ELITE1,
            p2: c.market_price && c.market_price.ELITE2
          });
        }
      });
      const list = Object.keys(map).map(k => map[k]).filter(b => b.chars.length);
      list.forEach(b => { b.names = b.chars.map(c => c.name).join(' / '); });
      list.sort((a, b) => parseFloat(a.id) - parseFloat(b.id));
      this.boxes = list;
      this.loading = false;
      this.refreshLeft();
      this.syncDeck();
    },

    pickType(key) {
      this.selType = key;
      this.selBoxId = '';
      this.syncDeck();
      this.saveSel();
    },

    chooseBox(b) {
      this.selBoxId = b.id;
      this.sheetBox = false;
      this.syncDeck();
      this.saveSel();
    },

    saveSel() {
      try { uni.setStorageSync(STORE_SEL, { boxId: this.curBox ? this.curBox.id : '', type: this.selType }); } catch (e) {}
    },

    /* 一盒 14 抽：order = 每一抽是哪位干员，taken = 这一抽是否已被取走 */
    syncDeck() {
      this.syncSlots();
      const box = this.curBox;
      if (!box) { this.deck = []; return; }
      if (this.selType === 'mix') {
        const all = [];
        this.boxes.forEach(b => b.chars.forEach((c, i) => all.push({ boxId: b.id, idx: i })));
        this.deck = shuffle(all);
        return;
      }
      this.deck = shuffle(box.chars.map((_, i) => i));
    },

    syncSlots() {
      const box = this.curBox;
      if (!box) { this.slotState = null; return; }
      const key = this.selType === 'mix' ? (STORE_SLOT + 'mix') : (STORE_SLOT + box.id);
      let st = null;
      try { st = uni.getStorageSync(key); } catch (e) {}
      const shapeOk = st && Array.isArray(st.order) && st.order.length > 0
        && Array.isArray(st.taken) && st.taken.length === st.order.length;

      if (shapeOk && st.v === SLOT_VER) {
        this.slotKey = key;
        this.slotState = st;
        return;
      }

      // 新盒 / 老格式存档（按角色排，会出同盒重复卡面）→ 按「款」重铺
      const order = this.buildOrder(box);
      const drawn = shapeOk ? st.taken.filter(Boolean).length : 0;
      const taken = new Array(order.length).fill(false);
      // 已经抽走的抽数原样保留：前 drawn 格置空（不再显示、不再出卡）
      for (let i = 0; i < drawn && i < order.length; i++) {
        taken[i] = true;
        order[i] = { c: -1, e: 0 };
      }
      st = { order, taken, opened: shapeOk ? !!st.opened : false, seq: key, v: SLOT_VER };
      try { uni.setStorageSync(key, st); } catch (e) {}
      this.slotKey = key;
      this.slotState = st;
    },

    /* 一个角色在盒里占几款：
       nolyELITE1=真 → 只有精一这一款（与 utils/boxWidget.js 的 single 判定一致）
       否则有精二立绘就精一/精二两款 */
    variantsOfChar(char, i) {
      const pair = artMap[char.name] || [];
      const e1 = pair[0] || '';
      const e2 = pair[1] || '';
      const hasE1 = /_1\.png/i.test(e1);
      const hasE2 = /_2\.png/i.test(e2) && e2 !== e1;
      if (char.noE1) return [{ c: i, e: 1 }];
      if (hasE1 && !hasE2) return [{ c: i, e: 1 }];
      if (!hasE1 && hasE2) return [{ c: i, e: 2 }];
      return [{ c: i, e: 1 }, { c: i, e: 2 }];
    },

    variantCountOf(box) {
      if (!box) return 0;
      let n = 0;
      box.chars.forEach((c, i) => { n += this.variantsOfChar(c, i).length; });
      return n;
    },

    /* 盒里的全部款：每款一张，整盒打乱（与稳定版 generateAllCards 一致）
       —— 不凑 14：7 角色 × 精一精二 = 14 款，4 角色就只有 8 款，硬补重复就是 bug */
    buildOrder(box) {
      if (this.selType === 'mix') {
        // 混池：所有常规盒的款进同一个池
        const all = [];
        this.poolBoxes.forEach(b => b.chars.forEach((c, i) => {
          this.variantsOfChar(c, i).forEach(v => all.push({ b: b.id, c: v.c, e: v.e }));
        }));
        if (!all.length) return [];
        const bag = shuffle(all);
        // 混池固定 14 抽；池子不足 14 款时才补位
        while (bag.length < PACKS) bag.push(all[Math.floor(Math.random() * all.length)]);
        return shuffle(bag.slice(0, PACKS));
      }
      const list = [];
      box.chars.forEach((c, i) => {
        this.variantsOfChar(c, i).forEach(v => list.push(v));
      });
      const bag = shuffle(list);
      // 一大盒物理上就是 14 包：款数超过 14 时随机取 14 款
      return bag.length > PACKS ? bag.slice(0, PACKS) : bag;
    },

    persistSlots() {
      const st = this.slotState;
      if (!st) return;
      const key = this.slotKey || (this.curBox ? STORE_SLOT + this.curBox.id : '');
      if (!key) return;
      try {
        uni.setStorageSync(key, {
          order: st.order.slice(), taken: st.taken.slice(), opened: !!st.opened, seq: key, v: SLOT_VER
        });
      } catch (e) {}
    },

    resetBox() {
      const box = this.curBox;
      if (box) {
        try { uni.removeStorageSync(STORE_SLOT + box.id); } catch (e) {}
        try { uni.removeStorageSync(STORE_DECK + box.id); } catch (e) {}
      }
      try { uni.removeStorageSync(STORE_SLOT + 'mix'); } catch (e) {}
      this.slotState = null;
      this.syncDeck();
      this.refreshLeft();
      uni.showToast({ title: '已换成新盒', icon: 'none' });
    },

    /* ---------- Code39 条码（真实编码，不是花纹） ---------- */
    code39Style(text, deg) {
      deg = deg || 90;
      const chars = ('*' + String(text || '0')).toUpperCase().split('');
      const segs = [];
      let x = 0;
      chars.forEach((ch, i) => {
        const pat = C39[ch] || C39['-'];
        for (let k = 0; k < 9; k++) {
          const w = pat.charAt(k) === 'w' ? 3 : 1;
          segs.push({ x, w, bar: k % 2 === 0 });
          x += w;
        }
        if (i < chars.length - 1) { segs.push({ x, w: 1, bar: false }); x += 1; }
      });
      const total = x || 1;
      const stops = segs.map(s => {
        const a = (s.x / total * 100).toFixed(3);
        const b = ((s.x + s.w) / total * 100).toFixed(3);
        return (s.bar ? 'rgba(248,249,240,.92)' : 'rgba(248,249,240,0)') + ' ' + a + '% ' + b + '%';
      }).join(', ');
      return {
        backgroundImage: 'linear-gradient(' + deg + 'deg, ' + stops + ')',
        backgroundSize: '100% 100%',
        backgroundRepeat: 'no-repeat'
      };
    },

    /* 每个盒号还剩几抽：抽空的那一盒在列表里就是"已抽空" */
    refreshLeft() {
      const m = {};
      this.boxes.forEach(b => {
        let st = null;
        try { st = uni.getStorageSync(STORE_SLOT + b.id); } catch (e) {}
        let left = this.variantCountOf(b);
        if (st && Array.isArray(st.taken) && Array.isArray(st.order)
          && st.taken.length === st.order.length && st.order.length) {
          let n = 0;
          for (let i = 0; i < st.taken.length; i++) if (st.taken[i]) n++;
          left = st.order.length - n;
        }
        m[b.id] = left;
      });
      this.boxLeftMap = m;
    },

    leftOf(b) {
      const v = this.boxLeftMap[b.id];
      return typeof v === 'number' ? v : this.variantCountOf(b);
    },

    /* ---------- 进盒（不预先决定结果，由用户在盒里自己挑位置） ---------- */
    draw() {
      const box = this.curBox;
      if (!box) { uni.showToast({ title: '先选个盒号', icon: 'none' }); return; }
      this.syncSlots();
      if (this.remain <= 0) {
        uni.showModal({
          title: '这盒抽空了',
          content: '#' + box.id + ' 的 ' + this.poolLen + ' 抽已经全部取走，要换一盒新的吗？',
          confirmText: '换新盒',
          cancelText: '再等等',
          success: r => { if (r.confirm) this.resetBox(); }
        });
        return;
      }

      const opened = !!(this.slotState && this.slotState.opened);
      this.card = { name: '', elite: 1, art: '', code: '', boxId: '', price: null, nameEn: '' };
      this.barcodeStyle = {};
      this.scene = 'stage';
      // 盒顶没撕过 → 先撕；撕过的盒直接摊平，让用户继续在剩下的位置里挑
      this.phase = opened ? 'flat' : 'case';
      this.topPull = opened ? 1 : 0;
      this.caseOpen = opened;
      this.pouchIn = false;
      this.cardIn = false;
      this.cardOut = false;
      this.flipped = false;
      this.flash = false;
      this.pull = 0;
      this.ty = 0;
      this.pickedSlot = -1;
      this.hint = opened ? ('还剩 ' + this.remain + ' 抽 · 点一格') : '横向撕开盒顶封条';
    },

    /* 把某一抽的干员算成一张卡；slotElite 由这一格是「哪一款」决定，不再随机 */
    prepareCard(char, boxId, slotElite) {
      const pair = artMap[char.name] || [];
      const e1 = pair[0] || '';
      const e2 = pair[1] || '';
      // 只认文件名真的带 _1 / _2 的，避免把精二立绘当成精一
      const hasE1 = /_1\.png/i.test(e1);
      const hasE2 = /_2\.png/i.test(e2) && e2 !== e1;

      let elite;
      if (slotElite === 1 || slotElite === 2) elite = slotElite;   // 盒里那一格定的款
      else if (char.noE1) elite = 1;                      // nolyELITE1 = 只有精一
      else if (hasE1 && !hasE2) elite = 1;            // 只有精一立绘 → 只能出精一
      else if (!hasE1 && hasE2) elite = 2;            // 只有精二立绘 → 只能出精二
      else elite = Math.random() < 0.5 ? 1 : 2;

      const art = elite === 2
        ? (hasE2 ? e2 : (e1 || char.av || ''))
        : (hasE1 ? e1 : (e2 || char.av || ''));
      const price = elite === 2 ? char.p2 : char.p1;
      const code = 'AK' + String(boxId).replace(/\D/g, '').padStart(3, '0') + (elite === 2 ? 'E2' : 'E1');
      this.card = {
        name: char.name,
        elite,
        art,
        price,
        boxId,
        nameEn: (char.nameEn || ''),
        code
      };
      this.barcodeStyle = this.code39Style(code, 180);
    },

    /* 某一格洗出来的是哪位干员（哪一款） */
    slotChar(i) {
      const st = this.slotState;
      const box = this.curBox;
      if (!st || !box) return null;
      const o = st.order[i];
      if (!o) return null;
      if (typeof o === 'object') {
        const b = o.b ? this.boxes.find(x => x.id === o.b) : box;
        if (!b || typeof o.c !== 'number' || o.c < 0) return null;   // 老存档留下的空位
        const c = b.chars[o.c];
        return c ? { char: c, boxId: b.id, e: o.e || 0 } : null;
      }
      const c = box.chars[o];
      return c ? { char: c, boxId: box.id, e: 0 } : null;
    },

    /* ---------- 用户在盒里挑一抽 ---------- */
    pickSlot(i) {
      if (this.phase !== 'flat') return;
      const st = this.slotState;
      const box = this.curBox;
      if (!st || !box || st.taken[i]) return;

      const hit = this.slotChar(i);
      if (!hit) return;
      const char = hit.char;
      const boxId = hit.boxId;

      // 这一抽从盒里消失，桌上就少一格
      const taken = st.taken.slice();
      taken[i] = true;
      this.slotState = { order: st.order.slice(), taken, opened: true };
      this.pickedSlot = i;
      this.persistSlots();
      this.refreshLeft();
      this.phase = 'picking';
      this.hint = '';
      this.prepareCard(char, boxId, hit.e);
      uni.vibrateShort && uni.vibrateShort({ success() {} });

      // 那一格抬起来 → 盒子淡出 → 这包被抽到台面中央
      setTimeout(() => {
        this.caseOpen = true;
        this.pouchIn = true;
        this.cardIn = true;
        this.phase = 'pouch';
        this.hint = '向上抽出通行证';
      }, 280);
    },

    /* ---------- 盒顶撕拉手势（阶段 1） ---------- */
    topDown(e) {
      if (this.phase !== 'case' && this.phase !== 'toptear') return;
      const t = e.touches[0];
      this._tsx = t.clientX;
      this._tsy = t.clientY;
      this._tdrag = true;
    },

    topMove(e) {
      if (!this._tdrag) return;
      const t = e.touches[0];
      const dx = t.clientX - this._tsx;
      const dy = t.clientY - this._tsy;
      // 必须横向为主，避免上滑误触
      if (this.phase === 'case') {
        if (dx > 6 && Math.abs(dx) > Math.abs(dy) * 1.2) this.phase = 'toptear';
        else return;
      }
      const p = Math.max(0, Math.min(1, dx / 130));
      this.topPull = p;
      if (p > 0.18 && !this._tsnap) {
        this._tsnap = true;
        uni.vibrateShort && uni.vibrateShort({ success() {} });
      }
    },

    topUp() {
      if (!this._tdrag) return;
      this._tdrag = false;
      this._tsnap = false;
      // 只点了一下（没横拖）也算撕开，避免卡在盒顶不知道怎么继续
      if (this.phase === 'case') { this.openTop(); return; }
      if (this.phase !== 'toptear') { this.topPull = 0; return; }
      if (this.topPull > 0.34) this.openTop();
      else { this.topPull = 0; this.phase = 'case'; }
    },

    /* 盒顶撕开 → 盒子摊平成平面，停下来等用户自己挑（阶段 2） */
    openTop() {
      this.topPull = 1;
      this.phase = 'opening';
      this.hint = '';
      uni.vibrateShort && uni.vibrateShort({ success() {} });

      // 撕条飞出 → 盒盖翻开 + 整体摊平
      setTimeout(() => { this.caseOpen = true; }, 120);
      // 摊平完成：盒内 14 抽全部露出来，交给用户挑（不再自动抽一包）
      setTimeout(() => {
        if (this.phase === 'opening') {
          this.phase = 'flat';
          this.hint = '共 ' + this.total + ' 抽 · 已洗匀 · 点一格';
          if (this.slotState) {
            this.slotState = { order: this.slotState.order.slice(), taken: this.slotState.taken.slice(), opened: true };
            this.persistSlots();
          }
        }
      }, 860);
    },

    /* ---------- 抽卡手势（阶段 3：选中的那一包向上抽出） ---------- */
    pouchDown(e) {
      if (this.phase !== 'pouch') return;
      this._py = e.touches[0].clientY;
      this._pdrag = true;
    },

    pouchMove(e) {
      if (!this._pdrag) return;
      const t = e.touches[0];
      const dy = t.clientY - this._py;
      this.ty = Math.max(-470, Math.min(0, dy));
      if (this.ty < -60) {
        this.hint = this.ty < -220 ? '松手 · 抽出' : '继续往上抽';
      } else {
        this.hint = '向上抽出通行证';
      }
    },

    pouchUp() {
      if (!this._pdrag) return;
      this._pdrag = false;
      if (this.ty < -220) this.pullOut();
      else { this.ty = 0; this.hint = '向上抽出通行证'; }
    },

    pullOut() {
      this.phase = 'card';
      this.hint = '点一下 · 看卡面';
      this.cardOut = true;
      this.ty = 0;
    },

    tapCard() {
      if (this.phase === 'card') {
        this.phase = 'flipped';
        this.flipped = true;
        this.hint = '';
        const elite = this.card.elite === 2;
        if (elite) { this.flash = true; setTimeout(() => { this.flash = false; }, 620); }
        this.pushHistory();
        return;
      }
      if (this.phase === 'flipped') this.again();
    },

    pushHistory() {
      const d = new Date();
      this.hist.unshift({
        name: this.card.name,
        elite: this.card.elite,
        boxId: this.card.boxId,
        time: (d.getMonth() + 1) + '/' + d.getDate() + ' ' + d.getHours() + ':' + String(d.getMinutes()).padStart(2, '0'),
        // 记录里能回看卡面：图 / 编码 / 市价都存下来，之后数据换了也不影响旧记录
        art: this.card.art || '',
        code: this.card.code || '',
        price: this.card.price == null ? null : this.card.price,
        nameEn: this.card.nameEn || ''
      });
      if (this.hist.length > 300) this.hist.length = 300;
      try { uni.setStorageSync(STORE_HIST, this.hist); } catch (e) {}
    },

    again() {
      // 同一盒还有剩余 → 直接回到摊平的盒面继续挑，不跳回首页
      if (this.remain > 0) {
        this.resetStage();
        this.scene = 'stage';
        this.phase = 'flat';
        this.caseOpen = true;
        this.topPull = 1;
        this.hint = '还剩 ' + this.remain + ' 抽 · 点一格';
        return;
      }
      this.closeStage();
      uni.showToast({ title: '这大盒抽空了 · 开个新盒', icon: 'none' });
    },

    /* 整盒抽完：回首页并直接开一盒新的（款重新洗） */
    finishBox() {
      this.closeStage();
      this.resetBox();
    },

    closeStage() {
      this.scene = 'home';
      this.resetStage();
    },

    resetStage() {
      this.phase = 'case';
      this.topPull = 0;
      this.caseOpen = false;
      this.pouchIn = false;
      this.cardIn = false;
      this.cardOut = false;
      this.flipped = false;
      this.pull = 0;
      this.ty = 0;
      this.pickedSlot = -1;
      this.hint = '';
      this.flash = false;
    },

    goBack() {
      uni.navigateBack({ fail() { uni.switchTab({ url: '/pages/index/index' }); } });
    },

    /* 联动 / 音律联觉：转去稳定版 */
    goStable() {
      uni.navigateTo({
        url: '/packageB/pages/gacha_simulated/gacha_simulated?from=gacha_next',
        fail() {
          uni.showToast({ title: '没打开稳定版，请从工具列表进入', icon: 'none' });
        }
      });
    },

    openHistory() {
      try {
        const h = uni.getStorageSync(STORE_HIST);
        if (Array.isArray(h)) this.hist = h;
      } catch (e) {}
      this.sheetHist = true;
    },

    /* ---------- 记录：回看卡面 ---------- */
    openCard(i) {
      if (i < 0 || i >= this.hist.length) return;
      this.viewIdx = i;
    },

    closeCard() {
      this.viewIdx = -1;
    },

    /* 老记录没存卡的图时，按名字 + 精一/精二从本地卡表里补一张 */
    artOf(name, elite) {
      const pair = artMap[name] || [];
      const e1 = pair[0] || '';
      const e2 = pair[1] || '';
      const hasE1 = /_1\.png/i.test(e1);
      const hasE2 = /_2\.png/i.test(e2) && e2 !== e1;
      if (elite === 2) return hasE2 ? e2 : (e1 || '');
      return hasE1 ? e1 : (e2 || '');
    },

    /* 记录里的卡面也要有条码：按存下来的编码现场画 */
    barcodeStyleFor(code) {
      return code ? this.code39Style(code, 180) : {};
    }
  }
};
</script>

<style>
@import './gacha-next.css';
</style>
