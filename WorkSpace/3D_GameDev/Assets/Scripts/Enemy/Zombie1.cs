using System.Collections.Generic;
using UnityEngine;

public class Zombie1 : Enemy
{

    public Animator animator;
    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //탐지
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //공격
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //공격

    public bool isAttacking = false;

    protected override void SetupStats()
    {
        detectRange = 6f; //탐지범위
        attackRange = 2.0f; //공격범위
        moveSpeed = 1.5f; //이동속도
    }

    protected override void SetupTree()
    {
        // 탐지 트리
        ENode detectTree = new SelectorNode(new List<ENode>
        {
            new ActionNode(CheckDetectRange)
        });

        // 이동 트리
        ENode moveTree = new SelectorNode(new List<ENode>
        {
            new ActionNode(MoveToPlayer)
        });

        // 공격 트리
        ENode attackTree = new SequenceNode(new List<ENode>
        {
            new ActionNode(CheckAttackRange),
            new SelectorNode(new List<ENode>
            {
                new ActionNode(NormalAttack)
            })
        });

        // 전체 트리 구성
        behaviorTree = new SequenceNode(new List<ENode>
        {
            detectTree,
            moveTree,
            attackTree
        });
    }

    // ------------------ 행동 노드 ------------------

    //탐지
    private ESTATE CheckDetectRange()
    {
        if (enemyMemory.isPlayerDetected)
        {
            //Debug.Log("플레이어 탐지됨!");
            animator.SetBool(_animIDFind, true);
            return ESTATE.SUCCESS;
        }
        //Debug.Log("플레이어 감지 실패");
        animator.SetBool(_animIDFind, false);
        return ESTATE.FAILED;
    }


    //이동
    private ESTATE MoveToPlayer()
    {
        if (enemyMemory.isInAttackRange)
        {
            return ESTATE.SUCCESS;
        }
        else 
        {
            OutOfAttackRange();
        }

        //Target Setting
        Vector3 target = new Vector3(enemyMemory.player.position.x, transform.position.y, enemyMemory.player.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, target); //distance
        
        //Move but, if isAttacking is true.. not MoveTowards
        if(!isAttacking) 
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);


        //move to Player is Lerp
        if (!isAttacking)
        {
            Vector3 direction = (target - transform.position).normalized; // Move Direct Vecter
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction); //Calculate Forward Quaternion
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, //now rotation vector
                    targetRotation,     //target rotation vector
                    Time.deltaTime * 5f); // rotation speed
            }
        }

        // 공격 범위 내에 있고, 회전이 완료되면 공격 시작
        float angle = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(target - transform.position));
        if (distanceToPlayer <= attackRange && angle < 20f) // 회전 각도 체크
        {
            return ESTATE.SUCCESS; // 공격 가능
        }

        //Debug.Log("이동 중...");

        return ESTATE.RUN;
    }



    //공격
    private ESTATE CheckAttackRange() //if true -> AttackActionNode gogo
    {

        if (!enemyMemory.isInAttackRange) //이동 시퀀스에서 true를 반환해야 여기를 들어올 수 있어서 소용없음.
        {
            Debug.Log("공격 거리 아님");
            OutOfAttackRange();
            return ESTATE.FAILED;
        }

        Debug.Log("공격 범위 안에 있음");
        animator.SetBool(_animIDAttackRange, true);
        return ESTATE.SUCCESS;
    }


    // 공격
    private ESTATE NormalAttack()
    {
        if (!enemyMemory.isInAttackRange)
        {
            Debug.Log("공격 범위 벗어남!");
            OutOfAttackRange();
            return ESTATE.FAILED;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            NormalAttackingStart();
            Debug.Log("공격 시작!");
        }

        return ESTATE.SUCCESS;
    }



    private void NormalAttackingStart() //Animation Event and Code Function
    {
        isAttacking = true;
        animator.SetBool(_animIDAttack, isAttacking);
    }
    private void NormalAttackingEnd() //Animation Event Function
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
    }
    private void OutOfAttackRange()
    {
        animator.SetBool(_animIDAttackRange, false);
    }

}
