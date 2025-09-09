using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string id;
    public string name;
    public string iconPath; // path in Resources
    [NonSerialized] public Sprite icon;
}

[Serializable]
public class ItemCollection
{
    public ItemData[] items;
}

public class ItemDatabase : MonoBehaviour
{
    private Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();

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

    public Sprite GetIcon(string itemId)
    {
        if (itemDict.TryGetValue(itemId, out var item))
            return item.icon;
        return null;
    }

    public string GetName(string itemId)
    {
        if (itemDict.TryGetValue(itemId, out var item))
            return item.name;
        return itemId;
    }
}
