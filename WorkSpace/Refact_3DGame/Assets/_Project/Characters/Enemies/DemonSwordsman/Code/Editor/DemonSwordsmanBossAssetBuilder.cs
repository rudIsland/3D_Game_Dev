using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace rudIsland.RPG3D.Editor
{
    // 필요한 원본만 새 GUID로 복제하고 보스용 Animator와 Prefab을 만든다.
    public static class DemonSwordsmanBossAssetBuilder
    {
        private const string SourceRoot = // 내부에서 사용하는 값
            "Assets/_ThirdParty/Demon Swordsman";
        private const string BossRoot = // 내부에서 사용하는 값
            "Assets/_Project/Characters/Enemies/Boss/DemonSwordsman";
        private const string ModelsRoot = BossRoot + "/Models"; // 내부에서 사용하는 값
        private const string MaterialsRoot = BossRoot + "/Materials"; // 내부에서 사용하는 값
        private const string TexturesRoot = BossRoot + "/Textures"; // 내부에서 사용하는 값
        private const string SwordAnimationsRoot = // 내부에서 사용하는 값
            BossRoot + "/Animations/Sword";
        private const string BeastAnimationsRoot = // 내부에서 사용하는 값
            BossRoot + "/Animations/Beast";
        private const string AnimatorRoot = // 애니메이터 참조
            BossRoot + "/Animations/Controllers";
        private const string SettingsRoot = BossRoot + "/Settings"; // 행동 설정 참조
        private const string PrefabsRoot = BossRoot + "/Prefabs"; // 내부에서 사용하는 값

        private const string ModelPath = ModelsRoot + "/base_mesh.fbx"; // 내부에서 사용하는 값
        private const string HandSwordModelPath = // 내부에서 사용하는 값
            ModelsRoot + "/sword_hand.fbx";
        private const string BodyMaterialPath = // 내부에서 사용하는 값
            MaterialsRoot + "/M_body_set_1.mat";
        private const string SwordMaterialPath = // 내부에서 사용하는 값
            MaterialsRoot + "/M_Sword_set_1.mat";
        private const string ControllerPath = // 내부에서 사용하는 값
            AnimatorRoot + "/DemonSwordsmanBoss.controller";
        private const string SettingsPath = // 행동 설정 참조
            SettingsRoot + "/DemonSwordsmanBossSettings.asset";
        private const string PrefabPath = // 내부에서 사용하는 값
            PrefabsRoot + "/DemonSwordsmanBoss.prefab";

        private static readonly string[] TextureFileNames = // 표시 이름
        {
            "T_body_AlbedoTransparency.png",
            "T_body_Normal.png",
            "T_body_SpecularSmoothness.png",
            "T_weapons_AlbedoTransparency.png",
            "T_weapons_Normal.png",
            "T_weapons_SpecularSmoothness.png"
        };

        private static readonly string[] SwordAnimationFileNames = // 표시 이름
        {
            "@anim_Sword_idle_1.FBX",
            "@anim_Sword_Walk_1.FBX",
            "@anim_Sword_Walk_Back.FBX",
            "@anim_Sword_Walk_Left.FBX",
            "@anim_Sword_Walk_Right.FBX",
            "@anim_Sword_run_1.FBX",
            "@anim_Sword_Turn_Left_90.FBX",
            "@anim_Sword_Turn_Right_90.FBX",
            "@anim_Sword_attack_1.FBX",
            "@anim_Sword_attack_2.FBX",
            "@anim_Sword_attack_3.FBX",
            "@anim_Sword_attack_4.FBX",
            "@anim_Sword_attack_5.FBX",
            "@anim_Sword_attack_6.FBX",
            "@anim_Sword_attack_7.FBX",
            "@anim_Sword_Jump.FBX",
            "@anim_Sword_rage.FBX",
            "@anim_Sword_hit_1.FBX",
            "@anim_Sword_hit_2.FBX",
            "@anim_Sword_death.FBX"
        };

        private static readonly string[] BeastAnimationFileNames = // 표시 이름
        {
            "@anim_idle_1.FBX",
            "@anim_run_1.FBX",
            "@anim_walking_left.FBX",
            "@anim_walking_right.FBX",
            "@anim_attack_1.FBX",
            "@anim_attack_2.FBX",
            "@anim_attack_3.FBX",
            "@anim_attack_4.FBX",
            "@anim_attack_5.FBX",
            "@anim_attack_6.FBX",
            "@anim_attack_7.FBX",
            "@anim_attack_8.FBX",
            "@anim_fear.FBX",
            "@anim_rage.FBX",
            "@anim_hit_1.FBX",
            "@anim_hit_2.FBX",
            "@anim_Dying.FBX"
        };

        [MenuItem("Tools/RPG3D/Boss/Build Demon Swordsman")]
        public static void BuildAll()
        {
            EnsureFolders();
            CopyRequiredAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Avatar copiedAvatar = ConfigureCopiedModel();
            ConfigureCopiedAnimationImporters(copiedAvatar);
            ConfigureCombatAnimationEvents();
            ConfigureCopiedMaterials();

            DemonSwordsmanBossSettings settings = CreateSettings();
            AnimatorController controller = CreateAnimatorController();
            CreateBossPrefab(settings, controller, copiedAvatar);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateOwnedReferences();
            Debug.Log(
                "Demon Swordsman 보스 리소스, Animator, Settings, Prefab 생성을 완료했습니다.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                BossRoot,
                ModelsRoot,
                MaterialsRoot,
                TexturesRoot,
                BossRoot + "/Animations",
                SwordAnimationsRoot,
                BeastAnimationsRoot,
                AnimatorRoot,
                SettingsRoot,
                PrefabsRoot
            };

            for (int index = 0; index < folders.Length; index++)
            {
                Directory.CreateDirectory(folders[index]);
            }
        }

        private static void CopyRequiredAssets()
        {
            CopyIfMissing(
                SourceRoot + "/base_mesh/base_mesh.fbx",
                ModelPath);
            CopyIfMissing(
                SourceRoot + "/base_mesh/mesh_separate/sword_hand.fbx",
                HandSwordModelPath);
            CopyIfMissing(
                SourceRoot + "/material/UPR/M_body_set_1.mat",
                BodyMaterialPath);
            CopyIfMissing(
                SourceRoot + "/material/UPR/M_Sword_set_1.mat",
                SwordMaterialPath);

            for (int index = 0; index < TextureFileNames.Length; index++)
            {
                string fileName = TextureFileNames[index];
                CopyIfMissing(
                    SourceRoot + "/texture/UPR/set_1/" + fileName,
                    TexturesRoot + "/" + fileName);
            }

            for (int index = 0; index < SwordAnimationFileNames.Length; index++)
            {
                string fileName = SwordAnimationFileNames[index];
                CopyIfMissing(
                    SourceRoot + "/animation/sword/" + fileName,
                    SwordAnimationsRoot + "/" + fileName);
            }

            for (int index = 0; index < BeastAnimationFileNames.Length; index++)
            {
                string fileName = BeastAnimationFileNames[index];
                CopyIfMissing(
                    SourceRoot + "/animation/monster/" + fileName,
                    BeastAnimationsRoot + "/" + fileName);
            }
        }

        private static void CopyIfMissing(
            string sourcePath,
            string destinationPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null)
            {
                return;
            }

            if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                throw new FileNotFoundException(
                    "복제할 원본 리소스를 찾지 못했습니다.",
                    sourcePath);
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                throw new InvalidOperationException(
                    $"리소스 복제에 실패했습니다: {sourcePath}");
            }
        }

        private static Avatar ConfigureCopiedModel()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;

            if (importer == null)
            {
                throw new InvalidOperationException(
                    "복제한 Demon Swordsman 모델 Importer를 찾지 못했습니다.");
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            Material bodyMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            Material swordMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(SwordMaterialPath);

            foreach (
                KeyValuePair<AssetImporter.SourceAssetIdentifier, Object> entry
                in importer.GetExternalObjectMap())
            {
                if (entry.Key.name.IndexOf(
                        "body",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    importer.AddRemap(entry.Key, bodyMaterial);
                }
                else if (entry.Key.name.IndexOf(
                             "weapon",
                             StringComparison.OrdinalIgnoreCase) >= 0 ||
                         entry.Key.name.IndexOf(
                             "sword",
                             StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    importer.AddRemap(entry.Key, swordMaterial);
                }
            }

            importer.SaveAndReimport();

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Avatar>()
                .FirstOrDefault();

            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                throw new InvalidOperationException(
                    "복제한 본체에서 유효한 Humanoid Avatar를 만들지 못했습니다.");
            }

            return avatar;
        }

        private static void ConfigureCopiedAnimationImporters(Avatar avatar)
        {
            for (int index = 0; index < SwordAnimationFileNames.Length; index++)
            {
                string fileName = SwordAnimationFileNames[index];
                bool loop = fileName.IndexOf(
                    "idle",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileName.IndexOf(
                        "walk",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileName.IndexOf(
                        "run",
                        StringComparison.OrdinalIgnoreCase) >= 0;
                ConfigureAnimationImporter(
                    SwordAnimationsRoot + "/" + fileName,
                    avatar,
                    loop);
            }

            for (int index = 0; index < BeastAnimationFileNames.Length; index++)
            {
                string fileName = BeastAnimationFileNames[index];
                bool loop = fileName.IndexOf(
                    "idle",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileName.IndexOf(
                        "walking",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileName.IndexOf(
                        "run",
                        StringComparison.OrdinalIgnoreCase) >= 0;
                ConfigureAnimationImporter(
                    BeastAnimationsRoot + "/" + fileName,
                    avatar,
                    loop);
            }
        }

        private static void ConfigureAnimationImporter(
            string assetPath,
            Avatar avatar,
            bool loop)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"애니메이션 Importer를 찾지 못했습니다: {assetPath}");
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;

            ModelImporterClipAnimation[] clips =
                importer.defaultClipAnimations;

            for (int index = 0; index < clips.Length; index++)
            {
                clips[index].loopTime = loop;
                clips[index].loopPose = loop;
                clips[index].keepOriginalOrientation = true;
                clips[index].keepOriginalPositionY = true;
                clips[index].keepOriginalPositionXZ = false;
                clips[index].lockRootRotation = false;
                clips[index].lockRootHeightY = true;
                clips[index].lockRootPositionXZ = false;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureCopiedMaterials()
        {
            RemapMaterialTextures(
                AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath),
                "T_body_");
            RemapMaterialTextures(
                AssetDatabase.LoadAssetAtPath<Material>(SwordMaterialPath),
                "T_weapons_");
        }

        private static void ConfigureCombatAnimationEvents()
        {
            SetAttackEvents(
                SwordAnimationsRoot + "/@anim_Sword_attack_1.FBX",
                1,
                false,
                0.32f,
                0.60f);
            SetAttackEvents(
                SwordAnimationsRoot + "/@anim_Sword_attack_2.FBX",
                2,
                true,
                0.30f,
                0.64f);
            SetAttackEvents(
                SwordAnimationsRoot + "/@anim_Sword_attack_4.FBX",
                3,
                false,
                0.52f,
                0.72f);
            SetAttackEvents(
                SwordAnimationsRoot + "/@anim_Sword_attack_5.FBX",
                4,
                false,
                0.40f,
                0.66f);
            SetAttackEvents(
                SwordAnimationsRoot + "/@anim_Sword_Jump.FBX",
                5,
                false,
                0.58f,
                0.78f);
            SetAttackEvents(
                BeastAnimationsRoot + "/@anim_attack_1.FBX",
                6,
                true,
                0.24f,
                0.58f);
            SetAttackEvents(
                BeastAnimationsRoot + "/@anim_attack_4.FBX",
                7,
                false,
                0.50f,
                0.72f);
            SetAttackEvents(
                BeastAnimationsRoot + "/@anim_attack_5.FBX",
                8,
                false,
                0.34f,
                0.68f);
            SetAttackEvents(
                BeastAnimationsRoot + "/@anim_attack_8.FBX",
                9,
                false,
                0.44f,
                0.72f);
        }

        private static void SetAttackEvents(
            string assetPath,
            int hitNumber,
            bool hasBranch,
            float openRatio,
            float closeRatio)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            AnimationClip sourceClip = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip =>
                    !clip.name.StartsWith("__preview__"));

            if (importer == null || sourceClip == null)
            {
                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            for (int index = 0; index < clips.Length; index++)
            {
                var events = new List<AnimationEvent>(2);

                if (hasBranch)
                {
                    events.Add(CreateAnimationEvent(
                        "OpenBranchWindow",
                        sourceClip.length * 0.58f,
                        0));
                }

                events.Add(CreateAnimationEvent(
                    "FinishAction",
                    sourceClip.length * 0.95f,
                    0));
                clips[index].events = events.ToArray();
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationEvent CreateAnimationEvent(
            string functionName,
            float time,
            int intParameter)
        {
            return new AnimationEvent
            {
                functionName = functionName,
                time = time,
                intParameter = intParameter
            };
        }

        private static void RemapMaterialTextures(
            Material material,
            string textureNamePrefix)
        {
            if (material == null)
            {
                return;
            }

            string[] texturePropertyNames =
                material.GetTexturePropertyNames();

            for (int index = 0;
                 index < texturePropertyNames.Length;
                 index++)
            {
                string propertyName = texturePropertyNames[index];
                Texture currentTexture = material.GetTexture(propertyName);

                if (currentTexture == null)
                {
                    continue;
                }

                string sourcePath = AssetDatabase.GetAssetPath(currentTexture);

                if (!sourcePath.StartsWith(SourceRoot, StringComparison.Ordinal))
                {
                    continue;
                }

                string fileName = Path.GetFileName(sourcePath);

                if (!fileName.StartsWith(
                        textureNamePrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                Texture copiedTexture = AssetDatabase.LoadAssetAtPath<Texture>(
                    TexturesRoot + "/" + fileName);
                material.SetTexture(propertyName, copiedTexture);
            }

            EditorUtility.SetDirty(material);
        }

        private static DemonSwordsmanBossSettings CreateSettings()
        {
            DemonSwordsmanBossSettings settings =
                AssetDatabase.LoadAssetAtPath<DemonSwordsmanBossSettings>(
                    SettingsPath);

            if (settings != null)
            {
                settings.SetRuntimeDefaults();
                EditorUtility.SetDirty(settings);
                return settings;
            }

            settings = ScriptableObject.CreateInstance<
                DemonSwordsmanBossSettings>();
            settings.SetRuntimeDefaults();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static AnimatorController CreateAnimatorController()
        {
            if (AssetDatabase.LoadMainAssetAtPath(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            controller.AddParameter(
                "MoveForward",
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                "MoveSide",
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                "MoveAmount",
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                "Action",
                AnimatorControllerParameterType.Int);
            controller.AddParameter(
                "Style",
                AnimatorControllerParameterType.Int);
            controller.AddParameter(
                "AttackKind",
                AnimatorControllerParameterType.Int);

            AnimatorStateMachine root =
                controller.layers[0].stateMachine;
            AnimatorState startState = AddState(
                root,
                "Start",
                GetSwordClip("@anim_Sword_idle_1.FBX"),
                new Vector3(250f, 100f));
            root.defaultState = startState;

            AnimatorStateMachine sword =
                root.AddStateMachine("Sword", new Vector3(500f, 50f));
            AnimatorStateMachine beast =
                root.AddStateMachine("Beast", new Vector3(500f, 350f));

            AnimatorState swordLocomotion = AddLocomotion(
                controller,
                sword,
                "SwordLocomotion",
                new[]
                {
                    Child(GetSwordClip("@anim_Sword_idle_1.FBX"), 0f, 0f),
                    Child(GetSwordClip("@anim_Sword_Walk_1.FBX"), 0f, 0.5f),
                    Child(GetSwordClip("@anim_Sword_run_1.FBX"), 0f, 1f),
                    Child(GetSwordClip("@anim_Sword_Walk_Back.FBX"), 0f, -1f),
                    Child(GetSwordClip("@anim_Sword_Walk_Left.FBX"), -1f, 0f),
                    Child(GetSwordClip("@anim_Sword_Walk_Right.FBX"), 1f, 0f)
                },
                new Vector3(260f, 260f));
            sword.defaultState = swordLocomotion;

            AnimatorState swordTurnLeft = AddState(
                sword,
                "SwordTurnLeft",
                GetSwordClip("@anim_Sword_Turn_Left_90.FBX"),
                new Vector3(500f, 60f));
            AnimatorState swordTurnRight = AddState(
                sword,
                "SwordTurnRight",
                GetSwordClip("@anim_Sword_Turn_Right_90.FBX"),
                new Vector3(720f, 60f));
            AnimatorState swordQuickSlash = AddState(
                sword,
                "SwordQuickSlash",
                GetSwordClip("@anim_Sword_attack_1.FBX"),
                new Vector3(500f, 200f));
            AnimatorState swordComboStart = AddState(
                sword,
                "SwordComboStart",
                GetSwordClip("@anim_Sword_attack_2.FBX"),
                new Vector3(500f, 320f));
            AnimatorState swordComboFinish = AddState(
                sword,
                "SwordComboFinish",
                GetSwordClip("@anim_Sword_attack_3.FBX"),
                new Vector3(720f, 320f));
            AnimatorState swordHeavySlash = AddState(
                sword,
                "SwordHeavySlash",
                GetSwordClip("@anim_Sword_attack_4.FBX"),
                new Vector3(500f, 440f));
            AnimatorState swordChaseSlash = AddState(
                sword,
                "SwordChaseSlash",
                GetSwordClip("@anim_Sword_attack_5.FBX"),
                new Vector3(720f, 440f));
            AddState(
                sword,
                "SwordWideSlash",
                GetSwordClip("@anim_Sword_attack_6.FBX"),
                new Vector3(500f, 560f));
            AnimatorState swordJumpAttack = AddState(
                sword,
                "SwordJumpAttack",
                GetSwordClip("@anim_Sword_Jump.FBX"),
                new Vector3(720f, 560f));
            AnimatorState swordHit = AddState(
                sword,
                "SwordHit",
                GetSwordClip("@anim_Sword_hit_1.FBX"),
                new Vector3(500f, 700f));

            AnimatorState beastLocomotion = AddLocomotion(
                controller,
                beast,
                "BeastLocomotion",
                new[]
                {
                    Child(GetBeastClip("@anim_idle_1.FBX"), 0f, 0f),
                    Child(GetBeastClip("@anim_run_1.FBX"), 0f, 1f),
                    Child(GetBeastClip("@anim_walking_left.FBX"), -1f, 0f),
                    Child(GetBeastClip("@anim_walking_right.FBX"), 1f, 0f)
                },
                new Vector3(260f, 260f));
            beast.defaultState = beastLocomotion;

            AnimatorState beastComboStart = AddState(
                beast,
                "BeastComboStart",
                GetBeastClip("@anim_attack_1.FBX"),
                new Vector3(500f, 180f));
            AnimatorState beastComboSecond = AddState(
                beast,
                "BeastComboSecond",
                GetBeastClip("@anim_attack_2.FBX"),
                new Vector3(720f, 180f));
            AnimatorState beastComboFinish = AddState(
                beast,
                "BeastComboFinish",
                GetBeastClip("@anim_attack_3.FBX"),
                new Vector3(940f, 180f));
            AnimatorState beastSlam = AddState(
                beast,
                "BeastSlam",
                GetBeastClip("@anim_attack_4.FBX"),
                new Vector3(500f, 340f));
            AnimatorState beastRush = AddState(
                beast,
                "BeastRush",
                GetBeastClip("@anim_attack_5.FBX"),
                new Vector3(720f, 340f));
            AnimatorState beastWideAttack = AddState(
                beast,
                "BeastWideAttack",
                GetBeastClip("@anim_attack_8.FBX"),
                new Vector3(940f, 340f));
            AnimatorState beastHit = AddState(
                beast,
                "BeastHit",
                GetBeastClip("@anim_hit_1.FBX"),
                new Vector3(500f, 500f));

            AnimatorState phaseFear = AddState(
                root,
                "PhaseChangeFear",
                GetBeastClip("@anim_fear.FBX"),
                new Vector3(750f, 200f));
            AnimatorState phaseRage = AddState(
                root,
                "PhaseChangeRage",
                GetBeastClip("@anim_rage.FBX"),
                new Vector3(950f, 200f));
            AnimatorState styleToSword = AddState(
                root,
                "StyleChangeToSword",
                GetSwordClip("@anim_Sword_rage.FBX"),
                new Vector3(750f, 350f));
            AnimatorState styleToBeast = AddState(
                root,
                "StyleChangeToBeast",
                GetBeastClip("@anim_rage.FBX"),
                new Vector3(950f, 350f));
            AnimatorState swordDeath = AddState(
                root,
                "SwordDeath",
                GetSwordClip("@anim_Sword_death.FBX"),
                new Vector3(1200f, 50f));
            AnimatorState beastDeath = AddState(
                root,
                "BeastDeath",
                GetBeastClip("@anim_Dying.FBX"),
                new Vector3(1200f, 350f));

            int swordStyle = (int)DemonSwordsmanStyle.Sword;
            int beastStyle = (int)DemonSwordsmanStyle.Beast;

            AddFlowTransition(
                startState,
                swordLocomotion,
                BossAnimationAction.Locomotion,
                swordStyle);
            AddFlowTransition(
                startState,
                beastLocomotion,
                BossAnimationAction.Locomotion,
                beastStyle);
            AddFlowTransition(
                swordLocomotion,
                swordTurnLeft,
                BossAnimationAction.TurnLeft,
                swordStyle);
            AddFlowTransition(
                swordLocomotion,
                swordTurnRight,
                BossAnimationAction.TurnRight,
                swordStyle);
            AddAttackTransition(
                swordLocomotion,
                swordQuickSlash,
                DemonSwordsmanAttackKind.QuickSlash,
                swordStyle);
            AddAttackTransition(
                swordLocomotion,
                swordComboStart,
                DemonSwordsmanAttackKind.SwordCombo,
                swordStyle);
            AddAttackTransition(
                swordLocomotion,
                swordHeavySlash,
                DemonSwordsmanAttackKind.HeavySlash,
                swordStyle);
            AddAttackTransition(
                swordLocomotion,
                swordChaseSlash,
                DemonSwordsmanAttackKind.ChaseSlash,
                swordStyle);
            AddAttackTransition(
                swordLocomotion,
                swordJumpAttack,
                DemonSwordsmanAttackKind.JumpSlash,
                swordStyle);
            AddAttackTransition(
                beastLocomotion,
                beastComboStart,
                DemonSwordsmanAttackKind.BeastCombo,
                beastStyle);
            AddAttackTransition(
                beastLocomotion,
                beastSlam,
                DemonSwordsmanAttackKind.BeastSlam,
                beastStyle);
            AddAttackTransition(
                beastLocomotion,
                beastRush,
                DemonSwordsmanAttackKind.BeastRush,
                beastStyle);
            AddAttackTransition(
                beastLocomotion,
                beastWideAttack,
                DemonSwordsmanAttackKind.BeastWideAttack,
                beastStyle);

            AddAttackTransition(
                swordComboStart,
                swordQuickSlash,
                DemonSwordsmanAttackKind.QuickSlash,
                swordStyle);
            AddAttackTransition(
                swordComboStart,
                swordChaseSlash,
                DemonSwordsmanAttackKind.ChaseSlash,
                swordStyle);
            AddAttackTransition(
                swordComboFinish,
                swordQuickSlash,
                DemonSwordsmanAttackKind.QuickSlash,
                swordStyle);
            AddAttackTransition(
                swordComboFinish,
                swordChaseSlash,
                DemonSwordsmanAttackKind.ChaseSlash,
                swordStyle);
            AddAttackTransition(
                beastComboStart,
                beastRush,
                DemonSwordsmanAttackKind.BeastRush,
                beastStyle);
            AddAttackTransition(
                beastComboStart,
                beastWideAttack,
                DemonSwordsmanAttackKind.BeastWideAttack,
                beastStyle);
            AddAttackTransition(
                beastComboSecond,
                beastRush,
                DemonSwordsmanAttackKind.BeastRush,
                beastStyle);
            AddAttackTransition(
                beastComboSecond,
                beastWideAttack,
                DemonSwordsmanAttackKind.BeastWideAttack,
                beastStyle);
            AddAttackTransition(
                beastComboFinish,
                beastRush,
                DemonSwordsmanAttackKind.BeastRush,
                beastStyle);
            AddAttackTransition(
                beastComboFinish,
                beastWideAttack,
                DemonSwordsmanAttackKind.BeastWideAttack,
                beastStyle);
            AddExitTimeTransition(
                swordComboStart,
                swordComboFinish,
                0.88f);
            AddExitTimeTransition(
                beastComboStart,
                beastComboSecond,
                0.88f);
            AddExitTimeTransition(
                beastComboSecond,
                beastComboFinish,
                0.88f);

            AnimatorState[] swordReturnStates =
            {
                swordTurnLeft,
                swordTurnRight,
                swordQuickSlash,
                swordComboStart,
                swordComboFinish,
                swordHeavySlash,
                swordChaseSlash,
                swordJumpAttack,
                swordHit
            };
            AddReturnTransitions(
                swordReturnStates,
                swordLocomotion,
                swordStyle);

            AnimatorState[] beastReturnStates =
            {
                beastComboStart,
                beastComboSecond,
                beastComboFinish,
                beastSlam,
                beastRush,
                beastWideAttack,
                beastHit
            };
            AddReturnTransitions(
                beastReturnStates,
                beastLocomotion,
                beastStyle);

            AddAnyFlowTransition(
                sword,
                swordHit,
                BossAnimationAction.Hit,
                swordStyle);
            AddAnyFlowTransition(
                beast,
                beastHit,
                BossAnimationAction.Hit,
                beastStyle);

            AddAnyFlowTransition(
                root,
                phaseFear,
                BossAnimationAction.PhaseFear);
            AddFlowTransition(
                phaseFear,
                phaseRage,
                BossAnimationAction.PhaseRage);
            AddFlowTransition(
                phaseRage,
                beastLocomotion,
                BossAnimationAction.Locomotion,
                beastStyle);
            AddAnyFlowTransition(
                root,
                styleToSword,
                BossAnimationAction.StyleChange,
                swordStyle);
            AddAnyFlowTransition(
                root,
                styleToBeast,
                BossAnimationAction.StyleChange,
                beastStyle);
            AddFlowTransition(
                styleToSword,
                swordLocomotion,
                BossAnimationAction.Locomotion,
                swordStyle);
            AddFlowTransition(
                styleToBeast,
                beastLocomotion,
                BossAnimationAction.Locomotion,
                beastStyle);
            AddAnyFlowTransition(
                root,
                swordDeath,
                BossAnimationAction.Death,
                swordStyle);
            AddAnyFlowTransition(
                root,
                beastDeath,
                BossAnimationAction.Death,
                beastStyle);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddLocomotion(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            string stateName,
            ChildMotion[] childMotions,
            Vector3 position)
        {
            var blendTree = new BlendTree
            {
                name = stateName + "BlendTree",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "MoveSide",
                blendParameterY = "MoveForward",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            for (int index = 0; index < childMotions.Length; index++)
            {
                ChildMotion child = childMotions[index];
                blendTree.AddChild(
                    child.Clip,
                    new Vector2(child.Side, child.Forward));
            }

            AnimatorState state =
                stateMachine.AddState(stateName, position);
            state.motion = blendTree;
            state.writeDefaultValues = false;
            return state;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string stateName,
            Motion motion,
            Vector3 position)
        {
            AnimatorState state =
                stateMachine.AddState(stateName, position);
            state.motion = motion;
            state.writeDefaultValues = false;
            return state;
        }

        private static void AddExitTimeTransition(
            AnimatorState from,
            AnimatorState to,
            float exitTime)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
        }

        private static void AddReturnTransitions(
            AnimatorState[] states,
            AnimatorState locomotion,
            int style)
        {
            for (int index = 0; index < states.Length; index++)
            {
                AddFlowTransition(
                    states[index],
                    locomotion,
                    BossAnimationAction.Locomotion,
                    style);
            }
        }

        private static void AddAttackTransition(
            AnimatorState from,
            AnimatorState to,
            DemonSwordsmanAttackKind attackKind,
            int style)
        {
            AnimatorStateTransition transition =
                CreateFlowTransition(from.AddTransition(to));
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                (int)BossAnimationAction.Attack,
                "Action");
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                style,
                "Style");
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                (int)attackKind,
                "AttackKind");
        }

        private static void AddFlowTransition(
            AnimatorState from,
            AnimatorState to,
            BossAnimationAction action,
            int style = -1)
        {
            AnimatorStateTransition transition =
                CreateFlowTransition(from.AddTransition(to));
            AddFlowConditions(transition, action, style);
        }

        private static void AddAnyFlowTransition(
            AnimatorStateMachine from,
            AnimatorState to,
            BossAnimationAction action,
            int style = -1)
        {
            AnimatorStateTransition transition =
                CreateFlowTransition(from.AddAnyStateTransition(to));
            AddFlowConditions(transition, action, style);
        }

        private static AnimatorStateTransition CreateFlowTransition(
            AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            return transition;
        }

        private static void AddFlowConditions(
            AnimatorStateTransition transition,
            BossAnimationAction action,
            int style)
        {
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                (int)action,
                "Action");

            if (style >= 0)
            {
                transition.AddCondition(
                    AnimatorConditionMode.Equals,
                    style,
                    "Style");
            }
        }

        private enum BossAnimationAction
        {
            Locomotion,
            TurnLeft,
            TurnRight,
            Attack,
            Hit,
            PhaseFear,
            PhaseRage,
            StyleChange,
            Death
        }

        private static void CreateBossPrefab(
            DemonSwordsmanBossSettings settings,
            RuntimeAnimatorController controller,
            Avatar copiedAvatar)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();

            try
            {
                GameObject sourcePrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        SourceRoot +
                        "/prefab/UPR/base_mesh_set_1.prefab");
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(
                    sourcePrefab,
                    previewScene);
                PrefabUtility.UnpackPrefabInstance(
                    visual,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                visual.name = "Visual";

                var bossRoot = new GameObject("DemonSwordsmanBoss");
                SceneManager.MoveGameObjectToScene(bossRoot, previewScene);
                visual.transform.SetParent(bossRoot.transform, false);

                RemapVisualAssets(
                    visual,
                    copiedAvatar,
                    controller);
                GameObject handSword = CreateHandSword(visual);
                GameObject beltSword = FindChild(
                    visual.transform,
                    "sword4");

                if (beltSword == null)
                {
                    throw new InvalidOperationException(
                        "본체에서 허리 검 sword4를 찾지 못했습니다.");
                }

                beltSword.name = "BeltSword";
                ConfigureClothColliders(visual);

                CharacterController characterController =
                    bossRoot.AddComponent<CharacterController>();
                characterController.height = 2.4f;
                characterController.radius = 0.5f;
                characterController.center = new Vector3(0f, 1.2f, 0f);
                characterController.stepOffset = 0.3f;
                characterController.slopeLimit = 50f;

                DemonSwordsmanAnimationController animationController =
                    visual.AddComponent<
                        DemonSwordsmanAnimationController>();
                visual.AddComponent<
                    DemonSwordsmanAnimationMoveReceiver>();
                visual.AddComponent<
                    DemonSwordsmanCombatAnimationEventReceiver>();

                Animator animator =
                    visual.GetComponentInChildren<Animator>(true);
                animationController.Configure(
                    animator,
                    handSword,
                    beltSword);

                DemonSwordsmanController bossController =
                    bossRoot.AddComponent<DemonSwordsmanController>();
                bossController.Configure(
                    null,
                    null,
                    settings,
                    animationController);

                if (handSword != null)
                {
                    handSword.SetActive(true);
                }

                if (beltSword != null)
                {
                    beltSword.SetActive(false);
                }

                PrefabUtility.SaveAsPrefabAsset(bossRoot, PrefabPath);
                Object.DestroyImmediate(bossRoot);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static GameObject CreateHandSword(GameObject visual)
        {
            GameObject handBoneObject = FindChild(
                visual.transform,
                "hand_r");
            Mesh handSwordMesh = AssetDatabase.LoadAllAssetsAtPath(
                    HandSwordModelPath)
                .OfType<Mesh>()
                .FirstOrDefault(mesh => string.Equals(
                    mesh.name,
                    "sword6",
                    StringComparison.OrdinalIgnoreCase));

            if (handBoneObject == null || handSwordMesh == null)
            {
                throw new InvalidOperationException(
                    "복제한 손검 Mesh 또는 hand_r 본을 찾지 못했습니다.");
            }

            var handSword = new GameObject("HandSword");
            handSword.transform.SetParent(visual.transform, false);

            SkinnedMeshRenderer renderer =
                handSword.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = handSwordMesh;
            renderer.sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(SwordMaterialPath);
            renderer.rootBone = handBoneObject.transform;
            renderer.bones = new[] { handBoneObject.transform };
            renderer.updateWhenOffscreen = true;
            return handSword;
        }
        private static void RemapVisualAssets(
            GameObject visual,
            Avatar copiedAvatar,
            RuntimeAnimatorController controller)
        {
            Dictionary<string, Mesh> copiedMeshes =
                AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                    .OfType<Mesh>()
                    .GroupBy(mesh => mesh.name)
                    .ToDictionary(group => group.Key, group => group.First());

            SkinnedMeshRenderer[] skinnedRenderers =
                visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (int index = 0; index < skinnedRenderers.Length; index++)
            {
                SkinnedMeshRenderer renderer = skinnedRenderers[index];

                if (renderer.sharedMesh != null &&
                    copiedMeshes.TryGetValue(
                        renderer.sharedMesh.name,
                        out Mesh copiedMesh))
                {
                    renderer.sharedMesh = copiedMesh;
                }

                RemapRendererMaterials(renderer);
            }

            MeshRenderer[] meshRenderers =
                visual.GetComponentsInChildren<MeshRenderer>(true);

            for (int index = 0; index < meshRenderers.Length; index++)
            {
                RemapRendererMaterials(meshRenderers[index]);
            }

            Animator animator = visual.GetComponentInChildren<Animator>(true);

            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            animator.avatar = copiedAvatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private static void RemapRendererMaterials(Renderer renderer)
        {
            Material bodyMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            Material swordMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(SwordMaterialPath);
            Material[] materials = renderer.sharedMaterials;

            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];

                if (material == null)
                {
                    continue;
                }

                if (material.name.IndexOf(
                        "body",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    materials[index] = bodyMaterial;
                }
                else if (material.name.IndexOf(
                             "weapon",
                             StringComparison.OrdinalIgnoreCase) >= 0 ||
                         material.name.IndexOf(
                             "sword",
                             StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    materials[index] = swordMaterial;
                }
            }

            renderer.sharedMaterials = materials;
        }

        private static void ConfigureClothColliders(GameObject visual)
        {
            int clothLayer = GetOrCreateLayer("BossCloth");
            CapsuleCollider[] colliders =
                visual.GetComponentsInChildren<CapsuleCollider>(true);

            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].gameObject.layer = clothLayer;
                colliders[index].isTrigger = true;
            }
        }

        private static int GetOrCreateLayer(string layerName)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);

            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            Object tagManager =
                AssetDatabase.LoadAllAssetsAtPath(
                    "ProjectSettings/TagManager.asset")[0];
            var serializedTagManager = new SerializedObject(tagManager);
            SerializedProperty layers =
                serializedTagManager.FindProperty("layers");

            for (int index = 8; index < 32; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);

                if (!string.IsNullOrEmpty(layer.stringValue))
                {
                    continue;
                }

                layer.stringValue = layerName;
                serializedTagManager.ApplyModifiedProperties();
                return index;
            }

            Debug.LogWarning(
                "빈 사용자 Layer가 없어 Cloth Collider를 Ignore Raycast Layer에 배치합니다.");
            return Physics.IgnoreRaycastLayer;
        }

        private static void ValidateOwnedReferences()
        {
            string[] paths =
            {
                PrefabPath,
                ControllerPath,
                BodyMaterialPath,
                SwordMaterialPath
            };

            for (int index = 0; index < paths.Length; index++)
            {
                string[] dependencies = AssetDatabase.GetDependencies(
                    paths[index],
                    true);

                for (int dependencyIndex = 0;
                     dependencyIndex < dependencies.Length;
                     dependencyIndex++)
                {
                    string dependency = dependencies[dependencyIndex];

                    if (dependency.StartsWith(
                            SourceRoot,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"프로젝트 소유 에셋에 원본 참조가 남았습니다: {paths[index]} -> {dependency}");
                    }
                }
            }
        }

        private static AnimationClip GetSwordClip(string fileName)
        {
            return GetClip(SwordAnimationsRoot + "/" + fileName);
        }

        private static AnimationClip GetBeastClip(string fileName)
        {
            return GetClip(BeastAnimationsRoot + "/" + fileName);
        }

        private static AnimationClip GetClip(string assetPath)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(
                    item => !item.name.StartsWith(
                        "__preview__",
                        StringComparison.Ordinal));

            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"AnimationClip을 찾지 못했습니다: {assetPath}");
            }

            return clip;
        }

        private static ChildMotion Child(
            AnimationClip clip,
            float side,
            float forward)
        {
            return new ChildMotion(clip, side, forward);
        }

        private static GameObject FindChild(
            Transform root,
            string childName)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(
                true);

            for (int index = 0; index < children.Length; index++)
            {
                if (string.Equals(
                        children[index].name,
                        childName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return children[index].gameObject;
                }
            }

            return null;
        }

        private readonly struct ChildMotion
        {
            public readonly AnimationClip Clip; // 내부에서 사용하는 값
            public readonly float Side; // 내부에서 사용하는 값
            public readonly float Forward; // 내부에서 사용하는 값

            public ChildMotion(
                AnimationClip clip,
                float side,
                float forward)
            {
                Clip = clip;
                Side = side;
                Forward = forward;
            }
        }
    }
}
