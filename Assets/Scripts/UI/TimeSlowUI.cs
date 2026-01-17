using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TimeSlowAbility için modern ve estetik UI göstergesi.
/// Ekranın sağ alt köşesinde minimal bir radial cooldown göstergesi.
/// </summary>
public class TimeSlowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cooldownRadialFill;    // Radial fill image (cooldown göstergesi)
    [SerializeField] private Image iconImage;              // Ability ikonu
    [SerializeField] private Image glowImage;              // Glow efekti (hazır olduğunda)
    [SerializeField] private TextMeshProUGUI keyText;      // E veya kalan saniye text
    [SerializeField] private CanvasGroup canvasGroup;      // Fade için

    [Header("Colors")]
    [SerializeField] private Color readyColor = new Color(0.3f, 0.8f, 1f, 1f);      // Açık mavi (hazır)
    [SerializeField] private Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Gri (cooldown)
    [SerializeField] private Color activeColor = new Color(1f, 1f, 0.5f, 1f);        // Sarı (aktif)

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 2f;        // Hazır olduğunda pulse hızı
    [SerializeField] private float pulseMinScale = 0.95f;
    [SerializeField] private float pulseMaxScale = 1.05f;
    [SerializeField] private float glowPulseSpeed = 3f;

    [Header("Settings")]
    [SerializeField] private bool hideWhenReady = false;   // Hazır olduğunda gizle?
    [SerializeField] private float fadeSpeed = 5f;

    private TimeSlowAbility timeSlowAbility;
    private RectTransform iconTransform;
    private float targetAlpha = 1f;
    private bool wasActive = false;

    private void Start()
    {
        timeSlowAbility = TimeSlowAbility.Instance;
        
        if (timeSlowAbility == null)
        {
            timeSlowAbility = FindFirstObjectByType<TimeSlowAbility>();
        }

        // UI elemanları atanmamışsa otomatik oluştur
        if (cooldownRadialFill == null)
        {
            CreateUIElements();
        }
        else
        {
            // Manuel atanmışsa keyText'i bulmaya çalış
            if (keyText == null)
            {
                keyText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (iconImage != null)
        {
            iconTransform = iconImage.GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Başlangıçta glow'u kapat
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// UI elemanlarını otomatik oluşturur
    /// </summary>
    private void CreateUIElements()
    {
        RectTransform containerRect = GetComponent<RectTransform>();
        if (containerRect == null)
        {
            containerRect = gameObject.AddComponent<RectTransform>();
        }
        
        // Sağ üst köşeye yerleştir
        containerRect.anchorMin = new Vector2(1, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(1, 1);
        containerRect.anchoredPosition = new Vector2(-30, -30);
        containerRect.sizeDelta = new Vector2(70, 70);

        // Background (koyu daire)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.6f);
        bgImage.sprite = GetCircleSprite();
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Glow (arka plan glow)
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform, false);
        glowImage = glowObj.AddComponent<Image>();
        glowImage.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        glowImage.sprite = GetCircleSprite();
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(-0.15f, -0.15f);
        glowRect.anchorMax = new Vector2(1.15f, 1.15f);
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;

        // Radial fill (cooldown)
        GameObject fillObj = new GameObject("CooldownFill");
        fillObj.transform.SetParent(transform, false);
        cooldownRadialFill = fillObj.AddComponent<Image>();
        cooldownRadialFill.color = readyColor;
        cooldownRadialFill.sprite = GetCircleSprite();
        cooldownRadialFill.type = Image.Type.Filled;
        cooldownRadialFill.fillMethod = Image.FillMethod.Radial360;
        cooldownRadialFill.fillOrigin = (int)Image.Origin360.Top;
        cooldownRadialFill.fillClockwise = true;
        cooldownRadialFill.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.1f, 0.1f);
        fillRect.anchorMax = new Vector2(0.9f, 0.9f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // İkon (iç daire)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(transform, false);
        iconImage = iconObj.AddComponent<Image>();
        iconImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        iconImage.sprite = GetCircleSprite();
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.25f, 0.25f);
        iconRect.anchorMax = new Vector2(0.75f, 0.75f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconTransform = iconRect;

        // E harfi veya kalan saniye için TextMeshPro
        GameObject textObj = new GameObject("KeyText");
        textObj.transform.SetParent(transform, false);
        keyText = textObj.AddComponent<TextMeshProUGUI>();
        keyText.text = "E";
        keyText.fontSize = 28;
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = readyColor;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Debug.Log("TimeSlowUI: UI elemanları otomatik oluşturuldu!");
    }

    private void Update()
    {
        // Pause, Game Over veya Main Menu'de gizle
        bool shouldHide = false;
        
        if (UIManager.Instance != null)
        {
            shouldHide = UIManager.Instance.IsPaused || UIManager.Instance.IsGameOver;
        }
        
        // Main Menu sahnesinde de gizle
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName.ToLower().Contains("menu") || sceneName.ToLower().Contains("main"))
        {
            shouldHide = true;
        }
        
        // CanvasGroup ile gizle/göster
        if (canvasGroup != null)
        {
            canvasGroup.alpha = shouldHide ? 0f : targetAlpha;
            canvasGroup.blocksRaycasts = !shouldHide;
        }
        
        if (shouldHide) return;
        
        if (timeSlowAbility == null)
        {
            // Tekrar bulmaya çalış
            timeSlowAbility = TimeSlowAbility.Instance;
            if (timeSlowAbility == null)
            {
                timeSlowAbility = FindFirstObjectByType<TimeSlowAbility>();
            }
            if (timeSlowAbility == null) return;
        }

        UpdateUI();
        UpdateAnimations();
        UpdateFade();
    }

    private void UpdateUI()
    {
        bool isActive = timeSlowAbility.IsSlowMotionActive;
        bool isReady = timeSlowAbility.IsReady;
        float progress = timeSlowAbility.CooldownProgress;
        float cooldownRemaining = timeSlowAbility.CooldownRemaining;

        // KeyText güncelle - her zaman, null değilse
        if (keyText != null)
        {
            if (isActive)
            {
                keyText.text = "E";
                keyText.color = activeColor;
                keyText.fontSize = 28;
            }
            else if (isReady)
            {
                keyText.text = "E";
                keyText.color = readyColor;
                keyText.fontSize = 28;
            }
            else
            {
                // Cooldown - kalan saniyeyi göster
                int seconds = Mathf.CeilToInt(cooldownRemaining);
                keyText.text = seconds.ToString();
                keyText.color = Color.white; // Daha görünür olsun
                keyText.fontSize = 26;
            }
        }

        // Radial fill güncelle
        if (cooldownRadialFill != null)
        {
            cooldownRadialFill.fillAmount = progress;

            // Renk ayarla
            if (isActive)
            {
                cooldownRadialFill.color = activeColor;
            }
            else if (isReady)
            {
                cooldownRadialFill.color = readyColor;
            }
            else
            {
                cooldownRadialFill.color = cooldownColor;
            }
        }

        // İkon rengi
        if (iconImage != null)
        {
            if (isActive)
            {
                iconImage.color = activeColor;
            }
            else if (isReady)
            {
                iconImage.color = readyColor;
            }
            else
            {
                iconImage.color = cooldownColor;
            }
        }

        // Glow efekti (hazır olduğunda)
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(isReady && !isActive);
            if (isReady && !isActive)
            {
                glowImage.color = new Color(readyColor.r, readyColor.g, readyColor.b, 
                    0.3f + 0.2f * Mathf.Sin(Time.unscaledTime * glowPulseSpeed));
            }
        }

        // Aktiflik değiştiğinde efekt
        if (isActive && !wasActive)
        {
            // Aktif oldu - scale punch
            if (iconTransform != null)
            {
                StartCoroutine(ScalePunch(1.3f, 0.15f));
            }
        }
        wasActive = isActive;

        // Görünürlük
        if (hideWhenReady)
        {
            targetAlpha = isReady ? 0.3f : 1f;
        }
        else
        {
            targetAlpha = 1f;
        }
    }

    private void UpdateAnimations()
    {
        if (timeSlowAbility == null || iconTransform == null) return;

        // Hazır olduğunda hafif pulse
        if (timeSlowAbility.IsReady && !timeSlowAbility.IsSlowMotionActive)
        {
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);
            iconTransform.localScale = Vector3.one * pulse;
        }
        else
        {
            // Normal scale'e dön
            iconTransform.localScale = Vector3.Lerp(iconTransform.localScale, Vector3.one, 
                Time.unscaledDeltaTime * 10f);
        }
    }

    private void UpdateFade()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
    }

    private System.Collections.IEnumerator ScalePunch(float punchScale, float duration)
    {
        if (iconTransform == null) yield break;

        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(punchScale, 1f, t * t); // Ease out
            iconTransform.localScale = originalScale * scale;
            yield return null;
        }

        iconTransform.localScale = originalScale;
    }

    /// <summary>
    /// UI'ı kod ile oluşturmak için yardımcı metod.
    /// Canvas altında çağrılmalı.
    /// </summary>
    public static TimeSlowUI CreateUI(Transform canvasTransform)
    {
        // Ana container
        GameObject container = new GameObject("TimeSlowUI");
        container.transform.SetParent(canvasTransform, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1, 0);  // Sağ alt
        containerRect.anchorMax = new Vector2(1, 0);
        containerRect.pivot = new Vector2(1, 0);
        containerRect.anchoredPosition = new Vector2(-20, 20);
        containerRect.sizeDelta = new Vector2(60, 60);

        TimeSlowUI ui = container.AddComponent<TimeSlowUI>();

        // Background (koyu daire)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(container.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        bgImage.sprite = GetCircleSprite();
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Glow (arka plan glow)
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(container.transform, false);
        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.color = new Color(0.3f, 0.8f, 1f, 0.3f);
        glowImage.sprite = GetCircleSprite();
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(-0.2f, -0.2f);
        glowRect.anchorMax = new Vector2(1.2f, 1.2f);
        glowRect.sizeDelta = Vector2.zero;
        ui.glowImage = glowImage;

        // Radial fill (cooldown)
        GameObject fillObj = new GameObject("CooldownFill");
        fillObj.transform.SetParent(container.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.8f, 1f, 1f);
        fillImage.sprite = GetCircleSprite();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.1f, 0.1f);
        fillRect.anchorMax = new Vector2(0.9f, 0.9f);
        fillRect.sizeDelta = Vector2.zero;
        ui.cooldownRadialFill = fillImage;

        // İkon (E harfi veya saat ikonu)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(container.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = new Color(0.3f, 0.8f, 1f, 1f);
        // Basit bir clock/hourglass ikonu yerine E harfi kullanalım
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.2f);
        iconRect.anchorMax = new Vector2(0.8f, 0.8f);
        iconRect.sizeDelta = Vector2.zero;
        ui.iconImage = iconImage;

        // E harfi için TextMeshPro
        GameObject textObj = new GameObject("KeyText");
        textObj.transform.SetParent(container.transform, false);
        TextMeshProUGUI keyText = textObj.AddComponent<TextMeshProUGUI>();
        keyText.text = "E";
        keyText.fontSize = 24;
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return ui;
    }

    private static Sprite GetCircleSprite()
    {
        // Unity'nin built-in Knob sprite'ını kullan
        return UnityEngine.Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }
}
