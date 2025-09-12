using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyUI : MonoBehaviour
{
    #region Inspector References
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Slider energyBar;
    [SerializeField] private TMP_Text energyText;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        // Subscribe to energy changes
        if (energySystem != null)
            energySystem.OnEnergyChanged += UpdateUI;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (energySystem != null)
            energySystem.OnEnergyChanged -= UpdateUI;
    }
    #endregion

    #region UI Updates
    private void UpdateUI(int current, int max)
    {
        if (energyBar != null)
        {
            energyBar.maxValue = max;
            energyBar.value = current;
        }

        if (energyText != null)
        {
            energyText.text = $"{current}/{max}";
        }
    }
    #endregion
}
