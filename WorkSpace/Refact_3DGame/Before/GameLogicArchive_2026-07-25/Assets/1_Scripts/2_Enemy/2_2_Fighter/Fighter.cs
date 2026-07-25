using UnityEngine;

public class Fighter : Enemy
{
    public FighterIdleState mIdleState { get; private set; }
    public FighterDetectedState mDetectedState { get; private set; }
    public FighterChaseState mChaseState { get; private set; }
    public FighterDeadState mDeadState { get; private set; }
    public FighterHitState mHitState { get; private set; }
    public FighterCombatState mCombatState { get; private set; }

    // 이번에 실행할 타겟 상태와 사거리
    [Header("다음 공격 정보")]
    public FighterAtkBaseState nextAttackState;//{ get; private set; }
    public float nextAttackRange;//{ get; private set; }

    //애니메이션
    public readonly int _animIDFightRange = Animator.StringToHash("IsFightRange");  //공격범위내에 있는지
    public readonly int _animIDIsDetected = Animator.StringToHash("IsDetected");    //탐지했는지
    public readonly int _animIDIsChase = Animator.StringToHash("IsChase");          //탐지&달리기 범위
    public readonly int _animIDHitTrigger = Animator.StringToHash("HitTrigger");    //피격 트리거
    public readonly int _animIDFightReady = Animator.StringToHash("IsFightReady");    //피격 트리거
    public readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex");  //공격 행동 인덱스
    public readonly int _animIDAttackTrigger = Animator.StringToHash("AttackTrigger"); //공격 트리거

    public FighterAtkBaseState fighterAtkJabState;
    public FighterAtkBaseState fighterAtkLowKickState;
    public FighterAtkBaseState fighterAtkHookState;
    public FighterAtkBaseState fighterAtkKickState;
    public FighterAtkBaseState fighterAtkJabCrossState;
    public FighterAtkBaseState fighterAtkFlyKickState;

    //스탯
    public FighterStatPreset fighterStat => stat as FighterStatPreset;
    public float detectedRange;
    public float detectedWalkSpeed;
    public float fightReadyRange;
    public float fightWalkSpeed;

    protected override void Awake()
    {
        base.Awake(); //컴포넌트를 받도록 부모의 Awake를 사용

    }

    public override void SetupStateMachine(EnemyStateMachine fsm)
    {
        //공격 먼저 할당
        mCombatState = new FighterCombatState(stateMachine, this);
        fighterAtkJabState = new FighterAtkJabState(stateMachine, this, fighterStat.Jab);
        fighterAtkLowKickState = new FighterAtkLowKickState(stateMachine, this, fighterStat.LowKick);
        fighterAtkHookState = new FighterAtkHookState(stateMachine, this, fighterStat.Hook);
        fighterAtkKickState = new FighterAtkKickState(stateMachine, this, fighterStat.Kick);
        fighterAtkJabCrossState = new FighterAtkJabCrossState(stateMachine, this, fighterStat.JabCross);
        fighterAtkFlyKickState = new FighterAtkFlyKickState(stateMachine, this, fighterStat.FlyKick);

        // 로컬 프로퍼티에 할당
        mIdleState = new FighterIdleState(stateMachine, this);
        mChaseState = new FighterChaseState(stateMachine, this);
        mDetectedState = new FighterDetectedState(stateMachine, this);
        mDeadState = new FighterDeadState(stateMachine, this);
        mHitState = new FighterHitState(stateMachine, this);

        // 상태 머신의 공통의 추상화된 변수들에 연결
        stateMachine.idleState = mIdleState;
        stateMachine.moveState = mChaseState;
        //stateMachine.attackState = mAttacState;
        stateMachine.deadState = mDeadState;
        stateMachine.hitState = mHitState;

        stateMachine.InitalizeState(mIdleState);
    }

    public override void InitializeEnemy()
    {
        // 1. 부모의 공통 스케일링 실행
        base.InitializeEnemy();

        // 2. Fighter만 가지고 있는 고유 스탯들 할당
        var fighterStat = stat as FighterStatPreset;
        if (fighterStat != null)
        {
            detectedRange = fighterStat.DetectedRange;
            detectedWalkSpeed = fighterStat.DetectedWalkSpeed;
            fightReadyRange = fighterStat.FightReadyRange;
            fightWalkSpeed = fighterStat.FightWalkSpeed;

            // Fighter 전용 NavMesh 설정
            if (navAgent != null)
                navAgent.angularSpeed = _AngularSpeed;

            if (playerDetector != null)
                playerDetector.sphereCollider.radius = detectedRange;
        }
    }

    // 다음 공격을 결정하는 함수
    public void DecideNextAttack()
    {
        //다음 공격을 "랜덤"으로 가져온다. <--추후 다른 로직 구현으로 변경 가능
        int rand = Random.Range(0, 6);

        //다음 공격 설정
        if (rand == 0)
        {
            nextAttackState = fighterAtkJabState;
            nextAttackRange = fighterStat.Jab.attackRange;
        }
        else if (rand == 1)
        {
            nextAttackState = fighterAtkLowKickState;
            nextAttackRange = fighterStat.LowKick.attackRange;
        }
        else if (rand == 2)
        {
            nextAttackState = fighterAtkHookState;
            nextAttackRange = fighterStat.Hook.attackRange;
        }
        else if (rand == 3)
        {
            nextAttackState = fighterAtkKickState;
            nextAttackRange = fighterStat.Kick.attackRange;
        }
        else if (rand == 4)
        {
            nextAttackState = fighterAtkJabCrossState;
            nextAttackRange = fighterStat.JabCross.attackRange;
        }
        else if (rand == 5)
        {
            nextAttackState = fighterAtkFlyKickState;
            nextAttackRange = fighterStat.FlyKick.attackRange;
        }
    }



    public void OnDeathHandler() { }

    public void AttackStart() => isAttacking = true;
    public void EndAttack() => isAttacking = false;



    public void EndHit() => isHit = false;


}
