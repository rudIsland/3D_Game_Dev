using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

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
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
        }
    }

    public override void Exit()
    {
        Debug.Log("FreeLook������");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
        }
    }

    public override void Tick(float deltaTime)
    {
        // �̵� ó��
        Move(CalculateMove(deltaTime), deltaTime);

        // �߷�, ���� ó��
        JumpAndGravity(deltaTime);
        GroundedCheck();

        // �ִϸ��̼� ������Ʈ
        UpdateAnimation(deltaTime);
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        // �Է°� �������� (PlayerInputReader���� �Է°� �ޱ�)
        Vector2 input = stateMachine.inputReader.moveInput;

        // ���� �ӵ� ����
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

        if (direction.magnitude < 0.1f) return Vector3.zero;

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

    //private void UpdateAnimation(float deltaTime)
    //{
    //    // �̵� �Է��� ũ�� ��� (�ִϸ��̼� ��� �ӵ��� ���)
    //    float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

    //    // �̵� �ִϸ��̼� ó��
    //    if (inputMagnitude == 0)
    //    { //0f�� Idle
    //        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, 0f, stateMachine.animationDampTime, deltaTime);
    //    }
    //    else
    //    { //2��° ��ǥ���� �ȱ� or �޸��� �ӵ��� ������ ���
    //        float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;
    //        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, targetSpeed, stateMachine.animationDampTime, deltaTime);
    //    }

    //    //����
    //    if (stateMachine.Grounded && stateMachine.verticalVelocity < 0.0f)
    //    {
    //        stateMachine.animator.SetBool(stateMachine._animIDJump, false);
    //    }


    //    //�ִϸ��̼� ��� �ӵ� ����(�Ű����� StringtoHash���̵�, ��ǥ ��, ���ޱ��� �ɸ��� �ð�(���� �ð�), �������� ��Ÿ �ð�
    //    stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, inputMagnitude, stateMachine.animationDampTime, deltaTime);
    //}

    private void UpdateAnimation(float deltaTime)
    {
        // �Է°� ũ�� ��������
        float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

        // ��ǥ �ӵ� ���
        float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;

        // _animationBlend�� ��ǥ �ӵ��� �ε巴�� ����
        stateMachine._animationBlend = Mathf.Lerp(
            stateMachine._animationBlend,
            inputMagnitude > 0 ? targetSpeed : 0f,
            deltaTime * stateMachine.SpeedChangeRate
        );

        // �ε��Ҽ��� ���� ���� (���� ���� 0���� ����)
        if (Mathf.Abs(stateMachine._animationBlend) < 0.01f)
        {
            stateMachine._animationBlend = 0f;
        }

        // MotionSpeed ��� (�Ƴ��α� ���� Ȯ��)
        float motionSpeed = stateMachine.inputReader.isMove ? inputMagnitude : (inputMagnitude > 0 ? 1f : 0f);

        if(motionSpeed > 1f) motionSpeed = 1f;

        // �ִϸ����Ϳ� �� ����
        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, stateMachine._animationBlend);
        stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, motionSpeed);
    }

    //�������� Ȯ��
    private void GroundedCheck()
    {
        // �浹 �˻� ��ġ ���
        Vector3 spherePosition = new Vector3(
            stateMachine.transform.position.x,
            stateMachine.transform.position.y - stateMachine.GroundedOffset,
            stateMachine.transform.position.z
        );

        // Physics.CheckSphere�� ����Ͽ� ���� ����
        stateMachine.Grounded = Physics.CheckSphere(spherePosition, 
            stateMachine.GroundedRadius, stateMachine.GroundLayers);

        // �ִϸ��̼� �Ķ���� ������Ʈ
        if (stateMachine.animator != null)
        {
            stateMachine.animator.SetBool(stateMachine._animIDGrounded, stateMachine.Grounded);
        }

        // ���¸� ����׷� ���
        //Debug.Log($"IsGrounded: {stateMachine.Grounded}");
    }

    //�߷� ����
    private void JumpAndGravity(float deltaTime)
    {
        if (stateMachine.Grounded)
        {
            // ���� ���� ó��
            stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f);

            if (stateMachine.animator.GetBool(stateMachine._animIDJump)) // ���� ���̶��
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false); // ���� �ִϸ��̼� Ȱ��ȭ
                Debug.Log("���� �Ϸ�");
            }
        }
        else
        {
            // ���� ���� ó��
            stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

            if (!stateMachine.animator.GetBool(stateMachine._animIDJump)) // ���� ���¶��
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, true); // ���� �ִϸ��̼� Ȱ��ȭ
                Debug.Log("���� ��");
            }
        }

        // ���� �̵� ����
        Vector3 movement = new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f);
        stateMachine.characterController.Move(movement * deltaTime);
    }

    //����
    public override void Jump()
    {
        stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

        if (stateMachine.animator)
        {
            stateMachine.animator.SetBool(stateMachine._animIDJump, true); // ���� �ִϸ��̼� Ʈ����
        }

        Debug.Log("���� ����");
    }


}
