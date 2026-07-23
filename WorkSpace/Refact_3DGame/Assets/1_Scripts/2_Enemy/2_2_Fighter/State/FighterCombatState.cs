using UnityEngine;
using UnityEngine.AI;
public class FighterCombatState : EnemyState
{
    private Fighter _fighter;
    private NavMeshAgent _navAgent;

    private float _attackTimer; // 공격 대기 타이머
    private const float ATTACK_COOLDOWN = 0.5f; // 공격 사이 대기 시간 (기호에 맞게 조절)
    public FighterCombatState(EnemyStateMachine stateMachine, Fighter fighter) : base(stateMachine)
    {
        _fighter = fighter;
        _navAgent = fighter.navAgent;
    }

    public override void Enter()
    {
        Debug.Log("FighterCombatState 진입");

        //  전투 상태/추적 상태를 활성화
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, true);
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, true);
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightReady, true);
        _fighter.isDetected = true;

        _fighter.DecideNextAttack(); //전투로 왔을 때 다음 공격을 설정

        // 2. 타이머 초기화 (공격 후 돌아왔을 때 0.5초를 기다리게 함)
        _attackTimer = ATTACK_COOLDOWN;

        if (_navAgent != null)
        {
            _navAgent.speed = _fighter.fightWalkSpeed;
            _navAgent.stoppingDistance = _fighter.nextAttackRange;
            _navAgent.isStopped = false;
        }

        Debug.Log($"다음 공격: {_fighter.nextAttackState}");
    }

    public override void Tick(float deltaTime)
    {

        if (_fighter.isHit) return;

        if (_navAgent == null) return;

        var target = _fighter.playerDetector.mPlayerPos;

        // 1. [타겟 실종] 플레이어를 놓치면 Idle로 돌아가서 처음부터 탐지 대기
        if (target == null)
        {
            StopMovement();
            stateMachine.SwitchState(_fighter.mIdleState);
            return;
        }

        float distance = Vector3.Distance(stateMachine.transform.position, target.position);

        // 2. 전투준비 사거리보다 멀어지면 추적상태로 전이
        if (distance > _fighter.fightReadyRange)
        {
            stateMachine.SwitchState(_fighter.mChaseState);
            return;
        }


        // 2. [공격 대기 타이머]
        if (_attackTimer > 0)
        {
            _attackTimer -= deltaTime;

            // 대기 시간 중에는 제자리 회전만 수행 (또는 아주 천천히 접근)
            RotateTowardsTarget(target.position);

            // 대기 중에는 NavMeshAgent를 멈춰서 미끄러짐 방지 (선택 사항)
            if (distance <= _fighter.nextAttackRange + 0.5f)
                _navAgent.isStopped = true;

            return; // 타이머가 남았다면 아래의 공격 전이 로직을 실행하지 않음
        }


        // 3. [공격 실행] 사거리 안이고 타이머가 끝났다면
        if (distance <= _fighter.nextAttackRange)
        {
            stateMachine.SwitchState(_fighter.nextAttackState);
        }
        else
        {
            // 사거리는 안됐지만 타이머는 끝났다면 다시 추격 재개
            _navAgent.isStopped = false;
            _navAgent.SetDestination(target.position);
        }
    }

    // 플레이어를 향해 부드럽게 회전하는 메서드
    private void RotateTowardsTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - stateMachine.transform.position).normalized;
        direction.y = 0; // 수평 회전만

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f // 회전 속도
            );
        }
    }

    private void StopMovement() //밀림 방지
    {
        if (_navAgent != null)
        {
            _navAgent.velocity = Vector3.zero;
            _navAgent.isStopped = true;
        }
    }


    public override void Exit()
    {
        Debug.Log("FighterCombatState 나가기");
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, false);
        _fighter.isDetected = true;
    }

}
