using System;

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
}

[Serializable]
public class RecipeCollection
{
    public RecipeData[] recipes;
}
