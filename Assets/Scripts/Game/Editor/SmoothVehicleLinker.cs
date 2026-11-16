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
    /// Wheel geometry: vehicles marked changed in wheels_v4.json use those anchors (canvas px, own radius per wheel, 3 wheels allowed); the rest use the old Catalog/wheels.json times the smooth scale S (checked by SmoothVehicleTests).
    /// Safe to re-run.
    /// </summary>
    public static class SmoothVehicleLinker
    {
        public const string Folder = "Assets/Art/Smooth/Vehicles2/";
        private const string ManifestDir = "Docs/asset-manifest/items/";
        private const string OldWheels = "Assets/Art/Catalog/wheels.json";

        /// <summary>Long-wheelbase vehicles whose wheels sit off the body centre: their centre of mass goes midway between the outer axles (RoadCar).</summary>
        public static readonly HashSet<string> CenterMassVehicles = new HashSet<string>
        {
            "vehicle-bus", "vehicle-semi-truck", "vehicle-camper", "vehicle-fire-truck", "vehicle-tractor",
        };

        /// <summary>wheels_v4.json (written by the art forge, tools/art/vehicle_layout.py): anchors in smooth canvas pixels per vehicle; only entries with changed = true are used.</summary>
        public const string V4Json = Folder + "wheels_v4.json";

        [System.Serializable] public class V4Wheel { public float cx, cy, r, size, worldX, worldY, worldR; }
        [System.Serializable] public class V4Car { public int canvasW, canvasH, S; public float widthUnits; public bool changed; public List<V4Wheel> wheels; }

        /// <summary>Parses wheels_v4.json (id -> entry). JsonUtility cannot read a dictionary, so the top-level keys are split by hand.</summary>
        public static Dictionary<string, V4Car> ReadV4(string path = V4Json)
        {
            var map = new Dictionary<string, V4Car>();
            if (!File.Exists(path)) return map;
            string txt = File.ReadAllText(path);
            int i = txt.IndexOf('{') + 1;
            while (true)
            {
                int q1 = txt.IndexOf('"', i); if (q1 < 0) break;
                int q2 = txt.IndexOf('"', q1 + 1);
                string id = txt.Substring(q1 + 1, q2 - q1 - 1);
                int open = txt.IndexOf('{', q2), depth = 0, j = open;
                for (; j < txt.Length; j++) { if (txt[j] == '{') depth++; else if (txt[j] == '}' && --depth == 0) break; }
                map[id] = JsonUtility.FromJson<V4Car>(txt.Substring(open, j - open + 1));
                i = j + 1;
            }
            return map;
        }

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
            var v4 = ReadV4();
            int linked = 0;
            foreach (var e in catalog.entries)
            {
                if (e.kind != CatalogKind.Vehicle) continue;
                e.centerMassOnWheels = CenterMassVehicles.Contains(e.id);
                e.smoothPreview = null; e.smoothBody = null; e.smoothWheels = null; e.smoothScale = 1; e.smoothV4 = false;
                string mf = ManifestDir + e.id + ".json";
                if (!File.Exists(mf)) continue;
                var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(mf));
                if (m.smooth_status != "ready" || m.smooth_scale < 1) continue;
                string dir = string.IsNullOrEmpty(m.smooth_project_path) ? Folder : (m.smooth_project_path.EndsWith("/") ? m.smooth_project_path : m.smooth_project_path + "/");   // Vehicles3 for the snow vehicles
                var prev = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "previews/" + e.id + ".png");
                var body = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "bodies/" + e.id + ".png");
                if (prev == null || body == null) continue;
                var list = new List<WheelArt>();
                bool ok = true;
                bool useV4 = v4.TryGetValue(e.id, out var vc) && vc.changed && vc.wheels != null && vc.wheels.Count >= 2 && vc.S == m.smooth_scale;
                if (useV4)
                {
                    for (int i = 0; i < vc.wheels.Count && ok; i++)
                    {
                        var ws = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "wheels/" + e.id + "_w" + i + ".png");
                        if (ws == null) { ok = false; break; }
                        var o = vc.wheels[i];
                        list.Add(new WheelArt { sprite = ws, cx = o.cx, cy = o.cy, r = o.r });     // canvas pixels of the smooth body; no visual enlargement
                    }
                }
                else if (e.wheels != null)
                    for (int i = 0; i < e.wheels.Length && ok; i++)
                    {
                        var ws = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "wheels/" + e.id + "_w" + i + ".png");
                        if (ws == null) { ok = false; break; }
                        var o = e.wheels[i];
                        float s = m.smooth_scale;
                        list.Add(new WheelArt { sprite = ws, cx = o.cx * s, cy = o.cy * s, r = o.r * s });
                    }
                if (!ok) { Debug.LogWarning("[SmoothVehicleLinker] " + e.id + ": wheel sprites missing, keeps old art"); continue; }
                e.smoothV4 = useV4;
                e.smoothPreview = prev; e.smoothBody = body; e.smoothWheels = list.ToArray(); e.smoothScale = m.smooth_scale;
                linked++;
            }
            return linked;
        }
    }
}
