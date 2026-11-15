using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Garage: swipe through every vehicle, equip or unlock one, and upgrade engine, suspension and ink tank.</summary>
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
        [SerializeField] private Button backButton;
        [SerializeField] private CardRail rail;
        [SerializeField] private UpgradeRow[] rows = new UpgradeRow[4];

        private int _lastCoins = -1;
        private CatalogEntry _focus;
        private bool _actionable;

        private void Awake()
        {
            actionButton.onClick.AddListener(OnActionPressed);
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
            var m = GameServices.Model;
            _focus = AssetCatalog.Load()?.Find(m.SelectedVehicleId);
            rail.Populate(CatalogKind.Vehicle, OnCardTapped);
            GameServices.Model.Changed += Refresh;
            Refresh();
            rail.Reveal(m.SelectedVehicleId);
        }

        public override void OnHide() { GameServices.Model.Changed -= Refresh; }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= Refresh;
        }

        private void OnCardTapped(CatalogEntry e)
        {
            _focus = e;
            var m = GameServices.Model;
            // Owned and playable: equip at once. Locked: only look, and the button offers the unlock.
            if (e.playable && m.IsVehicleUnlocked(e)) GameServices.Actions?.SelectCatalogItem(CatalogKind.Vehicle, e.id);
            else Refresh();
        }

        private void OnActionPressed()
        {
            if (_focus != null && _actionable) GameServices.Actions?.SelectCatalogItem(CatalogKind.Vehicle, _focus.id);
        }

        private void Refresh()
        {
            var theme = UIThemeManager.Current;
            var m = GameServices.Model;

            coinsText.SetText("{0}", m.Coins);
            if (_lastCoins >= 0 && m.Coins != _lastCoins) UIAnim.Punch(coinsText.transform.parent, 0.1f);
            _lastCoins = m.Coins;

            rail.Refresh(m.SelectedVehicleId, _focus);

            if (_focus == null)
            {
                carNameText.SetText(string.Empty);
                actionLabel.SetText("PICK A VEHICLE");
                _actionable = false;
                actionImage.sprite = theme.GetSprite(SpriteSlot.ButtonArcadeDisabled);
            }
            else
            {
                carNameText.SetText(_focus.displayName.ToUpperInvariant());
                bool equipped = m.SelectedVehicleId == _focus.id;
                bool enabled = true; string label; ColorToken colour;
                if (!_focus.playable) { label = "COMING SOON"; colour = ColorToken.SurfaceAlt; enabled = false; }
                else if (equipped) { label = "EQUIPPED"; colour = ColorToken.Success; enabled = false; }
                else if (!m.IsVehicleUnlocked(_focus))
                {
                    int missing = _focus.cost - m.Coins;
                    if (missing > 0) { label = "NEED " + missing + " MORE"; colour = ColorToken.SurfaceAlt; enabled = false; }
                    else { label = "UNLOCK  " + _focus.cost; colour = ColorToken.Accent; }
                }
                else { label = "EQUIP"; colour = ColorToken.Primary; }
                actionLabel.SetText(label);
                // Actionable: the orange arcade button. Equipped: the steel plate with its teal line. Blocked: disabled arcade.
                actionImage.sprite = theme.GetSprite(enabled ? SpriteSlot.ButtonArcade : (equipped ? SpriteSlot.ButtonPlate : SpriteSlot.ButtonArcadeDisabled));
                _actionable = enabled;
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
                r.buttonImage.sprite = theme.GetSprite(!maxed && m.Coins >= cost ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonArcadeDisabled);

                for (int p = 0; p < r.pips.Length; p++)
                    r.pips[p].sprite = theme.GetSprite(p < level ? SpriteSlot.PipOn : SpriteSlot.PipOff);

                if (r.shownLevel >= 0 && level != r.shownLevel) UIAnim.Punch(r.pips[Mathf.Clamp(level - 1, 0, r.pips.Length - 1)].transform, 0.5f);
                r.shownLevel = level;
            }
        }
    }
}
