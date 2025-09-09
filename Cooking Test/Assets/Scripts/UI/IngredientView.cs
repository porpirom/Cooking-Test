using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientView : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image iconImage;

    public void SetData(string id, int playerAmount, int requiredAmount, Sprite icon)
    {
        // ใช้ Rich Text แค่เปลี่ยนสีของจำนวนในกระเป๋า
        string playerColor = playerAmount < requiredAmount ? "red" : "white";
        amountText.text = $"<color={playerColor}>{playerAmount}</color>/{requiredAmount}";

        if (icon != null)
            iconImage.sprite = icon;
    }
}
