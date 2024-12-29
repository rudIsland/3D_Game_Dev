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
        Debug.Log($"스프린트: {stateMachine.inputReader.onSprint}");
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

    private void UpdateAnimation(float deltaTime)
    {
        // 이동 입력의 크기 계산 (애니메이션 재생 속도에 사용)
        float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;
        
        // 이동 애니메이션 처리
        if (inputMagnitude == 0)
        { //0f로 Idle
            stateMachine.animator.SetFloat(stateMachine._animIDSpeed, 0f, stateMachine.animationDampTime, deltaTime);
        }
        else
        { //2번째 목표값을 걷기 or 달리기 속도로 지정후 재생
            float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;
            stateMachine.animator.SetFloat(stateMachine._animIDSpeed, targetSpeed, stateMachine.animationDampTime, deltaTime);
        }

        //점프
        if (stateMachine.Grounded && stateMachine.verticalVelocity < 0.0f)
        {
            // 착지 애니메이션 처리
            if (stateMachine.animator)
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
            }
        }


        //애니메이션 재생 속도 제어(매개변수 StringtoHash아이디, 목표 값, 도달까지 걸리는 시간(감쇠 시간), 프레임의 델타 시간
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
        // 점프 속도 계산
        stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

        // 점프 애니메이션 활성화
        if (stateMachine.animator)
        {
            stateMachine.animator.SetBool(stateMachine._animIDJump, true);
        }

        Debug.Log("점프 실행");
    }

    private void JumpAndGravity(float deltaTime)
    {
        if (stateMachine.Grounded)
        {
            // 지면에 있을 때 속도 초기화
            stateMachine.verticalVelocity = Mathf.Max(stateMachine.verticalVelocity, -2f);

            // 점프 입력 처리
            if (stateMachine.inputJump && stateMachine.jumpTimeout <= 0.0f)
            {
                stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

                if (stateMachine.animator)
                {
                    Debug.Log("FreeLook점프!");
                    stateMachine.animator.SetBool(stateMachine._animIDJump, true);
                }
            }
        }
        else
        {
            // 중력 적용
            stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

            if (stateMachine.animator)
            {
                Debug.Log("FreeLook점프끝.");
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
            }
        }

        // 수직 속도를 캐릭터 이동에 반영
        stateMachine.characterController.Move(new Vector3(0.0f, stateMachine.verticalVelocity, 0.0f) * deltaTime);

        // 대기 시간 감소
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
