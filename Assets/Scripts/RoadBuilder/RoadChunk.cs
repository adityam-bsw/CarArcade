using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum RoadStyle
    {
        AsphaltHighway = 0,
        DirtTrack = 1,
        NeonGrid = 2
    }

    /// <summary>
    /// Renders customizable, ultra-smooth road ribbons with rounded start/end caps,
    /// mitered normal extrusion, and perfectly aligned top-surface EdgeCollider2D.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
    public class RoadChunk : MonoBehaviour
    {
        Mesh _mesh;
        EdgeCollider2D _edge;
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _tris = new List<int>();
        readonly List<Vector2> _edgePoints = new List<Vector2>();

        public float MaxX { get; private set; }

        public void Init(Material material, PhysicsMaterial2D physicsMaterial)
        {
            _mesh = new Mesh { name = "RoadRibbonMesh" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;

            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.sortingOrder = 5;

            _edge = GetComponent<EdgeCollider2D>();
            _edge.sharedMaterial = physicsMaterial;
            _edge.edgeRadius = 0.04f;
        }

        public void Build(List<Vector2> pts, int start, int end, float roadThickness, float supportDepth, RoadStyle style)
        {
            int n = end - start + 1;
            if (n < 2) return;

            // Palette based on selected style
            Color curbCol = new Color(0.96f, 0.96f, 0.98f, 1f);
            Color roadCol = new Color(0.22f, 0.24f, 0.28f, 1f);
            Color underCol = new Color(0.46f, 0.36f, 0.26f, 0.95f);
            Color fadeCol = new Color(0.28f, 0.20f, 0.12f, 0.0f);

            if (style == RoadStyle.DirtTrack)
            {
                curbCol = new Color(0.86f, 0.68f, 0.42f, 1f);
                roadCol = new Color(0.55f, 0.38f, 0.22f, 1f);
                underCol = new Color(0.38f, 0.25f, 0.15f, 0.95f);
                fadeCol = new Color(0.25f, 0.15f, 0.08f, 0.0f);
            }
            else if (style == RoadStyle.NeonGrid)
            {
                curbCol = new Color(0.0f, 1.0f, 0.95f, 1f);
                roadCol = new Color(0.12f, 0.08f, 0.24f, 1f);
                underCol = new Color(0.75f, 0.10f, 0.85f, 0.95f);
                fadeCol = new Color(0.40f, 0.0f, 0.50f, 0.0f);
            }

            _verts.Clear();
            _colors.Clear();
            _tris.Clear();
            _edgePoints.Clear();

            // 1. Compute smooth continuous normals along the spine
            var normals = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 dir;
                if (i == 0)
                    dir = pts[start + 1] - pts[start];
                else if (i == n - 1)
                    dir = pts[end] - pts[end - 1];
                else
                {
                    Vector2 d0 = (pts[start + i] - pts[start + i - 1]).normalized;
                    Vector2 d1 = (pts[start + i + 1] - pts[start + i]).normalized;
                    dir = d0 + d1;
                }

                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
                dir.Normalize();

                // Always keep the road top surface facing generally upward in 2D
                Vector2 norm = new Vector2(-dir.y, dir.x);
                if (norm.y < 0f) norm = -norm;
                normals[i] = norm;
            }

            // Smooth normals with a 3-tap filter so ribbon thickness never pinches on tight curves
            if (n >= 3)
            {
                var smoothedNormals = new Vector2[n];
                smoothedNormals[0] = normals[0];
                smoothedNormals[n - 1] = normals[n - 1];
                for (int i = 1; i < n - 1; i++)
                {
                    Vector2 sn = (normals[i - 1] + normals[i] * 2f + normals[i + 1]) * 0.25f;
                    smoothedNormals[i] = sn.sqrMagnitude > 1e-5f ? sn.normalized : normals[i];
                }
                normals = smoothedNormals;
            }

            float halfThick = roadThickness * 0.5f;
            float curbThick = roadThickness * 0.22f;

            // 2. Build main ribbon vertices & EdgeCollider2D points
            float maxX = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                Vector2 p = pts[start + i];
                if (p.x > maxX) maxX = p.x;

                Vector2 norm = normals[i];

                // Top curb surface (where tires roll)
                Vector2 v0 = p + norm * halfThick;
                // Curb-to-asphalt transition
                Vector2 v1 = p + norm * (halfThick - curbThick);
                // Bottom of asphalt slab
                Vector2 v2 = p - norm * halfThick;
                // Soft vertical + normal support shadow skirt underneath
                Vector2 shadowOffset = new Vector2(0f, -supportDepth);
                Vector2 v3 = v2 + shadowOffset;

                // Align physics EdgeCollider2D with the driving surface (slightly inset by edgeRadius)
                _edgePoints.Add(p + norm * (halfThick - 0.03f));

                AddV(v0.x, v0.y, curbCol);
                AddV(v1.x, v1.y, roadCol);
                AddV(v2.x, v2.y, underCol);
                AddV(v3.x, v3.y, fadeCol);
            }

            for (int i = 0; i < n - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;

                AddQuad(a + 0, b + 0, a + 1, b + 1);
                AddQuad(a + 1, b + 1, a + 2, b + 2);
                AddQuad(a + 2, b + 2, a + 3, b + 3);
            }

            // 3. Add rounded end-caps at start and end of the road ribbon for a polished look
            Vector2 startTangent = (pts[start + 1] - pts[start]).normalized;
            if (startTangent.sqrMagnitude > 1e-4f)
            {
                AddRoundCap(pts[start], -startTangent, normals[0], halfThick, curbCol, roadCol);
            }

            Vector2 endTangent = (pts[end] - pts[end - 1]).normalized;
            if (endTangent.sqrMagnitude > 1e-4f)
            {
                AddRoundCap(pts[end], endTangent, normals[n - 1], halfThick, curbCol, roadCol);
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();

            _edge.SetPoints(_edgePoints);
            MaxX = maxX;
        }

        void AddRoundCap(Vector2 center, Vector2 outwardDir, Vector2 norm, float radius, Color outerCol, Color innerCol)
        {
            const int capSegments = 6;
            int centerIdx = _verts.Count;
            AddV(center.x, center.y, innerCol);

            for (int i = 0; i <= capSegments; i++)
            {
                float t = (float)i / capSegments;
                float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, t);
                Vector2 dir = outwardDir * Mathf.Cos(angle) + norm * Mathf.Sin(angle);
                Vector2 pt = center + dir * radius;
                AddV(pt.x, pt.y, outerCol);
            }

            for (int i = 0; i < capSegments; i++)
            {
                _tris.Add(centerIdx);
                _tris.Add(centerIdx + 1 + i);
                _tris.Add(centerIdx + 2 + i);
            }
        }

        void AddQuad(int a0, int b0, int a1, int b1)
        {
            _tris.Add(a0); _tris.Add(b0); _tris.Add(a1);
            _tris.Add(a1); _tris.Add(b0); _tris.Add(b1);
        }

        void AddV(float x, float y, Color c)
        {
            _verts.Add(new Vector3(x, y, 0f));
            _colors.Add(c);
        }
    }
}
