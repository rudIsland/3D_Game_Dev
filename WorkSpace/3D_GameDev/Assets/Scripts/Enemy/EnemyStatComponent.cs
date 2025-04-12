using UnityEngine.UI;

public class EnemyStatComponent : CharacterStatsComponent
{

    private void Start()
    {
        UpdateHPUI();
    }

    public override void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)(stats.currentHP / stats.maxHP);
            hpText.text = ((int)stats.currentHP).ToString() + "/" + stats.maxHP;
        }
    }
}
