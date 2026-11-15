using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Creates (or refreshes in place) one SurfaceBehaviourSetSO per mapped catalogue surface, the SurfaceCompatibilityDatabase, and links the ice, mud and glass
    /// SurfaceRuleSO assets to their sets. Surfaces not listed here keep the old friction and speed behaviour. Safe to re-run.
    /// Menu: Road Builder / Stage / Build Surface Modules. Spec: blueprint section C, Docs/design/hazards.md section 3.
    /// </summary>
    public static class SurfaceModuleBuilder
    {
        public const string Dir = "Assets/Resources/Stage/Surfaces";
        public const string CompatPath = Dir + "/SurfaceCompatibility.asset";

        // Ids of the blueprint C2 surfaces that have no catalogue entry (and no art) yet. Their behaviour sets are built and tested; they become playable when the asset agents add the catalogue entry.
        public static readonly string[] PendingIds =
        {
            "surface-spring-road", "surface-magnetic-rail", "surface-acid", "surface-oil", "surface-conveyor", "surface-lava-river", "surface-sticky-candy", "surface-honey",
            "surface-metal-plates", "surface-ice-crystals", "surface-haunted-shadow", "surface-electric-rail", "surface-fragile-paper",
        };
        private static readonly string[] VirtualResults = { "surface-obsidian", "surface-electromagnetic", "surface-rope-bridge-strong" };
        public static readonly string[] ModifierTokens = { "mod-fire", "mod-wind", "mod-heavy", "mod-gravity-inversion" };

        public struct Map { public string id, hintFull, hintShort, verb, consequence; public SurfaceModuleSpec[] specs; }
        private static Map M(string id, string full, string shortHint, string verb, string consequence, params SurfaceModuleSpec[] specs) { return new Map { id = id, hintFull = full, hintShort = shortHint, verb = verb, consequence = consequence, specs = specs }; }
        private static SurfaceModuleSpec S(SurfaceModuleKind k, float a = 0, float b = 0, float c = 0, float d = 0) { return new SurfaceModuleSpec(k, a, b, c, d); }

        // Numbers of Sliding, Sinking and Breakable on ice, mud and glass come from their SurfaceRuleSO (empty spec). Others carry their own (see the module classes for a..d).
        // Each hint is icon + verb + consequence in under 60 characters. Asphalt is the stable baseline and intentionally has no module.
        public static List<Map> Maps()
        {
            return new List<Map>
            {
                M("surface-ice", "Ice: draw gentle bends or you slide.", "Ice: gentle bends", "Draw gentle bends", "sharp bends cut your drive", S(SurfaceModuleKind.Sliding)),
                M("surface-ice-bridge", "Ice bridge: sharp bends make you slide.", "Slippery", "Keep bends soft", "sharp bends cut your drive", S(SurfaceModuleKind.Sliding, 28f, 0.5f, 20f, 0.6f)),
                M("surface-snow", "Snow: soft drifts slow you, bends slide.", "Snow drifts", "Keep moving", "drifts slow you down", S(SurfaceModuleKind.Sliding, 32f, 0.4f, 25f, 0.6f), S(SurfaceModuleKind.Sinking, 0.3f, 0.8f, 3.5f, 22f)),
                M("surface-mud", "Mud: it sinks and slows you. Boost out.", "Mud: boost", "Boost out", "wheels sink and slow", S(SurfaceModuleKind.Sinking)),
                M("surface-sand", "Sand: sinks you a little, dunes launch you.", "Dunes launch", "Hit dunes fast", "dunes launch, sand drags", S(SurfaceModuleKind.Sinking, 0.5f, 1.2f, 3.5f, 22f), S(SurfaceModuleKind.Bounce, 3.2f, 1f, 0f, 4f)),
                M("surface-beach", "Soft sand drags you slowly.", "Soft sand", "Keep moving", "sand drags you", S(SurfaceModuleKind.Sinking, 0.4f, 1f, 3.5f, 22f)),
                M("surface-glass", "Glass cracks when you stop. Keep moving.", "Glass: keep moving", "Keep moving", "standing still breaks it", S(SurfaceModuleKind.Breakable)),
                M("surface-lava-crust", "Lava crust: breaks after 3 s. Keep moving.", "Crust: keep moving", "Keep moving", "crust breaks after 3 s", S(SurfaceModuleKind.Breakable, 3f, 3f, 4f)),
                M("surface-magma-bridge", "Magma bridge: breaks after 3 s. Keep moving.", "Hot: keep moving", "Keep moving", "bridge breaks after 3 s", S(SurfaceModuleKind.Breakable, 3f, 3f, 4f)),
                M("surface-cloud", "Cloud: it melts 2 s after you land.", "Cloud: keep going", "Do not stop", "dissolves 2 s after contact", S(SurfaceModuleKind.Dissolve, 2f, 0f)),
                M("surface-rope-bridge", "Rope bridge: it sways as you cross.", "Sways", "Stay steady", "the bridge sways", S(SurfaceModuleKind.Oscillation, 0.12f, 0.8f)),
                M("surface-neon-grid", "Neon grid: pads give a burst of speed.", "Speed pads", "Hit the pads", "speed burst", S(SurfaceModuleKind.SpeedPad, 2.5f, 1.2f, 0f, 3f)),
                M("surface-wooden-bridge", "Wooden bridge: fire burns it down.", "Burns", "Avoid fire", "wood burns and falls", S(SurfaceModuleKind.Heat, 2.5f, 4f)),
                M("surface-spring-road", "Spring road: it launches you up.", "Springs", "Hit the springs", "you are launched", S(SurfaceModuleKind.Bounce, 7f, 0.8f, 0f, 3f)),
                M("surface-candy", "Candy: springs you up.", "Bouncy", "Ride the bounce", "you bounce up", S(SurfaceModuleKind.Bounce, 5f, 0.6f, 0f, 2f)),
                M("surface-moon-dust", "Moon dust: low gravity bounce.", "Bouncy", "Ride the bounce", "long soft hops", S(SurfaceModuleKind.Bounce, 3.5f, 0.8f, 0f, 3f)),
                M("surface-magnetic-rail", "Magnetic rail: pulls metal vehicles down.", "Magnet pull", "Stay on the rail", "metal vehicles stick and speed up", S(SurfaceModuleKind.Magnetic, 7f, 0.8f)),
                M("surface-gravel", "Gravel: loose stones rock the car.", "Loose", "Slow in bends", "less stable, less grip", S(SurfaceModuleKind.Instability, 45f, 0.85f)),
                M("surface-sea-water", "Water: light cars float, grip is low.", "Floaty", "Steer gently", "low traction", S(SurfaceModuleKind.Buoyancy, 0.3f, 0.6f, 0.94f)),
                M("surface-acid", "Acid: it eats your durability. Do not linger.", "Acid: keep moving", "Keep moving", "durability drains", S(SurfaceModuleKind.Decay, 6f, 0.4f)),
                M("surface-oil", "Oil: low grip, the car spins under power.", "Oil: ease off", "Ease the throttle", "car rotates under power", S(SurfaceModuleKind.Torque, 70f, 0.55f)),
                M("surface-cobblestone", "Cobblestone: bumps shake the suspension.", "Bumpy", "Slow for bumps", "suspension bounces", S(SurfaceModuleKind.Bump, 0.9f, 0.35f)),
                M("surface-conveyor", "Conveyor: it carries you along the road.", "Moving belt", "Ride along", "belt adds speed", S(SurfaceModuleKind.Conveyor, 1.5f)),
                M("surface-river", "The current carries you along.", "Current", "Ride along", "current adds speed", S(SurfaceModuleKind.Conveyor, 1.5f)),
                M("surface-lava-river", "Lava river: platforms sink, cross when risen.", "Platforms sink", "Cross when risen", "sunk platforms slow you", S(SurfaceModuleKind.Cycle, 4f, 0.55f, 0.25f, 1.5f)),
                M("surface-sticky-candy", "Sticky candy: wheels stick, then let go.", "Sticky", "Keep power on", "stuck, then a burst", S(SurfaceModuleKind.Sticky, 1.2f, 2f, 2f, 2.5f)),
                M("surface-honey", "Honey: it slows and sinks you. Boost out.", "Honey: boost", "Boost out", "slow and sinking", S(SurfaceModuleKind.Sinking, 1.4f, 2f, 2.5f, 25f)),
                M("surface-metal-plates", "Metal plates: they heat up, then shift.", "Hot plates", "Do not linger", "hot plates shift and slow", S(SurfaceModuleKind.MetalHeat, 3f, 0.08f, 0.8f)),
                M("surface-ice-crystals", "Ice crystals: smash the spikes with a boost.", "Spikes: boost", "Boost through", "spikes snag, boost breaks them", S(SurfaceModuleKind.Spikes, 5f, 2f, -1.3f)),
                M("surface-floating-islands", "Floating rocks drift up and down.", "Drifting", "Time your jumps", "the road drifts", S(SurfaceModuleKind.Oscillation, 0.35f, 0.25f)),
                M("surface-haunted-shadow", "Haunted shadow: the road fades in and out.", "Fading road", "Trust the road", "it fades but stays solid", S(SurfaceModuleKind.Fade, 2f, 0.2f)),
                M("surface-electric-rail", "Electric rail: cross while it is safe.", "Watch the charge", "Wait for the blink", "charge cuts your drive", S(SurfaceModuleKind.Charge, 2.5f, 1.2f, 0.6f, 0.5f)),
                M("surface-fragile-paper", "Fragile paper: heavy loads tear it.", "Paper: stay light", "Keep moving", "heavy vehicles tear it", S(SurfaceModuleKind.Tear, 1.6f)),
                // results of merges (the pair rule says when they appear)
                M("surface-electromagnetic", "Electromagnetic rail: pulls metal and shocks.", "Magnet and charge", "Watch the charge", "pull plus shocks", S(SurfaceModuleKind.Magnetic, 7f, 0.8f), S(SurfaceModuleKind.Charge, 2.5f, 1.2f, 0.6f, 0.5f)),
                M("surface-rope-bridge-strong", "Strong rope bridge: heavy vehicles sway less.", "Steady bridge", "Cross steadily", "less sway", S(SurfaceModuleKind.Oscillation, 0.06f, 0.8f)),
            };
        }

        private static readonly string[][] Unlocks =
        {
            // Basic (1,000 to 2,500 m)
            new[] { "surface-asphalt", "surface-dirt", "surface-grass-trail", "surface-gravel", "surface-beach", "surface-cobblestone", "surface-salt-flat", "surface-rail-track", "surface-conveyor" },
            // Unstable (3,000 to 5,000 m, after the surface tutorial)
            new[] { "surface-sand", "surface-mud", "surface-ice", "surface-ice-bridge", "surface-snow", "surface-wooden-bridge", "surface-rope-bridge", "surface-sea-water", "surface-river" },
            // Advanced (6,000 to 10,000 m, 5 missions)
            new[] { "surface-glass", "surface-cloud", "surface-neon-grid", "surface-spring-road", "surface-honey", "surface-sticky-candy", "surface-metal-plates", "surface-ice-crystals", "surface-candy", "surface-moon-dust", "surface-floating-islands", "surface-coral" },
            // Special (12,000 to 18,000 m, boss reward)
            new[] { "surface-lava-crust", "surface-magma-bridge", "surface-lava-river", "surface-oil", "surface-acid", "surface-magnetic-rail", "surface-electric-rail" },
            // Exotic (20,000 to 30,000 m, world mastery)
            new[] { "surface-haunted-shadow", "surface-fragile-paper" },
        };
        private static readonly string[] UnlockIds = { "basic", "unstable", "advanced", "special", "exotic" };
        private static readonly string[] UnlockNames = { "Basic", "Unstable", "Advanced", "Special", "Exotic" };
        private static readonly int[] UnlockMin = { 1000, 3000, 6000, 12000, 20000 }, UnlockMax = { 2500, 5000, 10000, 18000, 30000 };
        private static readonly string[] UnlockGate = { "early distance", "after the surface tutorial", "5 missions", "boss reward", "world mastery" };

        [MenuItem("Road Builder/Stage/Build Surface Modules")]
        public static void BuildMenu()
        {
            var errors = Build();
            AssetDatabase.SaveAssets();
            SurfaceBehaviourLibrary.Invalidate();
            if (errors.Count == 0) Debug.Log("[SurfaceModuleBuilder] Surface modules, interactions, unlock categories and compatibility built and valid in " + Dir);
            else foreach (var e in errors) Debug.LogError("[SurfaceModuleBuilder] " + e);
        }

        public const string IconDir = "Assets/Resources/Stage/SurfaceIcons";
        public const string InteractionsPath = Dir + "/SurfaceInteractions.asset";
        public const string UnlockPath = Dir + "/SurfaceUnlockCategories.asset";

        public static string IconIdOf(string surfaceId) { return "icon-" + surfaceId.Replace("surface-", ""); }

        /// <summary>Every id a pair, a unlock list or a behaviour set may name: catalogue surfaces, pending ids, merge results.</summary>
        public static HashSet<string> KnownIds()
        {
            var known = new HashSet<string>(VirtualResults);
            foreach (var id in PendingIds) known.Add(id);
            var cat = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset");
            if (cat != null) foreach (var e in cat.entries) if (e != null && e.kind == CatalogKind.Surface) known.Add(e.id);
            return known;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        private static void ImportIcons()
        {
            if (!Directory.Exists(IconDir)) return;
            foreach (var f in Directory.GetFiles(IconDir, "*.png"))
            {
                string path = f.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { AssetDatabase.ImportAsset(path); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
                if (ti == null) continue;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single || ti.mipmapEnabled || ti.filterMode != FilterMode.Bilinear || ti.spritePixelsPerUnit != 128f;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear;
                ti.spritePixelsPerUnit = 128f; ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        public static List<string> Build()
        {
            var errors = new List<string>();
            Directory.CreateDirectory(Dir);
            ImportIcons();
            var known = KnownIds();
            bool haveCat = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset") != null;

            var sets = new Dictionary<string, SurfaceBehaviourSetSO>();
            foreach (var m in Maps())
            {
                if (haveCat && !known.Contains(m.id)) { errors.Add("surface not in catalogue: " + m.id); continue; }
                string path = Dir + "/BHV-" + m.id.Replace("surface-", "").ToUpperInvariant() + ".asset";
                var so = LoadOrCreate<SurfaceBehaviourSetSO>(path);
                so.surfaceId = m.id; so.modules = m.specs; so.hintFull = m.hintFull; so.hintShort = m.hintShort; so.hintIcon = IconIdOf(m.id);
                so.verb = m.verb; so.consequence = m.consequence;
                EditorUtility.SetDirty(so);
                sets[m.id] = so;
                so.Validate(errors);
                if (!File.Exists(IconDir + "/" + so.hintIcon + ".png")) errors.Add(m.id + ": icon missing " + so.hintIcon);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:SurfaceRuleSO", new[] { StageSliceBuilder.Dir }))
            {
                var rule = AssetDatabase.LoadAssetAtPath<SurfaceRuleSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (rule != null && sets.TryGetValue(rule.surfaceId, out var set)) { rule.behaviours = set; EditorUtility.SetDirty(rule); }
            }

            var db = LoadOrCreate<SurfaceCompatibilityDatabase>(CompatPath);
            db.pairs = Pairs();
            EditorUtility.SetDirty(db);
            var knownPlus = new HashSet<string>(known); foreach (var t in ModifierTokens) knownPlus.Add(t);
            db.Validate(errors, knownPlus);

            var table = LoadOrCreate<SurfaceInteractionTable>(InteractionsPath);
            table.projectileRows = ProjectileRows(); table.monsterRows = MonsterRows();
            EditorUtility.SetDirty(table);
            table.Validate(errors);

            var unlock = LoadOrCreate<SurfaceUnlockCategoriesSO>(UnlockPath);
            var cats = new SurfaceUnlockCategoriesSO.Category[Unlocks.Length];
            for (int i = 0; i < cats.Length; i++) cats[i] = new SurfaceUnlockCategoriesSO.Category { id = UnlockIds[i], displayName = UnlockNames[i], minDistanceM = UnlockMin[i], maxDistanceM = UnlockMax[i], extraGate = UnlockGate[i], surfaceIds = Unlocks[i] };
            unlock.categories = cats;
            EditorUtility.SetDirty(unlock);
            unlock.Validate(errors, known);
            if (haveCat)
            {
                var cat = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset");
                foreach (var e in cat.entries) if (e != null && e.kind == CatalogKind.Surface && unlock.IndexOf(e.id) < 0) errors.Add("surface without unlock category: " + e.id);
            }
            return errors;
        }

        private static SurfacePairRule P(string a, string b, SurfaceCombo c, string merge, string note, float factor = 0f, string warning = null)
        {
            return new SurfacePairRule { a = a, b = b, combo = c, mergeId = merge, note = note, factor = factor, warning = warning };
        }

        /// <summary>The pair rules of blueprint C3 plus the older event rules. A modifier token (mod-fire, mod-wind, mod-heavy, mod-gravity-inversion) stands for the non-surface half of a pair.</summary>
        public static SurfacePairRule[] Pairs()
        {
            var l = new List<SurfacePairRule>();
            var lavas = new[] { "surface-lava-crust", "surface-magma-bridge", "surface-lava-river" };
            foreach (var water in new[] { "surface-sea-water", "surface-river" })
                foreach (var lava in lavas)
                    l.Add(P(water, lava, SurfaceCombo.Merge, "surface-obsidian", "lava crust plus water: one obsidian behaviour"));
            l.Add(P("surface-ice", "surface-lava-crust", SurfaceCombo.Merge, "surface-sea-water", "ice melts to water"));
            l.Add(P("surface-ice", "surface-magma-bridge", SurfaceCombo.Merge, "surface-sea-water", "ice melts to water"));
            // C3 pairs
            l.Add(P("surface-oil", "mod-fire", SurfaceCombo.AllowedWarn, null, "oil plus fire: allowed, visible ignition warning", 0f, "Oil can catch fire"));
            l.Add(P("surface-ice", "mod-wind", SurfaceCombo.Reduced, null, "ice plus wind: allowed, wind force reduced", 0.5f, null));
            l.Add(P("surface-ice-bridge", "mod-wind", SurfaceCombo.Reduced, null, "ice plus wind: allowed, wind force reduced", 0.5f, null));
            l.Add(P("surface-glass", "mod-heavy", SurfaceCombo.AllowedWarn, null, "glass plus a heavy vehicle: only with a long warning", 0f, "Heavy on glass: it cracks sooner"));
            l.Add(P("surface-cloud", "mod-gravity-inversion", SurfaceCombo.Forbidden, null, "cloud plus gravity inversion is disallowed"));
            l.Add(P("surface-magnetic-rail", "surface-electric-rail", SurfaceCombo.Merge, "surface-electromagnetic", "magnetic plus electric rail: one electrical-magnetic behaviour"));
            l.Add(P("surface-rope-bridge", "mod-heavy", SurfaceCombo.Merge, "surface-rope-bridge-strong", "rope bridge plus a heavy vehicle: the stronger bridge variant"));
            foreach (var lava in lavas) l.Add(P("surface-acid", lava, SurfaceCombo.Forbidden, null, "acid plus lava is disallowed unless a designed chemical hazard"));
            // older event rules and extra fairness pairs (two slowers, two breakers, uncontrollable combinations)
            l.Add(P("surface-ice", "surface-mud", SurfaceCombo.Forbidden, null, "two slow-down events never touch (stage-events.md)"));
            l.Add(P("surface-ice", "surface-glass", SurfaceCombo.Forbidden, null, "slide plus break is unfair"));
            l.Add(P("surface-mud", "surface-glass", SurfaceCombo.Forbidden, null, "adjacent mud and glass are forbidden (stage-events.md)"));
            l.Add(P("surface-cloud", "surface-glass", SurfaceCombo.Forbidden, null, "two breaking surfaces in a row"));
            l.Add(P("surface-neon-grid", "surface-ice", SurfaceCombo.Forbidden, null, "speed pad on ice cannot be steered"));
            l.Add(P("surface-wooden-bridge", "surface-lava-crust", SurfaceCombo.Forbidden, null, "wood on lava burns at once"));
            l.Add(P("surface-wooden-bridge", "surface-magma-bridge", SurfaceCombo.Forbidden, null, "wood on lava burns at once"));
            l.Add(P("surface-wooden-bridge", "surface-lava-river", SurfaceCombo.Forbidden, null, "wood on lava burns at once"));
            l.Add(P("surface-honey", "surface-sticky-candy", SurfaceCombo.Forbidden, null, "two sticky slowers in a row"));
            l.Add(P("surface-honey", "surface-mud", SurfaceCombo.Forbidden, null, "two sinking slowers in a row"));
            l.Add(P("surface-oil", "surface-ice", SurfaceCombo.Forbidden, null, "two low-grip surfaces cannot be steered"));
            l.Add(P("surface-electric-rail", "surface-sea-water", SurfaceCombo.Forbidden, null, "electricity on water"));
            l.Add(P("surface-oil", "surface-lava-crust", SurfaceCombo.Forbidden, null, "oil on lava ignites at once"));
            l.Add(P("surface-ice-crystals", "surface-lava-crust", SurfaceCombo.Merge, "surface-sea-water", "crystals melt to water"));
            l.Add(P("surface-fragile-paper", "mod-fire", SurfaceCombo.Forbidden, null, "paper plus fire burns away at once"));
            return l.ToArray();
        }

        private static ProjectileSurfaceRow R(string surface, ProjectileSurfaceEffect e, SurfacePatchKind k, float strength, string note)
        {
            return new ProjectileSurfaceRow { surfaceId = surface, effect = e, patch = k, strength = strength, note = note };
        }

        /// <summary>Blueprint B2 persistent effects as defaults ("*") with per-surface overrides.</summary>
        public static ProjectileSurfaceRow[] ProjectileRows()
        {
            var l = new List<ProjectileSurfaceRow>
            {
                R("*", ProjectileSurfaceEffect.BreakWeak, SurfacePatchKind.Impact, 1f, "boulder: breaks glass and weak bridges (heavy impact on the piece)"),
                R("*", ProjectileSurfaceEffect.SandMound, SurfacePatchKind.Mound, 1f, "2 s sand mound slows"),
                R("*", ProjectileSurfaceEffect.SlipperyPatch, SurfacePatchKind.Slippery, 1f, "3 s slippery patch"),
                R("*", ProjectileSurfaceEffect.GravityDistort, SurfacePatchKind.GravityWarp, 1f, "gravity distorted 1.5 s"),
                R("*", ProjectileSurfaceEffect.Crater, SurfacePatchKind.Crater, 1f, "crater bump"),
                R("*", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.Burn, 1f, "lava bomb burns wooden roads (fire hazard on the piece)"),
                R("*", ProjectileSurfaceEffect.LiftLight, SurfacePatchKind.Lift, 1f, "bubble lifts light vehicles"),
                R("*", ProjectileSurfaceEffect.Sticky, SurfacePatchKind.Sticky, 1f, "gumdrop: sticky road"),
                R("*", ProjectileSurfaceEffect.TempSpike, SurfacePatchKind.Spike, 1f, "crystal spike: temporary road spike, boost or weight smashes it"),
                R("*", ProjectileSurfaceEffect.ChargeGrid, SurfacePatchKind.Charge, 1f, "neon bolt charges the grid"),
                R("*", ProjectileSurfaceEffect.Invisible, SurfacePatchKind.Fade, 1f, "shadow orb: road invisible for 1 s (stays solid)"),
                R("*", ProjectileSurfaceEffect.Snag, SurfacePatchKind.Snag, 1f, "thorn ball: temporary snag"),
                R("*", ProjectileSurfaceEffect.ElectricBarrier, SurfacePatchKind.Charge, 1f, "drone: electric barrier"),
                R("*", ProjectileSurfaceEffect.ChargeMetal, SurfacePatchKind.Charge, 1f, "storm pearl charges metal"),
                R("*", ProjectileSurfaceEffect.WeakenRoad, SurfacePatchKind.Acid, 1f, "acid glob weakens road durability"),
                // overrides
                R("surface-sea-water", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.None, 1f, "water puts the fire out"),
                R("surface-river", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.None, 1f, "water puts the fire out"),
                R("surface-ice", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.Slippery, 1f, "fire melts ice to a wet slick"),
                R("surface-ice-bridge", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.Slippery, 1f, "fire melts ice to a wet slick"),
                R("surface-snow", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.Slippery, 1f, "fire melts snow to a wet slick"),
                R("surface-sea-water", ProjectileSurfaceEffect.SandMound, SurfacePatchKind.None, 1f, "sand sinks in water"),
                R("surface-river", ProjectileSurfaceEffect.SandMound, SurfacePatchKind.None, 1f, "sand sinks in water"),
                R("surface-lava-river", ProjectileSurfaceEffect.SandMound, SurfacePatchKind.None, 1f, "sand sinks in lava"),
                R("surface-honey", ProjectileSurfaceEffect.Sticky, SurfacePatchKind.Sticky, 0.5f, "honey is already sticky: milder"),
                R("surface-sticky-candy", ProjectileSurfaceEffect.Sticky, SurfacePatchKind.Sticky, 0.5f, "candy is already sticky: milder"),
                R("surface-ice", ProjectileSurfaceEffect.SlipperyPatch, SurfacePatchKind.Slippery, 0.8f, "ice is already slick: shorter effect"),
                R("surface-oil", ProjectileSurfaceEffect.SlipperyPatch, SurfacePatchKind.Slippery, 0.8f, "oil is already slick"),
                R("surface-sea-water", ProjectileSurfaceEffect.ElectricBarrier, SurfacePatchKind.Charge, 1.5f, "water conducts: longer shock window"),
                R("surface-ice-crystals", ProjectileSurfaceEffect.TempSpike, SurfacePatchKind.Spike, 1f, "more crystals"),
                R("surface-oil", ProjectileSurfaceEffect.BurnWood, SurfacePatchKind.Burn, 1f, "oil plus fire: ignites (warning shown by the compatibility rule)"),
                R("surface-fragile-paper", ProjectileSurfaceEffect.BreakWeak, SurfacePatchKind.Impact, 1f, "a boulder tears paper"),
            };
            return l.ToArray();
        }

        private static MonsterSurfaceRow Mo(string id, float slow, float stun, float eat, int stage, string note)
        {
            return new MonsterSurfaceRow { surfaceId = id, slowMul = slow, stunSeconds = stun, eatMul = eat, stunStage = stage, note = note };
        }

        /// <summary>Monster interaction per surface: slow, stun and eat speed. Surfaces not listed leave the monster unchanged.</summary>
        public static MonsterSurfaceRow[] MonsterRows()
        {
            return new[]
            {
                Mo("surface-mud", 0.7f, 0f, 0.8f, 0, "mud slows the monster and it eats slower"),
                Mo("surface-honey", 0.6f, 0f, 0.7f, 0, "honey holds the monster"),
                Mo("surface-sand", 0.85f, 0f, 1f, 0, "soft sand slows it a little"),
                Mo("surface-snow", 0.9f, 0f, 1f, 0, "drifts slow it a little"),
                Mo("surface-sticky-candy", 0.6f, 0f, 1f, 0, "it sticks"),
                Mo("surface-ice", 0.95f, 0f, 1f, 0, "ice: nearly unchanged"),
                Mo("surface-ice-crystals", 0.9f, 0.8f, 1f, 1, "spikes up: the monster is stunned"),
                Mo("surface-glass", 1f, 0f, 1.4f, 0, "glass is eaten faster"),
                Mo("surface-cloud", 1f, 0f, 1.3f, 0, "cloud is eaten faster"),
                Mo("surface-fragile-paper", 1f, 0f, 1.6f, 0, "paper is eaten much faster"),
                Mo("surface-wooden-bridge", 1f, 0f, 1.2f, 0, "wood is eaten faster"),
                Mo("surface-electric-rail", 1f, 1f, 1f, 2, "a charged window stuns the monster"),
                Mo("surface-metal-plates", 0.9f, 0.6f, 1f, 2, "hot plates stun it briefly"),
                Mo("surface-lava-crust", 0.8f, 0f, 1f, 0, "hot: slows it"),
                Mo("surface-magma-bridge", 0.8f, 0f, 1f, 0, "hot: slows it"),
                Mo("surface-lava-river", 0.8f, 0f, 1f, 0, "hot: slows it"),
                Mo("surface-acid", 0.85f, 0f, 1f, 0, "acid slows it"),
                Mo("surface-oil", 1.1f, 0f, 1f, 0, "it skids faster"),
                Mo("surface-sea-water", 0.85f, 0f, 1f, 0, "water slows it"),
                Mo("surface-river", 0.85f, 0f, 1f, 0, "current slows it"),
                Mo("surface-magnetic-rail", 0.9f, 0f, 1f, 0, "magnet drags it"),
                Mo("surface-spring-road", 1f, 0.5f, 1f, 0, "springs make it stumble"),
                Mo("surface-neon-grid", 1.1f, 0f, 1f, 0, "pads speed it too"),
                Mo("surface-haunted-shadow", 1f, 0f, 0.9f, 0, "it loses the road in the shadow"),
                Mo("surface-cobblestone", 0.95f, 0f, 1f, 0, "bumps slow it a little"),
                Mo("surface-gravel", 0.95f, 0f, 1f, 0, "loose stones slow it a little"),
                Mo("surface-conveyor", 1f, 0f, 1f, 0, "belt carries it along"),
                Mo("surface-floating-islands", 1f, 0f, 1f, 0, "unchanged"),
            };
        }
    }
}
