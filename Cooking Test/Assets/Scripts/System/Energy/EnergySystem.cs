using UnityEngine;
using System;
using System.IO;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy Settings")]
    [SerializeField] private int maxEnergy = 30;
    [SerializeField] private float regenInterval = 5f; // วินาทีต่อ 1 energy

    private int currentEnergy;
    private string energyPath;
    private long lastUpdateUnix;

    public event Action<int, int> OnEnergyChanged;

    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;
    public bool IsFull => currentEnergy >= maxEnergy;
    public bool IsEmpty => currentEnergy <= 0;

    private void Awake()
    {
        energyPath = Path.Combine(Application.persistentDataPath, "player_energy.json");

        if (File.Exists(energyPath))
        {
            LoadEnergy(energyPath);
        }
        else
        {
            currentEnergy = maxEnergy;
            lastUpdateUnix = TimeManager.Instance.ToUnix(TimeManager.Instance.UtcNow);
            SaveEnergy(energyPath);
            Debug.Log($"[Energy] JSON not found, creating default: {currentEnergy}/{maxEnergy}");
        }
    }

    private void Start()
    {
        RecalculateEnergy();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    private void Update()
    {
        if (currentEnergy < maxEnergy)
        {
            RecalculateEnergy();
        }
    }

    private void RecalculateEnergy()
    {
        DateTime now = TimeManager.Instance.UtcNow;
        long nowUnix = TimeManager.Instance.ToUnix(now);
        long elapsedSeconds = nowUnix - lastUpdateUnix;

        if (elapsedSeconds >= regenInterval)
        {
            int recovered = (int)(elapsedSeconds / regenInterval);
            if (recovered > 0)
            {
                int oldEnergy = currentEnergy;
                currentEnergy = Mathf.Min(currentEnergy + recovered, maxEnergy);
                lastUpdateUnix += recovered * (long)regenInterval;

                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
                SaveEnergy(energyPath);

                Debug.Log($"[Energy] Regenerated {currentEnergy - oldEnergy}, now {currentEnergy}/{maxEnergy}");
            }
        }
    }

    public bool HasEnergy(int amount) => currentEnergy >= amount;

    public bool UseEnergy(int amount)
    {
        RecalculateEnergy();

        if (!HasEnergy(amount))
        {
            Debug.LogWarning("[Energy] Not enough!");
            return false;
        }

        currentEnergy -= amount;
        lastUpdateUnix = TimeManager.Instance.ToUnix(TimeManager.Instance.UtcNow);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        SaveEnergy(energyPath);

        return true;
    }

    public void AddEnergy(int amount)
    {
        RecalculateEnergy();

        int before = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        lastUpdateUnix = TimeManager.Instance.ToUnix(TimeManager.Instance.UtcNow);

        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        SaveEnergy(energyPath);

        Debug.Log($"[Energy] Added {amount} ({before} → {currentEnergy}/{maxEnergy})");
    }

    #region Save/Load
    [Serializable]
    private class EnergySaveData
    {
        public int currentEnergy;
        public long lastUpdateUnix;
    }

    public void SaveEnergy(string path)
    {
        EnergySaveData saveData = new EnergySaveData
        {
            currentEnergy = currentEnergy,
            lastUpdateUnix = lastUpdateUnix
        };
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(path, json);
    }

    public void LoadEnergy(string path)
    {
        string json = File.ReadAllText(path);
        EnergySaveData saveData = JsonUtility.FromJson<EnergySaveData>(json);
        currentEnergy = Mathf.Clamp(saveData.currentEnergy, 0, maxEnergy);
        lastUpdateUnix = saveData.lastUpdateUnix;
    }
    #endregion
}
