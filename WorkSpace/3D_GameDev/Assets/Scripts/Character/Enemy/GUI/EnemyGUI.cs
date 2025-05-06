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
            if (Camera.main == null) return;

            Vector3 camPosition = Camera.main.transform.position;
            Vector3 direction = transform.position - camPosition;
            direction.y = 0f; // 수평 방향만 고려

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    public void UpdateLevel()
    {
        if (enemy != null && levelText != null)
        {
            levelText.text = enemy.level.currentLevel.ToString();
        }
    }
}
