using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>Writes the Road-Eater, RO-B0, projectile, pattern and Countryside skin assets under Resources/Stage/Hazards from the code defaults (one source: <see cref="BossDefinitionSO.CreateDefault"/>).</summary>
    public static class HazardAssetBuilder
    {
        public const string Dir = "Assets/Resources/Stage/Hazards";

        [MenuItem("Road Builder/Stage/Build Hazard Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            Save(ScriptableObject.CreateInstance<MonsterDefinitionSO>(), Dir + "/MonsterDef-RoadEater.asset");
            var def = BossDefinitionSO.CreateDefault();
            def.bolt = Save(def.bolt, Dir + "/PRJ-BOLT.asset"); def.bomb = Save(def.bomb, Dir + "/PRJ-BOMB.asset"); def.roller = Save(def.roller, Dir + "/PRJ-SAW.asset");
            var seen = new System.Collections.Generic.Dictionary<string, AttackPatternSO>();
            AttackPatternSO Pat(AttackPatternSO p) { if (!seen.TryGetValue(p.id, out var s)) { s = Save(p, Dir + "/" + p.id + ".asset"); seen[p.id] = s; } return s; }
            for (int i = 0; i < def.pool1.Length; i++) def.pool1[i] = Pat(def.pool1[i]);
            for (int i = 0; i < def.pool2.Length; i++) def.pool2[i] = Pat(def.pool2[i]);
            for (int i = 0; i < def.pool3.Length; i++) def.pool3[i] = Pat(def.pool3[i]);
            Save(def, Dir + "/BossDef-RoB0.asset");
            for (ProjectileKind k = ProjectileKind.Bolt; k <= ProjectileKind.Roller; k++)
            {
                var skin = BossController.DefaultSkin(k);
                Save(skin, Dir + "/Skin-country-" + skin.archetypeId + ".asset");
            }
            BuildWorldAssets();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[Hazards] assets written to " + Dir);
        }


        public const string ArtDir = "Assets/Art/Smooth/Hazards";

        private const string EaterArtDir = "Assets/Art/Smooth/Hazards/RoadEater/";
        private static Sprite EaterSpr(string n) { return AssetDatabase.LoadAssetAtPath<Sprite>(EaterArtDir + n + ".png"); }
        private static void FillEaterArt(HazardArtSet art)
        {
            art.eaterHeads = new Sprite[5]; for (int i = 0; i < 5; i++) art.eaterHeads[i] = EaterSpr("eater_head_" + (i + 1));
            art.eaterFlames = new Sprite[4]; for (int i = 0; i < 4; i++) art.eaterFlames[i] = EaterSpr("eater_flame_" + (i + 1));
            art.eaterBodySegment = EaterSpr("eater_body_segment");
        }

        /// <summary>Points the existing HazardArt asset at the imported Road-Eater dragon sprites (no other asset is rewritten).</summary>
        [MenuItem("Road Builder/Stage/Assign Road-Eater Art")]
        public static void AssignEaterArt()
        {
            var art = AssetDatabase.LoadAssetAtPath<HazardArtSet>(Dir + "/HazardArt.asset");
            if (art == null) { Debug.LogError("[Hazards] HazardArt.asset missing"); return; }
            FillEaterArt(art); EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            Debug.Log("[Hazards] Road-Eater art assigned: heads " + art.eaterHeads.Length + ", flames " + art.eaterFlames.Length);
        }

        private static Sprite Spr(string name, float ppu)
        {
            string path = ArtDir + "/" + name + ".png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (imp == null) return null;
            if (imp.textureType != TextureImporterType.Sprite || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu))
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.spritePixelsPerUnit = ppu; imp.mipmapEnabled = false; imp.filterMode = FilterMode.Bilinear; imp.alphaIsTransparency = true;
                imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>15 world projectiles, 5 boss tiers, 10 Road-Eater variants and the art set (sprites imported as Sprite, own procedural art, see tools/art/make_hazard_art.py).</summary>
        [MenuItem("Road Builder/Stage/Build World Hazard Assets")]
        public static void BuildWorldAssets()
        {
            foreach (var d in new[] { Dir + "/World", Dir + "/Tiers", Dir + "/Eaters" }) if (!AssetDatabase.IsValidFolder(d)) AssetDatabase.CreateFolder(Dir, Path.GetFileName(d));
            var art = ScriptableObject.CreateInstance<HazardArtSet>();
            art.robotBody = Spr("robo_body", 85f); art.robotTrim = Spr("robo_trim", 85f); art.robotEye = Spr("robo_eye", 100f); art.robotAntenna = Spr("robo_antenna", 130f); art.robotJet = Spr("robo_jet", 80f);
            art.tellRing = Spr("tell_ring", 96f); art.tellMarker = Spr("tell_marker", 96f); art.tellGlow = Spr("tell_glow", 96f); art.trapStrip = Spr("trap_strip", 96f); art.finishGate = Spr("finish_gate", 32f); art.vignette = Spr("vignette", 128f);
            foreach (var v in EaterVariantSO.Defaults()) { var row = Object.Instantiate(v); row.name = v.id; Save(row, Dir + "/Eaters/" + v.id + ".asset"); }
            FillEaterArt(art); Save(art, Dir + "/HazardArt.asset");
            var sprites = new System.Collections.Generic.Dictionary<string, string> {
                { "hills", "prj_boulder" }, { "desert", "prj_sandspike" }, { "snow", "prj_iceshard" }, { "moon", "prj_gravityorb" }, { "mars", "prj_meteorcore" }, { "volcano", "prj_lavabomb" }, { "underwater", "prj_bubble" }, { "candy", "prj_gumdrop" },
                { "cave", "prj_crystal" }, { "synthwave", "prj_neonbolt" }, { "haunted", "prj_shadoworb" }, { "forest", "prj_thornball" }, { "city", "prj_drone" }, { "cloud", "prj_stormpearl" }, { "toxic", "prj_acidglob" } };
            foreach (var p in WorldProjectiles.Defaults())
            {
                var row = Object.Instantiate(p); row.name = p.id; row.sprite = Spr(sprites[p.worldId], 96f);
                row.tellSprite = p.telegraph == TelegraphShape.Marker ? art.tellMarker : p.telegraph == TelegraphShape.Ring ? art.tellRing : p.telegraph == TelegraphShape.Line ? null : art.tellGlow;
                Save(row, Dir + "/World/" + p.id + ".asset");
            }
            foreach (var t in BossTierSO.Defaults()) { var row = Object.Instantiate(t); row.name = t.id; Save(row, Dir + "/Tiers/" + t.id + ".asset"); }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[Hazards] world projectiles, tiers, eater variants and art set written");
        }

        private static T Save<T>(T so, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { EditorUtility.CopySerialized(so, existing); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(so, path); return so;
        }
    }
}
