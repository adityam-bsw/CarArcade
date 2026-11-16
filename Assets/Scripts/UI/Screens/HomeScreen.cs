using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Home (hero screen of the Retro Arcade Workshop look): play, garage, worlds, settings, coins and best distance.</summary>
    public class HomeScreen : AnimatedScreen
    {
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Button worldsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button rewardsButton;
        [SerializeField] private GameObject rewardsDot;
        [SerializeField] private Button progressButton;
        [SerializeField] private GameObject progressDot;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private TMP_Text scrapText;
        [SerializeField] private Button dailyButton;
        [SerializeField] private TMP_Text dailyText;
        [Header("Exit (Android Back on Home)")]
        [SerializeField] private TMP_Text exitHint;
        [Header("Entrance")]
        [SerializeField] private RectTransform titlePlate;
        [SerializeField] private RectTransform[] popIns;

        private readonly List<Tween> _tweens = new List<Tween>();
        private int _lastCoins = -1;

        // Back on Home: the first press shows "press back again", a second one within two seconds closes the app.
        private const float ExitWindow = 2f;
        private float _lastBackAt = -10f;
        private Tween _hintTween;

        public override bool BackCloses => true;

        public override void OnBackPressed()
        {
            if (Time.unscaledTime - _lastBackAt <= ExitWindow) { GameServices.Actions?.QuitApp(); return; }
            _lastBackAt = Time.unscaledTime;
            if (exitHint == null) return;
            _hintTween?.Kill();
            exitHint.alpha = 1f;
            exitHint.transform.parent.gameObject.SetActive(true);
            _hintTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .AppendInterval(ExitWindow - 0.3f).Append(exitHint.DOFade(0f, 0.3f))
                .OnComplete(() => exitHint.transform.parent.gameObject.SetActive(false));
        }

        private void Awake()
        {
            playButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            garageButton.onClick.AddListener(() => GameServices.Actions?.OpenGarage());
            worldsButton.onClick.AddListener(() => GameServices.Actions?.OpenWorlds());
            settingsButton.onClick.AddListener(() => Stack?.Push<SettingsScreen>());
            if (dailyButton != null) dailyButton.onClick.AddListener(OnDailyPressed);
            if (rewardsButton != null) rewardsButton.onClick.AddListener(() => GameServices.Actions?.OpenRewards());
            if (progressButton != null) progressButton.onClick.AddListener(() => Stack?.Push<ProgressHubScreen>(HubPage.Journey));
        }

        public override void OnShow(object args)
        {
            _lastCoins = -1;
            _lastBackAt = -10f;
            if (exitHint != null) exitHint.transform.parent.gameObject.SetActive(false);
            GameServices.Model.Changed += Refresh;
            Refresh();
            PlayEntrance();
        }

        public override void OnHide()
        {
            GameServices.Model.Changed -= Refresh;
            _hintTween?.Kill();
            KillTweens();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= Refresh;
            KillTweens();
        }

        // The plate drops on its chains, the controls pop in one after another. Rest poses are restored on kill.
        private void PlayEntrance()
        {
            KillTweens();
            if (titlePlate != null)
            {
                var rest = titlePlate.anchoredPosition;
                titlePlate.anchoredPosition = rest + new Vector2(0f, 220f);
                _tweens.Add(titlePlate.DOAnchorPos(rest, 0.45f).SetEase(Ease.OutBack).SetUpdate(true));
            }
            if (popIns == null) return;
            for (int i = 0; i < popIns.Length; i++)
            {
                var rt = popIns[i];
                if (rt == null) continue;
                var rest = rt.localScale;
                rt.localScale = rest * 0.6f;
                _tweens.Add(rt.DOScale(rest, 0.35f).SetEase(Ease.OutBack).SetDelay(0.12f + i * 0.07f).SetUpdate(true));
            }
        }

        public enum DailyAction { Start, Claim }

        /// <summary>The Home Daily Challenge button starts today's daily directly; only a finished, unclaimed daily claims its reward first.</summary>
        public static DailyAction DailyActionFor(bool claimable) { return claimable ? DailyAction.Claim : DailyAction.Start; }

        private void OnDailyPressed()
        {
            if (DailyActionFor(GameServices.Model.DailyChallengeClaimable) == DailyAction.Claim) GameServices.Actions?.ClaimDailyChallenge();
            else GameServices.Actions?.StartDaily();
        }

        private void KillTweens()
        {
            foreach (var t in _tweens) if (t != null && t.IsActive()) t.Complete();
            _tweens.Clear();
        }

        private void Refresh()
        {
            var m = GameServices.Model;
            coinsText.SetText("{0}", m.Coins);
            if (_lastCoins >= 0 && m.Coins != _lastCoins) UIAnim.Punch(coinsText.transform.parent, 0.1f);
            _lastCoins = m.Coins;
            bestText.SetText("BEST {0} m", Mathf.RoundToInt(m.BestDistance));
            if (rewardsDot != null) rewardsDot.SetActive(m.RewardsAvailable);
            if (progressDot != null) progressDot.SetActive(m.HubWaiting > 0);
            if (gemsText != null) gemsText.SetText("{0}", m.Gems);
            if (scrapText != null) scrapText.SetText("SCRAP {0}", m.Scrap);
            if (dailyButton != null) dailyButton.gameObject.SetActive(m.DailyChallengeOn);
            if (dailyText != null) dailyText.SetText(m.DailyChallengeInfo);
        }
    }
}
