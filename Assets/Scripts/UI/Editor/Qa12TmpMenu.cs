#if UNITY_EDITOR
using System; using System.Reflection; using UnityEditor; using UnityEngine;
namespace RoadBuilderGame.UI.Editor
{
    public static class Qa12TmpMenu
    {
        public static string SetSize(int w, int h)
        {
            var gvType = Type.GetType("UnityEditor.GameView,UnityEditor");
            var gv = EditorWindow.GetWindow(gvType);
            var sizesT = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesT);
            var inst = single.GetProperty("instance").GetValue(null);
            var group = sizesT.GetMethod("GetGroup").Invoke(inst, new object[] { (int)GameViewSizeGroupType.Standalone });
            var gvsT = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var typeEnum = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");
            var sz = gvsT.GetConstructors()[0].Invoke(new object[] { Enum.Parse(typeEnum, "FixedResolution"), w, h, "Q12_" + w + "x" + h });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new object[] { sz });
            int idx = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(gv, new object[] { idx, null });
            return "size idx " + idx;
        }
    }
}
#endif
