using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Daily reward and the day's missions. Shows data from the model and sends claim taps to the game.</summary>
    public class RewardsScreen : AnimatedScreen
    {
        [System.Serializable]
        public class MissionRow
        {
            public GameObject root;
            public TMP_Text text;
            public TMP_Text progress;
            public TMP_Text reward;
            public Button claim;
            public TMP_Text claimLabel;
            public Image claimImage;
        }

        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private TMP_Text dailyTitle;
        [SerializeField] private TMP_Text dailyReward;
        [SerializeField] private Button dailyButton;
        [SerializeField] private TMP_Text dailyButtonLabel;
        [SerializeField] private Image dailyButtonImage;
        [SerializeField] private MissionRow[] missions = new MissionRow[3];
        [SerializeField] private Button backButton;

        private void Awake()
        {
            backButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
            dailyButton.onClick.AddListener(() => GameServices.Actions?.ClaimDaily());
            for (int i = 0; i < missions.Length; i++)
            {
                int index = i;
                missions[i].claim.onClick.AddListener(() => GameServices.Actions?.ClaimMission(index));
            }
        }

        public override void OnBackPressed() { GameServices.Actions?.GoHome(); }

        public override void OnShow(object args)
        {
            GameServices.Model.Changed += Refresh;
            Refresh();
        }

        public override void OnHide() { GameServices.Model.Changed -= Refresh; }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= Refresh;
        }

        private void Refresh()
        {
            var m = GameServices.Model;
            var theme = UIThemeManager.Current;
            coinsText.SetText("{0}", m.Coins);
            gemsText.SetText("{0}", m.Gems);

            dailyTitle.SetText("DAY {0} OF 7", m.DailyDay);
            dailyReward.SetText(m.DailyGems > 0 ? "+{0} COINS  +" + m.DailyGems + " GEMS" : "+{0} COINS", m.DailyCoins);
            dailyButtonLabel.SetText(m.DailyAvailable ? "CLAIM" : "COME BACK TOMORROW");
            dailyButtonImage.sprite = theme.GetSprite(m.DailyAvailable ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonArcadeDisabled);

            for (int i = 0; i < missions.Length; i++)
            {
                var row = missions[i];
                bool has = i < m.Missions.Length;
                row.root.SetActive(has);
                if (!has) continue;
                var v = m.Missions[i];
                row.text.SetText(v.text);
                row.progress.SetText("{0} / {1}", v.progress, v.target);
                row.reward.SetText(v.rewardGems > 0 ? "+" + v.rewardCoins + "  +" + v.rewardGems + " GEM" : "+" + v.rewardCoins);
                bool claimable = v.Complete && !v.claimed;
                row.claimLabel.SetText(v.claimed ? "DONE" : (v.Complete ? "CLAIM" : "..."));
                row.claimImage.sprite = theme.GetSprite(claimable ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonArcadeDisabled);
            }
        }
    }
}
