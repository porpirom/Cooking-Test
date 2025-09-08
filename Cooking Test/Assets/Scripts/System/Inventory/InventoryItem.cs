using System;

[Serializable]
public class InventoryItem
{
    public string id;
    public int amount;
}

[Serializable]
public class InventorySaveData
{
    public InventoryItem[] items;
}
