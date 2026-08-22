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
            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.Combo));
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Attack));
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));

            machine.Update(0.14f);
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));

            machine.Update(0.01f);
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(2));
            Assert.That(scope.Animation.LastAttackType, Is.EqualTo(NightShadeSwordAttackType.ComboSecond));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Recovery));
            Assert.That(machine.CombatMemory.RemainingPostAttackDelay, Is.EqualTo(2.5f));
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
            Assert.That(machine.CombatMemory.RemainingPostAttackDelay, Is.EqualTo(2.5f));
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

        [Test]
        public void Combo_2타시작때이동거리를새로계산한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1.4f));
            NightShadeSwordStateMachine machine = EnterCombo(scope);

            scope.Animation.NormalizedTime = 0.4f;
            machine.Update(0.1f);
            Assert.That(
                scope.Movement.TotalAttackMoveDistance,
                Is.EqualTo(0.1f).Within(0.0001f));

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 2f);
            machine.Update(0.15f);
            Assert.That(
                scope.Animation.LastAttackType,
                Is.EqualTo(NightShadeSwordAttackType.ComboSecond));

            scope.Animation.NormalizedTime = 0.18f;
            machine.Update(0.1f);

            Assert.That(scope.Movement.AttackMoveCount, Is.EqualTo(2));
            Assert.That(
                scope.Movement.TotalAttackMoveDistance,
                Is.EqualTo(0.45f).Within(0.0001f));
        }

        [Test]
        public void Combo_2타판정은Combo두번째피해를출력한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine = EnterCombo(scope);

            scope.Animation.NormalizedTime = 0.4f;
            machine.Update(0.1f);
            machine.Update(0.15f);
            machine.OpenAttackHitAnimationEvent(0);
            machine.Update(0f);

            Assert.That(scope.Actions.OpenedDamages.Count, Is.EqualTo(1));
            Assert.That(
                scope.Actions.OpenedDamages[0].HealthDamage,
                Is.EqualTo(21f));
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
