using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Two adjacent chunks must share the exact boundary collider vertex (no gap, no step) even on a slope change.</summary>
    public class RoadChunkContinuityTests
    {
        private static List<Vector2> Pts(float x0, float x1)
        {
            var p = new List<Vector2>();
            for (float x = x0; x <= x1 + 1e-4f; x += 0.16f) p.Add(new Vector2(x, Mathf.Sin(x * 0.45f) * 1.8f));
            return p;
        }

        [Test]
        public void ChunkJoin_SharesBoundaryVertex_NoGapNoStep()
        {
            var a = new GameObject("a", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            var b = new GameObject("b", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            a.Init(null); b.Init(null);
            var skin = RoadSkins.Fallback(RoadStyle.AsphaltHighway);
            var pa = Pts(0f, 6.4f);
            var pb = Pts(6.4f, 12f); pb[0] = pa[pa.Count - 1];
            a.Build(pa, 0, pa.Count - 1, 0.5f, 0.5f, skin);
            b.Build(pb, 0, pb.Count - 1, 0.5f, 0.5f, skin, a.Geometry.Normals[a.Geometry.Count - 1]);
            var ca = a.ColliderPoints; var cb = b.ColliderPoints;
            Assert.AreEqual(0f, Vector2.Distance(ca[ca.Count - 1], cb[0]), 1e-5f, "gap/step at chunk join");
            Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(skin);
        }
    
        [Test]
        public void ChunkJoin_GhostVertices_LinkNeighbours()
        {
            var a = new GameObject("a", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            var b = new GameObject("b", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            a.Init(null); b.Init(null);
            var skin = RoadSkins.Fallback(RoadStyle.AsphaltHighway);
            var pa = Pts(0f, 6.4f); var pb = Pts(6.4f, 12f); pb[0] = pa[pa.Count - 1];
            a.Build(pa, 0, pa.Count - 1, 0.5f, 0.5f, skin);
            b.Build(pb, 0, pb.Count - 1, 0.5f, 0.5f, skin, a.Geometry.Normals[a.Geometry.Count - 1]);
            a.TryGetEnds(out _, out _, out Vector2 aBeforeLast, out _);
            b.TryGetEnds(out _, out Vector2 bSecond, out _, out _);
            a.SetGhosts(false, default, true, bSecond);
            b.SetGhosts(true, aBeforeLast, false, default);
            var ea = a.GetComponent<EdgeCollider2D>(); var eb = b.GetComponent<EdgeCollider2D>();
            Assert.IsTrue(ea.useAdjacentEndPoint && !ea.useAdjacentStartPoint);
            Assert.IsTrue(eb.useAdjacentStartPoint && !eb.useAdjacentEndPoint);
            Assert.AreEqual(0f, Vector2.Distance(ea.adjacentEndPoint, bSecond), 1e-4f);
            Assert.AreEqual(0f, Vector2.Distance(eb.adjacentStartPoint, aBeforeLast), 1e-4f);
            Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject); Object.DestroyImmediate(skin);
        }

        [Test]
        public void RandomPolylines_2000_JoinsWatertight_NoSpikes_SkinIndependent()
        {
            var rng = new System.Random(1234);
            var skinA = RoadSkins.Fallback(RoadStyle.AsphaltHighway);
            var skinB = RoadSkins.Fallback(RoadStyle.DirtTrack);
            var a = new GameObject("a", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            var b = new GameObject("b", typeof(EdgeCollider2D)).AddComponent<RoadChunk>();
            a.Init(null); b.Init(null);
            float maxJoin = 0f, maxStep = 0f;
            for (int t = 0; t < 2000; t++)
            {
                var all = new List<Vector2>();
                float x = 0f, y = 0f, slope = 0f;
                int n = 20 + rng.Next(40);
                for (int i = 0; i < n; i++)
                {
                    slope = Mathf.Clamp(slope + ((float)rng.NextDouble() - 0.5f) * 0.6f, -1.2f, 1.2f);
                    float dx = 0.12f + (float)rng.NextDouble() * 0.1f;
                    x += dx; y += slope * dx; all.Add(new Vector2(x, y));
                }
                int cut = 4 + rng.Next(n - 8);
                var pa = all.GetRange(0, cut + 1); var pb = all.GetRange(cut, n - cut);
                a.Build(pa, 0, pa.Count - 1, 0.5f, 0.5f, skinA);
                b.Build(pb, 0, pb.Count - 1, 0.5f, 0.5f, t % 2 == 0 ? skinA : skinB, a.Geometry.Normals[a.Geometry.Count - 1]);
                var ca = a.ColliderPoints; var cb = b.ColliderPoints;
                maxJoin = Mathf.Max(maxJoin, Vector2.Distance(ca[ca.Count - 1], cb[0]));
                maxStep = Mathf.Max(maxStep, Mathf.Abs(cb[1].y - cb[0].y), Mathf.Abs(ca[ca.Count - 1].y - ca[ca.Count - 2].y));
                for (int i = 1; i < cb.Count; i++) Assert.Greater(cb[i].x, cb[i - 1].x - 0.2f, "non-monotonic x");
            }
            Assert.Less(maxJoin, 1e-5f, "join gap");
            Assert.Less(maxStep, 0.5f, "y step/spike");
            Object.DestroyImmediate(a.gameObject); Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(skinA); Object.DestroyImmediate(skinB);
        }
    }
}
