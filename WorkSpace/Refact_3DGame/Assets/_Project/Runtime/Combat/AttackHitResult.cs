namespace rudIsland.RPG3D.Combat
{
    // 계산된 피격 결과와 실제 반영 수치를 함께 보관한다.
    public readonly struct AttackHitResult
    {
        public static AttackHitResult Ignored =>
            new AttackHitResult(
                AttackHitResultType.Ignored,
                0f,
                0f,
                0f,
                default,
                0f);

        public static AttackHitResult Dodged =>
            CreateWithoutDamage(AttackHitResultType.Dodged);

        public static AttackHitResult Parried =>
            CreateWithoutDamage(AttackHitResultType.Parried);

        public static AttackHitResult Guarded =>
            CreateWithoutDamage(AttackHitResultType.Guarded);

        public static AttackHitResult Damaged =>
            CreateWithoutDamage(AttackHitResultType.Damaged);

        public static AttackHitResult Staggered =>
            CreateWithoutDamage(AttackHitResultType.Staggered);

        public static AttackHitResult Killed =>
            CreateWithoutDamage(AttackHitResultType.Killed);

        public AttackHitResultType Type { get; }
        public float HealthDamage { get; }
        public float StaminaDamage { get; }
        public float StaggerDamage { get; }
        public HitReaction Reaction { get; }
        public float HitStopTime { get; }

        public bool StopsDefenderAction =>
            Type == AttackHitResultType.Staggered ||
            Type == AttackHitResultType.GuardBroken ||
            Type == AttackHitResultType.KnockedDown ||
            Type == AttackHitResultType.Killed;

        public AttackHitResult(
            AttackHitResultType type,
            float healthDamage,
            float staminaDamage,
            float staggerDamage,
            HitReaction reaction,
            float hitStopTime)
        {
            Type = type;
            HealthDamage = SanitizeNonNegative(healthDamage);
            StaminaDamage = SanitizeNonNegative(staminaDamage);
            StaggerDamage = SanitizeNonNegative(staggerDamage);
            Reaction = reaction;
            HitStopTime = SanitizeNonNegative(hitStopTime);
        }

        private static AttackHitResult CreateWithoutDamage(
            AttackHitResultType type)
        {
            return new AttackHitResult(
                type,
                0f,
                0f,
                0f,
                default,
                0f);
        }

        private static float SanitizeNonNegative(float value)
        {
            return value > 0f &&
                !float.IsNaN(value) &&
                !float.IsInfinity(value)
                    ? value
                    : 0f;
        }
    }
}
