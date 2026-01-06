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

    private enum TrapState { Idle, Warning, Rushing, Returning, Cooldown }
    private TrapState currentState = TrapState.Idle;

    private float stateTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        startPosition = transform.position;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    private void Update()
    {
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

        // Titreme efekti
        float shake = Mathf.Sin(Time.time * 50f) * 0.05f;
        transform.position = startPosition + new Vector2(shake, 0);

        if (stateTimer <= 0)
        {
            transform.position = startPosition;
            StartRush();
        }
    }

    private void StartRush()
    {
        currentState = TrapState.Rushing;

        if (animator != null)
            animator.SetTrigger("Rush");
    }

    private void HandleRushing()
    {
        rb.linearVelocity = rushDirection * rushSpeed;

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
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        if (animator != null)
            animator.SetTrigger("Return");
    }

    private void HandleReturning()
    {
        Vector2 direction = (startPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * returnSpeed;

        if (Vector2.Distance(transform.position, startPosition) < 0.1f)
        {
            transform.position = startPosition;
            rb.linearVelocity = Vector2.zero;
            StartCooldown();
        }
    }

    private void StartCooldown()
    {
        currentState = TrapState.Cooldown;
        stateTimer = cooldownTime;
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
            // İtme yönü: rush yönü + yukarı
            Vector2 pushDirection = rushDirection + Vector2.up * upwardPushForce;
            playerRb.linearVelocity = Vector2.zero; // Mevcut hızı sıfırla
            playerRb.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
        }
    }

    private Vector2 GetAxisLockedDirection(Vector2 direction)
    {
        // Spikehead ile aynı mantık - sadece yatay veya dikey
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return new Vector2(Mathf.Sign(direction.x), 0f);
        
        return new Vector2(0f, Mathf.Sign(direction.y));
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
