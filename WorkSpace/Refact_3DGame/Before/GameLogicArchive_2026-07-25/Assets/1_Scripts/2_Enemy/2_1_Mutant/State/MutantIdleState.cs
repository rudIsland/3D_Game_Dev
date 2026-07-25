using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MutantIdleState : EnemyState
{
    private readonly Mutant mMutant;
    public MutantIdleState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine) 
    {
        this.mMutant = mutant;
    }

    public override void Enter() {
        //Debug.Log("MutantIdleState 진입");
        // 확실하게 전투/추적 관련 파라미터 끄기
        stateMachine.enemy.animator.SetFloat(mMutant._animIDMoveSpeed, 0f);
        stateMachine.enemy.animator.SetBool(mMutant._animIDAttack, false);

        mMutant.ChangeDefaultMtl();
        
    }

    public override void Tick(float deltaTime) {

        // 1. 플레이어가 감지되었는지만 확인
        if (mMutant.playerDetector.mPlayerPos != null)
        {
            // 발견 즉시 판단 허브인 DetectedState로 전이
            stateMachine.SwitchState(mMutant.mDetectedState);
        }
    }

    

    public override void Exit() {
        //Debug.Log("MutantIdleState 나가기");
    }


}
