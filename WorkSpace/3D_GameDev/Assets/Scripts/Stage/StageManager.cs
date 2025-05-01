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
            DontDestroyOnLoad(gameObject); // 씬 넘어가도 유지
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }

        CurrentStageName = SceneManager.GetActiveScene().name;

        //SceneManager.LoadScene("01_HUD", LoadSceneMode.Additive);
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
        yield return null;

        GameManager.Instance.ResetEnemyCount();
    }

}
