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
    public float remainingTimeOnPause;
    public float totalCookingDuration; // Added to cap the time
}

public class CookingManager : MonoBehaviour
{
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Inventory inventory;
    [SerializeField] private RecipeLoader recipeLoader;
    [SerializeField] private int recipeIndex = 0;

    [Header("UI Sprites")]
    [SerializeField] private Sprite[] starSprites;
    [SerializeField] private Sprite[] frameSprites;

    [SerializeField] CookingPotAnimationController cookingPotAnimationController;
    public bool isCooking = false;
    public bool isPaused = false;
    private RecipeData currentRecipe;
    private DateTime endTime;
    private DateTime pauseStartTime;
    private string cookingStatePath;

    private float remainingTimeOnPause = 0f;
    private float totalCookingDuration; // Stores total recipe time

    private string pendingRecipeName = null;

    public event Action<int> OnCookingTimeChanged;
    public event Action<bool> OnCookingStateChanged;
    public event Action<RecipeData> OnCookingFinished;

    public bool IsCooking => isCooking && !isPaused;
    public RecipeLoader RecipeLoader => recipeLoader;
    public Inventory Inventory => inventory;
    public CookingPotAnimationController PotAnimationController => cookingPotAnimationController;

    public int RemainingTime
    {
        get
        {
            if (!isCooking) return 0;
            DateTime now = TimeManager.Instance.UtcNow;
            if (isPaused) return Mathf.CeilToInt(remainingTimeOnPause);
            return Mathf.Max(0, Mathf.CeilToInt((float)(endTime - now).TotalSeconds));
        }
    }

    private void Awake()
    {
        cookingStatePath = Path.Combine(Application.persistentDataPath, "player_cooking.json");
        Debug.Log($"[CookingManager-Awake] Save path: {cookingStatePath}");
        LoadCookingState();
        Debug.Log($"[CookingManager-Awake] State Loaded: isCooking={isCooking}, isPaused={isPaused}, remainingTimeOnPause={remainingTimeOnPause}");
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
                    Debug.Log($"[CookingManager-Start] Recipe loaded. Remaining time: {remaining}");
                    if (remaining <= 0 && !isPaused)
                    {
                        FinishCookingImmediate();
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

        TimeSpan remainingTime = endTime - TimeManager.Instance.UtcNow;
        int remaining = Mathf.Max(0, Mathf.CeilToInt((float)remainingTime.TotalSeconds));

        if (remaining <= 0)
        {
            FinishCookingImmediate();
        }
        else
        {
            OnCookingTimeChanged?.Invoke(remaining);
        }
    }

    private void OnDestroy()
    {
        if (isCooking)
        {
            Debug.Log("[CookingManager-OnDestroy] Saving state...");
            SaveCookingState();
        }
    }

    private void OnDisable()
    {
        if (isCooking)
        {
            Debug.Log("[CookingManager-OnDisable] Saving state...");
            SaveCookingState();
        }
    }

    private void OnApplicationQuit()
    {
        if (isCooking)
        {
            Debug.Log("[CookingManager-OnApplicationQuit] Saving state...");
            SaveCookingState();
        }
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
        Debug.Log($"[CookingManager-StartCooking] Starting cooking for {recipe.recipeName}");

        if (!energySystem.HasEnergy(recipe.energyCost))
        {
            Debug.Log("[CookingManager-StartCooking] Not enough energy. Cooking cancelled.");
            return;
        }

        foreach (var ing in recipe.ingredients)
        {
            if (!inventory.HasItem(ing.id, ing.amount))
            {
                Debug.Log($"[CookingManager-StartCooking] Missing ingredient {ing.id}. Cooking cancelled.");
                return;
            }
        }

        UpdateCookingAnimation();
        energySystem.UseEnergy(recipe.energyCost);
        foreach (var ing in recipe.ingredients)
            inventory.RemoveItem(ing.id, ing.amount);

        inventory.SaveToJson(Path.Combine(Application.persistentDataPath, "player_inventory.json"));
        energySystem.SaveEnergy(Path.Combine(Application.persistentDataPath, "player_energy.json"));

        endTime = TimeManager.Instance.UtcNow.AddSeconds(recipe.cookingTimeSeconds);
        isCooking = true;
        isPaused = false;
        currentRecipe = recipe;

        remainingTimeOnPause = 0f;
        totalCookingDuration = recipe.cookingTimeSeconds; // Save total duration

        Debug.Log($"[CookingManager-StartCooking] New cooking session started. Initial endTime: {endTime.ToLocalTime()}");

        cookingPotAnimationController.PlayCookingIdle();

        OnCookingStateChanged?.Invoke(true);
        OnCookingTimeChanged?.Invoke(recipe.cookingTimeSeconds);

        SaveCookingState();
    }

    private void FinishCookingImmediate()
    {
        if (!isCooking) return;
        Debug.Log($"[CookingManager-FinishCookingImmediate] Cooking finished for {currentRecipe.recipeName}");

        inventory.AddItem(currentRecipe.resultId, 1);
        isCooking = false;
        isPaused = false;
        cookingPotAnimationController.PlaySuccessSequence();

        OnCookingStateChanged?.Invoke(false);
        OnCookingTimeChanged?.Invoke(0);
        OnCookingFinished?.Invoke(currentRecipe);

        inventory.SaveToJson(Path.Combine(Application.persistentDataPath, "player_inventory.json"));
        energySystem.SaveEnergy(Path.Combine(Application.persistentDataPath, "player_energy.json"));

        if (File.Exists(cookingStatePath)) File.Delete(cookingStatePath);

        CookingUIController uiController = FindObjectOfType<CookingUIController>();
        if (uiController != null)
        {
            uiController.ShowItemReceivedPopup(currentRecipe);
        }
    }

    public void PauseCooking()
    {
        if (!isCooking || isPaused) return;

        remainingTimeOnPause = RemainingTime;
        isPaused = true;
        pauseStartTime = TimeManager.Instance.UtcNow;

        Debug.Log($"[CookingManager-PauseCooking] Paused! Remaining time stored: {remainingTimeOnPause} seconds.");

        SaveCookingState();
        OnCookingStateChanged?.Invoke(isCooking);

        OnCookingTimeChanged?.Invoke(Mathf.CeilToInt(remainingTimeOnPause));
    }

    public void ResumeCooking()
    {
        if (!isCooking || !isPaused) return;

        UpdateCookingAnimation();

        // คำนวณเวลาที่หยุดไป
        TimeSpan pausedDuration = TimeManager.Instance.UtcNow - pauseStartTime;

        // คำนวณเวลาที่เหลือทั้งหมดหลัง Resume
        float newRemainingTime = remainingTimeOnPause + (float)pausedDuration.TotalSeconds;

        // ตรวจสอบว่าเวลาที่เหลือใหม่ไม่เกินระยะเวลาทำอาหารทั้งหมด
        if (newRemainingTime > totalCookingDuration)
        {
            newRemainingTime = totalCookingDuration;
        }

        // ตั้งค่าเวลาสิ้นสุดใหม่ตาม Logic ที่ถูกต้องของคุณ
        endTime = TimeManager.Instance.UtcNow.AddSeconds(newRemainingTime);

        isPaused = false;
        remainingTimeOnPause = 0f;

        Debug.Log($"[CookingManager-ResumeCooking] Resumed! New endTime is: {endTime.ToLocalTime()}");

        int remaining = RemainingTime;
        if (remaining <= 0)
        {
            FinishCookingImmediate();
        }
        else
        {
            SaveCookingState();
            OnCookingStateChanged?.Invoke(true);
            OnCookingTimeChanged?.Invoke(RemainingTime);
        }
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
            pauseStartUnix = TimeManager.Instance.ToUnix(pauseStartTime),
            remainingTimeOnPause = this.remainingTimeOnPause,
            totalCookingDuration = this.totalCookingDuration
        };

        File.WriteAllText(cookingStatePath, JsonUtility.ToJson(state, true));
        Debug.Log($"[CookingManager-SaveState] State saved. isPaused={isPaused}, remainingTimeOnPause={remainingTimeOnPause}");
    }

    private void LoadCookingState()
    {
        if (!File.Exists(cookingStatePath))
        {
            Debug.Log("[CookingManager-LoadState] No cooking state file found.");
            return;
        }

        string json = File.ReadAllText(cookingStatePath);
        CookingStateData state = JsonUtility.FromJson<CookingStateData>(json);

        if (!state.isCooking)
        {
            Debug.Log("[CookingManager-LoadState] State found but not cooking.");
            return;
        }

        pendingRecipeName = state.recipeName;
        isPaused = state.isPaused;
        endTime = TimeManager.Instance.FromUnix(state.endTimeUnix);
        pauseStartTime = TimeManager.Instance.FromUnix(state.pauseStartUnix);
        remainingTimeOnPause = state.remainingTimeOnPause;
        totalCookingDuration = state.totalCookingDuration;

        Debug.Log($"[CookingManager-LoadState] State loaded. isPaused={isPaused}, remainingTimeOnPause={remainingTimeOnPause}");

        int remaining = RemainingTime;
        if (remaining <= 0 && !isPaused)
        {
            Debug.Log("[CookingManager-LoadState] Time has already run out. Finishing immediately.");
            isCooking = false;
            isPaused = false;
            currentRecipe = null;
            if (File.Exists(cookingStatePath)) File.Delete(cookingStatePath);
        }
    }
    private void UpdateCookingAnimation()
    {
        if (!isCooking)
        {
            cookingPotAnimationController.PlayAnimation("idle");
            cookingPotAnimationController.SetIsCookingIdlePlaying = false;
        }
        else
        {
            cookingPotAnimationController.PlayCookingIdle();
        }
    }
}