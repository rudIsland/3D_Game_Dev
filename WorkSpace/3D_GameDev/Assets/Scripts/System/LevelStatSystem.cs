using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelStatSystem : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI NowLevel;
    [SerializeField] private TextMeshProUGUI HPText;
    [SerializeField] private TextMeshProUGUI ATKText;
    [SerializeField] private TextMeshProUGUI DEFText;
    [SerializeField] private GameObject levelUpPanel;

    private PlayerStateMachine player;
    private PlayerStats stats;

    private void Start()
    {
        FindPlayerObject();

        UpdateEXP_StatUI();

        levelUpPanel.SetActive(false); // 시작 시 비활성화
    }

    private void FindPlayerObject()
    {
        player = FindObjectOfType<PlayerStateMachine>();
    }

    public void LevelStatInit(PlayerStateMachine p)
    {
        player = p;
        stats = player.playerStat;

        UpdateEXP_StatUI();
        levelUpPanel.SetActive(false);
    }

    public void UpdateEXP_StatUI()
    {
        if (player == null || stats == null) return;

        expSlider.maxValue = stats.level.MaxExp;
        expSlider.value = stats.level.currentExp;

        NowLevel.text = $"LEVEL: {stats.level.currentLevel}";
        HPText.text = $"HP: {stats.maxHP}";
        ATKText.text = $"ATK: {stats.ATK}";
        DEFText.text = $"DEF: {stats.DEF}";
    }

    public void IncreaseAttack()
    {
        stats.ATK += 2;
        stats.statPoint -= 1;
        UpdateLevelUp();
    }

    public void IncreaseDefense()
    {
        stats.DEF += 1;
        stats.statPoint -= 1;
        UpdateLevelUp();
    }

    public void IncreaseHP()
    {
        stats.maxHP += 20;
        stats.statPoint -= 1;
        UpdateLevelUp();
        stats.LevelUpHeal();  // 체력 회복
        GameManager.Instance.Resource.UpdateHPUI();
    }

    private void UpdateLevelUp()
    {
        UpdateEXP_StatUI();
        if (stats.statPoint <= 0)
        {
            CloseLevelPanel();
        }
    }

    public void CloseLevelPanel()
    {
        levelUpPanel.SetActive(false);
        GameManager.Instance.ContinueGame();
    }

    public void OpenLevelPanel()
    {
        levelUpPanel.SetActive(true);
    }
}
