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
            if (bonusText != null) { bonusText.overflowMode = TextOverflowModes.Truncate; bonusText.enableAutoSizing = true; bonusText.fontSizeMax = bonusText.fontSize; bonusText.fontSizeMin = 14f; }
            retryButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            menuButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
            if (doubleButton != null) doubleButton.onClick.AddListener(() => GameServices.Actions?.WatchOffer(AdPlacement.DoubleCoins));
            if (reviveButton != null) reviveButton.onClick.AddListener(() => GameServices.Actions?.WatchOffer(AdPlacement.Revive));
            if (extraDailyButton != null) extraDailyButton.onClick.AddListener(() => GameServices.Actions?.WatchOffer(AdPlacement.ExtraDailyReward));
        }

        private const int MaxBonusLines = 6;
        private static readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(256);
        private static readonly System.Collections.Generic.List<string> _keys = new System.Collections.Generic.List<string>(16);
        private static readonly System.Collections.Generic.List<int> _counts = new System.Collections.Generic.List<int>(16);
        private static readonly System.Collections.Generic.List<int> _sums = new System.Collections.Generic.List<int>(16);

        /// <summary>Groups "CHEST 250 m  +12" style lines by their label so a long run reads as a handful of rows ("CHEST x9  +108") that fit the panel.</summary>
        internal static string CondenseBonuses(string raw)
        {
            _keys.Clear(); _counts.Clear(); _sums.Clear(); _sb.Length = 0;
            foreach (var line in raw.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                int coins = 0; string head = line;
                int plus = line.LastIndexOf('+');
                if (plus >= 0) { int.TryParse(line.Substring(plus + 1).Trim(), out coins); head = line.Substring(0, plus); }
                int cut = head.Length;
                for (int i = 0; i < head.Length; i++) if (char.IsDigit(head[i])) { cut = i; break; }
                string key = head.Substring(0, cut).Trim();
                if (key.Length == 0) key = head.Trim();
                int idx = _keys.IndexOf(key);
                if (idx < 0) { _keys.Add(key); _counts.Add(0); _sums.Add(0); idx = _keys.Count - 1; }
                _counts[idx]++; _sums[idx] += coins;
            }
            int shown = Mathf.Min(_keys.Count, MaxBonusLines);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0) _sb.Append('\n');
                _sb.Append(_keys[i]);
                if (_counts[i] > 1) _sb.Append(" x").Append(_counts[i]);
                if (_sums[i] > 0) _sb.Append("  +").Append(_sums[i]);
            }
            if (_keys.Count > shown) _sb.Append("\n+").Append(_keys.Count - shown).Append(" MORE");
            return _sb.ToString();
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
            if (bonusText != null) bonusText.SetText(string.IsNullOrEmpty(r.bonusLines) ? "NO BONUSES THIS RUN" : CondenseBonuses(r.bonusLines));
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
