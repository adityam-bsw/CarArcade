#!/usr/bin/env python3
"""Generic smooth world backgrounds for the catalogue worlds that have no hand-made smooth set (own code, no generative AI, no downloads).

    python3 tools/art/make_bg_generic.py --output <dir> [--world id,id|all]

Same layer contract as make_bg_worlds.py: bg_<w>_sky_4x576, bg_<w>_sun_256, bg_<w>_far_2048x576, bg_<w>_mid_2048x576,
bg_<w>_near_2048x864 (props under about 190 px). Palettes come from SPECS below (SCENERY-style ramps: sky_top, sky_hor, haze, far, mid, near);
prop kinds: round, pine, rock, crystal, cactus, lolli, dead, kelp, puff, stack, spire, none. Layers wrap seamlessly in X.
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_bg_countryside as mc
from smoothart import *
from make_bg_countryside import W, H, HN, SS, pdist, env, cache_cols, paint_hill, mountains
from make_bg_worlds import sky_img, glow_disc, radial_bleed

def spec(sky_top, sky_hor, haze, far, mid, near, prop, sun="#FFF1C2", far_amp=200, accent="#FFFFFF", mprop=None, dark=False):
    return dict(sky_top=sky_top, sky_hor=sky_hor, haze=haze, far=far, mid=mid, near=near, prop=prop, sun=sun, far_amp=far_amp, accent=accent, mprop=mprop or prop, dark=dark)

# far/mid/near are (lit, shade) colour pairs.
SPECS = {
 "autumn-leaves": spec("#6AA9D8", "#F6E2BE", "#FBE9CF", ("#D9B79A", "#B9906F"), ("#D08A3C", "#A8602A"), ("#B5651F", "#7C3E14"), "round", accent="#E8742A"),
 "christmas":     spec("#2C4C7E", "#9DB9D9", "#DCE8F4", ("#C9D8EA", "#9FB5D0"), ("#E9F1F8", "#B9CBE0"), ("#F4F8FC", "#C5D3E4"), "pine", sun="#FFFFFF", accent="#D8323C"),
 "spring-blossom":spec("#7CC4F0", "#E6F4FF", "#F6FBFF", ("#B6D8D0", "#93BDB4"), ("#8FC98A", "#62A767"), ("#6CBF6A", "#459447"), "round", accent="#F7A9C9"),
 "summer":        spec("#3F9BEA", "#BFE6FF", "#E4F4FF", ("#8FC6D0", "#6AA5B5"), ("#5FBF62", "#369348"), ("#4DB04F", "#2C7F3C"), "round", sun="#FFF7C2", accent="#FFD23F"),
 "winter":        spec("#4A6E9E", "#BBD0E8", "#E5EEF8", ("#D2DFEE", "#A8BDD6"), ("#EAF2FA", "#BCCFE4"), ("#F7FAFD", "#CBD8E8"), "pine", sun="#FFFFFF"),
 "arctic":        spec("#3B8FC8", "#CFEFFF", "#EAF8FF", ("#BFE3F2", "#8CC4DE"), ("#DDF2FA", "#A9D3E6"), ("#EFFAFF", "#B9DDEE"), "crystal", sun="#FFFFFF", accent="#8FE0FF"),
 "canyon":        spec("#F29A5A", "#FFDDB0", "#FFEBCB", ("#E3926A", "#BC6C48"), ("#CC7447", "#9F5133"), ("#A85A38", "#793D26"), "rock", far_amp=240, accent="#6F8F55"),
 "coast":         spec("#4FB4EE", "#D8F2FF", "#EEF9FF", ("#8FD0E6", "#5FAFCF"), ("#F0DDA8", "#CFB67A"), ("#E9CF8E", "#BFA463"), "round", accent="#3FA066", far_amp=90),
 "farm":          spec("#69B6EE", "#D9EEFF", "#EEF7FF", ("#A9D2A0", "#86B67E"), ("#E3C76A", "#BCA24A"), ("#78B84F", "#4C8A36"), "round", accent="#C9552E", far_amp=110),
 "jungle":        spec("#2F8F6C", "#B8E8B8", "#D8F5D8", ("#5FA68A", "#3F8068"), ("#2F8A52", "#1F6339"), ("#1F7A3F", "#134F2A"), "round", sun="#F7FFC2", accent="#FF7A3D", far_amp=160),
 "wild-west":     spec("#F0A25E", "#FFE0B2", "#FFEDD1", ("#DDA27A", "#B77E58"), ("#CB8B58", "#A2663C"), ("#B8763F", "#8A5028"), "cactus", accent="#9A6A3A"),
 "mars":          spec("#8E3F3A", "#E5A58A", "#EBC1A6", ("#C47A63", "#9C5645"), ("#B2563F", "#83392B"), ("#8F3F2C", "#5E2519"), "rock", sun="#FFD9B8", accent="#E58A62", far_amp=170),
 "moon":          spec("#0C1022", "#2A3050", "#4B5278", ("#7C84A4", "#555C7C"), ("#9AA1BA", "#6B7391"), ("#B5BBD0", "#848BA6"), "rock", sun="#FFFFFF", accent="#5F6688", far_amp=150, dark=True),
 "industrial":    spec("#7E8FA6", "#D3DBE3", "#E1E6EC", ("#A8B4C2", "#8493A5"), ("#7F8C9B", "#5A6674"), ("#606B78", "#3F4853"), "stack", sun="#FFF1D0", accent="#F2A33C", far_amp=120),
 "synthwave":     spec("#1B1245", "#6B2A8F", "#C0489F", ("#4A2A80", "#321C5E"), ("#35206A", "#231450"), ("#1F1146", "#140A30"), "spire", sun="#FF7BD0", accent="#4DE3F0", dark=True),
 "candy-land":    spec("#F6A1D0", "#FFE3F1", "#FFF1F8", ("#F7C1DC", "#E39BC0"), ("#9FE0C8", "#6FC2A6"), ("#FFB6D8", "#E58BB8"), "lolli", sun="#FFFFFF", accent="#7ACFF0", far_amp=140),
 "cave":          spec("#150F2E", "#3A2A63", "#5A4690", ("#4C3A7E", "#2E2158"), ("#3A2A6A", "#241A4C"), ("#2A1D52", "#190F36"), "crystal", sun="#C9B6FF", accent="#8F6BFF", far_amp=180, dark=True),
 "cloud-kingdom": spec("#69B0F4", "#F2F9FF", "#FFFFFF", ("#DDEBFA", "#B4CDEA"), ("#F4F9FF", "#C7DBF2"), ("#FFFFFF", "#D7E5F6"), "puff", sun="#FFF8D6", accent="#FFD05A", far_amp=100),
 "haunted-halloween": spec("#2A1840", "#8F5A7A", "#B98AA0", ("#5E3B6E", "#43294F"), ("#3C2650", "#291A39"), ("#241636", "#150C22"), "dead", sun="#FFE8A0", accent="#FF8A1F", dark=True),
 "underwater":    spec("#0E5C96", "#5FC4D8", "#8ADCE6", ("#2D86AE", "#1E6A8E"), ("#1F7A96", "#145A74"), ("#C9B987", "#9C8A5A"), "kelp", sun="#E0FBFF", accent="#FF8A7A", far_amp=120),
 "volcano":       spec("#3A1A22", "#E2663A", "#F29A62", ("#7A3A3A", "#552428"), ("#5C2B2B", "#3C1A1D"), ("#3B1F20", "#241214"), "rock", sun="#FFD08A", accent="#FF6A1F", far_amp=230, dark=True),
}
for k, v in list(SPECS.items()):
    if isinstance(v, tuple): raise SystemExit("bad spec " + k)

def hx(c): return hexrgb(c)

def sun_img(sp, n=256):
    img = glow_disc(n, tuple(int(v) for v in hx(sp["sun"])), 0.65); c = n / 2
    img.paint(Mask(n, n, SS).circle(c, c, 46).cov(), vgrad(c - 46, c + 46, Ramp([(0, "#FFFFFF"), (1, sp["sun"])])))
    return radial_bleed(img)

def make_sky(sp):
    return sky_img(Ramp([(0, sp["sky_top"]), (0.62, sp["sky_hor"]), (1, sp["haze"])]))

def far_layer(sp, seed):
    mc.HAZE = hx(sp["haze"]); img = Image(W, H); amp = sp["far_amp"]
    back = mountains(340, amp, seed, 3)
    paint_hill(img, back, None, sp["far"][0], sp["far"][1], 0.40, 160)
    front = lambda x: 410 + 44 * env(x, seed + 3, 0.3) * wave_fbm(x, W, seed + 4, octaves=3, base=2) + 8 * wave_fbm(x, W, seed + 5, octaves=2, base=6)
    paint_hill(img, front, None, sp["far"][0], sp["far"][1], 0.32, 150)
    return img

def props(img, kind, spots, gc, sp, scale, h):
    """Draw simple two-tone props standing on the ground curve gc. spots: (x, size 0.7..1.2)."""
    body = Mask(W, h, SS, True); dark = Mask(W, h, SS, True); acc = Mask(W, h, SS, True); items = []
    for x, s in spots:
        g = gc(x) + 4; u = scale * s
        if kind == "round":
            body.capsule(x, g, x, g - 0.8 * u, 0.06 * u + 2); r = 0.34 * u
            dark.circle(x, g - 0.9 * u, r); dark.circle(x - r * .7, g - 0.7 * u, r * .75); dark.circle(x + r * .7, g - 0.72 * u, r * .75)
            acc.circle(x - r * .3, g - 1.0 * u, r * .22); acc.circle(x + r * .5, g - 0.8 * u, r * .18)
        elif kind == "pine":
            body.capsule(x, g, x, g - 0.2 * u, 0.04 * u + 2)
            for i in range(3):
                y1 = g - 0.14 * u - i * 0.3 * u; wd = (0.34 - i * 0.07) * u
                dark.polygon([(x - wd, y1), (x + wd, y1), (x, y1 - 0.55 * u)])
            acc.polygon([(x - 0.07 * u, g - 0.9 * u), (x + 0.07 * u, g - 0.9 * u), (x, g - 1.18 * u)])
        elif kind == "rock":
            for dx, dy, rx, ry in ((0, 0, 1.0, .62), (-.7, .18, .6, .38), (.75, .22, .55, .34)):
                dark.ellipse(x + dx * 0.4 * u, g - 0.16 * u + dy * 0.12 * u, rx * 0.4 * u, ry * 0.4 * u)
            acc.ellipse(x - 0.1 * u, g - 0.3 * u, 0.12 * u, 0.06 * u)
        elif kind == "crystal":
            for dx, hh in ((-0.16, 0.6), (0, 1.0), (0.17, 0.7)):
                dark.polygon([(x + (dx - 0.1) * u, g), (x + dx * u - 0.02 * u, g - hh * u), (x + dx * u + 0.04 * u, g - hh * u), (x + (dx + 0.1) * u, g)])
                acc.polygon([(x + dx * u - 0.02 * u, g - hh * u), (x + dx * u + 0.04 * u, g - hh * u), (x + dx * u + 0.01 * u, g - (hh + 0.12) * u)])
        elif kind == "cactus":
            r = 0.07 * u; hh = 0.9 * u; body.capsule(x, g, x, g - hh, r)
            for side, yy in ((-1, 0.45), (1, 0.6)):
                ax = x + side * r * 3.2; ay = g - hh * yy; body.polyline([(x, ay), (ax, ay), (ax, ay - 0.28 * u)], r * .7)
        elif kind == "lolli":
            body.capsule(x, g, x, g - 0.7 * u, 0.04 * u + 2)
            dark.circle(x, g - 0.85 * u, 0.28 * u); acc.circle(x, g - 0.85 * u, 0.15 * u)
        elif kind == "dead":
            body.capsule(x, g, x, g - 0.8 * u, 0.05 * u + 2)
            for sd, yy, L in ((-1, 0.5, 0.35), (1, 0.62, 0.3), (-1, 0.82, 0.22)):
                body.polyline([(x, g - yy * u), (x + sd * L * u, g - (yy + 0.2) * u)], 0.025 * u + 1.5)
            acc.circle(x + 0.45 * u, g - 0.1 * u, 0.1 * u)
        elif kind == "kelp":
            for dx in (-0.1, 0.0, 0.1):
                pts = [(x + dx * u + 0.05 * u * math.sin(i * 1.3 + x), g - i * 0.18 * u) for i in range(6)]
                body.polyline(pts, 0.035 * u + 1.5)
            acc.circle(x + 0.25 * u, g - 0.08 * u, 0.07 * u); acc.circle(x - 0.25 * u, g - 0.06 * u, 0.05 * u)
        elif kind == "puff":
            for dx, dy, r in ((0, 0.25, 0.3), (-0.3, 0.15, 0.22), (0.3, 0.17, 0.24), (0.12, 0.42, 0.22)):
                dark.circle(x + dx * u, g - dy * u, r * u)
        elif kind == "stack":
            w = 0.22 * u; body.polygon([(x - w, g), (x + w, g), (x + w * .8, g - 1.0 * u), (x - w * .8, g - 1.0 * u)])
            dark.polygon([(x - w * 1.2, g - 1.0 * u), (x + w * 1.2, g - 1.0 * u), (x + w * .8, g - 1.05 * u), (x - w * .8, g - 1.05 * u)])
            acc.polygon([(x - w, g - 0.5 * u), (x + w, g - 0.5 * u), (x + w * .9, g - 0.6 * u), (x - w * .9, g - 0.6 * u)])
        elif kind == "spire":
            w = 0.25 * u; body.polygon([(x - w, g), (x + w, g), (x + w, g - 0.9 * u), (x, g - 1.2 * u), (x - w, g - 0.9 * u)])
            acc.polygon([(x - w, g - 0.9 * u), (x + w, g - 0.9 * u), (x + w, g - 0.86 * u), (x - w, g - 0.86 * u)])
            acc.polygon([(x - w * .5, g - 0.5 * u), (x + w * .5, g - 0.5 * u), (x + w * .5, g - 0.46 * u), (x - w * .5, g - 0.46 * u)])
        else: continue
    # colours: body is a darker structural tone, dark = foliage/rock mass, acc = accent
    return body, dark, acc

def place(rnd, count, gap, scale_lo, scale_hi):
    spots = []
    for _ in range(count * 40):
        if len(spots) >= count: break
        x = rnd.uniform(0, W)
        if all(pdist(x, s[0]) > gap for s in spots): spots.append((x, rnd.uniform(scale_lo, scale_hi)))
    return spots

def paint_props(img, kind, spots, gc, sp, scale, h, rim_hi=None):
    if kind == "none": return
    body, dark, acc = props(img, kind, spots, gc, sp, scale, h)
    lit, shade = hx(sp["mid"][0]), hx(sp["mid"][1])
    nl, ns = hx(sp["near"][0]), hx(sp["near"][1])
    mass = {"round": ("#5DB45E", "#2F7F45"), "pine": ("#2F7A55", "#1E5A3E"), "rock": (sp["mid"][0], sp["mid"][1]), "crystal": (sp["accent"], sp["mid"][1]),
            "lolli": (sp["accent"], sp["near"][1]), "dead": (sp["mid"][1], sp["near"][1]), "kelp": ("#3FAE7A", "#1F7F58"), "puff": (sp["near"][0], sp["far"][1]),
            "stack": (sp["mid"][0], sp["mid"][1]), "spire": (sp["mid"][0], sp["mid"][1]), "cactus": ("#5DAA5A", "#2E6B44")}.get(kind, ("#5DB45E", "#2F7F45"))
    if sp["prop"] == "round" and sp["accent"] != "#FFFFFF" and sp.get("tree") is None:
        pass
    ymin = 0; ymax = h
    img.paint(body.cov(), vgrad(ymin, ymax, Ramp([(0, nl_hex(sp, 0)), (1, nl_hex(sp, 1))])) if kind not in ("cactus", "kelp") else vgrad(0, h, Ramp([(0, mass[0]), (1, mass[1])])))
    img.paint(dark.cov(), vgrad(0, h, Ramp([(0, mass[0]), (1, mass[1])])))
    img.paint(acc.cov(), sp["accent"])

def nl_hex(sp, i):
    # trunk/structure colour: dark brown for organic props, near ground tone for hard-edged ones
    return {"round": ("#8A5A36", "#5E3A22"), "pine": ("#7A4E2E", "#53331D"), "dead": ("#3A2A44", "#241830"), "lolli": ("#FFFFFF", "#E8E8F0"),
            "stack": (sp["near"][0], sp["near"][1]), "spire": (sp["mid"][1], sp["near"][1])}.get(sp["prop"], (sp["near"][0], sp["near"][1]))[i]

def mid_layer(sp, seed):
    mc.HAZE = hx(sp["haze"]); img = Image(W, H); rnd = random.Random(seed)
    ground = mountains(470, 80, seed + 7, 3, 0.07); gc = cache_cols(ground)
    paint_hill(img, ground, None, sp["mid"][0], sp["mid"][1], 0.14, 140)
    paint_props(img, sp["mprop"], place(rnd, 16, 90, 0.8, 1.1), gc, sp, 62, H)
    return img

def near_layer(sp, seed):
    mc.HAZE = hx(sp["haze"]); img = Image(W, HN); rnd = random.Random(seed)
    ground = lambda x: 772 + 30 * env(x, 61, 0.35) * (0.7 * wave_fbm(x, W, 62, octaves=2, base=2) + 0.3 * wave_fbm(x, W, 63, octaves=3, base=5))
    gc = cache_cols(ground)
    paint_hill(img, ground, None, sp["near"][0], sp["near"][1], 0.0, 220, ss=SS, h=HN, rim=0.30)
    paint_props(img, sp["prop"], place(rnd, 9, 170, 0.8, 1.2), gc, sp, 170, HN)
    # small ground tufts / pebbles in the accent tone
    t = Mask(W, HN, SS, True)
    for _ in range(120):
        x = rnd.uniform(0, W); g = gc(x) + rnd.uniform(3, 30); r = rnd.uniform(3, 7); t.ellipse(x, g, r * 1.3, r * .6)
    img.paint(t.cov(), sp["accent"], 0.55)
    return img

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--world", default="all"); a = ap.parse_args()
    ids = list(SPECS) if a.world == "all" else a.world.split(",")
    for n, wid in enumerate(ids):
        sp = SPECS[wid]; key = wid.replace("-", ""); d = os.path.join(a.output, wid); os.makedirs(d, exist_ok=True); seed = 100 + 37 * n
        out = {"bg_%s_sky_4x576" % key: make_sky(sp), "bg_%s_sun_256" % key: sun_img(sp), "bg_%s_far_2048x576" % key: far_layer(sp, seed),
               "bg_%s_mid_2048x576" % key: mid_layer(sp, seed + 11), "bg_%s_near_2048x864" % key: near_layer(sp, seed + 23)}
        for name, im in out.items(): write_png(im, os.path.join(d, name + ".png")); 
        print(wid, "done", flush=True)

if __name__ == "__main__": main()
