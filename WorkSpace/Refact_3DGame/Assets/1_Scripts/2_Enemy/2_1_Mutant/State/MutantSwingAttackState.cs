using UnityEngine;

public class MutantSwingAttackState : MutantSubAttackBase
{
    public override int AttackIndex => 1;
    public MutantSwingAttackState(EnemyStateMachine stateMachine, Mutant mutant, AttackData attackData) : base(stateMachine, mutant, attackData)
    {
    }
    public override void Enter()
    {
        //Debug.Log("MutantSwingAttackState 진입");
        mMutant.isAttacking = true;
        base.Enter();
    }



    public override void Exit()
    {
        //Debug.Log("MutantSwingAttackState 나가기");
        base.Exit();
        
    }
   
}
