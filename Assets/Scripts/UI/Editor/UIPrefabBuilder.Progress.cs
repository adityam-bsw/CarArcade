using TMPro;
using UnityEngine;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>The progression hub screen prefab (tabs and rows), built from the same blocks as the other screens. Part of UIPrefabBuilder.BuildAll.</summary>
    public static partial class UIPrefabBuilder
    {
        private static GameObject BuildProgressHub()
        {
            var root = ScreenRoot<ProgressHubScreen>("ProgressHubScreen", ShowStyle.Fade, out var c);
            WallBackdrop(c, false);
            c = DesignFrameRect(c);
            var header = Group(c, "Header");
            var tabsGroup = Group(c, "Tabs");
            var board = ArtImg(c, "Board", SpriteSlot.PanelBoard, TC, TC, new Vector2(0, -292), new Vector2(1780, 690));

            SmallTitle(header, "PROGRESS", out var title);
            var back = BackBadge(header);
            CoinCounter(header, TR, TR, new Vector2(-36, -40), out var coinsText);
            var gemPill = Img(header, "GemsPill", SpriteSlot.Pill, ColorToken.Background, 0.85f);
            Place(gemPill.rectTransform, TR, TR, new Vector2(-36, -156), new Vector2(220, 66));
            var gemIcon = Icon(gemPill.transform, "GemIcon", _gem, new Vector2(62, 62), ColorToken.Accent, tint: false);
            Place(gemIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, -6), new Vector2(62, 62));
            var gems = Label(gemPill.transform, "GemsText", "0", TextStyle.Number, ColorToken.Accent, 44, TextAlignmentOptions.MidlineRight);
            gems.enableAutoSizing = true; gems.fontSizeMin = 24; gems.fontSizeMax = 44; gems.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(gems.rectTransform, 74, 2, 26, 2);
            var scrap = Label(header, "ScrapText", "SCRAP 0", TextStyle.Button, ColorToken.TextPrimary, 28, TextAlignmentOptions.MidlineRight);
            Place(scrap.rectTransform, TR, TR, new Vector2(-48, -232), new Vector2(300, 40));

            var subtitle = Label(header, "Subtitle", "", TextStyle.Heading, ColorToken.TextPrimary, 34);
            subtitle.enableAutoSizing = true; subtitle.fontSizeMin = 20; subtitle.fontSizeMax = 34; subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            Place(subtitle.rectTransform, TC, TC, new Vector2(0, -176), new Vector2(1100, 44));

            var s = root.GetComponent<ProgressHubScreen>();
            for (int i = 0; i < 8; i++)
            {
                var tb = ArtButton(tabsGroup, "Tab" + i, SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "", 26,
                    ColorToken.TextPrimary, TC, TC, new Vector2((i - 3.5f) * 224f, -214), new Vector2(214, 76), 0f, new Vector4(16, 14, 16, 12), manualSprite: true);
                tb.label = Label(tb.rt, "Label", "", TextStyle.Button, ColorToken.TextPrimary, 26);
                tb.label.enableAutoSizing = true; tb.label.fontSizeMin = 14; tb.label.fontSizeMax = 26; tb.label.textWrappingMode = TextWrappingModes.NoWrap;
                Stretch(tb.label.rectTransform, 16, 14, 16, 12);
                string row = "tabs.Array.data[" + i + "]";
                Ref(s, row + ".button", tb.button); Ref(s, row + ".image", tb.image); Ref(s, row + ".label", tb.label);
            }

            const int Rows = 7;
            for (int i = 0; i < Rows; i++)
            {
                var rr = Rect("Row" + i, board.transform);
                Place(rr, C, C, new Vector2(0, 258 - i * 88), new Vector2(1600, 82));
                var plate = Img(rr, "Plate", SpriteSlot.Pill, ColorToken.Background, 0.55f);
                Stretch(plate.rectTransform);
                var t = Label(rr, "Title", "", TextStyle.Button, ColorToken.TextPrimary, 34, TextAlignmentOptions.MidlineLeft);
                t.enableAutoSizing = true; t.fontSizeMin = 18; t.fontSizeMax = 34; t.textWrappingMode = TextWrappingModes.NoWrap;
                Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(560, 60));
                var d = Label(rr, "Detail", "", TextStyle.Button, ColorToken.TextSecondary, 26, TextAlignmentOptions.MidlineLeft);
                d.enableAutoSizing = true; d.fontSizeMin = 14; d.fontSizeMax = 26; d.textWrappingMode = TextWrappingModes.Normal;
                Place(d.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(600, 0), new Vector2(720, 76));
                var btn = ArtButton(rr, "ActionButton", SpriteSlot.ButtonArcade, SpriteSlot.ButtonArcadePressed, SpriteSlot.ButtonArcadeDisabled, "GO", 28,
                    ColorToken.TextOnAccent, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(230, 76), 0f, new Vector4(36, 14, 18, 12), manualSprite: true);
                string row = "rows.Array.data[" + i + "]";
                Ref(s, row + ".root", rr.gameObject); Ref(s, row + ".title", t); Ref(s, row + ".detail", d);
                Ref(s, row + ".button", btn.button); Ref(s, row + ".buttonLabel", btn.label); Ref(s, row + ".buttonImage", btn.image);
            }

            var footer = Label(c, "Footer", "", TextStyle.Button, ColorToken.TextPrimary, 26);
            footer.enableAutoSizing = true; footer.fontSizeMin = 14; footer.fontSizeMax = 26; footer.textWrappingMode = TextWrappingModes.NoWrap;
            Place(footer.rectTransform, BC, BC, new Vector2(0, 34), new Vector2(1000, 44));
            var prev = ArtButton(c, "PrevButton", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "< PREV", 28, ColorToken.TextPrimary,
                BC, BC, new Vector2(-640, 14), new Vector2(230, 80), 0f, new Vector4(24, 20, 24, 18));
            var next = ArtButton(c, "NextButton", SpriteSlot.ButtonPlate, SpriteSlot.ButtonPlatePressed, SpriteSlot.ButtonPlate, "NEXT >", 28, ColorToken.TextPrimary,
                BC, BC, new Vector2(640, 14), new Vector2(230, 80), 0f, new Vector4(24, 20, 24, 18));

            Ref(s, "coinsText", coinsText); Ref(s, "gemsText", gems); Ref(s, "scrapText", scrap); Ref(s, "titleText", title);
            Ref(s, "subtitleText", subtitle); Ref(s, "footerText", footer);
            Ref(s, "backButton", back.button); Ref(s, "prevButton", prev.button); Ref(s, "nextButton", next.button);
            return root;
        }
    }
}
