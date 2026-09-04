namespace Characters.Combat
{
    // 체력 변화만 적용하고 무시, 피해, 사망 결과를 반환한다.
    internal static class HitDamageCalculator
    {
        internal static HitDamageResult Apply(
            UnitHealth health,
            float damage)
        {
            if (health == null || health.IsDead)
            {
                return HitDamageResult.Ignored;
            }

            float healthBeforeDamage = health.CurrentHealth;
            health.TakeDamage(damage);
            if (health.CurrentHealth >= healthBeforeDamage)
            {
                return HitDamageResult.Ignored;
            }

            return health.IsDead
                ? HitDamageResult.Killed
                : HitDamageResult.Damaged;
        }
    }
}
