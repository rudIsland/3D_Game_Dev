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

    private PlayerStateMachine Player => GameManager.Instance.player;
    private PlayerStats Stats => Player.playerStat;

    private void Start()
    {
        UpdateEXP_StatUI();

        levelUpPanel.SetActive(false); // 시작 시 비활성화
    }

    public void UpdateEXP_StatUI()
    {
        if (Player == null || Stats == null) return;

        expSlider.maxValue = Stats.level.MaxExp;
        expSlider.value = Stats.level.currentExp;

        NowLevel.text = $"LEVEL: {Stats.level.currentLevel}";
        HPText.text = $"HP: {Stats.maxHP}";
        ATKText.text = $"ATK: {Stats.ATK}";
        DEFText.text = $"DEF: {Stats.DEF}";
    }

    public void IncreaseAttack()
    {
        Stats.ATK += 2;
        Stats.statPoint -= 1;
        CheckLevelUpFinish();
    }

    public void IncreaseDefense()
    {
        Stats.DEF += 1;
        Stats.statPoint -= 1;
        CheckLevelUpFinish();
    }

    public void IncreaseHP()
    {
        Stats.maxHP += 20;
        Stats.statPoint -= 1;
        CheckLevelUpFinish();
    }

    private void CheckLevelUpFinish()
    {
        if (Stats.statPoint <= 0)
        {
            UpdateEXP_StatUI();
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
