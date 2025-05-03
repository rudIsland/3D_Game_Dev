using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public GameObject player;
    public Stage currentStage;

    [SerializeField] private readonly string SPAWN_POINT_TAGNAME = "SpawnPoint";

    public string CurrentStageName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        CurrentStageName = SceneManager.GetActiveScene().name;
    }

    public void MoveToStage(string nextStageName)
    {
        StartCoroutine(TransitionStage(nextStageName));
    }

    private IEnumerator TransitionStage(string nextStageName)
    {
        if (!string.IsNullOrEmpty(CurrentStageName))
        {
            yield return SceneManager.UnloadSceneAsync(CurrentStageName);
            Debug.Log($"[StageManager] Unloaded: {CurrentStageName}");
        }

        yield return SceneManager.LoadSceneAsync(nextStageName);
        CurrentStageName = nextStageName;
        Debug.Log($"[StageManager] Loaded: {nextStageName}");

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        GameObject spawn = GameObject.FindWithTag(SPAWN_POINT_TAGNAME);
        if (spawn != null && player != null)
        {
            player.transform.position = spawn.transform.position;
            player.transform.rotation = spawn.transform.rotation;
        }
        else
        {
            Debug.LogWarning($"[StageManager] Spawn point not found in stage '{nextStageName}'.");
        }

        // 현재 스테이지 스크립트 참조
        currentStage = GameObject.FindObjectOfType<Stage>();
        if (currentStage == null)
            Debug.LogWarning($"[StageManager] No Stage script found in '{nextStageName}' scene.");

        yield return null;

    }

}
