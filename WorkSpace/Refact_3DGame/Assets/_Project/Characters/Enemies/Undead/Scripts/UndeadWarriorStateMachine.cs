namespace rudIsland.RPG3D.Characters.Enemies.Undead
{
    // Undead Warrior의 현재 상태와 상태 변경 순서를 관리한다.
    public sealed class UndeadWarriorStateMachine
    {
        private readonly IUndeadWarriorState idleState =
            new UndeadWarriorIdleState();

        private IUndeadWarriorState currentState;
        private bool isEnabled;

        public void Enable()
        {
            if (isEnabled)
            {
                return;
            }

            isEnabled = true;
            ChangeState(idleState);
        }

        public void Update(float deltaTime)
        {
            if (isEnabled && currentState != null)
            {
                currentState.Update(deltaTime);
            }
        }

        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            currentState?.Exit();
            currentState = null;
            isEnabled = false;
        }

        internal void ChangeState(IUndeadWarriorState nextState)
        {
            if (nextState == null ||
                ReferenceEquals(currentState, nextState))
            {
                return;
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        // 실제 행동이 추가되기 전 안전하게 기다리는 최초 상태다.
        private sealed class UndeadWarriorIdleState :
            IUndeadWarriorState
        {
            public void Enter()
            {
            }

            public void Update(float deltaTime)
            {
            }

            public void Exit()
            {
            }
        }
    }

    internal interface IUndeadWarriorState
    {
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }
}
