#!/usr/bin/env bash
# 前端 H5 构建：HBuilderX 内置编译器 -> dist-h5 -> 同步到壳的 wwwroot
set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && { pwd -W 2>/dev/null || pwd; })"
# HBuilderX 安装目录：换机器时用 HB=/path/to/HBuilderX 覆盖
HB="${HB:-D:/Program Files/HBuilderX.3.2.9.20210927/HBuilderX}"
# 前端工程：显式优先，其次自动探测。
#   - 开发树（D:/ak-passport-win）里源码在别处，必须用 FRONTEND_SRC 指定；
#   - 开源仓库树（.../windows）里源码就在上一层，自动取上一级。
#   别写成固定 "$ROOT/.." —— 开发树里那会解析成 D:/，构建的是错目录。
_detect_src() {
  if [ -n "${FRONTEND_SRC:-}" ]; then echo "$FRONTEND_SRC"; return; fi
  # 开发树专用：仓库根放一个 .frontend_src 写前端绝对路径（已进 .git/info/exclude，不入库）
  if [ -f "$ROOT/.frontend_src" ]; then
    local v
    v="$(head -1 "$ROOT/.frontend_src" | tr -d '')"
    [ -n "$v" ] && { echo "$v"; return; }
  fi
  if [ -f "$ROOT/pages.json" ] && [ -f "$ROOT/manifest.json" ]; then echo "$ROOT"; return; fi
  local up
  up="$(cd "$ROOT/.." && { pwd -W 2>/dev/null || pwd; })"
  if [ -f "$up/pages.json" ] && [ -f "$up/manifest.json" ]; then echo "$up"; return; fi
  echo "$ROOT"
}
SRC="$(_detect_src)"
NODE_BIN="${NODE_BIN:-node}"
OUT="$ROOT/dist-h5"
WWW="$ROOT/src/AkPassport.Win/wwwroot"
TRASH="$ROOT/_trash"

# ★ 清空目录要「删完确认真的没了」。
#   正常终端 rm -rf 就够；受限环境里批量删除会被安全策略接管并静默失败（目录还在），
#   后果是 dist-h5 / wwwroot 里混着上一版产物 —— vite 会直接 "Build failed in 5s"，
#   wwwroot 则会攒下几十份 index-*.js（体积虚胖，还容易误判「改了没生效」）。
#   删不掉就改名搬到 _trash/ 下（_trash 可随时手工删），保证拿到的一定是空目录。
fresh_dir() {
  local d="$1"
  rm -rf "$d" 2>/dev/null
  [ -d "$d" ] || return 0
  mkdir -p "$TRASH"
  mv "$d" "$TRASH/$(basename "$d")-$(date +%Y%m%d%H%M%S)" 2>/dev/null
}

fresh_dir "$OUT"

cd "$HB/plugins/uniapp-cli-vite" || exit 1
echo "== 前端源码: $SRC"
UNI_INPUT_DIR="$SRC" UNI_OUTPUT_DIR="$OUT" UNI_PLATFORM=h5 \
  "$NODE_BIN" node_modules/@dcloudio/vite-plugin-uni/bin/uni.js build 2>&1 | tail -6

if [ ! -f "$OUT/index.html" ]; then
  echo "BUILD FAILED"
  exit 1
fi

# wwwroot 同样要先清空，否则历次构建的 assets 会一直攒在里面。
fresh_dir "$WWW"
mkdir -p "$WWW"
# 注意：必须用 "$OUT"/. 才能覆盖内容；直接 cp -r "$OUT" "$WWW" 会变成嵌套 wwwroot/dist-h5
cp -rf "$OUT"/. "$WWW"/
echo "synced -> $WWW"

# 壳运行时读的是 bin 下的 wwwroot（构建时从 src 拷过去的），
# 只同步 src 会出现「改完 H5 但界面没变」的假象 —— 这里一并覆盖。
for d in "$ROOT"/src/AkPassport.Win/bin/x64/Debug/*/wwwroot; do
  if [ -d "$d" ]; then
    fresh_dir "$d"
    mkdir -p "$d"
    cp -rf "$OUT"/. "$d"/ && echo "synced -> $d"
  fi
done
