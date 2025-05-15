using UnityEngine;

public class PlayerDeadState : PlayerBaseState
{
    public PlayerDeadState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("DeadState진입");
        stateMachine.animator.applyRootMotion = true;
        stateMachine.animator.SetTrigger(stateMachine._animIDDead);
    }

    public override void Exit()
    {
        Debug.Log("DeadState나가기");

    }

    public override void Tick(float deltaTime)
    {
 
    }

    public void Dead()
    {

    }
}
