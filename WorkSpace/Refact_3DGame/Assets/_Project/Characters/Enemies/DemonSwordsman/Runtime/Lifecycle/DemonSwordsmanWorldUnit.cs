namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // EnemyUnit 생명주기와 보스 상태 머신을 연결한다.
    public sealed class DemonSwordsmanWorldUnit : EnemyUnit
    {
        private readonly DemonSwordsmanStateMachine stateMachine; // 현재 행동 상태

        public override bool IsBoss => true; // 기능 사용 여부
        public DemonSwordsmanPhase CurrentPhase => // 현재 페이즈
            stateMachine.CurrentPhase;
        public DemonSwordsmanStyle CurrentStyle => // 현재 자세
            stateMachine.CurrentStyle;
        public string CurrentStateName => // 현재 상태 이름
            stateMachine.CurrentStateName;
        public string CurrentAttackName => // 현재 공격 이름
            stateMachine.CurrentAttackName;
        public float HealthRatio => // 체력 비율
            stateMachine.HealthRatio;
        public bool IsPhaseChanging => // 기능 사용 여부
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
