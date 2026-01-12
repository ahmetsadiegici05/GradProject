using UnityEngine;

/// <summary>
/// [DEPRECATED] Bu script artık kullanılmıyor.
/// Küçük eğim değişimleri ProgressionManager tarafından yönetiliyor.
/// Bu dosya güvenli bir şekilde silinebilir.
/// </summary>
[System.Obsolete("RotationTrigger artık kullanılmıyor. Küçük eğimler ProgressionManager ile yönetiliyor.")]
public class RotationTrigger : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning($"RotationTrigger ({gameObject.name}) artık kullanılmıyor. Bu component'i kaldırın.");
        enabled = false;
    }
}
