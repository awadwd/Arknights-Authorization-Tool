#!/usr/bin/env bash
# 一键产出 dist/AkPassport.app（无需 Xcode，只用 Command Line Tools 的 swiftc）。
set -euo pipefail
cd "$(dirname "$0")"

ROOT="$PWD"
APP="$ROOT/dist/AkPassport.app"
ARCH="$(uname -m)"
DEPLOY_TARGET="${DEPLOY_TARGET:-12.0}"
# H5 产物只在仓库里存一份（windows/src/AkPassport.Win/wwwroot），macos 侧按需同步过来，
# 不重复提交同一批文件。
WEB_SRC="${AKPASSPORT_WEB_SRC:-$ROOT/../windows/src/AkPassport.Win/wwwroot}"

if [ ! -f "$ROOT/wwwroot/index.html" ] && [ -f "$WEB_SRC/index.html" ]; then
	echo "== 首次构建，从 windows 目录同步 H5 产物 =="
	bash "$ROOT/scripts/sync-web.sh" "$WEB_SRC"
fi

if [ ! -f "$ROOT/wwwroot/index.html" ]; then
	echo "缺少 wwwroot，请先运行： bash scripts/sync-web.sh <H5 产物目录>"
	exit 1
fi

# 增量构建：不整体删除 .app（wwwroot 上百个文件会触发批量删除保护），只覆盖变动的部分
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
rm -f "$APP/Contents/MacOS/AkPassportMac"

echo "== 编译外壳 (${ARCH}-apple-macos${DEPLOY_TARGET}) =="
swiftc -swift-version 5 -O \
	-target "${ARCH}-apple-macos${DEPLOY_TARGET}" \
	-o "$APP/Contents/MacOS/AkPassportMac" \
	"$ROOT"/src/AkPassportMac/*.swift

cp "$ROOT/Info.plist" "$APP/Contents/Info.plist"

echo "== 同步 H5 产物 =="
mkdir -p "$APP/Contents/Resources/wwwroot"
rsync -a --delete "$ROOT/wwwroot/" "$APP/Contents/Resources/wwwroot/"
echo "   $(find "$APP/Contents/Resources/wwwroot" -type f | wc -l | tr -d ' ') 个文件"

# 图标：从 H5 产物里的 app-logo 生成 icns（没有就跳过，不影响运行）
ICON_SRC=""
for candidate in "$ROOT"/wwwroot/assets/app-logo*.png; do
	[ -f "$candidate" ] && ICON_SRC="$candidate" && break
done
if [ -n "$ICON_SRC" ]; then
	ICONSET="$ROOT/dist/AppIcon.iconset"
	rm -rf "$ICONSET"
	mkdir -p "$ICONSET"
	gen() { sips -s format png -z "$2" "$2" "$ICON_SRC" --out "$ICONSET/$1" >/dev/null 2>&1 || true; }
	gen icon_16x16.png 16
	gen icon_16x16@2x.png 32
	gen icon_32x32.png 32
	gen icon_32x32@2x.png 64
	gen icon_128x128.png 128
	gen icon_128x128@2x.png 256
	gen icon_256x256.png 256
	gen icon_256x256@2x.png 512
	gen icon_512x512.png 512
	gen icon_512x512@2x.png 1024
	if iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/app.icns" 2>/dev/null; then
		echo "== 图标已生成 =="
	else
		echo "!! 图标生成失败，跳过"
	fi
	rm -rf "$ICONSET"
fi

echo "== ad-hoc 签名 =="
if codesign --force --sign - "$APP" 2>/dev/null; then
	echo "   完成（仅本机可用；对外分发需要 Apple Developer 证书 + 公证）"
else
	echo "!! 签名失败，程序仍可本机运行"
fi

SIZE="$(du -sh "${APP}" | cut -f1)"
echo
echo "产物：${APP}  (${SIZE})"
echo "运行：open \"${APP}\"        # 或 bash run.sh 看前台日志"
