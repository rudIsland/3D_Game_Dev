using NUnit.Framework;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordSplitComboStateTests
    {
        [Test]
        public void ComboFirst_0점4에종료하고_0점15초대기후ComboSecond를실행한다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());

            FinishComboFirst(machine, scope);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Walk));
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.True);
            Assert.That(machine.FightMemory.RemainingAttackCooldown, Is.EqualTo(0.15f));
            Assert.That(machine.FightMemory.CompletedAttackCount, Is.Zero);

            machine.Update(0.14f);
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));
            Assert.That(scope.Animation.IdleCount, Is.GreaterThan(0));

            scope.Movement.IsFacingTarget = false;
            machine.Update(0.02f);
            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Walk));
            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));

            scope.Movement.IsFacingTarget = true;
            machine.Update(0.01f);

            Assert.That(machine.CurrentStateId, Is.EqualTo(NightShadeSwordStateId.Attack));
            Assert.That(scope.Animation.LastAttackType, Is.EqualTo(NightShadeSwordAttackType.ComboSecond));
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);

            Assert.That(machine.FightMemory.CompletedAttackCount, Is.EqualTo(1));
            Assert.That(machine.FightMemory.RemainingAttackCooldown, Is.EqualTo(2.5f));
        }

        [Test]
        public void ComboSecond대기중거리이탈_예약을취소하고재진입해도실행하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            FinishComboFirst(machine, scope);

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 4.5f);
            machine.Update(0.1f);

            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);
            Assert.That(machine.FightMemory.CompletedAttackCount, Is.EqualTo(1));
            Assert.That(machine.FightMemory.RemainingAttackCooldown, Is.EqualTo(2.5f));

            scope.TargetObject.transform.position =
                new Vector3(0f, 0f, 1f);
            machine.Update(0.8f);

            Assert.That(scope.Animation.AttackCount, Is.EqualTo(1));
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);
        }

        [Test]
        public void ComboSecond예약_피격사망Disable에서제거된다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            var hitRequest = new EnemyHitRequest(
                1f,
                1f,
                Vector3.zero,
                Vector3.back,
                0f,
                0f);

            FinishComboFirst(machine, scope);
            machine.ChangeToHitState(
                HitReaction.SmallHit,
                in hitRequest);
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);

            machine.Enable();
            FinishComboFirstFromEnabled(machine, scope);
            machine.ChangeToDeadState();
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);

            machine.Enable();
            FinishComboFirstFromEnabled(machine, scope);
            machine.Disable();
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);
            machine.Enable();
            Assert.That(machine.FightMemory.HasPendingComboSecond, Is.False);
        }

        [Test]
        public void 공격중대상사망_현재공격애니메이션이끝난후Idle로간다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 1f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.FightMemory.RecordAttack(
                NightShadeSwordAttackType.Light);
            machine.Update(0.1f);
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Attack));

            scope.TargetDeathState.IsDead = true;
            scope.Animation.NormalizedTime = 0.3f;
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Attack));

            scope.Animation.NormalizedTime = 1f;
            machine.Update(0.1f);

            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Idle));
            Assert.That(
                machine.FightMemory.HasPendingComboSecond,
                Is.False);
        }

        private static void FinishComboFirst(
            NightShadeSwordStateMachine machine,
            NightShadeSwordTestScope scope)
        {
            machine.Enable();
            FinishComboFirstFromEnabled(machine, scope);
        }

        private static void FinishComboFirstFromEnabled(
            NightShadeSwordStateMachine machine,
            NightShadeSwordTestScope scope)
        {
            machine.FightMemory.RecordAttack(NightShadeSwordAttackType.Light);
            machine.Update(0.1f);
            machine.Update(0.1f);
            Assert.That(scope.Animation.LastAttackType, Is.EqualTo(NightShadeSwordAttackType.ComboFirst));

            scope.Animation.NormalizedTime = 0.39f;
            machine.Update(0.1f);
            Assert.That(
                machine.CurrentStateId,
                Is.EqualTo(NightShadeSwordStateId.Attack));

            scope.Animation.NormalizedTime = 0.4f;
            machine.Update(0.1f);
        }
    }
}
