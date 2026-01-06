using UnityEngine;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Leaderboard Entries")]
    [SerializeField] private TextMeshProUGUI entry1Text;
    [SerializeField] private TextMeshProUGUI entry2Text;
    [SerializeField] private TextMeshProUGUI entry3Text;

    [Header("Format")]
    [SerializeField] private string entryFormat = "{0}. {1} - {2}";

    private void OnEnable()
    {
        UpdateLeaderboard();
    }

    public void UpdateLeaderboard()
    {
        LeaderboardEntry[] entries = LeaderboardData.GetEntries();

        if (entry1Text != null)
            entry1Text.text = FormatEntry(1, entries[0]);

        if (entry2Text != null)
            entry2Text.text = FormatEntry(2, entries[1]);

        if (entry3Text != null)
            entry3Text.text = FormatEntry(3, entries[2]);
    }

    private string FormatEntry(int rank, LeaderboardEntry entry)
    {
        if (entry.score == 0)
            return string.Format(entryFormat, rank, "---", "---");

        return string.Format(entryFormat, rank, entry.name, entry.score);
    }
}
