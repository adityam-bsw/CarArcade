using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>World skin of one archetype (hazards.md 4.5): look and sound only. Countryside ships first.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Projectile Skin", fileName = "SKIN-NEW")]
    public class ProjectileSkinSO : ScriptableObject
    {
        public string worldId = "country";
        public string archetypeId = "PRJ-BOLT";
        public string displayName = "Corn Shot";
        [Tooltip("Optional art. Null means the shape of our own procedural sprites (smooth circles) is used.")] public Sprite sprite;
        public Sprite tellSprite;
        public Color tint = new Color(1f, 0.82f, 0.2f);
        public Color tellTint = new Color(1f, 0.2f, 0.15f, 0.85f);
        public float scale = 0.8f;
        public string fireSfx;
    }
}
