using System;
using NUnit.Framework;
using Characters.Enemies.NightShade;
using UnityEngine;

namespace Tests.NightShade
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
        public void Update_Approach반복호출은관리힙을할당하지않는다()
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
            var targetStatus = new NightShadeSwordTargetStatus(
                scope.TargetObject.transform,
                scope.TargetDeathState,
                scope.Movement,
                settings.CombatRange);
            var memory = new NightShadeSwordCombatMemory();
            var context = new NightShadeSwordBehaviorContext(
                targetStatus,
                memory,
                scope.Movement,
                scope.Animation);
            var debug = new NightShadeSwordCombatDebug();
            var selector = new NightShadeSwordActionSelector(
                new FixedNightShadeSwordRandomProvider(),
                debug);
            var candidates = new INightShadeSwordCombatAction[4];
            candidates[0] = CreateAttack(
                NightShadeSwordActionId.Light,
                NightShadeSwordAttackType.Light,
                scope,
                context,
                settings);
            candidates[1] = new NightShadeSwordComboAction(
                context,
                settings.GetAttackData(NightShadeSwordActionId.Combo),
                settings.AttackSelection,
                scope.Actions.Value);
            candidates[2] = CreateAttack(
                NightShadeSwordActionId.Heavy,
                NightShadeSwordAttackType.Heavy,
                scope,
                context,
                settings);
            candidates[3] = CreateAttack(
                NightShadeSwordActionId.WideSwing,
                NightShadeSwordAttackType.WideSwing,
                scope,
                context,
                settings);
            memory.Reset();
            targetStatus.Refresh();
            for (int index = 0; index < 20; index++)
            {
                selector.Select(
                    NightShadeSwordCombatPhase.Attack,
                    candidates,
                    settings.AttackSelection.RandomBonusMax);
            }

            GC.GetAllocatedBytesForCurrentThread();
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1000; index++)
            {
                selector.Select(
                    NightShadeSwordCombatPhase.Attack,
                    candidates,
                    settings.AttackSelection.RandomBonusMax);
            }

            long allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            Assert.That(allocatedBytes, Is.Zero);
        }

        private static NightShadeSwordSingleAttackAction CreateAttack(
            NightShadeSwordActionId actionId,
            NightShadeSwordAttackType attackType,
            NightShadeSwordTestScope scope,
            NightShadeSwordBehaviorContext context,
            NightShadeSwordSettings settings)
        {
            return new NightShadeSwordSingleAttackAction(
                actionId,
                attackType,
                context,
                settings.GetAttackData(actionId),
                settings.AttackSelection,
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
            var debug = new NightShadeSwordCombatDebug();
            var runner = new NightShadeSwordActionRunner(debug);
            var first = new RunnerTestAction();
            var second = new RunnerTestAction();

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
            public NightShadeSwordActionId ActionId => NightShadeSwordActionId.Light;
            public bool IsFinished { get; private set; }
            internal bool FinishOnNextUpdate { get; set; }
            internal int EnterCount { get; private set; }
            internal int UpdateCount { get; private set; }
            internal int ExitCount { get; private set; }

            public bool CanStart(
                out NightShadeSwordActionRejectReason rejectReason)
            {
                rejectReason = NightShadeSwordActionRejectReason.None;
                return true;
            }

            public bool CanContinue(
                out NightShadeSwordActionStopReason stopReason)
            {
                stopReason = NightShadeSwordActionStopReason.None;
                return true;
            }

            public NightShadeSwordActionScore GetScore(float randomBonus)
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
