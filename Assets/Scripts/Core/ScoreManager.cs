using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // İstatistikler
    private float distanceTraveled;
    private int enemiesKilled;
    private int highScore;

    // Takip için
    private Vector2 lastPosition;
    private Transform player;
    private bool isTracking;

    // Public erişim
    public float DistanceTraveled => distanceTraveled;
    public int EnemiesKilled => enemiesKilled;
    public int TotalScore => CalculateScore();
    public int HighScore => highScore;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void Start()
    {
        FindPlayer();
        ResetStats();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            lastPosition = player.position;
        }
    }

    private void Update()
    {
        if (!isTracking || player == null)
            return;

        // Mesafe hesapla (sadece ileri gidiş)
        float horizontalDistance = player.position.x - lastPosition.x;
        if (horizontalDistance > 0)
        {
            distanceTraveled += horizontalDistance;
        }
        lastPosition = player.position;
    }

    private int CalculateScore()
    {
        return Mathf.RoundToInt(distanceTraveled * 10) + (enemiesKilled * 100);
    }

    public void AddEnemyKill()
    {
        enemiesKilled++;
    }

    public void StopTracking()
    {
        isTracking = false;

        int currentScore = CalculateScore();
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    public void SaveToLeaderboard(string playerName)
    {
        int currentScore = CalculateScore();
        LeaderboardData.AddScore(playerName, currentScore);
    }

    public void ResetStats()
    {
        distanceTraveled = 0;
        enemiesKilled = 0;
        isTracking = true;

        if (player != null)
            lastPosition = player.position;
    }
}
