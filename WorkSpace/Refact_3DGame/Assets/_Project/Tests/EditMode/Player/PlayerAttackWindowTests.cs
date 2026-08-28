using NUnit.Framework;
using Characters.Player.StateMachine.States.Attack;
using Characters.Player.Config;
using UnityEditor;
using UnityEngine;

namespace Tests.Player
{
    public sealed class PlayerAttackWindowTests
    {
        private const string AttackDataFolder =
            "Assets/_Project/Characters/Player/Code/StateMachine/States/Attack/AttackData";

        private PlayerAttackData attackData;

        [SetUp]
        public void SetUp()
        {
            attackData =
                ScriptableObject.CreateInstance<PlayerAttackData>();
            var serializedData = new SerializedObject(attackData);
            serializedData.FindProperty("comboOpenNormalizedTime")
                .floatValue = 0.4f;
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
                attackData.CanStartComboAt(0.5f, false, 0.8f),
                Is.False);
            Assert.That(
                attackData.CanStartComboAt(0.39f, true, 0.8f),
                Is.False);
            Assert.That(
                attackData.CanStartComboAt(0.4f, true, 0.8f),
                Is.True);
            Assert.That(
                attackData.CanStartComboAt(0.8f, true, 0.8f),
                Is.True);
            Assert.That(
                attackData.CanStartComboAt(0.81f, true, 0.8f),
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

        [Test]
        public void HeavyProtection_시작은포함하고종료는포함하지않는다()
        {
            Assert.That(
                PlayerAttackState.IsHeavyProtectionTime(0.1999f),
                Is.False);
            Assert.That(
                PlayerAttackState.IsHeavyProtectionTime(0.20f),
                Is.True);
            Assert.That(
                PlayerAttackState.IsHeavyProtectionTime(0.4199f),
                Is.True);
            Assert.That(
                PlayerAttackState.IsHeavyProtectionTime(0.42f),
                Is.False);
            Assert.That(
                PlayerAttackState.IsHeavyProtectionActive(
                    4,
                    true,
                    0.3f),
                Is.False);
            Assert.That(
                PlayerAttackState.IsHeavyProtectionActive(
                    5,
                    true,
                    0.3f),
                Is.True);
        }

        [TestCase(1, 0.5f, 0.25f)]
        [TestCase(2, 0.6f, 0.4f)]
        [TestCase(3, 0.55f, 0.2923077f)]
        [TestCase(4, 0.5f, 0.31666666f)]
        [TestCase(5, 0.4f, 0.29333332f)]
        [TestCase(6, 0.6f, 0.4f)]
        public void AttackAsset_공격별이동회전값을보관한다(
            int attackNumber,
            float expectedMoveDistance,
            float expectedTurnEndTime)
        {
            string assetPath =
                $"{AttackDataFolder}/PlayerAttack{attackNumber:00}Data.asset";
            PlayerAttackData attack =
                AssetDatabase.LoadAssetAtPath<PlayerAttackData>(
                    assetPath);

            Assert.That(attack, Is.Not.Null, assetPath);
            Assert.That(attack.MoveDistance, Is.EqualTo(expectedMoveDistance));
            Assert.That(
                attack.TurnEndNormalizedTime,
                Is.EqualTo(expectedTurnEndTime).Within(0.000001f));
        }

        [Test]
        public void CharacterConfig_공격공통보정값을한곳에보관한다()
        {
            PlayerCharacterConfig config =
                AssetDatabase.LoadAssetAtPath<PlayerCharacterConfig>(
                    "Assets/_Project/Characters/Player/Configs/PlayerCharacterConfig.asset");

            Assert.That(config, Is.Not.Null);
            PlayerAttackRuntimeConfig attacks =
                config.CreateRuntimeConfig().Attacks;
            Assert.That(attacks.TargetStopDistance, Is.EqualTo(0.85f));
            Assert.That(attacks.MaximumAddedMoveDistance, Is.EqualTo(0.25f));
            Assert.That(attacks.MaximumTurnAngle, Is.EqualTo(30f));
            Assert.That(attacks.ComboCloseNormalizedTime, Is.EqualTo(0.9f));
        }
    }
}
