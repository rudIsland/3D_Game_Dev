using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object Asset/Player Stat Preset")]
public class PlayerStatPreset : CharacterStatPreset
{
    [Header("Player Stat")]

    [Header("스태미나")]
    public float maxStamina = 100f;
    public float staminaRegenPerSecond = 7.5f;
    public float staminaRegenDelay = 2f;
    public float sprintStaminaCost = 15f;     // 초당 달리기에 소모되는 양 (추가)
    public float jumpStaminaCost = 25f;       // 점프 시 소모되는 양

    [Header("레벨 정보")]
    public int startLevel = 1;
    public int startExp = 0;
    public int statPoints = 0;
    public LevelTemplate levelTemplate;


}