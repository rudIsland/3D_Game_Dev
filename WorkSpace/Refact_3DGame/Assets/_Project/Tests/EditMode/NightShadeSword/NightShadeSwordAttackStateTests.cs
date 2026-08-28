using NUnit.Framework;
using Characters;
using Characters.Combat;
using Characters.Enemies.NightShade;
using UnityEngine;

namespace Tests.NightShade
{
    public sealed class NightShadeSwordAttackStateTests
    {
        [Test]
        public void AnimationEvent_현재공격Action만순서대로한번처리한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = EnterAttack(scope);

            machine.PlayAttackSoundAnimationEvent(0);
            machine.PlayAttackSoundAnimationEvent(0);
            machine.OpenAttackHitAnimationEvent(0);
            machine.CloseAttackHitAnimationEvent();
            machine.OpenAttackHitAnimationEvent(0);
            machine.Update(0.1f);

            Assert.That(
                scope.Actions.Calls,
                Is.EqualTo(new[] { "Sound:0", "Open", "Close" }));
            Assert.That(scope.Actions.OpenedDamages.Count, Is.EqualTo(1));
            Assert.That(
                scope.Actions.OpenedDamages[0].HealthDamage,
                Is.EqualTo(10f));
        }

        [Test]
        public void 피격중단_열린판정대기이벤트와공격속도를정리한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = EnterAttack(scope);
            machine.OpenAttackHitAnimationEvent(0);
            machine.Update(0.1f);
            int resetCount = scope.Animation.ResetSpeedCount;
            var hitRequest = new EnemyHitRequest(
                1f,
                1f,
                Vector3.zero,
                Vector3.back,
                0f,
                0f);

            machine.ChangeToHitState(HitReaction.SmallHit, in hitRequest);
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Combat));
            machine.Update(0f);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Hit));
            Assert.That(scope.Actions.CloseCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(scope.Animation.ResetSpeedCount, Is.GreaterThan(resetCount));
            Assert.That(
                machine.Debug.PreviousActionStopReason,
                Is.EqualTo(NightShadeSwordActionStopReason.Interrupted));
        }

        [Test]
        public void Light공격_계획한전진거리와이벤트기준회전종료를사용한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = EnterAttack(scope);
            Assert.That(
                machine.CurrentActionId,
                Is.EqualTo(NightShadeSwordActionId.Light));

            scope.Animation.NormalizedTime = 0.18f;
            machine.Update(0.1f);

            Assert.That(scope.Movement.AttackMoveCount, Is.EqualTo(1));
            Assert.That(
                scope.Movement.TotalAttackMoveDistance,
                Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(scope.Movement.LastAttackCanTurn, Is.True);

            machine.StopAttackTurnAnimationEvent();
            scope.Animation.NormalizedTime = 0.2f;
            machine.Update(0.1f);

            Assert.That(scope.Movement.LastAttackCanTurn, Is.False);
            Assert.That(
                scope.Movement.TotalAttackMoveDistance,
                Is.EqualTo(0.35f).Within(0.0001f));
        }

        [TestCase(1.4f, 5, 0.1f)]
        [TestCase(2.2f, 4, 0.35f)]
        [TestCase(2.6f, 7, 0.35f)]
        [TestCase(3.6f, 6, 0.35f)]
        public void 모든공격_대상거리로계획한전진을사용한다(
            float targetDistance,
            int expectedActionValue,
            float expectedMoveDistance)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, targetDistance));
            NightShadeSwordStateMachine machine = EnterAttack(scope);

            Assert.That(
                machine.CurrentActionId,
                Is.EqualTo((NightShadeSwordActionId)expectedActionValue));

            scope.Animation.NormalizedTime = 0.18f;
            machine.Update(0.1f);

            Assert.That(scope.Movement.AttackMoveCount, Is.EqualTo(1));
            Assert.That(
                scope.Movement.TotalAttackMoveDistance,
                Is.EqualTo(expectedMoveDistance).Within(0.0001f));
        }

        [Test]
        public void HeavyProtection_시작은포함하고종료는포함하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3.6f));
            NightShadeSwordStateMachine machine = EnterAttack(scope);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Heavy));

            scope.Animation.NormalizedTime = 0.1599f;
            Assert.That(machine.ProtectsSmallHit, Is.False);
            scope.Animation.NormalizedTime = 0.16f;
            Assert.That(machine.ProtectsSmallHit, Is.True);
            scope.Animation.NormalizedTime = 0.3899f;
            Assert.That(machine.ProtectsSmallHit, Is.True);
            scope.Animation.NormalizedTime = 0.39f;
            Assert.That(machine.ProtectsSmallHit, Is.False);
        }

        private static NightShadeSwordStateMachine EnterAttack(
            NightShadeSwordTestScope scope)
        {
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            Assert.That(machine.IsAttackStateActive, Is.True);
            return machine;
        }
    }
}
