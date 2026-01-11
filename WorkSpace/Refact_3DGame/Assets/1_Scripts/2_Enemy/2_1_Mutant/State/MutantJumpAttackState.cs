

using UnityEngine;

public class MutantJumpAttackState : MutantSubAttackBase
{
    public override int AttackIndex => 2;
    public MutantJumpAttackState(EnemyStateMachine stateMachine, Mutant mutant, AttackData attackData) : base(stateMachine, mutant, attackData)
    {

    }

    public override void Enter()
    {
       // Debug.Log("MutantJumpAttackState 진입");
        base.Enter();
        mMutant.isAttacking = true;
        stateMachine.enemy.animator.applyRootMotion = true;
    }


    public override void Exit()
    {
       // Debug.Log("MutantSwingAttackState 나가기");
        base.Exit();
    }



}
