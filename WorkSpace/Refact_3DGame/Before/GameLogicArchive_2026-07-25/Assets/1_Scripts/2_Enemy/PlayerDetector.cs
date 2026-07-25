using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    public SphereCollider sphereCollider;

    public Transform mPlayerPos = null;


    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }

    public void RemoveTarget()
    {
        if (mPlayerPos != null)
        {
            mPlayerPos = null;
        }
    }

    //플레이어가 들어오면
    private void OnTriggerEnter(Collider other) //타겟 추가
    {
        //Tag
        if (!other.CompareTag("Player")) return;

        // 플레이어가 범위에 들어오면 참조를 저장
        mPlayerPos = other.transform;

    }

    private void OnTriggerExit(Collider other) //타겟 제거
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 범위를 벗어나면 참조를 해제
            mPlayerPos = null;
        }
    }
}
