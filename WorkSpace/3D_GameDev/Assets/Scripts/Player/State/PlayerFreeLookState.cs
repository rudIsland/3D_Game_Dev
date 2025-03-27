using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

/*
 * �������� ����
 */
public class PlayerFreeLookState : PlayerBaseState
{
    private float _jumpTimeoutDelta;

    //private float _fallTimeoutDelta;

    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine) {}

    public override void Enter()
    {
        Debug.Log("FreeLook����");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed += stateMachine.OnTargetPressed;
        }
        _jumpTimeoutDelta = stateMachine.JumpTimeout; //���� �� �ð� �Ҵ�
    }

    public override void Exit()
    {
        Debug.Log("FreeLook������");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed -= stateMachine.OnTargetPressed;
        }
    }

    public override void Tick(float deltaTime)
    {

        // �߷�, ���� ó��
        JumpAndGravity();

        //���� üũ
        GroundedCheck();

        // �̵� ó��
        Move(CalculateMove(deltaTime), deltaTime);
        // �ִϸ��̼� ������Ʈ
        UpdateMoveAnimation(deltaTime);
    }

    public override void Target()
    {
        if (stateMachine.inputReader.isTarget) //Ÿ���õǸ� ������
        { 
            stateMachine.SwitchState(new PlayerTargetLookState(stateMachine));
            return;
        }
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        Vector2 input = stateMachine.inputReader.moveInput;
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        //  ī�޶� ���� ���� ���
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        //  ī�޶� ���� �Է� ���� ���
        Vector3 moveDirection = camForward * input.y + camRight * input.x;
        moveDirection.Normalize();

        //  "�ȱ�"�� ���� ȸ�� ����
        if (!stateMachine.inputReader.onSprint && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                deltaTime * stateMachine.rotateSpeed
            );
        }

        //  �̵� ����
        // - �ȱ��� ���� ī�޶� ����
        // - �޸����� �� ���� �ٶ󺸴� ����
        if (stateMachine.inputReader.onSprint)
        {
            return stateMachine.transform.forward;
        }
        else
        {
            return moveDirection;
        }
    }









    //�̵� �ִϸ��̼�
    private void UpdateMoveAnimation(float deltaTime)
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
            stateMachine.GroundedRadius, stateMachine.GroundLayers, QueryTriggerInteraction.Ignore);

        // �ִϸ��̼� �Ķ���� ������Ʈ
        if (stateMachine.animator != null)
        {
            stateMachine.animator.SetBool(stateMachine._animIDGrounded, stateMachine.Grounded);
        }
    }

    //������ �߷�
    private void JumpAndGravity()
    {
        if (stateMachine.Grounded)
        {

            if (stateMachine._hasAnimator)
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
                stateMachine.animator.SetBool(stateMachine._animIDFreeFall, false);
            }

            // ���� �ӵ� �ʱ�ȭ (���� �ӵ� ����)
            if (stateMachine.verticalVelocity < 0.0f)
            {
                stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f); // �ް��� ��ȭ ����
            }

            // ���� ��ٿ� ����
            if (_jumpTimeoutDelta > 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }

            // ���� ó��
            if (stateMachine.jump && _jumpTimeoutDelta <= 0.0f)
            {
                stateMachine.jump = false; // ���� �Է� �ʱ�ȭ
                _jumpTimeoutDelta = stateMachine.JumpTimeout; // ��ٿ� �ʱ�ȭ

                stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);
                if (stateMachine.animator)
                {
                    stateMachine.animator.SetBool(stateMachine._animIDJump, true);
                }
            }
        }
        else
        {
            // ���߿� �ִ� ��� �߷� ����
            if (stateMachine.verticalVelocity > stateMachine.terminalVelocity)
            {
                stateMachine.verticalVelocity += stateMachine.gravity * Time.deltaTime;
            }

            // �������� �ִ��� Ȯ���Ͽ� FreeFall �ִϸ��̼� Ȱ��ȭ
            if (!stateMachine.jump && stateMachine.verticalVelocity < 0.0f)
            {
                if (stateMachine._hasAnimator)
                {
                    stateMachine.animator.SetBool(stateMachine._animIDFreeFall, true);
                }
            }

            // ���� ��ٿ� �ʱ�ȭ
            _jumpTimeoutDelta = stateMachine.JumpTimeout;
        }

        // ĳ���� ���� �̵� ó��
        Vector3 verticalMovement = new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f);
        stateMachine.characterController.Move(verticalMovement * Time.deltaTime);
    }

    //����
    public override void Jump()
    {
        stateMachine.jump = true;
    }


}
