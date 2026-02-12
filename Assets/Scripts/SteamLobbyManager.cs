using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SteamLobbyManager : MonoBehaviour
{
    [Header("References")]
    public NetworkManager networkManager;

    [Header("Lobby Settings")]
    public int maxPlayers = 4;
    public string lobbySceneName = "LobbyScene";

    [Header("Input")]
    public InputActionReference inviteAction;

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> lobbyEntered;

    public static CSteamID CurrentLobbyID { get; private set; }

    private void Start()
    {
        if (!SteamInitializer.IsInitialized)
        {
            Debug.LogError("Steam not initialized!");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        // Enable the invite action
        if (inviteAction != null)
        {
            inviteAction.action.performed += OnInvitePressed;
            inviteAction.action.Enable();
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (inviteAction != null)
        {
            inviteAction.action.performed -= OnInvitePressed;
        }
    }

    private void OnInvitePressed(InputAction.CallbackContext context)
    {
        InviteFriend();
    }

    public void HostLobby()
    {
        Debug.Log("HostLobby() called");

        if (!SteamInitializer.IsInitialized)
        {
            Debug.LogError("Cannot host - Steam not initialized!");
            return;
        }

        Debug.Log("Calling SteamMatchmaking.CreateLobby...");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
        Debug.Log("CreateLobby call completed");
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        Debug.Log($"OnLobbyCreated callback received - Result: {callback.m_eResult}");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Failed to create lobby - Error: {callback.m_eResult}");
            return;
        }

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"✓ Lobby Created - ID: {CurrentLobbyID.m_SteamID}");

        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());

        Debug.Log("Starting host...");
        networkManager.StartHost();

        Debug.Log("Loading lobby scene...");
        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("✓ Invite Received - Joining lobby");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        Debug.Log($"✓ Entered Lobby - ID: {CurrentLobbyID.m_SteamID}");

        if (NetworkServer.active)
        {
            Debug.Log("Already hosting, skipping client connection");
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(CurrentLobbyID, "HostAddress");

        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("No host address found in lobby data");
            return;
        }

        Debug.Log($"Connecting to host: {hostAddress}");
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        Debug.Log("Loading lobby scene...");
        SceneManager.LoadScene(lobbySceneName);
    }

    public void LeaveLobby()
    {
        Debug.Log("Leaving lobby...");

        if (CurrentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
            Debug.Log("Left Steam lobby");
        }

        if (NetworkServer.active)
        {
            networkManager.StopHost();
            Debug.Log("Stopped hosting");
        }
        else if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
            Debug.Log("Disconnected from host");
        }
    }

    public void InviteFriend()
    {
        if (CurrentLobbyID != CSteamID.Nil)
        {
            Debug.Log("Opening Steam invite dialog...");
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
        }
        else
        {
            Debug.LogWarning("Cannot invite - not in a lobby");
        }
    }
}