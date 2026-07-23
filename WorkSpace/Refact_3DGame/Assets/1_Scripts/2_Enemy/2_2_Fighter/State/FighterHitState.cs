using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterHitState : EnemyState
{
    private Fighter _fighter;
    //얘는 Hit를 생각안해서 아직 모름.
    public FighterHitState(EnemyStateMachine stateMachine, Fighter fighter) : base(stateMachine)
    {
        _fighter = fighter;
    }

    public override void Enter()
    {
        //공격 끄기
        _fighter.isAttacking = false;
        _fighter.isHit = true;
        //stateMachine.enemy.animator.SetTrigger(_fighter._animIDHitTrigger);
    }

    public override void Tick(float deltaTime)
    {
        //맞고있는 동안은 아무런 액션 x
        if (_fighter.isHit) return;

        var target = _fighter.playerDetector.mPlayerPos;

        // 1. [타겟 실종] 플레이어를 놓치면 Idle로 돌아가서 처음부터 탐지 대기
        if (target == null)
        {
            stateMachine.SwitchState(_fighter.mIdleState);
            return;
        }

        float distance = Vector3.Distance(stateMachine.transform.position, target.position);

        // 2. 전투준비 사거리보다 멀어지면 추적상태로 전이
        if (distance > _fighter.fightReadyRange)
        {
            stateMachine.SwitchState(_fighter.mChaseState);
            return;
        }
        else
        {
            stateMachine.SwitchState(_fighter.mCombatState);
        }

    }

    public override void Exit()
    {
        _fighter.isHit = false;
    }
    
}
