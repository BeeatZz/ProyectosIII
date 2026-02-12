using UnityEngine;
using Mirror;
using Steamworks;

public class PlayerLobbyInfo : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSteamIDChanged))]
    public ulong steamID;

    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerName;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Send our Steam info to the server
        CmdSetSteamInfo(SteamUser.GetSteamID().m_SteamID, SteamFriends.GetPersonaName());
    }

    [Command]
    private void CmdSetSteamInfo(ulong steamID, string name)
    {
        this.steamID = steamID;
        this.playerName = name;
    }

    private void OnSteamIDChanged(ulong oldID, ulong newID)
    {
        steamID = newID;
        UpdateUI();
    }

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        playerName = newName;
        UpdateUI();
    }

    private void UpdateUI()
    {
        LobbyUIManager lobbyUI = FindObjectOfType<LobbyUIManager>();
        if (lobbyUI != null)
        {
            lobbyUI.UpdatePlayerDisplay(this);
        }
    }
}