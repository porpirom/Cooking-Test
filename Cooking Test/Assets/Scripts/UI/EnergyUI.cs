using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private Slider energyBar;
    [SerializeField] private TMP_Text energyText;

    private void OnEnable()
    {
        if (energySystem != null)
            energySystem.OnEnergyChanged += UpdateUI;
    }

    private void OnDisable()
    {
        if (energySystem != null)
            energySystem.OnEnergyChanged -= UpdateUI;
    }

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
}
