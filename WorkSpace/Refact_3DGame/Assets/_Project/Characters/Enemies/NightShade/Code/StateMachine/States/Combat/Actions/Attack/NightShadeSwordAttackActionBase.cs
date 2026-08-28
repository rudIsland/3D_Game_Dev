using Characters.Combat;
using UnityEngine;

namespace Characters.Enemies.NightShade
{
    internal abstract class NightShadeSwordAttackActionBase :
        INightShadeSwordAttackAction
    {
        private const int EventQueueCapacity = 8;

        private enum AttackEventType : byte
        {
            StopTurn = 0,
            PlaySound = 1,
            OpenHit = 2,
            CloseHit = 3
        }

        private readonly struct PendingAttackEvent
        {
            internal AttackEventType Type { get; }
            internal int HitIndex { get; }

            internal PendingAttackEvent(AttackEventType type, int hitIndex)
            {
                Type = type;
                HitIndex = hitIndex;
            }
        }

        protected readonly NightShadeSwordBehaviorContext Context;
        protected readonly NightShadeSwordRuntimeAttackData AttackData;
        protected readonly NightShadeSwordCombatOutput CombatOutput;
        private readonly NightShadeSwordAttackSelectionRuntimeConfig attackSelection;

        private readonly PendingAttackEvent[] eventQueue =
            new PendingAttackEvent[EventQueueCapacity];
        private readonly AttackTargetCorrection targetCorrection =
            new AttackTargetCorrection();

        private int firstEventIndex;
        private int queuedEventCount;
        private int nextSoundHitIndex;
        private int nextOpenHitIndex;
        private int openHitIndex;
        private int currentDamageHitIndex;
        private bool canTurn;
        private bool isActive;

        protected NightShadeSwordAttackType CurrentAttackType { get; private set; }
        protected INightShadeSwordMovement Movement => Context.Movement;
        protected INightShadeSwordAnimation Animation => Context.Animation;
        protected NightShadeSwordTargetStatus TargetStatus => Context.TargetStatus;
        protected NightShadeSwordCombatMemory CombatMemory => Context.CombatMemory;

        public abstract NightShadeSwordActionId ActionId { get; }
        public bool IsFinished { get; protected set; }
        public virtual bool ProtectsSmallHit => false;

        protected NightShadeSwordAttackActionBase(
            NightShadeSwordBehaviorContext context,
            NightShadeSwordRuntimeAttackData attackData,
            NightShadeSwordAttackSelectionRuntimeConfig attackSelection,
            NightShadeSwordCombatOutput combatOutput)
        {
            Context = context;
            AttackData = attackData;
            this.attackSelection = attackSelection;
            CombatOutput = combatOutput;
        }

        protected abstract NightShadeSwordAttackType FirstAttackType { get; }

        public bool CanStart(
            out NightShadeSwordActionRejectReason rejectReason)
        {
            if (!TargetStatus.IsDetected)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetNotDetected;
                return false;
            }

            if (!TargetStatus.IsInsideAttackRange)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetOutsideAttackRange;
                return false;
            }

            if (!TargetStatus.IsFacingAttackDirection)
            {
                rejectReason = NightShadeSwordActionRejectReason.DirectionNotMatched;
                return false;
            }

            if (CombatMemory.RemainingPostAttackDelay > 0f)
            {
                rejectReason =
                    NightShadeSwordActionRejectReason.PostAttackDelayRemaining;
                return false;
            }

            rejectReason = NightShadeSwordActionRejectReason.None;
            return true;
        }

        public bool CanContinue(
            out NightShadeSwordActionStopReason stopReason)
        {
            stopReason = NightShadeSwordActionStopReason.None;
            return true;
        }

        public NightShadeSwordActionScore GetScore(float randomBonus)
        {
            NightShadeSwordAttackScoreSettings scoreSettings =
                AttackData.Score;
            float distanceFitness = 1f - Mathf.Clamp01(
                Mathf.Abs(
                    TargetStatus.AttackDistanceRatio -
                    scoreSettings.PreferredDistance) /
                scoreSettings.DistanceTolerance);
            float repeatPenalty = CombatMemory.HasPreviousAttack &&
                CombatMemory.PreviousAttack == ActionId
                    ? attackSelection.RepeatPenalty
                    : 0f;
            return new NightShadeSwordActionScore(
                scoreSettings.BaseScore,
                distanceFitness * attackSelection.DistanceScoreWeight,
                repeatPenalty,
                randomBonus);
        }

        public virtual void Enter()
        {
            IsFinished = false;
            CombatMemory.RecordAttack(ActionId);
            isActive = true;
            openHitIndex = -1;
            StartAttackClip(FirstAttackType);
        }

        public virtual void Update(float deltaTime)
        {
            ProcessQueuedEvents();
            UpdateAttackMovement(deltaTime);
            if (HasCurrentAnimationFinished(1f))
            {
                IsFinished = true;
            }
        }

        public virtual void Exit(NightShadeSwordActionStopReason stopReason)
        {
            isActive = false;
            ClearQueuedEvents();
            CloseOpenHit();
            Animation.ResetAttackPlaybackSpeed();
            targetCorrection.Reset();
            if (stopReason == NightShadeSwordActionStopReason.Completed)
            {
                CombatMemory.StartPostAttackDelay(
                    AttackData.PostAttackDelay);
            }
        }

        public void QueueStopTurn()
        {
            Enqueue(AttackEventType.StopTurn, -1);
        }

        public void QueuePlaySound(int hitIndex)
        {
            Enqueue(AttackEventType.PlaySound, hitIndex);
        }

        public void QueueOpenHit(int hitIndex)
        {
            Enqueue(AttackEventType.OpenHit, hitIndex);
        }

        public void QueueCloseHit()
        {
            Enqueue(AttackEventType.CloseHit, -1);
        }

        protected void StartAttackClip(NightShadeSwordAttackType attackType)
        {
            ClearQueuedEvents();
            CloseOpenHit();
            CurrentAttackType = attackType;
            nextSoundHitIndex = 0;
            nextOpenHitIndex = 0;
            openHitIndex = -1;
            currentDamageHitIndex = attackType ==
                NightShadeSwordAttackType.ComboSecond
                    ? 1
                    : 0;
            canTurn = true;
            targetCorrection.Reset();
            if (TargetStatus.IsAlive)
            {
                targetCorrection.Begin(
                    Movement.Position,
                    Movement.Forward,
                    true,
                    TargetStatus.TargetPosition,
                    AttackData.MoveDistance,
                    AttackData.TargetStopDistance,
                    AttackData.MaximumAddedMoveDistance,
                    AttackData.MaximumTurnAngle,
                    AttackData.MovementCurve);
            }

            Animation.ResetAttackPlaybackSpeed();
            Animation.PlayAttack(attackType);
        }

        protected void ProcessQueuedEvents()
        {
            while (queuedEventCount > 0)
            {
                PendingAttackEvent pendingEvent = eventQueue[firstEventIndex];
                firstEventIndex = (firstEventIndex + 1) % EventQueueCapacity;
                queuedEventCount--;

                switch (pendingEvent.Type)
                {
                    case AttackEventType.StopTurn:
                        canTurn = false;
                        break;
                    case AttackEventType.PlaySound:
                        ProcessSoundEvent(pendingEvent.HitIndex);
                        break;
                    case AttackEventType.OpenHit:
                        ProcessOpenHitEvent(pendingEvent.HitIndex);
                        break;
                    case AttackEventType.CloseHit:
                        CloseOpenHit();
                        break;
                }
            }
        }

        protected void UpdateAttackMovement(float deltaTime)
        {
            if (!Animation.TryGetRequestedAnimationTime(
                    out float normalizedTime))
            {
                Movement.StayOnGround(deltaTime);
                return;
            }

            if (targetCorrection.IsActive)
            {
                if (canTurn && TargetStatus.IsAlive)
                {
                    targetCorrection.UpdateTargetDirection(
                        Movement.Position,
                        TargetStatus.TargetPosition);
                }

                Movement.ApplyAttackMovement(
                    targetCorrection.TurnDirection,
                    canTurn,
                    targetCorrection.EvaluateDeltaDistance(normalizedTime),
                    deltaTime);
                return;
            }

            if (canTurn && TargetStatus.IsAlive)
            {
                Movement.TurnToTarget(TargetStatus.TargetPosition, deltaTime);
            }
            else
            {
                Movement.StayOnGround(deltaTime);
            }
        }

        protected bool HasCurrentAnimationFinished(float exitNormalizedTime)
        {
            return Animation.TryGetRequestedAnimationTime(
                    out float normalizedTime) &&
                !Animation.IsTransitioning() &&
                normalizedTime >= exitNormalizedTime;
        }

        protected void ClearQueuedEvents()
        {
            firstEventIndex = 0;
            queuedEventCount = 0;
        }

        protected void CloseOpenHit()
        {
            if (openHitIndex < 0)
            {
                return;
            }

            CombatOutput.CloseAttackHit();
            openHitIndex = -1;
        }

        private void Enqueue(AttackEventType eventType, int hitIndex)
        {
            if (!isActive || queuedEventCount >= EventQueueCapacity)
            {
                return;
            }

            int writeIndex =
                (firstEventIndex + queuedEventCount) % EventQueueCapacity;
            eventQueue[writeIndex] = new PendingAttackEvent(eventType, hitIndex);
            queuedEventCount++;
        }

        private void ProcessSoundEvent(int hitIndex)
        {
            if (hitIndex != nextSoundHitIndex || hitIndex >= 1)
            {
                return;
            }

            CombatOutput.PlayAttackSound(CurrentAttackType, hitIndex);
            nextSoundHitIndex++;
        }

        private void ProcessOpenHitEvent(int hitIndex)
        {
            if (hitIndex != nextOpenHitIndex || hitIndex >= 1)
            {
                return;
            }

            CloseOpenHit();
            openHitIndex = hitIndex;
            nextOpenHitIndex++;
            CombatOutput.OpenAttackHit(
                AttackData.GetHitDamage(currentDamageHitIndex));
        }
    }
}
