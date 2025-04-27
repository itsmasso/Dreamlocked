using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sounds3DSOList", menuName = "Scriptable Objects/Sounds3DSOList")]
public class Sounds3DSOList : ScriptableObject
{
    public List<Sound3DSO> sound3DSOList = new List<Sound3DSO>();
}
