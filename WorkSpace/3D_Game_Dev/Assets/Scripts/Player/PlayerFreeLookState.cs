using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 자유시점 상태
 */
public class PlayerFreeLookState : PlayerBaseState
{
    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine)
    {

    }

    public override void Enter()
    {
        Debug.Log("FreeLook진입");
    }

    public override void Exit()
    {

        Debug.Log("FreeLook나가기");
    }

    public override void Tick(float deltaTime)
    {
        Debug.Log("FreeLook중...");
        Move(deltaTime); // 매 프레임 이동 처리
    }

}
