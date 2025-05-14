using Steamworks;
using UnityEngine;

public class SteamSDKManager : MonoBehaviour
{
    public static bool Initialized { get; private set; }

    void Awake()
    {
        if (!SteamAPI.Init())
        {
            Debug.LogError("Steam 초기화 실패");
            Initialized = false;
            return;
        }

        Initialized = true;
        Debug.Log("Steam 초기화 완료");
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
