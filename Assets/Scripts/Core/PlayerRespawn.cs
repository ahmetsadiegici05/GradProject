using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Start()
    {
        // Eğer daha önce bir checkpoint alındıysa, oyuncuyu oraya taşı
        if (CheckpointData.HasCheckpoint)
        {
            transform.position = CheckpointData.LastCheckpointPosition;
        }
    }
}
