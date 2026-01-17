using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header ("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    private bool dead;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration;
    [SerializeField] private int numberOfFlashes;
    private SpriteRenderer spriteRend;

    private void Awake()
    {
        currentHealth = startingHealth;
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
        
        // Restart sonrası layer collision'ın düzgün çalıştığından emin ol
        Physics2D.IgnoreLayerCollision(8, 9, false);

        if (uiManager == null)
        {
    #if UNITY_2023_1_OR_NEWER
            uiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
    #else
            uiManager = FindObjectOfType<UIManager>(true);
    #endif
        }
    }
    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            anim.SetTrigger("hurt");
            StartCoroutine(Invunerability());

            if (SoundManager.instance != null)
                SoundManager.instance.PlaySound(SoundManager.instance.hurtSound);
        }
        else
        {
            if (!dead)
            {
                if (SoundManager.instance != null)
                    SoundManager.instance.PlaySound(SoundManager.instance.deathSound);

                anim.SetTrigger("die");
                GetComponent<PlayerMovement>().enabled = false;
                dead = true;

                if (uiManager != null)
                    uiManager.GameOver();
            }
        }
    }
    public void AddHealth(float _value)
    {
        currentHealth = Mathf.Clamp(currentHealth + _value, 0, startingHealth);

        if (_value > 0 && SoundManager.instance != null)
            SoundManager.instance.PlaySound(SoundManager.instance.healSound);
    }
    private IEnumerator Invunerability()
    {
        Physics2D.IgnoreLayerCollision(8, 9, true);
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }
        Physics2D.IgnoreLayerCollision(8, 9, false);
    }
}