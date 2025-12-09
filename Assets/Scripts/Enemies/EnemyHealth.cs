using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private GameObject deathEffect;
    private float currentHealth;
    private bool dead;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        dead = false;
    }

    public void TakeDamage(float amount)
    {
        if (dead)
            return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        dead = true;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        gameObject.SetActive(false);
    }
}
