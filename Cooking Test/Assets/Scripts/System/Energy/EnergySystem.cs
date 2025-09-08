using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private int maxEnergy = 30;
    [SerializeField] private float regenInterval = 5f;

    private int currentEnergy; // ไม่ SerializeField → ไม่เปลี่ยนจาก Inspector
    private Coroutine regenCoroutine;
    private string energyPath;

    public event Action<int, int> OnEnergyChanged;

    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;
    public bool IsFull => currentEnergy >= maxEnergy;
    public bool IsEmpty => currentEnergy <= 0;

    private void Awake()
    {
        energyPath = Path.Combine(Application.streamingAssetsPath, "player_energy.json");

        if (File.Exists(energyPath))
        {
            LoadEnergy(energyPath);
            Debug.Log($"[Energy] Loaded from JSON: {currentEnergy}/{maxEnergy}");
        }
        else
        {
            currentEnergy = maxEnergy; // default value
            SaveEnergy(energyPath);
            Debug.Log($"[Energy] JSON not found, creating default: {currentEnergy}/{maxEnergy}");
        }
    }

    private void Start()
    {
        if (!IsFull)
            regenCoroutine = StartCoroutine(RegenCoroutine());

        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    private IEnumerator RegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(regenInterval);

            if (currentEnergy < maxEnergy)
            {
                currentEnergy++;
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
                SaveEnergy(energyPath);

                if (IsFull)
                {
                    regenCoroutine = null;
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
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        SaveEnergy(energyPath);

        if (regenCoroutine == null)
            regenCoroutine = StartCoroutine(RegenCoroutine());

        return true;
    }

    public void AddEnergy(int amount)
    {
        int before = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        SaveEnergy(energyPath);

        if (IsFull && regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        Debug.Log($"[Energy] Added {amount} ({before} → {currentEnergy}/{maxEnergy})");
    }

    #region Save/Load
    [Serializable]
    private class EnergySaveData { public int currentEnergy; }

    public void SaveEnergy(string path)
    {
        EnergySaveData saveData = new EnergySaveData { currentEnergy = currentEnergy };
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(path, json);
    }

    public void LoadEnergy(string path)
    {
        string json = File.ReadAllText(path);
        EnergySaveData saveData = JsonUtility.FromJson<EnergySaveData>(json);
        currentEnergy = Mathf.Clamp(saveData.currentEnergy, 0, maxEnergy);
    }
    #endregion
}
