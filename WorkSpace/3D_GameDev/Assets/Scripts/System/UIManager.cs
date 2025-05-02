using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI enemyCountTXt;
    public PlayerResource playerResource;
    public LevelStatSystem levelStatSystem;

    public static UIManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }

        playerResource = GetComponentInChildren<PlayerResource>();
        levelStatSystem = GetComponentInChildren<LevelStatSystem>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.enemyCountTxt = enemyCountTXt;
        }
    }
}
