using NUnit.Framework;
using rudIsland.RPG3D.Player.States.Attack;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerAnimationEventAssetTests
    {
        private const string AttackFolder =
            "Assets/_Project/Characters/Player/Models/Sources/30_Attack";
        private const string RollFolder =
            "Assets/_Project/Characters/Player/Models/Sources/10_Roll";
        private const string AttackDataFolder =
            "Assets/_Project/Characters/Player/Code/StateMachine/Attack/AttackData";

        [TestCase("1Hand_Base_Attack_A_1_InPlace.anim", 1, 1.3f, 0.28333333f)]
        [TestCase("1Hand_Base_Attack_A_2_InPlace.anim", 2, 1.4666667f, 0.48333332f)]
        [TestCase("1Hand_Base_Attack_A_3_InPlace.anim", 3, 1.4666667f, 0.4f)]
        [TestCase("1Hand_Base_Attack_A_4_InPlace.anim", 4, 1.4333333f, 0.4f)]
        [TestCase("1Hand_Base_Attack_A_5_InPlace.anim", 5, 1.9333333f, 0.36f)]
        [TestCase("1Hand_Base_Attack_Run_InPlace.anim", 6, 1.5f, 0.43076923f)]
        public void AttackClip_판정종료뒤현재공격종료이벤트가온다(
            string fileName,
            int attackNumber,
            float expectedEndTime,
            float movementEndNormalizedTime)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{AttackFolder}/{fileName}");
            Assert.That(clip, Is.Not.Null, fileName);
            AnimationEvent[] events =
                AnimationUtility.GetAnimationEvents(clip);

            int hitStartIndex = FindEventIndex(
                events,
                "StartAttackHitAnimationEvent");
            int hitEndIndex = FindEventIndex(
                events,
                "EndAttackHitAnimationEvent");
            int attackEndIndex = FindEventIndex(
                events,
                "EndAttackAnimationEvent");

            Assert.That(hitStartIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(hitEndIndex, Is.GreaterThan(hitStartIndex));
            Assert.That(attackEndIndex, Is.GreaterThan(hitEndIndex));
            Assert.That(
                events[attackEndIndex].time,
                Is.EqualTo(expectedEndTime).Within(0.00001f));
            Assert.That(
                events[attackEndIndex].intParameter,
                Is.EqualTo(attackNumber));

            PlayerAttackData attackData =
                AssetDatabase.LoadAssetAtPath<PlayerAttackData>(
                    $"{AttackDataFolder}/PlayerAttack0{attackNumber}Data.asset");
            Assert.That(attackData, Is.Not.Null);
            Assert.That(
                attackData.MovementCurve.Evaluate(
                    movementEndNormalizedTime),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                attackData.MovementCurve.Evaluate(1f),
                Is.EqualTo(1f).Within(0.0001f));
        }

        [TestCase("1Hand_Base_Roll_B_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_B_L45_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_B_R45_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_F_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_F_L45_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_F_L90_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_F_R45_InPlace.fbx")]
        [TestCase("1Hand_Base_Roll_F_R90_InPlace.fbx")]
        public void RollClip_초반무적이벤트순서가같다(string fileName)
        {
            AnimationClip clip = LoadFbxAnimationClip(
                $"{RollFolder}/{fileName}");
            Assert.That(clip, Is.Not.Null, fileName);
            AnimationEvent[] events =
                AnimationUtility.GetAnimationEvents(clip);
            int beginIndex = FindEventIndex(
                events,
                "BeginRollInvulnerabilityAnimationEvent");
            int endIndex = FindEventIndex(
                events,
                "EndRollInvulnerabilityAnimationEvent");

            Assert.That(beginIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(endIndex, Is.GreaterThan(beginIndex));
            Assert.That(
                events[beginIndex].time / clip.length,
                Is.EqualTo(0.05f).Within(0.001f));
            Assert.That(
                events[endIndex].time / clip.length,
                Is.EqualTo(0.42f).Within(0.001f));
        }

        private static int FindEventIndex(
            AnimationEvent[] events,
            string functionName)
        {
            for (int index = 0; index < events.Length; index++)
            {
                if (events[index].functionName == functionName)
                {
                    return index;
                }
            }

            return -1;
        }

        private static AnimationClip LoadFbxAnimationClip(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            return null;
        }
    }
}
