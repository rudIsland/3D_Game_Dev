using UnityEngine;
using UnityEngine.AI;

public abstract class FighterAtkBaseState : EnemyState, IAttackState
{
    protected Fighter _fighter;
    private NavMeshAgent _navAgent;
    protected AttackData _attackData;


    public abstract int AttackIndex { get; }
    public FighterAtkBaseState(EnemyStateMachine stateMachine, Fighter fighter, AttackData attackData) : base(stateMachine)
    {
        _fighter = fighter;
        _attackData = attackData;
        _navAgent = stateMachine.GetComponent<NavMeshAgent>();
    }

    public override void Enter()
    {
        Debug.Log($"{_attackData.animName} 진입");

        _fighter.isAttacking = true; //공격중 활성화


        // 애니메이션 실행 (데이터에 저장된 이름 사용)
        stateMachine.enemy.animator.SetInteger(_fighter._animIDAttackIndex, AttackIndex); //공격 인덱스 설정
        stateMachine.enemy.animator.SetBool(_fighter._animIDIsChase, false);        //추적은 그만
        stateMachine.enemy.animator.SetBool(_fighter._animIDFightRange, true);

        //navMesh끄기
        if (_navAgent != null)
        {
            _navAgent.isStopped = true;
            _navAgent.velocity = Vector3.zero; // 물리적 밀림 방지
            _navAgent.ResetPath();
        }

    }

    public override void Tick(float deltaTime)
    {
        // 1. [공격 애니메이션 중] 아무것도 하지 않음 (상태 전이 차단)
        if (_fighter.isAttacking)
        {
            return;
        }

        // 4. [모든 과정 끝] 이제야 전투 대기 상태로 복귀
        stateMachine.SwitchState(_fighter.mCombatState);

    }

    public float GetDamageFromData(int weaponIdx, int hitIndex)
    {
        // 1. HitList에 해당 순서의 데이터가 있는지 확인
        if (_attackData.hitList == null || _attackData.hitList.Count <= hitIndex)
            return 0f;

        var data = _attackData.hitList[hitIndex];

        // 2. 현재 충돌한 무기의 인덱스가 데이터와 일치하는지 확인
        if (data.weaponIndex == weaponIdx)
        {
            return data.damage;
        }

        return 0f;
    }


    protected virtual void OnAttackTick(float deltaTime) { }
    public override void Exit() 
    {
        Debug.Log($"{_attackData.animName} 나가기");

        // 다음 상태(Combat)로 가기 전에 확실히 공격 플래그를 꺼줌
        _fighter.isAttacking = false;
    }
}
