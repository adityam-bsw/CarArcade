using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoadBuilderGame.UI;

namespace RoadBuilderGame.Tests
{
    /// <summary>QA pass 1 UI fixes: pause model state, the pause screen prefab, offer plates that fit their text, and the legacy upgrade migration.</summary>
    public class UiQaFixTests
    {
        private const string Screens = "Assets/Prefabs/UI/Screens/";

        private static GameObject Prefab(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(Screens + name + ".prefab");
            Assert.IsNotNull(go, name + " prefab missing: run Road Builder > UI > Build Screen Prefabs");
            return go;
        }

        [Test]
        public void Pause_Model_RaisesOnlyOnChange()
        {
            var m = new GameModel();
            int raised = 0;
            m.PausedChanged += () => raised++;
            Assert.IsFalse(m.Paused);
            m.SetPaused(true); m.SetPaused(true);
            Assert.IsTrue(m.Paused);
            Assert.AreEqual(1, raised);
            m.SetPaused(false);
            Assert.AreEqual(2, raised);
        }

        [Test]
        public void PauseScreen_Prefab_IsAPopupWithAllButtonsWired()
        {
            var go = Prefab("PauseScreen");
            var screen = go.GetComponent<PauseScreen>();
            Assert.IsNotNull(screen);
            Assert.IsTrue(screen.IsPopup, "the HUD stays visible under the pause popup");
            Assert.IsTrue(screen.BackCloses);
            var so = new SerializedObject(screen);
            foreach (var f in new[] { "resumeButton", "quitButton", "soundButton", "soundButtonImage", "soundLabel" })
                Assert.IsNotNull(so.FindProperty(f).objectReferenceValue, f);
        }

        [Test]
        public void Hud_Prefab_HasPauseButton_AndBackPauses()
        {
            var go = Prefab("HudScreen");
            var hud = go.GetComponent<HudScreen>();
            Assert.IsTrue(hud.BackCloses, "Android Back on the HUD opens the pause popup");
            Assert.IsNotNull(new SerializedObject(hud).FindProperty("pauseButton").objectReferenceValue);
        }

        [Test]
        public void Home_BackAsksBeforeExit()
        {
            Assert.IsTrue(Prefab("HomeScreen").GetComponent<HomeScreen>().BackCloses);
        }

        [Test]
        public void Result_OfferPlates_FitTheirLabels()
        {
            var go = Prefab("ResultScreen");
            var screen = go.GetComponent<ResultScreen>();
            var so = new SerializedObject(screen);
            var cases = new[] { ("doubleButton", "2X COINS (2 LEFT)"), ("reviveButton", "REVIVE"), ("extraDailyButton", "EXTRA DAILY") };
            foreach (var (field, line) in cases)
            {
                var button = (Button)so.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(button, field);
                var plate = (RectTransform)button.transform;
                var label = button.GetComponentInChildren<TMP_Text>(true);
                var lr = label.rectTransform;
                label.text = line + "\n<size=62%>OPTIONAL AD</size>";
                label.enableAutoSizing = false;
                label.fontSize = 20f;                           // even at a comfortable 20 px the first line must fit its box without wrapping
                label.ForceMeshUpdate();
                float room = plate.rect.width + lr.offsetMin.x + lr.offsetMax.x;       // label box inside the plate (offsets are the padding)
                Assert.LessOrEqual(label.GetPreferredValues(line).x, room, field + ": the offer text must fit on its plate");
                Assert.GreaterOrEqual(plate.rect.width, 300f, field + " plate is no longer 210 px wide");
            }
        }

        [Test]
        public void LegacyOneBasedUpgradeLevels_MigrateWithoutLosingPurchases()
        {
            // An old save stored 1..5 (1 = nothing bought). Each purchase must still show: level 3 means two upgrades owned.
            var json = "{\"version\":1,\"engineLevel\":3,\"suspensionLevel\":1,\"inkTankLevel\":5,\"boostLevel\":2}";
            var p = JsonUtility.FromJson<PlayerProfile>(json);
            p.Sanitize();
            Assert.AreEqual(new[] { 2, 0, 4, 1, 0 }, p.UpgradesFor("vehicle-car").levels);
            // The migrated entry is then stored as is (no second -1 on the next load).
            var again = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(p));
            again.Sanitize();
            Assert.AreEqual(new[] { 2, 0, 4, 1, 0 }, again.UpgradesFor("vehicle-car").levels);
        }
    }
}
