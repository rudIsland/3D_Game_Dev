using UnityEngine;
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

    public override void Enter() { } //���� �Լ�

    public override void Tick(float deltaTime) { } //���� �Լ�

    public override void Exit() { } //���� �Լ�

    // �̵� ���� ����
    protected void Move(Vector3 moveDirect, float deltaTime)
    {
        if (moveDirect.magnitude > 0.1f) //�̵��� �������� ����
        {
            if (stateMachine.inputReader.onSprint) //�޸���
            {
                stateMachine.characterController.Move((moveDirect * stateMachine.sprintSpeed) * deltaTime);
            }
            else //�ȱ�
            {
                stateMachine.characterController.Move((moveDirect * stateMachine.moveSpeed) * deltaTime);
            }
        }
    }

    public virtual void Jump() {   }

  
}
