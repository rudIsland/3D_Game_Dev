using System;
using GameUI.CombatHud;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace EditorTools
{
    public static class CombatHudToolkitPrefabBuilder
    {
        private const string HudFolder = "Assets/_Project/GUI/CombatHud";
        private const string PrefabPath = HudFolder + "/CombatHud.prefab";
        private const string UiFolder =
            "Assets/_Project/UI/CombatHud";
        private const string UxmlPath = UiFolder + "/CombatHud.uxml";
        private const string PanelSettingsPath =
            UiFolder + "/CombatHudPanelSettings.asset";

        public static void BuildCombatHud()
        {
            AssetDatabase.Refresh();

            VisualTreeAsset visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                throw new InvalidOperationException(
                    "Combat HUD UXML could not be loaded: " + UxmlPath);
            }

            PanelSettings panelSettings = GetOrCreatePanelSettings();
            GameObject root = new GameObject(
                "CombatHud",
                typeof(CombatHudController),
                typeof(UIDocument),
                typeof(CombatHudToolkitView));

            try
            {
                UIDocument document = root.GetComponent<UIDocument>();
                document.panelSettings = panelSettings;
                document.visualTreeAsset = visualTree;
                document.sortingOrder = 10;

                CombatHudToolkitView toolkitView =
                    root.GetComponent<CombatHudToolkitView>();
                toolkitView.ConnectForEditor(document);

                CombatHudController controller =
                    root.GetComponent<CombatHudController>();
                controller.ConnectToolkitForEditor(toolkitView);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "Combat HUD prefab was rebuilt with UI Toolkit.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            PanelSettings panelSettings =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(
                    panelSettings,
                    PanelSettingsPath);
            }

            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode =
                PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 10;
            EditorUtility.SetDirty(panelSettings);
            return panelSettings;
        }
    }
}
