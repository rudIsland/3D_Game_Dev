using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object Asset/Player Stat Preset")]
public class PlayerStatPreset : CharacterStatPreset
{
    [Header("스태미나")]
    public float maxStamina = 100f;
    public float staminaRegenPerSecond = 7.5f;
    public float staminaRegenDelay = 2f;

    [Header("레벨 정보")]
    public int startLevel = 1;
    public float startExp = 0f;
    public LevelTemplate levelTemplate;
}