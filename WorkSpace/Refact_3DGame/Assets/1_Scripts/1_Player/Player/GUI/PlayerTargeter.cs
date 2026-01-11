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

            //Material 변경
            if (currentTarget.GetComponent<Enemy>().isDetected)
                currentTarget.GetComponent<Enemy>().ChangeDetectedMtl();
            else
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
        if (target == null) return;

        if (!targets.Contains(target))
        {
            targets.AddLast(target);

            //타겟제거 이벤트 제거 후 등록
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 중복 구독 방지를 위해 -= 후 +=
                enemy.OnEnemyDeath -= HandleEnemyDeath;
                enemy.OnEnemyDeath += HandleEnemyDeath;
            }
        }
    }

    private void OnTriggerExit(Collider other) //타겟 제거
    {
        Target target = other.GetComponent<Target>();
        if (target == null) return;

        // 범위 밖으로 나갈 때도 이벤트 해제
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.OnEnemyDeath -= HandleEnemyDeath;
        }

        RemoveTarget(target);
    }



    // 적이 죽었을 때 호출될 콜백 함수
    private void HandleEnemyDeath(Enemy deadEnemy)
    {
        Target target = deadEnemy.GetComponent<Target>();
        if (target != null)
        {
            // 리스트에서 제거하고 현재 타겟이라면 해제하는 기존 로직 실행
            RemoveTarget(target);
        }
    }
}
