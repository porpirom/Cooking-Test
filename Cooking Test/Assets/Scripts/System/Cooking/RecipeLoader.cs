using UnityEngine;
using System.IO;

public class RecipeLoader : MonoBehaviour
{
    [SerializeField] private string jsonFileName = "recipes.json";
    public RecipeCollection recipeCollection;

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
            foreach (var recipe in recipeCollection.recipes)
            {
                Debug.Log($"- {recipe.recipeName} needs {recipe.energyCost} energy, result: {recipe.resultId}");
            }
        }
        else
        {
            Debug.LogError("[RecipeLoader] JSON file not found at " + path);
        }
    }
}
