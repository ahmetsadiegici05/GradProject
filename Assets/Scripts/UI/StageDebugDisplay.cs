using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StageDebugDisplay : MonoBehaviour
{
    public static StageDebugDisplay Instance { get; private set; }

    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color debugTextColor = new Color(0.2f, 1f, 1f, 1f); // Cyan
    [SerializeField] private int debugFontSize = 24;

    [Header("Announcement")]
    [SerializeField] private bool showAnnouncement = true;
    [SerializeField] private float announcementDuration = 2.5f;
    [SerializeField] private Color announcementColor = Color.red; // DEPREM (Kirmizi)
    [SerializeField] private int announcementFontSize = 80;

    private Text debugText;
    private Text announcementText;
    
    // UI References
    private GameObject canvasObj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null && FindObjectOfType<StageDebugDisplay>() == null)
        {
            // Debug.Log("StageDebugDisplay: Auto-creating UI...");
            GameObject obj = new GameObject("StageDebugUI_Auto");
            obj.AddComponent<StageDebugDisplay>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (gameObject.name.Contains("_Auto"))
                DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        CreateCanvasAndText();
    }

    private void CreateCanvasAndText()
    {
        // Güvenli Font Yükleme
        Font uiFont = null;
        
        // 1. Once LegacyRuntime.ttf dene (Unity 2022+ icin)
        try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
        
        // 2. Olmazsa Arial.ttf dene (Eski surumler icin)
        if (uiFont == null) 
            try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch {}

        // 3. O da olmazsa OS fontu kullan
        if (uiFont == null) 
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 24);

        if (uiFont == null)
        {
            Debug.LogError("StageDebugDisplay: Font could not be loaded!");
            return;
        }

        // 1. Create Canvas
        canvasObj = new GameObject("DebugCanvas");
        canvasObj.transform.SetParent(this.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; 
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Create Debug Text (Sol Ust Kose)
        GameObject debugObj = new GameObject("DebugText");
        debugObj.transform.SetParent(canvasObj.transform, false);
        debugText = debugObj.AddComponent<Text>();
        debugText.font = uiFont;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.color = debugTextColor;
        debugText.fontSize = debugFontSize;
        debugText.horizontalOverflow = HorizontalWrapMode.Overflow;
        debugText.verticalOverflow = VerticalWrapMode.Overflow;
        debugText.raycastTarget = false; // Tıklamayı engellemesin
        
        // Golge ekle
        debugObj.AddComponent<Outline>().effectColor = new Color(0,0,0,0.8f);

        RectTransform debugRect = debugText.GetComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0, 1);
        debugRect.anchorMax = new Vector2(0, 1);
        debugRect.pivot = new Vector2(0, 1);
        debugRect.anchoredPosition = new Vector2(30, -250); // -100 du, kalplerin altina inmesi icin -250 yaptik
        debugRect.sizeDelta = new Vector2(500, 300);

        // 3. Create Announcement Text (Orta)
        GameObject announceObj = new GameObject("AnnouncementText");
        announceObj.transform.SetParent(canvasObj.transform, false);
        announcementText = announceObj.AddComponent<Text>();
        announcementText.font = uiFont;
        announcementText.alignment = TextAnchor.MiddleCenter;
        announcementText.color = announcementColor;
        announcementText.fontSize = announcementFontSize;
        announcementText.horizontalOverflow = HorizontalWrapMode.Overflow;
        announcementText.verticalOverflow = VerticalWrapMode.Overflow;
        announcementText.raycastTarget = false;
        
        // Golge ekle
        announceObj.AddComponent<Outline>().effectColor = Color.black;
        Shadow shadow = announceObj.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(5, -5);
        
        RectTransform announceRect = announcementText.GetComponent<RectTransform>();
        announceRect.anchorMin = new Vector2(0.5f, 0.5f);
        announceRect.anchorMax = new Vector2(0.5f, 0.5f);
        announceRect.pivot = new Vector2(0.5f, 0.5f);
        announceRect.anchoredPosition = new Vector2(0, 200); 
        announceRect.sizeDelta = new Vector2(1000, 300);
        
        // Baslangicta gizle
        announcementText.canvasRenderer.SetAlpha(0f);
        
        // Sahne kontrolu (MainMenu'de gizle)
        CheckSceneVisibility();
    }
    
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CheckSceneVisibility();
    }

    private void CheckSceneVisibility()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // Eger MainMenu ise gizle, degilse goster
        if (sceneName == "MainMenu")
        {
            SetVisible(false);
        }
        else
        {
            SetVisible(true);
        }
    }
    
    // Disaridan gorunurluk kontrolu
    public void SetVisible(bool visible)
    {
        if (canvasObj != null)
            canvasObj.SetActive(visible);
    }
    
    // UI Reset (Game Over durumunda cagrilabilir)
    public void ResetUI()
    {
        // Direkt gizle
        SetVisible(false);
        if (debugText != null) debugText.text = "";
    }

    private void Update()
    {
        if (showDebugInfo && debugText != null && ProgressionManager.Instance != null)
        {
            var pm = ProgressionManager.Instance;
            // Wave 0'dan basliyor, kullanici 1'den baslayan Difficulty gormek istiyor
            int difficultyLevel = pm.CurrentWave + 1; 

            string info = $"STAGE: {pm.StageIndex}\n" +
                          $"DIFFICULTY: {difficultyLevel}\n" +
                          $"SPEED MULT: x{pm.TrapSpeedMultiplier:F2}\n" +
                          $"TILT: {pm.CurrentTiltDegrees:F1}°";
            
            debugText.text = info;
        }
    }

    public void TriggerStageChangeEffect(int newStage)
    {
        if (!showAnnouncement || announcementText == null) return;

        StopAllCoroutines();
        StartCoroutine(AnimateAnnouncement(newStage));
    }
    
    // --- YENI: Isimle cagirmak icin (deprem ozel)
    public void ShowAnnouncement(string text)
    {
        if (!showAnnouncement || announcementText == null) return;

        StopAllCoroutines();
        StartCoroutine(AnimateCustomAnnouncement(text));
    }

    private IEnumerator AnimateCustomAnnouncement(string text)
    {
        announcementText.text = text;
        announcementText.color = Color.white; // Baslangic FLASH efekti icin beyaz
        announcementText.canvasRenderer.SetAlpha(1f); // Aninda gorunur

        Vector2 originalPos = new Vector2(0, 200);
        announcementText.GetComponent<RectTransform>().anchoredPosition = originalPos;

        // --- 1. SLAM EFFECT (IMPACT - Hizli Vurus) ---
        // Yazi ekrana "gum" diye vursun (3x buyuklukten 1x'e)
        float slamDuration = 0.15f;
        float timer = 0f;
        
        while(timer < slamDuration)
        {
            timer += Time.deltaTime;
            float t = timer / slamDuration;
            
            // Scale: 4 -> 1 hizli inis (Non-lineer)
            float scale = Mathf.Lerp(4.0f, 1.0f, t * t); 
            announcementText.transform.localScale = Vector3.one * scale;
            
            // Renk gecisi: Beyaz -> Kirmizi
            announcementText.color = Color.Lerp(Color.white, announcementColor, t);
            
            yield return null;
        }
        
        announcementText.transform.localScale = Vector3.one;
        announcementText.color = announcementColor; // Tam kirmizi

        // --- 2. AGGRESSIVE SHAKE (Siddetli Titresim) ---
        float waitTimer = 0f;
        
        while (waitTimer < announcementDuration)
        {
            waitTimer += Time.deltaTime;
            
            // Titresim siddeti artirildi (5f -> 15f)
            float xShake = Random.Range(-15f, 15f);
            float yShake = Random.Range(-15f, 15f);
            announcementText.GetComponent<RectTransform>().anchoredPosition = originalPos + new Vector2(xShake, yShake);
            
            yield return null;
        }
        
        announcementText.GetComponent<RectTransform>().anchoredPosition = originalPos;

        // --- 3. FADE OUT ---
        float durationOut = 0.5f;
        timer = 0f;
        while(timer < durationOut)
        {
            timer += Time.deltaTime;
            float alpha = 1f - (timer / durationOut);
            announcementText.canvasRenderer.SetAlpha(alpha);
            
            // Yukari dogru ucarken kaybolsun
            announcementText.transform.localPosition += Vector3.up * (Time.deltaTime * 100f);
            yield return null;
        }

        announcementText.canvasRenderer.SetAlpha(0f);
        announcementText.GetComponent<RectTransform>().anchoredPosition = originalPos;
    }

    private IEnumerator AnimateAnnouncement(int stage)
    {
        announcementText.text = "EARTHQUAKE";
        // announcementColor inspector'dan geliyor (Kirmizi)
        announcementText.color = announcementColor; 
        
        // Fade In
        float durationIn = 0.2f;
        float timer = 0f;
        while(timer < durationIn)
        {
            timer += Time.deltaTime;
            float alpha = timer / durationIn;
            announcementText.canvasRenderer.SetAlpha(alpha);
            
            // Hafif scale efekti (Buyukten kucuge otursun)
            float scale = Mathf.Lerp(1.5f, 1.0f, alpha);
            announcementText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        announcementText.transform.localScale = Vector3.one;
        announcementText.canvasRenderer.SetAlpha(1f);

        // Bekle
        yield return new WaitForSeconds(announcementDuration);

        // Fade Out
        float durationOut = 0.5f;
        timer = 0f;
        while(timer < durationOut)
        {
            timer += Time.deltaTime;
            float alpha = 1f - (timer / durationOut);
            announcementText.canvasRenderer.SetAlpha(alpha);
            
            // Yukari dogru ucarken kaybolsun
            announcementText.transform.localPosition += Vector3.up * (Time.deltaTime * 50f);
            yield return null;
        }

        announcementText.canvasRenderer.SetAlpha(0f);
        // Pozisyonu resetle
        announcementText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
    }
}
