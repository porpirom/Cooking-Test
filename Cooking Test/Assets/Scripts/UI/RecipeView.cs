using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RecipeView : MonoBehaviour
{
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private Image recipeIconImage;
    [SerializeField] private Image frameImage; // เพิ่มตรงนี้
    [SerializeField] private Button selectButton;

    public RecipeData RecipeData { get; private set; }
    public event Action<RecipeData> OnRecipeSelected;

    public void SetData(RecipeData recipe, Sprite icon, string displayName)
    {
        RecipeData = recipe;
        recipeNameText.text = displayName;
        if (icon != null)
            recipeIconImage.sprite = icon;
    }

    public void SetFrame(Sprite frame)
    {
        if (frameImage != null)
            frameImage.sprite = frame;
    }

    private void Awake()
    {
        selectButton.onClick.AddListener(() => OnRecipeSelected?.Invoke(RecipeData));
    }
}
