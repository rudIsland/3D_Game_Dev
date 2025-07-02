using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Stage : MonoBehaviour
{
    [SerializeField] protected GameObject Portal;
    public int clearLevel;

    protected virtual void Start()
    {
        Portal = GameObject.FindGameObjectWithTag("Portal");
        Portal.SetActive(false);
        UIManager.Instance.SetStageClearText();
    }

    protected virtual void Update()
    {
        StageClearCondition();
    }

    //�������� Ŭ�������� ����
    public virtual void StageClearCondition()
    {
        int currentLevel = Player.Instance.playerStateMachine.playerStat.level.currentLevel;
        if (currentLevel >= clearLevel)
        {
            Portal.SetActive(true);
        }
    }

}
