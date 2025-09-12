using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Serializable data to save/load cooking state.
/// </summary>
[Serializable]
public class CookingStateData
{
    public bool isCooking;
    public string recipeName;
    public long endTimeUnix;
    public bool isPaused;
    public long pauseStartUnix;
    public float remainingTimeOnPause;
    public float totalCookingDuration;
}

/// <summary>
/// Main cooking manager responsible for cooking logic, timing, inventory, and animations.
/// </summary>
public class CookingManager : MonoBehaviour
{
    #region Inspector References
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Inventory inventory;
    [SerializeField] private RecipeLoader recipeLoader;
    [SerializeField] private int recipeIndex = 0;

    [Header("UI Sprites")]
    [SerializeField] private Sprite[] starSprites;
    [SerializeField] private Sprite[] frameSprites;

    [SerializeField] private CookingPotAnimationController cookingPotAnimationController;
    #endregion

    #region Private Fields
    private RecipeData currentRecipe;
    private DateTime endTime;
    private DateTime pauseStartTime;
    private string cookingStatePath;
    private float remainingTimeOnPause = 0f;
    private float totalCookingDuration;
    private string pendingRecipeName = null;

    private bool isCooking = false;
    private bool isPaused = false;
    #endregion

    #region Events
    public event Action<int> OnCookingTimeChanged;
    public event Action<bool> OnCookingStateChanged;
    public event Action<RecipeData> OnCookingFinished;
    #endregion

    #region Public Properties
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
    #endregion

    #region Unity Methods
    private void Awake()
    {
        cookingStatePath = Path.Combine(Application.persistentDataPath, "player_cooking.json");
        LoadCookingState();
    }

    private void Start()
    {
        inventory.LoadFromJson(Path.Combine(Application.persistentDataPath, "player_inventory.json"));

        recipeLoader.OnRecipesLoaded += () =>
        {
            if (!string.IsNullOrEmpty(pendingRecipeName))
            {
                currentRecipe = Array.Find(recipeLoader.recipeCollection.recipes, r => r.recipeName == pendingRecipeName);
                if (currentRecipe != null)
                {
                    int remaining = RemainingTime;
                    if (remaining <= 0 && !isPaused)
                        FinishCookingImmediate();
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
        if (remaining <= 0)
            FinishCookingImmediate();
        else
            OnCookingTimeChanged?.Invoke(remaining);
    }

    private void OnDestroy() => SaveCookingStateIfNeeded();
    private void OnDisable() => SaveCookingStateIfNeeded();
    private void OnApplicationQuit() => SaveCookingStateIfNeeded();
    #endregion

    #region Public Methods
    public void CookSelectedRecipe()
    {
        if (isCooking || recipeLoader?.recipeCollection?.recipes.Length == 0) return;

        recipeIndex = Mathf.Clamp(recipeIndex, 0, recipeLoader.recipeCollection.recipes.Length - 1);
        currentRecipe = recipeLoader.recipeCollection.recipes[recipeIndex];

        StartCooking(currentRecipe);
    }

    public void StartCooking(RecipeData recipe)
    {
        if (!energySystem.HasEnergy(recipe.energyCost)) return;
        foreach (var ing in recipe.ingredients)
            if (!inventory.HasItem(ing.id, ing.amount)) return;

        UpdateCookingAnimation();
        energySystem.UseEnergy(recipe.energyCost);
        foreach (var ing in recipe.ingredients)
            inventory.RemoveItem(ing.id, ing.amount);

        inventory.SaveToJson(Path.Combine(Application.persistentDataPath, "player_inventory.json"));
        energySystem.SaveEnergy(Path.Combine(Application.persistentDataPath, "player_energy.json"));

        currentRecipe = recipe;
        endTime = TimeManager.Instance.UtcNow.AddSeconds(recipe.cookingTimeSeconds);
        totalCookingDuration = recipe.cookingTimeSeconds;
        isCooking = true;
        isPaused = false;
        remainingTimeOnPause = 0f;

        cookingPotAnimationController.PlayCookingIdle();

        OnCookingStateChanged?.Invoke(true);
        OnCookingTimeChanged?.Invoke(recipe.cookingTimeSeconds);

        SaveCookingState();
    }

    public void PauseCooking()
    {
        if (!isCooking || isPaused) return;

        remainingTimeOnPause = RemainingTime;
        isPaused = true;
        pauseStartTime = TimeManager.Instance.UtcNow;

        SaveCookingState();
        OnCookingStateChanged?.Invoke(isCooking);
        OnCookingTimeChanged?.Invoke(Mathf.CeilToInt(remainingTimeOnPause));
    }

    public void ResumeCooking()
    {
        if (!isCooking || !isPaused) return;

        UpdateCookingAnimation();

        float newRemaining = remainingTimeOnPause + (float)(TimeManager.Instance.UtcNow - pauseStartTime).TotalSeconds;
        newRemaining = Mathf.Min(newRemaining, totalCookingDuration);

        endTime = TimeManager.Instance.UtcNow.AddSeconds(newRemaining);
        isPaused = false;
        remainingTimeOnPause = 0f;

        int remaining = RemainingTime;
        if (remaining <= 0)
            FinishCookingImmediate();
        else
        {
            SaveCookingState();
            OnCookingStateChanged?.Invoke(true);
            OnCookingTimeChanged?.Invoke(remaining);
        }
    }
    #endregion

    #region Private Methods
    private void FinishCookingImmediate()
    {
        if (!isCooking) return;

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

        var uiController = FindObjectOfType<CookingUIController>();
        uiController?.ShowItemReceivedPopup(currentRecipe);
    }

    private void SaveCookingStateIfNeeded()
    {
        if (!isCooking || currentRecipe == null) return;
        SaveCookingState();
    }

    private void SaveCookingState()
    {
        CookingStateData state = new CookingStateData
        {
            isCooking = isCooking,
            recipeName = currentRecipe.recipeName,
            endTimeUnix = TimeManager.Instance.ToUnix(endTime),
            isPaused = isPaused,
            pauseStartUnix = TimeManager.Instance.ToUnix(pauseStartTime),
            remainingTimeOnPause = remainingTimeOnPause,
            totalCookingDuration = totalCookingDuration
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
        remainingTimeOnPause = state.remainingTimeOnPause;
        totalCookingDuration = state.totalCookingDuration;

        if (RemainingTime <= 0 && !isPaused)
        {
            isCooking = false;
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
    #endregion
}
