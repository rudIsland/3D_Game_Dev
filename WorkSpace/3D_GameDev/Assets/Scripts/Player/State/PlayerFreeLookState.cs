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
        Debug.Log($"������Ʈ: {stateMachine.inputReader.onSprint}");
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
            // ���� �ִϸ��̼� ó��
            if (stateMachine.animator)
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
            }
        }


        //�ִϸ��̼� ��� �ӵ� ����(�Ű����� StringtoHash���̵�, ��ǥ ��, ���ޱ��� �ɸ��� �ð�(���� �ð�), �������� ��Ÿ �ð�
        stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, inputMagnitude, stateMachine.animationDampTime, deltaTime);
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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 spherePosition = new Vector3(stateMachine.transform.position.x, stateMachine.transform.position.y - stateMachine.GroundedOffset, stateMachine.transform.position.z);
        Gizmos.DrawWireSphere(spherePosition, stateMachine.GroundedRadius);
    }

    public override void Jump()
    {
        // ���� �ӵ� ���
        stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

        // ���� �ִϸ��̼� Ȱ��ȭ
        if (stateMachine.animator)
        {
            stateMachine.animator.SetBool(stateMachine._animIDJump, true);
        }

        Debug.Log("���� ����");
    }

    private void JumpAndGravity(float deltaTime)
    {
        if (stateMachine.Grounded)
        {
            // ���鿡 ���� �� �ӵ� �ʱ�ȭ
            stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f);

            // ���� �Է� ó��
            if (stateMachine.inputJump && stateMachine.jumpTimeout <= 0.0f)
            {
                stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

                if (stateMachine.animator)
                {
                    Debug.Log("FreeLook����!");
                    stateMachine.animator.SetBool(stateMachine._animIDJump, true);
                }
            }
        }
        else
        {
            // �߷� ����
            stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

            if (stateMachine.animator)
            {
                Debug.Log("FreeLook������.");
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
            }
        }

        // ���� �ӵ��� ĳ���� �̵��� �ݿ�
        stateMachine.characterController.Move(new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * deltaTime);

        // ��� �ð� ����
        if (stateMachine.jumpTimeout > 0.0f)
        {
            stateMachine.jumpTimeout -= deltaTime;
        }
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (stateMachine.FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, stateMachine.FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(stateMachine.FootstepAudioClips[index], stateMachine.transform.TransformPoint(stateMachine.characterController.center), stateMachine.FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(stateMachine.LandingAudioClip, stateMachine.transform.TransformPoint(stateMachine.characterController.center), stateMachine.FootstepAudioVolume);
        }
    }

}
