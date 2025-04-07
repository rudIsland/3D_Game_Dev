using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTargetLookState : PlayerBaseState
{
    private float _jumpTimeoutDelta;

    const string TARGET_LOOK_BLEND_TREE = "TargetLookBlendTree";
    const string TARGET_LOOK_RIGHT = "TargetingRight";
    const string TARGET_LOOK_FOWARD = "TargetingForward";

    public PlayerTargetLookState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public void onTargetPressed()
    {
        CanCel();
    }

    public void CanCel()
    {
        stateMachine.targeter.CanCel(); 

        stateMachine.SwitchState(new PlayerFreeLookState(stateMachine));
    }

    public override void Enter()
    {
        Debug.Log("TargetLook����");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.TargetPressed += onTargetPressed;
        }
        _jumpTimeoutDelta = stateMachine.JumpTimeout; //���� �� �ð� �Ҵ�


        stateMachine.animator.Play(TARGET_LOOK_BLEND_TREE);
    }

    public override void Exit()
    {
        Debug.Log("TargetLook������");
        // �Է� �̺�Ʈ ����
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.TargetPressed -= onTargetPressed;
        }
    }

    public override void Tick(float deltaTime)
    {
        //����
        stateMachine.StartAttack();

        // �߷�, ���� ó��
        JumpAndGravity();

        //���� üũ
        GroundedCheck();

        // �̵� ó��
        Move(CalculateMove(deltaTime), deltaTime);
        // �ִϸ��̼� ������Ʈ
        UpdateMoveAnimation(deltaTime);

        //Other Animation Check
        OtherAnimaionCheck();
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        Vector2 input = stateMachine.inputReader.moveInput;
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        if (input.magnitude < 0.1f) return Vector3.zero;

        // ī�޶��� ����/���� ���� �������� (Y�� ����)
        Transform cam = Camera.main.transform;
        Vector3 cameraForward = cam.forward;
        Vector3 cameraRight = cam.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // ī�޶� ���� �̵� ���� ���
        Vector3 direction = (cameraForward * input.y) + (cameraRight * input.x);
        direction.Normalize();

        // Ÿ�� ������ �ٶ󺸵��� ȸ�� ����
        if (stateMachine.targeter.currentTarget != null) // Ÿ���� ������ ���� ȸ��
        {
            Vector3 targetDirection = stateMachine.targeter.currentTarget.transform.position - stateMachine.transform.position;
            targetDirection.y = 0; // Y�� ȸ�� ����
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                stateMachine.transform.rotation = Quaternion.Slerp(
                    stateMachine.transform.rotation,
                    targetRotation,
                    deltaTime * stateMachine.rotateSpeed // ȸ�� �ӵ� ����
                );
            }
        }

        return direction;
    }



    //�̵� �ִϸ��̼�
    private void UpdateMoveAnimation(float deltaTime)
    {

        //�÷��̾� �Է�(y��) �� �� �Է� X
        if (stateMachine.inputReader.moveInput.y == 0)
        {
            stateMachine.animator.SetFloat(TARGET_LOOK_FOWARD, 0f);
        }
        else
        {
            float value = stateMachine.inputReader.moveInput.y > 0 ? 1f : -1f;
            stateMachine.animator.SetFloat(TARGET_LOOK_FOWARD, value);
        }

        //�÷��̾� �Է�(x��) ������, ���� �Է� X
        if (stateMachine.inputReader.moveInput.x == 0)
        {
            stateMachine.animator.SetFloat(TARGET_LOOK_RIGHT, 0f);
        }
        else
        {
            float value = stateMachine.inputReader.moveInput.x > 0 ? 1f : -1f;
            stateMachine.animator.SetFloat(TARGET_LOOK_RIGHT, value);
        }

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

    }

}
