using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadBuilderGame
{
    /// <summary>
    /// Lives alone in the Boot scene. Applies process-wide settings once, then loads the game scene.
    /// Services that must exist before the game (ads, analytics, cloud save) are initialised here later.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string firstScene = "Main";

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            SceneManager.LoadScene(firstScene);
        }
    }
}
