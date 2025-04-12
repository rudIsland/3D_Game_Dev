using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyGUI : MonoBehaviour
{
    public Enemy enemy;
    public TextMeshProUGUI levelText;
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // ī�޶� ���� �ٶ󺸰� ȸ��
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
        }
    }

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    private void Start()
    {
        Debug.Log($"[EnemyGUI] enemy.levelSys.level: {enemy.levelSys.level}");
        levelText.text = enemy.levelSys.level.ToString();
    }

}
