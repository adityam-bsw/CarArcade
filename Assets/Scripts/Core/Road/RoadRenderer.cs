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
        private readonly List<int> _backTris = new List<int>();
        private Material[] _mats2;
        private const float BackingDepth = 0.6f;   // metres of opaque-to-clear backing under a smooth slab

        // Atlas mapping for the segment being built (textured mode only).
        private float _uBase;         // whole tiles subtracted from u (precision); same for every vertex of a chunk
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

        /// <summary>Taper length (m) of a free (cut or unjoined) slab end in smooth strips.</summary>
        private const float FrayLength = 0.9f;

        /// <summary>
        /// Rebuilds the mesh. <paramref name="freeStart"/> / <paramref name="freeEnd"/> mark chunk ends that touch no neighbouring chunk (an Eater cut,
        /// the runway ends): smooth slabs fray there (the body shrinks toward the end in a ragged, crumbling taper) instead of ending in a hard vertical rectangle.
        /// Looks only; the collider is untouched.
        /// </summary>
        public void Rebuild(RoadGeometry g, RoadSkin skin, bool freeStart = false, bool freeEnd = false)
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
            _backTris.Clear();

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
            // The strip is mapped by WORLD x (minus a whole number of tiles, for float precision) so every chunk joins the next with the same
            // texture phase: a chunk-local arc length restarted the art at each join (clipped craters, a tone step and a lip step).
            _uBase = Mathf.Floor(g.Spine[0].x * _uScale);

            for (int i = 0; i < g.Count; i++)
            {
                Vector2 p = g.Spine[i];
                Vector2 norm = g.Normals[i];
                float u = g.Spine[i].x * _uScale - _uBase;

                Vector2 v0 = p + norm * (g.HalfThick + lipW);         // top of the art (the collider top when the lip is 0)
                Vector2 v1 = p + norm * (g.HalfThick - curbThick);    // top surface to slab body
                Vector2 v2 = p - norm * g.HalfThick;                  // bottom of the slab
                Vector2 v3 = v2 + new Vector2(0f, -skirtDepth);       // end of the shadow skirt below

                if (smooth)
                {
                    Vector2 vs = p + norm * g.HalfThick;                   // where tyres roll
                    float f = FrayFactor(g, i, freeStart, freeEnd);        // 1 = full slab, smaller = crumbled end
                    float vSl = vSlab;
                    if (f < 0.999f)
                    {
                        float thick = g.Thickness * f;
                        v2 = vs - norm * thick;
                        v3 = v2 + new Vector2(0f, -skirtDepth * f);
                        vSl = Mathf.Max(0.02f, vSurface - thick / (skin.texelWorld * texH));
                    }
                    Add(v0, tint, u, vTop);
                    Add(vs, tint, u, vSurface);
                    Add(v2, tint, u, vSl);
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

            // Smooth strips: skip the round end caps. They are flat slab-colour fans without the lip art, so every segment
            // joint showed a notch on the top edge (the neighbouring strips already meet flush).
            if (!smooth)
            {
                if (g.StartTangent.sqrMagnitude > 1e-4f)
                    AddRoundCap(g.Spine[0], -g.StartTangent, g.Normals[0], g.HalfThick, skin, textured, g.Spine[0].x);
                if (g.EndTangent.sqrMagnitude > 1e-4f)
                    AddRoundCap(g.Spine[g.Count - 1], g.EndTangent, g.Normals[g.Count - 1], g.HalfThick, skin, textured, g.Spine[g.Count - 1].x);
            }

            // Backing under the slab: opaque at the slab bottom, clear BackingDepth lower, drawn before the strip (submesh 0),
            // so the strip's own fading skirt no longer lets background props show through right under the road.
            bool backing = smooth && skin.skirtColor.a > 0f;
            int backStart = _verts.Count;
            if (backing)
            {
                Color bc = skin.skirtColor; bc.a = 1f;
                Color bz = skin.skirtColor; bz.a = 0f;
                float bd = Mathf.Min(skirtDepth, BackingDepth);
                for (int i = 0; i < g.Count; i++)
                {
                    float ff = FrayFactor(g, i, freeStart, freeEnd);
                    Vector2 vTopB = g.Spine[i] + g.Normals[i] * g.HalfThick - g.Normals[i] * (g.Thickness * ff);
                    Vector2 vTopIn = g.Spine[i] + g.Normals[i] * (g.HalfThick * 0.5f);   // tucked under the slab so no gap shows
                    Add(vTopIn, bc, 0f, 0f);
                    Add(vTopB, bc, 0f, 0f);
                    Add(vTopB + new Vector2(0f, -bd), bz, 0f, 0f);
                }
                for (int i = 0; i < g.Count - 1; i++)
                {
                    int a = backStart + i * 3, b = backStart + (i + 1) * 3;
                    _backTris.Add(a); _backTris.Add(b); _backTris.Add(a + 1);
                    _backTris.Add(a + 1); _backTris.Add(b); _backTris.Add(b + 1);
                    _backTris.Add(a + 1); _backTris.Add(b + 1); _backTris.Add(a + 2);
                    _backTris.Add(a + 2); _backTris.Add(b + 1); _backTris.Add(b + 2);
                }
            }

            _baseColors.Clear(); _baseColors.AddRange(_colors);      // untinted copy for TintRange (section C surfaces)
            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.SetUVs(0, _uvs);
            if (backing)
            {
                _mesh.subMeshCount = 2;
                _mesh.SetTriangles(_backTris, 0);   // backing first, then the strip over it
                _mesh.SetTriangles(_tris, 1);
                if (_mats2 == null) _mats2 = new Material[2];
                _mats2[0] = SpriteFactory.MeshMaterial;
                _mats2[1] = skin.GetMaterial();
                _meshRenderer.sharedMaterials = _mats2;
            }
            else
            {
                _mesh.subMeshCount = 1;
                _mesh.SetTriangles(_tris, 0);
                _meshRenderer.sharedMaterial = skin.GetMaterial();
            }
            _mesh.RecalculateBounds();
        }

        // 1 inside the chunk; toward a free end the slab thins to ~15 % with a deterministic ragged jitter (no allocation, no randomness at run time).
        private static float FrayFactor(RoadGeometry g, int i, bool freeStart, bool freeEnd)
        {
            float d = float.MaxValue;
            if (freeStart) d = Mathf.Min(d, g.Arc[i]);
            if (freeEnd) d = Mathf.Min(d, g.Arc[g.Count - 1] - g.Arc[i]);
            if (d >= FrayLength) return 1f;
            float t = 1f - d / FrayLength;                                 // 0 inside .. 1 at the very end
            float h = Mathf.Abs(Mathf.Sin((i + 1) * 12.9898f) * 43758.5453f); h -= Mathf.Floor(h);
            float f = 1f - t * (0.80f + 0.12f * (h - 0.5f) * 2f);
            return Mathf.Clamp(f, 0.12f, 1f);
        }

        private void AddRoundCap(Vector2 center, Vector2 outwardDir, Vector2 norm, float radius, RoadSkin skin, bool textured, float arc)
        {
            const int capSegments = 6;
            int centerIdx = _verts.Count;
            Color inner = textured ? skin.tint : skin.roadColor;
            Color outer = textured ? skin.tint : skin.curbColor;
            float u = arc * _uScale - _uBase;

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
