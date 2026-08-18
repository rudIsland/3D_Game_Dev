using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordStateMachineTests
    {
        [Test]
        public void Update_가까운대상을찾고방향이맞으면Idle_Walk_Attack순서로전환한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());

            machine.Enable();
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Idle));

            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));

            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Attack));
            Assert.That(machine.IsAttackStateActive, Is.True);
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Update_공격거리안에서방향이맞지않으면회전만한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            scope.Movement.IsFacingTarget = false;
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());

            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));
            Assert.That(scope.Movement.TurnToCount, Is.EqualTo(1));
            Assert.That(scope.Movement.MoveToCount, Is.Zero);
        }

        [Test]
        public void Update_공격회복중에는새공격을시작하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.FightMemory.CompleteAttack(1f);

            machine.Update(0.1f);
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Walk));
            Assert.That(scope.Animation.AttackCount, Is.Zero);
        }

        [Test]
        public void Disable_Enable_전투상태와공격기억을초기화한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.Update(0.1f);
            machine.FightMemory.RecordAttack(
                NightShadeSwordAttackType.Heavy);
            machine.FightMemory.CompleteAttack(2f);

            machine.Disable();
            Assert.That(machine.IsAttackStateActive, Is.False);
            machine.Enable();

            Assert.That(machine.IsInCombat, Is.False);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Idle));
            Assert.That(machine.FightMemory.HasPreviousAttack, Is.False);
            Assert.That(
                machine.FightMemory.RemainingAttackCooldown,
                Is.Zero);
            Assert.That(machine.FightMemory.CompletedAttackCount, Is.Zero);
        }
    }
}
