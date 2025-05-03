
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI clearCountTXt;
    public PlayerResource playerResource;
    public LevelStatSystem levelStatSystem;
    public ESC_Option escOPtion;

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

        escOPtion = GetComponent<ESC_Option>();
        playerResource = GetComponentInChildren<PlayerResource>();
        levelStatSystem = GetComponentInChildren<LevelStatSystem>();
    }

    public void SetStageClearText()
    {
        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        clearCountTXt.text = $"{currentLevel} / {StageManager.Instance.currentStage.clearLevel} ";
    }
}
