using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Links the smooth catalogue vehicles (Assets/Art/Smooth/Vehicles2) to their AssetCatalog entries. Only vehicles whose manifest
    /// item says smooth_status = ready AND whose files exist are linked; everything else (needs_work, sent back, not yet imported)
    /// keeps its old art. Touches only the smooth* fields: widthUnits, mass, torque, speed and the old wheels stay as built by CatalogBuilder.
    /// Wheel geometry is the old Catalog/wheels.json times the smooth scale S (MAPPING.md; wheels_new.json was not shipped, MAPPING.md holds the same numbers, checked by SmoothVehicleTests).
    /// Safe to re-run.
    /// </summary>
    public static class SmoothVehicleLinker
    {
        public const string Folder = "Assets/Art/Smooth/Vehicles2/";
        private const string ManifestDir = "Docs/asset-manifest/items/";
        private const string OldWheels = "Assets/Art/Catalog/wheels.json";

        /// <summary>
        /// Smooth wheels that look undersized next to their body are DRAWN larger (WheelArt.visualScale, about the wheel centre = joint anchor). The physics radius
        /// and every collider stay as built. Only the vehicle's smallest wheels are enlarged, so the tractor keeps its big rear wheel as is.
        /// </summary>
        public static readonly Dictionary<string, float> VisualWheelScale = new Dictionary<string, float>
        {
            { "vehicle-bus", 1.25f }, { "vehicle-semi-truck", 1.25f }, { "vehicle-camper", 1.25f }, { "vehicle-fire-truck", 1.25f }, { "vehicle-tractor", 1.25f },
        };

        [System.Serializable] private class Manifest { public string smooth_status; public int smooth_scale; public string smooth_project_path; }
        [System.Serializable] private class WheelRow { public float cx, cy, r; }
        [System.Serializable] private class WheelCar { public string id; public List<WheelRow> wheels; }
        [System.Serializable] private class WheelFile { public List<WheelCar> vehicles; }

        [MenuItem("Road Builder/Catalog/Link Smooth Vehicles")]
        public static void LinkMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset");
            if (catalog == null) { Debug.LogError("[SmoothVehicleLinker] AssetCatalog missing. Build the catalog first."); return; }
            int n = Link(catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[SmoothVehicleLinker] linked smooth art for " + n + " vehicles");
        }

        public static int Link(AssetCatalog catalog)
        {
            var old = new Dictionary<string, WheelCar>();
            if (File.Exists(OldWheels))
                foreach (var c in JsonUtility.FromJson<WheelFile>(File.ReadAllText(OldWheels)).vehicles) old[c.id] = c;
            int linked = 0;
            foreach (var e in catalog.entries)
            {
                if (e.kind != CatalogKind.Vehicle) continue;
                e.centerMassOnWheels = VisualWheelScale.ContainsKey(e.id);
                e.smoothPreview = null; e.smoothBody = null; e.smoothWheels = null; e.smoothScale = 1;
                string mf = ManifestDir + e.id + ".json";
                if (!File.Exists(mf)) continue;
                var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(mf));
                if (m.smooth_status != "ready" || m.smooth_scale < 1) continue;
                string dir = string.IsNullOrEmpty(m.smooth_project_path) ? Folder : (m.smooth_project_path.EndsWith("/") ? m.smooth_project_path : m.smooth_project_path + "/");   // Vehicles3 for the snow vehicles
                var prev = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "previews/" + e.id + ".png");
                var body = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "bodies/" + e.id + ".png");
                if (prev == null || body == null) continue;
                var list = new List<WheelArt>();
                VisualWheelScale.TryGetValue(e.id, out float vis);
                float minR = float.MaxValue;
                if (e.wheels != null) foreach (var w0 in e.wheels) minR = Mathf.Min(minR, w0.r);
                bool ok = true;
                if (e.wheels != null)
                    for (int i = 0; i < e.wheels.Length && ok; i++)
                    {
                        var ws = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "wheels/" + e.id + "_w" + i + ".png");
                        if (ws == null) { ok = false; break; }
                        var o = e.wheels[i];
                        float s = m.smooth_scale;
                        list.Add(new WheelArt { sprite = ws, cx = o.cx * s, cy = o.cy * s, r = o.r * s, visualScale = vis > 0f && o.r <= minR * 1.05f ? vis : 0f });
                    }
                if (!ok) { Debug.LogWarning("[SmoothVehicleLinker] " + e.id + ": wheel sprites missing, keeps old art"); continue; }
                e.smoothPreview = prev; e.smoothBody = body; e.smoothWheels = list.ToArray(); e.smoothScale = m.smooth_scale;
                linked++;
            }
            return linked;
        }
    }
}
