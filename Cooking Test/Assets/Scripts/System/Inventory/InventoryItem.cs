using System;

/// <summary>
/// Represents a single inventory entry with an item ID and amount.
/// </summary>
[Serializable]
public class InventoryItem
{
    public string id;

    /// <summary>
    /// Quantity of the item. Must be non-negative.
    /// </summary>
    public int amount;
}

/// <summary>
/// Serializable wrapper for saving/loading inventory to JSON.
/// </summary>
[Serializable]
public class InventorySaveData
{
    /// <summary>
    /// Array of inventory items.
    /// </summary>
    public InventoryItem[] items;
}
