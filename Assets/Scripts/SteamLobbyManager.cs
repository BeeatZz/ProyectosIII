using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

#if !DISABLESTEAMWORKS
using Steamworks;
using UnityEngine.SceneManagement;
#endif

public class SteamLobbyManager : MonoBehaviour
{

    public static bool DevMode { get; private set; }
    public NetworkManager networkManager;
    public int maxPlayers = 4;
    public string lobbySceneName = "LobbyScene";
    public InputActionReference inviteAction;
    [SerializeField] private bool useSteam = true;
    [SerializeField] private string devHostAddress = "localhost";
#if !DISABLESTEAMWORKS
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> lobbyEntered;
    public static CSteamID CurrentLobbyID { get; private set; }
#endif
    private void Start()
    {
        DevMode = !useSteam;
        DontDestroyOnLoad(gameObject);

        if (!useSteam)
        {
            return;
        }

#if !DISABLESTEAMWORKS
        if (!SteamInitializer.IsInitialized)
        {
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        if (inviteAction != null)
        {
            inviteAction.action.performed += OnInvitePressed;
            inviteAction.action.Enable();
        }
#endif
    }

    private void OnDestroy()
    {
#if !DISABLESTEAMWORKS
        if (inviteAction != null)
            inviteAction.action.performed -= OnInvitePressed;
#endif
    }

 
    public void HostLobby()
    {
        if (!useSteam)
        {
            return;
        }

#if !DISABLESTEAMWORKS
        if (!SteamInitializer.IsInitialized) return;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
#endif
    }

    public void LeaveLobby()
    {
#if !DISABLESTEAMWORKS
        if (useSteam && CurrentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
        }
#endif
        if (NetworkServer.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }

    public void InviteFriend()
    {
#if !DISABLESTEAMWORKS
        if (!useSteam) return;

        if (CurrentLobbyID != CSteamID.Nil)
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
#endif
    }

    public void DevHost()
    {
        NetworkManager.singleton.StartHost();
    }

    public void DevJoin()
    {
        NetworkManager.singleton.networkAddress = devHostAddress;
        NetworkManager.singleton.StartClient();
    }

#if !DISABLESTEAMWORKS
    private void OnInvitePressed(InputAction.CallbackContext context) => InviteFriend();

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            return;
        }

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "name",
            SteamFriends.GetPersonaName() + "'s Lobby");
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "HostAddress",
            SteamUser.GetSteamID().ToString());

        NetworkManager.singleton.StartHost();
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(CurrentLobbyID, "HostAddress");
        if (string.IsNullOrEmpty(hostAddress))
        {
            return;
        }

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();
    }
#endif
}