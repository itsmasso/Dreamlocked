using System;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct ItemData : INetworkSerializable, IEquatable<ItemData>
{
    public int id;
    public float itemCharge;
    public int usesRemaining;
    public int uniqueId;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref itemCharge);
        serializer.SerializeValue(ref usesRemaining);
        serializer.SerializeValue(ref uniqueId);
    }
    // REQUIRED for NetworkList to work
    public bool Equals(ItemData other)
    {
        return 
               id == other.id &&
               itemCharge == other.itemCharge &&
               usesRemaining == other.usesRemaining &&
               uniqueId == other.uniqueId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(id, itemCharge, usesRemaining, uniqueId);
    }
}
