using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField, Min(0.1f)] private float speed = 3f;
    [SerializeField] private bool startAtPointA = true;

    private Rigidbody2D rb;
    private Transform target;
    private float baseSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        baseSpeed = speed;
    }

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"MovingPlatform '{name}' is missing point references.");
            enabled = false;
            return;
        }

        transform.position = startAtPointA ? pointA.position : pointB.position;
        target = startAtPointA ? pointB : pointA;
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        float effectiveSpeed = baseSpeed * speedMultiplier;

        Vector3 newPosition = Vector3.MoveTowards(transform.position, target.position, effectiveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if (Vector3.Distance(newPosition, target.position) < 0.02f)
        {
            target = target == pointA ? pointB : pointA;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
