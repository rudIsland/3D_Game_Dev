using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterDeadState : EnemyDeadState<Fighter>
{
    private Fighter _fighter;
    public FighterDeadState(EnemyStateMachine stateMachine, Fighter fighter) : base(stateMachine, fighter)
    {
        _fighter = fighter;
    }

    public override void Enter()
    {
        // 확실하게 전투/추적 관련 파라미터 끄기
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsDetected, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightReady, false);
        stateMachine.enemy.animator.SetInteger(_fighter._animIDAttackIndex, -1);

        base.Enter(); //부모만 호출하면 할거없음.

        Debug.Log("Fighter Dead State Enter");
    }
}
