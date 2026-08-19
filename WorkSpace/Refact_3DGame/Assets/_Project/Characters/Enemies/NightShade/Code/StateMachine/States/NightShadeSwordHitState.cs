using rudIsland.RPG3D.Characters.Combat;
using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    internal enum NightShadeHitStep
    {
        Reaction = 0,
        StayDown = 1,
        GetUp = 2
    }

    // 경직 애니메이션 동안 피격 밀림 곡선을 누적 적용한다.
    internal sealed class NightShadeSwordHitState : INightShadeSwordState
    {
        private readonly NightShadeSwordTargetReader targetReader;
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;

        private EnemyHitRequest hitRequest;
        private HitReaction reaction;
        private NightShadeHitStep hitStep;
        private float elapsedPushTime;
        private float previousPushProgress;
        private float remainingDownTime;
        private float elapsedReactionTime;
        private float pushDistance;

        internal NightShadeSwordHitState(
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

        internal void SetHitRequest(
            HitReaction nextReaction,
            in EnemyHitRequest nextHitRequest)
        {
            reaction = nextReaction;
            hitRequest = nextHitRequest;
            pushDistance = HitPushDistance.GetDistance(
                hitRequest.PushDistance,
                reaction);
        }

        public void Enter()
        {
            fightMemory.CancelComboSecond();
            fightMemory.ResetCompletedAttackCount();
            StartReaction();
        }

        internal bool TryRestart(
            HitReaction nextReaction,
            in EnemyHitRequest nextHitRequest)
        {
            if (!HitReactionPlayback.CanStart(
                    reaction,
                    nextReaction,
                    elapsedReactionTime))
            {
                return false;
            }

            SetHitRequest(nextReaction, in nextHitRequest);
            StartReaction();
            return true;
        }

        private void StartReaction()
        {
            elapsedPushTime = 0f;
            previousPushProgress = settings.EvaluateHitPushProgress(0f);
            remainingDownTime = 0f;
            elapsedReactionTime = 0f;
            hitStep = NightShadeHitStep.Reaction;
            animation.ResetAttackPlaybackSpeed();
            PlayReactionAnimation();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            elapsedReactionTime += Mathf.Max(0f, deltaTime);
            ApplyHitMovement(deltaTime);
            if (hitStep == NightShadeHitStep.StayDown)
            {
                remainingDownTime -= Mathf.Max(0f, deltaTime);
                if (remainingDownTime > 0f)
                {
                    return null;
                }

                hitStep = NightShadeHitStep.GetUp;
                animation.PlayGetUpFromStart();
                return null;
            }

            if (!animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                animation.IsTransitioning() || normalizedTime < 1f)
            {
                return null;
            }

            if (reaction == HitReaction.Knockdown &&
                hitStep == NightShadeHitStep.Reaction)
            {
                hitStep = NightShadeHitStep.StayDown;
                remainingDownTime = settings.KnockdownStayDuration;
                return null;
            }

            return targetReader.IsFound(settings.FindRangeSquared)
                ? settings.GetApproachState(targetReader.DistanceSquared)
                : NightShadeSwordStateId.Idle;
        }

        public void Exit()
        {
        }

        private void PlayReactionAnimation()
        {
            switch (reaction)
            {
                case HitReaction.SmallHit:
                    animation.PlaySmallHitFromStart(
                        hitRequest.HitDirection);
                    break;
                case HitReaction.BigHit:
                    animation.PlayBigHitFromStart(
                        hitRequest.HitDirection);
                    break;
                case HitReaction.Knockback:
                    animation.PlayKnockbackFromStart();
                    break;
                case HitReaction.Knockdown:
                    animation.PlayKnockdownFromStart();
                    break;
            }
        }

        private void ApplyHitMovement(float deltaTime)
        {
            float pushDuration =
                settings.GetHitPushDuration(reaction);
            elapsedPushTime = Mathf.Min(
                elapsedPushTime + Mathf.Max(0f, deltaTime), pushDuration);
            float pushProgress = Mathf.Max(
                previousPushProgress,
                settings.EvaluateHitPushProgress( elapsedPushTime / pushDuration));
            float deltaProgress = pushProgress - previousPushProgress;
            Vector3 movementAmount = hitRequest.PushDirection *
                (pushDistance * deltaProgress);

            previousPushProgress = pushProgress;
            movement.ApplyHitMovement(movementAmount, deltaTime);
        }
    }
}
