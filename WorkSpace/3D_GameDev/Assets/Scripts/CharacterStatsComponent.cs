using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsComponent : MonoBehaviour
{
    [Header("½ºÅÈ")]
    public CharacterStats stats = new CharacterStats();

    [Header("UI")]
    public Slider hpSlider;
    public Slider steminaSlider;

    public System.Action OnDeath;

    private void Start()
    {
        UpdateHPUI();
    }

    public void TakeDamage(double damage)
    {
        stats.TakeDamage(damage);
        UpdateHPUI();

        if (stats.IsDead)
        {
            OnDeath?.Invoke(); // Á×À½ ÄÝ¹é
        }
    }

    public void Heal(double amount)
    {
        stats.currentHP = Mathf.Min((float)(stats.currentHP + amount), (float)stats.maxHP);
        UpdateHPUI();
    }

    private void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)(stats.currentHP / stats.maxHP);
        }
    }
}
