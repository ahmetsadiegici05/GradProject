using UnityEngine;
using System.Collections;

/// <summary>
/// Dünyayı ve yerçekimini 90 derece döndürür.
/// Singleton olarak çalışır - Scene'de bir tane olmalı.
/// </summary>
public class WorldRotationManager : MonoBehaviour
{
    public static WorldRotationManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform worldRoot;      // Tüm level geometrisi bunun altında
    [SerializeField] private Transform cameraRoot;     // Kamera bunun altında (veya kameranın kendisi)

    [Header("Rotation Settings")]
    [SerializeField] private float rotateDuration = 0.6f;
    [SerializeField] private bool useInspectorGravityMagnitude = false;
    [SerializeField] private float gravityMagnitude = 25f;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("What Rotates?")]
    [SerializeField] private bool rotateCamera = false;  // FALSE: Kamera dönmez, drift olmaz
    [SerializeField] private bool rotateWorld = false;
    [SerializeField] private bool rotateGravity = true;

    [Header("Effects")]
    [SerializeField] private bool useScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.05f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float minDegreesForShake = 5f;

    [Header("Transition Shake")]
    [SerializeField] private bool shakeOnRoomTransition = true;
    [SerializeField] private float transitionShakeIntensity = 0.03f;
    [SerializeField] private float transitionShakeDuration = 0.18f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Current rotation state
    private float currentRotationAngle = 0f;
    private bool isRotating = false;

    private Coroutine shakeCoroutine;

    public bool IsRotating => isRotating;
    public float CurrentAngle => currentRotationAngle;

    // Yerçekimi yönünü almak için static property
    public static Vector2 GravityDirection => Physics2D.gravity.normalized;
    public static Vector2 UpDirection => -GravityDirection;
    public static Vector2 RightDirection => new Vector2(-GravityDirection.y, GravityDirection.x);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Projedeki Physics2D.gravity değerini baz al (jump hissi bozulmasın).
        if (!useInspectorGravityMagnitude)
        {
            float sceneMag = Physics2D.gravity.magnitude;
            // Eğer gravity çok yamuk ise (eski session'dan kalmış), magnitude'u default al
            if (sceneMag < 0.001f || sceneMag > 100f)
                sceneMag = 9.81f;
            gravityMagnitude = sceneMag;
        }

        // FİZİK HATA DÜZELTME: Karakter düşmelerini önlemek için iterasyonları artır
        Physics2D.velocityIterations = 8;
        Physics2D.positionIterations = 8;

        // Checkpoint yoksa gravity'yi sıfırla (yeni oyun veya restart)
        if (!CheckpointData.HasCheckpoint)
        {
            Physics2D.gravity = Vector2.down * gravityMagnitude;
            currentRotationAngle = 0f;
            
            if (rotateCamera && cameraRoot != null)
                cameraRoot.rotation = Quaternion.identity;
        }
        // Eğer gravity sıfırsa (nadir), default aşağı yön ver.
        else if (rotateGravity && Physics2D.gravity.sqrMagnitude < 0.0001f)
        {
            Physics2D.gravity = Vector2.down * gravityMagnitude;
        }
    }

    private void Start()
    {
        // Otomatik referans bulma
        if (cameraRoot == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraRoot = mainCam.transform;
        }

        if (worldRoot == null && debugMode)
        {
            Debug.LogWarning("WorldRotationManager: WorldRoot atanmadı! Inspector'dan Level geometrisini içeren parent objeyi atayın.");
        }
    }

    /// <summary>
    /// Dünyayı belirtilen açı kadar döndürür (pozitif = saat yönünün tersi, negatif = saat yönü)
    /// </summary>
    public void RotateByAngle(float degrees)
    {
        if (isRotating) return;
        StartCoroutine(RotateRoutine(degrees));
    }

    private IEnumerator RotateRoutine(float deltaDegrees)
    {
        isRotating = true;

        // Başlangıç değerleri
        Quaternion cameraStartRot = cameraRoot != null ? cameraRoot.rotation : Quaternion.identity;
        Quaternion worldStartRot = worldRoot != null ? worldRoot.rotation : Quaternion.identity;

        Quaternion cameraEndRot = cameraStartRot * Quaternion.Euler(0, 0, deltaDegrees);
        Quaternion worldEndRot = worldStartRot * Quaternion.Euler(0, 0, deltaDegrees);

        Vector2 gravityStart = Physics2D.gravity;
        Vector2 gravityStartDir = gravityStart.sqrMagnitude < 0.0001f ? Vector2.down : gravityStart.normalized;
        Vector2 gravityEnd = RotateVector(gravityStartDir * gravityMagnitude, deltaDegrees);

        // Animasyon
        float elapsed = 0f;
        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);
            float curveT = rotationCurve.Evaluate(t);

            // Kamerayı döndür
            if (rotateCamera && cameraRoot != null)
                cameraRoot.rotation = Quaternion.Slerp(cameraStartRot, cameraEndRot, curveT);

            // World root'u döndür (level geometrisi)
            if (rotateWorld && worldRoot != null)
                worldRoot.rotation = Quaternion.Slerp(worldStartRot, worldEndRot, curveT);

            // Yerçekimini döndür
            if (rotateGravity)
                Physics2D.gravity = Vector2.Lerp(gravityStart, gravityEnd, curveT);

            yield return null;
        }

        // Final değerleri
        if (rotateCamera && cameraRoot != null)
            cameraRoot.rotation = cameraEndRot;

        if (rotateWorld && worldRoot != null)
            worldRoot.rotation = worldEndRot;

        if (rotateGravity)
            Physics2D.gravity = gravityEnd;
        currentRotationAngle = (currentRotationAngle + deltaDegrees) % 360f;

        // Rotasyon sonrası oyuncuyu platformdan çıkar (gömülme düzeltmesi)
        DepenetratePlayer();

        // Screen shake efekti (küçük açılarda kapalı)
        if (useScreenShake && cameraRoot != null && Mathf.Abs(deltaDegrees) >= minDegreesForShake)
            TriggerShakeFromCore(shakeIntensity, shakeDuration);

        isRotating = false;

        if (debugMode)
            Debug.Log($"World rotated by {deltaDegrees}°. Current angle: {currentRotationAngle}°, Gravity: {Physics2D.gravity}");
    }

    public void TriggerRoomTransitionShake()
    {
        if (!useScreenShake || !shakeOnRoomTransition || cameraRoot == null)
            return;

        TriggerShakeFromCore(transitionShakeIntensity, transitionShakeDuration);

        if (SoundManager.instance != null)
            SoundManager.instance.PlaySound(SoundManager.instance.earthquakeSound);
    }

    private void TriggerShakeFromCore(float intensity, float duration)
    {
        // Öncelikli olarak CameraController üzerinden shake yap (stabilite için)
        var camController = cameraRoot.GetComponent<CameraController>();
        if (camController != null)
        {
            camController.TriggerShake(intensity, duration);
        }
        else
        {
            // Fallback (eski yöntem, eğer CameraController yoksa)
            StartScreenShake(intensity, duration);
        }
    }

    private void StartScreenShake(float intensity, float duration)
    {
        if (cameraRoot == null)
            return;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ScreenShakeRoutine(intensity, duration));
    }

    private IEnumerator ScreenShakeRoutine(float intensity, float duration)
    {
        // NOT: CameraController her LateUpdate'de kameranın pozisyonunu güncelliyor.
        // Burada "başlangıç pozisyonunu" sabitleyip sonunda geri yazarsak drift/sıçrama olur.
        // Bunun yerine her framede sadece kendi offset'imizi uygulayıp geri alıyoruz.
        Vector3 lastOffset = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Önce bir önceki offset'i geri al (baz pozisyon CameraController tarafından yönetilsin)
            cameraRoot.localPosition -= lastOffset;

            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            lastOffset = new Vector3(x, y, 0);
            cameraRoot.localPosition += lastOffset;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Son offset'i temizle
        cameraRoot.localPosition -= lastOffset;
        shakeCoroutine = null;
    }

    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    /// <summary>
    /// Rotasyon sonrası oyuncunun platforma gömülmüşse yukarı çıkarır
    /// </summary>
    private void DepenetratePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        Collider2D playerCollider = playerObj.GetComponent<Collider2D>();
        if (playerRb == null || playerCollider == null) return;

        // Yerçekiminin tersi yönünde (yukarı) kontrol et
        Vector2 upDir = -Physics2D.gravity.normalized;
        if (upDir.sqrMagnitude < 0.001f) upDir = Vector2.up;

        // Oyuncunun collider'ının altındaki overlap'leri kontrol et
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(Physics2D.AllLayers & ~(1 << playerObj.layer)); // Oyuncunun kendi layer'ı hariç

        Collider2D[] results = new Collider2D[8];
        int overlapCount = playerCollider.Overlap(filter, results);

        if (overlapCount > 0)
        {
            // Gömülme var, oyuncuyu yukarı it
            float pushDistance = 0.15f; // Küçük bir miktar yukarı it
            
            // En fazla 5 iterasyon ile oyuncuyu çıkar
            for (int i = 0; i < 5; i++)
            {
                playerRb.position += upDir * pushDistance;
                
                // Fizik sistemini güncelle
                Physics2D.SyncTransforms();
                
                // Hala gömülü mü kontrol et
                overlapCount = playerCollider.Overlap(filter, results);
                if (overlapCount == 0)
                    break;
            }

            // Dikey hızı sıfırla (gömülmeden çıktıktan sonra ani düşmeyi önle)
            Vector2 vel = playerRb.linearVelocity;
            float horizontalSpeed = Vector2.Dot(vel, new Vector2(-upDir.y, upDir.x));
            playerRb.linearVelocity = new Vector2(-upDir.y, upDir.x) * horizontalSpeed;

            if (debugMode)
                Debug.Log($"Player depenetrated after rotation");
        }
    }

    /// <summary>
    /// Yerçekimini sıfırla (normal hale getir)
    /// </summary>
    public void ResetRotation()
    {
        if (isRotating) return;
        
        if (rotateGravity)
            Physics2D.gravity = Vector2.down * gravityMagnitude;
        currentRotationAngle = 0f;
        
        if (rotateCamera && cameraRoot != null)
            cameraRoot.rotation = Quaternion.identity;

        if (rotateWorld && worldRoot != null)
            worldRoot.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Rotasyonu anında ayarla (checkpoint/respawn için). Animasyon yok.
    /// absoluteAngleDegrees: Z rotasyonu (derece)
    /// </summary>
    public void SetRotationInstant(float absoluteAngleDegrees)
    {
        if (isRotating) return;

        currentRotationAngle = absoluteAngleDegrees % 360f;

        Quaternion rot = Quaternion.Euler(0f, 0f, currentRotationAngle);

        if (rotateCamera && cameraRoot != null)
            cameraRoot.rotation = rot;

        if (rotateWorld && worldRoot != null)
            worldRoot.rotation = rot;

        if (rotateGravity)
            Physics2D.gravity = RotateVector(Vector2.down * gravityMagnitude, currentRotationAngle);
    }

    // Debug için
    private void OnGUI()
    {
        if (ScreenshotMode.IsHudHidden) return;
        if (!debugMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label($"Rotation: {currentRotationAngle:F1}°");
        GUILayout.Label($"Gravity: {Physics2D.gravity}");
        
        if (GUILayout.Button("Reset"))
            ResetRotation();
        
        GUILayout.EndArea();
    }
}
