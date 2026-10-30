#!/usr/bin/env bash
# 前台运行，日志直接打到终端（开发期用）。
set -euo pipefail
cd "$(dirname "$0")"

bash build.sh
echo
echo "== 启动（Ctrl+C 退出）=="
AKPASSPORT_DEV="${AKPASSPORT_DEV:-1}" "$PWD/dist/AkPassport.app/Contents/MacOS/AkPassportMac"
