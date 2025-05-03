using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    //충돌시
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            Debug.Log("적이 플레이어를 공격!");

            //take Damage
            Enemy attacker = GetComponentInParent<Enemy>();
            PlayerStateMachine defender = other.GetComponent<PlayerStateMachine>();

            DamageCalculator.DamageCalculate(attacker, defender);

        }
    }
}
