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
        [SerializeField] private Button closeButton;

        public override bool IsPopup => true;

        private void Awake()
        {
            soundButton.onClick.AddListener(() =>
                GameServices.Actions?.SetSoundEnabled(!GameServices.Model.SoundEnabled));
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
        }
    }
}
