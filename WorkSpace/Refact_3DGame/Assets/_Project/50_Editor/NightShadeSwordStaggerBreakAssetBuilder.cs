using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EditorTools
{
    // NightShade의 경직 붕괴 전용 클립과 Animator 상태만 갱신한다.
    public static class NightShadeSwordStaggerBreakAssetBuilder
    {
        private const string ControllerPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Controllers/NightShadeTwoHandSwordAnimator.controller";
        private const string HitClipFolder =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/Hit";
        private const string ConfigPath =
            "Assets/_Project/Characters/Enemies/NightShade/Configs/NightShadeSwordEliteConfig.asset";
        private const string EnterClipPath =
            HitClipFolder + "/StaggerEnter.anim";
        private const string StartClipPath =
            HitClipFolder + "/StaggerStart.anim";
        private const string IdleClipPath =
            HitClipFolder + "/StaggerIdle.anim";
        private const string EndClipPath =
            HitClipFolder + "/StaggerEnd.anim";
        private const string RootPositionX = "RootT.x";
        private const string RootPositionZ = "RootT.z";
        private const float StaggerEnterPlaybackSpeed = 1.2f;
        private const float StaggerStartPlaybackSpeed = 1.15f;
        private const string StaggerEnterTrigger = "StaggerEnter";
        private const string StaggerStartTrigger = "StaggerStart";
        private const string StaggerIdleTrigger = "StaggerIdle";
        private const string StaggerEndTrigger = "StaggerEnd";

        [MenuItem("Tools/rudIsland/Apply NightShade Stagger Break")]
        public static void Apply()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "NightShade Animator Controller를 찾지 못했습니다.");
            }

            PrepareInspectorForAssetUpdate();
            ApplyToController(controller);
            AssetDatabase.SaveAssets();
            PrepareInspectorForAssetUpdate();
            Debug.Log("NightShade 경직 붕괴 애니메이션을 적용했습니다.");
        }

        // 애니메이션 미리보기 객체를 닫고 변경하지 않는 Config를 Inspector에 표시한다.
        internal static void PrepareInspectorForAssetUpdate()
        {
            AnimationMode.StopAnimationMode();
            ActiveEditorTracker tracker =
                ActiveEditorTracker.sharedTracker;
            if (tracker != null)
            {
                tracker.isLocked = false;
            }

            Selection.activeObject =
                AssetDatabase.LoadMainAssetAtPath(ConfigPath);
            tracker?.ForceRebuild();
        }

        internal static void ApplyToController(AnimatorController controller)
        {
            AnimationClip enterClip = LoadClip(EnterClipPath);
            AnimationClip startClip = LoadClip(StartClipPath);
            AnimationClip idleClip = LoadClip(IdleClipPath);
            AnimationClip endClip = LoadClip(EndClipPath);
            ConfigureClip(enterClip, false);
            ConfigureClip(startClip, false);
            ConfigureClip(idleClip, true);
            ConfigureClip(endClip, false);
            KeepHorizontalRootPositionInPlace(enterClip);

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            RemoveState(stateMachine, "Stagger Break Enter");
            RemoveState(stateMachine, "Stagger Break Stay");
            RemoveState(stateMachine, "Stagger Break Recover");
            AnimatorState enterState = AddOrUpdateState(
                stateMachine,
                "Stagger Enter",
                enterClip,
                new Vector3(0f, 750f),
                StaggerEnterPlaybackSpeed);
            AnimatorState startState = AddOrUpdateState(
                stateMachine,
                "Stagger Start",
                startClip,
                new Vector3(220f, 750f),
                StaggerStartPlaybackSpeed);
            AnimatorState idleState = AddOrUpdateState(
                stateMachine,
                "Stagger Idle",
                idleClip,
                new Vector3(440f, 750f),
                1f);
            AnimatorState endState = AddOrUpdateState(
                stateMachine,
                "Stagger End",
                endClip,
                new Vector3(660f, 750f),
                1f);
            AddTrigger(controller, StaggerEnterTrigger);
            AddTrigger(controller, StaggerStartTrigger);
            AddTrigger(controller, StaggerIdleTrigger);
            AddTrigger(controller, StaggerEndTrigger);
            AddAnyStateTransition(
                stateMachine,
                enterState,
                StaggerEnterTrigger);
            AddTransition(
                enterState,
                startState,
                StaggerStartTrigger);
            AddTransition(
                startState,
                idleState,
                StaggerIdleTrigger);
            AddTransition(
                idleState,
                endState,
                StaggerEndTrigger);
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureClip(
            AnimationClip clip,
            bool loopTime)
        {
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loopTime;
            settings.loopBlend = loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimationUtility.SetAnimationEvents(
                clip,
                Array.Empty<AnimationEvent>());
            EditorUtility.SetDirty(clip);
        }

        // Enter의 수평 Root 이동을 제거해 월드 위치는 CharacterController만 변경한다.
        private static void KeepHorizontalRootPositionInPlace(
            AnimationClip clip)
        {
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(clip);
            for (int bindingIndex = 0;
                bindingIndex < bindings.Length;
                bindingIndex++)
            {
                EditorCurveBinding binding = bindings[bindingIndex];
                if (binding.type != typeof(Animator) ||
                    !string.IsNullOrEmpty(binding.path) ||
                    (binding.propertyName != RootPositionX &&
                        binding.propertyName != RootPositionZ))
                {
                    continue;
                }

                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0)
                {
                    continue;
                }

                Keyframe[] keys = curve.keys;
                float firstPosition = keys[0].value;
                for (int keyIndex = 0;
                    keyIndex < keys.Length;
                    keyIndex++)
                {
                    Keyframe key = keys[keyIndex];
                    key.value = firstPosition;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    keys[keyIndex] = key;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            EditorUtility.SetDirty(clip);
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException(
                $"애니메이션 클립을 찾지 못했습니다: {assetPath}");
        }

        private static AnimatorState AddOrUpdateState(
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
                }
                else
                {
                    stateMachine.RemoveState(state);
                }
            }

            if (keptState == null)
            {
                keptState = stateMachine.AddState(stateName, position);
            }
            else
            {
                childStates = stateMachine.states;
                for (int index = 0; index < childStates.Length; index++)
                {
                    if (childStates[index].state != keptState)
                    {
                        continue;
                    }

                    childStates[index].position = position;
                    stateMachine.states = childStates;
                    break;
                }
            }

            keptState.motion = clip;
            keptState.speed = playbackSpeed;
            keptState.cycleOffset = 0f;
            keptState.mirror = false;
            keptState.writeDefaultValues = true;
            keptState.speedParameterActive = false;
            EditorUtility.SetDirty(keptState);
            return keptState;
        }

        private static void AddTrigger(
            AnimatorController controller,
            string triggerName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name == triggerName)
                {
                    return;
                }
            }

            controller.AddParameter(
                triggerName,
                AnimatorControllerParameterType.Trigger);
        }

        // 기존 Transition은 건드리지 않아 Inspector에서 조정한 시간을 보존한다.
        private static void AddAnyStateTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState destinationState,
            string triggerName)
        {
            AnimatorStateTransition[] transitions =
                stateMachine.anyStateTransitions;
            for (int index = 0; index < transitions.Length; index++)
            {
                if (transitions[index].destinationState == destinationState &&
                    HasCondition(transitions[index], triggerName))
                {
                    return;
                }
            }

            AnimatorStateTransition transition =
                stateMachine.AddAnyStateTransition(destinationState);
            ConfigureNewTransition(
                transition,
                triggerName);
            transition.canTransitionToSelf = false;
        }

        // 기존 Transition은 건드리지 않아 Inspector에서 조정한 시간을 보존한다.
        private static void AddTransition(
            AnimatorState sourceState,
            AnimatorState destinationState,
            string triggerName)
        {
            AnimatorStateTransition[] transitions = sourceState.transitions;
            for (int index = 0; index < transitions.Length; index++)
            {
                if (transitions[index].destinationState == destinationState &&
                    HasCondition(transitions[index], triggerName))
                {
                    return;
                }
            }

            AnimatorStateTransition transition =
                sourceState.AddTransition(destinationState);
            ConfigureNewTransition(
                transition,
                triggerName);
        }

        private static void ConfigureNewTransition(
            AnimatorStateTransition transition,
            string triggerName)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.offset = 0f;
            transition.interruptionSource =
                TransitionInterruptionSource.None;
            transition.AddCondition(
                AnimatorConditionMode.If,
                0f,
                triggerName);
            EditorUtility.SetDirty(transition);
        }

        private static bool HasCondition(
            AnimatorStateTransition transition,
            string parameterName)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int index = 0; index < conditions.Length; index++)
            {
                if (conditions[index].parameter == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveState(
            AnimatorStateMachine stateMachine,
            string stateName)
        {
            ChildAnimatorState[] childStates = stateMachine.states;
            for (int index = 0; index < childStates.Length; index++)
            {
                if (childStates[index].state.name == stateName)
                {
                    stateMachine.RemoveState(childStates[index].state);
                }
            }
        }
    }
}
