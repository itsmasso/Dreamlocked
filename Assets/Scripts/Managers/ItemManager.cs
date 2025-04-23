using System.Collections.Generic;
using UnityEngine;

public class ItemManager : NetworkSingleton<ItemManager>
{
    private int currentItemId = 0;
    private HashSet<int> assignedIds = new HashSet<int>();
    private Dictionary<int, ItemData> itemDataMap = new();
    public void RegisterItem(ItemData data)
    {
        if (!itemDataMap.ContainsKey(data.uniqueId)){
            itemDataMap.Add(data.uniqueId, data);
            //Debug.Log("added" + data.uniqueId);
         } else
            itemDataMap[data.uniqueId] = data;
    }

    public void UnregisterItem(int uniqueId)
    {
        itemDataMap.Remove(uniqueId);
        ReclaimItemId(uniqueId);
    }

    public bool TryGetItem(int uniqueId, out ItemData item)
    {
        return itemDataMap.TryGetValue(uniqueId, out item);
    }

    public ItemData GetItem(int uniqueId)
    {
        itemDataMap.TryGetValue(uniqueId, out var item);
        return item;
    }
    public int GenerateUniqueItemId()
    {
        if (!IsServer)
        {
            Debug.LogError("Only the server should generate unique item IDs!");
            return -1;
        }

        currentItemId++;
        assignedIds.Add(currentItemId);
        return currentItemId;
    }

    public void ReclaimItemId(int id)
    {
        assignedIds.Remove(id);
    }
    public void ClearItemRegistry()
    {
        assignedIds.Clear();
        itemDataMap.Clear();
    }
    public override void OnDestroy()
    {
        ClearItemRegistry();
    }
}
