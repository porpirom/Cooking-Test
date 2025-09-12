using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientView : MonoBehaviour
{
    #region Inspector References
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image iconImage;
    #endregion

    #region Public Methods
    /// <summary>
    /// Set the ingredient display, showing player's current amount vs required amount, and update icon.
    /// </summary>
    public void SetData(string id, int playerAmount, int requiredAmount, Sprite icon)
    {
        // Color player's amount red if not enough, white otherwise
        string playerColor = playerAmount < requiredAmount ? "red" : "white";
        amountText.text = $"<color={playerColor}>{playerAmount}</color>/{requiredAmount}";

        if (icon != null)
            iconImage.sprite = icon;
    }
    #endregion
}
