using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordWalkAssetTests
    {
        private const string ControllerPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Controllers/NightShadeTwoHandSwordAnimator.controller";
        private const string WalkClipPath =
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_Walk.anim";
        private static readonly string[] ProtectedClipPaths =
        {
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_CombatBack.anim",
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_LightAttack.anim",
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_ComboAttack.anim",
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_HeavyAttack.anim",
            "Assets/_Project/Characters/Enemies/NightShade/Models/Animations/Clips/TwoHandSword/NightShadeSword_WideSwing.anim"
        };

        [Test]
        public void ApplyWalkAnimation_두번실행해도Walk만하나이고기존클립은바뀌지않는다()
        {
            Hash128[] hashesBefore = GetDependencyHashes(ProtectedClipPaths);
            MethodInfo applyWalk = GetApplyWalkMethod();

            applyWalk.Invoke(null, null);
            AssertWalkState();
            applyWalk.Invoke(null, null);
            AssertWalkState();

            Hash128[] hashesAfter = GetDependencyHashes(ProtectedClipPaths);
            AssertHashesEqual(hashesBefore, hashesAfter);
        }

        private static MethodInfo GetApplyWalkMethod()
        {
            Type builderType = Type.GetType(
                "rudIsland.RPG3D.EditorTools.NightShadeSwordEliteAssetBuilder, Assembly-CSharp-Editor");
            Assert.That(builderType, Is.Not.Null);
            MethodInfo method = builderType.GetMethod(
                "ApplyWalkAnimation",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return method;
        }

        private static void AssertWalkState()
        {
            AnimationClip walkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
            Assert.That(walkClip, Is.Not.Null);
            AnimationClipSettings clipSettings =
                AnimationUtility.GetAnimationClipSettings(walkClip);
            Assert.That(clipSettings.loopTime, Is.True);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null);
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

        private static Hash128[] GetDependencyHashes(string[] assetPaths)
        {
            var hashes = new Hash128[assetPaths.Length];
            for (int index = 0; index < assetPaths.Length; index++)
            {
                hashes[index] =
                    AssetDatabase.GetAssetDependencyHash(assetPaths[index]);
            }

            return hashes;
        }

        private static void AssertHashesEqual(Hash128[] expected, Hash128[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(
                    actual[index],
                    Is.EqualTo(expected[index]),
                    ProtectedClipPaths[index]);
            }
        }
    }
}
