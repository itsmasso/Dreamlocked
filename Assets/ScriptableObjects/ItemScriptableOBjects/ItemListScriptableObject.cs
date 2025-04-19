using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemListScriptableObject", menuName = "Scriptable Objects/ItemListScriptableObject")]
public class ItemListScriptableObject : ScriptableObject
{
    public List<ItemScriptableObject> itemListSO;
}

