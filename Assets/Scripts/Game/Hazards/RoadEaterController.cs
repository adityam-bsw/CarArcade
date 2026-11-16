using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Scene side of the Road-Eater: drives <see cref="RoadEaterModel"/>, deletes road behind its front through <see cref="RoadBuilder.EatBefore"/> at 10 Hz
    /// (mesh and collider in the same call), detects crossed road gaps for the chew delay and draws the dragon (head chomp cycle, wavy body chain, surge flames). It holds no rules: every number comes from
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
        private float _trimClock, _startX, _carX, _carY, _rescueCd;
        private EaterVariantSO _variant; private EaterSignatureModule _sig; private EaterCounterplay _counter; private readonly EaterHost _host = new EaterHost();
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
        /// <summary>The surface under the Eater (set by the manager to <c>RoadGameManager.MonsterSurfaces</c>); null = no surface effects.</summary>
        public IMonsterSurfaceQuery Surfaces;
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
            DestroyModules(); _variant = v; _vTint = VariantTint(v); if (_maw != null) ApplyHue(_art != null && _art.eaterHeads != null && _art.eaterHeads.Length > 0);
            if (v != null) { _sig = EaterSignatures.Create(v.signature); _counter = EaterCounterplays.Create(v.defeat); }
        }

        private void DestroyModules() { if (_sig != null) _sig.Destroy(); if (_counter != null) _counter.Destroy(); _sig = null; _counter = null; }

        public void Begin(float startX, uint seed)
        {
            _startX = startX; _model.Reset(seed); _trimClock = 0f; _inGap = false; DefeatedVariant = null; _rescueCd = 0f;
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
            if (Surfaces != null && _model.Chasing) { var fx = Surfaces.MonsterAt(_startX + _model.S); _model.ApplySurface(fx.SlowMul, fx.EatMul, fx.StunS); }
            else if (_model.SurfaceSlowMul != 1f || _model.SurfaceEatMul != 1f) _model.ApplySurface(1f, 1f, 0f);
            _model.Tick(dt, carS, carSpeed, airborne, boosting);
            TickVariant(dt, carWorldX, carY, airborne, boosting);
            if (_model.Eating)
            {
                _trimClock += dt;
                if (_trimClock >= _d.trimTickSeconds) { _trimClock = 0f; Trim(); }
            }
            UpdateVisual(dt, carWorldX, carY);
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

        // ------------------------------------------------------------------ view (imported dragon art, sprites built once, nothing allocated per frame)
        // The art faces left; the car is to the right, so every renderer is flipped. Orders sit behind the car (5) and above the road/background.
        private const int FlameOrder = 0, SegOrder = 3, HeadOrder = 4, SegCount = 5;
        private const float HeadX = -1.2f, HeadY = 0.3f, SegFirstX = -3.6f, SegSpacing = 0.5f, SegY = -0.3f, SegScale = 0.78f, SegTaper = 0.1f, SegSegDark = 0.28f, HeadHalfW = 2.9f, EdgeButtonFrac = 0.12f, WaveAmp = 0.22f, WaveSpeed = 3.2f, WavePhase = 0.9f;
        private const float FlameX = -4.2f, FlameY = 0.9f, FlameFps = 12f, ChompSlowFps = 5f, ChompFastFps = 13f, SnapFps = 16f;
        private readonly SpriteRenderer[] _seg = new SpriteRenderer[SegCount]; private readonly float[] _segX = new float[SegCount];
        private SpriteRenderer _flame; private HazardArtSet _art; private Color _vTint = Color.white; private Material _hueMat;
        private const float ArtHueDeg = 14f, SatFollow = 0.8f, ValFollow = 0.75f;
        private float _animT, _flameT;

        private void EnsureView()
        {
            if (_maw != null) return;
            _maw = new GameObject("RoadEaterMaw").transform;
            _art = HazardArtSet.Load();
            bool art = _art != null && _art.eaterHeads != null && _art.eaterHeads.Length > 0;
            _body = SpriteFactory.CreateSprite("Head", _maw, art ? _art.eaterHeads[0] : SpriteFactory.Circle, Color.white, HeadOrder);
            _defaultMat = _body.sharedMaterial;
            _body.flipX = true; _body.transform.localPosition = new Vector3(HeadX, HeadY, 0f);
            if (!art) _body.transform.localScale = new Vector3(3.6f, 3.2f, 1f);                      // missing art set: plain blob so the chase is still readable
            bool seg = art && _art.eaterBodySegment != null;
            for (int i = 0; i < SegCount; i++)
            {
                _seg[i] = SpriteFactory.CreateSprite("Seg" + i, _maw, seg ? _art.eaterBodySegment : null, Color.white, SegOrder);
                _seg[i].flipX = true; _seg[i].enabled = seg; float k = SegScale - SegTaper * i; _seg[i].transform.localScale = new Vector3(k, k, 1f);
                _seg[i].sortingOrder = SegOrder - i;                                                 // nearer the head draws over the next one
                _segX[i] = i == 0 ? SegFirstX : _segX[i - 1] - SegSpacing * 3.1f * (k + (SegScale - SegTaper * (i - 1))) * 0.5f;
                _seg[i].transform.localPosition = new Vector3(_segX[i], SegY, 0f);
            }
            ApplyHue(art);
            bool fl = art && _art.eaterFlames != null && _art.eaterFlames.Length > 0;
            _flame = SpriteFactory.CreateSprite("Flame", _maw, fl ? _art.eaterFlames[0] : null, Color.white, FlameOrder);
            _flame.flipX = true; _flame.enabled = false; _flame.transform.localPosition = new Vector3(FlameX, FlameY, 0f);
        }

        /// <summary>Variant colour as a hue rotation (art hue to the variant's hue) with saturation and brightness following the tint only part of the way, so the painted detail stays. No shader = a gentle white-to-tint multiply.</summary>
        private void ApplyHue(bool art)
        {
            if (!art) return;
            bool shader = _art.hueShader != null;
            if (shader && _hueMat == null) _hueMat = new Material(_art.hueShader) { name = "EaterHue" };
            Material m = shader && _variant != null ? _hueMat : null;
            if (m != null)
            {
                Color.RGBToHSV(_variant.tint, out float h, out float sat, out float v);
                float d = Mathf.DeltaAngle(ArtHueDeg, h * 360f);
                float satS = _variant.satScale > 0f ? _variant.satScale : Mathf.Lerp(1f, Mathf.Max(0.35f, sat), SatFollow);
                float valS = _variant.valScale > 0f ? _variant.valScale : Mathf.Lerp(1f, Mathf.Max(0.5f, v), ValFollow);
                m.SetFloat("_Hue", d); m.SetFloat("_Sat", satS); m.SetFloat("_Val", valS);
                _vTint = Color.white;
            }
            if (_body != null) _body.sharedMaterial = m != null ? m : _defaultMat;
            for (int i = 0; i < SegCount; i++) if (_seg[i] != null) _seg[i].sharedMaterial = m != null ? m : _defaultMat;
        }

        private Material _defaultMat;

        private void SetVisible(bool on) { if (on) EnsureView(); if (_maw != null) _maw.gameObject.SetActive(on); }

        /// <summary>Variant hue as a subtle shift: the tint at full brightness, blended 25 percent into white so the orange art keeps its detail (a multiply would flatten it to a solid colour).</summary>
        private static Color VariantTint(EaterVariantSO v)
        {
            if (v == null) return Color.white;
            Color t = v.tint; float m = Mathf.Max(0.001f, Mathf.Max(t.r, Mathf.Max(t.g, t.b)));
            return Color.Lerp(Color.white, new Color(t.r / m, t.g / m, t.b / m, 1f), TintStrength);
        }
        private const float TintStrength = 0.25f;

        private void UpdateVisual(float dt, float carX, float carY)
        {
            if (_maw == null || !_maw.gameObject.activeSelf) return;
            float trueX = _startX + _model.S;
            float edge = _cam != null ? _cam.transform.position.x - _cam.orthographicSize * _cam.aspect * (1f - 2f * EdgeButtonFrac) + HeadHalfW - HeadX : trueX;
            float x = Mathf.Max(trueX, edge);                          // at the left screen edge while far, at its real place once close
            float y = _road.TryGetRoadYAtX(x, out float ry) ? ry : carY;
            _maw.position = _host.UseMawOverride ? new Vector3(_host.MawOverride.x, _host.MawOverride.y, 0f) : new Vector3(x, y + 1.3f, 0f);   // maw point: the mouth is just ahead of it
            bool shown = !_host.HideMaw; _body.enabled = shown;
            bool dragon = _art != null && _art.eaterHeads != null && _art.eaterHeads.Length > 0;
            bool snap = _model.State == EaterState.Snap, surge = _model.State == EaterState.Surge || snap;
            float pulse = snap ? 1f + 0.06f * Mathf.Sin(Time.time * 30f) : 1f + 0.02f * Mathf.Sin(Time.time * 6f);
            _maw.localScale = new Vector3(pulse, pulse, 1f);
            // chomp cycle: ping-pong over the head frames, faster the closer it is
            if (dragon)
            {
                int n = _art.eaterHeads.Length, cyc = n > 1 ? 2 * n - 2 : 1;
                float close = Mathf.Clamp01(1f - _model.Gap / Mathf.Max(1f, _d.bandCalm));
                _animT += dt * (snap ? SnapFps : Mathf.Lerp(ChompSlowFps, ChompFastFps, close));
                int c = (int)_animT % cyc; _body.sprite = _art.eaterHeads[c < n ? c : cyc - c];
                float t = Time.time * WaveSpeed;
                for (int i = 0; i < SegCount; i++)
                {
                    if (_seg[i].sprite == null) continue;
                    _seg[i].enabled = shown;
                    var lp = _seg[i].transform.localPosition; lp.y = SegY + WaveAmp * Mathf.Sin(t - i * WavePhase); _seg[i].transform.localPosition = lp;
                }
                bool fl = shown && surge && _art.eaterFlames != null && _art.eaterFlames.Length > 0;
                _flame.enabled = fl;
                if (fl) { _flameT += dt * FlameFps; _flame.sprite = _art.eaterFlames[(int)_flameT % _art.eaterFlames.Length]; _flame.color = new Color(1f, 0.7f, 0.2f, 0.9f); }
            }
            bool gone = _model.State == EaterState.Escape || _model.State == EaterState.Retreat;
            Color s = gone ? new Color(0.7f, 0.7f, 0.75f) : _model.Band == EaterBand.Snap ? new Color(1f, 0.82f, 0.8f) : _model.Band == EaterBand.Danger ? new Color(1f, 0.92f, 0.9f) : Color.white;
            Color c2 = dragon ? new Color(_vTint.r * s.r, _vTint.g * s.g, _vTint.b * s.b, 1f) : (_variant != null ? _variant.tint : new Color(0.22f, 0.1f, 0.28f));
            _body.color = c2;
            for (int i = 0; i < SegCount; i++) if (_seg[i] != null) { float d = 1f - SegSegDark * (i + 1) / SegCount; _seg[i].color = new Color(c2.r * d, c2.g * d, c2.b * d, 1f); }
        }

        public void Destroy() { if (_hueMat != null) UnityEngine.Object.Destroy(_hueMat); DestroyModules(); if (_maw != null) UnityEngine.Object.Destroy(_maw.gameObject); _maw = null; }
    }
}
