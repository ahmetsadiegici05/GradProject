using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Player Sounds")]
    public AudioClip jumpSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip healSound;
    public AudioClip landSound;
    public AudioClip stepSound;
    public AudioClip loseSound;

    [Header("Environment/Enemy Sounds")]
    public AudioClip enemyDeathSound;
    public AudioClip explosionSound;
    public AudioClip earthquakeSound;

    [Header("Time Slow Sounds")]
    public AudioClip timeSlowStartSound;
    public AudioClip timeSlowLoopSound;
    public AudioClip timeSlowEndSound;

    [Header("Audio Sources")]
    /*[SerializeField]*/ public AudioSource bgmSource; // BGM kaynağını inspector'dan ata
    private AudioSource audioSource; // SFX için
    private AudioSource timeSlowLoopSource; // Loop sesi için ayrı kaynak

    private void Awake()
    {
        // Singleton Pattern
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // Eğer tüm oyun boyunca tek bir manager kalacaksa açılabilir
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Loop sesi için ayrı bir AudioSource oluştur
        timeSlowLoopSource = gameObject.AddComponent<AudioSource>();
        timeSlowLoopSource.loop = true;
        timeSlowLoopSource.playOnAwake = false;
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.pitch = 1f; // Normal sesler için pitch'i düzelt
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlayStepSound()
    {
        if (stepSound != null)
        {
            // Pitch Randomization: 0.9 ila 1.1 arasında rastgele
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(stepSound);
        }
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    // --- Time Slow Logic ---

    public void StartTimeSlowSequence()
    {
        // 1. Start sesini çal
        PlaySound(timeSlowStartSound);

        // 2. Loop sesini başlat (Fade In ile)
        if (timeSlowLoopSound != null)
        {
            timeSlowLoopSource.clip = timeSlowLoopSound;
            timeSlowLoopSource.volume = 0f;
            timeSlowLoopSource.Play();
            StartCoroutine(FadeSource(timeSlowLoopSource, 0.5f, 1f)); // 0.5 saniyede ses açılsın
        }

        // 3. BGM Pitch düşür
        if (bgmSource != null)
        {
            StartCoroutine(LerpPitch(bgmSource, 0.5f, 0.5f)); // 0.5 saniyede 0.5 pitch'e düş
        }
    }

    public void StopTimeSlowSequence()
    {
        // 1. End sesini çal
        PlaySound(timeSlowEndSound);

        // 2. Loop sesini durdur (Fade Out ile)
        StartCoroutine(FadeSource(timeSlowLoopSource, 0.5f, 0f, true));

        // 3. BGM Pitch normale döndür
        if (bgmSource != null)
        {
            StartCoroutine(LerpPitch(bgmSource, 0.5f, 1f));
        }
    }

    private System.Collections.IEnumerator FadeSource(AudioSource source, float duration, float targetVolume, bool stopAfter = false)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Time slow'dan etkilenmesin
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
        if (stopAfter) source.Stop();
    }

    private System.Collections.IEnumerator LerpPitch(AudioSource source, float duration, float targetPitch)
    {
        float startPitch = source.pitch;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.pitch = Mathf.Lerp(startPitch, targetPitch, elapsed / duration);
            yield return null;
        }

        source.pitch = targetPitch;
    }
}
