using UnityEngine;
using Steamworks;

public class SteamInitializer : MonoBehaviour
{
    private static SteamInitializer instance;
    public static bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSteam();
    }

    private void InitializeSteam()
    {
        try
        {
            IsInitialized = SteamAPI.Init();

            if (!IsInitialized)
            {
                Debug.LogError("[Steam] Failed to initialize. Is Steam running?");
                return;
            }

            Debug.Log("[Steam] Initialized successfully!");
            Debug.Log($"[Steam] User: {SteamFriends.GetPersonaName()}");
            Debug.Log($"[Steam] SteamID: {SteamUser.GetSteamID()}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Steam] Exception during initialization: {e}");
            IsInitialized = false;
        }
    }

    private void Update()
    {
        if (IsInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        if (IsInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[Steam] Shutdown");
        }
    }
}