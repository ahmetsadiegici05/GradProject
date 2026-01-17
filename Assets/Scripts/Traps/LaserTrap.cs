using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class LaserTrap : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private Transform firePoint;     // Lazerin çıkacağı nokta (Lazer başlığı)
    [SerializeField] private float range = 10f;       // Lazerin uzunluğu
    [SerializeField] private float width = 0.05f;     // Lazer kalınlığı
    [SerializeField] private LayerMask obstacleLayer; // Lazerin çarpıp duracağı duvarlar
    [SerializeField] private LayerMask playerLayer;   // Oyuncuyu algılayacağı layer

    [Header("Timing")]
    [SerializeField] private bool isAlwaysOn = false; // Sürekli açık mı kalsın?
    [SerializeField] private float activeDuration = 2f; // Ne kadar süre ateş etsin
    [SerializeField] private float cooldownDuration = 1.5f; // Ne kadar süre kapalı kalsın

    [Header("Visuals")]
    [SerializeField] private Color activeColor = Color.red;     // Lazer dış rengi
    [SerializeField] private Color coreColor = Color.white;     // Lazer iç çekirdek rengi (neon efekti)
    [SerializeField] private Material laserMaterial;            // Lazer materyali
    [SerializeField] private bool useNeonEffect = true;         // Neon efekti açık mı?
    [SerializeField] [Range(0.1f, 0.5f)] private float coreWidthRatio = 0.3f; // İç çekirdeğin dışa oranı

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f; // Aynı hedefe tekrar hasar verme süresi

    [Header("World Integration")]
    [Tooltip("Dönen dünya objesi (LevelRoot). Atanmazsa otomatik bulunur.")]
    [SerializeField] private Transform worldRoot;

    private LineRenderer lineRenderer;      // Dış glow
    private LineRenderer coreLineRenderer;  // İç çekirdek (neon efekti)
    private bool isFiring = false;
    private Vector3 endPoint;
    private Dictionary<int, float> lastDamageTime = new Dictionary<int, float>();
    private Vector3 localFirePointOffset; // FirePoint'in LaserTrap'e göre local pozisyonu

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        
        // Additive shader kullan (neon parlama efekti için)
        Material glowMat;
        if (laserMaterial != null)
        {
            glowMat = laserMaterial;
        }
        else
        {
            // Additive shader bul (yoksa Sprites/Default kullan)
            Shader addShader = Shader.Find("Sprites/Default");
            // Particles/Additive veya Legacy Shaders/Particles/Additive daha iyi görünür
            Shader particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader == null)
                particleShader = Shader.Find("Legacy Shaders/Particles/Additive");
            
            if (particleShader != null)
            {
                glowMat = new Material(particleShader);
            }
            else if (addShader != null)
            {
                glowMat = new Material(addShader);
            }
            else
            {
                glowMat = new Material(Shader.Find("Sprites/Default"));
            }
        }
        
        lineRenderer.material = glowMat;
        lineRenderer.startWidth = 0f;
        lineRenderer.endWidth = 0f;
        lineRenderer.sortingOrder = 10; // Önde görünsün
        
        // Neon efekti için iç çekirdek LineRenderer oluştur
        if (useNeonEffect)
        {
            GameObject coreObj = new GameObject("LaserCore");
            coreObj.transform.SetParent(transform);
            coreObj.transform.localPosition = Vector3.zero;
            
            coreLineRenderer = coreObj.AddComponent<LineRenderer>();
            coreLineRenderer.useWorldSpace = true;
            coreLineRenderer.material = new Material(glowMat);
            coreLineRenderer.startWidth = 0f;
            coreLineRenderer.endWidth = 0f;
            coreLineRenderer.sortingOrder = 11; // Dışın önünde
        }
        
        // WorldRoot'u otomatik bul (atanmadıysa)
        if (worldRoot == null)
        {
            // LevelRoot isimli objeyi ara
            GameObject levelRootObj = GameObject.Find("LevelRoot");
            if (levelRootObj != null)
            {
                worldRoot = levelRootObj.transform;
            }
        }
    }

    private void Start()
    {
        // LaserTrap'i WorldRoot'un child'ı yap (dünya dönerken birlikte dönsün)
        if (worldRoot != null && transform.parent != worldRoot)
        {
            // Mevcut world pozisyonunu koru
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;
            
            transform.SetParent(worldRoot);
            
            // Pozisyon ve rotasyonu geri yükle
            transform.position = worldPos;
            transform.rotation = worldRot;
        }
        
        // FirePoint ayarları
        if (firePoint != null)
        {
            // FirePoint'in local offset'ini kaydet
            localFirePointOffset = firePoint.position - transform.position;
            
            // FirePoint'in Rigidbody2D'si varsa devre dışı bırak (düşmesin)
            Rigidbody2D firePointRb = firePoint.GetComponent<Rigidbody2D>();
            if (firePointRb != null)
            {
                Destroy(firePointRb); // Tamamen kaldır
            }
            
            // Collider varsa kaldır
            Collider2D firePointCollider = firePoint.GetComponent<Collider2D>();
            if (firePointCollider != null)
            {
                Destroy(firePointCollider);
            }
            
            // FirePoint'i LaserTrap'in child'ı yap
            if (firePoint.parent != transform)
            {
                Vector3 fpWorldPos = firePoint.position;
                firePoint.SetParent(transform);
                firePoint.position = fpWorldPos;
            }
        }
        
        StartCoroutine(LaserLoop());
    }

    private void Update()
    {
        // Lazer çizimi her frame güncellenmeli (çünkü dünya/duvarlar hareket edebilir)
        if (isFiring || lineRenderer.enabled)
        {
            UpdateLaserBeam();
        }

        // Eğer lazer ateş ediyorsa hasar kontrolü yap
        if (isFiring)
        {
            CheckDamage();
        }
    }

    private IEnumerator LaserLoop()
    {
        while (true)
        {
            if (isAlwaysOn)
            {
                isFiring = true;
                EnableLaser(true);
                yield break; // Döngüden çık, hep açık kal
            }

            // 1. KAPALI (Cooldown) - Lazer görünmez
            isFiring = false;
            EnableLaser(false);
            yield return new WaitForSeconds(cooldownDuration);

            // 2. ATEŞ (Kırmızı lazer)
            isFiring = true;
            EnableLaser(true);

            // Her yeni ateşlemede hasar listesini temizle
            lastDamageTime.Clear();

            // Ses efekti çalınabilir (SoundManager.Instance.PlaySound...)

            float fireTimer = 0f;
            while (fireTimer < activeDuration)
            {
                fireTimer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void UpdateLaserBeam()
    {
        // Başlangıç noktası: FirePoint varsa oradan, yoksa objenin kendisinden
        Vector3 startPos = (firePoint != null) ? firePoint.position : transform.position;
        lineRenderer.SetPosition(0, startPos);

        // Ateş yönü: FirePoint varsa onun aşağı yönü, yoksa objenin aşağı yönü
        Vector2 direction = (firePoint != null) ? -firePoint.up : -transform.up; 
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, range, obstacleLayer);

        if (hit.collider != null)
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = startPos + (Vector3)(direction * range);
        }

        lineRenderer.SetPosition(1, endPoint);
        
        // Core line renderer'ı da güncelle (neon efekti)
        if (useNeonEffect && coreLineRenderer != null && coreLineRenderer.enabled)
        {
            coreLineRenderer.SetPosition(0, startPos);
            coreLineRenderer.SetPosition(1, endPoint);
        }
    }

    private void CheckDamage()
    {
        // Lazer çizgisi üzerinde oyuncu var mı?
        Vector3 startPos = (firePoint != null) ? firePoint.position : transform.position;
        
        Vector2 direction = (endPoint - startPos).normalized;
        float distance = Vector2.Distance(startPos, endPoint);

        // Raycast all to hit player even if overlapping with something else (but obstacles block it anyway)
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, distance, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                Health playerHealth = hit.collider.GetComponent<Health>();
                if (playerHealth != null)
                {
                    int id = hit.collider.gameObject.GetInstanceID();
                    
                    // Eğer bu objeye daha önce vurmadıysak veya süresi dolduysa
                    if (!lastDamageTime.ContainsKey(id) || Time.time >= lastDamageTime[id])
                    {
                        playerHealth.TakeDamage(damage);
                        lastDamageTime[id] = Time.time + damageInterval;
                    }
                }
            }
        }
    }
    
    // Editörde lazerin yönünü ve menzilini görmek için
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 startPos = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 dir = (firePoint != null) ? -firePoint.up : -transform.up;
        Gizmos.DrawLine(startPos, startPos + dir * range);
    }
    
    /// <summary>
    /// Lazeri aç/kapat ve neon efektini uygula
    /// </summary>
    private void EnableLaser(bool enable)
    {
        lineRenderer.enabled = enable;
        
        if (enable)
        {
            // Dış glow - daha geniş ve renkli
            lineRenderer.startColor = activeColor;
            lineRenderer.endColor = activeColor;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            
            // İç çekirdek - ince ve parlak beyaz (neon efekti)
            if (useNeonEffect && coreLineRenderer != null)
            {
                coreLineRenderer.enabled = true;
                coreLineRenderer.startColor = coreColor;
                coreLineRenderer.endColor = coreColor;
                coreLineRenderer.startWidth = width * coreWidthRatio;
                coreLineRenderer.endWidth = width * coreWidthRatio;
            }
        }
        else
        {
            if (coreLineRenderer != null)
            {
                coreLineRenderer.enabled = false;
            }
        }
    }
}