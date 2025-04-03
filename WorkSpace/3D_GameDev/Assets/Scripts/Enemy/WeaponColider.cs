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
            Debug.Log("무기가 플레이어를 공격!");
        }
    }
}
