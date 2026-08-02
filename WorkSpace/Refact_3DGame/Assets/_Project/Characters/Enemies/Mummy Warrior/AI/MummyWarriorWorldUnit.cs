using rudIsland.RPG3D.Combat;

namespace rudIsland.RPG3D.Characters.Enemies.MummyWarrior
{
    // EnemyUnit 생명주기에서 Mummy Warrior 상태머신과 체력을 연결한다.
    public sealed class MummyWarriorWorldUnit : EnemyUnit
    {
        private readonly MummyWarriorStateMachine stateMachine;

        public float CurrentHealth => Health.CurrentHealth;

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
            stateMachine.ChangeToHitState();
        }

        public void ApplyHit(in AttackHitData hit)
        {
            if (hit.AttackerTeam != Team) TakeDamage(hit.Damage.HealthDamage);
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
