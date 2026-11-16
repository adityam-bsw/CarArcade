using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Pooled sprite puffs for RO-B0: jet trail, muzzle flash, sparks, smoke, explosion and projectile trails. 64 renderers are created once; Tick and Emit allocate nothing.</summary>
    public sealed class RoB0Fx
    {
        private sealed class Pt { public SpriteRenderer Sr; public Transform T; public Vector2 Pos, Vel; public float Age, Life, S0, S1, Grav; public Color C; public bool On; }

        private const int Size = 64;
        private readonly Pt[] _p = new Pt[Size];
        private readonly Vector2[] _lastTrail = new Vector2[24];
        private readonly Transform _root; private int _next;

        public RoB0Fx(Sprite sprite)
        {
            _root = new GameObject("RoB0Fx").transform;
            for (int i = 0; i < Size; i++)
            {
                var sr = SpriteFactory.CreateSprite("P" + i, _root, sprite, Color.white, RoB0Data.OrderFx);
                sr.enabled = false; _p[i] = new Pt { Sr = sr, T = sr.transform };
            }
            Clear();
        }

        public void Emit(Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c, float grav = 0f, int order = RoB0Data.OrderFx)
        {
            Pt p = null;
            for (int i = 0; i < Size; i++) if (!_p[i].On) { p = _p[i]; break; }
            if (p == null) { p = _p[_next]; _next = (_next + 1) % Size; }
            p.On = true; p.Pos = pos; p.Vel = vel; p.Age = 0f; p.Life = Mathf.Max(0.05f, life); p.S0 = s0; p.S1 = s1; p.Grav = grav; p.C = c;
            p.Sr.sortingOrder = order; p.Sr.enabled = true; p.T.position = pos; p.T.localScale = new Vector3(s0, s0, 1f); p.Sr.color = c;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < Size; i++)
            {
                var p = _p[i]; if (!p.On) continue;
                p.Age += dt; float k = p.Age / p.Life;
                if (k >= 1f) { p.On = false; p.Sr.enabled = false; continue; }
                p.Vel.y -= p.Grav * dt; p.Pos += p.Vel * dt; p.T.position = p.Pos;
                float s = Mathf.Lerp(p.S0, p.S1, k); p.T.localScale = new Vector3(s, s, 1f);
                var c = p.C; c.a *= 1f - k * k; p.Sr.color = c;
            }
        }

        public void Clear() { for (int i = 0; i < Size; i++) { _p[i].On = false; _p[i].Sr.enabled = false; } for (int i = 0; i < _lastTrail.Length; i++) _lastTrail[i] = new Vector2(9999f, 9999f); }

        /// <summary>Projectile trail: a puff every 0.35 m of travel per slot (0..23). Family picks the colour.</summary>
        public void Trail(int slot, Vector2 pos, int family)
        {
            if ((uint)slot >= (uint)_lastTrail.Length) return;
            if ((pos - _lastTrail[slot]).sqrMagnitude < 0.1225f) return;
            _lastTrail[slot] = pos;
            Emit(pos, Vector2.zero, family == 2 ? 0.3f : 0.4f, family == 2 ? 0.45f : 0.5f, 0.05f, RoB0Data.TrailColor(family), 0f, RoB0Data.OrderTrail);
        }

        public void Burst(Vector2 pos, int n, float speed, float life, float s0, float s1, Color c, float grav, ref uint rs)
        {
            for (int i = 0; i < n; i++)
            {
                rs = rs * 1664525u + 1013904223u; float a = ((rs >> 8) & 0xFFFF) / 65535f * 6.2832f;
                rs = rs * 1664525u + 1013904223u; float v = speed * (0.4f + 0.6f * (((rs >> 8) & 0xFFFF) / 65535f));
                Emit(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, life, s0, s1, c, grav);
            }
        }

        public void Destroy() { if (_root != null) UnityEngine.Object.Destroy(_root.gameObject); }
    }
}
