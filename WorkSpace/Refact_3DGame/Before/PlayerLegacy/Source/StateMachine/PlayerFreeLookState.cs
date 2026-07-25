
using UnityEngine;


/*
 * 자유시점 상태
 */
public class PlayerFreeLookState : PlayerBaseState
{

    // 문자열 대신 해시값 사용 (최적화)
    private readonly int _FREE_LOOK_BLEND_TREE = Animator.StringToHash("FreeLookBlendTree");
    private Transform _MainCameraTransform;

    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine) {}

    public override void Enter()
    {
        Debug.Log("FreeLook진입");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed += onTargetPressed;
        }

        // 카메라 캐싱
        if (Camera.main != null)
            _MainCameraTransform = Camera.main.transform;


        stateMachine.jumpTimeoutDelta = stateMachine.JumpTimeout;
        stateMachine.fallTimeoutDelta = stateMachine.FallTimeout;

        stateMachine.animator.Play(_FREE_LOOK_BLEND_TREE);
    }

    public override void Exit()
    {
        Debug.Log("FreeLook나가기");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed -= onTargetPressed;
        }
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.JumpTimeout > 0f)
            stateMachine.JumpTimeout -= Time.deltaTime;

        //공격
        stateMachine.StartAttack();

        //점프
        JumpAndGravity(deltaTime);
        GroundedCheck(); //지면 체크

        //이동
        if (!stateMachine.Attacking)
        {
            CalculateMove(deltaTime);
            Move(deltaTime);
        }

        //애니메이션
        UpdateAniSpeedParameter(deltaTime);             // 이동 애니메이션 업데이트
        //stateMachine.regenStamina();           //가만히 있으면 스테미나 회복 업데이트 

    }

    
    public void onTargetPressed()
    {
        if (!stateMachine.targeter.SelectTarget())
        {
            return;
        }

        stateMachine.SwitchState(stateMachine._TargetLookState);
    }

    private void CalculateMove(float deltaTime)
    {
        Vector2 input = stateMachine.inputReader.moveInput;

        // 입력 거의 없으면 정지
        if (input.sqrMagnitude < 0.01f)
        {
            stateMachine.horizontalDir = Vector3.zero;
            return;
        }

        // 캐싱된 카메라 트랜스폼 사용
        if (_MainCameraTransform == null) return;

        Vector3 camForward = _MainCameraTransform.forward;
        Vector3 camRight = _MainCameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection =
            (camForward * input.y + camRight * input.x).normalized;

        // 공격 중 이동 차단
        if (stateMachine.animator.GetBool(stateMachine._animIDAttack))
        {
            stateMachine.horizontalDir = Vector3.zero;
            return;
        }

        // 회전 처리
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                stateMachine.inputReader.onSprint
                    ? moveDirection        // 달리기: 입력 방향
                    : moveDirection        // 걷기: 동일하지만 구조 유지
            );

            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                deltaTime * stateMachine.rotateSpeed
            );
        }

        // 수평 이동 방향 결정
        if (stateMachine.inputReader.onSprint)
        {
            // 달리기: 바라보는 방향으로 고정
            stateMachine.horizontalDir = stateMachine.transform.forward;
        }
        else
        {
            // 걷기: 카메라 기준 이동
            stateMachine.horizontalDir = moveDirection;
        }
    }


    //이동 애니메이션
    private void UpdateAniSpeedParameter(float deltaTime)
    {
        // 입력값 크기 가져오기
        float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

        // 달리기 가능 조건 판단
        bool canSprint = stateMachine.inputReader.onSprint && inputMagnitude > 0 && !stateMachine.isExhausted;

        // 목표 속도 계산
        float targetSpeed;
        if (inputMagnitude > 0)
        {
            // 실제로 달릴 수 있다면 sprintSpeed, 아니면 moveSpeed
            targetSpeed = canSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;
        }
        else
        {
            targetSpeed = 0f; // 입력 없으면 정지
        }

        // _animationBlend를 목표 속도로 부드럽게 변경
        stateMachine._ani_SpeedValue = Mathf.Lerp(
            stateMachine._ani_SpeedValue,
            inputMagnitude > 0 ? targetSpeed : 0f,
            deltaTime * stateMachine.SpeedChangeRate
        );

        // 부동소수점 문제 방지 (작은 값은 0으로 설정)
        if (Mathf.Abs(stateMachine._ani_SpeedValue) < 0.01f)
        {
            stateMachine._ani_SpeedValue = 0f;
        }

        // 부동소수점 오차 보정
        if (Mathf.Abs(stateMachine._ani_SpeedValue) < 0.01f) stateMachine._ani_SpeedValue = 0f;

        // 5. 애니메이터에 값 적용
        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, stateMachine._ani_SpeedValue);

        // MotionSpeed 처리 (애니메이션 재생 속도 조절)
        float motionSpeed = inputMagnitude > 0 ? 1f : 0f;
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
            stateMachine.GroundedRadius, stateMachine.GroundLayers, QueryTriggerInteraction.Ignore);

        if (stateMachine.animator != null)
        {
            stateMachine.animator.SetBool(stateMachine._animIDGrounded, stateMachine.Grounded);
        }
    }

    ////////////////////////////////  점프와 중력  ////////////////////////////////
    private void JumpAndGravity(float deltaTime)
    {
        if (stateMachine.Grounded)
        {
            // 착지 시 fall 타이머 리셋
            stateMachine.fallTimeoutDelta = stateMachine.FallTimeout;

            // 착지 애니 정리
            if (stateMachine._hasAnimator)
            {
                stateMachine.animator.SetBool(stateMachine._animIDJump, false);
                stateMachine.animator.SetBool(stateMachine._animIDFreeFall, false);
            }

            // 바닥 스티키
            if (stateMachine.verticalVelocity < 0f)
                stateMachine.verticalVelocity = -2f;

            // 점프
            if (stateMachine.isJumping //점프가 가능한지
                && stateMachine.jumpTimeoutDelta <= 0f //점프 쿨타임이 끝났는지
                && stateMachine.currentStamina > stateMachine.jumpStaminaCost)   //스테미나가 충분한지
            {
                stateMachine.isJumping = false;
                stateMachine.UseStamina(stateMachine.jumpStaminaCost);

                stateMachine.verticalVelocity =
                    Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);

                if (stateMachine._hasAnimator)
                {
                    stateMachine.animator.SetBool(stateMachine._animIDJump, true);
                }
            }

            // 점프 쿨타임 감소
            if (stateMachine.jumpTimeoutDelta >= 0f)
                stateMachine.jumpTimeoutDelta -= deltaTime;
        }
        else
        {
            // 공중 진입 시 점프 쿨타임 리셋
            stateMachine.jumpTimeoutDelta = stateMachine.JumpTimeout;

            // fall timeout 처리
            if (stateMachine.fallTimeoutDelta >= 0f)
            {
                stateMachine.fallTimeoutDelta -= deltaTime;
            }
            else
            {
                if (stateMachine._hasAnimator)
                {
                    stateMachine.animator.SetBool(stateMachine._animIDFreeFall, true);
                }
            }

            // 공중에서는 점프 입력 소모
            stateMachine.isJumping = false;
        }

        // 중력 적용 (terminalVelocity는 반드시 음수)
        if (stateMachine.verticalVelocity > stateMachine.terminalVelocity)
        {
            stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;
        }
    }
}
