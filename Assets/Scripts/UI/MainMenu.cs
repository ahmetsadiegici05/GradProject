
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
	public void PlayGame()
	{
        // Yeni oyuna başlarken checkpoint verisini sıfırla
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

