
using UnityEngine;
using UnityEngine.AI;

public class MutantAttackState : EnemyState, IAttackState
{
    protected Mutant mMutant;
    private NavMeshAgent _agent;
    private MutantRotateSubState _rotateSubState;
    private EnemyState _currentSubState;

    public MutantAttackState(EnemyStateMachine stateMachine, Mutant mutant) : base(stateMachine) 
    {
        mMutant = mutant;
        _agent = stateMachine.GetComponent<NavMeshAgent>();
        _rotateSubState = new MutantRotateSubState(stateMachine, mutant);
    }

    public override void Enter()
    {
        //Debug.Log("MutantAttackState(Super) 진입");

        // 1. 공통 동작: 에이전트 정지 및 물리 속도 초기화
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        mMutant.isDetected = true;

        if (mMutant.isTarget)
            mMutant.ChangeTargettMtl();
        else
            mMutant.ChangeDetectedMtl();

        // 1.  현재 위치에서 어떤 공격을 할지 결정
        mMutant.DecideNextAttack();

        // 2. 초기화: 공격 중 아님을 확인하고 첫 서브 상태(회전 혹은 타격) 결정
        // (타격 애니메이션이 시작되기 전까지는 회전 로직이 돌아가야 하므로 초기에는 false)
        mMutant.isAttacking = false;

        CheckAndSwitchSubState();
    }

    public override void Tick(float deltaTime)
    {
        if (mMutant.isHit) return;

        // 1. 공격 중(Punch, Swing 등)일 때는 서브 상태의 Tick이 탈출을 책임지므로 관여 안 함
        if (mMutant.isAttacking)
        {
            _currentSubState?.Tick(deltaTime);
            return;
        }

        // 2. 회전 상태일 때의 로직
        if (_currentSubState == _rotateSubState)
        {
            // 회전하다가 정면을 보게 되면 판단 허브로 돌아가서 재승인(공격/이동 결정)을 받음
            if (IsFacingTarget())
            {
                stateMachine.SwitchState(mMutant.mDetectedState);
                return;
            }
        }

        // 3. 서브 상태 결정 및 실행 (초기 진입 시나 회전 필요 시)
        CheckAndSwitchSubState();
        _currentSubState?.Tick(deltaTime);
    }
    private bool IsFacingTarget()
    {
        var target = mMutant.playerDetector.mPlayerPos;
        if (target == null) return false;

        Vector3 dir = (target.position - stateMachine.transform.position).normalized;
        dir.y = 0;
        float dot = Vector3.Dot(stateMachine.transform.forward, dir);
        return dot >= 0.93f; // 약 20도 이내
    }
    private void CheckAndSwitchSubState()
    {
        var target = mMutant.playerDetector.mPlayerPos;
        if (target == null) return;

        Vector3 dir = (target.position - stateMachine.transform.position).normalized;
        dir.y = 0;

        float toPlayerAngle = Vector3.Dot(stateMachine.transform.forward, dir);
        float attackAcceptAngle = 0.93f; // 약 20도 내외

        // 각도가 맞지 않으면 회전 상태, 맞으면 미리 결정된 공격 상태(Punch 등) 선택
        EnemyState nextSub = (toPlayerAngle < attackAcceptAngle) ? _rotateSubState : mMutant.nextAttackState;

        if (_currentSubState != nextSub)
        {
            _currentSubState?.Exit();
            _currentSubState = nextSub;
            _currentSubState.Enter();
        }
    }


    public override void Exit() {
        _currentSubState?.Exit();
        _currentSubState = null;
       // Debug.Log("MutantAttackState나가기");
    }

    public float GetDamageFromData(int weaponIndex, int hitIndex)
    {
        // 현재 실행 중인 서브 상태(Punch, Swing 등)가 인터페이스를 가지고 있다면
        if (_currentSubState is IAttackState subAttack)
        {
            // 실제 데미지 계산은 서브 상태에게 '위임'합니다.
            return subAttack.GetDamageFromData(weaponIndex, hitIndex);
        }

        return 0f;
    }
}
