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

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown starDropdown;
    [SerializeField] private TMP_InputField searchInput;

    [Header("Pagination")]
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;

    [Header("Page Indicator")]
    [SerializeField] private Transform pageIndicatorContainer;
    [SerializeField] private GameObject indicatorPrefab;

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
    private Dictionary<int, Sprite> frameSprites = new Dictionary<int, Sprite>();

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

        if (cookingManager.RecipeLoader.recipeCollection != null &&
            cookingManager.RecipeLoader.recipeCollection.recipes.Length > 0)
        {
            await OnRecipesLoadedAsync();
        }

        UpdateTimerText(cookingManager.RemainingTime);
    }

    private void OnDisable()
    {
        if (cookingManager != null)
            cookingManager.OnCookingTimeChanged -= UpdateTimerText;
    }

    private async Task OnRecipesLoadedAsync()
    {
        var recipes = cookingManager.RecipeLoader.recipeCollection.recipes;
        if (recipes == null || recipes.Length == 0) return;

        maxStar = 0;
        foreach (var recipe in recipes)
            if (recipe.starRating > maxStar) maxStar = recipe.starRating;

        await LoadStarSpritesAsync(maxStar);
        await LoadFrameSpritesAsync(maxStar);

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

    private async Task LoadFrameSpritesAsync(int maxStar)
    {
        frameSprites.Clear();

        for (int i = 1; i <= maxStar; i++)
        {
            string key = $"Frames/frame_{i}";
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                frameSprites[i] = handle.Result;
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
                options.Add(new TMP_Dropdown.OptionData("", icon));
            else
                options.Add(new TMP_Dropdown.OptionData(i + " STAR"));
        }

        starDropdown.ClearOptions();
        starDropdown.AddOptions(options);

        starDropdown.value = 0; // เริ่มต้นทั้งหมด
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
                view.SetData(recipe, icon, displayName);

                if (frameSprites.TryGetValue(recipe.starRating, out Sprite frame))
                {
                    view.SetFrame(frame);
                    var arf = btnObj.AddComponent<AspectRatioFitter>();
                    arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    arf.aspectRatio = (float)frame.rect.width / frame.rect.height;
                }

                view.OnRecipeSelected += OnSelectRecipe;
            }

            allRecipeButtons.Add(cellParent);
        }

        ApplyFilters(); // เริ่มด้วย filter ครั้งแรก
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
        // ลบเก่าออกก่อน
        ClearIndicators();

        pageIndicators.Clear();

        for (int i = 0; i < totalPages; i++)
        {
            GameObject dot = Instantiate(indicatorPrefab, pageIndicatorContainer);
            Image img = dot.GetComponent<Image>();

            if (i == currentPage)
                img.color = Color.white; // active (ทึบ)
            else
                img.color = new Color(1f, 1f, 1f, 0.3f); // inactive (โปร่ง)

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
