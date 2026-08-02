namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 살아 있는 동안 Idle, Alert, Chase, Attack 상태를 관리한다.
    internal sealed class ZombieAliveState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine;
        private readonly ZombieIdleState idleState;
        private readonly ZombieAlertState alertState;
        private readonly ZombieChaseState chaseState;
        private readonly ZombieAttackState attackState;

        private IZombieState currentChildState;
        private bool hasFoundTargetBefore;

        internal bool NeedsTargetUpdateEveryFrame =>
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
            ChangeChildState(alertState);
        }

        internal void ChangeToIdleAfterLostTarget()
        {
            hasFoundTargetBefore = false;
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
