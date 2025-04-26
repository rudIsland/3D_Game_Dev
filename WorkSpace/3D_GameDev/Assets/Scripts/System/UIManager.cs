using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Resource = GetComponentInChildren<PlayerResource>();
            GameManager.Instance.levelStatSystem = GetComponentInChildren<LevelStatSystem>();
        }
    }
}
