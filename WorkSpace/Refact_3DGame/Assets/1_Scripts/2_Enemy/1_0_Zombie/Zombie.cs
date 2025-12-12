
using UnityEngine;

public class Zombie : Enemy , IDamageable
{
   
    //애니메이션
    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //맞기
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //공격
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //죽음

    public float distanceToPlayer { get; private set; }

    public EnemyStatPreset stat;

    protected override void Awake()
    {
        base.Awake(); //부모의 Awake에서 컴포넌트를 받아오게 한다.

        //ScriptObject의 값을 복사하여 원본에 영향X
        maxHP = stat.maxHP;                 //최대체력
        currentHP = maxHP;                  //현재 체력
        attackPower = stat.attack;             //공격력
        defencePower = stat.defense;            //방어력
        detectRange = stat.DetectRange;     //탐지사정거리
        attackRange = stat.AttackRange;     //공격사정거리
        moveSpeed = stat.MoveSpeed;         //움직임 속도
        angularSpeed = stat.AngularSpeed;   //회전 속도
    }

    protected void UpdateDistanceToPlayer()
    {
        //플레이어와의 거리 계산으로 탐지
        //Vector3 toPlayer = player.position - transform.position;
        //toPlayer.y = 0f; // 평면 거리
        //distanceToPlayer = toPlayer.magnitude;
    }


    public override void SetupStateMachine(EnemyStateMachine stateMachine)
    {
        stateMachine.Initalize(new ZombieIdleState(stateMachine));
    }

    public void TakeDamage(double damage)
    {
        currentHP -= damage;

        if (isDead)
            stateMachine.SwitchState(new ZombieDeadState(stateMachine));
    }
}
