using UnityEngine;

public static class CheckpointData
{
    public static Vector3 LastCheckpointPosition;
    public static bool HasCheckpoint = false;

    public static void ResetData()
    {
        HasCheckpoint = false;
        LastCheckpointPosition = Vector3.zero;
    }
}
