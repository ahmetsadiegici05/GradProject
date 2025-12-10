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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

        Vector3 newPosition = Vector3.MoveTowards(transform.position, target.position, speed * Time.fixedDeltaTime);
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
