using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Browses everything in the AssetCatalog of one kind (vehicles, surfaces or worlds): group tabs, a scrolling grid,
    /// and a detail panel with a Select button. Selecting stores the choice in the profile only; the track and the
    /// vehicle rigs do not read it yet, and the screen says so. Pushed on the ScreenStack with a CatalogKind as args.
    /// </summary>
    public class CollectionScreen : AnimatedScreen
    {
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Button backButton;

        [Header("Browse")]
        [SerializeField] private RectTransform tabsRoot;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private CollectionTab tabPrefab;
        [SerializeField] private CollectionCard cardPrefab;

        [Header("Detail")]
        [SerializeField] private Image detailPreview;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailGroup;
        [SerializeField] private TMP_Text detailStatus;
        [SerializeField] private TMP_Text detailNote;
        [SerializeField] private TMP_Text footnote;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image selectButtonImage;
        [SerializeField] private TMP_Text selectLabel;

        private CatalogKind _kind;
        private bool _selectEnabled;
        private string _group;                       // null = all groups
        private CatalogEntry _current;
        private AssetCatalog _catalog;
        private readonly List<CollectionCard> _cards = new List<CollectionCard>();
        private readonly List<CollectionTab> _tabs = new List<CollectionTab>();

        private void Awake()
        {
            backButton.onClick.AddListener(() => Stack?.Pop());
            selectButton.onClick.AddListener(OnSelectPressed);
        }

        public override void OnShow(object args)
        {
            _kind = args is CatalogKind k ? k : CatalogKind.Vehicle;
            _catalog = AssetCatalog.Load();
            _group = null;
            _current = null;

            titleText.SetText(_kind == CatalogKind.Vehicle ? "VEHICLES" : (_kind == CatalogKind.Surface ? "SURFACES" : "WORLDS"));
            BuildTabs();
            RebuildGrid();
            GameServices.Model.Changed += RefreshSelectionState;
        }

        public override void OnHide() { GameServices.Model.Changed -= RefreshSelectionState; }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= RefreshSelectionState;
        }

        // ---------------------------------------------------------------- tabs and grid
        private void BuildTabs()
        {
            Clear(tabsRoot);
            _tabs.Clear();
            if (_catalog == null) return;

            AddTab(null, "ALL");
            foreach (var g in _catalog.Groups(_kind)) AddTab(g, g.ToUpperInvariant());
            UpdateTabHighlight();
        }

        private void AddTab(string group, string display)
        {
            var tab = Instantiate(tabPrefab, tabsRoot);
            tab.Bind(group, display, group == _group, OnTabPressed);
            _tabs.Add(tab);
        }

        private void OnTabPressed(CollectionTab tab)
        {
            _group = tab.Group;
            UpdateTabHighlight();
            RebuildGrid();
        }

        private void UpdateTabHighlight()
        {
            foreach (var t in _tabs) t.SetActive(t.Group == _group);
        }

        private void RebuildGrid()
        {
            Clear(gridRoot);
            _cards.Clear();
            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.cellSize = CollectionCard.SizeFor(_kind) + new Vector2(20f, 32f);
            var items = _catalog != null ? _catalog.Query(_kind, _group) : new List<CatalogEntry>();
            string selectedId = GameServices.Model.SelectedId(_kind);

            foreach (var e in items)
            {
                var card = Instantiate(cardPrefab, gridRoot);
                card.Bind(e, e.id == selectedId, OnCardPressed);
                _cards.Add(card);
            }
            countText.SetText("{0} items", items.Count);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;

            // Keep the detail panel on something sensible: the saved pick if shown, otherwise the first item.
            CatalogEntry keep = null;
            foreach (var e in items) if (e.id == selectedId) keep = e;
            ShowDetail(keep ?? (items.Count > 0 ? items[0] : null));
        }

        private static void Clear(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        }

        // ---------------------------------------------------------------- detail
        private void OnCardPressed(CollectionCard card) { ShowDetail(card.Entry); }

        private void ShowDetail(CatalogEntry e)
        {
            _current = e;
            foreach (var c in _cards) c.SetSelected(e != null && c.Entry.id == GameServices.Model.SelectedId(_kind));

            bool has = e != null;
            detailPreview.gameObject.SetActive(has);
            if (!has)
            {
                detailName.SetText("Nothing here yet");
                detailGroup.SetText(string.Empty);
                detailStatus.SetText(string.Empty);
                detailNote.SetText(string.Empty);
                selectButton.interactable = false;
                selectLabel.SetText("SELECT");
                footnote.SetText(string.Empty);
                return;
            }

            detailPreview.sprite = e.PreviewFor(ThemeArtStyles.For(GameServices.Model.World));
            detailPreview.preserveAspect = true;
            detailName.SetText(e.displayName);
            detailGroup.SetText(e.group);

            var theme = UIThemeManager.Current;
            if (e.Placeholder) { detailStatus.SetText("PLACEHOLDER: made quickly, to be replaced"); detailStatus.color = theme.GetColor(ColorToken.Danger); }
            else if (!e.Reviewed) { detailStatus.SetText("UNREVIEWED: awaiting inspection"); detailStatus.color = theme.GetColor(ColorToken.Accent); }
            else { detailStatus.SetText("APPROVED"); detailStatus.color = theme.GetColor(ColorToken.Success); }

            detailNote.SetText(string.IsNullOrEmpty(e.note) ? e.licence : e.note);
            UpdateSelectButton();
            // Highlight the card the player is looking at, not only the saved one.
            foreach (var c in _cards) c.SetSelected(c.Entry == e);
        }

        private void UpdateSelectButton()
        {
            if (_current == null) return;
            var model = GameServices.Model;
            var theme = UIThemeManager.Current;
            bool selected = model.SelectedId(_kind) == _current.id;
            bool playable = _current.playable || _kind == CatalogKind.World;

            string label; ColorToken colour = ColorToken.Primary; bool enabled = true; string foot;
            if (!playable)
            {
                label = "COMING SOON"; colour = ColorToken.TextSecondary; enabled = false;
                foot = string.IsNullOrEmpty(_current.playNote) ? "Not playable yet." : _current.playNote;
            }
            else if (selected)
            {
                label = "SELECTED"; colour = ColorToken.Success;
                foot = _kind == CatalogKind.Vehicle ? "This is the vehicle you ride." :
                       _kind == CatalogKind.Surface ? "This is the road you ride on." : "Remembered. Worlds are not on the track yet.";
            }
            else if (_kind == CatalogKind.Vehicle && !model.IsVehicleUnlocked(_current))
            {
                int missing = _current.cost - model.Coins;
                if (missing > 0) { label = "NEED " + missing + " MORE"; colour = ColorToken.TextSecondary; enabled = false; }
                else { label = "UNLOCK " + _current.cost; colour = ColorToken.Accent; }
                foot = "Costs " + _current.cost + " coins. You have " + model.Coins + ".";
            }
            else
            {
                label = _kind == CatalogKind.Vehicle ? "RIDE THIS" : (_kind == CatalogKind.Surface ? "USE THIS ROAD" : "SELECT");
                foot = _kind == CatalogKind.Surface ? "Changes the road at once. Grip " + _current.friction.ToString("0.0") + "." :
                       _kind == CatalogKind.Vehicle ? "Wheels are fitted under the picture. Heavier vehicles climb better but start slower." :
                       "Remembered. Worlds are not on the track yet.";
            }
            selectLabel.SetText(label);
            // Art buttons: orange arcade to act, steel plate when already selected, dull arcade when blocked.
            selectButtonImage.sprite = theme.GetSprite(colour == ColorToken.Success ? SpriteSlot.ButtonPlate : (colour == ColorToken.TextSecondary ? SpriteSlot.ButtonArcadeDisabled : SpriteSlot.ButtonArcade));
            selectButtonImage.color = Color.white;
            selectButton.interactable = true;
            _selectEnabled = enabled;
            footnote.SetText(foot);
        }

        private void OnSelectPressed()
        {
            if (_current == null || !_selectEnabled) return;
            GameServices.Actions?.SelectCatalogItem(_kind, _current.id);
        }

        private void RefreshSelectionState()
        {
            foreach (var c in _cards) { c.Refresh(); c.SetSelected(c.Entry == _current); }
            UpdateSelectButton();
        }
    }
}
