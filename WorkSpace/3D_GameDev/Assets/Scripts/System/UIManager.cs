using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public PlayerStatUIComponent playerStatUIComp;
    public LevelExpComponent levelExpComp;
    public LevelUpComponent levelupComp;
    public GameObject levelUpPanel;

    public BaseStateMachine stateMachine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ///DontDestroyOnLoad(gameObject); // 씬 넘어도 유지
        }
        else
        {
            //Destroy(gameObject);
        }
    }
}
