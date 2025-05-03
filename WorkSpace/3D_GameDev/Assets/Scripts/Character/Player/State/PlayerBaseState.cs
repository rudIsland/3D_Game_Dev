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

    public override void Enter() { } //진입 함수

    public override void Tick(float deltaTime) { } //갱신 함수

    public override void Exit() { } //종료 함수

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

  
}
