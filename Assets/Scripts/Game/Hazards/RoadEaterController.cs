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
        private float _trimClock, _startX, _carX, _carY, _rescueCd, _lastEatX = float.NegativeInfinity;
        private EaterVariantSO _variant; private EaterSignatureModule _sig; private EaterCounterplay _counter; private readonly EaterHost _host = new EaterHost();
        private Sprite _bodySprite;
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
        /// <summary>Gap list B fixes: the tank fraction (0..1) and a way to place a rescue ink ahead of the car while it is chased and nearly empty.</summary>
        public Func<float> Ink01 = () => 1f;
        public Action RescueInk;
        public EaterHost Host => _host;
        public EaterVariantSO Variant => _variant;
        /// <summary>Set when the chase ended by a counterplay option (not a plain escape): the manager pays the variant reward.</summary>
        public EaterVariantSO DefeatedVariant { get; private set; }
        public EaterCounterplay Counter => _counter;
        public int PursuitPhase => _counter != null ? _counter.PursuitPhase : 1;

        public RoadEaterController(RoadBuilder road, MonsterDefinitionSO data, Camera cam)
        {
            _road = road; _d = data; _cam = cam; _model = new RoadEaterModel(data); _host.Road = road;
            _model.Raised += OnEvent;
        }

        /// <summary>World variant (null = the plain Countryside Road-Eater). Call before <see cref="Begin"/>.</summary>
        public void Configure(EaterVariantSO v)
        {
            if (_variant == v) return;
            DestroyModules(); _variant = v; _bodySprite = null;
            if (v != null) { _sig = EaterSignatures.Create(v.signature); _counter = EaterCounterplays.Create(v.defeat); }
        }

        private void DestroyModules() { if (_sig != null) _sig.Destroy(); if (_counter != null) _counter.Destroy(); _sig = null; _counter = null; }

        public void Begin(float startX, uint seed)
        {
            _startX = startX; _model.Reset(seed); _trimClock = 0f; _inGap = false; DefeatedVariant = null; _rescueCd = 0f; _lastEatX = float.NegativeInfinity;
            Collectible.EatenBeforeX = float.NegativeInfinity;
            _host.ChaseStartX = startX; _host.HideMaw = false; _host.UseMawOverride = false; _host.ForceEatX = float.NegativeInfinity;
            if (_sig != null) _sig.Begin(_variant, _host, seed);
            if (_counter != null) _counter.Begin(_variant, _host);
            SetVisible(false);
        }

        public void Stop() { _model.Reset(1u); SetVisible(false); if (_sig != null) _sig.Stop(); if (_counter != null) _counter.Stop(); Collectible.EatenBeforeX = float.NegativeInfinity; }

        public void Warn() { _model.BeginWarn(); }

        public void StartChase(float carWorldX)
        {
            float oldest = _road.OldestRoadX();
            float behind = float.IsNaN(oldest) ? _d.startGap : carWorldX - oldest;
            _model.StartChase(carWorldX - _startX, behind);
        }

        public void OnLifeLost(float carWorldX)
        {
            _model.OnLifeLost(carWorldX - _startX); if (_counter != null) _counter.OnLifeLost();
            if (_model.State == EaterState.Pushback) Banner?.Invoke("PHEW!");
        }

        public void Tick(float dt, float carWorldX, float carY, float carSpeed, bool airborne, bool boosting)
        {
            if (!_model.Alive) return;
            float carS = carWorldX - _startX; _carX = carWorldX; _carY = carY;
            _model.Tick(dt, carS, carSpeed, airborne, boosting);
            TickVariant(dt, carWorldX, carY, airborne, boosting);
            if (_model.Eating)
            {
                _trimClock += dt;
                if (_trimClock >= _d.trimTickSeconds) { _trimClock = 0f; Trim(); }
            }
            UpdateVisual(carWorldX, carY);
        }

        private void TickVariant(float dt, float carX, float carY, bool airborne, bool boosting)
        {
            if (_model.Chasing)
            {
                _rescueCd -= dt;
                if (_rescueCd <= 0f && Ink01() < _d.inkRescueThreshold) { _rescueCd = 6f; RescueInk?.Invoke(); }    // forced rescue ink: a chase never strands the player without ink
            }
            if (_variant == null || !_model.Chasing) return;
            var h = _host; h.CarPos = new Vector2(carX, carY); h.Grounded = !airborne; h.Boosting = boosting; h.EaterX = _startX + _model.S; h.Gap = _model.Gap; h.Eating = _model.Eating;
            h.Clock += dt; h.ForceEatX = float.NegativeInfinity;
            float my = _road.TryGetRoadYAtX(h.EaterX, out float ry) ? ry : carY; h.MawPos = new Vector2(h.EaterX, my + 1.3f);
            h.Phase = PursuitPhase;
            bool can = _model.State == EaterState.Track && _model.GraceLeft <= 0f && _model.Gap >= _d.bandEdge;
            if (_sig != null) _sig.Tick(dt, can);
            if (_counter != null && _counter.Tick(dt) && _model.Chasing) { DefeatedVariant = _variant; _model.Retreat(false); }
        }

        private void Trim()
        {
            float frontX = _startX + _model.EatFront;
            if (_host.ForceEatX > frontX) frontX = Mathf.Min(_host.ForceEatX, _carX - _d.protectedZone);
            if (frontX > Collectible.EatenBeforeX) Collectible.EatenBeforeX = frontX;                           // pickups over eaten road vanish with it
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
                case EaterEvent.Escaped: Cue?.Invoke(DefeatedVariant != null ? DefeatedVariant.announcerCueId : "vo.rob0.eater.escape"); Cleared?.Invoke(false); break;
                case EaterEvent.TimedOut: Cue?.Invoke("vo.rob0.eater.retreat"); Cleared?.Invoke(true); break;
                case EaterEvent.Gone: SetVisible(false); break;
            }
        }

        // ------------------------------------------------------------------ view (procedural shapes, our own, smooth style)
        private void EnsureView()
        {
            if (_maw != null) return;
            _maw = new GameObject("RoadEaterMaw").transform;
            var art = HazardArtSet.Load(); _bodySprite = art != null && _variant != null ? art.Eater(_variant.id) : null;
            _body = SpriteFactory.CreateSprite("Body", _maw, _bodySprite != null ? _bodySprite : SpriteFactory.Circle, _bodySprite != null ? Color.white : new Color(0.22f, 0.1f, 0.28f), 30);
            _body.transform.localScale = _bodySprite != null ? Vector3.one : new Vector3(3.6f, 3.2f, 1f);
            for (int i = 0; i < _teeth.Length; i++)
            {
                _teeth[i] = SpriteFactory.CreateSprite("Tooth" + i, _maw, SpriteFactory.Circle, new Color(0.96f, 0.94f, 0.88f), 31); _teeth[i].enabled = _bodySprite == null;
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
            _maw.position = _host.UseMawOverride ? new Vector3(_host.MawOverride.x, _host.MawOverride.y, 0f) : new Vector3(x, y + 1.3f, 0f);
            bool shown = !_host.HideMaw; _body.enabled = shown; for (int i = 0; i < _teeth.Length; i++) _teeth[i].enabled = shown && _bodySprite == null;
            float pulse = _model.State == EaterState.Snap ? 1f + 0.12f * Mathf.Sin(Time.time * 30f) : 1f + 0.04f * Mathf.Sin(Time.time * 6f);
            _maw.localScale = new Vector3(pulse, pulse, 1f);
            Color c = _model.State == EaterState.Escape || _model.State == EaterState.Retreat ? new Color(0.5f, 0.5f, 0.55f)
                : _model.Band == EaterBand.Snap ? new Color(0.55f, 0.05f, 0.1f) : _model.Band == EaterBand.Danger ? new Color(0.4f, 0.08f, 0.2f) : new Color(0.22f, 0.1f, 0.28f);
            if (_bodySprite != null) c = _model.State == EaterState.Escape || _model.State == EaterState.Retreat ? new Color(0.7f, 0.7f, 0.75f) : _model.Band == EaterBand.Snap ? new Color(1f, 0.5f, 0.5f) : _model.Band == EaterBand.Danger ? new Color(1f, 0.75f, 0.8f) : Color.white;
            else if (_variant != null && _model.Band != EaterBand.Snap && _model.Band != EaterBand.Danger && _model.State != EaterState.Escape && _model.State != EaterState.Retreat) c = _variant.tint;
            _body.color = c;
        }

        public void Destroy() { DestroyModules(); if (_maw != null) UnityEngine.Object.Destroy(_maw.gameObject); _maw = null; }
    }
}
