using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAnimationEventAssetTests
    {
        private const string ClipRoot =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips";
        private const string AttackClipFolder = ClipRoot + "/Attack";
        private const string HitClipFolder = ClipRoot + "/Hit";
        private const string DeathClipFolder = ClipRoot + "/Death";
        private const string ControllerPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Controllers/NightShadeTwoHandSwordAnimator.controller";
        private const string WalkClipPath =
            ClipRoot + "/Walk/NightShadeSword_Walk.anim";
        private const string StaggerEnterClipPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Sources/Stagger Enter.anim";

        [Test]
        public void AnimatorController_방향별피격상태에맞는클립이연결되어있다()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null, ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AssertStateClip(
                stateMachine,
                "Hit",
                "NightShadeSword_BigHitFront",
                0.70f);
            AssertStateClip(
                stateMachine,
                "Hit Front",
                "NightShadeSword_BigHitFront",
                0.70f);
            AssertStateClip(
                stateMachine,
                "Hit Back",
                "NightShadeSword_BigHitBack",
                0.70f);
            AssertStateClip(
                stateMachine,
                "Hit Left",
                "NightShadeSword_BigHitLeft",
                0.50f);
            AssertStateClip(
                stateMachine,
                "Hit Right",
                "NightShadeSword_BigHitRight",
                0.50f);
            AssertStateClip(
                stateMachine,
                "Small Hit Front",
                "NightShadeSword_SmallHitFront",
                1f);
            AssertStateClip(
                stateMachine,
                "Small Hit Back",
                "NightShadeSword_SmallHitBack",
                1f);
            AssertStateClip(
                stateMachine,
                "Small Hit Left",
                "NightShadeSword_SmallHitLeft",
                1f);
            AssertStateClip(
                stateMachine,
                "Small Hit Right",
                "NightShadeSword_SmallHitRight",
                1f);
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_BigHitFront.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_BigHitBack.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_BigHitLeft.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_BigHitRight.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_SmallHitFront.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_SmallHitBack.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_SmallHitLeft.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_SmallHitRight.anim");
            AssertStateClip(
                stateMachine,
                "Stagger Enter",
                "Stagger Enter");
            AssertStateClip(
                stateMachine,
                "Stagger Start",
                "NightShadeSword_StaggerStart");
            AssertStateClip(
                stateMachine,
                "Stagger Idle",
                "NightShadeSword_StaggerIdle");
            AssertStateClip(
                stateMachine,
                "Stagger End",
                "NightShadeSword_StaggerEnd");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_StaggerStart.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_StaggerIdle.anim");
            AssertNoEvents(
                $"{HitClipFolder}/NightShadeSword_StaggerEnd.anim");
            AssertNoEvents(StaggerEnterClipPath);
            AssertLoopTime(StaggerEnterClipPath, false);
            AssertLoopTime(
                $"{HitClipFolder}/NightShadeSword_StaggerStart.anim",
                false);
            AssertLoopTime(
                $"{HitClipFolder}/NightShadeSword_StaggerIdle.anim",
                true);
            AssertLoopTime(
                $"{HitClipFolder}/NightShadeSword_StaggerEnd.anim",
                false);
        }

        [Test]
        public void AnimatorController_상태를기능별가로행으로배치한다()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null, ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AssertStateRow(
                stateMachine,
                0f,
                "Idle",
                "Chase",
                "Walk",
                "Combat Back",
                "Combat Left",
                "Combat Right");
            AssertStateRow(
                stateMachine,
                150f,
                "Light Attack",
                "Combo First",
                "Combo Second",
                "Heavy Attack",
                "Wide Swing");
            AssertStateRow(
                stateMachine,
                300f,
                "Hit",
                "Knockback",
                "Knockdown",
                "Get Up",
                "Dead");
            AssertStateRow(
                stateMachine,
                450f,
                "Small Hit Front",
                "Small Hit Back",
                "Small Hit Left",
                "Small Hit Right");
            AssertStateRow(
                stateMachine,
                600f,
                "Hit Front",
                "Hit Back",
                "Hit Left",
                "Hit Right");
            AssertStateRow(
                stateMachine,
                750f,
                "Stagger Enter",
                "Stagger Start",
                "Stagger Idle",
                "Stagger End");
        }

        [Test]
        public void StaggerEnter클립_수평위치곡선은제자리다()
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    StaggerEnterClipPath);
            Assert.That(clip, Is.Not.Null, StaggerEnterClipPath);

            bool foundPositionX = false;
            bool foundPositionZ = false;
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(clip);
            for (int bindingIndex = 0;
                bindingIndex < bindings.Length;
                bindingIndex++)
            {
                EditorCurveBinding binding = bindings[bindingIndex];
                if (binding.type != typeof(Transform) ||
                    binding.path != "mixamorig:Hips" ||
                    (binding.propertyName != "m_LocalPosition.x" &&
                        binding.propertyName != "m_LocalPosition.z"))
                {
                    continue;
                }

                foundPositionX |=
                    binding.propertyName == "m_LocalPosition.x";
                foundPositionZ |=
                    binding.propertyName == "m_LocalPosition.z";
                AnimationCurve curve =
                    AnimationUtility.GetEditorCurve(clip, binding);
                Assert.That(curve, Is.Not.Null, binding.propertyName);
                Keyframe[] keys = curve.keys;
                for (int keyIndex = 0;
                    keyIndex < keys.Length;
                    keyIndex++)
                {
                    Assert.That(keys[keyIndex].value, Is.Zero);
                    Assert.That(keys[keyIndex].inTangent, Is.Zero);
                    Assert.That(keys[keyIndex].outTangent, Is.Zero);
                }
            }

            Assert.That(foundPositionX, Is.True);
            Assert.That(foundPositionZ, Is.True);
        }

        [Test]
        public void AnimatorController_강제반응상태에정리된클립과속도가연결되어있다()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null, ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AssertStateClip(
                stateMachine,
                "Knockback",
                "NightShadeSword_Knockback",
                1f);
            AssertStateClip(
                stateMachine,
                "Knockdown",
                "NightShadeSword_Knockdown",
                1f);
            AssertStateClip(
                stateMachine,
                "Get Up",
                "NightShadeSword_GetUp",
                1.5f);
            AssertStateClip(
                stateMachine,
                "Dead",
                "NightShadeSword_Dead",
                0.8f);
            AssertNoEvents($"{HitClipFolder}/NightShadeSword_Knockback.anim");
            AssertNoEvents($"{HitClipFolder}/NightShadeSword_Knockdown.anim");
            AssertNoEvents($"{HitClipFolder}/NightShadeSword_GetUp.anim");
            AssertNoEvents($"{DeathClipFolder}/NightShadeSword_Dead.anim");
        }

        [Test]
        public void AnimatorController_Walk상태는반복클립하나만사용한다()
        {
            AnimationClip walkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
            Assert.That(walkClip, Is.Not.Null, WalkClipPath);
            AnimationClipSettings clipSettings =
                AnimationUtility.GetAnimationClipSettings(walkClip);
            Assert.That(clipSettings.loopTime, Is.True);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null, ControllerPath);

            ChildAnimatorState[] states =
                controller.layers[0].stateMachine.states;
            int walkStateCount = 0;
            AnimatorState walkState = null;
            for (int index = 0; index < states.Length; index++)
            {
                if (states[index].state.name != "Walk")
                {
                    continue;
                }

                walkState = states[index].state;
                walkStateCount++;
            }

            Assert.That(walkStateCount, Is.EqualTo(1));
            Assert.That(walkState, Is.Not.Null);
            Assert.That(walkState.motion, Is.SameAs(walkClip));
        }

        [Test]
        public void AttackClips_검움직임에맞춘사운드와정확한게임이벤트를가진다()
        {
            AssertClip(
                $"{AttackClipFolder}/NightShadeSword_LightAttack.anim",
                2,
                new[]
                {
                    Expected(0.36666667f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.4f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.53333336f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.7f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{AttackClipFolder}/NightShadeSword_ComboFirst.anim",
                2,
                new[]
                {
                    Expected(0.36666667f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.4f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.53333336f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.6666667f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{AttackClipFolder}/NightShadeSword_ComboSecond.anim",
                2,
                new[]
                {
                    Expected(0.23333334f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.33333334f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.36666667f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.46666667f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{AttackClipFolder}/NightShadeSword_HeavyAttack.anim",
                2,
                new[]
                {
                    Expected(0.6f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.6333333f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.7f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.9666667f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{AttackClipFolder}/NightShadeSword_WideSwing.anim",
                2,
                new[]
                {
                    Expected(0.43333334f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.46666667f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.6f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.7666667f, "CloseAttackHitAnimationEvent", 0)
                });
        }

        private static void AssertClip(
            string clipPath,
            int expectedSpeedEventCount,
            ExpectedEvent[] expectedEvents)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null, clipPath);
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            var gameplayEvents = new List<AnimationEvent>();
            int speedEventCount = 0;
            int unsupportedSpeedEventCount = 0;
            for (int index = 0; index < events.Length; index++)
            {
                AnimationEvent animationEvent = events[index];
                if (animationEvent.functionName == "SetAttackSpeed" ||
                    animationEvent.functionName == "ResetAttackSpeed")
                {
                    speedEventCount++;
                }
                else if (animationEvent.functionName ==
                        "SetAttackPlaybackSpeed" ||
                    animationEvent.functionName ==
                        "ResetAttackPlaybackSpeed")
                {
                    unsupportedSpeedEventCount++;
                }
                else if (animationEvent.functionName.EndsWith(
                    "AnimationEvent"))
                {
                    gameplayEvents.Add(animationEvent);
                }
            }

            Assert.That(speedEventCount, Is.EqualTo(expectedSpeedEventCount));
            Assert.That(unsupportedSpeedEventCount, Is.Zero);
            Assert.That(gameplayEvents.Count, Is.EqualTo(expectedEvents.Length));
            for (int index = 0; index < expectedEvents.Length; index++)
            {
                Assert.That(
                    gameplayEvents[index].functionName,
                    Is.EqualTo(expectedEvents[index].FunctionName));
                Assert.That(
                    gameplayEvents[index].time,
                    Is.EqualTo(expectedEvents[index].Time).Within(0.00001f));
                Assert.That(
                    gameplayEvents[index].intParameter,
                    Is.EqualTo(expectedEvents[index].HitIndex));
            }
        }

        private static void AssertStateClip(
            AnimatorStateMachine stateMachine,
            string stateName,
            string clipName,
            float playbackSpeed = 1f,
            bool mirrorsClip = false)
        {
            AnimatorState foundState = null;
            ChildAnimatorState[] states = stateMachine.states;
            for (int index = 0; index < states.Length; index++)
            {
                if (states[index].state.name == stateName)
                {
                    foundState = states[index].state;
                    break;
                }
            }

            Assert.That(foundState, Is.Not.Null, stateName);
            Assert.That(foundState.motion, Is.Not.Null, stateName);
            Assert.That(foundState.motion.name, Is.EqualTo(clipName));
            Assert.That(
                foundState.speed,
                Is.EqualTo(playbackSpeed).Within(0.001f));
            Assert.That(foundState.mirror, Is.EqualTo(mirrorsClip));
        }

        private static void AssertStateRow(
            AnimatorStateMachine stateMachine,
            float rowY,
            params string[] stateNames)
        {
            float previousX = float.NegativeInfinity;
            for (int nameIndex = 0; nameIndex < stateNames.Length; nameIndex++)
            {
                bool found = false;
                ChildAnimatorState[] states = stateMachine.states;
                for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
                {
                    if (states[stateIndex].state.name != stateNames[nameIndex])
                    {
                        continue;
                    }

                    Vector3 position = states[stateIndex].position;
                    Assert.That(position.y, Is.EqualTo(rowY).Within(0.001f));
                    Assert.That(position.x, Is.GreaterThan(previousX));
                    Assert.That(
                        states[stateIndex].state.motion,
                        Is.TypeOf<AnimationClip>());
                    previousX = position.x;
                    found = true;
                    break;
                }

                Assert.That(found, Is.True, stateNames[nameIndex]);
            }
        }

        private static void AssertNoEvents(string clipPath)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null, clipPath);
            Assert.That(
                AnimationUtility.GetAnimationEvents(clip),
                Is.Empty,
                clipPath);
        }

        private static void AssertLoopTime(
            string clipPath,
            bool expectedLoopTime)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null, clipPath);
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            Assert.That(settings.loopTime, Is.EqualTo(expectedLoopTime));
        }

        private static ExpectedEvent Expected(
            float time,
            string functionName,
            int hitIndex)
        {
            return new ExpectedEvent(time, functionName, hitIndex);
        }

        private readonly struct ExpectedEvent
        {
            internal float Time { get; }
            internal string FunctionName { get; }
            internal int HitIndex { get; }

            internal ExpectedEvent(
                float time,
                string functionName,
                int hitIndex)
            {
                Time = time;
                FunctionName = functionName;
                HitIndex = hitIndex;
            }
        }
    }
}
