using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatComponent : CharacterStatsComponent
{
    [Header("스태미나")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float staminaRegenPS = 7.5f;      // 초당 회복량
    public float regenDelay = 2f;     // 마지막 소비 후 회복 딜레이

    private float timeSinceLastUse = 0f;

    [Header("스테미나UI")]
    public Slider steminaSlider;
    public TextMeshProUGUI steminaText;

    private void Start()
    {
        // get Component
        steminaSlider = GameObject.Find("PlayerStemina").GetComponent<Slider>();
        steminaText = steminaSlider.GetComponentInChildren<TextMeshProUGUI>();

        // HP
        hpSlider = GameObject.Find("PlayerHP").GetComponent<Slider>();
        hpText = hpSlider.GetComponentInChildren<TextMeshProUGUI>();

        UpdateHPUI();

        // Stemina
        if (steminaSlider != null)
        {
            steminaSlider.maxValue = maxStamina;
            steminaSlider.value = currentStamina;
        }

        UpdateSliderStemina();


    }

    public bool CanUse(float amount)
    {
        return currentStamina >= amount;
    }

    public void Use(float amount)
    {
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        timeSinceLastUse = 0f;

        UpdateSliderStemina();
    }

    private void Update()
    {
        timeSinceLastUse += Time.deltaTime;

        if (timeSinceLastUse >= regenDelay && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenPS * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            UpdateSliderStemina();
        }
    }

    // Update Stemina and HP //
    private void UpdateSliderStemina()
    {
        if (steminaSlider != null)
        {
            steminaSlider.value = currentStamina;
            steminaText.text = ((int)currentStamina).ToString() + "/" + maxStamina;
        }
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
