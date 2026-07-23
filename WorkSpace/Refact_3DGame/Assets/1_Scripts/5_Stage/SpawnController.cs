using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SpawnController : MonoBehaviour
{
    [Header("Stage Config")]
    public StageData stageData;
    public Transform[] spawnPoints;
    public GameObject portal;

    [Header("UI Settings")]
    public TextMeshProUGUI killCountText;

    [Header("Respawn Settings")]
    public float respawnDelay = 5f;

    private Queue<Enemy> _enemyPool = new Queue<Enemy>();
    private int _totalSpawnedCount = 0;
    private int _currentKills = 0; // 이 스테이지에서의 카운트 (UI 표시용)
    private bool _isStageCleared = false;

    // 플레이어 데이터 참조를 위한 변수
    private PlayerStateMachine _player;

    void Start()
    {
        if (portal != null) portal.SetActive(false);

        // [수정] 스테이지 시작 시 플레이어의 저장된 처치수를 불러옴
        StartCoroutine(WaitAndInitialize());
    }

    // 플레이어가 스폰될 때까지 잠시 기다린 후 데이터를 연동합니다.
    private IEnumerator WaitAndInitialize()
    {
        while (StageManager.Instance.player == null)
        {
            yield return null;
        }

        _player = StageManager.Instance.player;

        if (stageData != null)
        {
            // [핵심] 저장된 처치수 데이터를 불러와 현재 처치수에 할당
            _currentKills = _player.killCount;

            InitializeSpawner();
            UpdateKillUI();

            // 만약 시작하자마자 이미 목표치를 달성한 상태라면 포탈을 엽니다.
            if (_currentKills >= stageData.clearKillCount)
            {
                ClearStage();
            }
        }
    }

    private void InitializeSpawner()
    {
        // _currentKills = 0; // 이 줄을 삭제하여 저장된 값이 유지되게 합니다.
        _totalSpawnedCount = 0;
        _isStageCleared = false;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Enemy newEnemy = Instantiate(stageData.monsterPrefab);
            newEnemy.gameObject.SetActive(false);

            int index = i;
            newEnemy.OnEnemyDeath += (enemy) => HandleMonsterDeath(enemy);
            newEnemy.OnDeathEffectFinished += (enemy) => HandleEffectFinished(enemy, index);

            _enemyPool.Enqueue(newEnemy);
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnMonsterAtIndex(i);
        }
    }

    private void SpawnMonsterAtIndex(int index)
    {
        if (_isStageCleared || _enemyPool.Count <= 0) return;

        Enemy enemy = _enemyPool.Dequeue();
        enemy.transform.position = spawnPoints[index].position;
        enemy.transform.rotation = spawnPoints[index].rotation;
        enemy.gameObject.SetActive(true);
        enemy.InitializeEnemy();

        _totalSpawnedCount++;
    }

    private void HandleMonsterDeath(Enemy instance)
    {
        // [수정] 플레이어 스크립트의 킬 카운트 증가 함수 호출
        if (_player != null)
        {
            _player.OnEnemyKilled(); // saveData.killCount 증가 및 저장 로직 포함
            _currentKills = _player.killCount; // 최신화된 저장 데이터를 가져옴
        }
        else
        {
            _currentKills++;
        }

        UpdateKillUI();

        if (_player != null)
        {
            _player.AddExperience(instance.deathEXP);
            var targeter = _player.GetComponent<PlayerTargeter>();
            if (targeter != null) targeter.CanCel();
        }

        if (!_isStageCleared && _currentKills >= stageData.clearKillCount)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        _isStageCleared = true;
        if (portal != null) portal.SetActive(true);

        if (killCountText != null)
            killCountText.color = Color.yellow;

        Debug.Log($"스테이지 클리어! 현재 총 {_currentKills}마리 처치 기록 보유.");
    }

    private void HandleEffectFinished(Enemy instance, int originalIndex)
    {
        instance.gameObject.SetActive(false);
        _enemyPool.Enqueue(instance);

        if (!_isStageCleared)
        {
            StartCoroutine(DelayedRespawn(respawnDelay, originalIndex));
        }
    }

    private void UpdateKillUI()
    {
        if (killCountText != null)
        {
            // 저장된 수치가 UI에 바로 반영됩니다.
            killCountText.text = $"Kills: {_currentKills} / {stageData.clearKillCount}";
        }
    }

    private IEnumerator DelayedRespawn(float delay, int index)
    {
        yield return new WaitForSeconds(delay);
        SpawnMonsterAtIndex(index);
    }
}