using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGUIController : MonoBehaviour
{
    public static PlayerGUIController Instance { get; private set; }
    [SerializeField] private PlayerStateMachine player;

    [Header("Sliders And Text")]
    public Slider hpSlider; public TextMeshProUGUI sliderHPText;
    public Slider staminaSlider; public TextMeshProUGUI sliderSteminaText;
    public Slider expSlider;


    [Header("Stat Text")]
    public TextMeshProUGUI StatLevelText;
    public TextMeshProUGUI statTextSTR;
    public TextMeshProUGUI statTextDEF;
    public TextMeshProUGUI statTextHP;

    public void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 1. Player 싱글톤 인스턴스를 통해 상태 머신 참조
        if (Player.Instance != null)
        {
            player = Player.Instance.playerStateMachine;
        }
        else
        {
            Debug.LogError("Player 인스턴스를 찾을 수 없습니다! Player 스크립트가 씬에 있는지 확인하세요.");
            return;
        }

        // 2. 이벤트 구독
        player.OnHPChanged += UpdateHPUI;
        player.OnStaminaChanged += UpdateStaminaSliderUI;
        player.OnExpChanged += UpdateExpSlider;
        player.OnLevelChanged += UpdateLevelUI;
        player.OnStatChanged += RefreshStatTexts;
        player.OnStatChanged += () => UpdateHPUI((float)player.currentHP, (float)player.maxHP);

        // 3. 초기 UI 셋팅
        RefreshAllUI();
    }

    // 모든 UI를 한 번에 갱신하는 함수
    public void RefreshAllUI()
    {
        UpdateHPUI((float)player.currentHP, (float)player.maxHP);
        UpdateStaminaSliderUI(player.currentStamina, player.maxStamina);
        UpdateExpSlider(player.currentExp, player.nextLevelMaxExp);
        UpdateLevelUI(player.currentLevel, player.nextLevelMaxExp);
        RefreshStatTexts();
    }

    public void UpdateHPUI(float current, float max)
    {
        hpSlider.maxValue = max;
        hpSlider.value = current;

        // 슬라이더 위 텍스트: "현재 / 최대" (예: 80 / 100)
        if (sliderHPText != null)
            sliderHPText.text = $"{current:F0} / {max:F0}";

        // HP 텍스트 업데이트 (예: HP: 80 / 100)
        if (statTextHP != null)
            statTextHP.text = $"HP: {max:F0}";
    }

    private void UpdateStaminaSliderUI(float current, float max)
    {
        staminaSlider.maxValue = max;
        staminaSlider.value = current;

        // 스태미나 텍스트: "현재 / 최대"
        if (sliderSteminaText != null)
            sliderSteminaText.text = $"{current:F0} / {max:F0}";
    }

    private void UpdateExpSlider(int current, int max)
    {
        expSlider.maxValue = max;
        expSlider.value = current;
    }

    private void UpdateLevelUI(int level, int nextExp)
    {
        if (StatLevelText != null)
            StatLevelText.text = $"{level}";

        expSlider.maxValue = nextExp;

        // 레벨이 오르면 스탯 수치들도 다시 표기
        RefreshStatTexts();
    }

    // 공격력, 방어력 텍스트 갱신
    public void RefreshStatTexts()
    {
        if (statTextSTR != null) statTextSTR.text = $"STR: {player.attack:F0}";
        if (statTextDEF != null) statTextDEF.text = $"DEF: {player.defense:F0}";
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.OnHPChanged -= UpdateHPUI;
        player.OnStaminaChanged -= UpdateStaminaSliderUI;
        player.OnExpChanged -= UpdateExpSlider;
        player.OnLevelChanged -= UpdateLevelUI;
    }
}