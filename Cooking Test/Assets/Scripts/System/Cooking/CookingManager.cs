using UnityEngine;

public class CookingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Inventory inventory;

    [Header("Recipes")]
    [SerializeField] private RecipeLoader recipeLoader;

    [Header("Test Cooking (Inspector)")]
    [Tooltip("Choose recipe to cook when pressing Play")]
    [SerializeField] private int recipeIndex = 0; // เลือก recipe ผ่าน Inspector

    private void Start()
    {
        if (recipeLoader == null || recipeLoader.recipeCollection == null || recipeLoader.recipeCollection.recipes.Length == 0)
        {
            Debug.LogError("[CookingManager] No recipes loaded!");
            return;
        }

        // Limit index to available recipes
        recipeIndex = Mathf.Clamp(recipeIndex, 0, recipeLoader.recipeCollection.recipes.Length - 1);

        // Start cooking test
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

        // Cooking success
        inventory.AddItem(recipe.resultId, 1);
        Debug.Log($"[CookingManager] Successfully cooked {recipe.recipeName}!");
    }
}
