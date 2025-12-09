using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class Spikehead : EnemyDamage
{
    [Header("SpikeHead Attributes")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float range = 10f;
    [SerializeField] private float checkDelay = 1f;
    [SerializeField] private LayerMask playerLayer;
    
    private Vector2[] directions = new Vector2[4];
    private Vector2 moveDirection;
    private float checkTimer;
    private bool attacking;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        CalculateDirections();
    }

    private void OnEnable()
    {
        if (rb != null)
            Stop();
    }

    private void Update()
    {
        if (attacking)
        {
            // Oyuncu alanında değilse durdu
            if (!IsPlayerInRange(out Vector2 playerDirection))
            {
                Stop();
                // Rasgele sola veya sağa git
                moveDirection = Random.value > 0.5f ? Vector2.right : Vector2.left;
                attacking = true; // Patrolü devam ettir
                return;
            }
            
            // Oyuncu yönü güncelle (zıplama sırasında da takip etsin)
            moveDirection = playerDirection;
            
            // Move towards direction
            rb.linearVelocity = moveDirection * speed;
        }
        else
        {
            // Check for player periodically (not every frame)
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkDelay)
            {
                CheckForPlayer();
                checkTimer = 0;
            }
        }
    }

    private void CheckForPlayer()
    {
        Collider2D player = Physics2D.OverlapCircle(transform.position, range, playerLayer);
        if (player == null)
            return;

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
        moveDirection = AxisLockedDirection(directionToPlayer);
        attacking = true;
    }

    private void CalculateDirections()
    {
        directions[0] = Vector2.right;     // Right
        directions[1] = Vector2.left;      // Left
        directions[2] = Vector2.up;        // Up
        directions[3] = Vector2.down;      // Down
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
        attacking = false;
    }

    private bool IsPlayerInRange(out Vector2 playerDirection)
    {
        Collider2D player = Physics2D.OverlapCircle(transform.position, range, playerLayer);
        if (player != null)
        {
            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            playerDirection = AxisLockedDirection(directionToPlayer);
            return true;
        }

        playerDirection = moveDirection; // Oyuncu bulunamadıysa son yönü koru
        return false;
    }

    private Vector2 AxisLockedDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return new Vector2(Mathf.Sign(direction.x), 0f);

        return new Vector2(0f, Mathf.Sign(direction.y));
    }

    private new void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        Stop();
    }
}