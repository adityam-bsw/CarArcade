using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Garage: pick or unlock a vehicle and upgrade engine, suspension and ink tank.</summary>
    public class GarageScreen : AnimatedScreen
    {
        [Serializable]
        public class UpgradeRow
        {
            public TMP_Text title;
            public TMP_Text hint;
            public TMP_Text cost;
            public Button button;
            public Image buttonImage;
            public Image[] pips;
            [NonSerialized] public int shownLevel = -1;
        }

        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text carNameText;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private Image actionImage;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button backButton;
        [SerializeField] private UpgradeRow[] rows = new UpgradeRow[3];

        private int _lastCoins = -1;

        private void Awake()
        {
            actionButton.onClick.AddListener(() => GameServices.Actions?.SelectOrUnlockVehicle());
            prevButton.onClick.AddListener(() => GameServices.Actions?.CycleVehicle(-1));
            nextButton.onClick.AddListener(() => GameServices.Actions?.CycleVehicle(1));
            backButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
            for (int i = 0; i < rows.Length; i++)
            {
                int stat = i;
                rows[i].button.onClick.AddListener(() => GameServices.Actions?.UpgradeStat(stat));
            }
        }

        public override void OnBackPressed() { GameServices.Actions?.GoHome(); }

        public override void OnShow(object args)
        {
            _lastCoins = -1;
            foreach (var r in rows) r.shownLevel = -1;
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
            var theme = UIThemeManager.Current;
            var m = GameServices.Model;

            coinsText.SetText("{0}", m.Coins);
            if (_lastCoins >= 0 && m.Coins != _lastCoins) UIAnim.Punch(coinsText.transform.parent, 0.1f);
            _lastCoins = m.Coins;

            carNameText.SetText(GameCatalog.VehicleName(m.Vehicle));

            bool unlocked = m.IsUnlocked(m.Vehicle);
            int vehicleCost = GameCatalog.VehicleCost(m.Vehicle);
            if (unlocked)
            {
                actionLabel.SetText("EQUIPPED");
                actionImage.color = theme.GetColor(ColorToken.Success);
            }
            else
            {
                actionLabel.SetText("UNLOCK  {0}", vehicleCost);
                actionImage.color = theme.GetColor(m.Coins >= vehicleCost ? ColorToken.Accent : ColorToken.SurfaceAlt);
            }

            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                int level = m.LevelOf(i);
                r.title.SetText(GameCatalog.UpgradeName(i));
                r.hint.SetText(GameCatalog.UpgradeHint(i));

                bool maxed = level >= GameCatalog.MaxUpgradeLevel;
                int cost = GameCatalog.UpgradeCost(level);
                if (maxed) r.cost.SetText("MAX"); else r.cost.SetText("UPGRADE  {0}", cost);
                r.buttonImage.color = theme.GetColor(maxed ? ColorToken.SurfaceAlt : (m.Coins >= cost ? ColorToken.Accent : ColorToken.SurfaceAlt));

                for (int p = 0; p < r.pips.Length; p++)
                    r.pips[p].color = theme.GetColor(p < level ? ColorToken.Accent : ColorToken.SurfaceAlt);

                if (r.shownLevel >= 0 && level != r.shownLevel) UIAnim.Punch(r.pips[Mathf.Clamp(level - 1, 0, r.pips.Length - 1)].transform, 0.5f);
                r.shownLevel = level;
            }
        }
    }
}
