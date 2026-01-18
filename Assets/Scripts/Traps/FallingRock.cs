using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingRock : MonoBehaviour
{
    [SerializeField] private float damage = 0.5f; // Bir tas yarim kalp vursun
    [SerializeField] private GameObject breakEffect; // Parçalanma efekti (varsa)
    [SerializeField] private bool destroyAfterTime = false;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private bool rotateVisual = true;
    [SerializeField] private bool breakOnPlayerHit = false;
    
    [Header("Physics")]
    [SerializeField] private float bounciness = 0.3f; // Hafif ziplamasi icin
    [SerializeField] private float friction = 0.6f;
    [SerializeField] private bool useTriggerUntilClear = true; // Tavandan gecene kadar trigger
    [SerializeField] private LayerMask ceilingLayer; // Tavani temsil eden layer
    [SerializeField] private bool destroyOnGroundContact = true; // Yere temas edince yok et
    [SerializeField] private float groundDestroyDelay = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool hasLanded = false;
    private bool firstContactLogged = false;

    private float startTime;

    private void Start()
    {
        startTime = Time.time;
        rb = GetComponent<Rigidbody2D>();
        
        // KRITIK: Rigidbody ayarlarini zorla set et
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f; // Daha hizli dussun
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = false; // Add explicit old-school setter just in case
        rb.constraints = RigidbodyConstraints2D.None; // Z rotasyonunu serbest birak
        
        Debug.Log($"FallingRock: Initialized at {transform.position}, GravityScale: {rb.gravityScale}, BodyType: {rb.bodyType}");
        
        // Fizik malzemesi ayarla (bounce ve friction)
        PhysicsMaterial2D material = new PhysicsMaterial2D();
        material.bounciness = bounciness;
        material.friction = friction;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.sharedMaterial = material;
            // Tavandan gecene kadar trigger
            if (useTriggerUntilClear)
            {
                if (ceilingLayer == 0)
                {
                    ceilingLayer = LayerMask.GetMask("Wall", "Ceiling");
                }
                col.isTrigger = true;
            }
        }
        
        if (destroyAfterTime)
        {
            Destroy(gameObject, lifeTime);
        }
        
        if (rotateVisual)
        {
            rb.AddTorque(Random.Range(-100f, 100f));
        }

        // FİZİK MOTORUNU UYANDIR
        rb.WakeUp();
        // Cok kucuk bir ilk hiz ver ki fizik motoru calismaya baslasin
        if (Physics2D.gravity.sqrMagnitude > 0.01f)
            rb.linearVelocity = Physics2D.gravity.normalized * 0.1f;
        else
            rb.linearVelocity = Vector2.down * 2f; // Yercekimi yoksa asagi it

        // Ground layer fallback
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }
    }

    public void ConfigureCeilingLayer(LayerMask mask)
    {
        ceilingLayer = mask;
    }

    public void ConfigureTriggerUntilClear(bool enabled)
    {
        useTriggerUntilClear = enabled;
    }
    public void IgnoreCollisionWith(Collider2D otherCollider)
    {
        if (otherCollider != null)
        {
            Collider2D myCollider = GetComponent<Collider2D>();
            if (myCollider != null)
            {
                Physics2D.IgnoreCollision(myCollider, otherCollider, true);
                // Debug.Log($"FallingRock: Ignoring collision with {otherCollider.name}");
            }
        }
    }    
    private void FixedUpdate()
    {
        // Ilk 1 saniye icinde asla static yapma (fizik motorunun calismasi icin zaman ver)
        if (Time.time < startTime + 1.0f) return;

        // Tavandan cikana kadar trigger kal
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger && useTriggerUntilClear)
        {
            if (ceilingLayer == 0)
            {
                ceilingLayer = LayerMask.GetMask("Wall", "Ceiling");
            }

            if (ceilingLayer != 0)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.useLayerMask = true;
                filter.layerMask = ceilingLayer;
                Collider2D[] results = new Collider2D[1];
                int overlapCount = col.Overlap(filter, results);
                if (overlapCount == 0)
                {
                    col.isTrigger = false;
                }
            }
        }

        // Eger yere dustuyse ve hareketsizse, Rigidbody'yi uyut (performans)
        // Not: Unity surumune gore linearVelocity veya velocity kullanilir. Guvenli olmasi icin velocity kullaniyoruz.
        if (!hasLanded && rb != null && rb.linearVelocity.sqrMagnitude < 0.1f)
        {
            hasLanded = true;
            rb.bodyType = RigidbodyType2D.Static; // Artik hareketsiz bir platform
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!firstContactLogged)
        {
            firstContactLogged = true;
            Debug.Log($"FallingRock FIRST TRIGGER: hit={collision.name} tag={collision.tag} layer={LayerMask.LayerToName(collision.gameObject.layer)} pos={transform.position}");
        }

        // Sadece Player'a carpinca hasar ver
        if (collision.CompareTag("Player"))
        {
            var health = collision.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            
            if (breakOnPlayerHit)
            {
                Break();
            }
        }
    }
    
    // Collision (IsTrigger kapaliysa)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!firstContactLogged)
        {
            firstContactLogged = true;
            ContactPoint2D cp = collision.contacts.Length > 0 ? collision.contacts[0] : default;
            string cpInfo = collision.contacts.Length > 0 ? $" contactPoint={cp.point}" : " contactPoint=none";
            Debug.Log($"FallingRock FIRST COLLISION: hit={collision.collider.name} tag={collision.collider.tag} layer={LayerMask.LayerToName(collision.collider.gameObject.layer)} pos={transform.position}{cpInfo}");
        }

        if (IsWallOrCeiling(collision.collider))
        {
            IgnoreCollisionWith(collision.collider);
            // Kucuk bir itis verip dusmeye devam etmesini sagla
            if (Physics2D.gravity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity = Physics2D.gravity.normalized * Mathf.Max(0.5f, rb.linearVelocity.magnitude);
            }
            return;
        }


        if (collision.collider.CompareTag("Player"))
        {
            // Eger linearVelocity.y 0'a yakin degilse (yukaridan dusuyor demektir), hasar ver.
            // Ama yerden sekme (kucuk ziplama) durumunda cok az hiz olabilir.
            // En temizi: hasLanded true ise ve yerde duruyorsa, sadece oyuncuya degdigi icin hasar vermesin.
            // Sadece DUSME (velocity > threshold) aninda hasar versin.

            bool isFalling = rb.linearVelocity.magnitude > 1.0f; 
            
            if (isFalling && !hasLanded)
            {
                var health = collision.collider.GetComponent<Health>();
                if (health != null) health.TakeDamage(damage);
                
                if (breakOnPlayerHit)
                {
                    Break();
                }
            }
        }
        else if (destroyOnGroundContact && IsGround(collision.collider))
        {
            StartCoroutine(DestroyAfterDelay(groundDestroyDelay));
        }
        // Yere carpinca hareketi durdur (hasLanded flag'i FixedUpdate'te kontrol edilecek)
    }

    private void Break()
    {
        if (breakEffect != null)
        {
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        }
        
        // Ses efekti icin SoundManager kontrolu
        if (SoundManager.instance != null && SoundManager.instance.explosionSound != null)
        {
            // Kucuk bir ses calabiliriz (sesi cok bogmamak icin volume ayarlanabilir)
           // AudioSource.PlayClipAtPoint(SoundManager.instance.explosionSound, transform.position);
        }
        
        Destroy(gameObject);
    }

    private bool IsWallOrCeiling(Collider2D col)
    {
        if (col == null) return false;

        // LayerMask icinde mi?
        if (ceilingLayer != 0 && ((ceilingLayer.value & (1 << col.gameObject.layer)) != 0))
            return true;

        // Layer adina gore (Wall/Ceiling)
        string layerName = LayerMask.LayerToName(col.gameObject.layer);
        if (layerName == "Wall" || layerName == "Ceiling")
            return true;

        // Isme gore (Ceiling objesi gibi)
        if (col.name.Contains("Ceiling"))
            return true;

        return false;
    }

    private bool IsGround(Collider2D col)
    {
        if (col == null) return false;

        if (groundLayer != 0 && ((groundLayer.value & (1 << col.gameObject.layer)) != 0))
            return true;

        string layerName = LayerMask.LayerToName(col.gameObject.layer);
        if (layerName == "Ground")
            return true;

        if (col.CompareTag("Ground"))
            return true;

        return false;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        // Kaybolmadan once biraz bekle
        float shrinkDuration = 0.5f; // Kuculme suresi
        float waitTime = Mathf.Max(0f, delay - shrinkDuration);
        
        yield return new WaitForSeconds(waitTime);
        
        // Yavasca kuculerek kaybol
        float timer = 0f;
        Vector3 originalScale = transform.localScale;
        
        while (timer < shrinkDuration)
        {
            if (this == null || gameObject == null) yield break;
            
            timer += Time.deltaTime;
            float progress = timer / shrinkDuration;
            
            // Scale'i kucult
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            
            // Opsiyonel: Hafifce dondurerek topraga karisiyormus hissi ver
            transform.Rotate(0, 0, 100 * Time.deltaTime);
            
            yield return null;
        }

        if (this != null && gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
