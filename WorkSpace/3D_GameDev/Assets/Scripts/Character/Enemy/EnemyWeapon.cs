using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    //�浹��
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            Debug.Log("���� �÷��̾ ����!");

            //take Damage
            Enemy attacker = GetComponentInParent<Enemy>();
            PlayerStateMachine defender = other.GetComponent<PlayerStateMachine>();

            DamageCalculator.DamageCalculate(attacker, defender);

        }
    }
}
