using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>
    /// Lays out every screen as a prefab from themed components (nothing is built at runtime). Re-run any time:
    /// it rewrites the prefabs in place. Menu: Road Builder > UI > Build Screen Prefabs.
    /// </summary>
    public static class UIPrefabBuilder
    {
        public const string Dir = "Assets/Prefabs/UI";
        public const string ScreensDir = Dir + "/Screens";
        public const string RootPath = Dir + "/UIRoot.prefab";

        private static Scene _scene;
        private static Sprite _coin, _heart, _podium, _gear, _star;

        private static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), TC = new Vector2(0.5f, 1);
        private static readonly Vector2 BC = new Vector2(0.5f, 0), BR = new Vector2(1, 0), C = new Vector2(0.5f, 0.5f);

        [MenuItem("Road Builder/UI/Build Screen Prefabs")]
        public static void BuildAll()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(IconFactory.GearPath) == null) IconFactory.Build();
            Directory.CreateDirectory(ScreensDir);

            _coin = Load("Assets/Resources/RoadBuilder/Coin.png");
            _heart = Load("Assets/Resources/RoadBuilder/Heart.png");
            _podium = Load("Assets/Resources/RoadBuilder/Art/Podium.png");
            _gear = Load(IconFactory.GearPath);
            _star = Load(IconFactory.StarPath);

            _scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Save(BuildHome(), "HomeScreen");
                Save(BuildWorlds(), "WorldsScreen");
                Save(BuildGarage(), "GarageScreen");
                Save(BuildHud(), "HudScreen");
                Save(BuildResult(), "ResultScreen");
                Save(BuildSettings(), "SettingsScreen");
                SaveRoot(BuildRoot());
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(_scene);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIPrefabBuilder] Prefabs written to " + Dir);
        }

        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, ScreensDir + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void SaveRoot(GameObject root)
        {
            PrefabUtility.SaveAsPrefabAsset(root, RootPath);
            Object.DestroyImmediate(root);
        }

        // ======================================================================== SCREENS
        private static GameObject BuildHome()
        {
            var root = ScreenRoot<HomeScreen>("HomeScreen", ShowStyle.Fade, out var c);

            var title = Label(c, "Title", "ROAD BUILDER RACER", TextStyle.Title);
            Place(title.rectTransform, TC, TC, new Vector2(0, -150), new Vector2(1500, 120));

            CoinPill(c, TL, TL, new Vector2(40, -40), out var coinsText);

            var settings = MakeButton(c, "SettingsButton", "", ColorToken.SurfaceAlt, ColorToken.TextPrimary, TR, TR, new Vector2(-40, -40), new Vector2(96, 96));
            var gear = Icon(settings.rt, "Gear", _gear, new Vector2(54, 54), ColorToken.TextPrimary);
            Place(gear.rectTransform, C, C, new Vector2(0, 5), new Vector2(54, 54));

            var bestPill = Img(c, "BestPill", SpriteSlot.Pill, ColorToken.SurfaceAlt);
            Place(bestPill.rectTransform, TR, TR, new Vector2(-156, -40), new Vector2(340, 84));
            var best = Label(bestPill.transform, "BestText", "BEST 0 m", TextStyle.Number);
            Stretch(best.rectTransform);

            var podium = new GameObject("Podium", typeof(RectTransform), typeof(Image));
            podium.transform.SetParent(c, false);
            var pimg = podium.GetComponent<Image>();
            pimg.sprite = _podium; pimg.raycastTarget = false;
            Place((RectTransform)podium.transform, new Vector2(0.5f, 0.35f), C, new Vector2(0, -20), new Vector2(520, 90));

            var play = MakeButton(c, "PlayButton", "START RACE!", ColorToken.Primary, ColorToken.TextPrimary, new Vector2(0.5f, 0.13f), C, Vector2.zero, new Vector2(440, 140), 52);
            var garage = MakeButton(c, "GarageButton", "GARAGE", ColorToken.Accent, ColorToken.TextOnAccent, new Vector2(0.5f, 0.13f), C, new Vector2(-430, 0), new Vector2(300, 110), 38);
            var worlds = MakeButton(c, "WorldsButton", "WORLDS", ColorToken.Secondary, ColorToken.TextPrimary, new Vector2(0.5f, 0.13f), C, new Vector2(430, 0), new Vector2(300, 110), 38);

            var s = root.GetComponent<HomeScreen>();
            Ref(s, "coinsText", coinsText); Ref(s, "bestText", best);
            Ref(s, "playButton", play.button); Ref(s, "garageButton", garage.button);
            Ref(s, "worldsButton", worlds.button); Ref(s, "settingsButton", settings.button);
            return root;
        }

        private static GameObject BuildWorlds()
        {
            var root = ScreenRoot<WorldsScreen>("WorldsScreen", ShowStyle.SlideUp, out var c);
            Dim(root.transform, ColorToken.Background, 0.92f, 0);

            var title = Label(c, "Title", "WORLDS", TextStyle.Heading);
            Place(title.rectTransform, TC, TC, new Vector2(0, -50), new Vector2(700, 100));
            var back = MakeButton(c, "BackButton", "< BACK", ColorToken.Secondary, ColorToken.TextPrimary, TL, TL, new Vector2(40, -40), new Vector2(260, 90), 34);

            var card = Img(c, "Card", SpriteSlot.Card, ColorToken.Surface);
            Place(card.rectTransform, C, C, new Vector2(0, -20), new Vector2(900, 640));

            var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            previewGo.transform.SetParent(card.transform, false);
            var preview = previewGo.GetComponent<Image>();
            preview.preserveAspect = true; preview.raycastTarget = false;
            Place((RectTransform)previewGo.transform, TC, TC, new Vector2(0, -30), new Vector2(760, 360));

            var wTitle = Label(card.transform, "WorldTitle", "COUNTRYSIDE HILLS", TextStyle.Heading, ColorToken.Accent, 46);
            Place(wTitle.rectTransform, TC, TC, new Vector2(0, -420), new Vector2(820, 70));
            var wDesc = Label(card.transform, "WorldDescription", "", TextStyle.Body, null, 30);
            Place(wDesc.rectTransform, TC, TC, new Vector2(0, -500), new Vector2(780, 110));

            var prev = MakeButton(c, "PrevButton", "<", ColorToken.Secondary, ColorToken.TextPrimary, C, C, new Vector2(-610, -20), new Vector2(130, 130), 60);
            var next = MakeButton(c, "NextButton", ">", ColorToken.Secondary, ColorToken.TextPrimary, C, C, new Vector2(610, -20), new Vector2(130, 130), 60);
            var road = MakeButton(c, "RoadButton", "ROAD: ASPHALT HIGHWAY", ColorToken.Accent, ColorToken.TextOnAccent, BC, BC, new Vector2(0, 70), new Vector2(760, 100), 36);

            var s = root.GetComponent<WorldsScreen>();
            Ref(s, "preview", preview); Ref(s, "titleText", wTitle); Ref(s, "descriptionText", wDesc);
            Ref(s, "roadLabel", road.label); Ref(s, "prevButton", prev.button); Ref(s, "nextButton", next.button);
            Ref(s, "roadButton", road.button); Ref(s, "backButton", back.button);
            return root;
        }

        private static GameObject BuildGarage()
        {
            var root = ScreenRoot<GarageScreen>("GarageScreen", ShowStyle.Fade, out var c);

            // Top and bottom bars leave the middle free so the showcase car stays visible.
            var top = Img(c, "TopBar", SpriteSlot.None, ColorToken.Background, 0.88f);
            top.rectTransform.anchorMin = TL; top.rectTransform.anchorMax = TR; top.rectTransform.pivot = TC;
            top.rectTransform.offsetMin = new Vector2(-300, -320); top.rectTransform.offsetMax = new Vector2(300, 0);
            var bottom = Img(c, "BottomBar", SpriteSlot.None, ColorToken.Background, 0.9f);
            bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = BR; bottom.rectTransform.pivot = BC;
            bottom.rectTransform.offsetMin = new Vector2(-300, 0); bottom.rectTransform.offsetMax = new Vector2(300, 420);

            var back = MakeButton(c, "BackButton", "< BACK", ColorToken.Secondary, ColorToken.TextPrimary, TL, TL, new Vector2(40, -40), new Vector2(260, 90), 34);
            var title = Label(c, "Title", "GARAGE", TextStyle.Heading);
            Place(title.rectTransform, TC, TC, new Vector2(0, -50), new Vector2(600, 100));
            CoinPill(c, TR, TR, new Vector2(-40, -40), out var coinsText);

            var prev = MakeButton(c, "PrevButton", "<", ColorToken.Secondary, ColorToken.TextPrimary, TC, C, new Vector2(-470, -235), new Vector2(120, 110), 56);
            var next = MakeButton(c, "NextButton", ">", ColorToken.Secondary, ColorToken.TextPrimary, TC, C, new Vector2(470, -235), new Vector2(120, 110), 56);
            var carName = Label(c, "CarName", "CLASSIC RUNNER", TextStyle.Heading, ColorToken.Accent, 56);
            Place(carName.rectTransform, TC, C, new Vector2(0, -235), new Vector2(760, 100));
            var action = MakeButton(c, "ActionButton", "EQUIPPED", ColorToken.Success, ColorToken.TextPrimary, TC, C, new Vector2(0, -395), new Vector2(480, 100), 40);

            var g = root.GetComponent<GarageScreen>();
            Ref(g, "coinsText", coinsText); Ref(g, "carNameText", carName); Ref(g, "actionLabel", action.label);
            Ref(g, "actionImage", action.image); Ref(g, "actionButton", action.button);
            Ref(g, "prevButton", prev.button); Ref(g, "nextButton", next.button); Ref(g, "backButton", back.button);

            string[] names = { "ENGINE", "SUSPENSION", "INK TANK" };
            float[] xs = { -580f, 0f, 580f };
            for (int i = 0; i < 3; i++)
            {
                var card = Img(c, "Upgrade_" + names[i], SpriteSlot.Card, ColorToken.Surface);
                Place(card.rectTransform, BC, BC, new Vector2(xs[i], 40), new Vector2(540, 340));

                var t = Label(card.transform, "Title", names[i], TextStyle.Heading, null, 40);
                Place(t.rectTransform, TC, TC, new Vector2(0, -26), new Vector2(500, 56));
                var h = Label(card.transform, "Hint", "", TextStyle.Caption, null, 26);
                Place(h.rectTransform, TC, TC, new Vector2(0, -86), new Vector2(500, 40));

                var pips = new Image[5];
                for (int p = 0; p < 5; p++)
                {
                    pips[p] = Img(card.transform, "Pip" + p, SpriteSlot.Pill, ColorToken.SurfaceAlt);
                    Place(pips[p].rectTransform, C, C, new Vector2((p - 2) * 84, 8), new Vector2(72, 24));
                }

                var up = MakeButton(card.transform, "UpgradeButton", "UPGRADE", ColorToken.Accent, ColorToken.TextOnAccent, BC, BC, new Vector2(0, 28), new Vector2(460, 100), 36);

                string row = "rows.Array.data[" + i + "]";
                Ref(g, row + ".title", t); Ref(g, row + ".hint", h); Ref(g, row + ".cost", up.label);
                Ref(g, row + ".button", up.button); Ref(g, row + ".buttonImage", up.image);
                Refs(g, row + ".pips", pips);
            }
            return root;
        }

        private static GameObject BuildHud()
        {
            var root = ScreenRoot<HudScreen>("HudScreen", ShowStyle.Fade, out var c);

            var distance = Label(c, "Distance", "0 m", TextStyle.Title, null, 84, TextAlignmentOptions.TopLeft);
            Place(distance.rectTransform, TL, TL, new Vector2(50, -30), new Vector2(560, 100));
            var speed = Label(c, "Speed", "0 km/h", TextStyle.Caption, ColorToken.TextPrimary, 30, TextAlignmentOptions.TopLeft);
            Place(speed.rectTransform, TL, TL, new Vector2(54, -130), new Vector2(400, 44));

            var hearts = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var h = new GameObject("Heart" + i, typeof(RectTransform), typeof(Image));
                h.transform.SetParent(c, false);
                hearts[i] = h.GetComponent<Image>();
                hearts[i].sprite = _heart; hearts[i].raycastTarget = false;
                Place((RectTransform)h.transform, TL, TL, new Vector2(54 + i * 66, -184), new Vector2(56, 56));
            }

            CoinPill(c, TR, TR, new Vector2(-40, -40), out var coinsText);

            var inkBg = Img(c, "InkBar", SpriteSlot.BarBackground, ColorToken.Outline);
            Place(inkBg.rectTransform, BC, BC, new Vector2(0, 46), new Vector2(760, 50));
            var fill = Img(inkBg.transform, "Fill", SpriteSlot.BarFill, ColorToken.Ink);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(4, 4); fill.rectTransform.offsetMax = new Vector2(-4, -4);
            var inkLabel = Label(inkBg.transform, "InkLabel", "ROAD INK 100%", TextStyle.Caption, ColorToken.TextPrimary, 26);
            Stretch(inkLabel.rectTransform);

            var boost = MakeButton(c, "BoostButton", "BOOST", ColorToken.Accent, ColorToken.TextOnAccent, BR, BR, new Vector2(-50, 40), new Vector2(300, 100), 40);

            var s = root.GetComponent<HudScreen>();
            Ref(s, "distanceText", distance); Ref(s, "speedText", speed); Ref(s, "coinsText", coinsText);
            Refs(s, "hearts", hearts); Ref(s, "inkBar", inkBg.rectTransform); Ref(s, "inkFill", fill.rectTransform);
            Ref(s, "inkLabel", inkLabel); Ref(s, "boostButton", boost.button);
            return root;
        }

        private static GameObject BuildResult()
        {
            var root = ScreenRoot<ResultScreen>("ResultScreen", ShowStyle.Pop, out var c);
            Dim(root.transform, ColorToken.Background, 0.9f, 0);

            var reason = Label(c, "Reason", "FELL INTO THE CHASM!", TextStyle.Heading, ColorToken.Danger, 60);
            Place(reason.rectTransform, new Vector2(0.5f, 0.82f), C, Vector2.zero, new Vector2(1400, 100));
            var cap = Label(c, "DistanceCaption", "DISTANCE", TextStyle.Caption, null, 30);
            Place(cap.rectTransform, new Vector2(0.5f, 0.70f), C, Vector2.zero, new Vector2(600, 44));
            var dist = Label(c, "Distance", "0 m", TextStyle.Title, null, 150);
            Place(dist.rectTransform, new Vector2(0.5f, 0.60f), C, Vector2.zero, new Vector2(1200, 190));

            var badge = new GameObject("NewBestBadge", typeof(RectTransform));
            badge.transform.SetParent(c, false);
            Place((RectTransform)badge.transform, new Vector2(0.5f, 0.47f), C, Vector2.zero, new Vector2(700, 90));
            var star = Icon(badge.transform, "Star", _star, new Vector2(70, 70), ColorToken.Accent);
            Place(star.rectTransform, C, C, new Vector2(-230, 0), new Vector2(70, 70));
            var bt = Label(badge.transform, "BadgeText", "NEW BEST!", TextStyle.Heading, ColorToken.Accent, 56);
            Place(bt.rectTransform, C, C, new Vector2(20, 0), new Vector2(420, 90));
            var star2 = Icon(badge.transform, "Star2", _star, new Vector2(70, 70), ColorToken.Accent);
            Place(star2.rectTransform, C, C, new Vector2(270, 0), new Vector2(70, 70));

            var pill = Img(c, "CoinsPill", SpriteSlot.Pill, ColorToken.SurfaceAlt);
            Place(pill.rectTransform, new Vector2(0.5f, 0.36f), C, Vector2.zero, new Vector2(340, 90));
            var coin = Icon(pill.transform, "CoinIcon", _coin, new Vector2(58, 58), ColorToken.Coin, tint: false);
            Place(coin.rectTransform, C, C, new Vector2(-110, 0), new Vector2(58, 58));
            var won = Label(pill.transform, "CoinsWon", "+0", TextStyle.Number, ColorToken.Coin, 48, TextAlignmentOptions.MidlineLeft);
            Place(won.rectTransform, C, C, new Vector2(40, 0), new Vector2(220, 80));
            var total = Label(c, "Total", "TOTAL 0", TextStyle.Caption, null, 28);
            Place(total.rectTransform, new Vector2(0.5f, 0.29f), C, Vector2.zero, new Vector2(500, 44));

            var retry = MakeButton(c, "RetryButton", "PLAY AGAIN", ColorToken.Primary, ColorToken.TextPrimary, new Vector2(0.5f, 0.15f), C, new Vector2(-260, 0), new Vector2(480, 150), 52);
            var menu = MakeButton(c, "MenuButton", "MENU", ColorToken.Secondary, ColorToken.TextPrimary, new Vector2(0.5f, 0.15f), C, new Vector2(300, 0), new Vector2(320, 110), 40);

            var s = root.GetComponent<ResultScreen>();
            Ref(s, "reasonText", reason); Ref(s, "distanceText", dist); Ref(s, "newBestBadge", badge);
            Ref(s, "coinsWonText", won); Ref(s, "totalText", total);
            Ref(s, "retryButton", retry.button); Ref(s, "menuButton", menu.button);
            return root;
        }

        private static GameObject BuildSettings()
        {
            var root = ScreenRoot<SettingsScreen>("SettingsScreen", ShowStyle.Pop, out var c);
            Dim(root.transform, ColorToken.Outline, 0.65f, 0);

            var panel = Img(c, "Panel", SpriteSlot.Popup, ColorToken.Surface);
            Place(panel.rectTransform, C, C, Vector2.zero, new Vector2(780, 560));

            var title = Label(panel.transform, "Title", "SETTINGS", TextStyle.Heading);
            Place(title.rectTransform, TC, TC, new Vector2(0, -40), new Vector2(600, 90));
            var sound = MakeButton(panel.transform, "SoundButton", "SOUND  ON", ColorToken.Success, ColorToken.TextPrimary, C, C, new Vector2(0, 40), new Vector2(560, 110), 42);
            var close = MakeButton(panel.transform, "CloseButton", "DONE", ColorToken.Secondary, ColorToken.TextPrimary, BC, BC, new Vector2(0, 80), new Vector2(320, 100), 40);
            var credits = Label(panel.transform, "Credits", "Fonts: Lilita One and Baloo 2 (SIL OFL 1.1)", TextStyle.Caption, null, 22);
            Place(credits.rectTransform, BC, BC, new Vector2(0, 24), new Vector2(700, 36));

            var s = root.GetComponent<SettingsScreen>();
            Ref(s, "soundButton", sound.button); Ref(s, "soundButtonImage", sound.image); Ref(s, "soundLabel", sound.label);
            Ref(s, "closeButton", close.button);
            return root;
        }

        private static GameObject BuildRoot()
        {
            var go = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, _scene);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var safe = Rect("SafeArea", go.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var stack = safe.gameObject.AddComponent<ScreenStack>();
            var flow = safe.gameObject.AddComponent<UIFlow>();

            string[] names = { "HomeScreen", "WorldsScreen", "GarageScreen", "HudScreen", "ResultScreen", "SettingsScreen" };
            var screens = new Object[names.Length];
            for (int i = 0; i < names.Length; i++)
                screens[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ScreensDir + "/" + names[i] + ".prefab").GetComponent<UIScreen>();

            Ref(stack, "root", safe);
            Refs(stack, "screenPrefabs", screens);
            Ref(flow, "stack", stack);
            return go;
        }

        // ======================================================================== BUILDING BLOCKS
        private struct Btn { public Button button; public Image image; public TextMeshProUGUI label; public RectTransform rt; }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static GameObject ScreenRoot<T>(string name, ShowStyle style, out RectTransform content) where T : UIScreen
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            SceneManager.MoveGameObjectToScene(go, _scene);
            Stretch((RectTransform)go.transform);
            var screen = go.AddComponent<T>();
            content = Rect("Content", go.transform);
            Stretch(content);

            var so = new SerializedObject(screen);
            so.FindProperty("showStyle").enumValueIndex = (int)style;
            so.FindProperty("content").objectReferenceValue = content;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        /// <summary>Full-screen tinted layer. It overflows the safe area so notch margins are covered too.</summary>
        private static void Dim(Transform root, ColorToken token, float alpha, int siblingIndex)
        {
            var img = Img(root, "Dim", SpriteSlot.None, token, alpha, raycast: true);
            Stretch(img.rectTransform, -400, -400, -400, -400);
            img.transform.SetSiblingIndex(siblingIndex);
        }

        private static Image Img(Transform parent, string name, SpriteSlot slot, ColorToken token, float alpha = 1f, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ThemedImage));
            go.transform.SetParent(parent, false);
            var ti = go.GetComponent<ThemedImage>();
            ti.slot = slot; ti.colorToken = token; ti.alpha = alpha;
            ti.Apply();
            var img = go.GetComponent<Image>();
            img.raycastTarget = raycast;
            if (slot == SpriteSlot.None)
            {
                // ThemedImage's first Apply (defaults) assigned a panel sprite; a "no sprite" layer must be flat.
                img.sprite = null;
                img.type = Image.Type.Simple;
            }
            return img;
        }

        private static Image Icon(Transform parent, string name, Sprite sprite, Vector2 size, ColorToken token, bool tint = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (tint)
            {
                // Adding ThemedImage applies its defaults once (slot Panel), so configure it first and
                // assign the icon sprite afterwards.
                var ti = go.AddComponent<ThemedImage>();
                ti.slot = SpriteSlot.None; ti.colorToken = token; ti.Apply();
            }
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            ((RectTransform)go.transform).sizeDelta = size;
            return img;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, TextStyle style,
            ColorToken? color = null, float? size = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ThemedText));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            var th = go.GetComponent<ThemedText>();
            th.style = style;
            if (color.HasValue) { th.overrideColor = true; th.colorToken = color.Value; }
            if (size.HasValue) { th.keepSize = true; t.fontSize = size.Value; }
            th.Apply();
            return t;
        }

        private static Btn MakeButton(Transform parent, string name, string label, ColorToken bg, ColorToken fg,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float fontSize = 40f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ThemedImage), typeof(Button), typeof(PressFeedback));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Place(rt, anchor, pivot, pos, size);

            var ti = go.GetComponent<ThemedImage>();
            ti.slot = SpriteSlot.Button; ti.colorToken = bg; ti.Apply();
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None; // PressFeedback owns the press animation

            var lab = Label(go.transform, "Label", label, TextStyle.Button, fg, fontSize);
            Stretch(lab.rectTransform, 0, 10, 0, 0); // sit above the button's bottom lip
            return new Btn { button = btn, image = img, label = lab, rt = rt };
        }

        private static Image CoinPill(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, out TextMeshProUGUI text)
        {
            var pill = Img(parent, "CoinPill", SpriteSlot.Pill, ColorToken.SurfaceAlt);
            Place(pill.rectTransform, anchor, pivot, pos, new Vector2(300, 84));
            var coin = Icon(pill.transform, "CoinIcon", _coin, new Vector2(56, 56), ColorToken.Coin, tint: false);
            Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(56, 56));
            text = Label(pill.transform, "CoinsText", "0", TextStyle.Number, ColorToken.Coin, 44, TextAlignmentOptions.MidlineRight);
            Stretch(text.rectTransform, 90, 0, 28, 0);
            return pill;
        }

        // ======================================================================== SERIALIZED FIELDS
        private static void Ref(Object target, string path, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(path);
            if (p == null) { Debug.LogError("[UIPrefabBuilder] Missing field '" + path + "' on " + target.GetType().Name); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Refs(Object target, string path, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(path);
            if (p == null) { Debug.LogError("[UIPrefabBuilder] Missing array '" + path + "' on " + target.GetType().Name); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
