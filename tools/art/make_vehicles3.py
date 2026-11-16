import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_vehicles2 as m
from make_vehicles2 import *
SK = T("#F4FAFF", "#C9D6E2", "#7C8CA0")
RED = T("#FF9A8A", "#E8433A", "#8E2A2A")
SKIN = T("#FFE0C0", "#F7C09A", "#D08C6A")
def helmet(c, hx, hy, r, col):
    c.disc(hx + 0.3, hy, r, SKIN, ink=1.8)
    c.part([(hx - 2.4, hy + 0.9, 3), (hx - 2.3, hy - 1.6, 6), (hx, hy - 2.6, 6), (hx + 2.3, hy - 1.4, 5), (hx + 2.6, hy + 0.2, 2)], col, ink=1.6, rim=2.0)
    c.part([(hx + 0.8, hy - 0.3, 1), (hx + 2.7, hy - 0.5, 1), (hx + 2.7, hy + 0.9, 2), (hx + 0.8, hy + 0.9, 1)], T("#E4F6FF", "#7FC4EA", "#2F6FA0"), ink=1.0)  # goggles

@design("vehicle-snowmobile")
def snowmobile(c):
    # ski + strut first
    c.part([(31.0, 18.4, 2), (31.5, 17.0, 2), (49.0, 17.0, 2), (51.6, 13.6, 3), (52.0, 15.6, 2), (50.5, 19.0, 4), (31.0, 19.0, 3)], SK, ink=0, rim=2.0)
    c.tube([(37.0, 12.5), (36.0, 17.6)], 1.2, DSTEEL)
    c.tube([(44.0, 12.5), (44.4, 17.6)], 1.2, DSTEEL)
    # track pod with tread, bogie wheels
    trk, tm = c.part([(3.0, 19.0, 6), (1.0, 15.0, 7), (4.0, 11.4, 7), (23.0, 11.4, 7), (27.0, 15.0, 8), (24.0, 19.0, 6)], T("#6B7885", "#3B434D", "#171C22"), rim=2.4, ink=0)
    inner, _ = c.part([(4.8, 17.2, 4), (3.6, 15.0, 4), (5.4, 13.0, 4), (22.0, 13.0, 4), (24.2, 15.0, 4), (22.0, 17.2, 4)], T("#59636D", "#3B434D", "#252B36"), rim=0)
    for x, y in ((7.0, 15.2), (13.5, 15.6), (20.0, 15.2)):
        c.disc(x, y, 1.35 * c.S / 2.0, T("#E7EDF2", "#9AA3AD", "#59636D"), ink=1.4)
    for i in range(26):          # tread lugs on the belt
        t = 1.5 + i * 1.0
        c.line([(t, 11.4 if 4 < t < 23 else 13), (t - 0.2, 12.4 if 4 < t < 23 else 14)], 0.5, "#0E1216", clip=trk, opacity=0.8) if False else None
    for i in range(24):
        x = 2.4 + i * 1.0
        c.line([(x, 18.4), (x + 0.15, 19.1)], 0.7, "#0E1216", clip=trk, opacity=0.9)
        c.line([(x, 11.4), (x + 0.1, 12.2)], 0.7, "#0E1216", clip=trk, opacity=0.7) if 4 < x < 23 else None
    # seat
    c.part([(4.5, 9.8, 4), (5.5, 7.6, 5), (17.0, 7.4, 5), (19.0, 9.6, 4)], T("#4A5560", "#2B333D", "#171C22"), rim=2.0, ink=0)
    # cowl / hood body
    cov, body = c.body([(10.0, 12.4, 5), (13.5, 9.4, 8), (28.0, 8.6, 8), (36.5, 8.0, 7), (43.0, 8.8, 8), (51.0, 11.6, 10), (51.2, 14.2, 5), (40.0, 14.6, 8), (28.0, 14.4, 6), (14.0, 14.4, 6)],
                       RED, rim=3.0, split=(0.5, 0.9))
    c.line([(15.0, 12.0), (49.5, 12.6)], 1.5, "#FFF1D6", clip=cov, opacity=0.95)
    c.line([(24.0, 10.2), (36.0, 9.7)], 1.2, "#8E2A2A", clip=cov, opacity=0.6)
    for i in range(4):  # side vents
        c.line([(27.0 + i * 1.2, 10.6), (27.6 + i * 1.2, 11.6)], 0.9, "#8E2A2A", clip=cov, opacity=0.8)
    c.lamp(48.2, 10.6, 50.6, 12.0, HEAD, rad=5)
    # windshield + handlebar
    c.pane([(31.0, 8.6, 0), (33.4, 4.6, 5), (35.4, 4.6, 4), (36.6, 8.6, 0)], "#5FA6CC", streak=[(32.0, 8.0), (33.6, 5.6)])
        # driver: seated upright
    pants = T("#4A6FA8", "#2F4F86", "#1B2F58"); jk = T("#FFE08A", "#F6B730", "#B07818")
    c.tube([(14.5, 8.2), (21.0, 8.4), (23.0, 12.6)], 2.3, pants, ink=1.3)
    c.part([(21.4, 13.4, 2), (21.8, 11.8, 2), (25.4, 11.8, 3), (25.6, 13.4, 2)], T("#59636D", "#3B434D", "#252B36"), ink=1.1)
    c.part([(12.6, 9.2, 4), (12.8, 5.4, 6), (15.6, 3.8, 6), (19.0, 4.2, 5), (19.2, 8.0, 4), (17.0, 9.6, 3)], jk, rim=2.0, ink=1.4)
    c.tube([(27.5, 9.0), (27.6, 6.6)], 1.0, DSTEEL)
    c.tube([(26.2, 6.4), (29.6, 6.2)], 1.0, DSTEEL)
    c.tube([(17.6, 5.2), (22.4, 7.6), (28.0, 6.4)], 1.7, jk, ink=1.0)
    c.disc(28.2, 6.4, 0.8 * c.S / 2.0, T("#3B434D", "#252B36", "#171C22"), ink=1.0)
    helmet(c, 17.0, 2.9, 1.9 * c.S / 2.0, T("#FFFFFF", "#E6ECF2", "#A9B6C6"))
    c.gloss(cov, 30, 9.6, 14, 2, 0.35)
    c.line([(0, 0), (0, 0)], 0.1, "#000000")

@design("vehicle-snowboard")
def snowboard(c):
    # board with turned-up tips, dark underside, bindings
    c.part([(1.0, 17.6, 2), (1.6, 15.7, 2), (4.0, 17.4, 8), (33.0, 17.6, 8), (36.4, 15.0, 2), (37.0, 16.4, 2), (35.5, 19.6, 4), (3.0, 19.6, 4)], T("#9CF0E0", "#17B8A0", "#0B6B6A"), rim=2.6, ink=0)
    c.line([(5.0, 18.4), (33.0, 18.6)], 1.2, "#FFFFFF", opacity=0.85)
    c.line([(11.0, 17.9), (13.0, 17.9)], 0.01, "#FFFFFF")
    for x in (10.0, 25.0):
        c.rect(x - 2.0, 16.0, x + 2.0, 17.6, T("#6B7885", "#3B434D", "#252B36"), rad=2)
    # rider: boots, legs (bent), torso, arms out, head with beanie
    boot = T("#59636D", "#3B434D", "#252B36")
    pants = T("#7F6AE0", "#4B3AB5", "#2A2078")
    for x0, x1 in ((9.4, 10.8), (24.4, 25.8)):
        pass
    c.part([(8.6, 16.2, 2), (9.0, 14.6, 2), (11.6, 14.6, 2), (11.8, 16.2, 2)], boot, ink=1.2)
    c.part([(23.4, 16.2, 2), (23.8, 14.6, 2), (26.4, 14.6, 2), (26.6, 16.2, 2)], boot, ink=1.2)
    c.part([(9.0, 15.0, 3), (13.0, 10.0, 5), (17.0, 9.0, 4), (19.0, 11.0, 4), (14.8, 12.8, 3), (11.8, 15.0, 3)], pants, rim=2.0, ink=1.4)   # back leg
    c.part([(23.8, 15.0, 3), (23.0, 11.2, 4), (19.4, 8.6, 4), (16.6, 9.8, 4), (20.2, 12.0, 3), (26.2, 15.0, 3)], pants, rim=2.0, ink=1.4)  # front leg
    c.part([(15.8, 10.6, 5), (15.4, 5.6, 6), (18.6, 4.6, 6), (21.2, 5.4, 5), (21.0, 10.2, 5)], T("#FF9A8A", "#E8433A", "#8E2A2A"), rim=2.2, ink=1.4)
    c.tube([(18.8, 5.8), (23.0, 8.0), (27.4, 8.6)], 1.5, T("#FF9A8A", "#E8433A", "#8E2A2A"), ink=1.0)    # front arm
    c.tube([(17.0, 5.8), (12.4, 7.0), (8.4, 8.4)], 1.5, T("#FF9A8A", "#E8433A", "#8E2A2A"), ink=1.0)     # back arm
    c.disc(27.8, 8.7, 0.7 * c.S / 2.0, T("#F4F0E0", "#D8D0B8", "#9A9078"), ink=1.0)
    c.disc(8.0, 8.5, 0.7 * c.S / 2.0, T("#F4F0E0", "#D8D0B8", "#9A9078"), ink=1.0)
    c.disc(19.4, 2.9, 1.9 * c.S / 2.0, SKIN, ink=1.8)
    c.part([(17.0, 3.2, 3), (17.2, 1.2, 6), (19.5, 1.0, 5), (21.6, 2.0, 4), (21.8, 3.4, 2)], T("#FFFFFF", "#E7EDF2", "#9AA3AD"), ink=1.6, rim=2.0)
    c.part([(20.2, 2.8, 1), (22.2, 2.6, 1), (22.2, 3.8, 2), (20.2, 3.8, 1)], T("#E4F6FF", "#7FC4EA", "#2F6FA0"), ink=1.0)

if __name__ == "__main__":
    out = sys.argv[1]
    for v in ("vehicle-snowmobile", "vehicle-snowboard"):
        render(v, out); print(v)
