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

    [Header("Tilt (Degrees)")]
    [SerializeField] private bool enableTiltProgression = true;
    [SerializeField] private float tiltStepDegrees = 1.75f;
    [SerializeField] private float maxTiltDegrees = 45f;
    [Tooltip("Saat yönünde artış için -1, saat yönünün tersine için +1")]
    [SerializeField] private float tiltDirectionSign = -1f;

    [Header("Player Scaling")]
    [SerializeField] private bool enablePlayerScaling = true;
    [SerializeField] private float playerSpeedStep = 0.07f; // %7
    [SerializeField] private float playerJumpStep = 0.04f;  // %4
    [SerializeField] private float maxPlayerSpeedMultiplier = 2.0f;
    [SerializeField] private float maxPlayerJumpMultiplier = 1.5f;

    [Header("Trap Scaling")]
    [SerializeField] private bool enableTrapScaling = true;
    [SerializeField] private float trapSpeedStep = 0.06f; // %6 - daha belirgin
    [SerializeField] private float maxTrapSpeedMultiplier = 1.8f;

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

    private float baseFixedDeltaTime;

    public int StageIndex => stageIndex;
    public float CurrentTiltDegrees => currentTiltSignedDegrees;
    public float PlayerSpeedMultiplier => currentPlayerSpeedMultiplier;
    public float PlayerJumpMultiplier => currentPlayerJumpMultiplier;
    public float TrapSpeedMultiplier => currentTrapSpeedMultiplier;

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

        if (enableTiltProgression)
        {
            float sign = Mathf.Sign(tiltDirectionSign == 0 ? -1f : tiltDirectionSign);
            float targetAbs = Mathf.Clamp(stageIndex * tiltStepDegrees, 0f, maxTiltDegrees);
            float targetSigned = targetAbs * sign;
            float delta = targetSigned - currentTiltSignedDegrees;

            currentTiltSignedDegrees = targetSigned;

            if (WorldRotationManager.Instance != null)
                WorldRotationManager.Instance.RotateByAngle(delta);
        }

        if (enablePlayerScaling)
        {
            currentPlayerSpeedMultiplier = Mathf.Min(maxPlayerSpeedMultiplier, 1f + stageIndex * playerSpeedStep);
            currentPlayerJumpMultiplier = Mathf.Min(maxPlayerJumpMultiplier, 1f + stageIndex * playerJumpStep);
        }

        if (enableTrapScaling)
        {
            currentTrapSpeedMultiplier = Mathf.Min(maxTrapSpeedMultiplier, 1f + stageIndex * trapSpeedStep);
        }

        if (enableTimeScale)
        {
            float target = Mathf.Min(maxTimeScale, 1f + stageIndex * timeScaleStep);
            Time.timeScale = target;
            Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
        }

        ApplyAll();
    }

    public void LoadFromCheckpoint(int savedStageIndex, float savedWorldAngleDegrees)
    {
        stageIndex = Mathf.Max(0, savedStageIndex);

        if (enablePlayerScaling)
        {
            currentPlayerSpeedMultiplier = Mathf.Min(maxPlayerSpeedMultiplier, 1f + stageIndex * playerSpeedStep);
            currentPlayerJumpMultiplier = Mathf.Min(maxPlayerJumpMultiplier, 1f + stageIndex * playerJumpStep);
        }

        if (enableTrapScaling)
            currentTrapSpeedMultiplier = Mathf.Min(maxTrapSpeedMultiplier, 1f + stageIndex * trapSpeedStep);

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
        // Debug hızlı test
        GUILayout.BeginArea(new Rect(10, 170, 240, 160));
        GUILayout.Label($"Stage: {stageIndex}");
        GUILayout.Label($"Tilt: {currentTiltSignedDegrees:0.0}°");
        GUILayout.Label($"PlayerSpeed x{currentPlayerSpeedMultiplier:0.00}");
        GUILayout.Label($"PlayerJump x{currentPlayerJumpMultiplier:0.00}");
        GUILayout.Label($"TrapSpeed x{currentTrapSpeedMultiplier:0.00}");
        if (enableTimeScale)
            GUILayout.Label($"TimeScale: {Time.timeScale:0.00}");

        if (GUILayout.Button("Advance Stage"))
            AdvanceStage();

        if (GUILayout.Button("Reset Progression"))
            ResetProgression();

        GUILayout.EndArea();
    }
}
