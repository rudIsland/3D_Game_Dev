
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("InGame UI Panels")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject ResoucePanel;
    [SerializeField] private GameObject playerStatPanel;
    public DeadPanelController deadPanel;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; // 중복 인스턴스 파괴 시 아래 로직 실행 방지
        }

        // 시작할 땐 모든 패널 끄기 및 시간 정규화
        ResumeGame();
        levelUpPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 빌드 인덱스가 3 미만인 씬에서는 ESC 메뉴 안 뜨게 방어
            if (StageManager.Instance.getCurrentScene().buildIndex < 3) return;

            // 레벨업 중에는 ESC 메뉴 안 뜨게 방어
            if (levelUpPanel.activeSelf) return;

            TogglePauseMenu();
        }
    }

    // --- 통합 일시정지 제어 ---
    public void TogglePauseMenu()
    {
        IsPaused = !IsPaused;
        pauseMenuPanel.SetActive(IsPaused);

        if (IsPaused) PauseGame();
        else ResumeGame();
    }

    //판넬 모두 닫기
    public void CloseAllHUDPanels()
    {
        levelUpPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        ResoucePanel.SetActive(false);
        playerStatPanel.SetActive(false);
    }


    // --- 레벨업 패널 로직 ---
    public void ShowLevelUpPanel()
    {
        IsPaused = true;
        levelUpPanel.SetActive(true);
        PauseGame();
    }

    public void CloseLevelUpPanel()
    {
        levelUpPanel.SetActive(false);
        // 레벨업 창을 닫을 때 일시정지 메뉴가 떠 있지 않다면 게임 재개
        if (!pauseMenuPanel.activeSelf)
        {
            IsPaused = false;
            ResumeGame();
        }
    }

    public void OffResourcePanel()
    {
        ResoucePanel.SetActive(false);
    }
    public void OnResourcePanel()
    {
        ResoucePanel.SetActive(true);
    }
    public void OffPausePanel()
    {
        pauseMenuPanel.SetActive(false);
    }
    public void OffLevelUPPanel()
    {
        levelUpPanel.SetActive(false);
    }

    public void OnResourceAndStatPanels()
    {
        ResoucePanel.SetActive(true);
        playerStatPanel.SetActive(true);
    }

    // --- 내부 시간 제어 함수 ---
    public void PauseGame()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        ShowCursor();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        HideCursor();
    }

    // --- 버튼 이벤트 ---
    public void OnClickExit()
    {
        ResumeGame(); // 씬 이동 전 반드시 시간을 1로 돌려야 합니다.
        CloseAllHUDPanels(); //나가기 하면 패널은 모두 닫는다.
        ShowCursor(); //나가기 하면 커서 보이게
        SceneManager.LoadScene("001_Scene_Lobby");

        //저장
        DataManager.Instance.SaveGame();
    }

    public void OnClickContinue()
    {
        TogglePauseMenu(); // 단순히 토글만 호출하면 됩니다.
    }

    //마우스 커서
    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



}