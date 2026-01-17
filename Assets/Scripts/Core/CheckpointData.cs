using UnityEngine;
using System.Collections.Generic;

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
    
    // Ölen düşmanların unique ID'leri (checkpoint anında ölen düşmanlar)
    public static HashSet<string> DeadEnemyIDs = new HashSet<string>();
    
    // Checkpoint anında ölen düşmanları kaydet
    public static void SaveDeadEnemies()
    {
        // Sahnedeki tüm EnemyHealth'leri bul (aktif olmayanlar dahil)
        var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy || enemy.IsDead)
            {
                DeadEnemyIDs.Add(enemy.GetUniqueID());
            }
        }
        
        Debug.Log($"Checkpoint: {DeadEnemyIDs.Count} ölen düşman kaydedildi.");
    }
    
    // Bir düşmanın checkpoint'te ölü olup olmadığını kontrol et
    public static bool IsEnemyDead(string uniqueID)
    {
        return DeadEnemyIDs.Contains(uniqueID);
    }

    public static void ResetData()
    {
        HasCheckpoint = false;
        LastCheckpointPosition = Vector3.zero;
        HasProgressionState = false;
        SavedStageIndex = 0;
        SavedWorldAngleDegrees = 0f;
        SpikeheadShootingUnlocked = false;
        DeadEnemyIDs.Clear();
    }
}
