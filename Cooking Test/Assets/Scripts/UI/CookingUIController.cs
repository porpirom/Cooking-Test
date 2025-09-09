using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CookingManager cookingManager;

    [Header("Recipe UI")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeButtonPrefab;

    [Header("Ingredient UI")]
    [SerializeField] private Transform ingredientListContainer;
    [SerializeField] private GameObject ingredientPrefab;

    [Header("Cooking Controls")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button cookButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    private RecipeData selectedRecipe;

    private void OnEnable()
    {
        if (cookingManager == null) return;

        cookingManager.OnCookingTimeChanged += UpdateTimerText;
        cookingManager.OnCookingStateChanged += UpdateButtonState;
        cookingManager.OnCookingFinished += OnCookingFinished;
    }

    private void OnDisable()
    {
        if (cookingManager == null) return;

        cookingManager.OnCookingTimeChanged -= UpdateTimerText;
        cookingManager.OnCookingStateChanged -= UpdateButtonState;
        cookingManager.OnCookingFinished -= OnCookingFinished;
    }

    private void Start()
    {
        if (cookingManager == null || cookingManager.RecipeLoader == null)
        {
            Debug.LogError("[CookingUI] CookingManager or RecipeLoader missing!");
            return;
        }

        // Subscribe event
        cookingManager.RecipeLoader.OnRecipesLoaded += PopulateRecipesUI;

        // ถ้า recipeCollection โหลดแล้ว ให้ populate ทันที
        if (cookingManager.RecipeLoader.recipeCollection != null &&
            cookingManager.RecipeLoader.recipeCollection.recipes.Length > 0)
        {
            PopulateRecipesUI();
        }

        // ตั้งค่าปุ่ม cook/pause/resume
        if (cookButton != null) cookButton.onClick.AddListener(OnCookButton);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseButton);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeButton);

        // อัปเดต UI เริ่มต้น
        UpdateButtonState(cookingManager.isCooking);
        UpdateTimerText(cookingManager.RemainingTime);
    }

    private void PopulateRecipesUI()
    {
        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        Debug.Log($"[CookingUI] Recipes count: {recipes.Length}");

        foreach (var recipe in recipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListContainer);
            RecipeView view = btnObj.GetComponent<RecipeView>();
            if (view != null)
            {
                view.SetData(recipe);
                view.OnRecipeSelected += OnSelectRecipe; // subscribe event
            }
            else
            {
                Debug.LogWarning("[CookingUI] recipeButtonPrefab missing RecipeView component!");
            }
        }
    }


    private void OnSelectRecipe(RecipeData recipe)
    {
        Debug.Log($"[CookingUI] Selected recipe: {recipe.recipeName}");
        selectedRecipe = recipe;
        UpdateIngredientUI(recipe);
    }

    private void UpdateIngredientUI(RecipeData recipe)
    {
        Debug.Log($"[CookingUI] Updating ingredients for {recipe.recipeName}");

        foreach (Transform child in ingredientListContainer)
            Destroy(child.gameObject);

        foreach (var ing in recipe.ingredients)
        {
            Debug.Log($"[CookingUI] Adding ingredient {ing.id} x {ing.amount}");
            GameObject slot = Instantiate(ingredientPrefab, ingredientListContainer);
            TMP_Text txt = slot.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{ing.id} x {ing.amount}";
            else
                Debug.LogWarning("[CookingUI] ingredientPrefab missing TMP_Text!");
        }
    }

    private void OnCookButton()
    {
        if (selectedRecipe == null)
        {
            Debug.Log("[CookingUI] No recipe selected");
            return;
        }
        Debug.Log($"[CookingUI] Cooking {selectedRecipe.recipeName}");
        cookingManager.StartCooking(selectedRecipe);
    }


    private void OnPauseButton()
    {
        cookingManager.PauseCooking();
    }

    private void OnResumeButton()
    {
        cookingManager.ResumeCooking();
    }

    private void UpdateTimerText(int remainingSeconds)
    {
        if (timerText == null) return;

        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateButtonState(bool isCooking)
    {
        bool isPaused = cookingManager.isPaused;

        if (cookButton != null) cookButton.interactable = !isCooking;
        if (pauseButton != null) pauseButton.interactable = isCooking && !isPaused;
        if (resumeButton != null) resumeButton.interactable = isCooking && isPaused;
    }

    private void OnCookingFinished(RecipeData recipe)
    {
        Debug.Log($"[CookingUI] Finished cooking {recipe.recipeName}");
        UpdateTimerText(0);
        UpdateButtonState(false);
    }
}
