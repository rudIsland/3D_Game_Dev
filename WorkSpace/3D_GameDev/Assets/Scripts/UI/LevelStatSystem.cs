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
    public GameObject levelUpPanel;

    private PlayerStateMachine player;
    private PlayerStats stats;

    private void Start()
    {
        player = Player.Instance.playerStateMachine;
        stats = player.playerStat;

        Update_StatUI();

        levelUpPanel.SetActive(false); // 시작 시 비활성화
    }

    public void Update_StatUI()
    {
        if (player == null || stats == null) return;
        UpdateExpSlider();

        NowLevel.text = $"{stats.level.currentLevel}";
        HPText.text = $"HP: {stats.maxHP}";
        ATKText.text = $"ATK: {stats.ATK}";
        DEFText.text = $"DEF: {stats.DEF}";
    }

    public void UpdateExpSlider()
    {
        if (expSlider == null || stats == null)
        {
            Debug.LogWarning("[LevelStatSystem] expSlider or stats is null.");
            return;
        }

        expSlider.maxValue = stats.level.MaxExp;
        expSlider.value = stats.level.currentExp;
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
        UIManager.Instance.playerResource.UpdateHPUI();
    }

    private void UpdateLevelUp()
    {
        Update_StatUI();
        if (stats.statPoint <= 0)
        {
            CloseLevelPanel();
        }
    }

    public void CloseLevelPanel()
    {
        levelUpPanel.SetActive(false);
        GameManager.Instance.ResumeGame();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenLevelPanel()
    {
        levelUpPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
