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
    protected void Move(float deltaTime)
    {

        //float speed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;

        //Vector3 horizontal = stateMachine.horizontalDir * speed;           
        //Vector3 vertical = Vector3.up * stateMachine.verticalVelocity;

        //stateMachine.characterController.Move((horizontal + vertical) * deltaTime);

        //이전코드




        // 1. 실제 이동 중인지 확인
        bool isMoving = stateMachine.horizontalDir.sqrMagnitude > 0;

        // 2. 달리기 가능 조건 판단
        // (Shift 누름) AND (이동 중) AND (탈진 상태 아님) AND (스테미나 0보다 큼)
        bool isSprinting = stateMachine.inputReader.onSprint &&
                           isMoving &&
                           !stateMachine.isExhausted &&
                           stateMachine.currentStamina > 0;

        // 3. 속도 결정: 위 조건이 하나라도 안 맞으면 무조건 moveSpeed
        float speed = isSprinting ? stateMachine.sprintSpeed : stateMachine.moveSpeed;

        // 4. 스테미나 소모
        if (isSprinting)
        {
            stateMachine.UseStamina(stateMachine.sprintStaminaCost * deltaTime);
        }

        // 5. 실제 이동 적용
        Vector3 horizontal = stateMachine.horizontalDir * speed;
        Vector3 vertical = Vector3.up * stateMachine.verticalVelocity;
        stateMachine.characterController.Move((horizontal + vertical) * deltaTime);

        // 6. 애니메이션 파라미터 (스테미나가 없어서 속도가 줄어든 것도 반영됨)
        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, speed, stateMachine.animationDampTime, deltaTime);
    }

    private void HandleStamina(bool isSprinting, float deltaTime)
    {
        if (isSprinting)
        {
            // 달리는 중: 스테미나 감소
            stateMachine.currentStamina -= stateMachine.sprintStaminaCost * deltaTime;
            if (stateMachine.currentStamina < 0) stateMachine.currentStamina = 0;
        }
        else
        {
            // 걷거나 멈춤: 스테미나 회복 (최대치 제한)
            if (stateMachine.currentStamina < stateMachine.maxStamina)
            {
                stateMachine.currentStamina += stateMachine.staminaRegenRate * deltaTime;
                if (stateMachine.currentStamina > stateMachine.maxStamina)
                    stateMachine.currentStamina = stateMachine.maxStamina;
            }
        }
    }

}
