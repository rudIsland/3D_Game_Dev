using UnityEngine;
using UnityEngine.SceneManagement;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("데이터 설정")]
    [SerializeField] private PlayerSavedData playerActiveData;

    public PlayerSavedData PlayerData => playerActiveData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // [추가] 저장된 스테이지 번호(Build Index)를 반환하는 함수
    public int GetSavedStageNumber()
    {
        if (playerActiveData == null) return 1;

        // 0이나 1은 보통 로비/초기화 씬이므로, 게임 시작 씬(최소 3번) 이상인지 체크
        return playerActiveData.currentStage;
    }

    public void SaveGame()
    {
        if (playerActiveData == null) return;

        // 현재 활성화된 씬 번호를 장부에 기록
        playerActiveData.currentStage = SceneManager.GetActiveScene().buildIndex;

        SaveSystem.Save(playerActiveData);
        Debug.Log($"<color=green>[Save Success]</color> Stage: {playerActiveData.currentStage}");
    }

    public void LoadGame()
    {
        if (playerActiveData == null) return;
        SaveSystem.Load(playerActiveData);
    }
}