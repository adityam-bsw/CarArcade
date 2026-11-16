using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>QA10-B1: a chunk that grows under the car must not recreate the collider the wheels stand on.</summary>
    public class RoadChunkGrowthTests
    {
        [Test]
        public void GrowingChunk_RecreatesSettledCollider_OnlyOncePerBlock()
        {
            var c = new GameObject("c", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            c.Init(null);
            var skin = RoadSkins.Fallback(RoadStyle.AsphaltHighway);
            var pts = new List<Vector2>();
            for (int i = 0; i < 40; i++) pts.Add(new Vector2(i * 0.16f, 0f));
            c.Build(pts, 0, pts.Count - 1, 0.5f, 0.5f, skin);
            int start = c.ColliderRebuilds;
            for (int i = 40; i < 100; i++) { pts.Add(new Vector2(i * 0.16f, 0f)); c.Build(pts, 0, pts.Count - 1, 0.5f, 0.5f, skin); }
            int added = c.ColliderRebuilds - start;          // 60 builds: 60 live-end rebuilds + one new block per 8 points, never rewriting a frozen block
            Assert.LessOrEqual(added, 60 + 60 / 8 + 6, "settled collider recreated too often");
            var cols = new List<EdgeCollider2D>(c.GetComponents<EdgeCollider2D>());
            cols.RemoveAll(x => !x.enabled);
            cols.Sort((a, b) => a.points[0].x.CompareTo(b.points[0].x));
            Assert.GreaterOrEqual(cols.Count, 3, "frozen blocks plus live end");
            int total = 0; for (int i = 0; i < cols.Count; i++) total += cols[i].pointCount;
            Assert.AreEqual(c.ColliderPoints.Count, total - (cols.Count - 1), "parts share exactly one vertex at each boundary (no gaps, no overlap)");
            var all = c.ColliderPoints; int idx = 0;
            foreach (var col in cols)
            {
                var pp = col.points;
                for (int k = 0; k < pp.Length; k++) Assert.AreEqual(0f, Vector2.Distance(pp[k], all[idx + k]), 1e-5f, "collider point equals visible road point");
                idx += pp.Length - 1;
            }
            Object.DestroyImmediate(c.gameObject); Object.DestroyImmediate(skin);
        }
    }
}
