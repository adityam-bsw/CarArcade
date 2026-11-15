using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Meteor storm hazard. After a start distance, every few seconds a strike is aimed at where the car will be:
    /// a pulsing red marker on the ground warns, then a rock falls and explodes. Inside the blast radius the car is hit
    /// (the manager treats it like any crash, so a shield absorbs it); inside the near-miss radius the player earns a bonus.
    /// Plain class owned by the manager; creates only a few small sprites and reuses nothing else.
    /// </summary>
    public sealed class MeteorSpawner
    {
        private sealed class Strike
        {
            public Transform marker, rock;
            public SpriteRenderer markerSr;
            public Vector2 target;
            public float age, warn;
        }

        private const float FallSeconds = 0.4f, DropHeight = 14f;

        private readonly GameplayTuning _t;
        private readonly RoadBuilder _road;
        private readonly List<Strike> _live = new List<Strike>(4);
        private float _nextIn;

        // Stage-driven mode (GameplayTuning.stageEvents): strikes happen only inside the EVT-METEOR window, at the event's interval and count, from a seeded generator.
        // Off (default for the old game): the legacy distance start and UnityEngine.Random are used and nothing below runs.
        private bool _windowOn;
        private float _winMin, _winMax;
        private int _winLeft;
        private Pcg32 _rng;
        /// <summary>True while the stage director owns the spawner.</summary>
        public bool StageDriven;
        /// <summary>Asked before every strike in stage mode (the director's CanSpawnHazard plus the respawn rules).</summary>
        public Func<bool> CanStrike;

        /// <summary>Raised when a marker appears (play the warning).</summary>
        public event Action Warned;
        /// <summary>Raised on impact with the blast centre; <c>hit</c> is true when the car is inside the blast.</summary>
        public event Action<Vector2, bool> Impacted;
        /// <summary>Raised when a strike landed close to the car but missed it.</summary>
        public event Action NearMissed;

        public MeteorSpawner(RoadBuilder road, GameplayTuning tuning) { _road = road; _t = tuning; }

        public void Reset()
        {
            foreach (var s in _live) Destroy(s);
            _live.Clear();
            _nextIn = 3f;
            _windowOn = false; _winLeft = 0;
        }

        /// <summary>Stage mode: the meteor window opened. The first strike comes one interval after the effect starts (never before the warning signals finished).</summary>
        public void BeginWindow(float intervalMin, float intervalMax, int maxStrikes, uint seed)
        {
            _windowOn = true; _winMin = intervalMin; _winMax = Mathf.Max(intervalMin, intervalMax); _winLeft = maxStrikes;
            _rng = new Pcg32(seed);
            _nextIn = NextInterval();
        }

        public void EndWindow() { _windowOn = false; }
        public bool WindowOpen => _windowOn;
        public int StrikesLeft => _winLeft;

        /// <summary>The car respawned: markers in the air are cleared and the next strike waits one full interval.</summary>
        public void OnRespawn()
        {
            _clearLive = true;      // applied at the start of the next Tick: a strike that hits the car raises this from inside Tick's own loop over the live list
            if (_windowOn) _nextIn = _winMax;
        }
        private bool _clearLive;

        private float NextInterval() { return _winMin + (_winMax - _winMin) * _rng.NextFloat01(); }
        private float Jitter() { return StageDriven ? (_rng.NextFloat01() * 2f - 1f) * 2.5f : UnityEngine.Random.Range(-2.5f, 2.5f); }

        private static void Destroy(Strike s)
        {
            if (s.marker != null) UnityEngine.Object.Destroy(s.marker.gameObject);
            if (s.rock != null) UnityEngine.Object.Destroy(s.rock.gameObject);
        }

        public void Tick(float dt, RoadCar car, float distance)
        {
            if (car == null || car.Body == null) return;
            if (_clearLive) { foreach (var c in _live) Destroy(c); _live.Clear(); _clearLive = false; }
            if (StageDriven)
            {
                if (_windowOn && _winLeft > 0 && (CanStrike == null || CanStrike()))
                {
                    _nextIn -= dt;
                    if (_nextIn <= 0f) { _nextIn = NextInterval(); _winLeft--; Spawn(car); }
                }
            }
            else if (distance >= _t.meteorStartDistance)
            {
                _nextIn -= dt;
                if (_nextIn <= 0f)
                {
                    _nextIn = UnityEngine.Random.Range(_t.meteorIntervalMin, _t.meteorIntervalMax);
                    Spawn(car);
                }
            }
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var s = _live[i];
                s.age += dt;
                float fallStart = s.warn;
                float pulse = 0.55f + 0.45f * Mathf.Sin(s.age * 18f);
                var c = s.markerSr.color; c.a = pulse; s.markerSr.color = c;
                if (s.age >= fallStart)
                {
                    float k = Mathf.Clamp01((s.age - fallStart) / FallSeconds);
                    s.rock.gameObject.SetActive(true);
                    s.rock.position = new Vector3(s.target.x, Mathf.Lerp(s.target.y + DropHeight, s.target.y, k * k), 0f);
                    s.rock.Rotate(0f, 0f, 540f * dt);
                    if (k >= 1f) { Land(s, car); Destroy(s); _live.RemoveAt(i); }
                }
            }
        }

        private void Spawn(RoadCar car)
        {
            float lead = _t.meteorWarnSeconds + FallSeconds;
            float vx = Mathf.Max(4f, car.Body.linearVelocity.x);
            float x = car.Body.position.x + vx * lead + Jitter();
            float y = _road.TryGetRoadYAtX(x, out float roadY) ? roadY : car.Body.position.y - 0.5f;

            var marker = new GameObject("MeteorMarker").transform;
            marker.position = new Vector3(x, y + 0.05f, 0f);
            marker.localScale = new Vector3(_t.meteorBlastRadius * 2.2f, 0.45f, 1f);
            var msr = marker.gameObject.AddComponent<SpriteRenderer>();
            msr.sprite = SpriteFactory.Circle;
            msr.color = new Color(1f, 0.2f, 0.15f, 0.8f);
            msr.sortingOrder = 12;

            var rock = new GameObject("Meteor").transform;
            rock.localScale = Vector3.one * 1.3f;
            var rsr = rock.gameObject.AddComponent<SpriteRenderer>();
            rsr.sprite = SpriteFactory.Circle;
            rsr.color = new Color(1f, 0.55f, 0.1f, 1f);
            rsr.sortingOrder = 16;
            rock.gameObject.SetActive(false);

            _live.Add(new Strike { marker = marker, rock = rock, markerSr = msr, target = new Vector2(x, y), warn = _t.meteorWarnSeconds });
            Warned?.Invoke();
        }

        private void Land(Strike s, RoadCar car)
        {
            float d = Vector2.Distance(car.Body.position, s.target);
            bool hit = d <= _t.meteorBlastRadius;
            Impacted?.Invoke(s.target, hit);
            if (!hit && d <= _t.nearMissRadius) NearMissed?.Invoke();
        }
    }
}
