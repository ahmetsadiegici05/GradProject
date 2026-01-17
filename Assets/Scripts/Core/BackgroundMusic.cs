using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance;
    
    [SerializeField] private AudioClip backgroundMusicClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton benzeri yapı: Sahneler arası müzik kesilmesin istersen
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // İstersen bunu açıp objeyi sahneler arası taşıyabilirsin
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Ayarlar
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = volume;
        audioSource.clip = backgroundMusicClip;

        if (backgroundMusicClip != null)
            audioSource.Play();
    }

    private void Start()
    {
        // SoundManager'a kendini bildir
        if (SoundManager.instance != null)
        {
            SoundManager.instance.bgmSource = this.audioSource;
        }
    }

    public void SetVolume(float vol)
    {
        volume = vol;
        if (audioSource != null)
            audioSource.volume = volume;
    }
}
