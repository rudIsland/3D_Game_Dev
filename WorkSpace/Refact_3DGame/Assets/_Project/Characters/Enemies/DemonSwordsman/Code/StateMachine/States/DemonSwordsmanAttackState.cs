using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    // 선택된 공격의 준비, 활성, 회복과 종료를 담당한다.
    internal sealed class DemonSwordsmanAttackState :
        IDemonSwordsmanState
    {
        private readonly DemonSwordsmanStateMachine stateMachine; // 현재 행동 상태
        private static readonly string NoAttackName = string.Concat((char)0xC5C6, (char)0xC74C); // 공격 관련 설정 또는 상태
        private DemonSwordsmanAttackPattern currentAttack; // 공격 관련 설정 또는 상태
        private DemonSwordsmanAttackPattern followUpAttack; // 공격 관련 설정 또는 상태
        private bool branchWasRead; // 기능 사용 여부
        private bool finishRequested; // 기능 사용 여부
        private int attacksBeforeReposition; // 공격 관련 설정 또는 상태
        private int comboCount; // 개수 또는 크기

        public string Name => // 표시 이름
            nameof(DemonSwordsmanActionState.Attack);
        public string CurrentAttackName => // 현재 공격 이름
            currentAttack != null
                ? currentAttack.DisplayName
                : NoAttackName;

        internal DemonSwordsmanAttackState(
            DemonSwordsmanStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        internal void Reset()
        {
            currentAttack = null;
            followUpAttack = null;
            branchWasRead = false;
            finishRequested = false;
            attacksBeforeReposition = 0;
            comboCount = 0;
        }

        internal void Prepare(DemonSwordsmanAttackPattern attack)
        {
            currentAttack = attack;
            followUpAttack = null;
            comboCount = 1;
        }

        internal void PrepareFollowUp(DemonSwordsmanAttackPattern attack)
        {
            currentAttack = attack;
            followUpAttack = null;
            comboCount++;
        }

        public void Enter()
        {
            branchWasRead = false;
            finishRequested = false;
            stateMachine.Movement.SetAttackRootMove(
                true,
                currentAttack.RootMoveMultiplier);
            stateMachine.Animation.PlayAttack(currentAttack);
        }

        public void Update(float deltaTime)
        {
            if (currentAttack == null)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Approach);
                return;
            }

            if (stateMachine.Target.HasTarget &&
                stateMachine.StateTime < currentAttack.WarningTime)
            {
                stateMachine.Movement.TurnTo(
                    stateMachine.Target.Position,
                    stateMachine.GetCurrentTurnSpeed() * 0.45f,
                    deltaTime);
            }

            stateMachine.Movement.StayOnGround(deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);

            if (finishRequested ||
                stateMachine.StateTime >= GetAttackTotalTime())
            {
                FinishAttack();
            }
        }

        public void Exit()
        {
            stateMachine.Movement.SetAttackRootMove(false, 1f);
            stateMachine.Animation.SetAnimationSpeed(1f);
            currentAttack = null;
            finishRequested = false;
        }

        internal void OpenBranchWindow()
        {
            if (branchWasRead ||
                currentAttack == null ||
                !currentAttack.HasBranch)
            {
                return;
            }

            branchWasRead = true;

            if (!stateMachine.TryGetTargetInfo(
                    out Vector3 targetPosition,
                    out float distance,
                    out float signedAngle))
            {
                return;
            }

            float middleDistance =
                (currentAttack.MinimumDistance +
                currentAttack.MaximumDistance) * 0.5f;
            DemonSwordsmanAttackKind nextKind;

            if (distance > middleDistance)
            {
                nextKind = currentAttack.FarBranchKind;
            }
            else if (Mathf.Abs(signedAngle) > 45f)
            {
                nextKind = currentAttack.CloseBranchKind;
            }
            else
            {
                nextKind = currentAttack.CloseBranchKind;
            }

            followUpAttack = stateMachine.FindAttack(nextKind);
        }

        internal void FinishFromAnimation()
        {
            if (currentAttack == null)
            {
                return;
            }

            float earliestFinish =
                currentAttack.WarningTime +
                currentAttack.ActiveTime;
            if (stateMachine.StateTime >= earliestFinish)
            {
                finishRequested = true;
            }
        }

        private void FinishAttack()
        {
            int maximumCombo = stateMachine.Phase ==
                DemonSwordsmanPhase.PhaseTwo
                    ? 3
                    : 2;
            if (followUpAttack != null &&
                comboCount < maximumCombo)
            {
                DemonSwordsmanAttackPattern nextAttack = followUpAttack;
                followUpAttack = null;
                stateMachine.ChangeToFollowUp(nextAttack);
                return;
            }

            attacksBeforeReposition++;

            if (stateMachine.Phase == DemonSwordsmanPhase.PhaseTwo &&
                stateMachine.UseStyleAction() <= 0)
            {
                stateMachine.ChangeToStyleChange();
                return;
            }

            if (attacksBeforeReposition >= 2)
            {
                attacksBeforeReposition = 0;
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Circle);
                return;
            }

            stateMachine.ChangeToMove(
                DemonSwordsmanMoveMode.Approach);
        }

        private float GetAttackTotalTime()
        {
            return currentAttack.TotalTime;
        }
    }
}
