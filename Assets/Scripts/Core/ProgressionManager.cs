using UnityEngine;

/// <summary>
/// Level/room geçişlerinde zorluk artışı:
/// - Eğim (world rotation) kademeli artar
/// - Oyuncu hız/jump artar
/// - İsteğe bağlı timeScale artar (dikkatli kullan)
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Progression")]
    [SerializeField] private int stageIndex = 0;

    [Header("Tilt Wave System")]
    [SerializeField] private bool enableTiltProgression = true;
    [SerializeField] private float tiltStepDegrees = 1.75f;
    [SerializeField] private float maxTiltDegrees = 7f;           // Dalga zirvesi (max eğim)
    [SerializeField] private int stagesPerWave = 3;               // Kaç stage'de bir dalga tamamlanır
    [SerializeField] private float tiltResetDuration = 1.5f;      // Eğim sıfırlanma animasyon süresi
    [Tooltip("Saat yönünde artış için -1, saat yönünün tersine için +1")]
    [SerializeField] private float tiltDirectionSign = -1f;

    [Header("Speed Scaling (Sürekli Artar)")]
    [SerializeField] private bool enableSpeedScaling = true;
    [SerializeField] private float speedStep = 0.05f;            // Onceki %1.5 -> Simdi %5 her stage
    [SerializeField] private float maxSpeedMultiplier = 2.5f;     // Onceki 1.5 -> Simdi 2.5 (Max %150)
    
    [Tooltip("Wawe başına ekstra zorluk çarpanı")]
    [SerializeField] private float waveDifficultyMultiplier = 0.1f; 

    [Header("Player Scaling")]
    [SerializeField] private bool enablePlayerScaling = true;
    [SerializeField] private float playerSpeedStep = 0.03f; // Onceki %2 -> Simdi %3
    [SerializeField] private float playerJumpStep = 0.015f;  // Onceki %1 -> Simdi %1.5
    [SerializeField] private float maxPlayerSpeedMultiplier = 1.25f; // Onceki 1.06 -> Simdi 1.25
    [SerializeField] private float maxPlayerJumpMultiplier = 1.15f;  // Onceki 1.04 -> Simdi 1.15

    [Header("Camera Scaling")]
    [SerializeField] private bool enableCameraScaling = true;
    [SerializeField] private float baseCameraSize = 7f;
    [SerializeField] private float cameraSizeStep = 0.15f; // Her stage biraz uzaklaşır
    [SerializeField] private float maxCameraSize = 9.5f;

    [Header("Game Speed (Optional)")]
    [SerializeField] private bool enableTimeScale = false;
    [SerializeField] private float timeScaleStep = 0.02f;
    [SerializeField] private float maxTimeScale = 1.15f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraController cameraController;

    private float currentTiltSignedDegrees = 0f;
    private float currentPlayerSpeedMultiplier = 1f;
    private float currentPlayerJumpMultiplier = 1f;
    private float currentTrapSpeedMultiplier = 1f;
    private float currentCameraSize = 7f;
    private int currentWave = 0;                                  // Hangi dalgadayız

    private float baseFixedDeltaTime;

    public int StageIndex => stageIndex;
    public float CurrentTiltDegrees => currentTiltSignedDegrees;
    public float PlayerSpeedMultiplier => currentPlayerSpeedMultiplier;
    public float PlayerJumpMultiplier => currentPlayerJumpMultiplier;
    public float TrapSpeedMultiplier => currentTrapSpeedMultiplier;
    public int CurrentWave => currentWave;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {
        if (playerMovement == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerMovement = playerObj.GetComponent<PlayerMovement>();
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }
        
        // Initial setup
        currentCameraSize = baseCameraSize;

        // Checkpoint varsa oradan yükle, yoksa sıfırdan başla
        if (CheckpointData.HasProgressionState)
        {
            LoadFromCheckpoint(CheckpointData.SavedStageIndex, CheckpointData.SavedWorldAngleDegrees);
        }
        else
        {
            // Yeni oyun - her şeyi sıfırla
            stageIndex = 0;
            ResetProgression();
        }
    }

    /// <summary>
    /// Sadece ileri progress için çağır (örn: nextRoom).
    /// </summary>
    public void AdvanceStage()
    {
        stageIndex++;

        // Tilt: üçgen dalga (artan -> azalan -> artan)
        // Örn stagesPerWave=3 ve step=1.75 => stage: 1:-1.75, 2:-3.5, 3:-5.25, 4:-3.5, 5:-1.75, 6:0, 7:-1.75 ...
        if (enableTiltProgression)
        {
            int peakSteps = Mathf.Max(1, stagesPerWave);
            int cycleLen = peakSteps * 2; // triangle period
            int m = stageIndex % cycleLen; // 0..cycleLen-1
            int k = (m <= peakSteps) ? m : (cycleLen - m); // 0..peakSteps

            float sign = Mathf.Sign(tiltDirectionSign == 0 ? -1f : tiltDirectionSign);
            float targetAbs = Mathf.Clamp(k * tiltStepDegrees, 0f, maxTiltDegrees);
            float targetSigned = targetAbs * sign;
            float delta = targetSigned - currentTiltSignedDegrees;

            currentTiltSignedDegrees = targetSigned;

            if (WorldRotationManager.Instance != null && Mathf.Abs(delta) > 0.01f)
            {
                // Her geçişte güçlü deprem (Duration: 1.5s, Intensity: 0.2f)
                WorldRotationManager.Instance.RotateByAngle(delta, 0.2f, 1.5f);
            }

            // Wave sayacı: her peakSteps stage'de bir tepe görülür (artan fazın sonu)
            // Bu sadece debug/presentasyon için.
            currentWave = stageIndex / peakSteps;
        }

        // Hız SÜREKLİ artar (dalga sıfırlamaz)
        if (enableSpeedScaling)
        {
            // Hiz formulu: (1 + stage * 0.05) + (wave * 0.1)
            float baseSpeed = 1f + (stageIndex * speedStep);
            float waveBonus = currentWave * waveDifficultyMultiplier;
            currentTrapSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, baseSpeed + waveBonus);
        }

        if (enablePlayerScaling)
        {
            currentPlayerSpeedMultiplier = Mathf.Min(maxPlayerSpeedMultiplier, 1f + stageIndex * playerSpeedStep);
            currentPlayerJumpMultiplier = Mathf.Min(maxPlayerJumpMultiplier, 1f + stageIndex * playerJumpStep);
        }

        if (enableCameraScaling)
        {
            float targetSize = baseCameraSize + (stageIndex * cameraSizeStep);
            
            // Wave basina ekstra zoom-out (daha hizli oldugu icin daha genis gorus lazim)
            targetSize += currentWave * 0.5f; 
            
            currentCameraSize = Mathf.Min(maxCameraSize, targetSize);
        }

        if (enableTimeScale)
        {
            float target = Mathf.Min(maxTimeScale, 1f + stageIndex * timeScaleStep);
            Time.timeScale = target;
            Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
        }

        ApplyAll();
        
        // Debug ve Sunum gorsellestirme
        if (StageDebugDisplay.Instance != null)
        {
            StageDebugDisplay.Instance.TriggerStageChangeEffect(stageIndex);
        }
        
        Debug.Log($"Stage {stageIndex} | Wave {currentWave} | Tilt: {currentTiltSignedDegrees:F1}° | Speed: x{currentTrapSpeedMultiplier:F2}");
    }

    public void LoadFromCheckpoint(int savedStageIndex, float savedWorldAngleDegrees)
    {
        stageIndex = Mathf.Max(0, savedStageIndex);
        currentWave = stageIndex / stagesPerWave;

        if (enablePlayerScaling)
        {
            currentPlayerSpeedMultiplier = Mathf.Min(maxPlayerSpeedMultiplier, 1f + stageIndex * playerSpeedStep);
            currentPlayerJumpMultiplier = Mathf.Min(maxPlayerJumpMultiplier, 1f + stageIndex * playerJumpStep);
        }

        if (enableCameraScaling)
        {
            float targetSize = baseCameraSize + (stageIndex * cameraSizeStep);
            targetSize += currentWave * 0.5f; 
            currentCameraSize = Mathf.Min(maxCameraSize, targetSize);
        }

        if (enableSpeedScaling)
            currentTrapSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, 1f + stageIndex * speedStep);

        if (enableTiltProgression)
            currentTiltSignedDegrees = savedWorldAngleDegrees;

        if (WorldRotationManager.Instance != null && enableTiltProgression)
            WorldRotationManager.Instance.SetRotationInstant(savedWorldAngleDegrees);

        if (enableTimeScale)
        {
            float target = Mathf.Min(maxTimeScale, 1f + stageIndex * timeScaleStep);
            Time.timeScale = target;
            Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
        }

        ApplyAll();
    }

    public void ResetProgression()
    {
        stageIndex = 0;
        currentWave = 0;
        currentTiltSignedDegrees = 0f;
        currentPlayerSpeedMultiplier = 1f;
        currentPlayerJumpMultiplier = 1f;
        currentTrapSpeedMultiplier = 1f;
        currentCameraSize = baseCameraSize;

        if (enableTimeScale)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }

        if (WorldRotationManager.Instance != null)
            WorldRotationManager.Instance.ResetRotation();

        ApplyAll();
    }

    private void ApplyAll()
    {
        if (enablePlayerScaling && playerMovement != null)
            playerMovement.SetMovementMultipliers(currentPlayerSpeedMultiplier, currentPlayerJumpMultiplier);

        if (enableCameraScaling && cameraController != null)
        {
            cameraController.SetZoom(currentCameraSize);
        }
    }
}
