using UnityEngine;

public class Mutant : Enemy
{
    //Mutant의 상태들
    public MutantIdleState mIdleState { get; private set; }
    public MutantMoveState mMoveState { get; private set; }
    public MutantAttackState mAttackState { get; private set; }
    public MutantDeadState mDeadState { get; private set; }
    public MutantHitState mHitState { get; private set; }
    public MutantPunchAttackState mPunchAttackState { get; private set; }
    public MutantSwingAttackState mSwingAttackState { get; private set; }
    public MutantJumpAttackState mJumpAttackState { get; private set; }
    public MutantDetectedState mDetectedState { get; private set; }


    // 이번에 실행할 타겟 상태와 사거리
    [Header("다음 공격 정보")]
    public MutantSubAttackBase nextAttackState ;//{ get; private set; }
    public float nextAttackRange ;//{ get; private set; }

    //애니메이션
    public readonly int _animIDRunning = Animator.StringToHash("RunningRange"); //탐지&달리기 범위
    public readonly int _animIDWalking = Animator.StringToHash("WalkRange"); //탐지&달리기 범위
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDHitTrigger = Animator.StringToHash("HitTrigger"); //피격 트리거
    public readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex"); //공격범위
    public readonly int _animIDAttackTrigger = Animator.StringToHash("AttackTrigger"); //공격 트리거
    public readonly int _animIDMoveSpeed = Animator.StringToHash("MoveSpeed"); //공격범위
    public const int ATTACK_JUMP = 1;
    public const int ATTACK_PUNCH = 2;
    public const int ATTACK_SWIP = 3;

    //Mutant의 스탯정보
    private MutantStatPreset mutantStat => stat as MutantStatPreset; //형변환 스탯
    public float mJumpAttackRange;
    public float mSwingAttackRange;
    public float mPunchAttackRange;
    public float mWalkRange;
    public float mRunRange;
    public float mWalkSpeed;
    public float mRunSpeed;


    protected override void Awake()
    {
        base.Awake();
    }

    public override void InitializeEnemy()
    {
        // 1. 부모의 공통 스케일링(HP, ATK 등)을 먼저 실행
        base.InitializeEnemy();

        // 2. Mutant만 가지고 있는 고유 스탯들 할당
        var mutantStat = stat as MutantStatPreset;
        if (mutantStat != null)
        {
            mJumpAttackRange = mutantStat._JumpAttackRange;
            mSwingAttackRange = mutantStat._SwingAttackRange;
            mPunchAttackRange = mutantStat._PunchAttackRange;
            mWalkRange = mutantStat._WalkRange;
            mRunRange = mutantStat._RunRange;
            mWalkSpeed = mutantStat._WalkSpeed;
            mRunSpeed = mutantStat._RunSpeed;

            // 탐지 콜리더 등 개별 설정
            if (playerDetector != null)
                playerDetector.sphereCollider.radius = _DetectRange;
        }
    }

    public override void SetupStateMachine(EnemyStateMachine stateMachine)
    {
        //공격 먼저 생성
        mPunchAttackState = new MutantPunchAttackState(stateMachine, this, mutantStat.PunchAttack);
        mSwingAttackState = new MutantSwingAttackState(stateMachine, this, mutantStat.SwingAttack);
        mJumpAttackState = new MutantJumpAttackState(stateMachine, this, mutantStat.JumpAttack);


        // 로컬 프로퍼티에 먼저 할당
        mIdleState = new MutantIdleState(stateMachine, this);
        mMoveState = new MutantMoveState(stateMachine, this);
        mDetectedState = new MutantDetectedState(stateMachine, this);
        mAttackState = new MutantAttackState(stateMachine, this); //추상클래스됨
        mDeadState = new MutantDeadState(stateMachine, this);
        mHitState = new MutantHitState(stateMachine, this);

        // 상태 머신의 공통의 추상화된 변수들에 연결
        stateMachine.idleState = mIdleState;
        stateMachine.moveState = mMoveState;
        stateMachine.attackState = mAttackState;
        stateMachine.deadState = mDeadState;
        stateMachine.hitState = mHitState;

        stateMachine.InitalizeState(mIdleState);
    }


    // 다음 공격을 결정하는 함수
    public void DecideNextAttack()
    {
        //다음 공격을 "랜덤"으로 가져온다. <--추후 다른 로직 구현으로 변경 가능
        int rand = Random.Range(1, 4);

        //다음 공격 설정
        if (rand == 1) {
            nextAttackState = mPunchAttackState; 
            nextAttackRange = mPunchAttackRange; 
        }
        else if (rand == 2) 
        { 
            nextAttackState = mSwingAttackState; 
            nextAttackRange = mSwingAttackRange; 
        }
        else if(rand == 3){ 
            nextAttackState = mJumpAttackState; 
            nextAttackRange = mJumpAttackRange; 
        }
    }


    public void OnDeathHandler() { }

    public void AttackStart() => isAttacking = true;
    public void EndAttack() => isAttacking = false;



    public void EndHit() => isHit = false;
}
