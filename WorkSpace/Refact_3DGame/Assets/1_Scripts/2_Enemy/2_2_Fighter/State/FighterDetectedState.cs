using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 플레이어를 탐지한 상태
 
 */
public class FighterDetectedState : EnemyState
{
    private Fighter _fighter;
    public FighterDetectedState(EnemyStateMachine stateMachine, Fighter fighter) : base(stateMachine)
    {
        _fighter = fighter;
    }

    public override void Enter()
    {
        Debug.Log("FighterDetectedState 진입");


        //  탐지를 활성화
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsDetected, true);


        //  추적 상태로 상태전이
        stateMachine.SwitchState(_fighter.mChaseState);
    }



    public override void Exit()
    {
        Debug.Log("FighterDetectedState 나가기");
    }

}
