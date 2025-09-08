using UnityEngine;
using System.IO;
using System;

public class RecipeLoader : MonoBehaviour
{
    [SerializeField] private string jsonFileName = "recipes.json";
    public RecipeCollection recipeCollection;

    public event Action OnRecipesLoaded;

    private void Start()
    {
        LoadRecipes();
    }

    private void LoadRecipes()
    {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            recipeCollection = JsonUtility.FromJson<RecipeCollection>(json);
            Debug.Log($"[RecipeLoader] Loaded {recipeCollection.recipes.Length} recipes");

            OnRecipesLoaded?.Invoke();
        }
        else
        {
            Debug.LogError("[RecipeLoader] JSON file not found at " + path);
        }
    }
}
