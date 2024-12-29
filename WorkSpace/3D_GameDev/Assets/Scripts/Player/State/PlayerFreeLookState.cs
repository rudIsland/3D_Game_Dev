using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

/*
 * 자유시점 상태
 */
public class PlayerFreeLookState : PlayerBaseState
{

    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine)
    {

    }

    public override void Enter()
    {
        Debug.Log("FreeLook진입");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
        }
    }

    public override void Exit()
    {
        Debug.Log("FreeLook나가기");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
        }
    }

    public override void Tick(float deltaTime)
    {
        // 이동 처리
        Move(CalculateMove(deltaTime), deltaTime);

        // 중력, 점프 처리
        JumpAndGravity(deltaTime);
        GroundedCheck();

        // 애니메이션 업데이트
        UpdateAnimation(deltaTime);
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

    //private void UpdateAnimation(float deltaTime)
    //{
    //    // 이동 입력의 크기 계산 (애니메이션 재생 속도에 사용)
    //    float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

    //    // 이동 애니메이션 처리
    //    if (inputMagnitude == 0)
    //    { //0f로 Idle
    //        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, 0f, stateMachine.animationDampTime, deltaTime);
    //    }
    //    else
    //    { //2번째 목표값을 걷기 or 달리기 속도로 지정후 재생
    //        float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;
    //        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, targetSpeed, stateMachine.animationDampTime, deltaTime);
    //    }

    //    //점프
    //    if (stateMachine.Grounded && stateMachine.verticalVelocity < 0.0f)
    //    {
    //        stateMachine.animator.SetBool(stateMachine._animIDJump, false);
    //    }


    //    //애니메이션 재생 속도 제어(매개변수 StringtoHash아이디, 목표 값, 도달까지 걸리는 시간(감쇠 시간), 프레임의 델타 시간
    //    stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, inputMagnitude, stateMachine.animationDampTime, deltaTime);
    //}

    private void UpdateAnimation(float deltaTime)
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
            stateMachine.GroundedRadius, stateMachine.GroundLayers);

        // 애니메이션 파라미터 업데이트
        if (stateMachine.animator != null)
        {
            stateMachine.animator.SetBool(stateMachine._animIDGrounded, stateMachine.Grounded);
        }

        // 상태를 디버그로 출력
        //Debug.Log($"IsGrounded: {stateMachine.Grounded}");
    }

    //중력 적용
    private void JumpAndGravity(float deltaTime)
    {
        if (stateMachine.Grounded)
        {
            // 착지 상태 처리
            stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f);

            if (stateMachine.animator.GetBool(stateMachine._animIDJump)) // 점프 중이라면
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false); // 착지 애니메이션 활성화
                Debug.Log("착지 완료");
            }
        }
        else
        {
            // 공중 상태 처리
            stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

            if (!stateMachine.animator.GetBool(stateMachine._animIDJump)) // 착지 상태라면
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, true); // 공중 애니메이션 활성화
                Debug.Log("점프 중");
            }
        }

        // 수직 이동 적용
        Vector3 movement = new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f);
        stateMachine.characterController.Move(movement * deltaTime);
    }

    //점프
    public override void Jump()
    {
        stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

        if (stateMachine.animator)
        {
            stateMachine.animator.SetBool(stateMachine._animIDJump, true); // 점프 애니메이션 트리거
        }

        Debug.Log("점프 실행");
    }


}
