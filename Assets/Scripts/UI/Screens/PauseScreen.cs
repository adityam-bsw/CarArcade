using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Pause popup over the HUD: Resume, a sound switch and Quit to menu. Views only: it asks the game to resume or quit and
    /// closes when the model says the run is no longer paused. Android Back resumes.
    /// </summary>
    public class PauseScreen : AnimatedScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundButtonImage;
        [SerializeField] private TMP_Text soundLabel;

        public override bool IsPopup => true;

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => GameServices.Actions?.ResumeRun());
            quitButton.onClick.AddListener(() => GameServices.Actions?.QuitRun());
            soundButton.onClick.AddListener(() => GameServices.Actions?.SetSoundEnabled(!GameServices.Model.SoundEnabled));
        }

        public override void OnBackPressed() { GameServices.Actions?.ResumeRun(); }

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
