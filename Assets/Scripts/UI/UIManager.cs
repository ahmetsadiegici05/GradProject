using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
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
    [Header("Audio Settings")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isPaused;
    
    // Public property'ler
    public bool IsPaused => isPaused;
    public bool IsGameOver => gameOverScreen != null && gameOverScreen.activeSelf;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (pauseScreen != null)
            pauseScreen.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        SetupAudio();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void GameOver()
    {
        if (SoundManager.instance != null)
            SoundManager.instance.PlaySound(SoundManager.instance.loseSound);

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

    private void SetupAudio()
    {
        if (musicSlider != null)
        {
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            musicSlider.value = savedMusic;
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxSlider != null)
        {
            float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.value = savedSFX;
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetVolume(value);
        }

        if (AmbientSound.Instance != null)
        {
            AmbientSound.Instance.SetVolume(value);
        }

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetVolume(value);
        }
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
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
