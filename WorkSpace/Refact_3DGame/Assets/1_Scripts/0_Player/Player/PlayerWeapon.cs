using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{

    private void OnTriggerStay(Collider other)
    {
        Transform rootTransform = other.transform.root; // 최상위 부모 찾기

        if (rootTransform.CompareTag("Enemy"))  // 부모가 Enemy인지 확인
        {
            Debug.Log($"[디버그] 적과 충돌: {rootTransform.name}");

            //take Damage
            PlayerStateMachine attacker = GetComponentInParent<PlayerStateMachine>();
            Enemy defender = other.GetComponent<Enemy>();

            //DamageCalculator.DamageCalculate(attacker, defender);

        }
    }

}
