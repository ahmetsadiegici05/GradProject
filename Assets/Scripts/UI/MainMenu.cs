using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
	[Header("Navigation")]
	[SerializeField] private Button firstSelectedButton;

	[Header("Selection Colors")]
	[SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
	[SerializeField] private Color highlightedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
	[SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
	[SerializeField] private Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

	private void Start()
	{
		SetupButtons();
		SelectFirstButton();

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		// DEBUG: Leaderboard'u göster
		LeaderboardEntry[] entries = LeaderboardData.GetEntries();
		for (int i = 0; i < entries.Length; i++)
		{
			Debug.Log($"Leaderboard {i + 1}: {entries[i].name} - {entries[i].score}");
		}
	}

	// Test için - Inspector'dan çağırabilirsin
	public void ClearLeaderboard()
	{
		LeaderboardData.ClearLeaderboard();
		Debug.Log("Leaderboard temizlendi!");
	}

	private void SetupButtons()
	{
		Button[] buttons = GetComponentsInChildren<Button>(true);

		foreach (Button btn in buttons)
		{
			ColorBlock colors = btn.colors;
			colors.normalColor = normalColor;
			colors.highlightedColor = highlightedColor;
			colors.pressedColor = pressedColor;
			colors.selectedColor = selectedColor;
			colors.colorMultiplier = 1f;
			btn.colors = colors;
		}
	}

	private void SelectFirstButton()
	{
		if (firstSelectedButton != null && EventSystem.current != null)
		{
			EventSystem.current.SetSelectedGameObject(null);
			EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
		}
	}

	public void PlayGame()
	{
		CheckpointData.ResetData();
		SceneManager.LoadScene("Level1");
	}

	public void QuitGame()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}
}

