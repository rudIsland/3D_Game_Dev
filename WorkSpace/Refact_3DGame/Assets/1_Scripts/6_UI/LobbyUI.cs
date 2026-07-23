using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("이어하기 버튼")]
    [SerializeField] private Button continueButton;

    [Header("초기화용 프리셋 (기본 스탯 설계도)")]
    [SerializeField] private PlayerStatPreset defaultPlayerPreset; // 인스펙터에서 할당

    private void Start()
    {
        CheckSaveData();
    }

    private void CheckSaveData()
    {
        if (continueButton != null)
        {
            // 1. 파일이 있는지 먼저 확인
            bool hasFile = SaveSystem.HasSaveFile();

            if (hasFile)
            {
                // 2. 저장된 데이터를 일단 로드해서 씬 번호를 확인
                DataManager.Instance.LoadGame();
                int savedIndex = DataManager.Instance.GetSavedStageNumber();

                // 3. 조건: 파일이 있고, 저장된 씬 번호가 4보다 클 때만 버튼 활성화
                // (즉, 1~4번 씬에서 저장된 데이터라면 이어하기 불가)
                continueButton.interactable = savedIndex > 3;

                Debug.Log($"저장된 씬 인덱스: {savedIndex} | 이어하기 가능 여부: {continueButton.interactable}");
            }
            else
            {
                // 파일 자체가 없으면 당연히 비활성화
                continueButton.interactable = false;
            }
        }
    }

    // [처음부터하기] 버튼
    public void OnClickNewGame()
    {
        // 1. 기존 세이브 파일 삭제
        SaveSystem.DeleteSaveFile();

        // 2. 장부(SO) 데이터 리셋
        if (DataManager.Instance != null && defaultPlayerPreset != null)
        {
            DataManager.Instance.PlayerData.ResetFromPreset(defaultPlayerPreset);
        }

        // 3. [핵심] 현재 씬에 이미 생성되어 있는 플레이어를 찾아 직접 초기화 명령 전달
        // 플레이어가 DontDestroyOnLoad로 살아있다면 Awake가 안 불리므로 여기서 강제로 시켜야 함
        PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>();
        if (player != null)
        {
            player.Initialize_Stats();
        }

        // 4. 리셋된 상태를 파일로 저장
        DataManager.Instance.SaveGame();

        // 5. 게임 시작
        SceneManager.LoadScene(3);
    }

    // [이어하기] 버튼
    public void OnContinueGame()
    {
        DataManager.Instance.LoadGame();
        int savedIndex = DataManager.Instance.GetSavedStageNumber();

        if (savedIndex < 3) savedIndex = 3;
        SceneManager.LoadScene(savedIndex);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


}