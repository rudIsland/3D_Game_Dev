
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

    // ������ �ִ� ���� ó��
    protected void Move(float deltaTime)
    {
        Move(Vector3.zero, deltaTime);
    }

    // �̵� ���� ����
    protected void Move(Vector3 motion, float deltaTime)
    {
        if (motion.magnitude > 0.1f) //�̵��� �������� ����
        {
            if (stateMachine.inputReader.onSprint) //�޸���
            {
                stateMachine.characterController.Move((motion * stateMachine.sprintSpeed) * deltaTime);
            }
            else //�ȱ�
            {
                stateMachine.characterController.Move((motion * stateMachine.moveSpeed) * deltaTime);
            }
        }
    }

    public virtual void Jump()
    {   
     
    }

    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Tick(float deltaTime)
    {
        // �Է°� ������� �̵� ���� ���
        Vector3 movement = CalculateMove();

        // �̵� ���� ȣ��
        if (movement.magnitude > 0.1f)
            Move(movement, deltaTime); // �Է��� ���� ���� �̵�


        // ���� ���� Ȯ��
        //GroundedCheck();
    }

    private Vector3 CalculateMove()
    {
        // �Է°� ��������
        Vector2 input = stateMachine.inputReader.moveInput; //input�� vector2�� x y���� ������.

        // �Է°��� ������ ���� ���·� ����
        if (input == Vector2.zero) return Vector3.zero;

        // �Է°��� XZ ��鿡�� �̵� ���ͷ� ��ȯ
        return new Vector3(input.x, 0, input.y).normalized; //y���� z������ �� vector3�� �����Ͽ� ����
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(stateMachine.transform.position.x, stateMachine.transform.position.y - stateMachine.GroundedOffset,
            stateMachine.transform.position.z);
        stateMachine.Grounded = Physics.CheckSphere(spherePosition, stateMachine.GroundedRadius, stateMachine.GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (stateMachine.animator)
        {
            stateMachine.animator.SetBool(stateMachine._animIDGrounded, stateMachine.Grounded);
        }
    }
}
