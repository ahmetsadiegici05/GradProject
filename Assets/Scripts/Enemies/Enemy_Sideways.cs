using UnityEngine;

public class Enemy_Sideways : MonoBehaviour
{
    [SerializeField] private float movementDistance;
    [SerializeField] private float speed;
    [SerializeField] private float damage;
    
    private bool movingInNegativeDir;
    private float baseSpeed;
    
    // Yerçekimine göre hareket için
    private Vector2 startPosition;
    private Vector2 moveAxis; // Hareket ekseni (yerçekimine dik)

    private void Awake()
    {
        baseSpeed = speed;
        startPosition = transform.position;
    }

    private void Start()
    {
        // Başlangıçta hareket eksenini hesapla
        UpdateMoveAxis();
    }

    private void Update()
    {
        // Hareket eksenini yerçekimine göre güncelle
        UpdateMoveAxis();

        float speedMultiplier = 1f;
        if (ProgressionManager.Instance != null)
            speedMultiplier = ProgressionManager.Instance.TrapSpeedMultiplier;

        float effectiveSpeed = baseSpeed * speedMultiplier;

        // Mevcut pozisyonun başlangıca göre hareket ekseni üzerindeki mesafesi
        Vector2 currentPos = transform.position;
        float currentOffset = Vector2.Dot(currentPos - startPosition, moveAxis);

        if (movingInNegativeDir)
        {
            if (currentOffset > -movementDistance)
            {
                // Negatif yönde hareket
                Vector2 movement = -moveAxis * effectiveSpeed * Time.deltaTime;
                transform.position = (Vector2)transform.position + movement;
            }
            else
            {
                movingInNegativeDir = false;
            }
        }
        else
        {
            if (currentOffset < movementDistance)
            {
                // Pozitif yönde hareket
                Vector2 movement = moveAxis * effectiveSpeed * Time.deltaTime;
                transform.position = (Vector2)transform.position + movement;
            }
            else
            {
                movingInNegativeDir = true;
            }
        }
    }

    private void UpdateMoveAxis()
    {
        // Yerçekimine dik olan eksen = hareket ekseni
        Vector2 gravityDir = Physics2D.gravity.normalized;
        if (gravityDir.sqrMagnitude < 0.001f)
            gravityDir = Vector2.down;

        // Yerçekimine dik = "yatay" hareket ekseni
        moveAxis = new Vector2(-gravityDir.y, gravityDir.x);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Health health = collision.GetComponent<Health>();
            if (health != null)
                health.TakeDamage(damage);
        }
    }
}