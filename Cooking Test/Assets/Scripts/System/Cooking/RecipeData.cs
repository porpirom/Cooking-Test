using System;
using UnityEngine;

/// <summary>
/// Represents an ingredient requirement for a recipe.
/// </summary>
[Serializable]
public class IngredientRequirement
{
    public string id;               // Item ID
    public int amount;              // Required amount
    public string iconPath;         // Path to icon (JSON/Resources)

    [NonSerialized]
    public Sprite icon;             // Loaded at runtime
}

/// <summary>
/// Data structure for a cooking recipe.
/// </summary>
[Serializable]
public class RecipeData
{
    public string recipeName;
    public int energyCost;
    public IngredientRequirement[] ingredients;
    public string resultId;             // Resulting item ID

    [Header("Cooking Time")]
    public int cookingTimeSeconds = 10; // Default to 10s

    [Range(1, 3)]
    public int starRating = 1;          // Difficulty or quality rating

    public string recipeIconPath;       // Path to recipe icon (JSON/Resources)

    [NonSerialized]
    public Sprite recipeIcon;           // Loaded at runtime
}

/// <summary>
/// Collection of multiple recipes.
/// </summary>
[Serializable]
public class RecipeCollection
{
    public RecipeData[] recipes;
}
