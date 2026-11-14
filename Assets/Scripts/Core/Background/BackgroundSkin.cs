using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// One scene of layered, horizontally looping artwork (for example a VISTA scene): four sprites ordered back to
    /// front, and how fast each scrolls relative to the camera. Pure data; the director decides how to show it.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Background Skin", fileName = "BackgroundSkin")]
    public class BackgroundSkin : ScriptableObject
    {
        public const int LayerCount = 4;

        public string displayName;
        public Mood mood;

        [Header("Layers, back to front")]
        public Sprite sky;
        public Sprite far;
        public Sprite mid;
        public Sprite near;

        [Tooltip("Camera-relative scroll per layer. 0 = fixed to the camera, 1 = moves with the world.")]
        public float[] scroll = { 0f, 0.2f, 0.4f, 0.8f };

        public Sprite Layer(int index)
        {
            switch (index)
            {
                case 0: return sky;
                case 1: return far;
                case 2: return mid;
                default: return near;
            }
        }

        public float Scroll(int index)
        {
            return (scroll != null && index < scroll.Length) ? scroll[index] : 0f;
        }
    }
}
