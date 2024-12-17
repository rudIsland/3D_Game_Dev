using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * �������� ����
 */
public class PlayerFreeLookState : PlayerBaseState
{
    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine)
    {

    }

    public override void Enter()
    {
        Debug.Log("FreeLook����");
    }

    public override void Exit()
    {

        Debug.Log("FreeLook������");
    }

    public override void Tick(float deltaTime)
    {
        Debug.Log("FreeLook��...");
        Move(deltaTime); // �� ������ �̵� ó��
    }

}
