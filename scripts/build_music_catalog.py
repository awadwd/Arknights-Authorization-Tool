# -*- coding: utf-8 -*-
"""生成首页音乐条用的曲库（精选版）。

之前这脚本抓的是全部 130 首 Instrumental，列表太长。现在改成白名单精选：
只留「生命流 + 几首方舟知名 BGM」，同名有纯音乐版的优先用纯音乐版。

用法：python scripts/build_music_catalog.py
输出：static/music/catalog.json
"""
import json
import urllib.request
import os
import io
import re

API = "https://monster-siren.hypergryph.com/api"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "static", "music", "catalog.json")

# 精选曲目（顺序即列表顺序）。写曲名即可，脚本自动找 cid；
# 若存在 "<曲名> (Instrumental)" 的纯音乐版，优先用它。
PICKS = [
    "生命流",                 # 明日方舟 OST1，主界面 BGM
    "觉醒",                   # 焰烬曙明 OST
    "Boiling Blood",          # 经典战斗曲
    "Operation Pyrite",       # 危机合约 黄铁
    "Operation Blade",        # 危机合约 利刃
    "Renegade",
    "Requiem",
    "Ständchen",
    "ManiFesto",              # 音律联觉 原声 EP
    "Everything's Alright",   # 二周年
]


def get(path):
    req = urllib.request.Request(API + path, headers={"User-Agent": "AkPassport/1.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode("utf-8"))


def norm(s):
    return re.sub(r"\s+", " ", (s or "")).strip().lower()


def main():
    songs = get("/songs")["data"]["list"]
    albums = {a["cid"]: a for a in get("/albums")["data"]}

    by_name = {}
    for s in songs:
        by_name.setdefault(norm(s.get("name")), []).append(s)

    items = []
    missing = []
    for pick in PICKS:
        base = by_name.get(norm(pick), [])
        if not base:
            missing.append(pick)
            continue
        # 优先取 "(Instrumental)" 版本，没有就用原曲
        inst = by_name.get(norm(pick + " (Instrumental)"), [])
        s = (inst or base)[0]
        album = albums.get(s.get("albumCid"), {})
        clean = re.sub(r"\s*[（(]\s*instrumental\s*[)）]\s*$", "", s["name"], flags=re.I).strip()
        items.append({
            "cid": s["cid"],
            "name": clean,
            "raw": s["name"],
            "album": album.get("name", ""),
            "albumCid": s.get("albumCid", ""),
            "cover": album.get("coverUrl", ""),
        })

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(
            {"source": "monster-siren", "updated": "2026-11-13", "count": len(items), "list": items},
            f, ensure_ascii=False, indent=0, separators=(",", ":"),
        )

    print("精选:", len(items), "首 ->", OUT)
    for it in items:
        print("  ", it["cid"], "|", it["name"], "|", it["album"])
    if missing:
        print("!! 没找到:", missing)


if __name__ == "__main__":
    main()
