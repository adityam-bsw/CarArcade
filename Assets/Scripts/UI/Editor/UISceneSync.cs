using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoadBuilderGame.UI.Editor
{
    /// <summary>
    /// Puts every screen prefab into the Main scene as a real, editable object under UIRoot/SafeArea, so the UI lives in the
    /// scene (hierarchy and Scene view) and is not created while the game runs. Screens start inactive; enable one to edit it.
    /// ScreenStack uses these instances as they are. Safe to re-run: existing instances are kept (their overrides too).
    /// </summary>
    public static class UISceneSync
    {
        private const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem("Road Builder/UI/Place Screens In Main Scene")]
        public static void PlaceScreens()
        {
            var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            var rootGo = GameObject.Find("UIRoot");
            if (rootGo == null) { Debug.LogError("[UISceneSync] No UIRoot in " + MainScene); return; }
            var stack = rootGo.GetComponentInChildren<ScreenStack>(true);
            if (stack == null) { Debug.LogError("[UISceneSync] No ScreenStack under UIRoot"); return; }
            var parent = stack.transform;

            string[] names = { "HomeScreen", "GarageScreen", "WorldsScreen", "HudScreen", "ResultScreen", "SettingsScreen", "CollectionScreen", "RewardsScreen", "PauseScreen" };
            int added = 0;
            foreach (var n in names)
            {
                string path = UIPrefabBuilder.ScreensDir + "/" + n + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (parent.Find(n) != null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                inst.name = n;
                inst.SetActive(false);
                added++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UISceneSync] Added " + added + " screen(s) to " + MainScene + " under " + parent.name);
        }
    }
}
