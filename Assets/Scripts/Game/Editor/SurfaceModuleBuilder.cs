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
        private static readonly string[] VirtualResults = { "surface-obsidian" };

        private struct Map { public string id, hintFull, hintShort; public SurfaceModuleSpec[] specs; }
        private static Map M(string id, string full, string shortHint, params SurfaceModuleSpec[] specs) { return new Map { id = id, hintFull = full, hintShort = shortHint, specs = specs }; }
        private static SurfaceModuleSpec S(SurfaceModuleKind k, float a = 0, float b = 0, float c = 0, float d = 0) { return new SurfaceModuleSpec(k, a, b, c, d); }

        // Numbers of Sliding, Sinking and Breakable on ice, mud and glass come from their SurfaceRuleSO (empty spec). Others carry their own.
        private static List<Map> Maps()
        {
            return new List<Map>
            {
                M("surface-ice", "Ice: sharp bends make you slide. Draw gentle curves.", "Ice: gentle bends", S(SurfaceModuleKind.Sliding)),
                M("surface-ice-bridge", "Ice bridge: sharp bends make you slide.", "Slippery", S(SurfaceModuleKind.Sliding, 28f, 0.5f, 20f, 0.6f)),
                M("surface-snow", "Snow: slippery on sharp bends.", "Slippery", S(SurfaceModuleKind.Sliding, 32f, 0.4f, 25f, 0.6f)),
                M("surface-mud", "Mud: it slows you down. Boost to break free.", "Mud: boost", S(SurfaceModuleKind.Sinking)),
                M("surface-sand", "Soft sand drags you slowly.", "Soft sand", S(SurfaceModuleKind.Sinking, 0.5f, 1.2f, 3.5f, 22f)),
                M("surface-glass", "Glass cracks when you stop. Keep moving.", "Glass: keep moving", S(SurfaceModuleKind.Breakable)),
                M("surface-cloud", "Cloud melts under you. Do not stop.", "Cloud: keep going", S(SurfaceModuleKind.Dissolve, 1.2f, 0f)),
                M("surface-candy", "Candy springs you up.", "Bouncy", S(SurfaceModuleKind.Bounce, 5f, 0.6f)),
                M("surface-moon-dust", "Low gravity bounce.", "Bouncy", S(SurfaceModuleKind.Bounce, 3.5f, 0.8f)),
                M("surface-rope-bridge", "Rope bridge sways as you cross.", "Sways", S(SurfaceModuleKind.Oscillation, 0.12f, 0.8f)),
                M("surface-neon-grid", "Neon pads give a burst of speed.", "Speed pad", S(SurfaceModuleKind.SpeedPad, 2.5f, 1.2f)),
                M("surface-wooden-bridge", "Wood burns. Avoid fire.", "Burns", S(SurfaceModuleKind.Heat, 2.5f, 4f)),
                M("surface-lava-crust", "Hot crust. Do not linger.", "Hot", S(SurfaceModuleKind.Heat, 1f, 2f)),
                M("surface-river", "The current carries you along.", "Current", S(SurfaceModuleKind.Conveyor, 1.5f)),
            };
        }

        [MenuItem("Road Builder/Stage/Build Surface Modules")]
        public static void BuildMenu()
        {
            var errors = Build();
            AssetDatabase.SaveAssets();
            if (errors.Count == 0) Debug.Log("[SurfaceModuleBuilder] Surface modules built and valid in " + Dir);
            else foreach (var e in errors) Debug.LogError("[SurfaceModuleBuilder] " + e);
        }

        public static List<string> Build()
        {
            var errors = new List<string>();
            Directory.CreateDirectory(Dir);
            var cat = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset");
            var known = new HashSet<string>(VirtualResults);
            if (cat != null) foreach (var e in cat.entries) if (e != null && e.kind == CatalogKind.Surface) known.Add(e.id);

            var sets = new Dictionary<string, SurfaceBehaviourSetSO>();
            foreach (var m in Maps())
            {
                if (cat != null && !known.Contains(m.id)) { errors.Add("surface not in catalogue: " + m.id); continue; }
                string path = Dir + "/BHV-" + m.id.Replace("surface-", "").ToUpperInvariant() + ".asset";
                var so = AssetDatabase.LoadAssetAtPath<SurfaceBehaviourSetSO>(path);
                if (so == null) { so = ScriptableObject.CreateInstance<SurfaceBehaviourSetSO>(); AssetDatabase.CreateAsset(so, path); }
                so.surfaceId = m.id; so.modules = m.specs; so.hintFull = m.hintFull; so.hintShort = m.hintShort;
                EditorUtility.SetDirty(so);
                sets[m.id] = so;
                so.Validate(errors);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:SurfaceRuleSO", new[] { StageSliceBuilder.Dir }))
            {
                var rule = AssetDatabase.LoadAssetAtPath<SurfaceRuleSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (rule != null && sets.TryGetValue(rule.surfaceId, out var set)) { rule.behaviours = set; EditorUtility.SetDirty(rule); }
            }

            var db = AssetDatabase.LoadAssetAtPath<SurfaceCompatibilityDatabase>(CompatPath);
            if (db == null) { db = ScriptableObject.CreateInstance<SurfaceCompatibilityDatabase>(); AssetDatabase.CreateAsset(db, CompatPath); }
            db.pairs = Pairs();
            EditorUtility.SetDirty(db);
            db.Validate(errors, known);
            return errors;
        }

        private static SurfacePairRule P(string a, string b, SurfaceCombo c, string merge, string note) { return new SurfacePairRule { a = a, b = b, combo = c, mergeId = merge, note = note }; }

        private static SurfacePairRule[] Pairs()
        {
            var l = new List<SurfacePairRule>();
            foreach (var water in new[] { "surface-sea-water", "surface-river" })
                foreach (var lava in new[] { "surface-lava-crust", "surface-magma-bridge" })
                    l.Add(P(water, lava, SurfaceCombo.Merge, "surface-obsidian", "water cools lava into obsidian"));
            foreach (var lava in new[] { "surface-lava-crust", "surface-magma-bridge" })
                l.Add(P("surface-ice", lava, SurfaceCombo.Merge, "surface-sea-water", "ice melts to water"));
            l.Add(P("surface-ice", "surface-mud", SurfaceCombo.Forbidden, null, "two slow-down events never touch (stage-events.md)"));
            l.Add(P("surface-ice", "surface-glass", SurfaceCombo.Forbidden, null, "slide plus break is unfair"));
            l.Add(P("surface-mud", "surface-glass", SurfaceCombo.Forbidden, null, "adjacent mud and glass are forbidden (stage-events.md)"));
            l.Add(P("surface-cloud", "surface-glass", SurfaceCombo.Forbidden, null, "two breaking surfaces in a row"));
            l.Add(P("surface-neon-grid", "surface-ice", SurfaceCombo.Forbidden, null, "speed pad on ice cannot be steered"));
            l.Add(P("surface-wooden-bridge", "surface-lava-crust", SurfaceCombo.Forbidden, null, "wood on lava burns at once"));
            l.Add(P("surface-wooden-bridge", "surface-magma-bridge", SurfaceCombo.Forbidden, null, "wood on lava burns at once"));
            return l.ToArray();
        }
    }
}
