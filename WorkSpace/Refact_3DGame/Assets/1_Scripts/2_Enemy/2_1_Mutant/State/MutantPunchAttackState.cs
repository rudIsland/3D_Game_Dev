
using UnityEngine;

public class MutantPunchAttackState : MutantSubAttackBase
{
    public override int AttackIndex => 0;

    public MutantPunchAttackState(EnemyStateMachine stateMachine, Mutant mutant, AttackData attackData) : base(stateMachine, mutant, attackData)
    {
        
    }

    public override void Enter()
    {
        //Debug.Log("MutantPunchAttackState 진입");
        mMutant.isAttacking = true;
        base.Enter();
       
    }

    public override void Exit()
    {
        //Debug.Log("MutantSwingAttackState 나가기");
        base.Exit();
    }
}
