using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;
    public GameObject player;

    [SerializeField] private readonly string SPAWN_POINT_TAGNAME = "SpawnPoint";

    public string CurrentStageName { get; private set; }

    private void Awake()
    {
        CurrentStageName = SceneManager.GetActiveScene().name;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MoveToStage(string nextStageName)
    {
        StartCoroutine(TransitionStage(nextStageName));
    }

    private IEnumerator TransitionStage(string nextStageName)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        if (!string.IsNullOrEmpty(CurrentStageName))
        {
            yield return SceneManager.UnloadSceneAsync(CurrentStageName);
            Debug.Log($"[StageManager] Unloaded: {CurrentStageName}");
        }

        yield return SceneManager.LoadSceneAsync(nextStageName, LoadSceneMode.Additive);
        CurrentStageName = nextStageName;
        Debug.Log($"[StageManager] Loaded: {nextStageName}");

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
    }
}
