using System;
using System.Collections.Generic;
using Characters.Combat.AttackData;
using Characters.Player.Lifecycle;
using Characters.Player.Config;
using Characters.Player.Combat.Attack;
using Characters.Player.Audio;
using Characters.Player.StateMachine.States.Attack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTools
{
    // 기존 CharacterTestScene의 Player 값을 대표 Config와 역할 컴포넌트로 옮긴다.
    public static class PlayerCharacterConfigMigration
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Dev/CharacterTestScene.unity";
        private const string ConfigFolder =
            "Assets/_Project/Characters/Player/Configs";
        private const string ConfigPath =
            ConfigFolder + "/PlayerCharacterConfig.asset";
        private const string PlayerPrefabPath =
            "Assets/_Project/Runtime/Characters/PlayerRoot.prefab";

        private static readonly string[] AttackPropertyPaths =
        {
            "targetStopDistance",
            "maximumAddedMoveDistance",
            "maximumTurnAngle",
            "comboCloseNormalizedTime"
        };

        [MenuItem("Tools/rudIsland/Player/현재 씬 설정 이전")]
        public static void MigrateActiveScene()
        {
            MigrateScene(SceneManager.GetActiveScene(), false);
        }

        // Unity batchmode에서 호출하는 고정 진입점이다.
        public static void MigrateCharacterTestScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            MigrateScene(scene, true);
        }

        [MenuItem("Tools/rudIsland/Player/PlayerRoot 프리팹 설정 이전")]
        public static void MigratePlayerRootPrefab()
        {
            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                PlayerController controller =
                    prefabRoot.GetComponentInChildren<PlayerController>(true);
                if (controller == null)
                {
                    throw new InvalidOperationException(
                        "PlayerRoot 프리팹에서 PlayerController를 찾지 못했습니다.");
                }

                MigrateController(controller);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void MigrateScene(Scene scene, bool exitAfterMigration)
        {
            try
            {
                PlayerController controller =
                    UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (controller == null)
                {
                    throw new InvalidOperationException(
                        $"{scene.path}에서 PlayerController를 찾지 못했습니다.");
                }

                MigrateController(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (exitAfterMigration)
                {
                    EditorApplication.Exit(1);
                }

                throw;
            }

            if (exitAfterMigration)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void MigrateController(PlayerController controller)
        {
            var controllerData = new SerializedObject(controller);
            if (controllerData.FindProperty("attackData") == null)
            {
                VerifyCompletedMigration(controller, controllerData);
                return;
            }

            PlayerAttackData[] attacks = ReadAttacks(controllerData);
            ValidateSharedAttackValues(attacks);
            MigrateAttackDamage(attacks);

            EnsureConfigFolder();
            PlayerCharacterConfig config =
                AssetDatabase.LoadAssetAtPath<PlayerCharacterConfig>(
                    ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PlayerCharacterConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var configData = new SerializedObject(config);
            CopyControllerValues(controllerData, configData);
            CopyAttackValues(attacks, configData);
            configData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            MoveFootstepAudioValues(controllerData);
            PlayerWeaponHitShape hitShape =
                MoveWeaponHitShapeValues(controller, controllerData);

            controllerData.Update();
            controllerData.FindProperty("config").objectReferenceValue = config;
            controllerData.FindProperty("weaponHitShape").objectReferenceValue = hitShape;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            Debug.Log(
                $"Player 설정 이전 완료: {ConfigPath}\n" +
                "공격 공통값 4개가 모두 같은 것을 확인한 뒤 합쳤습니다.");
        }

        private static void VerifyCompletedMigration(
            PlayerController controller,
            SerializedObject controllerData)
        {
            PlayerCharacterConfig config = controllerData
                .FindProperty("config")?.objectReferenceValue as
                PlayerCharacterConfig;
            PlayerWeaponHitShape hitShape = controllerData
                .FindProperty("weaponHitShape")?.objectReferenceValue as
                PlayerWeaponHitShape;
            if (config == null || hitShape == null ||
                controller.GetComponentInChildren<PlayerFootstepAudio>(true) == null)
            {
                throw new InvalidOperationException(
                    "Player 설정 이전 결과에 Config, 검 판정 또는 발소리 연결이 빠졌습니다.");
            }

            Debug.Log("Player 설정은 이미 새 Config 구조로 이전되어 있습니다.");
        }

        private static void CopyControllerValues(
            SerializedObject source,
            SerializedObject target)
        {
            Copy(source, "freeMoveTurnSpeed", target, "movement.freeMoveTurnSpeed");
            Copy(source, "targetMoveTurnSpeed", target, "movement.targetMoveTurnSpeed");
            Copy(source, "attackTurnSpeed", target, "movement.attackTurnSpeed");
            Copy(source, "walkSpeed", target, "movement.walkSpeed");
            Copy(source, "guardMoveSpeed", target, "movement.guardMoveSpeed");
            Copy(source, "sprintSpeed", target, "movement.sprintSpeed");
            Copy(source, "moveAcceleration", target, "movement.moveAcceleration");
            Copy(source, "moveDeceleration", target, "movement.moveDeceleration");
            Copy(source, "animationSmoothTime", target, "movement.animationSmoothTime");
            Copy(source, "rollDistance", target, "movement.rollDistance");
            Copy(source, "sprintRollDistance", target, "movement.sprintRollDistance");
            Copy(source, "rollCompleteNormalizedTime", target, "movement.rollCompleteNormalizedTime");
            Copy(source, "rollMovementCurve", target, "movement.rollMovementCurve");
            Copy(source, "gravity", target, "movement.gravity");
            Copy(source, "groundPull", target, "movement.groundPull");

            Copy(source, "maxHealth", target, "combat.maxHealth");
            Copy(source, "maxStamina", target, "combat.maxStamina");
            Copy(source, "staminaRecoverDelay", target, "combat.staminaRecoverDelay");
            Copy(source, "staminaRecoverSpeed", target, "combat.staminaRecoverSpeed");
            Copy(source, "guardStaminaRecoveryRate", target, "combat.guardStaminaRecoveryRate");
            Copy(source, "rollStaminaCost", target, "combat.rollStaminaCost");
            Copy(source, "sprintStaminaCostPerSecond", target, "combat.sprintStaminaCostPerSecond");
            Copy(source, "sprintRestartStamina", target, "combat.sprintRestartStamina");
            Copy(source, "actionInputBufferDuration", target, "combat.actionInputBufferDuration");
            Copy(source, "guardAngle", target, "combat.guardAngle");
            Copy(source, "guardRaiseDuration", target, "combat.guardRaiseDuration");
            Copy(source, "guardBreakControlLockDuration", target, "combat.guardBreakControlLockDuration");
            Copy(source, "hitPushDuration", target, "combat.hitPushDuration");
            Copy(source, "hitPushCurve", target, "combat.hitPushCurve");
            Copy(source, "stopPointLimit", target, "combat.stopPointLimit");
            Copy(source, "stopPointRecoverDelay", target, "combat.stopPointRecoverDelay");
            Copy(source, "stopPointRecoverSpeed", target, "combat.stopPointRecoverSpeed");

            Copy(source, "targetLayers", target, "target.targetLayers");
            Copy(source, "targetObstructionLayers", target, "target.obstructionLayers");
            Copy(source, "targetRange", target, "target.findRange");
            Copy(source, "targetBreakDistance", target, "target.breakDistance");
            Copy(source, "targetMaximumAngle", target, "target.maximumAngle");
            Copy(source, "targetHiddenGraceDuration", target, "target.hiddenGraceDuration");
            Copy(source, "targetHeightOffset", target, "target.heightOffset");
        }

        private static void CopyAttackValues(
            PlayerAttackData[] attacks,
            SerializedObject configData)
        {
            SerializedProperty targetAttacks =
                configData.FindProperty("attacks.attacks");
            targetAttacks.arraySize = attacks.Length;
            for (int index = 0; index < attacks.Length; index++)
            {
                targetAttacks.GetArrayElementAtIndex(index)
                    .objectReferenceValue = attacks[index];
            }

            var firstAttack = new SerializedObject(attacks[0]);
            Copy(firstAttack, "targetStopDistance", configData, "attacks.targetStopDistance");
            Copy(firstAttack, "maximumAddedMoveDistance", configData, "attacks.maximumAddedMoveDistance");
            Copy(firstAttack, "maximumTurnAngle", configData, "attacks.maximumTurnAngle");
            Copy(firstAttack, "comboCloseNormalizedTime", configData, "attacks.comboCloseNormalizedTime");
        }

        private static void MigrateAttackDamage(PlayerAttackData[] attacks)
        {
            for (int index = 0; index < attacks.Length; index++)
            {
                var data = new SerializedObject(attacks[index]);
                SerializedProperty target = data.FindProperty("attackDamage");
                target.FindPropertyRelative("healthDamage").floatValue =
                    data.FindProperty("damage").floatValue;
                target.FindPropertyRelative("staggerDamage").floatValue =
                    data.FindProperty("staggerDamage").floatValue;
                target.FindPropertyRelative("strength").enumValueIndex =
                    data.FindProperty("attackStrength").enumValueIndex;
                target.FindPropertyRelative("pushDistance").floatValue =
                    data.FindProperty("pushDistance").floatValue;
                target.FindPropertyRelative("hitStopDuration").floatValue =
                    data.FindProperty("hitStopDuration").floatValue;
                target.FindPropertyRelative("guardStaminaDamage").floatValue = 0f;
                target.FindPropertyRelative("canBlock").boolValue = true;
                target.FindPropertyRelative("damageSoundType").enumValueIndex = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(attacks[index]);
            }
        }

        private static void ValidateSharedAttackValues(PlayerAttackData[] attacks)
        {
            var differences = new List<string>();
            var first = new SerializedObject(attacks[0]);
            for (int propertyIndex = 0;
                propertyIndex < AttackPropertyPaths.Length;
                propertyIndex++)
            {
                string propertyPath = AttackPropertyPaths[propertyIndex];
                float expected = first.FindProperty(propertyPath).floatValue;
                for (int attackIndex = 1;
                    attackIndex < attacks.Length;
                    attackIndex++)
                {
                    var current = new SerializedObject(attacks[attackIndex]);
                    float actual = current.FindProperty(propertyPath).floatValue;
                    if (!Mathf.Approximately(expected, actual))
                    {
                        differences.Add(
                            $"{propertyPath}: 공격 1={expected}, 공격 {attackIndex + 1}={actual}");
                    }
                }
            }

            if (differences.Count > 0)
            {
                throw new InvalidOperationException(
                    "공격 공통값이 달라 이전을 중단합니다.\n" +
                    string.Join("\n", differences));
            }
        }

        private static PlayerAttackData[] ReadAttacks(SerializedObject controller)
        {
            SerializedProperty source = controller.FindProperty("attackData");
            if (source == null || source.arraySize != 6)
            {
                throw new InvalidOperationException(
                    "PlayerController의 기존 공격 데이터 6개가 필요합니다.");
            }

            var attacks = new PlayerAttackData[6];
            for (int index = 0; index < attacks.Length; index++)
            {
                attacks[index] = source.GetArrayElementAtIndex(index)
                    .objectReferenceValue as PlayerAttackData;
                if (attacks[index] == null ||
                    attacks[index].AttackNumber != index + 1)
                {
                    throw new InvalidOperationException(
                        $"Player 공격 {index + 1} 데이터가 없거나 순서가 다릅니다.");
                }
            }

            return attacks;
        }

        private static void MoveFootstepAudioValues(SerializedObject controller)
        {
            Animator animator = controller.FindProperty("playerAnimator")
                .objectReferenceValue as Animator;
            if (animator == null)
            {
                throw new InvalidOperationException("Player Animator가 필요합니다.");
            }

            PlayerFootstepAudio audio =
                animator.GetComponent<PlayerFootstepAudio>();
            if (audio == null)
            {
                audio = animator.gameObject.AddComponent<PlayerFootstepAudio>();
            }

            var audioData = new SerializedObject(audio);
            CopyArray(controller, "walkFootstepSounds", audioData, "walkSounds");
            CopyArray(controller, "runFootstepSounds", audioData, "runSounds");
            Copy(controller, "rollSound", audioData, "rollSound");
            Copy(controller, "walkFootstepVolume", audioData, "walkVolume");
            Copy(controller, "runFootstepVolume", audioData, "runVolume");
            Copy(controller, "rollSoundVolume", audioData, "rollVolume");
            Copy(controller, "footstepPitchChange", audioData, "pitchChange");
            audioData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audio);
        }

        private static PlayerWeaponHitShape MoveWeaponHitShapeValues(
            PlayerController controller,
            SerializedObject controllerData)
        {
            PlayerWeaponHitShape hitShape =
                controller.GetComponent<PlayerWeaponHitShape>();
            if (hitShape == null)
            {
                hitShape = controller.gameObject.AddComponent<PlayerWeaponHitShape>();
            }

            var shapeData = new SerializedObject(hitShape);
            Copy(controllerData, "weaponHitStart", shapeData, "startPoint");
            Copy(controllerData, "weaponHitEnd", shapeData, "endPoint");
            Copy(controllerData, "attackLayers", shapeData, "targetLayers");
            if (shapeData.FindProperty("targetLayers").intValue == 0)
            {
                Copy(controllerData, "targetLayers", shapeData, "targetLayers");
            }
            Copy(controllerData, "weaponHitRadius", shapeData, "radius");
            shapeData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hitShape);
            return hitShape;
        }

        private static void Copy(
            SerializedObject source,
            string sourcePath,
            SerializedObject target,
            string targetPath)
        {
            SerializedProperty sourceProperty = source.FindProperty(sourcePath);
            SerializedProperty targetProperty = target.FindProperty(targetPath);
            if (sourceProperty == null || targetProperty == null)
            {
                throw new InvalidOperationException(
                    $"설정 이전 경로를 찾지 못했습니다: {sourcePath} -> {targetPath}");
            }

            switch (sourceProperty.propertyType)
            {
                case SerializedPropertyType.Float:
                    targetProperty.floatValue = sourceProperty.floatValue;
                    break;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                    targetProperty.intValue = sourceProperty.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    targetProperty.boolValue = sourceProperty.boolValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    targetProperty.objectReferenceValue =
                        sourceProperty.objectReferenceValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    targetProperty.animationCurveValue =
                        new AnimationCurve(sourceProperty.animationCurveValue.keys)
                        {
                            preWrapMode = sourceProperty.animationCurveValue.preWrapMode,
                            postWrapMode = sourceProperty.animationCurveValue.postWrapMode
                        };
                    break;
                default:
                    throw new NotSupportedException(
                        $"지원하지 않는 설정 형식입니다: {sourcePath} ({sourceProperty.propertyType})");
            }
        }

        private static void CopyArray(
            SerializedObject source,
            string sourcePath,
            SerializedObject target,
            string targetPath)
        {
            SerializedProperty sourceArray = source.FindProperty(sourcePath);
            SerializedProperty targetArray = target.FindProperty(targetPath);
            targetArray.arraySize = sourceArray.arraySize;
            for (int index = 0; index < sourceArray.arraySize; index++)
            {
                targetArray.GetArrayElementAtIndex(index).objectReferenceValue =
                    sourceArray.GetArrayElementAtIndex(index).objectReferenceValue;
            }
        }

        private static void EnsureConfigFolder()
        {
            const string parent =
                "Assets/_Project/Characters/Player";
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder(parent, "Configs");
            }
        }
    }
}
