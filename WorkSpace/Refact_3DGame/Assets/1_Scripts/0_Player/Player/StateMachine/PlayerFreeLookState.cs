using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;


/*
 * 자유시점 상태
 */
public class PlayerFreeLookState : PlayerBaseState
{
    private float _jumpTimeoutDelta;
    const string FREE_LOOK_BLEND_TREE = "FreeLookBlendTree";

    public float sprintStaminaCostPerSecond = 10f;
    public float jumpStaminaCost = 20f;

    public PlayerFreeLookState(PlayerStateMachine stateMachine) : base(stateMachine) {}

    public override void Enter()
    {
        Debug.Log("FreeLook진입");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed += stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed += onTargetPressed;
            stateMachine.OnDeath += stateMachine.HandlePlayerDeath;
        }
        _jumpTimeoutDelta = stateMachine.JumpTimeout; //점프 텀 시간 할당


        stateMachine.animator.Play(FREE_LOOK_BLEND_TREE);
    }

    public override void Exit()
    {
        Debug.Log("FreeLook나가기");
        // 입력 이벤트 구독
        if (stateMachine.inputReader != null)
        {
            stateMachine.inputReader.jumpPressed -= stateMachine.OnJumpPressed;
            stateMachine.inputReader.TargetPressed -= onTargetPressed;
            stateMachine.OnDeath -= stateMachine.HandlePlayerDeath;
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
        // 이동 애니메이션 업데이트
        UpdateMoveAnimation(deltaTime);

        //가만히 있으면 스테미나 회복 업데이트 
        stateMachine.stat.RegenStamina();

        //Other Animation Check
        OtherAnimaionCheck();

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
    
    public void onTargetPressed()
    {
        if (!stateMachine.targeter.SelectTarget())
        {
            return;
        }

        stateMachine.SwitchState(new PlayerTargetLookState(stateMachine));
    }

    private Vector3 CalculateMove(float deltaTime)
    {
        Vector2 input = stateMachine.inputReader.moveInput;
        stateMachine.verticalVelocity += stateMachine.gravity * deltaTime;

        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        //  카메라 기준 방향 계산
        Transform cam = Camera.main.transform;
        Vector3 camForward = cam.forward; //월드 z축 기준 북(0,0,1) 남(0,0,-1) 동쪽(1,0,0) 서(-1,0,0)에 근접
        Vector3 camRight = cam.right; //카메라 기준에서 월드의 오른쪽 방향
        //ex) 북(0,0,1)을 보고있으면 오른쪽인 동(1,0,0), 동에서 오른쪽은 남(0,0,-1), 서쪽을보면 북(0,0,1)에 근접

        camForward.y = 0f;  camRight.y = 0f; //수평유지
        camForward.Normalize(); camRight.Normalize(); //정규화

        //  카메라 기준 입력 방향 계산
        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        // 공격중에는 이동 불가
        if (stateMachine.animator.GetBool(stateMachine._animIDAttack))
        {
            return Vector3.zero;
        }

        //  "걷기"일 때만 회전 적용
        if (!stateMachine.inputReader.onSprint && moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                deltaTime * stateMachine.rotateSpeed
            );
        }

        else if(stateMachine.inputReader.onSprint && input.sqrMagnitude > 0.1f) //달리기중에 입력받으면
        {
            // if Sprint.... Cost Stemina
            float sprintCost = sprintStaminaCostPerSecond * deltaTime;
            /*if (stateMachine.playerStat.CanUse(sprintCost))
            {
                stateMachine.playerStat.Use(sprintCost);
                UIManager.Instance.playerResource.UpdateStaminaUI();
            }
            else
            {
                stateMachine.inputReader.onSprint = false; // 스태미나 부족 시 달리기 중지
            }*/

            // 캐릭터를 moveDirection 기준으로 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                deltaTime * stateMachine.rotateSpeed
            );

            // 이 방향으로 이동
            return moveDirection;
        }

        //  이동 방향
        if (stateMachine.inputReader.onSprint) //달리기일 땐 현재 바라보는 방향
        {
            return stateMachine.transform.forward;
        }
        else //걷기일 때는 카메라 기준
        {
            return moveDirection;
        }
    }


    //이동 애니메이션
    private void UpdateMoveAnimation(float deltaTime)
    {
        // 입력값 크기 가져오기
        float inputMagnitude = stateMachine.inputReader.moveInput.magnitude;

        // 목표 속도 계산
        float targetSpeed = stateMachine.inputReader.onSprint ? stateMachine.sprintSpeed : stateMachine.moveSpeed;

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

        // MotionSpeed 계산 (아날로그 여부 확인)
        float motionSpeed = stateMachine.inputReader.isMove ? inputMagnitude : (inputMagnitude > 0 ? 1f : 0f);

        if(motionSpeed > 1f) motionSpeed = 1f;

        // 애니메이터에 값 적용
        stateMachine.animator.SetFloat(stateMachine._animIDSpeed, stateMachine._ani_SpeedValue);
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

            // 점프 쿨다운 적용
            if (_jumpTimeoutDelta > 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }

            // 점프 처리(점프가 가능한 상태 && 점프재사용시간 && 공격중x)
            if (stateMachine.jump && _jumpTimeoutDelta <= 0.0f && !stateMachine.inputReader.isAttack)
            {
                if (stateMachine.stat != null )//&& stateMachine.stat.CanUse(jumpStaminaCost))
                {
                    stateMachine.jump = false;
                    _jumpTimeoutDelta = stateMachine.JumpTimeout;

                    // if jump.... Cost Stemina
                    //stateMachine.playerStat.Use(jumpStaminaCost);
                    //UIManager.Instance.playerResource.UpdateStaminaUI();

                    // 점프 처리
                    stateMachine.verticalVelocity = Mathf.Sqrt(stateMachine.jumpHeight * -2f * stateMachine.gravity);
                    stateMachine.animator.SetBool(stateMachine._animIDJump, true);
                }
                else
                {
                    stateMachine.jump = false; // 스태미나가 부족하면 점프 무시
                }
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
        stateMachine.jump = true;
    }

    
}
