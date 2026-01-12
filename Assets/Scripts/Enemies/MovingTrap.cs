using UnityEngine;

/// <summary>
/// Genel amaçlı hareket eden tuzak.
/// İki nokta arasında gidip gelir, progression'a göre hızlanır.
/// Saw, spike platform, uçan tuzak vb. için kullanılabilir.
/// </summary>
public class MovingTrap : MonoBehaviour
{
    public enum MovementType
    {
        Linear,         // Düz çizgi
        Circular,       // Dairesel
        Sine            // Sinüs dalgası
    }

    [Header("Movement")]
    [SerializeField] private MovementType movementType = MovementType.Linear;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool startAtPointA = true;

    [Header("Circular Settings")]
    [SerializeField] private float circleRadius = 2f;
    [SerializeField] private float circleSpeed = 90f; // derece/saniye

    [Header("Sine Settings")]
    [SerializeField] private float sineAmplitude = 2f;
    [SerializeField] private float sineFrequency = 1f;
    [SerializeField] private bool sineOnYAxis = true;

    [Header("Rotation (Optional)")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Damage")]
    [SerializeField] private float damage = 1f;

    private float baseSpeed;
    private float baseCircleSpeed;
    private float baseRotationSpeed;
    private Transform target;
    private Vector3 startPosition;
    private float circleAngle;
    private float sineTime;

    private void Awake()
    {
        baseSpeed = speed;
        baseCircleSpeed = circleSpeed;
        baseRotationSpeed = rotationSpeed;
        startPosition = transform.position;
    }

    private void Start()
    {
        if (movementType == MovementType.Linear)
        {
            if (pointA != null && pointB != null)
            {
                transform.position = startAtPointA ? pointA.position : pointB.position;
                target = startAtPointA ? pointB : pointA;
            }
        }
    }

    private void Update()
    {
        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        switch (movementType)
        {
            case MovementType.Linear:
                UpdateLinearMovement(speedMultiplier);
                break;
            case MovementType.Circular:
                UpdateCircularMovement(speedMultiplier);
                break;
            case MovementType.Sine:
                UpdateSineMovement(speedMultiplier);
                break;
        }

        if (enableRotation)
        {
            float effectiveRotSpeed = baseRotationSpeed * speedMultiplier;
            transform.Rotate(0f, 0f, effectiveRotSpeed * Time.deltaTime);
        }
    }

    private void UpdateLinearMovement(float multiplier)
    {
        if (target == null) return;

        float effectiveSpeed = baseSpeed * multiplier;
        transform.position = Vector3.MoveTowards(transform.position, target.position, effectiveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            target = (target == pointA) ? pointB : pointA;
        }
    }

    private void UpdateCircularMovement(float multiplier)
    {
        float effectiveCircleSpeed = baseCircleSpeed * multiplier;
        circleAngle += effectiveCircleSpeed * Time.deltaTime;

        float x = startPosition.x + Mathf.Cos(circleAngle * Mathf.Deg2Rad) * circleRadius;
        float y = startPosition.y + Mathf.Sin(circleAngle * Mathf.Deg2Rad) * circleRadius;

        transform.position = new Vector3(x, y, startPosition.z);
    }

    private void UpdateSineMovement(float multiplier)
    {
        float effectiveSpeed = baseSpeed * multiplier;
        sineTime += Time.deltaTime * sineFrequency * multiplier;

        float offset = Mathf.Sin(sineTime * Mathf.PI * 2f) * sineAmplitude;

        // Lineer hareket + sine offset
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, effectiveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                target = (target == pointA) ? pointB : pointA;
            }

            // Sine offset ekle
            Vector3 pos = transform.position;
            if (sineOnYAxis)
                pos.y += offset * Time.deltaTime;
            else
                pos.x += offset * Time.deltaTime;
            transform.position = pos;
        }
        else
        {
            // Sadece sine hareketi
            Vector3 pos = startPosition;
            if (sineOnYAxis)
                pos.y += offset;
            else
                pos.x += offset;
            transform.position = pos;
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

    private void OnDrawGizmosSelected()
    {
        // Editor'da hareket yolunu göster
        if (movementType == MovementType.Linear && pointA != null && pointB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
        }
        else if (movementType == MovementType.Circular)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            
            // Daire çiz
            int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * 360f * Mathf.Deg2Rad;
                float angle2 = ((i + 1) / (float)segments) * 360f * Mathf.Deg2Rad;
                
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * circleRadius;
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * circleRadius;
                
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}
