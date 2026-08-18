namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 공격 선택, Animation Event 처리와 공격 후 상태 결정을 담당한다.
    internal sealed class NightShadeSwordAttackState : INightShadeSwordState
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

        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;
        private readonly NightShadeSwordAttackSelector attackSelector;
        private readonly NightShadeSwordActions actions;
        private readonly PendingAttackEvent[] eventQueue = new PendingAttackEvent[EventQueueCapacity];

        private NightShadeSwordAttackType attackType;
        private int firstEventIndex;
        private int queuedEventCount;
        private int nextSoundHitIndex;
        private int nextOpenHitIndex;
        private int openHitIndex;
        private bool canTurn;
        private bool isActive;

        internal NightShadeSwordAttackType AttackType => attackType;
        internal int QueuedEventCount => queuedEventCount;

        internal NightShadeSwordAttackState(
            NightShadeSwordTargetReader targetReader,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordFightMemory fightMemory,
            NightShadeSwordAttackSelector attackSelector,
            NightShadeSwordActions actions)
        {
            this.targetReader = targetReader;
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
            this.fightMemory = fightMemory;
            this.attackSelector = attackSelector;
            this.actions = actions;
        }

        public void Enter()
        {
            ClearQueuedEvents();
            bool playsReservedComboSecond =
                fightMemory.TakePendingComboSecond();
            attackType = playsReservedComboSecond
                ? NightShadeSwordAttackType.ComboSecond
                : attackSelector.Choose(targetReader.DistanceSquared, fightMemory);
            if (!playsReservedComboSecond)
            {
                fightMemory.RecordAttack(attackType);
            }
            nextSoundHitIndex = 0;
            nextOpenHitIndex = 0;
            openHitIndex = -1;
            canTurn = true;
            isActive = true;
            animation.ResetAttackPlaybackSpeed();
            animation.PlayAttack(attackType);
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            ProcessQueuedEvents();
            if (!animation.TryGetRequestedAnimationTime(out float normalizedTime))
            {
                movement.StayOnGround(deltaTime);
                return null;
            }

            if (canTurn)
            {
                movement.TurnTo(
                    targetReader.Position,
                    settings.AttackTurnSpeed,
                    deltaTime);
            }
            else
            {
                movement.StayOnGround(deltaTime);
            }

            float attackExitNormalizedTime =
                attackType == NightShadeSwordAttackType.ComboFirst
                    ? settings.ComboFirstExitNormalizedTime
                    : 1f;
            return !animation.IsTransitioning() &&
                normalizedTime >= attackExitNormalizedTime
                ? FinishAttack()
                : null;
        }

        public void Exit()
        {
            isActive = false;
            ClearQueuedEvents();
            CloseOpenHit();
            animation.ResetAttackPlaybackSpeed();
        }

        internal void QueueStopTurn()
        {
            Enqueue(AttackEventType.StopTurn, -1);
        }

        internal void QueuePlaySound(int hitIndex)
        {
            Enqueue(AttackEventType.PlaySound, hitIndex);
        }

        internal void QueueOpenHit(int hitIndex)
        {
            Enqueue(AttackEventType.OpenHit, hitIndex);
        }

        internal void QueueCloseHit()
        {
            Enqueue(AttackEventType.CloseHit, -1);
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

        private void ProcessQueuedEvents()
        {
            while (queuedEventCount > 0)
            {
                PendingAttackEvent pendingEvent = eventQueue[firstEventIndex];
                firstEventIndex =
                    (firstEventIndex + 1) % EventQueueCapacity;
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

        private void ProcessSoundEvent(int hitIndex)
        {
            if (hitIndex != nextSoundHitIndex ||
                hitIndex >= GetAttackHitCount())
            {
                return;
            }

            actions.PlayAttackSound(attackType, hitIndex);
            nextSoundHitIndex++;
        }

        private void ProcessOpenHitEvent(int hitIndex)
        {
            if (hitIndex != nextOpenHitIndex ||
                hitIndex >= GetAttackHitCount())
            {
                return;
            }

            CloseOpenHit();
            openHitIndex = hitIndex;
            nextOpenHitIndex++;
            actions.OpenAttackHit(attackType, hitIndex);
        }

        private void CloseOpenHit()
        {
            if (openHitIndex < 0)
            {
                return;
            }

            actions.CloseAttackHit();
            openHitIndex = -1;
        }

        private void ClearQueuedEvents()
        {
            firstEventIndex = 0;
            queuedEventCount = 0;
        }

        private int GetAttackHitCount()
        {
            return 1;
        }

        private NightShadeSwordStateId FinishAttack()
        {
            CloseOpenHit();
            if (attackType == NightShadeSwordAttackType.ComboFirst &&
                targetReader.IsFound(settings.FindRangeSquared))
            {
                fightMemory.ReserveComboSecond(settings.ComboSecondDelay);
                return NightShadeSwordStateId.Walk;
            }

            fightMemory.CompleteAttack(settings.GetAttackRecovery(attackType));

            if (!targetReader.IsFound(settings.FindRangeSquared))
            {
                return NightShadeSwordStateId.Idle;
            }

            return fightMemory.CompletedAttackCount >=
                    settings.AttacksBeforeCombatMove ||
                settings.IsVeryClose(targetReader.DistanceSquared)
                    ? NightShadeSwordStateId.CombatMove
                    : settings.GetApproachState(targetReader.DistanceSquared);
        }
    }
}
