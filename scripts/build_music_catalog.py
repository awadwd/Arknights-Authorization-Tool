import json, urllib.request, os, io, re

API = "https://monster-siren.hypergryph.com/api"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "static", "music", "catalog.json")

def get(path):
    req = urllib.request.Request(API + path, headers={"User-Agent": "AkPassport/1.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode("utf-8"))

songs = get("/songs")["data"]["list"]
albums = get("/albums")["data"]
amap = {a["cid"]: a for a in albums}

items = []
for s in songs:
    name = s.get("name", "")
    if "instrumental" not in name.lower():
        continue
    album = amap.get(s.get("albumCid"), {})
    clean = re.sub(r"\s*\(\s*instrumental\s*\)\s*$", "", name, flags=re.I).strip()
    items.append({
        "cid": s["cid"],
        "name": clean,
        "raw": name,
        "album": album.get("name", ""),
        "albumCid": s.get("albumCid", ""),
        "cover": album.get("coverUrl", ""),
    })

items.sort(key=lambda x: x["name"].lower())

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
    json.dump({"source": "monster-siren", "updated": "2026-11-13", "count": len(items), "list": items},
              f, ensure_ascii=False, indent=0, separators=(",", ":"))

print("instrumental:", len(items), "->", OUT)
print("sample:", items[0] if items else None)
