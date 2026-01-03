using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverScreen;

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
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Oyun dursun; UI butonları çalışmaya devam eder.
        Time.timeScale = 0f;
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

    public void Restart()
    {
        isPaused = false;
        Time.timeScale = 1f;
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
