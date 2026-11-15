using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>Creates the progression ScriptableObjects under Assets/Resources/Progression/ from the code defaults. Existing assets are kept (never overwritten), so hand-tuned values survive a re-run.</summary>
    public static class ProgressionAssetBuilder
    {
        public const string Dir = "Assets/Resources/Progression";

        [MenuItem("Road Builder/Progression/Create Data Assets")]
        public static void CreateAll()
        {
            Directory.CreateDirectory(Dir);
            int made = Make<UpgradeTreeSO>("UpgradeTree") + Make<WorldGateSO>("WorldGates") + Make<VehicleGateSO>("VehicleGates") + Make<ProgressionTuningSO>("ProgressionTuning")
                + Make<SurfaceGateSO>("SurfaceGates") + Make<VehicleProfileSO>("VehicleProfiles");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProgressionAssetBuilder] created " + made + " asset(s) in " + Dir);
        }

        /// <summary>Rewrites the gate, profile and tuning assets from the code defaults (the blueprint D2/D4/D6 tables). Use after changing a default; hand edits to those assets are lost. The upgrade tree is left alone.</summary>
        [MenuItem("Road Builder/Progression/Reset Data Assets To Defaults (overwrites)")]
        public static void ResetToDefaults()
        {
            Directory.CreateDirectory(Dir);
            foreach (var n in new[] { "WorldGates", "VehicleGates", "ProgressionTuning", "SurfaceGates", "VehicleProfiles" }) AssetDatabase.DeleteAsset(Dir + "/" + n + ".asset");
            CreateAll();
        }

        private static int Make<T>(string name) where T : ScriptableObject
        {
            string path = Dir + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return 0;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
            return 1;
        }
    }
}
