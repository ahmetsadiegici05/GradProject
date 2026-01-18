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

    [Header("Juice / Game Feel")]
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private bool enableHitStop = true;
    [SerializeField] private float hitStopDuration = 0.1f;

    private CameraController camController;

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

        camController = FindObjectOfType<CameraController>();
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

             // Juice: Shake Screen
            if (enableScreenShake && camController != null)
            {
                camController.TriggerShake(shakeIntensity, shakeDuration);
            }

            // Juice: Hit Stop (Frame Freeze)
            if (enableHitStop)
            {
                StartCoroutine(HitStop());
            }
        }
        else
        {
            if (!dead)
            {
                // Oldurucu darbede daha buyuk shake
                if (enableScreenShake && camController != null)
                {
                    camController.TriggerShake(shakeIntensity * 1.5f, shakeDuration * 1.5f);
                }

                // Slow motion death effect
                if (enableHitStop) StartCoroutine(HitStop(0.2f)); // Biraz daha uzun duraksama

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
    
    private IEnumerator HitStop(float durationOverride = -1f)
    {
        float duration = durationOverride > 0 ? durationOverride : hitStopDuration;
        
        if (Time.timeScale > 0)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale;
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