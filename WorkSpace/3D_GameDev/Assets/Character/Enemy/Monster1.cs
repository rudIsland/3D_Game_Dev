using System.Collections.Generic;
using UnityEngine;

public class Monster1 : Enemy
{
    protected override void SetupStats()
    {
        detectRange = 8f;
        attackRange = 1.8f;
        moveSpeed = 2.4f;
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
            Debug.Log("�÷��̾� Ž����!");
            return ESTATE.SUCCESS;
        }
        Debug.Log("�÷��̾� ���� ����");
        return ESTATE.FAILED;
    }


    //�̵�
    private ESTATE MoveToPlayer()
    {
        if (enemyMemory.isInAttackRange)
            return ESTATE.SUCCESS;

        Vector3 target = new Vector3(enemyMemory.player.position.x, transform.position.y, enemyMemory.player.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        Debug.Log("�̵� ��...");
        return ESTATE.RUN;
    }


    //����
    private ESTATE CheckAttackRange()
    {
        if (!enemyMemory.isInAttackRange)
        {
            Debug.Log("���� �Ÿ� �ƴ�");
            return ESTATE.FAILED;
        }

        Debug.Log("���� ���� �ȿ� ����!");
        return ESTATE.SUCCESS;
    }

    private ESTATE NormalAttack()
    {
        if (!enemyMemory.isInAttackRange)
            return ESTATE.FAILED;

        Debug.Log("�÷��̾ ����!");
        return ESTATE.SUCCESS;
    }
}
