using UnityEngine;

public class KillOnFall : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Fall Death")]
    [Tooltip("Yerçekimi yönünde bu mesafe kadar uzaklaşınca öl")]
    [SerializeField] private float killDistance = 20f;

    [SerializeField] private Health health;
    [SerializeField] private UIManager uiManager;

    [SerializeField] private bool disableObjectIfNoHandlers = true;

    private bool triggered;
    private Vector2 initialPosition;

    private void Awake()
    {
        if (target == null)
            target = transform;

        if (health == null)
            health = GetComponent<Health>() ?? GetComponentInParent<Health>();

        if (uiManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            uiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
#else
            uiManager = FindObjectOfType<UIManager>(true);
#endif
        }
    }

    private void Start()
    {
        // Başlangıç pozisyonunu kaydet (checkpoint'ten veya spawn'dan)
        initialPosition = target != null ? (Vector2)target.position : Vector2.zero;
    }

    private void Update()
    {
        if (triggered)
            return;

        if (target == null)
            return;

        // Yerçekimi yönünü al (dönen dünyada değişir)
        Vector2 gravityDir = Physics2D.gravity.normalized;
        if (gravityDir.sqrMagnitude < 0.001f)
            gravityDir = Vector2.down;

        // Oyuncunun yerçekimi yönündeki ilerlemesini hesapla
        // Pozitif değer = yerçekimi yönünde ilerleme (düşme)
        Vector2 currentPos = target.position;
        float fallAmount = Vector2.Dot(currentPos - initialPosition, gravityDir);

        if (fallAmount > killDistance)
        {
            triggered = true;

            if (health != null)
                health.TakeDamage(Mathf.Infinity);
            else if (uiManager != null)
                uiManager.GameOver();
            else if (disableObjectIfNoHandlers)
                gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Checkpoint veya respawn sonrası referans noktasını güncelle
    /// </summary>
    public void ResetFallTracking()
    {
        triggered = false;
        if (target != null)
            initialPosition = target.position;
    }
}
