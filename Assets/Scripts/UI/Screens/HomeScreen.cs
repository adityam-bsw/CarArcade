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
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private TMP_Text scrapText;
        [SerializeField] private Button dailyButton;
        [SerializeField] private TMP_Text dailyText;
        [Header("Entrance")]
        [SerializeField] private RectTransform titlePlate;
        [SerializeField] private RectTransform[] popIns;

        private readonly List<Tween> _tweens = new List<Tween>();
        private int _lastCoins = -1;

        public override bool BackCloses => false;

        private void Awake()
        {
            playButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            garageButton.onClick.AddListener(() => GameServices.Actions?.OpenGarage());
            worldsButton.onClick.AddListener(() => GameServices.Actions?.OpenWorlds());
            settingsButton.onClick.AddListener(() => Stack?.Push<SettingsScreen>());
            if (dailyButton != null) dailyButton.onClick.AddListener(OnDailyPressed);
            if (rewardsButton != null) rewardsButton.onClick.AddListener(() => GameServices.Actions?.OpenRewards());
        }

        public override void OnShow(object args)
        {
            _lastCoins = -1;
            GameServices.Model.Changed += Refresh;
            Refresh();
            PlayEntrance();
        }

        public override void OnHide()
        {
            GameServices.Model.Changed -= Refresh;
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

        private void OnDailyPressed()
        {
            var m = GameServices.Model;
            if (m.DailyChallengeClaimable) GameServices.Actions?.ClaimDailyChallenge();
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
            if (gemsText != null) gemsText.SetText("{0}", m.Gems);
            if (scrapText != null) scrapText.SetText("SCRAP {0}", m.Scrap);
            if (dailyButton != null) dailyButton.gameObject.SetActive(m.DailyChallengeOn);
            if (dailyText != null) dailyText.SetText(m.DailyChallengeInfo);
        }
    }
}
