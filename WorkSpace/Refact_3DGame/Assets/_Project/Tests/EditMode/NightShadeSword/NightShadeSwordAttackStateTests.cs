using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
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
                Is.EqualTo(new[] { "Sound:0", "Open:0", "Close" }));
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
        public void HeavyProtection_시작은포함하고종료는포함하지않는다()
        {
            Assert.That(NightShadeSwordAttackTiming.IsHeavyProtectionTime(0.1599f), Is.False);
            Assert.That(NightShadeSwordAttackTiming.IsHeavyProtectionTime(0.16f), Is.True);
            Assert.That(NightShadeSwordAttackTiming.IsHeavyProtectionTime(0.3899f), Is.True);
            Assert.That(NightShadeSwordAttackTiming.IsHeavyProtectionTime(0.39f), Is.False);
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
