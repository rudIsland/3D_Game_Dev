using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

/*
 * �÷��̾� �⺻ ���� ����
 * ���� ���ǵ� ���µ鿡 �⺻������ ����� ������ ����
 * ex) �̵�, ����
 */
public class PlayerBaseState : State
{
    protected PlayerStateMachine stateMachine;

    public PlayerBaseState(PlayerStateMachine playerStateMachine)
    {
        this.stateMachine = playerStateMachine;
    }

    // ������ �ִ� ���� ó��
    protected void Move(float deltaTime)
    {
        Move(Vector3.zero, deltaTime);
    }

    // �̵� ���� ����
    protected void Move(Vector3 motion, float deltaTime)
    {
        if (stateMachine.characterController == null)
        {
            return;
        }
   
        Vector3 inputDirection = new Vector3(stateMachine.joystick.Horizontal, 0f, stateMachine.joystick.Vertical); // ���̽�ƽ �Է�
        if (inputDirection.magnitude >= 0.1f) // �Է��� ���� ���
        {
            // �Է� ������ �������� ĳ������ �̵� ���� ����
            Vector3 moveDir = stateMachine.transform.right * inputDirection.x + stateMachine.transform.forward * inputDirection.z;
            Debug.Log("�̵��մϴ�.");
            // ���� �̵�
            stateMachine.characterController.Move(moveDir * stateMachine.moveSpeed * deltaTime);

            // ĳ���� ȸ��
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                Quaternion.LookRotation(moveDir),
                stateMachine.rotateSpeed * deltaTime
            );
        }
    }



    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Tick(float deltaTime)
    {
        Move(deltaTime); // �� ������ �̵� ó��
    }
}
