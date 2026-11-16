using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Makes the 128x64 catalogue previews of the 13 behaviour surfaces from their smooth road strips (a 2x down-scaled crop around the driving surface, our own pixels),
    /// then rebuilds the catalogue so the surfaces show in the road picker. Menu: Road Builder / Catalog / Build Behaviour Surfaces.
    /// </summary>
    public static class BehaviourSurfaceCatalogTool
    {
        [MenuItem("Road Builder/Catalog/Build Behaviour Surfaces")]
        public static void Run()
        {
            foreach (var id in SmoothOnlySurfaces.Ids)
            {
                string key = id.Substring("surface-".Length).Replace('-', '_');
                string strip = "Assets/Art/Smooth/Roads/road_" + key + "_1024x192.png";
                string prev = "Assets/Art/Catalog/Previews/" + id + ".png";
                if (!File.Exists(strip)) { Debug.LogError("[BehaviourSurfaceCatalog] missing " + strip); continue; }
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false); src.LoadImage(File.ReadAllBytes(strip));
                var dst = new Texture2D(128, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 128; x++)
                    {
                        int sx = 256 + x * 2, sy = src.height - 1 - y * 2;      // 128 strip rows from the top, 2x down-scaled (Unity rows count from the bottom)
                        var c = (src.GetPixel(sx, sy) + src.GetPixel(sx + 1, sy) + src.GetPixel(sx, sy - 1) + src.GetPixel(sx + 1, sy - 1)) * 0.25f;
                        dst.SetPixel(x, 63 - y, c);
                    }
                File.WriteAllBytes(prev, dst.EncodeToPNG());
                Object.DestroyImmediate(src); Object.DestroyImmediate(dst);
            }
            AssetDatabase.Refresh();
            CatalogBuilder.Build();
            Debug.Log("[BehaviourSurfaceCatalog] previews written and catalogue rebuilt");
        }
    }
}
