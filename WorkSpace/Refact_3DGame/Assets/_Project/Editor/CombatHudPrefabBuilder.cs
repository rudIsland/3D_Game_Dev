using System;
using System.IO;
using rudIsland.RPG3D.UI;
using rudIsland.RPG3D.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace rudIsland.RPG3D.Editor
{
    [InitializeOnLoad]
    public static class CombatHudPrefabBuilder
    {
        private const string RequestPath = "Temp/BuildCombatHud.request"; // 내부에서 사용하는 값
        private const string GuiFolder = "Assets/_Project/GUI"; // 내부에서 사용하는 값
        private const string HudFolder = GuiFolder + "/CombatHud"; // 내부에서 사용하는 값
        private const string SpriteFolder = HudFolder + "/Sprites"; // 내부에서 사용하는 값
        private const string PrefabPath = HudFolder + "/CombatHud.prefab"; // 내부에서 사용하는 값
        private const string CharacterTestScenePath = // 내부에서 사용하는 값
            "Assets/_Project/Scenes/Dev/CharacterTestScene.unity";
        private const string GameScenePath = // 내부에서 사용하는 값
            "Assets/_Project/Scenes/GameScene.unity";

        static CombatHudPrefabBuilder()
        {
            EditorApplication.delayCall += RunRequestedBuild;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        [MenuItem("Tools/RPG3D/Build Combat HUD")]
        public static void BuildCombatHud()
        {
            EnsureFolders();
            ConfigureCopiedSprites();

            Sprite frameSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    SpriteFolder + "/Hp_frame.png");
            Sprite fillSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    SpriteFolder + "/Hp_line.png");

            if (frameSprite == null || fillSprite == null)
            {
                throw new InvalidOperationException(
                    "Combat HUD copied sprites could not be loaded.");
            }

            GameObject prefab = CreatePrefab(frameSprite, fillSprite);
            ApplyPrefabToScene(
                CharacterTestScenePath,
                prefab,
                false);
            ApplyPrefabToScene(
                GameScenePath,
                prefab,
                true);

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Combat HUD prefab and scene instances were built from copied sprites.");
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

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project", "GUI");
            EnsureFolder(GuiFolder, "CombatHud");
            EnsureFolder(HudFolder, "Sprites");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void ConfigureCopiedSprites()
        {
            ConfigureSprite(
                SpriteFolder + "/Hp_frame.png",
                new Vector4(20f, 20f, 20f, 20f));
            ConfigureSprite(
                SpriteFolder + "/Hp_line.png",
                Vector4.zero);
            ConfigureSprite(
                SpriteFolder + "/big_bar_bg.png",
                Vector4.zero);
            ConfigureSprite(
                SpriteFolder + "/big_bar.png",
                Vector4.zero);
            ConfigureSprite(
                SpriteFolder + "/big_bar_frame.png",
                new Vector4(96f, 96f, 96f, 96f));
        }

        private static void ConfigureSprite(
            string assetPath,
            Vector4 border)
        {
            var importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Missing copied GUI image: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static GameObject CreatePrefab(
            Sprite frameSprite,
            Sprite fillSprite)
        {
            var root = new GameObject(
                "CombatHud",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CombatHudController));

            try
            {
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;

                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                HealthBarView playerBar = CreateHealthBar(
                    root.transform,
                    "PlayerHealthBar",
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(48f, 48f),
                    new Vector2(480f, 76f),
                    frameSprite,
                    fillSprite,
                    20);

                StaminaBarView staminaBar = CreateStaminaBar(
                    root.transform,
                    frameSprite,
                    fillSprite);

                HealthBarView enemyBar = CreateHealthBar(
                    root.transform,
                    "EnemyHealthBar",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -42f),
                    new Vector2(760f, 88f),
                    frameSprite,
                    fillSprite,
                    24);

                StaggerBarView staggerBar = CreateStaggerBar(
                    root.transform,
                    frameSprite,
                    fillSprite);

                root.GetComponent<CombatHudController>().ConnectForEditor(
                    null,
                    playerBar,
                    staminaBar,
                    enemyBar,
                    staggerBar);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static HealthBarView CreateHealthBar(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            Sprite frameSprite,
            Sprite fillSprite,
            int fontSize)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(HealthBarView));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetRect(
                rootRect,
                anchorMin,
                anchorMax,
                pivot,
                position,
                size);

            GameObject bar = CreateRectChild(root.transform, "Bar");
            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, 42f);

            Image background = bar.AddComponent<Image>();
            background.color = new Color(0.035f, 0.02f, 0.02f, 0.96f);
            background.raycastTarget = false;

            GameObject fillObject =
                CreateStretchChild(bar.transform, "HealthFill", 11f, 9f);
            Image fill = fillObject.AddComponent<Image>();
            fill.sprite = fillSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            GameObject frameObject =
                CreateStretchChild(bar.transform, "Frame", 0f, 0f);
            Image frame = frameObject.AddComponent<Image>();
            frame.sprite = frameSprite;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            Text healthText = CreateText(
                bar.transform,
                "HealthValue",
                fontSize,
                TextAnchor.MiddleCenter,
                Color.white);
            Stretch(healthText.rectTransform, 0f, 0f);

            Text nameText = CreateText(
                root.transform,
                "TargetName",
                fontSize,
                TextAnchor.MiddleCenter,
                new Color(0.9f, 0.78f, 0.56f, 1f));
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = new Vector2(0f, 30f);

            HealthBarView view = root.GetComponent<HealthBarView>();
            view.ConnectForEditor(root, fill, healthText, nameText);
            return view;
        }

        private static StaminaBarView CreateStaminaBar(
            Transform parent,
            Sprite frameSprite,
            Sprite fillSprite)
        {
            var root = new GameObject(
                "PlayerStaminaBar",
                typeof(RectTransform),
                typeof(StaminaBarView));
            root.transform.SetParent(parent, false);

            SetRect(
                root.GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(48f, 16f),
                new Vector2(360f, 24f));

            GameObject bar =
                CreateStretchChild(root.transform, "Bar", 0f, 0f);
            Image background = bar.AddComponent<Image>();
            background.color = new Color(0.02f, 0.035f, 0.015f, 0.96f);
            background.raycastTarget = false;

            GameObject fillObject =
                CreateStretchChild(bar.transform, "StaminaFill", 8f, 6f);
            Image fill = fillObject.AddComponent<Image>();
            fill.sprite = fillSprite;
            fill.color = new Color(0.6f, 0.82f, 0.2f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            GameObject frameObject =
                CreateStretchChild(bar.transform, "Frame", 0f, 0f);
            Image frame = frameObject.AddComponent<Image>();
            frame.sprite = frameSprite;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            StaminaBarView view = root.GetComponent<StaminaBarView>();
            view.ConnectForEditor(root, fill);
            return view;
        }

        private static StaggerBarView CreateStaggerBar(
            Transform parent,
            Sprite frameSprite,
            Sprite fillSprite)
        {
            var root = new GameObject(
                "EnemyStaggerBar",
                typeof(RectTransform),
                typeof(StaggerBarView));
            root.transform.SetParent(parent, false);

            SetRect(
                root.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -136f),
                new Vector2(600f, 28f));

            GameObject bar =
                CreateStretchChild(root.transform, "Bar", 0f, 0f);
            Image background = bar.AddComponent<Image>();
            background.color = new Color(0.04f, 0.03f, 0.015f, 0.96f);
            background.raycastTarget = false;

            GameObject fillObject =
                CreateStretchChild(bar.transform, "StaggerFill", 8f, 6f);
            Image fill = fillObject.AddComponent<Image>();
            fill.sprite = fillSprite;
            fill.color = new Color(0.95f, 0.67f, 0.18f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            GameObject frameObject =
                CreateStretchChild(bar.transform, "Frame", 0f, 0f);
            Image frame = frameObject.AddComponent<Image>();
            frame.sprite = frameSprite;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;

            Text staggerText = CreateText(
                bar.transform,
                "StaggerValue",
                18,
                TextAnchor.MiddleCenter,
                Color.white);
            Stretch(staggerText.rectTransform, 0f, 0f);

            StaggerBarView view = root.GetComponent<StaggerBarView>();
            view.ConnectForEditor(root, fill, staggerText);
            return view;
        }

        private static GameObject CreateRectChild(
            Transform parent,
            string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreateStretchChild(
            Transform parent,
            string name,
            float horizontalInset,
            float verticalInset)
        {
            GameObject child = CreateRectChild(parent, name);
            Stretch(
                child.GetComponent<RectTransform>(),
                horizontalInset,
                verticalInset);
            return child;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject =
                CreateRectChild(parent, name);
            Text text = textObject.AddComponent<Text>();
            text.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return text;
        }

        private static void Stretch(
            RectTransform rectTransform,
            float horizontalInset,
            float verticalInset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin =
                new Vector2(horizontalInset, verticalInset);
            rectTransform.offsetMax =
                new Vector2(-horizontalInset, -verticalInset);
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static void ApplyPrefabToScene(
            string scenePath,
            GameObject prefab,
            bool createManagerWhenMissing)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool closeAfterSave = !scene.isLoaded;

            if (closeAfterSave)
            {
                scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Additive);
            }
            else if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Save the open scene before building Combat HUD: " +
                    scenePath);
            }

            try
            {
                GameObject existingHud =
                    FindRootObject(scene, "CombatHud");
                if (existingHud != null)
                {
                    Object.DestroyImmediate(existingHud);
                }

                WorldObjectManager manager =
                    FindComponentInScene<WorldObjectManager>(scene);
                if (manager == null && createManagerWhenMissing)
                {
                    var managerObject =
                        new GameObject("WorldObjectManager");
                    SceneManager.MoveGameObjectToScene(
                        managerObject,
                        scene);
                    manager =
                        managerObject.AddComponent<WorldObjectManager>();
                }

                if (manager == null)
                {
                    throw new InvalidOperationException(
                        "WorldObjectManager is missing in " + scenePath);
                }

                var instance =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        prefab,
                        scene);
                CombatHudController controller =
                    instance.GetComponent<CombatHudController>();
                HealthBarView[] bars =
                    instance.GetComponentsInChildren<HealthBarView>(true);

                HealthBarView playerBar =
                    FindBar(bars, "PlayerHealthBar");
                HealthBarView enemyBar =
                    FindBar(bars, "EnemyHealthBar");
                StaminaBarView staminaBar =
                    instance.GetComponentInChildren<StaminaBarView>(true);
                StaggerBarView staggerBar =
                    instance.GetComponentInChildren<StaggerBarView>(true);

                if (staminaBar == null || staggerBar == null)
                {
                    throw new InvalidOperationException(
                        "Combat HUD resource bars are incomplete.");
                }

                controller.ConnectForEditor(
                    manager,
                    playerBar,
                    staminaBar,
                    enemyBar,
                    staggerBar);
                EditorUtility.SetDirty(controller);
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    controller);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (closeAfterSave && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static HealthBarView FindBar(
            HealthBarView[] bars,
            string name)
        {
            for (int index = 0; index < bars.Length; index++)
            {
                if (bars[index].name == name)
                {
                    return bars[index];
                }
            }

            throw new InvalidOperationException(
                "Missing health bar in prefab: " + name);
        }

        private static GameObject FindRootObject(
            Scene scene,
            string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == name)
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                T component =
                    roots[index].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
