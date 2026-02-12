using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    public NetworkManager networkManager;
    public int maxPlayers = 4;
    public string lobbySceneName = "LobbyScene";

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> lobbyEntered;

    public static CSteamID CurrentLobbyID { get; private set; }

    private void Start()
    {
        if (!SteamInitializer.IsInitialized)  // Changed this line
        {
            Debug.LogError("Steam not initialized!");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        DontDestroyOnLoad(gameObject);
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create lobby");
            return;
        }

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "HostAddress", SteamUser.GetSteamID().ToString());

        networkManager.StartHost();

        Debug.Log("✓ Lobby Created - ID: " + CurrentLobbyID.m_SteamID);

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
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(CurrentLobbyID, "HostAddress");

        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("No host address found in lobby data");
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
            networkManager.StopHost();
        else if (NetworkClient.isConnected)
            networkManager.StopClient();
    }

    public void InviteFriend()
    {
        if (CurrentLobbyID != CSteamID.Nil)
        {
            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
        }
        else
        {
            Debug.LogWarning("Cannot invite - not in a lobby");
        }
    }
}