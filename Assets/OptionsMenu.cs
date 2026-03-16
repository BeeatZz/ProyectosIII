using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    public GameObject audioPanel;
    public GameObject videoPanel;
    public GameObject gameplayPanel;

    void Start()
    {
        OpenAudio(); // panel que se abre por defecto
    }

    public void OpenAudio()
    {
        audioPanel.SetActive(true);
        videoPanel.SetActive(false);
        gameplayPanel.SetActive(false);
    }

    public void OpenVideo()
    {
        audioPanel.SetActive(false);
        videoPanel.SetActive(true);
        gameplayPanel.SetActive(false);
    }

    public void OpenGameplay()
    {
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
        gameplayPanel.SetActive(true);
    }
}