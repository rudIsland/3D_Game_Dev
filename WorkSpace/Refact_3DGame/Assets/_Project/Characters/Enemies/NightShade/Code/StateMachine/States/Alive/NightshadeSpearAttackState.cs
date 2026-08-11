using rudIsland.RPG3D.Characters.Combat.AttackData;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal enum NightshadeSpearAttackId
    {
        Attack01 = 1,
        Attack02,
        Attack03,
        Attack04,
        Attack05,
        Attack06,
        Attack07,
        Attack08,
        Attack09,
        Attack10,
        Attack11,
        Attack12,
        Attack13
    }

    internal enum NightshadeSpearAttackGroup
    {
        Thrust,
        Sweep,
        Heavy,
        Approach,
        Retreat,
        Finisher
    }

    internal enum AttackPhase
    {
        Ready,
        Hit,
        Recovery
    }

    // 모든 공격 State가 공유하는 실행 흐름이다.
    // 공격별 수치는 각 파생 State의 생성자에서 전달한다.
    internal abstract class NightshadeSpearAttackState : INightshadeSpearState
    {
        private const float RequiredTargetFacingAngle = 10f;
        private readonly NightshadeSpearStateMachine stateMachine;
        private readonly NightshadeSpearAttackId attackId;
        private readonly NightshadeSpearAttackGroup attackGroup;
        private readonly int animatorStateId;
        private readonly float maximumAttackDistanceSquared;
        private readonly float minimumTargetFacingDot;
        private readonly float hitStartTime;
        private readonly float hitEndTime;
        private readonly float transitionTime;
        private readonly float animationSpeed;
        private readonly bool canTurnDuringWindup;
        private readonly AttackDamage attackDamage;

        private AttackPhase currentPhase;
        private bool isHitOpen;
        private bool hasEnteredAttack;
        private bool isWaitingSequenceRecovery;
        private bool wasDamageApplied;
        private bool shouldResetActionOnExit;
        private float readyElapsed;
        private float recoveryRemaining;

        public string Name => GetType().Name;
        internal NightshadeSpearAttackId AttackId => attackId;
        internal int AttackNumber => (int)attackId;
        internal NightshadeSpearAttackGroup AttackGroup => attackGroup;
        internal int AnimatorStateId => animatorStateId;
        internal float MaximumAttackDistanceSquared => maximumAttackDistanceSquared;
        internal float HitStartTime => hitStartTime;
        internal float HitEndTime => hitEndTime;
        internal float TransitionTime => transitionTime;
        internal float AnimationSpeed => animationSpeed;
        internal bool CanTurnDuringWindup => canTurnDuringWindup;
        internal AttackDamage CurrentAttackDamage => attackDamage;
        internal AttackPhase CurrentPhase => currentPhase;

        internal bool IsFacingTarget(float targetFacingDot)
        {
            return targetFacingDot >= minimumTargetFacingDot;
        }

        protected NightshadeSpearAttackState(
            NightshadeSpearStateMachine stateMachine,
            NightshadeSpearAttackId attackId,
            NightshadeSpearAttackGroup attackGroup,
            string animatorStateName,
            float maximumAttackDistance,
            AttackDamage attackDamage,
            float hitStartTime,
            float hitEndTime,
            float transitionTime,
            float animationSpeed,
            bool canTurnDuringWindup)
        {
            this.stateMachine = stateMachine;
            this.attackId = attackId;
            this.attackGroup = attackGroup;
            animatorStateId = Animator.StringToHash(animatorStateName);
            maximumAttackDistanceSquared =
                Mathf.Max(0f, maximumAttackDistance) *
                Mathf.Max(0f, maximumAttackDistance);
            minimumTargetFacingDot = Mathf.Cos(
                RequiredTargetFacingAngle *
                Mathf.Deg2Rad);
            this.attackDamage = attackDamage;
            this.hitStartTime = Mathf.Clamp01(hitStartTime);
            this.hitEndTime = Mathf.Clamp(
                hitEndTime,
                this.hitStartTime,
                1f);
            this.transitionTime = Mathf.Max(0f, transitionTime);
            this.animationSpeed = Mathf.Max(0.01f, animationSpeed);
            this.canTurnDuringWindup = canTurnDuringWindup;
        }

        public void Enter()
        {
            currentPhase = AttackPhase.Ready;
            isHitOpen = false;
            hasEnteredAttack = false;
            isWaitingSequenceRecovery = false;
            wasDamageApplied = false;
            shouldResetActionOnExit = true;
            readyElapsed = 0f;
            recoveryRemaining = 0f;
            stateMachine.PlayAttack(this);
        }

        public void Update(float deltaTime)
        {
            switch (currentPhase)
            {
                case AttackPhase.Ready:
                    UpdateReady(deltaTime);
                    break;
                case AttackPhase.Hit:
                    UpdateHit(deltaTime);
                    break;
                default:
                    UpdateRecovery(deltaTime);
                    break;
            }
        }

        public void Exit()
        {
            FinishHitWindow();
            if (shouldResetActionOnExit)
            {
                stateMachine.Animation.ResetActionSpeed();
            }
        }

        private void UpdateReady(float deltaTime)
        {
            readyElapsed += deltaTime;
            stateMachine.StayOnGround(deltaTime);

            if (!stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime))
            {
                return;
            }

            hasEnteredAttack = true;
            if (readyElapsed < stateMachine.GetReadyDuration() ||
                normalizedTime < HitStartTime)
            {
                return;
            }

            currentPhase = AttackPhase.Hit;
            isHitOpen = true;
            stateMachine.BeginAttackHit(this);
        }

        private void UpdateHit(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);

            if (!stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime))
            {
                return;
            }

            if (normalizedTime > HitEndTime)
            {
                FinishHitWindow();
                currentPhase = AttackPhase.Recovery;
            }
        }

        private void UpdateRecovery(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);

            if (isWaitingSequenceRecovery)
            {
                recoveryRemaining -= deltaTime;
                if (recoveryRemaining <= 0f)
                {
                    FinishAttackSequence();
                }

                return;
            }

            if (!hasEnteredAttack ||
                stateMachine.IsActionTransitioning() ||
                !stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime) ||
                normalizedTime < 1f)
            {
                return;
            }

            FinishHitWindow();
            if (stateMachine.TryChangeToFollowUpState(
                    this,
                    wasDamageApplied))
            {
                return;
            }

            stateMachine.Animation.ResetActionSpeed();
            shouldResetActionOnExit = false;
            isWaitingSequenceRecovery = true;
            recoveryRemaining = stateMachine.GetSequenceRecoveryDuration();
            if (recoveryRemaining <= 0f)
            {
                FinishAttackSequence();
            }
        }

        internal void PrepareFollowUpTransition()
        {
            shouldResetActionOnExit = false;
        }

        private void FinishHitWindow()
        {
            if (!isHitOpen)
            {
                return;
            }

            isHitOpen = false;
            stateMachine.EndAttackHit();
            wasDamageApplied = stateMachine.WasAttackDamageApplied();
        }

        private void FinishAttackSequence()
        {
            if (!stateMachine.IsTargetFound())
            {
                stateMachine.ChangeToIdleState();
                return;
            }

            stateMachine.ChangeToChaseState();
        }
    }
}
