namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 가까운 대상에게 걷고 공격 거리 안에서는 멈춰서 다음 공격을 기다린다.
    internal sealed class NightShadeSwordWalkState : INightShadeSwordState
    {
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;

        private bool isWaitingInAttackRange;

        internal NightShadeSwordWalkState(
            NightShadeSwordTargetReader targetReader,
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordFightMemory fightMemory)
        {
            this.targetReader = targetReader;
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
            this.fightMemory = fightMemory;
        }

        public void Enter()
        {
            isWaitingInAttackRange =
                targetReader.DistanceSquared <= settings.AttackRangeSquared;
            if (isWaitingInAttackRange)
            {
                if (!fightMemory.HasPendingComboSecond)
                {
                    animation.PlayIdle();
                }
                return;
            }

            animation.PlayWalk();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            if (!targetReader.IsFound(settings.FindRangeSquared))
            {
                CompleteCancelledComboSecond();
                return NightShadeSwordStateId.Idle;
            }

            if (targetReader.DistanceSquared >= settings.RunStartRangeSquared)
            {
                return NightShadeSwordStateId.Chase;
            }

            bool isInAttackRange =
                targetReader.DistanceSquared <= settings.AttackRangeSquared;
            if (fightMemory.HasPendingComboSecond && !isInAttackRange)
            {
                CompleteCancelledComboSecond();
                if (fightMemory.CompletedAttackCount >=
                    settings.AttacksBeforeCombatMove)
                {
                    return NightShadeSwordStateId.CombatMove;
                }
            }

            if (isInAttackRange)
            {
                if (fightMemory.RemainingAttackCooldown <= 0f &&
                    movement.IsFacing(targetReader.Position, settings.AttackFacingDot))
                {
                    return NightShadeSwordStateId.Attack;
                }

                PlayIdleWhenAttackRangeEntered();
                movement.TurnTo(
                    targetReader.Position,
                    settings.TurnSpeed,
                    deltaTime);
                return null;
            }

            PlayWalkWhenAttackRangeExited();
            movement.MoveTo(
                targetReader.Position,
                settings.WalkSpeed,
                settings.TurnSpeed,
                deltaTime);
            return null;
        }

        public void Exit()
        {
        }

        private void CompleteCancelledComboSecond()
        {
            if (!fightMemory.HasPendingComboSecond)
            {
                return;
            }

            fightMemory.CancelComboSecond();
            fightMemory.CompleteAttack(
                settings.GetAttackRecovery(
                    NightShadeSwordAttackType.ComboFirst));
        }

        private void PlayIdleWhenAttackRangeEntered()
        {
            if (isWaitingInAttackRange)
            {
                return;
            }

            isWaitingInAttackRange = true;
            animation.PlayIdle();
        }

        private void PlayWalkWhenAttackRangeExited()
        {
            if (!isWaitingInAttackRange)
            {
                return;
            }

            isWaitingInAttackRange = false;
            animation.PlayWalk();
        }
    }
}
