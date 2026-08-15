using rudIsland.RPG3D.Player.Animations;
using rudIsland.RPG3D.Player.Movement;
using rudIsland.RPG3D.Player.Runtime.Hit;
using UnityEngine;

namespace rudIsland.RPG3D.Player.States.Hit
{
    // 피격 중에는 조작을 무시하고 공격 방향으로 밀린다.
    internal sealed class PlayerHitState : IPlayerState
    {
        private const float ControlReturnNormalizedTime = 0.9f;

        private readonly PlayerStateMachine stateMachine;
        private readonly PlayerAnimationController animationController;
        private readonly float pushDuration;
        private readonly AnimationCurve pushCurve;
        private readonly PlayerActionMovementCurve pushMovement;

        private PlayerHitRequest hitRequest;
        private float elapsedPushTime;

        public PlayerHitState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            float pushDuration,
            AnimationCurve pushCurve)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.pushDuration = Mathf.Max(0.01f, pushDuration);
            this.pushCurve = pushCurve;
            pushMovement = new PlayerActionMovementCurve();
        }

        public void Enter()
        {
            Restart();
        }

        public void Update(float deltaTime, PlayerStateInput input)
        {
            animationController.StopMove();
            ApplyHitMovement(deltaTime);

            if (animationController.TryGetHitTime(
                    out float normalizedTime) &&
                normalizedTime >= ControlReturnNormalizedTime)
            {
                stateMachine.ChangeToLookState();
            }
        }

        public void Exit()
        {
            elapsedPushTime = 0f;
            pushMovement.Reset();
        }

        internal void Restart()
        {
            stateMachine.EndAttackHit();
            elapsedPushTime = 0f;
            pushMovement.Begin(
                hitRequest.PushDistance,
                pushCurve);
            animationController.PlayHitFromStart();
        }

        internal void SetHitRequest(
            in PlayerHitRequest nextHitRequest)
        {
            hitRequest = nextHitRequest;
        }

        private void ApplyHitMovement(float deltaTime)
        {
            elapsedPushTime = Mathf.Min(
                elapsedPushTime + Mathf.Max(0f, deltaTime),
                pushDuration);
            float normalizedTime = elapsedPushTime / pushDuration;
            float deltaDistance =
                pushMovement.EvaluateDeltaDistance(normalizedTime);
            stateMachine.Movement.ApplyHitMovement(
                hitRequest.PushDirection * deltaDistance,
                deltaTime);
        }
    }
}
