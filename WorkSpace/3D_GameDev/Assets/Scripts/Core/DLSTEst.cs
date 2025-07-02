using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DLSTEst : MonoBehaviour
{
    public uint dlcAPPID = 480;

    private void Start()
    {
        if (SteamManager.Initialized)
        {
            bool IsPurchaseDLC = SteamApps.BIsDlcInstalled(new AppId_t(dlcAPPID));
            Debug.Log($"DLC���ſ���: {IsPurchaseDLC}");
        }
        else
        {
            Debug.LogWarning("Steam�ʱ�ȭ ����");
        }
    }
}
