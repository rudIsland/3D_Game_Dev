namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // EnemyUnit 생명주기와 보스 상태 머신을 연결한다.
    public sealed class DemonSwordsmanWorldUnit : EnemyUnit
    {
        private readonly DemonSwordsmanStateMachine stateMachine;

        public override bool IsBoss => true;
        public DemonSwordsmanPhase CurrentPhase =>
            stateMachine.CurrentPhase;
        public DemonSwordsmanStyle CurrentStyle =>
            stateMachine.CurrentStyle;
        public string CurrentStateName =>
            stateMachine.CurrentStateName;
        public string CurrentAttackName =>
            stateMachine.CurrentAttackName;
        public float HealthRatio =>
            stateMachine.HealthRatio;
        public bool IsPhaseChanging =>
            stateMachine.IsPhaseChanging;

        public DemonSwordsmanWorldUnit(
            float maxHealth,
            DemonSwordsmanStateMachine stateMachine)
            : base(maxHealth)
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

        public void OpenBranchWindow()
        {
            stateMachine.OpenBranchWindow();
        }

        public void SwapWeapon()
        {
            stateMachine.SwapWeapon();
        }

        public void FinishAction()
        {
            stateMachine.FinishAction();
        }

        public void TakeDamage(float damage)
        {
            Health.TakeDamage(damage);
        }
    }
}
