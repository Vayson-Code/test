using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [Header("-------Audio Sources--------")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource SFXSource;
    [Header("-------Audio Clip--------")]
    public AudioClip background;
    public AudioClip hit;
    public AudioClip fall;
    public AudioClip swooch;
    public AudioClip walk;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }
    public void PlayHitSound(AudioClip audio)
    {
        SFXSource.PlayOneShot(audio);
    }
}
