using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerStats : CharacterStats
{
    [Header("레벨")]
    public Level level;

    [Header("스태미나")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaRegenPS = 7.5f;      // 초당 회복량
    public float regenDelay = 2f;     // 마지막 소비 후 회복 딜레이

    public PlayerStats() { level = new Level(); } // level할당
}
