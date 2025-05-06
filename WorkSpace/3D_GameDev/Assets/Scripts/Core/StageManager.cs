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

        CurrentStageName = SceneManager.GetActiveScene().name;
    }

    public void MoveToStage(string nextStageName)
    {
        StartCoroutine(LoadAndPosition(nextStageName));
    }



    private IEnumerator LoadAndPosition(string nextStageName)
    {
        // 1. �� ��� �̵�
        SceneManager.LoadScene(nextStageName);
        CurrentStageName = nextStageName;
        Debug.Log($"[StageManager] Loaded: {nextStageName}");

        // 2. 1������ ��� (�� �� ������Ʈ���� Awake ����ǵ���)
        yield return null;

        // 3. �÷��̾� ���� �� ��ġ �̵�
        player = GameObject.FindGameObjectWithTag("Player");
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
