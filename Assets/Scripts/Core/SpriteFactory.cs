using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Small helpers for runtime-generated sprites and the shared sprite material.
    /// </summary>
    public static class SpriteFactory
    {
        static Sprite _circle;
        static Material _spriteMaterial;

        /// <summary>Anti-aliased white circle, 1 world unit in diameter.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "RuntimeCircle"
                };
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f, r = size * 0.5f - 1f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    byte a = (byte)(Mathf.Clamp01(r - d) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                return _circle;
            }
        }

        /// <summary>A white box sprite (fallback when an art asset is missing).</summary>
        public static Sprite Box(Color color)
        {
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        /// <summary>The project's default sprite material (works with the URP 2D renderer and in builds).</summary>
        public static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial != null) return _spriteMaterial;
                var tmp = new GameObject("tmp_sprite_material_probe");
                var sr = tmp.AddComponent<SpriteRenderer>();
                _spriteMaterial = sr.sharedMaterial;
                Object.Destroy(tmp);
                if (_spriteMaterial == null)
                    _spriteMaterial = new Material(Shader.Find("Sprites/Default"));
                return _spriteMaterial;
            }
        }

        static Material _meshMaterial;

        /// <summary>
        /// Vertex-coloured unlit material for procedural MeshRenderers (road, hills).
        /// Loaded from Resources so the shader is always included in builds.
        /// </summary>
        public static Material MeshMaterial
        {
            get
            {
                if (_meshMaterial != null) return _meshMaterial;
                _meshMaterial = Resources.Load<Material>("RoadBuilder/VertexColorUnlit");
                if (_meshMaterial == null)
                {
                    var shader = Shader.Find("RoadBuilder/VertexColorUnlit");
                    _meshMaterial = shader != null ? new Material(shader) : SpriteMaterial;
                }
                return _meshMaterial;
            }
        }

        public static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
