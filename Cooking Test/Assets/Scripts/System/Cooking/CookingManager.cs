using UnityEngine;

public class CookingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Inventory inventory;
    [SerializeField] private RecipeLoader recipeLoader;

    [Header("Test Cooking (Inspector)")]
    [Tooltip("Choose recipe to cook")]
    [SerializeField] private int recipeIndex = 0;

    // Inspector Button: กดเพื่อปรุงอาหาร
    [ContextMenu("Cook Selected Recipe")]
    public void CookSelectedRecipe()
    {
        if (recipeLoader == null || recipeLoader.recipeCollection == null || recipeLoader.recipeCollection.recipes.Length == 0)
        {
            Debug.LogError("[CookingManager] No recipes loaded!");
            return;
        }

        // Clamp index
        recipeIndex = Mathf.Clamp(recipeIndex, 0, recipeLoader.recipeCollection.recipes.Length - 1);

        CookRecipe(recipeLoader.recipeCollection.recipes[recipeIndex]);
    }

    private void CookRecipe(RecipeData recipe)
    {
        Debug.Log($"[CookingManager] Trying to cook {recipe.recipeName}...");

        // Check energy
        if (!energySystem.HasEnergy(recipe.energyCost))
        {
            Debug.LogWarning($"[CookingManager] Not enough energy! Required: {recipe.energyCost}, Current: {energySystem.CurrentEnergy}");
            return;
        }

        // Check inventory ingredients
        foreach (var ingredient in recipe.ingredients)
        {
            if (!inventory.HasItem(ingredient.id, ingredient.amount))
            {
                Debug.LogWarning($"[CookingManager] Not enough {ingredient.id}! Required: {ingredient.amount}");
                return;
            }
        }

        // Deduct energy
        energySystem.UseEnergy(recipe.energyCost);

        // Deduct ingredients
        foreach (var ingredient in recipe.ingredients)
            inventory.RemoveItem(ingredient.id, ingredient.amount);

        // Add result item
        inventory.AddItem(recipe.resultId, 1);

        // Save inventory after cooking
        string inventoryPath = System.IO.Path.Combine(Application.streamingAssetsPath, "player_inventory.json");
        inventory.SaveToJson(inventoryPath);

        Debug.Log($"[CookingManager] Successfully cooked {recipe.recipeName}!");
    }
}
