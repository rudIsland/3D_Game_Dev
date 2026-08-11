using System;
using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 상태 생성과 전환, Nightshade 공용 전투 정보를 조정한다.
    public sealed class NightshadeSpearStateMachine
    {
        private const float PhaseOneAttackSpeedMultiplier = 0.75f;
        private const float PhaseTwoAttackSpeedMultiplier = 0.95f;
        private const float PhaseOneReadySpeedMultiplier = 0.55f;
        private const float PhaseTwoReadySpeedMultiplier = 0.7f;
        private const float PhaseOneReadyDuration = 0.5f;
        private const float PhaseTwoReadyDuration = 0.45f;
        private const float PhaseOneSequenceRecoveryDuration = 0.7f;
        private const float PhaseTwoSequenceRecoveryDuration = 0.55f;
        private const int PhaseOneMaximumSequenceCount = 2;
        private const int PhaseTwoMaximumSequenceCount = 3;

        private readonly Transform target;
        private readonly NightshadeSpearMovement movement;
        private readonly NightshadeSpearAnimationController animation;
        private readonly NightshadeSpearEnterState enterState;
        private readonly NightshadeSpearIdleState idleState;
        private readonly NightshadeSpearChaseState chaseState;
        private readonly NightshadeSpearAttackState[] attackStates;
        private readonly NightshadeSpearHitState hitState;
        private readonly NightshadeSpearDeadState deadState;
        private readonly Action<AttackDamage, int> startAttackHit;
        private readonly Action endAttackHit;
        private readonly Func<bool> wasAttackDamageApplied;
        private readonly Action<int> playAttackReadyCue;
        private readonly Action<int, bool> playAttackHitCue;
        private readonly Action requestRelease;
        private readonly bool canTrackTarget;
        private readonly float phaseTwoHealthRate;

        private INightshadeSpearState currentState;
        private NightshadeSpearAttackState currentAttackState;
        private float currentTime;
        private bool isEnabled;
        private int currentPhase = 1;
        private NightshadeSpearAttackId lastStartingAttackId;
        private int sequenceAttackCount;

        internal Transform Target => target;
        internal NightshadeSpearMovement Movement => movement;
        internal NightshadeSpearAnimationController Animation => animation;
        internal float FindRangeSquared { get; }
        internal float RunStartRangeSquared { get; }
        internal float MaximumAttackRangeSquared { get; }
        internal float WalkSpeed { get; }
        internal float RunSpeed { get; }
        internal float TurnSpeed { get; }
        internal float DeadBodyKeepTime { get; }
        internal float CurrentTime => currentTime;
        public int CurrentPhase => currentPhase;
        public bool IsEnabled => isEnabled;
        public string CurrentStateName =>
            currentState != null
                ? currentState.GetType().Name
                : "Disabled";
        public string CurrentAttackName =>
            currentAttackState != null &&
            ReferenceEquals(currentState, currentAttackState)
                ? currentAttackState.AttackId.ToString()
                : string.Empty;
        public string CurrentAttackPhaseName =>
            currentAttackState != null &&
            ReferenceEquals(currentState, currentAttackState)
                ? currentAttackState.CurrentPhase.ToString()
                : string.Empty;

        public NightshadeSpearStateMachine(
            Transform target,
            NightshadeSpearMovement movement,
            NightshadeSpearAnimationController animation,
            float findRange,
            float runStartRange,
            float walkSpeed,
            float runSpeed,
            float turnSpeed,
            float deadBodyKeepTime,
            Action<AttackDamage, int> startAttackHit,
            Action endAttackHit,
            Func<bool> wasAttackDamageApplied,
            Action<int> playAttackReadyCue,
            Action<int, bool> playAttackHitCue,
            Action requestRelease,
            bool canTrackTarget = true,
            float phaseTwoHealthRate = 0.6f)
        {
            this.target = target;
            this.movement = movement;
            this.animation = animation;

            FindRangeSquared = findRange * findRange;
            RunStartRangeSquared = runStartRange * runStartRange;

            attackStates = new NightshadeSpearAttackState[]
            {
                new NightshadeSpearAttack01State(this),
                new NightshadeSpearAttack02State(this),
                new NightshadeSpearAttack03State(this),
                new NightshadeSpearAttack04State(this),
                new NightshadeSpearAttack05State(this),
                new NightshadeSpearAttack06State(this),
                new NightshadeSpearAttack07State(this),
                new NightshadeSpearAttack08State(this),
                new NightshadeSpearAttack09State(this),
                new NightshadeSpearAttack10State(this),
                new NightshadeSpearAttack11State(this),
                new NightshadeSpearAttack12State(this),
                new NightshadeSpearAttack13State(this)
            };

            float maximumAttackRangeSquared = 0f;
            for (int index = 0; index < attackStates.Length; index++)
            {
                maximumAttackRangeSquared = Mathf.Max(
                    maximumAttackRangeSquared,
                    attackStates[index].MaximumAttackDistanceSquared);
            }

            MaximumAttackRangeSquared = maximumAttackRangeSquared;
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            TurnSpeed = turnSpeed;
            DeadBodyKeepTime = Mathf.Max(0f, deadBodyKeepTime);
            this.startAttackHit = startAttackHit;
            this.endAttackHit = endAttackHit;
            this.wasAttackDamageApplied = wasAttackDamageApplied;
            this.playAttackReadyCue = playAttackReadyCue;
            this.playAttackHitCue = playAttackHitCue;
            this.requestRelease = requestRelease;
            this.canTrackTarget = canTrackTarget;
            this.phaseTwoHealthRate = Mathf.Clamp01(phaseTwoHealthRate);

            enterState = new NightshadeSpearEnterState(this);
            idleState = new NightshadeSpearIdleState(this);
            chaseState = new NightshadeSpearChaseState(this);
            hitState = new NightshadeSpearHitState(this);
            deadState = new NightshadeSpearDeadState(this);
        }

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            currentTime = 0f;
            currentPhase = 1;
            lastStartingAttackId = 0;
            sequenceAttackCount = 0;

            movement.Reset();
            animation.ResetAnimation();
            ChangeState(enterState);
        }

        public void Update(float deltaTime)
        {
            if (!isEnabled || currentState == null)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            currentTime += safeDeltaTime;
            currentState.Update(safeDeltaTime);
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            EndAttackHit();
            currentState = null;
            currentAttackState = null;
            sequenceAttackCount = 0;
            isEnabled = false;
            animation.ResetAnimation();
        }
        public void ChangeToHitState(Vector3 hitPosition)
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            ChangeToHitStateInternal(hitPosition);
        }

        public void SetHealthRatio(float healthRatio)
        {
            if (currentPhase >= 2 || healthRatio > phaseTwoHealthRate)
            {
                return;
            }

            currentPhase = 2;
        }

        public void ChangeToDeadState()
        {
            if (!isEnabled || ReferenceEquals(currentState, deadState))
            {
                return;
            }

            ChangeState(deadState);
        }

        internal bool IsTargetFound()
        {
            return canTrackTarget &&
                GetTargetDistanceSquared() <= FindRangeSquared;
        }

        internal float GetTargetDistanceSquared()
        {
            Vector3 distance = target.position - movement.Position;
            distance.y = 0f;
            return distance.sqrMagnitude;
        }

        internal float GetTargetFacingDot()
        {
            Vector3 direction = target.position - movement.Position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return 1f;
            }

            direction.Normalize();
            return Vector3.Dot(movement.Forward, direction);
        }

        internal void MoveToTarget(
            float deltaTime,
            bool shouldTurn = true)
        {
            float distanceSquared = GetTargetDistanceSquared();
            bool shouldRun = distanceSquared >= RunStartRangeSquared;
            float moveSpeed = shouldRun ? RunSpeed : WalkSpeed;
            Vector3 direction = movement.MoveTo(
                target.position,
                moveSpeed,
                TurnSpeed,
                deltaTime,
                shouldTurn);
            float moveSide = Vector3.Dot(movement.Right, direction);
            animation.SetMovement(moveSide, shouldRun ? 1f : 0.5f);
        }

        internal void MoveAwayFromTarget(
            float deltaTime,
            bool shouldTurn = true)
        {
            Vector3 direction = movement.MoveAwayFrom(
                target.position,
                WalkSpeed,
                TurnSpeed,
                deltaTime,
                shouldTurn);
            float moveSide = Vector3.Dot(movement.Right, direction);
            animation.SetMovement(moveSide, 0.5f);
        }

        internal void StayOnGround(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
        }

        internal bool TryChangeToContextAttackState()
        {
            float targetDistanceSquared = GetTargetDistanceSquared();
            float targetFacingDot = GetTargetFacingDot();
            NightshadeSpearAttackState selectedAttack = null;
            int availableAttackCount = 0;
            NightshadeSpearAttackState repeatedAttack = null;

            for (int index = 0; index < attackStates.Length; index++)
            {
                NightshadeSpearAttackState attackState = attackStates[index];
                if (!CanStartAttackSequence(
                        attackState,
                        targetDistanceSquared,
                        targetFacingDot))
                {
                    continue;
                }

                if (attackState.AttackId == lastStartingAttackId)
                {
                    repeatedAttack = attackState;
                    continue;
                }

                availableAttackCount++;
                if (UnityEngine.Random.Range(0, availableAttackCount) == 0)
                {
                    selectedAttack = attackState;
                }
            }

            selectedAttack = selectedAttack ?? repeatedAttack;
            if (selectedAttack == null)
            {
                return false;
            }

            lastStartingAttackId = selectedAttack.AttackId;
            sequenceAttackCount = 1;
            ChangeToAttackState(selectedAttack);
            return true;
        }

        internal bool TryChangeToFollowUpState(
            NightshadeSpearAttackState completedAttack,
            bool wasDamageApplied)
        {
            if (completedAttack == null ||
                !CanContinueAttackSequence(
                    currentPhase,
                    sequenceAttackCount,
                    wasDamageApplied,
                    completedAttack.AttackId) ||
                !TryGetFollowUpAttackId(
                    completedAttack.AttackId,
                    out NightshadeSpearAttackId followUpAttackId))
            {
                return false;
            }

            sequenceAttackCount++;
            completedAttack.PrepareFollowUpTransition();
            ChangeToAttackState(GetAttackState(followUpAttackId));
            return true;
        }

        internal static bool CanContinueAttackSequence(
            int phase,
            int currentSequenceCount,
            bool wasDamageApplied,
            NightshadeSpearAttackId completedAttackId)
        {
            return currentSequenceCount > 0 &&
                currentSequenceCount < GetMaximumSequenceCount(phase) &&
                (phase >= 2 || wasDamageApplied) &&
                TryGetFollowUpAttackId(completedAttackId, out _);
        }

        internal bool CanStartAttackSequence(
            NightshadeSpearAttackState attackState,
            float targetDistanceSquared,
            float targetFacingDot)
        {
            return attackState != null &&
                attackState.IsFacingTarget(targetFacingDot) &&
                IsContextAttackDistanceAllowed(
                    attackState.AttackId,
                    currentPhase,
                    targetDistanceSquared);
        }

        internal static bool IsContextAttackDistanceAllowed(
            NightshadeSpearAttackId attackId,
            int phase,
            float targetDistanceSquared)
        {
            float minimumDistance;
            float maximumDistance;

            switch (attackId)
            {
                case NightshadeSpearAttackId.Attack01:
                case NightshadeSpearAttackId.Attack02:
                case NightshadeSpearAttackId.Attack03:
                    minimumDistance = 0f;
                    maximumDistance = 2.3f;
                    break;
                case NightshadeSpearAttackId.Attack05:
                    minimumDistance = 1f;
                    maximumDistance = 2.8f;
                    break;
                case NightshadeSpearAttackId.Attack04:
                    minimumDistance = 2f;
                    maximumDistance = 3f;
                    break;
                case NightshadeSpearAttackId.Attack09 when phase >= 2:
                    minimumDistance = 0f;
                    maximumDistance = 2.6f;
                    break;
                case NightshadeSpearAttackId.Attack10 when phase >= 2:
                    minimumDistance = 1.2f;
                    maximumDistance = 2.8f;
                    break;
                case NightshadeSpearAttackId.Attack11 when phase >= 2:
                    minimumDistance = 0f;
                    maximumDistance = 1.8f;
                    break;
                case NightshadeSpearAttackId.Attack07 when phase >= 2:
                    minimumDistance = 3f;
                    maximumDistance = 5f;
                    break;
                default:
                    return false;
            }

            float safeDistanceSquared = Mathf.Max(0f, targetDistanceSquared);
            return safeDistanceSquared >= minimumDistance * minimumDistance &&
                safeDistanceSquared <= maximumDistance * maximumDistance;
        }

        internal static bool TryGetFollowUpAttackId(
            NightshadeSpearAttackId attackId,
            out NightshadeSpearAttackId followUpAttackId)
        {
            switch (attackId)
            {
                case NightshadeSpearAttackId.Attack01:
                    followUpAttackId = NightshadeSpearAttackId.Attack06;
                    return true;
                case NightshadeSpearAttackId.Attack02:
                    followUpAttackId = NightshadeSpearAttackId.Attack03;
                    return true;
                case NightshadeSpearAttackId.Attack04:
                    followUpAttackId = NightshadeSpearAttackId.Attack05;
                    return true;
                case NightshadeSpearAttackId.Attack07:
                    followUpAttackId = NightshadeSpearAttackId.Attack08;
                    return true;
                case NightshadeSpearAttackId.Attack09:
                    followUpAttackId = NightshadeSpearAttackId.Attack10;
                    return true;
                case NightshadeSpearAttackId.Attack10:
                    followUpAttackId = NightshadeSpearAttackId.Attack13;
                    return true;
                case NightshadeSpearAttackId.Attack11:
                    followUpAttackId = NightshadeSpearAttackId.Attack12;
                    return true;
                default:
                    followUpAttackId = 0;
                    return false;
            }
        }

        internal void ChangeToIdleState()
        {
            if (!ReferenceEquals(currentState, deadState))
            {
                sequenceAttackCount = 0;
                ChangeState(idleState);
            }
        }

        internal void ChangeToChaseState()
        {
            if (!ReferenceEquals(currentState, deadState))
            {
                sequenceAttackCount = 0;
                ChangeState(chaseState);
            }
        }

        internal void ChangeToAttackState(
            NightshadeSpearAttackState nextAttackState)
        {
            currentAttackState = nextAttackState;
            if (ReferenceEquals(currentState, currentAttackState))
            {
                currentState.Exit();
                currentState.Enter();
                return;
            }

            ChangeState(currentAttackState);
        }

        internal void ChangeToAliveState()
        {
            if (!ReferenceEquals(currentState, deadState))
            {
                ChangeToChaseState();
            }
        }

        internal void PlayAttack(NightshadeSpearAttackState attackState)
        {
            animation.SetMovement(0f, 0f);
            animation.PlayAttack(
                attackState.AnimatorStateId,
                attackState.TransitionTime,
                attackState.AnimationSpeed *
                GetReadySpeedMultiplier());
            playAttackReadyCue?.Invoke(attackState.AttackNumber);
        }

        internal void BeginAttackHit(NightshadeSpearAttackState attackState)
        {
            animation.SetActionSpeed(
                attackState.AnimationSpeed *
                GetAttackSpeedMultiplier());
            playAttackHitCue?.Invoke(
                attackState.AttackNumber,
                IsStrongAttack(attackState.AttackId));
            startAttackHit?.Invoke(
                attackState.CurrentAttackDamage,
                attackState.AttackNumber);
        }

        internal float GetReadyDuration()
        {
            return currentPhase >= 2
                ? PhaseTwoReadyDuration
                : PhaseOneReadyDuration;
        }

        internal float GetSequenceRecoveryDuration()
        {
            return currentPhase >= 2
                ? PhaseTwoSequenceRecoveryDuration
                : PhaseOneSequenceRecoveryDuration;
        }

        internal bool WasAttackDamageApplied()
        {
            return wasAttackDamageApplied?.Invoke() ?? false;
        }

        internal void EndAttackHit()
        {
            endAttackHit?.Invoke();
        }

        internal void RequestRelease()
        {
            requestRelease?.Invoke();
        }

        internal bool TryGetCurrentActionTime(out float normalizedTime)
        {
            return animation.TryGetCurrentActionTime(out normalizedTime);
        }

        internal bool IsActionTransitioning()
        {
            return animation.IsActionTransitioning();
        }

        private void ChangeToHitStateInternal(Vector3 hitPosition)
        {
            EndAttackHit();
            sequenceAttackCount = 0;
            hitState.SetHitPosition(hitPosition);
            if (ReferenceEquals(currentState, hitState))
            {
                hitState.Restart();
                return;
            }

            ChangeState(hitState);
        }

        private float GetAttackSpeedMultiplier()
        {
            return currentPhase >= 2
                ? PhaseTwoAttackSpeedMultiplier
                : PhaseOneAttackSpeedMultiplier;
        }

        private float GetReadySpeedMultiplier()
        {
            return currentPhase >= 2
                ? PhaseTwoReadySpeedMultiplier
                : PhaseOneReadySpeedMultiplier;
        }

        internal static int GetMaximumSequenceCount(int phase)
        {
            return phase >= 2
                ? PhaseTwoMaximumSequenceCount
                : PhaseOneMaximumSequenceCount;
        }

        private NightshadeSpearAttackState GetAttackState(
            NightshadeSpearAttackId attackId)
        {
            int attackIndex = (int)attackId - 1;
            return attackIndex >= 0 && attackIndex < attackStates.Length
                ? attackStates[attackIndex]
                : null;
        }

        private static bool IsStrongAttack(
            NightshadeSpearAttackId attackId)
        {
            return attackId == NightshadeSpearAttackId.Attack07 ||
                attackId == NightshadeSpearAttackId.Attack08 ||
                attackId == NightshadeSpearAttackId.Attack10 ||
                attackId == NightshadeSpearAttackId.Attack13;
        }

        private void ChangeState(INightshadeSpearState nextState)
        {
            if (ReferenceEquals(currentState, nextState))
            {
                return;
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

    }
}
