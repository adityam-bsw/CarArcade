using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>Writes the Resources/UI/HazardHud prefab (boss counter meter on the right edge, vignette, announcer subtitle). Colours here are placeholders; HazardHudView sets every colour from UITheme tokens at runtime.</summary>
    public static class HazardHudBuilder
    {
        [MenuItem("Road Builder/UI/Build Hazard HUD Prefab")]
        public static void Build()
        {
            var root = new GameObject("HazardHud", typeof(RectTransform), typeof(HazardHudView));
            Stretch(root.GetComponent<RectTransform>());
            var vig = Child(root, "Vignette", typeof(Image)); Stretch(vig.GetComponent<RectTransform>());
            var vi = vig.GetComponent<Image>(); vi.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Smooth/Hazards/vignette.png"); vi.raycastTarget = false; vi.color = Color.clear;

            var meter = Child(root, "BossMeter", typeof(Image)); var mr = meter.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(1f, 0.5f); mr.anchorMax = new Vector2(1f, 0.5f); mr.pivot = new Vector2(1f, 0.5f); mr.anchoredPosition = new Vector2(-24f, 0f); mr.sizeDelta = new Vector2(28f, 420f);
            var back = meter.GetComponent<Image>(); back.color = new Color(0f, 0f, 0f, 0.45f); back.raycastTarget = false;
            var fill = Child(meter, "Fill", typeof(Image)); Stretch(fill.GetComponent<RectTransform>(), 4f);
            var fi = fill.GetComponent<Image>(); fi.raycastTarget = false; fi.type = Image.Type.Filled; fi.fillMethod = Image.FillMethod.Vertical; fi.fillOrigin = 0; fi.fillAmount = 0f;
            var tier = Label(meter, "Tier", new Vector2(0f, 250f), 30, TextAlignmentOptions.Center);
            var phase = Label(meter, "Phase", new Vector2(0f, -250f), 28, TextAlignmentOptions.Center);
            var hits = Label(meter, "Hits", new Vector2(0f, -290f), 28, TextAlignmentOptions.Center);

            var sub = Child(root, "Subtitle", typeof(CanvasGroup), typeof(Image)); var sr = sub.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.5f, 0f); sr.anchorMax = new Vector2(0.5f, 0f); sr.pivot = new Vector2(0.5f, 0f); sr.anchoredPosition = new Vector2(0f, 190f); sr.sizeDelta = new Vector2(760f, 64f);
            var sb = sub.GetComponent<Image>(); sb.color = new Color(0f, 0f, 0f, 0.6f); sb.raycastTarget = false;
            var st = Label(sub, "Text", Vector2.zero, 34, TextAlignmentOptions.Center); Stretch(st.GetComponent<RectTransform>(), 8f);
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
        private static TMP_Text Label(GameObject parent, string name, Vector2 pos, float size, TextAlignmentOptions align)
        {
            var g = Child(parent, name, typeof(TextMeshProUGUI)); var t = g.GetComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = align; t.color = Color.white; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            var r = g.GetComponent<RectTransform>(); r.anchoredPosition = pos; r.sizeDelta = new Vector2(260f, 44f); return t;
        }
    }
}
