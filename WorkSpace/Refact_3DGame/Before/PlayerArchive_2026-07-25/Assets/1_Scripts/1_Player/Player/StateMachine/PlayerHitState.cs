using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitState : PlayerBaseState
{

    public PlayerHitState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        Debug.Log("PlayerHitState 진입");
        stateMachine.isHitting = true;
        stateMachine.animator.SetBool(stateMachine._animIDHit, true);
    }

    public override void Tick(float deltaTime)
    {
        //애니메이션 함수에서 isHitting을 false로 바꿔준다.
        if (!stateMachine.isHitting)
        {
            stateMachine.SwitchState(stateMachine._FreeLookState);
        }
    }

    public override void Exit()
    {
        Debug.Log("PlayerHitState 나가기");
        stateMachine.animator.SetBool(stateMachine._animIDHit, false);
    }
}
