using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordStateMachineTests
    {
        [Test]
        public void Update_Idle에서Combat단계를순서대로진행한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());

            machine.Enable();
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Idle));

            machine.Update(0.1f);
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Combat));
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Positioning));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WatchTarget));

            machine.Update(0.1f);
            Assert.That(machine.Debug.LastEvaluatedPhase, Is.EqualTo(NightShadeSwordCombatPhase.Attack));
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Attack));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Recovery));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.LeftRecovery));

            machine.Update(0.6f);
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Positioning));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WatchTarget));
        }

        [Test]
        public void Recovery_0점6초가끝나도공격쿨다운이남으면공격하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            int attackCount = scope.Animation.AttackCount;

            machine.Update(0.6f);
            machine.Update(1.3f);

            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Positioning));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.WatchTarget));
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(attackCount));

            machine.Update(0.2f);
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Attack));
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(attackCount + 1));
        }

        [Test]
        public void Disable_Enable_상태와전투기억을초기화한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.Update(0.1f);
            machine.FightMemory.RecordAttack(NightShadeSwordActionId.Heavy);
            machine.FightMemory.StartPostAttackDelay(2f);

            machine.Disable();
            machine.Enable();

            Assert.That(machine.IsInCombat, Is.False);
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Idle));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.None));
            Assert.That(machine.FightMemory.HasPreviousAttack, Is.False);
            Assert.That(machine.FightMemory.RemainingPostAttackDelay, Is.Zero);
        }
    }
}
