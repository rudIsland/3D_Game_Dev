using UnityEngine;

namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    internal enum ZombieAttackType
    {
        Swing = 0,
        Kick = 1,
        UpDown = 2
    }

    // 공격 중에는 추격 회전을 멈추고 애니메이션이 끝나기를 기다린다.
    internal sealed class ZombieAttackState : IZombieState
    {
        private const float CloseAttackRangeRatioSquared = 0.36f;
        private const float CloseSwingWeight = 3f;
        private const float CloseKickWeight = 6f;
        private const float CloseUpDownWeight = 1f;
        private const float FarSwingWeight = 4f;
        private const float FarKickWeight = 1f;
        private const float FarUpDownWeight = 5f;

        private readonly ZombieAliveState aliveState;
        private readonly ZombieStateMachine stateMachine;
        private ZombieAttackType previousAttackType;
        private bool hasPreviousAttack;
        private bool animationEndedByEvent;

        public ZombieAttackState(
            ZombieAliveState aliveState,
            ZombieStateMachine stateMachine)
        {
            this.aliveState = aliveState;
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            StartAttack();
        }

        internal void Restart()
        {
            StartAttack();
        }

        private void StartAttack()
        {
            animationEndedByEvent = false;
            ZombieAttackType attackType = ChooseAttack();
            previousAttackType = attackType;
            hasPreviousAttack = true;
            stateMachine.PlayAttack(attackType);
        }

        public void Update(float deltaTime)
        {
            stateMachine.StayOnGround(deltaTime);

            if (animationEndedByEvent || IsAnimationComplete())
            {
                aliveState.FinishAttack();
            }
        }

        public void Exit()
        {
            stateMachine.EndAttackHit();
        }

        internal void ResetAttackHistory()
        {
            hasPreviousAttack = false;
        }

        internal void NotifyAnimationEnded()
        {
            animationEndedByEvent = true;
        }

        private bool IsAnimationComplete()
        {
            return stateMachine.TryGetCurrentAnimationTime(
                       out float normalizedTime) &&
                !stateMachine.IsAnimationTransitioning() &&
                normalizedTime >= 1f;
        }

        private ZombieAttackType ChooseAttack()
        {
            bool isClose =
                stateMachine.GetTargetDistanceSquared() <=
                stateMachine.AttackRangeSquared *
                CloseAttackRangeRatioSquared;

            float swingWeight =
                isClose ? CloseSwingWeight : FarSwingWeight;
            float kickWeight =
                isClose ? CloseKickWeight : FarKickWeight;
            float upDownWeight =
                isClose ? CloseUpDownWeight : FarUpDownWeight;

            if (hasPreviousAttack)
            {
                switch (previousAttackType)
                {
                    case ZombieAttackType.Kick:
                        kickWeight = 0f;
                        break;
                    case ZombieAttackType.UpDown:
                        upDownWeight = 0f;
                        break;
                    default:
                        swingWeight = 0f;
                        break;
                }
            }

            float randomWeight = Random.Range(
                0f,
                swingWeight + kickWeight + upDownWeight);

            if (randomWeight < swingWeight)
            {
                return ZombieAttackType.Swing;
            }

            randomWeight -= swingWeight;
            return randomWeight < kickWeight
                ? ZombieAttackType.Kick
                : ZombieAttackType.UpDown;
        }
    }
}
