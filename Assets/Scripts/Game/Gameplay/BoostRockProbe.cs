#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Boost-rocking probe (Editor and development builds only): drops each named catalogue vehicle on a long flat road, settles it, applies exactly what the game does on a boost
    /// (TargetSpeed + bonus and BoostKick) for 3 s, then drops it back, and records the pitch range (deg), body height change (m) and top speed. Script-mode physics, deterministic.
    /// Play mode: BoostRockProbe.Run("pickup,monster-truck,...") then read <see cref="Lines"/> until <see cref="Done"/>.
    /// </summary>
    public sealed class BoostRockProbe : MonoBehaviour
    {
        public static bool Done;
        public static readonly List<string> Lines = new List<string>();
        private string _ids; private const float Bonus = 4.5f;

        public static void Run(string ids) { Done = false; Lines.Clear(); new GameObject("BoostRockProbe").AddComponent<BoostRockProbe>()._ids = ids; }
        private static float Norm(float a) { a %= 360f; if (a > 180f) a -= 360f; if (a < -180f) a += 360f; return a; }
        private void Step(RoadCar car) { car.SendMessage("FixedUpdate"); Physics2D.Simulate(Time.fixedDeltaTime); }

        private IEnumerator Start()
        {
            var oldMode = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
            var road = new GameObject("BoostRoad"); var edge = road.AddComponent<EdgeCollider2D>();
            edge.points = new[] { new Vector2(-20f, 0f), new Vector2(600f, 0f) };
            edge.sharedMaterial = new PhysicsMaterial2D("BoostAsphalt") { friction = 1.25f, bounciness = 0f };
            var cat = Resources.Load<AssetCatalog>("Catalog/AssetCatalog");
            foreach (var id in _ids.Split(','))
            {
                AssetCatalogEntryRef e = default; bool found = false;
                foreach (var en in cat.entries) if (en.id == id && en.kind == CatalogKind.Vehicle) { e = new AssetCatalogEntryRef { entry = en }; found = true; break; }
                if (!found) { Lines.Add(id + ",NOT FOUND"); continue; }
                var rig = e.entry.MakeRig(ArtStyle.Smooth); float baseSpeed = 6.5f * e.entry.speed;
                var car = RoadCar.Create(new Vector2(-6f, 2f), rig);
                car.MotorOn = false; car.TargetSpeed = baseSpeed;
                for (int i = 0; i < 90; i++) Step(car);
                car.MotorOn = true;
                for (int i = 0; i < 300; i++) Step(car);                     // 6 s to cruise speed
                float y0 = car.Body.position.y, flatMin = 999f, flatMax = -999f, yMin = 999f, yMax = -999f, top = 0f, aMin = 999f, aMax = -999f; int tMin = 0, tMax = 0; var trace = new StringBuilder();
                for (int i = 0; i < 100; i++) { Step(car); float p = Norm(car.Body.rotation); flatMin = Mathf.Min(flatMin, p); flatMax = Mathf.Max(flatMax, p); }
                car.TargetSpeed = baseSpeed + Bonus; car.BoostKick(car.TargetSpeed);
                for (int i = 0; i < 400; i++)                                // 3 s of boost then 5 s after it ends
                {
                    if (i == 150) car.TargetSpeed = baseSpeed;
                    Step(car); var b = car.Body; float p = Norm(b.rotation);
                    if (p < aMin) { aMin = p; tMin = i; } if (p > aMax) { aMax = p; tMax = i; } if (i % 10 == 0) trace.Append(p.ToString("0.0", CultureInfo.InvariantCulture)).Append(' '); yMin = Mathf.Min(yMin, b.position.y - y0); yMax = Mathf.Max(yMax, b.position.y - y0); top = Mathf.Max(top, b.linearVelocity.x);
                }
                string line = string.Join(",", id, "cruisePitch=" + flatMin.ToString("0.0", CultureInfo.InvariantCulture) + ".." + flatMax.ToString("0.0", CultureInfo.InvariantCulture),
                    "boostPitch=" + aMin.ToString("0.0", CultureInfo.InvariantCulture) + ".." + aMax.ToString("0.0", CultureInfo.InvariantCulture), "dY=" + yMin.ToString("0.000", CultureInfo.InvariantCulture) + ".." + yMax.ToString("0.000", CultureInfo.InvariantCulture), "top=" + top.ToString("0.00", CultureInfo.InvariantCulture), "tMin=" + tMin + " tMax=" + tMax + " trace=" + trace);
                Lines.Add(line); Debug.Log("[BoostProbe] " + line);
                Destroy(car.gameObject); yield return null;
            }
            Physics2D.simulationMode = oldMode; Destroy(road); Done = true; Debug.Log("[BoostProbe] DONE"); Destroy(gameObject);
        }
        private struct AssetCatalogEntryRef { public CatalogEntry entry; }
    }
}
#endif
