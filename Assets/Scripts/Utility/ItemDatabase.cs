using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ItemDatabase
{
    private static Dictionary<int, ItemScriptableObject> _items;

    public static void Initialize()
    {
        var loadedItems = Resources.LoadAll<ItemScriptableObject>("Items");
        Debug.Log($"[ItemDatabase] Attempting to load items from Resources/Items...");

        if (loadedItems == null || loadedItems.Length == 0)
        {
            Debug.LogWarning("[ItemDatabase] No ItemScriptableObjects found! Did you remember to create them?");
            return;
        }

        _items = new Dictionary<int, ItemScriptableObject>();

        foreach (var item in loadedItems)
        {
            if (_items.ContainsKey(item.id))
            {
                Debug.LogError($"[ItemDatabase] Duplicate ID {item.id} found in {item.name} — skipping.");
                continue;
            }

            Debug.Log($"[ItemDatabase] Loaded item: {item.name}, ID: {item.id}");
            _items.Add(item.id, item);
        }

        Debug.Log($"[ItemDatabase] Final count: {_items.Count} items loaded.");
    }

    public static ItemScriptableObject Get(int id) => _items.TryGetValue(id, out var def) ? def : null;
}