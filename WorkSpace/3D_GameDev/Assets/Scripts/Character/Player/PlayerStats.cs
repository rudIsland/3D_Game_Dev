using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[System.Serializable]
public class PlayerStats : CharacterStats
{
    [Header("스탯")]
    [SerializeField] private double serializedMaxHP = 100;
    [SerializeField] private double serializedCurrentHP = 100;
    [SerializeField] private double serializedATK = 10;
    [SerializeField] private double serializedDEF = 5;

    public override double maxHP { get => serializedMaxHP; set => serializedMaxHP = value; }
    public override double currentHP { get => serializedCurrentHP; set => serializedCurrentHP = value; }
    public override double ATK { get => serializedATK; set => serializedATK = value; }
    public override double DEF { get => serializedDEF; set => serializedDEF = value; }

    [Header("레벨")]
    public Level level;

    [Header("스태미나")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaRegenPS = 7.5f;
    public float regenDelay = 2f;

    private float timeSinceLastUse = 0f;

    public int statPoint = 0;

    public PlayerStats()
    {
        maxHP = 100;
        currentHP = 100;
        maxStamina = 100;
        currentStamina = 100;
        level = new Level();
        ATK = 10;
        DEF = 5;
        statPoint = 0;
    }

    public void ApplySavedData(SavedData data)
    {
        maxHP = data.maxHP;
        currentHP = data.currentHP;
        ATK = data.ATK;
        DEF = data.DEF;
        level.currentLevel = data.level;
        level.currentExp = data.currentExp;
        maxStamina = data.maxStamina;
        currentStamina = data.currentStamina;
        statPoint = data.statPoint;
    }


    public bool CanUse(float amount)
    {
        return currentStamina >= amount;
    }

    public void Use(float amount)
    {
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        timeSinceLastUse = 0f;
    }

    public void TickRegen(float deltaTime)
    {
        timeSinceLastUse += deltaTime;

        if (timeSinceLastUse >= regenDelay && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenPS * deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    public void AddExp(float exp)
    {
        level.currentExp += exp;
        Debug.Log($"현재 경험치량: {level.currentExp} / {level.MaxExp}");

        while (level.currentLevel < Level.MaxLevel && level.currentExp >= level.expArray[level.currentLevel - 1])
        {
            level.currentExp -= level.expArray[level.currentLevel - 1];
            level.currentLevel++;

            HandleLevelUp(); // 레벨업 처리 여기서 한 번에
        }

        UIManager.Instance.levelStatSystem.UpdateExpSlider();
        level.currentExp = Mathf.Max(0, level.currentExp);

        SaveSystem.SaveGameData();
    }

    private void HandleLevelUp()
    {
        statPoint += 1; // 스탯 포인트 지급
        LevelUpHeal();  // 체력 회복
        UIManager.Instance.playerResource.UpdateHPUI(); // 체력 슬라이더 즉시 갱신

        // 레벨업 UI, 퍼스 호출
        UIManager.Instance.levelStatSystem.OpenLevelPanel();
        UIManager.Instance.levelStatSystem.Update_StatUI();
        GameManager.Instance.onLevelUp?.Invoke();
    }

    public void LevelUpHeal()
    {
        currentHP = maxHP;
    }

}
