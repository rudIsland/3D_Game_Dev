using UnityEngine;

public class MutantDetectedState : EnemyState
{
    private readonly Mutant mMutant;

    public MutantDetectedState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine)
    {
        mMutant = mutant;
    }
    public override void Enter()
    {

        //Debug.Log("MutantDetectedState 상태 진입");

        mMutant.isDetected = true;

        if (mMutant.isTarget)
            mMutant.ChangeTargettMtl();
        else
            mMutant.ChangeDetectedMtl();

            // 1.  현재 위치에서 어떤 공격을 할지 결정
            mMutant.DecideNextAttack();
    }

    public override void Tick(float deltaTime)
    {
        var target = mMutant.playerDetector.mPlayerPos;

        // 2. 타겟이 시야에서 사라지면 다시 Idle로
        if (target == null)
        {
            stateMachine.SwitchState(mMutant.mIdleState);
            return;
        }


        float dist = Vector3.Distance(stateMachine.transform.position, target.position);

        // 3. 거리와 각도에 따른 전이 판단
        if (dist <= mMutant.nextAttackRange)
        {
            // 공격권 안이라면 공격 상태(Super)로 진입
            // (AttackState 내부에서 다시 정면인지 확인하여 Rotate 혹은 타격 수행)
            stateMachine.SwitchState(mMutant.mAttackState);
        }
        else
        {
            // 공격권 밖이라면 추격 상태로 진입
            stateMachine.SwitchState(mMutant.mMoveState);
        }
    }

    public override void Exit()
    {
        //Debug.Log("MutantDetectedState 나가기");
        mMutant.isDetected = false;
        mMutant.ChangeDefaultMtl();
        
    }
}