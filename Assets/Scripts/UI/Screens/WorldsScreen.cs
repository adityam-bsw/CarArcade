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
        [Tooltip("Dark banner behind the hint so it reads against the road; shown only while the hint has text.")]
        [SerializeField] private GameObject hintBanner;
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
            SetHint(string.Empty, ColorToken.TextPrimary);
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
            SetHint(string.Empty, ColorToken.TextPrimary);
            if (kind == CatalogKind.Surface) ShowTraits(AssetCatalog.Load()?.Find(GameServices.Model.SelectedSurfaceId) ?? new CatalogEntry { displayName = "Road" });
            rail.Populate(_tab, OnTapped);
            Refresh();
            rail.Reveal(GameServices.Model.SelectedId(_tab));
        }

        private void OnTapped(CatalogEntry e)
        {
            if (e.playable)
            {
                ShowTraits(e);
                int noticeBefore = GameServices.Model.NoticeId;
                GameServices.Actions?.SelectCatalogItem(_tab, e.id);
                string gate = GameServices.Model.LockLine(e.id);
                if (GameServices.Model.NoticeId != noticeBefore)
                {
                    SetHint(GameServices.Model.Notice, ColorToken.Danger);      // the manager refused the pick and says what is needed
                    rail.ShakeLocked(e.id);
                }
                else if (gate.Length > 0)
                {
                    SetHint(e.displayName + " is locked: " + gate, ColorToken.Danger);
                    rail.ShakeLocked(e.id);
                }
                return;
            }
            // Not playable yet: say why instead of silently doing nothing.
            SetHint(e.displayName + ": " + (string.IsNullOrEmpty(e.playNote) ? "not playable yet." : e.playNote), ColorToken.Danger);
            rail.ShakeLocked(e.id);
        }

        /// <summary>The message line sits on a dark banner so it keeps its contrast over the road in the door.</summary>
        private void SetHint(string text, ColorToken colour)
        {
            bool has = text.Length > 0;
            if (hintBanner != null)
            {
                // Activate first: enabling the label re-applies its theme style, which would overwrite the colour set below.
                bool wasShown = hintBanner.activeSelf;
                hintBanner.SetActive(has);
                if (has) UIAnim.Punch(hintBanner.transform, wasShown ? 0.05f : 0.1f);
            }
            hintText.SetText(text);
            hintText.color = UIThemeManager.Current.GetColor(colour);
        }

        /// <summary>Roads change the run, so say how: grip, speed, ink cost and coin value of the tapped track.</summary>
        private void ShowTraits(CatalogEntry e)
        {
            if (e.kind != CatalogKind.Surface) { SetHint(string.Empty, ColorToken.TextPrimary); return; }
            SetHint(e.displayName + ": " + e.TraitsLine(), ColorToken.TextPrimary);
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
            if (hintText.text.Length == 0) SetHint(string.Empty, ColorToken.TextPrimary);
        }
    }
}
