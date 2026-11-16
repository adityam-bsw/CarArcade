using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// Smooth catalogue vehicles (Assets/Art/Smooth/Vehicles2): import settings, which vehicles are linked, and that a smooth build
    /// of a car has the same world geometry as the pixel build (body box, wheel positions and radii, colliders, mass, torque).
    /// </summary>
    public class SmoothVehicleTests
    {
        private const string Folder = "Assets/Art/Smooth/Vehicles2/";
        private const float Tol = 1e-3f;

        [System.Serializable] private class Manifest { public string smooth_status; public int smooth_scale; public string smooth_project_path; }

        private static AssetCatalog Catalog() => AssetDatabase.LoadAssetAtPath<AssetCatalog>("Assets/Resources/Catalog/AssetCatalog.asset");

        private static Manifest ManifestOf(string id)
        {
            string p = "Docs/asset-manifest/items/" + id + ".json";
            return File.Exists(p) ? JsonUtility.FromJson<Manifest>(File.ReadAllText(p)) : null;
        }

        /// <summary>Folder of a vehicle's smooth art: its manifest smooth_project_path (Vehicles3 for the snow vehicles), else Vehicles2.</summary>
        private static string DirOf(string id)
        {
            var m = ManifestOf(id);
            return m == null || string.IsNullOrEmpty(m.smooth_project_path) ? Folder : m.smooth_project_path;
        }

        /// <summary>Vehicles whose manifest says smooth_status ready and whose smooth body exists: the set that must be linked.</summary>
        private static int ExpectedReadyCount()
        {
            int n = 0;
            foreach (var e in Catalog().entries)
            {
                if (e.kind != CatalogKind.Vehicle) continue;
                var m = ManifestOf(e.id);
                if (m != null && m.smooth_status == "ready" && File.Exists(DirOf(e.id) + "bodies/" + e.id + ".png")) n++;
            }
            return n;
        }

        [Test]
        public void Vehicles2_IsAVehicleFolder_ForTheImporter()
        {
            Assert.IsTrue(SmoothArt.IsVehicleFolder(Folder + "bodies/vehicle-car.png"));
            Assert.IsTrue(SmoothArt.IsVehicleFolder("Assets/Art/Smooth/Vehicles/x.png"));
            Assert.IsTrue(SmoothArt.IsVehicleFolder("Assets/Art/Smooth/Vehicles3/bodies/vehicle-snowmobile.png"));
            Assert.IsFalse(SmoothArt.IsVehicleFolder("Assets/Art/Smooth/Backgrounds/x.png"));
        }

        [Test]
        public void EveryVehicles2Texture_Ppu16xS_Bilinear_Astc5x5()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/'), "Assets/Art/Smooth/Vehicles3" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string id = Path.GetFileNameWithoutExtension(path);
                int us = id.LastIndexOf("_w");
                if (us > 0) id = id.Substring(0, us);
                var m = ManifestOf(id);
                Assert.IsNotNull(m, path + ": no manifest item");
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(16f * m.smooth_scale, imp.spritePixelsPerUnit, 1e-3f, path + " ppu must be 16 x S");
                Assert.AreEqual(FilterMode.Bilinear, imp.filterMode, path);
                Assert.IsFalse(imp.mipmapEnabled, path);
                var android = imp.GetPlatformTextureSettings("Android");
                Assert.IsTrue(android.overridden, path);
                Assert.AreEqual(TextureImporterFormat.ASTC_5x5, android.format, path);
                n++;
            }
            Assert.GreaterOrEqual(n, 90, "expected previews, bodies and wheels of the imported vehicles");
        }

        [Test]
        public void OnlyReadyVehicles_AreLinked_OthersKeepOldArt()
        {
            var cat = Catalog();
            int linked = 0;
            foreach (var e in cat.entries)
            {
                if (e.kind != CatalogKind.Vehicle) continue;
                var m = ManifestOf(e.id);
                bool ready = m != null && m.smooth_status == "ready";
                if (ready && File.Exists(DirOf(e.id) + "bodies/" + e.id + ".png")) { Assert.IsTrue(e.HasSmooth, e.id + " is ready but not linked (run Road Builder > Catalog > Link Smooth Vehicles)"); linked++; }
                else
                {
                    Assert.IsFalse(e.HasSmooth, e.id + " is not smooth-ready and must keep its old art");
                    Assert.AreSame(e.preview, e.PreviewFor(ArtStyle.Smooth), e.id);
                    Assert.AreSame(e.preview, e.MakeRig(ArtStyle.Smooth).body, e.id);
                }
                Assert.AreSame(e.preview, e.PreviewFor(ArtStyle.Pixel), e.id);
                Assert.AreSame(e.preview, e.MakeRig(ArtStyle.Pixel).body, e.id);
            }
            Assert.AreEqual(ExpectedReadyCount(), linked);
            Assert.GreaterOrEqual(linked, 25);
            foreach (var id in new[] { "vehicle-snowmobile", "vehicle-snowboard" })
                Assert.IsTrue(cat.Find(id).HasSmooth, id + " is smooth_status ready (Vehicles3)");
        }

        [Test]
        public void WidthUnits_StayTheOldClampedValue()
        {
            foreach (var e in Catalog().entries)
            {
                if (e.kind != CatalogKind.Vehicle || !e.HasSmooth) continue;
                float expect = e.id == "vehicle-bicycle" ? 1.85f : Mathf.Clamp(e.preview.rect.width / 14f, 1.5f, 3f);   // QA4 B10: the bicycle is a deliberate override
                Assert.AreEqual(expect, e.widthUnits, 1e-4f, e.id);
                Assert.AreEqual(e.widthUnits, e.MakeRig(ArtStyle.Smooth).width, 0f, e.id);
                Assert.AreEqual(e.smoothBody.rect.width, e.preview.rect.width * e.smoothScale, 1e-3f, e.id + " canvas = old x S");
                Assert.AreEqual(e.smoothBody.rect.height, e.preview.rect.height * e.smoothScale, 1e-3f, e.id);
            }
        }

        [Test]
        public void Bicycle_WheelRadius_IsSmallerThanMotorcycle_AndMonsterTruck()
        {
            float R(string id) { var e = Catalog().Find(id); var rig = e.MakeRig(ArtStyle.Smooth); float maxR = 0f; foreach (var w in rig.wheels) maxR = Mathf.Max(maxR, w.r / rig.body.rect.width * rig.width); return maxR; }
            Assert.Less(R("vehicle-bicycle"), R("vehicle-motorcycle"));
            Assert.Less(R("vehicle-bicycle"), R("vehicle-monster-truck"));
        }

        [Test]
        public void SmoothWheels_AreOldWheelsTimesS_AndMatchMappingMd()
        {
            // MAPPING.md lists, per vehicle, new wheel cx, cy, r, size in new pixels.
            var map = new Dictionary<string, List<float[]>>();
            var lines = new List<string>(File.ReadAllLines(Folder + "MAPPING.md"));
            lines.AddRange(File.ReadAllLines("Assets/Art/Smooth/Vehicles3/MAPPING.md"));
            foreach (var line in lines)
            {
                if (!line.StartsWith("| vehicle-")) continue;
                var cols = line.Split('|');
                string id = cols[1].Trim();
                var list = new List<float[]>();
                var parts = cols[9].Split(new[] { "->" }, System.StringSplitOptions.None);
                if (parts.Length < 2 || parts[1].Contains("none")) { map[id] = list; continue; }
                foreach (var w in parts[1].Split(';'))
                {
                    var nums = w.Trim().Trim('(', ')').Replace(")", "").Replace("(", "").Split(',');
                    var f = new float[4];
                    for (int i = 0; i < 4; i++) f[i] = float.Parse(nums[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    list.Add(f);
                }
                map[id] = list;
            }
            int checkedCars = 0;
            foreach (var e in Catalog().entries)
            {
                if (e.kind != CatalogKind.Vehicle || !e.HasSmooth) continue;
                Assert.IsTrue(map.TryGetValue(e.id, out var rows), e.id + " missing in MAPPING.md");
                int oldCount = e.wheels == null ? 0 : e.wheels.Length;
                Assert.AreEqual(oldCount, e.smoothWheels.Length, e.id);
                Assert.AreEqual(rows.Count, e.smoothWheels.Length, e.id + " vs MAPPING.md");
                for (int i = 0; i < rows.Count; i++)
                {
                    var w = e.smoothWheels[i];
                    Assert.AreEqual(rows[i][0], w.cx, 0.01f, e.id + " cx"); Assert.AreEqual(rows[i][1], w.cy, 0.01f, e.id + " cy");
                    Assert.AreEqual(rows[i][2], w.r, 0.01f, e.id + " r"); Assert.AreEqual(rows[i][3], w.sprite.rect.width, 0.01f, e.id + " size");
                    Assert.AreEqual(e.wheels[i].cx * e.smoothScale, w.cx, 1e-3f, e.id);
                    Assert.AreEqual(e.wheels[i].r * e.smoothScale, w.r, 1e-3f, e.id);
                }
                checkedCars++;
            }
            Assert.AreEqual(ExpectedReadyCount(), checkedCars);
        }

        private sealed class Geo
        {
            public Vector2 bodySize, bodyCenter; public Vector2[] poly; public float mass, torque;
            public List<Vector2> wheelPos = new List<Vector2>(), wheelAnchor = new List<Vector2>(), wheelSpriteSize = new List<Vector2>(), wheelSpriteCenter = new List<Vector2>();
            public List<float> wheelRadius = new List<float>();
        }

        private static Geo Measure(VehicleRig rig)
        {
            var car = RoadCar.Create(new Vector2(0f, 0f), rig);
            try
            {
                var g = new Geo();
                var body = car.Body.transform;
                var sr = body.Find("BodySprite").GetComponent<SpriteRenderer>();
                g.bodySize = sr.bounds.size; g.bodyCenter = sr.bounds.center - body.position;
                g.poly = body.GetComponent<PolygonCollider2D>().points;
                g.mass = car.Body.mass; g.torque = car.maxTorquePerWheel;
                foreach (var name in new[] { "BackWheel", "FrontWheel" })
                {
                    var t = car.transform.Find(name);
                    g.wheelPos.Add(t.position);
                    g.wheelRadius.Add(t.GetComponent<CircleCollider2D>().radius);
                    var ws = t.Find("WheelSprite");
                    g.wheelSpriteSize.Add(ws != null ? (Vector2)ws.GetComponent<SpriteRenderer>().bounds.size : Vector2.zero);
                    g.wheelSpriteCenter.Add(ws != null ? (Vector2)ws.GetComponent<SpriteRenderer>().bounds.center - (Vector2)t.position : Vector2.zero);
                }
                foreach (var j in body.GetComponents<WheelJoint2D>()) g.wheelAnchor.Add(j.anchor);
                return g;
            }
            finally { Object.DestroyImmediate(car.gameObject); }
        }

        [Test]
        public void SmoothBuild_HasSamePhysicsAndWorldGeometry_AsPixelBuild()
        {
            int n = 0;
            foreach (var e in Catalog().entries)
            {
                if (e.kind != CatalogKind.Vehicle || !e.HasSmooth) continue;
                var p = Measure(e.MakeRig(ArtStyle.Pixel));
                var s = Measure(e.MakeRig(ArtStyle.Smooth));
                string id = e.id;
                Assert.AreEqual(p.bodySize.x, s.bodySize.x, Tol, id + " body w"); Assert.AreEqual(p.bodySize.y, s.bodySize.y, Tol, id + " body h");
                Assert.AreEqual(p.bodyCenter.x, s.bodyCenter.x, Tol, id + " body cx"); Assert.AreEqual(p.bodyCenter.y, s.bodyCenter.y, Tol, id + " body cy");
                Assert.AreEqual(p.mass, s.mass, 0f, id); Assert.AreEqual(p.torque, s.torque, 0f, id);
                Assert.AreEqual(p.poly.Length, s.poly.Length, id);
                for (int i = 0; i < p.poly.Length; i++) Assert.AreEqual(0f, (p.poly[i] - s.poly[i]).magnitude, Tol, id + " collider pt " + i);
                for (int i = 0; i < 2; i++)
                {
                    Assert.AreEqual(0f, (p.wheelPos[i] - s.wheelPos[i]).magnitude, Tol, id + " wheel pos " + i);
                    Assert.AreEqual(p.wheelRadius[i], s.wheelRadius[i], Tol, id + " wheel radius " + i);
                    Assert.AreEqual(0f, (p.wheelAnchor[i] - s.wheelAnchor[i]).magnitude, Tol, id + " joint anchor " + i);
                    // the drawn wheel may be enlarged (WheelArt.visualScale, <= 1.25) about its centre; the physics radius above is unchanged
                    float vs = 1f;
                    foreach (var wa in e.smoothWheels) if (wa.visualScale > 0f) vs = Mathf.Max(vs, wa.visualScale);
                    Assert.LessOrEqual(vs, 1.25f + 1e-4f, id + " visual wheel scale");
                    Assert.That(s.wheelSpriteSize[i].x, Is.EqualTo(p.wheelSpriteSize[i].x).Within(Tol).Or.EqualTo(p.wheelSpriteSize[i].x * vs).Within(Tol), id + " wheel sprite w " + i);
                    Assert.AreEqual(0f, (p.wheelSpriteCenter[i] - s.wheelSpriteCenter[i]).magnitude, Tol, id + " wheel sprite centre " + i);
                }
                n++;
            }
            Assert.AreEqual(ExpectedReadyCount(), n);
        }
    }
}
