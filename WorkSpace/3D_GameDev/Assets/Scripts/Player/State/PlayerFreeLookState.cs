using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * �������� ����
 */
public class PlayerFreeLookState : PlayerBaseState
{
    private float _jumpTimeoutDelta;
    const string FREE_LOOK_BLEND_TREE = "FreeLookBlendTree";

    //private float _fallTimeoutDelta;

    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine) {}

    public override void Enter()
    {
        Debug.Log("FreeLook����");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed += onTargetPressed;
        }
        _jumpTimeoutDelta = stateMachine.JumpTimeout; //���� �� �ð� �Ҵ�


        stateMachine.animator.Play(FREE_LOOK_BLEND_TREE);
    }

    public override void Exit()
    {
        Debug.Log("FreeLook������");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed -= onTargetPressed;
        }
    }

    public override void Tick(float deltaTime)
    {
        //����
        stateMachine.Attacking();

        // �߷�, ���� ó��
        JumpAndGravity();

        //���� üũ
        GroundedCheck();

        // �̵� ó��
        Move(CalculateMove(deltaTime), deltaTime);
        // �̵� �ִϸ��̼� ������Ʈ
        UpdateMoveAnimation(deltaTime);

        //Other Animation Check
        OtherAnimaionCheck();

    }


    private void OtherAnimaionCheck()
    {
        // check hit anim
        if (stateMachine.animator.GetBool(stateMachine._animIDHit))
        {
            if (stateMachine.weapon.gameObject.activeSelf) //weapone is Enable.. not hit motion
            {
                stateMachine.animator.SetBool(stateMachine._animIDHit, false);
            }
        }
           
    }

    public void onTargetPressed()
    {
        if (!stateMachine.targeter.SelectTarget())
        {
            return;
        }

        stateMachine.SwitchState(new PlayerTargetLookState(stateMachine));
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        Vector2 input = stateMachine.inputReader.moveInput;
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        //  ī�޶� ���� ���� ���
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward; //���� z�� ���� ��(0,0,1) ��(0,0,-1) ����(1,0,0) ��(-1,0,0)�� ����
        Vector3 camRight = cam.right; //ī�޶� ���ؿ��� ������ ������ ����
        //ex) ��(0,0,1)�� ���������� �������� ��(1,0,0), ������ �������� ��(0,0,-1), ���������� ��(0,0,1)�� ����

        camForward.y = 0f;  camRight.y = 0f; //��������
        camForward.Normalize(); camRight.Normalize(); //����ȭ

        //  ī�޶� ���� �Է� ���� ���
        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        // If player is attacking, not move
        if (stateMachine.animator.GetBool(stateMachine._animIDAttack))
        {
            return Vector3.zero;
        }

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
        else if(stateMachine.inputReader.onSprint && input.sqrMagnitude > 0.1f) //�޸����߿� �Է¹�����
        {

            // ĳ���͸� moveDirection �������� ȸ��
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                deltaTime * stateMachine.rotateSpeed
            );

            // �� �������� �̵�
            return moveDirection;
        }

        //  �̵� ����
        if (stateMachine.inputReader.onSprint) //�޸����� �� ���� �ٶ󺸴� ����
        {
            return stateMachine.transform.forward;
        }
        else //�ȱ��� ���� ī�޶� ����
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
        stateMachine._ani_SpeedValue = Mathf.Lerp(
            stateMachine._ani_SpeedValue,
            inputMagnitude > 0 ? targetSpeed : 0f,
            deltaTime * stateMachine.SpeedChangeRate
        );

        // �ε��Ҽ��� ���� ���� (���� ���� 0���� ����)
        if (Mathf.Abs(stateMachine._ani_SpeedValue) < 0.01f)
        {
            stateMachine._ani_SpeedValue = 0f;
        }

        // MotionSpeed ��� (�Ƴ��α� ���� Ȯ��)
        float motionSpeed = stateMachine.inputReader.isMove ? inputMagnitude : (inputMagnitude > 0 ? 1f : 0f);

        if(motionSpeed > 1f) motionSpeed = 1f;

        // �ִϸ����Ϳ� �� ����
        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, stateMachine._ani_SpeedValue);
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
                stateMachine.inputReader.isJump = false;
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

            // ���� ó��(������ ������ ���� && ��������ð� && ������x)
            if (stateMachine.jump && _jumpTimeoutDelta <= 0.0f && !stateMachine.inputReader.isAttack)
            {
                stateMachine.jump = false; // ���� �Է� �ʱ�ȭ
                _jumpTimeoutDelta = stateMachine.JumpTimeout; // ��ٿ� �ʱ�ȭ

                stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);
                stateMachine.animator.SetBool(stateMachine._animIDJump, true);
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
