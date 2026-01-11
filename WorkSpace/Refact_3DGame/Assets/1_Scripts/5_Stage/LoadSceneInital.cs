
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneInital : MonoBehaviour
{
    private void Start()
    {
        // 1. GUI 및 시스템 관리용 씬을 먼저 로드 (Additive)
        // 이 씬에 있는 UI는 처음에 SetActive(false) 상태여야 타이틀 화면을 가리지 않습니다.
        SceneManager.LoadScene("01_Scene_HUD", LoadSceneMode.Additive);

        // 2. 실제 유저가 보게 될 시작 메뉴 씬 로드
        SceneManager.LoadScene("001_Scene_Lobby", LoadSceneMode.Additive);
       
    }


    public static void StartGame()
    {
        // Start_Scene을 내리고 Stage1을 로드
        SceneManager.UnloadSceneAsync("Start_Scene");
        SceneManager.LoadScene("Stage1", LoadSceneMode.Additive);

        // 이때 GUI를 활성화하는 로직을 넣으면 됩니다.
    }
}
