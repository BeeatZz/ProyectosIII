using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Collider))]
public class AllPlayersZone : NetworkBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;

    [SerializeField] private float checkDelay = 3f;

    private readonly HashSet<NetworkIdentity> playersInside = new();
    private Coroutine pendingCheck;


    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return;

        if (other.TryGetComponent<NetworkIdentity>(out var identity))
        {
            playersInside.Add(identity);
            RestartCheck();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active) return;

        if (other.TryGetComponent<NetworkIdentity>(out var identity))
            playersInside.Remove(identity);

    }


    private void RestartCheck()
    {
        if (pendingCheck != null)
            StopCoroutine(pendingCheck);

        pendingCheck = StartCoroutine(CheckAfterDelay());
    }

    private IEnumerator CheckAfterDelay()
    {
        yield return new WaitForSeconds(checkDelay);

        int totalPlayers = NetworkServer.connections.Count;

        if (totalPlayers == 0) yield break;

        playersInside.RemoveWhere(id => id == null || !id.isActiveAndEnabled);

        if (playersInside.Count >= totalPlayers)
        {
            sceneLoader.LoadScene();
        }
        

        pendingCheck = null;
    }
}