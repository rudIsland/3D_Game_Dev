using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 공격의 준비, 타격 창, 회복과 다음 행동 선택을 담당한다.
    internal sealed class NightshadeSpearAttackState : INightshadeSpearState
    {
        private readonly NightshadeSpearStateMachine stateMachine;
        private NightshadeSpearAttackPattern currentAttack;
        private int currentAttackNumber;
        private bool isHitOpen;
        private bool hasHitWindowFinished;
        private bool hasEnteredAttack;
        private bool isRecovering;
        private float recoveryRemaining;

        public string Name => nameof(NightshadeSpearAttackState);
        internal string CurrentAttackName =>
            currentAttack != null ? currentAttack.DisplayName : string.Empty;

        internal NightshadeSpearAttackState(
            NightshadeSpearStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        internal void Prepare(
            NightshadeSpearAttackPattern attack,
            int attackNumber)
        {
            currentAttack = attack;
            currentAttackNumber = attackNumber;
        }

        public void Enter()
        {
            isHitOpen = false;
            hasHitWindowFinished = false;
            hasEnteredAttack = false;
            isRecovering = false;
            recoveryRemaining = 0f;
            stateMachine.PlayAttack(currentAttack, currentAttackNumber);
        }

        public void Update(float deltaTime)
        {
            if (currentAttack == null)
            {
                stateMachine.ChangeToChaseState();
                return;
            }

            if (isRecovering)
            {
                UpdateRecovery(deltaTime);
                return;
            }

            stateMachine.UpdateAttackHit(
                currentAttack,
                currentAttackNumber,
                ref isHitOpen,
                ref hasHitWindowFinished);

            bool movedDuringAttack = false;
            if (stateMachine.TryGetCurrentActionTime(
                    out float normalizedTime))
            {
                hasEnteredAttack = true;
                movedDuringAttack = UpdateAttackMovement(
                    deltaTime,
                    normalizedTime);
            }

            if (!movedDuringAttack)
            {
                stateMachine.StayOnGround(deltaTime);
            }

            if (!hasEnteredAttack ||
                stateMachine.IsActionTransitioning() ||
                !stateMachine.TryGetCurrentActionTime(
                    out normalizedTime) ||
                normalizedTime < 1f)
            {
                return;
            }

            FinishHitWindow();
            stateMachine.Animation.ResetActionSpeed();
            isRecovering = true;
            recoveryRemaining = currentAttack.RecoveryTime;
            if (recoveryRemaining <= 0f)
            {
                FinishAttack();
            }
        }

        public void Exit()
        {
            FinishHitWindow();
            currentAttack = null;
            stateMachine.Animation.ResetActionSpeed();
        }

        private bool UpdateAttackMovement(
            float deltaTime,
            float normalizedTime)
        {
            if (normalizedTime >= currentAttack.HitStartTime)
            {
                if (currentAttack.AttackGroup ==
                    NightshadeSpearAttackGroup.Retreat)
                {
                    stateMachine.MoveAwayFromTarget(deltaTime);
                    return true;
                }

                return false;
            }

            if (currentAttack.AttackGroup ==
                NightshadeSpearAttackGroup.Approach)
            {
                stateMachine.MoveToTarget(deltaTime);
                return true;
            }

            if (currentAttack.CanTurnDuringWindup)
            {
                stateMachine.TurnToTarget(deltaTime);
            }

            return false;
        }

        private void UpdateRecovery(float deltaTime)
        {
            if (currentAttack.AttackGroup ==
                NightshadeSpearAttackGroup.Retreat)
            {
                stateMachine.MoveAwayFromTarget(deltaTime);
            }
            else
            {
                stateMachine.StayOnGround(deltaTime);
            }

            recoveryRemaining -= deltaTime;
            if (recoveryRemaining <= 0f)
            {
                FinishAttack();
            }
        }

        private void FinishHitWindow()
        {
            if (!isHitOpen)
            {
                return;
            }

            isHitOpen = false;
            hasHitWindowFinished = true;
            stateMachine.EndAttackHit();
        }

        private void FinishAttack()
        {
            isRecovering = false;
            if (!stateMachine.IsTargetFound())
            {
                stateMachine.ChangeToIdleState();
                return;
            }

            NightshadeSpearAttackPattern nextAttack =
                stateMachine.ChooseAttack(out int nextAttackNumber);
            if (currentAttack.CanChain && nextAttack != null)
            {
                stateMachine.ChangeToAttackState(
                    nextAttack,
                    nextAttackNumber);
                return;
            }

            stateMachine.ChangeToChaseState();
        }
    }
}
