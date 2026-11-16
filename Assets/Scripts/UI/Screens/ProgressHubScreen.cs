using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// The progression hub: eight tabs (mastery, journey, daily, weekly, album, shop, unlocks, workshop) that all show the same kind of rows.
    /// The game builds the rows (<see cref="IGameActions.BuildHub"/>); this screen only lays them out, pages through them and sends taps back
    /// (<see cref="IGameActions.HubAction"/>). Nothing here forces the player to do anything.
    /// </summary>
    public class ProgressHubScreen : AnimatedScreen
    {
        [Serializable] public class Tab { public Button button; public Image image; public TMP_Text label; }
        [Serializable] public class Row { public GameObject root; public TMP_Text title, detail, buttonLabel; public Button button; public Image buttonImage; }

        [SerializeField] private TMP_Text coinsText, gemsText, scrapText, titleText, subtitleText, footerText;
        [SerializeField] private Button backButton, prevButton, nextButton;
        [SerializeField] private Tab[] tabs = new Tab[8];
        [SerializeField] private Row[] rows = new Row[7];

        private static readonly string[] TabNames = { "MASTERY", "JOURNEY", "DAILY", "WEEKLY", "ALBUM", "SHOP", "ROADS", "SCRAP" };
        private HubPage _page = HubPage.Mastery;
        private int _pageIndex;
        private HubView _view;
        private string _toast = "";
        private Tween _toastTween;

        private void Awake()
        {
            // The scrap counter used to sit under the last tab (SCRAP) at the right edge: move it beside the gem pill, in the free space left of it.
            if (scrapText != null)
            {
                var rt = scrapText.rectTransform; rt.anchoredPosition = new Vector2(-(36f + 220f + 16f), -168f);
                scrapText.alignment = TextAlignmentOptions.MidlineRight; scrapText.textWrappingMode = TextWrappingModes.NoWrap;
            }
            backButton.onClick.AddListener(() => Stack?.Pop());
            prevButton.onClick.AddListener(() => Turn(-1));
            nextButton.onClick.AddListener(() => Turn(1));
            for (int i = 0; i < tabs.Length; i++)
            {
                int page = i;
                tabs[i].label.SetText(TabNames[i]);
                tabs[i].button.onClick.AddListener(() => Open((HubPage)page));
            }
            for (int i = 0; i < rows.Length; i++)
            {
                int slot = i;
                rows[i].button.onClick.AddListener(() => OnRowPressed(slot));
            }
        }

        public override void OnShow(object args)
        {
            if (args is HubPage p) _page = p;
            _pageIndex = 0; _toast = "";
            GameServices.Model.Changed += Refresh;
            Refresh();
        }

        public override void OnHide() { GameServices.Model.Changed -= Refresh; _toastTween?.Kill(); }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameServices.Model.Changed -= Refresh;
            _toastTween?.Kill();
        }

        private void Open(HubPage page)
        {
            if (page == _page) return;
            _page = page; _pageIndex = 0; _toast = "";
            Refresh();
        }

        private void Turn(int dir)
        {
            int pages = PageCount();
            _pageIndex = Mathf.Clamp(_pageIndex + dir, 0, pages - 1);
            Refresh();
        }

        private int PageCount() { return _view == null ? 1 : Mathf.Max(1, Mathf.CeilToInt(_view.rows.Count / (float)rows.Length)); }

        private void OnRowPressed(int slot)
        {
            if (_view == null) return;
            int index = _pageIndex * rows.Length + slot;
            if (index >= _view.rows.Count || string.IsNullOrEmpty(_view.rows[index].action)) return;
            string msg = GameServices.Actions != null ? GameServices.Actions.HubAction(_view.rows[index].action) : "";
            if (msg.Length > 0) ShowToast(msg);
            Refresh();
        }

        private void ShowToast(string msg)
        {
            _toast = msg;
            _toastTween?.Kill();
            _toastTween = DOVirtual.DelayedCall(2f, () => { _toast = ""; Refresh(); }).SetUpdate(true).SetLink(gameObject);
        }

        private void Refresh()
        {
            var m = GameServices.Model;
            var theme = UIThemeManager.Current;
            coinsText.SetText("{0}", m.Coins);
            gemsText.SetText("{0}", m.Gems);
            scrapText.SetText("SCRAP {0}", m.Scrap);

            for (int i = 0; i < tabs.Length; i++)
            {
                bool on = i == (int)_page;
                tabs[i].image.sprite = theme.GetSprite(on ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonPlate);
                tabs[i].label.color = theme.GetColor(on ? ColorToken.TextOnAccent : ColorToken.TextPrimary);
            }

            _view = GameServices.Actions != null ? GameServices.Actions.BuildHub(_page) : new HubView { page = _page };
            int pages = PageCount();
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pages - 1);
            titleText.SetText(_view.title);
            subtitleText.SetText(_toast.Length > 0 ? _toast : _view.subtitle);
            subtitleText.color = theme.GetColor(_toast.Length > 0 ? ColorToken.Success : ColorToken.TextPrimary);
            footerText.SetText(_view.footer + (pages > 1 ? "     PAGE " + (_pageIndex + 1) + " / " + pages : ""));
            prevButton.gameObject.SetActive(pages > 1);
            nextButton.gameObject.SetActive(pages > 1);
            prevButton.interactable = _pageIndex > 0;
            nextButton.interactable = _pageIndex < pages - 1;

            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                int index = _pageIndex * rows.Length + i;
                bool has = index < _view.rows.Count;
                r.root.SetActive(has);
                if (!has) continue;
                var d = _view.rows[index];
                r.title.SetText(d.title);
                r.title.color = theme.GetColor(d.done ? ColorToken.Success : ColorToken.TextPrimary);
                r.detail.SetText(d.detail);
                bool hasButton = d.button.Length > 0;
                r.button.gameObject.SetActive(hasButton);
                if (!hasButton) continue;
                bool act = d.enabled && d.action.Length > 0;
                r.buttonLabel.SetText(d.button);
                r.buttonLabel.color = theme.GetColor(act ? ColorToken.TextOnAccent : ColorToken.TextPrimary);
                r.button.interactable = act;
                r.buttonImage.sprite = theme.GetSprite(act ? SpriteSlot.ButtonArcade : SpriteSlot.ButtonArcadeDisabled);
            }
        }
    }
}
