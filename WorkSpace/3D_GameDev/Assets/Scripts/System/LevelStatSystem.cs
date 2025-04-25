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
        UpdateAllUI();

        levelUpPanel.SetActive(false); // 시작 시 비활성화
    }

    public void UpdateExpUI()
    {
        if (Player == null || Stats == null) return;

        expSlider.maxValue = Stats.level.MaxExp;
        expSlider.value = Stats.level.currentExp;

        if (Stats.level.IsLevelUp && Stats.level.TryLevelUp())
        {
            levelUpPanel.SetActive(true);
            UpdateAllUI();
        }
    }

    public void UpdateAllUI()
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
        UpdateAllUI();
    }

    public void DecreaseAttack()
    {
        Stats.ATK -= 2;
        UpdateAllUI();
    }

    public void IncreaseDefense()
    {
        Stats.DEF += 1;
        UpdateAllUI();
    }

    public void DecreaseDefense()
    {
        Stats.DEF -= 1;
        UpdateAllUI();
    }

    public void IncreaseHP()
    {
        Stats.maxHP += 20;
        UpdateAllUI();
    }

    public void DecreaseHP()
    {
        Stats.maxHP -= 20;
        UpdateAllUI();
    }

    public void HideLevelUpPanel()
    {
        levelUpPanel.SetActive(false);
    }

    public void TriggerLevelUpUI()
    {
        UpdateExpUI(); // 외부에서 호출 시 레벨업 갱신
    }

    public void UpAttack()
    {
        if (Stats != null)
        {
            Stats.ATK += 2;
            UpdateAllUI();
        }
    }

    public void DownAttack()
    {
        if (Stats != null)
        {
            Stats.ATK -= 2;
            UpdateAllUI();
        }
    }

    public void UpDef()
    {
        if (Stats != null)
        {
            Stats.DEF += 1;
            UpdateAllUI();
        }
    }

    public void DownDef()
    {
        if (Stats != null)
        {
            Stats.DEF -= 1;
            UpdateAllUI();
        }
    }


    public void UpHP()
    {
        if (Stats != null)
        {
            Stats.maxHP += 20;
            UpdateAllUI();
        }
    }

    public void DownHP()
    {
        if (Stats != null)
        {
            Stats.maxHP -= 20;
            UpdateAllUI();
        }
    }

    public void CloseLevelPanel()
    {
        gameObject.SetActive(false);
    }
}
