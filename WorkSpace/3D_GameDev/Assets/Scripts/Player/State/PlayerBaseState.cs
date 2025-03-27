
using UnityEngine;


/*
 * 플레이어 기본 상태 정의
 * 새로 정의될 상태들에 기본적으로 적용될 로직들 구현
 * ex) 이동, 점프
 */
public class PlayerBaseState : State
{
    protected PlayerStateMachine stateMachine;
    
    public PlayerBaseState(PlayerStateMachine playerStateMachine)
    {
        this.stateMachine = playerStateMachine;
    }

    // 가만히 있는 상태 처리
    protected void Move(float deltaTime)
    {
        Move(Vector3.zero, deltaTime);
    }

    // 이동 로직 구현
    protected void Move(Vector3 moveDirect, float deltaTime)
    {
        if (moveDirect.magnitude > 0.1f) //이동이 있을때만 실행
        {
            if (stateMachine.inputReader.onSprint) //달리기
            {
                stateMachine.characterController.Move((moveDirect * stateMachine.sprintSpeed) * deltaTime);
            }
            else //걷기
            {
                stateMachine.characterController.Move((moveDirect * stateMachine.moveSpeed) * deltaTime);
            }
        }
    }

    public virtual void Jump() {   }

    public virtual void Target() { }

    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Tick(float deltaTime)
    {

        // 이동 처리
        Move(CalculateMove(deltaTime), deltaTime);

        // 중력, 점프 처리
        //JumpAndGravity(deltaTime);
        //GroundedCheck();

        // 애니메이션 업데이트
        //Debug.Log($"스프린트: {stateMachine.inputReader.onSprint}");
        UpdateAnimation(deltaTime);
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        // 입력값 가져오기 (PlayerInputReader에서 입력값 받기)
        Vector2 input = stateMachine.inputReader.moveInput;

        // 수직 속도 적용
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

        if (direction.magnitude < 0.1f)
        {
            return Vector3.zero;
        }
        

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
            stateMachine.animator.SetBool(stateMachine._animIDJump, false);
        }


        //애니메이션 재생 속도 제어(매개변수 StringtoHash아이디, 목표 값, 도달까지 걸리는 시간(감쇠 시간), 프레임의 델타 시간
        stateMachine.animator.SetFloat(stateMachine._animIDMotionSpeed, inputMagnitude, stateMachine.animationDampTime, deltaTime);
    }

}
