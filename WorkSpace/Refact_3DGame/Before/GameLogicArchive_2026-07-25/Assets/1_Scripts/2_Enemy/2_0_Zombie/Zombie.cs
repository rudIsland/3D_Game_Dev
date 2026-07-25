
using UnityEngine;

public class Zombie : Enemy
{
    //좀비의 상태들
    public ZomBIdleState mIdleState { get; private set; }
    public ZomBMoveState mMoveState { get; private set; }
    public ZomBAttackState mAttackState { get; private set; }
    public ZomBDeadState mDeadState { get; private set; }
    public ZomBHitState mHitState { get; private set; }

    //애니메이션
    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //맞기
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //공격범위

    //좀비의 스탯정보
    public ZombieStatPreset zombieStat => stat as ZombieStatPreset; //형변환 스탯

    protected override void Awake()
    {
        base.Awake(); //부모의 Awake에서 컴포넌트를 받아오게 한다.

    }
    public override void InitializeEnemy()
    {
        // [0] 에러 방지: 컴포넌트 체크
        if (mainRenderer == null) mainRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        // [1] 레벨 스케일링 (HP, 공격력, 방어력, 경험치)
        int playerLevel = (StageManager.Instance != null && StageManager.Instance.player != null)
                          ? StageManager.Instance.player.currentLevel : 1;
        _Level = playerLevel;
        int factor = _Level - 1;

        _MaxHP = zombieStat.maxHP + (zombieStat.hpGrowthPerLevel * factor);
        _CurrentHP = _MaxHP;
        _AttackPower = zombieStat.attack + (zombieStat.atkGrowthPerLevel * factor);
        _DefencePower = zombieStat.defense + (zombieStat.defGrowthPerLevel * factor);
        _DeathEXP = zombieStat.baseDeathEXP + (zombieStat.expGrowthPerLevel * factor);

        // [2] 공통 범위 스탯 할당 (프리셋의 기본값들)
        _DetectRange = zombieStat._DetectRange;
        _AttackRange = zombieStat._AttackRange;
        _MoveSpeed = zombieStat._MoveSpeed;
        _AngularSpeed = zombieStat._AngularSpeed;

        // [3] 나머지 공통 초기화 로직 (물리/UI/상태)
        isDead = false;
        isHit = false;
        isTarget = false;
        isAttacking = false;
        isDetected = false;

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = true;

        if (navAgent != null) { navAgent.enabled = true; navAgent.ResetPath(); }

        if (enemyGUI != null)
        {
            enemyGUI.SetVisible(true);
            enemyGUI.SetLevel(_Level);
            enemyGUI.UpdateHPUI(_CurrentHP, _MaxHP);
        }

        ChangeDefaultMtl(); // 에러 방지된 함수 호출

        if (animator != null) { animator.Rebind(); animator.Update(0f); }
        if (stateMachine != null) stateMachine.SwitchState(stateMachine.idleState);
    }

    public override void SetupStateMachine(EnemyStateMachine stateMachine)
    {
        // 로컬 프로퍼티에 먼저 할당
        mIdleState = new ZomBIdleState(stateMachine, this);
        mMoveState = new ZomBMoveState(stateMachine, this);
        mAttackState = new ZomBAttackState(stateMachine, this, zombieStat.SwingAttack);
        mDeadState = new ZomBDeadState(stateMachine, this);
        mHitState = new ZomBHitState(stateMachine, this);

        // 상태 머신의 추상화된 변수들에 연결
        stateMachine.idleState = mIdleState;
        stateMachine.moveState = mMoveState;
        stateMachine.attackState = mAttackState;
        stateMachine.deadState = mDeadState;
        stateMachine.hitState = mHitState;

        stateMachine.InitalizeState(mIdleState);
    }


    public void AttackStart() => isAttacking = true;
    public void EndAttack() => isAttacking = false;

    public void EndHit() => isHit = false;

}
