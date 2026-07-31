namespace rudIsland.RPG3D.Characters.Enemies.Zombie
{
    // 살아 있는 동안 Idle, Alert, Chase, Attack, Hit 자식 상태를 관리한다.
    internal sealed class ZombieAliveState : IZombieState
    {
        private readonly ZombieStateMachine stateMachine;
        private readonly IdleState idleState;
        private readonly AlertState alertState;
        private readonly ChaseState chaseState;
        private readonly AttackState attackState;
        private readonly HitState hitState;

        private IZombieState currentChildState;

        public string Name =>
            currentChildState == null
                ? "Alive"
                : "Alive / " + currentChildState.Name;

        public ZombieAliveState(ZombieStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
            idleState = new IdleState(this, stateMachine);
            alertState = new AlertState(this, stateMachine);
            chaseState = new ChaseState(this, stateMachine);
            attackState = new AttackState(this, stateMachine);
            hitState = new HitState(this, stateMachine);
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

        public void PlayHit()
        {
            ChangeChildState(hitState);
        }

        private void ChooseDistanceState()
        {
            if (!stateMachine.IsTargetFound())
            {
                ChangeChildState(idleState);
                return;
            }

            ChangeChildState(
                stateMachine.IsTargetInAttackRange()
                    ? attackState
                    : chaseState);
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

        private sealed class IdleState : IZombieState
        {
            private readonly ZombieAliveState parent;
            private readonly ZombieStateMachine stateMachine;

            public string Name => "Idle";

            public IdleState(
                ZombieAliveState parent,
                ZombieStateMachine stateMachine)
            {
                this.parent = parent;
                this.stateMachine = stateMachine;
            }

            public void Enter()
            {
                stateMachine.SetMoveSpeed(0f);
            }

            public void Update(float deltaTime)
            {
                stateMachine.StayOnGround(deltaTime);

                if (stateMachine.IsTargetFound())
                {
                    parent.ChangeChildState(parent.alertState);
                }
            }

            public void Exit()
            {
            }
        }

        private sealed class AlertState : IZombieState
        {
            private readonly ZombieAliveState parent;
            private readonly ZombieStateMachine stateMachine;
            private float elapsedTime;

            public string Name => "Alert";

            public AlertState(
                ZombieAliveState parent,
                ZombieStateMachine stateMachine)
            {
                this.parent = parent;
                this.stateMachine = stateMachine;
            }

            public void Enter()
            {
                elapsedTime = 0f;
                stateMachine.SetMoveSpeed(0f);
                stateMachine.PlayScream();
            }

            public void Update(float deltaTime)
            {
                stateMachine.TurnToTarget(deltaTime);
                elapsedTime += deltaTime;

                if (elapsedTime >= stateMachine.AlertTime)
                {
                    parent.ChooseDistanceState();
                }
            }

            public void Exit()
            {
            }
        }

        private sealed class ChaseState : IZombieState
        {
            private readonly ZombieAliveState parent;
            private readonly ZombieStateMachine stateMachine;

            public string Name => "Chase";

            public ChaseState(
                ZombieAliveState parent,
                ZombieStateMachine stateMachine)
            {
                this.parent = parent;
                this.stateMachine = stateMachine;
            }

            public void Enter()
            {
                stateMachine.SetMoveSpeed(1f);
            }

            public void Update(float deltaTime)
            {
                if (!stateMachine.IsTargetFound())
                {
                    parent.ChangeChildState(parent.idleState);
                    return;
                }

                if (stateMachine.IsTargetInAttackRange())
                {
                    parent.ChangeChildState(parent.attackState);
                    return;
                }

                stateMachine.MoveToTarget(deltaTime);
            }

            public void Exit()
            {
                stateMachine.SetMoveSpeed(0f);
            }
        }

        private sealed class AttackState : IZombieState
        {
            private readonly ZombieAliveState parent;
            private readonly ZombieStateMachine stateMachine;
            private float elapsedTime;

            public string Name => "Attack";

            public AttackState(
                ZombieAliveState parent,
                ZombieStateMachine stateMachine)
            {
                this.parent = parent;
                this.stateMachine = stateMachine;
            }

            public void Enter()
            {
                elapsedTime = 0f;
                stateMachine.SetMoveSpeed(0f);
                stateMachine.PlayAttack();
            }

            public void Update(float deltaTime)
            {
                stateMachine.TurnToTarget(deltaTime);
                elapsedTime += deltaTime;

                if (elapsedTime < stateMachine.AttackInterval)
                {
                    return;
                }

                if (!stateMachine.IsTargetInAttackRange())
                {
                    parent.ChooseDistanceState();
                    return;
                }

                elapsedTime = 0f;
                stateMachine.PlayAttack();
            }

            public void Exit()
            {
            }
        }

        private sealed class HitState : IZombieState
        {
            private readonly ZombieAliveState parent;
            private readonly ZombieStateMachine stateMachine;
            private float elapsedTime;

            public string Name => "Hit";

            public HitState(
                ZombieAliveState parent,
                ZombieStateMachine stateMachine)
            {
                this.parent = parent;
                this.stateMachine = stateMachine;
            }

            public void Enter()
            {
                elapsedTime = 0f;
                stateMachine.SetMoveSpeed(0f);
                stateMachine.PlayHit();
            }

            public void Update(float deltaTime)
            {
                stateMachine.StayOnGround(deltaTime);
                elapsedTime += deltaTime;

                if (elapsedTime >= stateMachine.HitTime)
                {
                    parent.ChooseDistanceState();
                }
            }

            public void Exit()
            {
            }
        }
    }
}
