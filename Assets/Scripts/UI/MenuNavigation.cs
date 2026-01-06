using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuNavigation : MonoBehaviour
{
    [Header("First Selected Button")]
    [SerializeField] private Button firstSelectedButton;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color highlightedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupButtons = true;

    private void OnEnable()
    {
        if (autoSetupButtons)
            SetupAllButtons();

        SelectFirstButton();
    }

    private void SetupAllButtons()
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

    public void SelectFirstButton()
    {
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
    }
}
