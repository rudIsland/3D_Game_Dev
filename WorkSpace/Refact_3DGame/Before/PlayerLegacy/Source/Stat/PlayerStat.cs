using UnityEngine;

public class PlayerStat : CharacterStat
{
    public Level level = new Level();
    public int statPoint = 0;

    [Header("스태미나")]
    public float maxStamina;
    public float currentStamina;
    public float staminaRegenPS;    //초당 스테미나 회복
    public float staminaRegenDelay;        //스테미나 회복 딜레이

    private float regenTimer = 0f;  //스테미나 리젠 타이머

    //프리셋 스탯 적용
    //public override void ApplyPreset(CharacterStatPreset basePreset)
    //{
    //    base.ApplyPreset(basePreset);

    //    if (basePreset is PlayerStatPreset preset)
    //    {
    //        maxStamina = preset.maxStamina;
    //        currentStamina = preset.maxStamina;

    //        staminaRegenPS = preset.staminaRegenPerSecond;
    //        staminaRegenDelay = preset.staminaRegenDelay;

    //        level.currentLevel = preset.startLevel;
    //        level.currentExp = preset.startExp;
    //        level.expArray = preset.levelTemplate != null
    //            ? preset.levelTemplate.expArray
    //            : null;

    //        statPoint = 0;
    //    }
    //}

    public bool UseStamina(float amount)
    {
        if (currentStamina < amount)
            return false;

        currentStamina -= amount;
        regenTimer = 0f;  // 리젠 딜레이 초기화
        return true;
    }

    private void TickRegen(float deltaTime) //스테미나는 스탯에서만 관리
    {
        // 1) 스태미나가 이미 최대면 회복 불필요
        if (currentStamina >= maxStamina)
            return;

        // 2) 스태미나 사용 후 regenDelay 만큼 기다림
        regenTimer += deltaTime;
        if (regenTimer < staminaRegenDelay)
            return;

        // 3) 회복
        currentStamina += staminaRegenPS * deltaTime;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
    }



    public void RegenStamina()
    {
        TickRegen(Time.deltaTime);
        //UI에 적용
        // UIManager.Instance.playerResource.UpdateStaminaUI();
    }

}
