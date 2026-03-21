using UnityEngine;
using Mirror;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject confirmExitPanel;

    public static bool isPaused = false; 

    void Update()
    {
        if (!NetworkClient.isConnected) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmExitPanel.activeSelf)
            {
                confirmExitPanel.SetActive(false);
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        confirmExitPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenExitConfirmation()
    {
        confirmExitPanel.SetActive(true);
    }

    public void CancelExit()
    {
        confirmExitPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Debug.Log("Salir del juego");
        Application.Quit();
    }
}