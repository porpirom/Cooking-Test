using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Loads recipe data from a JSON file in StreamingAssets.
/// </summary>
public class RecipeLoader : MonoBehaviour
{
    [Header("JSON Settings")]
    [SerializeField] private string jsonFileName = "recipes.json";

    /// <summary>
    /// Loaded recipes at runtime.
    /// </summary>
    public RecipeCollection recipeCollection;

    /// <summary>
    /// Event invoked when recipes are successfully loaded.
    /// </summary>
    public event Action OnRecipesLoaded;

    private void Start()
    {
        LoadRecipes();
    }

    /// <summary>
    /// Loads recipes from JSON file.
    /// </summary>
    private void LoadRecipes()
    {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[RecipeLoader] JSON file not found at {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            recipeCollection = JsonUtility.FromJson<RecipeCollection>(json);

            if (recipeCollection?.recipes == null || recipeCollection.recipes.Length == 0)
            {
                Debug.LogWarning("[RecipeLoader] No recipes found in the JSON file.");
            }

            OnRecipesLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RecipeLoader] Failed to load recipes: {ex.Message}");
        }
    }
}
