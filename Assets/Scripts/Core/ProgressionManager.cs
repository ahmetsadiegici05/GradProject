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
    [SerializeField] private float speedStep = 0.015f;            // %1.5 her stage
    [SerializeField] private float maxSpeedMultiplier = 1.5f;     // Max %50 hızlanma

    [Header("Player Scaling")]
    [SerializeField] private bool enablePlayerScaling = true;
    [SerializeField] private float playerSpeedStep = 0.02f; // %2
    [SerializeField] private float playerJumpStep = 0.01f;  // %1
    [SerializeField] private float maxPlayerSpeedMultiplier = 1.06f; // max %6
    [SerializeField] private float maxPlayerJumpMultiplier = 1.04f;  // max %4

    [Header("Game Speed (Optional)")]
    [SerializeField] private bool enableTimeScale = false;
    [SerializeField] private float timeScaleStep = 0.02f;
    [SerializeField] private float maxTimeScale = 1.15f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    private float currentTiltSignedDegrees = 0f;
    private float currentPlayerSpeedMultiplier = 1f;
    private float currentPlayerJumpMultiplier = 1f;
    private float currentTrapSpeedMultiplier = 1f;
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
                // Stage 0 -> 1 geçişinde (ilk eğim) daha güçlü bir deprem efekti ver
                if (stageIndex == 1)
                {
                    // Büyük deprem: Uzun süreli ve şiddetli
                    WorldRotationManager.Instance.RotateByAngle(delta, 0.2f, 1.5f);
                }
                else
                {
                    // Diğer geçişler: Kontrollü sarsıntı (Ekran dışına taşırmayan, mekaniği bozmayan)
                    // Intensity 0.08 (Hafif), Duration 0.5 (Kısa)
                    WorldRotationManager.Instance.RotateByAngle(delta, 0.08f, 0.5f);
                }
            }

            // Wave sayacı: her peakSteps stage'de bir tepe görülür (artan fazın sonu)
            // Bu sadece debug/presentasyon için.
            currentWave = stageIndex / peakSteps;
        }

        // Hız SÜREKLİ artar (dalga sıfırlamaz)
        if (enableSpeedScaling)
        {
            currentTrapSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, 1f + stageIndex * speedStep);
        }

        if (enablePlayerScaling)
        {
            currentPlayerSpeedMultiplier = Mathf.Min(maxPlayerSpeedMultiplier, 1f + stageIndex * playerSpeedStep);
            currentPlayerJumpMultiplier = Mathf.Min(maxPlayerJumpMultiplier, 1f + stageIndex * playerJumpStep);
        }

        if (enableTimeScale)
        {
            float target = Mathf.Min(maxTimeScale, 1f + stageIndex * timeScaleStep);
            Time.timeScale = target;
            Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
        }

        ApplyAll();
        
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
    }

    private void OnGUI()
    {
        if (ScreenshotMode.IsHudHidden) return;

        // Debug hızlı test
        GUILayout.BeginArea(new Rect(10, 170, 240, 180));
        GUILayout.Label($"Stage: {stageIndex} | Wave: {currentWave}");
        GUILayout.Label($"Tilt: {currentTiltSignedDegrees:0.0}° (max {maxTiltDegrees}°)");
        GUILayout.Label($"Speed x{currentTrapSpeedMultiplier:0.00}");
        GUILayout.Label($"PlayerSpeed x{currentPlayerSpeedMultiplier:0.00}");
        if (enableTimeScale)
            GUILayout.Label($"TimeScale: {Time.timeScale:0.00}");

        if (GUILayout.Button("Advance Stage"))
            AdvanceStage();

        if (GUILayout.Button("Reset Progression"))
            ResetProgression();

        GUILayout.EndArea();
    }
}
