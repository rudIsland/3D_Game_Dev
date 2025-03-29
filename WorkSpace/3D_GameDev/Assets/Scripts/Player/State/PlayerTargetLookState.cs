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
        Debug.Log("TargetLook진입");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed += onTargetPressed;
        }
        _jumpTimeoutDelta = stateMachine.JumpTimeout; //점프 텀 시간 할당


        stateMachine.animator.Play(TARGET_LOOK_BLEND_TREE);
    }

    public override void Exit()
    {
        Debug.Log("TargetLook나가기");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed -= onTargetPressed;
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

    private Vector3 CalculateMove(float deltaTime)
    {
        // 입력값 가져오기 (PlayerInputReader에서 입력값 받기)
        Vector2 input = stateMachine.inputReader.moveInput;

        // 수직 속도 적용
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

        if (direction.magnitude < 0.1f) return Vector3.zero;

        //회전 계산
        Quaternion targetRotation = Quaternion.LookRotation(direction); //회전할 목표 방향을 쿼터니언으로 짐벌락 방지
        //Lerp로 부드럽게 회전 시키도록
        stateMachine.transform.rotation = Quaternion.Lerp(
            stateMachine.transform.rotation, //현재 위치
            targetRotation, //목표 위치
            stateMachine.rotateSpeed * deltaTime * 1f //시간 10f*0.06*1f = 0.16f (60fps 기준 0.16씩 회전)
        );


        // 입력값을 XZ 평면에서 월드 좌표로 변환
        return direction;
    }


    //이동 애니메이션
    private void UpdateMoveAnimation(float deltaTime)
    {

        //플레이어 입력(y값) 앞 뒤 입력 X
        if (stateMachine.inputReader.moveInput.y == 0)
        {
            stateMachine.animator.SetFloat(TARGET_LOOK_FOWARD, 0f);
        }
        else
        {
            float value = stateMachine.inputReader.moveInput.y > 0 ? 1f : -1f;
            stateMachine.animator.SetFloat(TARGET_LOOK_FOWARD, value);
        }

        //플레이어 입력(x값) 오른쪽, 왼쪽 입력 X
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
