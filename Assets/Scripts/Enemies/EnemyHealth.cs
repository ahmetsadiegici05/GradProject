using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private GameObject deathEffect;
    
    [Header("Fade Out Death")]
    [SerializeField] private bool useFadeOut = false;
    [SerializeField] private float fadeOutDuration = 1f;
    
    [Header("Float Up Death")]
    [SerializeField] private bool useFloatUp = false;
    [SerializeField] private float floatUpSpeed = 3f;
    [SerializeField] private float floatUpDuration = 1f;
    
    private float currentHealth;
    private bool dead;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        dead = false;
        
        // Alpha'yı sıfırla
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (dead)
            return;

        currentHealth -= amount;
        
        // Hasar flash efekti
        if (spriteRenderer != null)
            StartCoroutine(DamageFlash());

        if (currentHealth <= 0f)
            Die();
    }

    private IEnumerator DamageFlash()
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        
        if (!dead)
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        dead = true;

        // Skor sistemine bildir
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddEnemyKill();
        }

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // Collider'ı kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (useFloatUp && spriteRenderer != null)
        {
            StartCoroutine(FloatUpAndDestroy());
        }
        else if (useFadeOut && spriteRenderer != null)
        {
            StartCoroutine(FadeOutAndDestroy());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator FloatUpAndDestroy()
    {
        // Rigidbody varsa durdur
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < floatUpDuration)
        {
            elapsed += Time.deltaTime;
            
            // Yukarı doğru hareket
            transform.position += Vector3.up * floatUpSpeed * Time.deltaTime;
            
            // Aynı zamanda fade out
            float alpha = Mathf.Lerp(1f, 0f, elapsed / floatUpDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator FadeOutAndDestroy()
    {
        // Collider'ı kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Rigidbody varsa durdur
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            // Hafif aşağı düşme efekti
            transform.position += Vector3.down * Time.deltaTime * 0.5f;
            
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
