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
            foreach (ProjectileKind k in System.Enum.GetValues(typeof(ProjectileKind)))
            {
                var skin = BossController.DefaultSkin(k);
                Save(skin, Dir + "/Skin-country-" + skin.archetypeId + ".asset");
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("[Hazards] assets written to " + Dir);
        }

        private static T Save<T>(T so, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { EditorUtility.CopySerialized(so, existing); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(so, path); return so;
        }
    }
}
