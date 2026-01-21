using UnityEngine;
using System.Collections;

/// <summary>
/// Deprem mekaniği yöneticisi.
/// Deprem sırasında oyuncu yavaşça kayar (PlayerMovement'ta işlenir).
/// Hayatta kalmak için: Zıpla veya Time Slow kullan!
/// </summary>
public class EarthquakeManager : MonoBehaviour
{
    public static EarthquakeManager Instance { get; private set; }

    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensity = 0.08f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeInterval = 0.3f;

    [Header("Audio")]
    [SerializeField] private bool playEarthquakeSound = true;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State
    private bool isEarthquakeActive = false;
    private float shakeTimer = 0f;
    private CameraController cameraController;

    // Public access
    public bool IsEarthquakeActive => isEarthquakeActive;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        cameraController = FindAnyObjectByType<CameraController>();
    }

    private void Update()
    {
        if (!isEarthquakeActive)
            return;

        // Periyodik screen shake
        shakeTimer -= Time.deltaTime;
        if (shakeTimer <= 0 && cameraController != null)
        {
            cameraController.TriggerShake(shakeIntensity, shakeDuration);
            shakeTimer = shakeInterval;
        }
    }

    /// <summary>
    /// Depremi başlat
    /// </summary>
    public void StartEarthquake(float duration = 0f)
    {
        if (isEarthquakeActive)
            return;

        isEarthquakeActive = true;
        shakeTimer = 0f;

        // Deprem sesi
        if (playEarthquakeSound && SoundManager.instance != null && SoundManager.instance.earthquakeSound != null)
        {
            SoundManager.instance.PlaySound(SoundManager.instance.earthquakeSound);
        }

        if (debugMode)
            Debug.Log($"Earthquake STARTED! Duration: {(duration > 0 ? duration + "s" : "indefinite")}");

        // Süre belirtildiyse otomatik bitir
        if (duration > 0)
        {
            StartCoroutine(StopEarthquakeAfterDelay(duration));
        }
    }

    /// <summary>
    /// Depremi durdur
    /// </summary>
    public void StopEarthquake()
    {
        if (!isEarthquakeActive)
            return;

        isEarthquakeActive = false;

        if (debugMode)
            Debug.Log("Earthquake STOPPED!");
    }

    private IEnumerator StopEarthquakeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopEarthquake();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
