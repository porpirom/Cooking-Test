using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string id;
    public string name;
    public string iconPath; // Path in Resources
    [NonSerialized] public Sprite icon; // Loaded at runtime
}

[Serializable]
public class ItemCollection
{
    public ItemData[] items;
}

public class ItemDatabase : MonoBehaviour
{
    #region Private Fields
    private Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();
    #endregion

    #region Public Methods
    /// <summary>
    /// Loads item data from a JSON file and loads associated icons from Resources.
    /// </summary>
    public void LoadFromJson(string path)
    {
        string json = File.ReadAllText(path);
        ItemCollection collection = JsonUtility.FromJson<ItemCollection>(json);

        foreach (var item in collection.items)
        {
            item.icon = Resources.Load<Sprite>(item.iconPath);
            itemDict[item.id] = item;
        }
    }

    /// <summary>
    /// Returns the Sprite icon for the given item ID, or null if not found.
    /// </summary>
    public Sprite GetIcon(string itemId)
    {
        if (itemDict.TryGetValue(itemId, out var item))
            return item.icon;
        return null;
    }

    /// <summary>
    /// Returns the name of the item for the given item ID, or the ID itself if not found.
    /// </summary>
    public string GetName(string itemId)
    {
        if (itemDict.TryGetValue(itemId, out var item))
            return item.name;
        return itemId;
    }
    #endregion
}
