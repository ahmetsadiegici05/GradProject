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

    [Header("Ranged Attack")]
    [SerializeField] private Transform firepoint;
    [SerializeField] private GameObject[] fireballs;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int poolSize = 3;
    [SerializeField] private Transform projectileParent;
    [SerializeField] private float fireballCooldown = 3f;
    [SerializeField] private float shootingRange = 8f;
    [SerializeField] private bool requireShootingUnlock = true;
    
    private Vector2[] directions = new Vector2[4];
    private Vector2 moveDirection;
    private float checkTimer;
    private float fireballTimer;
    private bool attacking;
    private Rigidbody2D rb;

    private GameObject[] pooledFireballs;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        CalculateDirections();

        if (firepoint == null)
            firepoint = transform;

        BuildProjectilePool();
    }

    private void BuildProjectilePool()
    {
        // Prefer explicit fireballs array (can contain prefab assets or scene instances)
        if (fireballs != null && fireballs.Length > 0)
        {
            pooledFireballs = new GameObject[fireballs.Length];
            for (int i = 0; i < fireballs.Length; i++)
            {
                GameObject entry = fireballs[i];
                if (entry == null)
                    continue;

                // If it's a prefab asset reference, its scene is not valid -> instantiate.
                if (!entry.scene.IsValid())
                    pooledFireballs[i] = Instantiate(entry, projectileParent);
                else
                    pooledFireballs[i] = entry;

                pooledFireballs[i].SetActive(false);
            }
            return;
        }

        // Fallback: use a single prefab + pool size.
        if (projectilePrefab != null && poolSize > 0)
        {
            pooledFireballs = new GameObject[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                pooledFireballs[i] = Instantiate(projectilePrefab, projectileParent);
                pooledFireballs[i].SetActive(false);
            }
            return;
        }

        pooledFireballs = null;
        Debug.LogWarning("Spikehead: No projectile setup. Assign Fireballs array or Projectile Prefab.");
    }

    private void OnEnable()
    {
        if (rb != null)
            Stop();
    }

    private void Update()
    {
        fireballTimer += Time.deltaTime;

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
            
            // Ateş menzilinde mi kontrol et
            Collider2D player = Physics2D.OverlapCircle(transform.position, shootingRange, playerLayer);
            if (player != null
                && fireballTimer >= fireballCooldown
                && pooledFireballs != null
                && pooledFireballs.Length > 0
                && (!requireShootingUnlock || CheckpointData.SpikeheadShootingUnlocked))
            {
                ShootFireball();
                fireballTimer = 0;
            }
            
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

    private void ShootFireball()
    {
        if (pooledFireballs == null || pooledFireballs.Length == 0)
        {
            Debug.LogWarning("Spikehead: No projectile pool available!");
            return;
        }

        int fireballIndex = FindFireball();
        if (fireballIndex == -1)
        {
            Debug.LogWarning("Spikehead: No available fireball found!");
            return;
        }

        GameObject fireball = pooledFireballs[fireballIndex];
        if (fireball == null)
        {
            Debug.LogWarning("Spikehead: Fireball pool contains null entry!");
            return;
        }

        fireball.transform.position = firepoint.position;
        fireball.SetActive(true);

        EnemyProjectile projectile = fireball.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.ActivateProjectile();
            Debug.Log("Spikehead: Fireball shot!");
        }
        else
        {
            Debug.LogWarning("Spikehead: Fireball missing EnemyProjectile component!");
        }
    }

    private int FindFireball()
    {
        if (pooledFireballs == null)
            return -1;

        for (int i = 0; i < pooledFireballs.Length; i++)
        {
            if (pooledFireballs[i] != null && !pooledFireballs[i].activeInHierarchy)
                return i;
        }
        return -1;
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