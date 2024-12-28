
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
    protected void Move(Vector3 motion, float deltaTime)
    {
        if (motion.magnitude > 0.1f) //이동이 있을때만 실행
        {
            if (stateMachine.inputReader.onSprint) //달리기
            {
                stateMachine.characterController.Move((motion * stateMachine.sprintSpeed) * deltaTime);
            }
            else //걷기
            {
                stateMachine.characterController.Move((motion * stateMachine.moveSpeed) * deltaTime);
            }
        }
    }

    public virtual void Jump()
    {   
     
    }

    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Tick(float deltaTime)
    {
        // 입력값 기반으로 이동 방향 계산
        Vector3 movement = CalculateMove();

        // 이동 로직 호출
        if (movement.magnitude > 0.1f)
            Move(movement, deltaTime); // 입력이 있을 때만 이동


        // 지면 상태 확인
        //GroundedCheck();
    }

    private Vector3 CalculateMove()
    {
        // 입력값 가져오기
        Vector2 input = stateMachine.inputReader.moveInput; //input의 vector2의 x y값을 가져옴.

        // 입력값이 없으면 정지 상태로 설정
        if (input == Vector2.zero) return Vector3.zero;

        // 입력값을 XZ 평면에서 이동 벡터로 변환
        return new Vector3(input.x, 0, input.y).normalized; //y값을 z값으로 새 vector3로 정의하여 리턴
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
}
