using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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
        if (stateMachine.characterController == null)
        {
            return;
        }
   
        Vector3 inputDirection = new Vector3(stateMachine.joystick.Horizontal, 0f, stateMachine.joystick.Vertical); // 조이스틱 입력
        if (inputDirection.magnitude >= 0.1f) // 입력이 있을 경우
        {
            // 입력 방향을 기준으로 캐릭터의 이동 방향 설정
            Vector3 moveDir = stateMachine.transform.right * inputDirection.x + stateMachine.transform.forward * inputDirection.z;
            Debug.Log("이동합니다.");
            // 실제 이동
            stateMachine.characterController.Move(moveDir * stateMachine.moveSpeed * deltaTime);

            // 캐릭터 회전
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                Quaternion.LookRotation(moveDir),
                stateMachine.rotateSpeed * deltaTime
            );
        }
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
