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
            public GameObject costIcon;
            public TMP_Text buttonLabel;
            public Button button;
            public Image buttonImage;
            public Image[] pips;
            [NonSerialized] public int shownLevel = -1;
        }

        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text scrapText;
        [SerializeField] private TMP_Text carNameText;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private Image actionImage;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button masteryButton;
        [SerializeField] private TMP_Text profileText;
        [SerializeField] private CardRail rail;
        [SerializeField] private UpgradeRow[] rows = new UpgradeRow[5];

        private int _lastCoins = -1;
        private CatalogEntry _focus;
        private bool _actionable;

        private void Awake()
        {
            actionButton.onClick.AddListener(OnActionPressed);
            backButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
            if (masteryButton != null) masteryButton.onClick.AddListener(() => Stack?.Push<ProgressHubScreen>(HubPage.Mastery));
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

            if (scrapText != null) scrapText.SetText("SCRAP {0}", m.Scrap);
            rail.Refresh(m.SelectedVehicleId, _focus);

            if (profileText != null) profileText.SetText(_focus != null ? m.VehicleLine(_focus.id) : string.Empty);
            if (_focus == null)
            {
                carNameText.SetText(string.Empty);
                actionLabel.SetText("PICK A VEHICLE");
                _actionable = false;
                actionButton.interactable = false;
                actionImage.sprite = theme.GetSprite(SpriteSlot.ButtonArcadeDisabled);
                actionLabel.color = theme.GetColor(ColorToken.TextPrimary);
            }
            else
            {
                carNameText.SetText(_focus.displayName.ToUpperInvariant());
                bool equipped = m.SelectedVehicleId == _focus.id;
                bool enabled = true; string label;
                if (!_focus.playable) { label = "COMING SOON"; enabled = false; }
                else if (equipped) { label = "EQUIPPED"; enabled = false; }
                else if (!m.IsVehicleUnlocked(_focus) && m.LockLine(_focus.id).Length > 0) { label = m.LockLine(_focus.id); enabled = false; }
                else if (!m.IsVehicleUnlocked(_focus))
                {
                    int missing = _focus.cost - m.Coins;
                    if (missing > 0) { label = "NEED " + missing + " MORE"; enabled = false; }
                    else label = "UNLOCK  " + _focus.cost;
                }
                else label = "EQUIP";
                actionLabel.SetText(label);
                // Actionable: the orange arcade button. Equipped: the steel plate with its teal line. Blocked: steel (disabled arcade).
                // The button has no sprite swap of its own (a swap would paint the orange normal sprite back over a disabled look),
                // so the sprite set here is what shows, and the button is only interactable when the tap does something.
                actionImage.sprite = theme.GetSprite(enabled ? SpriteSlot.ButtonArcade : (equipped ? SpriteSlot.ButtonPlate : SpriteSlot.ButtonArcadeDisabled));
                actionLabel.color = theme.GetColor(enabled ? ColorToken.TextOnAccent : ColorToken.TextPrimary);
                _actionable = enabled;
                actionButton.interactable = enabled;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                int level = m.LevelOf(i);
                r.title.SetText(GameCatalog.UpgradeName(i));
                r.hint.SetText(GameCatalog.UpgradeHint(i));

                bool maxed = level >= GameCatalog.MaxUpgradeLevel;
                int cost = i < m.UpgradeCosts.Length ? m.UpgradeCosts[i] : GameCatalog.UpgradeCost(level);
                bool affordable = !maxed && m.Coins >= cost;
                if (maxed) r.cost.SetText(string.Empty); else r.cost.SetText("{0}", cost);
                if (r.costIcon != null) r.costIcon.SetActive(!maxed);
                r.cost.color = theme.GetColor(affordable || maxed ? ColorToken.Coin : ColorToken.Danger);
                r.buttonLabel.SetText(maxed ? "MAX" : "UPGRADE");
                r.buttonLabel.color = theme.GetColor(affordable ? ColorToken.TextOnAccent : ColorToken.TextPrimary);
                r.button.interactable = affordable;
                r.buttonImage.sprite = theme.GetSprite(affordable ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonArcadeDisabled);

                for (int p = 0; p < r.pips.Length; p++)
                    r.pips[p].sprite = theme.GetSprite(p < level ? SpriteSlot.PipOn : SpriteSlot.PipOff);

                if (r.shownLevel >= 0 && level != r.shownLevel) UIAnim.Punch(r.pips[Mathf.Clamp(level - 1, 0, r.pips.Length - 1)].transform, 0.5f);
                r.shownLevel = level;
            }
        }
    }
}
