using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.NightShade
{
    // 사망 애니메이션과 시체 유지 시간이 끝나면 풀 반환을 한 번 요청한다.
    internal sealed class NightShadeSwordDeadState : INightShadeSwordState
    {
        private readonly INightShadeSwordMovement movement;
        private readonly INightShadeSwordAnimation animation;
        private readonly NightShadeSwordSettings settings;
        private readonly NightShadeSwordFightMemory fightMemory;
        private readonly NightShadeSwordActions actions;

        private bool isAnimationFinished;
        private bool isReleaseRequested;
        private float remainingBodyKeepTime;

        internal NightShadeSwordDeadState(
            INightShadeSwordMovement movement,
            INightShadeSwordAnimation animation,
            NightShadeSwordSettings settings,
            NightShadeSwordFightMemory fightMemory,
            NightShadeSwordActions actions)
        {
            this.movement = movement;
            this.animation = animation;
            this.settings = settings;
            this.fightMemory = fightMemory;
            this.actions = actions;
        }

        public void Enter()
        {
            fightMemory.ClearCombo();
            isAnimationFinished = false;
            isReleaseRequested = false;
            remainingBodyKeepTime = 0f;
            animation.ResetAttackPlaybackSpeed();
            animation.PlayDead();
        }

        public NightShadeSwordStateId? Update(float deltaTime)
        {
            movement.StayOnGround(deltaTime);
            if (!isAnimationFinished)
            {
                if (!animation.TryGetRequestedAnimationTime(out float normalizedTime) ||
                    animation.IsTransitioning() ||
                    normalizedTime < 1f)
                {
                    return null;
                }

                isAnimationFinished = true;
                remainingBodyKeepTime = settings.DeadBodyKeepTime;
            }

            remainingBodyKeepTime -= deltaTime;
            if (!isReleaseRequested && remainingBodyKeepTime <= 0f)
            {
                isReleaseRequested = true;
                actions.RequestRelease();
            }

            return null;
        }

        public void Exit()
        {
        }
    }
}
