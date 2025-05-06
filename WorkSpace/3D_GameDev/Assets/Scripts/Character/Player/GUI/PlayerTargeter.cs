using System;
using System.Collections.Generic;
using UnityEngine;

public class Targeter : MonoBehaviour
{
    public SphereCollider SphereCollider;
    [SerializeField] private float TargetRadius = 6f;
    public List<Target> targets = new List<Target>(); //�ٶ� Ÿ�� ����Ʈ

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

        currentTarget = targets[0]; //���� ù��° ��Ҹ� ���� Ÿ������

        return true;
    }

    public void CanCel()
    {
        currentTarget = null;
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


    private void OnTriggerEnter(Collider other) //Ÿ�� �߰�
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Add(target);

        CharacterBase character = target.GetComponent<CharacterBase>();
        if (character != null)
        {
            character.OnDeath += () => RemoveTarget(target); //����� ����
        }
    }

    private void OnTriggerExit(Collider other) //Ÿ�� ����
    {
        Target target = other.GetComponent<Target>();
        if (target == null)
            return;

        targets.Remove(target);
        if(targets.Count < 1) OnCurrentTargetLost?.Invoke();
    }
}
