
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour, IPlayerDeadHandler
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

        if (clearCountTXt == null)
        {
            //Debug.LogWarning("clearCountTXt�� null�Դϴ�. �ν����Ϳ��� �Ҵ��ߴ��� Ȯ���ϼ���.");
            return;
        }

        if (Player.Instance == null)
        {
            Debug.LogWarning("Player.Instance�� null�Դϴ�.");
            return;
        }

        Stage currentStage = FindObjectOfType<Stage>();
        if (currentStage == null)
        {
            Debug.LogWarning("[UIManager]StageManager�� CurrentStage�� �Ҵ���� �ʾҽ��ϴ�");
            return;
        }

        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        clearCountTXt.text = $"{currentLevel} / {currentStage.clearLevel}";
    }

    public void RefreshAll()
    {
        playerResource.UpdateHPUI();
        playerResource.UpdateStaminaUI();
        levelStatSystem.UpdateExpSlider();
        levelStatSystem.Update_StatUI();
    }

    public void OnPlayerDeath(float time)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        deadPanel.ActiveDeadPanel(true);
        deadPanel.ShowDeadPanel(time);
    }
}
