
using UnityEngine;
using UnityEngine.AI;

public class FighterChaseState : EnemyState
{
    private Fighter _fighter;
    private NavMeshAgent _navAgent;
    public FighterChaseState(EnemyStateMachine stateMachine, Fighter fighter) : base(stateMachine)
    {
        _fighter = fighter;
        _navAgent = _navAgent = fighter.navAgent;
    }

    public override void Enter()
    {
        Debug.Log("FighterChaseState 진입");

        //추적상태 활성화
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, true);
        _fighter.isDetected = true;

        //타겟or탐지 머터리얼 변경
        if (_fighter.isTarget)
            _fighter.ChangeTargettMtl();
        else
            _fighter.ChangeDetectedMtl();

        //AI 활성화
        if (_fighter != null)
        {
            // 에이전트 활성화 및 초기화
            _navAgent.speed = _fighter.detectedWalkSpeed;
            _navAgent.isStopped = false;
            _navAgent.updatePosition = true;
            _navAgent.updateRotation = true;

            // 정지 거리를 현재 결정된 공격의 사거리로 설정
            _navAgent.stoppingDistance = _fighter.nextAttackRange;
            _navAgent.ResetPath();
        }
    }

    public override void Tick(float deltaTime)
    {
        if (_navAgent == null) return;

        var target = _fighter.playerDetector.mPlayerPos;

        // 1. [타겟 실종] 플레이어를 놓치면 Idle로 돌아가서 처음부터 탐지 대기
        if (target == null)
        {
            StopMovement();
            stateMachine.SwitchState(_fighter.mIdleState);
            return;
        }

        // 2. 거리 계산 후 추적 시작
        float distance = Vector3.Distance(stateMachine.transform.position, target.position);

        _navAgent.SetDestination(target.position);


        // 3. [전투상태로 전이] 다음공격 사거리 내 들어오면 전이
        if (!_navAgent.pathPending && distance <= _fighter.fightReadyRange)
        {
            stateMachine.SwitchState(_fighter.mCombatState);
        }


    }


    private void StopMovement() //밀림 방지
    {
        _navAgent.velocity = Vector3.zero;
    }


    public override void Exit()
    {
        Debug.Log("FighterChaseState 나가기");

        //애니메이션, 추적, AI 끄기
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, false);
        _fighter.isDetected = false;
        if (_navAgent != null)
        {
            _navAgent.isStopped = true;
            _navAgent.velocity = Vector3.zero; // 물리적 밀림 방지
            _navAgent.ResetPath();
        }
    }
}
