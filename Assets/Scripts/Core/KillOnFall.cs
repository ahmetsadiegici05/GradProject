using UnityEngine;

public class KillOnFall : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Fall Death")]
    [SerializeField] private float killY = -20f;

    [SerializeField] private Health health;
    [SerializeField] private UIManager uiManager;

    [SerializeField] private bool disableObjectIfNoHandlers = true;

    private bool triggered;

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

    private void Update()
    {
        if (triggered)
            return;

        if (target == null)
            return;

        if (target.position.y < killY)
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
}
