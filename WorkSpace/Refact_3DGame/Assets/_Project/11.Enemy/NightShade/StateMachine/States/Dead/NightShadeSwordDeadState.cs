using UnityEngine;

namespace NightShade
{
    // 사망 애니메이션과 시체 유지 시간이 끝나면 풀 반환을 한 번 요청한다.
    internal sealed class NightShadeSwordDeadState : INightShadeSwordState
    {
        private readonly NightShadeSwordBehaviorContext context;
        private readonly float deadBodyKeepTime;
        private readonly NightShadeSwordCombatOutput combatOutput;

        private bool isAnimationFinished;
        private bool isReleaseRequested;
        private float remainingBodyKeepTime;
        private float elapsedDeathTime;

        internal NightShadeSwordDeadState(
            NightShadeSwordBehaviorContext context,
            float deadBodyKeepTime,
            NightShadeSwordCombatOutput combatOutput)
        {
            this.context = context;
            this.deadBodyKeepTime = deadBodyKeepTime;
            this.combatOutput = combatOutput;
        }

        public void Enter()
        {
            isAnimationFinished = false;
            elapsedDeathTime = 0f;
            isReleaseRequested = false;
            remainingBodyKeepTime = 0f;
            context.Animation.ResetAttackPlaybackSpeed();
            context.Animation.PlayDead();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            context.Movement.StayOnGround(deltaTime);
            elapsedDeathTime += Mathf.Max(0f, deltaTime);
            if (!isAnimationFinished)
            {
                if (elapsedDeathTime < 12f &&
                    (!context.Animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                    context.Animation.IsTransitioning() ||
                    normalizedTime < 1f))
                {
                    return null;
                }

                isAnimationFinished = true;
                remainingBodyKeepTime = deadBodyKeepTime;
            }

            remainingBodyKeepTime -= deltaTime;
            if (!isReleaseRequested && remainingBodyKeepTime <= 0f)
            {
                isReleaseRequested = true;
                combatOutput.RequestRelease();
            }

            return null;
        }

        public void Exit()
        {
        }
    }
}
