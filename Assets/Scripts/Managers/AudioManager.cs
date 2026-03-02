using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("Audio Clip")]
    public AudioClip backgroundGame;
    public AudioClip attackSound;

    public static AudioManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        musicSource.clip = backgroundGame;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    /*public void PlayOnce(AudioClip clip)
    {
        if (SFXSource.clip == clip && SFXSource.isPlaying)
            return;

        SFXSource.clip = clip;
        SFXSource.Play();
    }*/

    public void StopSFX()
    {
        SFXSource.Stop();
    }
}