using System;
using UnityEngine;

public interface IUseableItem<T>
{
    public void UseItem(){}
    public void InitializeData(ItemData data){}
    T GetData();
    event Action<T> OnDataChanged;
}
