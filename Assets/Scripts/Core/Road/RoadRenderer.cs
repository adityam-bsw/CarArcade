using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Draws one road segment from its geometry and a skin. It never touches the collider. In flat mode it builds the
    /// original coloured ribbon; in textured mode it maps a pixel atlas along the road by distance, so patterns never
    /// stretch on slopes and never run out, however far the road goes.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RoadRenderer : MonoBehaviour
    {
        private const int SortingOrder = 5;

        private Mesh _mesh;
        private MeshRenderer _meshRenderer;
        private readonly List<Vector3> _verts = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _tris = new List<int>();

        // Atlas mapping for the segment being built (textured mode only).
        private float _uScale;        // u units per world metre along the road
        private float _vCapMid;       // a point in the middle of the slab body, used for the flat end caps

        public void Init()
        {
            _mesh = new Mesh { name = "RoadRibbonMesh" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshRenderer.sortingOrder = SortingOrder;
        }

        public void Rebuild(RoadGeometry g, RoadSkin skin)
        {
            if (_mesh == null) Init();
            if (g == null || g.Count < 2) return;
            if (skin == null) skin = RoadSkins.Get(RoadStyle.AsphaltHighway);

            bool textured = skin.Textured;
            _meshRenderer.sharedMaterial = skin.GetMaterial();

            _verts.Clear();
            _colors.Clear();
            _uvs.Clear();
            _tris.Clear();

            float curbThick = g.Thickness * (textured ? skin.CurbFraction : 0.22f);

            // Atlas rows from the top: curb, body, skirt. v = 1 is the top row.
            float rows = Mathf.Max(1, skin.AtlasRows);
            float vTop = 1f;
            float vCurb = 1f - skin.curbRows / rows;
            float vBody = 1f - (skin.curbRows + skin.bodyRows) / rows;
            float vBottom = 0f;
            _vCapMid = 1f - (skin.curbRows + skin.bodyRows * 0.5f) / rows;
            _uScale = textured ? 1f / (skin.atlas.width * skin.texelWorld) : 0f;

            Color tint = skin.tint;

            for (int i = 0; i < g.Count; i++)
            {
                Vector2 p = g.Spine[i];
                Vector2 norm = g.Normals[i];
                float u = g.Arc[i] * _uScale;

                Vector2 v0 = p + norm * g.HalfThick;                  // top surface (where tyres roll)
                Vector2 v1 = p + norm * (g.HalfThick - curbThick);    // top surface to slab body
                Vector2 v2 = p - norm * g.HalfThick;                  // bottom of the slab
                Vector2 v3 = v2 + new Vector2(0f, -g.SupportDepth);   // end of the shadow skirt below

                if (textured)
                {
                    Add(v0, tint, u, vTop);
                    Add(v1, tint, u, vCurb);
                    Add(v2, tint, u, vBody);
                    Add(v3, tint, u, vBottom);
                }
                else
                {
                    Add(v0, skin.curbColor, 0f, 0f);
                    Add(v1, skin.roadColor, 0f, 0f);
                    Add(v2, skin.underColor, 0f, 0f);
                    Add(v3, skin.fadeColor, 0f, 0f);
                }
            }

            for (int i = 0; i < g.Count - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;
                AddQuad(a + 0, b + 0, a + 1, b + 1);
                AddQuad(a + 1, b + 1, a + 2, b + 2);
                AddQuad(a + 2, b + 2, a + 3, b + 3);
            }

            // Rounded end caps keep both ends of a stroke from looking cut off.
            if (g.StartTangent.sqrMagnitude > 1e-4f)
                AddRoundCap(g.Spine[0], -g.StartTangent, g.Normals[0], g.HalfThick, skin, textured, g.Arc[0]);
            if (g.EndTangent.sqrMagnitude > 1e-4f)
                AddRoundCap(g.Spine[g.Count - 1], g.EndTangent, g.Normals[g.Count - 1], g.HalfThick, skin, textured, g.Arc[g.Count - 1]);

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();
        }

        private void AddRoundCap(Vector2 center, Vector2 outwardDir, Vector2 norm, float radius, RoadSkin skin, bool textured, float arc)
        {
            const int capSegments = 6;
            int centerIdx = _verts.Count;
            Color inner = textured ? skin.tint : skin.roadColor;
            Color outer = textured ? skin.tint : skin.curbColor;
            float u = arc * _uScale;

            Add(center, inner, u, _vCapMid);
            for (int i = 0; i <= capSegments; i++)
            {
                float t = (float)i / capSegments;
                float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, t);
                Vector2 dir = outwardDir * Mathf.Cos(angle) + norm * Mathf.Sin(angle);
                Vector2 pt = center + dir * radius;
                Add(pt, outer, u, _vCapMid);
            }

            for (int i = 0; i < capSegments; i++)
            {
                _tris.Add(centerIdx);
                _tris.Add(centerIdx + 1 + i);
                _tris.Add(centerIdx + 2 + i);
            }
        }

        private void AddQuad(int a0, int b0, int a1, int b1)
        {
            _tris.Add(a0); _tris.Add(b0); _tris.Add(a1);
            _tris.Add(a1); _tris.Add(b0); _tris.Add(b1);
        }

        private void Add(Vector2 p, Color c, float u, float v)
        {
            _verts.Add(new Vector3(p.x, p.y, 0f));
            _colors.Add(c);
            _uvs.Add(new Vector2(u, v));
        }
    }
}
