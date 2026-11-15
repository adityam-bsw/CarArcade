using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>In-run HUD: distance, speed, coins, lives, ink and the boost button. Never covers the drawing area.</summary>
    public class HudScreen : AnimatedScreen
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Image[] hearts;
        [SerializeField] private RectTransform inkBar;
        [SerializeField] private RectTransform inkFill;
        [SerializeField] private TMP_Text inkLabel;
        [SerializeField] private Button boostButton;
        [SerializeField] private RectTransform needle;
        [Header("Boost, combo, power-ups")]
        [SerializeField] private RectTransform boostMeter;
        [SerializeField] private TMP_Text boostCostText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private float gaugeMaxKmh = 80f;
        [Header("Stage event warning (S1 banner, S2 sign, S3 edge flash)")]
        [SerializeField] private TMP_Text stageBanner;
        [SerializeField] private RectTransform stageSign;
        [SerializeField] private TMP_Text stageSignName;
        [SerializeField] private TMP_Text stageSignMetres;
        [SerializeField] private Image edgeFlashLeft;
        [SerializeField] private Image edgeFlashRight;
        [Header("Pause")]
        [SerializeField] private Button pauseButton;

        private const float LowInk = 0.2f;

        private int _distance = -1, _speed = -1, _coins = -1, _lives = -1, _inkPercent = -1;
        private bool _lowPulse;
        private int _boostPercent = -1, _boostCost = -2, _multiplier = -1, _combo = -1, _gems = -1, _toastId;
        private bool _boostReady, _boostReadyKnown;
        private string _power;
        private Tween _toastTween;
        private int _bannerId, _flashId, _signMetres = -1;
        private bool _signShown, _signActive;
        private string _signName;
        private Tween _bannerTween, _flashTween;

        // Android Back during a run opens the pause popup instead of leaving.
        public override bool BackCloses => true;
        public override void OnBackPressed() { GameServices.Actions?.PauseRun(); }

        private void Awake()
        {
            boostButton.onClick.AddListener(() => GameServices.Actions?.Boost());
            var hazardHud = Resources.Load<HazardHudView>("UI/HazardHud"); if (hazardHud != null) Instantiate(hazardHud, transform, false);      // boss meter, vignette and announcer subtitle (gap list B)
            if (pauseButton != null) pauseButton.onClick.AddListener(() => GameServices.Actions?.PauseRun());
        }

        public override void OnShow(object args)
        {
            _distance = _speed = _coins = _lives = _inkPercent = -1;
            _boostPercent = -1; _boostCost = -2; _multiplier = _combo = _gems = -1; _boostReadyKnown = false; _power = null;
            _toastId = GameServices.Model.ToastId;
            _bannerId = GameServices.Model.StageBannerId; _flashId = GameServices.Model.StageFlashId;
            _signShown = false; _signActive = false; _signMetres = -1; _signName = null;
            if (stageBanner != null) stageBanner.alpha = 0f;
            if (stageSign != null) stageSign.gameObject.SetActive(false);
            SetFlashAlpha(0f);
            if (toastText != null) toastText.alpha = 0f;
            if (_lowPulse) { UIAnim.StopPulse(inkBar); _lowPulse = false; }
            GameServices.Model.HudChanged += Refresh;
            GameServices.Model.PausedChanged += OnPausedChanged;
            Refresh();
        }

        public override void OnHide()
        {
            KillToast(); KillStageTweens();
            GameServices.Model.HudChanged -= Refresh;
            GameServices.Model.PausedChanged -= OnPausedChanged;
            if (_lowPulse) { UIAnim.StopPulse(inkBar); _lowPulse = false; }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KillToast(); KillStageTweens();
            GameServices.Model.HudChanged -= Refresh;
            GameServices.Model.PausedChanged -= OnPausedChanged;
        }

        // The warning banner, edge flash and toast are unscaled tweens, so a pause would let them run out unseen. Hold them while paused:
        // a pause during a warning never shortens what the player gets to read (stage brief F0).
        private void OnPausedChanged()
        {
            bool paused = GameServices.Model.Paused;
            SetTweenPaused(_bannerTween, paused); SetTweenPaused(_flashTween, paused); SetTweenPaused(_toastTween, paused);
        }

        private static void SetTweenPaused(Tween t, bool paused)
        {
            if (t == null || !t.IsActive()) return;
            if (paused) t.Pause(); else t.Play();
        }

        // Runs every frame while playing, so it only touches a label when its value actually changed.
        private void Refresh()
        {
            var m = GameServices.Model;
            var theme = UIThemeManager.Current;

            int d = Mathf.FloorToInt(m.Distance);
            if (d != _distance) { _distance = d; distanceText.SetText("{0} m", d); }

            int s = Mathf.RoundToInt(m.SpeedKmh);
            if (s != _speed) { _speed = s; speedText.SetText("{0}", s); }
            // The needle sweeps 240 degrees: -120 at rest to +120 at full scale.
            if (needle != null) needle.localEulerAngles = new Vector3(0f, 0f, 120f - 240f * Mathf.Clamp01(m.SpeedKmh / gaugeMaxKmh));

            int c = m.Coins + m.SessionCoins;
            if (c != _coins)
            {
                bool gained = _coins >= 0 && c > _coins;
                _coins = c;
                coinsText.SetText("{0}", c);
                if (gained) UIAnim.Punch(coinsText.transform.parent, 0.12f);
            }

            if (m.Lives != _lives)
            {
                int previous = _lives;
                _lives = m.Lives;
                for (int i = 0; i < hearts.Length; i++)
                {
                    bool full = i < m.Lives;
                    hearts[i].sprite = theme.GetSprite(full ? SpriteSlot.HeartOn : SpriteSlot.HeartOff);
                    if (previous >= 0 && full != (i < previous)) UIAnim.Punch(hearts[i].transform, 0.4f);
                }
            }

            int ink = Mathf.RoundToInt(m.Ink01 * 100f);
            if (ink != _inkPercent)
            {
                _inkPercent = ink;
                inkFill.anchorMax = new Vector2(Mathf.Max(0.02f, m.Ink01), 1f);
                inkLabel.SetText("INK {0}%", ink);

                bool low = m.Ink01 < LowInk;
                if (low && !_lowPulse) { _lowPulse = true; UIAnim.StartPulse(inkBar, 1.05f); }
                else if (!low && _lowPulse) { _lowPulse = false; UIAnim.StopPulse(inkBar); }
            }

            RefreshExtras(m, theme);
            RefreshStage(m, theme);
        }

        private void KillStageTweens()
        {
            if (_bannerTween != null && _bannerTween.IsActive()) _bannerTween.Kill(); _bannerTween = null;
            if (_flashTween != null && _flashTween.IsActive()) _flashTween.Kill(); _flashTween = null;
        }

        private static Color TokenColor(UITheme theme, string token, ColorToken fallback)
        {
            return theme.GetColor(System.Enum.TryParse(token, out ColorToken t) ? t : fallback);
        }

        private void SetFlashAlpha(float a)
        {
            if (edgeFlashLeft != null) { var c = edgeFlashLeft.color; c.a = a; edgeFlashLeft.color = c; }
            if (edgeFlashRight != null) { var c = edgeFlashRight.color; c.a = a; edgeFlashRight.color = c; }
        }

        // The three warning signals. Each part reacts to a changed id or value only, so a quiet frame touches nothing.
        private void RefreshStage(GameModel m, UITheme theme)
        {
            if (stageBanner != null && m.StageBannerId != _bannerId)
            {
                _bannerId = m.StageBannerId;
                if (_bannerTween != null && _bannerTween.IsActive()) _bannerTween.Kill();
                stageBanner.SetText(m.StageBanner);
                stageBanner.color = TokenColor(theme, m.StageBannerTint, ColorToken.Accent);
                stageBanner.alpha = 1f;
                stageBanner.transform.localScale = Vector3.one * 0.7f;
                var seq = DOTween.Sequence().SetUpdate(true);          // scaled-time independent: a warning never shortens under slow-mo
                seq.Append(stageBanner.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
                seq.AppendInterval(1.1f);
                seq.Append(stageBanner.DOFade(0f, 0.3f));
                _bannerTween = seq;
            }

            if (stageSign != null)
            {
                if (m.StageSignOn != _signShown)
                {
                    _signShown = m.StageSignOn;
                    stageSign.gameObject.SetActive(_signShown);
                    if (_signShown)
                    {
                        var img = stageSign.GetComponent<Image>();
                        if (img != null) { var c = TokenColor(theme, m.StageSignTint, ColorToken.Accent); c.a = 0.92f; img.color = c; }
                        UIAnim.Punch(stageSign, 0.2f);
                        _signName = null; _signMetres = -1;
                    }
                }
                if (_signShown)
                {
                    if (m.StageSignName != _signName || m.StageSignActive != _signActive)
                    {
                        _signName = m.StageSignName; _signActive = m.StageSignActive;
                        stageSignName.SetText(_signName);
                        _signMetres = -1;
                    }
                    if (m.StageSignMetres != _signMetres) { _signMetres = m.StageSignMetres; stageSignMetres.SetText(_signActive ? "{0} M LEFT" : "IN {0} M", _signMetres); }
                }
            }

            if (m.StageFlashId != _flashId)
            {
                _flashId = m.StageFlashId;
                if (_flashTween != null && _flashTween.IsActive()) _flashTween.Kill();
                var col = TokenColor(theme, m.StageFlashTint, ColorToken.Danger);
                if (edgeFlashLeft != null) edgeFlashLeft.color = col;
                if (edgeFlashRight != null) edgeFlashRight.color = col;
                SetFlashAlpha(0.55f);
                _flashTween = DOVirtual.Float(0.55f, 0f, 1f, SetFlashAlpha).SetUpdate(true);       // S3: a 1 s edge flash, not a full-screen flash
            }
        }

        private void KillToast() { if (_toastTween != null && _toastTween.IsActive()) _toastTween.Kill(); _toastTween = null; }

        // Boost meter and price, combo, power-up timers, gems and the event toast. Each part only touches the UI when its value changed.
        private void RefreshExtras(GameModel m, UITheme theme)
        {
            int bp = Mathf.RoundToInt(m.Boost01 * 100f);
            if (bp != _boostPercent && boostMeter != null)
            {
                _boostPercent = bp;
                boostMeter.anchorMax = new Vector2(Mathf.Max(0.001f, m.Boost01), 1f);
            }
            if (boostCostText != null && m.BoostCost != _boostCost)
            {
                _boostCost = m.BoostCost;
                if (_boostCost == 0) boostCostText.SetText("FREE"); else boostCostText.SetText("{0} COINS", _boostCost);
            }
            if (!_boostReadyKnown || m.BoostReady != _boostReady)
            {
                _boostReadyKnown = true; _boostReady = m.BoostReady;
                if (boostButton.image != null) boostButton.image.sprite = theme.GetSprite(_boostReady ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonArcadeDisabled);
            }

            if (comboText != null && (m.Multiplier != _multiplier || m.Combo != _combo))
            {
                bool grew = m.Multiplier > _multiplier && _multiplier >= 0;
                _multiplier = m.Multiplier; _combo = m.Combo;
                if (m.Combo >= 2) comboText.SetText("x{0}  COMBO {1}", m.Multiplier, m.Combo); else comboText.SetText(string.Empty);
                if (grew) UIAnim.Punch(comboText.transform, 0.3f);
            }
            if (powerText != null && m.PowerText != _power) { _power = m.PowerText; powerText.SetText(_power); }
            if (gemsText != null && m.Gems + m.SessionGems != _gems) { _gems = m.Gems + m.SessionGems; gemsText.SetText("{0}", _gems); }

            if (toastText != null && m.ToastId != _toastId)
            {
                _toastId = m.ToastId;
                KillToast();
                toastText.SetText(m.Toast);
                toastText.alpha = 1f;
                toastText.transform.localScale = Vector3.one * 0.7f;
                var seq = DOTween.Sequence().SetUpdate(true);
                seq.Append(toastText.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack));
                seq.AppendInterval(0.9f);
                seq.Append(toastText.DOFade(0f, 0.35f));
                _toastTween = seq;
            }
        }
    }
}
