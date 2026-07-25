
using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance { get; private set; }

    [SerializeField] private EventSystem system;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
            Destroy(gameObject);

        //EveneSystem은 모든 씬에서 하나만 존재해야 하므로 Inital씬에서 할당 후 다른씬에는 배치x
        system = FindObjectOfType<EventSystem>();

        // 처음 로드될 때 HUD 비활성화
        UIManager.Instance.CloseAllHUDPanels();
        UIManager.Instance.ShowCursor(); //시간은 흐르고 커서만 보이게 한다.
    }

}
