using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Scene side of the Road-Eater: drives <see cref="RoadEaterModel"/>, deletes road behind its front through <see cref="RoadBuilder.EatBefore"/> at 10 Hz
    /// (mesh and collider in the same call), detects crossed road gaps for the chew delay and draws the maw. It holds no rules: every number comes from
    /// <see cref="MonsterDefinitionSO"/>, every decision from the model. Plain class owned by the manager; the few sprites are created once.
    /// </summary>
    public sealed class RoadEaterController
    {
        private readonly RoadBuilder _road;
        private readonly MonsterDefinitionSO _d;
        private readonly Camera _cam;
        private readonly RoadEaterModel _model;
        private Transform _maw;
        private SpriteRenderer _body;
        private readonly SpriteRenderer[] _teeth = new SpriteRenderer[5];
        private float _trimClock, _startX;
        private bool _inGap;

        public RoadEaterModel Model => _model;
        /// <summary>The manager supplies these; the controller never reads the player, the HUD or the save.</summary>
        public Action<string> Cue;
        public Action<string> Banner;
        public Action<float> Shake;
        /// <summary>The Snap resolved: the manager costs a heart (a shield absorbs it) and calls <see cref="RoadEaterModel.OnLifeLost"/> through the normal crash path.</summary>
        public Action Bite;
        public Action<bool> Cleared;          // true = timeout retreat, false = escape
        public Action NearMissed;

        public RoadEaterController(RoadBuilder road, MonsterDefinitionSO data, Camera cam)
        {
            _road = road; _d = data; _cam = cam; _model = new RoadEaterModel(data);
            _model.Raised += OnEvent;
        }

        public void Begin(float startX, uint seed)
        {
            _startX = startX; _model.Reset(seed); _trimClock = 0f; _inGap = false;
            SetVisible(false);
        }

        public void Stop() { _model.Reset(1u); SetVisible(false); }

        public void Warn() { _model.BeginWarn(); }

        public void StartChase(float carWorldX)
        {
            float oldest = _road.OldestRoadX();
            float behind = float.IsNaN(oldest) ? _d.startGap : carWorldX - oldest;
            _model.StartChase(carWorldX - _startX, behind);
        }

        public void OnLifeLost(float carWorldX)
        {
            _model.OnLifeLost(carWorldX - _startX);
            if (_model.State == EaterState.Pushback) Banner?.Invoke("PHEW!");
        }

        public void Tick(float dt, float carWorldX, float carY, float carSpeed, bool airborne, bool boosting)
        {
            if (!_model.Alive) return;
            float carS = carWorldX - _startX;
            _model.Tick(dt, carS, carSpeed, airborne, boosting);
            if (_model.Eating)
            {
                _trimClock += dt;
                if (_trimClock >= _d.trimTickSeconds) { _trimClock = 0f; Trim(); }
            }
            UpdateVisual(carWorldX, carY);
        }

        private void Trim()
        {
            float frontX = _startX + _model.EatFront;
            int n = _road.EatBefore(frontX, _d.wholeChunkMargin, _d.trimQuantum, 1);
            if (n > 0 && ParticleManager.Instance && _road.TryGetRoadYAtX(frontX, out float y))
                ParticleManager.Instance.SpawnBurst(new Vector2(frontX, y), new Color(0.45f, 0.32f, 0.2f), 3);   // rubble at the bite edge
            if (!_road.HasRoadAt(frontX))
            {
                if (!_inGap)
                {
                    _inGap = true;
                    float next = _road.NextRoadStartAfter(frontX);
                    if (!float.IsNaN(next)) _model.OnGapReached(next - frontX);
                }
            }
            else _inGap = false;
        }

        private void OnEvent(EaterEvent e)
        {
            switch (e)
            {
                case EaterEvent.WarnStart: Cue?.Invoke("sfx.eater.chew"); Shake?.Invoke(0.3f); break;
                case EaterEvent.ChaseStart: Cue?.Invoke("sfx.eater.chew"); SetVisible(true); break;
                case EaterEvent.SurgeTelegraph: Cue?.Invoke("vo.rob0.eater.surge"); Shake?.Invoke(0.25f); break;
                case EaterEvent.SnapStart: Cue?.Invoke("vo.rob0.eater.snap"); Shake?.Invoke(0.3f); break;
                case EaterEvent.BandChanged: if (_model.Band == EaterBand.Danger) Cue?.Invoke("vo.rob0.eater.danger"); break;
                case EaterEvent.ChewStart: Cue?.Invoke("sfx.eater.jumpChew"); break;
                case EaterEvent.Bite: Cue?.Invoke("vo.rob0.eater.hit"); Bite?.Invoke(); break;
                case EaterEvent.NearMiss: Cue?.Invoke("vo.rob0.eater.nearmiss"); NearMissed?.Invoke(); break;
                case EaterEvent.Escaped: Cue?.Invoke("vo.rob0.eater.escape"); Cleared?.Invoke(false); break;
                case EaterEvent.TimedOut: Cue?.Invoke("vo.rob0.eater.retreat"); Cleared?.Invoke(true); break;
                case EaterEvent.Gone: SetVisible(false); break;
            }
        }

        // ------------------------------------------------------------------ view (procedural shapes, our own, smooth style)
        private void EnsureView()
        {
            if (_maw != null) return;
            _maw = new GameObject("RoadEaterMaw").transform;
            _body = SpriteFactory.CreateSprite("Body", _maw, SpriteFactory.Circle, new Color(0.22f, 0.1f, 0.28f), 30);
            _body.transform.localScale = new Vector3(3.6f, 3.2f, 1f);
            for (int i = 0; i < _teeth.Length; i++)
            {
                _teeth[i] = SpriteFactory.CreateSprite("Tooth" + i, _maw, SpriteFactory.Circle, new Color(0.96f, 0.94f, 0.88f), 31);
                _teeth[i].transform.localPosition = new Vector3(1.5f, (i - 2) * 0.55f, 0f);      // jagged row on the side facing the car
                _teeth[i].transform.localScale = new Vector3(0.45f, 0.5f, 1f);
            }
        }

        private void SetVisible(bool on) { if (on) EnsureView(); if (_maw != null) _maw.gameObject.SetActive(on); }

        private void UpdateVisual(float carX, float carY)
        {
            if (_maw == null || !_maw.gameObject.activeSelf) return;
            float trueX = _startX + _model.S;
            float edge = _cam != null ? _cam.transform.position.x - _cam.orthographicSize * _cam.aspect + 1.7f : trueX;
            float x = Mathf.Max(trueX, edge);                          // at the left screen edge while far, at its real place once close
            float y = _road.TryGetRoadYAtX(x, out float ry) ? ry : carY;
            _maw.position = new Vector3(x, y + 1.3f, 0f);
            float pulse = _model.State == EaterState.Snap ? 1f + 0.12f * Mathf.Sin(Time.time * 30f) : 1f + 0.04f * Mathf.Sin(Time.time * 6f);
            _maw.localScale = new Vector3(pulse, pulse, 1f);
            Color c = _model.State == EaterState.Escape || _model.State == EaterState.Retreat ? new Color(0.5f, 0.5f, 0.55f)
                : _model.Band == EaterBand.Snap ? new Color(0.55f, 0.05f, 0.1f) : _model.Band == EaterBand.Danger ? new Color(0.4f, 0.08f, 0.2f) : new Color(0.22f, 0.1f, 0.28f);
            _body.color = c;
        }

        public void Destroy() { if (_maw != null) UnityEngine.Object.Destroy(_maw.gameObject); _maw = null; }
    }
}
