using rudIsland.RPG3D.Characters.Combat;
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
        private readonly float guardBreakControlLockDuration;

        private PlayerHitRequest hitRequest;
        private HitReaction reaction;
        private float elapsedPushTime;
        private float elapsedStateTime;
        private float elapsedReactionTime;
        private float pushDistance;
        private bool isGuardBreak;
        private bool hasHitAnimationStarted;
        private bool canReturnControl;

        public PlayerHitState(
            PlayerStateMachine stateMachine,
            PlayerAnimationController animationController,
            float pushDuration,
            AnimationCurve pushCurve,
            float guardBreakControlLockDuration)
        {
            this.stateMachine = stateMachine;
            this.animationController = animationController;
            this.pushDuration = Mathf.Max(0.01f, pushDuration);
            this.pushCurve = pushCurve;
            this.guardBreakControlLockDuration = Mathf.Max(0f, guardBreakControlLockDuration);
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
            elapsedStateTime += Mathf.Max(0f, deltaTime);
            elapsedReactionTime += Mathf.Max(0f, deltaTime);
            UpdateControlReturnState();

            if (canReturnControl &&
                (!isGuardBreak ||
                 elapsedStateTime >= guardBreakControlLockDuration))
            {
                stateMachine.ChangeToLookState();
            }
        }

        public void Exit()
        {
            elapsedPushTime = 0f;
            elapsedStateTime = 0f;
            elapsedReactionTime = 0f;
            isGuardBreak = false;
            hasHitAnimationStarted = false;
            canReturnControl = false;
            pushMovement.Reset();
        }

        internal void Restart()
        {
            stateMachine.EndAttackHit();
            elapsedPushTime = 0f;
            elapsedStateTime = 0f;
            elapsedReactionTime = 0f;
            hasHitAnimationStarted = false;
            canReturnControl = false;
            pushMovement.Begin(pushDistance, pushCurve);
            animationController.PlayHitFromStart(reaction);
        }

        internal bool TryRestart(
            HitReaction nextReaction,
            in PlayerHitRequest nextHitRequest,
            bool nextIsGuardBreak)
        {
            if (!HitReactionPlayback.CanStart(
                    reaction,
                    nextReaction,
                    elapsedReactionTime))
            {
                return false;
            }

            SetHitRequest(
                nextReaction,
                in nextHitRequest,
                nextIsGuardBreak);
            Restart();
            return true;
        }

        internal void SetHitRequest(
            HitReaction nextReaction,
            in PlayerHitRequest nextHitRequest,
            bool nextIsGuardBreak)
        {
            reaction = nextReaction;
            hitRequest = nextHitRequest;
            isGuardBreak = nextIsGuardBreak;
            pushDistance = HitPushDistance.GetDistance(
                hitRequest.PushDistance,
                reaction);
        }

        private void UpdateControlReturnState()
        {
            if (animationController.TryGetHitTime(out float normalizedTime))
            {
                hasHitAnimationStarted = true;
                canReturnControl = normalizedTime >= ControlReturnNormalizedTime;
                return;
            }

            if (hasHitAnimationStarted)
            {
                canReturnControl = true;
            }
        }

        private void ApplyHitMovement(float deltaTime)
        {
            elapsedPushTime = Mathf.Min(elapsedPushTime + Mathf.Max(0f, deltaTime), pushDuration);
            float normalizedTime = elapsedPushTime / pushDuration;
            float deltaDistance = pushMovement.EvaluateDeltaDistance(normalizedTime);
            stateMachine.Movement.ApplyHitMovement(hitRequest.PushDirection * deltaDistance, deltaTime);
        }
    }
}
