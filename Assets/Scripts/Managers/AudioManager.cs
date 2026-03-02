using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("Audio Clip")]
    public AudioClip mainMenuMusic;
    public AudioClip gameMusic;

    public AudioClip attackSound;
    public AudioClip moveSound;
    public AudioClip damageSound;
    public AudioClip shootSound;
    public AudioClip healSound;
    public AudioClip yourTurnSound;
    public AudioClip enemyMoveSound;
    public AudioClip enemyNotMoveSound;
    public AudioClip winSound;
    public AudioClip loseSound;



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
        // check current scene
        if (SceneManager.GetActiveScene().name == "MainMenu")
            musicSource.clip = mainMenuMusic;

        if (SceneManager.GetActiveScene().name == "Game")
            musicSource.clip = gameMusic;

        musicSource.volume = 0.1f;
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