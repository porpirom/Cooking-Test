using UnityEngine;
using System;
using System.Collections;
using System.IO;

[Serializable]
public class CookingStateData
{
    public bool isCooking;
    public string recipeName;
    public int remainingTime;
    public long lastSavedTimestamp; // Unix timestamp
}

public class CookingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Inventory inventory;
    [SerializeField] private RecipeLoader recipeLoader;

    [Header("Test Cooking (Inspector)")]
    [SerializeField] private int recipeIndex = 0;

    private bool isCooking = false;
    private RecipeData currentRecipe;
    private int remainingTime;

    private string cookingStatePath;

    // Events
    public event Action<int> OnCookingTimeChanged;
    public event Action<bool> OnCookingStateChanged;
    public event Action<RecipeData> OnCookingFinished;

    public bool IsCooking => isCooking;
    public int RemainingTime => remainingTime;

    private void Awake()
    {
        cookingStatePath = Path.Combine(Application.streamingAssetsPath, "player_cooking.json");
    }
    private void Start()
    {
        recipeLoader.OnRecipesLoaded += ResumeCookingIfNeeded;
    }
    private void ResumeCookingIfNeeded()
    {
        LoadCookingState();
    }
    public void CookSelectedRecipe()
    {
        if (isCooking)
        {
            Debug.LogWarning("[CookingManager] Already cooking!");
            return;
        }

        if (recipeLoader == null || recipeLoader.recipeCollection == null || recipeLoader.recipeCollection.recipes.Length == 0)
        {
            Debug.LogError("[CookingManager] No recipes loaded!");
            return;
        }

        recipeIndex = Mathf.Clamp(recipeIndex, 0, recipeLoader.recipeCollection.recipes.Length - 1);
        currentRecipe = recipeLoader.recipeCollection.recipes[recipeIndex];

        StartCoroutine(CookCoroutine(currentRecipe));
    }

    private IEnumerator CookCoroutine(RecipeData recipe, bool resume = false)
    {
        // Check Energy
        if (!resume && !energySystem.HasEnergy(recipe.energyCost))
        {
            Debug.LogWarning($"[CookingManager] Not enough energy! Required: {recipe.energyCost}");
            yield break;
        }

        // Check Inventory
        if (!resume)
        {
            foreach (var ing in recipe.ingredients)
            {
                if (!inventory.HasItem(ing.id, ing.amount))
                {
                    Debug.LogWarning($"[CookingManager] Not enough {ing.id}! Required: {ing.amount}");
                    yield break;
                }
            }
        }

        // Deduct Energy & Ingredients (only at start)
        if (!resume)
        {
            energySystem.UseEnergy(recipe.energyCost);
            foreach (var ing in recipe.ingredients)
                inventory.RemoveItem(ing.id, ing.amount);

            // Save Inventory + Energy
            string invPath = Path.Combine(Application.streamingAssetsPath, "player_inventory.json");
            inventory.SaveToJson(invPath);
            string energyPath = Path.Combine(Application.streamingAssetsPath, "player_energy.json");
            energySystem.SaveEnergy(energyPath);

            remainingTime = recipe.cookingTimeSeconds;
        }

        // Start cooking
        isCooking = true;
        OnCookingStateChanged?.Invoke(true);
        SaveCookingState();

        while (remainingTime > 0)
        {
            OnCookingTimeChanged?.Invoke(remainingTime);
            yield return new WaitForSeconds(1f);
            remainingTime--;
            SaveCookingState();
        }

        FinishCookingImmediate();
    }

    private void FinishCookingImmediate()
    {
        inventory.AddItem(currentRecipe.resultId, 1);
        Debug.Log($"[CookingManager] Finished cooking {currentRecipe.recipeName}!");

        isCooking = false;
        OnCookingStateChanged?.Invoke(false);
        OnCookingTimeChanged?.Invoke(0);
        OnCookingFinished?.Invoke(currentRecipe);

        // Save Inventory + Energy
        string invPath = Path.Combine(Application.streamingAssetsPath, "player_inventory.json");
        inventory.SaveToJson(invPath);
        string energyPath = Path.Combine(Application.streamingAssetsPath, "player_energy.json");
        energySystem.SaveEnergy(energyPath);

        // Clear cooking state
        if (File.Exists(cookingStatePath))
            File.Delete(cookingStatePath);
    }

    private void SaveCookingState()
    {
        if (currentRecipe == null) return;

        CookingStateData state = new CookingStateData
        {
            isCooking = isCooking,
            recipeName = currentRecipe.recipeName,
            remainingTime = remainingTime,
            lastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(cookingStatePath, json);
    }

    private void LoadCookingState()
    {
        if (!File.Exists(cookingStatePath)) return;

        string json = File.ReadAllText(cookingStatePath);
        CookingStateData state = JsonUtility.FromJson<CookingStateData>(json);

        if (!state.isCooking) return;

        currentRecipe = Array.Find(recipeLoader.recipeCollection.recipes,
                                   r => r.recipeName == state.recipeName);
        if (currentRecipe == null) return;

        long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int elapsed = (int)(nowTimestamp - state.lastSavedTimestamp);
        remainingTime = Mathf.Max(state.remainingTime - elapsed, 0);

        if (remainingTime == 0)
        {
            FinishCookingImmediate();
            return;
        }

        isCooking = true;
        OnCookingStateChanged?.Invoke(true);
        OnCookingTimeChanged?.Invoke(remainingTime);

        StartCoroutine(CookCoroutine(currentRecipe, resume: true));
    }

}
