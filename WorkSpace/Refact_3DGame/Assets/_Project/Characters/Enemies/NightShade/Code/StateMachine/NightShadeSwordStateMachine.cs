using System;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal enum NightShadeSwordAttackType
    {
        Light = 0,
        Combo = 1,
        Heavy = 2,
        WideSwing = 3
    }

    internal enum NightShadeCombatMoveType
    {
        Backward = 0,
        Left = 1,
        Right = 2
    }

    // 대기, 추적, 양손검 공격, 피격, 사망 순서를 한곳에서 관리한다.
    internal sealed class NightShadeSwordStateMachine
    {
        private const float LightAttackSoundTime = 0.2f;
        private const float ComboFirstAttackSoundTime = 0.14f;
        private const float ComboSecondAttackSoundTime = 0.52f;
        private const float WideSwingAttackSoundTime = 0.22f;
        private const float HeavyAttackSoundTime = 0.34f;

        private const float LightTurnEndTime = 0.18f;
        private const float ComboTurnEndTime = 0.12f;
        private const float WideSwingTurnEndTime = 0.2f;
        private const float HeavyTurnEndTime = 0.28f;

        private enum EnemyState
        {
            Idle = 0,
            Chase = 1,
            Attack = 2,
            CombatMove = 3,
            Hit = 4,
            Dead = 5
        }

        private readonly Transform target;
        private readonly IUnitDeathState targetDeathState;
        private readonly NightShadeSwordMovement movement;
        private readonly NightShadeSwordAnimationController animation;
        private readonly float findRangeSquared;
        private readonly float attackRangeSquared;
        private readonly float attackFacingDot;
        private readonly float chaseSpeed;
        private readonly float turnSpeed;
        private readonly float attackTurnSpeed;
        private readonly float lightAttackRecovery;
        private readonly float comboAttackRecovery;
        private readonly float wideSwingAttackRecovery;
        private readonly float heavyAttackRecovery;
        private readonly float combatMoveSpeed;
        private readonly float combatMoveDuration;
        private readonly int attacksBeforeCombatMove;
        private readonly float hitPushDuration;
        private readonly AnimationCurve hitPushCurve;
        private readonly float deadBodyKeepTime;
        private readonly Action<NightShadeSwordAttackType, int>
            playAttackSound;
        private readonly Action<NightShadeSwordAttackType, int>
            openAttackHit;
        private readonly Action closeAttackHit;
        private readonly Action requestRelease;

        private EnemyState currentState;
        private NightShadeSwordAttackType currentAttackType;
        private NightShadeSwordAttackType previousAttackType;
        private NightShadeCombatMoveType currentCombatMoveType;
        private EnemyHitRequest hitRequest;
        private bool hasPreviousAttack;
        private bool moveLeftNext;
        private bool isEnabled;
        private bool isInCombat;
        private int completedAttackCount;
        private int openHitIndex;
        private int playedSoundHitIndex;
        private float remainingAttackCooldown;
        private float remainingCombatMoveTime;
        private float elapsedHitPushTime;
        private float previousHitPushProgress;
        private float remainingDeadBodyKeepTime;
        private bool deadAnimationFinished;

        internal bool IsInCombat => isInCombat;

        internal event Action CombatStateChanged;

        internal NightShadeSwordStateMachine(
            Transform target,
            IUnitDeathState targetDeathState,
            NightShadeSwordMovement movement,
            NightShadeSwordAnimationController animation,
            float findRange,
            float attackRange,
            float attackFacingAngle,
            float chaseSpeed,
            float turnSpeed,
            float attackTurnSpeed,
            float lightAttackRecovery,
            float comboAttackRecovery,
            float wideSwingAttackRecovery,
            float heavyAttackRecovery,
            float combatMoveSpeed,
            float combatMoveDuration,
            int attacksBeforeCombatMove,
            float hitPushDuration,
            AnimationCurve hitPushCurve,
            float deadBodyKeepTime,
            Action<NightShadeSwordAttackType, int> playAttackSound,
            Action<NightShadeSwordAttackType, int> openAttackHit,
            Action closeAttackHit,
            Action requestRelease)
        {
            this.target = target;
            this.targetDeathState = targetDeathState;
            this.movement = movement;
            this.animation = animation;
            findRangeSquared = findRange * findRange;
            attackRangeSquared = attackRange * attackRange;
            attackFacingDot = Mathf.Cos(
                Mathf.Clamp(attackFacingAngle, 0f, 180f) *
                Mathf.Deg2Rad);
            this.chaseSpeed = chaseSpeed;
            this.turnSpeed = turnSpeed;
            this.attackTurnSpeed = attackTurnSpeed;
            this.lightAttackRecovery = Mathf.Max(0f, lightAttackRecovery);
            this.comboAttackRecovery = Mathf.Max(0f, comboAttackRecovery);
            this.wideSwingAttackRecovery =
                Mathf.Max(0f, wideSwingAttackRecovery);
            this.heavyAttackRecovery = Mathf.Max(0f, heavyAttackRecovery);
            this.combatMoveSpeed = Mathf.Max(0.1f, combatMoveSpeed);
            this.combatMoveDuration = Mathf.Max(0.1f, combatMoveDuration);
            this.attacksBeforeCombatMove =
                Mathf.Max(1, attacksBeforeCombatMove);
            this.hitPushDuration = Mathf.Max(0.01f, hitPushDuration);
            this.hitPushCurve = hitPushCurve;
            this.deadBodyKeepTime = deadBodyKeepTime;
            this.playAttackSound = playAttackSound;
            this.openAttackHit = openAttackHit;
            this.closeAttackHit = closeAttackHit;
            this.requestRelease = requestRelease;
        }

        internal void Enable()
        {
            isEnabled = true;
            movement.Reset();
            animation.ResetAttackPlaybackSpeed();
            hasPreviousAttack = false;
            moveLeftNext = true;
            completedAttackCount = 0;
            remainingAttackCooldown = 0f;
            remainingCombatMoveTime = 0f;
            SetCombatState(false);
            ChangeState(EnemyState.Idle, true);
        }

        internal void Disable()
        {
            isEnabled = false;
            closeAttackHit?.Invoke();
            openHitIndex = -1;
            animation.ResetAttackPlaybackSpeed();
            SetCombatState(false);
        }

        internal void Update(float deltaTime)
        {
            if (!isEnabled)
            {
                return;
            }

            if (remainingAttackCooldown > 0f)
            {
                remainingAttackCooldown = Mathf.Max(
                    0f,
                    remainingAttackCooldown - deltaTime);
            }

            switch (currentState)
            {
                case EnemyState.Chase:
                    UpdateChase(deltaTime);
                    break;
                case EnemyState.Attack:
                    UpdateAttack(deltaTime);
                    break;
                case EnemyState.CombatMove:
                    UpdateCombatMove(deltaTime);
                    break;
                case EnemyState.Hit:
                    UpdateHit(deltaTime);
                    break;
                case EnemyState.Dead:
                    UpdateDead(deltaTime);
                    break;
                default:
                    UpdateIdle(deltaTime);
                    break;
            }
        }

        internal void ChangeToHitState(in EnemyHitRequest nextHitRequest)
        {
            if (currentState == EnemyState.Dead)
            {
                return;
            }

            hitRequest = nextHitRequest;
            closeAttackHit?.Invoke();
            openHitIndex = -1;
            completedAttackCount = 0;
            remainingCombatMoveTime = 0f;
            elapsedHitPushTime = 0f;
            previousHitPushProgress = EvaluateHitPushProgress(0f);
            currentState = EnemyState.Hit;
            animation.ResetAttackPlaybackSpeed();
            animation.PlayHitFromStart();
            SetCombatState(true);
        }

        internal void ChangeToDeadState()
        {
            closeAttackHit?.Invoke();
            openHitIndex = -1;
            currentState = EnemyState.Dead;
            completedAttackCount = 0;
            remainingCombatMoveTime = 0f;
            deadAnimationFinished = false;
            remainingDeadBodyKeepTime = 0f;
            animation.ResetAttackPlaybackSpeed();
            animation.PlayDead();
            SetCombatState(false);
        }

        internal void NotifyDamaged()
        {
            if (currentState != EnemyState.Dead)
            {
                SetCombatState(true);
            }
        }

        private void UpdateIdle(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
            if (!IsTargetFound())
            {
                return;
            }

            SetCombatState(true);
            ChangeState(EnemyState.Chase);
        }

        private void UpdateChase(float deltaTime)
        {
            if (!IsTargetFound())
            {
                SetCombatState(false);
                ChangeState(EnemyState.Idle);
                return;
            }

            bool isInAttackRange =
                GetTargetDistanceSquared() <= attackRangeSquared;
            if (isInAttackRange &&
                remainingAttackCooldown <= 0f &&
                movement.IsFacing(target.position, attackFacingDot))
            {
                StartAttack();
                return;
            }

            if (isInAttackRange)
            {
                movement.TurnTo(target.position, turnSpeed, deltaTime);
            }
            else
            {
                movement.MoveTo(
                    target.position,
                    chaseSpeed,
                    turnSpeed,
                    deltaTime);
            }
        }

        private void StartAttack()
        {
            currentAttackType = ChooseAttack();
            previousAttackType = currentAttackType;
            hasPreviousAttack = true;
            openHitIndex = -1;
            playedSoundHitIndex = -1;
            currentState = EnemyState.Attack;
            animation.ResetAttackPlaybackSpeed();
            animation.PlayAttack(currentAttackType);
        }

        private void UpdateAttack(float deltaTime)
        {
            if (!IsTargetAlive())
            {
                FinishAttack();
                return;
            }

            if (!animation.TryGetRequestedAnimationTime(
                    out float normalizedTime))
            {
                movement.StayOnGround(deltaTime);
                return;
            }

            if (normalizedTime <
                GetAttackTurnEndTime(currentAttackType))
            {
                movement.TurnTo(
                    target.position,
                    attackTurnSpeed,
                    deltaTime);
            }
            else
            {
                movement.StayOnGround(deltaTime);
            }

            UpdateAttackSound(normalizedTime);
            UpdateAttackHitWindow(normalizedTime);

            if (!animation.IsTransitioning() && normalizedTime >= 1f)
            {
                FinishAttack();
            }
        }

        private void UpdateAttackSound(float normalizedTime)
        {
            int soundHitIndex = GetAttackSoundHitIndex(
                currentAttackType,
                normalizedTime);
            if (soundHitIndex < 0 ||
                soundHitIndex == playedSoundHitIndex)
            {
                return;
            }

            playedSoundHitIndex = soundHitIndex;
            playAttackSound?.Invoke(currentAttackType, soundHitIndex);
        }

        private void UpdateAttackHitWindow(float normalizedTime)
        {
            int hitIndex = GetActiveHitIndex(
                currentAttackType,
                normalizedTime);
            if (hitIndex == openHitIndex)
            {
                return;
            }

            closeAttackHit?.Invoke();
            openHitIndex = hitIndex;
            if (openHitIndex >= 0)
            {
                openAttackHit?.Invoke(currentAttackType, openHitIndex);
            }
        }

        private void FinishAttack()
        {
            closeAttackHit?.Invoke();
            openHitIndex = -1;
            animation.ResetAttackPlaybackSpeed();
            remainingAttackCooldown =
                GetAttackRecovery(currentAttackType);
            completedAttackCount++;

            if (IsTargetFound())
            {
                if (completedAttackCount >= attacksBeforeCombatMove ||
                    IsTargetVeryClose())
                {
                    StartCombatMove();
                }
                else
                {
                    ChangeState(EnemyState.Chase);
                }
            }
            else
            {
                SetCombatState(false);
                ChangeState(EnemyState.Idle);
            }
        }

        private void StartCombatMove()
        {
            if (IsTargetVeryClose())
            {
                currentCombatMoveType =
                    NightShadeCombatMoveType.Backward;
            }
            else
            {
                currentCombatMoveType = moveLeftNext
                    ? NightShadeCombatMoveType.Left
                    : NightShadeCombatMoveType.Right;
                moveLeftNext = !moveLeftNext;
            }

            remainingCombatMoveTime = combatMoveDuration;
            ChangeState(EnemyState.CombatMove);
        }

        private void UpdateCombatMove(float deltaTime)
        {
            if (!IsTargetFound())
            {
                completedAttackCount = 0;
                SetCombatState(false);
                ChangeState(EnemyState.Idle);
                return;
            }

            movement.MoveForCombat(
                target.position,
                currentCombatMoveType,
                combatMoveSpeed,
                turnSpeed,
                deltaTime);
            remainingCombatMoveTime = Mathf.Max(
                0f,
                remainingCombatMoveTime - deltaTime);
            if (remainingCombatMoveTime > 0f)
            {
                return;
            }

            completedAttackCount = 0;
            ChangeState(EnemyState.Chase);
        }

        private void UpdateHit(float deltaTime)
        {
            ApplyHitMovement(deltaTime);
            if (!animation.TryGetRequestedAnimationTime(
                    out float normalizedTime) ||
                animation.IsTransitioning() ||
                normalizedTime < 1f)
            {
                return;
            }

            if (IsTargetFound())
            {
                ChangeState(EnemyState.Chase);
            }
            else
            {
                SetCombatState(false);
                ChangeState(EnemyState.Idle);
            }
        }

        private void ApplyHitMovement(float deltaTime)
        {
            elapsedHitPushTime = Mathf.Min(
                elapsedHitPushTime + Mathf.Max(0f, deltaTime),
                hitPushDuration);
            float pushProgress = Mathf.Max(
                previousHitPushProgress,
                EvaluateHitPushProgress(
                    elapsedHitPushTime / hitPushDuration));
            float deltaProgress = pushProgress - previousHitPushProgress;
            Vector3 movementAmount =
                hitRequest.PushDirection *
                (hitRequest.PushDistance * deltaProgress);

            previousHitPushProgress = pushProgress;
            movement.ApplyHitMovement(movementAmount, deltaTime);
        }

        private void UpdateDead(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
            if (!deadAnimationFinished)
            {
                if (!animation.TryGetRequestedAnimationTime(
                        out float normalizedTime) ||
                    animation.IsTransitioning() ||
                    normalizedTime < 1f)
                {
                    return;
                }

                deadAnimationFinished = true;
                remainingDeadBodyKeepTime = deadBodyKeepTime;
            }

            remainingDeadBodyKeepTime -= deltaTime;
            if (remainingDeadBodyKeepTime <= 0f)
            {
                requestRelease?.Invoke();
            }
        }

        private void ChangeState(
            EnemyState nextState,
            bool force = false)
        {
            if (!force && currentState == nextState)
            {
                return;
            }

            closeAttackHit?.Invoke();
            openHitIndex = -1;
            currentState = nextState;
            switch (currentState)
            {
                case EnemyState.Chase:
                    animation.PlayChase();
                    break;
                case EnemyState.CombatMove:
                    animation.PlayCombatMove(currentCombatMoveType);
                    break;
                case EnemyState.Idle:
                    animation.PlayIdle();
                    break;
            }
        }

        private NightShadeSwordAttackType ChooseAttack()
        {
            float distanceSquared = GetTargetDistanceSquared();
            int roll = UnityEngine.Random.Range(0, 100);
            NightShadeSwordAttackType selectedAttack;

            if (distanceSquared <= attackRangeSquared * 0.36f)
            {
                selectedAttack = roll < 60
                    ? NightShadeSwordAttackType.Combo
                    : NightShadeSwordAttackType.Light;
            }
            else if (distanceSquared <= attackRangeSquared * 0.75f)
            {
                selectedAttack = roll < 30
                    ? NightShadeSwordAttackType.Light
                    : roll < 45
                        ? NightShadeSwordAttackType.Combo
                        : NightShadeSwordAttackType.WideSwing;
            }
            else
            {
                selectedAttack = roll < 60
                    ? NightShadeSwordAttackType.Heavy
                    : NightShadeSwordAttackType.WideSwing;
            }

            if (hasPreviousAttack && selectedAttack == previousAttackType)
            {
                if (distanceSquared <= attackRangeSquared * 0.36f)
                {
                    return selectedAttack == NightShadeSwordAttackType.Combo
                        ? NightShadeSwordAttackType.Light
                        : NightShadeSwordAttackType.Combo;
                }

                if (distanceSquared <= attackRangeSquared * 0.75f)
                {
                    return selectedAttack ==
                        NightShadeSwordAttackType.WideSwing
                            ? NightShadeSwordAttackType.Light
                            : NightShadeSwordAttackType.WideSwing;
                }

                return selectedAttack == NightShadeSwordAttackType.Heavy
                    ? NightShadeSwordAttackType.WideSwing
                    : NightShadeSwordAttackType.Heavy;
            }

            return selectedAttack;
        }

        private bool IsTargetVeryClose()
        {
            return GetTargetDistanceSquared() <=
                attackRangeSquared * 0.36f;
        }

        private float GetAttackRecovery(
            NightShadeSwordAttackType attackType)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.Combo:
                    return comboAttackRecovery;
                case NightShadeSwordAttackType.WideSwing:
                    return wideSwingAttackRecovery;
                case NightShadeSwordAttackType.Heavy:
                    return heavyAttackRecovery;
                default:
                    return lightAttackRecovery;
            }
        }

        private static float GetAttackTurnEndTime(
            NightShadeSwordAttackType attackType)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.Combo:
                    return ComboTurnEndTime;
                case NightShadeSwordAttackType.Heavy:
                    return HeavyTurnEndTime;
                case NightShadeSwordAttackType.WideSwing:
                    return WideSwingTurnEndTime;
                default:
                    return LightTurnEndTime;
            }
        }

        private bool IsTargetFound()
        {
            return IsTargetAlive() &&
                GetTargetDistanceSquared() <= findRangeSquared;
        }

        private bool IsTargetAlive()
        {
            return target != null &&
                target.gameObject.activeInHierarchy &&
                (targetDeathState == null || !targetDeathState.IsDead);
        }

        private float GetTargetDistanceSquared()
        {
            if (target == null)
            {
                return float.PositiveInfinity;
            }

            Vector3 difference = target.position - movement.Position;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }

        private float EvaluateHitPushProgress(float normalizedTime)
        {
            return Mathf.Clamp01(
                hitPushCurve != null && hitPushCurve.length > 0
                    ? hitPushCurve.Evaluate(Mathf.Clamp01(normalizedTime))
                    : normalizedTime);
        }

        private void SetCombatState(bool nextState)
        {
            if (isInCombat == nextState)
            {
                return;
            }

            isInCombat = nextState;
            CombatStateChanged?.Invoke();
        }

        private static int GetAttackSoundHitIndex(
            NightShadeSwordAttackType attackType,
            float normalizedTime)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.Combo:
                    if (normalizedTime >= ComboSecondAttackSoundTime)
                    {
                        return 1;
                    }

                    return normalizedTime >= ComboFirstAttackSoundTime
                        ? 0
                        : -1;
                case NightShadeSwordAttackType.Heavy:
                    return normalizedTime >= HeavyAttackSoundTime ? 0 : -1;
                case NightShadeSwordAttackType.WideSwing:
                    return normalizedTime >= WideSwingAttackSoundTime
                        ? 0
                        : -1;
                default:
                    return normalizedTime >= LightAttackSoundTime ? 0 : -1;
            }
        }

        private static int GetActiveHitIndex(
            NightShadeSwordAttackType attackType,
            float normalizedTime)
        {
            switch (attackType)
            {
                case NightShadeSwordAttackType.Combo:
                    if (normalizedTime >= 0.18f && normalizedTime <= 0.35f)
                    {
                        return 0;
                    }

                    return normalizedTime >= 0.56f && normalizedTime <= 0.73f
                        ? 1
                        : -1;
                case NightShadeSwordAttackType.Heavy:
                    return normalizedTime >= 0.42f && normalizedTime <= 0.68f
                        ? 0
                        : -1;
                case NightShadeSwordAttackType.WideSwing:
                    return normalizedTime >= 0.3f && normalizedTime <= 0.6f
                        ? 0
                        : -1;
                default:
                    return normalizedTime >= 0.28f && normalizedTime <= 0.52f
                        ? 0
                        : -1;
            }
        }
    }
}
