using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZomBIdleState : EnemyState
{
    private readonly Zombie mZombie;
    public ZomBIdleState(EnemyStateMachine stateMachine, Zombie zombie) : base(stateMachine) 
    {
        this.mZombie = zombie;
    }

    public override void Enter() {
       // Debug.Log("Zombie Idle State진입");
        // 확실하게 전투/추적 관련 파라미터 끄기
        stateMachine.enemy.animator.SetBool(mZombie._animIDFind, false);
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttackRange, false);
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttack, false);
    }

    public override void Tick(float deltaTime) {
        //플레이어를 탐지하면 움직이는 상태로 전환
        if(mZombie.playerDetector.mPlayerPos != null)
        {
            stateMachine.SwitchState(mZombie.mMoveState);
        }

    }

    public override void Exit() {

    }


}
