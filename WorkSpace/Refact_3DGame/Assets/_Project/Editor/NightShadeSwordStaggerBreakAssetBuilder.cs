using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace rudIsland.RPG3D.EditorTools
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
        private const string HipsPath = "mixamorig:Hips";
        private const string LocalPositionX = "m_LocalPosition.x";
        private const string LocalPositionZ = "m_LocalPosition.z";
        private const float StaggerEnterPlaybackSpeed = 1.2f;

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
            KeepHorizontalPositionInPlace(enterClip);

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            RemoveState(stateMachine, "Stagger Break Enter");
            RemoveState(stateMachine, "Stagger Break Stay");
            RemoveState(stateMachine, "Stagger Break Recover");
            AddOrUpdateState(
                stateMachine,
                "Stagger Enter",
                enterClip,
                new Vector3(0f, 750f),
                StaggerEnterPlaybackSpeed);
            AddOrUpdateState(
                stateMachine,
                "Stagger Start",
                startClip,
                new Vector3(220f, 750f),
                1f);
            AddOrUpdateState(
                stateMachine,
                "Stagger Idle",
                idleClip,
                new Vector3(440f, 750f),
                1f);
            AddOrUpdateState(
                stateMachine,
                "Stagger End",
                endClip,
                new Vector3(660f, 750f),
                1f);
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

        // Enter의 수평 이동을 제거해 월드 위치는 CharacterController만 변경한다.
        private static void KeepHorizontalPositionInPlace(
            AnimationClip clip)
        {
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(clip);
            for (int bindingIndex = 0;
                bindingIndex < bindings.Length;
                bindingIndex++)
            {
                EditorCurveBinding binding = bindings[bindingIndex];
                if (binding.type != typeof(Transform) ||
                    binding.path != HipsPath ||
                    (binding.propertyName != LocalPositionX &&
                        binding.propertyName != LocalPositionZ))
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
                for (int keyIndex = 0;
                    keyIndex < keys.Length;
                    keyIndex++)
                {
                    Keyframe key = keys[keyIndex];
                    key.value = 0f;
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

        private static void AddOrUpdateState(
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
