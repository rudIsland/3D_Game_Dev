using System.Collections.Generic;
using UnityEngine;

public class Zombie1 : Enemy
{

    public Animator animator;
    public readonly int _animIDFind = Animator.StringToHash("IsFind"); //Ž��
    public readonly int _animIDAttack = Animator.StringToHash("IsAttack"); //����
    public readonly int _animIDAttackRange = Animator.StringToHash("InAttackRange"); //����

    //public bool isAttacking = false;


    protected override void SetupStats()
    {
        detectRange = 6f; //Ž������
        attackRange = 1.2f; //���ݹ���
        moveSpeed = 1.5f; //�̵��ӵ�
    }

    protected override void SetupTree()
    {
        // Ž�� Ʈ��
        ENode detectTree = new SelectorNode(new List<ENode>
        {
            new ActionNode(CheckDetectRange)
        });

        // �̵� Ʈ��
        ENode moveTree = new SelectorNode(new List<ENode>
        {
            new ActionNode(MoveToPlayer)
        });

        // ���� Ʈ��
        ENode attackTree = new SequenceNode(new List<ENode>
        {
            new ActionNode(CheckAttackRange),
            new SelectorNode(new List<ENode>
            {
                new ActionNode(NormalAttack)
            })
        });

        // ��ü Ʈ�� ����
        behaviorTree = new SequenceNode(new List<ENode>
        {
            detectTree,
            moveTree,
            attackTree
        });
    }

    // ------------------ �ൿ ��� ------------------

    //Ž��
    private ESTATE CheckDetectRange()
    {
        if (enemyMemory.isPlayerDetected)
        {
            //Debug.Log("�÷��̾� Ž����!");
            animator.SetBool(_animIDFind, true);
            return ESTATE.SUCCESS;
        }
        //Debug.Log("�÷��̾� ���� ����");
        animator.SetBool(_animIDFind, false);
        return ESTATE.FAILED;
    }


    //�̵�
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
        if (!isAttacking)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        }

        // Target to Rotate
        RotateTowardsTarget(target);

        // If in AttackRagne and FacingTargeting is Success
        if (distanceToPlayer <= attackRange && IsFacingTarget(target))
        {
            return ESTATE.SUCCESS; // ���� ����
        }

        //Debug.Log("�̵� ��...");

        return ESTATE.RUN;
    }



    //����
    private ESTATE CheckAttackRange() //if true -> AttackActionNode gogo
    {

        // ȸ�� �Ϸ� ���� üũ
        Vector3 target = new Vector3(enemyMemory.player.position.x, transform.position.y, enemyMemory.player.position.z);
        if (!IsFacingTarget(target))
        {
            RotateTowardsTarget(target); // ���� ������ �ٶ󺸵��� ��
            return ESTATE.RUN; // �������� �ʰ� ȸ�� ����
        }

        Debug.Log("���� ���� �ȿ� ����");
        animator.SetBool(_animIDAttackRange, true);
        return ESTATE.SUCCESS;
    }

    // ��ǥ �������� ȸ�� (�ε巯�� ȸ��)
    private void RotateTowardsTarget(Vector3 target)
    {
        if (!isAttacking)
        {
            Vector3 direction = (target - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    // �÷��̾ ���ϰ� �ִ��� Ȯ��
    private bool IsFacingTarget(Vector3 target)
    {
        Vector3 directionToTarget = (target - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, directionToTarget);
        return dot > 0.95f; // 0.95 �̻��̸� ���� ������ ���� �ִٰ� �Ǵ�
    }


    // ����
    private ESTATE NormalAttack()
    {
        if (!enemyMemory.isInAttackRange)
        {
            Debug.Log("���� ���� ���!");
            OutOfAttackRange();
            return ESTATE.FAILED;
        }

        if (!isAttacking)
        {
            isAttacking = true;
            NormalAttackingStart();
            Debug.Log("���� ����!");
        }

        return ESTATE.SUCCESS;
    }


    //Animation Event Function
    private void NormalAttackingStart() //Animation Event and Code Function
    {
        isAttacking = true;
        animator.SetBool(_animIDAttack, isAttacking);
    }
    private void NormalAttackingEnd()
    {
        isAttacking = false;
        animator.SetBool(_animIDAttack, isAttacking);
    }

    private void OnWeapon()
    {
        weapon.gameObject.SetActive(true);
    }
    private void OffWeapon()
    {
        weapon.gameObject.SetActive(false);
    }

    private void OutOfAttackRange()
    {
        animator.SetBool(_animIDAttackRange, false);
    }

}
