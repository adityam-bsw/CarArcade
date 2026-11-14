using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>
    /// Generates the few icons the fonts cannot supply (gear, star) as anti-aliased white sprites that the theme
    /// can tint. Our own artwork, so no licence entry is needed. Menu: Road Builder > UI > Build Icons.
    /// </summary>
    public static class IconFactory
    {
        public const string Dir = "Assets/Art/UI/Generated";
        public const string GearPath = Dir + "/ui_icon_gear.png";
        public const string StarPath = Dir + "/ui_icon_star.png";

        [MenuItem("Road Builder/UI/Build Icons")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            Save(GearPath, Gear());
            Save(StarPath, Star());
            AssetDatabase.Refresh();
            Debug.Log("[IconFactory] Icons ready in " + Dir);
        }

        // All shapes live in 0..1 space with the centre at (0.5, 0.5).
        private static bool Gear(Vector2 p)
        {
            Vector2 d = p - new Vector2(0.5f, 0.5f);
            if (d.magnitude < 0.15f) return false; // centre hole
            const int teeth = 8;
            float step = Mathf.PI * 2f / teeth;
            var poly = new Vector2[teeth * 4];
            for (int i = 0; i < teeth; i++)
            {
                float a = i * step;
                poly[i * 4 + 0] = Polar(a - 0.30f, 0.33f);
                poly[i * 4 + 1] = Polar(a - 0.17f, 0.47f);
                poly[i * 4 + 2] = Polar(a + 0.17f, 0.47f);
                poly[i * 4 + 3] = Polar(a + 0.30f, 0.33f);
            }
            return Inside(poly, p);
        }

        private static bool Star(Vector2 p)
        {
            var poly = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? 0.47f : 0.20f;
                poly[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r - 0.03f);
            }
            return Inside(poly, p);
        }

        private static Vector2 Polar(float angle, float r)
            => new Vector2(0.5f + Mathf.Cos(angle) * r, 0.5f + Mathf.Sin(angle) * r);

        private static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        private static System.Func<Vector2, bool> Gear() => p => Gear(p);
        private static System.Func<Vector2, bool> Star() => p => Star(p);

        private static void Save(string path, System.Func<Vector2, bool> shape)
        {
            const int size = 128, ss = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            if (shape(new Vector2((x + (sx + 0.5f) / ss) / size, (y + (sy + 0.5f) / ss) / size))) hits++;
                    byte a = (byte)Mathf.RoundToInt(255f * hits / (ss * ss));
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.spritePixelsPerUnit = 100f;
            imp.SaveAndReimport();
        }
    }
}
