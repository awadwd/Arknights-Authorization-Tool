# -*- coding: utf-8 -*-
"""生成安卓启动图（替换 DCloud 默认的白底图标页）。

设计：深色工业底 + 细网格 + 暖橘光晕，中间放应用图标（圆角遮罩），
下方中文名 + 英文小标签。底部留白，不做任何贴边元素（不同机型刘海/手势条差异大）。
"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGO = os.path.join(ROOT, "static", "app-logo.png")
OUTDIR = os.path.join(ROOT, "static", "splash")
FONT_CN = "C:/Windows/Fonts/msyh.ttc"
FONT_EN = "C:/Windows/Fonts/consola.ttf"

SIZES = [(480, 800), (720, 1280), (1080, 1920)]

BG_TOP = (13, 15, 18)
BG_BOTTOM = (6, 7, 9)
AMBER = (241, 198, 68)
INK = (244, 245, 242)
MUTED = (150, 154, 156)


def vgrad(w, h, top, bottom):
    img = Image.new("RGB", (1, h))
    px = img.load()
    for y in range(h):
        t = y / max(1, h - 1)
        px[0, y] = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
    return img.resize((w, h), Image.BILINEAR)


def grid(w, h, step, color, alpha):
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for x in range(0, w, step):
        d.line([(x, 0), (x, h)], fill=color + (alpha,), width=1)
    for y in range(0, h, step):
        d.line([(0, y), (w, y)], fill=color + (alpha,), width=1)
    return layer


def glow(w, h, cx, cy, r, color):
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color + (46,))
    return layer.filter(ImageFilter.GaussianBlur(r // 2))


def rounded(img, radius):
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, img.size[0] - 1, img.size[1] - 1], radius=radius, fill=255)
    out = img.copy()
    out.putalpha(mask)
    return out


def build(w, h):
    base = vgrad(w, h, BG_TOP, BG_BOTTOM).convert("RGBA")
    base = Image.alpha_composite(base, grid(w, h, max(24, w // 22), (255, 255, 255), 8))

    cx, cy = w // 2, int(h * 0.465)
    base = Image.alpha_composite(base, glow(w, h, cx, cy, int(w * 0.33), AMBER))

    # 图标
    size = int(w * 0.235)
    logo = Image.open(LOGO).convert("RGBA").resize((size, size), Image.LANCZOS)
    logo = rounded(logo, int(size * 0.22))
    # 一点投影
    shadow = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse([size // 2, size // 2, size * 3 // 2, size * 3 // 2], fill=(0, 0, 0, 120))
    shadow = shadow.filter(ImageFilter.GaussianBlur(size // 6))
    base.alpha_composite(shadow, (cx - size, cy - size + int(size * 0.06)))
    base.alpha_composite(logo, (cx - size // 2, cy - size // 2))

    d = ImageDraw.Draw(base)

    # 中文名
    f_cn = ImageFont.truetype(FONT_CN, int(w * 0.052))
    title = "方舟通行证谷子查询工具"
    tw = d.textlength(title, font=f_cn)
    d.text((cx - tw / 2, cy + size * 0.72), title, font=f_cn, fill=INK)

    # 英文标签
    f_en = ImageFont.truetype(FONT_EN, int(w * 0.026))
    label = "AKPASSPORT  ·  ARKNIGHTS"
    lw = d.textlength(label, font=f_en)
    d.text((cx - lw / 2, cy + size * 0.72 + int(w * 0.082)), label, font=f_en, fill=MUTED)

    # 顶部细线装饰
    line_y = int(h * 0.155)
    lw2 = int(w * 0.10)
    d.line([(cx - lw2, line_y), (cx + lw2, line_y)], fill=AMBER + (150,), width=max(1, w // 540))

    return base.convert("RGB")


os.makedirs(OUTDIR, exist_ok=True)
for w, h in SIZES:
    img = build(w, h)
    p = os.path.join(OUTDIR, "%dx%d.png" % (w, h))
    img.save(p, "PNG", optimize=True)
    print("wrote", p, img.size)

# 供 manifest 引用的稳定文件名
mapping = {(480, 800): "hdpi", (720, 1280): "xhdpi", (1080, 1920): "xxhdpi"}
for w, h in SIZES:
    src = os.path.join(OUTDIR, "%dx%d.png" % (w, h))
    dst = os.path.join(OUTDIR, mapping[(w, h)] + ".png")
    if src != dst:
        os.replace(src, dst)
print(sorted(os.listdir(OUTDIR)))
