using UnityEngine;

[System.Serializable]
public class CharacterStat
{
    [Header("기본 스탯")]
    public double maxHP;
    public double currentHP;

    public double attack;
    public double defense;

    public bool IsDead => currentHP <= 0;

    // ScriptableObject 프리셋 적용
    public virtual void ApplyPreset(CharacterStatPreset preset)
    {
        if (preset == null)
        {
            Debug.LogWarning("CharacterStatPreset is null!");
            return;
        }

        maxHP = preset.maxHP;
        currentHP = preset.maxHP;

        attack = preset.attack;
        defense = preset.defense;
    }

    public void TakeDamage(double damage)
    {
        double reduced = Mathf.Max(0, (float)(damage - defense));
        currentHP = Mathf.Max(0, (float)(currentHP - reduced));
    }

}
