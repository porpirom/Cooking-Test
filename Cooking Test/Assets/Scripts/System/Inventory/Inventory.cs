using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // Private dictionary for runtime use
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // Read-only access
    public IReadOnlyDictionary<string, int> Items => items;

    // Check if enough item exists
    public bool HasItem(string id, int amount)
    {
        return items.ContainsKey(id) && items[id] >= amount;
    }

    public void AddItem(string id, int amount)
    {
        if (!items.ContainsKey(id))
            items[id] = 0;

        items[id] += amount;
        Debug.Log($"[Inventory] Added {amount} x {id}. Total: {items[id]}");

        SaveToJson(Path.Combine(Application.streamingAssetsPath, "player_inventory.json"));
    }

    public bool RemoveItem(string id, int amount)
    {
        if (!HasItem(id, amount))
        {
            Debug.LogWarning($"[Inventory] Not enough {id} to remove.");
            return false;
        }

        items[id] -= amount;
        if (items[id] <= 0)
            items.Remove(id);

        Debug.Log($"[Inventory] Removed {amount} x {id}. Remaining: {items.GetValueOrDefault(id, 0)}");

        SaveToJson(Path.Combine(Application.streamingAssetsPath, "player_inventory.json"));
        return true;
    }


    // Save inventory to JSON file
    public void SaveToJson(string path)
    {
        InventorySaveData saveData = new InventorySaveData();
        saveData.items = new InventoryItem[items.Count];

        int i = 0;
        foreach (var kv in items)
        {
            saveData.items[i] = new InventoryItem { id = kv.Key, amount = kv.Value };
            i++;
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(path, json);
        //Debug.Log($"[Inventory] Saved to {path}");
    }

    // Load inventory from JSON file
    public void LoadFromJson(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Inventory] JSON file not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

        items.Clear();
        foreach (var item in saveData.items)
        {
            items[item.id] = item.amount;
        }

        //Debug.Log($"[Inventory] Loaded {items.Count} items from {path}");
    }

    // Debug helper: print all items
    public void PrintAllItems()
    {
        Debug.Log("[Inventory] Current Items:");
        foreach (var kv in items)
        {
            Debug.Log($"{kv.Key}: {kv.Value}");
        }
    }
}
