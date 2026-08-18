using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAttackStateTests
    {
        [Test]
        public void AnimationEvent_ComboFirst는HitIndex0만한번처리한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordAttackState state = CreateComboState(scope);
            state.Enter();

            state.QueuePlaySound(0);
            state.QueueOpenHit(0);
            state.QueueCloseHit();
            state.QueuePlaySound(1);
            state.QueueOpenHit(1);
            state.QueueCloseHit();

            Assert.That(scope.Actions.Calls, Is.Empty);
            state.Update(0.1f);

            Assert.That(
                scope.Actions.Calls,
                Is.EqualTo(new[]
                {
                    "Sound:0",
                    "Open:0",
                    "Close"
                }));
        }

        [Test]
        public void AnimationEvent_중복되거나순서가늦은이벤트는무시한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordAttackState state = CreateComboState(scope);
            state.Enter();

            state.QueuePlaySound(0);
            state.QueuePlaySound(0);
            state.QueueOpenHit(0);
            state.QueueCloseHit();
            state.QueueOpenHit(0);
            state.Update(0.1f);

            Assert.That(
                scope.Actions.Calls,
                Is.EqualTo(new[] { "Sound:0", "Open:0", "Close" }));

            state.Exit();
            int oldCallCount = scope.Actions.Calls.Count;
            state.QueuePlaySound(1);
            Assert.That(state.QueuedEventCount, Is.Zero);
            Assert.That(scope.Actions.Calls.Count, Is.EqualTo(oldCallCount));
        }

        [Test]
        public void StopTurnEvent_다음Update부터공격회전을멈춘다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordAttackState state = CreateComboState(scope);
            state.Enter();
            state.QueueStopTurn();

            state.Update(0.1f);

            Assert.That(scope.Movement.TurnToCount, Is.Zero);
            Assert.That(scope.Movement.StayCount, Is.EqualTo(1));
        }

        [Test]
        public void Exit_열린판정을닫고이벤트와공격속도를초기화한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordAttackState state = CreateComboState(scope);
            state.Enter();
            state.QueueOpenHit(0);
            state.Update(0.1f);
            int resetCountBeforeExit = scope.Animation.ResetSpeedCount;

            state.Exit();

            Assert.That(scope.Actions.CloseCount, Is.EqualTo(1));
            Assert.That(state.QueuedEventCount, Is.Zero);
            Assert.That(
                scope.Animation.ResetSpeedCount,
                Is.EqualTo(resetCountBeforeExit + 1));
        }

        private static NightShadeSwordAttackState CreateComboState(NightShadeSwordTestScope scope)
        {
            NightShadeSwordSettings settings = scope.CreateSettings();
            var targetReader = new NightShadeSwordTargetReader(
                scope.TargetObject.transform,
                scope.TargetDeathState,
                scope.Movement);
            targetReader.Refresh();
            var fightMemory = new NightShadeSwordFightMemory();
            fightMemory.Reset();
            fightMemory.RecordAttack(NightShadeSwordAttackType.Light);

            return new NightShadeSwordAttackState(
                targetReader,
                scope.Movement,
                scope.Animation,
                settings,
                fightMemory,
                new NightShadeSwordAttackSelector(
                    settings.AttackRangeSquared),
                scope.Actions.Value);
        }
    }
}
