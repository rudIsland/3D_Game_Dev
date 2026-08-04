using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Boss.DemonSwordsman
{
    internal enum DemonSwordsmanMoveMode
    {
        Idle,
        Notice,
        Approach,
        Circle,
        BackAway
    }

    // 목표를 찾고 거리를 조절하는 이동 행동을 담당한다.
    internal sealed class DemonSwordsmanMoveState : IDemonSwordsmanState
    {
        private const float TurnAnimationAngle = 65f; // 각도 설정
        private const float LocomotionFadeTime = 0.15f; // 시간 설정

        private readonly DemonSwordsmanStateMachine stateMachine; // 현재 행동 상태
        private DemonSwordsmanMoveMode currentMode; // 내부에서 사용하는 값
        private DemonSwordsmanMoveMode nextMode; // 내부에서 사용하는 값
        private float circleDirection; // 이동 정보

        public string Name => currentMode.ToString(); // 표시 이름

        internal DemonSwordsmanMoveState(
            DemonSwordsmanStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        internal void Reset()
        {
            currentMode = DemonSwordsmanMoveMode.Idle;
            nextMode = DemonSwordsmanMoveMode.Idle;
            circleDirection = 1f;
        }

        internal void Prepare(DemonSwordsmanMoveMode moveMode)
        {
            nextMode = moveMode;
        }

        public void Enter()
        {
            currentMode = nextMode;
            stateMachine.Movement.SetAttackRootMove(false, 1f);
            stateMachine.Animation.SetAnimationSpeed(1f);

            if (currentMode == DemonSwordsmanMoveMode.Circle)
            {
                circleDirection = -circleDirection;
            }

            if (currentMode != DemonSwordsmanMoveMode.Notice)
            {
                stateMachine.Animation.PlayLocomotion(
                    stateMachine.Style,
                    LocomotionFadeTime);
            }
        }

        public void Update(float deltaTime)
        {
            switch (currentMode)
            {
                case DemonSwordsmanMoveMode.Idle:
                    UpdateIdle(deltaTime);
                    break;
                case DemonSwordsmanMoveMode.Notice:
                    UpdateNotice(deltaTime);
                    break;
                case DemonSwordsmanMoveMode.Approach:
                    UpdateApproach(deltaTime);
                    break;
                case DemonSwordsmanMoveMode.Circle:
                    UpdateCircle(deltaTime);
                    break;
                case DemonSwordsmanMoveMode.BackAway:
                    UpdateBackAway(deltaTime);
                    break;
            }
        }

        public void Exit()
        {
        }

        private void UpdateIdle(float deltaTime)
        {
            stateMachine.Movement.StayOnGround(deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);

            if (stateMachine.TryGetTargetInfo(
                    out _,
                    out float distance,
                    out _) &&
                distance <= stateMachine.Settings.FindRange)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Notice);
            }
        }

        private void UpdateNotice(float deltaTime)
        {
            if (!stateMachine.TryGetTargetInfo(
                    out Vector3 targetPosition,
                    out float distance,
                    out float signedAngle) ||
                distance > stateMachine.Settings.FindRange)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Idle);
                return;
            }

            stateMachine.Movement.TurnTo(
                targetPosition,
                stateMachine.GetCurrentTurnSpeed(),
                deltaTime);
            stateMachine.Movement.StayOnGround(deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);

            if (stateMachine.Style == DemonSwordsmanStyle.Sword &&
                Mathf.Abs(signedAngle) >= TurnAnimationAngle)
            {
                stateMachine.Animation.PlayTurn(signedAngle < 0f);
            }
            else
            {
                stateMachine.Animation.PlayLocomotion(
                    stateMachine.Style,
                    LocomotionFadeTime);
            }

            if (stateMachine.StateTime >=
                stateMachine.Settings.NoticeTime)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Approach);
            }
        }

        private void UpdateApproach(float deltaTime)
        {
            if (!stateMachine.TryGetTargetInfo(
                    out Vector3 targetPosition,
                    out float distance,
                    out float signedAngle) ||
                distance > stateMachine.Settings.FindRange)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Idle);
                return;
            }

            if (distance < stateMachine.Settings.TooCloseDistance)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.BackAway);
                return;
            }

            if (stateMachine.TryChangeToAttack(
                    distance,
                    Mathf.Abs(signedAngle)))
            {
                return;
            }

            if (distance <=
                stateMachine.Settings.PreferredDistance + 0.75f)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Circle);
                return;
            }

            stateMachine.Animation.PlayLocomotion(
                stateMachine.Style,
                LocomotionFadeTime);
            stateMachine.Movement.MoveTo(
                targetPosition,
                stateMachine.GetCurrentMoveSpeed(),
                stateMachine.GetCurrentTurnSpeed(),
                deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);
        }

        private void UpdateCircle(float deltaTime)
        {
            if (!stateMachine.TryGetTargetInfo(
                    out Vector3 targetPosition,
                    out float distance,
                    out float signedAngle) ||
                distance > stateMachine.Settings.FindRange)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Idle);
                return;
            }

            if (distance < stateMachine.Settings.TooCloseDistance)
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.BackAway);
                return;
            }

            stateMachine.Animation.PlayLocomotion(
                stateMachine.Style,
                LocomotionFadeTime);
            stateMachine.Movement.CircleAround(
                targetPosition,
                stateMachine.Settings.CircleSpeed *
                    stateMachine.GetPhaseMoveMultiplier(),
                stateMachine.Settings.PreferredDistance,
                circleDirection,
                stateMachine.GetCurrentTurnSpeed(),
                deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);

            if (stateMachine.StateTime <
                stateMachine.Settings.CircleTime)
            {
                return;
            }

            if (!stateMachine.TryChangeToAttack(
                    distance,
                    Mathf.Abs(signedAngle)))
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Approach);
            }
        }

        private void UpdateBackAway(float deltaTime)
        {
            if (!stateMachine.TryGetTargetInfo(
                    out Vector3 targetPosition,
                    out float distance,
                    out float signedAngle))
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Idle);
                return;
            }

            stateMachine.Animation.PlayLocomotion(
                stateMachine.Style,
                LocomotionFadeTime);
            stateMachine.Movement.BackAwayFrom(
                targetPosition,
                stateMachine.Settings.BackAwaySpeed *
                    stateMachine.GetPhaseMoveMultiplier(),
                stateMachine.GetCurrentTurnSpeed(),
                deltaTime);
            stateMachine.UpdateMoveAnimation(deltaTime);

            if (stateMachine.StateTime <
                stateMachine.Settings.BackAwayTime)
            {
                return;
            }

            if (!stateMachine.TryChangeToAttack(
                    distance,
                    Mathf.Abs(signedAngle)))
            {
                stateMachine.ChangeToMove(
                    DemonSwordsmanMoveMode.Circle);
            }
        }
    }
}
