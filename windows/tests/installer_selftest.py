#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
方舟通行证谷子查询工具 —— 安装器无人值守自测

用途
    在没有人工点「下一步」的情况下，把安装器在各种边界情况下跑一遍，
    用退出码 + 文件系统 + 注册表三方对账，判断「能不能正常安装」。

为什么要有这个脚本
    安装器之前只有 GUI 一条路径，无法自动化验证；而且「设置 → 应用」里
    显示 1.0.0 而不是 1.0.0-beta.4 这类问题，人工点一遍很容易漏掉。

安全边界（很重要）
    · 所有安装都落在 _test/sandbox 下，绝不碰真实的安装目录。
    · 本机若已安装，先把 卸载注册表键 / 开机自启值 / 两个 .lnk 备份并从
      系统里摘掉（模拟「干净机器」），测试结束在 finally 里原样还原。
    · 卸载一律显式 /dir=，绝不依赖注册表推断，避免误删真实安装。
    · /purge 只对沙箱里自建的数据目录执行。

用法
    python tests/installer_selftest.py                # 全跑
    python tests/installer_selftest.py --keep         # 保留沙箱便于排查
    python tests/installer_selftest.py --list         # 只列场景
    python tests/installer_selftest.py -s S08 -s S09  # 只跑指定场景
"""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import time
import winreg
from pathlib import Path

CREATE_NO_WINDOW = 0x08000000
HKCU = winreg.HKEY_CURRENT_USER
UNINSTALL_KEY = r"Software\Microsoft\Windows\CurrentVersion\Uninstall\AkPassportWin"
RUN_KEY = r"Software\Microsoft\Windows\CurrentVersion\Run"
RUN_VALUE = "AkPassportWin"
APP_NAME = "方舟通行证谷子查询工具"

ROOT = Path(__file__).resolve().parent.parent
DIST = ROOT / "dist"
SANDBOX = ROOT / "_test" / "sandbox"
BACKUP = ROOT / "_test" / "_guard"
LOG = Path(os.environ.get("LOCALAPPDATA", "")) / "AkPassport-setup.log"

# 退出码约定（与 SetupExit 一一对应）
EXIT = {
    0: "成功",
    1: "用户取消",
    2: "未安装",
    3: "安装包不完整",
    4: "磁盘空间不足",
    5: "目标不可写/被占用",
    6: "其它失败",
}

results: list[tuple[str, str, bool, str]] = []


# ---------------------------------------------------------------- 基础设施

def record(sid: str, name: str, ok: bool, detail: str = "") -> None:
    results.append((sid, name, ok, detail))
    print(f"  [{'PASS' if ok else 'FAIL'}] {sid} {name}" + (f" — {detail}" if detail else ""))


def run(exe: Path, args: list[str], timeout: int = 300, env: dict | None = None):
    """跑一次安装器，返回 (退出码, 耗时秒)。静默模式不应弹任何窗口。"""
    t0 = time.time()
    try:
        p = subprocess.run(
            [str(exe), *args],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=timeout,
            creationflags=CREATE_NO_WINDOW,
            env=env,
        )
        return p.returncode, time.time() - t0
    except subprocess.TimeoutExpired:
        return -9, time.time() - t0


def files_under(path: Path) -> int:
    if not path.exists():
        return 0
    return sum(1 for _ in path.rglob("*") if _.is_file())


def wait_gone(path: Path, seconds: float = 20.0) -> bool:
    """卸载是延迟删除（ping 5 秒），要等一会儿。"""
    end = time.time() + seconds
    while time.time() < end:
        if not path.exists():
            return True
        time.sleep(0.5)
    return not path.exists()


def kill_app() -> None:
    subprocess.run(["taskkill", "/F", "/IM", "AkPassport.Win.exe"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                   creationflags=CREATE_NO_WINDOW)


def app_running() -> bool:
    p = subprocess.run(["tasklist", "/FI", "IMAGENAME eq AkPassport.Win.exe"],
                       stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                       creationflags=CREATE_NO_WINDOW, text=True, encoding="utf-8", errors="replace")
    return "AkPassport.Win.exe" in (p.stdout or "")


def log_tail(lines: int = 6) -> str:
    try:
        data = LOG.read_text(encoding="utf-8", errors="replace").splitlines()
        return " | ".join(data[-lines:])
    except Exception:
        return ""


# ---------------------------------------------------------------- 注册表 / 快捷方式

def snapshot_values(root, path: str) -> dict | None:
    try:
        with winreg.OpenKey(root, path, 0, winreg.KEY_READ) as k:
            count = winreg.QueryInfoKey(k)[1]
            return {winreg.EnumValue(k, i)[0]: winreg.EnumValue(k, i)[1:] for i in range(count)}
    except FileNotFoundError:
        return None
    except OSError:
        return None


def delete_tree(root, path: str) -> None:
    try:
        winreg.DeleteKey(root, path)
    except OSError:
        pass


def restore_values(root, path: str, data: dict | None) -> None:
    delete_tree(root, path)
    if not data:
        return
    with winreg.CreateKeyEx(root, path, 0, winreg.KEY_WRITE) as k:
        for name, (value, typ) in data.items():
            winreg.SetValueEx(k, name, 0, typ, value)


def snapshot_value(root, path: str, name: str):
    try:
        with winreg.OpenKey(root, path, 0, winreg.KEY_READ) as k:
            return winreg.QueryValueEx(k, name)
    except OSError:
        return None


def delete_value(root, path: str, name: str) -> None:
    try:
        with winreg.OpenKey(root, path, 0, winreg.KEY_SET_VALUE) as k:
            winreg.DeleteValue(k, name)
    except OSError:
        pass


def shell_desktop() -> Path:
    """桌面可能被重定向（本机就在 D:/Users/huang/桌面），必须按注册表解析。
    用 %USERPROFILE%/Desktop 会查到一个空壳目录，把「已建快捷方式」误判成没建。"""
    try:
        with winreg.OpenKey(
            HKCU,
            r"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders",
        ) as k:
            v, _ = winreg.QueryValueEx(k, "Desktop")
            return Path(os.path.expandvars(v))
    except OSError:
        return Path(os.environ["USERPROFILE"]) / "Desktop"


def desktop_lnk() -> Path:
    return shell_desktop() / f"{APP_NAME}.lnk"


def startmenu_lnk() -> Path:
    return Path(os.environ["APPDATA"]) / r"Microsoft\Windows\Start Menu\Programs" / f"{APP_NAME}.lnk"


class Guard:
    """把本机真实的安装痕迹先收起来，测试完再原样放回。"""

    def __init__(self):
        self.uninstall = snapshot_values(HKCU, UNINSTALL_KEY)
        self.run_val = snapshot_value(HKCU, RUN_KEY, RUN_VALUE)
        self.lnks: list[tuple[Path, Path]] = []

    def enter(self):
        BACKUP.mkdir(parents=True, exist_ok=True)
        print("== 隔离真实安装状态")
        print(f"   注册表: {'有' if self.uninstall else '无'}"
              f"（{self.uninstall.get('DisplayVersion') if self.uninstall else '—'}）")
        print(f"   开机自启: {'有' if self.run_val else '无'}")

        delete_tree(HKCU, UNINSTALL_KEY)
        delete_value(HKCU, RUN_KEY, RUN_VALUE)

        for i, lnk in enumerate([desktop_lnk(), startmenu_lnk()]):
            if lnk.exists():
                dst = BACKUP / f"{i}.lnk"
                try:
                    shutil.copy2(lnk, dst)
                    self.lnks.append((dst, lnk))
                    lnk.unlink()
                    print(f"   快捷方式已暂存: {lnk.name}")
                except Exception as ex:
                    print(f"   !! 暂存失败 {lnk}: {ex}")

    def exit(self):
        print("== 还原真实安装状态")
        restore_values(HKCU, UNINSTALL_KEY, self.uninstall)
        if self.run_val is not None:
            try:
                with winreg.CreateKeyEx(HKCU, RUN_KEY, 0, winreg.KEY_WRITE) as k:
                    winreg.SetValueEx(k, RUN_VALUE, 0, self.run_val[1], self.run_val[0])
            except OSError:
                pass
        for src, dst in self.lnks:
            try:
                shutil.copy2(src, dst)
            except Exception as ex:
                print(f"   !! 恢复快捷方式失败 {dst}: {ex}")
        print(f"   已还原：注册表 {'有' if self.uninstall else '无'}，"
              f"自启 {'有' if self.run_val else '无'}，快捷方式 {len(self.lnks)} 个")

        # 还原校验：别让「还原失败」静默通过，否则下次跑测试会以错的基线开始
        now = snapshot_values(HKCU, UNINSTALL_KEY)
        if (now is None) != (self.uninstall is None):
            print("   !! 还原校验失败：注册表存在性与测试前不一致")
        elif now and self.uninstall:
            moved = [k for k in self.uninstall if now.get(k) != self.uninstall[k]]
            if moved:
                print(f"   !! 还原校验失败：{moved} 与测试前不同")
            else:
                print("   还原校验：注册表值一致")
        else:
            print("   还原校验：注册表已回到「未安装」")


# ---------------------------------------------------------------- 场景

def setup_exe() -> Path:
    cands = sorted(DIST.glob(f"{APP_NAME}-*-安装程序.exe"), key=lambda p: p.stat().st_mtime)
    if not cands:
        sys.exit("找不到安装程序，先跑 bash _build_setup.sh")
    return cands[-1]


def pkg_version() -> str:
    import re
    s = (ROOT / "src/AkPassport.Win/AkPassport.Win.csproj").read_text(encoding="utf-8")
    m = re.search(r"<Version>([^<]+)</Version>", s)
    return m.group(1).strip() if m else "1.0.0"


def fresh(path: Path) -> Path:
    """清空并重建沙箱目录。"""
    if path.exists():
        subprocess.run(["cmd", "/c", "rmdir", "/s", "/q", str(path)],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                       creationflags=CREATE_NO_WINDOW)
    path.mkdir(parents=True, exist_ok=True)
    return path


def installed_ok(d: Path) -> tuple[bool, str]:
    need = ["AkPassport.Win.exe", "AkPassport.Updater.exe",
            os.path.join("wwwroot", "index.html"), os.path.join("Assets", "app.ico")]
    miss = [n for n in need if not (d / n).exists()]
    if miss:
        return False, "缺 " + ", ".join(miss)
    n = files_under(d)
    return (n > 100), f"{n} 个文件"


def scenarios(exe: Path, version: str):
    base = SANDBOX / "install"

    # ---------- S01 全新安装 ----------
    def s01():
        d = fresh(base / "s01")
        rc, dt = run(exe, [f"/dir={d}", "/silent"])
        ok, detail = installed_ok(d) if rc == 0 else (False, f"rc={rc}")
        record("S01", "全新静默安装到空目录", rc == 0 and ok, f"rc={rc} {detail} {dt:.1f}s")

    # ---------- S02 注册表 ----------
    def s02():
        vals = snapshot_values(HKCU, UNINSTALL_KEY) or {}
        d = str(base / "s01")

        def v(name, default=None):
            pair = vals.get(name)          # 快照存的是 (值, 类型)
            return pair[0] if pair else default

        checks = {
            "InstallLocation": v("InstallLocation") == d,
            "DisplayVersion": v("DisplayVersion") == version,
            "DisplayName": v("DisplayName") == APP_NAME,
            "QuietUninstallString": "/uninstall /quiet" in str(v("QuietUninstallString")),
            "EstimatedSize": int(v("EstimatedSize", 0) or 0) > 10000,
        }
        bad = [k for k, ok in checks.items() if not ok]
        record("S02", "卸载注册表项正确（含完整版本号）", not bad,
               f"DisplayVersion={v('DisplayVersion')} 不合格={bad or '无'}")

    # ---------- S03 幂等覆盖 ----------
    def s03():
        d = base / "s01"
        rc, dt = run(exe, [f"/dir={d}", "/silent"])
        ok, detail = installed_ok(d) if rc == 0 else (False, f"rc={rc}")
        record("S03", "重复覆盖安装（幂等）", rc == 0 and ok, f"rc={rc} {detail} {dt:.1f}s")

    # ---------- S04 旧版残留清理 ----------
    def s04():
        d = base / "s01"
        (d / "old_leftover.txt").write_text("stale", encoding="utf-8")
        (d / "stale_dir").mkdir(exist_ok=True)
        (d / "stale_dir" / "x.dll").write_text("stale", encoding="utf-8")
        rc, _ = run(exe, [f"/dir={d}", "/silent"])
        gone = not (d / "old_leftover.txt").exists() and not (d / "stale_dir").exists()
        record("S04", "覆盖安装清掉上一版残留", rc == 0 and gone,
               f"rc={rc} 残留{'已清' if gone else '仍在'}")

    # ---------- S05 只读文件 ----------
    def s05():
        d = base / "s01"
        ro = d / "readonly_old.dat"
        ro.write_text("x", encoding="utf-8")
        subprocess.run(["attrib", "+R", str(ro)], creationflags=CREATE_NO_WINDOW,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        rc, _ = run(exe, [f"/dir={d}", "/silent"])
        gone = not ro.exists()
        ok, detail = installed_ok(d) if rc == 0 else (False, f"rc={rc}")
        record("S05", "目录里有只读文件仍能覆盖", rc == 0 and gone and ok,
               f"rc={rc} 只读文件{'已清' if gone else '仍在'} {detail}")

    # ---------- S06 中文 + 空格路径 ----------
    def s06():
        d = fresh(SANDBOX / "中文 路径" / "安装 目录")
        rc, dt = run(exe, [f"/dir={d}", "/silent"])
        ok, detail = installed_ok(d) if rc == 0 else (False, f"rc={rc}")
        record("S06", "中文与空格路径", rc == 0 and ok, f"rc={rc} {detail} {dt:.1f}s")

    # ---------- S07 深层不存在路径 ----------
    def s07():
        d = SANDBOX / "deep" / "a" / "b" / "c" / "d" / "e"
        if d.exists():
            shutil.rmtree(d, ignore_errors=True)
        rc, _ = run(exe, [f"/dir={d}", "/silent"])
        ok, detail = installed_ok(d) if rc == 0 else (False, f"rc={rc}")
        record("S07", "多层不存在的路径自动创建", rc == 0 and ok, f"rc={rc} {detail}")

    # ---------- S08 目标是一个文件 ----------
    def s08():
        d = SANDBOX / "as_file"
        d.parent.mkdir(parents=True, exist_ok=True)
        d.write_text("i am a file", encoding="utf-8")
        rc, _ = run(exe, [f"/dir={d}", "/silent"])
        intact = d.is_file() and d.read_text(encoding="utf-8") == "i am a file"
        record("S08", "目标路径是文件时明确失败且不破坏原文件", rc == 5 and intact,
               f"rc={rc}({EXIT.get(rc, '?')}) 原文件{'完好' if intact else '被破坏'}")

    # ---------- S09 损坏包（无内嵌 payload） ----------
    def s09():
        bad = ROOT / "_test" / "nopayload_out" / "AkPassportSetup.exe"
        if not bad.exists():
            record("S09", "安装包不完整时明确拒绝", False,
                   "缺少样本 _test/nopayload_out/AkPassportSetup.exe")
            return
        d = fresh(SANDBOX / "nopayload")
        rc, _ = run(bad, [f"/dir={d}", "/silent"])
        empty = files_under(d) == 0
        record("S09", "安装包不完整时明确拒绝", rc == 3 and empty,
               f"rc={rc}({EXIT.get(rc, '?')}) 目录{'空' if empty else '被写入'}")

    # ---------- S10 程序正在运行 ----------
    def s10():
        d = base / "s01"
        exe_app = d / "AkPassport.Win.exe"
        env = dict(os.environ)
        # 让它用沙箱数据目录，别把真实数据搅了
        env["LOCALAPPDATA"] = str(SANDBOX / "appdata")
        env["APPDATA"] = str(SANDBOX / "appdata_roaming")
        Path(env["LOCALAPPDATA"]).mkdir(parents=True, exist_ok=True)
        Path(env["APPDATA"]).mkdir(parents=True, exist_ok=True)
        try:
            subprocess.Popen([str(exe_app)], cwd=str(d), env=env,
                             stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            time.sleep(3)
            was = app_running()
            rc, _ = run(exe, [f"/dir={d}", "/silent"])
            time.sleep(1)
            still = app_running()
            ok, detail = installed_ok(d) if rc == 0 else (False, f"rc={rc}")
            record("S10", "程序运行中覆盖安装（自动结束占用进程）",
                   rc == 0 and ok and not still, f"启动={'是' if was else '否'} 安装后运行中={still} {detail}")
        finally:
            kill_app()

    # ---------- S11 无写权限 ----------
    def s11():
        d = fresh(SANDBOX / "readonly_dir")
        user = os.environ.get("USERNAME", "")
        subprocess.run(["icacls", str(d), "/deny", f"{user}:(OI)(CI)(W)"],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                       creationflags=CREATE_NO_WINDOW)
        try:
            rc, _ = run(exe, [f"/dir={d}", "/silent"])
            record("S11", "目标目录不可写时明确失败", rc == 5,
                   f"rc={rc}({EXIT.get(rc, '?')})")
        finally:
            subprocess.run(["icacls", str(d), "/remove:d", user],
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                           creationflags=CREATE_NO_WINDOW)

    # ---------- S12 快捷方式 ----------
    def s12():
        d = fresh(base / "s12")
        for p in (desktop_lnk(), startmenu_lnk()):
            if p.exists():
                p.unlink()
        rc, _ = run(exe, [f"/dir={d}", "/silent", "/desktop", "/startmenu"])
        dl, sl = desktop_lnk().exists(), startmenu_lnk().exists()
        record("S12", "按参数创建桌面/开始菜单快捷方式", rc == 0 and dl and sl,
               f"rc={rc} 桌面={dl} 开始菜单={sl}")

    # ---------- S13 默认不建快捷方式 ----------
    def s13():
        d = fresh(base / "s13")
        for p in (desktop_lnk(), startmenu_lnk()):
            if p.exists():
                p.unlink()
        rc, _ = run(exe, [f"/dir={d}", "/silent"])
        dl, sl = desktop_lnk().exists(), startmenu_lnk().exists()
        record("S13", "静默模式默认不污染桌面/开始菜单", rc == 0 and not dl and not sl,
               f"rc={rc} 桌面={dl} 开始菜单={sl}")

    # ---------- S14 静默卸载保留数据 ----------
    def s14():
        d = fresh(base / "s14")
        data = fresh(SANDBOX / "data_keep")
        (data / "preferences.json").write_text("{}", encoding="utf-8")
        rc1, _ = run(exe, [f"/dir={d}", "/silent"])
        if rc1 != 0:
            record("S14", "静默卸载并保留用户数据", False, f"前置安装失败 rc={rc1}")
            return
        rc2, _ = run(exe, [f"/uninstall", f"/dir={d}", "/quiet", "/keepdata",
                           f"/datadir={data}"])
        gone = wait_gone(d)
        reg = snapshot_values(HKCU, UNINSTALL_KEY)
        ok = rc2 == 0 and gone and reg is None and (data / "preferences.json").exists()
        record("S14", "静默卸载并保留用户数据", ok,
               f"rc={rc2} 目录{'已删' if gone else '仍在'} 注册表{'已清' if reg is None else '残留'} "
               f"数据{'在' if (data / 'preferences.json').exists() else '被删'}")

    # ---------- S15 重复卸载 ----------
    def s15():
        d = base / "nope"
        rc, _ = run(exe, ["/uninstall", f"/dir={d}", "/quiet"])
        record("S15", "卸载未安装的目标返回明确码", rc == 2,
               f"rc={rc}({EXIT.get(rc, '?')})")

    # ---------- S16 /purge 删指定数据目录 ----------
    def s16():
        d = fresh(base / "s16")
        data = fresh(SANDBOX / "AkPassport")          # 名字要能通过数据目录识别
        (data / "preferences.json").write_text("{}", encoding="utf-8")
        (data / "gacha.json").write_text("[]", encoding="utf-8")
        rc1, _ = run(exe, [f"/dir={d}", "/silent"])
        rc2, _ = run(exe, ["/uninstall", f"/dir={d}", "/quiet", "/purge", f"/datadir={data}"])
        ok = rc1 == 0 and rc2 == 0 and not data.exists()
        record("S16", "/purge 按参数删除用户数据", ok,
               f"安装 rc={rc1} 卸载 rc={rc2} 数据目录{'已删' if not data.exists() else '仍在'}")

    # ---------- S17 /purge 打错路径要拒绝 ----------
    def s17():
        d = fresh(base / "s17")
        wrong = fresh(SANDBOX / "not_a_data_dir")
        (wrong / "important.txt").write_text("keep me", encoding="utf-8")
        rc1, _ = run(exe, [f"/dir={d}", "/silent"])
        rc2, _ = run(exe, ["/uninstall", f"/dir={d}", "/quiet", "/purge", f"/datadir={wrong}"])
        survived = (wrong / "important.txt").exists()
        record("S17", "/purge 遇到可疑路径拒绝删除", rc1 == 0 and rc2 == 0 and survived,
               f"rc={rc2} 无关文件{'保留' if survived else '被误删'}")

    return [s01, s02, s03, s04, s05, s06, s07, s08,
            s09, s10, s11, s12, s13, s14, s15, s16, s17]


# ---------------------------------------------------------------- main

def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--keep", action="store_true", help="保留沙箱目录")
    ap.add_argument("--list", action="store_true", help="只列场景")
    ap.add_argument("-s", "--scenario", action="append", default=[], help="只跑指定场景，如 S08")
    args = ap.parse_args()

    exe = setup_exe()
    version = pkg_version()

    if args.list:
        for i in range(1, 18):
            print(f"S{i:02d}")
        return 0

    print("=" * 72)
    print("方舟通行证谷子查询工具 · 安装器无人值守自测")
    print("=" * 72)
    print(f"安装包   : {exe.name}  ({exe.stat().st_size / 1048576:.1f} MB)")
    print(f"包内版本 : {version}")
    print(f"沙箱     : {SANDBOX}")
    print(f"本机权限 : {'管理员' if _is_admin() else '普通用户'}")
    print(f"残留进程 : {'有' if app_running() else '无'}")
    print()

    if app_running():
        print("!! 检测到程序正在运行，先结束它，否则测不准")
        kill_app()
        time.sleep(1)

    if SANDBOX.exists() and not args.keep:
        subprocess.run(["cmd", "/c", "rmdir", "/s", "/q", str(SANDBOX)],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                       creationflags=CREATE_NO_WINDOW)
    SANDBOX.mkdir(parents=True, exist_ok=True)

    guard = Guard()
    guard.enter()
    t0 = time.time()

    try:
        print("\n== 执行场景")
        for fn in scenarios(exe, version):
            sid = "S" + fn.__name__[1:]
            if args.scenario and sid not in {s.upper() for s in args.scenario}:
                continue
            try:
                fn()
            except Exception as ex:
                record(sid, fn.__doc__ or "场景异常", False, f"{type(ex).__name__}: {ex}")
    finally:
        guard.exit()
        kill_app()
        if not args.keep:
            subprocess.run(["cmd", "/c", "rmdir", "/s", "/q", str(SANDBOX)],
                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                           creationflags=CREATE_NO_WINDOW)

    passed = sum(1 for r in results if r[2])
    total = len(results)
    print("\n" + "=" * 72)
    print(f"结果：{passed}/{total} 通过，用时 {time.time() - t0:.1f}s")
    if passed != total:
        print("未通过：")
        for sid, name, ok, detail in results:
            if not ok:
                print(f"  - {sid} {name} — {detail}")
        print("安装器日志尾部：" + (log_tail() or "(无)"))
    print("=" * 72)
    return 0 if passed == total else 1


def _is_admin() -> bool:
    try:
        import ctypes
        return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except Exception:
        return False


if __name__ == "__main__":
    sys.exit(main())
