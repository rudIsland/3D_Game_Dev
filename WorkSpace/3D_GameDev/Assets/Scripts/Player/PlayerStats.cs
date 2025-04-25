using UnityEngine;

[System.Serializable]
public class PlayerStats : CharacterStats
{
    [Header("레벨")]
    public Level level;

    [Header("스태미나")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaRegenPS = 7.5f;
    public float regenDelay = 2f;

    private float timeSinceLastUse = 0f;

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
}
