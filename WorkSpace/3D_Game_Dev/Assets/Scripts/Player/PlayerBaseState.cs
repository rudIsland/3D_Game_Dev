using Unity.Mathematics;
using UnityEngine;

/*
 * 플레이어 기본 상태 정의
 * 새로 정의될 상태들에 기본적으로 적용될 로직들 구현
 * ex) 이동, 점프
 */
public class PlayerBaseState : State
{
    protected PlayerStateMachine stateMachine;
    Vector3 moveVector; //플레이어 이동

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
        if (stateMachine.characterController == null)
        {
            return;
        }

        //조이스틱 입력을 받는다.
        float moveX = stateMachine.joystick.Horizontal;
        float moveZ = stateMachine.joystick.Vertical;
        
        //이동할 Vector값 계산
        moveVector = new Vector3(moveX, 0f, moveZ) * stateMachine.moveSpeed * deltaTime; // 조이스틱 입력

        //stateMachine.rigid.MovePosition(stateMachine.rigid.position + moveVector);
        stateMachine.characterController.Move(moveVector);

        Quaternion dirQuat = Quaternion.LookRotation(moveVector);
        Quaternion moveQuat = Quaternion.Slerp(stateMachine.rigid.rotation, dirQuat, stateMachine.rotateSpeed);
        stateMachine.rigid.MoveRotation(moveQuat);
        
    }



    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Tick(float deltaTime)
    {
        Move(deltaTime); // 매 프레임 이동 처리
    }
}
