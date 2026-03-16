using UnityEngine;
using Mirror;

public class HostOnlyButton : MonoBehaviour
{
    public GameObject objectToHide;

    void Update()
    {
        if (!NetworkClient.active && !NetworkServer.active)
            return;

        objectToHide.SetActive(NetworkServer.active);

        enabled = false;
    }
}