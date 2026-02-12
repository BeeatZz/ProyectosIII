using UnityEngine;

public class InviteButton : MonoBehaviour
{
    public void OnInviteButtonClicked()
    {
        SteamLobbyManager lobbyManager = FindObjectOfType<SteamLobbyManager>();

        if (lobbyManager != null)
        {
            lobbyManager.InviteFriend();
        }
        else
        {
        }
    }
}