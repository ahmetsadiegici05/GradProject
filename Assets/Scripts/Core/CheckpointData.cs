using UnityEngine;

public static class CheckpointData
{
    public static Vector3 LastCheckpointPosition;
    public static bool HasCheckpoint = false;

    // Progression / rotation state (respawn'da geri yüklemek için)
    public static bool HasProgressionState = false;
    public static int SavedStageIndex = 0;
    public static float SavedWorldAngleDegrees = 0f;

    // Gameplay unlocks
    public static bool SpikeheadShootingUnlocked = false;

    public static void ResetData()
    {
        HasCheckpoint = false;
        LastCheckpointPosition = Vector3.zero;
        HasProgressionState = false;
        SavedStageIndex = 0;
        SavedWorldAngleDegrees = 0f;
        SpikeheadShootingUnlocked = false;
    }
}
