using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordHitAndDeadStateTests
    {
        [Test]
        public void ChangeToHitState_0점18초안의SmallHit은애니메이션을다시시작하지않는다()
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

            machine.ChangeToHitState(
                HitReaction.SmallHit,
                in hitRequest);
            machine.Update(0.1f);

            machine.ChangeToHitState(
                HitReaction.SmallHit,
                in hitRequest);
            machine.Update(0.05f);

            Assert.That(scope.Animation.SmallHitCount, Is.EqualTo(1));
            Assert.That(scope.Animation.LastHitDirection, Is.EqualTo(Vector3.right));
            Assert.That(
                scope.Movement.TotalHitMovement.x,
                Is.EqualTo(0.06f).Within(0.001f));

            machine.Update(0.03f);
            machine.ChangeToHitState(
                HitReaction.SmallHit,
                in hitRequest);

            Assert.That(scope.Animation.SmallHitCount, Is.EqualTo(2));
        }

        [Test]
        public void ChangeToHitState_BigHit은SmallHit을즉시덮어쓴다()
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
            machine.ChangeToHitState(
                HitReaction.SmallHit,
                in hitRequest);
            machine.Update(0.01f);

            machine.ChangeToHitState(
                HitReaction.BigHit,
                in hitRequest);

            Assert.That(scope.Animation.SmallHitCount, Is.EqualTo(1));
            Assert.That(scope.Animation.BigHitCount, Is.EqualTo(1));
        }

        [Test]
        public void HitState_넉백은BigHit보다더멀고긴시간동안밀린다()
        {
            using var staggerScope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            using var knockbackScope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine staggerMachine =
                staggerScope.CreateStateMachine(staggerScope.CreateSettings());
            NightShadeSwordStateMachine knockbackMachine =
                knockbackScope.CreateStateMachine(knockbackScope.CreateSettings());
            var hitRequest = new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.right,
                1f,
                0f);
            staggerMachine.Enable();
            knockbackMachine.Enable();

            staggerMachine.ChangeToHitState(
                HitReaction.BigHit,
                in hitRequest);
            knockbackMachine.ChangeToHitState(
                HitReaction.Knockback,
                in hitRequest);
            Assert.That(knockbackScope.Animation.KnockbackCount, Is.EqualTo(1));
            Assert.That(knockbackScope.Animation.HitCount, Is.Zero);
            staggerMachine.Update(0.2f);
            knockbackMachine.Update(0.2f);

            Assert.That(
                staggerScope.Movement.TotalHitMovement.x,
                Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(
                knockbackScope.Movement.TotalHitMovement.x,
                Is.EqualTo(0.5f).Within(0.001f));

            knockbackMachine.Update(0.2f);

            Assert.That(
                knockbackScope.Movement.TotalHitMovement.x,
                Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void HitState_Knockdown은넘어진후기다리고일어난다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(
                    scope.CreateSettings(knockdownStayDuration: 0.5f));
            var hitRequest = new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.right,
                1f,
                0f);
            machine.Enable();

            machine.ChangeToHitState(
                HitReaction.Knockdown,
                in hitRequest);

            Assert.That(scope.Animation.KnockdownCount, Is.EqualTo(1));
            Assert.That(scope.Animation.GetUpCount, Is.Zero);
            machine.Update(0.3f);
            Assert.That(
                scope.Movement.TotalHitMovement.x,
                Is.EqualTo(0.5f).Within(0.001f));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.3f);
            Assert.That(
                scope.Movement.TotalHitMovement.x,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(scope.Animation.GetUpCount, Is.Zero);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Hit));

            machine.Update(0.25f);
            Assert.That(scope.Animation.GetUpCount, Is.Zero);
            machine.Update(0.25f);
            Assert.That(scope.Animation.GetUpCount, Is.EqualTo(1));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));
        }

        [Test]
        public void GetHitSide_공격자가있는방향을고른다()
        {
            Assert.That(
                NightShadeHitDirection.GetSide(
                    Vector3.forward,
                    Vector3.right,
                    Vector3.back),
                Is.EqualTo(NightShadeHitSide.Front));
            Assert.That(
                NightShadeHitDirection.GetSide(
                    Vector3.forward,
                    Vector3.right,
                    Vector3.forward),
                Is.EqualTo(NightShadeHitSide.Back));
            Assert.That(
                NightShadeHitDirection.GetSide(
                    Vector3.forward,
                    Vector3.right,
                    Vector3.left),
                Is.EqualTo(NightShadeHitSide.Right));
            Assert.That(
                NightShadeHitDirection.GetSide(
                    Vector3.forward,
                    Vector3.right,
                    Vector3.right),
                Is.EqualTo(NightShadeHitSide.Left));
        }

        [Test]
        public void HitState_밀림최종거리는프레임간격과상관없이같다()
        {
            using var oneFrameScope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            using var fourFrameScope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine oneFrameMachine =
                oneFrameScope.CreateStateMachine(oneFrameScope.CreateSettings());
            NightShadeSwordStateMachine fourFrameMachine =
                fourFrameScope.CreateStateMachine(fourFrameScope.CreateSettings());
            var hitRequest = new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.right,
                1f,
                0f);
            oneFrameMachine.Enable();
            fourFrameMachine.Enable();
            oneFrameMachine.ChangeToHitState(
                HitReaction.BigHit,
                in hitRequest);
            fourFrameMachine.ChangeToHitState(
                HitReaction.BigHit,
                in hitRequest);

            oneFrameMachine.Update(0.2f);
            for (int frame = 0; frame < 4; frame++)
            {
                fourFrameMachine.Update(0.05f);
            }

            Assert.That(
                oneFrameScope.Movement.TotalHitMovement.x,
                Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(
                fourFrameScope.Movement.TotalHitMovement.x,
                Is.EqualTo(
                    oneFrameScope.Movement.TotalHitMovement.x).Within(0.001f));
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

            machine.ChangeToHitState(
                HitReaction.SmallHit,
                in hitRequest);

            Assert.That(scope.Actions.CloseCount, Is.EqualTo(1));
            Assert.That(
                scope.Animation.ResetSpeedCount,
                Is.GreaterThan(resetCountBeforeHit));
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Hit));
        }

        [Test]
        public void HeavyProtection_시작은포함하고종료는포함하지않는다()
        {
            Assert.That(
                NightShadeSwordAttackState.IsHeavyProtectionTime(0.1599f),
                Is.False);
            Assert.That(
                NightShadeSwordAttackState.IsHeavyProtectionTime(0.16f),
                Is.True);
            Assert.That(
                NightShadeSwordAttackState.IsHeavyProtectionTime(0.3899f),
                Is.True);
            Assert.That(
                NightShadeSwordAttackState.IsHeavyProtectionTime(0.39f),
                Is.False);
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
