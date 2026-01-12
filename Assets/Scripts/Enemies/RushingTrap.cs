using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EnemyHealth))]
public class RushingTrap : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Rush Settings")]
    [SerializeField] private float rushSpeed = 15f;
    [SerializeField] private float pushForce = 20f;
    [SerializeField] private float upwardPushForce = 8f;
    [SerializeField] private float maxRushDistance = 15f;

    [Header("Timing")]
    [SerializeField] private float warningTime = 0.4f;
    [SerializeField] private float cooldownTime = 2.5f;
    [SerializeField] private float returnSpeed = 5f;

    [Header("Effects")]
    [SerializeField] private AudioSource warningSound;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Animator animator;

    [Header("Damage (Optional)")]
    [SerializeField] private int damage = 1;

    private Vector2 startPosition;
    private Vector2 rushDirection;
    private Rigidbody2D rb;

    [Header("Rotation Safety")]
    [SerializeField] private bool pauseDuringWorldRotation = true;

    private enum TrapState { Idle, Warning, Rushing, Returning, Cooldown }
    private TrapState currentState = TrapState.Idle;

    private float stateTimer;

    private float baseRushSpeed;
    private float baseReturnSpeed;

    private Vector2 desiredVelocity;

    private void Awake()
    {
        // SpriteRenderer'ı Awake'de bul (Start'tan önce)
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        // Sprite'ın görünür olduğundan emin ol
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            // normalColor alpha'sı 0 olabilir (Inspector'da ayarlanmadıysa), zorla 1 yap
            if (normalColor.a < 0.01f)
                normalColor = new Color(normalColor.r, normalColor.g, normalColor.b, 1f);
            spriteRenderer.color = normalColor;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        startPosition = transform.position;

        baseRushSpeed = rushSpeed;
        baseReturnSpeed = returnSpeed;

        desiredVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // Rengi tekrar uygula (Awake'de de yapıldı ama emin olmak için)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = normalColor;
        }
            
        Debug.Log($"RushingTrap '{name}' started at position: {transform.position}, visible: {(spriteRenderer != null ? spriteRenderer.enabled.ToString() : "no renderer")}");
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        rb.linearVelocity = desiredVelocity;
    }

    private void Update()
    {
        // Dünya/yerçekimi dönerken tuzağı dondur (dalga reset animasyonu sırasında drift olmasın)
        if (pauseDuringWorldRotation && WorldRotationManager.Instance != null && WorldRotationManager.Instance.IsRotating)
        {
            desiredVelocity = Vector2.zero;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }
        
        switch (currentState)
        {
            case TrapState.Idle:
                HandleIdle();
                break;
            case TrapState.Warning:
                HandleWarning();
                break;
            case TrapState.Rushing:
                HandleRushing();
                break;
            case TrapState.Returning:
                HandleReturning();
                break;
            case TrapState.Cooldown:
                HandleCooldown();
                break;
        }
    }

    private void HandleIdle()
    {
        Collider2D player = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        
        if (player != null)
        {
            // Oyuncuya doğru yön hesapla
            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            rushDirection = GetAxisLockedDirection(directionToPlayer);
            
            StartWarning();
        }
    }

    private void StartWarning()
    {
        currentState = TrapState.Warning;
        stateTimer = warningTime;

        // Görsel uyarı
        if (spriteRenderer != null)
            spriteRenderer.color = warningColor;

        // Ses uyarısı
        if (warningSound != null)
            warningSound.Play();

        // Animasyon
        if (animator != null)
            animator.SetTrigger("Warning");
    }

    private void HandleWarning()
    {
        stateTimer -= Time.deltaTime;

        // Titreme efekti - yerçekimine dik yönde (rush yönünde) titre
        float shake = Mathf.Sin(Time.time * 50f) * 0.05f;
        transform.position = startPosition + rushDirection * shake;

        if (stateTimer <= 0)
        {
            transform.position = startPosition;
            StartRush();
        }
    }

    private void StartRush()
    {
        currentState = TrapState.Rushing;

        desiredVelocity = Vector2.zero;

        if (animator != null)
            animator.SetTrigger("Rush");
    }

    private void HandleRushing()
    {
        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        desiredVelocity = rushDirection * (baseRushSpeed * speedMultiplier);

        // Maksimum mesafe kontrolü
        float distanceFromStart = Vector2.Distance(transform.position, startPosition);
        if (distanceFromStart >= maxRushDistance)
        {
            StartReturning();
        }
    }

    private void StartReturning()
    {
        currentState = TrapState.Returning;
        desiredVelocity = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        if (animator != null)
            animator.SetTrigger("Return");
    }

    private void HandleReturning()
    {
        Vector2 direction = (startPosition - (Vector2)transform.position).normalized;

        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        desiredVelocity = direction * (baseReturnSpeed * speedMultiplier);

        if (Vector2.Distance(transform.position, startPosition) < 0.1f)
        {
            transform.position = startPosition;
            desiredVelocity = Vector2.zero;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            StartCooldown();
        }
    }

    private void StartCooldown()
    {
        currentState = TrapState.Cooldown;
        stateTimer = cooldownTime;

        desiredVelocity = Vector2.zero;
    }

    private void HandleCooldown()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0)
        {
            currentState = TrapState.Idle;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) == 0)
            return;

        // Her zaman hasar ver
        DamagePlayer(collision);

        // Rush sırasında ek olarak it ve geri dön
        if (currentState == TrapState.Rushing)
        {
            PushPlayer(collision);
            StartReturning();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Duvara çarptıysa geri dön
        if (((1 << collision.gameObject.layer) & playerLayer) == 0)
        {
            if (currentState == TrapState.Rushing)
                StartReturning();
            return;
        }

        // Her zaman hasar ver
        DamagePlayer(collision.collider);

        // Rush sırasında ek olarak it ve geri dön
        if (currentState == TrapState.Rushing)
        {
            PushPlayer(collision.collider);
            StartReturning();
        }
    }

    private void DamagePlayer(Collider2D playerCollider)
    {
        Health playerHealth = playerCollider.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    private void PushPlayer(Collider2D playerCollider)
    {
        Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
        
        if (playerRb != null)
        {
            // Yerçekimine göre "yukarı" yönünü hesapla
            Vector2 gravityDir = Physics2D.gravity.normalized;
            Vector2 upDir = -gravityDir; // Yerçekiminin tersi = yukarı
            
            // İtme yönü: rush yönü + yukarı (yerçekimine göre)
            Vector2 pushDirection = rushDirection + upDir * upwardPushForce;
            playerRb.linearVelocity = Vector2.zero; // Mevcut hızı sıfırla
            playerRb.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
        }
    }

    private Vector2 GetAxisLockedDirection(Vector2 direction)
    {
        // Yerçekimine göre "yatay" ve "dikey" eksenleri hesapla
        Vector2 gravityDir = Physics2D.gravity.normalized;
        if (gravityDir.sqrMagnitude < 0.001f)
            gravityDir = Vector2.down;
        
        Vector2 upDir = -gravityDir;           // Yerçekiminin tersi = yukarı
        Vector2 rightDir = new Vector2(-gravityDir.y, gravityDir.x); // Yerçekimine dik = sağ
        
        // Oyuncuya olan yönü yerçekimi eksenlerine göre ayrıştır
        float horizontalComponent = Vector2.Dot(direction, rightDir);  // Yatay bileşen
        float verticalComponent = Vector2.Dot(direction, upDir);       // Dikey bileşen
        
        // Hangi bileşen daha baskın?
        if (Mathf.Abs(horizontalComponent) > Mathf.Abs(verticalComponent))
            return rightDir * Mathf.Sign(horizontalComponent);  // Yatay saldır
        
        return upDir * Mathf.Sign(verticalComponent);  // Dikey saldır
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Vector3 start = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Vector3 rushEnd = Application.isPlaying 
            ? start + (Vector3)(rushDirection * maxRushDistance)
            : start + Vector3.right * maxRushDistance;
        Gizmos.DrawLine(start, rushEnd);
    }
}
