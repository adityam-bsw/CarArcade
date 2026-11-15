using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>End of run: reason, distance count-up, new-best celebration, coins, retry as the primary action.</summary>
    public class ResultScreen : AnimatedScreen
    {
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private GameObject newBestBadge;
        [Tooltip("Patch of blank paper over the medal printed on the certificate art. Shown unless the run is a new best.")]
        [SerializeField] private GameObject medalCover;
        [SerializeField] private TMP_Text coinsWonText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [Header("Optional rewarded offers (never mid-run)")]
        [SerializeField] private Button doubleButton;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button extraDailyButton;
        [SerializeField] private TMP_Text doubleLabel;
        [SerializeField] private TMP_Text extraDailyLabel;

        private void Awake()
        {
            retryButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            menuButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
            if (doubleButton != null) doubleButton.onClick.AddListener(() => GameServices.Actions?.WatchOffer(AdPlacement.DoubleCoins));
            if (reviveButton != null) reviveButton.onClick.AddListener(() => GameServices.Actions?.WatchOffer(AdPlacement.Revive));
            if (extraDailyButton != null) extraDailyButton.onClick.AddListener(() => GameServices.Actions?.WatchOffer(AdPlacement.ExtraDailyReward));
        }

        public override void OnBackPressed() { GameServices.Actions?.GoHome(); }

        public override void OnHide() { GameServices.Model.Changed -= RefreshOffers; }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= RefreshOffers;
        }

        private void RefreshOffers()
        {
            var m = GameServices.Model;
            var r = m.LastRun;
            totalText.SetText("TOTAL {0}", r.totalCoins);
            coinsWonText.SetText("+{0}", r.coinsWon);
            if (doubleButton != null) doubleButton.gameObject.SetActive(m.OfferDouble);
            if (reviveButton != null) reviveButton.gameObject.SetActive(m.OfferRevive);
            if (extraDailyButton != null) extraDailyButton.gameObject.SetActive(m.OfferExtraDaily);
            // Short first line so it fits its plate; the second line keeps the offer clearly marked as an optional ad.
            if (doubleLabel != null) doubleLabel.SetText("2X COINS (" + m.OfferDoubleLeft + " LEFT)\n<size=62%>OPTIONAL AD</size>");
            if (extraDailyLabel != null) extraDailyLabel.SetText("EXTRA DAILY\n<size=62%>OPTIONAL AD</size>");
        }

        public override void OnShow(object args)
        {
            GameServices.Model.Changed += RefreshOffers;
            RefreshOffers();
            var r = GameServices.Model.LastRun;
            reasonText.SetText(r.reason ?? string.Empty);
            totalText.SetText("TOTAL {0}", r.totalCoins);
            coinsWonText.SetText("+{0}", r.coinsWon);
            if (bonusText != null) bonusText.SetText(string.IsNullOrEmpty(r.bonusLines) ? "NO BONUSES THIS RUN" : r.bonusLines);
            if (gemsText != null) gemsText.SetText(r.gems > 0 ? "+{0} GEM" + (r.gems > 1 ? "S" : "") : string.Empty, r.gems);

            int metres = Mathf.FloorToInt(r.distance);
            UIAnim.CountUp(distanceText, 0, metres, Mathf.Clamp(metres / 120f, 0.5f, 1.4f), "{0} m");

            if (medalCover != null) medalCover.SetActive(!r.newBest);
            newBestBadge.SetActive(r.newBest);
            if (r.newBest)
            {
                newBestBadge.transform.localScale = Vector3.one;
                UIAnim.Punch(newBestBadge.transform, 0.35f);
            }
        }
    }
}
