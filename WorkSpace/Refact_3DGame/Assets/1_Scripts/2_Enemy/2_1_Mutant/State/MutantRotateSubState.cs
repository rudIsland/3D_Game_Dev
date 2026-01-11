using UnityEngine;
using UnityEngine.AI;

public class MutantRotateSubState : EnemyState
{
    private Mutant mMutant;

    public MutantRotateSubState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine) 
    {
        mMutant = mutant; 
    }

    public override void Enter()
    {
        //Debug.Log("MutantRotateSubState 상태 진입");
        // 회전 중에는 Idle 애니메이션이 나오도록 속도 파라미터를 0으로 설정
        stateMachine.enemy.animator.SetFloat(mMutant._animIDMoveSpeed, 0f);
    }

    public override void Tick(float deltaTime)
    {
        var target = mMutant.playerDetector.mPlayerPos;
        if (target != null)
        {
            Vector3 direction = (target.position - stateMachine.transform.position).normalized;
            direction.y = 0;

            // Mutant의 회전 속도(angularSpeed)를 사용하여 부드럽게 회전
            stateMachine.transform.rotation = Quaternion.Slerp(
                stateMachine.transform.rotation,
                Quaternion.LookRotation(direction),
                deltaTime * mMutant.angularSpeed);
        }
    }

    public override void Exit()
    {
        //Debug.Log("MutantRotateSubState 상태 나가기");
    }
}
