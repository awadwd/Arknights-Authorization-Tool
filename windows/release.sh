#!/usr/bin/env bash
# =============================================================================
# 一键出 Windows Beta 包
#   1. 编译前端 H5 → dist-h5，并同步到壳的 wwwroot
#   2. dotnet publish（Release / win-x64 / 自包含单文件）
#   3. 产物拷到 dist/AkPassport-Win-<版本>-win-x64/，再压成同名 zip（绿色版）
#   4. 发布目录再压成 payload.zip，内嵌进安装程序，出单个安装 exe
#
# 用法：bash release.sh
# 版本号只改 src/AkPassport.Win/AkPassport.Win.csproj 里的 <Version>
# =============================================================================
set -uo pipefail

# ---- 环境变量兜底：受限/沙箱 shell 里这些 Windows 系统变量可能为空，----
# ---- 为空会导致 dotnet NuGet 还原 / MSBuild 报 path1 null 或找不到模块。----
export APPDATA="${APPDATA:-$USERPROFILE\\AppData\\Roaming}"
export LOCALAPPDATA="${LOCALAPPDATA:-$USERPROFILE\\AppData\\Local}"
export ProgramData="${ProgramData:-C:\\ProgramData}"
export ALLUSERSPROFILE="${ALLUSERSPROFILE:-C:\\ProgramData}"
export ProgramFiles="${ProgramFiles:-C:\\Program Files}"
export ProgramFiles_x86="${ProgramFiles_x86:-C:\\Program Files (x86)}"
export CommonProgramFiles="${CommonProgramFiles:-C:\\Program Files\\Common Files}"
export CommonProgramFiles_x86="${CommonProgramFiles_x86:-C:\\Program Files (x86)\\Common Files}"
export SystemDrive="${SystemDrive:-C:}"
export SystemRoot="${SystemRoot:-C:\\WINDOWS}"
export windir="${windir:-C:\\WINDOWS}"
export ComSpec="${ComSpec:-C:\\WINDOWS\\system32\\cmd.exe}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-$USERPROFILE\\.nuget\\packages}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && { pwd -W 2>/dev/null || pwd; })"
PROJ="$ROOT/src/AkPassport.Win/AkPassport.Win.csproj"
CSPROJ="$PROJ"
PY="${PY:-python}"
TRASH="$ROOT/_trash"

# ★ 清空目录（删完必须确认真的没了）：正常终端 rm -rf 就够；
#   受限环境里批量删除会被安全策略接管并静默失败（目录还在），
#   于是新版里混着上一版的残留文件。删不掉就改名搬到 _trash/ 下。
fresh_dir() {
  local d="$1"
  rm -rf "$d" 2>/dev/null
  [ -d "$d" ] || return 0
  mkdir -p "$TRASH"
  mv "$d" "$TRASH/$(basename "$d")-$(date +%Y%m%d%H%M%S)" 2>/dev/null
}

# ---- 版本号：从 csproj 读，避免脚本和工程两处不一致 ----
VER="$("$PY" - "$CSPROJ" <<'PY'
import re, sys, io
s = io.open(sys.argv[1], 'r', encoding='utf-8').read()
m = re.search(r'<Version>([^<]+)</Version>', s)
print(m.group(1).strip() if m else '1.0.0')
PY
)"
NAME="AkPassport-Win-$VER-win-x64"
OUT="$ROOT/dist/$NAME"
ZIP="$ROOT/dist/$NAME.zip"

echo "== 版本: $VER"
echo "== 产物: $OUT"

# ---- 0. 关掉正在运行的实例，避免 exe 被占用 ----
"$PY" -c "import os; os.system('taskkill /F /IM AkPassport.Win.exe >nul 2>&1')"

# ---- 1. 前端 ----
bash "$ROOT/_build_h5.sh" || { echo "!! H5 构建失败，终止"; exit 1; }
[ -f "$ROOT/src/AkPassport.Win/wwwroot/index.html" ] || { echo "!! wwwroot 里没有 index.html"; exit 1; }

# ---- 2. 发布 ----
# 注意：**不要**用 PublishSingleFile。
# 单文件模式会把 Assets / wwwroot 这两份 Content 丢掉（发布目录里只剩 exe），
# 结果就是装完打开一片空白。文件夹模式才是 WinUI 非打包应用的稳妥方式。
fresh_dir "$OUT"
dotnet publish "$PROJ" -c Release -r win-x64 --self-contained true -p:Platform=x64 \
  -p:WindowsAppSDKSelfContained=true \
  -p:DebugType=None -p:DebugSymbols=false \
  -o "$OUT" 2>&1 | tail -5

[ -f "$OUT/AkPassport.Win.exe" ] || { echo "!! 发布失败：没有 AkPassport.Win.exe"; exit 1; }
[ -f "$OUT/wwwroot/index.html" ] || { echo "!! 发布产物缺 wwwroot —— 装完会白屏"; exit 1; }
[ -f "$OUT/Assets/app.ico" ] || { echo "!! 发布产物缺 Assets"; exit 1; }

# ---- 2.5 独立更新器 ----
# 主程序没法覆盖正在运行的自己，自动更新必须靠这个独立 exe。
# 它必须进 zip（绿色版解压后就在 exe 旁边），也必须进 payload.zip（安装版同理）。
UPD_PROJ="$ROOT/src/AkPassport.Updater/AkPassport.Updater.csproj"
UPD_OUT="$ROOT/src/AkPassport.Updater/out"
echo "== 构建更新器 =="
dotnet publish "$UPD_PROJ" -c Release -r win-x64 --self-contained true -o "$UPD_OUT" 2>&1 | tail -3
[ -f "$UPD_OUT/AkPassport.Updater.exe" ] || { echo "!! 更新器构建失败"; exit 1; }
cp -f "$UPD_OUT/AkPassport.Updater.exe" "$OUT/AkPassport.Updater.exe"
echo "updater -> $OUT/AkPassport.Updater.exe"

# 开发目录也放一份，免得在 Debug 里点「下载并安装」提示更新器缺失
for d in "$ROOT"/src/AkPassport.Win/bin/x64/Debug/*/; do
  [ -d "$d" ] && cp -f "$UPD_OUT/AkPassport.Updater.exe" "$d" 2>/dev/null
done

# ---- 3. 附带说明文件 ----
cp -f "$ROOT/docs/使用说明.txt" "$OUT/使用说明.txt" 2>/dev/null || true

# ---- 3.5 安装程序 ----
# payload.zip = 发布目录的内容（注意：条目直接在根，不能带顶层目录名），
# 安装时解压到用户选的目录。内嵌进 AkPassportSetup.exe，所以安装包是单文件。
SETUP_DIR="$ROOT/installer/AkPassportSetup"
"$PY" - "$OUT" "$SETUP_DIR/payload.zip" <<'PY'
import os, sys, zipfile
src, out = sys.argv[1], sys.argv[2]
# 用 'w' 覆盖写（会截断），不要 os.remove：大文件删除可能被安全策略拦掉，
# 拦掉之后内嵌的就是上一版 payload。
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for base, dirs, files in os.walk(src):
        for f in files:
            p = os.path.join(base, f)
            z.write(p, os.path.relpath(p, src))   # 条目直接在根，不能带顶层目录名
print('payload ->', out, round(os.path.getsize(out) / 1048576, 1), 'MB')
PY

SETUP_OUT="$ROOT/dist/方舟通行证谷子查询工具-$VER-安装程序"
rm -rf "$SETUP_OUT"
dotnet publish "$SETUP_DIR/AkPassportSetup.csproj" -c Release -r win-x64 \
  --self-contained true -p:Platform=x64 -p:Version="$VER" \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None -p:DebugSymbols=false \
  -o "$SETUP_OUT" 2>&1 | tail -5

SETUP_EXE="$ROOT/dist/方舟通行证谷子查询工具-$VER-安装程序.exe"
if [ -f "$SETUP_OUT/AkPassportSetup.exe" ]; then
  mv -f "$SETUP_OUT/AkPassportSetup.exe" "$SETUP_EXE"
  rm -rf "$SETUP_OUT"
  echo "== 安装程序: $SETUP_EXE"
  # ★ GitHub Release 附件必须用 ASCII 名：实测中文文件名上传后中文会被吞掉，
  #   "方舟通行证谷子查询工具-1.0.0-安装程序.exe" 会变成 "-1.0.0-.exe"。
  #   所以同时产出一份 ASCII 别名的副本，本地分发用中文名、上传用这份。
  cp -f "$SETUP_EXE" "$ROOT/dist/AkPassport-Win-$VER-Setup.exe" 2>/dev/null \
    && echo "== 上传用别名: $ROOT/dist/AkPassport-Win-$VER-Setup.exe"
else
  echo "!! 安装程序构建失败（详见上面输出），绿色版 zip 仍然可用"
fi

# ---- 4. 打包 ----
"$PY" - "$OUT" "$ZIP" <<'PY'
import os, sys, zipfile
src, out = sys.argv[1], sys.argv[2]
# 同样用 'w' 覆盖写，不做删除（见上面 payload 的说明）
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for base, dirs, files in os.walk(src):
        for f in files:
            p = os.path.join(base, f)
            z.write(p, os.path.relpath(p, os.path.dirname(src)))
print('zip ->', out, round(os.path.getsize(out) / 1048576, 1), 'MB')
PY

echo "== 完成: $ZIP"
