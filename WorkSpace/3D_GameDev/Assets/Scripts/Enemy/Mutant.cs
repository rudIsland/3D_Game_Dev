using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mutant : Enemy
{
    public Animator animator;
    public readonly int _animIDWalk = Animator.StringToHash("WalkRange"); //탐지
    public readonly int _animIDRunning = Animator.StringToHash("RunningRange"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDPunchRange = Animator.StringToHash("PunchRange"); //공격
    public readonly int _animIDJumpAttack = Animator.StringToHash("JumpAttackRange"); //점프공격
    public readonly int _animIDSwipAttack = Animator.StringToHash("SwipRange"); //점프공격
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //맞기
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //죽음
    

    [Header("공격범위")]
    [SerializeField] private float JumpAttackRange = 8f;
    [SerializeField] private float PunchAttackRange = 1.0f;
    [SerializeField] private float SwipAttackRange = 1.5f;
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runningSpeed = 2.3f;

    [Header("점프공격 쿨타임")]
    [SerializeField] private float jumpAttackCooldown = 10f;
    private float jumpAttackTimer = 0f;

    private bool canJumpAttack = false;
    private float jumpRangeCheckTimer = 0f;
    private readonly float jumpRangeCheckInterval = 5f;

    [Header("탐지범위")]
    [SerializeField] private float RunningArange = 10f;
    [SerializeField] private float WalkArange = 5f;

    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool isWalk = false;

    [Header("탐지범위")]
    public EnemyWeapon leftWeapon;
    public EnemyWeapon rightWeapon;

    protected override void SetupStats()
    {
        detectRange = 15f; //탐지범위
        attackRange = 1.0f; //공격범위
        moveSpeed = runningSpeed; //이동속도
        angularSpeed = 180f; //회전속도
        statComp.stats.currentHP = statComp.stats.maxHP; //현재 체력설정
        level.SetLevel(3); //레벨설정

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI레벨설정
    }

    protected override void Start()
    {
        base.Start();
        OnDeath += HandleDeath;
    }

    protected override void Update()
    {
        if (statComp.stats.IsDead) return;

        UpdateDistanceToPlayer();
        UpdateDetectionStatus();
        UpdateAnimatorStates();

        if (!isRunning && !isWalk)
        {
            HandleUndetectedState();
            return;
        }

        HandleDetectedState();
        behaviorTree?.Evaluate();
    }

    //탐지범위 체크
    protected override void UpdateDetectionStatus()
    {
        isRunning = enemyMemory.distanceToPlayer <= RunningArange && !GameManager.Instance.playerStateMachine.isDead;
        isWalk = enemyMemory.distanceToPlayer <= WalkArange && !GameManager.Instance.playerStateMachine.isDead;
    }


    protected override void SetupTree()
    {
        // 탐지 트리
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(CheckRunningRange),
            new ActionNode(CheckWalkRange)
        });

        // 이동 트리
        ENode moveTree = new SelectorNode(new List<ENode> {
            new ActionNode(MoveToPlayer)
        });

        // 공격 트리
        ENode attackTree = new SequenceNode(new List<ENode> {
            new SelectorNode(new List<ENode>{
                new ActionNode(SwipAttack),
                new ActionNode(PunchAttack),
                //new ActionNode(JumpAttack)
            })
        });

        // 전체 트리 구성
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree, moveTree, attackTree
        });
    }

    // ------------------ 행동 노드 ------------------

    //탐지
    private ESTATE CheckWalkRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        bool isInRange = dist <= WalkArange;
        animator.SetBool(_animIDWalk, isInRange);
        return isInRange ? ESTATE.SUCCESS : ESTATE.FAILED;
    }

    private ESTATE CheckRunningRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        bool isInRange = dist <= RunningArange;
        animator.SetBool(_animIDRunning, isInRange);
        return isInRange ? ESTATE.SUCCESS : ESTATE.FAILED;
    }


    //이동
    private ESTATE MoveToPlayer()
    {
        if (!isRunning && !isWalk)
        {
            Debug.Log("[이동] 탐지 실패 → 이동 정지");
            SetAgentStop(true);
            return ESTATE.FAILED;
        }

        if (IsInPunchRange() || IsInSwipRange())
        {
            Debug.Log("[이동] 공격범위 진입 → 이동 정지");
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        if (!isAttacking)
        {
            Debug.Log("[이동] 플레이어에게 이동 중");
            SetAgentStop(false);
            agent.SetDestination(enemyMemory.player.position);
        }

        return ESTATE.RUN;
    }

    //공격

    private bool IsInPunchRange()
    {
        return Vector3.Distance(transform.position, enemyMemory.player.position) <= PunchAttackRange;
    }

    private bool IsInJumpRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        return dist <= JumpAttackRange;
    }

    private bool IsInSwipRange()
    {
        float dist = Vector3.Distance(transform.position, enemyMemory.player.position);
        return dist <= SwipAttackRange; // 겹쳐도 허용

    }


    // 공격
    private ESTATE PunchAttack()
    {
        if (!IsInPunchRange()) 
            return ESTATE.FAILED;


        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            agent.isStopped = true;
            animator.SetBool(_animIDAttack, true);
            animator.SetBool(_animIDPunchRange, true);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE JumpAttack()
    {
        if (jumpAttackTimer > 0f)
        {
            Debug.Log("[공격] 점프공격 불가 (쿨타임)");
            return ESTATE.FAILED;
        }

        if (!IsInJumpRange())
        {
            Debug.Log("[공격] 점프공격 거리 아님");
            return ESTATE.FAILED;
        }

        if (!RollChance(50f))
        {
            Debug.Log("[공격] 점프공격 확률 실패");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 점프공격 회전 중");
            if (!isAttacking)
                RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[공격] 점프공격 실행!");
            isAttacking = true;
            agent.isStopped = true;
            jumpAttackTimer = jumpAttackCooldown;
            animator.SetBool(_animIDJumpAttack, true);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE SwipAttack()
    {
        if (!IsInSwipRange()) return ESTATE.FAILED;

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            agent.isStopped = true;
            animator.SetBool(_animIDSwipAttack, true);
        }

        return ESTATE.SUCCESS;
    }




    /**********************************************************************************/


    // 데미지 받기
    public override void ApplyDamage(double damage)
    {
        stats.currentHP -= damage;
        stats.currentHP = Mathf.Max((float)stats.currentHP, 0);
        animator.SetBool(_animIDHit, true);

        CheckDie();

        statComp.UpdateHPUI();
    }


    // --- 유틸 함수 ---

    private void UpdateAnimatorStates()
    {
        // 탐지 실패 → 모두 정지
        if (!isWalk && !isRunning)
        {
            SetAgentStop(true);
            SetAgentRotation(false);

            animator.SetBool(_animIDWalk, false);
            animator.SetBool(_animIDRunning, false);
            return;
        }

        // 걷기 상태 → 걷기만 true
        if (isWalk)
        {
            animator.SetBool(_animIDWalk, true);
            animator.SetBool(_animIDRunning, false);
            agent.speed = walkSpeed;
            return;
        }

        // 달리기 상태 → 달리기만 true
        if (isRunning)
        {
            animator.SetBool(_animIDRunning, true);
            agent.speed = runningSpeed;
            return;
        }
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

    private void HandleUndetectedState()
    {
        SetAgentStop(true);
        SetAgentRotation(false);
    }

    private void HandleDetectedState()
    {
        if ((IsInPunchRange() || IsInSwipRange() || canJumpAttack) && !isAttacking)
        {
            SetAgentRotation(false);
            RotateTowardsPlayer();
        }
        else if (isAttacking)
        {
            SetAgentRotation(false);
        }
        else
        {
            SetAgentRotation(true);
        }
    }


    // Check Facing to Target(Player)
    private bool IsFacingTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.90f; // 0.90 이상이면 거의 정면을 보고 있다고 판단
    }


    /***************** Animation Event Function *******************/

    protected override void HandleDeath()
    {
        Debug.Log("적 죽는중...");
        animator.applyRootMotion = true;
        animator.SetTrigger(_animIDDead);

        //NavMeshAgent를 비활성화하기 전에 정지 먼저 설정
        SetAgentStop(true);
        agent.speed = moveSpeed; // ← 속도 리셋 (선택)

        agent.enabled = false; // 이제 안전하게 비활성화
        GetComponent<Collider>().enabled = false;
        GetComponent<Target>().enabled = false;
    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        agent.isStopped = true;
        SetAgentRotation(false); // ← 회전도 끊음
        animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (statComp.stats.IsDead) return;

        isAttacking = false;

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            SetAgentRotation(true); // ← 회전 다시 허용
        }

        animator.SetBool(_animIDAttack, false);
    }


    private void OffHit()
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
        animator.SetBool(_animIDHit, isAttacking);
    }

    public void OnLeftWeapon()
    {
        leftWeapon.gameObject.SetActive(true);
    }
    public void OnRightWeapon()
    {
        rightWeapon.gameObject.SetActive(true);
    }
    public void OffWeapon()
    {
        leftWeapon.gameObject.SetActive(false);
        rightWeapon.gameObject.SetActive(false);
    }

    private bool RollChance(float percent)
    {
        return Random.Range(0f, 100f) <= percent;
    }
}
