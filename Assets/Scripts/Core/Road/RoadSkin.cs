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
        [Tooltip("World size of one atlas pixel. 14/216 matches the background art at the default camera.")]
        public float texelWorld = 14f / 216f;
        public Color tint = Color.white;

        [System.NonSerialized] private Material _material;

        public bool Textured => atlas != null;
        public int AtlasRows => curbRows + bodyRows + skirtRows;
        public float CurbFraction => (float)curbRows / Mathf.Max(1, curbRows + bodyRows);

        public Material GetMaterial()
        {
            if (!Textured) return SpriteFactory.MeshMaterial;
            if (_material == null || _material.mainTexture != atlas)
            {
                var baseMaterial = Resources.Load<Material>("RoadBuilder/RoadTextured");
                if (baseMaterial != null) _material = new Material(baseMaterial);
                else
                {
                    var shader = Shader.Find("RoadBuilder/RoadTextured");
                    _material = shader != null ? new Material(shader) : SpriteFactory.MeshMaterial;
                }
                _material.mainTexture = atlas;
            }
            return _material;
        }
    }

    /// <summary>Finds the skin for a road style. Falls back to the original flat colours if no asset exists.</summary>
    public static class RoadSkins
    {
        private static readonly Dictionary<RoadStyle, RoadSkin> Cache = new Dictionary<RoadStyle, RoadSkin>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Cache.Clear(); }

        public static RoadSkin Get(RoadStyle style)
        {
            if (Cache.TryGetValue(style, out var cached) && cached != null) return cached;

            var skin = Resources.Load<RoadSkin>("Roads/" + style);
            if (skin == null) skin = Fallback(style);
            Cache[style] = skin;
            return skin;
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
