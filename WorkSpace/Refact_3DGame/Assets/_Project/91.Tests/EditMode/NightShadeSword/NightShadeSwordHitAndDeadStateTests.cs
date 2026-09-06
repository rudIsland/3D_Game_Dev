using NUnit.Framework;
using Characters;
using Characters.Combat;
using Characters.Enemies.NightShade;
using UnityEngine;

namespace Tests.NightShade
{
    public sealed class NightShadeSwordHitAndDeadStateTests
    {
        [Test]
        public void 강제반응_같은Tick요청은StaggerBreak을가장먼저고른다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            EnemyHitRequest hitRequest = CreateHitRequest();
            machine.Enable();
            machine.ChangeToHitState(HitReaction.SmallHit, in hitRequest);
            machine.ChangeToHitState(HitReaction.BigHit, in hitRequest);
            machine.ChangeToHitState(HitReaction.Knockback, in hitRequest);
            machine.ChangeToHitState(HitReaction.Knockdown, in hitRequest);
            machine.ChangeToHitState(HitReaction.StaggerBreak, in hitRequest);

            machine.Update(0f, true);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Hit));
            Assert.That(scope.Animation.StaggerEnterCount, Is.EqualTo(1));
            Assert.That(scope.Animation.StaggerStartCount, Is.Zero);
            Assert.That(scope.Animation.KnockdownCount, Is.Zero);
            Assert.That(scope.Animation.KnockbackCount, Is.Zero);
            Assert.That(scope.Movement.TotalHitMovement, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void StaggerBreak_앉기_대기_회복뒤Combat으로돌아간다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(staggerBreakStayDuration: 0.5f));
            EnemyHitRequest hitRequest = CreateHitRequest();
            machine.Enable();
            machine.ChangeToHitState(HitReaction.StaggerBreak, in hitRequest);
            machine.Update(0f);

            Assert.That(scope.Animation.StaggerEnterCount, Is.EqualTo(1));
            Assert.That(scope.Animation.StaggerStartCount, Is.Zero);

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            Assert.That(scope.Animation.StaggerStartCount, Is.EqualTo(1));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            Assert.That(scope.Animation.StaggerIdleCount, Is.EqualTo(1));

            machine.Update(0.49f);
            Assert.That(scope.Animation.StaggerEndCount, Is.Zero);
            machine.Update(0.01f);
            Assert.That(scope.Animation.StaggerEndCount, Is.EqualTo(1));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Combat));
        }

        [Test]
        public void StaggerBreak중추가피격은연출을재시작하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            EnemyHitRequest hitRequest = CreateHitRequest();
            machine.Enable();
            machine.ChangeToHitState(HitReaction.StaggerBreak, in hitRequest);
            machine.Update(0f);

            machine.ChangeToHitState(HitReaction.StaggerBreak, in hitRequest);
            machine.ChangeToHitState(HitReaction.BigHit, in hitRequest);
            machine.Update(0f);

            Assert.That(scope.Animation.StaggerEnterCount, Is.EqualTo(1));
            Assert.That(scope.Animation.StaggerStartCount, Is.Zero);
            Assert.That(scope.Animation.BigHitCount, Is.Zero);
        }

        [Test]
        public void HitStop중에도강제반응에는진입하지만Action시간은진행하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            int attackCount = scope.Animation.AttackCount;
            EnemyHitRequest hitRequest = CreateHitRequest();
            machine.ChangeToHitState(HitReaction.BigHit, in hitRequest);

            machine.Update(1f, true);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Hit));
            Assert.That(scope.Animation.BigHitCount, Is.EqualTo(1));
            Assert.That(scope.Movement.TotalHitMovement, Is.EqualTo(Vector3.zero));
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(attackCount));
        }

        [Test]
        public void SmallHit_0점18초안에는재시작하지않고이후에는재시작한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            EnemyHitRequest hitRequest = CreateHitRequest();
            machine.Enable();
            machine.ChangeToHitState(HitReaction.SmallHit, in hitRequest);
            machine.Update(0f);
            machine.Update(0.1f);

            machine.ChangeToHitState(HitReaction.SmallHit, in hitRequest);
            machine.Update(0.05f);
            Assert.That(scope.Animation.SmallHitCount, Is.EqualTo(1));

            machine.Update(0.03f);
            machine.ChangeToHitState(HitReaction.SmallHit, in hitRequest);
            machine.Update(0f);
            Assert.That(scope.Animation.SmallHitCount, Is.EqualTo(2));
        }

        [Test]
        public void Dead_피격보다우선하고유지시간뒤한번만풀반환한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(deadBodyKeepTime: 0.5f));
            EnemyHitRequest hitRequest = CreateHitRequest();
            machine.Enable();
            machine.ChangeToHitState(HitReaction.Knockdown, in hitRequest);
            machine.ChangeToDeadState();

            machine.Update(0f, true);
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Dead));
            Assert.That(scope.Animation.DeadCount, Is.EqualTo(1));
            Assert.That(scope.Animation.KnockdownCount, Is.Zero);

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.25f);
            machine.Update(0.25f);
            machine.Update(1f);
            Assert.That(scope.Actions.ReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void HitState_넉백밀림최종거리는프레임간격과상관없이같다()
        {
            using var oneFrameScope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            using var fourFrameScope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine oneFrame = oneFrameScope.CreateStateMachine(
                oneFrameScope.CreateSettings());
            NightShadeSwordStateMachine fourFrames = fourFrameScope.CreateStateMachine(
                fourFrameScope.CreateSettings());
            EnemyHitRequest hitRequest = CreateHitRequest();
            oneFrame.Enable();
            fourFrames.Enable();
            oneFrame.ChangeToHitState(HitReaction.Knockback, in hitRequest);
            fourFrames.ChangeToHitState(HitReaction.Knockback, in hitRequest);
            oneFrame.Update(0f);
            fourFrames.Update(0f);

            oneFrame.Update(0.4f);
            for (int index = 0; index < 4; index++)
            {
                fourFrames.Update(0.1f);
            }

            Assert.That(
                fourFrameScope.Movement.TotalHitMovement.x,
                Is.EqualTo(oneFrameScope.Movement.TotalHitMovement.x).Within(0.001f));
        }

        private static EnemyHitRequest CreateHitRequest()
        {
            return new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.right,
                1f,
                0f);
        }
    }
}
