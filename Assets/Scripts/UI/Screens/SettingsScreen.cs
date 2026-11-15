using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>Settings popup: sound toggle and credits.</summary>
    public class SettingsScreen : AnimatedScreen
    {
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundButtonImage;
        [SerializeField] private TMP_Text soundLabel;
        [Tooltip("Adaptive Challenge opt-in (default off). Optional so an older prefab still works.")]
        [SerializeField] private Button adaptiveButton;
        [SerializeField] private Image adaptiveButtonImage;
        [SerializeField] private TMP_Text adaptiveLabel;
        [SerializeField] private Button closeButton;

        public override bool IsPopup => true;

        private void Awake()
        {
            soundButton.onClick.AddListener(() =>
                GameServices.Actions?.SetSoundEnabled(!GameServices.Model.SoundEnabled));
            if (adaptiveButton != null)
                adaptiveButton.onClick.AddListener(() => GameServices.Actions?.SetAdaptiveChallenge(!GameServices.Model.AdaptiveChallenge));
            closeButton.onClick.AddListener(() => Stack?.Pop());
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
            bool on = GameServices.Model.SoundEnabled;
            soundLabel.SetText(on ? "ON" : "OFF");
            soundButtonImage.sprite = UIThemeManager.Current.GetSprite(on ? SpriteSlot.ToggleOn : SpriteSlot.ToggleOff);
            if (adaptiveButton != null)
            {
                bool ad = GameServices.Model.AdaptiveChallenge;
                if (adaptiveLabel != null) adaptiveLabel.SetText(ad ? "ON" : "OFF");       // the visible opt-in indicator
                if (adaptiveButtonImage != null) adaptiveButtonImage.sprite = UIThemeManager.Current.GetSprite(ad ? SpriteSlot.ToggleOn : SpriteSlot.ToggleOff);
            }
        }
    }
}
