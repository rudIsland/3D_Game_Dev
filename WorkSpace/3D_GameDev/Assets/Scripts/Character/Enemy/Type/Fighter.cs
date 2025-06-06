using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class Fighter : Enemy
{
    private enum AttackType {
        FlyKick,
        JapCross,
        Kick
    }
    public readonly int _animIDIdle = Animator.StringToHash("Idle"); //서있기
    public readonly int _animIDWalk = Animator.StringToHash("IsWalk"); //걷기
    public readonly int _animIDDectected = Animator.StringToHash("IsDetected"); //탐지
    public readonly int _animIDFightDectected = Animator.StringToHash("FightDecteced"); //공격준비탐지
    public readonly int _animIDFight = Animator.StringToHash("Fight"); //공격모션 범위
    public readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex"); //공격모션 범위
    public readonly int _animIDAttackTrigger = Animator.StringToHash("AttackTrigger"); //공격모션 범위
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //죽음

    [Header("탐지범위")]
    private float DectectedRange = 7f;
    private float FightDectectedRange = 4.5f;
    private float FightRange = 1.55f;
    [SerializeField] private bool isDecteced;

    // 공격 관련 타이머 & 범위
    private float jumpAttackTimer = 3f;
    private float FlyKickRange = 1.5f;
    private float JapCrossRange = 1.15f;
    private float KickRange = 1.3f;
    private float HookRange = 1.2f;
    private float JapRange = 1.0f;
    private float LowKickRange = 1.0f;
    //private float StepForwardRange = 1.5f;

    private const int ATTACK_JAP = 1;
    private const int ATTACK_LOWKICK = 2;
    private const int ATTACK_HOOK = 3;
    private const int ATTACK_KICK = 4;
    private const int ATTACK_JABCROSS = 5;
    private const int ATTACK_FLYKICK = 6;

    [Header("무기")]
    [SerializeField] private EnemyWeapon LeftHandWeapon; //왼쪽 손
    [SerializeField] private EnemyWeapon RightHandWeapon; //오른쪽 손
    [SerializeField] private EnemyWeapon LeftFootWeapon; //왼쪽 발
    [SerializeField] private EnemyWeapon RightFootWeapon; //오른쪽 발
    protected override void Awake()
    {
        base.Awake(); //자기자신의 컴포넌트 가져오게
    }
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        SetupStats();

        OnDeath += HandleDeath; //죽음 델리게이트
        LeftHandWeapon.gameObject.SetActive(false);
        RightHandWeapon.gameObject.SetActive(false);
        LeftFootWeapon.gameObject.SetActive(false);
        RightFootWeapon.gameObject.SetActive(false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (enemyStat.IsDead) return;
        if (Player.Instance.playerStateMachine.currentState is PlayerDeadState)
        {
            ChangeDefaultMtl();
            return;
        }

        UpdateDistanceToPlayer();
        HandleDetectedState();
        

        if (jumpAttackTimer > 0f)
            jumpAttackTimer -= Time.deltaTime;

        behaviorTree?.Evaluate();
    }

    private void HandleDetectedState()
    {
        if (!(FightRange >= enemyMemory.distanceToPlayer))
        {
            // 공격 범위 밖: 이동/회전 가능
            SetAgentStop(false);
            SetAgentRotation(true);
        }
        else if (!isAttacking)
        {
            // 공격 범위 안, 공격 전: 회전만 허용
            SetAgentStop(true);
            SetAgentRotation(false);
            RotateTowardsPlayer();
        }
        else
        {
            // 공격 중: 이동, 회전 금지
            SetAgentStop(true);
            SetAgentRotation(false);
        }
    }


    protected override void RotateTowardsPlayer()
    {
        Vector3 direction = (enemyMemory.player.position - transform.position).normalized;
        direction.y = 0;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
    }

    protected override void SetupStats()
    {
        detectRange = DectectedRange; //탐지범위
        attackRange = FightRange; //공격범위
        moveSpeed = 1.5f; //이동속도
        angularSpeed = 260f; //회전속도

        level.SetLevel(20); //레벨설정

        enemyStat.maxHP = 1000;
        enemyStat.ATK = 40f;
        enemyStat.DEF = 30f;
        enemyStat.currentHP = enemyStat.maxHP; //현재 체력설정
        deathEXP = 4500;

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI레벨설정
    }
    /****************************** 트리노드  ************************************/
    protected override void SetupTree()
    {
        // 탐지 트리
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(DectectedPlayer),
        });

        // SetupTree() 수정
        ENode attackAndMoveTree = new SelectorNode(new List<ENode> {
            new ActionNode(FightSelector),
            new ActionNode(MoveToPlayer)
        });

        // 전체 트리 구성
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree,
            attackAndMoveTree
        });
    }

    private ESTATE DectectedPlayer()
    {
        //if (GameManager.Instance.player.isDead) return ESTATE.FAILED;

        float distance = enemyMemory.distanceToPlayer;

        isDecteced = distance <= DectectedRange;
        // 탐지 범위 설정
        animator.SetBool(_animIDDectected, isDecteced);
        animator.SetBool(_animIDFightDectected, distance <= FightDectectedRange);
        animator.SetBool(_animIDWalk, (isDecteced && distance >= FightRange));
        animator.SetBool(_animIDFight, distance <= FightRange);

        // 가장 넓은 탐지 거리 내에 들어오면 SUCCESS 반환
        if (distance <= DectectedRange)
        {
            ChangeDetectedMtl();
            return ESTATE.SUCCESS;
        }

        // 탐지 실패 시 바로 정지
        SetAgentStop(true);
        ChangeDefaultMtl();
        return ESTATE.FAILED;

    }

    private ESTATE FacePlayerCheck()
    {
        if (isAttacking) return ESTATE.SUCCESS; // 공격 중이면 회전 금지

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE FightSelector()
    {
        float flyKickWeight = 0f;
        float japCrossWeight = 2f;
        float hookWeight = 2f;
        float kickWeight = 2f;
        float japWeight = 2f;
        float lowKickWeight = 2f;

        float distance = enemyMemory.distanceToPlayer;

        if (distance <= JapRange)
            japWeight += 8f;

        if (distance <= LowKickRange)
            lowKickWeight += 8f;

        if (distance <= HookRange)
            hookWeight += 8f;

        if (distance <= KickRange)
            kickWeight += 8f;

        if (distance <= JapCrossRange)
            japCrossWeight += 8f;

        if (distance <= FlyKickRange && jumpAttackTimer <= 0f)
            flyKickWeight = 5f;

        float totalWeight = flyKickWeight + japCrossWeight + hookWeight + kickWeight + japWeight + lowKickWeight;
        if (totalWeight <= 0f) return ESTATE.FAILED;

        float roll = Random.Range(0f, totalWeight);

        if (roll <= flyKickWeight)
            return FlyKickAttack();

        roll -= flyKickWeight;
        if (roll <= japCrossWeight)
            return JapCrossAttack();

        roll -= japCrossWeight;
        if (roll <= hookWeight)
            return HookAttack();

        roll -= hookWeight;
        if (roll <= kickWeight)
            return KickAttack();

        roll -= kickWeight;
        if (roll <= lowKickWeight)
            return LowKickAttack();

        return JapAttack();
    }


    //이동
    private ESTATE MoveToPlayer()
    {
        if (!isDecteced)
        {
            Debug.Log("[이동] 탐지 실패 → 이동 정지");
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        // 공격 중엔 이동하지 않음
        if (isAttacking)
        {
            Debug.Log("[이동] 공격 중 → 이동 정지");
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        // 공격 실패 후 이동
        Debug.Log("[이동] 플레이어에게 이동 중");
        SetAgentStop(false);
        if (agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(enemyMemory.player.position);

        return ESTATE.RUN;
    }

    //공격

    private ESTATE JapCrossAttack()
    {
        // 공격 조건 검사 후 애니메이션 재생
        if (! (JapCrossRange >= enemyMemory.distanceToPlayer))
        {
            Debug.Log("[공격] 잽크로스 공격 실패: 범위 아님");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 잽크로스 공격 회전 중...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[공격] 잽크로스 공격 시작");
            NormalAttackingStart(); //원래 없는건데 추가해봄 너무 피격이 쉬워서

            animator.SetInteger(_animIDAttackIndex, ATTACK_JABCROSS);
            animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE KickAttack()
    {
        // 공격 조건 검사 후 애니메이션 재생
        if (!(KickRange >= enemyMemory.distanceToPlayer))
        {
            Debug.Log("[공격] 킥 공격 실패: 범위 아님");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 킥 공격 회전 중...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[공격] 킥 공격 시작");
            NormalAttackingStart(); //원래 없는건데 추가해봄 너무 피격이 쉬워서

            animator.SetInteger(_animIDAttackIndex, ATTACK_KICK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE FlyKickAttack()
    {
        // 공격 조건 검사 후 애니메이션 재생
        if (!(FlyKickRange >= enemyMemory.distanceToPlayer))
        {
            Debug.Log("[공격] 플라이킥 공격 실패: 범위 아님");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 플라이킥 공격 회전 중...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[공격] 플라이킥 공격 시작");
            NormalAttackingStart(); //원래 없는건데 추가해봄 너무 피격이 쉬워서
            jumpAttackTimer = 3f;
            animator.SetInteger(_animIDAttackIndex, ATTACK_FLYKICK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE HookAttack()
    {
        if (!(HookRange >= enemyMemory.distanceToPlayer)) return ESTATE.FAILED;
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }
        if (!isAttacking)
        {
            NormalAttackingStart();
            animator.SetInteger(_animIDAttackIndex, ATTACK_HOOK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE LowKickAttack()
    {
        if (!(LowKickRange >= enemyMemory.distanceToPlayer)) return ESTATE.FAILED;
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }
        if (!isAttacking)
        {
            NormalAttackingStart();
            animator.SetInteger(_animIDAttackIndex, ATTACK_LOWKICK);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE JapAttack()
    {
        if (!(JapRange >= enemyMemory.distanceToPlayer)) return ESTATE.FAILED;
        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }
        if (!isAttacking)
        {
            NormalAttackingStart();
            animator.SetInteger(_animIDAttackIndex, ATTACK_JAP);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    /****************************** HiT & 무기  ************************************/

    private void OffHit()
    {
        isAttacking = false;
        //animator.SetBool(_animIDHit, false);
        //animator.SetBool(_animIDAttack, isAttacking);
    }

    public void OnLeftHandWeapon()
    {
        LeftHandWeapon.gameObject.SetActive(true);
    }
    public void OnRightHandWeapon()
    {
        RightHandWeapon.gameObject.SetActive(true);
    }
    public void OnLeftFootWeapon()
    {
        LeftFootWeapon.gameObject.SetActive(true);
    }
    public void OnRightFootWeapon()
    {
        RightFootWeapon.gameObject.SetActive(true);
    }
    public void OffWeapon()
    {
        LeftHandWeapon.gameObject.SetActive(false);
        RightHandWeapon.gameObject.SetActive(false);
        LeftFootWeapon.gameObject.SetActive(false);
        RightFootWeapon.gameObject.SetActive(false);
    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        animator.applyRootMotion = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        //animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (enemyStat.IsDead) return;

        isAttacking = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            animator.ResetTrigger(_animIDAttackTrigger);
            animator.SetInteger(_animIDAttackIndex, 0);
        }

    }

    /****************************** Agent And 검사 Logic  ************************************/

    protected override void HandleDeath()
    {
        Debug.Log("적 죽는중...");
        animator.applyRootMotion = true;
        animator.SetTrigger(_animIDDead);

        //NavMeshAgent를 비활성화하기 전에 정지 먼저 설정
        SetAgentStop(true);

        agent.enabled = false; // 이제 안전하게 비활성화
        GetComponent<Collider>().enabled = false;
        GetComponent<Target>().enabled = false;

        base.HandleDeath();
    }

    private bool IsFacingTarget(Vector3 target)
    {
        //Vector의 방향을 구하고 정규화(기하벡터)
        Vector3 directionToTarget = (target - transform.position).normalized;
        //파이터 방향과 플레이어 방향으로 각도 구하기
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.98f; // 0.95 이상이면 거의 정면을 보고 있다고 판단
    }

    private void SetAgentStop(bool stop)
    {
        if (!agent.enabled) return;

        if (agent.isStopped != stop)
            agent.isStopped = stop;
    }

    private void SetAgentRotation(bool enable)
    {
        if (agent.updateRotation != enable)
            agent.updateRotation = enable;
    }

    // 데미지 받기
    public override void ApplyDamage(double damage)
    {
        enemyStat.currentHP -= damage;
        enemyStat.currentHP = Mathf.Max((float)enemyStat.currentHP, 0);

        //if (!isAttacking)
        //{
        //    animator.SetBool(_animIDHit, true);
        //}
        CheckDie();

        UpdateHPUI();
    }
}
