using System;
using System.IO;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using rudIsland.RPG3D.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace rudIsland.RPG3D.EditorTools
{
    // NightShade 모델, RustySword와 양손검 애니메이션을 전투 프리팹으로 묶는다.
    public static class NightShadeSwordEliteAssetBuilder
    {
        private const string ModelPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Meshes/Nightshade J Friedrich 1_Copy.fbx";
        private const string SwordPrefabPath =
            "Assets/_ThirdParty/Danvil/Rusty Sword/Prefabs/RustySword.prefab";
        private const string SwordBodySoundPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Sound/Attack/WEAPSwrd_BaseMetal_HoveAud_SwordCombat_03.wav";
        private const string SwordAccentSoundPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Sound/Attack/WEAPSwrd_BaseMetal_HoveAud_SwordCombat_06.wav";
        private const string AnimatorControllerPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Controllers/NightShadeTwoHandSwordAnimator.controller";
        private const string EnemyPrefabPath =
            "Assets/_Project/Scenes/Dev/CharacterTest/Prefabs/NightShadeSwordElite.prefab";
        private const string SpawnSettingsPath =
            "Assets/_Project/Scenes/Dev/CharacterTest/Settings/NightShadeTestSpawnSettings.asset";
        private const string AnimationCopyFolder =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword";
        private const string AttackSpeedParameterName = "AttackSpeed";

        private const string IdleSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Movement/Idle/Idle/2Hand_Up_Idle_A_1.fbx";
        private const string ChaseSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Movement/Run/Type A/Base/InPlace/2Hand_Up_Run_A_F_InPlace.fbx";
        private const string CombatBackSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Movement/Block A Walk Slow/InPlace/2Hand_Up_Block_Walk_Slow_B_InPlace.fbx";
        private const string CombatLeftSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Movement/Block A Walk Slow/InPlace/2Hand_Up_Block_Walk_Slow_F_L90_A_InPlace.fbx";
        private const string CombatRightSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Movement/Block A Walk Slow/InPlace/2Hand_Up_Block_Walk_Slow_F_R90_A_InPlace.fbx";
        private const string LightAttackSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Attack_A/2Hand_Up_Attack_A_1.fbx";
        private const string ComboAttackSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Attack_A/2Hand_Up_Attack_A_Combo_12.fbx";
        private const string HeavyAttackSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Attack_B/2Hand_Up_Attack_B_1.fbx";
        private const string WideSwingSourcePath =
            "Assets/_ThirdParty/AnimationBundle/FBX_Animations/Two Hand Up/Attack_A/2Hand_Up_Attack_A_3.fbx";
        private const string HitClipPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/FromSources/A_SpearCombatAnimationV2_Hit_Fw_RM.anim";
        private const string DeadClipPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/FromSources/A_SpearCombatAnimationV2_HitDeath_Fw_RM.anim";

        private const float SwordLength = 1.5f;
        private const float SwordHitRadius = 0.13f;
        private static readonly Vector3 AuthoredSwordDirection =
            new Vector3(-0.76f, -0.35f, -0.54f).normalized;

        [MenuItem("Tools/rudIsland/Build NightShade Sword Elite")]
        public static void Build()
        {
            AnimatorController animatorController = BuildAnimatorController();
            NightShadeSwordController enemyPrefab =
                BuildEnemyPrefab(animatorController);
            ConnectSpawnSettings(enemyPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "NightShadeSwordElite 프리팹과 양손검 Animator를 만들었습니다.");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static AnimatorController BuildAnimatorController()
        {
            EnsureAssetFolder(AnimationCopyFolder);
            AnimationClip idleClip = CopyAnimationClip(
                IdleSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_Idle.anim",
                true);
            AnimationClip chaseClip = CopyAnimationClip(
                ChaseSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_Chase.anim",
                true);
            AnimationClip combatBackClip = CopyAnimationClip(
                CombatBackSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_CombatBack.anim",
                true);
            AnimationClip combatLeftClip = CopyAnimationClip(
                CombatLeftSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_CombatLeft.anim",
                true);
            AnimationClip combatRightClip = CopyAnimationClip(
                CombatRightSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_CombatRight.anim",
                true);
            AnimationClip lightAttackClip = CopyAnimationClip(
                LightAttackSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_LightAttack.anim",
                false);
            AnimationClip comboAttackClip = CopyAnimationClip(
                ComboAttackSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_ComboAttack.anim",
                false);
            AnimationClip heavyAttackClip = CopyAnimationClip(
                HeavyAttackSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_HeavyAttack.anim",
                false);
            AnimationClip wideSwingClip = CopyAnimationClip(
                WideSwingSourcePath,
                $"{AnimationCopyFolder}/NightShadeSword_WideSwing.anim",
                false);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    AnimatorControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(
                        AnimatorControllerPath);
            }

            EnsureFloatParameter(
                controller,
                AttackSpeedParameterName);
            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            AnimatorState[] oldStates = new AnimatorState[
                stateMachine.states.Length];
            for (int index = 0; index < stateMachine.states.Length; index++)
            {
                oldStates[index] = stateMachine.states[index].state;
            }

            for (int index = 0; index < oldStates.Length; index++)
            {
                stateMachine.RemoveState(oldStates[index]);
            }

            AnimatorState idle = AddState(
                stateMachine,
                "Idle",
                idleClip,
                new Vector3(0f, 0f));
            AddState(
                stateMachine,
                "Chase",
                chaseClip,
                new Vector3(260f, 0f));
            AddState(
                stateMachine,
                "Combat Back",
                combatBackClip,
                new Vector3(520f, 0f));
            AddState(
                stateMachine,
                "Combat Left",
                combatLeftClip,
                new Vector3(650f, 0f));
            AddState(
                stateMachine,
                "Combat Right",
                combatRightClip,
                new Vector3(780f, 0f));
            AddState(
                stateMachine,
                "Light Attack",
                lightAttackClip,
                new Vector3(0f, 90f),
                true);
            AddState(
                stateMachine,
                "Combo Attack",
                comboAttackClip,
                new Vector3(260f, 90f),
                true);
            AddState(
                stateMachine,
                "Heavy Attack",
                heavyAttackClip,
                new Vector3(520f, 90f),
                true);
            AddState(
                stateMachine,
                "Wide Swing",
                wideSwingClip,
                new Vector3(520f, 0f),
                true);
            AddState(
                stateMachine,
                "Hit",
                LoadClip(HitClipPath),
                new Vector3(130f, 180f));
            AddState(
                stateMachine,
                "Dead",
                LoadClip(DeadClipPath),
                new Vector3(390f, 180f));
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            Vector3 position,
            bool usesAttackSpeed = false)
        {
            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = clip;
            state.writeDefaultValues = true;
            if (usesAttackSpeed)
            {
                state.speedParameterActive = true;
                state.speedParameter = AttackSpeedParameterName;
            }

            return state;
        }

        private static void EnsureFloatParameter(
            AnimatorController controller,
            string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name != parameterName)
                {
                    continue;
                }

                if (parameters[index].type !=
                    AnimatorControllerParameterType.Float)
                {
                    throw new InvalidOperationException(
                        $"{parameterName} Animator 파라미터는 Float여야 합니다.");
                }

                return;
            }

            controller.AddParameter(
                parameterName,
                AnimatorControllerParameterType.Float);
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            AnimationClip directClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (directClip != null)
            {
                return directClip;
            }

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip &&
                    !clip.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException(
                $"애니메이션 클립을 찾지 못했습니다: {assetPath}");
        }

        private static AnimationClip CopyAnimationClip(
            string sourcePath,
            string copyPath,
            bool loop)
        {
            AnimationClip sourceClip = LoadClip(sourcePath);
            AnimationClip copyClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(copyPath);
            AnimationEvent[] savedEvents = copyClip != null
                ? AnimationUtility.GetAnimationEvents(copyClip)
                : null;
            if (copyClip == null)
            {
                copyClip = new AnimationClip();
                AssetDatabase.CreateAsset(copyClip, copyPath);
            }

            EditorUtility.CopySerialized(sourceClip, copyClip);
            if (savedEvents != null)
            {
                AnimationUtility.SetAnimationEvents(copyClip, savedEvents);
            }

            copyClip.name = Path.GetFileNameWithoutExtension(copyPath);
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(copyClip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(copyClip, settings);
            EditorUtility.SetDirty(copyClip);
            return copyClip;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            string[] folderNames = folderPath.Split('/');
            string currentPath = folderNames[0];
            for (int index = 1; index < folderNames.Length; index++)
            {
                string nextPath =
                    $"{currentPath}/{folderNames[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        folderNames[index]);
                }

                currentPath = nextPath;
            }
        }

        private static NightShadeSwordController BuildEnemyPrefab(
            RuntimeAnimatorController animatorController)
        {
            GameObject modelAsset =
                LoadRequiredAsset<GameObject>(ModelPath);
            GameObject swordAsset =
                LoadRequiredAsset<GameObject>(SwordPrefabPath);
            AudioClip swordBodySound =
                LoadRequiredAsset<AudioClip>(SwordBodySoundPath);
            AudioClip swordAccentSound =
                LoadRequiredAsset<AudioClip>(SwordAccentSoundPath);
            GameObject enemy =
                PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (enemy == null)
            {
                throw new InvalidOperationException(
                    "NightShade 모델을 프리팹 편집용으로 만들지 못했습니다.");
            }

            try
            {
                enemy.name = "NightShadeSwordElite";
                enemy.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                enemy.transform.localScale = Vector3.one;
                SetEnemyLayer(enemy.transform);
                HideOldLance(enemy.transform);

                Animator animator =
                    enemy.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    throw new InvalidOperationException(
                        "NightShade 모델에서 Animator를 찾지 못했습니다.");
                }

                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;

                CharacterController characterController =
                    enemy.GetComponent<CharacterController>();
                if (characterController == null)
                {
                    characterController =
                        enemy.AddComponent<CharacterController>();
                }

                characterController.center = new Vector3(0f, 1f, 0f);
                characterController.height = 2f;
                characterController.radius = 0.45f;
                characterController.stepOffset = 0.3f;

                Transform rightHand =
                    animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand == null)
                {
                    rightHand = FindChild(enemy.transform, "RightHand");
                }

                if (rightHand == null)
                {
                    throw new InvalidOperationException(
                        "NightShade 모델에서 RightHand 뼈를 찾지 못했습니다.");
                }

                GameObject sword =
                    PrefabUtility.InstantiatePrefab(
                        swordAsset,
                        rightHand) as GameObject;
                if (sword == null)
                {
                    throw new InvalidOperationException(
                        "RustySword 프리팹을 오른손에 만들지 못했습니다.");
                }

                sword.name = "RustySword";
                PlaceSwordOnRightHand(sword.transform);
                Transform swordStartPoint = CreateHitPoint(
                    sword.transform,
                    "SwordHitStart",
                    new Vector3(0f, 0.12f, 0f));
                Transform swordEndPoint = CreateHitPoint(
                    sword.transform,
                    "SwordHitEnd",
                    new Vector3(0f, 0.95f, 0f));

                NightShadeSwordAnimationController animationController =
                    enemy.GetComponent<NightShadeSwordAnimationController>();
                if (animationController == null)
                {
                    animationController = enemy.AddComponent<
                        NightShadeSwordAnimationController>();
                }

                animationController.ConnectForEditor(animator);
                NightShadeSwordAnimationEventReceiver eventReceiver =
                    animator.GetComponent<
                        NightShadeSwordAnimationEventReceiver>();
                if (eventReceiver == null)
                {
                    eventReceiver = animator.gameObject.AddComponent<
                        NightShadeSwordAnimationEventReceiver>();
                }

                eventReceiver.ConnectForEditor(animationController);
                if (enemy.GetComponent<CombatHitEffectPlayer>() == null)
                {
                    enemy.AddComponent<CombatHitEffectPlayer>();
                }

                AudioSource bodyAudioSource =
                    enemy.GetComponent<AudioSource>();
                if (bodyAudioSource == null)
                {
                    bodyAudioSource = enemy.AddComponent<AudioSource>();
                }

                AudioSource accentAudioSource =
                    enemy.AddComponent<AudioSource>();

                NightShadeSwordAttackAudio attackAudio =
                    enemy.GetComponent<NightShadeSwordAttackAudio>();
                if (attackAudio == null)
                {
                    attackAudio = enemy.AddComponent<
                        NightShadeSwordAttackAudio>();
                }

                attackAudio.ConnectForEditor(
                    bodyAudioSource,
                    accentAudioSource,
                    swordBodySound,
                    swordAccentSound);

                NightShadeSwordController swordController =
                    enemy.GetComponent<NightShadeSwordController>();
                if (swordController == null)
                {
                    swordController =
                        enemy.AddComponent<NightShadeSwordController>();
                }

                swordController.ConnectForEditor(
                    animator,
                    swordStartPoint,
                    swordEndPoint,
                    SwordHitRadius);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    enemy,
                    EnemyPrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException(
                        "NightShadeSwordElite 프리팹을 저장하지 못했습니다.");
                }

                return savedPrefab.GetComponent<NightShadeSwordController>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        private static void ConnectSpawnSettings(
            NightShadeSwordController enemyPrefab)
        {
            SpawnSettings settings =
                LoadRequiredAsset<SpawnSettings>(SpawnSettingsPath);
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("prefab").objectReferenceValue =
                enemyPrefab;
            serializedSettings.FindProperty("initialSize").intValue = 1;
            serializedSettings.FindProperty("maxSize").intValue = 2;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void PlaceSwordOnRightHand(Transform sword)
        {
            MeshFilter meshFilter = sword.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                sword.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                sword.localScale = Vector3.one;
                return;
            }

            Bounds bounds = meshFilter.sharedMesh.bounds;
            Vector3 size = bounds.size;
            Vector3 bladeAxis = Vector3.forward;
            float bladeLength = size.z;
            float minimum = bounds.min.z;
            float maximum = bounds.max.z;

            if (size.x > bladeLength && size.x >= size.y)
            {
                bladeAxis = Vector3.right;
                bladeLength = size.x;
                minimum = bounds.min.x;
                maximum = bounds.max.x;
            }
            else if (size.y > bladeLength)
            {
                bladeAxis = Vector3.up;
                bladeLength = size.y;
                minimum = bounds.min.y;
                maximum = bounds.max.y;
            }

            if (Mathf.Abs(minimum) > Mathf.Abs(maximum))
            {
                bladeAxis = -bladeAxis;
            }

            sword.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.FromToRotation(
                    bladeAxis,
                    AuthoredSwordDirection));
            float scale = bladeLength > 0.0001f
                ? SwordLength / bladeLength
                : 1f;
            sword.localScale = Vector3.one * scale;
        }

        private static Transform CreateHitPoint(
            Transform parent,
            string objectName,
            Vector3 localPosition)
        {
            var hitPoint = new GameObject(objectName);
            hitPoint.layer = parent.gameObject.layer;
            hitPoint.transform.SetParent(parent, false);
            hitPoint.transform.localPosition = localPosition;
            return hitPoint.transform;
        }

        private static void HideOldLance(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                Transform child = children[index];
                if (child == root ||
                    child.name.IndexOf(
                        "lance",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private static Transform FindChild(Transform root, string childName)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == childName)
                {
                    return children[index];
                }
            }

            return null;
        }

        private static void SetEnemyLayer(Transform root)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                enemyLayer = 7;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                children[index].gameObject.layer = enemyLayer;
            }
        }

        private static T LoadRequiredAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"필수 에셋을 찾지 못했습니다: {assetPath}");
            }

            return asset;
        }
    }
}
