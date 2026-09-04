using Characters.Combat.AttackData;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    // EnemyAttackData Asset에서 복사한 런타임 읽기 전용 공격 설정이다.
    internal readonly struct NightShadeSwordRuntimeAttackData
    {
        private readonly AttackDamage firstHitDamage;
        private readonly AttackDamage secondHitDamage;

        internal NightShadeSwordActionId ActionId { get; }
        internal float PostAttackDelay { get; }
        internal NightShadeSwordAttackScoreSettings Score { get; }
        internal float ComboFirstExitNormalizedTime { get; }
        internal float ComboSecondDelay { get; }
        internal float MoveDistance { get; }
        internal AnimationCurve MovementCurve { get; }
        internal float TargetStopDistance { get; }
        internal float MaximumAddedMoveDistance { get; }
        internal float MaximumTurnAngle { get; }
        internal NightShadeSwordRuntimeAttackData(
            NightShadeSwordAttackData source)
        {
            source.Validate();
            ActionId = source.ActionId;
            PostAttackDelay = source.PostAttackDelay;
            Score = new NightShadeSwordAttackScoreSettings(
                source.Utility.BaseScore,
                source.Utility.PreferredDistance,
                source.Utility.DistanceTolerance);
            ComboFirstExitNormalizedTime =
                source.ComboFirstExitNormalizedTime;
            ComboSecondDelay = source.ComboSecondDelay;
            MoveDistance = source.MoveDistance;
            MovementCurve = CloneCurve(source.MovementCurve);
            TargetStopDistance = source.TargetStopDistance;
            MaximumAddedMoveDistance = source.MaximumAddedMoveDistance;
            MaximumTurnAngle = source.MaximumTurnAngle;
            firstHitDamage = CloneDamage(source.GetHitDamage(0));
            secondHitDamage = source.ActionId == NightShadeSwordActionId.Combo
                ? CloneDamage(source.GetHitDamage(1))
                : null;
        }

        internal AttackDamage GetHitDamage(int hitIndex)
        {
            return hitIndex == 1 && secondHitDamage != null
                ? secondHitDamage
                : firstHitDamage;
        }

        private static AttackDamage CloneDamage(AttackDamage source)
        {
            if (source == null)
            {
                return new AttackDamage();
            }

            return new AttackDamage(
                source.HealthDamage,
                source.Strength,
                source.StaggerDamage,
                source.PushDistance,
                source.GuardStaminaDamage,
                source.CanBlock,
                source.HitStopDuration,
                source.DamageSoundType);
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            if (source == null)
            {
                return null;
            }

            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }
    }
}
