#!/usr/bin/env bash
# =============================================================================
# 生成一个「安装包不完整」的样本，给 tests/installer_selftest.py 的 S09 用
#
# 做法：把 payload.zip 临时挪开 → 编译一个不含内嵌载荷的安装器 → 挪回来。
# 产物：_test/nopayload_out/AkPassportSetup.exe（单文件，约 100 MB，跑测试时才需要）
#
# 为什么不入库：100 MB 的二进制没必要进 git，需要时现场生成即可。
# =============================================================================
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && { pwd -W 2>/dev/null || pwd; })"
PROJ="$ROOT/installer/AkPassportSetup/AkPassportSetup.csproj"
DIR="$ROOT/installer/AkPassportSetup"
OUT="$ROOT/_test/nopayload_out"
HOLD="$DIR/payload.zip.hold"

export APPDATA="${APPDATA:-$USERPROFILE/AppData/Roaming}"
export LOCALAPPDATA="${LOCALAPPDATA:-$USERPROFILE/AppData/Local}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$USERPROFILE/.nuget/packages}"

if [ ! -f "$DIR/payload.zip" ]; then
  echo "!! 没有 payload.zip，先跑 bash _build_setup.sh"
  exit 1
fi

cleanup() {
  [ -f "$HOLD" ] && mv -f "$HOLD" "$DIR/payload.zip" && echo "== payload.zip 已还原"
}
trap cleanup EXIT

mv -f "$DIR/payload.zip" "$HOLD" || { echo "!! 挪走 payload.zip 失败"; exit 1; }

mkdir -p "$OUT"
dotnet publish "$PROJ" -c Release -r win-x64 --self-contained true -p:Platform=x64 \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None -p:DebugSymbols=false \
  -o "$OUT" 2>&1 | tail -3

[ -f "$OUT/AkPassportSetup.exe" ] || { echo "!! 样本生成失败"; exit 1; }
echo "== 样本: $OUT/AkPassportSetup.exe ($(du -h "$OUT/AkPassportSetup.exe" | cut -f1))"
