using System;
using DG.Tweening;
using UnityEngine;
using Pose = RoadBuilderGame.RoB0Data.Pose;
using D = RoadBuilderGame.RoB0Data;

namespace RoadBuilderGame
{
    /// <summary>
    /// Part-based RO-B0 rig on sprite swaps and DOTween (no skeletal licence): one body renderer that swaps between six poses, a white flash overlay, shield bubble, jet flame flicker and pooled fx.
    /// Hierarchy (one owner per transform): root (boss position, facing flip) / fall (enter and defeat tweens) / spin (defeat) / sway (bob, lean, breathing) / kick (recoil punch) / shake (hurt shake) / art.
    /// Tweens are created once and restarted; <see cref="Tick"/> allocates nothing. Holds no rules: <see cref="BossController"/> feeds it state and events.
    /// </summary>
    public sealed class RoB0Rig
    {
        private readonly Transform _root, _fall, _spin, _sway, _kick, _shake, _muzzle, _shieldT;
        private readonly SpriteRenderer _body, _flash, _shield, _flameL, _flameR;
        private readonly Transform _bodyT, _flameLT, _flameRT;
        private readonly Sprite[] _pose = new Sprite[D.PoseCount];
        private readonly Material _flashMat; private readonly RoB0Fx _fx;
        private readonly Tweener _tRecoil, _tShake, _tEnterMove, _tEnterScale; private readonly Sequence _tPop, _tDefeat;
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static Sprite _flameSprite;

        private float _t, _ph, _face = 1f, _flashAmt, _boost, _hurtLeft, _fireLeft, _phaseLeft, _blinkLeft, _blinkIn, _sparkIn, _puffIn, _lastMeter, _hurtCool, _smokeIn;
        private bool _damaged, _shieldOn, _popping, _defeated, _telling;
        private Pose _cur = (Pose)(-1); private uint _rs = 12345u;

        /// <summary>Camera shake request (amplitude 0..1).</summary>
        public Action<float> Shake;
        public bool IsDefeated => _defeated;
        public Vector2 MuzzleWorld => _muzzle.position;

        public RoB0Rig(Transform root, HazardArtSet a, RoB0Fx fx)
        {
            _root = root; _fx = fx;
            _pose[(int)Pose.Idle] = a.rob0Idle; _pose[(int)Pose.Blink] = a.rob0EyeOpen; _pose[(int)Pose.Angry] = a.rob0EyeAngry;
            _pose[(int)Pose.Firing] = a.rob0Firing; _pose[(int)Pose.Hurt] = a.rob0Hurt; _pose[(int)Pose.Damaged] = a.rob0Damaged;
            _fall = Node("Fall", _root); _spin = Node("Spin", _fall); _sway = Node("Sway", _spin); _kick = Node("Kick", _sway); _shake = Node("Shake", _kick);
            _muzzle = Node("Muzzle", _shake);
            _flameL = SpriteFactory.CreateSprite("FlameL", _shake, Flame(), D.FlameColor, D.OrderFlameTop); _flameR = SpriteFactory.CreateSprite("FlameR", _shake, Flame(), D.FlameColor, D.OrderFlameTop);
            _flameLT = _flameL.transform; _flameRT = _flameR.transform; _flameLT.localPosition = D.JetL; _flameRT.localPosition = D.JetR;
            _body = SpriteFactory.CreateSprite("Body", _shake, _pose[0], Color.white, D.OrderBody); _bodyT = _body.transform;
            _flash = SpriteFactory.CreateSprite("Flash", _shake, _pose[0], Color.white, D.OrderFlash); _flash.transform.SetParent(_bodyT, false);
            if (a.flashShader != null) { _flashMat = new Material(a.flashShader) { name = "RoB0Flash" }; _flash.sharedMaterial = _flashMat; }
            _flash.enabled = false;
            _shield = SpriteFactory.CreateSprite("Shield", _sway, a.rob0Shield, Color.white, D.OrderShield); _shieldT = _shield.transform;
            _shieldT.localPosition = D.ShieldCentre; _shieldT.localScale = Vector3.one * D.ShieldScale; _shield.enabled = false;
            _ph = ((_rs >> 4) & 0xFF) * 0.0245f;

            // tweens: created once, restarted (SetAutoKill(false)). Each animates one property of one transform.
            _tRecoil = _kick.DOPunchPosition(D.Recoil, D.RecoilSeconds, 8, 0.6f).SetAutoKill(false).Pause();
            _tShake = _shake.DOShakePosition(D.ShakeSeconds, 0.3f, 26, 90f, false, true).SetAutoKill(false).Pause();
            _tEnterMove = _fall.DOLocalMove(Vector3.zero, D.EnterSeconds).SetEase(Ease.OutCubic).SetAutoKill(false).Pause();
            _tEnterScale = _fall.DOScale(Vector3.one, D.EnterSeconds).SetEase(Ease.OutBack).SetAutoKill(false).Pause();
            _tPop = DOTween.Sequence().Append(_shieldT.DOScale(D.ShieldPopScale, D.ShieldPopSeconds).SetEase(Ease.OutCubic)).Join(_shield.DOFade(0f, D.ShieldPopSeconds))
                .OnComplete(OnPopDone).SetAutoKill(false).Pause();
            _tDefeat = DOTween.Sequence()
                .Append(_spin.DORotate(new Vector3(0f, 0f, 780f), D.DefeatSeconds, RotateMode.FastBeyond360).SetEase(Ease.InQuad))
                .Join(_fall.DOLocalMoveY(-9f, D.DefeatSeconds).SetEase(Ease.InQuad))
                .Join(_fall.DOScale(0.7f, D.DefeatSeconds))
                .InsertCallback(D.DefeatSeconds * 0.4f, ExplodeSmall).InsertCallback(D.DefeatSeconds * 0.75f, ExplodeSmall).AppendCallback(ExplodeBig)
                .SetAutoKill(false).Pause();
        }

        private static Transform Node(string n, Transform parent) { var t = new GameObject(n).transform; t.SetParent(parent, false); return t; }

        private static Sprite Flame()
        {
            if (_flameSprite != null) return _flameSprite;
            const int w = 32, h = 64; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "RoB0Flame" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float v = y / (float)(h - 1);                                   // 0 = tip (bottom), 1 = root (top)
                float half = Mathf.Sin(Mathf.Clamp01(v) * 1.5708f) * w * 0.5f * 0.95f;
                float a = Mathf.Clamp01(half - Mathf.Abs(x - (w - 1) * 0.5f) + 0.5f) * Mathf.Clamp01(1.1f - v * 0.35f) * Mathf.Clamp01(v * 8f);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            _flameSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 64f);      // pivot at the root (top) so it hangs below the nozzle
            return _flameSprite;
        }

        // ------------------------------------------------------------------ lifecycle
        /// <summary>The boss appears: fly in from the right with ease and scale in.</summary>
        public void Enter()
        {
            HardReset(); _fall.gameObject.SetActive(true); _shieldOn = true; _shield.enabled = true;
            _fall.localPosition = D.EnterFrom; _fall.localScale = Vector3.one * 0.35f;       // the tweens start from here (start value is read on first play)
            _tEnterMove.Restart(); _tEnterScale.Restart();
            _blinkIn = Mathf.Lerp(D.BlinkMin, D.BlinkMax, Rand()); _sparkIn = Mathf.Lerp(D.SparkMin, D.SparkMax, Rand());
        }

        /// <summary>Back to a neutral hidden state (new run, stop).</summary>
        public void HardReset()
        {
            _defeated = false;                                                              // guards the defeat callbacks while rewinding
            _tRecoil.Rewind(); _tShake.Rewind(); _tEnterMove.Pause(); _tEnterScale.Pause(); _tPop.Pause(); _tDefeat.Rewind(); _tDefeat.Pause();
            _fall.localPosition = Vector3.zero; _fall.localScale = Vector3.one; _spin.localRotation = Quaternion.identity; _kick.localPosition = Vector3.zero; _shake.localPosition = Vector3.zero;
            _shieldT.localScale = Vector3.one * D.ShieldScale; _shield.enabled = false;
            _hurtLeft = _fireLeft = _phaseLeft = _blinkLeft = _flashAmt = _boost = _hurtCool = 0f; _lastMeter = 0f;
            _damaged = _popping = _shieldOn = _telling = false; _cur = (Pose)(-1); _fx.Clear();
        }

        public void Hide() { HardReset(); _fall.gameObject.SetActive(false); }

        // ------------------------------------------------------------------ events
        public void Fire()
        {
            if (_defeated) return;
            _fireLeft = D.FireSeconds; _boost = 1f; _tRecoil.Restart(); ApplyPose(Resolve());
            Vector2 m = _shake.TransformPoint(D.FlashPoint);
            _fx.Emit(m, Vector2.zero, 0.14f, 0.5f, 1.5f, new Color(1f, 0.95f, 0.7f, 0.9f));
            _fx.Burst(m, 6, 6f, 0.3f, 0.2f, 0.05f, new Color(0.6f, 0.9f, 1f, 0.9f), 0f, ref _rs);
        }

        public void PhaseTelegraph() { if (_defeated) return; _phaseLeft = D.PhaseAngrySeconds; _boost = 1f; _tShake.Restart(); }

        public void Hurt()
        {
            if (_defeated) return;
            _hurtLeft = D.HurtSeconds; _flashAmt = 0.85f; _hurtCool = D.HurtCooldown; _tShake.Restart(); Shake?.Invoke(0.12f);
        }

        public void Defeat()
        {
            if (_defeated) return;
            _defeated = true; _tEnterMove.Complete(); _tEnterScale.Complete(); _hurtLeft = D.HurtSeconds * 3f; _flashAmt = 1f; _tShake.Restart();
            PopShield(); _tDefeat.Restart();
        }

        private void PopShield()
        {
            if (!_shieldOn) return;
            _shieldOn = false; _popping = true; _shieldT.localScale = Vector3.one * D.ShieldScale;
            var c = _shield.color; c.a = D.ShieldAlpha; _shield.color = c; _shield.enabled = true; _tPop.Restart();
            _fx.Burst(_shieldT.position, 14, 5f, 0.45f, 0.3f, 0.05f, new Color(0.7f, 0.92f, 1f, 0.9f), 0f, ref _rs);
            Shake?.Invoke(0.2f);
        }

        private void OnPopDone() { _popping = false; _shield.enabled = false; }

        private void ExplodeSmall()
        {
            if (!_defeated) return;
            Vector2 p = _body.transform.position; _flashAmt = 0.8f; Shake?.Invoke(0.3f);
            _fx.Burst(p, 8, 5f, 0.5f, 0.5f, 1.4f, new Color(1f, 0.6f, 0.15f, 0.9f), 0f, ref _rs);
            _fx.Burst(p, 6, 3f, 0.9f, 0.6f, 1.6f, new Color(0.25f, 0.25f, 0.3f, 0.7f), -1.5f, ref _rs);
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p, new Color(1f, 0.8f, 0.3f), 10);
        }

        private void ExplodeBig()
        {
            if (!_defeated) return;
            Vector2 p = _body.transform.position; Shake?.Invoke(0.6f);
            _fx.Emit(p, Vector2.zero, 0.25f, 1f, 4.5f, new Color(1f, 0.95f, 0.7f, 0.95f));
            _fx.Burst(p, 14, 7f, 0.7f, 0.8f, 2f, new Color(1f, 0.5f, 0.1f, 0.9f), 0f, ref _rs);
            _fx.Burst(p, 10, 4f, 1.1f, 0.8f, 2.2f, new Color(0.2f, 0.2f, 0.25f, 0.75f), -1.2f, ref _rs);
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(p, new Color(1f, 0.85f, 0.3f), 20);
            _fall.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ tick
        public void Tick(float dt, Vector2 vel, float posX, float carX, bool telling, float meter01, int phase)
        {
            _t += dt; _telling = telling;
            if (_hurtLeft > 0f) _hurtLeft -= dt;
            if (_fireLeft > 0f) _fireLeft -= dt;
            if (_phaseLeft > 0f) _phaseLeft -= dt;
            if (_hurtCool > 0f) _hurtCool -= dt;
            if (_blinkLeft > 0f) _blinkLeft -= dt;
            else if ((_blinkIn -= dt) <= 0f) { _blinkLeft = D.BlinkHold; _blinkIn = Mathf.Lerp(D.BlinkMin, D.BlinkMax, Rand()); }
            _flashAmt = Mathf.MoveTowards(_flashAmt, 0f, 3.5f * dt); _boost = Mathf.MoveTowards(_boost, 0f, 3f * dt);

            // state from the model: damaged at 66 percent, shield down from phase 3, hurt when the counter meter jumps
            if (!_defeated)
            {
                if (!_damaged && (meter01 >= D.DamagedAt01 || phase >= 3)) _damaged = true;
                if (_shieldOn && phase >= 3) PopShield();
                if (meter01 - _lastMeter >= D.HurtMinGain01 && _hurtCool <= 0f && _hurtLeft <= 0f) Hurt();
            }
            _lastMeter = meter01;

            // facing: art faces left; the flip squashes through zero so the turn reads as a quick spin
            float want = carX > posX + D.FaceDeadZone ? -1f : 1f;
            _face = Mathf.MoveTowards(_face, want, D.FlipSpeed * dt);
            _root.localScale = new Vector3(_face * D.RigScale, D.RigScale, 1f);

            // hover bob, sway, lean and breathing (code driven: continuous and cheap)
            float bob = Mathf.Sin(6.2832f * D.BobHz * _t + _ph) * D.BobAmplitude;
            float sway = Mathf.Sin(6.2832f * 0.37f * _t + _ph * 1.7f) * D.SwayDeg;
            float lean = Mathf.Clamp(-vel.x * D.LeanDegPerMps, -D.LeanMaxDeg, D.LeanMaxDeg) * (_face >= 0f ? 1f : -1f);
            float br = Mathf.Sin(6.2832f * D.BreatheHz * _t) * D.BreatheAmount;
            _sway.localPosition = new Vector3(0f, bob, 0f); _sway.localRotation = Quaternion.Euler(0f, 0f, sway + lean); _sway.localScale = new Vector3(1f + br, 1f - br, 1f);

            ApplyPose(Resolve());

            // flash overlay
            if (_flashMat != null) { _flash.enabled = _flashAmt > 0.01f; if (_flash.enabled) _flashMat.SetFloat(FlashId, _flashAmt); }
            else _body.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.55f), _flashAmt);

            // thruster flicker and gentle trail
            float f = 0.85f + 0.2f * Mathf.Sin(_t * 47f) + 0.12f * Mathf.Sin(_t * 31f + 1f) + _boost * D.FlameBoost;
            _flameLT.localScale = new Vector3(D.FlameW * (0.9f + 0.1f * Mathf.Sin(_t * 53f)), D.FlameH * f, 1f);
            _flameRT.localScale = new Vector3(D.FlameW * (0.9f + 0.1f * Mathf.Sin(_t * 41f + 2f)), D.FlameH * (f * 0.95f + 0.05f), 1f);
            if ((_puffIn -= dt) <= 0f && !_defeated)
            {
                _puffIn = 0.07f;
                _fx.Emit(_flameLT.position + Vector3.down * 0.5f, new Vector2(-1.4f, -1.2f), 0.5f, 0.26f, 0.05f, new Color(0.75f, 0.92f, 1f, 0.35f), 0f, D.OrderFlame);
                _fx.Emit(_flameRT.position + Vector3.down * 0.5f, new Vector2(-1.4f, -1.2f), 0.5f, 0.26f, 0.05f, new Color(0.75f, 0.92f, 1f, 0.35f), 0f, D.OrderFlame);
            }

            // shield pulse
            if (_shieldOn)
            {
                var sc = _damaged ? D.ShieldPhase2 : D.ShieldPhase1; sc.a = D.ShieldAlpha + D.ShieldPulse * Mathf.Sin(_t * 4.2f);
                _shield.color = sc; _shieldT.localScale = Vector3.one * (D.ShieldScale * (1f + 0.03f * Mathf.Sin(_t * 3.1f)));
            }

            // damage: sparks and a thin smoke
            if (_damaged && !_defeated)
            {
                if ((_sparkIn -= dt) <= 0f)
                {
                    _sparkIn = Mathf.Lerp(D.SparkMin, D.SparkMax, Rand());
                    Vector2 sp = _shake.TransformPoint(D.SparkPoint);
                    _fx.Burst(sp, 5, 4f, 0.35f, 0.22f, 0.04f, new Color(1f, 0.9f, 0.4f, 1f), 6f, ref _rs);
                    _flashAmt = Mathf.Max(_flashAmt, 0.25f);
                }
                if ((_smokeIn -= dt) <= 0f) { _smokeIn = 0.22f; _fx.Emit(_shake.TransformPoint(D.SparkPoint), new Vector2(0.5f, 1.4f), 0.9f, 0.25f, 0.9f, new Color(0.25f, 0.25f, 0.3f, 0.5f), 0f); }
            }
            else if (_defeated && (_smokeIn -= dt) <= 0f) { _smokeIn = 0.06f; _fx.Emit(_body.transform.position, new Vector2(0.4f, 1.2f), 1f, 0.4f, 1.3f, new Color(0.2f, 0.2f, 0.25f, 0.7f), 0f); }
            _fx.Tick(dt);
        }

        private Pose Resolve()
        {
            if (_hurtLeft > 0f) return Pose.Hurt;
            if (_fireLeft > 0f) return Pose.Firing;
            if (_telling || _phaseLeft > 0f) return _damaged ? Pose.Damaged : Pose.Angry;
            if (_damaged) return Pose.Damaged;
            return _blinkLeft > 0f ? Pose.Blink : Pose.Idle;
        }

        private void ApplyPose(Pose p)
        {
            if (p == _cur) return;
            _cur = p; int i = (int)p; var s = _pose[i] != null ? _pose[i] : _pose[0];
            _body.sprite = s; _flash.sprite = s;
            _bodyT.localPosition = D.PoseOffset[i]; _bodyT.localScale = Vector3.one * D.PoseScale[i];
            _muzzle.localPosition = D.PoseMuzzle[i];
        }

        private float Rand() { _rs = _rs * 1664525u + 1013904223u; return ((_rs >> 8) & 0xFFFF) / 65535f; }

        public void Destroy()
        {
            _tRecoil.Kill(); _tShake.Kill(); _tEnterMove.Kill(); _tEnterScale.Kill(); _tPop.Kill(); _tDefeat.Kill();
            if (_flashMat != null) UnityEngine.Object.Destroy(_flashMat);
            _fx.Destroy();
        }
    }
}
