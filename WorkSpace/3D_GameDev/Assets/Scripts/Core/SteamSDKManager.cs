using Steamworks;
using UnityEngine;

public class SteamSDKManager : MonoBehaviour
{
    public static bool Initialized { get; private set; }

    void Awake()
    {
        if (!SteamAPI.Init())
        {
            Debug.LogError("Steam �ʱ�ȭ ����");
            Initialized = false;
            return;
        }

        Initialized = true;
        Debug.Log("Steam �ʱ�ȭ �Ϸ�");
    }

    void Update()
    {
        if (Initialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        if (Initialized)
        {
            SteamAPI.Shutdown();
        }
    }


}
