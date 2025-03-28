using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targeter : MonoBehaviour
{
    public SphereCollider SphereCollider;
    public List<Target> targets = new List<Target>(); //바라볼 타겟 리스트

    public Target currendTarget;

    public bool SelectTarget()
    {
        if(targets.Count == 0)
            return false;

        currendTarget = targets[0]; //가장 첫번째 요소를 현재 타겟으로

        return true;
    }

    public void CancCel()
    {
        currendTarget = null;
    }


    private void OnTriggerEnter(Collider other) //타겟 추가
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Add(target);
    }

    private void OnTriggerExit(Collider other) //타겟 제거
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Remove(target);
    }
}
