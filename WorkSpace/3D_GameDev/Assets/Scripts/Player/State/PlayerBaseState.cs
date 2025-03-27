
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

    public virtual void Target() { }

    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Tick(float deltaTime)
    {

        // �̵� ó��
        Move(CalculateMove(deltaTime), deltaTime);

        // �߷�, ���� ó��
        //JumpAndGravity(deltaTime);
        //GroundedCheck();

        // �ִϸ��̼� ������Ʈ
        //Debug.Log($"������Ʈ: {stateMachine.inputReader.onSprint}");
        UpdateAnimation(deltaTime);
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        // �Է°� �������� (PlayerInputReader���� �Է°� �ޱ�)
        Vector2 input = stateMachine.inputReader.moveInput;

        // ���� �ӵ� ����
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

        if (direction.magnitude < 0.1f)
        {
            return Vector3.zero;
        }
        

        //ȸ�� ���
        Quaternion targetRotation = Quaternion.LookRotation(direction); //ȸ���� ��ǥ ������ ���ʹϾ����� ������ ����
        //Lerp�� �ε巴�� ȸ�� ��Ű����
        stateMachine.transform.rotation = Quaternion.Lerp(
            stateMachine.transform.rotation, //���� ��ġ
            targetRotation, //��ǥ ��ġ
            stateMachine.rotateSpeed * deltaTime * 1f //�ð� 10f*0.06*1f = 0.16f (60fps ���� 0.16�� ȸ��)
        );


        // �Է°��� XZ ��鿡�� ���� ��ǥ�� ��ȯ
        return direction;
    }

    private void UpdateAnimation(float deltaTime)
    {
        // �̵� �Է��� ũ�� ��� (�ִϸ��̼� ��� �ӵ��� ���)
        float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

        // �̵� �ִϸ��̼� ó��
        if (inputMagnitude == 0)
        { //0f�� Idle
            stateMachine.animator.SetFloat(stateMachine._animIDSpeed, 0f, stateMachine.animationDampTime, deltaTime);
        }
        else
        { //2��° ��ǥ���� �ȱ� or �޸��� �ӵ��� ������ ���
            float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;
            stateMachine.animator.SetFloat(stateMachine._animIDSpeed, targetSpeed, stateMachine.animationDampTime, deltaTime);
        }

        //����
        if (stateMachine.Grounded && stateMachine.verticalVelocity < 0.0f)
        {
            stateMachine.animator.SetBool(stateMachine._animIDJump, false);
        }


        //�ִϸ��̼� ��� �ӵ� ����(�Ű����� StringtoHash���̵�, ��ǥ ��, ���ޱ��� �ɸ��� �ð�(���� �ð�), �������� ��Ÿ �ð�
        stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, inputMagnitude, stateMachine.animationDampTime, deltaTime);
    }

}
