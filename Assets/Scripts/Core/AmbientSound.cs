using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class AmbientSound : MonoBehaviour
{
    public static AmbientSound Instance;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip forestAmbienceClip;
    [Range(0f, 1f)]
    [SerializeField] private float baseVolume = 0.4f;

    [Header("Time Slow Effect")]
    [SerializeField] private float smoothSpeed = 3.0f; // Geçiş hızı
    [SerializeField] private float normalPitch = 1.0f;
    [SerializeField] private float slowPitch = 0.5f;
    
    // LowPassFilter ayarları: 22000Hz (Tam net) -> 700Hz (Boğuk)
    [SerializeField] private float normalCutoffFreq = 22000f; 
    [SerializeField] private float slowCutoffFreq = 700f;   

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
            
        // Componentleri al
        audioSource = GetComponent<AudioSource>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();

        // Code setup...
        audioSource.clip = forestAmbienceClip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        
        // Kaydedilen ses ayarını yükle
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", baseVolume);
        audioSource.volume = savedVolume;
        
        // Başlangıçta net duyulsun
        audioSource.pitch = normalPitch;
        lowPassFilter.cutoffFrequency = normalCutoffFreq;

        if (forestAmbienceClip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    private void OnValidate()
    {
        // Editor içinde dosyayı sürüklediğinde AudioSource'da da görünsün diye
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        if (audioSource != null)
        {
            audioSource.clip = forestAmbienceClip;
            audioSource.volume = baseVolume;
        }
    }

    private void Start()
    {
        // Zaten Awake'te play dedik, burayı basitleştirebiliriz
    }

    private void Update()
    {
        // Time Slow aktif mi kontrol et
        bool isSlowMo = false;
        if (TimeSlowAbility.Instance != null)
        {
            isSlowMo = TimeSlowAbility.Instance.IsSlowMotionActive;
        }

        // Hedef değerleri belirle
        float targetPitch = isSlowMo ? slowPitch : normalPitch;
        float targetCutoff = isSlowMo ? slowCutoffFreq : normalCutoffFreq;

        // Yumuşak geçiş için Lerp kullan (Time.unscaledDeltaTime kullanıyoruz çünkü oyun zamanı yavaşlıyor)
        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.unscaledDeltaTime * smoothSpeed);
        lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, targetCutoff, Time.unscaledDeltaTime * smoothSpeed);
    }
}
