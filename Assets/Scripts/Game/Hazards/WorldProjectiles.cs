using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The 15 world projectiles of blueprint B2 as data rows plus the world-id lookup. Assets live in Resources/Stage/Hazards/World (written by the hazard asset builder from
    /// <see cref="Defaults"/>); a missing asset falls back to the code row so a build never runs without them. Countryside keeps the bolt/bomb/roller archetypes (no row).
    /// </summary>
    public static class WorldProjectiles
    {
        public const string ResourceDir = "Stage/Hazards/World/";
        private static List<ProjectileDefinitionSO> _defaults;
        private static readonly Dictionary<string, ProjectileDefinitionSO> _byWorld = new Dictionary<string, ProjectileDefinitionSO>();

        /// <summary>Catalogue ids (world-*) and short ids both resolve to the short id used in the rows.</summary>
        public static string ShortId(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return "";
            string w = worldId.StartsWith("world-") ? worldId.Substring(6) : worldId;
            switch (w)
            {
                case "rolling-hills": case "hills": return "hills";
                case "winter": case "arctic": case "snow": case "christmas": return "snow";
                case "candy-land": case "candy": return "candy";
                case "haunted-halloween": case "haunted": return "haunted";
                case "jungle": case "forest": case "summer": case "spring-blossom": case "autumn-leaves": return "forest";
                case "night-city": case "city": return "city";
                case "cloud-kingdom": case "cloud": return "cloud";
                case "industrial": case "toxic": case "toxic-swamp": return "toxic";
                default: return w;      // desert, moon, mars, volcano, underwater, cave, synthwave
            }
        }

        public static IReadOnlyList<ProjectileDefinitionSO> Defaults()
        {
            if (_defaults != null) return _defaults;
            _defaults = new List<ProjectileDefinitionSO>
            {
                Row("hills", "PRJ-BOULDER", "Boulder Ball", ProjectilePattern.Arc, 9f, TelegraphShape.Marker, "dust cloud", ProjectileSurfaceEffect.BreakWeak, 0.1f, 1.8f, new Color(0.62f, 0.55f, 0.45f), 1.1f),
                Row("desert", "PRJ-SANDSPIKE", "Sand Spike", ProjectilePattern.Straight, 12f, TelegraphShape.Line, "ground crack", ProjectileSurfaceEffect.SandMound, 2f, 1.6f, new Color(0.93f, 0.78f, 0.45f), 0.9f),
                Row("snow", "PRJ-ICESHARD", "Ice Shard", ProjectilePattern.Splitting, 14f, TelegraphShape.Line, "blue targeting line", ProjectileSurfaceEffect.SlipperyPatch, 3f, 2f, new Color(0.65f, 0.88f, 1f), 0.9f),
                Row("moon", "PRJ-GRAVORB", "Gravity Orb", ProjectilePattern.Homing, 8f, TelegraphShape.Ring, "floating rings", ProjectileSurfaceEffect.GravityDistort, 1.5f, 2.4f, new Color(0.7f, 0.6f, 1f), 1f),
                Row("mars", "PRJ-METEORCORE", "Meteor Core", ProjectilePattern.Arc, 11f, TelegraphShape.Marker, "red marker", ProjectileSurfaceEffect.Crater, 4f, 1.8f, new Color(1f, 0.4f, 0.25f), 1.1f),
                Row("volcano", "PRJ-LAVABOMB", "Lava Bomb", ProjectilePattern.Bouncing, 9f, TelegraphShape.Glow, "orange glow", ProjectileSurfaceEffect.BurnWood, 3f, 1.8f, new Color(1f, 0.55f, 0.1f), 1f),
                Row("underwater", "PRJ-BUBBLE", "Pressure Bubble", ProjectilePattern.Homing, 7f, TelegraphShape.Ring, "bubble trail", ProjectileSurfaceEffect.LiftLight, 1.5f, 2.2f, new Color(0.6f, 0.9f, 1f), 1.1f),
                Row("candy", "PRJ-GUMDROP", "Gumdrop Bomb", ProjectilePattern.Bouncing, 10f, TelegraphShape.Flash, "wrapper flash", ProjectileSurfaceEffect.Sticky, 3f, 1.8f, new Color(1f, 0.4f, 0.7f), 1f),
                Row("cave", "PRJ-CRYSTAL", "Crystal Spike", ProjectilePattern.Straight, 15f, TelegraphShape.Glow, "ceiling glimmer", ProjectileSurfaceEffect.TempSpike, 2.5f, 1.4f, new Color(0.55f, 0.95f, 0.9f), 0.9f),
                Row("synthwave", "PRJ-NEONBOLT", "Neon Bolt", ProjectilePattern.Zigzag, 16f, TelegraphShape.Flash, "grid flash", ProjectileSurfaceEffect.ChargeGrid, 3f, 2f, new Color(1f, 0.25f, 0.85f), 0.9f),
                Row("haunted", "PRJ-SHADOWORB", "Shadow Orb", ProjectilePattern.Homing, 8f, TelegraphShape.Marker, "dark silhouette", ProjectileSurfaceEffect.Invisible, 1f, 2.4f, new Color(0.25f, 0.18f, 0.35f), 1f),
                Row("forest", "PRJ-THORN", "Thorn Ball", ProjectilePattern.Splitting, 12f, TelegraphShape.Glow, "branch movement", ProjectileSurfaceEffect.Snag, 3f, 1.6f, new Color(0.35f, 0.6f, 0.2f), 1f),
                Row("city", "PRJ-DRONE", "Traffic Drone", ProjectilePattern.Straight, 13f, TelegraphShape.Line, "red laser", ProjectileSurfaceEffect.ElectricBarrier, 3f, 1.8f, new Color(0.85f, 0.88f, 0.95f), 1.1f),
                Row("cloud", "PRJ-STORMPEARL", "Storm Pearl", ProjectilePattern.Arc, 10f, TelegraphShape.Flash, "lightning flicker", ProjectileSurfaceEffect.ChargeMetal, 3f, 2f, new Color(0.95f, 0.95f, 1f), 1f),
                Row("toxic", "PRJ-ACID", "Acid Glob", ProjectilePattern.Bouncing, 9f, TelegraphShape.Marker, "green pool", ProjectileSurfaceEffect.WeakenRoad, 4f, 1.8f, new Color(0.55f, 0.95f, 0.25f), 1f),
            };
            return _defaults;
        }

        private static ProjectileDefinitionSO Row(string world, string id, string name, ProjectilePattern pat, float speed, TelegraphShape tg, string tgLabel,
            ProjectileSurfaceEffect fx, float fxSeconds, float fxRadius, Color tint, float scale)
        {
            var p = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            p.name = p.id = id; p.worldId = world; p.displayName = name; p.kind = ProjectileKind.World; p.pattern = pat; p.speed = speed;
            p.telegraph = tg; p.telegraphLabel = tgLabel; p.surfaceEffect = fx; p.surfaceSeconds = fxSeconds; p.surfaceRadius = fxRadius; p.tint = tint; p.scale = scale;
            p.tellSeconds = 1.2f; p.lockLeadSeconds = 0.35f; p.hitRadius = 0.45f * scale; p.carReach = 0.45f; p.activeSeconds = 3f; p.flightSeconds = 1.0f;
            p.minMuzzleDistance = 9f; p.spawnClearance = 2.6f; p.nearMissRadius = 1.8f; p.poolSize = pat == ProjectilePattern.Splitting ? 3 : 1;
            p.tellCueId = "vo.rob0.proj.bolt"; p.fireSfxId = "sfx.proj.fire.bolt";
            p.homingTurnRate = 28f; p.homingSeconds = 1.6f; p.arcHeight = pat == ProjectilePattern.Arc ? 3.5f : 0f;
            // blueprint: surface interaction only for effects the row names; hills breaks weak bridges (no persistent patch)
            if (fx == ProjectileSurfaceEffect.BreakWeak) { p.surfaceSeconds = 0f; }
            return p;
        }

        /// <summary>The projectile of a world (assets first, code rows second). Null for Countryside and unknown worlds.</summary>
        public static ProjectileDefinitionSO ForWorld(string worldId)
        {
            string s = ShortId(worldId); if (s.Length == 0) return null;
            if (_byWorld.TryGetValue(s, out var hit)) return hit;
            ProjectileDefinitionSO found = null;
            var all = Defaults();
            for (int i = 0; i < all.Count; i++) if (all[i].worldId == s) { found = Resources.Load<ProjectileDefinitionSO>(ResourceDir + all[i].id); if (found == null) found = all[i]; break; }
            _byWorld[s] = found; return found;
        }

        public static void ClearCache() { _byWorld.Clear(); }
    }
}
