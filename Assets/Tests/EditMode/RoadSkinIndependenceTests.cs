using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// The contract of the three-layer design: how a road LOOKS must never change where it IS or how it behaves.
    /// Game logic reads only RoadGeometry and the collider, so these must hold for every skin.
    /// </summary>
    public class RoadSkinIndependenceTests
    {
        private readonly List<Object> _toDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _toDestroy) if (o != null) Object.DestroyImmediate(o);
            _toDestroy.Clear();
        }

        // A hilly stroke sampled the way RoadBuilder does (0.16 m spacing).
        private static List<Vector2> HillyRoad()
        {
            var pts = new List<Vector2>();
            for (float x = 0f; x <= 30f; x += 0.16f)
                pts.Add(new Vector2(x, Mathf.Sin(x * 0.45f) * 1.8f + Mathf.Sin(x * 0.17f) * 1.1f));
            return pts;
        }

        private RoadSkin FlatSkin(RoadStyle style)
        {
            var s = RoadSkins.Fallback(style);
            _toDestroy.Add(s);
            return s;
        }

        private RoadSkin TexturedSkin(int curb, int body, int skirt, Color tint)
        {
            var s = ScriptableObject.CreateInstance<RoadSkin>();
            var atlas = new Texture2D(64, curb + body + skirt, TextureFormat.RGBA32, false);
            s.atlas = atlas;
            s.curbRows = curb; s.bodyRows = body; s.skirtRows = skirt;
            s.tint = tint;
            _toDestroy.Add(s);
            _toDestroy.Add(atlas);
            return s;
        }

        private RoadChunk BuildChunk(List<Vector2> pts, RoadSkin skin)
        {
            var go = new GameObject("TestRoadChunk");
            _toDestroy.Add(go);
            var chunk = go.AddComponent<RoadChunk>();
            chunk.Init(new PhysicsMaterial2D("test") { friction = 1.25f, bounciness = 0f });
            chunk.Build(pts, 0, pts.Count - 1, 0.38f, 1.35f, skin);
            return chunk;
        }

        private static void AssertSamePoints(IReadOnlyList<Vector2> expected, IReadOnlyList<Vector2> actual, string what)
        {
            Assert.AreEqual(expected.Count, actual.Count, what + ": point count");
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].x, actual[i].x, 0f, what + ": x at " + i);
                Assert.AreEqual(expected[i].y, actual[i].y, 0f, what + ": y at " + i);
            }
        }

        [Test]
        public void ColliderPoints_AreIdentical_ForEverySkin()
        {
            var pts = HillyRoad();
            var reference = BuildChunk(pts, FlatSkin(RoadStyle.AsphaltHighway));
            var expected = new List<Vector2>(reference.ColliderPoints);
            Assert.Greater(expected.Count, 100, "test road should have many points");

            AssertSamePoints(expected, BuildChunk(pts, FlatSkin(RoadStyle.DirtTrack)).ColliderPoints, "flat dirt");
            AssertSamePoints(expected, BuildChunk(pts, FlatSkin(RoadStyle.NeonGrid)).ColliderPoints, "flat neon");
            AssertSamePoints(expected, BuildChunk(pts, TexturedSkin(2, 4, 20, Color.white)).ColliderPoints, "textured 2/4/20");
            AssertSamePoints(expected, BuildChunk(pts, TexturedSkin(3, 9, 5, new Color(1f, 0.6f, 0.4f))).ColliderPoints, "textured 3/9/5 tinted");
        }

        [Test]
        public void ColliderPoints_SitOnTheDrivingSurface()
        {
            // The collider is the spine pushed up its normal by half the thickness, minus a 3 cm inset.
            var pts = HillyRoad();
            var chunk = BuildChunk(pts, TexturedSkin(2, 4, 20, Color.white));
            var g = chunk.Geometry;
            for (int i = 0; i < g.Count; i++)
            {
                Vector2 expected = g.Spine[i] + g.Normals[i] * (0.38f * 0.5f - 0.03f);
                Assert.AreEqual(expected.x, chunk.ColliderPoints[i].x, 1e-5f);
                Assert.AreEqual(expected.y, chunk.ColliderPoints[i].y, 1e-5f);
            }
        }

        [Test]
        public void ColliderPhysicsMaterial_IsNotChangedBySkin()
        {
            var pts = HillyRoad();
            var a = BuildChunk(pts, FlatSkin(RoadStyle.AsphaltHighway));
            var b = BuildChunk(pts, TexturedSkin(2, 4, 20, Color.white));
            var ea = a.GetComponent<EdgeCollider2D>();
            var eb = b.GetComponent<EdgeCollider2D>();
            Assert.AreEqual(ea.sharedMaterial.friction, eb.sharedMaterial.friction);
            Assert.AreEqual(ea.edgeRadius, eb.edgeRadius);
        }

        [Test]
        public void TexturedMesh_MapsAtlasByDistance_AndStaysInRange()
        {
            var pts = HillyRoad();
            var skin = TexturedSkin(2, 4, 20, Color.white);
            var chunk = BuildChunk(pts, skin);
            var mesh = chunk.GetComponent<MeshFilter>().sharedMesh;
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            Assert.AreEqual(mesh.vertexCount, uvs.Count);
            float lastU = -1f;
            int knots = chunk.Geometry.Count;
            for (int i = 0; i < knots; i++)
            {
                float u = uvs[i * 4].x;                      // top vertex of each knot
                Assert.GreaterOrEqual(u, lastU - 1e-6f, "u must never run backwards along the road");
                lastU = u;
                Assert.AreEqual(1f, uvs[i * 4].y, 1e-6f, "top of the road samples the top row");
                Assert.AreEqual(0f, uvs[i * 4 + 3].y, 1e-6f, "end of the skirt samples the bottom row");
            }
            // Total u equals road length / (atlas width * texel size).
            float expectedU = chunk.Geometry.Arc[knots - 1] / (64f * skin.texelWorld);
            Assert.AreEqual(expectedU, uvs[(knots - 1) * 4].x, 1e-3f);
        }
    }
}
