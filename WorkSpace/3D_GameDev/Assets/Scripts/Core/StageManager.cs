using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public GameObject player;

    [SerializeField] private readonly string SPAWN_POINT_TAGNAME = "SpawnPoint";

    public string CurrentStageName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CurrentStageName = SceneManager.GetActiveScene().name;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Start 씬은 스폰 안 함
        if (scene.name == "0_Start" || scene.name == "01_HUD")
            return;

        PositionPlayerToSpawn();
    }

    public void MoveToStage(string nextStageName)
    {
        CurrentStageName = nextStageName;
        SceneManager.LoadScene(nextStageName, LoadSceneMode.Single);
        SaveSystem.SaveGameData();
    }


    public void PositionPlayerToSpawn()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        GameObject spawn = GameObject.FindWithTag(SPAWN_POINT_TAGNAME);

        if (player != null && spawn != null)
        {
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }
    }

}
