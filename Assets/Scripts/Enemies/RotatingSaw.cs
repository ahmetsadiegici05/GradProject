using UnityEngine;

/// <summary>
/// Dönen testere (Saw) için basit script.
/// Sürekli döner ve progression'a göre hızlanır.
/// Bu scripti Saw objelerine ekleyin.
/// </summary>
public class RotatingSaw : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f; // derece/saniye
    [SerializeField] private bool clockwise = true;

    [Header("Movement (Optional)")]
    [SerializeField] private bool enableMovement = false;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Damage")]
    [SerializeField] private float damage = 1f;

    private float baseRotationSpeed;
    private float baseMoveSpeed;
    private Transform moveTarget;
    private Vector3 startPos;

    private void Awake()
    {
        baseRotationSpeed = rotationSpeed;
        baseMoveSpeed = moveSpeed;
        startPos = transform.position;
    }

    private void Start()
    {
        if (enableMovement && pointA != null && pointB != null)
        {
            moveTarget = pointB;
        }
    }

    private void Update()
    {
        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        // Rotation
        float effectiveRotSpeed = baseRotationSpeed * speedMultiplier;
        float rotDir = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, rotDir * effectiveRotSpeed * Time.deltaTime);

        // Movement (opsiyonel)
        if (enableMovement && moveTarget != null)
        {
            float effectiveMoveSpeed = baseMoveSpeed * speedMultiplier;
            transform.position = Vector3.MoveTowards(transform.position, moveTarget.position, effectiveMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, moveTarget.position) < 0.05f)
            {
                moveTarget = (moveTarget == pointA) ? pointB : pointA;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Health playerHealth = collision.collider.GetComponent<Health>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
    }
}
