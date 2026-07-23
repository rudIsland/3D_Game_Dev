
using UnityEngine;

public class FighterIdleState : EnemyState
{
    private Fighter _fighter;
    public FighterIdleState(EnemyStateMachine stateMachine, Fighter fighter) : base(stateMachine)
    {
        _fighter = fighter;
    }

    public override void Enter()
    {
        Debug.Log("FighterIdleState 진입");

        // 확실하게 전투/추적 관련 파라미터 끄기
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsDetected, false);
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, false);

        //기본 M
        _fighter.ChangeDefaultMtl();

    }

    public override void Tick(float deltaTime)
    {

        // 1. 플레이어가 감지되었는지만 확인
        if (_fighter.playerDetector.mPlayerPos != null)
        {
            // 발견 즉시 판단 허브인 DetectedState로 전이
            stateMachine.SwitchState(_fighter.mDetectedState);
        }
    }



    public override void Exit()
    {
        Debug.Log("FighterIdleState 나가기");
    }
}
