#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Text;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>QA only: starts a real run per world and per vehicle and logs [ArtAudit] lines with the sprite and texture paths actually rendered (car, wheels, background layers, road, pickups).</summary>
    public sealed class ArtAuditProbe : MonoBehaviour
    {
        public static void Run(bool worlds, bool vehicles) { var go = new GameObject("ArtAuditProbe"); go.AddComponent<ArtAuditProbe>().StartCoroutine(go.GetComponent<ArtAuditProbe>().Routine(worlds, vehicles)); }

        private static string Tag(Sprite s)
        {
            if (s == null) return "null";
#if UNITY_EDITOR
            string p = UnityEditor.AssetDatabase.GetAssetPath(s.texture);
            if (!string.IsNullOrEmpty(p)) return (p.Contains("/Smooth/") ? "SMOOTH:" : "OLD:") + System.IO.Path.GetFileName(p);
#endif
            return s.name;
        }

        private IEnumerator Wait(float s) { float t = 0; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        private string Snap(RoadGameManager g)
        {
            var sb = new StringBuilder();
            var car = g.DebugCar;
            if (car != null) foreach (var sr in car.GetComponentsInChildren<SpriteRenderer>()) sb.Append("car[").Append(sr.name).Append("]=").Append(Tag(sr.sprite)).Append(' ');
            int bg = 0;
            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                string n = sr.transform.root.name + "/" + sr.name;
                if (sr.sprite == null) continue;
                if (n.ToLower().Contains("backg") || (sr.transform.parent != null && sr.transform.parent.name.ToLower().Contains("backg"))) { if (bg++ < 6) sb.Append("bg[").Append(sr.name).Append("]=").Append(Tag(sr.sprite)).Append(' '); }
                else if (sr.name.StartsWith("Pickup_")) { sb.Append("pk=").Append(Tag(sr.sprite)).Append(' '); break; }
            }
            foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (mr.sharedMaterial != null && mr.sharedMaterial.mainTexture != null && mr.transform.root == g.DebugRoad.transform.root) { sb.Append("road=").Append(mr.sharedMaterial.mainTexture.name).Append(' '); break; }
            return sb.ToString();
        }

        private IEnumerator Routine(bool worlds, bool vehicles)
        {
            var g = RoadGameManager.Instance;
            var cat = AssetCatalog.Load();
            if (worlds)
                foreach (var e in g.CheatCatalog(CatalogKind.World))
                {
                    g.CheatSelect(CatalogKind.World, e.id);
                    if (!g.DebugPlaying) { g.StartRace(); yield return Wait(1.2f); }
                    else yield return Wait(0.8f);
                    Debug.Log("[ArtAudit] WORLD " + e.id + " " + Snap(g));
                }
            if (vehicles)
                foreach (var e in g.CheatCatalog(CatalogKind.Vehicle))
                {
                    g.CheatSelect(CatalogKind.Vehicle, e.id);
                    if (!g.DebugPlaying) { g.StartRace(); yield return Wait(1.2f); } else yield return Wait(0.6f);
                    Debug.Log("[ArtAudit] VEHICLE " + e.id + " smoothAvail=" + e.HasSmooth + " " + Snap(g));
                }
            Debug.Log("[ArtAudit] DONE");
        }
    }
}
#endif
