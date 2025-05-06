using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{

    private void OnTriggerStay(Collider other)
    {
        Transform rootTransform = other.transform.root; // �ֻ��� �θ� ã��

        if (rootTransform.CompareTag("Enemy"))  // �θ� Enemy���� Ȯ��
        {
            Debug.Log($"[�����] ���� �浹: {rootTransform.name}");

            //take Damage
            PlayerStateMachine attacker = GetComponentInParent<PlayerStateMachine>();
            Enemy defender = other.GetComponent<Enemy>();

            DamageCalculator.DamageCalculate(attacker, defender);

        }
    }

}
