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
            stateMachine.inputReader.TargetPressed -= onTargetPressed;
        }
    }

    public override void Tick(float deltaTime)
    {
        //공격
        stateMachine.StartAttack();

        // 중력, 점프 처리
        JumpAndGravity();

        //지면 체크
        GroundedCheck();

        // 이동 처리
        Move(CalculateMove(deltaTime), deltaTime);
        // 애니메이션 업데이트
        UpdateMoveAnimation(deltaTime);

        //Other Animation Check
        OtherAnimaionCheck();
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        Vector2 input = stateMachine.inputReader.moveInput;
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        if (input.magnitude < 0.1f) return Vector3.zero;

        // 카메라의 전방/우측 방향 가져오기 (Y축 제거)
        Transform cam = Camera.main.transform;
        Vector3 cameraForward = cam.forward;
        Vector3 cameraRight = cam.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 카메라 기준 이동 방향 계산
        Vector3 direction = (cameraForward * input.y) + (cameraRight * input.x);
        direction.Normalize();

        // 타겟 방향을 바라보도록 회전 적용
        if (stateMachine.targeter.currentTarget != null) // 타겟이 존재할 때만 회전
        {
            Vector3 targetDirection = stateMachine.targeter.currentTarget.transform.position - stateMachine.transform.position;
            targetDirection.y = 0; // Y축 회전 방지
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                stateMachine.transform.rotation = Quaternion.Slerp(
                    stateMachine.transform.rotation,
                    targetRotation,
                    deltaTime * stateMachine.rotateSpeed // 회전 속도 조절
                );
            }
        }

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
                stateMachine.inputReader.isJump = false;
            }

            // 수직 속도 초기화 (낙하 속도 제한)
            if (stateMachine.verticalVelocity < 0.0f)
            {
                stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f); // 급격한 변화 방지
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

    }

}
