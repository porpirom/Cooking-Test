using UnityEngine;
using System;
using System.Collections;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private int maxEnergy = 30;
    [SerializeField] private int currentEnergy = 10;
    [SerializeField] private float regenInterval = 5f;

    private Coroutine regenCoroutine;

    // Event: fired whenever energy changes
    public event Action<int, int> OnEnergyChanged;

    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;
    public bool IsFull => currentEnergy >= maxEnergy;
    public bool IsEmpty => currentEnergy <= 0;

    private void Start()
    {
        if (!IsFull)
            regenCoroutine = StartCoroutine(RegenerateEnergy());

        // Trigger initial update
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    private IEnumerator RegenerateEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenInterval);

            if (currentEnergy < maxEnergy)
            {
                currentEnergy++;
                Debug.Log($"[Energy Regen] {currentEnergy}/{maxEnergy}");
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

                if (IsFull)
                {
                    Debug.Log("[Energy] Full, stop regen");
                    yield break;
                }
            }
        }
    }

    public bool HasEnergy(int amount) => currentEnergy >= amount;

    public bool UseEnergy(int amount)
    {
        if (!HasEnergy(amount))
        {
            Debug.LogWarning("[Energy] Not enough!");
            return false;
        }

        currentEnergy -= amount;
        Debug.Log($"[Energy Used] -{amount} | {currentEnergy}/{maxEnergy}");
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

        if (regenCoroutine == null)
            regenCoroutine = StartCoroutine(RegenerateEnergy());

        return true;
    }

    public void AddEnergy(int amount)
    {
        int before = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);

        Debug.Log($"[Energy Add] +{amount} ({before} → {currentEnergy}/{maxEnergy})");
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

        if (IsFull && regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
    }
}
