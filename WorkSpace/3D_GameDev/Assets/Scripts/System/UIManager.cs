using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI enemyCountTXt;
    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Resource = GetComponentInChildren<PlayerResource>();
            GameManager.Instance.levelStatSystem = GetComponentInChildren<LevelStatSystem>();
            GameManager.Instance.enemyCountTxt = enemyCountTXt;

        }
    }
}
