using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerStats : CharacterStats
{
    [Header("����")]
    public Level level;

    [Header("���¹̳�")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaRegenPS = 7.5f;      // �ʴ� ȸ����
    public float regenDelay = 2f;     // ������ �Һ� �� ȸ�� ������

    public PlayerStats() { level = new Level(); } // level�Ҵ�
}
