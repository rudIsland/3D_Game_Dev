using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZomBHitState : EnemyState
{
    private Zombie mZombie;
    public ZomBHitState(EnemyStateMachine stateMachine, Zombie zombie) : base(stateMachine)
    {
        this.mZombie = zombie;
    }

    public override void Enter()
    {
        mZombie.isHit = true;
        mZombie.isAttacking = false;
        stateMachine.enemy.animator.SetBool(mZombie._animIDHit, true);

    }

    public override void Tick(float deltaTime)
    {
        if (!mZombie.isHit)
        {
            stateMachine.SwitchState(mZombie.mIdleState);
        }
    }

    

    public override void Exit() 
    {
        stateMachine.enemy.animator.SetBool(mZombie._animIDHit, false);

    }

    
}
