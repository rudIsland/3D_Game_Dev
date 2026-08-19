using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordApproachTransitionTests
    {
        [TestCase(5.5f, true)]
        [TestCase(6f, false)]
        public void Attack_종료후거리별로Walk또는Chase를선택한다(float distance, bool expectsWalk)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            EnterAttack(machine);
            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, distance);
            scope.Animation.NormalizedTime = 1f;

            machine.Update(0.1f);

            AssertApproachState(machine.CurrentStateId, expectsWalk);
        }

        [Test]
        public void Attack_대상이없어지면Idle로전환한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 3f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            EnterAttack(machine);
            scope.TargetDeathState.IsDead = true;
            scope.Animation.NormalizedTime = 1f;

            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Idle));
        }

        [TestCase(1f, 2)]
        [TestCase(3f, 1)]
        public void Attack_매우가깝거나공격횟수를채우면CombatMove로전환한다(
            float distance,
            int attacksBeforeCombatMove)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordSettings settings = scope.CreateSettings(
                attacksBeforeCombatMove: attacksBeforeCombatMove);
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(settings);
            machine.Enable();
            machine.FightMemory.RecordAttack(
                NightShadeSwordAttackType.ComboFirst);
            EnterAttack(machine);
            scope.Animation.NormalizedTime = 1f;

            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.CombatMove));
        }

        [TestCase(5.5f, true)]
        [TestCase(6f, false)]
        public void CombatMove_종료후거리별로Walk또는Chase를선택한다(
            float distance,
            bool expectsWalk)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordSettings settings = scope.CreateSettings();
            var targetReader = new NightShadeSwordTargetReader(
                scope.TargetObject.transform,
                scope.TargetDeathState,
                scope.Movement);
            targetReader.Refresh();
            var fightMemory = new NightShadeSwordFightMemory();
            fightMemory.Reset();
            var state = new NightShadeSwordCombatMoveState(
                targetReader,
                scope.Movement,
                scope.Animation,
                settings,
                fightMemory);
            state.Enter();

            NightShadeSwordStateId? nextState = state.Update(0.6f);

            Assert.That(nextState.HasValue, Is.True);
            AssertApproachState(nextState.Value, expectsWalk);
        }

        [TestCase(5.5f, true)]
        [TestCase(6f, false)]
        public void Hit_종료후거리별로Walk또는Chase를선택한다(
            float distance,
            bool expectsWalk)
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, distance));
            NightShadeSwordSettings settings = scope.CreateSettings();
            var targetReader = new NightShadeSwordTargetReader(
                scope.TargetObject.transform,
                scope.TargetDeathState,
                scope.Movement);
            targetReader.Refresh();
            var fightMemory = new NightShadeSwordFightMemory();
            fightMemory.Reset();
            var state = new NightShadeSwordHitState(
                targetReader,
                scope.Movement,
                scope.Animation,
                settings,
                fightMemory);
            var hitRequest = new EnemyHitRequest(
                1f,
                100f,
                Vector3.zero,
                Vector3.back,
                1f,
                0f);
            state.SetHitRequest(
                HitReaction.BigHit,
                in hitRequest);
            state.Enter();
            scope.Animation.NormalizedTime = 1f;

            NightShadeSwordStateId? nextState = state.Update(0.2f);

            Assert.That(nextState.HasValue, Is.True);
            AssertApproachState(nextState.Value, expectsWalk);
        }

        private static void EnterAttack(NightShadeSwordStateMachine machine)
        {
            machine.Update(0.1f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Attack));
        }

        private static void AssertApproachState(
            NightShadeSwordStateId actualState,
            bool expectsWalk)
        {
            NightShadeSwordStateId expectedState = expectsWalk
                ? NightShadeSwordStateId.Walk
                : NightShadeSwordStateId.Chase;
            Assert.That(actualState, Is.EqualTo(expectedState));
        }
    }
}
