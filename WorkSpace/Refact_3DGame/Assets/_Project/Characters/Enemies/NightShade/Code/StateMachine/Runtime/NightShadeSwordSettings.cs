using System;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Enemies.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // Inspector Config를 역할별로 복사한 NightShade 런타임 설정이다.
    internal sealed class NightShadeSwordSettings
    {
        internal NightShadeSwordLifeRuntimeConfig Life { get; }
        internal NightShadeSwordTargetRuntimeConfig CombatRange { get; }
        internal NightShadeSwordAttackSelectionRuntimeConfig AttackSelection { get; }
        internal NightShadeSwordMovementRuntimeConfig Movement { get; }
        internal NightShadeSwordRecoveryRuntimeConfig Recovery { get; }
        internal NightShadeSwordHitReactionRuntimeConfig HitReaction { get; }
        private readonly NightShadeSwordAttacksRuntimeConfig attacks;

        internal NightShadeSwordSettings(
            NightShadeSwordLifeSettings life,
            NightShadeSwordCombatRangeSettings combatRange,
            NightShadeSwordAttackSelectionSettings attackSelection,
            NightShadeSwordMovementSettings movement,
            EnemyAttackData[] attacks,
            NightShadeSwordRecoverySettings recovery,
            NightShadeSwordHitReactionSettings hitReaction)
        {
            life.Validate();
            combatRange.Validate();
            attackSelection.Validate();
            movement.Validate();
            recovery.Validate();
            hitReaction.Validate();

            Life = new NightShadeSwordLifeRuntimeConfig(life);
            CombatRange = new NightShadeSwordTargetRuntimeConfig(combatRange);
            Movement = new NightShadeSwordMovementRuntimeConfig(movement);
            AttackSelection = new NightShadeSwordAttackSelectionRuntimeConfig(
                attackSelection);
            Recovery = new NightShadeSwordRecoveryRuntimeConfig(recovery);
            HitReaction = new NightShadeSwordHitReactionRuntimeConfig(
                hitReaction);
            this.attacks = new NightShadeSwordAttacksRuntimeConfig(attacks);
        }

        internal NightShadeSwordRuntimeAttackData GetAttackData(
            NightShadeSwordActionId actionId)
        {
            return attacks.Get(actionId);
        }
    }

    internal sealed class NightShadeSwordLifeRuntimeConfig
    {
        internal float MaxHealth { get; }
        internal float StaggerLimit { get; }
        internal float StaggerRecoverDelay { get; }
        internal float StaggerRecoverSpeed { get; }
        internal float DeadBodyKeepTime { get; }

        internal NightShadeSwordLifeRuntimeConfig(
            NightShadeSwordLifeSettings source)
        {
            MaxHealth = source.MaxHealth;
            StaggerLimit = source.StaggerLimit;
            StaggerRecoverDelay = source.StaggerRecoverDelay;
            StaggerRecoverSpeed = source.StaggerRecoverSpeed;
            DeadBodyKeepTime = source.DeadBodyKeepTime;
        }
    }

    internal sealed class NightShadeSwordTargetRuntimeConfig
    {
        internal LayerMask TargetLayers { get; }
        internal float FindRangeSquared { get; }
        internal float AttackRange { get; }
        internal float AttackRangeSquared { get; }
        internal float WalkStartRangeSquared { get; }
        internal float RunStartRangeSquared { get; }
        internal float AttackFacingDot { get; }

        internal NightShadeSwordTargetRuntimeConfig(
            NightShadeSwordCombatRangeSettings source)
        {
            float findRange = source.FindRange;
            float attackRange = source.AttackRange;
            float walkStartRange = Mathf.Clamp(
                source.WalkStartRange,
                attackRange,
                findRange);
            float runStartRange = Mathf.Clamp(
                source.RunStartRange,
                walkStartRange,
                findRange);

            TargetLayers = source.TargetLayers;
            FindRangeSquared = findRange * findRange;
            AttackRange = attackRange;
            AttackRangeSquared = attackRange * attackRange;
            WalkStartRangeSquared = walkStartRange * walkStartRange;
            RunStartRangeSquared = runStartRange * runStartRange;
            AttackFacingDot = Mathf.Cos(
                source.AttackFacingAngle * Mathf.Deg2Rad);
        }
    }

    internal sealed class NightShadeSwordMovementRuntimeConfig
    {
        internal float WalkSpeed { get; }
        internal float ChaseSpeed { get; }
        internal float TurnSpeed { get; }
        internal float AttackTurnSpeed { get; }
        internal float Gravity { get; }
        internal float GroundPull { get; }

        internal NightShadeSwordMovementRuntimeConfig(
            NightShadeSwordMovementSettings source)
        {
            WalkSpeed = source.WalkSpeed;
            ChaseSpeed = source.ChaseSpeed;
            TurnSpeed = source.TurnSpeed;
            AttackTurnSpeed = source.AttackTurnSpeed;
            Gravity = source.Gravity;
            GroundPull = source.GroundPull;
        }
    }

    internal sealed class NightShadeSwordAttackSelectionRuntimeConfig
    {
        internal float DistanceScoreWeight { get; }
        internal float RepeatPenalty { get; }
        internal float RandomBonusMax { get; }

        internal NightShadeSwordAttackSelectionRuntimeConfig(
            NightShadeSwordAttackSelectionSettings source)
        {
            DistanceScoreWeight = source.DistanceScoreWeight;
            RepeatPenalty = source.RepeatPenalty;
            RandomBonusMax = source.RandomBonusMax;
        }
    }

    internal sealed class NightShadeSwordRecoveryRuntimeConfig
    {
        internal float MoveSpeed { get; }
        internal float MoveDuration { get; }
        internal float IdleBaseScore { get; }
        internal float IdleDistanceWeight { get; }
        internal float BackBaseScore { get; }
        internal float BackCloseWeight { get; }
        internal float SideBaseScore { get; }
        internal float SideDistanceWeight { get; }
        internal float RepeatPenalty { get; }
        internal float RandomBonusMax { get; }

        internal NightShadeSwordRecoveryRuntimeConfig(
            NightShadeSwordRecoverySettings source)
        {
            MoveSpeed = source.MoveSpeed;
            MoveDuration = source.MoveDuration;
            IdleBaseScore = source.IdleBaseScore;
            IdleDistanceWeight = source.IdleDistanceWeight;
            BackBaseScore = source.BackBaseScore;
            BackCloseWeight = source.BackCloseWeight;
            SideBaseScore = source.SideBaseScore;
            SideDistanceWeight = source.SideDistanceWeight;
            RepeatPenalty = source.RepeatPenalty;
            RandomBonusMax = source.RandomBonusMax;
        }
    }

    internal sealed class NightShadeSwordHitReactionRuntimeConfig
    {
        internal float PushDuration { get; }
        internal float KnockbackPushDuration { get; }
        internal float KnockdownPushDuration { get; }
        internal float KnockdownStayDuration { get; }
        internal float StaggerBreakStayDuration { get; }

        private readonly AnimationCurve pushCurve;
        internal AnimationCurve PushCurve => pushCurve;

        internal NightShadeSwordHitReactionRuntimeConfig(
            NightShadeSwordHitReactionSettings source)
        {
            PushDuration = source.PushDuration;
            KnockbackPushDuration = source.KnockbackPushDuration;
            KnockdownPushDuration = source.KnockdownPushDuration;
            KnockdownStayDuration = source.KnockdownStayDuration;
            StaggerBreakStayDuration = source.StaggerBreakStayDuration;
            pushCurve = CloneCurve(source.PushCurve);
        }

        internal float GetPushDuration(HitReaction reaction)
        {
            switch (reaction)
            {
                case HitReaction.Knockdown:
                    return KnockdownPushDuration;
                case HitReaction.Knockback:
                    return KnockbackPushDuration;
                default:
                    return PushDuration;
            }
        }

        internal float EvaluatePushProgress(float normalizedTime)
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);
            return Mathf.Clamp01(pushCurve != null && pushCurve.length > 0
                ? pushCurve.Evaluate(clampedTime)
                : clampedTime);
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

    internal sealed class NightShadeSwordAttacksRuntimeConfig
    {
        private readonly NightShadeSwordRuntimeAttackData[] attacks;

        internal NightShadeSwordAttacksRuntimeConfig(
            EnemyAttackData[] attackData)
        {
            attacks = CreateRuntimeAttacks(attackData);
        }

        internal NightShadeSwordRuntimeAttackData Get(
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
            if (attackData == null || attackData.Length != requiredAttackCount)
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
    }
}
