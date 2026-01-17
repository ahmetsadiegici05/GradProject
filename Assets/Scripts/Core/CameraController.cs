using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Player")]
    [SerializeField] private Transform player;
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private float lookAheadDistance = 2f;

    [Header("Zoom")]
    [SerializeField] private bool overrideOrthographicSize = true;
    [SerializeField] private float targetOrthographicSize = 7f;
    [SerializeField] private float zoomSmoothTime = 0.1f;
    
    [Header("Room Camera (Optional)")]
    [SerializeField] private bool useRoomCamera = false;
    [SerializeField] private float roomTransitionSpeed = 3f;
    
    private Vector2 currentRoomPos;
    private float lookAhead;
    private Vector3 velocity = Vector3.zero;

    private Camera cam;
    private float zoomVelocity;

    // Shake logic - Perlin noise for smoothness
    private Vector3 internalPosition; // Position WITHOUT shake
    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;
    private float shakeNoiseOffset;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        internalPosition = transform.position;

        if (player == null)
        {
            // Otomatik olarak "Player" tag'li objeyi bul
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("CameraController: Player referansı atanmadı ve 'Player' tag'li obje bulunamadı!");
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Apply shake timer with decay
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            if (shakeTimer <= 0)
            {
                shakeIntensity = 0f;
                shakeDuration = 0f;
            }
        }

        ApplyZoom();

        // Yerçekimine göre yön vektörleri
        Vector2 gravityDir = Physics2D.gravity.normalized;
        if (gravityDir.sqrMagnitude < 0.001f)
            gravityDir = Vector2.down;
        
        Vector2 rightDir = new Vector2(-gravityDir.y, gravityDir.x);
        Vector2 upDir = -gravityDir;

        // Calculate TARGET position for logic (internalPosition logic)
        Vector3 nextInternalPos = internalPosition;

        if (useRoomCamera)
        {
            // Room camera modu - yerçekimine göre oda pozisyonuna git
            Vector3 targetPos = new Vector3(currentRoomPos.x, currentRoomPos.y, internalPosition.z);
            nextInternalPos = Vector3.SmoothDamp(
                internalPosition, 
                targetPos, 
                ref velocity, 
                roomTransitionSpeed
            );
        }
        else
        {
            // Follow player modu - yerçekimine göre takip
            float facing = Mathf.Sign(player.lossyScale.x);
            if (Mathf.Approximately(facing, 0f)) facing = 1f;
            lookAhead = Mathf.Lerp(lookAhead, lookAheadDistance * facing, Time.deltaTime * followSpeed);
            
            // Yerçekimine göre look ahead yönü
            Vector2 lookAheadOffset = rightDir * lookAhead;
            
            Vector3 targetPos = new Vector3(
                player.position.x + lookAheadOffset.x, 
                player.position.y + lookAheadOffset.y, 
                internalPosition.z
            );
            
            nextInternalPos = Vector3.Lerp(internalPosition, targetPos, Time.deltaTime * followSpeed);
        }

        // Keep internal position updated
        internalPosition = nextInternalPos;

        // Apply Shake Offset with smooth Perlin noise
        Vector3 shakeOffset = Vector3.zero;
        if (shakeIntensity > 0f && shakeDuration > 0f)
        {
            // Decay multiplier (shake azalarak biter)
            float decay = shakeTimer / shakeDuration;
            float currentIntensity = shakeIntensity * decay;
            
            // Perlin noise for smoothness (time-based)
            float noiseTime = Time.unscaledTime * 25f + shakeNoiseOffset;
            float x = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f * currentIntensity;
            float y = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * 2f * currentIntensity;
            shakeOffset = new Vector3(x, y, 0);
        }

        // Final Transform Apply
        transform.position = internalPosition + shakeOffset;
    }

    public void TriggerShake(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
        shakeNoiseOffset = Random.Range(0f, 100f); // Her shake farklı pattern
    }

    private void ApplyZoom()
    {
        if (!overrideOrthographicSize || cam == null) return;
        if (!cam.orthographic) return;

        if (zoomSmoothTime <= 0f)
        {
            cam.orthographicSize = targetOrthographicSize;
            return;
        }

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetOrthographicSize,
            ref zoomVelocity,
            zoomSmoothTime
        );
    }

    public void SetZoom(float orthographicSize)
    {
        targetOrthographicSize = orthographicSize;
    }

    public void MoveToNewRoom(Transform _newRoom)
    {
        currentRoomPos = _newRoom.position;
    }
    
    /// <summary>
    /// Kamerayı anında oyuncuya taşı (checkpoint/respawn için)
    /// </summary>
    public void SnapToPlayer()
    {
        if (player == null) return;
        
        internalPosition = new Vector3(player.position.x, player.position.y, internalPosition.z);
        transform.position = internalPosition;
        lookAhead = 0f;
        velocity = Vector3.zero;
    }
}