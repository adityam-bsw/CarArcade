using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Builds one smooth BackgroundSkin per generated world set (tools/art/make_bg_generic.py) under Assets/Art/Smooth/Backgrounds/Worlds/{id}/ into
    /// Resources/Backgrounds/world_{id}_smooth.asset. RoadGameManager.ApplyWorld picks that asset by catalogue id (world-{id}) over the pixel layers.
    /// Menu: Road Builder > Smooth > Build Generic World Skins. Safe to re-run.
    /// </summary>
    public static class SmoothWorldFactory
    {
        private const string Root = "Assets/Art/Smooth/Backgrounds/Worlds/";
        private const string BgDir = "Assets/Resources/Backgrounds";

        [MenuItem("Road Builder/Smooth/Build Generic World Skins")]
        public static void Build()
        {
            Directory.CreateDirectory(BgDir);
            int n = 0;
            foreach (var dir in Directory.GetDirectories(Root))
            {
                string id = Path.GetFileName(dir), key = id.Replace("-", "");
                string d = Root + id + "/";
                Sprite S(string f) { return AssetDatabase.LoadAssetAtPath<Sprite>(d + f + ".png"); }
                string path = BgDir + "/world_" + id + "_smooth.asset";
                var skin = AssetDatabase.LoadAssetAtPath<BackgroundSkin>(path);
                if (skin == null) { skin = ScriptableObject.CreateInstance<BackgroundSkin>(); AssetDatabase.CreateAsset(skin, path); }
                skin.displayName = id + " (Smooth)";
                skin.artStyle = ArtStyle.Smooth;
                skin.excludeFromRemix = true;
                skin.sky = S("bg_" + key + "_sky_4x576"); skin.far = S("bg_" + key + "_far_2048x576");
                skin.mid = S("bg_" + key + "_mid_2048x576"); skin.near = S("bg_" + key + "_near_2048x864");
                skin.sun = S("bg_" + key + "_sun_256");
                skin.nearAlternates = new Sprite[0]; skin.clouds = new Sprite[0]; skin.overlay = null;
                skin.scroll = new[] { 0f, 0.2f, 0.4f, 0.8f };
                var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tmp.LoadImage(File.ReadAllBytes(d + "bg_" + key + "_near_2048x864.png"));
                var c = tmp.GetPixel(0, 0); c.a = 1f; Object.DestroyImmediate(tmp);
                skin.groundFill = c;
                if (skin.sky == null || skin.far == null || skin.mid == null || skin.near == null) Debug.LogError("[SmoothWorldFactory] missing layer for " + id);
                EditorUtility.SetDirty(skin); n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[SmoothWorldFactory] built " + n + " generic smooth world skins");
        }
    }
}
