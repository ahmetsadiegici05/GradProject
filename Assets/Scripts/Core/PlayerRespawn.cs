using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Awake()
    {
        // En kritik fix: KillOnFall'u hemen devre dışı bırak
        // Boyece pozisyon ayarlanirken yanlislikla tetiklenmez
        KillOnFall kof = GetComponent<KillOnFall>();
        if (kof != null) kof.enabled = false;
    }

    private void Start()
    {
        // Eğer daha önce bir checkpoint alındıysa, oyuncuyu oraya taşı
        if (CheckpointData.HasCheckpoint)
        {
            Debug.Log($"[Respawn] Checkpoint found. Stage: {CheckpointData.SavedStageIndex}, Angle: {CheckpointData.SavedWorldAngleDegrees}");

            // 1. ÖNCE dünya rotasyonunu yükle 
            if (ProgressionManager.Instance != null && CheckpointData.HasProgressionState)
            {
                ProgressionManager.Instance.LoadFromCheckpoint(
                    CheckpointData.SavedStageIndex,
                    CheckpointData.SavedWorldAngleDegrees
                );
                
                Physics2D.SyncTransforms(); // Fizik dunyasini guncelle
            }
            
            // 2. SONRA oyuncu fizik hizlarini sifirla
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.Sleep(); // Uyut ki hareket etmesin
            }

            // 3. Pozisyonu ayarla (Guvenli yukseklik)
            Vector2 upDir = Vector2.up;
            if (WorldRotationManager.Instance != null)
                upDir = WorldRotationManager.UpDirection;

            Vector3 safePos = CheckpointData.LastCheckpointPosition + (Vector3)(upDir * 0.5f);
            transform.position = safePos;
            
            Physics2D.SyncTransforms(); // Tekrar guncelle
            
            if (rb != null) rb.WakeUp(); // Uyandir

            Debug.Log($"[Respawn] Player moved to {safePos}");

            // 4. Kamerayı anında oyuncuya taşı
            CameraController cam = FindFirstObjectByType<CameraController>();
            if (cam != null)
            {
                cam.SnapToPlayer();
            }
            
            // 5. KillOnFall'u guvenli bir sekilde tekrar aktif et
            KillOnFall killOnFall = GetComponent<KillOnFall>();
            if (killOnFall != null)
            {
                StartCoroutine(DelayedResetFallTracking(killOnFall));
            }
        }
        else
        {
            // Checkpoint yoksa
            KillOnFall killOnFall = GetComponent<KillOnFall>();
            if (killOnFall != null)
            {
                killOnFall.enabled = true; // Elimizle aciyoruz
                killOnFall.ResetFallTracking();
            }
        }
    }
    
    private System.Collections.IEnumerator DelayedResetFallTracking(KillOnFall killOnFall)
    {
        // Bir kac frame bekle ki fizik ve pozisyonlar tam otursun
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        
        // Simdi takibi baslat
        killOnFall.enabled = true;
        killOnFall.ResetFallTracking();
        Debug.Log("[Respawn] KillOnFall reactivated.");
    }
}
