namespace rudIsland.RPG3D.Characters
{
    // 적 유닛의 공통 타입과 재활성화 시 체력 초기화를 제공한다.
    public abstract class EnemyUnit : Unit
    {
        // 보스 여부를 알려준다.
        public virtual bool IsBoss => false;

        // 적의 최대 체력으로 기본 유닛을 만든다.
        protected EnemyUnit(float maxHealth)
            : base(maxHealth)
        {
        }

        // 풀에서 다시 사용될 때 체력을 최대치로 되돌린다.
        protected override void OnUnitResourceEnable()
        {
            Health.Reset();
        }

        // 적 전용 활성화 작업을 호출한다.
        protected sealed override void OnUnitEnable() => OnEnemyEnable();

        // 자식 적이 활성화될 때 필요한 작업을 작성하는 지점이다.
        protected virtual void OnEnemyEnable()
        {
        }
    }
}