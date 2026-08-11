using NUnit.Framework;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade.Tests
{
    public sealed class NightshadeSpearAttackSequenceTests
    {
        private GameObject nightshadeObject;
        private GameObject targetObject;
        private NightshadeSpearStateMachine stateMachine;
        private Random.State randomState;

        [SetUp]
        public void SetUp()
        {
            randomState = Random.state;
            Random.InitState(20260808);

            nightshadeObject = new GameObject("Nightshade Test");
            CharacterController characterController =
                nightshadeObject.AddComponent<CharacterController>();
            NightshadeSpearAnimationController animationController =
                nightshadeObject.AddComponent<
                    NightshadeSpearAnimationController>();

            targetObject = new GameObject("Player Target");
            targetObject.transform.position = new Vector3(0f, 0f, 1.5f);

            var movement = new NightshadeSpearMovement(
                nightshadeObject.transform,
                characterController,
                -22f,
                -2f);
            stateMachine = new NightshadeSpearStateMachine(
                targetObject.transform,
                movement,
                animationController,
                25f,
                6f,
                1.6f,
                3.8f,
                300f,
                2f,
                null,
                null,
                null,
                null,
                null,
                null,
                true,
                0.6f);
        }

        [TearDown]
        public void TearDown()
        {
            Random.state = randomState;
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(nightshadeObject);
        }

        [TestCase(1, 1, 0f, true)]
        [TestCase(3, 1, 2.3f, true)]
        [TestCase(5, 1, 0.99f, false)]
        [TestCase(5, 1, 1f, true)]
        [TestCase(4, 1, 1.99f, false)]
        [TestCase(4, 1, 3f, true)]
        [TestCase(7, 1, 4f, false)]
        [TestCase(7, 2, 3f, true)]
        [TestCase(10, 2, 1.19f, false)]
        [TestCase(11, 2, 1.8f, true)]
        public void ContextAttack_페이즈와거리조건을지킨다(
            int attackNumber,
            int phase,
            float distance,
            bool expected)
        {
            bool result =
                NightshadeSpearStateMachine.IsContextAttackDistanceAllowed(
                    (NightshadeSpearAttackId)attackNumber,
                    phase,
                    distance * distance);

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(6)]
        [TestCase(8)]
        [TestCase(12)]
        [TestCase(13)]
        public void ContextAttack_후속전용공격을시작공격에서제외한다(
            int attackNumber)
        {
            Assert.That(
                NightshadeSpearStateMachine.IsContextAttackDistanceAllowed(
                    (NightshadeSpearAttackId)attackNumber,
                    2,
                    1f),
                Is.False);
        }

        [TestCase(1, 1, 1.5f)]
        [TestCase(2, 1, 1.5f)]
        [TestCase(3, 1, 1.5f)]
        [TestCase(4, 1, 2.5f)]
        [TestCase(5, 1, 1.5f)]
        [TestCase(1, 2, 1.5f)]
        [TestCase(7, 2, 4f)]
        [TestCase(9, 2, 1f)]
        [TestCase(10, 2, 2f)]
        [TestCase(11, 2, 1f)]
        public void ContextAttack_페이즈별시작공격목록을허용한다(
            int attackNumber,
            int phase,
            float distance)
        {
            Assert.That(
                NightshadeSpearStateMachine.IsContextAttackDistanceAllowed(
                    (NightshadeSpearAttackId)attackNumber,
                    phase,
                    distance * distance),
                Is.True);
        }

        [Test]
        public void ContextAttack_다른후보가있으면직전시작공격을반복하지않는다()
        {
            Assert.That(stateMachine.TryChangeToContextAttackState(), Is.True);
            string firstAttackName = stateMachine.CurrentAttackName;

            stateMachine.ChangeToChaseState();
            Assert.That(stateMachine.TryChangeToContextAttackState(), Is.True);

            Assert.That(stateMachine.CurrentAttackName, Is.Not.EqualTo(
                firstAttackName));
        }

        [TestCase(1, 6)]
        [TestCase(2, 3)]
        [TestCase(4, 5)]
        [TestCase(7, 8)]
        [TestCase(9, 10)]
        [TestCase(10, 13)]
        [TestCase(11, 12)]
        public void FollowUp_정해진공격으로만연결한다(
            int attackNumber,
            int expectedFollowUpNumber)
        {
            bool hasFollowUp =
                NightshadeSpearStateMachine.TryGetFollowUpAttackId(
                    (NightshadeSpearAttackId)attackNumber,
                    out NightshadeSpearAttackId followUpId);

            Assert.That(hasFollowUp, Is.True);
            Assert.That((int)followUpId, Is.EqualTo(expectedFollowUpNumber));
        }

        [Test]
        public void FollowUp_페이즈별최대연계수를제한한다()
        {
            Assert.That(
                NightshadeSpearStateMachine.GetMaximumSequenceCount(1),
                Is.EqualTo(2));
            Assert.That(
                NightshadeSpearStateMachine.GetMaximumSequenceCount(2),
                Is.EqualTo(3));
        }

        [Test]
        public void FollowUp_1페이즈에서는피해가적용된경우만연결한다()
        {
            Assert.That(
                NightshadeSpearStateMachine.CanContinueAttackSequence(
                    1,
                    1,
                    false,
                    NightshadeSpearAttackId.Attack01),
                Is.False);
            Assert.That(
                NightshadeSpearStateMachine.CanContinueAttackSequence(
                    1,
                    1,
                    true,
                    NightshadeSpearAttackId.Attack01),
                Is.True);
        }

        [Test]
        public void FollowUp_2페이즈에서는회피되어도최대연계까지연결한다()
        {
            Assert.That(
                NightshadeSpearStateMachine.CanContinueAttackSequence(
                    2,
                    1,
                    false,
                    NightshadeSpearAttackId.Attack09),
                Is.True);
            Assert.That(
                NightshadeSpearStateMachine.CanContinueAttackSequence(
                    2,
                    2,
                    false,
                    NightshadeSpearAttackId.Attack10),
                Is.True);
            Assert.That(
                NightshadeSpearStateMachine.CanContinueAttackSequence(
                    2,
                    3,
                    false,
                    NightshadeSpearAttackId.Attack10),
                Is.False);
        }
    }
}
