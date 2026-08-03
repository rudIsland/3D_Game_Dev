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

        protected EnemyUnit(
            float maxHealth,
            float staggerLimit,
            float staggerRecoverDelay,
            float staggerRecoverSpeed,
            float maxStamina,
            float staminaRecoverDelay,
            float staminaRecoverSpeed,
            float guardAngle)
            : base(
                UnitTeam.Enemy,
                maxHealth,
                staggerLimit,
                staggerRecoverDelay,
                staggerRecoverSpeed,
                maxStamina,
                staminaRecoverDelay,
                staminaRecoverSpeed,
                guardAngle)
        {
        }

        protected override void OnUnitResourceEnable()
        {
            Health.Reset();
            Stamina.Reset();
        }

        protected sealed override void OnUnitEnable() => OnEnemyEnable();

        protected virtual void OnEnemyEnable()
        {
        }
    }
}
