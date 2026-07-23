using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public PlayerStateMachine player; // 이제 에디터에서 GlobalManager 하위의 Player를 드래그해 넣어주세요.
    private Scene currentScene;

    public int currentStage; // 현재 플레이 중인 스테이지

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 부모가 이미 DDOL이면 생략 가능하지만, 단독 사용 시를 위해 안전장치로 추가 가능
            // DontDestroyOnLoad(transform.root.gameObject); 
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // [핵심] 이 함수를 호출하면 이동 -> 스폰 지점 찾기 -> 플레이어 배치가 순차적으로 진행됩니다.
    public void MoveToNextStage(string sceneName)
    {
        Debug.Log($"{sceneName}으로 이동을 시작합니다.");

        SceneManager.LoadScene(sceneName);
    }

    // 씬 로드가 물리적으로 완료된 직후 호출됨
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupPlayerPosition();
        currentScene = scene;
        // 인게임x HUD 설정
        if (scene.buildIndex < 3 )
        {
            // 처음 로드될 때 HUD 비활성화
            UIManager.Instance.OffResourcePanel();
            UIManager.Instance.OffPausePanel();
            UIManager.Instance.OffLevelUPPanel();
        }
        else // 그 외의 번호(실제 인게임 스테이지들)일 때
        {
            UIManager.Instance.OnResourceAndStatPanels();
            UIManager.Instance.ResumeGame();
        }
    }

    private void SetupPlayerPosition()
    {
        // 1. 새 씬에서 "SpawnPoint"라는 이름을 가진 오브젝트를 찾습니다.
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");

        if (spawnPoint != null)
        {
            // 2. 플레이어 참조 확인 (없으면 찾기)
            if (player == null) player = FindObjectOfType<PlayerStateMachine>();

            if (player != null)
            {
                // 3. 물리 엔진(CharacterController) 일시 정지 (위치 튕김 방지)
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                // 4. 위치 및 회전값 적용
                player.transform.position = spawnPoint.transform.position;
                player.transform.rotation = spawnPoint.transform.rotation;

                // 5. 물리 엔진 재가동
                if (cc != null) cc.enabled = true;

                Debug.Log($"<color=green>{SceneManager.GetActiveScene().name}</color>: 플레이어를 SpawnPoint로 이동시켰습니다.");
            }
        }
        else
        {
            // 로비 씬 등 스폰 포인트가 없는 씬을 위한 예외 처리
            Debug.LogWarning("현재 씬에 SpawnPoint 오브젝트가 없습니다.");
        }
    }

    public Scene getCurrentScene()
    {
        return currentScene;
    }


}