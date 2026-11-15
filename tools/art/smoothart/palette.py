"""Palette tokens from Docs/design/ART_BIBLE.md section 6. Never write raw colours in generators; add a token here."""
import math

def clamp(v, lo=0.0, hi=1.0):
    return lo if v < lo else hi if v > hi else v

def smoothstep(a, b, x):
    t = clamp((x - a) / (b - a)) if b != a else 0.0
    return t * t * (3 - 2 * t)

def hexrgb(h):
    h = h.lstrip("#")
    return (float(int(h[0:2], 16)), float(int(h[2:4], 16)), float(int(h[4:6], 16)))

def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t)

class Ramp:
    """Multi-stop colour ramp. Ramp([(0, c0), (0.5, c1), (1, c2)])(t) -> (r, g, b). Colours may be hex strings or tuples."""
    def __init__(self, stops):
        self.stops = [(p, hexrgb(c) if isinstance(c, str) else c) for p, c in stops]
    def __call__(self, t):
        s = self.stops
        if t <= s[0][0]:
            return s[0][1]
        for i in range(1, len(s)):
            if t <= s[i][0]:
                p0, c0 = s[i - 1]; p1, c1 = s[i]
                return lerp(c0, c1, (t - p0) / (p1 - p0) if p1 != p0 else 1.0)
        return s[-1][1]

# UI tokens (binding) plus proposed world additions. Values are hex strings; use hexrgb() or Ramp to consume.
PAL = {
    "DeepInk": "#18202A", "WorkshopCharcoal": "#252B36", "RacingOrange": "#F4A340", "SignalRed": "#D94B40",
    "WarmCream": "#E7E0CF", "WorkshopTeal": "#6B9B87", "Steel": "#59636D",
    "RedDeep": "#8E2A2A", "GoldLight": "#F6C945", "GoldDeep": "#B88A1E",
    "IceLight": "#E6F6FF", "IceMid": "#8CC8F0", "IceDeep": "#3F7FBF",
    "MudLight": "#9A6B3E", "MudMid": "#6B4426", "MudDeep": "#3C2515",
    "GlassLight": "#F2FFFF", "GlassMid": "#9FE4EE", "GlassDeep": "#3B8EA6",
    "SteelLight": "#8A939B", "SteelDark": "#3B434D",
}
# One scenery ramp per world (sky top, sky horizon, far, mid, near, haze).
SCENERY = {
    "countryside": dict(sky_top="#5BB6F0", sky_hor="#CDEBFF", far="#9CC9C4", mid="#6FB07A", near="#3E8A4F", haze="#E9F6FF"),
    "desert":      dict(sky_top="#FFB35C", sky_hor="#FFE2B0", far="#E3A27B", mid="#C97C54", near="#9A5A3A", haze="#FFF1D6"),
    "night_city":  dict(sky_top="#141B3A", sky_hor="#3A3F7A", far="#2B3260", mid="#222850", near="#161A38", haze="#5560A8"),
}

# Gameplay sprite colours (vehicles, wheels, pickups). Gold is the reward colour code; red is warnings only, so the Classic
# car uses a cherry red that is darker and bluer than SignalRed (still flagged in FORGE_NOTES: Bible 6 suggests RacingOrange).
PAL.update({
    "CarRed": "#D8323C", "CarRedLight": "#F27A6E", "CarRedDeep": "#7E1F2E",
    "CarCyan": "#18B8E8", "CarCyanLight": "#8CE6FF", "CarCyanDeep": "#0C6A9A",
    "CarOrange": "#F28C1E", "CarOrangeLight": "#FFC066", "CarOrangeDeep": "#B24E12",
    "GlassSky": "#BFE6F7", "GlassSkyDeep": "#5FA6CC",
    "TyreLight": "#6A7582", "TyreMid": "#444D59", "TyreDeep": "#2C333D",
    "GoldMid": "#E9A92B", "HeartRed": "#F0506E", "HeartLight": "#FF93A5", "HeartDeep": "#B02A4A",
    "InkPurple": "#7B3FC4", "InkPurpleLight": "#A877E8", "InkPurpleDeep": "#4A1F86", "OverloadCyan": "#60D6E6",
})
