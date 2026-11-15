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
        public const string ComponentsDir = Dir + "/Components";

        private static Scene _scene;
        private static Sprite _coin, _heart, _podium, _gear, _star;

        private static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), TC = new Vector2(0.5f, 1);
        private static readonly Vector2 BC = new Vector2(0.5f, 0), BR = new Vector2(1, 0), C = new Vector2(0.5f, 0.5f);

        [MenuItem("Road Builder/UI/Build Screen Prefabs")]
        public static void BuildAll()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(IconFactory.GearPath) == null) IconFactory.Build();
            Directory.CreateDirectory(ScreensDir);
            Directory.CreateDirectory(ComponentsDir);

            _coin = Load("Assets/Resources/RoadBuilder/Coin.png");
            _heart = Load("Assets/Resources/RoadBuilder/Heart.png");
            _podium = Load("Assets/Resources/RoadBuilder/Art/Podium.png");
            _gear = Load(IconFactory.GearPath);
            _star = Load(IconFactory.StarPath);

            _scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var cardPrefab = SaveComponent<CollectionCard>(BuildCard(), "CollectionCard");
                var tabPrefab = SaveComponent<CollectionTab>(BuildTab(), "CollectionTab");
                Save(BuildHome(), "HomeScreen");
                Save(BuildWorlds(cardPrefab), "WorldsScreen");
                Save(BuildGarage(cardPrefab), "GarageScreen");
                Save(BuildHud(), "HudScreen");
                Save(BuildResult(), "ResultScreen");
                Save(BuildSettings(), "SettingsScreen");
                Save(BuildRewards(), "RewardsScreen");
                Save(BuildCollection(cardPrefab, tabPrefab), "CollectionScreen");
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

        private static T SaveComponent<T>(GameObject root, string name) where T : Component
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(root, ComponentsDir + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return asset.GetComponent<T>();
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

            WallBackdrop(c, true);
            var full = c;                 // full-bleed parent (rails and fills that must reach the screen edges)
            c = DesignFrameRect(c);       // everything else is anchored inside the 1920x1080 frame
            Props(c, PropMode.Full);

            // Grouped by area; every element keeps its own edge anchor.
            var hdrL = Group(c, "HeaderLeft");
            var hdrC = Group(c, "HeaderCenter");
            var hdrR = Group(c, "HeaderRight");
            var controls = Group(c, "Controls");

            // Title plate hangs from chains across the top.
            var titlePlate = ArtImg(hdrC, "TitlePlate", SpriteSlot.PlateTitle, TC, TC, new Vector2(0, 0), new Vector2(1000, 200));
            var title = Label(titlePlate.transform, "Title", "ROAD BUILDER RACER", TextStyle.Title, ColorToken.TextPrimary, 68);
            title.enableAutoSizing = true; title.fontSizeMin = 36; title.fontSizeMax = 68;
            Place(title.rectTransform, C, C, new Vector2(0, -2), new Vector2(820, 84));

            // Left: stamped coin counter. Right: settings badge and the best-distance license plate.
            var counter = ArtImg(hdrL, "CoinCounter", SpriteSlot.PlateCounter, TL, TL, new Vector2(36, -44), new Vector2(360, 104));
            var coinsText = Label(counter.transform, "CoinsText", "0", TextStyle.Number, ColorToken.Coin, 52, TextAlignmentOptions.MidlineRight);
            Place(coinsText.rectTransform, C, C, new Vector2(52, 2), new Vector2(210, 70));

            var settings = ArtButton(hdrR, "SettingsButton", SpriteSlot.BadgeHex, SpriteSlot.BadgeHex, SpriteSlot.BadgeHex, "", 0, ColorToken.TextPrimary,
                TR, TR, new Vector2(-40, -40), new Vector2(120, 120), 0f);
            var bestPlate = ArtImg(hdrR, "BestPlate", SpriteSlot.PlateLicense, TR, TR, new Vector2(-30, -176), new Vector2(400, 104), 3f);
            var best = Label(bestPlate.transform, "BestText", "BEST 0 m", TextStyle.Number, ColorToken.TextOnAccent, 46);
            best.enableAutoSizing = true; best.fontSizeMin = 28; best.fontSizeMax = 46;
            Place(best.rectTransform, C, C, new Vector2(0, -6), new Vector2(300, 56));

            // Controls on the garage floor: one big arcade button flanked by two steel tabs, tilted the other way.
            var play = ArtButton(controls, "PlayButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled, "START RACE",
                56, ColorToken.TextOnAccent, BC, BC, new Vector2(0, 54), new Vector2(560, 218), 0f, new Vector4(96, 40, 34, 36));
            var garage = ArtButton(controls, "GarageButton", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "GARAGE",
                44, ColorToken.TextPrimary, BC, BC, new Vector2(-560, 70), new Vector2(360, 134), 3f, new Vector4(30, 30, 30, 30));
            var worlds = ArtButton(controls, "WorldsButton", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "WORLDS",
                44, ColorToken.TextPrimary, BC, BC, new Vector2(560, 70), new Vector2(360, 134), -3f, new Vector4(30, 30, 30, 30));

            // Rewards: daily reward and missions, with a red sticker when something can be claimed (top right, left of the gear).
            var rewards = ArtButton(hdrR, "RewardsButton", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "REWARDS",
                32, ColorToken.TextPrimary, TR, TR, new Vector2(-190, -48), new Vector2(260, 104), -2f, new Vector4(26, 26, 26, 26));
            var rewardsDot = ArtImg(rewards.rt, "Dot", SpriteSlot.DecoSticker, TR, TR, new Vector2(18, 18), new Vector2(60, 60), 10f);

            // Hand-stuck details that overlap the plates (never tappable).
            ArtImg(hdrL, "TapeCounter", SpriteSlot.DecoTape, TL, TL, new Vector2(300, -26), new Vector2(130, 40), -9f);
            ArtImg(hdrR, "TapeBest", SpriteSlot.DecoTape, TR, TR, new Vector2(-270, -158), new Vector2(130, 40), 11f);
            ArtImg(controls, "StickerStar", SpriteSlot.DecoSticker, BC, BC, new Vector2(250, 214), new Vector2(104, 104), 12f);

            var s = root.GetComponent<HomeScreen>();
            Ref(s, "coinsText", coinsText); Ref(s, "bestText", best);
            Ref(s, "playButton", play.button); Ref(s, "garageButton", garage.button);
            Ref(s, "worldsButton", worlds.button); Ref(s, "settingsButton", settings.button);
            Ref(s, "titlePlate", titlePlate.rectTransform);
            Ref(s, "rewardsButton", rewards.button); Ref(s, "rewardsDot", rewardsDot.gameObject);
            Refs(s, "popIns", new Object[] { counter.rectTransform, settings.rt, bestPlate.rectTransform, garage.rt, play.rt, worlds.rt, rewards.rt });
            return root;
        }

        private enum PropMode { Full, Light, Lamps }

        // name, sprite id, x, y (top-left on the 384x216 grid), pivot x, pivot y, which modes show it (F full, L light, A lamps only)
        private static readonly (string name, string id, float x, float y, float px, float py, string modes)[] PropTable =
        {
            ("Glow0", "lamp_glow", -4, 33, 0.5f, 0.5f, "FA"), ("Glow1", "lamp_glow", 288, 33, 0.5f, 0.5f, "FA"),
            ("Pegboard", "pegboard", 14, 42, 0.5f, 0.5f, "F"), ("Wrench", "wrench", 17, 47, 0.5f, 0.5f, "F"),
            ("Hammer", "hammer", 61, 45, 0.5f, 0.5f, "F"), ("Screwdriver", "screwdriver", 17, 69, 0.5f, 0.5f, "F"),
            ("Saw", "saw", 49, 83, 0.5f, 0.5f, "F"), ("Gear", "gear", 19, 93, 0.5f, 0.5f, "F"), ("Toolbox", "toolbox", 47, 108, 0.5f, 0.5f, "F"),
            ("Shelf", "shelf", 289, 117, 0.5f, 0.5f, "F"), ("RobotBody", "robot_body", 295, 57, 0.5f, 0.5f, "F"),
            ("RobotArmL", "robot_arm", 292, 84, 0.5f, 0.95f, "F"), ("RobotArmR", "robot_arm", 333, 84, 0.5f, 0.95f, "F"),
            ("RobotEyes", "robot_eyes", 310, 71, 0.5f, 0.5f, "F"), ("RobotAntenna", "robot_antenna", 314, 51, 0.4f, 0.1f, "F"),
            ("CanRed", "can_red", 342, 102, 0.5f, 0.1f, "F"), ("CanOrange", "can_orange", 353, 102, 0.5f, 0.1f, "F"), ("CanTeal", "can_teal", 364, 102, 0.5f, 0.1f, "F"),
            ("Tyre0", "tyre", 293, 134, 0.5f, 0.5f, "FL"), ("Tyre1", "tyre", 293, 146, 0.5f, 0.5f, "FL"), ("Drum", "drum", 343, 128, 0.5f, 0.5f, "FL"),
            ("Lamp0", "lamp", 33, 10, 0.48f, 1f, "FA"), ("Lamp1", "lamp", 325, 10, 0.48f, 1f, "FA"),
            ("ClockFace", "clock_face", 352, 40, 0.5f, 0.5f, "FA"),
        };

        // Props are grouped by what they are: lights, the pegboard with its tools, the shelf, the robot, the tyre stack...
        // Left groups hang from the left screen edge, right groups from the right edge, so each group moves as one thing.
        private static readonly (string group, bool right, string[] items)[] PropGroups =
        {
            ("LightLeft", false, new[] { "Glow0", "Lamp0" }),
            ("LightRight", true, new[] { "Glow1", "Lamp1" }),
            ("PegboardTools", false, new[] { "Pegboard", "Wrench", "Hammer", "Screwdriver", "Saw", "Gear", "Toolbox" }),
            ("ShelfAndCans", true, new[] { "Shelf", "CanRed", "CanOrange", "CanTeal" }),
            ("Robot", true, new[] { "RobotBody", "RobotArmL", "RobotArmR", "RobotEyes", "RobotAntenna" }),
            ("TyreStack", true, new[] { "Tyre0", "Tyre1" }),
            ("OilDrum", true, new[] { "Drum" }),
            ("Clock", true, new[] { "ClockFace" }),
        };

        /// <summary>
        /// Stand-alone pixel props (5 px per art pixel), each its own Image so WorkshopProps can animate it, arranged as
        /// Props / Left or Right edge / group / prop. Full: the whole garage. Light: tyres and drum. Lamps: lamps, glows and clock.
        /// </summary>
        private static void Props(Transform c, PropMode mode)
        {
            var set = AssetDatabase.LoadAssetAtPath<WorkshopPropSet>("Assets/Resources/UI/WorkshopProps.asset");
            if (set == null) { Debug.LogError("[UIPrefabBuilder] WorkshopProps.asset missing: run Create Default Theme"); return; }

            // Full width, 1080 high, centred; the two edge holders pin their groups to the left or right screen edge.
            var holder = Rect("Props", c);
            holder.anchorMin = new Vector2(0, 0.5f); holder.anchorMax = new Vector2(1, 0.5f);
            holder.pivot = new Vector2(0.5f, 0.5f); holder.sizeDelta = new Vector2(0, 1080); holder.anchoredPosition = Vector2.zero;
            holder.gameObject.AddComponent<WorkshopProps>();
            var left = Rect("LeftEdge", holder);
            left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0, 1); left.pivot = new Vector2(0, 0.5f);
            left.sizeDelta = new Vector2(960, 0); left.anchoredPosition = Vector2.zero;
            var right = Rect("RightEdge", holder);
            right.anchorMin = new Vector2(1, 0); right.anchorMax = new Vector2(1, 1); right.pivot = new Vector2(1, 0.5f);
            right.sizeDelta = new Vector2(960, 0); right.anchoredPosition = Vector2.zero;

            char key = mode == PropMode.Full ? 'F' : (mode == PropMode.Light ? 'L' : 'A');
            foreach (var g in PropGroups)
            {
                // The group's box is the union of its props that this mode shows.
                var members = new System.Collections.Generic.List<(string name, string id, float x, float y, float px, float py, Sprite sprite)>();
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var item in g.items)
                {
                    foreach (var e in PropTable)
                    {
                        if (e.name != item || e.modes.IndexOf(key) < 0) continue;
                        var sprite = set.Get(e.id);
                        if (sprite == null) { Debug.LogWarning("[UIPrefabBuilder] No prop sprite " + e.id); continue; }
                        members.Add((e.name, e.id, e.x, e.y, e.px, e.py, sprite));
                        minX = Mathf.Min(minX, e.x); minY = Mathf.Min(minY, e.y);
                        maxX = Mathf.Max(maxX, e.x + sprite.rect.width); maxY = Mathf.Max(maxY, e.y + sprite.rect.height);
                    }
                }
                if (members.Count == 0) continue;

                var side = g.right ? right : left;
                var grp = Rect(g.group, side);
                grp.anchorMin = grp.anchorMax = new Vector2(g.right ? 1f : 0f, 1f);
                grp.pivot = new Vector2(g.right ? 1f : 0f, 1f);
                grp.sizeDelta = new Vector2(maxX - minX, maxY - minY) * 5f;
                grp.anchoredPosition = new Vector2(g.right ? maxX * 5f - 1920f : minX * 5f, -minY * 5f);

                foreach (var m in members)
                {
                    var go = new GameObject(m.name, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(grp, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = m.sprite; img.raycastTarget = false; img.preserveAspect = true;
                    var size = m.sprite.rect.size * 5f;
                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(m.px, m.py);
                    rt.sizeDelta = size;
                    // Position by the sprite's top-left corner inside its group, whatever the pivot is.
                    rt.anchoredPosition = new Vector2((m.x - minX) * 5f + size.x * m.px, -(m.y - minY) * 5f - size.y * (1f - m.py));
                }
            }

            // Clock hands hang off the face so they turn about its centre.
            RectTransform face = null;
            foreach (var r in holder.GetComponentsInChildren<RectTransform>(true)) if (r.name == "ClockFace") face = r;
            if (face != null)
            {
                foreach (var h in new[] { ("ClockMinute", "clock_hand_minute"), ("ClockHour", "clock_hand_hour") })
                {
                    var sprite = set.Get(h.Item2);
                    var go = new GameObject(h.Item1, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(face, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = sprite; img.raycastTarget = false;
                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.sizeDelta = sprite.rect.size * 5f;
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// The 1920x1080 design frame: all controls of a screen are anchored inside it (see DesignFrame). Full-bleed art
        /// (background, dim) stays outside it, on the screen content itself.
        /// </summary>
        private static RectTransform DesignFrameRect(Transform content)
        {
            var rt = Rect("Frame", content);
            Place(rt, C, C, Vector2.zero, DesignFrame.DesignSize);
            rt.gameObject.AddComponent<DesignFrame>();
            return rt;
        }

        /// <summary>A stretch container that only groups its children; they keep their own edge anchors.</summary>
        private static RectTransform Group(Transform parent, string name)
        {
            var rt = Rect(name, parent);
            Stretch(rt);
            return rt;
        }

        // ---- Retro Arcade Workshop building blocks -------------------------------------------------------------
        private static Sprite ThemeSprite(SpriteSlot slot)
            => AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UI/UITheme_Default.asset").GetSprite(slot);

        /// <summary>Plain image with a sprite from the theme, for pieces a script swaps itself (pips, hearts, needle, toggle).</summary>
        private static Image RawImg(Transform parent, string name, SpriteSlot slot, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = ThemeSprite(slot);
            img.raycastTarget = false; img.preserveAspect = true;
            Place((RectTransform)go.transform, anchor, pivot, pos, size);
            return img;
        }

        /// <summary>
        /// Garage background built from separate parts so it fits any screen shape: tiles repeat sideways (wall, floor,
        /// hazard line, ceiling beam), the door sits in the centre, and ink / floor-coloured fills cover anything taller than
        /// 1080. It bleeds past the safe area on purpose.
        /// </summary>
        private static void WallBackdrop(Transform c, bool door)
        {
            var back = Rect("Background", c);
            Stretch(back, -400, -400, -400, -400);

            var top = Img(back, "FillTop", SpriteSlot.None, ColorToken.Background);
            // The fills only cover what lies above and below the 1080-high stage, so the door's opening stays see-through.
            top.rectTransform.anchorMin = new Vector2(0, 0.5f); top.rectTransform.anchorMax = Vector2.one;
            top.rectTransform.offsetMin = new Vector2(0, 540); top.rectTransform.offsetMax = Vector2.zero;
            var bottom = Img(back, "FillBottom", SpriteSlot.None, ColorToken.Surface);
            bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1, 0.5f);
            bottom.rectTransform.offsetMin = Vector2.zero; bottom.rectTransform.offsetMax = new Vector2(0, -540);

            // A 1080-high stage, full width, centred: everything below is placed in 5-pixel art units from its top.
            var stage = Rect("Stage", back);
            stage.anchorMin = new Vector2(0, 0.5f); stage.anchorMax = new Vector2(1, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f); stage.sizeDelta = new Vector2(0, 1080); stage.anchoredPosition = Vector2.zero;

            // ONE wall pattern for the whole background. Around the door it is shown through three clipped windows that all
            // use the same tile origin, so the planks line up across the joins (no seams) and the door opening stays see-through.
            if (door)
            {
                WallWindow(stage, "WallTop", 0f, 1f, 0f, 1f, new Vector4(0, 810, 0, 50));      // above the door
                WallWindow(stage, "WallLeft", 0f, 0.5f, 0f, 2f, new Vector4(0, 330, 470, 270)); // left of the door
                WallWindow(stage, "WallRight", 0.5f, 1f, -1f, 1f, new Vector4(470, 330, 0, 270)); // right of the door
            }
            else WallWindow(stage, "Wall", 0f, 1f, 0f, 1f, new Vector4(0, 330, 0, 50));
            TileStrip(stage, "Floor", SpriteSlot.BgFloorTile, 750, 330);
            TileStrip(stage, "HazardLine", SpriteSlot.BgHazardTile, 740, 20);
            TileStrip(stage, "Beam", SpriteSlot.BgBeamTile, 0, 50);
            if (door) ArtImg(stage, "Door", SpriteSlot.BgDoor, TC, TC, new Vector2(0, -270), new Vector2(940, 510));
        }

        /// <summary>
        /// A clipped view onto the one shared wall: the container is the half or the whole stage (so its width follows the
        /// screen), the wall image inside spans the full stage width from the same left edge, and the clip padding picks the part to show.
        /// </summary>
        private static void WallWindow(Transform stage, string name, float cxMin, float cxMax, float childXMin, float childXMax, Vector4 padding)
        {
            var clip = Rect(name, stage);
            clip.anchorMin = new Vector2(cxMin, 0); clip.anchorMax = new Vector2(cxMax, 1);
            clip.offsetMin = clip.offsetMax = Vector2.zero;
            clip.gameObject.AddComponent<RectMask2D>().padding = padding;

            var go = new GameObject("Planks", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(clip, false);
            var img = go.GetComponent<Image>();
            img.sprite = ThemeSprite(SpriteSlot.BgWallTile);
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(childXMin, 0); rt.anchorMax = new Vector2(childXMax, 1);
            rt.offsetMin = new Vector2(0, 330); rt.offsetMax = new Vector2(0, -50);   // wall band: 50 px below the stage top down to 750
        }

        private static void TileStrip(Transform stage, string name, SpriteSlot slot, float top, float height,
            float xMin = 0f, float xMax = 1f, float leftInset = 0f, float rightInset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(stage, false);
            var img = go.GetComponent<Image>();
            img.sprite = ThemeSprite(slot);
            img.type = Image.Type.Tiled;
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(xMin, 1); rt.anchorMax = new Vector2(xMax, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(leftInset, -top - height);
            rt.offsetMax = new Vector2(rightInset, -top);
        }

        private static Image SmallTitle(Transform c, string text, out TextMeshProUGUI label)
        {
            var plate = ArtImg(c, "TitlePlate", SpriteSlot.PlateTitleSmall, TC, TC, Vector2.zero, new Vector2(640, 160));
            label = Label(plate.transform, "Title", text, TextStyle.Title, ColorToken.TextPrimary, 58);
            label.enableAutoSizing = true; label.fontSizeMin = 32; label.fontSizeMax = 58;
            Place(label.rectTransform, C, C, new Vector2(0, -4), new Vector2(470, 66));
            return plate;
        }

        private static Btn BackBadge(Transform c)
            => ArtButton(c, "BackButton", SpriteSlot.BadgeHexBack, SpriteSlot.BadgeHexBack, SpriteSlot.BadgeHexBack, "", 0, ColorToken.TextPrimary,
                TL, TL, new Vector2(40, -36), new Vector2(120, 120), 0f);

        private static Image CoinCounter(Transform c, Vector2 anchor, Vector2 pivot, Vector2 pos, out TextMeshProUGUI text)
        {
            var plate = ArtImg(c, "CoinCounter", SpriteSlot.PlateCounter, anchor, pivot, pos, new Vector2(360, 104));
            text = Label(plate.transform, "CoinsText", "0", TextStyle.Number, ColorToken.Coin, 52, TextAlignmentOptions.MidlineRight);
            Place(text.rectTransform, C, C, new Vector2(52, 2), new Vector2(210, 70));
            return plate;
        }

        /// <summary>Illustrated, non-tinted image from a theme slot at its designed aspect. Never takes taps.</summary>
        private static Image ArtImg(Transform parent, string name, SpriteSlot slot, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float rotZ = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ThemedImage));
            go.transform.SetParent(parent, false);
            var ti = go.GetComponent<ThemedImage>();
            ti.slot = slot; ti.colorToken = ColorToken.Tint; ti.tint = false;
            ti.Apply();
            var img = go.GetComponent<Image>();
            img.raycastTarget = false; img.preserveAspect = true; img.type = Image.Type.Simple;
            Place((RectTransform)go.transform, anchor, pivot, pos, size);
            go.transform.localEulerAngles = new Vector3(0, 0, rotZ);
            return img;
        }

        /// <summary>
        /// A button made of artwork. Normal/pressed/disabled sprites come from theme slots, the label is TextMeshPro placed
        /// by <paramref name="pad"/> (left, bottom, right, top). The hit area is the whole image (at least 96 px).
        /// </summary>
        private static Btn ArtButton(Transform parent, string name, SpriteSlot normal, SpriteSlot pressed, SpriteSlot disabled, string label,
            float fontSize, ColorToken fg, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float rotZ, Vector4 pad = default)
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UI/UITheme_Default.asset");
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ThemedImage), typeof(Button), typeof(PressFeedback));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Place(rt, anchor, pivot, pos, size);
            go.transform.localEulerAngles = new Vector3(0, 0, rotZ);

            var ti = go.GetComponent<ThemedImage>();
            ti.slot = normal; ti.colorToken = ColorToken.Tint; ti.tint = false; ti.Apply();
            var img = go.GetComponent<Image>();
            img.preserveAspect = true; img.type = Image.Type.Simple; img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { pressedSprite = theme.GetSprite(pressed), highlightedSprite = theme.GetSprite(normal),
                selectedSprite = theme.GetSprite(normal), disabledSprite = theme.GetSprite(disabled) };

            TextMeshProUGUI lab = null;
            if (!string.IsNullOrEmpty(label))
            {
                lab = Label(go.transform, "Label", label, TextStyle.Button, fg, fontSize);
                lab.enableAutoSizing = true; lab.fontSizeMin = fontSize * 0.6f; lab.fontSizeMax = fontSize;
                Stretch(lab.rectTransform, pad.x, pad.y, pad.z, pad.w);
            }
            return new Btn { button = btn, image = img, label = lab, rt = rt };
        }

        /// <summary>A horizontal scroll strip for CollectionCards. Returns the CardRail on it.</summary>
        private static CardRail BuildRail(Transform parent, string name, CollectionCard cardPrefab, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(CardRail));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, anchor, pivot, pos, size);
            // Rails run the full screen width (edge to edge), whatever the screen shape; only their height is fixed.
            var rr = (RectTransform)go.transform;
            rr.anchorMin = new Vector2(0, anchor.y); rr.anchorMax = new Vector2(1, anchor.y);
            rr.sizeDelta = new Vector2(0, size.y); rr.anchoredPosition = new Vector2(0, pos.y);

            var viewport = Rect("Viewport", (RectTransform)go.transform);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0);

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 22; row.padding = new RectOffset(40, 40, 10, 10); row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = false; row.childControlHeight = false;
            row.childForceExpandWidth = false; row.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 30f;
            scroll.decelerationRate = 0.12f;

            var rail = go.GetComponent<CardRail>();
            Ref(rail, "scroll", scroll); Ref(rail, "content", content); Ref(rail, "cardPrefab", cardPrefab);
            return rail;
        }


        private static GameObject BuildWorlds(CollectionCard cardPrefab)
        {
            var root = ScreenRoot<WorldsScreen>("WorldsScreen", ShowStyle.SlideUp, out var c);
            WallBackdrop(c, true);
            var full = c;                 // full-bleed parent (rails and fills that must reach the screen edges)
            c = DesignFrameRect(c);       // everything else is anchored inside the 1920x1080 frame
            Props(c, PropMode.Lamps);

            // Grouped by area; every element keeps its own edge anchor.
            var header = Group(c, "Header");
            var tabs = Group(c, "Tabs");
            var rightPanel = Group(c, "RightPanel");

            SmallTitle(header, "WORLDS", out _);
            var back = BackBadge(header);

            // Left wall: two big tabs choose what the rail below shows. They also say what is picked now.
            var worldTab = ArtButton(tabs, "WorldTab", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcade, "WORLD",
                34, ColorToken.TextOnAccent, TL, TL, new Vector2(40, -200), new Vector2(430, 168), 0f, new Vector4(80, 34, 30, 30));
            var roadTab = ArtButton(tabs, "RoadTab", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "ROAD",
                34, ColorToken.TextPrimary, TL, TL, new Vector2(40, -390), new Vector2(430, 168), 0f, new Vector4(40, 30, 30, 30));
            worldTab.button.transition = Selectable.Transition.None;
            roadTab.button.transition = Selectable.Transition.None;

            // Right wall: the picked combination and the way out to a race.
            var plate = ArtImg(rightPanel, "SummaryPlate", SpriteSlot.PlateName, TR, TR, new Vector2(-34, -200), new Vector2(480, 90));
            var summary = Label(plate.transform, "Summary", "", TextStyle.Body, ColorToken.TextPrimary, 30);
            summary.enableAutoSizing = true; summary.fontSizeMin = 16; summary.fontSizeMax = 30;
            Place(summary.rectTransform, C, C, new Vector2(0, -2), new Vector2(400, 56));
            var race = ArtButton(rightPanel, "RaceButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                "RACE!", 52, ColorToken.TextOnAccent, TR, TR, new Vector2(-40, -310), new Vector2(480, 187), 0f, new Vector4(100, 40, 40, 34));
            ArtImg(rightPanel, "StickerStar", SpriteSlot.DecoSticker, TR, TR, new Vector2(-24, -296), new Vector2(84, 84), 14f);

            var rail = BuildRail(full, "Rail", cardPrefab, BC, BC, new Vector2(0, 6), new Vector2(1920, 310));
            var hint = Label(c, "Hint", "", TextStyle.Body, ColorToken.TextPrimary, 30);
            hint.enableAutoSizing = true; hint.fontSizeMin = 18; hint.fontSizeMax = 30;
            Place(hint.rectTransform, BC, BC, new Vector2(0, 322), new Vector2(1500, 50));

            var s = root.GetComponent<WorldsScreen>();
            Ref(s, "rail", rail);
            Ref(s, "worldTab", worldTab.button); Ref(s, "roadTab", roadTab.button);
            Ref(s, "worldTabImage", worldTab.image); Ref(s, "roadTabImage", roadTab.image);
            Ref(s, "worldTabLabel", worldTab.label); Ref(s, "roadTabLabel", roadTab.label);
            Ref(s, "raceButton", race.button); Ref(s, "summaryText", summary);
            Ref(s, "hintText", hint); Ref(s, "backButton", back.button);
            return root;
        }

        private static GameObject BuildGarage(CollectionCard cardPrefab)
        {
            var root = ScreenRoot<GarageScreen>("GarageScreen", ShowStyle.Fade, out var c);
            WallBackdrop(c, true);
            var full = c;                 // full-bleed parent (rails and fills that must reach the screen edges)
            c = DesignFrameRect(c);       // everything else is anchored inside the 1920x1080 frame
            // No floor props here: the plates and the vehicle rail use that space.

            // Grouped by area; every element keeps its own edge anchor.
            var header = Group(c, "Header");
            var upgrades = Group(c, "UpgradePlates");
            var rightPanel = Group(c, "RightPanel");

            SmallTitle(header, "GARAGE", out _);
            var back = BackBadge(header);
            CoinCounter(header, TR, TR, new Vector2(-36, -40), out var coinsText);

            // Workshop plates for the four upgrades on the left wall; the door in the middle shows the showcase car.
            var g = root.GetComponent<GarageScreen>();
            string[] names = { "ENGINE", "SUSPENSION", "INK TANK", "BOOST" };
            for (int i = 0; i < 4; i++)
            {
                var plate = ArtImg(upgrades, "Upgrade_" + names[i], SpriteSlot.PlateUpgrade, TL, TL, new Vector2(34, -146 - i * 156), new Vector2(400, 194));
                plate.rectTransform.localScale = new Vector3(0.78f, 0.78f, 1f);   // four plates fit above the card rail
                var t = Label(plate.transform, "Title", names[i], TextStyle.Button, ColorToken.TextPrimary, 32, TextAlignmentOptions.MidlineLeft);
                t.enableAutoSizing = true; t.fontSizeMin = 18; t.fontSizeMax = 32;
                Place(t.rectTransform, TL, TL, new Vector2(30, -22), new Vector2(200, 42));
                var h = Label(plate.transform, "Hint", "", TextStyle.Caption, ColorToken.Accent, 20, TextAlignmentOptions.MidlineRight);
                Place(h.rectTransform, TL, TL, new Vector2(196, -26), new Vector2(176, 34));

                var pips = new Image[5];
                for (int p = 0; p < 5; p++)
                    pips[p] = RawImg(plate.transform, "Pip" + p, SpriteSlot.PipOff, TL, C, new Vector2(200 + (p - 2) * 62, -87), new Vector2(40, 24));

                var up = ArtButton(plate.transform, "UpgradeButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                    "UPGRADE", 30, ColorToken.TextOnAccent, BC, BC, new Vector2(0, -16), new Vector2(262, 102), 0f, new Vector4(52, 22, 20, 18));

                string row = "rows.Array.data[" + i + "]";
                Ref(g, row + ".title", t); Ref(g, row + ".hint", h); Ref(g, row + ".cost", up.label);
                Ref(g, row + ".button", up.button); Ref(g, row + ".buttonImage", up.image);
                Refs(g, row + ".pips", pips);
            }

            // Right wall: the name plate and the main action (equip / unlock).
            var namePlate = ArtImg(rightPanel, "NamePlate", SpriteSlot.PlateName, TR, TR, new Vector2(-34, -170), new Vector2(480, 90));
            var carName = Label(namePlate.transform, "CarName", "", TextStyle.Heading, ColorToken.Accent, 38);
            carName.enableAutoSizing = true; carName.fontSizeMin = 22; carName.fontSizeMax = 38;
            Place(carName.rectTransform, C, C, new Vector2(0, -2), new Vector2(380, 56));
            var action = ArtButton(rightPanel, "ActionButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                "EQUIP", 48, ColorToken.TextOnAccent, TR, TR, new Vector2(-40, -276), new Vector2(480, 187), 0f, new Vector4(100, 40, 40, 34));
            ArtImg(rightPanel, "StickerStar", SpriteSlot.DecoSticker, TR, TR, new Vector2(-24, -262), new Vector2(84, 84), 14f);

            // The vehicle rail runs along the floor.
            var rail = BuildRail(full, "VehicleRail", cardPrefab, BC, BC, new Vector2(0, 6), new Vector2(1920, 290));

            Ref(g, "rail", rail);
            Ref(g, "coinsText", coinsText); Ref(g, "carNameText", carName); Ref(g, "actionLabel", action.label);
            Ref(g, "actionImage", action.image); Ref(g, "actionButton", action.button);
            Ref(g, "backButton", back.button);
            return root;
        }

        private static GameObject BuildHud()
        {
            var root = ScreenRoot<HudScreen>("HudScreen", ShowStyle.Fade, out var c);
            c = DesignFrameRect(c);

            // Grouped by corner so each cluster moves as one thing; every element keeps its own edge anchor.
            var topLeft = Group(c, "TopLeft"); var topCenter = Group(c, "TopCenter"); var topRight = Group(c, "TopRight");
            var bottomLeft = Group(c, "BottomLeft"); var bottomCenter = Group(c, "BottomCenter"); var bottomRight = Group(c, "BottomRight");

            // Top left: three heart badges. Top centre: the odometer. Top right: the stamped coin counter.
            var hearts = new Image[3];
            for (int i = 0; i < 3; i++)
                hearts[i] = RawImg(topLeft, "Heart" + i, SpriteSlot.HeartOn, TL, TL, new Vector2(40 + i * 84, -36), new Vector2(76, 76));

            var odo = ArtImg(topCenter, "Odometer", SpriteSlot.HudOdometer, TC, TC, new Vector2(0, -24), new Vector2(380, 124));
            var distance = Label(odo.transform, "Distance", "0 m", TextStyle.Number, ColorToken.TextPrimary, 60);
            distance.enableAutoSizing = true; distance.fontSizeMin = 30; distance.fontSizeMax = 60;
            Place(distance.rectTransform, C, C, new Vector2(0, 4), new Vector2(320, 70));
            var counterPlate = CoinCounter(topRight, TR, TR, new Vector2(-36, -34), out var coinsText);

            // Bottom left: the speedometer with a needle. Bottom centre: the ink gauge. Bottom right: boost.
            var speedo = ArtImg(bottomLeft, "Speedometer", SpriteSlot.HudSpeedo, BC, BL(), new Vector2(40, 36), new Vector2(240, 240));
            speedo.rectTransform.anchorMin = speedo.rectTransform.anchorMax = Vector2.zero;
            speedo.rectTransform.pivot = Vector2.zero; speedo.rectTransform.anchoredPosition = new Vector2(40, 36);
            var needle = RawImg(speedo.transform, "Needle", SpriteSlot.HudNeedle, C, new Vector2(0.5f, 0.1f), new Vector2(0, -2), new Vector2(18, 92));
            RawImg(speedo.transform, "Hub", SpriteSlot.HudHub, C, C, new Vector2(0, -8), new Vector2(40, 40));
            var speed = Label(speedo.transform, "Speed", "0", TextStyle.Number, ColorToken.TextPrimary, 38);
            Place(speed.rectTransform, C, C, new Vector2(0, -60), new Vector2(160, 46));
            var unit = Label(speedo.transform, "Unit", "KM/H", TextStyle.Caption, ColorToken.Accent, 18);
            Place(unit.rectTransform, C, C, new Vector2(0, -92), new Vector2(120, 26));

            var inkFrame = ArtImg(bottomCenter, "InkFrame", SpriteSlot.InkFrame, BC, BC, new Vector2(0, 30), new Vector2(640, 72));
            var inkBar = Rect("InkBar", inkFrame.transform);
            Place(inkBar, TL, TL, new Vector2(20, -18), new Vector2(600, 30));
            var fill = RawImg(inkBar, "Fill", SpriteSlot.InkFill, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fill.preserveAspect = false;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            var inkLabel = Label(inkBar, "InkLabel", "INK 100%", TextStyle.Caption, ColorToken.TextPrimary, 22);
            Stretch(inkLabel.rectTransform);

            var boost = ArtButton(bottomRight, "BoostButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                "BOOST", 44, ColorToken.TextOnAccent, BR, BR, new Vector2(-40, 36), new Vector2(320, 125), 0f, new Vector4(66, 52, 26, 18));
            var boostCost = Label(boost.rt, "Cost", "5 COINS", TextStyle.Caption, ColorToken.TextOnAccent, 24);
            Place(boostCost.rectTransform, BC, BC, new Vector2(10, 26), new Vector2(220, 30));
            // The free boost meter: fills from air time, flips and coin combos; full means the next boost costs nothing.
            var meterFrame = ArtImg(bottomRight, "BoostMeterFrame", SpriteSlot.InkFrame, BR, BR, new Vector2(-40, 172), new Vector2(320, 36));
            var meter = Rect("BoostMeter", meterFrame.transform);
            Place(meter, TL, TL, new Vector2(10, -9), new Vector2(300, 18));
            var meterFill = RawImg(meter, "Fill", SpriteSlot.PipOn, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            meterFill.preserveAspect = false;
            meterFill.rectTransform.anchorMin = Vector2.zero; meterFill.rectTransform.anchorMax = Vector2.one;
            meterFill.rectTransform.offsetMin = Vector2.zero; meterFill.rectTransform.offsetMax = Vector2.zero;

            // Combo under the odometer, power-up timers above the ink bar, a toast for events, gems under the coin counter.
            var combo = Label(topCenter, "Combo", "", TextStyle.Heading, ColorToken.Accent, 54);
            Place(combo.rectTransform, TC, TC, new Vector2(0, -158), new Vector2(700, 70));
            var power = Label(bottomCenter, "PowerUps", "", TextStyle.Caption, ColorToken.TextPrimary, 28);
            Place(power.rectTransform, BC, BC, new Vector2(0, 112), new Vector2(900, 40));
            var toast = Label(c, "Toast", "", TextStyle.Title, ColorToken.Accent, 64);
            Place(toast.rectTransform, C, C, new Vector2(0, 200), new Vector2(1100, 100));
            var gemsCap = Label(topRight, "GemsCaption", "GEMS", TextStyle.Caption, ColorToken.TextSecondary, 24, TextAlignmentOptions.MidlineRight);
            Place(gemsCap.rectTransform, TR, TR, new Vector2(-120, -150), new Vector2(100, 36));
            var gemsHud = Label(topRight, "GemsText", "0", TextStyle.Number, ColorToken.Accent, 40, TextAlignmentOptions.MidlineRight);
            Place(gemsHud.rectTransform, TR, TR, new Vector2(-40, -146), new Vector2(80, 46));

            var s = root.GetComponent<HudScreen>();
            Ref(s, "distanceText", distance); Ref(s, "speedText", speed); Ref(s, "coinsText", coinsText);
            Refs(s, "hearts", hearts); Ref(s, "inkBar", inkBar); Ref(s, "inkFill", fill.rectTransform);
            Ref(s, "inkLabel", inkLabel); Ref(s, "boostButton", boost.button); Ref(s, "needle", needle.rectTransform);
            Ref(s, "boostMeter", meterFill.rectTransform); Ref(s, "boostCostText", boostCost); Ref(s, "comboText", combo);
            Ref(s, "powerText", power); Ref(s, "toastText", toast); Ref(s, "gemsText", gemsHud);
            return root;
        }

        private static Vector2 BL() => Vector2.zero;

        private static GameObject BuildResult()
        {
            var root = ScreenRoot<ResultScreen>("ResultScreen", ShowStyle.Pop, out var c);
            WallBackdrop(c, false);
            var full = c;                 // full-bleed parent (rails and fills that must reach the screen edges)
            c = DesignFrameRect(c);       // everything else is anchored inside the 1920x1080 frame

            var actions = Group(c, "Actions");
            Props(c, PropMode.Lamps);

            // An award certificate on the wall, with the verdict on its red banner and a wax seal at the bottom.
            var cert = ArtImg(c, "Certificate", SpriteSlot.CertPaper, C, C, new Vector2(-170, 76), new Vector2(920, 640), -1.5f);
            var reason = Label(cert.transform, "Reason", "FELL INTO THE CHASM!", TextStyle.Heading, ColorToken.TextPrimary, 40);
            reason.enableAutoSizing = true; reason.fontSizeMin = 22; reason.fontSizeMax = 40;
            Place(reason.rectTransform, C, C, new Vector2(0, 220), new Vector2(370, 64));
            var cap = Label(cert.transform, "DistanceCaption", "DISTANCE", TextStyle.Caption, ColorToken.TextOnAccent, 30);
            Place(cap.rectTransform, C, C, new Vector2(0, 124), new Vector2(500, 40));
            var dist = Label(cert.transform, "Distance", "0 m", TextStyle.Title, ColorToken.TextOnAccent, 140);
            dist.enableAutoSizing = true; dist.fontSizeMin = 60; dist.fontSizeMax = 140;
            Place(dist.rectTransform, C, C, new Vector2(0, 30), new Vector2(760, 170));

            var badge = new GameObject("NewBestBadge", typeof(RectTransform));
            badge.transform.SetParent(cert.transform, false);
            Place((RectTransform)badge.transform, C, C, new Vector2(250, 36), new Vector2(340, 120));
            badge.transform.localEulerAngles = new Vector3(0, 0, -10f);
            ArtImg(badge.transform, "Stamp", SpriteSlot.StampRecord, C, C, Vector2.zero, new Vector2(340, 120));
            var bt = Label(badge.transform, "BadgeText", "NEW BEST!", TextStyle.Heading, ColorToken.Danger, 46);
            Place(bt.rectTransform, C, C, new Vector2(0, -2), new Vector2(290, 70));

            var counter = ArtImg(cert.transform, "CoinsPlate", SpriteSlot.PlateCounter, C, C, new Vector2(40, -170), new Vector2(360, 104));
            var won = Label(counter.transform, "CoinsWon", "+0", TextStyle.Number, ColorToken.Coin, 52, TextAlignmentOptions.MidlineRight);
            Place(won.rectTransform, C, C, new Vector2(52, 2), new Vector2(210, 70));
            var total = Label(cert.transform, "Total", "TOTAL 0", TextStyle.Caption, ColorToken.TextOnAccent, 28);
            Place(total.rectTransform, C, C, new Vector2(40, -240), new Vector2(400, 40));

            // The bonus board pinned beside the certificate: flips, air time, chests and gems earned in the run.
            var board = ArtImg(c, "BonusBoard", SpriteSlot.PanelBoard, C, C, new Vector2(640, 76), new Vector2(520, 315), 2f);
            var boardTitle = Label(board.transform, "Title", "BONUSES", TextStyle.Heading, ColorToken.Accent, 36);
            Place(boardTitle.rectTransform, C, C, new Vector2(0, 84), new Vector2(380, 50));
            var bonus = Label(board.transform, "Lines", "", TextStyle.Caption, ColorToken.TextPrimary, 26);
            bonus.enableAutoSizing = true; bonus.fontSizeMin = 14; bonus.fontSizeMax = 28; bonus.textWrappingMode = TextWrappingModes.Normal;
            Place(bonus.rectTransform, C, C, new Vector2(0, -2), new Vector2(400, 150));
            var gemsWon = Label(board.transform, "Gems", "", TextStyle.Number, ColorToken.Accent, 34);
            Place(gemsWon.rectTransform, C, C, new Vector2(0, -102), new Vector2(380, 44));

            var retry = ArtButton(actions, "RetryButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                "PLAY AGAIN", 48, ColorToken.TextOnAccent, BC, BC, new Vector2(-250, 30), new Vector2(480, 187), 0f, new Vector4(100, 40, 40, 34));
            var menu = ArtButton(actions, "MenuButton", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate,
                "MENU", 42, ColorToken.TextPrimary, BC, BC, new Vector2(330, 50), new Vector2(340, 127), -3f, new Vector4(30, 30, 30, 30));

            var s = root.GetComponent<ResultScreen>();
            Ref(s, "reasonText", reason); Ref(s, "distanceText", dist); Ref(s, "newBestBadge", badge);
            Ref(s, "coinsWonText", won); Ref(s, "totalText", total); Ref(s, "bonusText", bonus); Ref(s, "gemsText", gemsWon);
            Ref(s, "retryButton", retry.button); Ref(s, "menuButton", menu.button);
            return root;
        }

        private static GameObject BuildRewards()
        {
            var root = ScreenRoot<RewardsScreen>("RewardsScreen", ShowStyle.Fade, out var c);
            WallBackdrop(c, false);
            c = DesignFrameRect(c);
            var header = Group(c, "Header");
            var daily = Group(c, "Daily");
            var missionsGroup = Group(c, "Missions");

            SmallTitle(header, "REWARDS", out _);
            var back = BackBadge(header);
            CoinCounter(header, TR, TR, new Vector2(-36, -40), out var coinsText);
            var gemsCap = Label(header, "GemsCaption", "GEMS", TextStyle.Caption, ColorToken.TextSecondary, 26, TextAlignmentOptions.MidlineRight);
            Place(gemsCap.rectTransform, TR, TR, new Vector2(-190, -166), new Vector2(110, 40));
            var gems = Label(header, "GemsText", "0", TextStyle.Number, ColorToken.Accent, 44, TextAlignmentOptions.MidlineRight);
            Place(gems.rectTransform, TR, TR, new Vector2(-40, -160), new Vector2(140, 52));

            // Left: the daily reward board.
            var board = ArtImg(daily, "DailyBoard", SpriteSlot.PanelBoard, C, C, new Vector2(-500, -50), new Vector2(700, 424), -1.5f);
            var dTitle = Label(board.transform, "Caption", "DAILY REWARD", TextStyle.Heading, ColorToken.TextPrimary, 44);
            Place(dTitle.rectTransform, C, C, new Vector2(0, 128), new Vector2(560, 60));
            var dayText = Label(board.transform, "Day", "DAY 1 OF 7", TextStyle.Number, ColorToken.Accent, 48);
            Place(dayText.rectTransform, C, C, new Vector2(0, 56), new Vector2(560, 60));
            var dReward = Label(board.transform, "Reward", "+50 COINS", TextStyle.Caption, ColorToken.Coin, 34);
            Place(dReward.rectTransform, C, C, new Vector2(0, -4), new Vector2(560, 50));
            var dBtn = ArtButton(board.transform, "ClaimButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                "CLAIM", 40, ColorToken.TextOnAccent, C, C, new Vector2(0, -98), new Vector2(420, 130), 0f, new Vector4(66, 26, 26, 22));

            // Right: three missions for the day.
            var mBoard = ArtImg(missionsGroup, "MissionBoard", SpriteSlot.PanelBoard, C, C, new Vector2(440, -40), new Vector2(1000, 605), 1f);
            var mTitle = Label(mBoard.transform, "Caption", "TODAY'S MISSIONS", TextStyle.Heading, ColorToken.TextPrimary, 44);
            Place(mTitle.rectTransform, C, C, new Vector2(0, 232), new Vector2(700, 60));

            var s = root.GetComponent<RewardsScreen>();
            for (int i = 0; i < 3; i++)
            {
                float y = 120 - i * 140;
                var rowRoot = Rect("Mission" + i, mBoard.transform);
                Place(rowRoot, C, C, new Vector2(0, y), new Vector2(900, 120));
                var t = Label(rowRoot, "Text", "", TextStyle.Button, ColorToken.TextPrimary, 32, TextAlignmentOptions.MidlineLeft);
                t.enableAutoSizing = true; t.fontSizeMin = 18; t.fontSizeMax = 32;
                Place(t.rectTransform, C, C, new Vector2(-190, 22), new Vector2(520, 50));
                var rw = Label(rowRoot, "Reward", "", TextStyle.Caption, ColorToken.Coin, 26, TextAlignmentOptions.MidlineLeft);
                Place(rw.rectTransform, C, C, new Vector2(-190, -24), new Vector2(520, 36));
                var pr = Label(rowRoot, "Progress", "0 / 0", TextStyle.Number, ColorToken.Accent, 36);
                Place(pr.rectTransform, C, C, new Vector2(120, 0), new Vector2(190, 50));
                var cl = ArtButton(rowRoot, "ClaimButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                    "CLAIM", 32, ColorToken.TextOnAccent, C, C, new Vector2(350, 0), new Vector2(210, 96), 0f, new Vector4(48, 20, 20, 18));

                string row = "missions.Array.data[" + i + "]";
                Ref(s, row + ".root", rowRoot.gameObject); Ref(s, row + ".text", t); Ref(s, row + ".progress", pr); Ref(s, row + ".reward", rw);
                Ref(s, row + ".claim", cl.button); Ref(s, row + ".claimLabel", cl.label); Ref(s, row + ".claimImage", cl.image);
            }

            Ref(s, "coinsText", coinsText); Ref(s, "gemsText", gems);
            Ref(s, "dailyTitle", dayText); Ref(s, "dailyReward", dReward);
            Ref(s, "dailyButton", dBtn.button); Ref(s, "dailyButtonLabel", dBtn.label); Ref(s, "dailyButtonImage", dBtn.image);
            Ref(s, "backButton", back.button);
            return root;
        }

        private static GameObject BuildSettings()
        {
            var root = ScreenRoot<SettingsScreen>("SettingsScreen", ShowStyle.Pop, out var c);
            Dim(root.transform, ColorToken.Outline, 0.7f, 0);
            c = DesignFrameRect(c);

            // A workshop control board with a hanging title plate and one big switch.
            var board = ArtImg(c, "Board", SpriteSlot.PanelBoard, C, C, Vector2.zero, new Vector2(860, 520));
            var plate = ArtImg(board.transform, "TitlePlate", SpriteSlot.PlateTitleSmall, TC, TC, new Vector2(0, 52), new Vector2(480, 120));
            var title = Label(plate.transform, "Title", "SETTINGS", TextStyle.Title, ColorToken.TextPrimary, 48);
            title.enableAutoSizing = true; title.fontSizeMin = 28; title.fontSizeMax = 48;
            Place(title.rectTransform, C, C, new Vector2(0, -3), new Vector2(350, 56));

            var soundLabelStatic = Label(board.transform, "SoundCaption", "SOUND", TextStyle.Heading, ColorToken.TextPrimary, 48, TextAlignmentOptions.MidlineLeft);
            Place(soundLabelStatic.rectTransform, C, C, new Vector2(-230, 50), new Vector2(300, 70));
            var sound = ArtButton(board.transform, "SoundButton", SpriteSlot.ToggleOn, SpriteSlot.ToggleOn, SpriteSlot.ToggleOff, "ON", 36,
                ColorToken.TextPrimary, C, C, new Vector2(150, 50), new Vector2(210, 104), 0f, new Vector4(24, 20, 100, 20));
            sound.button.transition = Selectable.Transition.None;
            sound.label.alignment = TextAlignmentOptions.MidlineLeft;
            var close = ArtButton(board.transform, "CloseButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled,
                "DONE", 44, ColorToken.TextOnAccent, BC, BC, new Vector2(0, 30), new Vector2(330, 128), 0f, new Vector4(70, 30, 30, 26));
            var credits = Label(board.transform, "Credits", "Fonts: Lilita One and Baloo 2 (SIL OFL 1.1)", TextStyle.Caption, ColorToken.TextSecondary, 22);
            Place(credits.rectTransform, C, C, new Vector2(0, -62), new Vector2(700, 36));

            var s = root.GetComponent<SettingsScreen>();
            Ref(s, "soundButton", sound.button); Ref(s, "soundButtonImage", sound.image); Ref(s, "soundLabel", sound.label);
            Ref(s, "closeButton", close.button);
            return root;
        }

        private static GameObject BuildCard()
        {
            var go = new GameObject("CollectionCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback), typeof(CollectionCard));
            SceneManager.MoveGameObjectToScene(go, _scene);
            ((RectTransform)go.transform).sizeDelta = new Vector2(300, 224);
            var hit = go.GetComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);                         // invisible, but takes the tap
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = hit;
            btn.transition = Selectable.Transition.None;

            // Everything visible lives in 'Visual' so the selected card can lift without fighting the layout.
            var visual = Rect("Visual", go.transform);
            Place(visual, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 224));
            var bg = RawImg(visual, "Background", SpriteSlot.CardVehicle, C, C, Vector2.zero, new Vector2(300, 224));
            Stretch(bg.rectTransform);
            bg.preserveAspect = false;
            var pv = RawImg(visual, "Preview", SpriteSlot.None, TL, TL, Vector2.zero, new Vector2(200, 100));
            pv.sprite = null;
            var name = Label(visual, "Name", "Name", TextStyle.Button, ColorToken.TextOnAccent, 26);
            name.enableAutoSizing = true; name.fontSizeMin = 16; name.fontSizeMax = 26;
            var badge = Label(visual, "Badge", "", TextStyle.Button, ColorToken.Secondary, 20);
            badge.enableAutoSizing = true; badge.fontSizeMin = 12; badge.fontSizeMax = 20;
            var frame = RawImg(visual, "Frame", SpriteSlot.FrameVehicle, C, C, Vector2.zero, new Vector2(316, 240));
            var lockTag = RawImg(visual, "LockTag", SpriteSlot.LockTag, TR, C, Vector2.zero, new Vector2(54, 70));

            var c = go.GetComponent<CollectionCard>();
            Ref(c, "visual", visual); Ref(c, "background", bg); Ref(c, "preview", pv); Ref(c, "nameText", name);
            Ref(c, "badgeText", badge); Ref(c, "frame", frame); Ref(c, "lockTag", lockTag); Ref(c, "button", btn);
            return go;
        }

        private static GameObject BuildTab()
        {
            var go = new GameObject("CollectionTab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PressFeedback), typeof(CollectionTab));
            SceneManager.MoveGameObjectToScene(go, _scene);
            ((RectTransform)go.transform).sizeDelta = new Vector2(230, 88);
            var bg = go.GetComponent<Image>();
            bg.sprite = ThemeSprite(SpriteSlot.TabNormal); bg.preserveAspect = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            var label = Label(go.transform, "Label", "ALL", TextStyle.Button, ColorToken.TextPrimary, 28);
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 28;
            Stretch(label.rectTransform, 14, 8, 14, 4);

            var t = go.GetComponent<CollectionTab>();
            Ref(t, "background", bg); Ref(t, "label", label); Ref(t, "button", btn);
            return go;
        }

        private static GameObject BuildCollection(CollectionCard cardPrefab, CollectionTab tabPrefab)
        {
            var root = ScreenRoot<CollectionScreen>("CollectionScreen", ShowStyle.Fade, out var c);
            WallBackdrop(c, false);
            var full = c;                 // full-bleed parent (rails and fills that must reach the screen edges)
            c = DesignFrameRect(c);       // everything else is anchored inside the 1920x1080 frame
            Props(c, PropMode.Lamps);

            var header = Group(c, "Header");
            var back = BackBadge(header);
            SmallTitle(header, "VEHICLES", out var title);
            var count = Label(header, "Count", "0 items", TextStyle.Caption, ColorToken.TextSecondary, 28, TextAlignmentOptions.MidlineRight);
            Place(count.rectTransform, TR, TR, new Vector2(-50, -62), new Vector2(360, 50));

            // Group tabs
            var tabs = Rect("Tabs", c);
            Place(tabs, TC, TC, new Vector2(0, -160), new Vector2(1840, 76));
            var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8; tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false; tabLayout.childControlHeight = false;
            tabLayout.childForceExpandWidth = false; tabLayout.childForceExpandHeight = false;

            // Scrolling grid
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(c, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(40, 40); scrollRt.offsetMax = new Vector2(-640, -260);

            var viewport = Rect("Viewport", scrollRt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0);

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(0, 0); content.offsetMax = new Vector2(0, 0);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230, 280); grid.spacing = new Vector2(16, 16);
            grid.padding = new RectOffset(8, 8, 8, 8); grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 30f;

            // Detail panel
            var detail = Img(c, "Detail", SpriteSlot.PanelBoard, ColorToken.Tint);
            detail.rectTransform.anchorMin = new Vector2(1, 0); detail.rectTransform.anchorMax = new Vector2(1, 1);
            detail.rectTransform.pivot = new Vector2(1, 0.5f);
            detail.rectTransform.offsetMin = new Vector2(-600, 40); detail.rectTransform.offsetMax = new Vector2(-40, -260);

            var dBack = Img(detail.transform, "PreviewBack", SpriteSlot.Panel, ColorToken.Background, 0.55f);
            Place(dBack.rectTransform, TC, TC, new Vector2(0, -24), new Vector2(520, 280));
            var dpv = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            dpv.transform.SetParent(dBack.transform, false);
            Stretch((RectTransform)dpv.transform, 12, 12, 12, 12);
            var detailPreview = dpv.GetComponent<Image>();
            detailPreview.preserveAspect = true; detailPreview.raycastTarget = false;

            var dName = Label(detail.transform, "Name", "Name", TextStyle.Heading, null, 40);
            dName.enableAutoSizing = true; dName.fontSizeMin = 24; dName.fontSizeMax = 40;
            Place(dName.rectTransform, TC, TC, new Vector2(0, -324), new Vector2(520, 52));
            var dGroup = Label(detail.transform, "Group", "", TextStyle.Caption, null, 26);
            Place(dGroup.rectTransform, TC, TC, new Vector2(0, -380), new Vector2(520, 34));
            var dStatus = Label(detail.transform, "Status", "", TextStyle.Body, ColorToken.Success, 24);
            dStatus.enableAutoSizing = true; dStatus.fontSizeMin = 16; dStatus.fontSizeMax = 24;
            Place(dStatus.rectTransform, TC, TC, new Vector2(0, -418), new Vector2(520, 36));
            var dNote = Label(detail.transform, "Note", "", TextStyle.Body, ColorToken.TextSecondary, 24, TextAlignmentOptions.Top);
            dNote.enableAutoSizing = true; dNote.fontSizeMin = 16; dNote.fontSizeMax = 24;
            Place(dNote.rectTransform, TC, TC, new Vector2(0, -460), new Vector2(520, 112));
            var dFoot = Label(detail.transform, "Footnote", "", TextStyle.Caption, null, 22);
            Place(dFoot.rectTransform, BC, BC, new Vector2(0, 142), new Vector2(520, 56));
            var select = ArtButton(detail.transform, "SelectButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled, "SELECT", 40, ColorToken.TextOnAccent, BC, BC, new Vector2(0, 34), new Vector2(440, 172), 0f, new Vector4(90, 36, 34, 32));
            select.button.transition = Selectable.Transition.None;

            var s = root.GetComponent<CollectionScreen>();
            Ref(s, "titleText", title); Ref(s, "countText", count); Ref(s, "backButton", back.button);
            Ref(s, "tabsRoot", tabs); Ref(s, "gridRoot", content); Ref(s, "scroll", scroll);
            Ref(s, "tabPrefab", tabPrefab); Ref(s, "cardPrefab", cardPrefab);
            Ref(s, "detailPreview", detailPreview); Ref(s, "detailName", dName); Ref(s, "detailGroup", dGroup);
            Ref(s, "detailStatus", dStatus); Ref(s, "detailNote", dNote); Ref(s, "footnote", dFoot);
            Ref(s, "selectButton", select.button); Ref(s, "selectButtonImage", select.image); Ref(s, "selectLabel", select.label);
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

            string[] names = { "HomeScreen", "WorldsScreen", "GarageScreen", "HudScreen", "ResultScreen", "SettingsScreen", "CollectionScreen" };
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
