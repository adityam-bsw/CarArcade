using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Creates the base textured-road material and one RoadSkin asset per road style, with the original flat colours,
    /// so the game looks exactly as before until a skin is given an atlas.
    /// Menu: Road Builder > Roads > Create Default Skins. Safe to re-run (existing skins keep their atlas).
    /// </summary>
    public static class RoadSkinFactory
    {
        private const string SkinDir = "Assets/Resources/Roads";
        private const string MaterialPath = "Assets/Resources/RoadBuilder/RoadTextured.mat";

        [MenuItem("Road Builder/Roads/Create Default Skins")]
        public static void Create()
        {
            Directory.CreateDirectory(SkinDir);
            EnsureMaterial();

            foreach (RoadStyle style in System.Enum.GetValues(typeof(RoadStyle)))
            {
                string path = $"{SkinDir}/{style}.asset";
                var skin = AssetDatabase.LoadAssetAtPath<RoadSkin>(path);
                if (skin == null)
                {
                    var template = RoadSkins.Fallback(style);
                    skin = template;
                    AssetDatabase.CreateAsset(skin, path);
                }
                skin.style = style;
                EditorUtility.SetDirty(skin);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RoadSkinFactory] Skins ready in " + SkinDir);
        }

        private static void EnsureMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null) return;
            var shader = Shader.Find("RoadBuilder/RoadTextured");
            if (shader == null) { Debug.LogError("[RoadSkinFactory] Shader RoadBuilder/RoadTextured not found"); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.CreateAsset(new Material(shader), MaterialPath);
        }
    }
}
