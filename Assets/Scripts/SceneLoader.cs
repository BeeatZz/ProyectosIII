using UnityEngine;
using Mirror;

public class SceneLoader : NetworkBehaviour
{
    public string gameSceneName = "TestScene";

    public void LoadScene() //este metodo solo puede llamarlo el host, y se usa para cualquier cambio de escena que se necesite hacer
    {
        if (!NetworkServer.active)
        {
            return; 
        }

        NetworkManager.singleton.ServerChangeScene(gameSceneName);
    }
}