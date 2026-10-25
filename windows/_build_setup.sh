#!/usr/bin/env bash
# =============================================================================
# 只重建安装程序（不重新编译前端、不重新 publish 主程序）
#
# 用法：bash _build_setup.sh
#
# 前提：dist/AkPassport-Win-<版本>-win-x64/ 已经是最新发布产物。
# 它会把这个目录压成 installer/AkPassportSetup/payload.zip（内嵌资源），
# 再 publish 出单个 exe 到 dist/方舟通行证谷子查询工具-<版本>-安装程序.exe。
#
# ★ payload.zip / 最终 zip 都用「覆盖写」（zipfile 的 'w' 会截断），
#   不做 os.remove —— 大文件删除在受限环境里会被安全策略拦掉，
#   拦掉之后内嵌的就是上一版 payload（体积对不上、H5 是旧的）。
# =============================================================================
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && { pwd -W 2>/dev/null || pwd; })"
PROJ="$ROOT/src/AkPassport.Win/AkPassport.Win.csproj"
SETUP_DIR="$ROOT/installer/AkPassportSetup"
PY="${PY:-python}"

VER="$("$PY" - "$PROJ" <<'PY'
import re, sys, io
s = io.open(sys.argv[1], 'r', encoding='utf-8').read()
m = re.search(r'<Version>([^<]+)</Version>', s)
print(m.group(1).strip() if m else '1.0.0')
PY
)"
SRC="$ROOT/dist/AkPassport-Win-$VER-win-x64"
SETUP_OUT="$ROOT/dist/方舟通行证谷子查询工具-$VER-安装程序"
SETUP_EXE="$ROOT/dist/方舟通行证谷子查询工具-$VER-安装程序.exe"

if [ ! -f "$SRC/AkPassport.Win.exe" ]; then
  echo "!! $SRC 不是有效的发布产物，先跑 bash release.sh"
  exit 1
fi

echo "== 版本: $VER"
"$PY" - "$SRC" "$SETUP_DIR/payload.zip" <<'PY'
import os, sys, zipfile
src, out = sys.argv[1], sys.argv[2]
n = 0
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for base, dirs, files in os.walk(src):
        for f in files:
            p = os.path.join(base, f)
            z.write(p, os.path.relpath(p, src))   # 条目直接在根，不能带顶层目录名
            n += 1
print('payload ->', out, n, '个文件', round(os.path.getsize(out) / 1048576, 1), 'MB')
PY

rm -rf "$SETUP_OUT"
dotnet publish "$SETUP_DIR/AkPassportSetup.csproj" -c Release -r win-x64 \
  --self-contained true -p:Platform=x64 -p:Version="$VER" \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None -p:DebugSymbols=false \
  -o "$SETUP_OUT" 2>&1 | tail -5

if [ -f "$SETUP_OUT/AkPassportSetup.exe" ]; then
  mv -f "$SETUP_OUT/AkPassportSetup.exe" "$SETUP_EXE"
  rm -rf "$SETUP_OUT"
  echo "== 安装程序: $SETUP_EXE"
else
  echo "!! 安装程序构建失败"
  exit 1
fi
