using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class CookingUIController : MonoBehaviour
{
    #region Inspector References
    [Header("Managers")]
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
    [SerializeField] private Sprite cookButtonEnabledSprite;
    [SerializeField] private Sprite cookButtonDisabledSprite;

    [Header("Filters")]
    [SerializeField] private Transform starFilterContainer;
    [SerializeField] private GameObject starFilterButtonPrefab;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button toggleDropdownButton;

    [Header("Pagination")]
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;

    [Header("Page Indicators")]
    [SerializeField] private Transform pageIndicatorContainer;
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private Sprite indicatorNormalSprite;
    [SerializeField] private Sprite indicatorActiveSprite;

    [Header("Notifications")]
    [SerializeField] private GameObject itemReceivedPopup;
    [SerializeField] private Transform itemDisplayContainer;
    [SerializeField] private Button itemReceivedCloseButton;
    #endregion

    #region Private Fields
    private List<GameObject> pageIndicators = new List<GameObject>();
    private List<GameObject> starFilterButtons = new List<GameObject>();
    private List<GameObject> allRecipeButtons = new List<GameObject>();
    private List<GameObject> filteredRecipeButtons = new List<GameObject>();

    private Dictionary<int, Sprite> starSprites = new Dictionary<int, Sprite>();

    private int currentPage = 0;
    private int recipesPerPage = 4;
    private int selectedStarFilter = 0;
    private int maxStar = 3;

    private RecipeData selectedRecipe;
    #endregion

    #region Unity Methods
    private async void Start()
    {
        // Load items database
        itemDatabase.LoadFromJson(Application.streamingAssetsPath + "/items.json");

        // Button bindings
        cookButton.onClick.AddListener(OnCookButton);
        pauseButton.onClick.AddListener(OnPauseButton);
        resumeButton.onClick.AddListener(OnResumeButton);

        searchInput.onValueChanged.AddListener(OnSearchChanged);
        toggleDropdownButton.onClick.AddListener(ToggleStarFilterDropdown);

        nextPageButton.onClick.AddListener(OnNextPage);
        prevPageButton.onClick.AddListener(OnPrevPage);

        // Manager events
        cookingManager.OnCookingTimeChanged += UpdateTimerText;
        cookingManager.RecipeLoader.OnRecipesLoaded += OnRecipesLoaded;
        cookingManager.OnCookingStateChanged += OnCookingStateChanged;

        if (itemReceivedCloseButton != null)
            itemReceivedCloseButton.onClick.AddListener(OnCloseItemReceivedPopup);

        // Load recipes immediately if already available
        if (cookingManager.RecipeLoader.recipeCollection != null &&
            cookingManager.RecipeLoader.recipeCollection.recipes.Length > 0)
        {
            await OnRecipesLoadedAsync();
        }

        // Initialize UI state
        UpdateTimerText(cookingManager.RemainingTime);
        OnCookingStateChanged(!cookingManager.IsCooking);

        if (cookingManager.Inventory != null)
            cookingManager.Inventory.OnInventoryChanged += OnInventoryChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (cookingManager != null)
        {
            cookingManager.OnCookingTimeChanged -= UpdateTimerText;
            cookingManager.OnCookingStateChanged -= OnCookingStateChanged;
        }
        if (cookingManager.Inventory != null)
            cookingManager.Inventory.OnInventoryChanged -= OnInventoryChanged;
    }
    #endregion


    #region Inventory & Cooking
    private void OnInventoryChanged()
    {
        if (selectedRecipe != null)
        {
            UpdateIngredientUI(selectedRecipe);
            UpdateCookButtonState();
        }
    }

    private void UpdateCookButtonState()
    {
        if (selectedRecipe == null)
        {
            cookButton.interactable = false;
            cookButton.GetComponent<Image>().sprite = cookButtonDisabledSprite;
            return;
        }

        bool canCook = selectedRecipe.ingredients.All(ing =>
        {
            int playerAmount = cookingManager.Inventory.Items.GetValueOrDefault(ing.id, 0);
            return playerAmount >= ing.amount;
        });

        cookButton.interactable = canCook;
        cookButton.GetComponent<Image>().sprite = canCook ? cookButtonEnabledSprite : cookButtonDisabledSprite;
    }

    private void OnCookingStateChanged(bool isCooking)
    {
        cookButton.interactable = !isCooking;
        cookButton.GetComponent<Image>().sprite = isCooking ? cookButtonDisabledSprite : cookButtonEnabledSprite;
    }
    #endregion

    #region Recipe Loading
    private async void OnRecipesLoaded()
    {
        await OnRecipesLoadedAsync();
    }

    private async Task OnRecipesLoadedAsync()
    {
        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        if (recipes == null || recipes.Length == 0) return;

        maxStar = recipes.Max(r => r.starRating);
        await LoadStarSpritesAsync(maxStar);

        SetupStarFilterButtons();
        PopulateRecipesUI();
    }

    private async Task LoadStarSpritesAsync(int maxStar)
    {
        starSprites.Clear();

        for (int i = 1; i <= maxStar; i++)
        {
            string key = $"Stars/star_{i}";
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                starSprites[i] = handle.Result;
            else
                Debug.LogWarning($"Failed to load {key} from Addressables.");
        }
    }
    #endregion

    #region Filters
    private void SetupStarFilterButtons()
    {
        foreach (Transform child in starFilterContainer)
            Destroy(child.gameObject);

        starFilterButtons.Clear();

        for (int i = 1; i <= maxStar; i++)
        {
            GameObject starButton = Instantiate(starFilterButtonPrefab, starFilterContainer);
            starFilterButtons.Add(starButton);

            Image backgroundImage = starButton.GetComponentInChildren<Image>();
            Image starImage = backgroundImage.transform.GetChild(0).GetComponent<Image>();

            if (starSprites.TryGetValue(i, out Sprite sprite))
                starImage.sprite = sprite;

            int starValue = i;
            starButton.GetComponentInChildren<Button>().onClick.AddListener(() => OnStarFilterChanged(starValue));
        }
    }

    private void OnStarFilterChanged(int starValue)
    {
        selectedStarFilter = (selectedStarFilter == starValue) ? 0 : starValue;
        ApplyFilters();
        CloseStarFilterContainer(false);
    }

    private void ToggleStarFilterDropdown()
    {
        bool active = !starFilterContainer.gameObject.activeSelf;
        starFilterContainer.gameObject.SetActive(active);
        starFilterContainer.GetComponentInParent<Image>().enabled = active;
    }

    private void OnSearchChanged(string text)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        string search = searchInput.text.ToLower();

        filteredRecipeButtons = allRecipeButtons.Where(obj =>
        {
            RecipeView view = obj.GetComponentInChildren<RecipeView>();
            if (view == null) return false;

            bool starOk = (selectedStarFilter == 0 || view.RecipeData.starRating == selectedStarFilter);
            bool searchOk = string.IsNullOrEmpty(search) || view.RecipeData.recipeName.ToLower().Contains(search);

            return starOk && searchOk;
        }).ToList();

        foreach (var obj in allRecipeButtons)
            obj.SetActive(false);

        currentPage = 0;
        ShowPage(currentPage);
    }

    private void CloseStarFilterContainer(bool isActive)
    {
        starFilterContainer.gameObject.SetActive(isActive);
        starFilterContainer.GetComponentInParent<Image>().enabled = isActive;
    }
    #endregion

    #region Recipe UI
    private void PopulateRecipesUI()
    {
        foreach (Transform child in recipeListContainer)
            Destroy(child.gameObject);

        allRecipeButtons.Clear();

        foreach (var recipe in cookingManager.RecipeLoader.recipeCollection.recipes)
        {
            GameObject cellParent = new GameObject("CellParent", typeof(RectTransform));
            cellParent.transform.SetParent(recipeListContainer);
            cellParent.transform.localScale = Vector3.one;

            GameObject btnObj = Instantiate(recipeButtonPrefab, cellParent.transform);
            btnObj.transform.localScale = Vector3.one;

            RecipeView view = btnObj.GetComponent<RecipeView>();
            if (view != null)
            {
                Sprite icon = itemDatabase.GetIcon(recipe.resultId);
                string displayName = itemDatabase.GetName(recipe.resultId);

                view.SetData(recipe, icon, displayName);
                view.OnRecipeSelected += OnSelectRecipe;
            }

            allRecipeButtons.Add(cellParent);
        }

        ApplyFilters();
    }

    private void OnSelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        UpdateIngredientUI(recipe);
        UpdateCookButtonState();

        foreach (var obj in allRecipeButtons)
        {
            RecipeView view = obj.GetComponentInChildren<RecipeView>();
            if (view != null)
                view.SetHighlight(view.RecipeData == selectedRecipe);
        }
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
    #endregion

    #region Pagination
    private void ShowPage(int page)
    {
        if (filteredRecipeButtons.Count == 0)
        {
            prevPageButton.gameObject.SetActive(false);
            nextPageButton.gameObject.SetActive(false);
            ClearIndicators();
            return;
        }

        int startIndex = page * recipesPerPage;
        int endIndex = startIndex + recipesPerPage;

        for (int i = 0; i < filteredRecipeButtons.Count; i++)
            filteredRecipeButtons[i].SetActive(i >= startIndex && i < endIndex);

        int maxPage = (filteredRecipeButtons.Count - 1) / recipesPerPage;
        bool multiplePages = (maxPage > 0);
        prevPageButton.gameObject.SetActive(multiplePages);
        nextPageButton.gameObject.SetActive(multiplePages);

        UpdatePageIndicators(page, maxPage + 1);
    }

    private void OnNextPage()
    {
        if (filteredRecipeButtons.Count == 0) return;

        int maxPage = (filteredRecipeButtons.Count - 1) / recipesPerPage;
        currentPage = (currentPage < maxPage) ? currentPage + 1 : 0;

        ShowPage(currentPage);
    }

    private void OnPrevPage()
    {
        if (filteredRecipeButtons.Count == 0) return;

        int maxPage = (filteredRecipeButtons.Count - 1) / recipesPerPage;
        currentPage = (currentPage > 0) ? currentPage - 1 : maxPage;

        ShowPage(currentPage);
    }

    private void UpdatePageIndicators(int currentPage, int totalPages)
    {
        ClearIndicators();
        pageIndicators.Clear();

        for (int i = 0; i < totalPages; i++)
        {
            GameObject dot = Instantiate(indicatorPrefab, pageIndicatorContainer);
            Image img = dot.GetComponent<Image>();
            img.sprite = (i == currentPage) ? indicatorActiveSprite : indicatorNormalSprite;
            pageIndicators.Add(dot);
        }
    }

    private void ClearIndicators()
    {
        foreach (Transform child in pageIndicatorContainer)
            Destroy(child.gameObject);
    }
    #endregion

    #region Notifications
    public void ShowItemReceivedPopup(RecipeData recipe)
    {
        itemReceivedPopup.SetActive(true);

        foreach (Transform child in itemDisplayContainer)
            Destroy(child.gameObject);

        GameObject obj = Instantiate(recipeButtonPrefab, itemDisplayContainer);
        obj.transform.localScale = Vector3.one;

        RecipeView view = obj.GetComponent<RecipeView>();
        if (view != null)
        {
            Sprite icon = itemDatabase.GetIcon(recipe.resultId);
            string displayName = itemDatabase.GetName(recipe.resultId);

            view.SetData(recipe, icon, displayName);

            Button btn = view.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    private void OnCloseItemReceivedPopup()
    {
        if (cookingManager != null && cookingManager.PotAnimationController != null)
            cookingManager.PotAnimationController.PlayAnimation("idle");

        if (itemReceivedPopup != null)
            itemReceivedPopup.SetActive(false);
    }
    #endregion

    #region Buttons
    private void OnCookButton() { if (selectedRecipe != null) cookingManager.StartCooking(selectedRecipe); }
    private void OnPauseButton() { cookingManager.PauseCooking(); }
    private void OnResumeButton() { cookingManager.ResumeCooking(); }
    #endregion

    #region UI Updates
    private void UpdateTimerText(int remainingSeconds)
    {
        int m = remainingSeconds / 60;
        int s = remainingSeconds % 60;
        timerText.text = $"{m:00}:{s:00}";
    }
    #endregion
}
