using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HP_Bar : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // ī�޶� ���� �ٶ󺸰� ȸ��
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
        }
    }

}
