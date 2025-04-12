using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class CharacterStatsComponent : MonoBehaviour
{
    [Header("���뽺��")]
    public CharacterStats stats = new CharacterStats();

    [Header("���� ü��UI")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    public System.Action OnDeath;

    public void TakeDamage(double damage)
    {
        stats.TakeDamage(damage);
        UpdateHPUI();

        if (stats.IsDead)
        {
            OnDeath?.Invoke(); // ���� �ݹ�
        }
    }

    public void Heal(double amount)
    {
        stats.currentHP = Mathf.Min((float)(stats.currentHP + amount), (float)stats.maxHP);
        UpdateHPUI();
    }

    public virtual void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)(stats.currentHP / stats.maxHP);
            hpText.text = stats.currentHP.ToString()+"/"+stats.maxHP;
        }
    }
}
