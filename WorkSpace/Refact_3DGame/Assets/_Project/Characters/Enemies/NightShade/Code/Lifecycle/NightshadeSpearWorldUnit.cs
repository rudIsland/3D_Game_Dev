using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // EnemyUnit 생명주기에서 Nightshade 상태머신과 체력을 연결한다.
    public sealed class NightshadeSpearWorldUnit : EnemyUnit
    {
        private readonly NightshadeSpearStateMachine stateMachine; // 현재 행동 상태

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStagger => Stagger.CurrentStagger;
        public int CurrentPhase => stateMachine.CurrentPhase;
        public string CurrentStateName => stateMachine.CurrentStateName;
        public string CurrentAttackName => stateMachine.CurrentAttackName;

        public NightshadeSpearWorldUnit(
            float maxHealth,
            NightshadeSpearStateMachine stateMachine,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed)
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

            if (Health.CurrentHealth >= healthBeforeDamage || IsDead) return;
            stateMachine.SetHealthRatio(
                Health.CurrentHealth / Health.MaxHealth);
            stateMachine.ChangeToHitState();
        }

        protected override void HandleAttackHitResult(
            in AttackHitResult result)
        {
            if (result.Type == AttackHitResultType.Staggered ||
                result.Type == AttackHitResultType.KnockedDown)
            {
                stateMachine.SetHealthRatio(
                    Health.CurrentHealth / Health.MaxHealth);
                HitReaction reaction = result.Reaction;
                stateMachine.ChangeToHitState(
                    in reaction);
            }
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
        protected override void OnUnitDisable() => stateMachine.Disable();

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
        }

        private void HandleHealthDied() => stateMachine.ChangeToDeadState();
    }
}
