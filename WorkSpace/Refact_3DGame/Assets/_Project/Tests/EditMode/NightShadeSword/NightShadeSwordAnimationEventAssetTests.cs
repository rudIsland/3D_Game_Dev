using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAnimationEventAssetTests
    {
        private const string ClipFolder =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword";
        private const string ControllerPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Controllers/NightShadeTwoHandSwordAnimator.controller";
        private const string WalkClipPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_Walk.anim";

        [Test]
        public void AnimatorController_방향별피격상태에맞는클립이연결되어있다()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null, ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AssertStateClip(
                stateMachine,
                "Hit Front",
                "NightShadeSword_BigHit");
            AssertStateClip(
                stateMachine,
                "Hit Back",
                "NightShadeSword_BigHit");
            AssertStateClip(
                stateMachine,
                "Hit Left",
                "NightShadeSword_BigHit");
            AssertStateClip(
                stateMachine,
                "Hit Right",
                "NightShadeSword_BigHit");
            AssertStateClip(
                stateMachine,
                "Small Hit Front",
                "NightShadeSword_SmallHitFront",
                1.25f);
            AssertStateClip(
                stateMachine,
                "Small Hit Back",
                "NightShadeSword_SmallHitBack",
                1.25f);
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
                $"{ClipFolder}/NightShadeSword_BigHit.anim");
            AssertNoEvents(
                $"{ClipFolder}/NightShadeSword_SmallHitFront.anim");
            AssertNoEvents(
                $"{ClipFolder}/NightShadeSword_SmallHitBack.anim");
            AssertNoEvents(
                $"{ClipFolder}/NightShadeSword_SmallHitLeft.anim");
            AssertNoEvents(
                $"{ClipFolder}/NightShadeSword_SmallHitRight.anim");
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
            AssertNoEvents($"{ClipFolder}/NightShadeSword_Knockback.anim");
            AssertNoEvents($"{ClipFolder}/NightShadeSword_Knockdown.anim");
            AssertNoEvents($"{ClipFolder}/NightShadeSword_GetUp.anim");
            AssertNoEvents($"{ClipFolder}/NightShadeSword_Dead.anim");
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
                $"{ClipFolder}/NightShadeSword_LightAttack.anim",
                2,
                new[]
                {
                    Expected(0.36666667f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.4f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.53333336f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.7f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{ClipFolder}/NightShadeSword_ComboFirst.anim",
                2,
                new[]
                {
                    Expected(0.36666667f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.4f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.53333336f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.6666667f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{ClipFolder}/NightShadeSword_ComboSecond.anim",
                2,
                new[]
                {
                    Expected(0.23333334f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.33333334f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.36666667f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.46666667f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{ClipFolder}/NightShadeSword_HeavyAttack.anim",
                2,
                new[]
                {
                    Expected(0.6f, "OpenAttackHitAnimationEvent", 0),
                    Expected(0.6333333f, "PlayAttackSoundAnimationEvent", 0),
                    Expected(0.7f, "StopAttackTurnAnimationEvent", 0),
                    Expected(0.9666667f, "CloseAttackHitAnimationEvent", 0)
                });
            AssertClip(
                $"{ClipFolder}/NightShadeSword_WideSwing.anim",
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
            float playbackSpeed = 1f)
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
