using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>A resolved cosmetic: what it is called, what it does to the car and what it looks like.</summary>
    public struct CosmeticInfo { public string id, label; public CosmeticKind kind; public Color color; public bool valid; }

    /// <summary>
    /// Cosmetics: stickers, paint colours and trim from vehicle mastery (generated per vehicle), the gem shop and the album. Owned ids live in
    /// <c>PlayerProfile.cosmeticsOwned</c>; the equipped ones per vehicle in <c>equips</c>. No gameplay effect.
    /// </summary>
    public static class Cosmetics
    {
        public static string MasteryId(MasteryReward r, string vehicleId)
        {
            return (r == MasteryReward.Sticker ? "sticker:" : r == MasteryReward.Colour ? "colour:" : "trim:") + vehicleId;
        }

        public static string GoalStickerId(string vehicleId) { return "sticker:" + vehicleId + ":goal"; }

        public static bool Owned(PlayerProfile p, string id) { return p.cosmeticsOwned != null && p.cosmeticsOwned.Contains(id); }

        /// <summary>Adds a cosmetic and equips it on the vehicle it belongs to when that slot is empty. An owned duplicate pays scrap instead (blueprint D1: scrap from duplicate parts). Returns true when it was new.</summary>
        public static bool Grant(PlayerProfile p, ProgressionTuningSO t, string id, string equipOn = null)
        {
            if (p == null || string.IsNullOrEmpty(id)) return false;
            if (p.cosmeticsOwned == null) p.cosmeticsOwned = new List<string>();
            if (p.cosmeticsOwned.Contains(id)) { MasteryService.AddScrap(p, t != null ? t.scrapPerDuplicate : 5); return false; }
            p.cosmeticsOwned.Add(id);
            string veh = equipOn ?? VehicleOf(id);
            if (!string.IsNullOrEmpty(veh))
            {
                var info = Resolve(t, id);
                var e = EquipFor(p, veh);
                if (info.kind == CosmeticKind.Colour && e.colour.Length == 0) e.colour = id;
                else if (info.kind == CosmeticKind.Sticker && e.sticker.Length == 0) e.sticker = id;
                else if (info.kind == CosmeticKind.Trim && e.trim.Length == 0) e.trim = id;
            }
            return true;
        }

        /// <summary>The vehicle a mastery cosmetic id belongs to ("" for shop and album items, which fit any vehicle).</summary>
        public static string VehicleOf(string id)
        {
            if (id == null) return "";
            int c = id.IndexOf(':');
            if (c < 0) return "";
            string rest = id.Substring(c + 1);
            int g = rest.IndexOf(':');
            return g >= 0 ? rest.Substring(0, g) : rest;
        }

        public static EquipEntry EquipFor(PlayerProfile p, string vehicleId)
        {
            if (p.equips == null) p.equips = new List<EquipEntry>();
            for (int i = 0; i < p.equips.Count; i++) if (p.equips[i].vehicleId == vehicleId) return p.equips[i];
            var e = new EquipEntry { vehicleId = vehicleId };
            p.equips.Add(e);
            return e;
        }

        public static bool Equip(PlayerProfile p, ProgressionTuningSO t, string vehicleId, string id)
        {
            if (!Owned(p, id)) return false;
            var info = Resolve(t, id);
            if (!info.valid) return false;
            var e = EquipFor(p, vehicleId);
            string cur = info.kind == CosmeticKind.Colour ? e.colour : info.kind == CosmeticKind.Sticker ? e.sticker : e.trim;
            string next = cur == id ? "" : id;       // tapping the equipped one takes it off
            if (info.kind == CosmeticKind.Colour) e.colour = next; else if (info.kind == CosmeticKind.Sticker) e.sticker = next; else e.trim = next;
            return true;
        }

        public static bool IsEquipped(PlayerProfile p, string vehicleId, string id)
        {
            var e = EquipFor(p, vehicleId);
            return e.colour == id || e.sticker == id || e.trim == id;
        }

        /// <summary>Resolves a mastery-generated id (sticker:/colour:/trim:<vehicle>) or an entry of the cosmetics table.</summary>
        public static CosmeticInfo Resolve(ProgressionTuningSO t, string id)
        {
            var info = new CosmeticInfo { id = id };
            if (string.IsNullOrEmpty(id)) return info;
            if (t != null && t.cosmetics != null)
                for (int i = 0; i < t.cosmetics.Length; i++)
                    if (t.cosmetics[i].id == id) { info.label = t.cosmetics[i].label; info.kind = t.cosmetics[i].kind; info.color = t.cosmetics[i].color; info.valid = true; return info; }
            string veh = VehicleOf(id);
            if (veh.Length == 0) return info;
            float hue = Hue(veh);
            if (id.StartsWith("colour:")) { info.kind = CosmeticKind.Colour; info.color = Color.HSVToRGB(hue, 0.6f, 1f); info.label = "MASTER COLOUR"; }
            else if (id.StartsWith("trim:")) { info.kind = CosmeticKind.Trim; info.color = new Color(1f, 0.82f, 0.25f); info.label = "MASTER GOLD TRIM"; }
            else if (id.StartsWith("sticker:")) { info.kind = CosmeticKind.Sticker; info.color = Color.HSVToRGB((hue + 0.5f) % 1f, 0.7f, 1f); info.label = id.EndsWith(":goal") ? "GOAL STICKER" : "MASTER STICKER"; }
            else return info;
            info.valid = true;
            return info;
        }

        private static float Hue(string key)
        {
            uint h = 2166136261u;
            for (int i = 0; i < key.Length; i++) { h ^= key[i]; h *= 16777619u; }
            return (h % 360u) / 360f;
        }

        /// <summary>Gives everything a save already earned but never recorded (mastery levels claimed before cosmetics existed, finished goals). No scrap is paid for these. Safe to run at every load.</summary>
        public static void Reconcile(PlayerProfile p, ProgressionTuningSO t)
        {
            if (p == null || t == null || p.vehicleProgress == null) return;
            var goals = VehicleProfileSO.Load();
            foreach (var v in p.vehicleProgress)
            {
                for (int i = 0; i < t.mastery.Length && i < 30; i++)
                {
                    if ((v.masteryClaimedMask & (1 << i)) == 0) continue;
                    var r = t.mastery[i].reward;
                    if (r == MasteryReward.Coins || r == MasteryReward.Gem) continue;
                    string id = MasteryId(r, v.id);
                    if (!Owned(p, id)) GrantQuiet(p, t, id);
                }
                var vp = goals.Find(v.id);
                if (vp != null && p.Counter(v.id + ":" + vp.goalId) >= vp.goalTarget && !Owned(p, GoalStickerId(v.id))) GrantQuiet(p, t, GoalStickerId(v.id));
            }
        }

        /// <summary>Test hook: grants and equips without paying scrap.</summary>
        public static void GrantQuiet_ForTests(PlayerProfile p, string id) { GrantQuiet(p, null, id); }

        private static void GrantQuiet(PlayerProfile p, ProgressionTuningSO t, string id) { int scrap = p.scrap; Grant(p, t, id); p.scrap = scrap; }

        /// <summary>Awards the goal sticker when a vehicle's mastery goal has just been met. Returns true when granted.</summary>
        public static bool CheckGoal(PlayerProfile p, ProgressionTuningSO t, string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId) || Owned(p, GoalStickerId(vehicleId))) return false;
            var vp = VehicleProfileSO.Load().Find(vehicleId);
            if (vp == null || p.Counter(vehicleId + ":" + vp.goalId) < vp.goalTarget) return false;
            return Grant(p, t, GoalStickerId(vehicleId));
        }
    }

    /// <summary>Puts the equipped colour, sticker and trim on a built car. Cheap: a tint on the body renderer and up to two small child sprites from one cached texture.</summary>
    public static class CosmeticApplier
    {
        private static Sprite _badge, _star;

        public static void Apply(GameObject car, PlayerProfile p, ProgressionTuningSO t, string vehicleId)
        {
            if (car == null || p == null || string.IsNullOrEmpty(vehicleId)) return;
            var e = Cosmetics.EquipFor(p, vehicleId);
            if (e.colour.Length == 0 && e.sticker.Length == 0 && e.trim.Length == 0) return;
            SpriteRenderer body = null; float best = 0f;
            foreach (var sr in car.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.sprite == null || sr.name.Contains("Sticker") || sr.name.Contains("Trim")) continue;
                var b = sr.bounds.size; float a = b.x * b.y;
                if (a > best) { best = a; body = sr; }
            }
            if (body == null) return;
            if (e.colour.Length > 0) { var c = Cosmetics.Resolve(t, e.colour); if (c.valid) body.color = c.color; }
            var bb = body.bounds;
            if (e.sticker.Length > 0) { var c = Cosmetics.Resolve(t, e.sticker); if (c.valid) Add(body, "Sticker", Badge(), c.color, bb.center + new Vector3(bb.size.x * 0.27f, -bb.size.y * 0.16f, 0f), bb.size.y * 0.2f); }
            if (e.trim.Length > 0) { var c = Cosmetics.Resolve(t, e.trim); if (c.valid) Add(body, "Trim", Star(), c.color, bb.center + new Vector3(-bb.size.x * 0.2f, bb.size.y * 0.18f, 0f), bb.size.y * 0.34f); }
        }

        private static void Add(SpriteRenderer body, string name, Sprite sprite, Color color, Vector3 worldPos, float worldSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(body.transform, true);
            go.transform.position = worldPos;
            float s = worldSize / Mathf.Max(0.01f, sprite.bounds.size.y);
            var ls = body.transform.lossyScale; go.transform.localScale = new Vector3(s / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), s / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.color = color;
            r.sortingLayerID = body.sortingLayerID; r.sortingOrder = body.sortingOrder + 1;
        }

        private static Sprite Badge()
        {
            if (_badge != null) return _badge;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = d < 0.82f ? 1f : (d < 1f ? Mathf.Clamp01((1f - d) / 0.18f) : 0f);
                float ring = d > 0.66f && d < 0.78f ? 0.5f : 1f;
                // a small star cut into the patch so the sticker reads as a badge, not a flat disc
                Vector2 q = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f) / (n / 2f);
                float edge = 0.2f + 0.38f * Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(q.y, q.x) * 2.5f)), 1.5f);
                if (d < edge) ring = 0.22f;
                tex.SetPixel(x, y, new Color(ring, ring, ring, a * 0.95f));
            }
            tex.Apply(false, true);
            return _badge = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static Sprite Star()
        {
            if (_star != null) return _star;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                Vector2 q = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f) / (n / 2f);
                float ang = Mathf.Atan2(q.y, q.x), r = q.magnitude;
                float edge = 0.45f + 0.5f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2.5f)), 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, r < edge ? 1f : 0f));
            }
            tex.Apply(false, true);
            return _star = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }
    }
}
