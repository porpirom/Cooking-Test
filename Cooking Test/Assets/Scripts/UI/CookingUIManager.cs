using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CookingUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CookingManager cookingManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button cookButton;

    [Header("Inspector Recipe Selection")]
    [SerializeField] private int recipeIndex = 0;

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

        // Update UI ทันทีจาก CookingManager state
        if (cookingManager != null)
        {
            UpdateTimerText(cookingManager.RemainingTime);
            UpdateButtonState(cookingManager.IsCooking);
        }
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
        if (cookButton != null)
            cookButton.interactable = !isCooking;
    }

    private void OnCookingFinished(RecipeData recipe)
    {
        // อัปเดต UI ทันทีเมื่อ cooking เสร็จ
        UpdateTimerText(0);
        UpdateButtonState(false);
    }
}
