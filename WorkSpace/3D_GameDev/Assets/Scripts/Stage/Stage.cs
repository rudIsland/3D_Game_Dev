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
        UIManager.Instance.SetStageClearText();
    }

    //�������� Ŭ�������� ����
    public virtual void StageClearCondition()
    {

    }

}
