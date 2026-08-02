using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // EnemyUnit 생명주기에서 Nightshade 상태머신과 체력을 연결한다.
    public sealed class NightshadeSpearWorldUnit : EnemyUnit
    {
        private readonly NightshadeSpearStateMachine stateMachine; // 현재 행동 상태
        private readonly UnitStagger stagger;

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력
        public float CurrentStagger => stagger.CurrentStagger;
        public int CurrentPhase => stateMachine.CurrentPhase;
        public string CurrentStateName => stateMachine.CurrentStateName;
        public string CurrentAttackName => stateMachine.CurrentAttackName;

        public NightshadeSpearWorldUnit(
            float maxHealth,
            NightshadeSpearStateMachine stateMachine,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed)
            : base(maxHealth)
        {
            this.stateMachine = stateMachine;
            stagger = new UnitStagger(
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed);
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

        public AttackHitResult ApplyHit(in AttackHitData hit)
        {
            AttackHitResult hitResult = ApplyHealthAndStaggerHit(
                in hit,
                stagger);
            if (hitResult == AttackHitResult.Damaged ||
                hitResult == AttackHitResult.Staggered)
            {
                stateMachine.SetHealthRatio(
                    Health.CurrentHealth / Health.MaxHealth);
                stateMachine.ChangeToHitState(in hit);
            }

            return hitResult;
        }

        protected override void OnUnitCreate()
        {
            Health.Died += HandleHealthDied;
        }

        protected override void OnEnemyEnable()
        {
            stagger.Reset();
            stateMachine.Enable();
        }

        protected override void OnUnitTick(float deltaTime)
        {
            stagger.Update(deltaTime);
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
