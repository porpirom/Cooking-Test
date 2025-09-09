using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventoryTest : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    private string path;

    private void Start()
    {
        path = Path.Combine(Application.streamingAssetsPath, "player_inventory.json");

        // Load inventory from JSON
        inventory.LoadFromJson(path);

        // Test add/remove
        //inventory.AddItem("mushroom", 2);
        //inventory.AddItem("water", 2);

        //inventory.RemoveItem("water", 1);

        // Print current inventory
        //inventory.PrintAllItems();

        // Save back to JSON
        inventory.SaveToJson(path);
    }
}

