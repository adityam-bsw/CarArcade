using NUnit.Framework;
using UnityEngine;
using RoadBuilderGame.UI;

namespace RoadBuilderGame.Tests
{
    public class PortraitAndRoadsTests
    {
        [Test]
        public void WorldRoads_AreThematic_AndUnknownWorldsFallBack()
        {
            Assert.AreEqual("surface-snow", WorldRoads.DefaultFor("world-arctic", EnvironmentTheme.Country));
            Assert.AreEqual("surface-lava-crust", WorldRoads.DefaultFor("world-volcano", EnvironmentTheme.Country));
            Assert.AreEqual("surface-sand", WorldRoads.DefaultFor("world-desert", EnvironmentTheme.Desert));
            Assert.IsFalse(string.IsNullOrEmpty(WorldRoads.DefaultFor("world-unknown", EnvironmentTheme.Country)));
        }

        [Test]
        public void PortraitLayout_AppliesAndRestoresLandscape()
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(PortraitLayout));
            var child = new GameObject("c", typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(go.transform, false);
            child.anchorMin = child.anchorMax = new Vector2(1, 1); child.anchoredPosition = new Vector2(-10, -20); child.sizeDelta = new Vector2(100, 50);
            var pl = go.GetComponent<PortraitLayout>();
            pl.Entries = new[] { new PortraitLayout.Entry { target = child, anchorMin = new Vector2(.5f, .5f), anchorMax = new Vector2(.5f, .5f), pivot = new Vector2(.5f, .5f), position = Vector2.zero, size = new Vector2(100, 50), scale = 2f } };
            pl.Apply(true);
            Assert.AreEqual(2f, child.localScale.x);
            Assert.AreEqual(new Vector2(.5f, .5f), child.anchorMin);
            pl.Apply(false);
            Assert.AreEqual(1f, child.localScale.x);
            Assert.AreEqual(new Vector2(-10, -20), child.anchoredPosition);
            Object.DestroyImmediate(go);
        }
    }
}
