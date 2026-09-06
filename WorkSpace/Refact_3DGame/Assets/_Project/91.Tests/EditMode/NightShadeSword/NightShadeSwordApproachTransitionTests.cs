using NUnit.Framework;
using Characters.Enemies.NightShade;
using UnityEngine;

namespace Tests.NightShade
{
    public sealed class NightShadeSwordApproachTransitionTests
    {
        [TestCase(1f, 9)]
        [TestCase(2f, 10)]
        [TestCase(3.6f, 8)]
        public void Recovery_거리별Utility최고점Action을고른다(
            float distance,
            int expectedActionValue)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());

            EnterRecovery(machine, scope);

            var expectedAction = (NightShadeSwordActionId)expectedActionValue;
            Assert.That(machine.CurrentActionId, Is.EqualTo(expectedAction));
            Assert.That(machine.Debug.SelectedAction, Is.EqualTo(expectedAction));
        }

        [Test]
        public void Recovery_고정난수와동점등록순서에따라Right를재현할수있다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2f));
            var random = new SequenceNightShadeSwordRandomProvider(
                0f, 0f, 0f, 0f,
                0f, 0f, 0f, 1f);
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                random);

            EnterRecovery(machine, scope);

            Assert.That(machine.CurrentActionId, Is.EqualTo(NightShadeSwordActionId.RightRecovery));
        }

        [Test]
        public void Recovery_대상이사라지면즉시Idle로돌아간다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings(),
                new FixedNightShadeSwordRandomProvider());
            EnterRecovery(machine, scope);

            scope.TargetDeathState.IsDead = true;
            machine.Update(0.1f);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Idle));
        }

        private static void EnterRecovery(
            NightShadeSwordStateMachine machine,
            NightShadeSwordTestScope scope)
        {
            machine.Enable();
            machine.Update(0.1f);
            machine.Update(0.1f);
            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);
            if (machine.CurrentCombatPhase == NightShadeSwordCombatPhase.Attack &&
                machine.CurrentActionId == NightShadeSwordActionId.Combo)
            {
                machine.Update(0.15f);
                scope.Animation.NormalizedTime = 1f;
                machine.Update(0.1f);
            }
            Assert.That(machine.CurrentCombatPhase, Is.EqualTo(NightShadeSwordCombatPhase.Recovery));
        }
    }
}
