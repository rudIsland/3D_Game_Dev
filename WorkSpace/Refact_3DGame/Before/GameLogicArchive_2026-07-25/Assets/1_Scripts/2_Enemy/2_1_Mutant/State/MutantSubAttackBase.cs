using UnityEngine;

public abstract class MutantSubAttackBase : EnemyState, IAttackState
{
    protected Mutant mMutant;
    protected AttackData _attackData;
    public abstract int AttackIndex { get; }

    public MutantSubAttackBase(EnemyStateMachine stateMachine, Mutant mutant, AttackData attackData) : base(stateMachine)
    {
        mMutant = mutant;
        _attackData = attackData;
    }

    public override void Enter()
    {
        mMutant.isAttacking = true;
        mMutant.isDetected = true;

        // 1.  현재 위치에서 어떤 공격을 할지 결정
        mMutant.DecideNextAttack();

        stateMachine.enemy.animator.SetBool(mMutant._animIDAttack, true);
        stateMachine.enemy.animator.SetInteger(mMutant._animIDAttackIndex, AttackIndex);
        stateMachine.enemy.animator.SetTrigger(mMutant._animIDAttackTrigger);
    }
    public override void Tick(float deltaTime)
    {
        if (!mMutant.isAttacking)
        {
            //Debug.Log("MutantSubAttackBase Tick!!!");
            stateMachine.SwitchState(mMutant.mDetectedState);
        }
    }
    public override void Exit()
    {
        stateMachine.enemy.animator.SetBool(mMutant._animIDAttack, false);
    }

    public float GetDamageFromData(int weaponIndex, int hitIndex)
    {
        // 1. HitList에 해당 순서의 데이터가 있는지 확인
        if (_attackData.hitList == null || _attackData.hitList.Count <= hitIndex)
            return 0f;

        var data = _attackData.hitList[hitIndex];

        // 2. 현재 충돌한 무기의 인덱스가 데이터와 일치하는지 확인
        if (data.weaponIndex == weaponIndex)
        {
            return data.damage;
        }

        return 0f;
    }
}
