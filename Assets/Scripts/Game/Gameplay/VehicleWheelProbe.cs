#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Wheel-layout verification probe (Editor and development builds only). Builds a synthetic asphalt course (flat, hill, ramp, landing; friction 1.25 like surface-asphalt),
    /// drives every smooth catalogue vehicle and the three slice cars on it for 15 s and writes one CSV row of measurements per vehicle plus a side-view PNG.
    /// Start in Play mode: VehicleWheelProbe.Run(outDir). Touches nothing in the save or the game state.
    /// </summary>
    public sealed class VehicleWheelProbe : MonoBehaviour
    {
        public static bool Done;
        public static readonly List<string> Lines = new List<string>();
        private string _out;
        private const float Friction = 1.25f;
        private const float Duration = 15f;
        // course (x ranges, metres)
        private const float FlatA = 8f, FlatB = 38f, HillA = 40f, HillB = 64f, RampA = 76f, RampB = 84f;

        public static void Run(string outDir)
        {
            Done = false; Lines.Clear();
            var go = new GameObject("VehicleWheelProbe");
            go.AddComponent<VehicleWheelProbe>()._out = outDir;
        }

        private void Start() { StartCoroutine(Routine()); }

        private static float Height(float x)
        {
            if (x >= HillA && x <= HillB) { float s = Mathf.Sin(Mathf.PI * (x - HillA) / (HillB - HillA)); return 3f * s * s; }
            if (x >= RampA && x <= RampB) return 0.375f * (x - RampA);
            return 0f;
        }

        private GameObject BuildRoad()
        {
            var road = new GameObject("ProbeRoad");
            var pts = new List<Vector2>();
            for (float x = -14f; x <= 170f; x += 0.25f)
            {
                if (Mathf.Abs(x - RampB) < 0.13f) { pts.Add(new Vector2(RampB, Height(RampB))); pts.Add(new Vector2(RampB, 0f)); }
                pts.Add(new Vector2(x, Height(x)));
            }
            var edge = road.AddComponent<EdgeCollider2D>();
            edge.points = pts.ToArray();
            edge.sharedMaterial = new PhysicsMaterial2D("ProbeAsphalt") { friction = Friction, bounciness = 0f };
            var lr = road.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.positionCount = pts.Count;
            for (int i = 0; i < pts.Count; i++) lr.SetPosition(i, new Vector3(pts[i].x, pts[i].y - 0.15f, 0f));
            lr.widthMultiplier = 0.3f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0.22f, 0.22f, 0.25f);
            lr.sortingOrder = 1;
            return road;
        }

        private sealed class Result
        {
            public string id; public int wheels;
            public float contact, omegaRatio, pitchMean, pitchMax, jitter, topSpeed, endX; public int flips, airSamples;
            public float rampPitchMax, hillContact, hillMinVx;
            public string note = "";
        }

        private int _stepCount;
        /// <summary>One manual physics step: the car's FixedUpdate, then Physics2D.Simulate (script mode, so the probe runs far faster than real time and is deterministic).</summary>
        private void Step(RoadCar car)
        {
            car.SendMessage("FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            _stepCount++;
        }
        private static float Norm(float a) { a %= 360f; if (a > 180f) a -= 360f; if (a < -180f) a += 360f; return a; }

        private IEnumerator Routine()
        {
            Directory.CreateDirectory(_out); Directory.CreateDirectory(Path.Combine(_out, "shots"));
            int oldCapture = Time.captureFramerate; Time.captureFramerate = 60;
            var oldMode = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
            var road = BuildRoad();
            var camGo = new GameObject("ProbeCam"); var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 1.45f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.82f, 0.95f);
            cam.enabled = false; cam.nearClipPlane = -50f; cam.farClipPlane = 50f;
            var rt = new RenderTexture(768, 384, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;

            var cat = Resources.Load<AssetCatalog>("Catalog/AssetCatalog");
            var jobs = new List<(string id, System.Func<RoadCar> make, float speedMul)>();
            foreach (var e in cat.entries)
            {
                if (e.kind != CatalogKind.Vehicle || !e.HasSmooth) continue;
                var rig = e.MakeRig(ArtStyle.Smooth); var sm = e.speed;
                jobs.Add((e.id, () => RoadCar.Create(new Vector2(-6f, 2f), rig), sm));
            }
            foreach (var t in new[] { VehicleType.ClassicRed, VehicleType.SportCyan, VehicleType.MonsterTruck })
            { var tt = t; jobs.Add(("slice-" + tt, () => RoadCar.Create(new Vector2(-6f, 2f), tt, ArtStyle.Smooth), 1f)); }

            var csv = new StringBuilder("id,wheels,contact,omegaRatio,pitchMean,pitchMax,flips,jitter,topSpeed,endX,rampPitchMax,hillContact,hillMinVx,airSamples,note\n");
            foreach (var job in jobs)
            {
                var r = new Result { id = job.id };
                var car = job.make();
                var joints = car.Body.GetComponents<WheelJoint2D>(); r.wheels = joints.Length;
                // settle, motor off, so the shot and the static pose are clean
                car.MotorOn = false; car.TargetSpeed = 6.5f * job.speedMul;
                for (int i = 0; i < 90; i++) Step(car);
                r.note += "restY=" + car.Body.position.y.ToString("0.000", CultureInfo.InvariantCulture) + ";";
                car.MotorOn = true;
                float t = 0f, prevVy = 0f; bool first = true, shot = false, wasFlipped = false;
                double cSum = 0; int cN = 0, hN = 0; double hSum = 0, oSum = 0; int oN = 0, pN = 0; double pSum = 0; float jit = 0f; r.hillMinVx = 99f;
                while (t < Duration)
                {
                    Step(car);
                    if ((_stepCount & 127) == 0) yield return null;
                    t += Time.fixedDeltaTime;
                    var b = car.Body; float x = b.position.x; float vx = b.linearVelocity.x, vy = b.linearVelocity.y;
                    float pitch = Norm(b.rotation);
                    r.topSpeed = Mathf.Max(r.topSpeed, vx); r.endX = x;
                    bool flipped = car.IsUpsideDown; if (flipped && !wasFlipped) r.flips++; wasFlipped = flipped;
                    int touching = 0;
                    for (int i = 0; i < joints.Length; i++) if (joints[i].connectedBody.IsTouching(new ContactFilter2D { useTriggers = false })) touching++;
                    if (touching == 0 && !car.Body.IsTouching(new ContactFilter2D { useTriggers = false })) r.airSamples++;
                    if (x > FlatA && x < FlatB)
                    {
                        cSum += touching / (float)joints.Length; cN++;
                        pSum += Mathf.Abs(pitch); pN++;
                        for (int i = 0; i < joints.Length; i++)
                        {
                            var w = joints[i].connectedBody; float rr = w.GetComponent<CircleCollider2D>().radius * w.transform.lossyScale.x;
                            if (vx > 0.5f) { oSum += -w.angularVelocity * Mathf.Deg2Rad * rr / vx; oN++; }
                        }
                        if (!first) jit = Mathf.Max(jit, Mathf.Abs(vy - prevVy) / Time.fixedDeltaTime);
                        if (!shot && x > 22f) { shot = true; yield return StartCoroutine(Shot(cam, rt, car, job.id)); }
                    }
                    if (x > HillA + 4f && x < HillB - 4f) { hSum += touching / (float)joints.Length; hN++; r.hillMinVx = Mathf.Min(r.hillMinVx, vx); }
                    if (x > RampA - 4f && x < RampB + 14f) r.rampPitchMax = Mathf.Max(r.rampPitchMax, Mathf.Abs(pitch));
                    r.pitchMax = Mathf.Max(r.pitchMax, Mathf.Abs(pitch));
                    prevVy = vy; first = false;
                    if (x > 165f) break;
                }
                r.contact = cN > 0 ? (float)(cSum / cN) : -1f; r.omegaRatio = oN > 0 ? (float)(oSum / oN) : -1f; r.pitchMean = pN > 0 ? (float)(pSum / pN) : -1f;
                r.jitter = jit; r.hillContact = hN > 0 ? (float)(hSum / hN) : -1f;
                string line = string.Join(",", r.id, r.wheels, F(r.contact), F(r.omegaRatio), F(r.pitchMean), F(r.pitchMax), r.flips, F(r.jitter), F(r.topSpeed), F(r.endX), F(r.rampPitchMax), F(r.hillContact), F(r.hillMinVx), r.airSamples, r.note);
                csv.AppendLine(line); Lines.Add(line); Debug.Log("[WheelProbe] " + line);
                Destroy(car.gameObject);
                yield return null; yield return null;
            }
            File.WriteAllText(Path.Combine(_out, "results.csv"), csv.ToString());
            Time.captureFramerate = oldCapture; Physics2D.simulationMode = oldMode;
            rt.Release(); Destroy(road); Destroy(camGo);
            Done = true; Debug.Log("[WheelProbe] DONE");
        }

        private static string F(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);

        private IEnumerator Shot(Camera cam, RenderTexture rt, RoadCar car, string id)
        {
            yield return new WaitForEndOfFrame();
            var p = car.Body.position;
            cam.transform.position = new Vector3(p.x, p.y + 0.1f, -10f);
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(_out, "shots", id + ".png"), tex.EncodeToPNG());
            Destroy(tex);
        }
    }
}
#endif
