using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CookingUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CookingManager cookingManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button cookButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    private void OnEnable()
    {
        if (cookingManager != null)
        {
            cookingManager.OnCookingTimeChanged += UpdateTimerText;
            cookingManager.OnCookingStateChanged += UpdateButtonState;
            cookingManager.OnCookingFinished += OnCookingFinished;
        }
    }

    private void OnDisable()
    {
        if (cookingManager != null)
        {
            cookingManager.OnCookingTimeChanged -= UpdateTimerText;
            cookingManager.OnCookingStateChanged -= UpdateButtonState;
            cookingManager.OnCookingFinished -= OnCookingFinished;
        }
    }

    private void Start()
    {
        if (cookButton != null)
            cookButton.onClick.AddListener(() => cookingManager.CookSelectedRecipe());

        if (pauseButton != null)
            pauseButton.onClick.AddListener(() => cookingManager.PauseCooking());

        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => cookingManager.ResumeCooking());

        // Update initial UI based on the manager's current state
        UpdateButtonState(cookingManager.isCooking);
        UpdateTimerText(cookingManager.RemainingTime);
    }

    private void UpdateTimerText(int remainingSeconds)
    {
        if (timerText != null)
        {
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void UpdateButtonState(bool isCooking)
    {
        bool isPaused = cookingManager.isPaused; // Get the paused state directly

        if (cookButton != null)
            cookButton.interactable = !isCooking;

        // Pause button is interactable only when cooking and not paused
        if (pauseButton != null)
            pauseButton.interactable = isCooking && !isPaused;

        // Resume button is interactable only when cooking and paused
        if (resumeButton != null)
            resumeButton.interactable = isCooking && isPaused;
    }

    private void OnCookingFinished(RecipeData recipe)
    {
        UpdateTimerText(0);
        UpdateButtonState(false);
    }
}