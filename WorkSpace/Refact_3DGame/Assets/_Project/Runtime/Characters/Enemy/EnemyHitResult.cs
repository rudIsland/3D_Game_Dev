using rudIsland.RPG3D.Characters.Combat;

namespace rudIsland.RPG3D.Characters
{
    // 체력 변화와 행동 반응을 서로 다른 값으로 반환한다.
    public readonly struct EnemyHitResult
    {
        public static EnemyHitResult Ignored =>
            new EnemyHitResult(HitDamageResult.Ignored, HitReaction.None);
        public static EnemyHitResult Damaged =>
            new EnemyHitResult(HitDamageResult.Damaged, HitReaction.None);
        public static EnemyHitResult Killed =>
            new EnemyHitResult(HitDamageResult.Killed, HitReaction.None);

        public HitDamageResult DamageResult { get; }
        public HitReaction Reaction { get; }
        public bool HasDamageFeedback =>
            DamageResult != HitDamageResult.Ignored;

        public EnemyHitResult(HitDamageResult damageResult, HitReaction reaction)
        {
            DamageResult = damageResult;
            Reaction = damageResult == HitDamageResult.Damaged
                ? reaction
                : HitReaction.None;
        }
    }
}
