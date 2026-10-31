#!/usr/bin/env python3
"""把 H5 产物里的 YOUR_KNOW_CLOUD_CLIENT_ID 换成真实知晓云 ClientID。

为什么不在仓库里直接写真实 ID：上游是开源仓库，产物里的占位符是给所有人看的模板，
README 也写明「填你自己的 ClientID」。所以真实 ID 只放在本地未跟踪的
config/knowcloud.local.json 里，构建时注入到 .app 内那一份产物，
仓库里的 wwwroot 永远保持脱敏。

用法：inject-knowcloud.py <wwwroot 目录>
"""

import json
import os
import sys

PLACEHOLDER = "YOUR_KNOW_CLOUD_CLIENT_ID"

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONFIG = os.path.join(ROOT, "config", "knowcloud.local.json")


def load_config():
    """优先环境变量，其次本地配置文件。都缺就返回 None（保持占位符）。"""
    main = os.environ.get("AKPASSPORT_KC_MAIN", "").strip()
    ends = os.environ.get("AKPASSPORT_KC_ENDS", "").strip()
    if main and ends:
        return {"main": main, "ends": ends}

    if not os.path.isfile(CONFIG):
        return None
    try:
        with open(CONFIG, encoding="utf-8") as handle:
            data = json.load(handle)
    except (OSError, ValueError) as error:
        print("   config/knowcloud.local.json 读不了：%s" % error)
        return None

    main = str(data.get("main", "")).strip()
    ends = str(data.get("ends", "")).strip()
    if not main or not ends:
        print("   配置里缺 main 或 ends")
        return None
    return {"main": main, "ends": ends}


def main():
    if len(sys.argv) < 2:
        print("用法：inject-knowcloud.py <wwwroot 目录>")
        return 2

    target = os.path.join(sys.argv[1], "assets")
    if not os.path.isdir(target):
        print("   找不到 assets 目录：%s" % target)
        return 2

    config = load_config()
    if config is None:
        print("   跳过：没有 config/knowcloud.local.json（知晓云功能保持关闭）")
        print("   需要的话：cp config/knowcloud.example.json config/knowcloud.local.json")
        return 0

    touched = 0
    replaced = 0
    for name in sorted(os.listdir(target)):
        if not name.endswith(".js"):
            continue
        path = os.path.join(target, name)
        try:
            with open(path, encoding="utf-8") as handle:
                text = handle.read()
        except (OSError, UnicodeDecodeError):
            continue
        if PLACEHOLDER not in text:
            continue
        # 终末地包自成一套 ClientID，文件名前缀带 package_ArknightEnds-
        client_id = config["ends"] if name.startswith("package_ArknightEnds-") else config["main"]
        count = text.count(PLACEHOLDER)
        text = text.replace(PLACEHOLDER, client_id)
        with open(path, "w", encoding="utf-8") as handle:
            handle.write(text)
        touched += 1
        replaced += count

    if touched:
        print("   %d 个文件，%d 处占位符已替换" % (touched, replaced))
    else:
        print("   没有剩余占位符（可能已经注入过）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
