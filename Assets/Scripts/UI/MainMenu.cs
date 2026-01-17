using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
	[Header("Navigation")]
	[SerializeField] private Button firstSelectedButton;

	[Header("Panels")]
	[SerializeField] private GameObject leaderboardPanel;
	[SerializeField] private GameObject menuContainer;

	[Header("Audio Settings")]
	[SerializeField] private Slider musicSlider;
	[SerializeField] private Slider sfxSlider;

	[Header("Selection Colors")]
	[SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
	[SerializeField] private Color highlightedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
	[SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
	[SerializeField] private Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

	private void Start()
	{
		if (leaderboardPanel != null)
			leaderboardPanel.SetActive(false);
			
		SetupButtons();
		SetupAudio();
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

	private void SetupAudio()
	{
		if (musicSlider != null)
		{
			float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
			musicSlider.value = savedMusic;
			ApplyMusicVolume(savedMusic);
			musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
		}

		if (sfxSlider != null)
		{
			float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
			sfxSlider.value = savedSFX;
			ApplySFXVolume(savedSFX);
			sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
		}
	}

	public void OnMusicVolumeChanged(float value)
	{
		ApplyMusicVolume(value);
		PlayerPrefs.SetFloat("MusicVolume", value);
		PlayerPrefs.Save();
	}

	public void OnSFXVolumeChanged(float value)
	{
		ApplySFXVolume(value);
		PlayerPrefs.SetFloat("SFXVolume", value);
		PlayerPrefs.Save();
	}

	private void ApplyMusicVolume(float value)
	{
		if (BackgroundMusic.Instance != null)
		{
			BackgroundMusic.Instance.SetVolume(value);
		}
	}

	private void ApplySFXVolume(float value)
	{
		if (SoundManager.instance != null)
		{
			SoundManager.instance.SetVolume(value);
		}
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

	private void Update()
	{
		if (leaderboardPanel != null && leaderboardPanel.activeSelf)
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				CloseLeaderboard();
			}
		}
	}

	public void OpenLeaderboard()
	{
		if (leaderboardPanel != null)
		{
			leaderboardPanel.SetActive(true);
			if (menuContainer != null)
				menuContainer.SetActive(false);
		}
	}

	public void CloseLeaderboard()
	{
		if (leaderboardPanel != null)
		{
			leaderboardPanel.SetActive(false);
			if (menuContainer != null)
				menuContainer.SetActive(true);
			
			SelectFirstButton();
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

