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

        [Tooltip("Pixel (default) snaps the layers to the art-pixel grid. Smooth scrolls in float with no snapping.")]
        public ArtStyle artStyle = ArtStyle.Pixel;

        [Header("Layers, back to front")]
        public Sprite sky;
        public Sprite far;
        public Sprite mid;
        public Sprite near;

        [Header("Sky decor (Smooth only, optional)")]
        [Tooltip("Sun or moon disc. Fixed to the camera (scroll 0) in the upper sky.")]
        public Sprite sun;
        [Tooltip("Loose cloud sprites drifting between the sky and the far layer.")]
        public Sprite[] clouds = new Sprite[0];
        [Tooltip("Camera-relative scroll of the clouds (between the sky 0 and the far layer).")]
        public float cloudScroll = 0.08f;
        [Tooltip("Star or glow overlay drawn over the sky (behind the sun or moon). Tiled, fixed to the camera (scroll 0) like the sky.")]
        public Sprite overlay;
        [Tooltip("Alternate near layers (same size and alignment) a player or a test can pick instead of 'near'. Index 0.. = first alternate.")]
        public Sprite[] nearAlternates = new Sprite[0];
        [Tooltip("Colour shown below the near layer when the camera dips. Alpha 0 = sample the near layer (pixel skins).")]
        public Color groundFill = new Color(0f, 0f, 0f, 0f);

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
