using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Represents a player's inventory. Handles adding, removing, saving, and loading items.
/// Fully encapsulated with events for UI updates.
/// </summary>
public class Inventory : MonoBehaviour
{
    #region Fields

    // Private dictionary for runtime use
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // Default save path
    private string InventoryPath => Path.Combine(Application.persistentDataPath, "player_inventory.json");

    #endregion

    #region Properties

    /// <summary>
    /// Provides read-only access to the inventory items.
    /// </summary>
    public IReadOnlyDictionary<string, int> Items => items;

    #endregion

    #region Events

    /// <summary>
    /// Invoked whenever the inventory changes (add/remove/load).
    /// </summary>
    public event System.Action OnInventoryChanged;

    #endregion

    #region Public Methods

    /// <summary>
    /// Checks if the inventory contains at least the specified amount of an item.
    /// </summary>
    public bool HasItem(string id, int amount)
    {
        return items.ContainsKey(id) && items[id] >= amount;
    }

    /// <summary>
    /// Adds a specified amount of an item to the inventory. Auto-saves and triggers event.
    /// </summary>
    public void AddItem(string id, int amount)
    {
        if (amount <= 0) return; // Safety check

        if (!items.ContainsKey(id)) items[id] = 0;
        items[id] += amount;

        SaveToJson();
        OnInventoryChanged?.Invoke();

        // Debug.Log($"[Inventory] Added {amount} of {id}. New count: {items[id]}");
    }

    /// <summary>
    /// Removes a specified amount of an item. Returns false if not enough quantity.
    /// Auto-saves and triggers event if successful.
    /// </summary>
    public bool RemoveItem(string id, int amount)
    {
        if (amount <= 0) return false; // Safety check
        if (!HasItem(id, amount)) return false;

        items[id] -= amount;
        if (items[id] <= 0) items.Remove(id);

        SaveToJson();
        OnInventoryChanged?.Invoke();

        // Debug.Log($"[Inventory] Removed {amount} of {id}. Remaining: {GetItemCount(id)}");
        return true;
    }

    /// <summary>
    /// Returns the current quantity of a specific item. Returns 0 if not present.
    /// </summary>
    public int GetItemCount(string id)
    {
        return items.TryGetValue(id, out int count) ? count : 0;
    }

    /// <summary>
    /// Debug helper: prints all items in the console.
    /// </summary>
    public void PrintAllItems()
    {
        Debug.Log("[Inventory] Current Items:");
        foreach (var kv in items)
        {
            Debug.Log($"{kv.Key}: {kv.Value}");
        }
    }

    #endregion

    #region Persistence (Save/Load)

    [System.Serializable]
    private class InventoryItem
    {
        public string id;
        public int amount;
    }

    [System.Serializable]
    private class InventorySaveData
    {
        public InventoryItem[] items;
    }

    /// <summary>
    /// Saves the current inventory to JSON.
    /// </summary>
    public void SaveToJson(string path = null)
    {
        path ??= InventoryPath;

        try
        {
            InventorySaveData saveData = new InventorySaveData
            {
                items = new InventoryItem[items.Count]
            };

            int i = 0;
            foreach (var kv in items)
            {
                saveData.items[i] = new InventoryItem { id = kv.Key, amount = kv.Value };
                i++;
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);

            // Debug.Log($"[Inventory] Saved {items.Count} items to {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Inventory] Failed to save inventory: {e.Message}");
        }
    }

    /// <summary>
    /// Loads inventory from JSON. Clears current inventory first.
    /// </summary>
    public void LoadFromJson(string path = null)
    {
        path ??= InventoryPath;

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Inventory] JSON file not found: {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

            items.Clear();
            foreach (var item in saveData.items)
            {
                if (item.amount > 0)
                    items[item.id] = item.amount;
            }

            OnInventoryChanged?.Invoke();
            // Debug.Log($"[Inventory] Loaded {items.Count} items from {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Inventory] Failed to load inventory: {e.Message}");
        }
    }

    #endregion
}
