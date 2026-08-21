// Config 값을 복사해 런타임 로직에 전달하는 불변 스냅샷이다.
using System;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Characters.Enemies.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // Inspector에서 받은 전투 수치를 생성 시 한 번 계산해 보관한다.
    internal sealed class NightShadeSwordSettings
    {
        internal LayerMask TargetLayers { get; }
        internal float MaxHealth { get; }
        internal float StaggerLimit { get; }
        internal float StaggerRecoverDelay { get; }
        internal float StaggerRecoverSpeed { get; }
        internal float FindRangeSquared { get; }
        internal float AttackRange { get; }
        internal float AttackRangeSquared { get; }
        internal float WalkStartRangeSquared { get; }
        internal float RunStartRangeSquared { get; }
        internal float AttackFacingDot { get; }
        internal float WalkSpeed { get; }
        internal float ChaseSpeed { get; }
        internal float TurnSpeed { get; }
        internal float AttackTurnSpeed { get; }
        internal float Gravity { get; }
        internal float GroundPull { get; }
        internal float RecoveryMoveSpeed { get; }
        internal float RecoveryMoveDuration { get; }
        internal float AttackDistanceScoreWeight { get; }
        internal float AttackRepeatPenalty { get; }
        internal float AttackRandomBonusMax { get; }
        internal float IdleRecoveryBaseScore { get; }
        internal float IdleRecoveryDistanceWeight { get; }
        internal float BackRecoveryBaseScore { get; }
        internal float BackRecoveryCloseWeight { get; }
        internal float SideRecoveryBaseScore { get; }
        internal float SideRecoveryDistanceWeight { get; }
        internal float RecoveryRepeatPenalty { get; }
        internal float RecoveryRandomBonusMax { get; }
        internal float HitPushDuration { get; }
        internal float KnockbackPushDuration { get; }
        internal float KnockdownPushDuration { get; }
        internal float KnockdownStayDuration { get; }
        internal float StaggerBreakStayDuration { get; }
        internal AnimationCurve HitPushCurve { get; }
        internal float DeadBodyKeepTime { get; }
        internal float ComboFirstExitNormalizedTime { get; }
        internal float ComboSecondDelay { get; }

        private readonly NightShadeSwordRuntimeAttackData[] attacks;

        internal NightShadeSwordSettings(
            NightShadeSwordLifeSettings life,
            NightShadeSwordCombatRangeSettings combatRange,
            NightShadeSwordAttackSelectionSettings attackSelection,
            NightShadeSwordMovementSettings movement,
            EnemyAttackData[] attackData,
            NightShadeSwordRecoverySettings recovery,
            NightShadeSwordHitReactionSettings hitReaction)
        {
            life.Validate();
            combatRange.Validate();
            attackSelection.Validate();
            movement.Validate();
            recovery.Validate();
            hitReaction.Validate();
            attacks = CreateRuntimeAttacks(attackData);

            TargetLayers = combatRange.TargetLayers;
            MaxHealth = life.MaxHealth;
            StaggerLimit = life.StaggerLimit;
            StaggerRecoverDelay = life.StaggerRecoverDelay;
            StaggerRecoverSpeed = life.StaggerRecoverSpeed;

            float safeFindRange = combatRange.FindRange;
            float safeAttackRange = combatRange.AttackRange;
            float safeWalkStartRange = Mathf.Clamp(
                combatRange.WalkStartRange,
                safeAttackRange,
                safeFindRange);
            float safeRunStartRange = Mathf.Clamp(
                combatRange.RunStartRange,
                safeWalkStartRange,
                safeFindRange);

            FindRangeSquared = safeFindRange * safeFindRange;
            AttackRange = safeAttackRange;
            AttackRangeSquared = safeAttackRange * safeAttackRange;
            WalkStartRangeSquared = safeWalkStartRange * safeWalkStartRange;
            RunStartRangeSquared = safeRunStartRange * safeRunStartRange;
            AttackFacingDot = Mathf.Cos(
                combatRange.AttackFacingAngle * Mathf.Deg2Rad);
            WalkSpeed = movement.WalkSpeed;
            ChaseSpeed = movement.ChaseSpeed;
            TurnSpeed = movement.TurnSpeed;
            AttackTurnSpeed = movement.AttackTurnSpeed;
            Gravity = movement.Gravity;
            GroundPull = movement.GroundPull;

            NightShadeSwordRuntimeAttackData comboAttack =
                GetAttackSettings(NightShadeSwordActionId.Combo);
            ComboFirstExitNormalizedTime =
                comboAttack.ComboFirstExitNormalizedTime;
            ComboSecondDelay = comboAttack.ComboSecondDelay;
            AttackDistanceScoreWeight =
                attackSelection.DistanceScoreWeight;
            AttackRepeatPenalty = attackSelection.RepeatPenalty;
            AttackRandomBonusMax = attackSelection.RandomBonusMax;

            RecoveryMoveSpeed = recovery.MoveSpeed;
            RecoveryMoveDuration = recovery.MoveDuration;
            IdleRecoveryBaseScore = recovery.IdleBaseScore;
            IdleRecoveryDistanceWeight = recovery.IdleDistanceWeight;
            BackRecoveryBaseScore = recovery.BackBaseScore;
            BackRecoveryCloseWeight = recovery.BackCloseWeight;
            SideRecoveryBaseScore = recovery.SideBaseScore;
            SideRecoveryDistanceWeight = recovery.SideDistanceWeight;
            RecoveryRepeatPenalty = recovery.RepeatPenalty;
            RecoveryRandomBonusMax = recovery.RandomBonusMax;

            HitPushDuration = hitReaction.PushDuration;
            KnockbackPushDuration = hitReaction.KnockbackPushDuration;
            KnockdownPushDuration = hitReaction.KnockdownPushDuration;
            KnockdownStayDuration = hitReaction.KnockdownStayDuration;
            StaggerBreakStayDuration =
                hitReaction.StaggerBreakStayDuration;
            HitPushCurve = CloneCurve(hitReaction.PushCurve);
            DeadBodyKeepTime = life.DeadBodyKeepTime;

        }

        internal AttackDamage GetAttackDamage(
            NightShadeSwordAttackType attackType)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.ComboFirst:
                    return GetAttackSettings(
                        NightShadeSwordActionId.Combo).GetHitDamage(0);
                case NightShadeSwordAttackType.ComboSecond:
                    return GetAttackSettings(
                        NightShadeSwordActionId.Combo).GetHitDamage(1);
                case NightShadeSwordAttackType.Heavy:
                    return GetAttackSettings(
                        NightShadeSwordActionId.Heavy).GetHitDamage(0);
                case NightShadeSwordAttackType.WideSwing:
                    return GetAttackSettings(
                        NightShadeSwordActionId.WideSwing).GetHitDamage(0);
                default:
                    return GetAttackSettings(
                        NightShadeSwordActionId.Light).GetHitDamage(0);
            }
        }

        internal NightShadeSwordAttackScoreSettings GetAttackScoreSettings(
            NightShadeSwordActionId actionId)
        {
            return GetAttackSettings(actionId).Score;
        }

        internal float GetPostAttackDelay(NightShadeSwordActionId actionId)
        {
            return GetAttackSettings(actionId).PostAttackDelay;
        }

        internal float EvaluateHitPushProgress(float normalizedTime)
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);
            return Mathf.Clamp01(HitPushCurve != null && HitPushCurve.length > 0
                ? HitPushCurve.Evaluate(clampedTime)
                : clampedTime);
        }

        internal float GetHitPushDuration(HitReaction reaction)
        {
            switch (reaction)
            {
                case HitReaction.Knockdown:
                    return KnockdownPushDuration;
                case HitReaction.Knockback:
                    return KnockbackPushDuration;
                default:
                    return HitPushDuration;
            }
        }

        private NightShadeSwordRuntimeAttackData GetAttackSettings(
            NightShadeSwordActionId actionId)
        {
            int index = (int)actionId - (int)NightShadeSwordActionId.Light;
            if (index < 0 || index >= attacks.Length)
            {
                index = 0;
            }

            return attacks[index];
        }

        private static NightShadeSwordRuntimeAttackData[] CreateRuntimeAttacks(
            EnemyAttackData[] attackData)
        {
            const int requiredAttackCount = 4;
            if (attackData == null ||
                attackData.Length != requiredAttackCount)
            {
                throw new ArgumentException(
                    "NightShadeSword 공격 데이터는 Light, Combo, Heavy, WideSwing 4개가 필요합니다.",
                    nameof(attackData));
            }

            var runtimeAttacks =
                new NightShadeSwordRuntimeAttackData[requiredAttackCount];
            var hasAttack = new bool[requiredAttackCount];
            for (int sourceIndex = 0;
                sourceIndex < attackData.Length;
                sourceIndex++)
            {
                if (attackData[sourceIndex] is not
                    NightShadeSwordAttackData swordAttack)
                {
                    throw new ArgumentException(
                        $"attackData[{sourceIndex}]에 NightShadeSwordAttackData가 필요합니다.",
                        nameof(attackData));
                }

                swordAttack.Validate();
                int targetIndex =
                    (int)swordAttack.ActionId -
                    (int)NightShadeSwordActionId.Light;
                if (targetIndex < 0 ||
                    targetIndex >= requiredAttackCount ||
                    hasAttack[targetIndex])
                {
                    throw new ArgumentException(
                        $"{swordAttack.ActionId} 공격 데이터가 중복되었거나 올바르지 않습니다.",
                        nameof(attackData));
                }

                runtimeAttacks[targetIndex] =
                    new NightShadeSwordRuntimeAttackData(swordAttack);
                hasAttack[targetIndex] = true;
            }

            for (int index = 0; index < hasAttack.Length; index++)
            {
                if (!hasAttack[index])
                {
                    throw new ArgumentException(
                        "NightShadeSword 공격 데이터에 빠진 공격이 있습니다.",
                        nameof(attackData));
                }
            }

            return runtimeAttacks;
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            var curve = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return curve;
        }
    }
}
