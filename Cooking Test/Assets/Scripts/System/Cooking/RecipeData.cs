using System;
using UnityEngine;

[Serializable]
public class IngredientRequirement
{
    public string id;
    public int amount;
}

[Serializable]
public class RecipeData
{
    public string recipeName;
    public int energyCost;
    public IngredientRequirement[] ingredients;
    public string resultId;

    [Header("Cooking Time")]
    public int cookingTimeSeconds = 10; // default 10s

    [Range(1, 3)]
    public int starRating = 1;
}

[Serializable]
public class RecipeCollection
{
    public RecipeData[] recipes;
}
