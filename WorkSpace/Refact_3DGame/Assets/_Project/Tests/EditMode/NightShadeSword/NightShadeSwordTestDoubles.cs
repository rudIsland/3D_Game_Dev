using System;
using System.Collections.Generic;
using rudIsland.RPG3D.Characters;
using rudIsland.RPG3D.Characters.Combat;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using rudIsland.RPG3D.Characters.Enemies.AttackData;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEditor;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    internal sealed class NightShadeSwordTestScope : IDisposable
    {
        internal GameObject TargetObject { get; }
        internal FakeNightShadeSwordMovement Movement { get; }
        internal FakeNightShadeSwordAnimation Animation { get; }
        internal FakeUnitDeathState TargetDeathState { get; }
        internal NightShadeSwordTestActions Actions { get; }

        internal NightShadeSwordTestScope(Vector3 targetPosition)
        {
            TargetObject = new GameObject("NightShade Test Target");
            TargetObject.transform.position = targetPosition;
            Movement = new FakeNightShadeSwordMovement();
            Animation = new FakeNightShadeSwordAnimation();
            TargetDeathState = new FakeUnitDeathState();
            Actions = new NightShadeSwordTestActions();
        }

        internal NightShadeSwordSettings CreateSettings(
            float findRange = 10f,
            float attackRange = 4f,
            float walkStartRange = 5f,
            float runStartRange = 6f,
            float deadBodyKeepTime = 1f,
            float knockdownStayDuration = 0.5f,
            float staggerBreakStayDuration = 1.25f,
            float comboFirstExitNormalizedTime = 0.4f,
            float comboSecondDelay = 0.15f,
            float recoveryMoveDuration = 0.6f)
        {
            EnemyAttackData[] attacks = CreateAttackData(
                comboFirstExitNormalizedTime,
                comboSecondDelay);
            try
            {
                return new NightShadeSwordSettings(
                    new NightShadeSwordLifeSettings(
                        250f,
                        100f,
                        2.5f,
                        8f,
                        deadBodyKeepTime),
                    new NightShadeSwordCombatRangeSettings(
                        1 << 17,
                        findRange,
                        attackRange,
                        walkStartRange,
                        runStartRange,
                        15f),
                    new NightShadeSwordAttackSelectionSettings(
                        0.55f,
                        0.25f,
                        0.05f),
                    new NightShadeSwordMovementSettings(
                        1.8f,
                        3f,
                        360f,
                        180f,
                        -22f,
                        -2f),
                    attacks,
                    new NightShadeSwordRecoverySettings(
                        2f,
                        recoveryMoveDuration,
                        0.35f,
                        0.35f,
                        0.25f,
                        0.65f,
                        0.35f,
                        0.35f,
                        0.20f,
                        0.05f),
                    new NightShadeSwordHitReactionSettings(
                        0.2f,
                        0.4f,
                        0.6f,
                        knockdownStayDuration,
                        staggerBreakStayDuration,
                        AnimationCurve.Linear(0f, 0f, 1f, 1f)));
            }
            finally
            {
                for (int index = 0; index < attacks.Length; index++)
                {
                    UnityEngine.Object.DestroyImmediate(attacks[index]);
                }
            }
        }

        private static EnemyAttackData[] CreateAttackData(
            float comboFirstExitNormalizedTime,
            float comboSecondDelay)
        {
            return new EnemyAttackData[]
            {
                CreateSingleAttackData(
                    NightShadeSwordActionId.Light,
                    2f,
                    0.35f,
                    0.55f,
                    0.55f),
                CreateComboAttackData(
                    comboFirstExitNormalizedTime,
                    comboSecondDelay),
                CreateSingleAttackData(
                    NightShadeSwordActionId.Heavy,
                    3f,
                    0.40f,
                    0.90f,
                    0.30f),
                CreateSingleAttackData(
                    NightShadeSwordActionId.WideSwing,
                    2.5f,
                    0.38f,
                    0.65f,
                    0.45f)
            };
        }

        private static NightShadeSwordSingleAttackData CreateSingleAttackData(
            NightShadeSwordActionId actionId,
            float postAttackDelay,
            float baseScore,
            float preferredDistance,
            float distanceTolerance)
        {
            NightShadeSwordSingleAttackData attack =
                ScriptableObject.CreateInstance<
                    NightShadeSwordSingleAttackData>();
            var serializedAttack = new SerializedObject(attack);
            serializedAttack.FindProperty("actionId").enumValueIndex =
                (int)actionId;
            SetCommonAttackData(
                serializedAttack,
                1,
                postAttackDelay,
                baseScore,
                preferredDistance,
                distanceTolerance);
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();
            return attack;
        }

        private static NightShadeSwordComboAttackData CreateComboAttackData(
            float firstExitNormalizedTime,
            float secondDelay)
        {
            NightShadeSwordComboAttackData attack =
                ScriptableObject.CreateInstance<
                    NightShadeSwordComboAttackData>();
            var serializedAttack = new SerializedObject(attack);
            serializedAttack.FindProperty("firstExitNormalizedTime").floatValue =
                firstExitNormalizedTime;
            serializedAttack.FindProperty("secondDelay").floatValue =
                secondDelay;
            SetCommonAttackData(
                serializedAttack,
                2,
                2.5f,
                0.40f,
                0.25f,
                0.35f);
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();
            return attack;
        }

        private static void SetCommonAttackData(
            SerializedObject serializedAttack,
            int hitCount,
            float postAttackDelay,
            float baseScore,
            float preferredDistance,
            float distanceTolerance)
        {
            SerializedProperty hitDamages =
                serializedAttack.FindProperty("hitDamages");
            hitDamages.arraySize = hitCount;
            for (int index = 0; index < hitCount; index++)
            {
                SetDamage(
                    hitDamages.GetArrayElementAtIndex(index),
                    new AttackDamage());
            }

            serializedAttack.FindProperty("postAttackDelay").floatValue =
                postAttackDelay;
            SerializedProperty utility =
                serializedAttack.FindProperty("utility");
            utility.FindPropertyRelative("baseScore").floatValue = baseScore;
            utility.FindPropertyRelative("preferredDistance").floatValue =
                preferredDistance;
            utility.FindPropertyRelative("distanceTolerance").floatValue =
                distanceTolerance;
        }

        private static void SetDamage(
            SerializedProperty property,
            AttackDamage damage)
        {
            property.FindPropertyRelative("healthDamage").floatValue =
                damage.HealthDamage;
            property.FindPropertyRelative("strength").enumValueIndex =
                (int)damage.Strength;
            property.FindPropertyRelative("staggerDamage").floatValue =
                damage.StaggerDamage;
            property.FindPropertyRelative("pushDistance").floatValue =
                damage.PushDistance;
            property.FindPropertyRelative("hitStopDuration").floatValue =
                damage.HitStopDuration;
            property.FindPropertyRelative("guardStaminaDamage").floatValue =
                damage.GuardStaminaDamage;
            property.FindPropertyRelative("canBlock").boolValue =
                damage.CanBlock;
            property.FindPropertyRelative("damageSoundType").enumValueIndex =
                (int)damage.DamageSoundType;
        }

        internal NightShadeSwordStateMachine CreateStateMachine(
            NightShadeSwordSettings settings,
            INightShadeSwordRandomProvider randomProvider = null)
        {
            return new NightShadeSwordStateMachine(
                TargetObject.transform,
                TargetDeathState,
                Movement,
                Animation,
                settings,
                Actions.Value,
                randomProvider);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(TargetObject);
        }
    }

    internal sealed class FakeNightShadeSwordMovement : INightShadeSwordMovement
    {

        internal int MoveToCount { get; private set; }
        internal int TurnToCount { get; private set; }
        internal int StayCount { get; private set; }
        internal int CombatMoveCount { get; private set; }

        internal float LastMoveSpeed { get; private set; }

        internal Vector3 TotalHitMovement { get; private set; }
        internal bool IsFacingTarget { get; set; } = true;

        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; } = Vector3.forward;

        public void Reset()
        {
        }

        public void MoveTo(
            Vector3 targetPosition,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            MoveToCount++;
            LastMoveSpeed = moveSpeed;
        }

        public void TurnTo(
            Vector3 targetPosition,
            float turnSpeed,
            float deltaTime)
        {
            TurnToCount++;
        }

        public void MoveForCombat(
            Vector3 targetPosition,
            NightShadeCombatMoveType moveType,
            float moveSpeed,
            float turnSpeed,
            float deltaTime)
        {
            CombatMoveCount++;
        }

        public void StayOnGround(float deltaTime)
        {
            StayCount++;
        }

        public void ApplyHitMovement(Vector3 horizontalMovement, float deltaTime)
        {

            TotalHitMovement += horizontalMovement;
        }

        public bool IsFacing(Vector3 targetPosition, float minimumFacingDot)
        {
            return IsFacingTarget;
        }
    }

    internal sealed class FixedNightShadeSwordRandomProvider :
        INightShadeSwordRandomProvider
    {
        private readonly float value;

        internal FixedNightShadeSwordRandomProvider(float value = 0f)
        {
            this.value = Mathf.Clamp01(value);
        }

        public float Next01()
        {
            return value;
        }
    }

    internal sealed class SequenceNightShadeSwordRandomProvider :
        INightShadeSwordRandomProvider
    {
        private readonly float[] values;
        private int index;

        internal SequenceNightShadeSwordRandomProvider(params float[] values)
        {
            this.values = values;
        }

        public float Next01()
        {
            if (values == null || values.Length == 0)
            {
                return 0f;
            }

            float value = values[index % values.Length];
            index++;
            return Mathf.Clamp01(value);
        }
    }

    internal sealed class FakeNightShadeSwordAnimation : INightShadeSwordAnimation
    {
        internal int IdleCount { get; private set; }
        internal int ChaseCount { get; private set; }
        internal int WalkCount { get; private set; }
        internal int CombatMoveCount { get; private set; }

        internal int AttackCount { get; private set; }
        internal int HitCount { get; private set; }
        internal int SmallHitCount { get; private set; }
        internal int BigHitCount { get; private set; }
        internal int KnockbackCount { get; private set; }
        internal int KnockdownCount { get; private set; }
        internal int GetUpCount { get; private set; }
        internal int StaggerEnterCount { get; private set; }
        internal int StaggerStartCount { get; private set; }
        internal int StaggerIdleCount { get; private set; }
        internal int StaggerEndCount { get; private set; }
        internal int DeadCount { get; private set; }
        internal int ResetSpeedCount { get; private set; }
        internal NightShadeSwordAttackType LastAttackType { get; private set; }
        internal Vector3 LastHitDirection { get; private set; }

        internal float NormalizedTime { get; set; }

        public void PlayIdle() => IdleCount++;
        public void PlayChase() => ChaseCount++;
        public void PlayWalk() => WalkCount++;
        public void PlayCombatMove(NightShadeCombatMoveType moveType)
        {
            CombatMoveCount++;
        }

        public void PlayAttack(NightShadeSwordAttackType attackType)
        {
            AttackCount++;
            LastAttackType = attackType;
            NormalizedTime = 0f;
        }

        public void PlaySmallHitFromStart(Vector3 incomingDirection)
        {
            HitCount++;
            SmallHitCount++;
            LastHitDirection = incomingDirection;
            NormalizedTime = 0f;
        }

        public void PlayBigHitFromStart(Vector3 incomingDirection)
        {
            HitCount++;
            BigHitCount++;
            LastHitDirection = incomingDirection;
            NormalizedTime = 0f;
        }

        public void PlayKnockbackFromStart()
        {
            KnockbackCount++;
            NormalizedTime = 0f;
        }

        public void PlayKnockdownFromStart()
        {
            KnockdownCount++;
            NormalizedTime = 0f;
        }

        public void PlayGetUpFromStart()
        {
            GetUpCount++;
            NormalizedTime = 0f;
        }

        public void PlayStaggerEnterFromStart()
        {
            StaggerEnterCount++;
            NormalizedTime = 0f;
        }

        public void PlayStaggerStartFromStart()
        {
            StaggerStartCount++;
            NormalizedTime = 0f;
        }

        public void PlayStaggerIdleFromStart()
        {
            StaggerIdleCount++;
            NormalizedTime = 0f;
        }

        public void PlayStaggerEndFromStart()
        {
            StaggerEndCount++;
            NormalizedTime = 0f;
        }

        public void PlayDead()
        {
            DeadCount++;
            NormalizedTime = 0f;
        }

        public void ResetAttackPlaybackSpeed() => ResetSpeedCount++;

        public bool TryGetRequestedAnimationTime(out float normalizedTime)
        {
            normalizedTime = NormalizedTime;
            return true;
        }

        public bool IsTransitioning() => false;
    }

    internal sealed class FakeUnitDeathState : IUnitDeathState
    {
        public bool IsDead { get; set; }
    }

    internal sealed class NightShadeSwordTestActions
    {
        internal NightShadeSwordActions Value { get; }
        internal List<string> Calls { get; } = new List<string>();
        internal int CloseCount { get; private set; }
        internal int ReleaseCount { get; private set; }

        internal NightShadeSwordTestActions()
        {
            Value = new NightShadeSwordActions(
                PlaySound,
                OpenHit,
                CloseHit,
                RequestRelease);
        }

        private void PlaySound(NightShadeSwordAttackType attackType, int hitIndex)
        {
            Calls.Add($"Sound:{hitIndex}");
        }

        private void OpenHit(NightShadeSwordAttackType attackType, int hitIndex)
        {
            Calls.Add($"Open:{hitIndex}");
        }

        private void CloseHit()
        {
            CloseCount++;
            Calls.Add("Close");
        }

        private void RequestRelease()
        {
            ReleaseCount++;
        }
    }
}
