using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float resetTime = 3f;
    [SerializeField] private float damage = 1f;
    
    private float lifetime;
    private Vector2 direction;
    private BoxCollider2D boxCollider;

    private float baseSpeed;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        baseSpeed = speed;
    }

    public void ActivateProjectile()
    {
        // Spikehead'den player'a doğru yönü hesapla
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            direction = (player.transform.position - transform.position).normalized;
        }
        else
        {
            direction = transform.right; // Player yoksa sağa doğru
        }

        lifetime = 0;
        gameObject.SetActive(true);
        if (boxCollider != null)
            boxCollider.enabled = true;
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        float effectiveSpeed = baseSpeed * speedMultiplier;

        Vector3 movement = (Vector3)direction * effectiveSpeed * Time.deltaTime;
        transform.position += movement;

        lifetime += Time.deltaTime;
        if (lifetime > resetTime)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            gameObject.SetActive(false);
        }
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
        }
    }
}
