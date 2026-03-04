using Mirror;
using TMPro;
using UnityEngine;
using System.Collections;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private GameObject nameplateRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Camera targetCamera;

    private PlayerLobbyInfo lobbyInfo;

    private void Start()
    {
        lobbyInfo = GetComponentInParent<PlayerLobbyInfo>(true);

        if (isLocalPlayer)
        {
            if (nameplateRoot != null)
                nameplateRoot.SetActive(true);
            if (targetCamera == null)
                targetCamera = Camera.main;
            return;
        }

        StartCoroutine(WaitForName());
    }

    private IEnumerator WaitForName()
    {
        while (lobbyInfo == null || string.IsNullOrEmpty(lobbyInfo.playerName))
            yield return null;

        ApplyName(lobbyInfo.playerName);
    }

    public void RefreshName()
    {
        if (lobbyInfo == null)
            lobbyInfo = GetComponentInParent<PlayerLobbyInfo>(true);

        if (lobbyInfo != null && !string.IsNullOrEmpty(lobbyInfo.playerName))
            ApplyName(lobbyInfo.playerName);
        else
            StartCoroutine(WaitForName());
    }

    private void LateUpdate()
    {
        if (isLocalPlayer || nameplateRoot == null || !nameplateRoot.activeSelf) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        nameplateRoot.transform.LookAt(
            nameplateRoot.transform.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up);
    }

    private void ApplyName(string name)
    {
        if (nameText != null)
            nameText.text = name;
        
    }
}