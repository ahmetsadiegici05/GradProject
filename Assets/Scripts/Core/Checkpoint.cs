using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Color activeColor = Color.green;
    private bool activated;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !activated)
        {
            activated = true;
            
            // Pozisyonu kaydet
            CheckpointData.LastCheckpointPosition = transform.position;
            CheckpointData.HasCheckpoint = true;
            
            // Görsel geri bildirim (Rengi değiştir)
            if (spriteRenderer != null)
                spriteRenderer.color = activeColor;
                
            Debug.Log("Checkpoint Activated!");
        }
    }
}
