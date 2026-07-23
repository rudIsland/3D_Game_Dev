public interface IDamageable
{
    // 방어력을 외부(계산기)에서 알 수 있도록 추가
    float defense { get; }

    void TakeDamage(DamageInfo damageInfo);
}