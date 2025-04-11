using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyGUI : MonoBehaviour
{
    private Enemy enemy;
    public TextMeshProUGUI levelText;
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // 카메라 쪽을 바라보게 회전
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
        }
    }

    private void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        levelText.text = enemy.levelSys.level.ToString();
    }

}
