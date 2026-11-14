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
        [SerializeField] private TMP_Text coinsWonText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            retryButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            menuButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
        }

        public override void OnBackPressed() { GameServices.Actions?.GoHome(); }

        public override void OnShow(object args)
        {
            var r = GameServices.Model.LastRun;
            reasonText.SetText(r.reason ?? string.Empty);
            totalText.SetText("TOTAL {0}", r.totalCoins);
            coinsWonText.SetText("+{0}", r.coinsWon);

            int metres = Mathf.FloorToInt(r.distance);
            UIAnim.CountUp(distanceText, 0, metres, Mathf.Clamp(metres / 120f, 0.5f, 1.4f), "{0} m");

            newBestBadge.SetActive(r.newBest);
            if (r.newBest)
            {
                newBestBadge.transform.localScale = Vector3.one;
                UIAnim.Punch(newBestBadge.transform, 0.35f);
            }
        }
    }
}
