using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    // 메뉴와 외부 빌드 요청을 UI Toolkit HUD 생성기로 전달한다.
    [InitializeOnLoad]
    public static class CombatHudPrefabBuilder
    {
        private const string RequestPath = "Temp/BuildCombatHud.request";

        static CombatHudPrefabBuilder()
        {
            EditorApplication.delayCall += RunRequestedBuild;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        [MenuItem("Tools/RPG3D/Build Combat HUD")]
        public static void BuildCombatHud()
        {
            CombatHudToolkitPrefabBuilder.BuildCombatHud();
        }

        private static void RunRequestedBuild()
        {
            if (!File.Exists(RequestPath) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                BuildCombatHud();
                File.Delete(RequestPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += RunRequestedBuild;
            }
        }
    }
}
