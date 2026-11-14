using UnityEngine;

namespace RoadBuilderGame
{
    public enum EnvironmentTheme
    {
        Country = 0,
        Desert = 1,
        CyberNight = 2
    }

    /// <summary>
    /// Rich procedural backdrop supporting Country, Desert, and Cyber themes.
    /// Hills are anchored in world Y (with gentle vertical parallax) so they never bounce
    /// erratically when the car climbs or drops!
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        class HillLayer
        {
            public Mesh mesh;
            public float parallaxX, parallaxY, worldBaseY, amplitude, seed;
            public Color top, bottom;
            public Vector3[] verts;
            public Color[] colors;
        }

        const int HillSegments = 70;
        HillLayer[] _layers;
        Transform _celestialBody;
        SpriteRenderer _celestialSr;
        Transform[] _clouds;
        float[] _cloudU;
        float[] _cloudY;

        public EnvironmentTheme CurrentTheme { get; private set; } = EnvironmentTheme.Country;

        public void Init()
        {
            _layers = new[]
            {
                // Far hills: 10% X parallax, 85% Y lock (barely shifts in screen space vertically)
                CreateLayer("FarHills", 0.10f, 0.88f, -0.5f, 1.8f, 0f, -30),
                // Near hills: 28% X parallax, 72% Y lock (gentle vertical depth)
                CreateLayer("NearHills", 0.28f, 0.75f, -2.2f, 1.3f, 3.7f, -29),
            };

            var celestial = SpriteFactory.CreateSprite("CelestialBody", transform, SpriteFactory.Circle, Color.white, -40);
            celestial.transform.localScale = Vector3.one * 2.4f;
            _celestialBody = celestial.transform;
            _celestialSr = celestial;

            _clouds = new Transform[5];
            _cloudU = new float[_clouds.Length];
            _cloudY = new float[_clouds.Length];
            for (int i = 0; i < _clouds.Length; i++)
            {
                var cloud = new GameObject("Cloud" + i).transform;
                cloud.SetParent(transform, false);
                float s = Random.Range(0.8f, 1.4f);
                AddPuff(cloud, new Vector2(-0.7f, 0f), 1.1f * s);
                AddPuff(cloud, new Vector2(0f, 0.3f), 1.5f * s);
                AddPuff(cloud, new Vector2(0.8f, 0.05f), 1.2f * s);
                _clouds[i] = cloud;
                _cloudU[i] = i * 7.5f + Random.Range(-1.5f, 1.5f);
                _cloudY[i] = Random.Range(0.30f, 0.72f);
            }

            ApplyTheme(EnvironmentTheme.Country);
        }

        public void ApplyTheme(EnvironmentTheme theme)
        {
            CurrentTheme = theme;
            var cam = Camera.main;

            if (theme == EnvironmentTheme.Country)
            {
                if (cam != null) cam.backgroundColor = new Color(0.50f, 0.78f, 0.98f);
                _celestialSr.color = new Color(1f, 0.93f, 0.50f);
                _layers[0].top = new Color(0.64f, 0.84f, 0.78f);
                _layers[0].bottom = new Color(0.50f, 0.74f, 0.70f);
                _layers[1].top = new Color(0.45f, 0.75f, 0.42f);
                _layers[1].bottom = new Color(0.32f, 0.60f, 0.32f);
            }
            else if (theme == EnvironmentTheme.Desert)
            {
                if (cam != null) cam.backgroundColor = new Color(0.98f, 0.70f, 0.45f);
                _celestialSr.color = new Color(1f, 0.45f, 0.20f);
                _layers[0].top = new Color(0.88f, 0.60f, 0.38f);
                _layers[0].bottom = new Color(0.72f, 0.45f, 0.28f);
                _layers[1].top = new Color(0.95f, 0.72f, 0.32f);
                _layers[1].bottom = new Color(0.80f, 0.55f, 0.20f);
            }
            else if (theme == EnvironmentTheme.CyberNight)
            {
                if (cam != null) cam.backgroundColor = new Color(0.08f, 0.06f, 0.16f);
                _celestialSr.color = new Color(0.0f, 0.95f, 1.0f);
                _layers[0].top = new Color(0.35f, 0.12f, 0.50f);
                _layers[0].bottom = new Color(0.20f, 0.06f, 0.32f);
                _layers[1].top = new Color(0.18f, 0.08f, 0.35f);
                _layers[1].bottom = new Color(0.10f, 0.04f, 0.20f);
            }

            for (int l = 0; l < _layers.Length; l++)
            {
                for (int i = 0; i <= HillSegments; i++)
                {
                    _layers[l].colors[i * 2] = _layers[l].top;
                    _layers[l].colors[i * 2 + 1] = _layers[l].bottom;
                }
                _layers[l].mesh.colors = _layers[l].colors;
            }
        }

        void AddPuff(Transform parent, Vector2 pos, float size)
        {
            var sr = SpriteFactory.CreateSprite("Puff", parent, SpriteFactory.Circle, new Color(1f, 1f, 1f, 0.88f), -35);
            sr.transform.localPosition = pos;
            sr.transform.localScale = new Vector3(size, size * 0.8f, 1f);
        }

        HillLayer CreateLayer(string name, float parallaxX, float parallaxY, float worldBaseY, float amp, float seed, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SpriteFactory.MeshMaterial;
            mr.sortingOrder = order;

            var layer = new HillLayer
            {
                mesh = new Mesh { name = name },
                parallaxX = parallaxX,
                parallaxY = parallaxY,
                worldBaseY = worldBaseY,
                amplitude = amp,
                seed = seed,
                verts = new Vector3[(HillSegments + 1) * 2],
                colors = new Color[(HillSegments + 1) * 2]
            };
            layer.mesh.MarkDynamic();
            var tris = new int[HillSegments * 6];
            for (int i = 0; i < HillSegments; i++)
            {
                int v = i * 2, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            layer.mesh.vertices = layer.verts;
            layer.mesh.triangles = tris;
            mf.sharedMesh = layer.mesh;
            return layer;
        }

        public void Follow(Camera cam)
        {
            Vector3 c = cam.transform.position;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            // Sun/Moon stays calmly in the upper sky with very subtle vertical parallax
            _celestialBody.position = new Vector3(c.x + halfW * 0.65f, c.y * 0.92f + halfH * 0.55f, 0f);

            for (int i = 0; i < _clouds.Length; i++)
            {
                float span = halfW * 2f + 10f;
                float sx = Mathf.Repeat(_cloudU[i] - c.x * 0.08f, span) - span * 0.5f;
                _clouds[i].position = new Vector3(c.x + sx, c.y * 0.88f + halfH * _cloudY[i], 0f);
            }

            foreach (var l in _layers)
            {
                float x0 = c.x - halfW - 2f, x1 = c.x + halfW + 2f;
                float layerAnchorY = c.y * l.parallaxY + l.worldBaseY;

                for (int i = 0; i <= HillSegments; i++)
                {
                    float wx = Mathf.Lerp(x0, x1, i / (float)HillSegments);
                    float u = wx - c.x * (1f - l.parallaxX) + l.seed * 10f;
                    float h = Mathf.Sin(u * 0.21f) * 0.9f + Mathf.Sin(u * 0.53f + 1.3f) * 0.35f + Mathf.Sin(u * 0.09f + 2f) * 1.2f;
                    float y = layerAnchorY + h * l.amplitude;
                    l.verts[i * 2] = new Vector3(wx, y, 0f);
                    l.verts[i * 2 + 1] = new Vector3(wx, c.y - halfH - 4f, 0f);
                }
                l.mesh.vertices = l.verts;
                l.mesh.RecalculateBounds();
            }
        }
    }
}
