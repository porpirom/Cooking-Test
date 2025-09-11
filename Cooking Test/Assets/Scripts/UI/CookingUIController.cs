using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

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
    private int lastDropdownIndex = -1;

    // เก็บ sprite ดาวและกรอบจาก Addressables
    private Dictionary<int, Sprite> starSprites = new Dictionary<int, Sprite>();
    private Dictionary<int, Sprite> frameSprites = new Dictionary<int, Sprite>();

    private async void Start()
    {
        itemDatabase.LoadFromJson(Application.streamingAssetsPath + "/items.json");

        cookButton.onClick.AddListener(OnCookButton);
        pauseButton.onClick.AddListener(OnPauseButton);
        resumeButton.onClick.AddListener(OnResumeButton);

        starDropdown.onValueChanged.AddListener(OnStarFilterChanged);
        searchInput.onValueChanged.AddListener(OnSearchChanged);

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

        foreach (var recipe in cookingManager.RecipeLoader.recipeCollection.recipes)
        {
            // สร้าง parent ตัวกลางให้ GridLayoutGroup จัดการ
            GameObject cellParent = new GameObject("CellParent", typeof(RectTransform));
            cellParent.transform.SetParent(recipeListContainer);
            cellParent.transform.localScale = Vector3.one;
            cellParent.transform.localPosition = Vector3.zero;

            // สร้างปุ่ม/รูปเข้าไปใน parent ตัวกลาง
            GameObject btnObj = Instantiate(recipeButtonPrefab, cellParent.transform);
            btnObj.transform.localScale = Vector3.one;
            btnObj.transform.localPosition = Vector3.zero;

            RecipeView view = btnObj.GetComponent<RecipeView>();
            if (view != null)
            {
                // ตั้งค่าข้อมูล recipe
                Sprite icon = itemDatabase.GetIcon(recipe.resultId);
                string displayName = itemDatabase.GetName(recipe.resultId);
                view.SetData(recipe, icon, displayName);

                // ใส่ frame จาก Addressables ตามดาว
                if (frameSprites.TryGetValue(recipe.starRating, out Sprite frame))
                {
                    view.SetFrame(frame);

                    // ตั้ง AspectRatioFitter อัตโนมัติตาม frame
                    var arf = btnObj.AddComponent<AspectRatioFitter>();
                    arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    arf.aspectRatio = (float)frame.rect.width / frame.rect.height;
                }

                view.OnRecipeSelected += OnSelectRecipe;
            }
        }

        UpdateRecipeUIVisibility();
    }

    private void OnStarFilterChanged(int index)
    {
        int clickedStar = maxStar - index;

        if (lastDropdownIndex == index)
        {
            selectedStarFilter = 0; // แสดงทั้งหมด
            lastDropdownIndex = -1;
        }
        else
        {
            selectedStarFilter = clickedStar;
            lastDropdownIndex = index;
        }

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
            RecipeView view = child.GetComponentInChildren<RecipeView>(); // ใช้ GetComponentInChildren
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
