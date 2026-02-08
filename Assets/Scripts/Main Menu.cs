using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    AudioManager audioManager;
    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Start()
    {
        
    }

    public void Play(string sceneName)
    {
        audioManager.PlayHitSound(audioManager.walk);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

    }

    public void Quit()
    {
        audioManager.PlayHitSound(audioManager.hit);
        Application.Quit();
    }

    public void TogglePanel(GameObject panel)
    {
        audioManager.PlayHitSound(audioManager.swooch);
        panel.SetActive(!panel.activeSelf);
        
    }
    public void TogglePanel3(GameObject currentPanel)
    {

        // Deactivate the current panel
        if (currentPanel != null)
            currentPanel.SetActive(false);
    }


}