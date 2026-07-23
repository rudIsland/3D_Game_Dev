using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZomBSwingSubState : EnemyState
{
    private readonly Zombie mZombie;

    public ZomBSwingSubState(EnemyStateMachine stateMachine, Zombie zombie) : base(stateMachine)
    {
        this.mZombie = zombie;
    }
    public override void Enter()
    {
        //Debug.Log("ZomBSwingSubState 진입");
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttack, true);
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttackRange, true);
    }

    public override void Tick(float deltaTime)
    {
     
    }

    public override void Exit()
    {
       // Debug.Log("ZomBSwingSubState 나가기");
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttack, false);
    }
   
}
