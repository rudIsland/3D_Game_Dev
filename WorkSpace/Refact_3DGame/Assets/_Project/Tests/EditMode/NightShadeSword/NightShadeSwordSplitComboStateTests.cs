using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordSplitComboStateTests
    {
        [Test]
        public void Combo_1타후0점15초를기다리고2타를같은Action에서실행한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine = EnterCombo(scope);

            scope.Animation.NormalizedTime = 0.4f;
            machine.Update(0.1f);
            Assert.That(
                machine.FightMemory.ComboStep,
                Is.EqualTo(NightShadeSwordComboStep.Connecting));
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Combo));

            machine.Update(0.14f);
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));

            machine.Update(0.01f);
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(2));
            Assert.That(scope.Animation.LastAttackType, Is.EqualTo(NightShadeSwordAttackType.ComboSecond));
            Assert.That(
                machine.FightMemory.ComboStep,
                Is.EqualTo(NightShadeSwordComboStep.ComboSecond));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Recovery));
            Assert.That(machine.FightMemory.RemainingPostAttackDelay, Is.EqualTo(2.5f));
        }

        [Test]
        public void Combo_연결전에공격거리를벗어나면2타를취소하고Recovery로간다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine = EnterCombo(scope);
            scope.Animation.NormalizedTime = 0.4f;
            machine.Update(0.1f);

            scope.TargetObject.transform.position = new Vector3(0f, 0f, 4.5f);
            machine.Update(0.01f);

            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Recovery));
            Assert.That(machine.FightMemory.ComboStep, Is.EqualTo(NightShadeSwordComboStep.None));
            Assert.That(machine.FightMemory.RemainingPostAttackDelay, Is.EqualTo(2.5f));
        }

        [Test]
        public void 공격중대상이사라지면애니메이션완료후Idle로간다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);

            scope.TargetDeathState.IsDead = true;
            scope.Animation.NormalizedTime = 0.5f;
            machine.Update(0.1f);
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Attack));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Idle));
        }

        private static NightShadeSwordStateMachine EnterCombo(
            NightShadeSwordTestScope scope)
        {
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Combo));
            Assert.That(scope.Animation.LastAttackType, Is.EqualTo(NightShadeSwordAttackType.ComboFirst));
            return machine;
        }
    }
}
