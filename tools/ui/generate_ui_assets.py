#!/usr/bin/env python3
"""
Road Builder Racer - Retro Arcade Workshop UI Asset Generator

Requirements:  python3 -m pip install Pillow

Examples:
    python generate_ui_assets.py --style smooth
    python generate_ui_assets.py --style pixel
    python generate_ui_assets.py --style all --output Generated

No network access. No generative AI. Original procedural artwork.
Writes PNGs, a ledger CSV and a contact sheet per style.
"""

from PIL import Image, ImageDraw, ImageFont
import argparse
import csv
import math
from pathlib import Path
from datetime import date

PALETTE = {
    "charcoal": "#252B36",
    "orange": "#F4A340",
    "red": "#D94B40",
    "cream": "#E7E0CF",
    "teal": "#6B9B87",
    "ink": "#18202A",
    "steel": "#59636D",
    "light_steel": "#AAB0B1",
    "paper": "#D9C9A5",
    "white": "#FFFFFF",
}

OUTPUT = Path("Generated")
TODAY = date.today().isoformat()


def color(name):
    return PALETTE.get(name, name)


def rgba(value, alpha=255):
    return (*Image.new("RGB", (1, 1), color(value)).getpixel((0, 0)), alpha)


class Art:
    def __init__(self, size, style="smooth"):
        self.w, self.h = size
        self.style = style
        self.scale = 3 if style == "smooth" else 1
        self.im = Image.new("RGBA", (self.w * self.scale, self.h * self.scale), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def p(self, value):
        if self.style == "pixel":
            return int(round(value / 4) * 4)
        return int(value * self.scale)

    def xy(self, coords):
        return tuple(self.p(v) for v in coords)

    def polygon(self, points, fill, outline=None, width=1):
        pts = [self.xy(point) for point in points]
        self.d.polygon(pts, fill=color(fill) if isinstance(fill, str) else fill)
        if outline:
            self.d.line(pts + [pts[0]], fill=color(outline), width=max(1, self.p(width)), joint="curve")

    def rect(self, box, fill, outline=None, width=1):
        self.d.rectangle(self.xy(box), fill=color(fill) if isinstance(fill, str) else fill,
                         outline=color(outline) if outline else None, width=max(1, self.p(width)))

    def ellipse(self, box, fill, outline=None, width=1):
        self.d.ellipse(self.xy(box), fill=color(fill) if isinstance(fill, str) else fill,
                       outline=color(outline) if outline else None, width=max(1, self.p(width)))

    def arc(self, box, start, end, fill, width=1):
        self.d.arc(self.xy(box), start, end, fill=color(fill), width=max(1, self.p(width)))

    def line(self, points, fill, width=1):
        self.d.line([self.xy(point) for point in points], fill=color(fill),
                    width=max(1, self.p(width)), joint="curve")

    def text(self, position, value, fill="cream", size=18):
        try:
            font = ImageFont.truetype("DejaVuSans-Bold.ttf", self.p(size))
        except OSError:
            try:
                font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", self.p(size))
            except OSError:
                font = ImageFont.load_default()
        self.d.text(self.xy(position), value, fill=color(fill), font=font)

    def finish(self):
        if self.style == "smooth":
            return self.im.resize((self.w, self.h), Image.Resampling.LANCZOS)
        return self.im


def rivet(a, x, y, r=6):
    a.ellipse((x - r, y - r, x + r, y + r), "light_steel", "ink", 2)
    a.ellipse((x - 2, y - 2, x + 2, y + 2), "steel")


def metal_plate(a, w, h, title=""):
    a.polygon([(12, 20), (w - 16, 12), (w - 8, h - 18), (20, h - 10), (8, h - 28)], "ink")
    a.polygon([(18, 8), (w - 24, 8), (w - 8, 24), (w - 8, h - 20), (w - 22, h - 8),
               (20, h - 8), (8, h - 22), (8, 24)], "steel", "ink", 4)
    a.polygon([(24, 18), (w - 30, 18), (w - 18, 30), (w - 18, h - 30), (w - 30, h - 18),
               (26, h - 18), (18, h - 30), (18, 28)], "charcoal", "light_steel", 2)
    for x, y in [(24, 24), (w - 24, 24), (24, h - 24), (w - 24, h - 24)]:
        rivet(a, x, y)
    if title:
        a.text((38, 34), title, "cream", 24)


def paper_ticket(a, w, h):
    a.polygon([(8, 10), (w - 8, 10), (w - 8, h - 25), (w - 18, h - 10), (w - 30, h - 24),
               (w - 42, h - 10), (8, h - 10)], "paper", "charcoal", 3)
    for y in range(24, h - 25, 18):
        a.ellipse((w - 15, y, w - 7, y + 8), "cream")


def tape(a, x, y, w=70, h=22):
    a.polygon([(x, y + 3), (x + w - 5, y), (x + w, y + h - 4), (x + 6, y + h), (x + 2, y + h // 2)],
              "cream", "paper", 1)


def make_panel(style, output):
    a = Art((640, 360), style)
    metal_plate(a, 640, 360, "WORKSHOP CONTROL")
    tape(a, 490, 4, 105, 35)
    a.text((40, 110), "ROAD BUILDER", "orange", 30)
    a.text((40, 155), "RACER DIVISION", "cream", 19)
    a.line([(40, 210), (580, 210)], "steel", 3)
    a.text((40, 235), "GARAGE / SERVICE / RACING", "teal", 15)
    a.finish().save(output)


def make_button(style, output, state="normal"):
    colours = {"normal": ("orange", "red"), "pressed": ("red", "charcoal"),
               "disabled": ("steel", "charcoal"), "selected": ("teal", "charcoal")}
    front, back = colours[state]
    a = Art((320, 112), style)
    a.polygon([(20, 18), (300, 12), (310, 88), (295, 100), (22, 104), (10, 90)], back, "ink", 4)
    a.polygon([(20, 10), (295, 10), (310, 24), (300, 84), (285, 96), (20, 96), (10, 82)], front, "ink", 3)
    a.line([(25, 20), (280, 20)], "cream", 2)
    for x in (32, 288):
        rivet(a, x, 54, 4)
    label = {"pressed": "PRESSED", "disabled": "LOCKED", "selected": "SELECTED"}.get(state, "START RACE")
    a.text((105, 40), label, "charcoal" if state == "normal" else "cream", 19)
    a.finish().save(output)


def make_ticket(style, output):
    a = Art((400, 280), style)
    paper_ticket(a, 400, 280)
    tape(a, 35, 0, 85, 30)
    a.text((35, 45), "VEHICLE PASS", "charcoal", 22)
    a.line([(35, 82), (340, 82)], "steel", 2)
    a.polygon([(80, 175), (105, 130), (270, 130), (315, 170), (320, 190), (75, 190)], "red", "charcoal", 4)
    a.ellipse((115, 175, 165, 225), "charcoal")
    a.ellipse((255, 175, 305, 225), "charcoal")
    a.text((35, 235), "CLASS / 001", "charcoal", 15)
    a.finish().save(output)


def make_ribbon(style, output):
    a = Art((256, 96), style)
    a.polygon([(20, 12), (235, 12), (220, 48), (235, 84), (20, 84), (35, 48)], "red", "ink", 3)
    a.text((66, 32), "NEW RECORD", "cream", 18)
    a.finish().save(output)


def make_badge(style, output):
    a = Art((128, 128), style)
    a.polygon([(64, 5), (112, 28), (120, 76), (64, 122), (8, 76), (16, 28)], "orange", "ink", 4)
    a.ellipse((35, 32, 93, 90), "cream", "charcoal", 4)
    star = []
    for i in range(10):
        ang = math.radians(-90 + i * 36)
        rad = 22 if i % 2 == 0 else 9
        star.append((64 + math.cos(ang) * rad, 61 + math.sin(ang) * rad))
    a.polygon(star, "red")
    a.finish().save(output)


def make_gauge(style, output):
    a = Art((320, 72), style)
    a.polygon([(12, 12), (305, 12), (312, 24), (305, 60), (12, 60), (6, 48)], "charcoal", "ink", 3)
    a.polygon([(18, 20), (190, 20), (190, 52), (18, 52)], "orange")
    for x in range(22, 300, 28):
        a.line([(x, 12), (x, 19)], "cream", 2)
    a.finish().save(output)


def make_icon(style, output, name):
    a = Art((64, 64), style)
    if name == "coin":
        a.ellipse((8, 8, 56, 56), "orange", "ink", 4)
        a.ellipse((16, 16, 48, 48), "cream", "charcoal", 2)
        a.text((24, 17), "$", "charcoal", 24)
    elif name == "gear":
        a.ellipse((10, 10, 54, 54), "steel", "ink", 4)
        a.ellipse((23, 23, 41, 41), "charcoal", "cream", 3)
        for angle in range(0, 360, 45):
            x1 = 32 + math.cos(math.radians(angle)) * 21
            y1 = 32 + math.sin(math.radians(angle)) * 21
            x2 = 32 + math.cos(math.radians(angle)) * 29
            y2 = 32 + math.sin(math.radians(angle)) * 29
            a.line([(x1, y1), (x2, y2)], "light_steel", 7)
    elif name == "lock":
        a.arc((17, 4, 47, 43), 180, 360, "cream", 6)
        a.polygon([(12, 29), (52, 29), (52, 58), (12, 58)], "red", "ink", 3)
        a.ellipse((28, 37, 36, 45), "cream")
    elif name == "star":
        pts = []
        for i in range(10):
            ang = math.radians(-90 + i * 36)
            rad = 27 if i % 2 == 0 else 12
            pts.append((32 + math.cos(ang) * rad, 32 + math.sin(ang) * rad))
        a.polygon(pts, "orange", "ink", 3)
    elif name == "heart":
        a.ellipse((8, 8, 35, 37), "red")
        a.ellipse((29, 8, 56, 37), "red")
        a.polygon([(8, 25), (56, 25), (32, 58)], "red", "ink", 2)
    elif name == "arrow_left":
        a.polygon([(8, 32), (38, 8), (38, 23), (58, 23), (58, 41), (38, 41), (38, 56)], "cream", "ink", 2)
    elif name == "arrow_right":
        a.polygon([(56, 32), (26, 8), (26, 23), (6, 23), (6, 41), (26, 41), (26, 56)], "cream", "ink", 2)
    a.finish().save(output)


def generate(style):
    folder = OUTPUT / style
    folder.mkdir(parents=True, exist_ok=True)
    assets = []

    def export(filename, fn):
        path = folder / filename
        fn(style, path)
        assets.append(path)

    export("UI_Panel_Metal_Main_Normal.png", make_panel)
    export("UI_Card_Vehicle_Ticket_Normal.png", make_ticket)
    export("UI_Ribbon_NewRecord.png", make_ribbon)
    export("UI_Badge_Star_Normal.png", make_badge)
    export("UI_HUD_Progress_Ink.png", make_gauge)
    for state in ["normal", "pressed", "disabled", "selected"]:
        export(f"UI_Button_Arcade_Primary_{state.title()}.png",
               lambda s, p, state=state: make_button(s, p, state))
    for icon in ["coin", "gear", "lock", "star", "heart", "arrow_left", "arrow_right"]:
        export(f"UI_Icon_{icon.title()}.png", lambda s, p, icon=icon: make_icon(s, p, icon))
    return assets


def contact_sheet(assets, style):
    """One PNG with every asset of a batch on a dark checker, so the batch can be reviewed at a glance."""
    cell, pad = 340, 16
    cols = 4
    rows = (len(assets) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * (cell + pad) + pad, rows * (cell + pad + 20) + pad), (60, 64, 72, 255))
    d = ImageDraw.Draw(sheet)
    for i, path in enumerate(assets):
        im = Image.open(path).convert("RGBA")
        k = min(cell / im.width, cell / im.height, 1.0)
        if k < 1.0:
            im = im.resize((int(im.width * k), int(im.height * k)), Image.Resampling.LANCZOS)
        x = pad + (i % cols) * (cell + pad)
        y = pad + (i // cols) * (cell + pad + 20)
        sheet.alpha_composite(im, (x + (cell - im.width) // 2, y + (cell - im.height) // 2))
        d.text((x, y + cell + 2), path.stem[:44], fill=(230, 230, 230, 255))
    out = OUTPUT / f"contact_sheet_{style}.png"
    sheet.save(out)
    print(f"Contact sheet: {out}")


def write_ledger(assets):
    ledger = OUTPUT / "UI_Asset_Ledger.csv"
    ledger.parent.mkdir(parents=True, exist_ok=True)
    write_header = not ledger.exists()
    with ledger.open("a", newline="", encoding="utf-8") as file:
        writer = csv.writer(file)
        if write_header:
            writer.writerow(["asset_id", "filename", "source", "source_url", "license", "author",
                             "download_date", "modification", "theme_slot", "status"])
        for path in assets:
            writer.writerow([path.stem, path.name, "Procedural Python/Pillow generator", "",
                             "Original procedural artwork - ownership confirmation required",
                             "Project team - confirm ownership", TODAY, "Generated locally",
                             "REQUIRES_SPRITE_SLOT_MAPPING", "GENERATED_REVIEW"])
    print(f"Ledger: {ledger}")


def main():
    parser = argparse.ArgumentParser(description="Road Builder Racer UI asset generator")
    parser.add_argument("--style", choices=["smooth", "pixel", "all"], default="all")
    parser.add_argument("--output", default="Generated")
    args = parser.parse_args()

    global OUTPUT
    OUTPUT = Path(args.output)
    styles = ["smooth", "pixel"] if args.style == "all" else [args.style]
    all_assets = []
    for style in styles:
        print(f"Generating {style} assets...")
        batch = generate(style)
        contact_sheet(batch, style)
        all_assets.extend(batch)
    write_ledger(all_assets)
    print(f"Generated {len(all_assets)} assets in {OUTPUT.resolve()}")
    print("Review artwork and complete SpriteSlot mapping before import.")


if __name__ == "__main__":
    main()
