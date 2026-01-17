using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Start()
    {
        // Eğer daha önce bir checkpoint alındıysa, oyuncuyu oraya taşı
        if (CheckpointData.HasCheckpoint)
        {
            // ÖNCE dünya rotasyonunu yükle (yerçekimi değişir)
            if (ProgressionManager.Instance != null && CheckpointData.HasProgressionState)
            {
                ProgressionManager.Instance.LoadFromCheckpoint(
                    CheckpointData.SavedStageIndex,
                    CheckpointData.SavedWorldAngleDegrees
                );
            }
            
            // SONRA oyuncu pozisyonunu ayarla
            transform.position = CheckpointData.LastCheckpointPosition;
            
            // Kamerayı anında oyuncuya taşı
            CameraController cam = FindFirstObjectByType<CameraController>();
            if (cam != null)
            {
                cam.SnapToPlayer();
            }
            
            // KillOnFall referans noktasını güncelle (rotation'dan SONRA)
            KillOnFall killOnFall = GetComponent<KillOnFall>();
            if (killOnFall != null)
            {
                // Bir frame bekle ki fizik güncellensń
                StartCoroutine(DelayedResetFallTracking(killOnFall));
            }
        }
        else
        {
            // Checkpoint yoksa sadece KillOnFall'u resetle
            KillOnFall killOnFall = GetComponent<KillOnFall>();
            if (killOnFall != null)
                killOnFall.ResetFallTracking();
        }
    }
    
    private System.Collections.IEnumerator DelayedResetFallTracking(KillOnFall killOnFall)
    {
        // Bir frame bekle ki fizik ve pozisyonlar güncellensin
        yield return null;
        yield return new WaitForFixedUpdate();
        
        killOnFall.ResetFallTracking();
    }
}
