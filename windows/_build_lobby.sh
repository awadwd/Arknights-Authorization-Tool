#!/usr/bin/env bash
# 编译联机大厅服务端（独立控制台程序，默认端口 23516）
#
# 注意：在部分受限 shell / CI 里 APPDATA、LOCALAPPDATA、NUGET_PACKAGES 可能是空字符串，
# 会让 dotnet 还原报 "Value cannot be null (Parameter 'path1')"，这里统一兜底。
set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export USERPROFILE="${USERPROFILE:-$HOME}"
export APPDATA="${APPDATA:-$USERPROFILE/AppData/Roaming}"
export LOCALAPPDATA="${LOCALAPPDATA:-$USERPROFILE/AppData/Local}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$USERPROFILE/.nuget/packages}"

cd "$ROOT/src/BattleLobbyServer"
echo "APPDATA=$APPDATA"
dotnet build -c Debug
