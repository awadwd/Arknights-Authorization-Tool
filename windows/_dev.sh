#!/usr/bin/env bash
# 开发流程：结束进程 -> 编译 -> 启动 -> 用 CDP 截图
# 用法: bash _dev.sh <route> <out.png> [waitSeconds]
set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXE="$ROOT/src/AkPassport.Win/bin/x64/Debug/net10.0-windows10.0.26100.0/AkPassport.Win.exe"
ROUTE="${1:-pages/list/list}"
OUT="${2:-$ROOT/_shell.png}"
WAIT="${3:-9}"
NODE="${NODE:-node}"

if [ "${SKIP_BUILD:-0}" != "1" ]; then
  taskkill /F /IM AkPassport.Win.exe >/dev/null 2>&1
  sleep 2
  dotnet build "$ROOT/src/AkPassport.Win/AkPassport.Win.csproj" -c Debug -p:Platform=x64 -v m 2>&1 | grep -E "^.*error|个错误" | head -5
fi

taskkill /F /IM AkPassport.Win.exe >/dev/null 2>&1
sleep 1
("$EXE" --route="$ROUTE" >/dev/null 2>&1 &)
sleep "$WAIT"

NODE_PATH="${NODE_PATH:-}" "$NODE" -e "
const p=require('puppeteer-core');
(async()=>{
  const b=await p.connect({browserURL:'http://127.0.0.1:9222',defaultViewport:null});
  const pages=await b.pages();
  const pg=pages.find(x=>x.url().includes('app.local'))||pages[0];
  const i=await pg.evaluate(()=>({w:innerWidth,h:innerHeight,root:getComputedStyle(document.documentElement).fontSize,url:location.href}));
  console.log(JSON.stringify(i));
  await pg.screenshot({path:'$OUT'});
  b.disconnect();
})().catch(e=>{console.log('CDP_FAIL',e.message)})
"
