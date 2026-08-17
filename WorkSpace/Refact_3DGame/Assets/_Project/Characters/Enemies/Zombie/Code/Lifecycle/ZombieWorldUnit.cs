using System;
using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // EnemyUnit 생명주기에서 좀비의 탐지·추적 상태머신을 실행한다.
    public sealed class ZombieWorldUnit : EnemyUnit
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태
        private readonly ZombieAttackRangeDetector attackRangeDetector;
        private readonly ZombieStagger stagger;
        private readonly CombatHitStop hitStop;

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStagger => stagger.CurrentStagger;
        public float MaxStagger => stagger.StaggerLimit;
        public bool IsInCombat => stateMachine.IsInCombat;

        public event Action<ZombieWorldUnit> StaggerChanged;
        public event Action<ZombieWorldUnit> CombatStateChanged;

        internal ZombieWorldUnit(
            float maxHealth,
            ZombieStateMachine stateMachine,
            ZombieAttackRangeDetector attackRangeDetector,
            ZombieStagger stagger,
            CombatHitStop hitStop)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
            this.attackRangeDetector = attackRangeDetector;
            this.stagger = stagger;
            this.hitStop = hitStop;
        }

        public EnemyHitResult TakeHit(in EnemyHitRequest hitRequest)
        {
            float healthBeforeDamage = Health.CurrentHealth;

            Health.TakeDamage(hitRequest.Damage);

            if (Health.CurrentHealth >= healthBeforeDamage)
            {
                return EnemyHitResult.Ignored;
            }

            if (IsDead)
            {
                hitStop.Request(hitRequest.HitStopDuration);
                return EnemyHitResult.Damaged;
            }

            stateMachine.NotifyDamaged();
            bool shouldEnterHitState =
                stagger.TryAccumulate(hitRequest.StaggerDamage);
            if (hitRequest.StaggerDamage > 0f)
            {
                StaggerChanged?.Invoke(this);
            }

            if (shouldEnterHitState)
            {
                hitStop.Request(hitRequest.HitStopDuration);
                stateMachine.ChangeToHitState(in hitRequest);
                return EnemyHitResult.Staggered;
            }

            hitStop.Request(hitRequest.HitStopDuration);
            return EnemyHitResult.Damaged;
        }

        internal void NotifyAttackAnimationEnded()
        {
            stateMachine.NotifyAttackAnimationEnded();
        }

        internal bool BeginAttackHit()
        {
            return stateMachine.BeginAttackHit();
        }

        internal bool BeginAttackRecovery()
        {
            return stateMachine.BeginAttackRecovery();
        }

        internal void NotifyAlertAnimationEnded()
        {
            stateMachine.NotifyAlertAnimationEnded();
        }

        protected override void OnUnitCreate()
        {
            Health.Died += HandleHealthDied;
            stateMachine.CombatStateChanged +=
                HandleCombatStateChanged;
        }

        protected override void OnEnemyEnable()
        {
            hitStop.Reset();
            attackRangeDetector.Close();
            stagger.Reset();
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            if (hitStop.Update(deltaTime))
            {
                return;
            }

            if (stagger.UpdateRecovery(deltaTime))
            {
                StaggerChanged?.Invoke(this);
            }

            stateMachine.Update(deltaTime);
            attackRangeDetector.Tick();
        }

        protected override void OnUnitDisable()
        {
            hitStop.Reset();
            stateMachine.Disable();
            attackRangeDetector.Close();
        }

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
            stateMachine.CombatStateChanged -=
                HandleCombatStateChanged;
        }

        private void HandleHealthDied()
        {
            stateMachine.ChangeToDeadState();
        }

        private void HandleCombatStateChanged()
        {
            CombatStateChanged?.Invoke(this);
        }
    }
}
