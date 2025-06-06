
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI clearCountTXt;
    public PlayerResource playerResource;
    public LevelStatSystem levelStatSystem;
    public ESC_Option EscOption;
    public DeadPanel deadPanel;

    public static UIManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }

        EscOption = GetComponentInChildren<ESC_Option>();
        playerResource = GetComponentInChildren<PlayerResource>();
        levelStatSystem = GetComponentInChildren<LevelStatSystem>();
        deadPanel = GetComponentInChildren<DeadPanel>();
    }

    public void SetStageClearText()
    {
        Stage currentStage = FindObjectOfType<Stage>();
        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        clearCountTXt.text = $"{currentLevel} / {currentStage.clearLevel} ";
    }

    public void RefreshAll()
    {
        playerResource.UpdateHPUI();
        playerResource.UpdateStaminaUI();
        levelStatSystem.UpdateExpSlider();
        levelStatSystem.Update_StatUI();
    }
}
