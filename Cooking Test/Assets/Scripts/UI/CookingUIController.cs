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
    [SerializeField] private Sprite cookButtonEnabledSprite;
    [SerializeField] private Sprite cookButtonDisabledSprite;

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown starDropdown;
    [SerializeField] private TMP_InputField searchInput;

    [Header("Pagination")]
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;

    [Header("Page Indicator")]
    [SerializeField] private Transform pageIndicatorContainer;
    [SerializeField] private GameObject indicatorPrefab;

    [Header("Page Indicator Sprites")]
    [SerializeField] private Sprite indicatorNormalSprite;
    [SerializeField] private Sprite indicatorActiveSprite;

    [Header("Notification UI")]
    [SerializeField] private GameObject itemReceivedPopup;   // หน้าต่างแจ้งเตือน
    [SerializeField] private Transform itemDisplayContainer;  // ไว้วาง RecipeView/ItemView
    [SerializeField] private Button itemReceivedCloseButton;  // ปุ่มปิด popup

    private List<GameObject> pageIndicators = new List<GameObject>();

    private List<GameObject> recipeButtons = new List<GameObject>();
    private int currentPage = 0;
    private int recipesPerPage = 4;

    private RecipeData selectedRecipe;
    private int selectedStarFilter = 0;
    private int maxStar = 3;
    private int lastDropdownIndex = -1;

    // เก็บ sprite ดาวและกรอบจาก Addressables
    private Dictionary<int, Sprite> starSprites = new Dictionary<int, Sprite>();

    private List<GameObject> allRecipeButtons = new List<GameObject>();
    private List<GameObject> filteredRecipeButtons = new List<GameObject>();

    private async void Start()
    {
        itemDatabase.LoadFromJson(Application.streamingAssetsPath + "/items.json");

        cookButton.onClick.AddListener(OnCookButton);
        pauseButton.onClick.AddListener(OnPauseButton);
        resumeButton.onClick.AddListener(OnResumeButton);

        starDropdown.onValueChanged.AddListener(OnStarFilterChanged);
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        nextPageButton.onClick.AddListener(OnNextPage);
        prevPageButton.onClick.AddListener(OnPrevPage);

        cookingManager.OnCookingTimeChanged += UpdateTimerText;
        cookingManager.RecipeLoader.OnRecipesLoaded += OnRecipesLoaded;

        if (itemReceivedCloseButton != null)
            itemReceivedCloseButton.onClick.AddListener(OnCloseItemReceivedPopup);

        // ⭐ เชื่อมกับ CookingStateChanged
        cookingManager.OnCookingStateChanged += OnCookingStateChanged;

        if (cookingManager.RecipeLoader.recipeCollection != null &&
            cookingManager.RecipeLoader.recipeCollection.recipes.Length > 0)
        {
            await OnRecipesLoadedAsync();
        }

        UpdateTimerText(cookingManager.RemainingTime);

        // อัพเดต UI ตอนเริ่มเกม
        OnCookingStateChanged(!cookingManager.IsCooking);

        if (cookingManager.Inventory != null)
            cookingManager.Inventory.OnInventoryChanged += OnInventoryChanged;

    }

    private void OnDisable()
    {
        if (cookingManager != null)
        {
            cookingManager.OnCookingTimeChanged -= UpdateTimerText;
            cookingManager.OnCookingStateChanged -= OnCookingStateChanged;
        }
        if (cookingManager.Inventory != null)
            cookingManager.Inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        // อัพเดต Ingredient UI และปุ่ม cook หากมี recipe ถูกเลือก
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
            cookButton.GetComponent<Image>().sprite = cookButtonDisabledSprite; // Set to disabled sprite
            return;
        }

        bool canCook = true;

        foreach (var ing in selectedRecipe.ingredients)
        {
            int playerAmount = cookingManager.Inventory.Items.GetValueOrDefault(ing.id, 0);
            if (playerAmount < ing.amount)
            {
                canCook = false;
                break;
            }
        }

        cookButton.interactable = canCook;
        // Use sprite based on ingredient availability
        cookButton.GetComponent<Image>().sprite = canCook ? cookButtonEnabledSprite : cookButtonDisabledSprite;
    }
    private void OnCookingStateChanged(bool isCooking)
    {
        cookButton.interactable = !isCooking;

        // Use sprite based on cooking state
        if (isCooking)
        {
            cookButton.GetComponent<Image>().sprite = cookButtonDisabledSprite;
        }
        else
        {
            cookButton.GetComponent<Image>().sprite = cookButtonEnabledSprite;
        }
    }
    private async Task OnRecipesLoadedAsync()
    {
        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        if (recipes == null || recipes.Length == 0) return;

        maxStar = 0;
        foreach (var recipe in recipes)
            if (recipe.starRating > maxStar) maxStar = recipe.starRating;

        await LoadStarSpritesAsync(maxStar);

        SetupStarDropdown();
        PopulateRecipesUI();

    }

    private async void OnRecipesLoaded()
    {
        await OnRecipesLoadedAsync();
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

    private void SetupStarDropdown()
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        for (int i = maxStar; i >= 1; i--)
        {
            if (starSprites.TryGetValue(i, out Sprite icon))
                options.Add(new TMP_Dropdown.OptionData("", icon)); // ใช้ sprite จาก Addressables เฉพาะ filter
            else
                options.Add(new TMP_Dropdown.OptionData(i + " STAR"));
        }

        starDropdown.ClearOptions();
        starDropdown.AddOptions(options);

        starDropdown.value = 0;
        selectedStarFilter = 0;
        lastDropdownIndex = -1;
    }

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

                // Replace these two lines in PopulateRecipesUI:
                Sprite starSprite = starSprites.ContainsKey(1) ? starSprites[1] : null;

                view.SetData(recipe, icon, displayName);

                view.OnRecipeSelected += OnSelectRecipe;
            }


            allRecipeButtons.Add(cellParent);
        }

        ApplyFilters();
    }

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
        {
            filteredRecipeButtons[i].SetActive(i >= startIndex && i < endIndex);
        }

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

        if (currentPage < maxPage)
            currentPage++;
        else
            currentPage = 0; // วนไปหน้าแรก

        ShowPage(currentPage);
    }

    private void OnPrevPage()
    {
        if (filteredRecipeButtons.Count == 0) return;

        int maxPage = (filteredRecipeButtons.Count - 1) / recipesPerPage;

        if (currentPage > 0)
            currentPage--;
        else
            currentPage = maxPage; // วนไปหน้าสุดท้าย

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
        {
            Destroy(child.gameObject);
        }
    }

    private void OnStarFilterChanged(int index)
    {
        int clickedStar = maxStar - index;

        if (lastDropdownIndex == index)
        {
            selectedStarFilter = 0;
            lastDropdownIndex = -1;
        }
        else
        {
            selectedStarFilter = clickedStar;
            lastDropdownIndex = index;
        }

        ApplyFilters();
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
            bool searchOk = string.IsNullOrEmpty(search) ||
                            view.RecipeData.recipeName.ToLower().Contains(search);

            return starOk && searchOk;
        }).ToList();

        // ปิดทุกอันก่อน
        foreach (var obj in allRecipeButtons)
            obj.SetActive(false);

        currentPage = 0;
        ShowPage(currentPage);
    }
    private void OnSelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        UpdateIngredientUI(recipe);
        UpdateCookButtonState();

        // อัพเดต highlight ทุก recipe
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
        if (itemReceivedPopup != null)
            itemReceivedPopup.SetActive(false);

        if (cookingManager != null && cookingManager.PotAnimationController != null)
            cookingManager.PotAnimationController.PlayAnimation("idle");
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
