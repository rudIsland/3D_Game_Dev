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

            //animation
            Animator animator = rootTransform.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsHit", true);
                Debug.Log("[�����] �� �ִϸ��̼� ����!");
            }

            //take Damage
            CharacterStatsComponent attacker = GetComponentInParent<CharacterStatsComponent>();
            CharacterStatsComponent defender = rootTransform.GetComponent<CharacterStatsComponent>();

            DamageSystem.ApplyDamage(attacker, defender);

        }
    }

}
