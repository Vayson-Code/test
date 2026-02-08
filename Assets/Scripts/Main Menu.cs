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

    public void Play()
    {
        audioManager.PlayHitSound(audioManager.walk);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");

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

}