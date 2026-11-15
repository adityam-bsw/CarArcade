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

        private void OnDestroy() { if (_mesh != null) Destroy(_mesh); }      // the mesh is created in code, so it is ours to free

        private readonly List<Color> _baseColors = new List<Color>();

        /// <summary>Multiplies the vertex colours of every vertex whose world x lies in [x0, x1] by <paramref name="c"/> (surface crack, burn and fade looks). Looks only; no allocation.</summary>
        public void TintRange(float x0, float x1, Color c)
        {
            if (_mesh == null || _verts.Count == 0 || _baseColors.Count != _verts.Count) return;
            float ox = transform.position.x;
            for (int i = 0; i < _verts.Count; i++)
            {
                float x = _verts[i].x + ox;
                if (x >= x0 && x <= x1) _colors[i] = _baseColors[i] * c;
            }
            _mesh.SetColors(_colors);
        }

        /// <summary>Restores the untinted vertex colours.</summary>
        public void ClearTint()
        {
            if (_mesh == null || _baseColors.Count != _verts.Count || _verts.Count == 0) return;
            for (int i = 0; i < _colors.Count; i++) _colors[i] = _baseColors[i];
            _mesh.SetColors(_colors);
        }

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
            bool smooth = skin.SmoothStrip;

            // Atlas rows from the top: curb, body, skirt. v = 1 is the top row.
            float rows = Mathf.Max(1, skin.AtlasRows);
            float vTop = 1f;
            float vCurb = 1f - skin.curbRows / rows;
            float vBody = 1f - (skin.curbRows + skin.bodyRows) / rows;
            float vBottom = 0f;
            _vCapMid = 1f - (skin.curbRows + skin.bodyRows * 0.5f) / rows;
            _uScale = textured ? 1f / (skin.atlas.width * skin.texelWorld) : 0f;

            // Smooth strips are mapped 1:1 in world units (no row stretching): the strip's lip row sits on the collider top,
            // the rows above it rise over the collider, the slab ends where the thickness says, and the fade runs out the
            // rest of the strip height. Four rings, same as the pixel layout.
            float texH = textured ? skin.atlas.height : 1f;
            float lipW = smooth ? skin.lipPx * skin.texelWorld : 0f;
            float vSurface = 1f, vSlab = 0f, skirtDepth = g.SupportDepth;
            if (smooth)
            {
                vSurface = 1f - skin.lipPx / texH;
                vSlab = Mathf.Max(0.02f, vSurface - g.Thickness / (skin.texelWorld * texH));
                skirtDepth = vSlab * texH * skin.texelWorld;
                _vCapMid = vSurface - g.Thickness * 0.5f / (skin.texelWorld * texH);
            }

            Color tint = skin.tint;

            for (int i = 0; i < g.Count; i++)
            {
                Vector2 p = g.Spine[i];
                Vector2 norm = g.Normals[i];
                float u = g.Arc[i] * _uScale;

                Vector2 v0 = p + norm * (g.HalfThick + lipW);         // top of the art (the collider top when the lip is 0)
                Vector2 v1 = p + norm * (g.HalfThick - curbThick);    // top surface to slab body
                Vector2 v2 = p - norm * g.HalfThick;                  // bottom of the slab
                Vector2 v3 = v2 + new Vector2(0f, -skirtDepth);       // end of the shadow skirt below

                if (smooth)
                {
                    Vector2 vs = p + norm * g.HalfThick;                   // where tyres roll
                    Add(v0, tint, u, vTop);
                    Add(vs, tint, u, vSurface);
                    Add(v2, tint, u, vSlab);
                    Add(v3, tint, u, vBottom);
                }
                else if (textured)
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

            _baseColors.Clear(); _baseColors.AddRange(_colors);      // untinted copy for TintRange (section C surfaces)
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
