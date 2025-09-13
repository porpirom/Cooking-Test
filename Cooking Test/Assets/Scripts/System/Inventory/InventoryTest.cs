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
        path = Path.Combine(Application.persistentDataPath, "player_inventory.json");

        // Load inventory from JSON
        inventory.LoadFromJson(path);

        // Test add/remove
        /*inventory.AddItem("egg", 2);
        inventory.AddItem("vegetable", 2);
        inventory.AddItem("rice", 2);
        inventory.AddItem("carrot", 2);*/

        //inventory.RemoveItem("egg", 120);
        //inventory.RemoveItem("vegetable", 1);
        //inventory.RemoveItem("rice", 1);
        //inventory.RemoveItem("carrot", 1);

        // Print current inventory
        //inventory.PrintAllItems();

        // Save back to JSON
        inventory.SaveToJson(path);
    }
}

