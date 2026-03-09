using UnityEngine;
using Mirror;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class PlayerLobbyInfo : NetworkBehaviour
{
    [SerializeField] private string devPlayerName = "DevPlayer";
    [SerializeField] private ulong devSteamID = 1000;

    [SyncVar(hook = nameof(OnSteamIDChanged))]
    public ulong steamID;

    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerName;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (!string.IsNullOrEmpty(playerName))
        {
            return;
        }

        if (!SteamLobbyManager.DevMode && IsSteamAvailable())
        {
#if !DISABLESTEAMWORKS
            ulong id = SteamUser.GetSteamID().m_SteamID;
            string name = SteamFriends.GetPersonaName();
            CmdSetPlayerInfo(id, name);
#endif
        }
        else
        {
            CmdSetPlayerInfo(devSteamID, devPlayerName);
        }
    }

    [Command]
    private void CmdSetPlayerInfo(ulong id, string name)
    {
        steamID = id;
        playerName = name;
    }

    private void OnSteamIDChanged(ulong oldID, ulong newID)
    {
        UpdateUI();
    }

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        GetComponent<PlayerNameplate>()?.RefreshName();
        UpdateUI();
    }

    private void UpdateUI()
    {
        LobbyUIManager lobbyUI = FindFirstObjectByType<LobbyUIManager>();
        if (lobbyUI != null)
            lobbyUI.UpdatePlayerDisplay(this);
    }

    private static bool IsSteamAvailable()
    {
#if DISABLESTEAMWORKS
        return false;
#else
        try
        {
            return SteamAPI.IsSteamRunning();
        }
        catch
        {
            return false;
        }
#endif
    }
}