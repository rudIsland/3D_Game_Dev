using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mutant : Enemy
{
    private enum AttackType
    {
        Jump,
        Swip,
        Punch
    }

    public readonly int _animIDIdle = Animator.StringToHash("Idle"); //서있기
    public readonly int _animIDHit = Animator.StringToHash("IsHit"); //탐지
    public readonly int _animIDWalk = Animator.StringToHash("WalkRange"); //탐지
    public readonly int _animIDRunning = Animator.StringToHash("RunningRange"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDAttackIndex = Animator.StringToHash("AttackIndex"); //공격번호
    public readonly int _animIDDead = Animator.StringToHash("IsDead"); //죽음
    public readonly int _animIDAttackTrigger = Animator.StringToHash("AttackTrigger"); //공격 트리거

    private const int ATTACK_JUMP = 1;
    private const int ATTACK_PUNCH = 2;
    private const int ATTACK_SWIP = 3;

    [Header("공격범위")]
    [SerializeField] private float JumpAttackRange = 3f;
    [SerializeField] private float PunchAttackRange = 1.0f;
    [SerializeField] private float SwipAttackRange = 2f;
    [SerializeField] private bool isJumpAttackRange = false;
    [SerializeField] private bool isPunchAttackRange = false;
    [SerializeField] private bool isSwipAttackRange = false;

    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runningSpeed = 3f;

    [Header("공격 확률")]
    [SerializeField] private float jumpAttackChance = 0.5f;  // 50% 확률
    [SerializeField] private float SwipAttackChance = 0.6f; // 60% 확률

    [Header("점프공격 쿨타임")]
    [SerializeField] private float jumpAttackCooldown = 5f;
    private float jumpAttackTimer = 0f;

    private bool isJumpAttacking = false;

    [Header("탐지범위")]
    [SerializeField] private float RunningArange = 10f;
    [SerializeField] private float WalkArange = 5f;

    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool isWalk = false;

    [Header("무기")]
    public EnemyWeapon leftWeapon;
    public EnemyWeapon rightWeapon;

    protected override void SetupStats()
    {
        detectRange = 10f; //탐지범위
        attackRange = 1.0f; //공격범위
        moveSpeed = runningSpeed; //이동속도
        angularSpeed = 180f; //회전속도

        level.SetLevel(3); //레벨설정

        enemyStat.maxHP = 500;
        enemyStat.ATK = 25f;
        enemyStat.DEF = 15f;
        enemyStat.currentHP = enemyStat.maxHP; //현재 체력설정
        deathEXP = 1500;

        GetComponentInChildren<EnemyGUI>()?.UpdateLevel(); //GUI레벨설정
    }

    protected override void Start()
    {
        base.Start();
        OnDeath += HandleDeath;
        leftWeapon.gameObject.SetActive(false);
        rightWeapon.gameObject.SetActive(false);
    }

    protected override void Update()
    {
        if (enemyStat.IsDead) return;
        if (Player.Instance.playerStateMachine.currentState is PlayerDeadState)
        {
            ChangeDefaultMtl();
            return;
        }

        JumpAttackCoolTime();

        UpdateDistanceToPlayer(); //플레이어와의 거리 계산
        UpdateDetectionStatus(); //각 탐지범위 계산
        UpdateAnimatorStates(); //애니메이션 재생


        if (!isRunning && !isWalk) //걷거나 뛰는 상태가 아닐경우
        {
            HandleUndetectedState(); //agent로 추적 false
            return;
        }

        HandleDetectedState();
        behaviorTree?.Evaluate();
    }

    private void JumpAttackCoolTime()
    {
        jumpAttackTimer -= Time.deltaTime; //시간 흐름대로 줄어들도록 설정
    }


    //탐지범위 체크
    protected override void UpdateDetectionStatus()
    {
       // if (!GameManager.Instance.player.isDead) return; 
        isRunning = enemyMemory.distanceToPlayer <= RunningArange;
        isWalk = enemyMemory.distanceToPlayer <= WalkArange;
        isJumpAttackRange = enemyMemory.distanceToPlayer <= JumpAttackRange;
        isPunchAttackRange = enemyMemory.distanceToPlayer <= PunchAttackRange;
        isSwipAttackRange = enemyMemory.distanceToPlayer <= SwipAttackRange;

        if (isTarget) return; //타겟일땐 바꾸지않게

        // 상태 변경된 경우만 머터리얼 바꿈
        if (!enemyMemory.isPlayerDetected)
        {
            ChangeDefaultMtl();
        }
    }


    protected override void SetupTree()
    {
        // 탐지 트리
        ENode detectTree = new SelectorNode(new List<ENode> {
            new ActionNode(CheckRunningRange),
            new ActionNode(CheckWalkRange)
        });

        ENode attackAndMoveTree = new SelectorNode(new List<ENode> {
            new ActionNode(SelectAndPerformAttack),
            new ActionNode(MoveToPlayer)
        });

        // 전체 트리 구성
        behaviorTree = new SequenceNode(new List<ENode> {
            detectTree,
            attackAndMoveTree
        });
    }


    // ------------------ 행동 노드 ------------------

    //탐지
    private ESTATE CheckWalkRange()
    {
        animator.SetBool(_animIDWalk, isWalk);
        return isWalk ? ESTATE.SUCCESS : ESTATE.FAILED;
    }

    private ESTATE CheckRunningRange()
    {
        animator.SetBool(_animIDRunning, isRunning);
        return isRunning ? ESTATE.SUCCESS : ESTATE.FAILED;
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

        // 공격 중엔 이동하지 않음
        if (isAttacking)
        {
            Debug.Log("[이동] 공격 중 → 이동 정지");
            ChangeDetectedMtl();
            SetAgentStop(true);
            return ESTATE.SUCCESS;
        }

        // 공격 실패 후 이동
        Debug.Log("[이동] 플레이어에게 이동 중");

        ChangeDetectedMtl();
        SetAgentStop(false);
        agent.SetDestination(enemyMemory.player.position);

        return ESTATE.RUN;
    }


    private ESTATE SelectAndPerformAttack()
    {
        float jumpWeight = 0f;
        float swipWeight = 1f;
        float punchWeight = 2f;

        // 점프 공격 조건 + 거리 기반 가중치 감소
        if (isJumpAttackRange && jumpAttackTimer <= 0f)
        {
            float distance = enemyMemory.distanceToPlayer;

            if (distance <= 1.5f)
                jumpWeight = 1f;
            else if (distance <= 2.5f)
                jumpWeight = 5f;
            else
                jumpWeight = 15f;
        }

        if (isSwipAttackRange)
            swipWeight += 10f;

        if (isPunchAttackRange)
            punchWeight += 10f;

        float totalWeight = jumpWeight + swipWeight + punchWeight;
        if (totalWeight <= 0f) return ESTATE.FAILED;

        float roll = Random.Range(0f, totalWeight);

        if (roll <= jumpWeight)
            return JumpAttack();

        roll -= jumpWeight;
        if (roll <= swipWeight)
            return PunchAttack();

        return SwipAttack();
    }

    // 공격
    private ESTATE PunchAttack()
    {
        if (!isPunchAttackRange)
        {
            Debug.Log("[공격] 펀치 공격 실패: 범위 아님");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 펀치 공격 회전 중...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[공격] 펀치 공격 시작");
            NormalAttackingStart(); //원래 없는건데 추가해봄 너무 피격이 쉬워서

            animator.SetInteger(_animIDAttackIndex, ATTACK_PUNCH);
            animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }

    private ESTATE JumpAttack()
    {
        if (!isJumpAttackRange || jumpAttackTimer > 0f || !RollChance(jumpAttackChance))
            return ESTATE.FAILED;

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 점프 공격 회전 중...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            isJumpAttacking = true;
            jumpAttackTimer = jumpAttackCooldown;

            NormalAttackingStart(); //원래 없는건데 추가해봄 너무 피격이 쉬워서
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;

            animator.SetInteger(_animIDAttackIndex, ATTACK_JUMP);
            animator.SetTrigger(_animIDAttackTrigger);
        }
        return ESTATE.SUCCESS;
    }

    private ESTATE SwipAttack()
    {
        if (!isSwipAttackRange)
        {
            Debug.Log("[공격] 스윕 공격 실패: 범위 아님");
            return ESTATE.FAILED;
        }

        if (!RollChance(SwipAttackChance))
        {
            Debug.Log("[공격] 스윕 공격 실패: 확률 실패");
            return ESTATE.FAILED;
        }

        if (!IsFacingTarget(enemyMemory.player.position))
        {
            Debug.Log("[공격] 스윕 공격 회전 중...");
            if (!isAttacking) RotateTowardsPlayer();
            return ESTATE.RUN;
        }

        if (!isAttacking)
        {
            Debug.Log("[공격] 스윕 공격 시작");
            NormalAttackingStart(); //원래 없는건데 추가해봄 너무 피격이 쉬워서

            animator.SetInteger(_animIDAttackIndex, ATTACK_SWIP);
            animator.SetTrigger(_animIDAttackTrigger);
        }

        return ESTATE.SUCCESS;
    }




    /**********************************************************************************/


    // 데미지 받기
    public override void ApplyDamage(double damage)
    {
        enemyStat.currentHP -= damage;
        enemyStat.currentHP = Mathf.Max((float)enemyStat.currentHP, 0);

        if (!isAttacking)
        {
            animator.SetBool(_animIDHit,true);
        }

        CheckDie();

        UpdateHPUI();
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
            animator.SetBool(_animIDWalk, false);
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
        if ((isPunchAttackRange || isSwipAttackRange || jumpAttackTimer <= 0f) && !isAttacking)
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

        base.HandleDeath();
    }

    private void NormalAttackingStart()
    {
        isAttacking = true;
        animator.applyRootMotion = true;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        animator.SetBool(_animIDAttack, true);
    }

    private void NormalAttackingEnd()
    {
        if (enemyStat.IsDead) return;

        isAttacking = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;

            if (isJumpAttacking)
            {
                agent.updatePosition = true;
                agent.updateRotation = true;
                isJumpAttacking = false;
            }
        }

        animator.ResetTrigger(_animIDAttackTrigger);
        animator.SetInteger(_animIDAttackIndex, 0);
        animator.SetBool(_animIDAttack, false);
    }


    private void OffHit()
    {
        isAttacking = false;
        animator.SetBool(_animIDHit, false);
        animator.SetBool(_animIDAttack, isAttacking);
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
        return Random.value <= percent;
    }
}
