using Core;

namespace Core
{
    // 체력 변화와 행동 반응을 서로 다른 값으로 반환한다.
    public readonly struct EnemyHitResult
    {
        // 반응이 없는 고정 결과는 공통 값으로 보관한다. 피해 반응이 있으면 생성자로 조합한다.
        public static readonly EnemyHitResult Ignored =
            new EnemyHitResult(HitDamageResult.Ignored, HitReaction.None);
        public static readonly EnemyHitResult Killed =
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
