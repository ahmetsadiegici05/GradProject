using UnityEngine;
using System;
using System.IO;

public class ScreenshotMode : MonoBehaviour
{
    public static ScreenshotMode Instance;
    
    // Global flag for other scripts to check
    public static bool IsHudHidden { get; private set; } = false;

    [Header("Assign Manually or Auto-Find")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Canvas mainCanvas;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Try to auto-find if not assigned
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");
            
        // Usually the main UI has the "UIManager" or is a root Canvas
        if (mainCanvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    mainCanvas = c;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // Toggle HUD and Player (H)
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleMode();
        }

        // Take Screenshot (K)
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeScreenshot();
        }
    }

    private void ToggleMode()
    {
        IsHudHidden = !IsHudHidden;
        
        // Hide/Show Player Visuals (keep logic running)
        if (playerObject != null)
        {
            SpriteRenderer[] renderers = playerObject.GetComponentsInChildren<SpriteRenderer>();
            foreach(var sr in renderers)
            {
                sr.enabled = !IsHudHidden;
            }
        }

        // Hide/Show Canvas
        if (mainCanvas != null)
        {
            mainCanvas.enabled = !IsHudHidden;
        }

        // Debug messages (OnGUI) in other scripts should check ScreenshotMode.IsHudHidden
    }

    private void TakeScreenshot()
    {
        string folderPath = Path.Combine(Application.dataPath, "../Screenshots");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(folderPath, fileName);
        
        // SuperSize 2 means 2x resolution
        ScreenCapture.CaptureScreenshot(fullPath, 2); 
        Debug.Log($"Screenshot saved to: {fullPath}");
    }
}
