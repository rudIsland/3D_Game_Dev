using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class CharacterStatsComponent : MonoBehaviour
{
    [Header("���뽺��")]
    public CharacterStats stats;

    [Header("���� ü��UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    public void UpdateResource()
    {
        UpdateHPUI();
    }

    public virtual void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)(stats.currentHP / stats.maxHP);
            hpText.text = (int)stats.currentHP + "/" + stats.maxHP.ToString();
        }
    }
}
