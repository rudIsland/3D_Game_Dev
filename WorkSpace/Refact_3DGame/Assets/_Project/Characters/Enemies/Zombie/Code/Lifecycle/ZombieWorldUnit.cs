using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // EnemyUnit 생명주기에서 좀비의 탐지·추적 상태머신을 실행한다.
    public sealed class ZombieWorldUnit : EnemyUnit
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStagger => Stagger.CurrentStagger; // 현재 경직 누적값
        public HitReaction LastHitReaction =>
            stateMachine.LastHitReaction;

        public ZombieWorldUnit(
            float maxHealth,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed,
            ZombieStateMachine stateMachine)
            : base(
                maxHealth,
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed,
                0f,
                0f,
                0f,
                0f)
        {
            this.stateMachine = stateMachine;
        }

        public void TakeDamage(float damage)
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

        protected override void HandleAttackHitResult(
            in AttackHitResult result)
        {
            if (result.Type == AttackHitResultType.Staggered ||
                result.Type == AttackHitResultType.KnockedDown)
            {
                HitReaction reaction = result.Reaction;
                stateMachine.ChangeToHitState(
                    in reaction);
            }
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
