using UnityEngine;
using UnityEngine.AI;

public class MutantMoveState : EnemyState
{
    private readonly Mutant mMutant;
    private readonly NavMeshAgent mAgent;

    public MutantMoveState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine)
    {
        this.mMutant = mutant;
        mAgent = stateMachine.GetComponent<NavMeshAgent>();
    }

    public override void Enter()
    {
       //Debug.Log("MutantMoveState 진입: 추격 시작");
        mMutant.isDetected = true;
        if (mMutant.isTarget)
            mMutant.ChangeTargettMtl();
        else
            mMutant.ChangeDetectedMtl();

        if (mAgent != null)
        {
            // 에이전트 활성화 및 초기화
            mAgent.isStopped = false;
            mAgent.updatePosition = true;
            mAgent.updateRotation = true;

            // 정지 거리를 현재 결정된 공격의 사거리로 설정
            mAgent.stoppingDistance = mMutant.nextAttackRange;
            mAgent.ResetPath();
        }
    }
    public override void Tick(float deltaTime)
    {

        if (mAgent == null) return;

        var target = mMutant.playerDetector.mPlayerPos;

        // 1. [타겟 실종] 플레이어를 놓치면 Idle로 돌아가서 처음부터 탐지 대기
        if (target == null)
        {
            StopMovement();
            stateMachine.SwitchState(mMutant.mIdleState);
            return;
        }

        float distance = Vector3.Distance(stateMachine.transform.position, target.position);

        // 2. [속도 조절] 거리에 따라 걷기/달리기 속도 가변 설정
        AdjustSpeedByDistance(distance);

        // 3. [목적지 갱신] 실시간으로 플레이어 위치 추적
        mAgent.SetDestination(target.position);

        // 4. [애니메이션] 현재 에이전트의 속도를 애니메이터에 전달
        float currentVelocity = mAgent.velocity.magnitude;
        stateMachine.enemy.animator.SetFloat(mMutant._animIDMoveSpeed, currentVelocity);

        // 5. [상태 전이] 공격 사거리 안으로 들어오면 Detected로 가서 판단 위임
        // 여기서 바로 Attack으로 가지 않고 Detected로 가서 '회전'이 필요한지 등을 재확인합니다.
        if (!mAgent.pathPending && distance <= mMutant.nextAttackRange)
        {
            stateMachine.SwitchState(mMutant.mDetectedState);
        }
    }

    private void AdjustSpeedByDistance(float distance)
    {
        if (distance <= mMutant.mWalkRange)
        {
            mAgent.speed = mMutant.mWalkSpeed;
        }
        else if (distance <= mMutant.mRunRange)
        {
            mAgent.speed = mMutant.mRunSpeed;
        }
        else
        {
            //멀어지면 0으로
            mAgent.speed = 0f;
        }
    }

    private void StopMovement()
    {
        mAgent.velocity = Vector3.zero;
        stateMachine.enemy.animator.SetFloat(mMutant._animIDMoveSpeed, 0f);
    }

    public override void Exit()
    {
        //Debug.Log("MutantMoveState 나가기");
        if (mAgent != null)
        {
            mAgent.isStopped = true;
            mAgent.velocity = Vector3.zero; // 물리적 밀림 방지
            mAgent.ResetPath();
        }
    }
}
