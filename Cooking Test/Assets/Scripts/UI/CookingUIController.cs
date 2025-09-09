using System;
using System.Collections.Generic;
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

    [Header("Star Filter")]
    [SerializeField] private TMP_Dropdown starDropdown;

    [Header("Search")]
    [SerializeField] private TMP_InputField searchInput;

    private RecipeData selectedRecipe;
    private int selectedStarFilter = 0; // 0 = ALL
    private int maxStar = 3; // will be determined dynamically
    private string searchQuery = "";

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

        // Subscribe event when recipes are loaded
        cookingManager.RecipeLoader.OnRecipesLoaded += OnRecipesLoaded;

        // Setup buttons
        if (cookButton != null) cookButton.onClick.AddListener(OnCookButton);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseButton);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeButton);

        // Setup star dropdown
        if (starDropdown != null)
            starDropdown.onValueChanged.AddListener(OnStarFilterChanged);

        // Setup search input
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchValueChanged);

        // If recipes already loaded, populate immediately
        if (cookingManager.RecipeLoader.recipeCollection != null &&
            cookingManager.RecipeLoader.recipeCollection.recipes.Length > 0)
        {
            OnRecipesLoaded();
        }

        UpdateButtonState(cookingManager.isCooking);
        UpdateTimerText(cookingManager.RemainingTime);
    }

    private void OnRecipesLoaded()
    {
        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        if (recipes == null || recipes.Length == 0) return;

        // Determine max star dynamically
        maxStar = 0;
        foreach (var recipe in recipes)
        {
            if (recipe.starRating > maxStar) maxStar = recipe.starRating;
        }

        SetupStarDropdown();
        PopulateRecipesUI();
    }

    private void SetupStarDropdown()
    {
        if (starDropdown == null) return;

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("ALL")); // index 0 = ALL
        for (int i = maxStar; i >= 1; i--)
        {
            options.Add(new TMP_Dropdown.OptionData(i + " STAR"));
        }

        starDropdown.ClearOptions();
        starDropdown.AddOptions(options);
        starDropdown.value = 0; // default ALL
    }

    private void PopulateRecipesUI()
    {
        foreach (Transform child in recipeListContainer)
            Destroy(child.gameObject);

        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        foreach (var recipe in recipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListContainer);
            RecipeView view = btnObj.GetComponent<RecipeView>();
            if (view != null)
            {
                view.SetData(recipe);
                view.OnRecipeSelected += OnSelectRecipe;
            }
        }

        UpdateRecipeUIVisibility();
    }

    private void OnStarFilterChanged(int index)
    {
        if (index == 0)
            selectedStarFilter = 0; // ALL
        else
            selectedStarFilter = maxStar - (index - 1);

        UpdateRecipeUIVisibility();
    }

    private void OnSearchValueChanged(string query)
    {
        searchQuery = query.Trim().ToLower();
        UpdateRecipeUIVisibility();
    }

    private void UpdateRecipeUIVisibility()
    {
        foreach (Transform child in recipeListContainer)
        {
            RecipeView view = child.GetComponent<RecipeView>();
            if (view != null)
            {
                bool matchesStar = selectedStarFilter == 0 || view.RecipeData.starRating == selectedStarFilter;
                bool matchesSearch = string.IsNullOrEmpty(searchQuery) || view.RecipeData.recipeName.ToLower().Contains(searchQuery);
                child.gameObject.SetActive(matchesStar && matchesSearch);
            }
        }
    }

    private void OnSelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        UpdateIngredientUI(recipe);
    }
    private void UpdateIngredientUI(RecipeData recipe)
    {
        foreach (Transform child in ingredientListContainer)
            Destroy(child.gameObject);

        foreach (var ing in recipe.ingredients)
        {
            GameObject slot = Instantiate(ingredientPrefab, ingredientListContainer);
            TMP_Text txt = slot.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                int playerAmount = cookingManager.Inventory.GetItemCount(ing.id);
                string colorStart = playerAmount < ing.amount ? "<color=#FF0000>" : ""; // ·¥ß∂È“‰¡ËæÕ
                string colorEnd = playerAmount < ing.amount ? "</color>" : "";
                txt.text = $"{colorStart}{playerAmount}{colorEnd}/{ing.amount}";
            }
        }
    }

    private void OnCookButton()
    {
        if (selectedRecipe == null) return;
        cookingManager.StartCooking(selectedRecipe);
    }

    private void OnPauseButton() => cookingManager.PauseCooking();
    private void OnResumeButton() => cookingManager.ResumeCooking();

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
        UpdateTimerText(0);
        UpdateButtonState(false);
    }

}
