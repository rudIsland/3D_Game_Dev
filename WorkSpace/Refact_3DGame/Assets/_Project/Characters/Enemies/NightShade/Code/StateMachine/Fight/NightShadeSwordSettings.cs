using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // Inspector에서 받은 전투 수치를 생성 시 한 번 계산해 보관한다.
    internal sealed class NightShadeSwordSettings
    {
        internal float FindRangeSquared { get; }
        internal float AttackRangeSquared { get; }
        internal float WalkStartRangeSquared { get; }
        internal float RunStartRangeSquared { get; }
        internal float AttackFacingDot { get; }
        internal float WalkSpeed { get; }
        internal float ChaseSpeed { get; }
        internal float TurnSpeed { get; }
        internal float AttackTurnSpeed { get; }
        internal float CombatMoveSpeed { get; }
        internal float CombatMoveDuration { get; }
        internal int AttacksBeforeCombatMove { get; }
        internal float HitPushDuration { get; }
        internal AnimationCurve HitPushCurve { get; }
        internal float DeadBodyKeepTime { get; }
        internal float ComboFirstExitNormalizedTime { get; }
        internal float ComboSecondDelay { get; }

        private readonly float lightAttackRecovery;
        private readonly float comboAttackRecovery;
        private readonly float wideSwingAttackRecovery;
        private readonly float heavyAttackRecovery;

        internal NightShadeSwordSettings(
            float findRange,
            float attackRange,
            float walkStartRange,
            float runStartRange,
            float attackFacingAngle,
            float walkSpeed,
            float chaseSpeed,
            float turnSpeed,
            float attackTurnSpeed,
            float lightAttackRecovery,
            float comboAttackRecovery,
            float comboFirstExitNormalizedTime,
            float comboSecondDelay,
            float wideSwingAttackRecovery,
            float heavyAttackRecovery,
            float combatMoveSpeed,
            float combatMoveDuration,
            int attacksBeforeCombatMove,
            float hitPushDuration,
            AnimationCurve hitPushCurve,
            float deadBodyKeepTime)
        {
            float safeFindRange = Mathf.Max(0.1f, findRange);
            float safeAttackRange = Mathf.Clamp(attackRange, 0.1f, safeFindRange);
            float safeWalkStartRange = Mathf.Clamp(
                walkStartRange,
                safeAttackRange,
                safeFindRange);
            float safeRunStartRange = Mathf.Clamp(
                runStartRange,
                safeWalkStartRange,
                safeFindRange);

            FindRangeSquared = safeFindRange * safeFindRange;
            AttackRangeSquared = safeAttackRange * safeAttackRange;
            WalkStartRangeSquared = safeWalkStartRange * safeWalkStartRange;
            RunStartRangeSquared = safeRunStartRange * safeRunStartRange;
            AttackFacingDot = Mathf.Cos(Mathf.Clamp(attackFacingAngle, 0f, 180f) * Mathf.Deg2Rad);
            WalkSpeed = Mathf.Max(0.1f, walkSpeed);
            ChaseSpeed = Mathf.Max(0.1f, chaseSpeed);
            TurnSpeed = turnSpeed;
            AttackTurnSpeed = attackTurnSpeed;
            this.lightAttackRecovery =
                Mathf.Max(0f, lightAttackRecovery);
            this.comboAttackRecovery =
                Mathf.Max(0f, comboAttackRecovery);
            ComboFirstExitNormalizedTime = Mathf.Clamp(
                comboFirstExitNormalizedTime,
                0.35f,
                1f);
            ComboSecondDelay = Mathf.Max(0f, comboSecondDelay);
            this.wideSwingAttackRecovery =
                Mathf.Max(0f, wideSwingAttackRecovery);
            this.heavyAttackRecovery =
                Mathf.Max(0f, heavyAttackRecovery);
            CombatMoveSpeed = Mathf.Max(0.1f, combatMoveSpeed);
            CombatMoveDuration = Mathf.Max(0.1f, combatMoveDuration);
            AttacksBeforeCombatMove =
                Mathf.Max(1, attacksBeforeCombatMove);
            HitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            HitPushCurve = hitPushCurve;
            DeadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
        }

        internal bool IsVeryClose(float distanceSquared)
        {
            return distanceSquared <= AttackRangeSquared * 0.36f;
        }

        internal NightShadeSwordStateId GetApproachState(float distanceSquared)
        {
            return distanceSquared < RunStartRangeSquared
                ? NightShadeSwordStateId.Walk
                : NightShadeSwordStateId.Chase;
        }

        internal float GetAttackRecovery(NightShadeSwordAttackType attackType)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.ComboFirst:
                case NightShadeSwordAttackType.ComboSecond:
                    return comboAttackRecovery;
                case NightShadeSwordAttackType.WideSwing:
                    return wideSwingAttackRecovery;
                case NightShadeSwordAttackType.Heavy:
                    return heavyAttackRecovery;
                default:
                    return lightAttackRecovery;
            }
        }

        internal float EvaluateHitPushProgress(float normalizedTime)
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);
            return Mathf.Clamp01(HitPushCurve != null && HitPushCurve.length > 0
                ? HitPushCurve.Evaluate(clampedTime)
                : clampedTime);
        }
    }
}
