using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Leaderboard Save")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private GameObject saveScoreButton;
    [SerializeField] private TextMeshProUGUI savedMessageText;

    [Header("Pause")]
    [SerializeField] private GameObject pauseScreen;

    private bool isPaused;

    private void Awake()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (pauseScreen != null)
            pauseScreen.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void GameOver()
    {
        // Skor takibini durdur ve istatistikleri göster
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopTracking();
            UpdateGameOverStats();
        }

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Oyun dursun; UI butonları çalışmaya devam eder.
        Time.timeScale = 0f;
    }

    private void UpdateGameOverStats()
    {
        if (ScoreManager.Instance == null)
            return;

        if (distanceText != null)
            distanceText.text = $"Distance: {ScoreManager.Instance.DistanceTraveled:F1} m";

        if (enemiesKilledText != null)
            enemiesKilledText.text = $"Enemies Killed: {ScoreManager.Instance.EnemiesKilled}";

        if (scoreText != null)
            scoreText.text = $"Score: {ScoreManager.Instance.TotalScore}";

        // Kaydetme UI'ını sıfırla
        if (saveScoreButton != null)
            saveScoreButton.SetActive(true);

        if (savedMessageText != null)
            savedMessageText.gameObject.SetActive(false);

        if (playerNameInput != null)
        {
            playerNameInput.text = "";
            playerNameInput.interactable = true;
        }
    }

    public void SaveScoreToLeaderboard()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("ScoreManager bulunamadı!");
            return;
        }

        string playerName = "Player";
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            playerName = playerNameInput.text;
            Debug.Log($"İsim girildi: {playerName}");
        }
        else
        {
            Debug.LogWarning("PlayerNameInput bağlı değil veya boş!");
        }

        ScoreManager.Instance.SaveToLeaderboard(playerName);
        Debug.Log($"Skor kaydedildi: {playerName} - {ScoreManager.Instance.TotalScore}");

        // UI'ı güncelle
        if (saveScoreButton != null)
            saveScoreButton.SetActive(false);
        else
            Debug.LogWarning("SaveScoreButton bağlı değil!");

        if (playerNameInput != null)
            playerNameInput.interactable = false;

        if (savedMessageText != null)
        {
            savedMessageText.text = "Score Saved!";
            savedMessageText.gameObject.SetActive(true);
            Debug.Log("Score Saved mesajı gösterildi");
        }
        else
        {
            Debug.LogWarning("SavedMessageText bağlı değil!");
        }
    }

    public void TogglePause()
    {
        // Game Over varken pause açma
        if (gameOverScreen != null && gameOverScreen.activeSelf)
            return;

        if (pauseScreen == null)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (pauseScreen == null)
            return;

        isPaused = true;
        pauseScreen.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        isPaused = false;
        if (pauseScreen != null)
            pauseScreen.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // En son checkpoint'ten devam et
    public void ContinueFromCheckpoint()
    {
        isPaused = false;
        Time.timeScale = 1f;
        // Checkpoint verisi korunuyor, sadece sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // En baştan başla (checkpoint'i sıfırla)
    public void RestartFromBeginning()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // Gravity'yi sıfırla
        Physics2D.gravity = new Vector2(0f, -Mathf.Abs(Physics2D.gravity.magnitude > 0.1f ? Physics2D.gravity.magnitude : 9.81f));
        
        // Checkpoint verisini sıfırla
        CheckpointData.ResetData();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Restart()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // Gravity'yi sıfırla (eğimli kalmış olabilir)
        Physics2D.gravity = new Vector2(0f, -Mathf.Abs(Physics2D.gravity.magnitude > 0.1f ? Physics2D.gravity.magnitude : 9.81f));
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
