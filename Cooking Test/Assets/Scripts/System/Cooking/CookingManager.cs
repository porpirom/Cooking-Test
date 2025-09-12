using UnityEngine;
using System;
using System.IO;

[Serializable]
public class CookingStateData
{
    public bool isCooking;
    public string recipeName;
    public long endTimeUnix;
    public bool isPaused;
    public long pauseStartUnix;
}

public class CookingManager : MonoBehaviour
{
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Inventory inventory;
    [SerializeField] private RecipeLoader recipeLoader;
    [SerializeField] private int recipeIndex = 0;

    [Header("UI Sprites")]
    [SerializeField] private Sprite[] starSprites;   // index 0 = ดาว 1 ดวง, index 1 = ดาว 2 ดวง...
    [SerializeField] private Sprite[] frameSprites;  // index 0 = กรอบ 1 ดาว, index 1 = กรอบ 2 ดาว...

    // Make these variables public to be accessible by UIManager
    public bool isCooking = false;
    public bool isPaused = false;
    private RecipeData currentRecipe;
    private DateTime endTime;
    private DateTime pauseStartTime;
    private string cookingStatePath;

    private string pendingRecipeName = null;

    public event Action<int> OnCookingTimeChanged;
    public event Action<bool> OnCookingStateChanged;
    public event Action<RecipeData> OnCookingFinished;

    public bool IsCooking => isCooking && !isPaused;
    public RecipeLoader RecipeLoader => recipeLoader;
    public Inventory Inventory => inventory;

    public int RemainingTime
    {
        get
        {
            if (!isCooking) return 0;
            DateTime now = TimeManager.Instance.UtcNow;
            if (isPaused) return Mathf.Max(0, Mathf.CeilToInt((float)(endTime - pauseStartTime).TotalSeconds));
            return Mathf.Max(0, Mathf.CeilToInt((float)(endTime - now).TotalSeconds));
        }
    }

    private void Awake()
    {
        cookingStatePath = Path.Combine(Application.persistentDataPath, "player_cooking.json");
        Debug.Log($"[CookingManager] Save path: {cookingStatePath}");

        // Load state in Awake to ensure it's available before Start
        LoadCookingState();
    }

    private void Start()
    {
        string path = Path.Combine(Application.persistentDataPath, "player_inventory.json");
        inventory.LoadFromJson(path);

        recipeLoader.OnRecipesLoaded += () =>
        {
            if (!string.IsNullOrEmpty(pendingRecipeName))
            {
                currentRecipe = Array.Find(recipeLoader.recipeCollection.recipes, r => r.recipeName == pendingRecipeName);
                if (currentRecipe != null)
                {
                    int remaining = RemainingTime;
                    if (remaining <= 0 && !isPaused)
                    {
                        FinishCookingImmediate(); // ถ้าหมดเวลาแล้วก็เสร็จอาหารทันที
                    }
                    else
                    {
                        isCooking = true;
                        OnCookingStateChanged?.Invoke(true);
                        OnCookingTimeChanged?.Invoke(remaining);
                    }
                }
                pendingRecipeName = null;
            }
        };

    }

    private void Update()
    {
        if (!isCooking || isPaused) return;

        int remaining = RemainingTime;
        // The event is now called every frame to ensure a smooth countdown
        OnCookingTimeChanged?.Invoke(remaining);

        if (remaining <= 0)
        {
            FinishCookingImmediate();
        }
    }
    private void OnDestroy()
    {
        if (isCooking) SaveCookingState();
    }

    public void CookSelectedRecipe()
    {
        if (isCooking) return;
        if (recipeLoader == null || recipeLoader.recipeCollection == null || recipeLoader.recipeCollection.recipes.Length == 0) return;

        recipeIndex = Mathf.Clamp(recipeIndex, 0, recipeLoader.recipeCollection.recipes.Length - 1);
        currentRecipe = recipeLoader.recipeCollection.recipes[recipeIndex];

        StartCooking(currentRecipe);
    }

    public void StartCooking(RecipeData recipe)
    {
        Debug.Log($"[CookingManager] StartCooking called for {recipe.recipeName}");

        // เช็คพลังงานก่อน
        if (!energySystem.HasEnergy(recipe.energyCost))
        {
            Debug.Log("[CookingManager] Not enough energy");
            return; // ออกจากฟังก์ชันทันที
        }

        // เช็ควัตถุดิบทั้งหมด
        foreach (var ing in recipe.ingredients)
        {
            if (!inventory.HasItem(ing.id, ing.amount))
            {
                Debug.Log($"[CookingManager] Missing ingredient {ing.id}");
                return; // ออกจากฟังก์ชันทันที
            }
        }

        // ผ่านทุกเงื่อนไขแล้วค่อยหักพลังงานและวัตถุดิบ
        energySystem.UseEnergy(recipe.energyCost);
        foreach (var ing in recipe.ingredients)
            inventory.RemoveItem(ing.id, ing.amount);

        inventory.SaveToJson(Path.Combine(Application.persistentDataPath, "player_inventory.json"));
        energySystem.SaveEnergy(Path.Combine(Application.persistentDataPath, "player_energy.json"));

        // เริ่มนับเวลา
        endTime = TimeManager.Instance.UtcNow.AddSeconds(recipe.cookingTimeSeconds);
        isCooking = true;
        isPaused = false;
        currentRecipe = recipe;

        OnCookingStateChanged?.Invoke(true);
        OnCookingTimeChanged?.Invoke(recipe.cookingTimeSeconds);

        SaveCookingState();
    }
    private void FinishCookingImmediate()
    {
        if (!isCooking) return;

        inventory.AddItem(currentRecipe.resultId, 1);
        isCooking = false;
        isPaused = false;

        OnCookingStateChanged?.Invoke(false);
        OnCookingTimeChanged?.Invoke(0);
        OnCookingFinished?.Invoke(currentRecipe);

        inventory.SaveToJson(Path.Combine(Application.persistentDataPath, "player_inventory.json"));
        energySystem.SaveEnergy(Path.Combine(Application.persistentDataPath, "player_energy.json"));

        if (File.Exists(cookingStatePath)) File.Delete(cookingStatePath);

        // --- เพิ่มส่วนนี้ --- 
        CookingUIController uiController = FindObjectOfType<CookingUIController>();
        if (uiController != null)
        {
            uiController.ShowItemReceivedPopup(currentRecipe);
        }
    }


    public void PauseCooking()
    {
        if (!isCooking || isPaused) return;
        pauseStartTime = TimeManager.Instance.UtcNow;
        isPaused = true;
        SaveCookingState();
        OnCookingStateChanged?.Invoke(isCooking);
    }
    public void ResumeCooking()
    {
        if (!isCooking || !isPaused) return;

        TimeSpan pausedDuration = TimeManager.Instance.UtcNow - pauseStartTime;
        endTime = endTime.Add(pausedDuration);
        isPaused = false;
        SaveCookingState();
        OnCookingStateChanged?.Invoke(true);
        OnCookingTimeChanged?.Invoke(RemainingTime);
    }


    private void OnDisable()
    {
        if (isCooking) SaveCookingState();
    }

    private void OnApplicationQuit()
    {
        if (isCooking) SaveCookingState();
    }

    private void SaveCookingState()
    {
        if (currentRecipe == null) return;

        CookingStateData state = new CookingStateData
        {
            isCooking = isCooking,
            recipeName = currentRecipe.recipeName,
            endTimeUnix = TimeManager.Instance.ToUnix(endTime),
            isPaused = isPaused,
            pauseStartUnix = TimeManager.Instance.ToUnix(pauseStartTime)
        };

        File.WriteAllText(cookingStatePath, JsonUtility.ToJson(state, true));
    }
    private void LoadCookingState()
    {
        if (!File.Exists(cookingStatePath)) return;

        string json = File.ReadAllText(cookingStatePath);
        CookingStateData state = JsonUtility.FromJson<CookingStateData>(json);
        if (!state.isCooking) return;

        pendingRecipeName = state.recipeName;
        isPaused = state.isPaused;
        endTime = TimeManager.Instance.FromUnix(state.endTimeUnix);
        pauseStartTime = TimeManager.Instance.FromUnix(state.pauseStartUnix);

        // 👇 เพิ่มตรงนี้
        int remaining = Mathf.Max(0, Mathf.CeilToInt((float)(endTime - TimeManager.Instance.UtcNow).TotalSeconds));
        if (remaining <= 0 && !isPaused)
        {
            // ถ้าเวลาหมดไปแล้วในตอนที่เกมปิด
            isCooking = false;
            isPaused = false;
            currentRecipe = null;
            if (File.Exists(cookingStatePath)) File.Delete(cookingStatePath);
        }
    }

}