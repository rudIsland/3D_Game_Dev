using UnityEngine;
using Characters.Combat.AttackData;

namespace Characters.Enemies.Zombie
{
    // 같은 Config를 쓰는 좀비가 공유한다. 곡선은 복사 후 외부에 노출하지 않는다.
    internal sealed class ZombieSettings
    {
        internal CharacterLifeSettings Life { get; }
        internal float DeadBodyKeepTime { get; }
        internal float ZoneTrackingMargin { get; }
        internal float HomeArrivalDistance { get; }
        internal float HomeRecoveryDelay { get; }
        internal float HealthRecoverySpeed { get; }
        internal float FindRange { get; }
        internal float IdleTargetCheckInterval { get; }
        internal float AttackRange { get; }
        internal float AttackFacingAngle { get; }
        internal LayerMask TargetLayers { get; }
        internal AttackDamage SwingAttackDamage { get; }
        internal AttackDamage KickAttackDamage { get; }
        internal AttackDamage UpDownAttackDamage { get; }
        internal float ChaseSpeed { get; }
        internal float TurnSpeed { get; }
        internal float Gravity { get; }
        internal float GroundPull { get; }
        internal float HitPushDuration { get; }
        internal float KnockbackPushDuration { get; }
        internal float FindRangeSquared { get; }
        internal float AttackRangeSquared { get; }
        internal float MinimumAttackFacingDot { get; }
        internal float HomeArrivalDistanceSquared { get; }
        private readonly AnimationCurve hitPushCurve;
        private readonly float hitPushCurveStart;
        private readonly float hitPushCurveRange;

        internal ZombieSettings(ZombieConfig source)
        {
            Life = new CharacterLifeSettings(source.MaxHealth, source.StaggerLimit,
                source.StaggerRecoverDelay, source.StaggerRecoverSpeed);
            DeadBodyKeepTime = source.DeadBodyKeepTime;
            ZoneTrackingMargin = source.ZoneTrackingMargin;
            HomeArrivalDistance = source.HomeArrivalDistance;
            HomeRecoveryDelay = source.HomeRecoveryDelay;
            HealthRecoverySpeed = source.HealthRecoverySpeed;
            FindRange = source.FindRange;
            IdleTargetCheckInterval = source.IdleTargetCheckInterval;
            AttackRange = source.AttackRange;
            AttackFacingAngle = source.AttackFacingAngle;
            TargetLayers = source.TargetLayers;
            SwingAttackDamage = CopyDamage(source.SwingAttackDamage);
            KickAttackDamage = CopyDamage(source.KickAttackDamage);
            UpDownAttackDamage = CopyDamage(source.UpDownAttackDamage);
            ChaseSpeed = source.ChaseSpeed;
            TurnSpeed = source.TurnSpeed;
            Gravity = source.Gravity;
            GroundPull = source.GroundPull;
            HitPushDuration = source.HitPushDuration;
            KnockbackPushDuration = source.KnockbackPushDuration;
            FindRangeSquared = FindRange * FindRange;
            AttackRangeSquared = AttackRange * AttackRange;
            MinimumAttackFacingDot = Mathf.Cos(AttackFacingAngle * Mathf.Deg2Rad);
            HomeArrivalDistanceSquared = HomeArrivalDistance * HomeArrivalDistance;
            hitPushCurve = new AnimationCurve(source.HitPushCurve.keys)
            {
                preWrapMode = source.HitPushCurve.preWrapMode,
                postWrapMode = source.HitPushCurve.postWrapMode
            };
            hitPushCurveStart = hitPushCurve.Evaluate(0f);
            hitPushCurveRange = hitPushCurve.Evaluate(1f) - hitPushCurveStart;
        }

        internal float EvaluateHitPushProgress(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            if (Mathf.Abs(hitPushCurveRange) <= 0.000001f)
                return normalizedTime;
            return Mathf.Clamp01((hitPushCurve.Evaluate(normalizedTime) - hitPushCurveStart) / hitPushCurveRange);
        }

        private static AttackDamage CopyDamage(AttackDamage source)
        {
            return source == null ? null : new AttackDamage(source.HealthDamage,
                source.Strength, source.StaggerDamage, source.PushDistance,
                source.GuardStaminaDamage, source.CanBlock, source.HitStopDuration,
                source.DamageSoundType);
        }
    }
}
