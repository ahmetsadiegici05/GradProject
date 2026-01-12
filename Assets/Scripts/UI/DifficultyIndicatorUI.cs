using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Eğim/zorluk durumunu gösteren UI elementi.
/// "Stability" veya "Danger Level" olarak kullanılabilir.
/// </summary>
public class DifficultyIndicatorUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image fillBar;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Settings")]
    [SerializeField] private string labelFormat = "STABILITY";
    [SerializeField] private bool invertDisplay = true; // true = eğim arttıkça bar azalır

    [Header("Colors")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningThreshold = 0.6f;
    [SerializeField] private float dangerThreshold = 0.3f;

    [Header("Animation")]
    [SerializeField] private bool pulseOnDanger = true;
    [SerializeField] private float pulseSpeed = 2f;

    private float displayValue = 1f;
    private float targetValue = 1f;

    private void Start()
    {
        if (labelText != null)
            labelText.text = labelFormat;
    }

    private void Update()
    {
        if (ProgressionManager.Instance == null) return;

        // Mevcut eğimi al ve 0-1 arasına normalize et
        float currentTilt = Mathf.Abs(ProgressionManager.Instance.CurrentTiltDegrees);
        float maxTilt = 45f; // Inspector'dan da alınabilir
        
        float tiltRatio = Mathf.Clamp01(currentTilt / maxTilt);
        
        // Stability olarak göster (eğim arttıkça stability düşer)
        targetValue = invertDisplay ? (1f - tiltRatio) : tiltRatio;

        // Smooth geçiş
        displayValue = Mathf.Lerp(displayValue, targetValue, Time.deltaTime * 3f);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (fillBar != null)
        {
            fillBar.fillAmount = displayValue;

            // Renk değişimi
            Color targetColor;
            if (displayValue > warningThreshold)
                targetColor = safeColor;
            else if (displayValue > dangerThreshold)
                targetColor = Color.Lerp(warningColor, safeColor, (displayValue - dangerThreshold) / (warningThreshold - dangerThreshold));
            else
                targetColor = Color.Lerp(dangerColor, warningColor, displayValue / dangerThreshold);

            // Tehlike durumunda pulse efekti
            if (pulseOnDanger && displayValue <= dangerThreshold)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) / 2f;
                targetColor = Color.Lerp(targetColor, dangerColor, pulse * 0.5f);
            }

            fillBar.color = targetColor;
        }

        if (percentageText != null)
        {
            int percentage = Mathf.RoundToInt(displayValue * 100f);
            percentageText.text = $"{percentage}%";
            
            // Düşük değerlerde kırmızı
            if (displayValue <= dangerThreshold)
                percentageText.color = dangerColor;
            else if (displayValue <= warningThreshold)
                percentageText.color = warningColor;
            else
                percentageText.color = Color.white;
        }
    }
}
