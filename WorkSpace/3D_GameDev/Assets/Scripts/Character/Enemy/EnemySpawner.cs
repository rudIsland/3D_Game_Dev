using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰할 Enemy 프리팹")]
    public Enemy SpawnEnemyPrefabs;

    private Enemy SpawnedEnemy;
    private bool isRespawning = false;

    private void Start()
    {
        SpawnEnemy();
    }


    private void SpawnEnemy()
    {
        SpawnedEnemy = Instantiate(SpawnEnemyPrefabs, transform.position, transform.rotation);
 
        SceneManager.MoveGameObjectToScene(SpawnedEnemy.gameObject, gameObject.scene);
        SpawnedEnemy.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath()
    {
        if (!isRespawning)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        isRespawning = true;

        yield return new WaitForSeconds(5f);

        SpawnEnemy();

        isRespawning = false;
    }
}
