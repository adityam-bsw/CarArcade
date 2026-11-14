using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Home: play, garage, worlds, settings, coins and best distance.</summary>
    public class HomeScreen : AnimatedScreen
    {
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Button worldsButton;
        [SerializeField] private Button settingsButton;

        public override bool BackCloses => false;

        private void Awake()
        {
            playButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            garageButton.onClick.AddListener(() => GameServices.Actions?.OpenGarage());
            worldsButton.onClick.AddListener(() => GameServices.Actions?.OpenWorlds());
            settingsButton.onClick.AddListener(() => Stack?.Push<SettingsScreen>());
        }

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
            coinsText.SetText("{0}", m.Coins);
            bestText.SetText("BEST {0} m", Mathf.RoundToInt(m.BestDistance));
        }
    }
}
