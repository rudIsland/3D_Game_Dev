using System;
using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAllocationTests
    {
        [Test]
        public void Update_Idle반복호출은관리힙을할당하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 20f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            machine.Enable();
            WarmUp(machine);

            long allocatedBytes = MeasureUpdates(machine, 1000);

            Assert.That(allocatedBytes, Is.Zero);
        }

        [Test]
        public void Update_Positioning반복호출은관리힙을할당하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 4.5f));
            NightShadeSwordStateMachine machine = scope.CreateStateMachine(
                scope.CreateSettings());
            machine.Enable();
            machine.Update(0.016f);
            WarmUp(machine);

            long allocatedBytes = MeasureUpdates(machine, 1000);

            Assert.That(allocatedBytes, Is.Zero);
        }

        [Test]
        public void Select_공격후보반복평가는관리힙을할당하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2.2f));
            NightShadeSwordSettings settings = scope.CreateSettings();
            var situation = new NightShadeSwordSituationReader(
                scope.TargetObject.transform,
                scope.TargetDeathState,
                scope.Movement,
                settings);
            var memory = new NightShadeSwordFightMemory();
            var debug = new NightShadeSwordCombatDebug();
            var selector = new NightShadeSwordActionSelector(
                new FixedNightShadeSwordRandomProvider(),
                settings,
                debug);
            var candidates = new INightShadeSwordCombatAction[4];
            candidates[0] = CreateAttack(
                NightShadeSwordActionId.Light,
                NightShadeSwordAttackType.Light,
                scope,
                situation,
                memory,
                settings);
            candidates[1] = new NightShadeSwordComboAction(
                situation,
                memory,
                scope.Movement,
                scope.Animation,
                settings,
                scope.Actions.Value);
            candidates[2] = CreateAttack(
                NightShadeSwordActionId.Heavy,
                NightShadeSwordAttackType.Heavy,
                scope,
                situation,
                memory,
                settings);
            candidates[3] = CreateAttack(
                NightShadeSwordActionId.WideSwing,
                NightShadeSwordAttackType.WideSwing,
                scope,
                situation,
                memory,
                settings);
            memory.Reset();
            situation.Refresh();
            for (int index = 0; index < 20; index++)
            {
                selector.Select(candidates, candidates.Length, situation, memory);
            }

            GC.GetAllocatedBytesForCurrentThread();
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1000; index++)
            {
                selector.Select(candidates, candidates.Length, situation, memory);
            }

            long allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            Assert.That(allocatedBytes, Is.Zero);
        }

        private static NightShadeSwordSingleAttackAction CreateAttack(
            NightShadeSwordActionId actionId,
            NightShadeSwordAttackType attackType,
            NightShadeSwordTestScope scope,
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory memory,
            NightShadeSwordSettings settings)
        {
            return new NightShadeSwordSingleAttackAction(
                actionId,
                attackType,
                situation,
                memory,
                scope.Movement,
                scope.Animation,
                settings,
                scope.Actions.Value);
        }

        private static void WarmUp(NightShadeSwordStateMachine machine)
        {
            for (int index = 0; index < 20; index++)
            {
                machine.Update(0.016f);
            }
        }

        private static long MeasureUpdates(
            NightShadeSwordStateMachine machine,
            int count)
        {
            GC.GetAllocatedBytesForCurrentThread();
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < count; index++)
            {
                machine.Update(0.016f);
            }

            return GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
        }
    }

    public sealed class NightShadeSwordActionRunnerTests
    {
        [Test]
        public void Runner_EnterUpdateExit를한번씩호출하고계속가능하면교체하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 2f));
            NightShadeSwordSettings settings = scope.CreateSettings();
            var situation = new NightShadeSwordSituationReader(
                scope.TargetObject.transform,
                scope.TargetDeathState,
                scope.Movement,
                settings);
            var memory = new NightShadeSwordFightMemory();
            var debug = new NightShadeSwordCombatDebug();
            var runner = new NightShadeSwordActionRunner(
                situation,
                memory,
                debug);
            var first = new RunnerTestAction();
            var second = new RunnerTestAction();
            situation.Refresh();
            memory.Reset();

            Assert.That(runner.Start(first), Is.True);
            Assert.That(runner.Start(second), Is.False);
            Assert.That(runner.Update(0.1f), Is.False);
            Assert.That(runner.Update(0.1f), Is.False);
            first.FinishOnNextUpdate = true;
            Assert.That(runner.Update(0.1f), Is.True);

            Assert.That(first.EnterCount, Is.EqualTo(1));
            Assert.That(first.UpdateCount, Is.EqualTo(3));
            Assert.That(first.ExitCount, Is.EqualTo(1));
            Assert.That(second.EnterCount, Is.Zero);
        }

        private sealed class RunnerTestAction : INightShadeSwordCombatAction
        {
            public NightShadeSwordActionId ActionId => NightShadeSwordActionId.WatchTarget;
            public NightShadeSwordCombatPhase Phase => NightShadeSwordCombatPhase.Positioning;
            public bool IsFinished { get; private set; }
            internal bool FinishOnNextUpdate { get; set; }
            internal int EnterCount { get; private set; }
            internal int UpdateCount { get; private set; }
            internal int ExitCount { get; private set; }

            public bool CanStart(
                NightShadeSwordSituationReader situation,
                NightShadeSwordFightMemory fightMemory,
                out NightShadeSwordActionRejectReason rejectReason)
            {
                rejectReason = NightShadeSwordActionRejectReason.None;
                return true;
            }

            public bool CanContinue(
                NightShadeSwordSituationReader situation,
                NightShadeSwordFightMemory fightMemory,
                out NightShadeSwordActionStopReason stopReason)
            {
                stopReason = NightShadeSwordActionStopReason.None;
                return true;
            }

            public NightShadeSwordActionScore GetScore(
                NightShadeSwordSituationReader situation,
                NightShadeSwordFightMemory fightMemory,
                float randomBonus)
            {
                return default;
            }

            public void Enter()
            {
                EnterCount++;
                IsFinished = false;
            }

            public void Update(float deltaTime)
            {
                UpdateCount++;
                IsFinished = FinishOnNextUpdate;
            }

            public void Exit(NightShadeSwordActionStopReason stopReason)
            {
                ExitCount++;
            }
        }
    }
}
