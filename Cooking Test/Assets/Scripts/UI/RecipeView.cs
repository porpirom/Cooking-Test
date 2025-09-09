using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RecipeView : MonoBehaviour
{
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private Image recipeIconImage;
    [SerializeField] private Button selectButton;

    public RecipeData RecipeData { get; private set; }
    public event Action<RecipeData> OnRecipeSelected;

    public void SetData(RecipeData recipe, Sprite icon, string displayName)
    {
        RecipeData = recipe;
        recipeNameText.text = displayName;
        if (icon != null && recipeIconImage != null)
            recipeIconImage.sprite = icon;
    }

    private void Awake()
    {
        selectButton.onClick.AddListener(() => OnRecipeSelected?.Invoke(RecipeData));
    }
}
