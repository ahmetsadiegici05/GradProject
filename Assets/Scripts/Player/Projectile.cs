using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float damage = 1f;
    private float direction;
    private Vector2 moveDirection;
    private bool hit;
    private float lifetime;

    private Animator anim;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (hit) return;

        // Yerçekimine göre hareket yönünde ilerle
        // Time slow aktifken oyuncu mermileri normal hızda uçsun
        float timeCompensation = TimeSlowAbility.Instance != null ? TimeSlowAbility.Instance.PlayerTimeCompensation : 1f;
        transform.position += (Vector3)moveDirection * speed * Time.deltaTime * timeCompensation;

        lifetime += Time.deltaTime * timeCompensation;
        if (lifetime > 5) gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hit)
            return;

        // Door'lardan geç, yok olma
        if (collision.GetComponent<Door>() != null)
            return;

        // Can collectible'lerinden geç, yok olma
        if (collision.GetComponent<HealthCollectible>() != null)
            return;

        // Ekran dışındaysa hasar verme
        if (!IsVisibleOnScreen())
            return;

        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.TakeDamage(damage);

        hit = true;
        boxCollider.enabled = false;
        anim.SetTrigger("explode");
    }
    
    /// <summary>
    /// Mermi ekranda görünür mü kontrol et
    /// </summary>
    private bool IsVisibleOnScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
        
        // Viewport koordinatları 0-1 arasında ise ekranda
        return viewportPos.x >= 0f && viewportPos.x <= 1f &&
               viewportPos.y >= 0f && viewportPos.y <= 1f &&
               viewportPos.z > 0f; // Kameranın önünde
    }

    public void SetDirection(float _direction)
    {
        lifetime = 0;
        direction = _direction;
        gameObject.SetActive(true);
        hit = false;
        boxCollider.enabled = true;

        // Yerçekimine göre "sağ" yönünü hesapla
        Vector2 gravityDir = Physics2D.gravity.normalized;
        if (gravityDir.sqrMagnitude < 0.001f)
            gravityDir = Vector2.down;

        Vector2 rightDir = new Vector2(-gravityDir.y, gravityDir.x);
        moveDirection = rightDir * direction;

        // Scale flip (eski davranış - sprite yönü)
        float localScaleX = transform.localScale.x;
        if (Mathf.Sign(localScaleX) != direction)
            localScaleX = -localScaleX;

        transform.localScale = new Vector3(localScaleX, transform.localScale.y, transform.localScale.z);

        // Sprite rotasyonu - sadece yerçekimi normal değilse döndür
        bool gravityIsNormal = Mathf.Abs(gravityDir.y + 1f) < 0.1f; // yaklaşık (0, -1)
        if (!gravityIsNormal)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            if (direction < 0) angle += 180f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}