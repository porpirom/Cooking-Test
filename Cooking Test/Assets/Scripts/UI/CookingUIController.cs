using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CookingUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CookingManager cookingManager;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Recipe UI")]
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeButtonPrefab;

    [Header("Ingredient UI")]
    [SerializeField] private Transform ingredientListContainer;
    [SerializeField] private GameObject ingredientPrefab;

    [Header("Controls")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button cookButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown starDropdown;
    [SerializeField] private TMP_InputField searchInput;

    private RecipeData selectedRecipe;
    private int selectedStarFilter = 0;
    private int maxStar = 3;

    private void Start()
    {
        // Load ItemDatabase first
        itemDatabase.LoadFromJson(Application.streamingAssetsPath + "/items.json");

        cookButton.onClick.AddListener(OnCookButton);
        pauseButton.onClick.AddListener(OnPauseButton);
        resumeButton.onClick.AddListener(OnResumeButton);

        starDropdown.onValueChanged.AddListener(OnStarFilterChanged);
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        // Subscribe event ของ CookingManager
        cookingManager.OnCookingTimeChanged += UpdateTimerText;

        cookingManager.RecipeLoader.OnRecipesLoaded += OnRecipesLoaded;
        if (cookingManager.RecipeLoader.recipeCollection != null &&
            cookingManager.RecipeLoader.recipeCollection.recipes.Length > 0)
        {
            OnRecipesLoaded();
        }

        // อัปเดตเวลาเริ่มต้น
        UpdateTimerText(cookingManager.RemainingTime);
    }
    private void OnDisable()
    {
        if (cookingManager != null)
            cookingManager.OnCookingTimeChanged -= UpdateTimerText;
    }


    private void OnRecipesLoaded()
    {
        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        if (recipes == null || recipes.Length == 0) return;

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
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("ALL"));
        for (int i = maxStar; i >= 1; i--)
            options.Add(new TMP_Dropdown.OptionData(i + " STAR"));

        starDropdown.ClearOptions();
        starDropdown.AddOptions(options);
        starDropdown.value = 0;
    }

    private void PopulateRecipesUI()
    {
        foreach (Transform child in recipeListContainer)
            Destroy(child.gameObject);

        foreach (var recipe in cookingManager.RecipeLoader.recipeCollection.recipes)
        {
            GameObject btnObj = Instantiate(recipeButtonPrefab, recipeListContainer);
            RecipeView view = btnObj.GetComponent<RecipeView>();
            if (view != null)
            {
                Sprite icon = itemDatabase.GetIcon(recipe.resultId);
                if(icon == null)
                    Debug.LogWarning($"[CookingUIController] Icon not found for item ID: {recipe.resultId}");
                string displayName = itemDatabase.GetName(recipe.resultId);
                view.SetData(recipe, icon, displayName);
                view.OnRecipeSelected += OnSelectRecipe;
            }
        }

        UpdateRecipeUIVisibility();
    }

    private void OnStarFilterChanged(int index)
    {
        selectedStarFilter = (index == 0) ? 0 : maxStar - (index - 1);
        UpdateRecipeUIVisibility();
    }

    private void OnSearchChanged(string text)
    {
        UpdateRecipeUIVisibility();
    }

    private void UpdateRecipeUIVisibility()
    {
        string search = searchInput.text.ToLower();
        foreach (Transform child in recipeListContainer)
        {
            RecipeView view = child.GetComponent<RecipeView>();
            if (view == null) continue;

            bool starOk = (selectedStarFilter == 0 || view.RecipeData.starRating == selectedStarFilter);
            bool searchOk = string.IsNullOrEmpty(search) || view.RecipeData.recipeName.ToLower().Contains(search);
            child.gameObject.SetActive(starOk && searchOk);
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
            IngredientView view = slot.GetComponent<IngredientView>();
            if (view != null)
            {
                int playerAmount = cookingManager.Inventory.Items.GetValueOrDefault(ing.id, 0);
                Sprite icon = itemDatabase.GetIcon(ing.id);
                view.SetData(ing.id, playerAmount, ing.amount, icon);
            }
        }
    }

    private void OnCookButton() { if (selectedRecipe != null) cookingManager.StartCooking(selectedRecipe); }
    private void OnPauseButton() { cookingManager.PauseCooking(); }
    private void OnResumeButton() { cookingManager.ResumeCooking(); }

    private void UpdateTimerText(int remainingSeconds)
    {
        int m = remainingSeconds / 60;
        int s = remainingSeconds % 60;
        timerText.text = $"{m:00}:{s:00}";
    }
}
