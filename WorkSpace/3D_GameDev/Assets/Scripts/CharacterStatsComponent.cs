using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsComponent : MonoBehaviour
{
    [Header("½ºÅÈ")]
    public CharacterStats stats = new CharacterStats();

    [Header("UI")]
    public Slider hpSlider;

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
        stats.CurrentHP = Mathf.Min((float)(stats.CurrentHP + amount), (float)stats.MaxHP);
        UpdateHPUI();
    }

    private void UpdateHPUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)(stats.CurrentHP / stats.MaxHP);
        }
    }
}
