using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonColorController : MonoBehaviour
{
    public Color selectedColor = Color.green;    // Color when button is selected
    public Color defaultColor = Color.white;     // Default color for buttons
    public Button[] buttons;                     // List of buttons in the window

    private Button selectedButton;

    void Start()
    {
        // Initialize all buttons with default color
        foreach (Button button in buttons)
        {
            SetButtonColor(button, defaultColor);
            button.onClick.AddListener(() => OnButtonClicked(button));
        }
    }

    void OnButtonClicked(Button clickedButton)
    {
        if (selectedButton != null)
        {
            // Reset the previously selected button's color
            SetButtonColor(selectedButton, defaultColor);
        }

        // Set the clicked button as the new selected button
        selectedButton = clickedButton;
        SetButtonColor(selectedButton, selectedColor);
    }

    void SetButtonColor(Button button, Color color)
    {
        // Change the button's background color
        ColorBlock cb = button.colors;
        cb.selectedColor = color;
        cb.normalColor = color;
        button.colors = cb;
    }
}
