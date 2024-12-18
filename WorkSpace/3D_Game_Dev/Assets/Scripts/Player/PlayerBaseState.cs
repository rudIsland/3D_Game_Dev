using Unity.Mathematics;
using UnityEngine;

/*
 * �÷��̾� �⺻ ���� ����
 * ���� ���ǵ� ���µ鿡 �⺻������ ����� ������ ����
 * ex) �̵�, ����
 */
public class PlayerBaseState : State
{
    protected PlayerStateMachine stateMachine;
    Vector3 moveVector; //�÷��̾� �̵�

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

        //���̽�ƽ �Է��� �޴´�.
        float moveX = stateMachine.joystick.Horizontal;
        float moveZ = stateMachine.joystick.Vertical;
        
        //�̵��� Vector�� ���
        moveVector = new Vector3(moveX, 0f, moveZ) * stateMachine.moveSpeed * deltaTime; // ���̽�ƽ �Է�

        //stateMachine.rigid.MovePosition(stateMachine.rigid.position + moveVector);
        stateMachine.characterController.Move(moveVector);

        Quaternion dirQuat = Quaternion.LookRotation(moveVector);
        Quaternion moveQuat = Quaternion.Slerp(stateMachine.rigid.rotation, dirQuat, stateMachine.rotateSpeed);
        stateMachine.rigid.MoveRotation(moveQuat);
        
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
