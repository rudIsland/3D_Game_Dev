using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZomBRotateSubState : EnemyState
{
    private Zombie mZombie;
    private readonly NavMeshAgent mAgent;
    private float mRotateSpeed = 5.0f;
    
    public ZomBRotateSubState(EnemyStateMachine stateMachine, Zombie zombie) : base(stateMachine)
    {
        mZombie = zombie;
        mAgent = stateMachine.GetComponent<NavMeshAgent>();
    }


    public override void Enter()
    {
       // Debug.Log("ZomBRotateSubState진입");
        
        //플레이어가 "공격범위"안에는있지만 바라보지 않아서 공격X
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttack, false);

        if (mAgent != null)
        {
            mAgent.velocity = Vector3.zero; // 1. 남은 속도 제거 (미끄러짐 방지)
            mAgent.isStopped = true;        // 2. 이동 정지

            mAgent.updateRotation = false;  // 3. Agent가 회전 간섭 X
            mAgent.updatePosition = false;  // 4. Agent가 위치 간섭 X (제자리 회전 필수)
        }
    }

    public override void Tick(float deltaTime)
    {

        var target = mZombie.playerDetector.mPlayerPos;
        if (target == null) return;

        Vector3 dir = (target.transform.position - stateMachine.transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.001f)
        {
            // 부드러운 회전 처리
            Quaternion targetRot = Quaternion.LookRotation(dir);
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                targetRot,
                deltaTime * mRotateSpeed
            );
        }

    }


    public override void Exit()
    {
       // Debug.Log("ZomBRotateSubState나가기");
        if (mAgent != null)
        {
            mAgent.updateRotation = true;
            mAgent.updatePosition = true;
        }

        mZombie.isAttacking = false;
    }
}
