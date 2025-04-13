using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Cinemachine.DocumentationSortingAttribute;

public class PlayerStatComponent : CharacterStatsComponent
{
    private float timeSinceLastUse = 0f;

    [Header("스테미나UI")]
    public Slider steminaSlider;
    public TextMeshProUGUI steminaText;

    public PlayerStats playerStats => stats as PlayerStats;

    private void Awake()
    {
        stats = new PlayerStats();
    }

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
            steminaSlider.maxValue = playerStats.maxStamina;
            steminaSlider.value = playerStats.currentStamina;
        }

        UpdateSliderStemina();


    }

    public bool CanUse(float amount)
    {
        return playerStats.currentStamina >= amount;
    }

    public void Use(float amount)
    {
        playerStats.currentStamina = Mathf.Max(playerStats.currentStamina - amount, 0f);
        timeSinceLastUse = 0f;

        UpdateSliderStemina();
    }

    private void Update()
    {
        timeSinceLastUse += Time.deltaTime;

        if (timeSinceLastUse >= playerStats.regenDelay && playerStats.currentStamina < playerStats.maxStamina)
        {
            playerStats.currentStamina += playerStats.staminaRegenPS * Time.deltaTime;
            playerStats.currentStamina = Mathf.Min(playerStats.currentStamina, playerStats.maxStamina);

            UpdateSliderStemina();
        }
    }

    // Update Stemina and HP //
    private void UpdateSliderStemina()
    {
        if (steminaSlider != null)
        {
            steminaSlider.value = playerStats.currentStamina;
            steminaText.text = ((int)playerStats.currentStamina).ToString() + "/" + playerStats.maxStamina;
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
