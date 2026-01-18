using UnityEngine;
using System.Collections;

public class RockSpawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private int maxSpawnCount = 3; // Kac tane tas dussun? (0 = Sonsuz)
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float spawnWidth = 10f;
    [SerializeField] private float spawnHeight = 20f; // Player'in ne kadar yukarisindan?
    [SerializeField] private bool checkCeiling = false; // Tavana carpinca dursun mu? (Mid-air spawn sorunu icin false yapin)

    [Header("Activation")]
    [SerializeField] private bool autoStart = false; // Otomatik baslat
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask ceilingLayer;
    [SerializeField] private bool oneTimeUse = true;

    private bool isActive = false;
    private Coroutine spawnCoroutine;
    private bool hasTriggeredThisStay = false;
    private bool hasEverTriggered = false;

    private void Start()
    {
        // Eger ceilingLayer atanmamissa default olarak Ground ve Default yap
        if (ceilingLayer == 0)
        {
            ceilingLayer = LayerMask.GetMask("Ground", "Default");
            if (ceilingLayer == 0)
                ceilingLayer = ~playerLayer;
        }
        
        // Otomatik baslat
        if (autoStart)
        {
            Debug.Log("RockSpawner: Auto-starting...");
            isActive = true;
            spawnCoroutine = StartCoroutine(SpawnRoutine());
            hasTriggeredThisStay = true;
            hasEverTriggered = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player layer kontrolu
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            if (!isActive && !hasTriggeredThisStay && !hasEverTriggered)
            {
                isActive = true;
                spawnCoroutine = StartCoroutine(SpawnRoutine());
                hasTriggeredThisStay = true;
                hasEverTriggered = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (oneTimeUse) return;

        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            isActive = false;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            if (!oneTimeUse)
            {
                hasTriggeredThisStay = false;
            }
        }
    }

    private IEnumerator SpawnRoutine()
    {
        Debug.Log($"RockSpawner: Spawn routine started. Interval: {spawnInterval}s");
        int count = 0;
        while (isActive)
        {
            // Max sayiya ulasildiysa dur
            if (maxSpawnCount > 0 && count >= maxSpawnCount)
            {
                isActive = false;
                break;
            }

            SpawnRock();
            count++;
            yield return new WaitForSeconds(spawnInterval);
        }
        spawnCoroutine = null;
        Debug.Log("RockSpawner: Spawn routine stopped.");
    }

    private void SpawnRock()
    {
        if (rockPrefab == null)
        {
            Debug.LogError("RockSpawner: rockPrefab is NULL! Please assign a rock prefab in Inspector.");
            return;
        }

        // Yercekimi yonune gore "yukari"yi bul
        Vector2 upDir = Vector2.up;
        if (WorldRotationManager.Instance != null)
        {
            upDir = WorldRotationManager.UpDirection;
        }
        else if (Physics2D.gravity.sqrMagnitude > 0.01f)
        {
            upDir = -Physics2D.gravity.normalized;
        }

        Vector2 rightDir = new Vector2(upDir.y, -upDir.x); 
        Vector3 center = transform.position;
        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        
        // --- SPAWN HEIGHT FIX ---
        float heightToUse = spawnHeight;
        // Eger inspector'da 20'den kucukse (eski prefab degeri), koddan 20 yapalim
        if (heightToUse < 18f) heightToUse = 22f; 

        Vector3 rayOrigin = center + (Vector3)(rightDir * randomX);
        
        // Ceiling check sadece checkCeiling=true ise yuksekligi kussun
        if (checkCeiling)
        {
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, upDir, heightToUse, ceilingLayer); 
            if (hit.collider != null)
            {
                heightToUse = hit.distance - 1.0f;
                if (heightToUse < 1f) heightToUse = 1f;
            }
        }

        Vector3 spawnPos = rayOrigin + (Vector3)(upDir * heightToUse);

        GameObject rock = Instantiate(rockPrefab, spawnPos, Quaternion.identity);
        
        // YENI YONTEM: Dogdugu noktadaki TUM engellerle carpismayi kapat (Player ve Ground haric)
        Collider2D rockCollider = rock.GetComponent<Collider2D>();
        FallingRock rockScript = rock.GetComponent<FallingRock>();
        
        if (rockCollider != null && rockScript != null)
        {
            rockScript.ConfigureCeilingLayer(ceilingLayer);
            rockScript.ConfigureTriggerUntilClear(true);

            // Dogdugu noktada 2 birim yari capindaki her seyi bul
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(spawnPos, 2f);
            foreach (var col in hitColliders)
            {
                // Kendi kendine veya Player/Zemin ile carpismayi kapatma
                if (col == rockCollider) continue;
                if (((1 << col.gameObject.layer) & playerLayer) != 0) continue;
                if (col.CompareTag("Player")) continue;
                if (col.gameObject.layer == LayerMask.NameToLayer("Ground")) continue; // Ground layer kontrolu

                // Diger her seyle (duvar, tavan, kamera cercevesi) carpismayi yoksay
                // ozellikle tavana takilmayi engeller
                Physics2D.IgnoreCollision(rockCollider, col, true);
            }
        }

        Debug.Log($"RockSpawner: Spawned rock at {spawnPos}, UpDir: {upDir}, Height: {heightToUse}");
    }

    public void ForceStartSpawning()
    {
        if (hasEverTriggered && oneTimeUse) return; // Tek kullanimliksa ve kullanildiysa tekrar calisma
        if (isActive) return; // Zaten calisiyorsa tekrar baslatma

        Debug.Log("RockSpawner: Force started by external event.");
        isActive = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
        hasEverTriggered = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        
        Vector2 upDir = Vector2.up; 
        if (Application.isPlaying && WorldRotationManager.Instance != null)
             upDir = WorldRotationManager.UpDirection;
             
        Vector2 rightDir = new Vector2(upDir.y, -upDir.x);

        Vector3 center = transform.position;
        Vector3 topCenter = center + (Vector3)(upDir * spawnHeight);
        
        // Yukseklik cizgisi
        Gizmos.DrawLine(center, topCenter);
        Gizmos.DrawWireSphere(topCenter, 0.5f);
        
        // Genislik cizgisi (spawn alani)
        Vector3 p1 = topCenter + (Vector3)(rightDir * spawnWidth / 2f);
        Vector3 p2 = topCenter - (Vector3)(rightDir * spawnWidth / 2f);
        
        Gizmos.DrawLine(p1, p2);
        
        // Alan gorsellestirme (Kutu gibi)
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Vector3 b1 = center + (Vector3)(rightDir * spawnWidth / 2f);
        Vector3 b2 = center - (Vector3)(rightDir * spawnWidth / 2f);
        
        // Basit bir 3D cizim yerine line'larla dortgen cizelim
        Gizmos.DrawLine(b1, p1); // Sag kenar
        Gizmos.DrawLine(b2, p2); // Sol kenar
        Gizmos.DrawLine(b1, b2); // Alt kenar
    }
}
