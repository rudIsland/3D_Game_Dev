
namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    // EnemyUnit 생명주기에서 Mummy Warrior 상태머신과 체력을 연결한다.
    public sealed class MummyWarriorWorldUnit : EnemyUnit
    {
        private readonly MummyWarriorStateMachine stateMachine; // 현재 행동 상태

        public float CurrentHealth => Health.CurrentHealth; // 현재 체력

        public MummyWarriorWorldUnit(float maxHealth, MummyWarriorStateMachine stateMachine)
            : base(maxHealth)
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

        protected override void OnUnitCreate()
        {
            Health.Died += HandleHealthDied;
        }

        protected override void OnEnemyEnable() => stateMachine.Enable();
        protected override void OnUnitTick(float deltaTime) => stateMachine.Update(deltaTime);
        protected override void OnUnitDisable() => stateMachine.Disable();

        protected override void OnUnitDispose()
        {
            Health.Died -= HandleHealthDied;
        }

        private void HandleHealthDied() => stateMachine.ChangeToDeadState();
    }
}
