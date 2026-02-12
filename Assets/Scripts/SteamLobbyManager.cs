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

        if (!SteamInitializer.IsInitialized)
        {
            return;
        }

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            return;
        }

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());

        networkManager.StartHost();

        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);


        if (NetworkServer.active)
        {
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(CurrentLobbyID, "HostAddress");

        if (string.IsNullOrEmpty(hostAddress))
        {
            return;
        }

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        SceneManager.LoadScene(lobbySceneName);
    }

    public void LeaveLobby()
    {

        if (CurrentLobbyID != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyID);
            CurrentLobbyID = CSteamID.Nil;
        }

        if (NetworkServer.active)
        {
            networkManager.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
        }
    }

    public void InviteFriend()
    {
        if (CurrentLobbyID != CSteamID.Nil)
        {
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
        }
        else
        {
        }
    }
}