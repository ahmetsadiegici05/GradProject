using UnityEngine;

public static class LeaderboardData
{
    private const int MAX_ENTRIES = 3;
    private const string SCORE_KEY = "Leaderboard_Score_";
    private const string NAME_KEY = "Leaderboard_Name_";

    public static void AddScore(string playerName, int score)
    {
        // Mevcut skorları al
        LeaderboardEntry[] entries = GetEntries();

        // Yeni skoru ekle ve sırala
        LeaderboardEntry newEntry = new LeaderboardEntry(playerName, score);
        
        // En düşük skordan büyükse ekle
        for (int i = 0; i < MAX_ENTRIES; i++)
        {
            if (score > entries[i].score)
            {
                // Kaydır ve ekle
                for (int j = MAX_ENTRIES - 1; j > i; j--)
                {
                    entries[j] = entries[j - 1];
                }
                entries[i] = newEntry;
                break;
            }
        }

        // Kaydet
        SaveEntries(entries);
    }

    public static LeaderboardEntry[] GetEntries()
    {
        LeaderboardEntry[] entries = new LeaderboardEntry[MAX_ENTRIES];

        for (int i = 0; i < MAX_ENTRIES; i++)
        {
            string name = PlayerPrefs.GetString(NAME_KEY + i, "---");
            int score = PlayerPrefs.GetInt(SCORE_KEY + i, 0);
            entries[i] = new LeaderboardEntry(name, score);
        }

        return entries;
    }

    private static void SaveEntries(LeaderboardEntry[] entries)
    {
        for (int i = 0; i < MAX_ENTRIES; i++)
        {
            PlayerPrefs.SetString(NAME_KEY + i, entries[i].name);
            PlayerPrefs.SetInt(SCORE_KEY + i, entries[i].score);
        }
        PlayerPrefs.Save();
    }

    public static void ClearLeaderboard()
    {
        for (int i = 0; i < MAX_ENTRIES; i++)
        {
            PlayerPrefs.DeleteKey(NAME_KEY + i);
            PlayerPrefs.DeleteKey(SCORE_KEY + i);
        }
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public struct LeaderboardEntry
{
    public string name;
    public int score;

    public LeaderboardEntry(string name, int score)
    {
        this.name = name;
        this.score = score;
    }
}
