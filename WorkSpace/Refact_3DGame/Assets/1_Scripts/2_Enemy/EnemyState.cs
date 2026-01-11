

public abstract class EnemyState : State
{
    protected EnemyStateMachine stateMachine;    // 현재 상태

    public EnemyState(EnemyStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public override void Enter() { }
    public override void Tick(float deltaTime) { }
    public override void Exit() { }

}
