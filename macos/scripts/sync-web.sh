#!/usr/bin/env bash
# 把前端的 H5 产物同步到 macos/wwwroot。
# 用法：bash scripts/sync-web.sh [产物目录]
#
# 默认取同仓库 windows 下的那一份 —— 产物只在仓库里存一次，macOS 侧按需拷过来。
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="${1:-$ROOT/../windows/src/AkPassport.Win/wwwroot}"

if [ ! -f "$SRC/index.html" ]; then
	echo "找不到 H5 产物：$SRC/index.html" >&2
	echo "请先执行 windows/_build_h5.sh，或传入正确目录：bash scripts/sync-web.sh <dir>" >&2
	exit 1
fi

mkdir -p "$ROOT/wwwroot"
rsync -a --delete "$SRC/" "$ROOT/wwwroot/"
echo "已同步 $(find "$ROOT/wwwroot" -type f | wc -l | tr -d ' ') 个文件 -> $ROOT/wwwroot"
