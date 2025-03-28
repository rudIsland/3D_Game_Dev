using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targeter : MonoBehaviour
{
    public SphereCollider SphereCollider;
    public List<Target> targets = new List<Target>(); //�ٶ� Ÿ�� ����Ʈ

    public Target currendTarget;

    public bool SelectTarget()
    {
        if(targets.Count == 0)
            return false;

        currendTarget = targets[0]; //���� ù��° ��Ҹ� ���� Ÿ������

        return true;
    }

    public void CancCel()
    {
        currendTarget = null;
    }


    private void OnTriggerEnter(Collider other) //Ÿ�� �߰�
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Add(target);
    }

    private void OnTriggerExit(Collider other) //Ÿ�� ����
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Remove(target);
    }
}
