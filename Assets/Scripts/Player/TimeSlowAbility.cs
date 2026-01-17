using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Oyuncuya zaman yavaşlatma yeteneği verir.
/// E tuşuna basınca kısa süreliğine zaman yavaşlar.
/// </summary>
public class TimeSlowAbility : MonoBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] private KeyCode activateKey = KeyCode.E;
    [SerializeField] private float slowMotionScale = 0.3f;      // Zaman ne kadar yavaşlasın (0.3 = %30 hız)
    [SerializeField] private float slowDuration = 2f;           // Kaç saniye sürsün (gerçek zamanda)
    [SerializeField] private float cooldownTime = 8f;           // Tekrar kullanmak için bekleme

    [Header("Visual Effects")]
    [SerializeField] private bool useVisualEffects = true;
    [SerializeField] private Color slowMotionTint = new Color(0.7f, 0.85f, 1f, 1f); // Hafif mavi ton
    [SerializeField] private float vignetteIntensity = 0.3f;

    [Header("UI (Optional)")]
    [SerializeField] private Image cooldownFillImage;           // Cooldown göstergesi
    [SerializeField] private Image abilityIconImage;            // Ability ikonu

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource activateSound;
    [SerializeField] private AudioSource deactivateSound;

    private float cooldownTimer = 0f;
    private bool isSlowMotionActive = false;
    private float originalFixedDeltaTime;
    private SpriteRenderer[] allSpriteRenderers;
    private Color[] originalColors;

    // Static instance for easy access
    public static TimeSlowAbility Instance { get; private set; }
    
    public bool IsSlowMotionActive => isSlowMotionActive;
    public float CooldownProgress => cooldownTimer <= 0 ? 1f : 1f - (cooldownTimer / cooldownTime);
    public bool IsReady => cooldownTimer <= 0f && !isSlowMotionActive;
    
    /// <summary>
    /// Time slow aktifken oyuncunun normal hızda kalması için kullanılacak çarpan.
    /// Örn: timeScale=0.3 iken bu değer ~3.33 olur, böylece oyuncu normal hızda kalır.
    /// </summary>
    public float PlayerTimeCompensation => isSlowMotionActive ? (1f / Time.timeScale) : 1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
            
        originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        // Sahnedeki tüm sprite'ları bul (görsel efekt için)
        if (useVisualEffects)
        {
            allSpriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            originalColors = new Color[allSpriteRenderers.Length];
            for (int i = 0; i < allSpriteRenderers.Length; i++)
            {
                originalColors[i] = allSpriteRenderers[i].color;
            }
        }
    }

    private void Update()
    {
        // Cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.unscaledDeltaTime; // unscaled çünkü zaman yavaşken de çalışmalı
        }

        // UI güncelle
        UpdateUI();

        // Ability aktifleştirme
        if (Input.GetKeyDown(activateKey) && IsReady)
        {
            StartCoroutine(SlowMotionRoutine());
        }
    }

    private IEnumerator SlowMotionRoutine()
    {
        isSlowMotionActive = true;

        // SoundManager Integration
        if (SoundManager.instance != null)
            SoundManager.instance.StartTimeSlowSequence();

        // Ses efekti (Eski kod iptal)
        /*if (activateSound != null)
            activateSound.Play();*/

        // Zaman yavaşlat
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionScale;

        // Görsel efekt - renk tonu
        if (useVisualEffects)
            ApplySlowMotionVisuals(true);

        // Süre boyunca bekle (unscaled time kullan)
        float elapsed = 0f;
        while (elapsed < slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Süre bitmeden önce hafif "fade out" efekti
            if (elapsed > slowDuration * 0.7f)
            {
                float fadeProgress = (elapsed - slowDuration * 0.7f) / (slowDuration * 0.3f);
                Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, fadeProgress);
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
            }
            
            yield return null;
        }

        // Normal zamana dön
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        // Görsel efekti kaldır
        if (useVisualEffects)
            ApplySlowMotionVisuals(false);

        // SoundManager Stop Logic
        if (SoundManager.instance != null)
            SoundManager.instance.StopTimeSlowSequence();

        // Ses efekti (Eski kod iptal)
        /*if (deactivateSound != null)
            deactivateSound.Play();*/

        isSlowMotionActive = false;
        cooldownTimer = cooldownTime;
    }

    private void ApplySlowMotionVisuals(bool activate)
    {
        if (allSpriteRenderers == null) return;

        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            if (allSpriteRenderers[i] == null) continue;

            if (activate)
            {
                // Mavi ton uygula
                Color original = originalColors[i];
                allSpriteRenderers[i].color = new Color(
                    original.r * slowMotionTint.r,
                    original.g * slowMotionTint.g,
                    original.b * slowMotionTint.b,
                    original.a
                );
            }
            else
            {
                // Orijinal renge dön
                allSpriteRenderers[i].color = originalColors[i];
            }
        }
    }

    private void UpdateUI()
    {
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = CooldownProgress;
            
            // Hazır değilse gri yap
            cooldownFillImage.color = IsReady ? Color.white : Color.gray;
        }

        if (abilityIconImage != null)
        {
            // Aktifken parlat
            if (isSlowMotionActive)
                abilityIconImage.color = Color.cyan;
            else if (IsReady)
                abilityIconImage.color = Color.white;
            else
                abilityIconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
    }

    private void OnDestroy()
    {
        // Oyun bittiğinde zaman normal kalsın
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }

    private void OnDisable()
    {
        // Disable olduğunda da normal zaman
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            isSlowMotionActive = false;
        }
    }

    // Debug GUI
    private void OnGUI()
    {
        if (ScreenshotMode.IsHudHidden) return;

        GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 200, 80));
        GUILayout.Box("Time Slow Ability");
        
        if (isSlowMotionActive)
            GUILayout.Label("STATUS: ACTIVE");
        else if (IsReady)
            GUILayout.Label("STATUS: READY [E]");
        else
            GUILayout.Label($"COOLDOWN: {cooldownTimer:0.0}s");
        
        GUILayout.EndArea();
    }
}
