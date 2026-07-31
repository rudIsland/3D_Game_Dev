namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // EnemyUnit 생명주기에서 Zombie HFSM을 실행한다.
    public sealed class ZombieWorldUnit : EnemyUnit
    {
        private readonly ZombieStateMachine stateMachine;

        public string CurrentStateName => stateMachine.CurrentStateName;

        public ZombieWorldUnit(float maxHealth,ZombieStateMachine stateMachine): base(maxHealth)
        {
            this.stateMachine = stateMachine;
            stateMachine.SetHealth(Health);
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

        public void TakeDamage(float damage)
        {
            stateMachine.TakeDamage(damage);
        }
    }
}
