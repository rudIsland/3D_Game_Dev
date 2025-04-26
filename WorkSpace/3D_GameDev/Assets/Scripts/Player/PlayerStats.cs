using UnityEngine;

[System.Serializable]
public class PlayerStats : CharacterStats
{
    [Header("����")]
    [SerializeField] private double serializedMaxHP = 100;
    [SerializeField] private double serializedCurrentHP = 100;
    [SerializeField] private double serializedATK = 1000;
    [SerializeField] private double serializedDEF = 5;

    public override double maxHP { get => serializedMaxHP; set => serializedMaxHP = value; }
    public override double currentHP { get => serializedCurrentHP; set => serializedCurrentHP = value; }
    public override double ATK { get => serializedATK; set => serializedATK = value; }
    public override double DEF { get => serializedDEF; set => serializedDEF = value; }

    [Header("����")]
    public Level level;

    [Header("���¹̳�")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaRegenPS = 7.5f;
    public float regenDelay = 2f;

    private float timeSinceLastUse = 0f;

    public int statPoint = 0;

    public PlayerStats()
    {
        level = new Level();
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
        Debug.Log($"���� ����ġ��: {level.currentExp} / {level.MaxExp}");

        while (level.currentLevel < Level.MaxLevel && level.currentExp >= level.expArray[level.currentLevel - 1])
        {
            level.currentExp -= level.expArray[level.currentLevel - 1];
            level.currentLevel++;

            HandleLevelUp(); // ������ ó�� ���⼭ �� ����
        }

        level.currentExp = Mathf.Max(0, level.currentExp);
    }

    private void HandleLevelUp()
    {
        statPoint += 1; // ���� ����Ʈ ����
        LevelUpHeal();  // ü�� ȸ��
        GameManager.Instance.Resource.UpdateHPUI(); // ü�� �����̴� ��� ����

        // ������ UI, �۽� ȣ��
        GameManager.Instance.levelStatSystem.OpenLevelPanel();
        GameManager.Instance.levelStatSystem.UpdateEXP_StatUI();
        GameManager.Instance.onLevelUp?.Invoke();
    }

    public void LevelUpHeal()
    {
        currentHP = maxHP;
    }

}
