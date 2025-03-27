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
 * 자유시점 상태
 */
public class PlayerFreeLookState : PlayerBaseState
{
    private float _jumpTimeoutDelta;

    //private float _fallTimeoutDelta;

    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine) {}

    public override void Enter()
    {
        Debug.Log("FreeLook진입");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed += stateMachine.OnTargetPressed;
        }
        _jumpTimeoutDelta = stateMachine.JumpTimeout; //점프 텀 시간 할당
    }

    public override void Exit()
    {
        Debug.Log("FreeLook나가기");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed -= stateMachine.OnTargetPressed;
        }
    }

    public override void Tick(float deltaTime)
    {

        // 중력, 점프 처리
        JumpAndGravity();

        //지면 체크
        GroundedCheck();

        // 이동 처리
        Move(CalculateMove(deltaTime), deltaTime);
        // 애니메이션 업데이트
        UpdateMoveAnimation(deltaTime);
    }

    public override void Target()
    {
        if (stateMachine.inputReader.isTarget) //타겟팅되면 나가기
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

        //  카메라 기준 방향 계산
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        //  카메라 기준 입력 방향 계산
        Vector3 moveDirection = camForward * input.y + camRight * input.x;
        moveDirection.Normalize();

        //  "걷기"일 때만 회전 적용
        if (!stateMachine.inputReader.onSprint && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                deltaTime * stateMachine.rotateSpeed
            );
        }

        //  이동 방향
        // - 걷기일 때는 카메라 기준
        // - 달리기일 땐 현재 바라보는 방향
        if (stateMachine.inputReader.onSprint)
        {
            return stateMachine.transform.forward;
        }
        else
        {
            return moveDirection;
        }
    }









    //이동 애니메이션
    private void UpdateMoveAnimation(float deltaTime)
    {
        // 입력값 크기 가져오기
        float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

        // 목표 속도 계산
        float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;

        // _animationBlend를 목표 속도로 부드럽게 변경
        stateMachine._animationBlend = Mathf.Lerp(
            stateMachine._animationBlend,
            inputMagnitude > 0 ? targetSpeed : 0f,
            deltaTime * stateMachine.SpeedChangeRate
        );

        // 부동소수점 문제 방지 (작은 값은 0으로 설정)
        if (Mathf.Abs(stateMachine._animationBlend) < 0.01f)
        {
            stateMachine._animationBlend = 0f;
        }

        // MotionSpeed 계산 (아날로그 여부 확인)
        float motionSpeed = stateMachine.inputReader.isMove ? inputMagnitude : (inputMagnitude > 0 ? 1f : 0f);

        if(motionSpeed > 1f) motionSpeed = 1f;

        // 애니메이터에 값 적용
        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, stateMachine._animationBlend);
        stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, motionSpeed);
    }

    //지면인지 확인
    private void GroundedCheck()
    {
        // 충돌 검사 위치 계산
        Vector3 spherePosition = new Vector3(
            stateMachine.transform.position.x,
            stateMachine.transform.position.y - stateMachine.GroundedOffset,
            stateMachine.transform.position.z
        );

        // Physics.CheckSphere를 사용하여 지면 감지
        stateMachine.Grounded = Physics.CheckSphere(spherePosition, 
            stateMachine.GroundedRadius, stateMachine.GroundLayers, QueryTriggerInteraction.Ignore);

        // 애니메이션 파라미터 업데이트
        if (stateMachine.animator != null)
        {
            stateMachine.animator.SetBool(stateMachine._animIDGrounded, stateMachine.Grounded);
        }
    }

    //점프와 중력
    private void JumpAndGravity()
    {
        if (stateMachine.Grounded)
        {

            if (stateMachine._hasAnimator)
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
                stateMachine.animator.SetBool(stateMachine._animIDFreeFall, false);
            }

            // 수직 속도 초기화 (낙하 속도 제한)
            if (stateMachine.verticalVelocity < 0.0f)
            {
                stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f); // 급격한 변화 방지
            }

            // 점프 쿨다운 적용
            if (_jumpTimeoutDelta > 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }

            // 점프 처리
            if (stateMachine.jump && _jumpTimeoutDelta <= 0.0f)
            {
                stateMachine.jump = false; // 점프 입력 초기화
                _jumpTimeoutDelta = stateMachine.JumpTimeout; // 쿨다운 초기화

                stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);
                if (stateMachine.animator)
                {
                    stateMachine.animator.SetBool(stateMachine._animIDJump, true);
                }
            }
        }
        else
        {
            // 공중에 있는 경우 중력 적용
            if (stateMachine.verticalVelocity > stateMachine.terminalVelocity)
            {
                stateMachine.verticalVelocity += stateMachine.gravity * Time.deltaTime;
            }

            // 떨어지고 있는지 확인하여 FreeFall 애니메이션 활성화
            if (!stateMachine.jump && stateMachine.verticalVelocity < 0.0f)
            {
                if (stateMachine._hasAnimator)
                {
                    stateMachine.animator.SetBool(stateMachine._animIDFreeFall, true);
                }
            }

            // 점프 쿨다운 초기화
            _jumpTimeoutDelta = stateMachine.JumpTimeout;
        }

        // 캐릭터 수직 이동 처리
        Vector3 verticalMovement = new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f);
        stateMachine.characterController.Move(verticalMovement * Time.deltaTime);
    }

    //점프
    public override void Jump()
    {
        stateMachine.jump = true;
    }


}
