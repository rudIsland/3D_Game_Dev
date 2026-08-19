using System;
using System.Collections.Generic;
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
        private const string AnimationClipsFolder =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips";
        private const string AnimationSourcesFolder =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Sources";
        private const string IdleClipPath =
            AnimationClipsFolder + "/Idle/NightShadeSword_Idle.anim";
        private const string ChaseClipPath =
            AnimationClipsFolder + "/Run/NightShadeSword_Chase.anim";
        private const string WalkClipPath =
            AnimationClipsFolder + "/Walk/NightShadeSword_Walk.anim";
        private const string CombatBackClipPath =
            AnimationClipsFolder + "/Walk/NightShadeSword_CombatBack.anim";
        private const string CombatLeftClipPath =
            AnimationClipsFolder + "/Walk/NightShadeSword_CombatLeft.anim";
        private const string CombatRightClipPath =
            AnimationClipsFolder + "/Walk/NightShadeSword_CombatRight.anim";
        private const string LightAttackClipPath =
            AnimationClipsFolder + "/Attack/NightShadeSword_LightAttack.anim";
        private const string ComboFirstClipPath =
            AnimationClipsFolder + "/Attack/NightShadeSword_ComboFirst.anim";
        private const string ComboSecondClipPath =
            AnimationClipsFolder + "/Attack/NightShadeSword_ComboSecond.anim";
        private const string HeavyAttackClipPath =
            AnimationClipsFolder + "/Attack/NightShadeSword_HeavyAttack.anim";
        private const string WideSwingClipPath =
            AnimationClipsFolder + "/Attack/NightShadeSword_WideSwing.anim";
        private const string DeadClipPath =
            AnimationClipsFolder + "/Death/NightShadeSword_Dead.anim";
        private const string BigHitClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_BigHit.anim";
        private const string KnockbackClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_Knockback.anim";
        private const string KnockdownClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_Knockdown.anim";
        private const string GetUpClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_GetUp.anim";
        private const string SmallHitFrontClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_SmallHitFront.anim";
        private const string SmallHitBackClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_SmallHitBack.anim";
        private const string SmallHitLeftClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_SmallHitLeft.anim";
        private const string SmallHitRightClipPath =
            AnimationClipsFolder + "/Hit/NightShadeSword_SmallHitRight.anim";
        private const string AttackSpeedParameterName = "AttackSpeed";

        private const string IdleSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Idle_A_1.fbx";
        private const string ChaseSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Run_A_F_InPlace.fbx";
        private const string WalkSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Walk_A_F_InPlace.fbx";
        private const string CombatBackSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Walk_Slow_B_B_InPlace.fbx";
        private const string CombatLeftSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Block_Walk_Slow_F_L90_A_InPlace.fbx";
        private const string CombatRightSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Block_Walk_Slow_F_R90_A_InPlace.fbx";
        private const string LightAttackSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Attack_A_1.fbx";
        private const string ComboFirstSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Attack_A_1.fbx";
        private const string ComboSecondSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Attack_A_2_Combo.fbx";
        private const string HeavyAttackSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Attack_B_1.fbx";
        private const string WideSwingSourcePath =
            AnimationSourcesFolder + "/2Hand_Up_Attack_A_3.fbx";
        private const string DeadSourcePath =
            AnimationSourcesFolder + "/@anim_Sword_death.FBX";
        private const string BigHitSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-GetHit-F2.FBX";
        private const string KnockbackSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-Knockback-Back2.FBX";
        private const string KnockdownSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-Knockdown1.FBX";
        private const string GetUpSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-Getup1.FBX";
        private const string SmallHitFrontSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-GetHit-F1.FBX";
        private const string SmallHitBackSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-GetHit-B1.FBX";
        private const string SmallHitLeftSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-GetHit-L1.FBX";
        private const string SmallHitRightSourcePath =
            AnimationSourcesFolder + "/RPG-Character@2Hand-Sword-GetHit-R1.FBX";
        private const float SwordLength = 1.5f;
        private const float SwordHitRadius = 0.13f;
        private const float KnockbackPlaybackSpeed = 1f;
        private const float KnockdownPlaybackSpeed = 1f;
        private const float GetUpPlaybackSpeed = 1.5f;
        private const float SmallHitFrontBackPlaybackSpeed = 1.25f;
        private const float SmallHitLeftRightPlaybackSpeed = 1f;
        private const float BigHitPlaybackSpeed = 1f;
        private const float DeadPlaybackSpeed = 0.8f;
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
            Debug.Log("NightShadeSwordElite 프리팹과 양손검 Animator를 만들었습니다.");
        }

        [MenuItem("Tools/rudIsland/Apply NightShade Sword Attack Events")]
        public static void ApplyAttackAnimationEvents()
        {
            ApplyAttackAnimationEvents(LoadClip(LightAttackClipPath), CreateLightAttackEvents());
            ApplyAttackAnimationEvents(LoadClip(ComboFirstClipPath), CreateComboFirstEvents());
            ApplyAttackAnimationEvents(LoadClip(ComboSecondClipPath), CreateComboSecondEvents());
            ApplyAttackAnimationEvents(LoadClip(HeavyAttackClipPath), CreateHeavyAttackEvents());
            ApplyAttackAnimationEvents(LoadClip(WideSwingClipPath), CreateWideSwingAttackEvents());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NightShade 양손검 공격 이벤트만 적용했습니다.");
        }

        [MenuItem("Tools/rudIsland/Apply NightShade Sword Sound Timing")]
        public static void ApplyAttackSoundTiming()
        {
            ApplyAttackSoundTiming(
                LoadClip(LightAttackClipPath),
                0.36666667f);
            ApplyAttackSoundTiming(
                LoadClip(ComboFirstClipPath),
                0.36666667f);
            ApplyAttackSoundTiming(
                LoadClip(ComboSecondClipPath),
                0.23333334f);
            ApplyAttackSoundTiming(
                LoadClip(HeavyAttackClipPath),
                0.6333333f);
            ApplyAttackSoundTiming(
                LoadClip(WideSwingClipPath),
                0.43333334f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NightShade 양손검 사운드 이벤트 시간만 적용했습니다.");
        }

        [MenuItem("Tools/rudIsland/Apply NightShade Sword Split Combo")]
        public static void ApplySplitCombo()
        {
            EnsureAssetFolder(AnimationClipsFolder);
            AnimationClip comboFirstClip = CopyAnimationClip(
                ComboFirstSourcePath,
                ComboFirstClipPath,
                false);
            AnimationClip comboSecondClip = CopyAnimationClip(
                ComboSecondSourcePath,
                ComboSecondClipPath,
                false);
            ApplyAttackAnimationEvents(
                comboFirstClip,
                CreateComboFirstEvents());
            ApplyAttackAnimationEvents(
                comboSecondClip,
                CreateComboSecondEvents());

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    AnimatorControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"NightShade Animator Controller를 찾지 못했습니다: {AnimatorControllerPath}");
            }

            AddOrUpdateSplitComboStates(
                controller,
                comboFirstClip,
                comboSecondClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NightShade 양손검 Combo First와 Combo Second만 적용했습니다.");
        }

        [MenuItem("Tools/rudIsland/Apply NightShade Sword Walk")]
        public static void ApplyWalkAnimation()
        {
            EnsureAssetFolder(AnimationClipsFolder);
            AnimationClip walkClip = CopyAnimationClip(
                WalkSourcePath,
                WalkClipPath,
                true);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"NightShade Animator Controller를 찾지 못했습니다: {AnimatorControllerPath}");
            }

            AddOrUpdateWalkState(controller, walkClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NightShade 양손검 Walk 클립과 상태만 적용했습니다.");
        }

        [MenuItem("Tools/rudIsland/Apply NightShade Hit Reactions")]
        public static void ApplyHitReactionAnimations()
        {
            EnsureAssetFolder(AnimationClipsFolder);
            AnimationClip smallHitFrontClip =
                CopyReactionAnimationClip(
                    SmallHitFrontSourcePath,
                    SmallHitFrontClipPath);
            AnimationClip smallHitBackClip =
                CopyReactionAnimationClip(
                    SmallHitBackSourcePath,
                    SmallHitBackClipPath);
            AnimationClip smallHitLeftClip =
                CopyReactionAnimationClip(
                    SmallHitLeftSourcePath,
                    SmallHitLeftClipPath);
            AnimationClip smallHitRightClip =
                CopyReactionAnimationClip(
                    SmallHitRightSourcePath,
                    SmallHitRightClipPath);
            AnimationClip knockbackClip = CopyReactionAnimationClip(
                KnockbackSourcePath,
                KnockbackClipPath);
            AnimationClip knockdownClip = CopyReactionAnimationClip(
                KnockdownSourcePath,
                KnockdownClipPath);
            AnimationClip getUpClip = CopyReactionAnimationClip(
                GetUpSourcePath,
                GetUpClipPath);
            AnimationClip bigHitClip = CopyReactionAnimationClip(
                BigHitSourcePath,
                BigHitClipPath);
            AnimationClip deadClip = CopyReactionAnimationClip(
                DeadSourcePath,
                DeadClipPath);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    AnimatorControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"NightShade Animator Controller를 찾지 못했습니다: {AnimatorControllerPath}");
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            AddOrUpdateReactionState(
                stateMachine,
                "Small Hit Front",
                smallHitFrontClip,
                new Vector3(0f, 270f),
                SmallHitFrontBackPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Small Hit Back",
                smallHitBackClip,
                new Vector3(130f, 270f),
                SmallHitFrontBackPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Small Hit Left",
                smallHitLeftClip,
                new Vector3(260f, 270f),
                SmallHitLeftRightPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Small Hit Right",
                smallHitRightClip,
                new Vector3(390f, 270f),
                SmallHitLeftRightPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Knockback",
                knockbackClip,
                new Vector3(520f, 180f),
                KnockbackPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Knockdown",
                knockdownClip,
                new Vector3(650f, 180f),
                KnockdownPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Get Up",
                getUpClip,
                new Vector3(780f, 180f),
                GetUpPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Hit Front",
                bigHitClip,
                new Vector3(0f, 180f),
                BigHitPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Hit Back",
                bigHitClip,
                new Vector3(130f, 180f),
                BigHitPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Hit Left",
                bigHitClip,
                new Vector3(260f, 180f),
                BigHitPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Hit Right",
                bigHitClip,
                new Vector3(390f, 180f),
                BigHitPlaybackSpeed);
            AddOrUpdateReactionState(
                stateMachine,
                "Dead",
                deadClip,
                new Vector3(910f, 180f),
                DeadPlaybackSpeed);
            ApplyAttackAnimationEvents(
                LoadClip(HeavyAttackClipPath),
                CreateHeavyAttackEvents());
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("NightShade 피격, 넉백, 다운과 사망 애니메이션을 적용했습니다.");
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
            EnsureAssetFolder(AnimationClipsFolder);
            AnimationClip idleClip = CopyAnimationClip(
                IdleSourcePath,
                IdleClipPath,
                true);
            AnimationClip chaseClip = CopyAnimationClip(
                ChaseSourcePath,
                ChaseClipPath,
                true);
            AnimationClip walkClip = CopyAnimationClip(
                WalkSourcePath,
                WalkClipPath,
                true);
            AnimationClip combatBackClip = CopyAnimationClip(
                CombatBackSourcePath,
                CombatBackClipPath,
                true);
            AnimationClip combatLeftClip = CopyAnimationClip(
                CombatLeftSourcePath,
                CombatLeftClipPath,
                true);
            AnimationClip combatRightClip = CopyAnimationClip(
                CombatRightSourcePath,
                CombatRightClipPath,
                true);
            AnimationClip lightAttackClip = CopyAnimationClip(
                LightAttackSourcePath,
                LightAttackClipPath,
                false);
            AnimationClip comboFirstClip = CopyAnimationClip(
                ComboFirstSourcePath,
                ComboFirstClipPath,
                false);
            AnimationClip comboSecondClip = CopyAnimationClip(
                ComboSecondSourcePath,
                ComboSecondClipPath,
                false);
            AnimationClip heavyAttackClip = CopyAnimationClip(
                HeavyAttackSourcePath,
                HeavyAttackClipPath,
                false);
            AnimationClip wideSwingClip = CopyAnimationClip(
                WideSwingSourcePath,
                WideSwingClipPath,
                false);
            AnimationClip deadClip = CopyReactionAnimationClip(
                DeadSourcePath,
                DeadClipPath);
            AnimationClip bigHitClip = CopyReactionAnimationClip(
                BigHitSourcePath,
                BigHitClipPath);
            AnimationClip knockbackClip = CopyReactionAnimationClip(
                KnockbackSourcePath,
                KnockbackClipPath);
            AnimationClip knockdownClip = CopyReactionAnimationClip(
                KnockdownSourcePath,
                KnockdownClipPath);
            AnimationClip getUpClip = CopyReactionAnimationClip(
                GetUpSourcePath,
                GetUpClipPath);
            AnimationClip smallHitFrontClip =
                CopyReactionAnimationClip(
                    SmallHitFrontSourcePath,
                    SmallHitFrontClipPath);
            AnimationClip smallHitBackClip =
                CopyReactionAnimationClip(
                    SmallHitBackSourcePath,
                    SmallHitBackClipPath);
            AnimationClip smallHitLeftClip =
                CopyReactionAnimationClip(
                    SmallHitLeftSourcePath,
                    SmallHitLeftClipPath);
            AnimationClip smallHitRightClip =
                CopyReactionAnimationClip(
                    SmallHitRightSourcePath,
                    SmallHitRightClipPath);

            ApplyAttackAnimationEvents(lightAttackClip, CreateLightAttackEvents());
            ApplyAttackAnimationEvents(comboFirstClip, CreateComboFirstEvents());
            ApplyAttackAnimationEvents(comboSecondClip, CreateComboSecondEvents());
            ApplyAttackAnimationEvents(heavyAttackClip, CreateHeavyAttackEvents());
            ApplyAttackAnimationEvents(wideSwingClip, CreateWideSwingAttackEvents());

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }

            EnsureFloatParameter(controller, AttackSpeedParameterName);
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
                "Walk",
                walkClip,
                new Vector3(390f, 0f));
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
                "Combo First",
                comboFirstClip,
                new Vector3(260f, 90f),
                true);
            AddState(
                stateMachine,
                "Combo Second",
                comboSecondClip,
                new Vector3(390f, 90f),
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
                "Small Hit Front",
                smallHitFrontClip,
                new Vector3(0f, 270f),
                playbackSpeed:
                    SmallHitFrontBackPlaybackSpeed);
            AddState(
                stateMachine,
                "Small Hit Back",
                smallHitBackClip,
                new Vector3(130f, 270f),
                playbackSpeed:
                    SmallHitFrontBackPlaybackSpeed);
            AddState(
                stateMachine,
                "Small Hit Left",
                smallHitLeftClip,
                new Vector3(260f, 270f),
                playbackSpeed:
                    SmallHitLeftRightPlaybackSpeed);
            AddState(
                stateMachine,
                "Small Hit Right",
                smallHitRightClip,
                new Vector3(390f, 270f),
                playbackSpeed:
                    SmallHitLeftRightPlaybackSpeed);
            AddState(
                stateMachine,
                "Hit Front",
                bigHitClip,
                new Vector3(0f, 180f),
                playbackSpeed: BigHitPlaybackSpeed);
            AddState(
                stateMachine,
                "Hit Back",
                bigHitClip,
                new Vector3(130f, 180f),
                playbackSpeed: BigHitPlaybackSpeed);
            AddState(
                stateMachine,
                "Hit Left",
                bigHitClip,
                new Vector3(260f, 180f),
                playbackSpeed: BigHitPlaybackSpeed);
            AddState(
                stateMachine,
                "Hit Right",
                bigHitClip,
                new Vector3(390f, 180f),
                playbackSpeed: BigHitPlaybackSpeed);
            AddState(
                stateMachine,
                "Knockback",
                knockbackClip,
                new Vector3(520f, 180f),
                playbackSpeed: KnockbackPlaybackSpeed);
            AddState(
                stateMachine,
                "Knockdown",
                knockdownClip,
                new Vector3(650f, 180f),
                playbackSpeed: KnockdownPlaybackSpeed);
            AddState(
                stateMachine,
                "Get Up",
                getUpClip,
                new Vector3(780f, 180f),
                playbackSpeed: GetUpPlaybackSpeed);
            AddState(
                stateMachine,
                "Dead",
                deadClip,
                new Vector3(910f, 180f),
                playbackSpeed: DeadPlaybackSpeed);
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            Vector3 position,
            bool usesAttackSpeed = false,
            float playbackSpeed = 1f)
        {
            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = clip;
            state.speed = Mathf.Max(0.01f, playbackSpeed);
            state.writeDefaultValues = true;
            if (usesAttackSpeed)
            {
                state.speedParameterActive = true;
                state.speedParameter = AttackSpeedParameterName;
            }

            return state;
        }

        private static void AddOrUpdateWalkState(AnimatorController controller, AnimationClip walkClip)
        {
            if (controller.layers.Length == 0)
            {
                throw new InvalidOperationException("NightShade Animator Controller에 Base Layer가 없습니다.");
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            ChildAnimatorState[] childStates = stateMachine.states;
            AnimatorState walkState = null;
            for (int index = 0; index < childStates.Length; index++)
            {
                AnimatorState state = childStates[index].state;
                if (state.name != "Walk")
                {
                    continue;
                }

                if (walkState == null)
                {
                    walkState = state;
                    continue;
                }

                stateMachine.RemoveState(state);
            }

            if (walkState == null)
            {
                walkState = stateMachine.AddState("Walk", new Vector3(390f, 0f));
            }

            walkState.motion = walkClip;
            walkState.writeDefaultValues = true;
            walkState.speedParameterActive = false;
            EditorUtility.SetDirty(walkState);
            EditorUtility.SetDirty(controller);
        }

        private static void AddOrUpdateSplitComboStates(
            AnimatorController controller,
            AnimationClip comboFirstClip,
            AnimationClip comboSecondClip)
        {
            if (controller.layers.Length == 0)
            {
                throw new InvalidOperationException(
                    "NightShade Animator Controller에 Base Layer가 없습니다.");
            }

            EnsureFloatParameter(controller, AttackSpeedParameterName);
            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            RemoveStatesByName(stateMachine, "Combo Attack");
            AddOrUpdateAttackState(
                stateMachine,
                "Combo First",
                comboFirstClip,
                new Vector3(260f, 90f));
            AddOrUpdateAttackState(
                stateMachine,
                "Combo Second",
                comboSecondClip,
                new Vector3(390f, 90f));
            EditorUtility.SetDirty(controller);
        }

        private static void AddOrUpdateAttackState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            Vector3 position)
        {
            ChildAnimatorState[] childStates = stateMachine.states;
            AnimatorState keptState = null;
            for (int index = 0; index < childStates.Length; index++)
            {
                AnimatorState state = childStates[index].state;
                if (state.name != stateName)
                {
                    continue;
                }

                if (keptState == null)
                {
                    keptState = state;
                    continue;
                }

                stateMachine.RemoveState(state);
            }

            if (keptState == null)
            {
                keptState = stateMachine.AddState(stateName, position);
            }

            keptState.motion = clip;
            keptState.writeDefaultValues = true;
            keptState.speedParameterActive = true;
            keptState.speedParameter = AttackSpeedParameterName;
            EditorUtility.SetDirty(keptState);
        }

        private static void AddOrUpdateReactionState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            Vector3 position,
            float playbackSpeed)
        {
            ChildAnimatorState[] childStates = stateMachine.states;
            AnimatorState keptState = null;
            for (int index = 0; index < childStates.Length; index++)
            {
                AnimatorState state = childStates[index].state;
                if (state.name != stateName)
                {
                    continue;
                }

                if (keptState == null)
                {
                    keptState = state;
                    continue;
                }

                stateMachine.RemoveState(state);
            }

            if (keptState == null)
            {
                keptState = stateMachine.AddState(stateName, position);
            }

            keptState.motion = clip;
            keptState.speed = Mathf.Max(0.01f, playbackSpeed);
            keptState.writeDefaultValues = true;
            keptState.speedParameterActive = false;
            EditorUtility.SetDirty(keptState);
        }

        private static void RemoveStatesByName(
            AnimatorStateMachine stateMachine,
            string stateName)
        {
            ChildAnimatorState[] childStates = stateMachine.states;
            for (int index = 0; index < childStates.Length; index++)
            {
                AnimatorState state = childStates[index].state;
                if (state.name == stateName)
                {
                    stateMachine.RemoveState(state);
                }
            }
        }

        private static void EnsureFloatParameter(AnimatorController controller, string parameterName)
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
                    throw new InvalidOperationException($"{parameterName} Animator 파라미터는 Float여야 합니다.");
                }

                return;
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
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
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException($"애니메이션 클립을 찾지 못했습니다: {assetPath}");
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

        private static AnimationClip CopyReactionAnimationClip(
            string sourcePath,
            string copyPath)
        {
            AnimationClip clip = CopyAnimationClip(
                sourcePath,
                copyPath,
                false);
            AnimationUtility.SetAnimationEvents(
                clip,
                Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationEvent[] CreateLightAttackEvents()
        {
            return new[]
            {
                CreateAttackSpeedEvent(0f, 0.5f),
                CreateAttackSpeedResetEvent(0.2f),
                CreateAttackEvent(
                    0.4f,
                    "StopAttackTurnAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.36666667f,
                    "PlayAttackSoundAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.53333336f,
                    "OpenAttackHitAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.7f,
                    "CloseAttackHitAnimationEvent",
                    0)
            };
        }

        private static AnimationEvent[] CreateComboFirstEvents()
        {
            return new[]
            {
                CreateAttackSpeedEvent(0.033333335f, 0.6f),
                CreateAttackSpeedResetEvent(0.23333334f),
                CreateAttackEvent(
                    0.36666667f,
                    "PlayAttackSoundAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.4f,
                    "StopAttackTurnAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.53333336f,
                    "OpenAttackHitAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.6666667f,
                    "CloseAttackHitAnimationEvent",
                    0)
            };
        }

        private static AnimationEvent[] CreateComboSecondEvents()
        {
            return new[]
            {
                CreateAttackSpeedEvent(0.033333335f, 0.8f),
                CreateAttackSpeedResetEvent(0.13333334f),
                CreateAttackEvent(
                    0.33333334f,
                    "StopAttackTurnAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.23333334f,
                    "PlayAttackSoundAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.36666667f,
                    "OpenAttackHitAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.46666667f,
                    "CloseAttackHitAnimationEvent",
                    0),
            };
        }

        private static AnimationEvent[] CreateHeavyAttackEvents()
        {
            return new[]
            {
                CreateAttackSpeedEvent(0f, 0.5f),
                CreateAttackSpeedResetEvent(0.46666667f),
                CreateAttackEvent(
                    0.7f,
                    "StopAttackTurnAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.6333333f,
                    "PlayAttackSoundAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.6f,
                    "OpenAttackHitAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.9666667f,
                    "CloseAttackHitAnimationEvent",
                    0)
            };
        }

        private static AnimationEvent[] CreateWideSwingAttackEvents()
        {
            return new[]
            {
                CreateAttackSpeedEvent(0.033333335f, 0.5f),
                CreateAttackSpeedResetEvent(0.36666667f),
                CreateAttackEvent(
                    0.46666667f,
                    "StopAttackTurnAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.43333334f,
                    "PlayAttackSoundAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.6f,
                    "OpenAttackHitAnimationEvent",
                    0),
                CreateAttackEvent(
                    0.7666667f,
                    "CloseAttackHitAnimationEvent",
                    0)
            };
        }

        private static void ApplyAttackAnimationEvents(AnimationClip clip, AnimationEvent[] nextAttackEvents)
        {
            AnimationEvent[] oldEvents =
                AnimationUtility.GetAnimationEvents(clip);
            var eventsToKeep = new List<AnimationEvent>(oldEvents.Length + nextAttackEvents.Length);
            for (int index = 0; index < oldEvents.Length; index++)
            {
                if (!IsNightShadeAttackEvent(oldEvents[index].functionName))
                {
                    eventsToKeep.Add(oldEvents[index]);
                }
            }

            eventsToKeep.AddRange(nextAttackEvents);
            eventsToKeep.Sort(CompareAnimationEvents);
            AnimationUtility.SetAnimationEvents(clip, eventsToKeep.ToArray());
            EditorUtility.SetDirty(clip);
        }

        private static void ApplyAttackSoundTiming(
            AnimationClip clip,
            float soundTime)
        {
            AnimationEvent[] oldEvents =
                AnimationUtility.GetAnimationEvents(clip);
            var nextEvents =
                new List<AnimationEvent>(oldEvents.Length);
            for (int index = 0; index < oldEvents.Length; index++)
            {
                AnimationEvent animationEvent = oldEvents[index];
                if (animationEvent.functionName ==
                        "PlayAttackSoundAnimationEvent" ||
                    animationEvent.functionName ==
                        "SetAttackPlaybackSpeed")
                {
                    continue;
                }

                nextEvents.Add(animationEvent);
            }

            nextEvents.Add(CreateAttackEvent(
                soundTime,
                "PlayAttackSoundAnimationEvent",
                0));
            nextEvents.Sort(CompareAnimationEvents);
            AnimationUtility.SetAnimationEvents(
                clip,
                nextEvents.ToArray());
            EditorUtility.SetDirty(clip);
        }

        private static bool IsNightShadeAttackEvent(string functionName)
        {
            return functionName == "StopAttackTurnAnimationEvent" ||
                functionName == "PlayAttackSoundAnimationEvent" ||
                functionName == "OpenAttackHitAnimationEvent" ||
                functionName == "CloseAttackHitAnimationEvent" ||
                functionName == "SetAttackSpeed" ||
                functionName == "ResetAttackSpeed" ||
                functionName == "SetAttackPlaybackSpeed" ||
                functionName == "ResetAttackPlaybackSpeed" ||
                functionName == "BeginSuperArmorAnimationEvent" ||
                functionName == "EndSuperArmorAnimationEvent";
        }

        private static int CompareAnimationEvents(AnimationEvent left, AnimationEvent right)
        {
            int timeComparison = left.time.CompareTo(right.time);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            int nameComparison = string.CompareOrdinal(left.functionName, right.functionName);
            return nameComparison != 0
                ? nameComparison
                : left.intParameter.CompareTo(right.intParameter);
        }

        private static AnimationEvent CreateAttackEvent(
            float time,
            string functionName,
            int hitIndex)
        {
            return new AnimationEvent
            {
                time = time,
                functionName = functionName,
                intParameter = hitIndex
            };
        }

        private static AnimationEvent CreateAttackSpeedEvent(
            float time,
            float speed)
        {
            return new AnimationEvent
            {
                time = time,
                functionName = "SetAttackSpeed",
                floatParameter = speed
            };
        }

        private static AnimationEvent CreateAttackSpeedResetEvent(float time)
        {
            return new AnimationEvent
            {
                time = time,
                functionName = "ResetAttackSpeed"
            };
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
                    AssetDatabase.CreateFolder(currentPath, folderNames[index]);
                }

                currentPath = nextPath;
            }
        }

        private static NightShadeSwordController BuildEnemyPrefab(RuntimeAnimatorController animatorController)
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
                throw new InvalidOperationException("NightShade 모델을 프리팹 편집용으로 만들지 못했습니다.");
            }

            try
            {
                enemy.name = "NightShadeSwordElite";
                enemy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                enemy.transform.localScale = Vector3.one;
                SetEnemyLayer(enemy.transform);
                HideOldLance(enemy.transform);

                Animator animator =
                    enemy.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    throw new InvalidOperationException("NightShade 모델에서 Animator를 찾지 못했습니다.");
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
                    throw new InvalidOperationException("NightShade 모델에서 RightHand 뼈를 찾지 못했습니다.");
                }

                GameObject sword =
                    PrefabUtility.InstantiatePrefab(swordAsset, rightHand) as GameObject;
                if (sword == null)
                {
                    throw new InvalidOperationException("RustySword 프리팹을 오른손에 만들지 못했습니다.");
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
                eventReceiver.ConnectForEditor(animationController, swordController);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException("NightShadeSwordElite 프리팹을 저장하지 못했습니다.");
                }

                return savedPrefab.GetComponent<NightShadeSwordController>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        private static void ConnectSpawnSettings(NightShadeSwordController enemyPrefab)
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
                sword.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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

            sword.SetLocalPositionAndRotation(Vector3.zero, Quaternion.FromToRotation(bladeAxis, AuthoredSwordDirection));
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
                    child.name.IndexOf("lance", StringComparison.OrdinalIgnoreCase) < 0)
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
                throw new InvalidOperationException($"필수 에셋을 찾지 못했습니다: {assetPath}");
            }

            return asset;
        }
    }
}
