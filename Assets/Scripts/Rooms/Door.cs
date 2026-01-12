using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform previousRoom;
    [SerializeField] private Transform nextRoom;
    [SerializeField] private CameraController cam;
    
    [Header("Progression")]
    [Tooltip("Bu kapıdan geçince eğim/zorluk artsın mı?")]
    [SerializeField] private bool triggerRotation = false;
    [SerializeField] private bool debugLogs = false;

    private bool hasTriggeredRotation = false;

    private void Awake()
    {
        if (cam == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cam = mainCam.GetComponent<CameraController>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        if (cam == null)
        {
            Debug.LogWarning($"Door '{name}': CameraController bulunamadı. Inspector'dan cam atayın.");
            return;
        }

        // "İleri" yönünü yerçekimine göre belirle (dönüşlerde de çalışsın)
        Vector2 forward = Vector2.right;
        if (WorldRotationManager.Instance != null)
            forward = WorldRotationManager.RightDirection;

        Vector2 diff = (Vector2)collision.transform.position - (Vector2)transform.position;
        bool comingFromBehind = Vector2.Dot(diff, forward) < 0f;

        if (comingFromBehind)
        {
            if (nextRoom == null)
            {
                Debug.LogWarning($"Door '{name}': NextRoom atanmadı.");
                return;
            }

            cam.MoveToNewRoom(nextRoom);

            if (WorldRotationManager.Instance != null)
                WorldRotationManager.Instance.TriggerRoomTransitionShake();

            if (triggerRotation && !hasTriggeredRotation && ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.AdvanceStage();
                hasTriggeredRotation = true;
            }

            if (debugLogs)
                Debug.Log($"Door '{name}': NEXT room -> {nextRoom.name}" + (triggerRotation && hasTriggeredRotation ? " (stage advanced)" : ""));
        }
        else
        {
            if (previousRoom == null)
            {
                Debug.LogWarning($"Door '{name}': PreviousRoom atanmadı.");
                return;
            }

            cam.MoveToNewRoom(previousRoom);

            if (WorldRotationManager.Instance != null)
                WorldRotationManager.Instance.TriggerRoomTransitionShake();

            if (debugLogs)
                Debug.Log($"Door '{name}': PREVIOUS room -> {previousRoom.name}");
        }
    }
}