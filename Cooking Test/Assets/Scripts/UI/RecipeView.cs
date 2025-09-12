using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class RecipeView : MonoBehaviour
{
    #region Inspector References
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
    #endregion

    #region Properties & Events
    // Expose RecipeData read-only for other scripts
    public RecipeData RecipeData { get; private set; }
    // Event triggered when this recipe is selected
    public event Action<RecipeData> OnRecipeSelected;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        // Bind select button to trigger OnRecipeSelected event
        if (selectButton != null)
            selectButton.onClick.AddListener(() => OnRecipeSelected?.Invoke(RecipeData));
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Sets the recipe display, including icon, name, and stars.
    /// </summary>
    public void SetData(RecipeData recipe, Sprite icon, string displayName)
    {
        RecipeData = recipe;
        recipeNameText.text = displayName;

        if (icon != null)
            recipeIconImage.sprite = icon;

        SetupStars(recipe.starRating);
        SetHighlight(false);
    }

    /// <summary>
    /// Updates the frame to highlighted or normal state.
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (frameImage != null)
            frameImage.sprite = highlighted ? highlightedFrame : normalFrame;
    }
    #endregion

    #region Private Methods
    // Instantiates star icons based on star count
    private void SetupStars(int starCount)
    {
        foreach (Transform child in starContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < starCount; i++)
            Instantiate(starPrefab, starContainer);
    }
    #endregion
}
