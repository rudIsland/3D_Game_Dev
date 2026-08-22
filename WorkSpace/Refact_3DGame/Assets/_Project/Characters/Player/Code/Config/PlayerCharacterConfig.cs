using System;
using rudIsland.RPG3D.Player.States.Attack;
using UnityEngine;

namespace rudIsland.RPG3D.Player.Config
{
    [CreateAssetMenu(
        fileName = "PlayerCharacterConfig",
        menuName = "rudIsland/RPG3D/Player/Character Config")]
    // 플레이어의 밸런스 값과 공격 자산 참조를 한곳에 보관한다.
    public sealed class PlayerCharacterConfig : ScriptableObject
    {
        [SerializeField] private PlayerMovementSettings movement = new();
        [SerializeField] private PlayerCombatSettings combat = new();
        [SerializeField] private PlayerTargetSettings target = new();
        [SerializeField] private PlayerAttackSettings attacks = new();

        internal PlayerCharacterRuntimeConfig CreateRuntimeConfig()
        {
            ValidateSettings();
            return new PlayerCharacterRuntimeConfig(
                movement,
                combat,
                target,
                attacks);
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void ValidateSettings()
        {
            movement ??= new PlayerMovementSettings();
            combat ??= new PlayerCombatSettings();
            target ??= new PlayerTargetSettings();
            attacks ??= new PlayerAttackSettings();
            movement.Validate();
            combat.Validate();
            target.Validate();
            attacks.Validate();
        }
    }

    [Serializable]
    internal sealed class PlayerMovementSettings
    {
        [Header("회전")]
        [SerializeField, Min(0f)] private float freeMoveTurnSpeed = 720f;
        [SerializeField, Min(0f)] private float targetMoveTurnSpeed = 540f;
        [SerializeField, Min(0f)] private float attackTurnSpeed = 360f;

        [Header("이동")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.8f;
        [SerializeField, Min(0f)] private float guardMoveSpeed = 1.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 5.5f;
        [SerializeField, Min(0f)] private float moveAcceleration = 30f;
        [SerializeField, Min(0f)] private float moveDeceleration = 40f;
        [SerializeField, Min(0f)] private float animationSmoothTime = 0.06f;

        [Header("구르기")]
        [SerializeField, Min(0f)] private float rollDistance = 2.2f;
        [SerializeField, Min(0f)] private float sprintRollDistance = 2.5f;
        [SerializeField, Range(0.01f, 1f)]
        private float rollCompleteNormalizedTime = 0.7f;
        [SerializeField] private AnimationCurve rollMovementCurve =
            CreateDefaultRollMovementCurve();

        [Header("중력")]
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundPull = -2f;

        internal float FreeMoveTurnSpeed => freeMoveTurnSpeed;
        internal float TargetMoveTurnSpeed => targetMoveTurnSpeed;
        internal float AttackTurnSpeed => attackTurnSpeed;
        internal float WalkSpeed => walkSpeed;
        internal float GuardMoveSpeed => guardMoveSpeed;
        internal float SprintSpeed => sprintSpeed;
        internal float MoveAcceleration => moveAcceleration;
        internal float MoveDeceleration => moveDeceleration;
        internal float AnimationSmoothTime => animationSmoothTime;
        internal float RollDistance => rollDistance;
        internal float SprintRollDistance => sprintRollDistance;
        internal float RollCompleteNormalizedTime => rollCompleteNormalizedTime;
        internal AnimationCurve RollMovementCurve => rollMovementCurve;
        internal float Gravity => gravity;
        internal float GroundPull => groundPull;

        internal void Validate()
        {
            freeMoveTurnSpeed = Mathf.Max(0f, freeMoveTurnSpeed);
            targetMoveTurnSpeed = Mathf.Max(0f, targetMoveTurnSpeed);
            attackTurnSpeed = Mathf.Max(0f, attackTurnSpeed);
            walkSpeed = Mathf.Max(0f, walkSpeed);
            guardMoveSpeed = Mathf.Clamp(guardMoveSpeed, 0f, walkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            moveAcceleration = Mathf.Max(0f, moveAcceleration);
            moveDeceleration = Mathf.Max(0f, moveDeceleration);
            animationSmoothTime = Mathf.Max(0f, animationSmoothTime);
            rollDistance = Mathf.Max(0f, rollDistance);
            sprintRollDistance = Mathf.Max(0f, sprintRollDistance);
            rollCompleteNormalizedTime = Mathf.Clamp(
                rollCompleteNormalizedTime,
                0.01f,
                1f);
            if (rollMovementCurve == null || rollMovementCurve.length < 2)
            {
                rollMovementCurve = CreateDefaultRollMovementCurve();
            }
        }

        private static AnimationCurve CreateDefaultRollMovementCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 3f),
                new Keyframe(0.15f, 0.45f, 1.2f, 1.2f),
                new Keyframe(0.35f, 0.75f, 0.6f, 0.6f),
                new Keyframe(0.7f, 0.95f, 0.1f, 0.1f),
                new Keyframe(1f, 1f, 0f, 0f));
            curve.preWrapMode = WrapMode.Clamp;
            curve.postWrapMode = WrapMode.Clamp;
            return curve;
        }
    }

    [Serializable]
    internal sealed class PlayerCombatSettings
    {
        [Header("생명과 스태미나")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(1f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float staminaRecoverDelay = 0.8f;
        [SerializeField, Min(0f)] private float staminaRecoverSpeed = 35f;
        [SerializeField, Range(0f, 1f)]
        private float guardStaminaRecoveryRate;
        [SerializeField, Min(0f)] private float rollStaminaCost = 25f;
        [SerializeField, Min(0f)]
        private float sprintStaminaCostPerSecond = 15f;
        [SerializeField, Min(0f)] private float sprintRestartStamina = 20f;

        [Header("입력과 방어")]
        [SerializeField, Min(0f)] private float actionInputBufferDuration = 0.2f;
        [SerializeField, Range(0f, 180f)] private float guardAngle = 120f;
        [SerializeField, Min(0f)] private float guardRaiseDuration = 0.05f;
        [SerializeField, Min(0f)]
        private float guardBreakControlLockDuration = 1f;

        [Header("피격과 행동 중단")]
        [SerializeField, Min(0.01f)] private float hitPushDuration = 0.15f;
        [SerializeField] private AnimationCurve hitPushCurve =
            CreateDefaultHitPushCurve();
        [SerializeField, Min(1f)] private float stopPointLimit = 30f;
        [SerializeField, Min(0f)] private float stopPointRecoverDelay = 1.5f;
        [SerializeField, Min(0f)] private float stopPointRecoverSpeed = 15f;

        internal float MaxHealth => maxHealth;
        internal float MaxStamina => maxStamina;
        internal float StaminaRecoverDelay => staminaRecoverDelay;
        internal float StaminaRecoverSpeed => staminaRecoverSpeed;
        internal float GuardStaminaRecoveryRate => guardStaminaRecoveryRate;
        internal float RollStaminaCost => rollStaminaCost;
        internal float SprintStaminaCostPerSecond => sprintStaminaCostPerSecond;
        internal float SprintRestartStamina => sprintRestartStamina;
        internal float ActionInputBufferDuration => actionInputBufferDuration;
        internal float GuardAngle => guardAngle;
        internal float GuardRaiseDuration => guardRaiseDuration;
        internal float GuardBreakControlLockDuration => guardBreakControlLockDuration;
        internal float HitPushDuration => hitPushDuration;
        internal AnimationCurve HitPushCurve => hitPushCurve;
        internal float StopPointLimit => stopPointLimit;
        internal float StopPointRecoverDelay => stopPointRecoverDelay;
        internal float StopPointRecoverSpeed => stopPointRecoverSpeed;

        internal void Validate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            maxStamina = Mathf.Max(1f, maxStamina);
            staminaRecoverDelay = Mathf.Max(0f, staminaRecoverDelay);
            staminaRecoverSpeed = Mathf.Max(0f, staminaRecoverSpeed);
            guardStaminaRecoveryRate = Mathf.Clamp01(guardStaminaRecoveryRate);
            rollStaminaCost = Mathf.Max(0f, rollStaminaCost);
            sprintStaminaCostPerSecond = Mathf.Max(0f, sprintStaminaCostPerSecond);
            sprintRestartStamina = Mathf.Clamp(sprintRestartStamina, 0f, maxStamina);
            actionInputBufferDuration = Mathf.Max(0f, actionInputBufferDuration);
            guardAngle = Mathf.Clamp(guardAngle, 0f, 180f);
            guardRaiseDuration = Mathf.Max(0f, guardRaiseDuration);
            guardBreakControlLockDuration = Mathf.Max(0f, guardBreakControlLockDuration);
            hitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            stopPointLimit = Mathf.Max(1f, stopPointLimit);
            stopPointRecoverDelay = Mathf.Max(0f, stopPointRecoverDelay);
            stopPointRecoverSpeed = Mathf.Max(0f, stopPointRecoverSpeed);
            if (hitPushCurve == null || hitPushCurve.length < 2)
            {
                hitPushCurve = CreateDefaultHitPushCurve();
            }
        }

        private static AnimationCurve CreateDefaultHitPushCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 2f, 2f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
    }

    [Serializable]
    internal sealed class PlayerTargetSettings
    {
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask obstructionLayers;
        [SerializeField, Min(0f)] private float findRange = 12f;
        [SerializeField, Min(0f)] private float breakDistance = 15f;
        [SerializeField, Range(0f, 180f)] private float maximumAngle = 70f;
        [SerializeField, Min(0f)] private float hiddenGraceDuration = 0.35f;
        [SerializeField, Min(0f)] private float heightOffset = 1.2f;

        internal LayerMask TargetLayers => targetLayers;
        internal LayerMask ObstructionLayers => obstructionLayers;
        internal float FindRange => findRange;
        internal float BreakDistance => breakDistance;
        internal float MaximumAngle => maximumAngle;
        internal float HiddenGraceDuration => hiddenGraceDuration;
        internal float HeightOffset => heightOffset;

        internal void Validate()
        {
            findRange = Mathf.Max(0f, findRange);
            breakDistance = Mathf.Max(findRange, breakDistance);
            maximumAngle = Mathf.Clamp(maximumAngle, 0f, 180f);
            hiddenGraceDuration = Mathf.Max(0f, hiddenGraceDuration);
            heightOffset = Mathf.Max(0f, heightOffset);
        }
    }

    [Serializable]
    internal sealed class PlayerAttackSettings
    {
        [SerializeField] private PlayerAttackData[] attacks =
            new PlayerAttackData[6];
        [Header("공통 락온 보정")]
        [SerializeField, Min(0f)] private float targetStopDistance = 0.85f;
        [SerializeField, Min(0f)] private float maximumAddedMoveDistance = 0.25f;
        [SerializeField, Range(0f, 180f)] private float maximumTurnAngle = 30f;
        [Header("공통 콤보")]
        [SerializeField, Range(0f, 1f)]
        private float comboCloseNormalizedTime = 0.9f;

        internal PlayerAttackData[] Attacks => attacks;
        internal float TargetStopDistance => targetStopDistance;
        internal float MaximumAddedMoveDistance => maximumAddedMoveDistance;
        internal float MaximumTurnAngle => maximumTurnAngle;
        internal float ComboCloseNormalizedTime => comboCloseNormalizedTime;

        internal void Validate()
        {
            attacks ??= new PlayerAttackData[6];
            targetStopDistance = Mathf.Max(0f, targetStopDistance);
            maximumAddedMoveDistance = Mathf.Max(0f, maximumAddedMoveDistance);
            maximumTurnAngle = Mathf.Clamp(maximumTurnAngle, 0f, 180f);
            comboCloseNormalizedTime = Mathf.Clamp01(comboCloseNormalizedTime);
        }
    }

    // ScriptableObject를 직접 들고 다니지 않도록 생성 시 만든 불변 설정 묶음이다.
    internal sealed class PlayerCharacterRuntimeConfig
    {
        internal PlayerMovementRuntimeConfig Movement { get; }
        internal PlayerCombatRuntimeConfig Combat { get; }
        internal PlayerTargetRuntimeConfig Target { get; }
        internal PlayerAttackRuntimeConfig Attacks { get; }

        internal PlayerCharacterRuntimeConfig(
            PlayerMovementSettings movement,
            PlayerCombatSettings combat,
            PlayerTargetSettings target,
            PlayerAttackSettings attacks)
        {
            Movement = new PlayerMovementRuntimeConfig(movement);
            Combat = new PlayerCombatRuntimeConfig(combat);
            Target = new PlayerTargetRuntimeConfig(target);
            Attacks = new PlayerAttackRuntimeConfig(attacks);
        }
    }

    internal sealed class PlayerMovementRuntimeConfig
    {
        internal float FreeMoveTurnSpeed { get; }
        internal float TargetMoveTurnSpeed { get; }
        internal float AttackTurnSpeed { get; }
        internal float WalkSpeed { get; }
        internal float GuardMoveSpeed { get; }
        internal float SprintSpeed { get; }
        internal float MoveAcceleration { get; }
        internal float MoveDeceleration { get; }
        internal float AnimationSmoothTime { get; }
        internal float RollDistance { get; }
        internal float SprintRollDistance { get; }
        internal float RollCompleteNormalizedTime { get; }
        internal AnimationCurve RollMovementCurve { get; }
        internal float Gravity { get; }
        internal float GroundPull { get; }

        internal PlayerMovementRuntimeConfig(PlayerMovementSettings source)
        {
            FreeMoveTurnSpeed = source.FreeMoveTurnSpeed;
            TargetMoveTurnSpeed = source.TargetMoveTurnSpeed;
            AttackTurnSpeed = source.AttackTurnSpeed;
            WalkSpeed = source.WalkSpeed;
            GuardMoveSpeed = source.GuardMoveSpeed;
            SprintSpeed = source.SprintSpeed;
            MoveAcceleration = source.MoveAcceleration;
            MoveDeceleration = source.MoveDeceleration;
            AnimationSmoothTime = source.AnimationSmoothTime;
            RollDistance = source.RollDistance;
            SprintRollDistance = source.SprintRollDistance;
            RollCompleteNormalizedTime = source.RollCompleteNormalizedTime;
            RollMovementCurve = CloneCurve(source.RollMovementCurve);
            Gravity = source.Gravity;
            GroundPull = source.GroundPull;
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

    internal sealed class PlayerCombatRuntimeConfig
    {
        internal float MaxHealth { get; }
        internal float MaxStamina { get; }
        internal float StaminaRecoverDelay { get; }
        internal float StaminaRecoverSpeed { get; }
        internal float GuardStaminaRecoveryRate { get; }
        internal float RollStaminaCost { get; }
        internal float SprintStaminaCostPerSecond { get; }
        internal float SprintRestartStamina { get; }
        internal float ActionInputBufferDuration { get; }
        internal float GuardAngle { get; }
        internal float MinimumGuardDot { get; }
        internal float GuardRaiseDuration { get; }
        internal float GuardBreakControlLockDuration { get; }
        internal float HitPushDuration { get; }
        internal AnimationCurve HitPushCurve { get; }
        internal float StopPointLimit { get; }
        internal float StopPointRecoverDelay { get; }
        internal float StopPointRecoverSpeed { get; }

        internal PlayerCombatRuntimeConfig(PlayerCombatSettings source)
        {
            MaxHealth = source.MaxHealth;
            MaxStamina = source.MaxStamina;
            StaminaRecoverDelay = source.StaminaRecoverDelay;
            StaminaRecoverSpeed = source.StaminaRecoverSpeed;
            GuardStaminaRecoveryRate = source.GuardStaminaRecoveryRate;
            RollStaminaCost = source.RollStaminaCost;
            SprintStaminaCostPerSecond = source.SprintStaminaCostPerSecond;
            SprintRestartStamina = source.SprintRestartStamina;
            ActionInputBufferDuration = source.ActionInputBufferDuration;
            GuardAngle = source.GuardAngle;
            MinimumGuardDot = Mathf.Cos(
                source.GuardAngle * 0.5f * Mathf.Deg2Rad);
            GuardRaiseDuration = source.GuardRaiseDuration;
            GuardBreakControlLockDuration = source.GuardBreakControlLockDuration;
            HitPushDuration = source.HitPushDuration;
            HitPushCurve = CloneCurve(source.HitPushCurve);
            StopPointLimit = source.StopPointLimit;
            StopPointRecoverDelay = source.StopPointRecoverDelay;
            StopPointRecoverSpeed = source.StopPointRecoverSpeed;
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

    internal sealed class PlayerTargetRuntimeConfig
    {
        internal LayerMask TargetLayers { get; }
        internal LayerMask ObstructionLayers { get; }
        internal float FindRange { get; }
        internal float BreakDistance { get; }
        internal float BreakDistanceSquared { get; }
        internal float MaximumAngle { get; }
        internal float HiddenGraceDuration { get; }
        internal float HeightOffset { get; }

        internal PlayerTargetRuntimeConfig(PlayerTargetSettings source)
        {
            TargetLayers = source.TargetLayers;
            ObstructionLayers = source.ObstructionLayers;
            FindRange = source.FindRange;
            BreakDistance = source.BreakDistance;
            BreakDistanceSquared = source.BreakDistance * source.BreakDistance;
            MaximumAngle = source.MaximumAngle;
            HiddenGraceDuration = source.HiddenGraceDuration;
            HeightOffset = source.HeightOffset;
        }
    }

    internal sealed class PlayerAttackRuntimeConfig
    {
        internal PlayerAttackData[] Attacks { get; }
        internal float TargetStopDistance { get; }
        internal float MaximumAddedMoveDistance { get; }
        internal float MaximumTurnAngle { get; }
        internal float ComboCloseNormalizedTime { get; }

        internal PlayerAttackRuntimeConfig(PlayerAttackSettings source)
        {
            Attacks = source.Attacks;
            TargetStopDistance = source.TargetStopDistance;
            MaximumAddedMoveDistance = source.MaximumAddedMoveDistance;
            MaximumTurnAngle = source.MaximumTurnAngle;
            ComboCloseNormalizedTime = source.ComboCloseNormalizedTime;
            ValidateAttacks(Attacks);
        }

        private static void ValidateAttacks(PlayerAttackData[] attacks)
        {
            if (attacks == null || attacks.Length != 6)
            {
                throw new ArgumentException(
                    "Player 공격 데이터는 1~6까지 6개가 필요합니다.",
                    nameof(attacks));
            }

            for (int index = 0; index < attacks.Length; index++)
            {
                if (attacks[index] == null ||
                    attacks[index].AttackNumber != index + 1)
                {
                    throw new ArgumentException(
                        $"Player 공격 데이터 {index + 1}번이 없거나 순서가 올바르지 않습니다.",
                        nameof(attacks));
                }
            }
        }
    }
}
