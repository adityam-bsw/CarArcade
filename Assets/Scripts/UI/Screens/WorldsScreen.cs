using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// World select, built like the vehicle select: the garage door shows the live world with the showcase car on the chosen
    /// road, one card rail along the floor, and two tabs on the left wall that switch the rail between worlds and roads.
    /// A tap equips at once; RACE starts a run. Views only.
    /// </summary>
    public class WorldsScreen : AnimatedScreen
    {
        [SerializeField] private CardRail rail;
        [SerializeField] private Button worldTab;
        [SerializeField] private Button roadTab;
        [SerializeField] private Image worldTabImage;
        [SerializeField] private Image roadTabImage;
        [SerializeField] private TMP_Text worldTabLabel;
        [SerializeField] private TMP_Text roadTabLabel;
        [SerializeField] private Button raceButton;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button backButton;

        private CatalogKind _tab = CatalogKind.World;

        private void Awake()
        {
            worldTab.onClick.AddListener(() => SetTab(CatalogKind.World));
            roadTab.onClick.AddListener(() => SetTab(CatalogKind.Surface));
            raceButton.onClick.AddListener(() => GameServices.Actions?.StartRace());
            backButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
        }

        public override void OnBackPressed() { GameServices.Actions?.GoHome(); }

        public override void OnShow(object args)
        {
            GameServices.Model.Changed += Refresh;
            _tab = CatalogKind.World;
            hintText.SetText(string.Empty);
            rail.Populate(_tab, OnTapped);
            Refresh();
            rail.Reveal(GameServices.Model.SelectedId(_tab));
        }

        public override void OnHide() { GameServices.Model.Changed -= Refresh; }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= Refresh;
        }

        private void SetTab(CatalogKind kind)
        {
            if (kind == _tab) return;
            _tab = kind;
            hintText.SetText(string.Empty);
            if (kind == CatalogKind.Surface) ShowTraits(AssetCatalog.Load()?.Find(GameServices.Model.SelectedSurfaceId) ?? new CatalogEntry { displayName = "Road" });
            rail.Populate(_tab, OnTapped);
            Refresh();
            rail.Reveal(GameServices.Model.SelectedId(_tab));
        }

        private void OnTapped(CatalogEntry e)
        {
            if (e.playable) { ShowTraits(e); GameServices.Actions?.SelectCatalogItem(_tab, e.id); return; }
            // Not playable yet: say why instead of silently doing nothing.
            hintText.SetText(e.displayName + ": " + (string.IsNullOrEmpty(e.playNote) ? "not playable yet." : e.playNote));
            hintText.color = UIThemeManager.Current.GetColor(ColorToken.Danger);
            UIAnim.Punch(hintText.transform, 0.06f);
        }

        /// <summary>Roads change the run, so say how: grip, speed, ink cost and coin value of the tapped track.</summary>
        private void ShowTraits(CatalogEntry e)
        {
            if (e.kind != CatalogKind.Surface) { hintText.SetText(string.Empty); return; }
            hintText.SetText(e.displayName + ": " + e.TraitsLine());
            hintText.color = UIThemeManager.Current.GetColor(ColorToken.TextPrimary);
        }

        private void Refresh()
        {
            var m = GameServices.Model;
            var theme = UIThemeManager.Current;
            var cat = AssetCatalog.Load();
            string world = cat?.Find(m.SelectedWorldId)?.displayName ?? "";
            string road = cat?.Find(m.SelectedSurfaceId)?.displayName ?? "";

            rail.Refresh(m.SelectedId(_tab), null);

            bool worlds = _tab == CatalogKind.World;
            worldTabImage.sprite = theme.GetSprite(worlds ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonPlate);
            roadTabImage.sprite = theme.GetSprite(worlds ? SpriteSlot.ButtonPlate : SpriteSlot.ButtonArcade);
            worldTabLabel.SetText("WORLD\n<size=62%>" + world + "</size>");
            roadTabLabel.SetText("ROAD\n<size=62%>" + road + "</size>");
            var onAccent = theme.GetColor(ColorToken.TextOnAccent);
            var onSteel = theme.GetColor(ColorToken.TextPrimary);
            worldTabLabel.color = worlds ? onAccent : onSteel;
            roadTabLabel.color = worlds ? onSteel : onAccent;

            summaryText.SetText(world + " on " + road);
            if (hintText.text.Length == 0) hintText.color = theme.GetColor(ColorToken.TextPrimary);
        }
    }
}
