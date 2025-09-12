using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RecipeView : MonoBehaviour
{
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private Image recipeIconImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Button selectButton;

    [Header("Stars")]
    [SerializeField] private Transform starContainer;
    [SerializeField] private GameObject starPrefab;

    [Header("Frames")]
    [SerializeField] private Sprite normalFrame;
    [SerializeField] private Sprite highlightedFrame;

    public RecipeData RecipeData { get; private set; }
    public event Action<RecipeData> OnRecipeSelected;

    public void SetData(RecipeData recipe, Sprite icon, string displayName)
    {
        RecipeData = recipe;
        recipeNameText.text = displayName;
        if (icon != null)
            recipeIconImage.sprite = icon;

        SetupStars(recipe.starRating);
        SetHighlight(false); // เริ่มต้นเป็นกรอบปกติ
    }

    private void SetupStars(int starCount)
    {
        foreach (Transform child in starContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < starCount; i++)
            Instantiate(starPrefab, starContainer);
    }

    public void SetHighlight(bool highlighted)
    {
        if (frameImage != null)
            frameImage.sprite = highlighted ? highlightedFrame : normalFrame;
    }

    private void Awake()
    {
        selectButton.onClick.AddListener(() => OnRecipeSelected?.Invoke(RecipeData));
    }
}
