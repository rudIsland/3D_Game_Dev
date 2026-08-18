using NUnit.Framework;
using rudIsland.RPG3D.Player.States.Attack;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class PlayerAttackWindowTests
    {
        private PlayerAttackData attackData;

        [SetUp]
        public void SetUp()
        {
            attackData =
                ScriptableObject.CreateInstance<PlayerAttackData>();
            var serializedData = new SerializedObject(attackData);
            serializedData.FindProperty("comboOpenNormalizedTime")
                .floatValue = 0.4f;
            serializedData.FindProperty("comboCloseNormalizedTime")
                .floatValue = 0.8f;
            serializedData.FindProperty("rollCancelOpenNormalizedTime")
                .floatValue = 0.6f;
            serializedData.FindProperty("turnEndNormalizedTime")
                .floatValue = 0.3f;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(attackData);
        }

        [Test]
        public void CanStartComboAt_판정종료와시작종료시간을모두확인한다()
        {
            Assert.That(
                attackData.CanStartComboAt(0.5f, false),
                Is.False);
            Assert.That(
                attackData.CanStartComboAt(0.39f, true),
                Is.False);
            Assert.That(
                attackData.CanStartComboAt(0.4f, true),
                Is.True);
            Assert.That(
                attackData.CanStartComboAt(0.8f, true),
                Is.True);
            Assert.That(
                attackData.CanStartComboAt(0.81f, true),
                Is.False);
        }

        [Test]
        public void CanCancelToRollAt_열리는시간전에는거부한다()
        {
            Assert.That(
                attackData.CanCancelToRollAt(0.59f),
                Is.False);
            Assert.That(
                attackData.CanCancelToRollAt(0.6f),
                Is.True);
        }

        [Test]
        public void CanTurnAt_검판정시작시간부터회전을막는다()
        {
            Assert.That(attackData.CanTurnAt(0.29f), Is.True);
            Assert.That(attackData.CanTurnAt(0.3f), Is.False);
        }

        [Test]
        public void CanAcceptAnimationEnd_현재공격이벤트는정규화1검사없이받는다()
        {
            Assert.That(
                PlayerAttackState.CanAcceptAnimationEnd(
                    3,
                    3,
                    true,
                    true),
                Is.True);
            Assert.That(
                PlayerAttackState.CanAcceptAnimationEnd(
                    2,
                    3,
                    true,
                    true),
                Is.False);
        }
    }
}
