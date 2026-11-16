using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// How a road looks, and only that. Two modes: flat colours (the original look, also used by the neon road) or a
    /// pixel texture atlas that repeats along the road's length. A skin never changes where the road is or how it
    /// behaves; physics comes from RoadGeometry and the physics material alone.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Road Skin", fileName = "RoadSkin")]
    public class RoadSkin : ScriptableObject
    {
        public RoadStyle style;
        public string displayName;
        [Tooltip("Pixel (default): atlas stays point filtered as imported. Smooth: atlas is sampled bilinear. Switch only after the atlas is converted.")]
        public ArtStyle artStyle = ArtStyle.Pixel;

        [Header("Flat colours (used when no atlas is assigned)")]
        public Color curbColor = new Color(0.96f, 0.96f, 0.98f, 1f);
        public Color roadColor = new Color(0.22f, 0.24f, 0.28f, 1f);
        public Color underColor = new Color(0.46f, 0.36f, 0.26f, 0.95f);
        public Color fadeColor = new Color(0.28f, 0.20f, 0.12f, 0f);

        [Header("Textured (pixel atlas, top row = road top)")]
        [Tooltip("Repeats along the road. Point filtered, wrap U = Repeat, wrap V = Clamp.")]
        public Texture2D atlas;
        [Tooltip("Rows of the atlas used for the top surface, the slab body and the shadow under it.")]
        public int curbRows = 2;
        public int bodyRows = 4;
        public int skirtRows = 20;
        [Tooltip("World size of one atlas pixel. 14/216 matches the pixel background art at the default camera; smooth skins use 1/128 (gameplay ppu).")]
        public float texelWorld = 14f / 216f;
        public Color tint = Color.white;

        [Header("Smooth strips only (artStyle = Smooth)")]
        [Tooltip("Pixel row of the strip (counted from its top edge) where the driving surface sits. The strip is drawn so this row lines up with the collider top; the rows above it (grass blades, mud bumps) rise over the collider. Asphalt 4, grass 14: grass sits lower in its strip than asphalt.")]
        public float lipPx;
        [Tooltip("Surface id (surface-asphalt, surface-grass-trail ...) this smooth skin replaces when the world is Smooth. Empty on pixel skins.")]
        public string surfaceId;

        [Header("Animation frames (smooth strips): the strip repeated in another state, swapped on the shared material (no rebuild)")]
        [Tooltip("Frame 0 is the atlas; frame i is frames[i-1]. Conveyor belt B, electric rail off, fragile paper torn.")]
        public Texture2D[] frames = new Texture2D[0];

        [System.NonSerialized] private Material _material;
        [System.NonSerialized] private Texture2D _materialAtlas;
        [System.NonSerialized] private int _frame;

        public int FrameCount => 1 + (frames != null ? frames.Length : 0);
        public int Frame => _frame;
        /// <summary>Shows animation frame <paramref name="i"/> (0 = the main atlas). One texture assignment on the shared material; a missing frame falls back to 0.</summary>
        public void SetFrame(int i)
        {
            if (frames == null || i < 0 || i > frames.Length) i = 0;
            if (i == _frame) return;
            _frame = i;
            if (_material == null) return;
            var t = i == 0 ? atlas : frames[i - 1];
            if (t != null) { ApplySmoothSampling(t); _material.mainTexture = t; }
        }

        private void ApplySmoothSampling(Texture2D t)
        {
            if (artStyle != ArtStyle.Smooth || t == null) return;
            t.filterMode = FilterMode.Bilinear; t.wrapModeU = TextureWrapMode.Repeat; t.wrapModeV = TextureWrapMode.Clamp;
        }

        public bool Textured => atlas != null;
        public int AtlasRows => curbRows + bodyRows + skirtRows;
        /// <summary>True for a smooth strip drawn 1:1 (no row stretching) with its lip aligned to the collider top.</summary>
        public bool SmoothStrip => artStyle == ArtStyle.Smooth && atlas != null;
        public float CurbFraction => (float)curbRows / Mathf.Max(1, curbRows + bodyRows);

        public Material GetMaterial()
        {
            if (!Textured) return SpriteFactory.MeshMaterial;
            if (_material == null || _materialAtlas != atlas)
            {
                var baseMaterial = Resources.Load<Material>("RoadBuilder/RoadTextured");
                if (baseMaterial != null) _material = new Material(baseMaterial);
                else
                {
                    var shader = Shader.Find("RoadBuilder/RoadTextured");
                    _material = shader != null ? new Material(shader) : SpriteFactory.MeshMaterial;
                }
                _material.mainTexture = atlas; _materialAtlas = atlas; _frame = 0;
                // Smooth skins sample bilinear and repeat along the road (U) / clamp across it (V). Pixel skins are left as imported.
                if (artStyle == ArtStyle.Smooth)
                {
                    atlas.filterMode = FilterMode.Bilinear;
                    atlas.wrapModeU = TextureWrapMode.Repeat;
                    atlas.wrapModeV = TextureWrapMode.Clamp;
                }
            }
            return _material;
        }
    }

    /// <summary>Finds the skin for a road style. Falls back to the original flat colours if no asset exists.</summary>
    public static class RoadSkins
    {
        private static readonly Dictionary<RoadStyle, RoadSkin> Cache = new Dictionary<RoadStyle, RoadSkin>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Cache.Clear(); SmoothCache.Clear(); }

        public static RoadSkin Get(RoadStyle style)
        {
            if (Cache.TryGetValue(style, out var cached) && cached != null) return cached;

            var skin = Resources.Load<RoadSkin>("Roads/" + style);
            if (skin == null) skin = Fallback(style);
            Cache[style] = skin;
            return skin;
        }

        private static readonly Dictionary<string, RoadSkin> SmoothCache = new Dictionary<string, RoadSkin>();

        /// <summary>
        /// The smooth skin for a surface id (Resources/Roads/Smooth/{id}), or null when that surface has no smooth strip yet.
        /// Callers use it only when the current world is Smooth, so pixel worlds never receive one.
        /// </summary>
        public static RoadSkin GetSmooth(string surfaceId)
        {
            if (string.IsNullOrEmpty(surfaceId)) return null;
            if (SmoothCache.TryGetValue(surfaceId, out var cached)) return cached;
            var skin = Resources.Load<RoadSkin>("Roads/Smooth/" + surfaceId);
            SmoothCache[surfaceId] = skin;
            return skin;
        }

        /// <summary>Surface id of a legacy road style, for looking up its smooth variant.</summary>
        public static string SurfaceIdOf(RoadStyle style)
        {
            switch (style)
            {
                case RoadStyle.AsphaltHighway: return "surface-asphalt";
                case RoadStyle.DirtTrack: return "surface-dirt";
                default: return null;
            }
        }

        // The colours the road used before skins existed.
        public static RoadSkin Fallback(RoadStyle style)
        {
            var s = ScriptableObject.CreateInstance<RoadSkin>();
            s.style = style;
            if (style == RoadStyle.DirtTrack)
            {
                s.displayName = "Dirt Track";
                s.curbColor = new Color(0.86f, 0.68f, 0.42f, 1f);
                s.roadColor = new Color(0.55f, 0.38f, 0.22f, 1f);
                s.underColor = new Color(0.38f, 0.25f, 0.15f, 0.95f);
                s.fadeColor = new Color(0.25f, 0.15f, 0.08f, 0f);
            }
            else if (style == RoadStyle.NeonGrid)
            {
                s.displayName = "Neon Grid";
                s.curbColor = new Color(0.0f, 1.0f, 0.95f, 1f);
                s.roadColor = new Color(0.12f, 0.08f, 0.24f, 1f);
                s.underColor = new Color(0.75f, 0.10f, 0.85f, 0.95f);
                s.fadeColor = new Color(0.40f, 0.0f, 0.50f, 0f);
            }
            else s.displayName = "Asphalt Highway";
            return s;
        }
    }
}
