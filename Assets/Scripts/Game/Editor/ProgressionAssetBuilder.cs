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
            int made = Make<UpgradeTreeSO>("UpgradeTree") + Make<WorldGateSO>("WorldGates") + Make<VehicleGateSO>("VehicleGates") + Make<ProgressionTuningSO>("ProgressionTuning");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProgressionAssetBuilder] created " + made + " asset(s) in " + Dir);
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
