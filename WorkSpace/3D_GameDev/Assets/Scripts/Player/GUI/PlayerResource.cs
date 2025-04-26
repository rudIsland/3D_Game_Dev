using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResource : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Stamina")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TextMeshProUGUI staminaText;

    private PlayerStateMachine Player => GameManager.Instance.player;
    private PlayerStats Stats => Player.playerStat;

    private void Start()
    {
        UpdateHPUI();
        UpdateStaminaUI();
    }

    public void UpdateHPUI()
    {
        if (Stats == null) return;

        hpSlider.maxValue = (float)Stats.maxHP;
        hpSlider.value = (float)Stats.currentHP;
        hpText.text = $"{(int)Stats.currentHP} / {Stats.maxHP}";
    }

    public void UpdateStaminaUI()
    {
        if (Stats == null) return;

        staminaSlider.maxValue = (float)Stats.maxStamina;
        staminaSlider.value = (float)Stats.currentStamina;
        staminaText.text = $"{(int)Stats.currentStamina} / {Stats.maxStamina}";
    }

    public void UpdateResourceAll()
    {
        UpdateHPUI();
        UpdateStaminaUI();
    }

}
