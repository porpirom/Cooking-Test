using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeView : MonoBehaviour
{
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private Button selectButton;
    public RecipeData RecipeData { get; private set; }
    public event Action<RecipeData> OnRecipeSelected;

    public void SetData(RecipeData recipe)
    {
        RecipeData = recipe;
        recipeNameText.text = recipe.recipeName;
    }

    private void Awake()
    {
        selectButton.onClick.AddListener(() => OnRecipeSelected?.Invoke(RecipeData));
    }
}

