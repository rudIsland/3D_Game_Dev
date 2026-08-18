using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordHitAndDeadStateTests
    {
        [Test]
        public void ChangeToHitState_연속경직이면애니메이션과밀림진행도를다시시작한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            var hitRequest = new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.right,
                1f,
                0f);
            machine.Enable();

            machine.ChangeToHitState(in hitRequest);
            machine.Update(0.2f);
            Assert.That(scope.Movement.LastHitMovement.x, Is.EqualTo(1f).Within(0.001f));

            machine.ChangeToHitState(in hitRequest);
            machine.Update(0.1f);

            Assert.That(scope.Animation.HitCount, Is.EqualTo(2));
            Assert.That(scope.Movement.LastHitMovement.x, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void ChangeToHitState_공격중열린판정과공격속도를정리한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            machine.OpenAttackHitAnimationEvent(0);
            machine.Update(0.1f);
            int resetCountBeforeHit = scope.Animation.ResetSpeedCount;
            var hitRequest = new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.back,
                1f,
                0f);

            machine.ChangeToHitState(in hitRequest);

            Assert.That(scope.Actions.CloseCount, Is.EqualTo(1));
            Assert.That(
                scope.Animation.ResetSpeedCount,
                Is.GreaterThan(resetCountBeforeHit));
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Hit));
        }

        [Test]
        public void Dead_애니메이션과유지시간이끝난후한번만풀반환을요청한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(
                    scope.CreateSettings(deadBodyKeepTime: 0.5f));
            machine.Enable();
            machine.ChangeToDeadState();
            machine.ChangeToDeadState();
            scope.Animation.NormalizedTime = 1f;

            machine.Update(0.25f);
            Assert.That(scope.Actions.ReleaseCount, Is.Zero);
            machine.Update(0.25f);
            machine.Update(1f);

            Assert.That(scope.Animation.DeadCount, Is.EqualTo(1));
            Assert.That(scope.Actions.ReleaseCount, Is.EqualTo(1));
        }
    }
}
