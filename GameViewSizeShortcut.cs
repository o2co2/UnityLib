/*
 * 
 * Script to use ALT+1 ALT+2 ALT+3 for setting gameview resolution.
 * If shortcut fail (such as 1280x720) you must add it manually in gameview dropdown first.
 * 
 * Author: Wappen
 * 
 */
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.ShortcutManagement;

namespace Com.Zwmin
{
    public static class GameViewSizeShortcut
    {
        [MenuItem("游戏窗口工具/测试->1280x720")]
        static void _To720()
        {
            GameViewUtils.TrySetSize("1280x720");
        }

        //[Shortcut("Wappen/Switch Game view to 1080p", KeyCode.Alpha2, ShortcutModifiers.Alt)]
        [MenuItem("游戏窗口工具/测试->1920x1080")]
        static void _To1080()
        {
            GameViewUtils.TrySetSize("1920x1080");
        }

        [MenuItem("游戏窗口工具/测试->7680x1080")]
        static void _To1440()
        {
            GameViewUtils.TrySetSize("7680x1080");
        }

        [MenuItem("游戏窗口工具/测试->透明屏:7680x1080")]
        static void _Totmp()
        {
            GameViewUtils.TrySetSizeName(7680, 1080, "透明屏");
        }

        [MenuItem("游戏窗口工具/删除->自定义窗口分辨率")]
        static void _ToRemove()
        {
            GameViewUtils.RemoveCustomSizes(GameViewSizeGroupType.Standalone);
        }

        [MenuItem("游戏窗口工具/保存->自定义窗口分辨率")]
        static void _ToSave()
        {
            GameViewUtils.SaveCustomSizes();
        }

        [MenuItem("游戏窗口工具/测试")]
        static void _ToTest()
        {
            // GameViewUtils.SaveCustomSizes();
        }


    }

    // From https://answers.unity.com/questions/956123/add-and-select-game-view-resolution.html
    // https://www.programmersought.com/article/8025486338/

    public static class GameViewUtils
    {
        static object s_GameViewSizes_instance;

        static Type s_GameViewType;
        static MethodInfo s_GameView_SizeSelectionCallback;

        static Type s_GameViewSizesType;
        static MethodInfo s_GameViewSizes_GetGroup;

        static Type s_GameViewSizeSingleType;



        static GameViewUtils()
        {
            s_GameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            s_GameView_SizeSelectionCallback = s_GameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
            s_GameViewSizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            s_GameViewSizeSingleType = typeof(ScriptableSingleton<>).MakeGenericType(s_GameViewSizesType);
            s_GameViewSizes_GetGroup = s_GameViewSizesType.GetMethod("GetGroup");

            var instanceProp = s_GameViewSizeSingleType.GetProperty("instance");
            s_GameViewSizes_instance = instanceProp.GetValue(null, null);
        }

        /// <summary>
        /// Try to find and set game view size to specified query.
        /// Size must be already exists in game view setting.
        /// </summary>
        /// <param name="sizeText">Query string such as 1280x720 or 16:9</param>
        public static bool TrySetSize(string sizeText)
        {
            int foundIndex = FindSize(GameViewSizeGroupType.Standalone, sizeText);
            if (foundIndex < 0)
            {
                //Debug.LogError($"Size {sizeText} was not found in game view settings");
                string[] pixstrs= sizeText.Split("x");
                if(pixstrs.Length==2)
                {
                    int pixw = int.Parse( pixstrs[0]);
                    int pixh = int.Parse( pixstrs[1]);
                    AddCustomSize(GameViewSizeType.FixedResolution, GameViewSizeGroupType.Standalone, pixw, pixh, "自定义"+ sizeText);
                    TrySetSize(sizeText);
                }
                return true;
            }

            SetSizeIndex(foundIndex);
            return true;
        }

        /// <summary>
        /// Try to find and set game view size to specified query.
        /// Size must be already exists in game view setting.
        /// </summary>
        /// <param name="sizeText">Query string such as 1280x720 or 16:9</param>
        public static bool TrySetSizeName(int w,int h,string name)
        {
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            EditorWindow currentWindow = EditorWindow.GetWindow(type);
            SceneView lastSceneView = SceneView.lastActiveSceneView;

            string sizeText = w.ToString() + "x" + h.ToString();
            int foundIndex = FindSize(GameViewSizeGroupType.Standalone, sizeText);
            if (foundIndex < 0)
            {
                //Debug.LogError($"Size {sizeText} was not found in game view settings");
                AddCustomSize(GameViewSizeType.FixedResolution, GameViewSizeGroupType.Standalone, w, h, name + ":" + sizeText);
                //TrySetSizeName(w,h,name);
                foundIndex = FindSize(GameViewSizeGroupType.Standalone, sizeText);

                //return true;
            }

            EditorWindow gv = EditorWindow.GetWindow(s_GameViewType);
            s_GameView_SizeSelectionCallback.Invoke(gv, new object[] { foundIndex, null });

            if (lastSceneView != null)
                lastSceneView.Focus();

            if (currentWindow != null)
                currentWindow.Focus();
            return true;
        }


        /// <summary>
        /// 设置当前游戏视图大小为目标分辨率指数。
        /// 索引必须事先知道。
        /// </summary>
        public static void SetSizeIndex(int index)
        {
            // 调用GameView。SizeSelectionCallback也会自动聚焦游戏视图，
            // 如果是别的事情，我们会恢复专注
            //EditorWindow currentWindow = EditorWindow.focusedWindow;

            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            EditorWindow currentWindow = EditorWindow.GetWindow(type);
            SceneView lastSceneView = SceneView.lastActiveSceneView;

            EditorWindow gv = EditorWindow.GetWindow(s_GameViewType);
            s_GameView_SizeSelectionCallback.Invoke(gv, new object[] { index, null });

            // Hack, 会模拟激活场景视图，以防它被激活，
            // 因为EditorWindow。focusedWindow现在可以是检查器
            // 如果场景视图和游戏视图在同一个对接组中，
            // SizeSelectionCallback将切换到游戏视图，而不知道用户是否离开场景视图可见。
            // - 如果最后的活动实际上是游戏视图，它应该被currentWindow修正。专注,没有问题
            // - 如果最后激活的是其他东西，比如检查器的控制台，这将打开场景视图，应该是无害的。
            // 如果您不想要此行为，请将此删除
            if (lastSceneView != null)
                lastSceneView.Focus();

            if (currentWindow != null)
                currentWindow.Focus();
        }

        /// <summary>
        /// Finding text could be fixed resoluation as WxH "1280x720"
        /// or ratio like W:H "16:9"
        /// </summary>
        public static int FindSize(GameViewSizeGroupType sizeGroupType, string text)
        {
            var group = GetGroup(sizeGroupType); // class GameViewSizeGroup
            var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
            var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
            for (int i = 0; i < displayTexts.Length; i++)
            {
                string display = displayTexts[i];

                bool found = display.Contains(text);
                if (found)
                    return i;
            }
            return -1;
        }

        static object GetGroup(GameViewSizeGroupType type)
        {
            return s_GameViewSizes_GetGroup.Invoke(s_GameViewSizes_instance, new object[] { (int)type });
        }

        public enum GameViewSizeType
        {
            AspectRatio, FixedResolution
        }

        public static void SaveCustomSizes()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            var saveToHDD = sizesType.GetMethod("SaveToHDD");
            var instance = instanceProp.GetValue(null, null);

            saveToHDD.Invoke(instance, null);
        }

        public static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
        {
            var group = GetGroup(sizeGroupType);
            var addCustomSize = s_GameViewSizes_GetGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
            var gvsType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
            string assemblyName = "UnityEditor.dll";
            Assembly assembly = Assembly.Load(assemblyName);
            Type gameViewSize = assembly.GetType("UnityEditor.GameViewSize");
            Type gameViewSizeType = assembly.GetType("UnityEditor.GameViewSizeType");
            ConstructorInfo ctor = gameViewSize.GetConstructor(new Type[]{ gameViewSizeType, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
            addCustomSize.Invoke(group, new object[] { newSize });
        }


        public static void RemoveCustomSizes(GameViewSizeGroupType sizeGroupType)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProp = singleType.GetProperty("instance");
            var getGroup = sizesType.GetMethod("GetGroup");
            var instance = instanceProp.GetValue(null, null);
            var group = getGroup.Invoke(instance, new object[] { (int)sizeGroupType });

            var getCustomCount = getGroup.ReturnType.GetMethod("GetCustomCount"); // or group.GetType(). 
            var getBuiltinCount = getGroup.ReturnType.GetMethod("GetBuiltinCount"); // or group.GetType(). 
            var removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize"); // or group.GetType(). 

            int customCount = (int)getCustomCount.Invoke(group, null);
            int builtinCount = (int)getBuiltinCount.Invoke(group, null);


            Debug.Log("RemoveCustomSizes for Group: " + sizeGroupType.ToString() + "  Count: " + customCount + "  BuiltIn: " + builtinCount);

            for (int i = 0; i < customCount; i++)
            {
                removeCustomSize.Invoke(group, new object[] { builtinCount + (customCount - i - 1) });
            }
        }

    }

}
