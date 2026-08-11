
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // EnemyUnit 생명주기에서 좀비의 탐지·추적 상태머신을 실행한다.
    public sealed class ZombieWorldUnit : EnemyUnit
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public ZombieWorldUnit(
            float maxHealth,
            ZombieStateMachine stateMachine)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
        }

        public void TakeDamage(float damage, Vector3 hitPosition)
        {
            float healthBeforeDamage = Health.CurrentHealth;

            Health.TakeDamage(damage);

            if (Health.CurrentHealth >= healthBeforeDamage)
            {
                return;
            }

            if (IsDead)
            {
                return;
            }

            stateMachine.ChangeToHitState();
        }

        internal void NotifyAttackAnimationEnded()
        {
            stateMachine.NotifyAttackAnimationEnded();
        }

        internal bool CanTurnDuringAttack()
        {
            return stateMachine.CanTurnDuringAttack();
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
        }

        protected override void OnEnemyEnable()
        {
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            stateMachine.Update(deltaTime);
        }

        protected override void OnUnitDisable()
        {
            stateMachine.Disable();
        }

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
        }

        private void HandleHealthDied()
        {
            stateMachine.ChangeToDeadState();
        }
    }
}
