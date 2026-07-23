using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZomBAttackState : EnemyState, IAttackState
{
    private readonly Zombie mZombie;
    private AttackData _attackData;

    // 자식 상태들
    private EnemyState mCurrentSubState;
    private ZomBRotateSubState mRotateSubState;
    private ZomBSwingSubState mSwingSubState;

    public ZomBAttackState(EnemyStateMachine stateMachine, Zombie zombie, AttackData attackData) : base(stateMachine) 
    {
        this.mZombie = zombie;
        this._attackData = attackData;

        // 자식 상태 생성 (부모의 stateMachine과 zombie 전달)
        mRotateSubState = new ZomBRotateSubState(stateMachine, zombie);
        mSwingSubState = new ZomBSwingSubState(stateMachine, zombie);
    }

    public override void Enter()
    {
        //Debug.Log("Zombie Attack State진입");
        mZombie.isDetected = true;
        if (mZombie.isTarget)
            mZombie.ChangeTargettMtl();
        else
            mZombie.ChangeDetectedMtl();

        // 공격 범위 파라미터 활성화
        stateMachine.enemy.animator.SetBool(mZombie._animIDAttackRange, true);
       
        // 첫 자식 상태 설정
        CheckAndSwitchSubState();
    }

    public override void Tick(float deltaTime)
    {

        if (mZombie.isAttacking) return; //공격중에는 모든 로직을 막는다.

        var target = mZombie.playerDetector.mPlayerPos;

        // 타겟이 없어지면 Idle(원하면 Move로 바꿔도 됨)
        if (target == null)
        {
            stateMachine.SwitchState(mZombie.mIdleState);
            return;
        }

        // 거리가 너무 멀어짐 -> Move (추격 재개)
        float dist = Vector3.Distance(stateMachine.transform.position, target.transform.position);
        if (dist > mZombie.attackRange + 0.5f) // 0.5f 여유 둠
        {
            stateMachine.SwitchState(mZombie.mMoveState);
            return;
        }

        // 각도에 따라 자식 상태(회전/타격) 전환
        CheckAndSwitchSubState();

        // 현재 자식 상태의 Tick 실행
        mCurrentSubState?.Tick(deltaTime);
    }


    public override void Exit() {
        //Debug.Log("Zombie Attack State나가기");
        mCurrentSubState?.Exit();
        mCurrentSubState = null;
        stateMachine.enemy.animator.SetBool("IsAttack", false);
        stateMachine.enemy.animator.SetBool("InAttackRange", false);

        mZombie.isDetected = false;
    }


    //플레이어 공격에 대한 서브상태로 전환
    private void CheckAndSwitchSubState()
    {

        var target = mZombie.playerDetector.mPlayerPos;
        if (target == null) return;

        Vector3 dir = (target.transform.position - stateMachine.transform.position).normalized;
        dir.y = 0;

        //내적(Dot)연산으로 연산 최적화(Cos연산)
        float toPlayerAngle = Vector3.Dot(stateMachine.transform.forward, dir);
        float AttackAcceptAngle = 0.93f;

        //내적 연산의 결과가 AttackAcceptAngle보다 작으면 회전시킨다. 아닐경우 공격가능
        EnemyState nextState = (toPlayerAngle < AttackAcceptAngle) ? mRotateSubState : mSwingSubState;

        if (mCurrentSubState != nextState)
        {
            mCurrentSubState?.Exit();
            mCurrentSubState = nextState;
            mCurrentSubState.Enter();
        }
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
