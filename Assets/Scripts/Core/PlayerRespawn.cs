using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Start()
    {
        // Eğer daha önce bir checkpoint alındıysa, oyuncuyu oraya taşı
        if (CheckpointData.HasCheckpoint)
        {
            transform.position = CheckpointData.LastCheckpointPosition;

            // Progression / rotation geri yükle
            if (ProgressionManager.Instance != null && CheckpointData.HasProgressionState)
            {
                ProgressionManager.Instance.LoadFromCheckpoint(
                    CheckpointData.SavedStageIndex,
                    CheckpointData.SavedWorldAngleDegrees
                );
            }
        }

        // KillOnFall referans noktasını güncelle
        KillOnFall killOnFall = GetComponent<KillOnFall>();
        if (killOnFall != null)
            killOnFall.ResetFallTracking();
    }
}
