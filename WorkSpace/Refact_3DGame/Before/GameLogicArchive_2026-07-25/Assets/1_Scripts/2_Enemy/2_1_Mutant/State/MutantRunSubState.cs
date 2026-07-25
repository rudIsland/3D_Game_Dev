using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MutantRunSubState : EnemyState
{
    private readonly Mutant mMutant;
    private readonly NavMeshAgent mAgent;

    public MutantRunSubState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine)
    {
        this.mMutant = mutant;
        mAgent = stateMachine.GetComponent<NavMeshAgent>();
    }

    public override void Enter()
    {
        stateMachine.enemy.animator.SetBool("IsFind", true);
        //Debug.Log("MutantRunSubState진입");
        if (mAgent != null)
        {
            // 1. 위치 동기화 (가장 중요)
            mAgent.Warp(stateMachine.transform.position);

            mAgent.updatePosition = true;
            mAgent.updateRotation = true;
            mAgent.isStopped = false;
            mAgent.speed = mMutant.moveSpeed;
            mAgent.angularSpeed = mMutant.angularSpeed;
            mAgent.stoppingDistance = mMutant.attackRange * 0.8f;

            mAgent.ResetPath();
        }


    }
    public override void Tick(float deltaTime)
    {

        if (mAgent == null) return;

        //타겟이 없으면 idle로 상태전환
        var target = mMutant.playerDetector.mPlayerPos;
        if (target == null)
        {
            // Find 애니 끄기
            //Debug.Log("타겟을 놓침! (Move -> Idle)"); // 이 로그가 전투 중에 뜨는지 확인
            stateMachine.enemy.animator.SetBool("IsFind", false);

            mAgent.isStopped = true;
            mAgent.ResetPath();
            stateMachine.SwitchState(mMutant.mIdleState);
            return;
        }

        // 매 프레임 목표 갱신
        mAgent.SetDestination(target.transform.position);

        //공격 범위 체크
        if (!mAgent.pathPending && mAgent.remainingDistance <= mMutant.attackRange)
        {
            // AttackState로 전환 (IsFind는 켜진 상태 유지!)
            mAgent.velocity = Vector3.zero;
            stateMachine.SwitchState(mMutant.mAttackState);
        }

    }


    public override void Exit()
    {
        if (mAgent == null) return;

        mAgent.isStopped = true; //추적 정지
        mAgent.ResetPath(); //추적 경로 초기화
        mAgent.velocity = Vector3.zero;
    }
}
