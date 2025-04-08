using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponColider : MonoBehaviour
{
    //충돌시
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            Debug.Log("적이 플레이어를 공격!");
            Animator animator = other.GetComponent<Animator>();
            animator.SetBool("Hit", true);

            //take Damage
            CharacterStatsComponent attacker = GetComponentInParent<CharacterStatsComponent>();
            CharacterStatsComponent defender = other.GetComponent<CharacterStatsComponent>();

            DamageSystem.ApplyDamage(attacker, defender);
        }
    }
}
