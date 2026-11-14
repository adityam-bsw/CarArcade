using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>
    /// Builds a sandbox scene (Assets/Scenes/UIKit.unity) showing every themed building block, for judging the
    /// look and testing a theme change in one place. Not part of the game build.
    /// </summary>
    public static class UIKitSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/UIKit.unity";

        [MenuItem("Road Builder/UI/Build UI Kit Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UIThemeManager.Current.GetColor(ColorToken.Background);
            cam.orthographic = true;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var safe = Rect("SafeArea", canvasGo.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Title
            var title = Text("Title", safe, "ROAD BUILDER RACER", TextStyle.Title);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1400, 110));

            // Panel with caption
            var panel = Image("Panel", safe, SpriteSlot.Panel, ColorToken.Surface);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1200, 520));
            var cap = Text("Caption", panel.rectTransform, "Pick your ride and start drawing", TextStyle.Caption);
            Place(cap.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1000, 50));

            // Buttons
            Button(panel.rectTransform, "Garage", ColorToken.Accent, new Vector2(-380, -10), ColorToken.TextOnAccent);
            Button(panel.rectTransform, "START RACE!", ColorToken.Primary, new Vector2(0, -10), ColorToken.TextPrimary, big: true);
            Button(panel.rectTransform, "Modes", ColorToken.Secondary, new Vector2(380, -10), ColorToken.TextPrimary);

            // Currency pill
            var pill = Image("CoinPill", safe, SpriteSlot.Pill, ColorToken.SurfaceAlt);
            Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(-190, -70), new Vector2(320, 80));
            var coins = Text("Coins", pill.rectTransform, "570", TextStyle.Number);
            coins.GetComponent<ThemedText>().overrideColor = true;
            coins.GetComponent<ThemedText>().colorToken = ColorToken.Coin;
            Stretch(coins.rectTransform);

            // Ink bar
            var barBg = Image("InkBar", safe, SpriteSlot.BarBackground, ColorToken.Outline);
            Place(barBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(760, 44));
            var fill = Image("Fill", barBg.rectTransform, SpriteSlot.BarFill, ColorToken.Ink);
            fill.rectTransform.anchorMin = new Vector2(0, 0);
            fill.rectTransform.anchorMax = new Vector2(0.62f, 1);
            fill.rectTransform.offsetMin = new Vector2(4, 4);
            fill.rectTransform.offsetMax = new Vector2(-4, -4);
            var inkLabel = Text("InkLabel", barBg.rectTransform, "ROAD INK 62%", TextStyle.Caption);
            Stretch(inkLabel.rectTransform);
            inkLabel.alignment = TextAlignmentOptions.Center;

            // Components were configured after OnEnable ran, so restyle them once everything is set.
            foreach (var t in Object.FindObjectsByType<ThemedText>(FindObjectsSortMode.None)) t.Apply();
            foreach (var i in Object.FindObjectsByType<ThemedImage>(FindObjectsSortMode.None)) i.Apply();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[UIKit] Saved " + ScenePath);
        }

        // ---------------------------------------------------------------- helpers
        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor.y >= 0.99f ? new Vector2(0.5f, 1f) : (anchor.y <= 0.01f ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f));
            if (anchor.x >= 0.99f) rt.pivot = new Vector2(1f, rt.pivot.y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, TextStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ThemedText));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            go.GetComponent<ThemedText>().style = style;
            return t;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, SpriteSlot slot, ColorToken color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(ThemedImage));
            go.transform.SetParent(parent, false);
            var ti = go.GetComponent<ThemedImage>();
            ti.slot = slot; ti.colorToken = color;
            return go.GetComponent<UnityEngine.UI.Image>();
        }

        private static void Button(RectTransform parent, string label, ColorToken color, Vector2 pos, ColorToken textColor, bool big = false)
        {
            var img = Image("Btn_" + label, parent, SpriteSlot.Button, color);
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, big ? new Vector2(420, 150) : new Vector2(320, 120));
            img.gameObject.AddComponent<UnityEngine.UI.Button>().targetGraphic = img;
            var t = Text("Label", img.rectTransform, label, TextStyle.Button);
            var themed = t.GetComponent<ThemedText>();
            themed.overrideColor = true; themed.colorToken = textColor;
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(0, 8); // sit above the button's bottom lip
        }
    }
}
