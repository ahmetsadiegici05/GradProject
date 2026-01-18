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
    
    [Header("Enhancements")]
    [SerializeField] private RockSpawner[] connectedSpawners; // Bu kapi tetiklenince calisacak spawnler

    private bool hasTriggeredRotation = false;
    private bool hasTriggeredTrap = false; // Tuzagin yalnizca bir kez calismasi icin

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

            // Deprem sesi sadece ileri giderken ve eğim değişecekse çalsın
            if (triggerRotation && !hasTriggeredRotation && WorldRotationManager.Instance != null)
            {
                WorldRotationManager.Instance.TriggerRoomTransitionShake();
            }

            // --- YENI: Kapidan gecince RockSpawner'lari calistir (SADECE ILK GECISTE) ---
            if (connectedSpawners != null && connectedSpawners.Length > 0 && !hasTriggeredTrap)
            {
                hasTriggeredTrap = true; // Tek seferlik isaretle
                StartCoroutine(TriggerTrapSequence());
            }

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

            // Geri giderken deprem sesi ÇALMASIN

            if (debugLogs)
                Debug.Log($"Door '{name}': PREVIOUS room -> {previousRoom.name}");
        }
    }

    private System.Collections.IEnumerator TriggerTrapSequence()
    {
        // 1. Yaziyi goster (HEMEN - UYARI)
        if (StageDebugDisplay.Instance != null)
        {
            StageDebugDisplay.Instance.ShowAnnouncement("EARTHQUAKE");
        }

        // 2. Ekrani salla (HEMEN - Sarsinti baslasin)
        // triggerRotation zaten true ise yukarida calmistir, degilse biz calalim
        if (WorldRotationManager.Instance != null && !triggerRotation)
        {
            WorldRotationManager.Instance.TriggerRoomTransitionShake();
        }

        // 3. Bekle (Oyuncuya kacma/pozisyon alma sansi ver)
        yield return new WaitForSeconds(1.5f); // 1.5 saniye mühlet

        // 4. Taslari dusur (GERCEK TEHLIKE)
        if (connectedSpawners != null)
        {
            foreach (var spawner in connectedSpawners)
            {
                if (spawner != null) spawner.ForceStartSpawning();
            }
        }
    }
}