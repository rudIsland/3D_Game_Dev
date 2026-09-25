using UnityEngine;
using Core;

namespace Player
{
    // 피격 중에는 조작을 무시하고 공격 방향으로 밀린다.
    internal sealed class PlayerHitState : IPlayerState
    {
        private const float ControlReturnNormalizedTime = 0.9f;

        private readonly IPlayerLookStateChange lookStateChange;
        private readonly PlayerMovement movement;
        private readonly PlayerAttackHit attackHit;
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

        /// <summary>복귀 요청·이동·공격 종료와 피격 재생 설정을 연결한다.</summary>
        public PlayerHitState(
            IPlayerLookStateChange lookStateChange,
            PlayerMovement movement,
            PlayerAttackHit attackHit,
            PlayerAnimationController animationController,
            float pushDuration,
            AnimationCurve pushCurve,
            float guardBreakControlLockDuration)
        {
            this.lookStateChange = lookStateChange;
            this.movement = movement;
            this.attackHit = attackHit;
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

        /// <summary>밀림과 재생 시간을 갱신하고 제어 잠금이 끝난 프레임에 시점 복귀를 요청한다.</summary>
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
                lookStateChange.ChangeToLookState();
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

        // 공격 판정을 닫은 뒤 밀림과 피격 애니메이션을 처음부터 시작한다.
        internal void Restart()
        {
            attackHit.Close();
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

        // 밀림 곡선의 이번 프레임 거리에 기존 중력 이동을 합친다.
        private void ApplyHitMovement(float deltaTime)
        {
            elapsedPushTime = Mathf.Min(elapsedPushTime + Mathf.Max(0f, deltaTime), pushDuration);
            float normalizedTime = elapsedPushTime / pushDuration;
            float deltaDistance = pushMovement.EvaluateDeltaDistance(normalizedTime);
            movement.ApplyHitMovement(hitRequest.PushDirection * deltaDistance, deltaTime);
        }
    }
}
