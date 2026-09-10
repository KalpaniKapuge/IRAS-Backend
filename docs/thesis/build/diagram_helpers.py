"""Shared PIL helpers for hand-drawn box/arrow style thesis diagrams."""
from PIL import Image, ImageDraw, ImageFont

INK = (26, 26, 26)
ACCENT = (37, 84, 199)
ACCENT_FILL = (234, 240, 254)
GREY_FILL = (243, 243, 243)
WHITE = (255, 255, 255)


def load_font(size, bold=False):
    names = ["timesbd.ttf", "Times New Roman Bold.ttf"] if bold else ["times.ttf", "Times New Roman.ttf"]
    for n in names:
        try:
            return ImageFont.truetype(n, size)
        except Exception:
            continue
    # fall back to default PIL font family available on most Windows systems
    fallback = ["arialbd.ttf"] if bold else ["arial.ttf"]
    for n in fallback:
        try:
            return ImageFont.truetype(n, size)
        except Exception:
            continue
    return ImageFont.load_default()


def new_canvas(w, h, bg=WHITE):
    img = Image.new("RGB", (w, h), bg)
    return img, ImageDraw.Draw(img)


def wrap_text(draw, text, font, max_width):
    words = text.split()
    lines, cur = [], ""
    for w in words:
        trial = (cur + " " + w).strip()
        if draw.textlength(trial, font=font) <= max_width:
            cur = trial
        else:
            if cur:
                lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def box(draw, x, y, w, h, title=None, subtitle=None, fill=WHITE, outline=INK, width=2,
        title_font=None, sub_font=None, dashed=False, radius=8):
    if dashed:
        dash_len, gap = 8, 6
        # top/bottom
        for yy in (y, y + h):
            xx = x
            while xx < x + w:
                draw.line([(xx, yy), (min(xx + dash_len, x + w), yy)], fill=outline, width=width)
                xx += dash_len + gap
        for xx in (x, x + w):
            yy = y
            while yy < y + h:
                draw.line([(xx, yy), (xx, min(yy + dash_len, y + h))], fill=outline, width=width)
                yy += dash_len + gap
        draw.rectangle([x, y, x + w, y + h], fill=fill)
    else:
        draw.rounded_rectangle([x, y, x + w, y + h], radius=radius, fill=fill, outline=outline, width=width)
    cy = y + 14
    if title:
        tf = title_font or load_font(15, bold=True)
        for line in wrap_text(draw, title, tf, w - 16):
            tw = draw.textlength(line, font=tf)
            draw.text((x + w / 2 - tw / 2, cy), line, font=tf, fill=INK)
            cy += 19
    if subtitle:
        sf = sub_font or load_font(12)
        cy += 3
        for part in subtitle.split("\n"):
            for line in wrap_text(draw, part, sf, w - 16):
                tw = draw.textlength(line, font=sf)
                draw.text((x + w / 2 - tw / 2, cy), line, font=sf, fill=INK)
                cy += 16
    return (x, y, w, h)


def arrow(draw, p1, p2, color=ACCENT, width=2, dashed=False, head=10):
    x1, y1 = p1
    x2, y2 = p2
    if dashed:
        import math
        dist = math.hypot(x2 - x1, y2 - y1)
        steps = max(int(dist / 12), 1)
        for i in range(steps):
            if i % 2 == 0:
                sx = x1 + (x2 - x1) * i / steps
                sy = y1 + (y2 - y1) * i / steps
                ex = x1 + (x2 - x1) * (i + 1) / steps
                ey = y1 + (y2 - y1) * (i + 1) / steps
                draw.line([(sx, sy), (ex, ey)], fill=color, width=width)
    else:
        draw.line([(x1, y1), (x2, y2)], fill=color, width=width)
    import math
    ang = math.atan2(y2 - y1, x2 - x1)
    for da in (0.5, -0.5):
        ax = x2 - head * math.cos(ang - da * 0.6)
        ay = y2 - head * math.sin(ang - da * 0.6)
        draw.line([(x2, y2), (ax, ay)], fill=color, width=width)


def label(draw, x, y, text, font=None, color=INK, bg=WHITE, anchor="mm", pad=3):
    f = font or load_font(12)
    tw = draw.textlength(text, font=f)
    th = 14
    if anchor == "mm":
        rect = [x - tw / 2 - pad, y - th / 2 - pad, x + tw / 2 + pad, y + th / 2 + pad]
        tx, ty = x - tw / 2, y - th / 2
    else:
        rect = [x - pad, y - pad, x + tw + pad, y + th + pad]
        tx, ty = x, y
    draw.rectangle(rect, fill=bg)
    draw.text((tx, ty), text, font=f, fill=color)


def actor(draw, cx, top_y, label_text, font=None):
    """Simple stick-figure UML actor."""
    head_r = 10
    draw.ellipse([cx - head_r, top_y, cx + head_r, top_y + 2 * head_r], outline=INK, width=2)
    body_top = top_y + 2 * head_r
    body_bottom = body_top + 26
    draw.line([(cx, body_top), (cx, body_bottom)], fill=INK, width=2)
    draw.line([(cx - 16, body_top + 8), (cx + 16, body_top + 8)], fill=INK, width=2)
    draw.line([(cx, body_bottom), (cx - 14, body_bottom + 22)], fill=INK, width=2)
    draw.line([(cx, body_bottom), (cx + 14, body_bottom + 22)], fill=INK, width=2)
    f = font or load_font(13, bold=True)
    tw = draw.textlength(label_text, font=f)
    draw.text((cx - tw / 2, body_bottom + 26), label_text, font=f, fill=INK)
    return body_bottom + 26


def ellipse_usecase(draw, cx, cy, w, h, text, font=None, fill=ACCENT_FILL):
    draw.ellipse([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], fill=fill, outline=INK, width=2)
    f = font or load_font(11)
    lines = wrap_text(draw, text, f, w - 14)
    ty = cy - (len(lines) * 14) / 2
    for line in lines:
        tw = draw.textlength(line, font=f)
        draw.text((cx - tw / 2, ty), line, font=f, fill=INK)
        ty += 14


def save(img, path):
    img.save(path, "PNG")
    print("wrote", path)
