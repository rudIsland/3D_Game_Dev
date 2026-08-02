namespace rudIsland.RPG3D.Characters
{
    // 풀에서 다시 나온 적은 새 적처럼 체력을 되돌린다.
    public abstract class EnemyUnit : Unit
    {
        public virtual bool IsBoss => false; // 기능 사용 여부

        protected EnemyUnit(float maxHealth)
            : base(UnitTeam.Enemy, maxHealth)
        {
        }

        protected sealed override void OnUnitEnable()
        {
            Health.Reset();
            OnEnemyEnable();
        }

        protected virtual void OnEnemyEnable()
        {
        }
    }
}
