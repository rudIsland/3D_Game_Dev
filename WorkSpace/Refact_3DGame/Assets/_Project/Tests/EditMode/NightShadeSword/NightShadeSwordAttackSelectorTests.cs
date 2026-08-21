using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAttackSelectorTests
    {
        [TestCase(1f, 5)]
        [TestCase(2.2f, 4)]
        [TestCase(2.6f, 7)]
        [TestCase(3.6f, 6)]
        public void Decision_거리별최고점공격을고른다(
            float distance,
            int expectedActionValue)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());

            EnterAttack(machine);

            var expectedAction = (NightShadeSwordActionId)expectedActionValue;
            Assert.That(machine.CurrentActionId, Is.EqualTo(expectedAction));
            Assert.That(machine.Debug.SelectedAction, Is.EqualTo(expectedAction));
        }

        [Test]
        public void Decision_직전공격반복감점이최종점수와선택에반영된다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            machine.Enable();
            machine.FightMemory.RecordAttack(NightShadeSwordActionId.Light);

            machine.Update(0.1f);
            machine.Update(0.1f);

            NightShadeSwordActionDebugEntry light = machine.Debug.Candidates[0];
            Assert.That(light.ActionId, Is.EqualTo(NightShadeSwordActionId.Light));
            Assert.That(light.Score.RepeatPenalty, Is.EqualTo(0.25f));
            Assert.That(
                light.Score.FinalScore,
                Is.EqualTo(
                    Mathf.Clamp01(
                        light.Score.BaseScore +
                        light.Score.DistanceScore -
                        light.Score.RepeatPenalty +
                        light.Score.RandomBonus)).Within(0.0001f));
            Assert.That(
                machine.CurrentActionId,
                Is.EqualTo(NightShadeSwordActionId.WideSwing));
        }

        [Test]
        public void Decision_고정난수공급자는같은입력에서같은결과를낸다()
        {
            NightShadeSwordActionId first = SelectAtDistance(2.6f, 0.75f);
            NightShadeSwordActionId second = SelectAtDistance(2.6f, 0.75f);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void FightMemory_공격Recovery와쿨다운을초기화하고0까지만감소시킨다()
        {
            var memory = new NightShadeSwordFightMemory();
            memory.Reset();
            memory.RecordAttack(NightShadeSwordActionId.Heavy);
            memory.RecordRecovery(NightShadeSwordActionId.LeftRecovery);
            memory.StartPostAttackDelay(1f);
            memory.UpdatePostAttackDelay(2f);

            Assert.That(memory.RemainingPostAttackDelay, Is.Zero);
            Assert.That(memory.HasPreviousAttack, Is.True);
            Assert.That(memory.HasPreviousRecovery, Is.True);

            memory.Reset();

            Assert.That(memory.HasPreviousAttack, Is.False);
            Assert.That(memory.HasPreviousRecovery, Is.False);
            Assert.That(memory.RecentSelection, Is.EqualTo(NightShadeSwordActionId.None));
        }

        private static NightShadeSwordActionId SelectAtDistance(
            float distance,
            float randomValue)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider(randomValue));
            EnterAttack(machine);
            return machine.CurrentActionId;
        }

        private static void EnterAttack(NightShadeSwordStateMachine machine)
        {
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentCombatPhase,
                Is.EqualTo(NightShadeSwordCombatPhase.Attack));
        }
    }
}
