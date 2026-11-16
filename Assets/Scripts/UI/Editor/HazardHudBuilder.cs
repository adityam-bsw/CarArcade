using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>Writes the Resources/UI/HazardHud prefab (compact boss counter plate top-right, vignette, announcer subtitle). Colours here are placeholders; HazardHudView sets every colour from UITheme tokens at runtime.</summary>
    public static class HazardHudBuilder
    {
        [MenuItem("Road Builder/UI/Build Hazard HUD Prefab")]
        public static void Build()
        {
            var root = new GameObject("HazardHud", typeof(RectTransform), typeof(HazardHudView));
            Stretch(root.GetComponent<RectTransform>());
            var vig = Child(root, "Vignette", typeof(Image)); Stretch(vig.GetComponent<RectTransform>());
            var vi = vig.GetComponent<Image>(); vi.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Smooth/Hazards/vignette.png"); vi.raycastTarget = false; vi.color = Color.clear;

            // Compact themed plate in the top-right below the gems pill (about 12 percent of the screen height, never over the road band): tier name, a short horizontal meter, phase and hits on one line.
            var meter = Child(root, "BossMeter", typeof(Image), typeof(ThemedImage)); var mr = meter.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(1f, 1f); mr.anchorMax = new Vector2(1f, 1f); mr.pivot = new Vector2(1f, 1f); mr.anchoredPosition = new Vector2(-32f, -250f); mr.sizeDelta = new Vector2(440f, 150f);
            ThemeImg(meter, SpriteSlot.PanelBoard, ColorToken.Tint, false, 1f); meter.GetComponent<Image>().raycastTarget = false;
            var tier = Label(meter, "Tier", 30, ColorToken.Accent, 0f, 0f, 340f, 38f); TopAnchor(tier, 18f);
            var frame = Child(meter, "BarFrame", typeof(Image), typeof(ThemedImage)); var fr = frame.GetComponent<RectTransform>();
            fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 1f); fr.pivot = new Vector2(0.5f, 1f); fr.anchoredPosition = new Vector2(0f, -62f); fr.sizeDelta = new Vector2(340f, 32f);
            ThemeImg(frame, SpriteSlot.BarBackground, ColorToken.Ink, true, 0.9f); frame.GetComponent<Image>().raycastTarget = false;
            var fill = Child(frame, "Fill", typeof(Image), typeof(ThemedImage)); Stretch(fill.GetComponent<RectTransform>(), 6f);
            ThemeImg(fill, SpriteSlot.BarFill, ColorToken.Ink, true, 1f);
            var fi = fill.GetComponent<Image>(); fi.raycastTarget = false; fi.type = Image.Type.Filled; fi.fillMethod = Image.FillMethod.Horizontal; fi.fillOrigin = 0; fi.fillAmount = 0f;
            var phase = Label(meter, "Phase", 26, ColorToken.TextPrimary, 0f, 0f, 170f, 34f);
            var hits = Label(meter, "Hits", 26, ColorToken.TextPrimary, 0f, 0f, 170f, 34f);
            var pr = phase.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0.28f, 0f); pr.pivot = new Vector2(0.5f, 0f); pr.anchoredPosition = new Vector2(0f, 16f);
            var hr = hits.rectTransform; hr.anchorMin = hr.anchorMax = new Vector2(0.72f, 0f); hr.pivot = new Vector2(0.5f, 0f); hr.anchoredPosition = new Vector2(0f, 16f);

            var sub = Child(root, "Subtitle", typeof(CanvasGroup), typeof(Image), typeof(ThemedImage)); var sr = sub.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0f); sr.anchorMax = new Vector2(0.5f, 0f); sr.pivot = new Vector2(0.5f, 0f); sr.anchoredPosition = new Vector2(0f, 190f); sr.sizeDelta = new Vector2(760f, 64f);
            ThemeImg(sub, SpriteSlot.Pill, ColorToken.Ink, true, 0.85f); sub.GetComponent<Image>().raycastTarget = false;
            var st = Label(sub, "Text", 34, ColorToken.TextPrimary, 0f, 0f, 0f, 0f); Stretch(st.GetComponent<RectTransform>(), 16f);
            sub.GetComponent<CanvasGroup>().alpha = 0f; sub.GetComponent<CanvasGroup>().blocksRaycasts = false;

            var so = new SerializedObject(root.GetComponent<HazardHudView>());
            so.FindProperty("vignette").objectReferenceValue = vi; so.FindProperty("meterRoot").objectReferenceValue = mr; so.FindProperty("meterFill").objectReferenceValue = fi; 
            so.FindProperty("tierText").objectReferenceValue = tier; so.FindProperty("phaseText").objectReferenceValue = phase; so.FindProperty("hitsText").objectReferenceValue = hits;
            so.FindProperty("subtitleText").objectReferenceValue = st; so.FindProperty("subtitleGroup").objectReferenceValue = sub.GetComponent<CanvasGroup>();
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/Resources/UI")) AssetDatabase.CreateFolder("Assets/Resources", "UI");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/UI/HazardHud.prefab"); Object.DestroyImmediate(root);
            Debug.Log("[Hazards] HazardHud prefab written");
        }

        private static GameObject Child(GameObject parent, string name, params System.Type[] comps)
        {
            var list = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) }; list.AddRange(comps);
            var g = new GameObject(name, list.ToArray()); g.transform.SetParent(parent.transform, false); return g;
        }
        private static void Stretch(RectTransform r, float pad = 0f) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(pad, pad); r.offsetMax = new Vector2(-pad, -pad); }
        private static void ThemeImg(GameObject g, SpriteSlot slot, ColorToken token, bool tint, float alpha)
        {
            var ti = g.GetComponent<ThemedImage>(); ti.slot = slot; ti.colorToken = token; ti.tint = tint; ti.alpha = alpha; ti.Apply();
        }
        private static void TopAnchor(TMP_Text t, float top) { var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f); r.anchoredPosition = new Vector2(0f, -top); }
        private static void Bottom(TMP_Text t, float y) { var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.pivot = new Vector2(0.5f, 0f); r.anchoredPosition = new Vector2(0f, y); }
        /// <summary>Themed TMP label (Lilita One via the theme's Display font); auto-sizes so it always fits its box.</summary>
        private static TMP_Text Label(GameObject parent, string name, float size, ColorToken color, float x, float y, float w, float h)
        {
            var g = Child(parent, name, typeof(TextMeshProUGUI), typeof(ThemedText)); var t = g.GetComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 14f;
            var th = g.GetComponent<ThemedText>(); th.style = TextStyle.Button; th.overrideColor = true; th.colorToken = color; th.keepSize = true; th.Apply();
            var r = g.GetComponent<RectTransform>(); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); return t;
        }
    }
}
