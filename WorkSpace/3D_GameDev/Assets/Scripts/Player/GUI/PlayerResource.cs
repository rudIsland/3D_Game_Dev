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


    private PlayerStats stats;


    private void Start()
    {
        FindPlayerObject();

        UpdateHPUI();
        UpdateStaminaUI();
    }
    private void FindPlayerObject()
    {
        stats = FindObjectOfType<PlayerStateMachine>().playerStat;
    }

    public void UpdateHPUI()
    {
        if (stats == null) return;

        hpSlider.maxValue = (float)stats.maxHP;
        hpSlider.value = (float)stats.currentHP;
        hpText.text = $"{(int)stats.currentHP} / {stats.maxHP}";
    }

    public void UpdateStaminaUI()
    {
        if (stats == null) return;

        staminaSlider.maxValue = (float)stats.maxStamina;
        staminaSlider.value = (float)stats.currentStamina;
        staminaText.text = $"{(int)stats.currentStamina} / {stats.maxStamina}";
    }

    public void UpdateResourceAll()
    {
        UpdateHPUI();
        UpdateStaminaUI();
    }

}
