using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject confirmExitPanel;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmExitPanel.activeSelf)
            {
                // Si está el confirm abierto, lo cerramos primero
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

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        confirmExitPanel.SetActive(false);
        Time.timeScale = 1f;
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
