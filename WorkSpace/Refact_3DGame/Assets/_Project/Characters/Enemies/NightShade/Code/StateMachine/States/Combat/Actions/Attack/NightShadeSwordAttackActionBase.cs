using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal abstract class NightShadeSwordAttackActionBase :
        NightShadeSwordCombatActionBase,
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

        protected readonly INightShadeSwordMovement Movement;
        protected readonly INightShadeSwordAnimation Animation;
        protected readonly NightShadeSwordSettings Settings;
        protected readonly NightShadeSwordActions Actions;

        private readonly PendingAttackEvent[] eventQueue =
            new PendingAttackEvent[EventQueueCapacity];

        private int firstEventIndex;
        private int queuedEventCount;
        private int nextSoundHitIndex;
        private int nextOpenHitIndex;
        private int openHitIndex;
        private bool canTurn;
        private bool isActive;

        protected NightShadeSwordAttackType CurrentAttackType { get; private set; }

        public override NightShadeSwordCombatPhase Phase => NightShadeSwordCombatPhase.Attack;
        public virtual bool ProtectsSmallHit => false;
        public int QueuedEventCount => queuedEventCount;

        protected NightShadeSwordAttackActionBase(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordActions actions)
            : base(situation, fightMemory)
        {
            Movement = movement;
            Animation = animation;
            Settings = settings;
            Actions = actions;
        }

        protected abstract NightShadeSwordAttackType FirstAttackType { get; }

        public override bool CanStart(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            out NightShadeSwordActionRejectReason rejectReason)
        {
            if (!situation.IsTargetDetected)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetNotDetected;
                return false;
            }

            if (!situation.IsInsideAttackRange)
            {
                rejectReason = NightShadeSwordActionRejectReason.TargetOutsideAttackRange;
                return false;
            }

            if (!situation.IsFacingAttackDirection)
            {
                rejectReason = NightShadeSwordActionRejectReason.DirectionNotMatched;
                return false;
            }

            if (fightMemory.RemainingPostAttackDelay > 0f)
            {
                rejectReason =
                    NightShadeSwordActionRejectReason.PostAttackDelayRemaining;
                return false;
            }

            rejectReason = NightShadeSwordActionRejectReason.None;
            return true;
        }

        public override bool CanContinue(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            out NightShadeSwordActionStopReason stopReason)
        {
            stopReason = NightShadeSwordActionStopReason.None;
            return true;
        }

        public override NightShadeSwordActionScore GetScore(
            NightShadeSwordSituationReader situation,
            NightShadeSwordFightMemory fightMemory,
            float randomBonus)
        {
            NightShadeSwordAttackScoreSettings scoreSettings =
                Settings.GetAttackScoreSettings(ActionId);
            float distanceFitness = 1f - Mathf.Clamp01(
                Mathf.Abs(
                    situation.AttackDistanceRatio -
                    scoreSettings.PreferredDistance) /
                scoreSettings.DistanceTolerance);
            float repeatPenalty = fightMemory.HasPreviousAttack &&
                fightMemory.PreviousAttack == ActionId
                    ? Settings.AttackRepeatPenalty
                    : 0f;
            return new NightShadeSwordActionScore(
                scoreSettings.BaseScore,
                distanceFitness * Settings.AttackDistanceScoreWeight,
                repeatPenalty,
                randomBonus);
        }

        public override void Enter()
        {
            base.Enter();
            FightMemory.RecordAttack(ActionId);
            FightMemory.ClearCombo();
            isActive = true;
            openHitIndex = -1;
            StartAttackClip(FirstAttackType);
        }

        public override void Update(float deltaTime)
        {
            ProcessQueuedEvents();
            UpdateAttackMovement(deltaTime);
            if (HasCurrentAnimationFinished(1f))
            {
                IsFinished = true;
            }
        }

        public override void Exit(NightShadeSwordActionStopReason stopReason)
        {
            isActive = false;
            ClearQueuedEvents();
            CloseOpenHit();
            Animation.ResetAttackPlaybackSpeed();
            FightMemory.ClearCombo();
            if (stopReason == NightShadeSwordActionStopReason.Completed)
            {
                FightMemory.StartPostAttackDelay(
                    Settings.GetPostAttackDelay(ActionId));
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
            canTurn = true;
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
            if (!Animation.TryGetRequestedAnimationTime(out _))
            {
                Movement.StayOnGround(deltaTime);
                return;
            }

            if (canTurn && Situation.IsTargetAlive)
            {
                Movement.TurnTo(
                    Situation.TargetPosition,
                    Settings.AttackTurnSpeed,
                    deltaTime);
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

            Actions.CloseAttackHit();
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

            Actions.PlayAttackSound(CurrentAttackType, hitIndex);
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
            Actions.OpenAttackHit(CurrentAttackType, hitIndex);
        }
    }
}
