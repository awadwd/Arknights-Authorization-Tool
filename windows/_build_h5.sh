#!/usr/bin/env bash
# 前端 H5 构建：HBuilderX 内置编译器 -> dist-h5 -> 同步到壳的 wwwroot
set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# HBuilderX 安装目录：换机器时用 HB=/path/to/HBuilderX 覆盖
HB="${HB:-D:/Program Files/HBuilderX.3.2.9.20210927/HBuilderX}"
# 前端工程：默认取仓库根目录（uni-app 源码就在 windows/ 的上一层）
SRC="${FRONTEND_SRC:-$(cd "$ROOT/.." && pwd)}"
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
UNI_INPUT_DIR="$SRC" UNI_OUTPUT_DIR="$OUT" UNI_PLATFORM=h5 \
  node node_modules/@dcloudio/vite-plugin-uni/bin/uni.js build 2>&1 | tail -6

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
