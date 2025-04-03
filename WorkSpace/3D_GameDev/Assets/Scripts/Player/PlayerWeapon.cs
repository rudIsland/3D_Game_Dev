using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Transform rootTransform = other.transform.root; // �ֻ��� �θ� ã��

        if (rootTransform.CompareTag("Enemy"))  // �θ� Enemy���� Ȯ��
        {
            Debug.Log($"[�����] ���� �浹: {rootTransform.name}");

            Animator animator = rootTransform.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsHit", true);
                Debug.Log("[�����] �� �ִϸ��̼� ����!");
            }
            else
            {
                Debug.LogWarning("[���] Enemy�� Animator ����!");
            }
        }
    }

}
