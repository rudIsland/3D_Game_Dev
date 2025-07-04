using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerTargeter : MonoBehaviour
{
    public SphereCollider SphereCollider;
    [SerializeField] private float TargetRadius = 6f;
    public LinkedList<Target> targets = new LinkedList<Target>(); //바라볼 타겟 리스트

    public Target currentTarget;

    public event Action OnCurrentTargetLost;

    private void Awake()
    {
        SphereCollider = GetComponent<SphereCollider>();
    }

    private void Start()
    {
        SetTargetRadius();
    }

    void SetTargetRadius()
    {
       SphereCollider.radius = TargetRadius;
    }

    public bool SelectTarget()
    {
        if(targets.Count == 0)
            return false;

        currentTarget = targets.First.Value; //가장 첫번째 요소를 현재 타겟으로
        currentTarget.GetComponent<Enemy>().isTarget = true;
        currentTarget.GetComponent<Enemy>().ChangeTargettMtl();

        return true;
    }

    public void CanCel()
    {
        if (currentTarget != null)
        {
            currentTarget.GetComponent<Enemy>().isTarget = false;
            currentTarget.GetComponent<Enemy>().ChangeDefaultMtl();
            currentTarget = null;
        }
    }

    public void RemoveTarget(Target target)
    {
        if (targets.Contains(target))
        {
            targets.Remove(target);
            if (currentTarget == target)
            {
                CanCel();
                OnCurrentTargetLost?.Invoke();
            }
        }
    }


    private void OnTriggerEnter(Collider other) //타겟 추가
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.AddLast(target);

        CharacterBase character = target.GetComponent<CharacterBase>();
        if (character != null)
        {
            character.OnDeath += () => RemoveTarget(target); //사망시 제거
        }
    }

    private void OnTriggerExit(Collider other) //타겟 제거
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Remove(target);
        if(targets.Count < 1) OnCurrentTargetLost?.Invoke();
    }
}
