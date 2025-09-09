using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientView : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image iconImage;

    public void SetData(IngredientRequirement ingredient, Sprite icon)
    {
        amountText.text = ingredient.amount.ToString();
        iconImage.sprite = icon;
    }
}
