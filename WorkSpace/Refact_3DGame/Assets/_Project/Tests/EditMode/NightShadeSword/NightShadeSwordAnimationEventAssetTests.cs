using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAnimationEventAssetTests
    {
        private const string ClipFolder =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword";

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
