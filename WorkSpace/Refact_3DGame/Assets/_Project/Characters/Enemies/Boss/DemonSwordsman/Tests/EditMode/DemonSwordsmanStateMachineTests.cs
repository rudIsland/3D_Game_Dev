using NUnit.Framework;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman.Tests
{
    public sealed class DemonSwordsmanStateMachineTests
    {
        private DemonSwordsmanBossSettings settings;
        private FakeTarget target;
        private FakeMovement movement;
        private FakeAnimation animation;
        private DemonSwordsmanStateMachine stateMachine;
        private UnitHealth health;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<
                DemonSwordsmanBossSettings>();
            settings.SetRuntimeDefaults();
            target = new FakeTarget
            {
                HasTarget = true,
                Position = new Vector3(0f, 0f, 2f)
            };
            movement = new FakeMovement();
            animation = new FakeAnimation();
            stateMachine = new DemonSwordsmanStateMachine(
                settings,
                target,
                movement,
                animation);
            health = new UnitHealth(settings.MaxHealth);
            stateMachine.SetHealth(health);
            stateMachine.Enable();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Update_목표가사라지면Idle로돌아간다()
        {
            stateMachine.Update(settings.NoticeTime + 0.1f);
            target.HasTarget = false;
            stateMachine.Update(0.1f);

            Assert.That(stateMachine.CurrentStateName, Is.EqualTo("Idle"));
        }

        [Test]
        public void Attack_공격후이동상태로돌아간다()
        {
            stateMachine.Update(0.1f);
            stateMachine.Update(settings.NoticeTime + 0.1f);
            stateMachine.Update(0.1f);

            Assert.That(
                stateMachine.CurrentStateName,
                Is.EqualTo(nameof(DemonSwordsmanActionState.Attack)));
            Assert.That(animation.AttackCount, Is.EqualTo(1));

            stateMachine.Update(10f);

            Assert.That(
                stateMachine.CurrentStateName,
                Is.Not.EqualTo(nameof(DemonSwordsmanActionState.Attack)));
        }

        [Test]
        public void Branch_한번결정한후속타는플레이어가움직여도바뀌지않는다()
        {
            DemonSwordsmanAttackPattern combo = null;
            for (int index = 0; index < settings.Attacks.Length; index++)
            {
                if (settings.Attacks[index].Kind ==
                    DemonSwordsmanAttackKind.SwordCombo)
                {
                    combo = settings.Attacks[index];
                    break;
                }
            }

            target.Position = new Vector3(0f, 0f, 0.5f);
            stateMachine.ChangeToFollowUp(combo);
            stateMachine.Update(combo.BranchTime + 0.01f);
            stateMachine.OpenBranchWindow();

            target.Position = new Vector3(0f, 0f, 4f);
            stateMachine.OpenBranchWindow();
            stateMachine.Update(combo.TotalTime + 0.1f);

            Assert.That(
                stateMachine.CurrentAttackName,
                Is.EqualTo("빠른 검 베기"));
        }

        private sealed class FakeTarget : IDemonSwordsmanTarget
        {
            public bool HasTarget { get; set; }
            public Vector3 Position { get; set; }
        }

        private sealed class FakeMovement : IDemonSwordsmanMovement
        {
            public Vector3 Position => Vector3.zero;
            public float MoveForward => 0f;
            public float MoveSide => 0f;
            public float MoveAmount => 0f;

            public void ResetMovement()
            {
            }

            public void MoveTo(
                Vector3 targetPosition,
                float moveSpeed,
                float turnSpeed,
                float deltaTime)
            {
            }

            public void CircleAround(
                Vector3 targetPosition,
                float moveSpeed,
                float preferredDistance,
                float sideDirection,
                float turnSpeed,
                float deltaTime)
            {
            }

            public void BackAwayFrom(
                Vector3 targetPosition,
                float moveSpeed,
                float turnSpeed,
                float deltaTime)
            {
            }

            public void TurnTo(
                Vector3 targetPosition,
                float turnSpeed,
                float deltaTime)
            {
            }

            public void StayOnGround(float deltaTime)
            {
            }

            public void Stop(float deltaTime)
            {
            }

            public void SetAttackRootMove(
                bool isEnabled,
                float moveMultiplier)
            {
            }

            public void ApplyAttackAnimationMove(Vector3 animationMove)
            {
            }

            public float GetSignedTargetAngle(Vector3 targetPosition)
            {
                return 0f;
            }
        }

        private sealed class FakeAnimation : IDemonSwordsmanAnimation
        {
            public int AttackCount { get; private set; }

            public void ResetAnimation(DemonSwordsmanStyle style)
            {
            }

            public void SetMovement(
                float moveForward,
                float moveSide,
                float moveAmount,
                float deltaTime)
            {
            }

            public void PlayLocomotion(
                DemonSwordsmanStyle style,
                float crossFadeTime)
            {
            }

            public void PlayTurn(bool turnLeft)
            {
            }

            public void PlayAttack(DemonSwordsmanAttackPattern attack)
            {
                AttackCount++;
            }

            public void PlayPhaseFear()
            {
            }

            public void PlayPhaseRage()
            {
            }

            public void PlayStyleChange(DemonSwordsmanStyle nextStyle)
            {
            }

            public void ShowStyle(DemonSwordsmanStyle style)
            {
            }

            public void SetAnimationSpeed(float speed)
            {
            }
        }
    }
}