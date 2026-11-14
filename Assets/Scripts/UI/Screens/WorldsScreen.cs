using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>World select: environment card with previous/next, and the road material.</summary>
    public class WorldsScreen : AnimatedScreen
    {
        [SerializeField] private Image preview;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text roadLabel;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button roadButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button allWorldsButton;
        [SerializeField] private Button allSurfacesButton;

        private EnvironmentTheme _shownWorld = (EnvironmentTheme)(-1);

        private void Awake()
        {
            prevButton.onClick.AddListener(() => GameServices.Actions?.CycleWorld(-1));
            nextButton.onClick.AddListener(() => GameServices.Actions?.CycleWorld(1));
            roadButton.onClick.AddListener(() => GameServices.Actions?.CycleRoadStyle());
            backButton.onClick.AddListener(() => GameServices.Actions?.GoHome());
            if (allWorldsButton != null) allWorldsButton.onClick.AddListener(() => Stack?.Push<CollectionScreen>(CatalogKind.World));
            if (allSurfacesButton != null) allSurfacesButton.onClick.AddListener(() => Stack?.Push<CollectionScreen>(CatalogKind.Surface));
        }

        public override void OnBackPressed() { GameServices.Actions?.GoHome(); }

        public override void OnShow(object args)
        {
            _shownWorld = (EnvironmentTheme)(-1);
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
            GameCatalog.World(m.World, out string title, out string desc, out string previewPath);

            if (_shownWorld != m.World)
            {
                bool first = (int)_shownWorld == -1;
                _shownWorld = m.World;
                preview.sprite = Resources.Load<Sprite>(previewPath);
                if (!first) UIAnim.Punch(preview.transform, 0.06f);
            }
            titleText.SetText(title);
            descriptionText.SetText(desc);
            roadLabel.SetText("ROAD: " + GameCatalog.RoadStyleName(m.RoadStyle));
        }
    }
}
