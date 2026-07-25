
using UnityEngine;

public class MutantHitState : EnemyState
{
    private Mutant mMutant;
    public MutantHitState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine)
    {
        this.mMutant = mutant;
    }

    public override void Enter()
    {
        mMutant.isHit = true;
        mMutant.isAttacking = false;
        stateMachine.enemy.animator.SetBool(mMutant._animIDAttack, false);
        stateMachine.enemy.animator.SetTrigger(mMutant._animIDHitTrigger);
        stateMachine.enemy.animator.SetBool(mMutant._animIDisHit,true);

        if (mMutant.isTarget)
            mMutant.ChangeTargettMtl();
        else
            mMutant.ChangeDetectedMtl();
    }

    public override void Tick(float deltaTime)
    {
        if (!mMutant.isHit)
        {
            stateMachine.SwitchState(mMutant.mIdleState);
        }
    }

    

    public override void Exit() 
    {
        stateMachine.enemy.animator.SetBool(mMutant._animIDisHit, false);

    }

    
}
