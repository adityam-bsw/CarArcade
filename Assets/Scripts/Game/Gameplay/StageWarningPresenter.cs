using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// The three-signal warning for a stage event (stage-events.md section 4): S1 banner and announcer cue 5 s ahead, S2 sign and sting 3 s ahead,
    /// S3 edge flash and cue 1 s ahead. Writes <see cref="GameModel"/> (the HUD displays it) and asks the <see cref="AudioDirector"/> for the cues.
    /// Holds no rules: the director decides when, this decides what the player sees and hears. Every warning has a word or shape and a sound, never colour alone.
    /// </summary>
    public sealed class StageWarningPresenter
    {
        private readonly StageRunController _run;
        private readonly AudioDirector _audio;
        private readonly GameplayTuning _tuning;
        private StageEventState _tracked;
        private int _lastMetres = -1;
        // Catch-up spacing (stage-events.md section 4): signals that arrive together after a jump or respawn play at least signalMinGapS apart, in order.
        private float _clock, _lastSlotAt = -100f;
        private StageEventState _pendSign, _pendCue;
        private float _pendSignAt, _pendCueAt;

        public StageWarningPresenter(StageRunController run, AudioDirector audio, GameplayTuning tuning)
        {
            _run = run; _audio = audio; _tuning = tuning;
            run.Signal += OnSignal;
            run.EventStarted += OnStarted;
            run.EventEnded += OnEnded;
        }

        /// <summary>Clear everything shown (run start, run end, jump).</summary>
        public void Reset()
        {
            _tracked = null; _lastMetres = -1; _pendSign = _pendCue = null; _lastSlotAt = -100f;
            var m = GameServices.Model;
            m.StageSignOn = false; m.StageSignActive = false; m.StageSignMetres = 0; m.StageBanner = ""; m.StageSignName = "";
        }

        /// <summary>Per frame: keep the sign's metre count current (an int change only, no allocation).</summary>
        public void Tick(float distanceM, float scaledDt)
        {
            _clock += scaledDt;
            if (_pendSign != null && _clock >= _pendSignAt) { var e = _pendSign; _pendSign = null; ShowSign(e); }
            if (_pendCue != null && _clock >= _pendCueAt) { var e = _pendCue; _pendCue = null; ShowCue(e); }
            if (_tracked == null) return;
            var m = GameServices.Model;
            float target = _tracked.Phase == StagePhase.Active ? _tracked.EndM : _tracked.StartM;
            int metres = Mathf.Max(0, Mathf.CeilToInt(target - distanceM));
            if (metres != _lastMetres) { _lastMetres = metres; m.StageSignMetres = metres; }
        }

        private void OnSignal(StageEventState e, StageSignal s)
        {
            switch (s)
            {
                case StageSignal.Announcer:
                    ShowBanner(e);
                    _lastSlotAt = _clock;
                    break;
                case StageSignal.Sign:
                {
                    float at = Mathf.Max(_clock, _lastSlotAt + e.Definition.signalMinGapS);
                    _lastSlotAt = at;
                    if (at <= _clock + 0.0001f) ShowSign(e); else { _pendSign = e; _pendSignAt = at; }
                    break;
                }
                case StageSignal.Cue:
                {
                    float at = Mathf.Max(_clock, _lastSlotAt + e.Definition.signalMinGapS);
                    _lastSlotAt = at;
                    if (at <= _clock + 0.0001f) ShowCue(e); else { _pendCue = e; _pendCueAt = at; }
                    break;
                }
            }
        }

        private void ShowBanner(StageEventState e)
        {
            var d = e.Definition; var m = GameServices.Model;
            m.StageBanner = StageCues.BannerText(d.bannerKey);
            m.StageBannerTint = d.tintColorToken ?? "";
            m.StageBannerId++;
            _audio?.StageCue(d.announcerCueId);
        }

        private void ShowSign(StageEventState e)
        {
            var d = e.Definition; var m = GameServices.Model;
            _tracked = e; _lastMetres = -1;
            m.StageSignName = StageCues.SignText(d.bannerKey);
            m.StageSignTint = d.tintColorToken ?? "";
            m.StageSignActive = false; m.StageSignOn = true;
            _audio?.StageCue(d.warnSfxId);
        }

        private void ShowCue(StageEventState e)
        {
            var d = e.Definition; var m = GameServices.Model;
            m.StageFlashTint = d.tintColorToken ?? "";
            m.StageFlashId++;
            _audio?.StageCue(d.cueSfxId);
            if (_tuning != null && _tuning.stageHaptics) Haptic();
        }

        private void OnStarted(StageEventState e)
        {
            var m = GameServices.Model;
            if (_tracked != e)
            {   // started without a sign (debug jump, or a very short lead): show the sign now
                _tracked = e;
                m.StageSignName = StageCues.SignText(e.Definition.bannerKey);
                m.StageSignTint = e.Definition.tintColorToken ?? "";
            }
            m.StageSignActive = true; m.StageSignOn = true; _lastMetres = -1;
        }

        private void OnEnded(StageEventState e)
        {
            if (_tracked == e) { _tracked = null; var m = GameServices.Model; m.StageSignOn = false; m.StageSignActive = false; }
        }

        private static void Haptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
