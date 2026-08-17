namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 살아 있는 동안 Idle, Alert, Chase, Attack 상태를 관리한다.
    internal sealed class ZombieAliveState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine; // 현재 행동 상태
        private readonly ZombieIdleState idleState; // 현재 행동 상태
        private readonly ZombieAlertState alertState; // 현재 행동 상태
        private readonly ZombieChaseState chaseState; // 현재 행동 상태
        private readonly ZombieAttackState attackState; // 공격 관련 설정 또는 상태

        private IZombieState currentChildState; // 현재 행동 상태
        private bool hasFoundTargetBefore; // 기능 사용 여부

        internal bool NeedsTargetUpdateEveryFrame => // 기능 사용 여부
            !ReferenceEquals(currentChildState, idleState);

        public ZombieAliveState(ZombieStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
            idleState = new ZombieIdleState(this, stateMachine);
            alertState = new ZombieAlertState(this, stateMachine);
            chaseState = new ZombieChaseState(this, stateMachine);
            attackState = new ZombieAttackState(this, stateMachine);
        }

        public void Enter()
        {
            ChangeChildState(idleState);
        }

        public void Update(float deltaTime)
        {
            currentChildState?.Update(deltaTime);
        }

        public void Exit()
        {
            currentChildState?.Exit();
            currentChildState = null;
        }

        internal void ChangeToAlert()
        {
            stateMachine.EnterCombat();
            ChangeChildState(alertState);
        }

        internal void ChangeToIdleAfterLostTarget()
        {
            hasFoundTargetBefore = false;
            stateMachine.ExitCombat();
            ChangeChildState(idleState);
        }

        internal void FinishAttack()
        {
            if (!stateMachine.IsTargetFound())
            {
                ChangeToIdleAfterLostTarget();
                return;
            }

            if (stateMachine.IsReadyToAttack())
            {
                attackState.Restart();
                return;
            }

            ChangeChildState(chaseState);
        }

        internal void ChangeToAttack()
        {
            ChangeChildState(attackState);
        }

        internal void ChooseIdleNextState()
        {
            if (!stateMachine.IsTargetFound())
            {
                stateMachine.ExitCombat();
                return;
            }

            if (!hasFoundTargetBefore)
            {
                ChangeToAlert();
                return;
            }

            ChooseDistanceState();
        }

        internal void FinishAlert()
        {
            hasFoundTargetBefore = true;
            ChooseDistanceState();
        }

        internal void NotifyAlertAnimationEnded()
        {
            alertState.NotifyAnimationEnded();
        }

        internal void NotifyAttackAnimationEnded()
        {
            if (ReferenceEquals(currentChildState, attackState))
            {
                attackState.NotifyAnimationEnded();
            }
        }

        internal bool BeginAttackHit()
        {
            return ReferenceEquals(currentChildState, attackState) &&
                attackState.BeginAttackHit();
        }

        internal bool BeginAttackRecovery()
        {
            return ReferenceEquals(currentChildState, attackState) &&
                attackState.BeginAttackRecovery();
        }

        internal void ChooseDistanceState()
        {
            if (!stateMachine.IsTargetFound())
            {
                ChangeToIdleAfterLostTarget();
                return;
            }

            ChangeChildState(
                stateMachine.IsReadyToAttack()
                    ? attackState
                    : chaseState);
        }

        internal void ResetTargetAwareness()
        {
            hasFoundTargetBefore = false;
            attackState.ResetAttackHistory();
        }

        private void ChangeChildState(IZombieState nextState)
        {
            if (ReferenceEquals(currentChildState, nextState))
            {
                return;
            }

            currentChildState?.Exit();
            currentChildState = nextState;
            currentChildState.Enter();
        }
    }
}
