using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStateMachine player;
    public PlayerResource Resource { get; private set; }
    //public LevelStatSystem levelStat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    void Start()
    {
        player = GameObject.Find("Player").GetComponent<PlayerStateMachine>();
        Resource = GameObject.Find("PlayerResource").GetComponent<PlayerResource>();
        //var ui = FindObjectOfType<PlayerStatUIComponent>();
        //var playerStatComp = player.GetComponent<PlayerStatComponent>();
        //ui.Init(playerStatComp);
    }

    public void getPlayerExpKillEnemy(float exp)
    {
        player.playerStat.level.AddExp(exp);
    }

    public void GameReset()
    {
        Debug.Log("게임 재시작");
        //SceneManager.LoadScene("Sample");
    }

}
