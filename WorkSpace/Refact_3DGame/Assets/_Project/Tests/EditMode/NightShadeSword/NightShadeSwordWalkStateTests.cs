using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordWalkStateTests
    {
        [TestCase(4.5f, true)]
        [TestCase(5.5f, false)]
        public void Idle_대상거리별로Walk또는Chase를선택한다(float distance, bool expectsWalk)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();

            machine.Update(0.1f);

            NightShadeSwordStateId expectedState = expectsWalk
                ? NightShadeSwordStateId.Walk
                : NightShadeSwordStateId.Chase;
            Assert.That(machine.CurrentStateId, Is.EqualTo(expectedState));
        }

        [Test]
        public void Chase_5미터이내에서Walk로전환한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 5.5f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 5f);
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));
        }

        [Test]
        public void Walk와Chase_5미터와6미터사이에서는이전상태를유지한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 4.5f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 5.5f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 6f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Chase));

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 5.5f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Chase));

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 5f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));
        }

        [Test]
        public void Walk_공격거리밖에서는걷기속도로이동한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 4.5f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);

            machine.Update(0.1f);

            Assert.That(scope.Animation.WalkCount, Is.EqualTo(1));
            Assert.That(scope.Movement.MoveToCount, Is.EqualTo(1));
            Assert.That(scope.Movement.LastMoveSpeed, Is.EqualTo(1.8f));
        }

        [Test]
        public void Walk_공격거리안에서회복중이면Idle로회전만한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            int idleCountBeforeWalk = scope.Animation.IdleCount;
            machine.FightMemory.CompleteAttack(1f);

            machine.Update(0.1f);
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));
            Assert.That(
                scope.Animation.IdleCount,
                Is.EqualTo(idleCountBeforeWalk + 1));
            Assert.That(scope.Animation.WalkCount, Is.Zero);
            Assert.That(scope.Movement.TurnToCount, Is.EqualTo(1));
            Assert.That(scope.Movement.MoveToCount, Is.Zero);
            Assert.That(scope.Animation.AttackCount, Is.Zero);
        }

        [Test]
        public void Walk_회복이끝나고방향이맞으면Attack으로전환한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            scope.Movement.IsFacingTarget = false;
            machine.Enable();
            machine.FightMemory.CompleteAttack(0.15f);
            machine.Update(0.1f);
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));

            scope.Movement.IsFacingTarget = true;
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Attack));
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));
        }
    }
}
