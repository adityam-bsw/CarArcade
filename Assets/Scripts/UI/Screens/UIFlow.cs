using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// Connects game state to screens: when the model's screen changes, the matching screen prefab becomes the
    /// root of the stack. Also routes the button-press event to the click sound.
    /// </summary>
    public class UIFlow : MonoBehaviour
    {
        [SerializeField] private ScreenStack stack;

        private ScreenState? _applied;

        private void OnEnable()
        {
            GameServices.Model.ScreenChanged += Apply;
            PressFeedback.Pressed += PlayClick;
        }

        private void OnDisable()
        {
            GameServices.Model.ScreenChanged -= Apply;
            PressFeedback.Pressed -= PlayClick;
        }

        // Start runs after every Awake, so the game has already published its first screen.
        private void Start() { Apply(); }

        private void Apply()
        {
            var state = GameServices.Model.Screen;
            if (_applied == state && stack.Depth > 0) return;
            _applied = state;

            switch (state)
            {
                case ScreenState.HomeDashboard: stack.Reset<HomeScreen>(); break;
                case ScreenState.StageSelect: stack.Reset<WorldsScreen>(); break;
                case ScreenState.GarageMenu: stack.Reset<GarageScreen>(); break;
                case ScreenState.Playing: stack.Reset<HudScreen>(); break;
                case ScreenState.GameOver: stack.Reset<ResultScreen>(); break;
                case ScreenState.Rewards: stack.Reset<RewardsScreen>(); break;
            }
        }

        private static void PlayClick()
        {
            if (SoundManager.Instance) SoundManager.Instance.PlayClick();
        }
    }
}
