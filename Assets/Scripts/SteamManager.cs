using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private static SteamManager s_instance;
    private static bool s_EverInitialized = false;

    public static SteamManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                return new GameObject("SteamManager").AddComponent<SteamManager>();
            }
            return s_instance;
        }
    }

    public static bool Initialized
    {
        get
        {
            return s_instance != null && s_EverInitialized;
        }
    }

    private void Awake()
    {
        if (s_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Packsize.Test())
        {
            Debug.LogError("[Steamworks.NET] Packsize Test returned false");
            return;
        }

        if (!DllCheck.Test())
        {
            Debug.LogError("[Steamworks.NET] DllCheck Test returned false");
            return;
        }

        try
        {
            if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
            {
                Application.Quit();
                return;
            }
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. " + e);
            Application.Quit();
            return;
        }

        s_EverInitialized = SteamAPI.Init();
        if (!s_EverInitialized)
        {
            Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Is Steam running?");
        }
        else
        {
            Debug.Log("[Steamworks.NET] Initialized successfully");
        }
    }

    private void OnEnable()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
    }

    private void OnDestroy()
    {
        if (s_instance != this)
        {
            return;
        }

        s_instance = null;

        if (!s_EverInitialized)
        {
            return;
        }

        SteamAPI.Shutdown();
    }

    private void Update()
    {
        if (!s_EverInitialized)
        {
            return;
        }

        SteamAPI.RunCallbacks();
    }
}